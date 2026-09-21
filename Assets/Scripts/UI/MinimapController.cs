using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SundarbanGuardian.Core;
using SundarbanGuardian.Data;

namespace SundarbanGuardian.UI
{
    /// <summary>
    /// Scrolling / Panning GPS Navigation Minimap using NASA Worldview satellite imagery.
    ///
    /// The minimap crops a small portion of the high-res Landsat delta image,
    /// centering on the player via RawImage.uvRect offsetting.
    /// The player chevron marker remains FIXED at the center, pointing in heading direction.
    /// Hazard markers pan relative to the player inside the rounded mask.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [System.Serializable]
        public class MarkerItem
        {
            public RectTransform rectTransform;
            public float worldX;
            public float worldZ;
            public string riskTier;
            public Image fillImage;
            public Image ringImage;
            public RectTransform pulseRingRt;
            public Image pulseRingImage;
            public Color baseColor;
            public bool isDangerZone;
        }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Satellite Image Lat/Lon Bounds")]
        public float boundsSouth = 21.8f;
        public float boundsNorth = 23.9f;
        public float boundsWest  = 89.5f;
        public float boundsEast  = 91.0f;

        [Header("World-Space Mapping Calibration")]
        public float mapWorldMinX = -200f;
        public float mapWorldMaxX =  140f;
        public float mapWorldMinZ = -160f;
        public float mapWorldMaxZ =  240f;

        [Header("Minimap Appearance & Panning")]
        [Tooltip("Side length of the minimap square frame in pixels")]
        public float minimapSize    = 185f;
        [Tooltip("Fraction of the full satellite texture shown across the minimap viewport")]
        [Range(0.05f, 0.5f)]
        public float cropSize       = 0.22f;
        public float markerSize     = 12f;
        public float playerDotSize  = 16f;

        [Header("Source Satellite Texture")]
        public Texture2D satelliteMapTexture;

        // ── Internal References ────────────────────────────────────────────────
        private Canvas          _canvas;
        private RectTransform   _mapRect;
        private RawImage        _rawImage;
        private RectTransform   _markerLayer;
        private RectTransform   _playerMarker;
        private Transform       _playerTf;
        private Text            _coordText;
        private readonly List<MarkerItem> _hazardMarkers = new List<MarkerItem>();

        // ── Coordinate helpers ────────────────────────────────────────────────

