using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SundarbanGuardian.Data;
using SundarbanGuardian.Inventory;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Core
{
    public class HudController : MonoBehaviour
    {
        [Header("System References")]
        public GameManager gameManager;
        public SpeciesHealth activeHealth;

        private Canvas mainCanvas;
        private RectTransform healthFillRect;
        private Image healthFillImage;
        private Text healthValueText;
        private Text speciesBadgeText;

        // Hazard Popup Elements
        private GameObject hazardPopupPanel;
        private Image hazardBadgeImage;
        private Text hazardBadgeText;
        private Text hazardValueText;
        private Text hazardSubText;
        private CanvasGroup hazardCanvasGroup;
        private bool isHazardVisible = false;

        // Minimap Elements (Scrolling / Panning NASA Landsat Navigation Radar)
        [Header("Minimap Configuration")]
        public Texture2D satelliteMapTexture;
        public float minimapCropSize = 0.22f; // UV span (approx 4.5x zoom in on player)
        public float minimapFrameSize = 185f; // Side length of the masked radar in screen px

        private RectTransform minimapContainer;
        private RectTransform minimapMaskRect;
        private RectTransform playerMarker;
        private RectTransform markerLayer;
        private Text coordDisplayText;
        private Text nasaCreditText;
        private RectTransform mapContentRect;
        private RawImage mapSatelliteImage;

        [System.Serializable]
        public class MinimapMarkerInfo
        {
            public RectTransform rectTransform;
            public float worldX;
            public float worldZ;
            public string markerType;
            public float worldRadius;
            public Image fillImage;
            public Image ringImage;
            public RectTransform pulseRingRt;
            public Image pulseRingImage;
            public Color baseColor;
            public bool isDangerZone;
        }
        private readonly System.Collections.Generic.List<MinimapMarkerInfo> activeMinimapMarkers = new System.Collections.Generic.List<MinimapMarkerInfo>();

        private static Sprite _cachedCircleFillSprite;
        private static Sprite _cachedCircleRingSprite;

        public static Sprite GetOrCreateCircleSprite(int size = 128, bool isRing = false, float ringThickness = 0.08f)
        {
            if (!isRing && _cachedCircleFillSprite != null) return _cachedCircleFillSprite;
            if (isRing && _cachedCircleRingSprite != null) return _cachedCircleRingSprite;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float center = size * 0.5f;
            float radius = (size * 0.5f) - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    if (isRing)
                    {
                        float innerRadius = radius * (1f - ringThickness);
                        float outerAlpha = Mathf.Clamp01(radius - dist + 1f);
                        float innerAlpha = Mathf.Clamp01(dist - innerRadius + 1f);
                        float a = Mathf.Min(outerAlpha, innerAlpha);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                    else
                    {
                        float a = Mathf.Clamp01(radius - dist + 1f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            if (isRing) _cachedCircleRingSprite = sprite;
            else _cachedCircleFillSprite = sprite;
            return sprite;
        }

        private const float mapWorldMinX = -200f;
        private const float mapWorldMaxX = 140f;
        private const float mapWorldMinZ = -160f;
        private const float mapWorldMaxZ = 240f;

        // Fact Card / End Screen Overlay
        private GameObject factCardPanel;
        private Text factCardTitle;
        private Text factCardBodyBn;
        private Text factCardCitation;
        private RawImage factCardPhoto;
        private Text factCardPhotoCaption;
        private Button nextLevelButton;
        private Button restartButton;
        private Button startMissionButton;

        // Data & Sources Overlay
        private GameObject dataSourcesPanel;
        private Button dataSourcesButton;

        // Backpack HUD Elements
        private GameObject backpackContainer;
        private readonly Button[] toolSlotButtons = new Button[4];
        private readonly Image[] toolSlotBgs = new Image[4];
        private readonly Image[] toolSlotIcons = new Image[4];
        private readonly Text[] toolSlotTexts = new Text[4];
        private readonly Text[] toolSlotCountTexts = new Text[4];
        private readonly Outline[] toolSlotOutlines = new Outline[4];
        private Button useToolButton;
        private Image useToolBtnImg;
        private Text useToolBtnText;
        private Text useToolSubText;
        private Outline useToolBtnOutline;
        private GameObject restorationToastPanel;
        private Text restorationToastText;
        private Coroutine toastCoroutine;
        private Backpack activeBackpack;

        private Transform trackedPlayer;

        private void Awake()
        {
            EnsureCanvasAndBuildUI();
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = Object.FindAnyObjectByType<GameManager>();
            }

            if (gameManager != null)
            {
                gameManager.LevelCompleted += OnLevelCompleted;
                gameManager.GameOver += OnGameOver;
                trackedPlayer = gameManager.playerObject != null ? gameManager.playerObject.transform : null;

                if (speciesBadgeText != null && !string.IsNullOrEmpty(gameManager.currentSpecies))
                {
                    speciesBadgeText.text = gameManager.currentSpecies.ToUpperInvariant().Replace("_", " ");
                }
            }

            HookHealth();
            HudEvents.EnteredHazardZone += ShowHazardNotification;
            HudEvents.ExitedHazardZone += HideHazardNotification;

            if (hazardPopupPanel != null)
                hazardPopupPanel.SetActive(false);

            if (factCardPanel != null)
                factCardPanel.SetActive(false);

            if (dataSourcesPanel != null)
                dataSourcesPanel.SetActive(false);

            HudEvents.EnvironmentalRestoration += ShowRestorationToast;
            HookBackpack();

            PopulateMinimapMarkers();

            string currentSp = gameManager != null ? gameManager.currentSpecies : GameManager.selectedSpecies;
            if (!string.IsNullOrEmpty(currentSp) && currentSp.ToLower().Contains("hilsha"))
            {
                ShowPreLevelBriefing();
            }
        }

        private void HookHealth()
        {
            if (activeHealth == null && gameManager != null)
            {
                activeHealth = gameManager.activeHealth;
            }

            if (activeHealth == null)
            {
                activeHealth = Object.FindAnyObjectByType<SpeciesHealth>();
            }

            if (activeHealth != null)
            {
                activeHealth.HealthChanged += OnHealthChanged;
                OnHealthChanged(activeHealth.currentHealth / Mathf.Max(1f, activeHealth.maxHealth));
            }
        }

        private void Update()
        {
            if (trackedPlayer == null || !trackedPlayer.gameObject.activeInHierarchy)
            {
                if (gameManager != null && gameManager.playerObject != null && gameManager.playerObject.activeInHierarchy)
                {
                    trackedPlayer = gameManager.playerObject.transform;
                }
                else
                {
                    string[] names = { "Deer", "Tiger", "Dolphin", "Hilsha" };
                    foreach (var n in names)
                    {
                        var go = GameObject.Find(n);
                        if (go != null && go.activeInHierarchy)
                        {
                            trackedPlayer = go.transform;
                            break;
                        }
                    }
                }
            }

            if (trackedPlayer != null)
            {
                UpdateMinimapMarker(trackedPlayer.position);
            }

            if (activeBackpack == null || !activeBackpack.gameObject.activeInHierarchy)
            {
                HookBackpack();
            }

            UpdateUseToolButtonState();
        }

        private void OnHealthChanged(float healthRatio)
        {
            healthRatio = Mathf.Clamp01(healthRatio);

            if (healthFillRect != null)
            {
                healthFillRect.anchorMax = new Vector2(healthRatio, 1f);
            }

            if (healthFillImage != null)
            {
                // Vibrant red fill at all health levels (darker crimson when critical)
                healthFillImage.color = Color.Lerp(new Color(0.60f, 0.05f, 0.05f, 1f), new Color(0.95f, 0.15f, 0.15f, 1f), healthRatio);
            }

            if (healthValueText != null)
            {
                float curHp = (activeHealth != null) ? activeHealth.currentHealth : healthRatio * 200f;
                healthValueText.text = $"{Mathf.CeilToInt(curHp)}";
            }
        }

        private void ShowHazardNotification(string riskTier, float rawValue, string unit)
        {
            if (hazardPopupPanel == null) return;

            hazardPopupPanel.SetActive(true);
            isHazardVisible = true;

            Color badgeColor;
            string tierName;

            switch (riskTier?.ToLowerInvariant())
            {
                case "high":
                    badgeColor = new Color(0.9f, 0.15f, 0.15f, 0.95f);
                    tierName = "CRITICAL HAZARD ZONE";
                    break;
                case "medium":
                    badgeColor = new Color(0.95f, 0.55f, 0.05f, 0.95f);
                    tierName = "ELEVATED RISK ZONE";
                    break;
                default:
                    badgeColor = new Color(0.15f, 0.65f, 0.85f, 0.95f);
                    tierName = "SATELLITE MONITORED ZONE";
                    break;
            }

            if (hazardBadgeImage != null) hazardBadgeImage.color = badgeColor;
            if (hazardBadgeText != null) hazardBadgeText.text = tierName;

            if (hazardValueText != null)
            {
                hazardValueText.text = $"Reading: {rawValue:F2} {unit}";
            }

            if (hazardSubText != null)
            {
                if (unit.ToLowerInvariant().Contains("ndvi"))
                    hazardSubText.text = "NASA MODIS (MOD13Q1) Canopy Observation";
                else if (unit.ToLowerInvariant().Contains("psu") || unit.ToLowerInvariant().Contains("salinity"))
                    hazardSubText.text = "NASA PO.DAAC SMAP / BFRI Field Monitoring";
                else
                    hazardSubText.text = "Verified Earth Observation Sensor Data";
            }
        }

        private void HideHazardNotification()
        {
            if (hazardPopupPanel != null)
            {
                hazardPopupPanel.SetActive(false);
                isHazardVisible = false;
            }
        }

        private void UpdateMinimapMarker(Vector3 worldPos)
        {
            if (mapSatelliteImage == null || playerMarker == null) return;

            // 1. Calculate normalized UV coordinates of player in world
            float playerU = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinX, mapWorldMaxX, worldPos.x));
            float playerV = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinZ, mapWorldMaxZ, worldPos.z));

            // 2. Center the RawImage UV rect on the player (scrolling/panning under player)
            float uMin = playerU - (minimapCropSize * 0.5f);
            float vMin = playerV - (minimapCropSize * 0.5f);
            mapSatelliteImage.uvRect = new Rect(uMin, vMin, minimapCropSize, minimapCropSize);

            // 3. Player marker stays FIXED at exact center of minimap frame
            playerMarker.anchoredPosition = Vector2.zero;

            // Rotate player marker with player's heading
            if (trackedPlayer != null)
            {
                playerMarker.localEulerAngles = new Vector3(0f, 0f, -trackedPlayer.eulerAngles.y);
            }

            // 4. Update all hazard/sanctuary/goal markers so they pan relative to the player
            float halfSpan = minimapFrameSize * 0.5f;
            for (int i = 0; i < activeMinimapMarkers.Count; i++)
            {
                var info = activeMinimapMarkers[i];
                if (info == null || info.rectTransform == null) continue;

                float mU = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinX, mapWorldMaxX, info.worldX));
                float mV = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinZ, mapWorldMaxZ, info.worldZ));

                float deltaU = mU - playerU;
                float deltaV = mV - playerV;

                // Relative pixel offset from frame center
                float px = (deltaU / minimapCropSize) * minimapFrameSize;
                float py = (deltaV / minimapCropSize) * minimapFrameSize;

                info.rectTransform.anchoredPosition = new Vector2(px, py);

                // Free Fire Danger Zone Pulse Animation
                if (info.isDangerZone)
                {
                    float t = Time.time;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                    if (info.fillImage != null)
                    {
                        info.fillImage.color = new Color(info.baseColor.r, info.baseColor.g, info.baseColor.b, Mathf.Lerp(0.18f, 0.32f, pulse));
                    }
                    if (info.ringImage != null)
                    {
                        info.ringImage.color = new Color(info.baseColor.r, info.baseColor.g, info.baseColor.b, Mathf.Lerp(0.72f, 0.98f, pulse));
                    }
                    if (info.pulseRingRt != null && info.pulseRingImage != null)
                    {
                        float shockwave = (t * 1.3f) % 1.0f;
                        info.pulseRingRt.localScale = Vector3.one * (1.0f + shockwave * 0.16f);
                        info.pulseRingImage.color = new Color(info.baseColor.r, info.baseColor.g, info.baseColor.b, (1f - shockwave) * 0.45f);
                    }
                }

                // Cull markers that pan far outside frame
                bool isVisible = Mathf.Abs(px) <= (halfSpan + 45f) && Mathf.Abs(py) <= (halfSpan + 45f);
                if (info.rectTransform.gameObject.activeSelf != isVisible)
                {
                    info.rectTransform.gameObject.SetActive(isVisible);
                }
            }

            // 5. Update coordinate readout with authentic GPS telemetry
            if (coordDisplayText != null)
            {
                float lat = 21.8f + playerV * (23.9f - 21.8f);
                float lon = 89.5f + playerU * (91.0f - 89.5f);
                coordDisplayText.text = $"LAT: {lat:F2}°N | LON: {lon:F2}°E  ({worldPos.x:F0}m, {worldPos.z:F0}m)";
            }
        }

        private void OnLevelCompleted()
        {
            ShowEndCard("MISSION ACCOMPLISHED!", new Color(0.1f, 0.7f, 0.3f));
        }

        private void OnGameOver()
        {
            ShowEndCard("SPECIES IN CRITICAL DISTRESS", new Color(0.85f, 0.15f, 0.15f));
        }

        public void ShowPreLevelBriefing()
        {
            if (factCardPanel == null) return;

            if (startMissionButton != null) startMissionButton.gameObject.SetActive(true);
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
            if (restartButton != null) restartButton.gameObject.SetActive(false);

            string sp = gameManager != null ? gameManager.currentSpecies : GameManager.selectedSpecies;
            UpdateFactCardPhoto(sp);

            if (factCardTitle != null)
            {
                factCardTitle.text = "মা ইলিশ রক্ষা অভিযান (MOTHER HILSA BRIEFING)";
                factCardTitle.color = new Color(0.25f, 0.95f, 0.75f);
            }

            LevelData data = gameManager != null ? gameManager.CurrentLevelData : null;
            if (data != null && !string.IsNullOrEmpty(data.factTextBn))
            {
                if (factCardBodyBn != null) factCardBodyBn.text = data.factTextBn;
                if (factCardCitation != null) factCardCitation.text = $"Data Source Citation:\n{data.dataSource}";
            }
            else
            {
                if (factCardBodyBn != null)
                {
                    factCardBodyBn.text = "মা ইলিশ রক্ষা অভিযান (Mother Hilsa Conservation):\n\n" +
                                          "• অক্টোবর মাসে ২২ দিন মা ইলিশ ধরা ও বিক্রি বন্ধ থাকে।\n" +
                                          "  [October Spawning Ban: 22-day total prohibition on catching or selling mother hilsa]\n\n" +
                                          "• ১ নভেম্বর থেকে ৩০ জুন ছোট জাটকা মাছ ধরা নিষেধ।\n" +
                                          "  [Jatka Protection: 8-month ban on catching juvenile hilsa under 25cm]\n\n" +
                                          "• আইন অমান্য করলে ২ বছর জেল ও ৫ লাখ টাকা জরিমানা।\n" +
                                          "  [Legal Penalty: Up to 2 years imprisonment and 500,000 BDT fine]\n\n" +
                                          "• ২০২৪ সালের তথ্য: এই পদক্ষেপে ৫২% মা ইলিশ নিরাপদে ডিম ছেড়েছে।\n" +
                                          "  [2024 Impact Data: 52% of mother hilsa successfully spawned safely]\n\n" +
                                          "• নদীর বিপদ: অবৈধ কারেন্ট জাল এড়িয়ে নিরাপদে সাঁতার কাটুন!\n" +
                                          "  [Mission Hazard: Evade illegal monofilament gillnets to protect the spawning migration]";
                }
                if (factCardCitation != null)
                {
                    factCardCitation.text = "Data Source Citation:\nMinistry of Fisheries and Livestock, Bangladesh — Mother Hilsa Conservation Campaign & Jatka Protection Act";
                }
            }

            factCardPanel.SetActive(true);
        }

        private void ShowEndCard(string title, Color headerColor)
        {
            if (factCardPanel == null) return;

            if (startMissionButton != null) startMissionButton.gameObject.SetActive(false);
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(true);
            if (restartButton != null) restartButton.gameObject.SetActive(true);

            factCardPanel.SetActive(true);

            string sp = gameManager != null ? gameManager.currentSpecies : GameManager.selectedSpecies;
            UpdateFactCardPhoto(sp);

            if (factCardTitle != null)
            {
                if (!string.IsNullOrEmpty(sp) && sp.ToLower().Contains("hilsha") && title.Contains("MISSION ACCOMPLISHED"))
                {
                    factCardTitle.text = "মিশন সফল — মা ইলিশ রক্ষা পেয়েছে! (MISSION ACCOMPLISHED)";
                    factCardTitle.color = new Color(0.2f, 0.95f, 0.45f);
                }
                else
                {
                    factCardTitle.text = title;
                    factCardTitle.color = headerColor;
                }
            }

            LevelData data = gameManager != null ? gameManager.CurrentLevelData : null;
            if (data != null)
            {
                if (factCardBodyBn != null && !string.IsNullOrEmpty(data.factTextBn))
                {
                    factCardBodyBn.text = data.factTextBn;
                }

                if (factCardCitation != null && !string.IsNullOrEmpty(data.dataSource))
                {
                    factCardCitation.text = $"Data Source Citation:\n{data.dataSource}";
                }
            }
            else
            {
                if (factCardBodyBn != null)
                {
                    factCardBodyBn.text = "মিশন সম্পন্ন — মা ইলিশ নিরাপদ স্থানে পৌঁছেছে!\n\n" +
                                          "• আপনি সফলভাবে নিষিদ্ধ কারেন্ট জাল ও শিকারী এড়িয়ে মা ইলিশকে রক্ষা করেছেন।\n" +
                                          "  [You successfully evaded illegal gillnets and predators to protect the spawning hilsa.]\n\n" +
                                          "• ২০২৪ তথ্য: সংরক্ষণ অভিযানের কারণে ৫২% মা ইলিশ নিরাপদে ডিম পাড়তে পেরেছিল।\n" +
                                          "  [2024 Survey: Conservation enforcement enabled 52% of mother hilsa to spawn safely.]\n\n" +
                                          "• সুন্দরবনের নদী ও মোহনা রক্ষা আমাদের সবার দায়িত্ব।\n" +
                                          "  [Safeguarding the rivers of the Sundarbans is a shared national responsibility.]";
                }
                if (factCardCitation != null)
                {
                    factCardCitation.text = "Data Source Citation:\nMinistry of Fisheries and Livestock, Bangladesh — Mother Hilsa Conservation Campaign & Jatka Protection Act";
                }
            }
        }

        private void UpdateFactCardPhoto(string speciesName)
        {
            if (factCardPhoto == null) return;

            string s = (speciesName ?? "").ToLower();
            string path = "Assets/Textures/real_sundarban_river.jpg";
            string caption = "📷 সুন্দরবনের নদীপথ ও জলজ মোহনা\nREAL SUNDARBAN ESTUARY";

            if (s.Contains("tiger") || s.Contains("বাঘ"))
            {
                path = "Assets/Textures/real_sundarban_tiger.jpg";
                caption = "📷 রয়েল বেঙ্গল টাইগার (ক্যামেরা ট্র্যাপ)\nROYAL BENGAL TIGER FIELD RECORD";
            }
            else if (s.Contains("deer") || s.Contains("হরিণ"))
            {
                path = "Assets/Textures/real_sundarban_deer.jpg";
                caption = "📷 সুন্দরবনের চিত্রা হরিণ পাল\nSPOTTED DEER (CHITAL) HERD";
            }
            else if (s.Contains("dolphin") || s.Contains("ডলফিন"))
            {
                path = "Assets/Textures/real_sundarban_dolphin.jpg";
                caption = "📷 গাঙ্গেয় ডলফিন অভয়ারণ্য\nGANGES RIVER DOLPHIN RECORD";
            }
            else if (s.Contains("hilsha") || s.Contains("ইলিশ"))
            {
                path = "Assets/Textures/Hilsha_Reference.png";
                caption = "📷 মা ইলিশ প্রজনন সংরক্ষণ\nMOTHER HILSA CONSERVATION";
            }
            else
            {
                path = "Assets/Textures/real_sundarban_mangrove.jpg";
                caption = "📷 সুন্দরবনের শ্বাসমূল ও ম্যানগ্রোভ বন\nDENSE MANGROVE CANOPY & ROOTS";
            }

#if UNITY_EDITOR
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) factCardPhoto.texture = tex;
#endif
            if (factCardPhotoCaption != null) factCardPhotoCaption.text = caption;
        }

        public void EnsureCanvasAndBuildUI()
        {
            GameObject canvasGo = GameObject.Find("HUD_Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("HUD_Canvas");
                mainCanvas = canvasGo.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                mainCanvas = canvasGo.GetComponent<Canvas>();
                for (int i = canvasGo.transform.childCount - 1; i >= 0; i--)
                {
                    var child = canvasGo.transform.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            Font defaultFont = Resources.Load<Font>("Fonts/SegoeUI");
#if UNITY_EDITOR
            if (defaultFont == null)
            {
                defaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SegoeUI.ttf");
            }
#endif
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Font kalpurushFont = Resources.Load<Font>("Fonts/Kalpurush");
#if UNITY_EDITOR
            if (kalpurushFont == null)
            {
                kalpurushFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Kalpurush.ttf");
            }
#endif
            if (kalpurushFont == null) kalpurushFont = defaultFont;

            BuildTopLeftMinimap(canvasGo.transform, defaultFont);
            BuildTopLeftHealthBar(canvasGo.transform, defaultFont);
            BuildHazardPopup(canvasGo.transform, defaultFont);
            BuildFactCardOverlay(canvasGo.transform, kalpurushFont);
            BuildDataSourcesScreen(canvasGo.transform, defaultFont);
            BuildBackpackHUD(canvasGo.transform, defaultFont);
            BuildRestorationToast(canvasGo.transform, defaultFont);
            BuildTouchControls(canvasGo.transform, defaultFont);

            // Popups & overlays must be hidden by default
            if (hazardPopupPanel != null) hazardPopupPanel.SetActive(false);
            if (factCardPanel != null) factCardPanel.SetActive(false);
            if (dataSourcesPanel != null) dataSourcesPanel.SetActive(false);
            if (restorationToastPanel != null) restorationToastPanel.SetActive(false);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Sundarban/Bake Transparent HUD to Scene")]
        public static void BakeHUDToSceneMenu()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            if (hud == null)
            {
                var hudGo = GameObject.Find("HUD");
                if (hudGo == null) hudGo = new GameObject("HUD");
                hud = hudGo.GetComponent<HudController>();
                if (hud == null) hud = hudGo.AddComponent<HudController>();
            }
            hud.EnsureCanvasAndBuildUI();
            UnityEditor.EditorUtility.SetDirty(hud.gameObject);
            var canvasGo = GameObject.Find("HUD_Canvas");
            if (canvasGo != null) UnityEditor.EditorUtility.SetDirty(canvasGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[HudController] Successfully baked transparent HUD to scene!");
        }
#endif

        private void BuildTopLeftMinimap(Transform parent, Font font)
        {
            // Load NASA Landsat Sundarbans Satellite Texture
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

            // Load UI Sprites for sleek rounded frame
            Sprite roundedSprite = null;
#if UNITY_EDITOR
            roundedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/rounded_card_12px.png");
#endif
            if (roundedSprite == null) roundedSprite = Resources.Load<Sprite>("Textures/UI/rounded_card_12px");

            // ── 1. Minimap Main Container ──
            GameObject mapBox = new GameObject("Minimap_Container", typeof(RectTransform));
            mapBox.transform.SetParent(parent, false);
            minimapContainer = mapBox.GetComponent<RectTransform>();
            minimapContainer.anchorMin = new Vector2(0f, 1f);
            minimapContainer.anchorMax = new Vector2(0f, 1f);
            minimapContainer.pivot = new Vector2(0f, 1f);
            minimapContainer.anchoredPosition = new Vector2(20f, -20f);
            minimapContainer.sizeDelta = new Vector2(minimapFrameSize + 22f, minimapFrameSize + 76f);

            var containerBg = mapBox.AddComponent<Image>();
            if (roundedSprite != null)
            {
                containerBg.sprite = roundedSprite;
                containerBg.type = Image.Type.Sliced;
            }
            containerBg.color = new Color(0.03f, 0.06f, 0.08f, 0.85f);

            var containerShadow = mapBox.AddComponent<Shadow>();
            containerShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            containerShadow.effectDistance = new Vector2(2f, -3f);

            // ── 2. Top Header ("🛰️ NASA LANDSAT • DELTA RADAR") ──
            GameObject titleGo = new GameObject("Minimap_Header", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(mapBox.transform, false);
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 11;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "🛰️ NASA LANDSAT • DELTA RADAR";
            titleText.color = new Color(0.35f, 0.95f, 0.75f);
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -6f);
            titleRt.sizeDelta = new Vector2(0f, 18f);

            // ── 3. Masked Minimap Viewport (Rounded-Square Mask) ──
            GameObject maskGo = new GameObject("Minimap_Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(mapBox.transform, false);
            minimapMaskRect = maskGo.GetComponent<RectTransform>();
            minimapMaskRect.anchorMin = new Vector2(0.5f, 1f);
            minimapMaskRect.anchorMax = new Vector2(0.5f, 1f);
            minimapMaskRect.pivot = new Vector2(0.5f, 1f);
            minimapMaskRect.anchoredPosition = new Vector2(0f, -28f);
            minimapMaskRect.sizeDelta = new Vector2(minimapFrameSize, minimapFrameSize);

            var maskImg = maskGo.GetComponent<Image>();
            if (roundedSprite != null)
            {
                maskImg.sprite = roundedSprite;
                maskImg.type = Image.Type.Sliced;
            }
            maskImg.color = Color.white;

            var maskComp = maskGo.GetComponent<Mask>();
            maskComp.showMaskGraphic = false;

            // ── 4. Scrolling NASA Satellite Image (RawImage) ──
            GameObject imgGo = new GameObject("Minimap_Satellite_Image", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(maskGo.transform, false);
            mapContentRect = imgGo.GetComponent<RectTransform>();
            mapContentRect.anchorMin = Vector2.zero;
            mapContentRect.anchorMax = Vector2.one;
            mapContentRect.pivot = new Vector2(0.5f, 0.5f);
            mapContentRect.sizeDelta = Vector2.zero;

            mapSatelliteImage = imgGo.GetComponent<RawImage>();
            if (satelliteMapTexture != null)
            {
                mapSatelliteImage.texture = satelliteMapTexture;
            }
            mapSatelliteImage.color = new Color(0.96f, 0.98f, 1.0f, 0.96f);
            mapSatelliteImage.uvRect = new Rect(0.4f, 0.4f, minimapCropSize, minimapCropSize);

            // ── 5. Marker Layer (Holds hazard / sanctuary / goal markers) ──
            GameObject markerLayerGo = new GameObject("Marker_Layer", typeof(RectTransform));
            markerLayerGo.transform.SetParent(maskGo.transform, false);
            markerLayer = markerLayerGo.GetComponent<RectTransform>();
            markerLayer.anchorMin = new Vector2(0.5f, 0.5f);
            markerLayer.anchorMax = new Vector2(0.5f, 0.5f);
            markerLayer.pivot = new Vector2(0.5f, 0.5f);
            markerLayer.anchoredPosition = Vector2.zero;
            markerLayer.sizeDelta = Vector2.zero;

            // ── 6. Fixed Center Player Marker (Navigation Chevron / Arrow) ──
            GameObject pMarkerGo = new GameObject("Player_Marker", typeof(RectTransform));
            pMarkerGo.transform.SetParent(maskGo.transform, false);
            playerMarker = pMarkerGo.GetComponent<RectTransform>();
            playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
            playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
            playerMarker.pivot = new Vector2(0.5f, 0.5f);
            playerMarker.anchoredPosition = Vector2.zero;
            playerMarker.sizeDelta = new Vector2(24f, 24f);

            // Outer Pulse Ring
            GameObject pRing = new GameObject("Pulse_Ring", typeof(RectTransform), typeof(Image));
            pRing.transform.SetParent(pMarkerGo.transform, false);
            var prRt = pRing.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0.5f, 0.5f);
            prRt.anchorMax = new Vector2(0.5f, 0.5f);
            prRt.sizeDelta = new Vector2(18f, 18f);
            var prImg = pRing.GetComponent<Image>();
            prImg.color = new Color(0.20f, 0.95f, 0.55f, 0.35f);

            // Center Navigation Arrow / Dot
            GameObject pDot = new GameObject("Center_Dot", typeof(RectTransform), typeof(Image));
            pDot.transform.SetParent(pMarkerGo.transform, false);
            var pdRt = pDot.GetComponent<RectTransform>();
            pdRt.anchorMin = new Vector2(0.5f, 0.5f);
            pdRt.anchorMax = new Vector2(0.5f, 0.5f);
            pdRt.sizeDelta = new Vector2(10f, 10f);
            var pdImg = pDot.GetComponent<Image>();
            pdImg.color = Color.white;
            var pdOutline = pDot.AddComponent<Outline>();
            pdOutline.effectColor = new Color(0.05f, 0.25f, 0.12f, 0.95f);
            pdOutline.effectDistance = new Vector2(1.2f, -1.2f);

            // Forward Direction Pointer (Heading Chevron / Tip)
            GameObject pTip = new GameObject("Heading_Pointer", typeof(RectTransform), typeof(Image));
            pTip.transform.SetParent(pMarkerGo.transform, false);
            var ptRt = pTip.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 0.5f);
            ptRt.anchorMax = new Vector2(0.5f, 0.5f);
            ptRt.pivot = new Vector2(0.5f, 0f);
            ptRt.anchoredPosition = new Vector2(0f, 3f);
            ptRt.sizeDelta = new Vector2(5f, 8f);
            var ptImg = pTip.GetComponent<Image>();
            ptImg.color = new Color(0.20f, 0.98f, 0.55f, 1f);

            // ── 7. Outer Radar Border Frame (Sits on top of Mask) ──
            GameObject borderFrameGo = new GameObject("Minimap_Border_Frame", typeof(RectTransform), typeof(Image));
            borderFrameGo.transform.SetParent(mapBox.transform, false);
            var bfRt = borderFrameGo.GetComponent<RectTransform>();
            bfRt.anchorMin = minimapMaskRect.anchorMin;
            bfRt.anchorMax = minimapMaskRect.anchorMax;
            bfRt.pivot = minimapMaskRect.pivot;
            bfRt.anchoredPosition = minimapMaskRect.anchoredPosition;
            bfRt.sizeDelta = minimapMaskRect.sizeDelta;

            var bfImg = borderFrameGo.GetComponent<Image>();
            if (roundedSprite != null)
            {
                bfImg.sprite = roundedSprite;
                bfImg.type = Image.Type.Sliced;
            }
            bfImg.color = new Color(0f, 0f, 0f, 0f);
            var bfOutline = borderFrameGo.AddComponent<Outline>();
            bfOutline.effectColor = new Color(0.25f, 0.85f, 0.60f, 0.80f);
            bfOutline.effectDistance = new Vector2(1.8f, -1.8f);

            CreateCompassTick(borderFrameGo.transform, "N", font, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Color(0.98f, 0.88f, 0.35f));
            CreateCompassTick(borderFrameGo.transform, "S", font, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Color(0.70f, 0.80f, 0.85f, 0.7f));
            CreateCompassTick(borderFrameGo.transform, "E", font, new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Color(0.70f, 0.80f, 0.85f, 0.7f));
            CreateCompassTick(borderFrameGo.transform, "W", font, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Color(0.70f, 0.80f, 0.85f, 0.7f));

            // ── 8. Coordinates Readout ──
            GameObject coordGo = new GameObject("Coord_Display", typeof(RectTransform), typeof(Text));
            coordGo.transform.SetParent(mapBox.transform, false);
            coordDisplayText = coordGo.GetComponent<Text>();
            coordDisplayText.font = font;
            coordDisplayText.fontSize = 10;
            coordDisplayText.fontStyle = FontStyle.Normal;
            coordDisplayText.color = new Color(0.85f, 0.92f, 0.96f, 0.95f);
            coordDisplayText.alignment = TextAnchor.MiddleCenter;
            var coordRt = coordGo.GetComponent<RectTransform>();
            coordRt.anchorMin = new Vector2(0f, 0f);
            coordRt.anchorMax = new Vector2(1f, 0f);
            coordRt.pivot = new Vector2(0.5f, 0f);
            coordRt.anchoredPosition = new Vector2(0f, 24f);
            coordRt.sizeDelta = new Vector2(0f, 16f);

            // ── 9. NASA Satellite Imagery Credit Line ──
            GameObject creditGo = new GameObject("NASA_Credit_Text", typeof(RectTransform), typeof(Text));
            creditGo.transform.SetParent(mapBox.transform, false);
            nasaCreditText = creditGo.GetComponent<Text>();
            nasaCreditText.font = font;
            nasaCreditText.fontSize = 8;
            nasaCreditText.fontStyle = FontStyle.Normal;
            nasaCreditText.color = new Color(0.70f, 0.85f, 0.92f, 0.85f);
            nasaCreditText.alignment = TextAnchor.MiddleCenter;
            nasaCreditText.text = "Satellite imagery: NASA Worldview (worldview.earthdata.nasa.gov)";
            var creditRt = creditGo.GetComponent<RectTransform>();
            creditRt.anchorMin = new Vector2(0f, 0f);
            creditRt.anchorMax = new Vector2(1f, 0f);
            creditRt.pivot = new Vector2(0.5f, 0f);
            creditRt.anchoredPosition = new Vector2(0f, 7f);
            creditRt.sizeDelta = new Vector2(0f, 14f);
        }

        private void CreateCompassTick(Transform parent, string label, Font font, Vector2 anchor, Vector2 offset, Color color)
        {
            GameObject go = new GameObject($"Compass_{label}", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(16f, 16f);

            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = 9;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = label;
            var outl = go.AddComponent<Outline>();
            outl.effectColor = Color.black;
            outl.effectDistance = new Vector2(1f, -1f);
        }

        public void PopulateMinimapMarkers()
        {
            if (mapContentRect == null && minimapMaskRect == null) return;

            foreach (var m in activeMinimapMarkers)
            {
                if (m != null && m.rectTransform != null)
                {
                    if (Application.isPlaying) Destroy(m.rectTransform.gameObject);
                    else DestroyImmediate(m.rectTransform.gameObject);
                }
            }
            activeMinimapMarkers.Clear();

            // 1. Hazard zones from current LevelData
            LevelData ld = gameManager != null ? gameManager.CurrentLevelData : null;
            if (ld != null && ld.hazardZones != null && ld.hazardZones.Count > 0)
            {
                foreach (var hz in ld.hazardZones)
                {
                    SpawnMinimapHazardMarker(hz.centerX, hz.centerZ, hz.riskTier, hz.radius);
                }

                if (ld.goalZone != null)
                {
                    SpawnMinimapGoalMarker(ld.goalZone.centerX, ld.goalZone.centerZ);
                }
            }
            else
            {
                var triggers = FindObjectsByType<HazardZoneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var zt in triggers)
                {
                    SpawnMinimapHazardMarker(zt.transform.position.x, zt.transform.position.z, zt.riskTier, 12f);
                }
            }

            // 2. Green Healing Sanctuaries
            var glades = FindObjectsByType<SundarbanGuardian.Environment.HealingZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hz in glades)
            {
                SpawnMinimapGladeMarker(hz.transform.position.x, hz.transform.position.z);
            }

            // Initial update if player exists
            if (trackedPlayer != null)
            {
                UpdateMinimapMarker(trackedPlayer.position);
            }
        }

        private void SpawnMinimapHazardMarker(float worldX, float worldZ, string riskTier, float worldRadius)
        {
            Transform parentTf = markerLayer != null ? markerLayer.transform : (minimapMaskRect != null ? minimapMaskRect.transform : mapContentRect.transform);

            // Free Fire Danger Zone Circle
            // Calculate pixel diameter scaled to world radius
            float worldWidth = mapWorldMaxX - mapWorldMinX;
            float worldDiam = Mathf.Max(worldRadius * 2f, 22f);
            float pixelDiameter = Mathf.Clamp((worldDiam / worldWidth / minimapCropSize) * minimapFrameSize, 48f, 105f);

            GameObject zoneObj = new GameObject($"DangerZone_{riskTier}", typeof(RectTransform));
            zoneObj.transform.SetParent(parentTf, false);
            var rt = zoneObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(pixelDiameter, pixelDiameter);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // Free Fire Danger Zone Color: bright danger red for high, vivid warning amber for medium
            Color mainColor;
            bool isHighRisk = string.Equals(riskTier, "high", System.StringComparison.OrdinalIgnoreCase);
            if (isHighRisk)
            {
                mainColor = new Color(1.0f, 0.12f, 0.12f, 1.0f); // Iconic Free Fire Red
            }
            else if (string.Equals(riskTier, "medium", System.StringComparison.OrdinalIgnoreCase))
            {
                mainColor = new Color(1.0f, 0.48f, 0.08f, 1.0f); // Amber Warning
            }
            else
            {
                mainColor = new Color(0.95f, 0.85f, 0.15f, 1.0f); // Caution Yellow
            }

            var fillSprite = GetOrCreateCircleSprite(128, false);
            var ringSprite = GetOrCreateCircleSprite(128, true, 0.085f);

            // 1. Semi-transparent Red Fill Circle (Free Fire Danger Zone fill)
            var fillGo = new GameObject("Fill_Circle", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(zoneObj.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.24f);
            fillImg.raycastTarget = false;

            // 2. Bright Neon Red Outer Border Ring
            var ringGo = new GameObject("Border_Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(zoneObj.transform, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.sizeDelta = Vector2.zero;
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = ringSprite;
            ringImg.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.88f);
            ringImg.raycastTarget = false;

            // 3. Shockwave / Expanding Pulse Ring
            var pulseGo = new GameObject("Pulse_Ring", typeof(RectTransform), typeof(Image));
            pulseGo.transform.SetParent(zoneObj.transform, false);
            var pulseRt = pulseGo.GetComponent<RectTransform>();
            pulseRt.anchorMin = Vector2.zero;
            pulseRt.anchorMax = Vector2.one;
            pulseRt.sizeDelta = Vector2.zero;
            var pulseImg = pulseGo.GetComponent<Image>();
            pulseImg.sprite = ringSprite;
            pulseImg.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.45f);
            pulseImg.raycastTarget = false;

            // 4. Center Warning Core
            var coreGo = new GameObject("Center_Dot", typeof(RectTransform), typeof(Image));
            coreGo.transform.SetParent(zoneObj.transform, false);
            var coreRt = coreGo.GetComponent<RectTransform>();
            coreRt.anchorMin = new Vector2(0.5f, 0.5f);
            coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.sizeDelta = new Vector2(7f, 7f);
            var coreImg = coreGo.GetComponent<Image>();
            coreImg.sprite = fillSprite;
            coreImg.color = new Color(1f, 1f, 1f, 0.95f);
            coreImg.raycastTarget = false;
            var coreOutl = coreGo.AddComponent<Outline>();
            coreOutl.effectColor = new Color(mainColor.r * 0.7f, 0f, 0f, 0.95f);
            coreOutl.effectDistance = new Vector2(1f, -1f);

            // 5. Danger Zone Tactical Label
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var labelGo = new GameObject("Danger_Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(zoneObj.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 1f);
            labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 1f);
            labelRt.sizeDelta = new Vector2(85f, 14f);
            var labelTxt = labelGo.GetComponent<Text>();
            if (font != null) labelTxt.font = font;
            labelTxt.text = "DANGER ZONE";
            labelTxt.fontSize = 8;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.color = new Color(1f, 0.35f, 0.35f, 0.95f);
            var labelOutl = labelGo.AddComponent<Outline>();
            labelOutl.effectColor = new Color(0.1f, 0f, 0f, 0.95f);
            labelOutl.effectDistance = new Vector2(1f, -1f);

            activeMinimapMarkers.Add(new MinimapMarkerInfo
            {
                rectTransform = rt,
                worldX = worldX,
                worldZ = worldZ,
                markerType = riskTier,
                worldRadius = worldRadius,
                fillImage = fillImg,
                ringImage = ringImg,
                pulseRingRt = pulseRt,
                pulseRingImage = pulseImg,
                baseColor = mainColor,
                isDangerZone = true
            });
        }

        private void SpawnMinimapGladeMarker(float worldX, float worldZ)
        {
            Transform parentTf = markerLayer != null ? markerLayer.transform : (minimapMaskRect != null ? minimapMaskRect.transform : mapContentRect.transform);
            GameObject glade = new GameObject("Minimap_Glade_Dot", typeof(RectTransform), typeof(Image));
            glade.transform.SetParent(parentTf, false);
            var rt = glade.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(14f, 14f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var img = glade.GetComponent<Image>();
            img.color = new Color(0.18f, 0.95f, 0.40f, 0.95f);
            img.raycastTarget = false;

            var outline = glade.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.35f, 0.12f, 0.95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            activeMinimapMarkers.Add(new MinimapMarkerInfo { rectTransform = rt, worldX = worldX, worldZ = worldZ, markerType = "glade" });
        }

        private void SpawnMinimapGoalMarker(float worldX, float worldZ)
        {
            Transform parentTf = markerLayer != null ? markerLayer.transform : (minimapMaskRect != null ? minimapMaskRect.transform : mapContentRect.transform);
            GameObject goal = new GameObject("Minimap_Goal_Marker", typeof(RectTransform), typeof(Image));
            goal.transform.SetParent(parentTf, false);
            var rt = goal.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(15f, 15f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var img = goal.GetComponent<Image>();
            img.color = new Color(1.0f, 0.88f, 0.20f, 0.98f);
            img.raycastTarget = false;

            var outline = goal.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.1f, 0.0f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            activeMinimapMarkers.Add(new MinimapMarkerInfo { rectTransform = rt, worldX = worldX, worldZ = worldZ, markerType = "goal" });
        }

        private void BuildTopLeftHealthBar(Transform parent, Font font)
        {
            // Health bar placed at top-center (matching user diagram: "HP 200")
            GameObject barRoot = new GameObject("HealthBar_Root", typeof(RectTransform));
            barRoot.transform.SetParent(parent, false);
            var rootRt = barRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, -14f);
            rootRt.sizeDelta = new Vector2(260f, 32f);

            // Explicit "HP" Label Badge on Left
            GameObject hpBadgeGo = new GameObject("HP_Badge", typeof(RectTransform), typeof(Image));
            hpBadgeGo.transform.SetParent(barRoot.transform, false);
            var hpBadgeRt = hpBadgeGo.GetComponent<RectTransform>();
            hpBadgeRt.anchorMin = new Vector2(0f, 0f);
            hpBadgeRt.anchorMax = new Vector2(0f, 1f);
            hpBadgeRt.pivot = new Vector2(1f, 0.5f);
            hpBadgeRt.anchoredPosition = new Vector2(-6f, 0f);
            hpBadgeRt.sizeDelta = new Vector2(44f, 0f);
            var hpBadgeImg = hpBadgeGo.GetComponent<Image>();
            hpBadgeImg.color = new Color(0.85f, 0.12f, 0.12f, 0.95f);
            var hpBadgeOutline = hpBadgeGo.AddComponent<Outline>();
            hpBadgeOutline.effectColor = new Color(1.0f, 0.35f, 0.35f, 0.95f);
            hpBadgeOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject hpTxtGo = new GameObject("HP_Text", typeof(RectTransform), typeof(Text));
            hpTxtGo.transform.SetParent(hpBadgeGo.transform, false);
            var hpTxt = hpTxtGo.GetComponent<Text>();
            hpTxt.font = font;
            hpTxt.fontSize = 15;
            hpTxt.fontStyle = FontStyle.Bold;
            hpTxt.alignment = TextAnchor.MiddleCenter;
            hpTxt.color = Color.white;
            hpTxt.text = "HP";
            var hpTxtRt = hpTxtGo.GetComponent<RectTransform>();
            hpTxtRt.anchorMin = Vector2.zero;
            hpTxtRt.anchorMax = Vector2.one;
            hpTxtRt.sizeDelta = Vector2.zero;

            // Bar Background
            GameObject bgGo = new GameObject("Bar_Bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(barRoot.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.18f, 0.04f, 0.04f, 0.85f);
            var bgOutline = bgGo.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.95f, 0.25f, 0.25f, 0.95f);
            bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Bar Fill (Vibrant Red)
            GameObject fillGo = new GameObject("Bar_Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bgGo.transform, false);
            healthFillRect = fillGo.GetComponent<RectTransform>();
            healthFillRect.anchorMin = Vector2.zero;
            healthFillRect.anchorMax = Vector2.one;
            healthFillRect.sizeDelta = Vector2.zero;
            healthFillImage = fillGo.GetComponent<Image>();
            healthFillImage.color = new Color(0.92f, 0.18f, 0.18f, 1f);

            // Health percentage text centered in bar
            GameObject valGo = new GameObject("Health_Text", typeof(RectTransform), typeof(Text));
            valGo.transform.SetParent(barRoot.transform, false);
            healthValueText = valGo.GetComponent<Text>();
            healthValueText.font = font;
            healthValueText.fontSize = 15;
            healthValueText.fontStyle = FontStyle.Bold;
            healthValueText.text = "200";
            healthValueText.color = Color.white;
            healthValueText.alignment = TextAnchor.MiddleCenter;
            var valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = Vector2.zero;
            valRt.anchorMax = Vector2.one;
            valRt.sizeDelta = Vector2.zero;
            var txtShadow = valGo.AddComponent<Outline>();
            txtShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            txtShadow.effectDistance = new Vector2(1f, -1f);
        }

        private void BuildHazardPopup(Transform parent, Font font)
        {
            hazardPopupPanel = new GameObject("Hazard_Popup_Panel", typeof(RectTransform), typeof(Image));
            hazardPopupPanel.transform.SetParent(parent, false);
            var panelRt = hazardPopupPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -54f);
            panelRt.sizeDelta = new Vector2(420f, 85f);

            var panelBg = hazardPopupPanel.GetComponent<Image>();
            panelBg.color = new Color(0.08f, 0.1f, 0.12f, 0.92f);

            var outline = hazardPopupPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.35f, 0.15f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Badge Header (Tier)
            GameObject badgeGo = new GameObject("Tier_Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(hazardPopupPanel.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -12f);
            badgeRt.sizeDelta = new Vector2(260f, 20f);
            hazardBadgeImage = badgeGo.GetComponent<Image>();
            hazardBadgeImage.color = new Color(0.9f, 0.2f, 0.2f);

            GameObject badgeTextGo = new GameObject("Badge_Text", typeof(RectTransform), typeof(Text));
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            hazardBadgeText = badgeTextGo.GetComponent<Text>();
            hazardBadgeText.font = font;
            hazardBadgeText.fontSize = 11;
            hazardBadgeText.fontStyle = FontStyle.Bold;
            hazardBadgeText.alignment = TextAnchor.MiddleCenter;
            hazardBadgeText.color = Color.white;
            hazardBadgeText.text = "NASA SATELLITE HAZARD";
            var btRt = badgeTextGo.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // Value Display
            GameObject valGo = new GameObject("Value_Text", typeof(RectTransform), typeof(Text));
            valGo.transform.SetParent(hazardPopupPanel.transform, false);
            hazardValueText = valGo.GetComponent<Text>();
            hazardValueText.font = font;
            hazardValueText.fontSize = 16;
            hazardValueText.fontStyle = FontStyle.Bold;
            hazardValueText.alignment = TextAnchor.MiddleCenter;
            hazardValueText.color = new Color(1f, 0.95f, 0.7f);
            hazardValueText.text = "Reading: 0.82 NDVI";
            var valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0f, 0.5f);
            valRt.anchorMax = new Vector2(1f, 0.5f);
            valRt.anchoredPosition = new Vector2(0f, -5f);
            valRt.sizeDelta = new Vector2(0f, 22f);

            // Sub text (NASA / Agency attribution)
            GameObject subGo = new GameObject("Sub_Text", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(hazardPopupPanel.transform, false);
            hazardSubText = subGo.GetComponent<Text>();
            hazardSubText.font = font;
            hazardSubText.fontSize = 11;
            hazardSubText.alignment = TextAnchor.MiddleCenter;
            hazardSubText.color = new Color(0.7f, 0.8f, 0.9f);
            hazardSubText.text = "NASA Earth Observation Satellite Data";
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 8f);
            subRt.sizeDelta = new Vector2(0f, 16f);
        }

        private void BuildFactCardOverlay(Transform parent, Font font)
        {
            factCardPanel = new GameObject("Fact_Card_Overlay", typeof(RectTransform), typeof(Image));
            factCardPanel.transform.SetParent(parent, false);
            var panelRt = factCardPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(860f, 540f);

            var panelBg = factCardPanel.GetComponent<Image>();
            panelBg.color = new Color(0.04f, 0.08f, 0.12f, 0.98f);

            var outline = factCardPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.85f, 0.65f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Header Title
            GameObject titleGo = new GameObject("Fact_Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(factCardPanel.transform, false);
            factCardTitle = titleGo.GetComponent<Text>();
            factCardTitle.font = font;
            factCardTitle.fontSize = 22;
            factCardTitle.fontStyle = FontStyle.Bold;
            factCardTitle.alignment = TextAnchor.MiddleCenter;
            factCardTitle.color = new Color(0.3f, 0.95f, 0.75f);
            factCardTitle.text = "মা ইলিশ রক্ষা অভিযান (MOTHER HILSA BRIEFING)";
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -16f);
            titleRt.sizeDelta = new Vector2(-40f, 38f);

            // Authentic Field Photo Preview on Left
            GameObject photoCardGo = new GameObject("Fact_Photo_Card", typeof(RectTransform), typeof(Image));
            photoCardGo.transform.SetParent(factCardPanel.transform, false);
            var pcRt = photoCardGo.GetComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0.04f, 0.23f);
            pcRt.anchorMax = new Vector2(0.31f, 0.88f);
            pcRt.anchoredPosition = Vector2.zero;
            pcRt.sizeDelta = Vector2.zero;
            photoCardGo.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.95f);
            var pcOutline = photoCardGo.AddComponent<Outline>();
            pcOutline.effectColor = new Color(0.25f, 0.85f, 0.65f, 0.75f);
            pcOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject photoGo = new GameObject("Photo_Raw", typeof(RectTransform), typeof(RawImage));
            photoGo.transform.SetParent(photoCardGo.transform, false);
            var pRt = photoGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.05f, 0.22f);
            pRt.anchorMax = new Vector2(0.95f, 0.96f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.sizeDelta = Vector2.zero;
            factCardPhoto = photoGo.GetComponent<RawImage>();

            GameObject photoCapGo = new GameObject("Photo_Caption", typeof(RectTransform), typeof(Text));
            photoCapGo.transform.SetParent(photoCardGo.transform, false);
            var pCapRt = photoCapGo.GetComponent<RectTransform>();
            pCapRt.anchorMin = new Vector2(0.05f, 0.02f);
            pCapRt.anchorMax = new Vector2(0.95f, 0.20f);
            pCapRt.anchoredPosition = Vector2.zero;
            pCapRt.sizeDelta = Vector2.zero;
            factCardPhotoCaption = photoCapGo.GetComponent<Text>();
            factCardPhotoCaption.font = font;
            factCardPhotoCaption.fontSize = 11;
            factCardPhotoCaption.fontStyle = FontStyle.Italic;
            factCardPhotoCaption.alignment = TextAnchor.MiddleCenter;
            factCardPhotoCaption.color = new Color(0.65f, 0.88f, 0.80f);
            factCardPhotoCaption.text = "📷 সুন্দরবনের বাস্তব দৃশ্য\nREAL FIELD PHOTO";

            // Body (Bengali Fact Text)
            GameObject bodyGo = new GameObject("Fact_Body_Bn", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(factCardPanel.transform, false);
            factCardBodyBn = bodyGo.GetComponent<Text>();
            factCardBodyBn.font = font;
            factCardBodyBn.fontSize = 14;
            factCardBodyBn.lineSpacing = 1.25f;
            factCardBodyBn.alignment = TextAnchor.UpperLeft;
            factCardBodyBn.color = new Color(0.94f, 0.95f, 0.95f);
            factCardBodyBn.text = "মা ইলিশ রক্ষা অভিযান (Mother Hilsa Conservation):\n\n" +
                                  "• অক্টোবর মাসে ২২ দিন মা ইলিশ ধরা ও বিক্রি বন্ধ থাকে।\n" +
                                  "  [October Spawning Ban: 22-day total prohibition on catching or selling mother hilsa]\n\n" +
                                  "• ১ নভেম্বর থেকে ৩০ জুন ছোট জাটকা মাছ ধরা নিষেধ।\n" +
                                  "  [Jatka Protection: 8-month ban on catching juvenile hilsa under 25cm]\n\n" +
                                  "• আইন অমান্য করলে ২ বছর জেল ও ৫ লাখ টাকা জরিমানা।\n" +
                                  "  [Legal Penalty: Up to 2 years imprisonment and 500,000 BDT fine]\n\n" +
                                  "• ২০২৪ সালের তথ্য: এই পদক্ষেপে ৫২% মা ইলিশ নিরাপদে ডিম ছেড়েছে।\n" +
                                  "  [2024 Impact Data: 52% of mother hilsa successfully spawned safely]\n\n" +
                                  "• নদীর বিপদ: অবৈধ কারেন্ট জাল এড়িয়ে নিরাপদে সাঁতার কাটুন!\n" +
                                  "  [Mission Hazard: Evade illegal monofilament gillnets to protect the spawning migration]";
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.33f, 0.22f);
            bodyRt.anchorMax = new Vector2(0.96f, 0.88f);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyRt.sizeDelta = Vector2.zero;

            // Citation Sub-Panel (Box with background)
            GameObject citBoxGo = new GameObject("Citation_Box", typeof(RectTransform), typeof(Image));
            citBoxGo.transform.SetParent(factCardPanel.transform, false);
            var citBoxRt = citBoxGo.GetComponent<RectTransform>();
            citBoxRt.anchorMin = new Vector2(0.05f, 0.12f);
            citBoxRt.anchorMax = new Vector2(0.95f, 0.205f);
            citBoxRt.anchoredPosition = Vector2.zero;
            citBoxRt.sizeDelta = Vector2.zero;
            var citBoxImg = citBoxGo.GetComponent<Image>();
            citBoxImg.color = new Color(0.08f, 0.18f, 0.24f, 0.85f);
            var citOutline = citBoxGo.AddComponent<Outline>();
            citOutline.effectColor = new Color(0.2f, 0.6f, 0.8f, 0.6f);
            citOutline.effectDistance = new Vector2(1f, -1f);

            // Citation Text
            GameObject citGo = new GameObject("Fact_Citation", typeof(RectTransform), typeof(Text));
            citGo.transform.SetParent(citBoxGo.transform, false);
            factCardCitation = citGo.GetComponent<Text>();
            factCardCitation.font = font;
            factCardCitation.fontSize = 12;
            factCardCitation.fontStyle = FontStyle.Normal;
            factCardCitation.alignment = TextAnchor.MiddleLeft;
            factCardCitation.color = new Color(0.65f, 0.88f, 1.0f);
            factCardCitation.text = "Data Source Citation: Ministry of Fisheries and Livestock, Bangladesh — Mother Hilsa Conservation Campaign & Jatka Protection Act";
            var citRt = citGo.GetComponent<RectTransform>();
            citRt.anchorMin = new Vector2(0.02f, 0f);
            citRt.anchorMax = new Vector2(0.98f, 1f);
            citRt.anchoredPosition = Vector2.zero;
            citRt.sizeDelta = Vector2.zero;

            // Start Mission Button (Pre-Level Briefing)
            GameObject btnStartGo = new GameObject("Btn_Start_Mission", typeof(RectTransform), typeof(Image), typeof(Button));
            btnStartGo.transform.SetParent(factCardPanel.transform, false);
            var bStartRt = btnStartGo.GetComponent<RectTransform>();
            bStartRt.anchorMin = new Vector2(0.25f, 0.025f);
            bStartRt.anchorMax = new Vector2(0.75f, 0.10f);
            bStartRt.anchoredPosition = Vector2.zero;
            bStartRt.sizeDelta = Vector2.zero;
            btnStartGo.GetComponent<Image>().color = new Color(0.12f, 0.65f, 0.45f, 0.95f);
            var bStartOutline = btnStartGo.AddComponent<Outline>();
            bStartOutline.effectColor = new Color(0.4f, 0.95f, 0.75f, 0.9f);
            bStartOutline.effectDistance = new Vector2(1.5f, -1.5f);
            startMissionButton = btnStartGo.GetComponent<Button>();
            startMissionButton.onClick.AddListener(() => {
                if (factCardPanel != null) factCardPanel.SetActive(false);
            });

            GameObject btnStartTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnStartTextGo.transform.SetParent(btnStartGo.transform, false);
            var bst = btnStartTextGo.GetComponent<Text>();
            bst.font = font;
            bst.fontSize = 15;
            bst.fontStyle = FontStyle.Bold;
            bst.alignment = TextAnchor.MiddleCenter;
            bst.color = Color.white;
            bst.text = "শুরু করুন ▶ (Start Swimming)";
            var bstRt = btnStartTextGo.GetComponent<RectTransform>();
            bstRt.anchorMin = Vector2.zero;
            bstRt.anchorMax = Vector2.one;
            bstRt.sizeDelta = Vector2.zero;

            // Next Level Button (Post-Level)
            GameObject btnNextGo = new GameObject("Btn_Next_Level", typeof(RectTransform), typeof(Image), typeof(Button));
            btnNextGo.transform.SetParent(factCardPanel.transform, false);
            var bnRt = btnNextGo.GetComponent<RectTransform>();
            bnRt.anchorMin = new Vector2(0.52f, 0.025f);
            bnRt.anchorMax = new Vector2(0.95f, 0.10f);
            bnRt.anchoredPosition = Vector2.zero;
            bnRt.sizeDelta = Vector2.zero;
            btnNextGo.GetComponent<Image>().color = new Color(0.18f, 0.68f, 0.38f, 0.95f);
            nextLevelButton = btnNextGo.GetComponent<Button>();
            nextLevelButton.onClick.AddListener(() => {
                if (gameManager != null) gameManager.AdvanceToNextSpecies();
            });

            GameObject btnNextTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnNextTextGo.transform.SetParent(btnNextGo.transform, false);
            var bnt = btnNextTextGo.GetComponent<Text>();
            bnt.font = font;
            bnt.fontSize = 14;
            bnt.fontStyle = FontStyle.Bold;
            bnt.alignment = TextAnchor.MiddleCenter;
            bnt.color = Color.white;
            bnt.text = "নতুন মিশন → (Next Mission)";
            var bntRt = btnNextTextGo.GetComponent<RectTransform>();
            bntRt.anchorMin = Vector2.zero;
            bntRt.anchorMax = Vector2.one;
            bntRt.sizeDelta = Vector2.zero;

            // Replay Button (Post-Level)
            GameObject btnReplayGo = new GameObject("Btn_Replay", typeof(RectTransform), typeof(Image), typeof(Button));
            btnReplayGo.transform.SetParent(factCardPanel.transform, false);
            var brRt = btnReplayGo.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0.05f, 0.025f);
            brRt.anchorMax = new Vector2(0.48f, 0.10f);
            brRt.anchoredPosition = Vector2.zero;
            brRt.sizeDelta = Vector2.zero;
            btnReplayGo.GetComponent<Image>().color = new Color(0.20f, 0.24f, 0.28f, 0.95f);
            restartButton = btnReplayGo.GetComponent<Button>();
            restartButton.onClick.AddListener(() => {
                if (gameManager != null) gameManager.RestartLevel();
            });

            GameObject btnReplayTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnReplayTextGo.transform.SetParent(btnReplayGo.transform, false);
            var brt = btnReplayTextGo.GetComponent<Text>();
            brt.font = font;
            brt.fontSize = 14;
            brt.fontStyle = FontStyle.Bold;
            brt.alignment = TextAnchor.MiddleCenter;
            brt.color = new Color(0.9f, 0.85f, 0.65f);
            brt.text = "আবার শুরু ↺ (Restart)";
            var brtRt = btnReplayTextGo.GetComponent<RectTransform>();
            brtRt.anchorMin = Vector2.zero;
            brtRt.anchorMax = Vector2.one;
            brtRt.sizeDelta = Vector2.zero;
        }

        private void BuildDataSourcesScreen(Transform parent, Font font)
        {
            // ── 1. Top-Right "NASA DATA POPUP" Button ──
            GameObject btnOpenGo = new GameObject("Btn_Data_Sources", typeof(RectTransform), typeof(Image), typeof(Button));
            btnOpenGo.transform.SetParent(parent, false);
            var btnRt = btnOpenGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-20f, -16f);
            btnRt.sizeDelta = new Vector2(175f, 36f);

            var btnImg = btnOpenGo.GetComponent<Image>();
            btnImg.color = new Color(0.08f, 0.18f, 0.26f, 0.92f);
            var btnOutline = btnOpenGo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.35f, 0.75f, 0.95f, 0.85f);
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject btnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTxtGo.transform.SetParent(btnOpenGo.transform, false);
            var btnTxt = btnTxtGo.GetComponent<Text>();
            btnTxt.font = font;
            btnTxt.fontSize = 12;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = new Color(0.92f, 0.96f, 1.0f);
            btnTxt.text = "🛰️ NASA DATA POPUP";
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.sizeDelta = Vector2.zero;

            dataSourcesButton = btnOpenGo.GetComponent<Button>();
            dataSourcesButton.onClick.AddListener(() => {
                if (dataSourcesPanel != null)
                {
                    bool nextState = !dataSourcesPanel.activeSelf;
                    dataSourcesPanel.SetActive(nextState);
                }
            });

            // ── 2. Modal Information Panel ──
            dataSourcesPanel = new GameObject("DataSources_Modal_Panel", typeof(RectTransform), typeof(Image));
            dataSourcesPanel.transform.SetParent(parent, false);
            var panelRt = dataSourcesPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(680f, 520f);

            var panelImg = dataSourcesPanel.GetComponent<Image>();
            panelImg.color = new Color(0.04f, 0.08f, 0.11f, 0.98f);
            var panelOutline = dataSourcesPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.85f, 0.75f, 0.35f, 0.95f);
            panelOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(dataSourcesPanel.transform, false);
            var tText = titleGo.GetComponent<Text>();
            tText.font = font;
            tText.fontSize = 20;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.85f, 0.35f);
            tText.text = "NASA Earth Observation Data & Sources";
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -18f);
            tRt.sizeDelta = new Vector2(-40f, 36f);

            // Body text with exact datasets & honesty note
            GameObject bodyGo = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(dataSourcesPanel.transform, false);
            var bText = bodyGo.GetComponent<Text>();
            bText.font = font;
            bText.fontSize = 13;
            bText.lineSpacing = 1.25f;
            bText.color = new Color(0.90f, 0.92f, 0.95f);
            bText.text =
                "1. MODIS NDVI (MOD13Q1.061):\n" +
                "• Measures: Canopy vegetation density and habitat health across the Sundarbans mangrove delta.\n" +
                "• Extraction: Retrieved via NASA AppEEARS platform (appeears.earthdatacloud.nasa.gov).\n\n" +
                "2. MODIS Land Surface Temperature (MOD11A2.061):\n" +
                "• Measures: Estuary surface thermal proxy for dolphin and hilsha migration routes.\n" +
                "• Extraction: Retrieved directly via NASA AppEEARS platform.\n\n" +
                "3. Estuary & River Salinity Index (Salinity Source):\n" +
                "• Measures: NASA PO.DAAC SMAP L3 SSS (Sea Surface Salinity) combined with BFRI Chandpur station in-situ readings.\n\n" +
                "4. Ministry of Fisheries & Livestock, Bangladesh (Conservation Campaign):\n" +
                "• Measures: 22-day October Mother Hilsa protection ban & Nov 1 - Jun 30 Jatka protection law (52% safe spawning rate in 2024).\n\n" +
                "5. Satellite Navigation Radar (Minimap Imagery):\n" +
                "• Source: NASA Worldview (worldview.earthdata.nasa.gov) — Landsat-7/8 Optical Earth Observation.\n\n" +
                "⚠️ Scientific Transparency Note:\n" +
                "Due to coastal land-masking fill values in open water regions, nearest reliable valid satellite readings were used to provide continuous ecological simulation.";

            var bRt = bodyGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.offsetMin = new Vector2(28f, 70f);
            bRt.offsetMax = new Vector2(-28f, -60f);

            // Play Intro Video Button
            GameObject introBtnGo = new GameObject("Btn_Play_Intro", typeof(RectTransform), typeof(Image), typeof(Button));
            introBtnGo.transform.SetParent(dataSourcesPanel.transform, false);
            var ibRt = introBtnGo.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0.5f, 0f);
            ibRt.anchorMax = new Vector2(0.5f, 0f);
            ibRt.pivot = new Vector2(0.5f, 0f);
            ibRt.anchoredPosition = new Vector2(-110f, 18f);
            ibRt.sizeDelta = new Vector2(210f, 38f);

            introBtnGo.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.65f, 0.95f);
            var ibBtn = introBtnGo.GetComponent<Button>();
            ibBtn.onClick.AddListener(() => {
                if (dataSourcesPanel != null) dataSourcesPanel.SetActive(false);
                var intro = Object.FindAnyObjectByType<SundarbanGuardian.UI.IntroSequenceController>();
                if (intro != null) intro.PlayIntro();
            });

            GameObject ibTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            ibTxtGo.transform.SetParent(introBtnGo.transform, false);
            var ibTxt = ibTxtGo.GetComponent<Text>();
            ibTxt.font = font;
            ibTxt.fontSize = 13;
            ibTxt.fontStyle = FontStyle.Bold;
            ibTxt.alignment = TextAnchor.MiddleCenter;
            ibTxt.color = Color.white;
            ibTxt.text = "🎬 তথ্যচিত্র (Intro Video)";
            var ibTxtRt = ibTxtGo.GetComponent<RectTransform>();
            ibTxtRt.anchorMin = Vector2.zero;
            ibTxtRt.anchorMax = Vector2.one;
            ibTxtRt.sizeDelta = Vector2.zero;

            // Close Button
            GameObject closeGo = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(dataSourcesPanel.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0f);
            cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(110f, 18f);
            cRt.sizeDelta = new Vector2(160f, 38f);

            closeGo.GetComponent<Image>().color = new Color(0.20f, 0.55f, 0.35f, 0.95f);
            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(() => {
                if (dataSourcesPanel != null) dataSourcesPanel.SetActive(false);
            });

            GameObject closeTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeTxtGo.transform.SetParent(closeGo.transform, false);
            var cTxt = closeTxtGo.GetComponent<Text>();
            cTxt.font = font;
            cTxt.fontSize = 13;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.text = "✓ Close";
            var cTxtRt = closeTxtGo.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.sizeDelta = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (activeHealth != null)
                activeHealth.HealthChanged -= OnHealthChanged;

            HudEvents.EnteredHazardZone -= ShowHazardNotification;
            HudEvents.ExitedHazardZone -= HideHazardNotification;
            HudEvents.EnvironmentalRestoration -= ShowRestorationToast;

            SundarbanGuardian.Player.MobileInputManager.AutoRunStateChanged -= OnAutoRunChanged;
            UnhookBackpack();

            if (gameManager != null)
            {
                gameManager.LevelCompleted -= OnLevelCompleted;
                gameManager.GameOver -= OnGameOver;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── BACKPACK & ENVIRONMENTAL RESTORATION HUD ─────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        private void HookBackpack()
        {
            UnhookBackpack();

            if (gameManager != null && gameManager.playerObject != null && gameManager.playerObject.activeInHierarchy)
            {
                activeBackpack = gameManager.playerObject.GetComponent<Backpack>();
            }

            if (activeBackpack == null)
            {
                activeBackpack = Backpack.LocalPlayerBackpack;
            }

            if (activeBackpack == null)
            {
                activeBackpack = Object.FindAnyObjectByType<Backpack>();
            }

            if (activeBackpack != null)
            {
                activeBackpack.ToolCountChanged += OnToolCountChanged;
                activeBackpack.ActiveToolChanged += OnActiveToolChanged;
                activeBackpack.NearbyObstacleChanged += OnNearbyObstacleChanged;

                for (int i = 0; i < 4; i++)
                {
                    OnToolCountChanged((ToolType)i, activeBackpack.GetCount((ToolType)i));
                }
                OnActiveToolChanged(activeBackpack.activeTool);
            }
        }

        private void UnhookBackpack()
        {
            if (activeBackpack != null)
            {
                activeBackpack.ToolCountChanged -= OnToolCountChanged;
                activeBackpack.ActiveToolChanged -= OnActiveToolChanged;
                activeBackpack.NearbyObstacleChanged -= OnNearbyObstacleChanged;
                activeBackpack = null;
            }
        }

        private void OnToolCountChanged(ToolType tool, int count)
        {
            int idx = (int)tool;
            if (idx >= 0 && idx < toolSlotIcons.Length && toolSlotIcons[idx] != null && toolSlotIcons[idx].sprite != null)
            {
                if (idx < toolSlotTexts.Length && toolSlotTexts[idx] != null) toolSlotTexts[idx].text = "";
            }
            else if (idx >= 0 && idx < toolSlotTexts.Length && toolSlotTexts[idx] != null)
            {
                toolSlotTexts[idx].text = ToolInfo.GetSymbol(tool);
            }

            if (idx >= 0 && idx < toolSlotCountTexts.Length && toolSlotCountTexts[idx] != null)
            {
                toolSlotCountTexts[idx].text = count.ToString();
            }
        }

        private void OnActiveToolChanged(ToolType active)
        {
            for (int i = 0; i < 4; i++)
            {
                if (toolSlotOutlines[i] != null)
                {
                    bool isSelected = ((ToolType)i == active);
                    toolSlotOutlines[i].enabled = isSelected;
                    toolSlotOutlines[i].effectColor = new Color(0.95f, 0.85f, 0.25f, 1.0f);
                }
            }
            UpdateUseToolButtonState();
        }

        private void OnNearbyObstacleChanged(UsableObstacle obs)
        {
            UpdateUseToolButtonState();
        }

        private void UpdateUseToolButtonState()
        {
            if (hitActionBtn == null || hitActionBtnImg == null) return;

            if (activeBackpack != null && activeBackpack.nearbyObstacle != null && !activeBackpack.nearbyObstacle.isCleared)
            {
                var obs = activeBackpack.nearbyObstacle;
                ToolType req = obs.requiredTool;
                bool hasTool = activeBackpack.HasTool(req);

                if (hasTool)
                {
                    if (hitActionBtnOutline != null)
                    {
                        hitActionBtnOutline.enabled = true;
                        hitActionBtnOutline.effectColor = new Color(0.2f, 0.95f, 0.4f, 1.0f); // vibrant emerald green
                    }
                    if (hitActionBtnText != null) hitActionBtnText.text = "USE";
                }
                else
                {
                    if (hitActionBtnOutline != null)
                    {
                        hitActionBtnOutline.enabled = true;
                        hitActionBtnOutline.effectColor = new Color(0.9f, 0.2f, 0.2f, 1.0f); // red out of stock
                    }
                    if (hitActionBtnText != null) hitActionBtnText.text = "EMPTY";
                }
            }
            else
            {
                if (hitActionBtnOutline != null)
                {
                    hitActionBtnOutline.enabled = true;
                    hitActionBtnOutline.effectColor = new Color(0.85f, 0.90f, 0.95f, 0.65f); // calm glass rim
                }
                if (hitActionBtnText != null) hitActionBtnText.text = "FIRE";
            }
        }

        private void BuildBackpackHUD(Transform parent, Font font)
        {
            // Positioned on the right side, directly below NASA Data Popup
            GameObject boxGo = new GameObject("Backpack_HUD_Container", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(parent, false);
            backpackContainer = boxGo;

            var rt = boxGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -58f);
            rt.sizeDelta = new Vector2(244f, 54f);

            var bg = boxGo.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.10f, 0.50f); // transparent dark glass

            var outline = boxGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.75f, 0.95f, 0.50f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 4 Tool Buttons in Horizontal Row: SEED | NIFE | CLEAN | TRAP DESTROY
            // User requested: "use emojies on 4 tools, dont use text"
            float slotW = 52f;
            float slotH = 44f;
            float spacing = 6f;
            float startX = 6f;
            float startY = -5f;

            // Load Tool Icon Sprites
            Sprite[] toolSprites = new Sprite[4];
#if UNITY_EDITOR
            toolSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_tool_seed.png");
            toolSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_tool_knife.png");
            toolSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_tool_clean.png");
            toolSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_tool_trap.png");
#endif
            if (toolSprites[0] == null) toolSprites[0] = Resources.Load<Sprite>("Textures/UI/icon_tool_seed");
            if (toolSprites[1] == null) toolSprites[1] = Resources.Load<Sprite>("Textures/UI/icon_tool_knife");
            if (toolSprites[2] == null) toolSprites[2] = Resources.Load<Sprite>("Textures/UI/icon_tool_clean");
            if (toolSprites[3] == null) toolSprites[3] = Resources.Load<Sprite>("Textures/UI/icon_tool_trap");

            for (int i = 0; i < 4; i++)
            {
                ToolType tType = (ToolType)i;
                GameObject btnGo = new GameObject($"ToolSlot_{tType}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(boxGo.transform, false);

                var sRt = btnGo.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0f, 1f);
                sRt.anchorMax = new Vector2(0f, 1f);
                sRt.pivot = new Vector2(0f, 1f);
                sRt.anchoredPosition = new Vector2(startX + i * (slotW + spacing), startY);
                sRt.sizeDelta = new Vector2(slotW, slotH);

                var slotImg = btnGo.GetComponent<Image>();
                Color theme = ToolInfo.GetThemeColor(tType);
                slotImg.color = new Color(theme.r * 0.25f, theme.g * 0.25f, theme.b * 0.25f, 0.75f);
                toolSlotBgs[i] = slotImg;

                var sOutline = btnGo.AddComponent<Outline>();
                sOutline.effectColor = new Color(0.95f, 0.85f, 0.25f, 1.0f);
                sOutline.effectDistance = new Vector2(2f, -2f);
                sOutline.enabled = (i == 0);
                toolSlotOutlines[i] = sOutline;

                var btn = btnGo.GetComponent<Button>();
                int capturedIdx = i;
                btn.onClick.AddListener(() => {
                    if (activeBackpack != null)
                    {
                        activeBackpack.SelectTool((ToolType)capturedIdx);
                    }
                });
                toolSlotButtons[i] = btn;

                // Center Tool Graphic (Clean icon sprite, with emoji fallback)
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(btnGo.transform, false);
                var iconImg = iconGo.GetComponent<Image>();
                if (toolSprites[i] != null)
                {
                    iconImg.sprite = toolSprites[i];
                    iconImg.preserveAspect = true;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.enabled = false;
                }
                iconImg.raycastTarget = false;
                toolSlotIcons[i] = iconImg;
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(0f, 2f);
                iconRt.sizeDelta = new Vector2(28f, 28f);

                // Fallback Emoji text (if sprite isn't loaded)
                GameObject emojiGo = new GameObject("Emoji", typeof(RectTransform), typeof(Text));
                emojiGo.transform.SetParent(btnGo.transform, false);
                var emojiTxt = emojiGo.GetComponent<Text>();
                emojiTxt.font = font;
                emojiTxt.fontSize = 22;
                emojiTxt.alignment = TextAnchor.MiddleCenter;
                emojiTxt.color = Color.white;
                emojiTxt.text = (toolSprites[i] != null) ? "" : ToolInfo.GetSymbol(tType);
                toolSlotTexts[i] = emojiTxt;
                emojiTxt.raycastTarget = false;

                var emojiRt = emojiGo.GetComponent<RectTransform>();
                emojiRt.anchorMin = Vector2.zero;
                emojiRt.anchorMax = Vector2.one;
                emojiRt.sizeDelta = Vector2.zero;

                // Corner count badge
                GameObject countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
                countGo.transform.SetParent(btnGo.transform, false);
                var countTxt = countGo.GetComponent<Text>();
                countTxt.font = font;
                countTxt.fontSize = 11;
                countTxt.fontStyle = FontStyle.Bold;
                countTxt.alignment = TextAnchor.LowerRight;
                countTxt.color = new Color(0.95f, 0.95f, 0.75f, 0.95f);
                countTxt.text = "3";
                toolSlotCountTexts[i] = countTxt;

                var countRt = countGo.GetComponent<RectTransform>();
                countRt.anchorMin = new Vector2(0f, 0f);
                countRt.anchorMax = new Vector2(1f, 0f);
                countRt.pivot = new Vector2(1f, 0f);
                countRt.anchoredPosition = new Vector2(-4f, 2f);
                countRt.sizeDelta = new Vector2(0f, 14f);
            }
        }

        private void BuildRestorationToast(Transform parent, Font font)
        {
            GameObject toastGo = new GameObject("Toast_Restoration_Popup", typeof(RectTransform), typeof(Image));
            toastGo.transform.SetParent(parent, false);
            restorationToastPanel = toastGo;

            var rt = toastGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta = new Vector2(420f, 44f);

            var bg = toastGo.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.45f, 0.22f, 0.95f); // vibrant emerald toast

            var outline = toastGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.85f, 0.25f, 1.0f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject txtGo = new GameObject("Toast_Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(toastGo.transform, false);
            restorationToastText = txtGo.GetComponent<Text>();
            restorationToastText.font = font;
            restorationToastText.fontSize = 14;
            restorationToastText.fontStyle = FontStyle.Bold;
            restorationToastText.alignment = TextAnchor.MiddleCenter;
            restorationToastText.color = Color.white;
            restorationToastText.text = "🎉 +10 HP — Environmental Restoration!";

            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            toastGo.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── MOBILE TOUCH CONTROLS (FREE FIRE / PUBG MOBILE STYLE) ────────────
        // ─────────────────────────────────────────────────────────────────────

        private Button autoRunBtn;
        private Image autoRunBtnImg;
        private Outline autoRunBtnOutline;
        private Image autoRunIconImg;
        private Button hitActionBtn;
        private Text hitActionBtnText;
        private Image hitActionBtnImg;
        private Outline hitActionBtnOutline;
        private Image fireIconImg;

        private void BuildTouchControls(Transform parent, Font font)
        {
            // Load Free Fire style UI sprites
            Sprite joyBaseSp = null;
            Sprite joyKnobSp = null;
            Sprite glassBtnSp = null;
            Sprite autoRunIconSp = null;
            Sprite fireIconSp = null;

#if UNITY_EDITOR
            joyBaseSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/joystick_base.png");
            joyKnobSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/joystick_knob.png");
            glassBtnSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/btn_circle_glass.png");
            autoRunIconSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_autorun.png");
            fireIconSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_fire.png");
#endif
            if (joyBaseSp == null) joyBaseSp = Resources.Load<Sprite>("Textures/UI/joystick_base");
            if (joyKnobSp == null) joyKnobSp = Resources.Load<Sprite>("Textures/UI/joystick_knob");
            if (glassBtnSp == null) glassBtnSp = Resources.Load<Sprite>("Textures/UI/btn_circle_glass");
            if (autoRunIconSp == null) autoRunIconSp = Resources.Load<Sprite>("Textures/UI/icon_autorun");
            if (fireIconSp == null) fireIconSp = Resources.Load<Sprite>("Textures/UI/icon_fire");

            // 1. Touch Look Zone covering whole screen behind other UI
            GameObject lookZoneGo = new GameObject("Touch_Look_Zone", typeof(RectTransform), typeof(Image), typeof(SundarbanGuardian.UI.TouchLookZone));
            lookZoneGo.transform.SetParent(parent, false);
            lookZoneGo.transform.SetAsFirstSibling();
            var lRt = lookZoneGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
            var lImg = lookZoneGo.GetComponent<Image>();
            lImg.color = new Color(0f, 0f, 0f, 0f); // transparent raycast receiver
            lImg.raycastTarget = true;

            // 2. Virtual Joystick Base at Bottom-Left (Free Fire Dial Ring)
            GameObject joyBaseGo = new GameObject("Joystick_Base", typeof(RectTransform), typeof(Image), typeof(SundarbanGuardian.UI.VirtualJoystick));
            joyBaseGo.transform.SetParent(parent, false);
            var jRt = joyBaseGo.GetComponent<RectTransform>();
            jRt.anchorMin = new Vector2(0f, 0f);
            jRt.anchorMax = new Vector2(0f, 0f);
            jRt.pivot = new Vector2(0.5f, 0.5f);
            jRt.anchoredPosition = new Vector2(120f, 120f);
            jRt.sizeDelta = new Vector2(170f, 170f);

            var jImg = joyBaseGo.GetComponent<Image>();
            if (joyBaseSp != null)
            {
                jImg.sprite = joyBaseSp;
                jImg.color = Color.white;
            }
            else
            {
                jImg.color = new Color(0.06f, 0.08f, 0.10f, 0.50f);
            }

            // Joystick Handle Knob (White/Silver Puck)
            GameObject knobGo = new GameObject("Joystick_Handle", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(joyBaseGo.transform, false);
            var kRt = knobGo.GetComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0.5f, 0.5f);
            kRt.anchorMax = new Vector2(0.5f, 0.5f);
            kRt.pivot = new Vector2(0.5f, 0.5f);
            kRt.anchoredPosition = Vector2.zero;
            kRt.sizeDelta = new Vector2(68f, 68f);

            var kImg = knobGo.GetComponent<Image>();
            if (joyKnobSp != null)
            {
                kImg.sprite = joyKnobSp;
                kImg.color = Color.white;
            }
            else
            {
                kImg.color = new Color(0.92f, 0.94f, 0.97f, 0.90f);
            }

            var joyComp = joyBaseGo.GetComponent<SundarbanGuardian.UI.VirtualJoystick>();
            joyComp.backgroundRing = jRt;
            joyComp.handleKnob = kRt;
            joyComp.maxRadius = 65f;

            // 3. Circular Glass Auto-Run Button on Left Side above Joystick (below Map)
            GameObject autoRunGo = new GameObject("Btn_AutoRun", typeof(RectTransform), typeof(Image), typeof(Button));
            autoRunGo.transform.SetParent(parent, false);
            var arRt = autoRunGo.GetComponent<RectTransform>();
            arRt.anchorMin = new Vector2(0f, 0f);
            arRt.anchorMax = new Vector2(0f, 0f);
            arRt.pivot = new Vector2(0.5f, 0.5f);
            arRt.anchoredPosition = new Vector2(70f, 235f);
            arRt.sizeDelta = new Vector2(56f, 56f);

            autoRunBtnImg = autoRunGo.GetComponent<Image>();
            if (glassBtnSp != null)
            {
                autoRunBtnImg.sprite = glassBtnSp;
                autoRunBtnImg.color = Color.white;
            }
            else
            {
                autoRunBtnImg.color = new Color(0.08f, 0.10f, 0.12f, 0.65f);
            }

            autoRunBtnOutline = autoRunGo.AddComponent<Outline>();
            autoRunBtnOutline.effectColor = new Color(0.85f, 0.90f, 0.95f, 0.50f);
            autoRunBtnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Child Runner Icon Image
            GameObject arIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            arIconGo.transform.SetParent(autoRunGo.transform, false);
            autoRunIconImg = arIconGo.GetComponent<Image>();
            if (autoRunIconSp != null)
            {
                autoRunIconImg.sprite = autoRunIconSp;
                autoRunIconImg.preserveAspect = true;
                autoRunIconImg.color = Color.white;
            }
            autoRunIconImg.raycastTarget = false;
            var arIconRt = arIconGo.GetComponent<RectTransform>();
            arIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            arIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            arIconRt.pivot = new Vector2(0.5f, 0.5f);
            arIconRt.anchoredPosition = Vector2.zero;
            arIconRt.sizeDelta = new Vector2(36f, 36f);

            autoRunBtn = autoRunGo.GetComponent<Button>();
            autoRunBtn.onClick.AddListener(() => {
                SundarbanGuardian.Player.MobileInputManager.ToggleAutoRun();
            });

            SundarbanGuardian.Player.MobileInputManager.AutoRunStateChanged += OnAutoRunChanged;

            // 4. Large Circular FIRE Button (Bottom-Right / Right Thumb Area per Image 1)
            GameObject hitGo = new GameObject("Btn_Fire", typeof(RectTransform), typeof(Image), typeof(Button));
            hitGo.transform.SetParent(parent, false);
            var hRt = hitGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(1f, 0f);
            hRt.anchorMax = new Vector2(1f, 0f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = new Vector2(-110f, 110f);
            hRt.sizeDelta = new Vector2(110f, 110f);

            hitActionBtnImg = hitGo.GetComponent<Image>();
            if (glassBtnSp != null)
            {
                hitActionBtnImg.sprite = glassBtnSp;
                hitActionBtnImg.color = Color.white;
            }
            else
            {
                hitActionBtnImg.color = new Color(0.12f, 0.65f, 0.35f, 0.95f);
            }

            hitActionBtnOutline = hitGo.AddComponent<Outline>();
            hitActionBtnOutline.effectColor = new Color(0.85f, 0.90f, 0.95f, 0.65f);
            hitActionBtnOutline.effectDistance = new Vector2(2f, -2f);

            // Child Bullet/Sparks Icon Image
            GameObject fireIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            fireIconGo.transform.SetParent(hitGo.transform, false);
            fireIconImg = fireIconGo.GetComponent<Image>();
            if (fireIconSp != null)
            {
                fireIconImg.sprite = fireIconSp;
                fireIconImg.preserveAspect = true;
                fireIconImg.color = Color.white;
            }
            fireIconImg.raycastTarget = false;
            var fireIconRt = fireIconGo.GetComponent<RectTransform>();
            fireIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            fireIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            fireIconRt.pivot = new Vector2(0.5f, 0.5f);
            fireIconRt.anchoredPosition = new Vector2(0f, 5f);
            fireIconRt.sizeDelta = new Vector2(62f, 62f);

            // Label (FIRE / USE)
            GameObject hTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            hTxtGo.transform.SetParent(hitGo.transform, false);
            hitActionBtnText = hTxtGo.GetComponent<Text>();
            hitActionBtnText.font = font;
            hitActionBtnText.fontSize = 12;
            hitActionBtnText.fontStyle = FontStyle.Bold;
            hitActionBtnText.alignment = TextAnchor.LowerCenter;
            hitActionBtnText.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            hitActionBtnText.text = "FIRE";
            var hTxtRt = hTxtGo.GetComponent<RectTransform>();
            hTxtRt.anchorMin = new Vector2(0f, 0f);
            hTxtRt.anchorMax = new Vector2(1f, 0f);
            hTxtRt.pivot = new Vector2(0.5f, 0f);
            hTxtRt.anchoredPosition = new Vector2(0f, 8f);
            hTxtRt.sizeDelta = new Vector2(0f, 18f);

            hitActionBtn = hitGo.GetComponent<Button>();
            hitActionBtn.onClick.AddListener(() => {
                SundarbanGuardian.Player.MobileInputManager.TriggerHitAction();
            });
        }

        private void OnAutoRunChanged(bool isRunning)
        {
            if (autoRunBtnOutline != null)
            {
                if (isRunning)
                {
                    autoRunBtnOutline.effectColor = new Color(0.25f, 0.95f, 0.45f, 1.0f); // bright emerald glow
                    autoRunBtnOutline.effectDistance = new Vector2(2.5f, -2.5f);
                }
                else
                {
                    autoRunBtnOutline.effectColor = new Color(0.85f, 0.90f, 0.95f, 0.50f);
                    autoRunBtnOutline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        private void ShowRestorationToast(string message)
        {
            if (restorationToastPanel == null || restorationToastText == null) return;

            restorationToastText.text = $"🎉 {message}";
            restorationToastPanel.SetActive(true);

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(HideToastCoroutine());
        }

        private IEnumerator HideToastCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            if (restorationToastPanel != null)
            {
                restorationToastPanel.SetActive(false);
            }
        }
    }
}