        public Vector2 WorldToUV(float worldX, float worldZ)
        {
            float u = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinX, mapWorldMaxX, worldX));
            float v = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinZ, mapWorldMaxZ, worldZ));
            return new Vector2(u, v);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            BuildUI();
            BuildHazardMarkers();
            BuildPlayerMarker();
        }

        private void LateUpdate()
        {
            if (_playerTf == null) _playerTf = FindPlayer();
            if (_playerTf == null || _rawImage == null) return;

            Vector3 pos = _playerTf.position;
            Vector2 playerUV = WorldToUV(pos.x, pos.z);

            // 1. Pan the RawImage via uvRect centered on player
            float uMin = playerUV.x - (cropSize * 0.5f);
            float vMin = playerUV.y - (cropSize * 0.5f);
            _rawImage.uvRect = new Rect(uMin, vMin, cropSize, cropSize);

            // 2. Player marker stays fixed at exact center of frame
            if (_playerMarker != null)
            {
                _playerMarker.anchoredPosition = Vector2.zero;
                _playerMarker.localEulerAngles = new Vector3(0f, 0f, -_playerTf.eulerAngles.y);
            }

            // 3. Pan hazard markers relative to player
            float halfSpan = minimapSize * 0.5f;
            for (int i = 0; i < _hazardMarkers.Count; i++)
            {
                var m = _hazardMarkers[i];
                if (m == null || m.rectTransform == null) continue;

                Vector2 mUV = WorldToUV(m.worldX, m.worldZ);
                float deltaU = mUV.x - playerUV.x;
                float deltaV = mUV.y - playerUV.y;

                float px = (deltaU / cropSize) * minimapSize;
                float py = (deltaV / cropSize) * minimapSize;
                m.rectTransform.anchoredPosition = new Vector2(px, py);

                if (m.isDangerZone)
                {
                    float t = Time.time;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                    if (m.fillImage != null) m.fillImage.color = new Color(m.baseColor.r, m.baseColor.g, m.baseColor.b, Mathf.Lerp(0.18f, 0.32f, pulse));
                    if (m.ringImage != null) m.ringImage.color = new Color(m.baseColor.r, m.baseColor.g, m.baseColor.b, Mathf.Lerp(0.72f, 0.98f, pulse));
                    if (m.pulseRingRt != null && m.pulseRingImage != null)
                    {
                        float shockwave = (t * 1.3f) % 1.0f;
                        m.pulseRingRt.localScale = Vector3.one * (1.0f + shockwave * 0.16f);
                        m.pulseRingImage.color = new Color(m.baseColor.r, m.baseColor.g, m.baseColor.b, (1f - shockwave) * 0.45f);
                    }
                }

                bool inView = Mathf.Abs(px) <= (halfSpan + 45f) && Mathf.Abs(py) <= (halfSpan + 45f);
                if (m.rectTransform.gameObject.activeSelf != inView)
                    m.rectTransform.gameObject.SetActive(inView);
            }

            // 4. Update GPS coordinates
            if (_coordText != null)
            {
                float lat = boundsSouth + playerUV.y * (boundsNorth - boundsSouth);
                float lon = boundsWest  + playerUV.x * (boundsEast  - boundsWest);
                _coordText.text = $"LAT: {lat:F2}°N | LON: {lon:F2}°E";
            }
        }

        // ── UI Construction ───────────────────────────────────────────────────

        void BuildUI()
        {
            _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas == null)
            {
                var cGO = new GameObject("MinimapCanvas");
                _canvas = cGO.AddComponent<Canvas>();
                _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 20;
                cGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cGO.AddComponent<GraphicRaycaster>();
            }

            // Load high-res NASA satellite image
            if (satelliteMapTexture == null)
            {
#if UNITY_EDITOR
                satelliteMapTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/nasa_sundarbans_landsat.jpg");
                if (satelliteMapTexture == null)
                    satelliteMapTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sundarban_map.jpeg");
#endif
                if (satelliteMapTexture == null)
                    satelliteMapTexture = Resources.Load<Texture2D>("Textures/nasa_sundarbans_landsat");
            }

            Sprite roundedSprite = null;
#if UNITY_EDITOR
            roundedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/rounded_card_12px.png");
#endif

            // ── Dark container card ─────────────────────────────────────
            var panel     = new GameObject("Minimap_Panel");
            panel.transform.SetParent(_canvas.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0f, 1f);
            panelRect.anchorMax        = new Vector2(0f, 1f);
            panelRect.pivot            = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -16f);
            panelRect.sizeDelta        = new Vector2(minimapSize + 16f, minimapSize + 56f);

            var bgImg = panel.AddComponent<Image>();
            if (roundedSprite != null) { bgImg.sprite = roundedSprite; bgImg.type = Image.Type.Sliced; }
            bgImg.color = new Color(0.04f, 0.07f, 0.09f, 0.88f);

            // Header title
            var titleGO = new GameObject("Minimap_Header", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(panel.transform, false);
            var titleText = titleGO.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 10;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "🛰️ NASA LANDSAT • DELTA RADAR";
            titleText.color = new Color(0.30f, 0.95f, 0.70f);
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -5f);
            titleRt.sizeDelta = new Vector2(0f, 16f);

            // ── Mask viewport ──────────────────────────────────────────
            var maskGO = new GameObject("Minimap_Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(panel.transform, false);
            var maskRt = maskGO.GetComponent<RectTransform>();
            maskRt.anchorMin = new Vector2(0.5f, 1f);
            maskRt.anchorMax = new Vector2(0.5f, 1f);
            maskRt.pivot = new Vector2(0.5f, 1f);
            maskRt.anchoredPosition = new Vector2(0f, -24f);
            maskRt.sizeDelta = new Vector2(minimapSize, minimapSize);

            var maskImg = maskGO.GetComponent<Image>();
            if (roundedSprite != null) { maskImg.sprite = roundedSprite; maskImg.type = Image.Type.Sliced; }
            maskImg.color = Color.white;
            var maskComp = maskGO.GetComponent<Mask>();
            maskComp.showMaskGraphic = false;

            // ── Satellite RawImage (Pans via uvRect) ───────────────────
            var imgGO = new GameObject("Minimap_SatelliteImage", typeof(RectTransform), typeof(RawImage));
            imgGO.transform.SetParent(maskGO.transform, false);
            _mapRect = imgGO.GetComponent<RectTransform>();
            _mapRect.anchorMin = Vector2.zero;
            _mapRect.anchorMax = Vector2.one;
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapRect.sizeDelta = Vector2.zero;

            _rawImage = imgGO.GetComponent<RawImage>();
            _rawImage.texture = satelliteMapTexture;
            _rawImage.uvRect = new Rect(0.4f, 0.4f, cropSize, cropSize);

            // ── Marker container layer ─────────────────────────────────
            var markerGO = new GameObject("Marker_Layer", typeof(RectTransform));
            markerGO.transform.SetParent(maskGO.transform, false);
            _markerLayer = markerGO.GetComponent<RectTransform>();
            _markerLayer.anchorMin = new Vector2(0.5f, 0.5f);
            _markerLayer.anchorMax = new Vector2(0.5f, 0.5f);
            _markerLayer.pivot = new Vector2(0.5f, 0.5f);
            _markerLayer.anchoredPosition = Vector2.zero;
            _markerLayer.sizeDelta = Vector2.zero;

            // ── Outer border rim with compass ticks ─────────────────────
            var borderGO = new GameObject("Minimap_Border", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(panel.transform, false);
            var borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = maskRt.anchorMin;
            borderRect.anchorMax = maskRt.anchorMax;
            borderRect.pivot = maskRt.pivot;
            borderRect.anchoredPosition = maskRt.anchoredPosition;
            borderRect.sizeDelta = maskRt.sizeDelta;

            var borderImg = borderGO.GetComponent<Image>();
            if (roundedSprite != null) { borderImg.sprite = roundedSprite; borderImg.type = Image.Type.Sliced; }
            borderImg.color = new Color(0f, 0f, 0f, 0f);
            var borderOutline = borderGO.AddComponent<Outline>();
            borderOutline.effectColor = new Color(0.25f, 0.85f, 0.60f, 0.80f);
            borderOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // ── Coordinates display ────────────────────────────────────
            var coordGO = new GameObject("Minimap_Coords", typeof(RectTransform), typeof(Text));
            coordGO.transform.SetParent(panel.transform, false);
            var coordRt = coordGO.GetComponent<RectTransform>();
            coordRt.anchorMin = new Vector2(0f, 0f);
            coordRt.anchorMax = new Vector2(1f, 0f);
            coordRt.pivot = new Vector2(0.5f, 0f);
            coordRt.anchoredPosition = new Vector2(0f, 14f);
            coordRt.sizeDelta = new Vector2(0f, 14f);
            _coordText = coordGO.GetComponent<Text>();
            _coordText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _coordText.fontSize = 9;
            _coordText.color = new Color(0.85f, 0.92f, 0.96f, 0.95f);
            _coordText.alignment = TextAnchor.MiddleCenter;

            // ── NASA Credit Line (Mandatory attribution) ────────────────
            var creditGO = new GameObject("Minimap_Credit", typeof(RectTransform), typeof(Text));
            creditGO.transform.SetParent(panel.transform, false);
            var creditRt = creditGO.GetComponent<RectTransform>();
            creditRt.anchorMin = new Vector2(0f, 0f);
            creditRt.anchorMax = new Vector2(1f, 0f);
            creditRt.pivot = new Vector2(0.5f, 0f);
            creditRt.anchoredPosition = new Vector2(0f, 2f);
            creditRt.sizeDelta = new Vector2(0f, 12f);
            var creditText = creditGO.GetComponent<Text>();
            creditText.text = "Satellite imagery: NASA Worldview (worldview.earthdata.nasa.gov)";
            creditText.fontSize = 8;
            creditText.color = new Color(0.65f, 0.78f, 0.85f, 0.75f);
            creditText.alignment = TextAnchor.MiddleCenter;
            creditText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            creditText.raycastTarget = false;
        }

        void BuildHazardMarkers()
        {
            if (_markerLayer == null) return;

            var gm = FindAnyObjectByType<GameManager>();
            LevelData ld = gm != null ? gm.CurrentLevelData : null;

            if (ld != null && ld.hazardZones != null && ld.hazardZones.Count > 0)
            {
                foreach (var zone in ld.hazardZones)
                {
                    SpawnMarker(zone.centerX, zone.centerZ, ZoneColor(zone.riskTier), markerSize, zone.zoneId + "_dot", zone.riskTier);
                }
                return;
            }

            foreach (var zt in FindObjectsByType<HazardZoneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SpawnMarker(zt.transform.position.x, zt.transform.position.z, ZoneColor(zt.riskTier), markerSize, zt.name + "_dot", zt.riskTier);
            }
        }

        void BuildPlayerMarker()
        {
            if (_rawImage == null) return;

            var pMarkerGo = new GameObject("Player_Marker", typeof(RectTransform));
            pMarkerGo.transform.SetParent(_rawImage.transform.parent, false);
            _playerMarker = pMarkerGo.GetComponent<RectTransform>();
            _playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
            _playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _playerMarker.pivot = new Vector2(0.5f, 0.5f);
            _playerMarker.anchoredPosition = Vector2.zero;
            _playerMarker.sizeDelta = new Vector2(playerDotSize + 8f, playerDotSize + 8f);

            // Pulse ring
            var ring = new GameObject("Pulse_Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(pMarkerGo.transform, false);
            var ringRect = ring.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(playerDotSize + 4f, playerDotSize + 4f);
            var ringImg = ring.GetComponent<Image>();
            ringImg.color = new Color(0.20f, 0.95f, 0.55f, 0.35f);

            // Center Navigation Dot
            var dot = new GameObject("Center_Dot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(pMarkerGo.transform, false);
            var dotRect = dot.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(playerDotSize - 4f, playerDotSize - 4f);
            var dotImg = dot.GetComponent<Image>();
            dotImg.color = Color.white;
            var dotOutline = dot.AddComponent<Outline>();
            dotOutline.effectColor = new Color(0.05f, 0.25f, 0.12f, 0.95f);
            dotOutline.effectDistance = new Vector2(1.2f, -1.2f);

            // Heading pointer arrow
            var arrow = new GameObject("Heading_Pointer", typeof(RectTransform), typeof(Image));
            arrow.transform.SetParent(pMarkerGo.transform, false);
            var arrowRt = arrow.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRt.pivot = new Vector2(0.5f, 0f);
            arrowRt.anchoredPosition = new Vector2(0f, 2f);
            arrowRt.sizeDelta = new Vector2(4f, 8f);
            var arrowImg = arrow.GetComponent<Image>();
            arrowImg.color = new Color(0.20f, 0.98f, 0.55f, 1f);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        void SpawnMarker(float worldX, float worldZ, Color col, float radiusOrSize, string objName, string riskTier)
        {
            float worldWidth = mapWorldMaxX - mapWorldMinX;
            float worldDiam = Mathf.Max(radiusOrSize * 2f, 22f);
            float pixelDiameter = Mathf.Clamp((worldDiam / worldWidth / cropSize) * minimapSize, 48f, 105f);

            var go = new GameObject(objName, typeof(RectTransform));
            go.transform.SetParent(_markerLayer != null ? _markerLayer : _rawImage.transform.parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(pixelDiameter, pixelDiameter);

            var fillSprite = SundarbanGuardian.Core.HudController.GetOrCreateCircleSprite(128, false);
            var ringSprite = SundarbanGuardian.Core.HudController.GetOrCreateCircleSprite(128, true, 0.085f);

            // 1. Transparent Red Fill Circle
            var fillGo = new GameObject("Fill_Circle", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.color = new Color(col.r, col.g, col.b, 0.24f);
            fillImg.raycastTarget = false;

            // 2. Neon Red Outer Ring
            var ringGo = new GameObject("Border_Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(go.transform, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.sizeDelta = Vector2.zero;
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = ringSprite;
            ringImg.color = new Color(col.r, col.g, col.b, 0.88f);
            ringImg.raycastTarget = false;

            // 3. Pulse / Shockwave Ring
            var pulseGo = new GameObject("Pulse_Ring", typeof(RectTransform), typeof(Image));
            pulseGo.transform.SetParent(go.transform, false);
            var pulseRt = pulseGo.GetComponent<RectTransform>();
            pulseRt.anchorMin = Vector2.zero;
            pulseRt.anchorMax = Vector2.one;
            pulseRt.sizeDelta = Vector2.zero;
            var pulseImg = pulseGo.GetComponent<Image>();
            pulseImg.sprite = ringSprite;
            pulseImg.color = new Color(col.r, col.g, col.b, 0.45f);
            pulseImg.raycastTarget = false;

            // 4. Center Warning Core
            var coreGo = new GameObject("Center_Dot", typeof(RectTransform), typeof(Image));
            coreGo.transform.SetParent(go.transform, false);
            var coreRt = coreGo.GetComponent<RectTransform>();
            coreRt.anchorMin = new Vector2(0.5f, 0.5f);
            coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.sizeDelta = new Vector2(7f, 7f);
            var coreImg = coreGo.GetComponent<Image>();
            coreImg.sprite = fillSprite;
            coreImg.color = new Color(1f, 1f, 1f, 0.95f);
            coreImg.raycastTarget = false;
            var coreOutl = coreGo.AddComponent<Outline>();
            coreOutl.effectColor = new Color(col.r * 0.7f, 0f, 0f, 0.95f);
            coreOutl.effectDistance = new Vector2(1f, -1f);

            _hazardMarkers.Add(new MarkerItem
            {
                rectTransform = rt,
                worldX = worldX,
                worldZ = worldZ,
                riskTier = riskTier,
                fillImage = fillImg,
                ringImage = ringImg,
                pulseRingRt = pulseRt,
                pulseRingImage = pulseImg,
                baseColor = col,
                isDangerZone = true
            });
        }

        static Color ZoneColor(string risk) => risk switch
        {
            "high"   => new Color(0.94f, 0.18f, 0.14f, 0.95f),
            "medium" => new Color(1.00f, 0.54f, 0.08f, 0.92f),
            "low"    => new Color(0.90f, 0.84f, 0.10f, 0.90f),
            _        => new Color(0.30f, 0.85f, 0.35f, 0.88f),
        };

        Transform FindPlayer()
        {
            var go = GameObject.FindWithTag("Player") ?? GameObject.Find("Player")
                                                      ?? GameObject.Find("Hilsha_Fish")
                                                      ?? GameObject.Find("Tiger_Player")
                                                      ?? GameObject.Find("Deer_Player");
            return go != null ? go.transform : null;
        }

        public void RefreshMarkers()
        {
            foreach (var m in _hazardMarkers)
                if (m != null && m.rectTransform != null) Destroy(m.rectTransform.gameObject);
            _hazardMarkers.Clear();
            BuildHazardMarkers();
        }
    }
}
