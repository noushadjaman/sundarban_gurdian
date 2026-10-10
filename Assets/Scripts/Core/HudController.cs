using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        // Tactical Full Screen Satellite Map Popup
        private GameObject tacticalMapModal;
        private RectTransform tacticalPlayerMarker;
        private Text tacticalGoalDistText;
        private RectTransform tacticalOverlay;
        private RectTransform tacticalMapContent;
        private Font cachedDefaultFont;
        private Text tacticalPlayerCoordsText;
        private Text tacticalThreatCountText;
        private Transform tacticalThreatCardsContainer;
        private GameObject tacticalIntelPanel;
        private RectTransform tacticalMapViewport;
        private bool isTacticalIntelPanelVisible = true;
        private readonly System.Collections.Generic.List<DetectedThreatData> activeThreats = new System.Collections.Generic.List<DetectedThreatData>();

        // ── COMPETITIVE TOURNAMENT & ROUND HUD ─────────────────────────────
        private RectTransform competitiveTopBar;
        private Text roundBadgeText;
        private Image roundBadgeBg;
        private Text roundTimerText;
        private Text roundParText;
        private Button roundSelectorNavBtn;

        // Competitive Round Results Modal
        private GameObject roundResultsModal;
        private Text resultTitleText;
        private Text resultSubtitleText;
        private Text resultStarsText;
        private Text resultRankBadgeText;
        private Text resultTimeValueText;
        private Text resultParValueText;
        private Text resultTimeBonusText;
        private Text resultVitalityBonusText;
        private Text resultScoreValueText;
        private GameObject resultNewRecordBadge;
        private Transform leaderboardRowsContainer;
        private Button btnNextRoundAction;
        private Text btnNextRoundText;
        private Button btnRetryRoundAction;
        private Button btnOpenBracketAction;

        // Grand Tournament Championship Modal
        private GameObject championshipModal;
        private Text tourneyTotalTimeText;
        private Text tourneyTotalScoreText;
        private Text tourneyTotalStarsText;
        private Text tourneyGradeText;
        private Transform tourneyCardsContainer;
        private Button btnRestartTourneyAction;
        private Button btnTourneyRoundSelectAction;

        // Round Selector / Tournament Bracket Modal
        private GameObject roundSelectorModal;
        private Transform roundCardsContainer;
        private Button btnCloseRoundSelector;

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
            public ToolPickupItem toolItem;
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
        private const float mapWorldMaxX =  160f;
        private const float mapWorldMinZ = -160f;
        private const float mapWorldMaxZ =  280f;

        // Tactical Map UV Viewport Bounds (Focuses on Sundarbans Delta with Southern Bay of Bengal at bottom)
        public const float TacMapUvMinX = 0.38f;
        public const float TacMapUvMinY = 0.28f;
        public const float TacMapUvSpanX = 0.42f;
        public const float TacMapUvSpanY = 0.56f;

        // Calibrated mapping from world coordinates to NASA Landsat Sundarbans UV
        // Dense Sundarbans Mangrove Delta is at U: 0.44..0.72, V: 0.40..0.80.
        // Bay of Bengal open sea is at V < 0.38 (long-range view looking south).
        public static Vector2 WorldToLandsatUV(float worldX, float worldZ)
        {
            float normX = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinX, mapWorldMaxX, worldX));
            float normZ = Mathf.Clamp01(Mathf.InverseLerp(mapWorldMinZ, mapWorldMaxZ, worldZ));

            float u = Mathf.Lerp(0.44f, 0.72f, normX);
            float v = Mathf.Lerp(0.40f, 0.80f, normZ);
            return new Vector2(u, v);
        }

        public static Vector2 WorldToTacticalMapNormalized(float worldX, float worldZ)
        {
            Vector2 uv = WorldToLandsatUV(worldX, worldZ);
            float normU = Mathf.Clamp01((uv.x - TacMapUvMinX) / TacMapUvSpanX);
            float normV = Mathf.Clamp01((uv.y - TacMapUvMinY) / TacMapUvSpanY);
            return new Vector2(normU, normV);
        }

        // Authentic NASA Landsat-9 / MODIS Earth Science Sundarbans Geographic Bounds:
        public const float NasaBoundsSouth = 21.7500f; // Bay of Bengal / Kotka Sanctuary Coast (21°45'00"N)
        public const float NasaBoundsNorth = 22.4800f; // Northern Sundarbans / Chandpai Forest Edge (22°28'48"N)
        public const float NasaBoundsWest  = 89.2000f; // Passur / Shibsa River Estuary (89°12'00"E)
        public const float NasaBoundsEast  = 89.8800f; // Sharonkhola / Baleshwar Mouth (89°52'48"E)

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

        // Data & Sources / NASA Learning Hub Overlay
        private GameObject dataSourcesModalGo;
        private GameObject dataSourcesPanel;
        private Button dataSourcesButton;
        private Font cachedKalpurushFont;
        private int currentNasaLearningTab = 0;
        private Text nasaHubHeaderText;
        private Text nasaHubCategoryTitleText;
        private Text nasaHubBodyText;
        private Text nasaHubFactBadgeText;
        private readonly Button[] nasaTabButtons = new Button[5];
        private readonly Image[] nasaTabBtnImages = new Image[5];
        private readonly Text[] nasaTabBtnTexts = new Text[5];
        private Text didYouKnowToastText;
        private GameObject didYouKnowToastGo;
        private Coroutine didYouKnowCoroutine;

        // Backpack HUD Elements
        private GameObject backpackContainer;
        private readonly Button[] toolSlotButtons = new Button[4];
        private readonly Image[] toolSlotBgs = new Image[4];
        private readonly Image[] toolSlotIcons = new Image[4];
        private readonly Text[] toolSlotTexts = new Text[4];
        private readonly Text[] toolSlotCountTexts = new Text[4];
        private readonly Outline[] toolSlotOutlines = new Outline[4];
        private Sprite[] cachedToolSprites = new Sprite[4];
        private Button useToolButton;
        private Image useToolBtnImg;
        private Text useToolBtnText;
        private Text useToolSubText;
        private Outline useToolBtnOutline;
        private Button eatFoodBtn;
        private Text foodCountBadgeText;
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

            if (dataSourcesModalGo != null)
                dataSourcesModalGo.SetActive(false);
            else if (dataSourcesPanel != null)
                dataSourcesPanel.SetActive(false);

            HudEvents.EnvironmentalRestoration += ShowRestorationToast;
            HudEvents.HazardZoneCleared += OnHazardZoneCleared;
            HookBackpack();

            PopulateMinimapMarkers();

            ToolPickupItem.PickupSpawned += SpawnMinimapToolMarker;
            ToolPickupItem.PickupCollected += RemoveMinimapToolMarker;
            SundarbanGuardian.Environment.FoodItem.FoodSpawned += SpawnMinimapFoodMarker;
            SundarbanGuardian.Environment.FoodItem.FoodEaten += RemoveMinimapFoodMarker;

            string currentSp = gameManager != null ? gameManager.currentSpecies : GameManager.selectedSpecies;
            if (!string.IsNullOrEmpty(currentSp) && currentSp.ToLower().Contains("hilsha"))
            {
                ShowPreLevelBriefing();
            }

            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.RoundStarted += OnCompetitiveRoundStarted;
                CompetitiveRoundManager.Instance.RoundTimerTick += OnCompetitiveRoundTimerTick;
                CompetitiveRoundManager.Instance.RoundCompleted += ShowCompetitiveRoundResults;
                CompetitiveRoundManager.Instance.TournamentCompleted += ShowChampionshipPodium;
                CompetitiveRoundManager.Instance.RoundFailed += ShowCompetitiveRoundFailed;

                UpdateCompetitiveHUD(CompetitiveRoundManager.Instance.CurrentRoundConfig);
            }
        }

        private void HookHealth()
        {
            SpeciesHealth targetHealth = null;
            if (gameManager != null && gameManager.playerObject != null && gameManager.playerObject.activeInHierarchy)
            {
                targetHealth = gameManager.playerObject.GetComponent<SpeciesHealth>();
            }
            else if (gameManager != null && gameManager.activeHealth != null && gameManager.activeHealth.gameObject.activeInHierarchy)
            {
                targetHealth = gameManager.activeHealth;
            }
            else if (trackedPlayer != null && trackedPlayer.gameObject.activeInHierarchy)
            {
                targetHealth = trackedPlayer.GetComponent<SpeciesHealth>();
            }

            if (targetHealth == null)
            {
                string[] names = { "Tiger", "Deer", "Dolphin", "Hilsha" };
                foreach (var n in names)
                {
                    var go = GameObject.Find(n);
                    if (go != null && go.activeInHierarchy)
                    {
                        targetHealth = go.GetComponent<SpeciesHealth>();
                        if (targetHealth != null) break;
                    }
                }
            }

            if (targetHealth == null)
            {
                var allH = Object.FindObjectsByType<SpeciesHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (allH != null && allH.Length > 0) targetHealth = allH[0];
            }

            if (activeHealth != targetHealth)
            {
                if (activeHealth != null)
                {
                    activeHealth.HealthChanged -= OnHealthChanged;
                }
                activeHealth = targetHealth;
                if (activeHealth != null)
                {
                    activeHealth.HealthChanged += OnHealthChanged;
                    OnHealthChanged(activeHealth.currentHealth / Mathf.Max(1f, activeHealth.maxHealth));
                }
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
                if (tacticalMapModal != null && tacticalMapModal.activeSelf)
                {
                    UpdateTacticalMapPopup();
                }
            }

            if (activeBackpack == null || !activeBackpack.gameObject.activeInHierarchy)
            {
                HookBackpack();
            }

            UpdateUseToolButtonState();

            // Keyboard shortcut for Tactical Full Map: 'M' or 'Tab'
            bool toggleTactical = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
                    {
                        toggleTactical = true;
                    }
                }
            }
            catch { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
                {
                    toggleTactical = true;
                }
            }
            catch { }
#endif
            if (toggleTactical)
            {
                ToggleTacticalMapPopup();
            }

            // Keyboard shortcut for NASA Learning Hub: 'N', 'F1', or 'H'
            bool toggleNasa = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
                    {
                        toggleNasa = true;
                    }
                }
            }
            catch { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.H))
                {
                    toggleNasa = true;
                }
            }
            catch { }
#endif

            if (toggleNasa)
            {
                ToggleNASADataPopup();
            }

            // Close popup on Escape
            bool pressEscape = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    pressEscape = true;
                }
            }
            catch { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) pressEscape = true;
            }
            catch { }
#endif

            if (pressEscape)
            {
                if (tacticalMapModal != null && tacticalMapModal.activeSelf) CloseTacticalMapPopup();
                if (dataSourcesModalGo != null && dataSourcesModalGo.activeSelf) CloseNASADataPopup();
                if (roundSelectorModal != null && roundSelectorModal.activeSelf) CloseRoundSelectorModal();
                if (roundResultsModal != null && roundResultsModal.activeSelf) CloseRoundResultsModal();
            }

            // Round Selector shortcut [R]
            bool roundSelectPressed = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                {
                    roundSelectPressed = true;
                }
            }
            catch { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.R)) roundSelectPressed = true;
            }
            catch { }
#endif
            if (roundSelectPressed)
            {
                ToggleRoundSelectorModal();
            }

            // Eat Food key shortcut [E]
            bool eatPressed = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    eatPressed = true;
                }
            }
            catch { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.E)) eatPressed = true;
            }
            catch { }
#endif
            if (eatPressed)
            {
                PerformPlayerEatFood();
            }

            // Auto-eat emergency food only when character HP is critically low (< 25%)
            if (activeHealth != null && activeHealth.currentHealth > 0f && activeHealth.currentHealth < (activeHealth.maxHealth * 0.25f))
            {
                if (Time.time >= nextHudAutoEatTime)
                {
                    nextHudAutoEatTime = Time.time + 4.0f;
                    PerformPlayerAutoEatFood();
                }
            }
        }

        private float nextHudAutoEatTime = 0f;

        public void PerformPlayerEatFood()
        {
            var badge = GameObject.Find("FoodBadge");
            int count = 1;
            if (badge != null)
            {
                var txt = badge.GetComponent<Text>();
                if (txt != null) int.TryParse(txt.text, out count);
            }

            if (count <= 0)
            {
                HudEvents.RaiseEnvironmentalRestoration("⚠️ No food in inventory! Collect Keora fruits in the forest.");
                return;
            }

            if (activeHealth != null)
            {
                if (activeHealth.currentHealth >= activeHealth.maxHealth)
                {
                    HudEvents.RaiseEnvironmentalRestoration("HP is already FULL! (স্বাস্থ্য সম্পূর্ণ)");
                    return;
                }

                count--;
                if (badge != null)
                {
                    var txt = badge.GetComponent<Text>();
                    if (txt != null) txt.text = count.ToString();
                }
                activeHealth.EatFood(25f);
                HudEvents.RaiseEnvironmentalRestoration($"🍗 +25 HP — Ate Food Ration! Health: {Mathf.CeilToInt(activeHealth.currentHealth)}/{Mathf.CeilToInt(activeHealth.maxHealth)}");
            }
        }

        public void PerformPlayerAutoEatFood()
        {
            var badge = GameObject.Find("FoodBadge");
            int count = 0;
            if (badge != null)
            {
                var txt = badge.GetComponent<Text>();
                if (txt != null) int.TryParse(txt.text, out count);
            }

            if (count > 0 && activeHealth != null && activeHealth.currentHealth > 0f)
            {
                count--;
                if (badge != null)
                {
                    var txt = badge.GetComponent<Text>();
                    if (txt != null) txt.text = count.ToString();
                }
                activeHealth.EatFood(25f);
                HudEvents.RaiseEnvironmentalRestoration($"⚠️ CRITICAL HP! Auto-ate Keora Fruit (+25 HP)! Health: {Mathf.CeilToInt(activeHealth.currentHealth)}/{Mathf.CeilToInt(activeHealth.maxHealth)}");
            }
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
                float maxHp = (activeHealth != null) ? activeHealth.maxHealth : 150f;
                float curHp = (activeHealth != null) ? activeHealth.currentHealth : healthRatio * maxHp;
                healthValueText.text = $"{Mathf.CeilToInt(curHp)}";
            }
        }

        private string FormatHazardReadingEnglish(float rawValue, string unit)
        {
            string u = (unit ?? "").ToLowerInvariant();
            if (u.Contains("কুমির") || u.Contains("croc"))
                return $"Threat Level: {rawValue:F0}% — Saltwater Crocodile River Channel";
            if (u.Contains("বন উজাড়") || u.Contains("deforest") || u.Contains("canopy") || u.Contains("ndvi"))
                return $"NDVI Index: {rawValue:F2} — Severe Deforestation & Canopy Loss";
            if (u.Contains("ফাঁদ") || u.Contains("trap") || u.Contains("poach"))
                return $"Threat Level: {rawValue:F0}% — Illegal Poacher Wire Snares";
            if (u.Contains("জাল") || u.Contains("net"))
                return $"Density: {rawValue:F0}% — Banned Monofilament Current Net";
            if (u.Contains("লবণাক্ত") || u.Contains("psu") || u.Contains("salin"))
                return $"Salinity: {rawValue:F1} PSU — Hypersaline Mudflat & Tree Dieback";
            if (u.Contains("শব্দ") || u.Contains("db") || u.Contains("trawler") || u.Contains("motor"))
                return $"Noise: {rawValue:F0} dB — Trawler Motor Acoustic Pollution";
            if (u.Contains("পলি") || u.Contains("shallow") || u.Contains("silt"))
                return $"Depth: {rawValue:F1} m — Silt-Choked Shallow Channel";

            return $"Sensor Telemetry: {rawValue:F2} {unit}";
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
                hazardValueText.text = FormatHazardReadingEnglish(rawValue, unit);
                hazardValueText.fontSize = 17;
                hazardValueText.fontStyle = FontStyle.Bold;
                hazardValueText.color = new Color(0.96f, 0.98f, 1.0f);
            }

            if (hazardSubText != null)
            {
                hazardSubText.fontSize = 12;
                hazardSubText.fontStyle = FontStyle.Bold;
                hazardSubText.color = new Color(0.40f, 0.95f, 0.70f);

                string u = (unit ?? "").ToLowerInvariant();
                if (u.Contains("ndvi") || u.Contains("deforest") || u.Contains("উজাড়"))
                    hazardSubText.text = "NASA MODIS (MOD13Q1) Canopy Observation • Plant Seeds [1] to Reforest";
                else if (u.Contains("psu") || u.Contains("salin") || u.Contains("লবণাক্ত"))
                    hazardSubText.text = "NASA PO.DAAC SMAP Salinity Data • Neutralize with Cleanser [3]";
                else if (u.Contains("croc") || u.Contains("কুমির"))
                    hazardSubText.text = "Wildlife Sanctuary Habitat • Stay on High Riverbanks";
                else if (u.Contains("net") || u.Contains("জাল"))
                    hazardSubText.text = "Fisheries Enforcement Layer • Cut with Knife [2]";
                else if (u.Contains("trap") || u.Contains("ফাঁদ"))
                    hazardSubText.text = "Poacher Patrol Layer • Dismantle with Trap Destroyer [4]";
                else
                    hazardSubText.text = "Verified Earth Observation Sensor Telemetry";
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

            // 1. Calculate normalized UV coordinates of player on Landsat map (stays in lush forest)
            Vector2 playerUV = WorldToLandsatUV(worldPos.x, worldPos.z);
            float playerU = playerUV.x;
            float playerV = playerUV.y;

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

                Vector2 markerUV = WorldToLandsatUV(info.worldX, info.worldZ);

                float deltaU = markerUV.x - playerU;
                float deltaV = markerUV.y - playerV;

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

            // 5. Update coordinate readout with authentic NASA Earth Science telemetry
            if (coordDisplayText != null)
            {
                (float lat, float lon) = WorldToNasaCoordinates(worldPos.x, worldPos.z);
                coordDisplayText.text = $"LAT: {lat:F3}°N | LON: {lon:F3}°E  ({worldPos.x:F0}m, {worldPos.z:F0}m)";
            }
        }

        private void OnLevelCompleted()
        {
            if (CompetitiveRoundManager.Instance == null)
            {
                ShowEndCard("MISSION ACCOMPLISHED!", new Color(0.1f, 0.7f, 0.3f));
            }
        }

        private void OnGameOver()
        {
            if (CompetitiveRoundManager.Instance == null)
            {
                ShowEndCard("SPECIES IN CRITICAL DISTRESS", new Color(0.85f, 0.15f, 0.15f));
            }
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
                    child.SetActive(false);
                    child.transform.SetParent(null, false);
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
            if (defaultFont == null)
            {
                try { defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch { }
            }

            Font kalpurushFont = Resources.Load<Font>("Fonts/Kalpurush");
#if UNITY_EDITOR
            if (kalpurushFont == null)
            {
                kalpurushFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Kalpurush.ttf");
            }
#endif
            if (kalpurushFont == null) kalpurushFont = defaultFont;
            cachedDefaultFont = defaultFont;
            cachedKalpurushFont = kalpurushFont;

            EnsureEventSystem();

            BuildTopLeftMinimap(canvasGo.transform, defaultFont);
            BuildTopLeftHealthBar(canvasGo.transform, defaultFont);
            BuildHazardPopup(canvasGo.transform, defaultFont);
            BuildFactCardOverlay(canvasGo.transform, kalpurushFont);
            BuildDataSourcesScreen(canvasGo.transform, defaultFont);
            BuildTacticalMapPopup(canvasGo.transform, defaultFont);
            BuildBackpackHUD(canvasGo.transform, defaultFont);
            BuildRestorationToast(canvasGo.transform, defaultFont);
            BuildTouchControls(canvasGo.transform, defaultFont);
            BuildCompetitiveTopBar(canvasGo.transform, defaultFont);
            BuildCompetitiveRoundResultsModal(canvasGo.transform, defaultFont);
            BuildChampionshipPodiumModal(canvasGo.transform, defaultFont);
            BuildRoundSelectorModal(canvasGo.transform, defaultFont);

            // Popups & overlays must be hidden by default
            if (hazardPopupPanel != null) hazardPopupPanel.SetActive(false);
            if (factCardPanel != null) factCardPanel.SetActive(false);
            if (dataSourcesModalGo != null) dataSourcesModalGo.SetActive(false);
            else if (dataSourcesPanel != null) dataSourcesPanel.SetActive(false);
            if (tacticalMapModal != null) tacticalMapModal.SetActive(false);
            if (restorationToastPanel != null) restorationToastPanel.SetActive(false);
            if (roundResultsModal != null) roundResultsModal.SetActive(false);
            if (championshipModal != null) championshipModal.SetActive(false);
            if (roundSelectorModal != null) roundSelectorModal.SetActive(false);
        }

        public static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
            {
                if (Application.isPlaying) Destroy(standaloneModule);
                else DestroyImmediate(standaloneModule);
            }

            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                inputSystemModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            try
            {
                inputSystemModule.AssignDefaultActions();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[HudController] Could not assign default actions: " + ex.Message);
            }
#else
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
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
            minimapContainer.sizeDelta = new Vector2(minimapFrameSize + 22f, minimapFrameSize + 92f);

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
            titleText.alignment = TextAnchor.MiddleLeft;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(10f, -6f);
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

            // ── 6. Fixed Center Player Marker (Directional Navigation Triangle) ──
            GameObject pMarkerGo = new GameObject("Player_Marker", typeof(RectTransform));
            pMarkerGo.transform.SetParent(maskGo.transform, false);
            playerMarker = pMarkerGo.GetComponent<RectTransform>();
            playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
            playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
            playerMarker.pivot = new Vector2(0.5f, 0.5f);
            playerMarker.anchoredPosition = Vector2.zero;
            playerMarker.sizeDelta = new Vector2(26f, 26f);

            // Outer Pulse Ring
            GameObject pRing = new GameObject("Pulse_Ring", typeof(RectTransform), typeof(Image));
            pRing.transform.SetParent(pMarkerGo.transform, false);
            var prRt = pRing.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0.5f, 0.5f);
            prRt.anchorMax = new Vector2(0.5f, 0.5f);
            prRt.sizeDelta = new Vector2(22f, 22f);
            var prImg = pRing.GetComponent<Image>();
            prImg.color = new Color(0.20f, 0.95f, 0.55f, 0.35f);

            // Directional Triangle Player Marker (High-Contrast Navigation Arrow)
            GameObject pTri = new GameObject("Triangle_Marker", typeof(RectTransform), typeof(Image));
            pTri.transform.SetParent(pMarkerGo.transform, false);
            var triRt = pTri.GetComponent<RectTransform>();
            triRt.anchorMin = new Vector2(0.5f, 0.5f);
            triRt.anchorMax = new Vector2(0.5f, 0.5f);
            triRt.pivot = new Vector2(0.5f, 0.5f);
            triRt.anchoredPosition = Vector2.zero;
            triRt.sizeDelta = new Vector2(24f, 24f);

            var triImg = pTri.GetComponent<Image>();
            Sprite triSp = Resources.Load<Sprite>("Textures/UI/icon_triangle_marker");
#if UNITY_EDITOR
            if (triSp == null) triSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_triangle_marker.png");
#endif
            if (triSp != null)
            {
                triImg.sprite = triSp;
                triImg.preserveAspect = true;
                triImg.color = Color.white;
            }
            else
            {
                triImg.color = new Color(0.15f, 0.95f, 0.50f, 1f);
            }

            // ── 7. Outer Radar Border Frame (Sits on top of Mask) ──
            GameObject borderFrameGo = new GameObject("Minimap_Border_Frame", typeof(RectTransform), typeof(Image), typeof(Button));
            borderFrameGo.transform.SetParent(mapBox.transform, false);
            var bfBtn = borderFrameGo.GetComponent<Button>();
            bfBtn.onClick.AddListener(OpenTacticalMapPopup);

            // Expand hint badge button at top right of minimap
            GameObject expGo = new GameObject("Minimap_Expand_Badge", typeof(RectTransform), typeof(Image), typeof(Button));
            expGo.transform.SetParent(mapBox.transform, false);
            var ebRt = expGo.GetComponent<RectTransform>();
            ebRt.anchorMin = new Vector2(1f, 1f);
            ebRt.anchorMax = new Vector2(1f, 1f);
            ebRt.pivot = new Vector2(1f, 1f);
            ebRt.anchoredPosition = new Vector2(-6f, -5f);
            ebRt.sizeDelta = new Vector2(86f, 20f);

            var ebImg = expGo.GetComponent<Image>();
            if (roundedSprite != null) { ebImg.sprite = roundedSprite; ebImg.type = Image.Type.Sliced; }
            ebImg.color = new Color(0.12f, 0.30f, 0.25f, 0.90f);
            var ebOut = expGo.AddComponent<Outline>();
            ebOut.effectColor = new Color(0.35f, 0.95f, 0.75f, 0.85f);
            var ebBtn = expGo.GetComponent<Button>();
            ebBtn.onClick.AddListener(OpenTacticalMapPopup);

            GameObject ebTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            ebTxtGo.transform.SetParent(expGo.transform, false);
            var ebTxt = ebTxtGo.GetComponent<Text>();
            ebTxt.font = font;
            ebTxt.fontSize = 9;
            ebTxt.fontStyle = FontStyle.Bold;
            ebTxt.alignment = TextAnchor.MiddleCenter;
            ebTxt.color = new Color(0.98f, 0.95f, 0.45f);
            ebTxt.text = "⛶ FULL MAP [M]";
            ebTxt.raycastTarget = false;
            var ebTxtRt = ebTxtGo.GetComponent<RectTransform>();
            ebTxtRt.anchorMin = Vector2.zero;
            ebTxtRt.anchorMax = Vector2.one;
            ebTxtRt.sizeDelta = Vector2.zero;

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

            // ── Tap Hint Strip directly under radar frame ──
            GameObject hintGo = new GameObject("Minimap_Tap_Hint_Strip", typeof(RectTransform), typeof(Image), typeof(Button));
            hintGo.transform.SetParent(mapBox.transform, false);
            var hRt = hintGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0f);
            hRt.anchorMax = new Vector2(0.5f, 0f);
            hRt.pivot = new Vector2(0.5f, 0f);
            hRt.anchoredPosition = new Vector2(0f, 40f);
            hRt.sizeDelta = new Vector2(minimapFrameSize - 8f, 18f);

            var hImg = hintGo.GetComponent<Image>();
            if (roundedSprite != null) { hImg.sprite = roundedSprite; hImg.type = Image.Type.Sliced; }
            hImg.color = new Color(0.06f, 0.22f, 0.28f, 0.88f);
            var hOut = hintGo.AddComponent<Outline>();
            hOut.effectColor = new Color(0.35f, 0.95f, 0.75f, 0.75f);
            var hBtn = hintGo.GetComponent<Button>();
            hBtn.onClick.AddListener(OpenTacticalMapPopup);

            GameObject hTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            hTxtGo.transform.SetParent(hintGo.transform, false);
            var hTxt = hTxtGo.GetComponent<Text>();
            hTxt.font = font;
            hTxt.fontSize = 8;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.color = new Color(0.92f, 0.98f, 0.80f);
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.text = "👆 TAP RADAR FOR NASA FULL MAP";
            hTxt.raycastTarget = false;
            var hTxtRt = hTxtGo.GetComponent<RectTransform>();
            hTxtRt.anchorMin = Vector2.zero;
            hTxtRt.anchorMax = Vector2.one;
            hTxtRt.sizeDelta = Vector2.zero;

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
            coordRt.anchoredPosition = new Vector2(0f, 22f);
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
            creditRt.anchoredPosition = new Vector2(0f, 6f);
            creditRt.sizeDelta = new Vector2(0f, 14f);

            // ── 10. Minimap Full-Area Touch & Click Interceptor (Any tap opens Full Map) ──
            GameObject tapAreaGo = new GameObject("Minimap_Full_Tap_Area", typeof(RectTransform), typeof(Image), typeof(Button));
            tapAreaGo.transform.SetParent(mapBox.transform, false);
            var taRt = tapAreaGo.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.sizeDelta = Vector2.zero;

            var taImg = tapAreaGo.GetComponent<Image>();
            taImg.color = new Color(0f, 0f, 0f, 0f);
            taImg.raycastTarget = true;

            var taBtn = tapAreaGo.GetComponent<Button>();
            taBtn.targetGraphic = taImg;
            taBtn.onClick.AddListener(OpenTacticalMapPopup);
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

            // 1. Only authentic NASA Landsat/MODIS Problem Zones (Deforestation / Canopy Loss / Crisis) on Minimap
            LevelData ld = gameManager != null ? gameManager.CurrentLevelData : null;
            var markedPositions = new System.Collections.Generic.List<Vector2>();

            if (ld != null && ld.hazardZones != null && ld.hazardZones.Count > 0)
            {
                foreach (var hz in ld.hazardZones)
                {
                    Vector2 p = new Vector2(hz.centerX, hz.centerZ);
                    bool tooClose = false;
                    foreach (var mp in markedPositions)
                    {
                        if (Vector2.Distance(p, mp) < 18f) { tooClose = true; break; }
                    }
                    if (!tooClose)
                    {
                        markedPositions.Add(p);
                        SpawnMinimapHazardMarker(hz.centerX, hz.centerZ, hz.riskTier, hz.radius);
                    }
                }

                if (ld.goalZone != null)
                {
                    SpawnMinimapGoalMarker(ld.goalZone.centerX, ld.goalZone.centerZ);
                }
            }
            else
            {
                // Fallback: WorldDangerZone authentic deforestation zones
                var worldZones = FindObjectsByType<WorldDangerZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var wdz in worldZones)
                {
                    if (wdz != null && !wdz.isFinished)
                    {
                        Vector2 p = new Vector2(wdz.transform.position.x, wdz.transform.position.z);
                        bool tooClose = false;
                        foreach (var mp in markedPositions)
                        {
                            if (Vector2.Distance(p, mp) < 18f) { tooClose = true; break; }
                        }
                        if (!tooClose)
                        {
                            markedPositions.Add(p);
                            SpawnMinimapHazardMarker(wdz.transform.position.x, wdz.transform.position.z, wdz.riskTier, wdz.interactionRadius);
                        }
                    }
                }
            }

            // Always ensure Goal Sanctuary is marked at ocean coast
            if (ld == null || ld.goalZone == null)
            {
                SpawnMinimapGoalMarker(35f, -95f);
            }

            // 2. Green Healing Sanctuaries (Glades)
            var glades = FindObjectsByType<SundarbanGuardian.Environment.HealingZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hz in glades)
            {
                SpawnMinimapGladeMarker(hz.transform.position.x, hz.transform.position.z);
            }

            // 3. Collectible Tool Pickups on Minimap (small crisp 3D icons)
            var tools = FindObjectsByType<ToolPickupItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var tp in tools)
            {
                SpawnMinimapToolMarker(tp);
            }

            // 4. Collectible Food Items on Minimap
            var foods = FindObjectsByType<SundarbanGuardian.Environment.FoodItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var f in foods)
            {
                SpawnMinimapFoodMarker(f);
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

            // Clean, uncluttered NASA Hazard Zone marker scaled cleanly
            float worldWidth = mapWorldMaxX - mapWorldMinX;
            float worldDiam = Mathf.Max(worldRadius * 2f, 20f);
            float pixelDiameter = Mathf.Clamp((worldDiam / worldWidth / minimapCropSize) * minimapFrameSize, 18f, 30f);

            GameObject zoneObj = new GameObject($"DangerZone_{riskTier}", typeof(RectTransform));
            zoneObj.transform.SetParent(parentTf, false);
            var rt = zoneObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(pixelDiameter, pixelDiameter);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // Free Fire Danger Zone Color: Deep Purple for Polluted Water, bright danger red for high, vivid warning amber for medium
            Color mainColor;
            string tierStr = (riskTier ?? "").ToLowerInvariant();
            bool isPolluted = tierStr.Contains("pollut") || tierStr.Contains("purple") || tierStr.Contains("toxic");

            if (isPolluted)
            {
                mainColor = new Color(0.55f, 0.08f, 0.82f, 1.0f); // Iconic Deep Purple for Polluted Water
            }
            else if (string.Equals(riskTier, "high", System.StringComparison.OrdinalIgnoreCase))
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

            // 1. Semi-transparent Fill Circle
            var fillGo = new GameObject("Fill_Circle", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(zoneObj.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.color = new Color(mainColor.r, mainColor.g, mainColor.b, isPolluted ? 0.38f : 0.24f);
            fillImg.raycastTarget = false;

            // 2. Bright Outer Border Ring
            var ringGo = new GameObject("Border_Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(zoneObj.transform, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.sizeDelta = Vector2.zero;
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = ringSprite;
            ringImg.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.92f);
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
            pulseImg.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.50f);
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
            coreOutl.effectColor = new Color(mainColor.r * 0.7f, 0f, mainColor.b * 0.7f, 0.95f);
            coreOutl.effectDistance = new Vector2(1f, -1f);

            // 5. Danger Zone Tactical Label
            Font font = Resources.Load<Font>("Fonts/SegoeUI");
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch { }
            }
            var labelGo = new GameObject("Danger_Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(zoneObj.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 1f);
            labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 1f);
            labelRt.sizeDelta = new Vector2(95f, 14f);
            var labelTxt = labelGo.GetComponent<Text>();
            if (font != null) labelTxt.font = font;
            labelTxt.text = isPolluted ? "POLLUTED WATER" : "DANGER ZONE";
            labelTxt.fontSize = 8;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.color = isPolluted ? new Color(0.85f, 0.50f, 1.0f, 0.98f) : new Color(1f, 0.35f, 0.35f, 0.95f);
            var labelOutl = labelGo.AddComponent<Outline>();
            labelOutl.effectColor = new Color(0.1f, 0f, 0.1f, 0.95f);
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

        private void OnHazardZoneCleared(float x, float z)
        {
            for (int i = 0; i < activeMinimapMarkers.Count; i++)
            {
                var m = activeMinimapMarkers[i];
                if (m == null || !m.isDangerZone) continue;
                float dist = Vector2.Distance(new Vector2(m.worldX, m.worldZ), new Vector2(x, z));
                if (dist < 28f)
                {
                    m.isDangerZone = false;
                    m.baseColor = new Color(0.2f, 0.9f, 0.4f, 1f);
                    if (m.fillImage != null)
                        m.fillImage.color = new Color(0.18f, 0.85f, 0.35f, 0.22f);
                    if (m.ringImage != null)
                        m.ringImage.color = new Color(0.25f, 0.95f, 0.50f, 0.92f);
                    if (m.pulseRingRt != null)
                        m.pulseRingRt.gameObject.SetActive(false);

                    var lbl = m.rectTransform.GetComponentInChildren<Text>();
                    if (lbl != null)
                    {
                        lbl.text = "REFORESTED \u2714";
                        lbl.color = new Color(0.40f, 1.0f, 0.60f, 0.95f);
                    }
                    break;
                }
            }
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

        private void SpawnMinimapToolMarker(ToolPickupItem item)
        {
            if (item == null) return;
            Transform parentTf = markerLayer != null ? markerLayer.transform : (minimapMaskRect != null ? minimapMaskRect.transform : (mapContentRect != null ? mapContentRect.transform : null));
            if (parentTf == null) return;

            GameObject toolMarkerGo = new GameObject($"Minimap_Tool_{item.toolType}", typeof(RectTransform), typeof(Image));
            toolMarkerGo.transform.SetParent(parentTf, false);
            var rt = toolMarkerGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(20f, 20f); // small crisp pill
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // Background circular pill for high contrast against satellite terrain/water
            var bgImg = toolMarkerGo.GetComponent<Image>();
            bgImg.sprite = GetOrCreateCircleSprite(64, false);
            bgImg.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            bgImg.raycastTarget = false;

            var outline = toolMarkerGo.AddComponent<Outline>();
            outline.effectColor = ToolInfo.GetThemeColor(item.toolType);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Child crisp tool icon
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(toolMarkerGo.transform, false);
            var iconImg = iconGo.GetComponent<Image>();
            int tIdx = (int)item.toolType;
            if (cachedToolSprites != null && tIdx >= 0 && tIdx < cachedToolSprites.Length && cachedToolSprites[tIdx] != null)
            {
                iconImg.sprite = cachedToolSprites[tIdx];
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
            }
            else
            {
                iconImg.color = ToolInfo.GetThemeColor(item.toolType);
            }
            iconImg.raycastTarget = false;
            var iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero;
            iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(2f, 2f);
            iRt.offsetMax = new Vector2(-2f, -2f);

            activeMinimapMarkers.Add(new MinimapMarkerInfo
            {
                rectTransform = rt,
                worldX = item.transform.position.x,
                worldZ = item.transform.position.z,
                markerType = "tool",
                toolItem = item
            });
        }

        private void RemoveMinimapToolMarker(ToolPickupItem item)
        {
            if (item == null) return;
            for (int i = activeMinimapMarkers.Count - 1; i >= 0; i--)
            {
                var m = activeMinimapMarkers[i];
                if (m != null && m.markerType == "tool" &&
                    (m.toolItem == item ||
                     (Mathf.Abs(m.worldX - item.transform.position.x) < 1.0f &&
                      Mathf.Abs(m.worldZ - item.transform.position.z) < 1.0f)))
                {
                    if (m.rectTransform != null)
                    {
                        if (Application.isPlaying) Destroy(m.rectTransform.gameObject);
                        else DestroyImmediate(m.rectTransform.gameObject);
                    }
                    activeMinimapMarkers.RemoveAt(i);
                    break;
                }
            }
        }

        private void SpawnMinimapFoodMarker(SundarbanGuardian.Environment.FoodItem item)
        {
            if (item == null) return;
            Transform parentTf = markerLayer != null ? markerLayer.transform : (minimapMaskRect != null ? minimapMaskRect.transform : (mapContentRect != null ? mapContentRect.transform : null));
            if (parentTf == null) return;

            GameObject foodMarkerGo = new GameObject("Minimap_Food_Item", typeof(RectTransform), typeof(Image));
            foodMarkerGo.transform.SetParent(parentTf, false);
            var rt = foodMarkerGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(18f, 18f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var bgImg = foodMarkerGo.GetComponent<Image>();
            bgImg.sprite = GetOrCreateCircleSprite(64, false);
            bgImg.color = new Color(0.06f, 0.16f, 0.08f, 0.92f);
            bgImg.raycastTarget = false;

            var outline = foodMarkerGo.AddComponent<Outline>();
            outline.effectColor = new Color(1.0f, 0.80f, 0.25f, 1.0f); // warm golden rim
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            GameObject txtGo = new GameObject("Icon", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(foodMarkerGo.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.font = cachedDefaultFont;
            txt.fontSize = 11;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "🍗";
            txt.raycastTarget = false;
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            activeMinimapMarkers.Add(new MinimapMarkerInfo
            {
                rectTransform = rt,
                worldX = item.transform.position.x,
                worldZ = item.transform.position.z,
                markerType = "food"
            });
        }

        private void RemoveMinimapFoodMarker(SundarbanGuardian.Environment.FoodItem item)
        {
            if (item == null) return;
            for (int i = activeMinimapMarkers.Count - 1; i >= 0; i--)
            {
                var m = activeMinimapMarkers[i];
                if (m != null && m.markerType == "food" &&
                    Mathf.Abs(m.worldX - item.transform.position.x) < 1.5f &&
                    Mathf.Abs(m.worldZ - item.transform.position.z) < 1.5f)
                {
                    if (m.rectTransform != null)
                    {
                        if (Application.isPlaying) Destroy(m.rectTransform.gameObject);
                        else DestroyImmediate(m.rectTransform.gameObject);
                    }
                    activeMinimapMarkers.RemoveAt(i);
                    break;
                }
            }
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
            healthValueText.text = "150";
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
            panelRt.sizeDelta = new Vector2(560f, 96f);

            var panelBg = hazardPopupPanel.GetComponent<Image>();
            panelBg.color = new Color(0.04f, 0.08f, 0.12f, 0.85f);

            var outline = hazardPopupPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.85f, 1.0f, 0.90f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Badge Header (Tier)
            GameObject badgeGo = new GameObject("Tier_Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(hazardPopupPanel.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -12f);
            badgeRt.sizeDelta = new Vector2(300f, 22f);
            hazardBadgeImage = badgeGo.GetComponent<Image>();
            hazardBadgeImage.color = new Color(0.9f, 0.2f, 0.2f);

            GameObject badgeTextGo = new GameObject("Badge_Text", typeof(RectTransform), typeof(Text));
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            hazardBadgeText = badgeTextGo.GetComponent<Text>();
            hazardBadgeText.font = font;
            hazardBadgeText.fontSize = 12;
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
            hazardValueText.fontSize = 17;
            hazardValueText.fontStyle = FontStyle.Bold;
            hazardValueText.alignment = TextAnchor.MiddleCenter;
            hazardValueText.color = new Color(0.96f, 0.98f, 1.0f);
            hazardValueText.text = "Reading: 0.82 NDVI";
            var valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0f, 0.5f);
            valRt.anchorMax = new Vector2(1f, 0.5f);
            valRt.anchoredPosition = new Vector2(0f, -5f);
            valRt.sizeDelta = new Vector2(0f, 26f);

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
            factCardTitle.color = new Color(0.35f, 1.0f, 0.85f);
            factCardTitle.text = "SUNDARBANS SPECIES SURVIVAL BRIEFING";
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
            factCardPhotoCaption.color = new Color(0.70f, 0.90f, 1.0f);
            factCardPhotoCaption.text = "NASA SATELLITE FIELD OBSERVATION\nSundarbans UNESCO Biosphere Reserve";

            // Body (English Fact Text)
            GameObject bodyGo = new GameObject("Fact_Body_Bn", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(factCardPanel.transform, false);
            factCardBodyBn = bodyGo.GetComponent<Text>();
            factCardBodyBn.font = font;
            factCardBodyBn.fontSize = 15;
            factCardBodyBn.fontStyle = FontStyle.Bold;
            factCardBodyBn.lineSpacing = 1.30f;
            factCardBodyBn.alignment = TextAnchor.UpperLeft;
            factCardBodyBn.color = new Color(0.95f, 0.98f, 1.0f);
            factCardBodyBn.text = "MOTHER HILSA REPRODUCTIVE RUN (CONSERVATION BRIEFING):\n\n" +
                                  "• October Spawning Ban: 22-day nationwide prohibition on catching or transporting breeding hilsa.\n\n" +
                                  "• Jatka Protection: 8-month ban from Nov 1 to June 30 protecting juvenile hilsa under 25cm.\n\n" +
                                  "• Legal Enforcement: Up to 2 years imprisonment and 500,000 BDT fines for illegal netting.\n\n" +
                                  "• 2024 Impact Data: 52% of mother hilsa safely reached upstream freshwater to spawn.\n\n" +
                                  "• Mission Hazard: Evade illegal monofilament gillnets to protect the spawning migration!";
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
            factCardCitation.color = new Color(0.70f, 0.90f, 1.0f);
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
            bst.text = "START MISSION ▶";
            bst.raycastTarget = false;
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
            bnt.text = "NEXT SPECIES MISSION →";
            bnt.raycastTarget = false;
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
            brt.text = "RESTART MISSION ↺";
            brt.raycastTarget = false;
            var brtRt = btnReplayTextGo.GetComponent<RectTransform>();
            brtRt.anchorMin = Vector2.zero;
            brtRt.anchorMax = Vector2.one;
            brtRt.sizeDelta = Vector2.zero;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // 🛰️ NASA LEARNING & EXPLORATION HUB (শিশু বিজ্ঞান ও অক্ষ শিক্ষা একাডেমি)
        // ──────────────────────────────────────────────────────────────────────────
        private float lastNasaToggleTime = 0f;

        public void ToggleNASADataPopup()
        {
            if (Time.unscaledTime - lastNasaToggleTime < 0.25f) return;
            lastNasaToggleTime = Time.unscaledTime;

            if (dataSourcesModalGo == null && dataSourcesPanel == null) return;
            GameObject target = dataSourcesModalGo != null ? dataSourcesModalGo : dataSourcesPanel;
            bool nextState = !target.activeSelf;
            if (nextState) OpenNASADataPopup();
            else CloseNASADataPopup();
        }

        public void OpenNASADataPopup()
        {
            GameObject target = dataSourcesModalGo != null ? dataSourcesModalGo : dataSourcesPanel;
            if (target != null)
            {
                target.transform.SetAsLastSibling();
                target.SetActive(true);
                SwitchNasaLearningTab(currentNasaLearningTab);
            }
        }

        public void CloseNASADataPopup()
        {
            GameObject target = dataSourcesModalGo != null ? dataSourcesModalGo : dataSourcesPanel;
            if (target != null)
            {
                target.SetActive(false);
            }
            if (didYouKnowToastGo != null)
            {
                didYouKnowToastGo.SetActive(false);
            }
        }

        private void BuildDataSourcesScreen(Transform parent, Font font)
        {
            Font banglaFont = cachedKalpurushFont ?? font;

            // ── 1. Top-Right "NASA DATA POPUP [N]" Button ──
            GameObject btnOpenGo = new GameObject("Btn_Data_Sources", typeof(RectTransform), typeof(Image), typeof(Button));
            btnOpenGo.transform.SetParent(parent, false);
            btnOpenGo.transform.SetAsLastSibling();
            var btnRt = btnOpenGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-20f, -14f);
            btnRt.sizeDelta = new Vector2(210f, 40f);

            var btnImg = btnOpenGo.GetComponent<Image>();
            Sprite nasaCardSp = null;
#if UNITY_EDITOR
            nasaCardSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/rounded_card_12px.png");
#endif
            if (nasaCardSp == null) nasaCardSp = Resources.Load<Sprite>("Textures/UI/rounded_card_12px");
            if (nasaCardSp != null) { btnImg.sprite = nasaCardSp; btnImg.type = Image.Type.Sliced; }
            btnImg.color = new Color(0.04f, 0.12f, 0.20f, 0.45f);

            var btnOutline = btnOpenGo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.35f, 0.85f, 0.98f, 0.70f);
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject btnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTxtGo.transform.SetParent(btnOpenGo.transform, false);
            var btnTxt = btnTxtGo.GetComponent<Text>();
            btnTxt.font = font;
            btnTxt.fontSize = 12;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = new Color(0.92f, 0.97f, 1.0f);
            btnTxt.text = "🛰️ NASA HUB [N]";
            btnTxt.raycastTarget = false;
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.sizeDelta = Vector2.zero;

            dataSourcesButton = btnOpenGo.GetComponent<Button>();
            dataSourcesButton.onClick.AddListener(() => {
                ToggleNASADataPopup();
            });

            var clickFallback = btnOpenGo.AddComponent<NasaPopupDirectPointer>();
            clickFallback.onTrigger = () => ToggleNASADataPopup();

            // ── 2. Full-Screen Modal Backdrop (Scrim) ──
            dataSourcesModalGo = new GameObject("NASA_Learning_Modal", typeof(RectTransform), typeof(Image));
            dataSourcesModalGo.transform.SetParent(parent, false);
            var modalRt = dataSourcesModalGo.GetComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.sizeDelta = Vector2.zero;

            var modalImg = dataSourcesModalGo.GetComponent<Image>();
            modalImg.color = new Color(0.02f, 0.04f, 0.08f, 0.92f); // Dark translucent glass
            modalImg.raycastTarget = true; // Blocks clicks through to game world

            // Clicking outside panel closes modal
            var modalBtn = dataSourcesModalGo.AddComponent<Button>();
            modalBtn.transition = Selectable.Transition.None;
            modalBtn.onClick.AddListener(() => CloseNASADataPopup());

            // ── 3. Main Center Dialog Card ──
            dataSourcesPanel = new GameObject("DataSources_Modal_Panel", typeof(RectTransform), typeof(Image));
            dataSourcesPanel.transform.SetParent(dataSourcesModalGo.transform, false);
            var panelRt = dataSourcesPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(980f, 620f);

            var panelImg = dataSourcesPanel.GetComponent<Image>();
            panelImg.color = new Color(0.04f, 0.08f, 0.12f, 0.99f);
            var panelOutline = dataSourcesPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.25f, 0.85f, 0.95f, 0.95f);
            panelOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // Prevent clicks on dialog card from closing through modal backdrop
            var cardBlocker = dataSourcesPanel.AddComponent<Button>();
            cardBlocker.transition = Selectable.Transition.None;

            // ── 4. Dialog Top Header ──
            GameObject headerGo = new GameObject("Header_Bar", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(dataSourcesPanel.transform, false);
            var hRt = headerGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0f, 56f);
            headerGo.GetComponent<Image>().color = new Color(0.07f, 0.16f, 0.25f, 1f);

            // Title Text
            GameObject titleGo = new GameObject("Header_Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(headerGo.transform, false);
            nasaHubHeaderText = titleGo.GetComponent<Text>();
            nasaHubHeaderText.font = font;
            nasaHubHeaderText.fontSize = 17;
            nasaHubHeaderText.fontStyle = FontStyle.Bold;
            nasaHubHeaderText.alignment = TextAnchor.MiddleLeft;
            nasaHubHeaderText.color = new Color(0.35f, 0.92f, 1.0f);
            nasaHubHeaderText.text = "🛰️ NASA LEARNING & EXPLORATION HUB • সুন্দরবন বিজ্ঞান একাডেমি";
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(20f, 0f);
            tRt.offsetMax = new Vector2(-60f, -4f);

            // Subtitle Text
            GameObject subTitleGo = new GameObject("Header_Subtitle", typeof(RectTransform), typeof(Text));
            subTitleGo.transform.SetParent(headerGo.transform, false);
            var subText = subTitleGo.GetComponent<Text>();
            subText.font = font;
            subText.fontSize = 11;
            subText.alignment = TextAnchor.MiddleLeft;
            subText.color = new Color(0.85f, 0.88f, 0.92f);
            subText.text = "Learn How Satellites, 3D Spatial Axes & Mangrove Animals Save Earth's Largest Delta!";
            var stRt = subTitleGo.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(1f, 0.5f);
            stRt.offsetMin = new Vector2(20f, 4f);
            stRt.offsetMax = new Vector2(-60f, 0f);

            // Close 'X' Button in Header
            GameObject closeXGo = new GameObject("Btn_Close_X", typeof(RectTransform), typeof(Image), typeof(Button));
            closeXGo.transform.SetParent(headerGo.transform, false);
            var cxRt = closeXGo.GetComponent<RectTransform>();
            cxRt.anchorMin = new Vector2(1f, 0.5f);
            cxRt.anchorMax = new Vector2(1f, 0.5f);
            cxRt.pivot = new Vector2(1f, 0.5f);
            cxRt.anchoredPosition = new Vector2(-12f, 0f);
            cxRt.sizeDelta = new Vector2(34f, 34f);
            closeXGo.GetComponent<Image>().color = new Color(0.85f, 0.20f, 0.20f, 0.90f);
            var cxBtn = closeXGo.GetComponent<Button>();
            cxBtn.onClick.AddListener(() => CloseNASADataPopup());

            GameObject cxTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            cxTxtGo.transform.SetParent(closeXGo.transform, false);
            var cxTxt = cxTxtGo.GetComponent<Text>();
            cxTxt.font = font;
            cxTxt.fontSize = 16;
            cxTxt.fontStyle = FontStyle.Bold;
            cxTxt.alignment = TextAnchor.MiddleCenter;
            cxTxt.color = Color.white;
            cxTxt.text = "✕";
            cxTxt.raycastTarget = false;
            var cxTxtRt = cxTxtGo.GetComponent<RectTransform>();
            cxTxtRt.anchorMin = Vector2.zero;
            cxTxtRt.anchorMax = Vector2.one;
            cxTxtRt.sizeDelta = Vector2.zero;

            // ── 5. Tab Buttons Row ──
            GameObject tabRowGo = new GameObject("Tabs_Bar", typeof(RectTransform), typeof(Image));
            tabRowGo.transform.SetParent(dataSourcesPanel.transform, false);
            var tbRt = tabRowGo.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = new Vector2(0f, -56f);
            tbRt.sizeDelta = new Vector2(0f, 42f);
            tabRowGo.GetComponent<Image>().color = new Color(0.03f, 0.07f, 0.11f, 1f);

            string[] tabTitles = {
                "🦌 1. How Axis Works",
                "🧭 2. 3D Coordinates & GPS",
                "🛰️ 3. NASA Satellite Eyes",
                "🛡️ 4. Saving Sundarbans",
                "🎮 5. How To Play & Tools"
            };

            float tabWidth = 188f;
            float tabSpacing = 4f;
            float tabStartX = 10f;

            for (int i = 0; i < 5; i++)
            {
                int tabIdx = i;
                GameObject tabBtnGo = new GameObject($"TabBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                tabBtnGo.transform.SetParent(tabRowGo.transform, false);
                var tBtnRt = tabBtnGo.GetComponent<RectTransform>();
                tBtnRt.anchorMin = new Vector2(0f, 0.5f);
                tBtnRt.anchorMax = new Vector2(0f, 0.5f);
                tBtnRt.pivot = new Vector2(0f, 0.5f);
                tBtnRt.anchoredPosition = new Vector2(tabStartX + i * (tabWidth + tabSpacing), 0f);
                tBtnRt.sizeDelta = new Vector2(tabWidth, 34f);

                nasaTabBtnImages[i] = tabBtnGo.GetComponent<Image>();
                nasaTabButtons[i] = tabBtnGo.GetComponent<Button>();
                nasaTabButtons[i].onClick.AddListener(() => SwitchNasaLearningTab(tabIdx));

                GameObject tabTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                tabTxtGo.transform.SetParent(tabBtnGo.transform, false);
                nasaTabBtnTexts[i] = tabTxtGo.GetComponent<Text>();
                nasaTabBtnTexts[i].font = font;
                nasaTabBtnTexts[i].fontSize = 11;
                nasaTabBtnTexts[i].fontStyle = FontStyle.Bold;
                nasaTabBtnTexts[i].alignment = TextAnchor.MiddleCenter;
                nasaTabBtnTexts[i].text = tabTitles[i];
                nasaTabBtnTexts[i].raycastTarget = false;
                var tabTxtRt = tabTxtGo.GetComponent<RectTransform>();
                tabTxtRt.anchorMin = Vector2.zero;
                tabTxtRt.anchorMax = Vector2.one;
                tabTxtRt.sizeDelta = Vector2.zero;
            }

            // ── 6. Category Title & Badge Bar ──
            GameObject catHeaderGo = new GameObject("Category_Header", typeof(RectTransform));
            catHeaderGo.transform.SetParent(dataSourcesPanel.transform, false);
            var chRt = catHeaderGo.GetComponent<RectTransform>();
            chRt.anchorMin = new Vector2(0f, 1f);
            chRt.anchorMax = new Vector2(1f, 1f);
            chRt.pivot = new Vector2(0.5f, 1f);
            chRt.anchoredPosition = new Vector2(0f, -100f);
            chRt.sizeDelta = new Vector2(0f, 40f);

            // Title
            GameObject catTitleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            catTitleGo.transform.SetParent(catHeaderGo.transform, false);
            nasaHubCategoryTitleText = catTitleGo.GetComponent<Text>();
            nasaHubCategoryTitleText.font = font;
            nasaHubCategoryTitleText.fontSize = 15;
            nasaHubCategoryTitleText.fontStyle = FontStyle.Bold;
            nasaHubCategoryTitleText.alignment = TextAnchor.MiddleLeft;
            nasaHubCategoryTitleText.color = new Color(0.95f, 0.85f, 0.35f);
            var ctRt = catTitleGo.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 0f);
            ctRt.anchorMax = new Vector2(0.70f, 1f);
            ctRt.offsetMin = new Vector2(22f, 0f);
            ctRt.offsetMax = Vector2.zero;

            // Fact Badge Pill
            GameObject badgeGo = new GameObject("Badge_Pill", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(catHeaderGo.transform, false);
            var bPillRt = badgeGo.GetComponent<RectTransform>();
            bPillRt.anchorMin = new Vector2(0.70f, 0.15f);
            bPillRt.anchorMax = new Vector2(1f, 0.85f);
            bPillRt.offsetMin = Vector2.zero;
            bPillRt.offsetMax = new Vector2(-22f, 0f);
            badgeGo.GetComponent<Image>().color = new Color(0.12f, 0.35f, 0.25f, 0.90f);

            GameObject badgeTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            badgeTxtGo.transform.SetParent(badgeGo.transform, false);
            nasaHubFactBadgeText = badgeTxtGo.GetComponent<Text>();
            nasaHubFactBadgeText.font = font;
            nasaHubFactBadgeText.fontSize = 11;
            nasaHubFactBadgeText.fontStyle = FontStyle.Bold;
            nasaHubFactBadgeText.alignment = TextAnchor.MiddleCenter;
            nasaHubFactBadgeText.color = new Color(0.4f, 1.0f, 0.6f);
            nasaHubFactBadgeText.raycastTarget = false;
            var bTxtRt = badgeTxtGo.GetComponent<RectTransform>();
            bTxtRt.anchorMin = Vector2.zero;
            bTxtRt.anchorMax = Vector2.one;
            bTxtRt.sizeDelta = Vector2.zero;

            // ── 7. Scrollable Educational Body Text Container ──
            GameObject scrollGo = new GameObject("Scroll_View", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(dataSourcesPanel.transform, false);
            var sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = new Vector2(20f, 62f);
            sRt.offsetMax = new Vector2(-20f, -145f);

            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0.02f, 0.05f, 0.08f, 0.60f);

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var cRt = contentGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 600f);

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = vpRt;
            scrollRect.content = cRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;

            // Educational Text Inside Content
            GameObject textGo = new GameObject("Body_Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(contentGo.transform, false);
            nasaHubBodyText = textGo.GetComponent<Text>();
            nasaHubBodyText.font = banglaFont;
            nasaHubBodyText.fontSize = 14;
            nasaHubBodyText.lineSpacing = 1.35f;
            nasaHubBodyText.color = new Color(0.92f, 0.94f, 0.98f);
            var bodyRt = textGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(16f, 12f);
            bodyRt.offsetMax = new Vector2(-16f, -12f);

            // ── 8. Bottom Action Bar ──
            GameObject bottomBarGo = new GameObject("Bottom_Bar", typeof(RectTransform), typeof(Image));
            bottomBarGo.transform.SetParent(dataSourcesPanel.transform, false);
            var bbRt = bottomBarGo.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = Vector2.zero;
            bbRt.sizeDelta = new Vector2(0f, 58f);
            bottomBarGo.GetComponent<Image>().color = new Color(0.04f, 0.09f, 0.15f, 1f);

            // Button 1: Previous Lesson (◀ Previous)
            GameObject prevBtnGo = new GameObject("Btn_Prev_Lesson", typeof(RectTransform), typeof(Image), typeof(Button));
            prevBtnGo.transform.SetParent(bottomBarGo.transform, false);
            var pRt = prevBtnGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0.5f);
            pRt.anchorMax = new Vector2(0f, 0.5f);
            pRt.pivot = new Vector2(0f, 0.5f);
            pRt.anchoredPosition = new Vector2(20f, 0f);
            pRt.sizeDelta = new Vector2(150f, 38f);
            prevBtnGo.GetComponent<Image>().color = new Color(0.12f, 0.25f, 0.38f, 0.95f);
            var pBtn = prevBtnGo.GetComponent<Button>();
            pBtn.onClick.AddListener(() => SwitchNasaLearningTab((currentNasaLearningTab + 4) % 5));
            CreateButtonLabel(prevBtnGo.transform, "◀ Previous", font, 12);

            // Button 2: Next Lesson (Next ▶)
            GameObject nextBtnGo = new GameObject("Btn_Next_Lesson", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnGo.transform.SetParent(bottomBarGo.transform, false);
            var nRt = nextBtnGo.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.5f);
            nRt.anchorMax = new Vector2(0f, 0.5f);
            nRt.pivot = new Vector2(0f, 0.5f);
            nRt.anchoredPosition = new Vector2(180f, 0f);
            nRt.sizeDelta = new Vector2(150f, 38f);
            nextBtnGo.GetComponent<Image>().color = new Color(0.12f, 0.25f, 0.38f, 0.95f);
            var nBtn = nextBtnGo.GetComponent<Button>();
            nBtn.onClick.AddListener(() => SwitchNasaLearningTab((currentNasaLearningTab + 1) % 5));
            CreateButtonLabel(nextBtnGo.transform, "Next Lesson ▶", font, 12);

            // Button 3: Play Intro Video (🎬 তথ্যচিত্র)
            GameObject introBtnGo = new GameObject("Btn_Play_Intro", typeof(RectTransform), typeof(Image), typeof(Button));
            introBtnGo.transform.SetParent(bottomBarGo.transform, false);
            var ibRt = introBtnGo.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0.5f, 0.5f);
            ibRt.anchorMax = new Vector2(0.5f, 0.5f);
            ibRt.pivot = new Vector2(0.5f, 0.5f);
            ibRt.anchoredPosition = new Vector2(10f, 0f);
            ibRt.sizeDelta = new Vector2(190f, 38f);
            introBtnGo.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.65f, 0.95f);
            var ibBtn = introBtnGo.GetComponent<Button>();
            ibBtn.onClick.AddListener(() => {
                CloseNASADataPopup();
                var intro = Object.FindAnyObjectByType<SundarbanGuardian.UI.IntroSequenceController>();
                if (intro != null) intro.PlayIntro();
            });
            CreateButtonLabel(introBtnGo.transform, "🎬 তথ্যচিত্র (Video)", font, 12);

            // Button 4: Did You Know? Trivia (💡 মজার তথ্য)
            GameObject triviaBtnGo = new GameObject("Btn_Trivia", typeof(RectTransform), typeof(Image), typeof(Button));
            triviaBtnGo.transform.SetParent(bottomBarGo.transform, false);
            var trRt = triviaBtnGo.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(1f, 0.5f);
            trRt.anchorMax = new Vector2(1f, 0.5f);
            trRt.pivot = new Vector2(1f, 0.5f);
            trRt.anchoredPosition = new Vector2(-160f, 0f);
            trRt.sizeDelta = new Vector2(150f, 38f);
            triviaBtnGo.GetComponent<Image>().color = new Color(0.70f, 0.45f, 0.15f, 0.95f);
            var trBtn = triviaBtnGo.GetComponent<Button>();
            trBtn.onClick.AddListener(() => ShowRandomDidYouKnowFact());
            CreateButtonLabel(triviaBtnGo.transform, "💡 Did You Know?", font, 12);

            // Button 5: Close Hub (✓ Close [Esc])
            GameObject closeGo = new GameObject("Btn_Close_Hub", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(bottomBarGo.transform, false);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-20f, 0f);
            closeRt.sizeDelta = new Vector2(130f, 38f);
            closeGo.GetComponent<Image>().color = new Color(0.20f, 0.55f, 0.35f, 0.95f);
            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(() => CloseNASADataPopup());
            CreateButtonLabel(closeGo.transform, "✓ Close [Esc]", font, 12);

            // Build Did You Know Toast Box
            BuildDidYouKnowToast(dataSourcesPanel.transform, font);

            // Default to Tab 0
            SwitchNasaLearningTab(0);
        }

        private static void CreateButtonLabel(Transform parent, string label, Font font, int fontSize)
        {
            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(parent, false);
            var txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;
            txt.raycastTarget = false;
            var rt = txtGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        public void SwitchNasaLearningTab(int tabIndex)
        {
            currentNasaLearningTab = Mathf.Clamp(tabIndex, 0, 4);

            for (int i = 0; i < 5; i++)
            {
                if (nasaTabBtnImages[i] != null)
                {
                    bool isSelected = (i == currentNasaLearningTab);
                    nasaTabBtnImages[i].color = isSelected ? new Color(0.18f, 0.55f, 0.75f, 1f) : new Color(0.08f, 0.14f, 0.20f, 0.95f);
                }
                if (nasaTabBtnTexts[i] != null)
                {
                    bool isSelected = (i == currentNasaLearningTab);
                    nasaTabBtnTexts[i].color = isSelected ? Color.white : new Color(0.70f, 0.78f, 0.85f);
                }
            }

            UpdateNasaLearningContent();
        }

        private void UpdateNasaLearningContent()
        {
            if (nasaHubCategoryTitleText == null || nasaHubBodyText == null) return;

            switch (currentNasaLearningTab)
            {
                case 0:
                    nasaHubCategoryTitleText.text = "🦌 HOW AXIS WORKS: The Spotted Deer (Axis axis) & Ecosystem Balance";
                    if (nasaHubFactBadgeText != null) nasaHubFactBadgeText.text = "★ BIOLOGICAL AXIS • খাদ্যশৃঙ্খলের অক্ষ";
                    nasaHubBodyText.text =
                        "🌟 1. কেন চিত্রা হরিণের নাম 'Axis axis'? (Why Axis axis?)\n" +
                        "বিজ্ঞানীরা সুন্দরবনের চিত্রা হরিণের বৈজ্ঞানিক নাম দিয়েছেন 'Axis axis'। ল্যাটিন ও গণিতে 'অক্ষ' (Axis) বলতে বোঝায় যার ওপর কেন্দ্র করে সবকিছু আবর্তিত হয়। সুন্দরবনের পুরো বন্যপ্রাণী ও খাদ্যশৃঙ্খল এই চিত্রা হরিণকে কেন্দ্র করেই বেঁচে থাকে!\n\n" +
                        "🐒 2. বানর ও হরিণের অপূর্ব মিতালী (Monkey & Deer Teamwork):\n" +
                        "• বানররা উঁচু কেওড়া গাছে চড়ে ফল ও কচি পাতা খায়। বানর ফল ছিঁড়ে নিচে ফেলে, আর মাটিতে দাঁড়িয়ে হরিণ সেই সুস্বাদু খাবার খায়!\n" +
                        "• প্রতিদানে, মাটিতে বাঘ বা শিকারি এলে হরিণের প্রখর শ্রবণশক্তি ও ঘ্রাণশক্তি তা আগে টের পেয়ে তীব্র সতর্কবার্তা (Alert Call) দেয়, যাতে বানররা দ্রুত গাছে উঠে রক্ষা পায়।\n\n" +
                        "🐅 3. রয়েল বেঙ্গল টাইগারকে বাঁচায় এই অক্ষ (Saving Tigers & Villages):\n" +
                        "• বাঘের প্রধান খাদ্যই হলো চিত্রা হরিণ (বাঘের মোট খাদ্যের ৭০% এর বেশি)।\n" +
                        "• বনে হরিণ থাকলে বাঘ কখনো মানুষের গ্রামে আসে না। কিন্তু শিকারিরা যখন ফাঁদ পেতে হরিণ মারে, তখন ক্ষুধার্ত বাঘ লোকালয়ে চলে আসে এবং মানুষ-বাঘ সংঘাত বাড়ে। হরিণ বাঁচলেই মানুষ ও বাঘ দুটোই নিরাপদে থাকে!\n\n" +
                        "📊 4. সুন্দরবনের বর্তমান পরিসংখ্যান:\n" +
                        "সর্বশেষ সরকারি জরিপ অনুযায়ী সুন্দরবনে বর্তমানে প্রায় ১,৪০,৩৫৭টি চিত্রা হরিণ বাস করে।";
                    break;

                case 1:
                    nasaHubCategoryTitleText.text = "🧭 3D COORDINATE AXES & SATELLITE GPS: X, Y, Z অক্ষ কীভাবে কাজ করে?";
                    if (nasaHubFactBadgeText != null) nasaHubFactBadgeText.text = "★ MATHEMATICS & GPS • 3D স্থানাঙ্ক";
                    nasaHubBodyText.text =
                        "🎮 1. গেমের ভেতরের ৩টি অক্ষ (The 3 Axes in 3D Games):\n" +
                        "এই গেমে তোমার প্রাণী যখন হাঁটে, সাঁতার কাটে বা লাফ দেয়, কম্পিউটার ৩টি অক্ষের ওপর হিসাব করে:\n" +
                        "• X-Axis (পূর্ব ↔ পশ্চিম দ্রাঘিমাংশ / Longitude): নদী ও বনের ডানে-বামে চলাচলের অক্ষ (৮৯.৫০°E থেকে ৯১.০০°E পর্যন্ত বিস্তৃত)।\n" +
                        "• Z-Axis (দক্ষিণ ↔ উত্তর অক্ষাংশ / Latitude): বঙ্গোপসাগরের মোহনা (২১.৮০°N) থেকে শুরু করে পদ্মা-মেঘনার মোহনা (২৩.৮৫°N) পর্যন্ত এগিয়ে যাওয়ার অক্ষ!\n" +
                        "• Y-Axis (উচ্চতা ও গভীরতা / Height & Depth): স্থলভাগে ডাঙার উচ্চতা, জোয়ার-ভাটার পানির স্তর, এবং ডলফিনের নদীতে ডুব দেওয়ার গভীরতা মাপার অক্ষ!\n\n" +
                        "🛰️ 2. মহাকাশে নাসা স্যাটেলাইট অক্ষ (How NASA Uses Orbital Axes):\n" +
                        "• Terra ও Landsat স্যাটেলাইটগুলো পৃথিবীর মেরু অক্ষের (Polar Axis) ওপর দিয়ে ঘণ্টায় ২৭,০০০ কিমি বেগে পৃথিবীকে প্রদক্ষিণ করে (প্রতি ৯০ মিনিটে একবার)।\n" +
                        "• পৃথিবী নিজ অক্ষের ওপর পশ্চিম থেকে পূর্বে ঘুরতে থাকে, আর স্যাটেলাইট উপর থেকে সুন্দরবনের প্রতিটি গাছের ছবি স্ক্যান করে ডেটা তৈরি করে!\n\n" +
                        "🗺️ 3. মিনিম্যাপের স্যাটেলাইট রেডার:\n" +
                        "স্ক্রিনের উপরের-বামের মিনিম্যাপে আসল নাসার ল্যান্ডস্যাট ছবি দেখতে পাচ্ছ। তোমার প্রাণীর X ও Z অবস্থানকে বাস্তব অক্ষাংশ-দ্রাঘিমাংশে রূপান্তর করে এই রেডারটি চলে!";
                    break;

                case 2:
                    nasaHubCategoryTitleText.text = "🛰️ NASA SATELLITE EYES: মহাকাশ থেকে বন ও নদীর স্বাস্থ্য পরীক্ষা";
                    if (nasaHubFactBadgeText != null) nasaHubFactBadgeText.text = "★ NASA MODIS & SMAP • মহাকাশ ডেটা";
                    nasaHubBodyText.text =
                        "🌱 1. MODIS NDVI (গাছের সবুজ স্বাস্থ্য মাপার মিটার):\n" +
                        "• স্যাটেলাইট: Terra MODIS (MOD13Q1)\n" +
                        "• কীভাবে কাজ করে? সুস্থ পাতা সূর্যালোক থেকে লাল আলো শোষণ করে এবং নিকট-অবলোহিত (NIR) আলো মহাশূন্যে প্রতিফলিত করে।\n" +
                        "• NDVI = 0.82 (গাঢ় সবুজ): ঘন সতেজ সুন্দরী ও কেওড়া বন। পশু-পাখির পর্যাপ্ত খাদ্য ও ছায়া রয়েছে।\n" +
                        "• NDVI = 0.11 (লাল বিপদ সংকেত): গাছ কেটে ফেলা বা ধ্বংস হওয়া বনভূমি! এই অঞ্চলে ঢুকলে প্রাণীদের ডিহাইড্রেশন ও স্বাস্থ্যক্ষতি হয়।\n\n" +
                        "🌡️ 2. MODIS LST (মাটি ও নদীর থার্মোমিটার):\n" +
                        "• স্যাটেলাইট: Terra MODIS (MOD11A2)\n" +
                        "• মহাকাশ থেকে নদী ও মাটির তাপমাত্রা কেলভিনে মেপে সেলসিয়াসে রূপান্তর করা হয় (°C = K - 273.15)। নদী ২১°C-২৩°C থাকলে ইলিশ ও শুশুক স্বস্তিতে সাঁতার কাটে।\n\n" +
                        "🌊 3. SMAP Salinity (পানির লবণাক্ততার পরিমাপ):\n" +
                        "• PSU (Practical Salinity Unit): বৃষ্টির মিষ্টি পানি = 0 PSU, সাগরের লোনা পানি = ২৮-৩৫ PSU।\n" +
                        "• অতিরিক্ত লবণাক্ততা বাড়লে সুন্দরী গাছের আগা মরে যায় ('Top-Dying Disease')। তাই হরিণ ও বাঘের জন্য মিষ্টি পানির পুকুর অত্যন্ত জরুরি!";
                    break;

                case 3:
                    nasaHubCategoryTitleText.text = "🛡️ SAVING SUNDARBANS: কেন সুন্দরবন রক্ষা করা আমাদের বাঁচার লড়াই?";
                    if (nasaHubFactBadgeText != null) nasaHubFactBadgeText.text = "★ THE GREEN SHIELD • প্রাকৃতিক ঢাল";
                    nasaHubBodyText.text =
                        "🌪️ 1. সুপার সাইক্লোন থেকে বাঁচানোর প্রাকৃতিক দেয়াল:\n" +
                        "• সুন্দরবনের ম্যানগ্রোভ গাছের শ্বাসমূল ও ঠেস-মূল মাটিকে পাথরের মতো আঁকড়ে ধরে এবং সমুদ্রের জলোচ্ছ্বাসের ঢেউয়ের শক্তি ৬৬% পর্যন্ত কমিয়ে দেয়!\n" +
                        "• সিডর, আইলা, রেমাল-এর মতো ঘূর্ণিঝড়ের আঘাত নিজের বুকে টেনে নিয়ে সুন্দরবন উপকূলের ৪ কোটির বেশি মানুষকে রক্ষা করে।\n\n" +
                        "🌍 2. ব্লু কার্বন (Blue Carbon Super-Storage):\n" +
                        "• ম্যানগ্রোভ বন সাধারণ স্থলজ বনের চেয়ে ৩ থেকে ৫ গুণ বেশি কার্বন ধরে রাখতে পারে! বৈশ্বিক উষ্ণতা কমাতে সুন্দরবন এক অমূল্য সম্পদ।\n\n" +
                        "⚠️ 3. যে বিপদগুলো তুমি এই গেমে দূর করবে:\n" +
                        "• 🪤 শিকারির তারের ফাঁদ: হরিণ ও বাঘের পা আটকে ফেলে রক্তক্ষরণ ঘটায়।\n" +
                        "• 🕸️ নিষিদ্ধ কারেন্ট জাল: নদীতে পেতে রাখা সূক্ষ্ম প্লাস্টিক জাল, যাতে ডলফিন ও মা মাছ আটকা পড়ে মারা যায়।\n" +
                        "• 🧪 তেল ও বিষাক্ত রাসায়নিক: জাহাজের তেল চুইয়ে নদীর পানি নষ্ট করে মাছের ফুলকা বন্ধ করে দেয়।\n" +
                        "• 🪓 বন উজাড়: গাছ কেটে ফেলা মরুভূমি অঞ্চল।";
                    break;

                case 4:
                    nasaHubCategoryTitleText.text = "🎮 HOW TO PLAY & RESTORATION TOOLS: গেম কন্ট্রোল ও উদ্ধার সরঞ্জাম";
                    if (nasaHubFactBadgeText != null) nasaHubFactBadgeText.text = "★ GUARDIAN CONTROLS • খেলার কৌশল";
                    nasaHubBodyText.text =
                        "🕹️ 1. চলাচলের নিয়ম (Movement & Camera):\n" +
                        "• স্ক্রিনের বামের জয়স্টিক দিয়ে হাঁটো, অথবা কীবোর্ডে W, A, S, D চাপো।\n" +
                        "• স্ক্রিনের ডানে আঙুল টেনে তাকাও, অথবা মাউসের Right-Click ড্র্যাগ করে ক্যামেরা ঘোরাও।\n" +
                        "• লাফ দিতে Jump বাটন বা কীবোর্ডে Spacebar চাপো। দৌড়াতে Auto-Run বাটন চাপো।\n\n" +
                        "🧰 2. পরিবেশ উদ্ধারের ৪টি সুপার টুল (Hotkeys 1, 2, 3, 4):\n" +
                        "• 🌱 Tool 1 - বীজ (Seed): গাছপালাহীন মরুভূমিতে ম্যানগ্রোভ চারা রোপণ করো (+10 HP বোনাস!)।\n" +
                        "• 🔪 Tool 2 - জাল কাটার ছুরি (Net-Cutter): নদীতে অবৈধ কারেন্ট জাল কেটে ডলফিন ও মাছকে মুক্ত করো (+10 HP!)।\n" +
                        "• 🧪 Tool 3 - পরিষ্কারের তরল (Clean Liquid): তেলের দাগ ও বিষাক্ত পানিতে ছিটিয়ে পরিবেশ শোধন করো (+10 HP!)।\n" +
                        "• 🪤 Tool 4 - ফাঁদ ধ্বংসকারী (Trap Destroyer): শিকারির ধাতব তারের ফাঁদ ভেঙে বাঘ-হরিণকে বাঁচাও (+10 HP!)।\n\n" +
                        "💧 3. মিষ্টি পানির পুকুর (Sweetwater Oasis):\n" +
                        "ম্যাপে নীল গোল পুকুর খুঁজে সেখানে গেলে পানি পান করে +30 HP ও সম্পূর্ণ শক্তি ফিরে পাবে!\n\n" +
                        "⌨️ শর্টকাট: খেলার যে কোনো সময় কীবোর্ডে 'N' বা 'F1' চাপলে এই নাসা হাব খুলবে।";
                    break;
            }
        }

        private void BuildDidYouKnowToast(Transform parent, Font font)
        {
            didYouKnowToastGo = new GameObject("Trivia_Toast_Box", typeof(RectTransform), typeof(Image));
            didYouKnowToastGo.transform.SetParent(parent, false);
            var tRt = didYouKnowToastGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(0f, 40f);
            tRt.sizeDelta = new Vector2(740f, 130f);

            var tImg = didYouKnowToastGo.GetComponent<Image>();
            tImg.color = new Color(0.06f, 0.16f, 0.26f, 0.98f);
            var tOutline = didYouKnowToastGo.AddComponent<Outline>();
            tOutline.effectColor = new Color(0.95f, 0.75f, 0.25f, 0.95f);
            tOutline.effectDistance = new Vector2(2f, -2f);

            GameObject txtGo = new GameObject("Toast_Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(didYouKnowToastGo.transform, false);
            didYouKnowToastText = txtGo.GetComponent<Text>();
            didYouKnowToastText.font = font;
            didYouKnowToastText.fontSize = 13;
            didYouKnowToastText.fontStyle = FontStyle.Bold;
            didYouKnowToastText.alignment = TextAnchor.MiddleCenter;
            didYouKnowToastText.color = new Color(1f, 0.95f, 0.75f);
            didYouKnowToastText.lineSpacing = 1.3f;
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(16f, 8f);
            txtRt.offsetMax = new Vector2(-16f, -8f);

            didYouKnowToastGo.SetActive(false);
        }

        private static readonly string[] didYouKnowFacts = {
            "💡 মজার তথ্য ১: চিত্রা হরিণ (Axis axis) ঘণ্টায় ৬৫ কিমি বেগে দৌড়াতে পারে এবং এক লাফে ২-৩ মিটার উঁচু বাধা পার হতে পারে!",
            "💡 মজার তথ্য ২: সুন্দরী গাছের শ্বাসমূল (Pneumatophores) মাটির নিচ থেকে কাঠির মতো খাড়া উঠে আসে যাতে কাদায় ডুবে থাকা গাছ বাতাস থেকে অক্সিজেন নিতে পারে!",
            "💡 মজার তথ্য ৩: গাঙ্গেয় শুশুক (Ganges Dolphin) প্রায় অন্ধ। এরা চোখ দিয়ে না দেখে নদীর ঘোলা পানিতে আল্ট骖নিক শব্দতরঙ্গ (Sonar) ছুঁড়ে শিকারের অবস্থান চেনে!",
            "💡 মজার তথ্য ৪: একটি পূর্ণবয়স্ক মা ইলিশ একবারে প্রায় ২০ লক্ষ ডিম ছাড়ে! মা ইলিশ সংরক্ষণ অভিযানে ৫২% মা ইলিশ নিরাপদে ডিম দিতে পেরেছিল।",
            "💡 মজার তথ্য ৫: রয়্যাল বেঙ্গল টাইগার চমৎকার সাঁতারু! এরা সুন্দরবনের এক নদী থেকে আরেক নদীতে সাঁতরে কয়েক কিলোমিটার পথ অনায়াসে পার হতে পারে।",
            "💡 মজার তথ্য ৬: সুন্দরবনের কাদামাটি সাধারণ বনের চেয়ে ৪ গুণ বেশি কার্বন ডাই অক্সাইড ধরে রেখে জলবায়ু পরিবর্তন রোধ করে!"
        };

        private int triviaIndex = 0;

        public void ShowRandomDidYouKnowFact()
        {
            if (didYouKnowToastGo == null || didYouKnowToastText == null) return;
            didYouKnowToastText.text = didYouKnowFacts[triviaIndex % didYouKnowFacts.Length];
            triviaIndex++;

            didYouKnowToastGo.transform.SetAsLastSibling();
            didYouKnowToastGo.SetActive(true);

            if (didYouKnowCoroutine != null) StopCoroutine(didYouKnowCoroutine);
            didYouKnowCoroutine = StartCoroutine(HideTriviaToastRoutine());
        }

        private IEnumerator HideTriviaToastRoutine()
        {
            yield return new WaitForSeconds(5.5f);
            if (didYouKnowToastGo != null) didYouKnowToastGo.SetActive(false);
        }

        // ── Authentic NASA Geographic Coordinates Telemetry Helpers ──
        public static (float lat, float lon) WorldToNasaCoordinates(float worldX, float worldZ)
        {
            Vector2 uv = WorldToLandsatUV(worldX, worldZ);
            float normV = Mathf.InverseLerp(0.40f, 0.80f, uv.y);
            float normU = Mathf.InverseLerp(0.44f, 0.72f, uv.x);
            float lat = Mathf.Lerp(NasaBoundsSouth, NasaBoundsNorth, normV);
            float lon = Mathf.Lerp(NasaBoundsWest,  NasaBoundsEast,  normU);
            return (lat, lon);
        }

        public static string FormatNasaCoordinatesDecimal(float lat, float lon)
        {
            return $"{lat:F4}°N, {lon:F4}°E";
        }

        public static string FormatNasaCoordinatesDMS(float lat, float lon)
        {
            int latDeg = Mathf.FloorToInt(lat);
            float latMinTotal = (lat - latDeg) * 60f;
            int latMin = Mathf.FloorToInt(latMinTotal);
            float latSec = (latMinTotal - latMin) * 60f;

            int lonDeg = Mathf.FloorToInt(lon);
            float lonMinTotal = (lon - lonDeg) * 60f;
            int lonMin = Mathf.FloorToInt(lonMinTotal);
            float lonSec = (lonMinTotal - lonMin) * 60f;

            return $"{latDeg}°{latMin:00}'{latSec:00.0}\"N, {lonDeg}°{lonMin:00}'{lonSec:00.0}\"E";
        }

        [System.Serializable]
        public class DetectedThreatData
        {
            public string id;
            public string title;
            public string category;
            public string riskTier; // "LETHAL", "HIGH", "MODERATE", "CLEARED", "SAFE HAVEN"
            public Color color;
            public Vector3 worldPos;
            public float radius;
            public float damage;
            public float damageInterval;
            public string nasaSensor;
            public string description;
            public string defenseTip;
            public bool isNeutralized;
            public RectTransform pinRt;
            public RectTransform circleRt;
            public Text cardDistText;
        }

        // ── Full Tactical Satellite Map & NASA Threat Telemetry Modal ──
        public void OpenTacticalMapPopup()
        {
            if (tacticalMapModal != null)
            {
                tacticalMapModal.SetActive(true);
                if (tacticalOverlay != null)
                {
                    PopulateTacticalOverlay(tacticalOverlay, cachedDefaultFont);
                }
                UpdateTacticalMapPopup();
            }
        }

        public void CloseTacticalMapPopup()
        {
            if (tacticalMapModal != null)
            {
                tacticalMapModal.SetActive(false);
            }
        }

        public void ToggleTacticalMapPopup()
        {
            if (tacticalMapModal != null)
            {
                if (tacticalMapModal.activeSelf) CloseTacticalMapPopup();
                else OpenTacticalMapPopup();
            }
        }

        public void ToggleTacticalIntelPanel()
        {
            isTacticalIntelPanelVisible = !isTacticalIntelPanelVisible;
            if (tacticalIntelPanel != null)
            {
                tacticalIntelPanel.SetActive(isTacticalIntelPanelVisible);
            }
            if (tacticalMapViewport != null)
            {
                tacticalMapViewport.offsetMax = new Vector2(isTacticalIntelPanelVisible ? -370f : -12f, -50f);
            }
        }

        private void BuildTacticalMapPopup(Transform parent, Font font)
        {
            // Dimmed Screen Backdrop covering whole screen
            GameObject modalGo = new GameObject("Tactical_Map_Modal", typeof(RectTransform), typeof(Image), typeof(Button));
            modalGo.transform.SetParent(parent, false);
            tacticalMapModal = modalGo;
            var mRt = modalGo.GetComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.sizeDelta = Vector2.zero;

            var mImg = modalGo.GetComponent<Image>();
            mImg.color = new Color(0.01f, 0.03f, 0.06f, 0.88f); // dark glass dim
            var mBtn = modalGo.GetComponent<Button>();
            mBtn.targetGraphic = mImg;
            mBtn.onClick.AddListener(CloseTacticalMapPopup);

            // 92% Screen Container
            GameObject boxGo = new GameObject("Tactical_Map_Container", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(modalGo.transform, false);
            var bRt = boxGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.03f, 0.03f);
            bRt.anchorMax = new Vector2(0.97f, 0.97f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = Vector2.zero;

            var bImg = boxGo.GetComponent<Image>();
            bImg.color = new Color(0.03f, 0.06f, 0.09f, 0.98f);
            var bOutline = boxGo.AddComponent<Outline>();
            bOutline.effectColor = new Color(0.35f, 0.85f, 0.95f, 0.85f);
            bOutline.effectDistance = new Vector2(2f, -2f);

            // ── TOP HEADER BAR (48px) ──
            GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(boxGo.transform, false);
            var hRt = headerGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0f, 48f);
            headerGo.GetComponent<Image>().color = new Color(0.04f, 0.10f, 0.16f, 0.98f);

            // Header Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(headerGo.transform, false);
            var tText = titleGo.GetComponent<Text>();
            tText.font = font;
            tText.fontSize = 13;
            tText.fontStyle = FontStyle.Bold;
            tText.color = new Color(0.35f, 0.95f, 0.75f);
            tText.text = "🛰️ NASA EARTH OBSERVATORY • SUNDARBANS TACTICAL RADAR";
            tText.alignment = TextAnchor.MiddleLeft;
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(0.48f, 1f);
            tRt.offsetMin = new Vector2(16f, 0f);
            tRt.offsetMax = Vector2.zero;

            // Live Telemetry Sensor Fix Badge in Header
            GameObject fixGo = new GameObject("Live_Sensor_Fix_Badge", typeof(RectTransform), typeof(Text));
            fixGo.transform.SetParent(headerGo.transform, false);
            tacticalPlayerCoordsText = fixGo.GetComponent<Text>();
            tacticalPlayerCoordsText.font = font;
            tacticalPlayerCoordsText.fontSize = 10;
            tacticalPlayerCoordsText.fontStyle = FontStyle.Bold;
            tacticalPlayerCoordsText.color = new Color(0.95f, 0.85f, 0.35f);
            tacticalPlayerCoordsText.alignment = TextAnchor.MiddleCenter;
            tacticalPlayerCoordsText.text = "📍 LIVE FIX: LAT 21°51'24.8\"N | LON 89°34'15.2\"E [LANDSAT-9 / MODIS TERRA]";
            var fixRt = fixGo.GetComponent<RectTransform>();
            fixRt.anchorMin = new Vector2(0.48f, 0f);
            fixRt.anchorMax = new Vector2(0.80f, 1f);
            fixRt.offsetMin = Vector2.zero;
            fixRt.offsetMax = Vector2.zero;

            // Toggle Intel Drawer Button
            GameObject toggleGo = new GameObject("Btn_Toggle_Intel", typeof(RectTransform), typeof(Image), typeof(Button));
            toggleGo.transform.SetParent(headerGo.transform, false);
            var togRt = toggleGo.GetComponent<RectTransform>();
            togRt.anchorMin = new Vector2(0.83f, 0.5f);
            togRt.anchorMax = new Vector2(0.83f, 0.5f);
            togRt.pivot = new Vector2(0.5f, 0.5f);
            togRt.anchoredPosition = Vector2.zero;
            togRt.sizeDelta = new Vector2(100f, 30f);
            var togImg = toggleGo.GetComponent<Image>();
            togImg.color = new Color(0.12f, 0.35f, 0.50f, 0.95f);
            var togOut = toggleGo.AddComponent<Outline>();
            togOut.effectColor = new Color(0.35f, 0.85f, 0.95f, 0.8f);

            GameObject togTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            togTxtGo.transform.SetParent(toggleGo.transform, false);
            var togTxt = togTxtGo.GetComponent<Text>();
            togTxt.font = font;
            togTxt.fontSize = 11;
            togTxt.fontStyle = FontStyle.Bold;
            togTxt.alignment = TextAnchor.MiddleCenter;
            togTxt.color = Color.white;
            togTxt.text = "⊞ INTEL FEED";
            togTxt.raycastTarget = false;
            var togTxtRt = togTxtGo.GetComponent<RectTransform>();
            togTxtRt.anchorMin = Vector2.zero;
            togTxtRt.anchorMax = Vector2.one;
            togTxtRt.sizeDelta = Vector2.zero;

            var togBtn = toggleGo.GetComponent<Button>();
            togBtn.onClick.AddListener(ToggleTacticalIntelPanel);

            // Close Button
            GameObject closeGo = new GameObject("Btn_Close_Tactical", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(headerGo.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 0.5f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.anchoredPosition = new Vector2(-12f, 0f);
            cRt.sizeDelta = new Vector2(95f, 30f);

            var cImg = closeGo.GetComponent<Image>();
            cImg.color = new Color(0.85f, 0.20f, 0.20f, 0.95f);
            var cOutline = closeGo.AddComponent<Outline>();
            cOutline.effectColor = new Color(1f, 0.6f, 0.6f, 0.8f);

            GameObject cTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            cTxtGo.transform.SetParent(closeGo.transform, false);
            var cTxt = cTxtGo.GetComponent<Text>();
            cTxt.font = font;
            cTxt.fontSize = 11;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.text = "✕ CLOSE [M]";
            cTxt.raycastTarget = false;
            var cTxtRt = cTxtGo.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.sizeDelta = Vector2.zero;

            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(CloseTacticalMapPopup);

            // ── MAIN MAP VIEWPORT (LEFT / CENTER) ──
            GameObject mapViewportGo = new GameObject("Map_Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            mapViewportGo.transform.SetParent(boxGo.transform, false);
            tacticalMapViewport = mapViewportGo.GetComponent<RectTransform>();
            tacticalMapViewport.anchorMin = new Vector2(0f, 0f);
            tacticalMapViewport.anchorMax = new Vector2(1f, 1f);
            tacticalMapViewport.offsetMin = new Vector2(12f, 40f);
            tacticalMapViewport.offsetMax = new Vector2(-370f, -50f);

            var mvImg = mapViewportGo.GetComponent<Image>();
            mvImg.color = Color.black;
            var mvMask = mapViewportGo.GetComponent<Mask>();
            mvMask.showMaskGraphic = false;

            // Full Satellite RawImage
            GameObject satGo = new GameObject("Full_Satellite_Image", typeof(RectTransform), typeof(RawImage));
            satGo.transform.SetParent(mapViewportGo.transform, false);
            tacticalMapContent = satGo.GetComponent<RectTransform>();
            tacticalMapContent.anchorMin = Vector2.zero;
            tacticalMapContent.anchorMax = Vector2.one;
            tacticalMapContent.sizeDelta = Vector2.zero;
            var sRaw = satGo.GetComponent<RawImage>();
            if (satelliteMapTexture != null) sRaw.texture = satelliteMapTexture;
            sRaw.uvRect = new Rect(TacMapUvMinX, TacMapUvMinY, TacMapUvSpanX, TacMapUvSpanY);

            // NASA Boundary Lat/Lon Labels along border of Map Viewport
            CreateTacticalMapBorderLabels(mapViewportGo.transform, font);

            // Overlay Layer (Danger circles and tactical pins)
            GameObject overlayGo = new GameObject("Tactical_Overlay", typeof(RectTransform));
            overlayGo.transform.SetParent(mapViewportGo.transform, false);
            tacticalOverlay = overlayGo.GetComponent<RectTransform>();
            tacticalOverlay.anchorMin = Vector2.zero;
            tacticalOverlay.anchorMax = Vector2.one;
            tacticalOverlay.sizeDelta = Vector2.zero;

            // Player Directional Triangle Marker on Tactical Map
            GameObject pTacGo = new GameObject("Tactical_Player_Marker", typeof(RectTransform), typeof(Image));
            pTacGo.transform.SetParent(mapViewportGo.transform, false);
            tacticalPlayerMarker = pTacGo.GetComponent<RectTransform>();
            tacticalPlayerMarker.sizeDelta = new Vector2(34f, 34f);
            tacticalPlayerMarker.pivot = new Vector2(0.5f, 0.5f);
            var pTacImg = pTacGo.GetComponent<Image>();
            Sprite triSp = Resources.Load<Sprite>("Textures/UI/icon_triangle_marker");
#if UNITY_EDITOR
            if (triSp == null) triSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_triangle_marker.png");
#endif
            if (triSp != null)
            {
                pTacImg.sprite = triSp;
                pTacImg.preserveAspect = true;
                pTacImg.color = Color.white;
            }
            else
            {
                pTacImg.color = new Color(0.2f, 0.95f, 0.5f);
            }

            // Outer Pulse Wave on Player Marker
            GameObject pWaveGo = new GameObject("Pulse_Wave", typeof(RectTransform), typeof(Image));
            pWaveGo.transform.SetParent(pTacGo.transform, false);
            var pwRt = pWaveGo.GetComponent<RectTransform>();
            pwRt.anchorMin = new Vector2(0.5f, 0.5f);
            pwRt.anchorMax = new Vector2(0.5f, 0.5f);
            pwRt.sizeDelta = new Vector2(32f, 32f);
            var pwImg = pWaveGo.GetComponent<Image>();
            pwImg.sprite = GetOrCreateCircleSprite(128, true, 0.12f);
            pwImg.color = new Color(0.25f, 0.95f, 0.65f, 0.6f);

            // ── RIGHT SIDE: NASA HAZARD & THREAT INTELLIGENCE DRAWER (355px) ──
            BuildTacticalIntelPanel(boxGo.transform, font);

            // ── BOTTOM MISSION STATUS BAR (36px) ──
            GameObject bottomBarGo = new GameObject("Bottom_Bar", typeof(RectTransform), typeof(Image));
            bottomBarGo.transform.SetParent(boxGo.transform, false);
            var bbRt = bottomBarGo.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = Vector2.zero;
            bbRt.sizeDelta = new Vector2(0f, 36f);
            bottomBarGo.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.98f);

            GameObject infoTxtGo = new GameObject("Info_Text", typeof(RectTransform), typeof(Text));
            infoTxtGo.transform.SetParent(bottomBarGo.transform, false);
            tacticalGoalDistText = infoTxtGo.GetComponent<Text>();
            tacticalGoalDistText.font = font;
            tacticalGoalDistText.fontSize = 11;
            tacticalGoalDistText.fontStyle = FontStyle.Bold;
            tacticalGoalDistText.color = new Color(0.95f, 0.85f, 0.35f);
            tacticalGoalDistText.text = "🎯 SANCTUARY EXTRACTION GOAL: [75, 270] (22.4553°N, 89.7015°E) | DISTANCE: Calculating... | NASA LANDSAT-9 OLI";
            tacticalGoalDistText.alignment = TextAnchor.MiddleLeft;
            var itRt = infoTxtGo.GetComponent<RectTransform>();
            itRt.anchorMin = Vector2.zero;
            itRt.anchorMax = Vector2.one;
            itRt.offsetMin = new Vector2(16f, 0f);
            itRt.offsetMax = new Vector2(-16f, 0f);

            modalGo.SetActive(false); // Hidden until tapped
        }

        private void CreateTacticalMapBorderLabels(Transform parent, Font font)
        {
            // North
            CreateBorderTick(parent, font, new Vector2(0.5f, 1f), new Vector2(0f, -12f), "▲ 22.4800°N • NORTH CHANDPAI MANGROVES");
            // South
            CreateBorderTick(parent, font, new Vector2(0.5f, 0f), new Vector2(0f, 12f), "▼ 21.7500°N • BAY OF BENGAL / KOTKA SANCTUARY");
            // West
            CreateBorderTick(parent, font, new Vector2(0f, 0.5f), new Vector2(65f, 0f), "◄ 89.2000°E\nPASSUR ESTUARY");
            // East
            CreateBorderTick(parent, font, new Vector2(1f, 0.5f), new Vector2(-65f, 0f), "89.8800°E ►\nBALESHWAR RIVER");
        }

        private void CreateBorderTick(Transform parent, Font font, Vector2 anchor, Vector2 offset, string text)
        {
            GameObject go = new GameObject($"Border_Tick_{text.Substring(0, Mathf.Min(8, text.Length))}", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(240f, 26f);

            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = 9;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.45f, 0.85f, 0.95f, 0.80f);
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
            var outl = go.AddComponent<Outline>();
            outl.effectColor = Color.black;
            outl.effectDistance = new Vector2(1f, -1f);
        }

        private void BuildTacticalIntelPanel(Transform parent, Font font)
        {
            GameObject panelGo = new GameObject("Tactical_Intel_Drawer", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            tacticalIntelPanel = panelGo;
            var pRt = panelGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(1f, 0f);
            pRt.anchorMax = new Vector2(1f, 1f);
            pRt.pivot = new Vector2(1f, 0.5f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.offsetMin = new Vector2(-360f, 40f);
            pRt.offsetMax = new Vector2(-12f, -50f);

            var pImg = panelGo.GetComponent<Image>();
            pImg.color = new Color(0.04f, 0.08f, 0.12f, 0.96f);
            var pOut = panelGo.AddComponent<Outline>();
            pOut.effectColor = new Color(0.25f, 0.75f, 0.85f, 0.7f);

            // Drawer Header
            GameObject phGo = new GameObject("Drawer_Header", typeof(RectTransform), typeof(Image));
            phGo.transform.SetParent(panelGo.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = new Vector2(0f, 1f);
            phRt.anchorMax = new Vector2(1f, 1f);
            phRt.pivot = new Vector2(0.5f, 1f);
            phRt.anchoredPosition = Vector2.zero;
            phRt.sizeDelta = new Vector2(0f, 46f);
            phGo.GetComponent<Image>().color = new Color(0.06f, 0.14f, 0.20f, 0.98f);

            GameObject phTxtGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            phTxtGo.transform.SetParent(phGo.transform, false);
            var phTxt = phTxtGo.GetComponent<Text>();
            phTxt.font = font;
            phTxt.fontSize = 11;
            phTxt.fontStyle = FontStyle.Bold;
            phTxt.color = new Color(1.0f, 0.35f, 0.35f);
            phTxt.alignment = TextAnchor.MiddleLeft;
            phTxt.text = "🔴 NASA THREAT & HAZARD TELEMETRY";
            var phtRt = phTxtGo.GetComponent<RectTransform>();
            phtRt.anchorMin = Vector2.zero;
            phtRt.anchorMax = Vector2.one;
            phtRt.offsetMin = new Vector2(12f, 18f);
            phtRt.offsetMax = new Vector2(-12f, 0f);

            GameObject cntTxtGo = new GameObject("ThreatCount", typeof(RectTransform), typeof(Text));
            cntTxtGo.transform.SetParent(phGo.transform, false);
            tacticalThreatCountText = cntTxtGo.GetComponent<Text>();
            tacticalThreatCountText.font = font;
            tacticalThreatCountText.fontSize = 9;
            tacticalThreatCountText.fontStyle = FontStyle.Normal;
            tacticalThreatCountText.color = new Color(0.75f, 0.88f, 0.95f, 0.9f);
            tacticalThreatCountText.alignment = TextAnchor.MiddleLeft;
            tacticalThreatCountText.text = "Scanning NASA Landsat, MODIS & VIIRS telemetry...";
            var cntRt = cntTxtGo.GetComponent<RectTransform>();
            cntRt.anchorMin = Vector2.zero;
            cntRt.anchorMax = Vector2.one;
            cntRt.offsetMin = new Vector2(12f, 0f);
            cntRt.offsetMax = new Vector2(-12f, -22f);

            // Scrollable Threats List
            GameObject scrollGo = new GameObject("Threats_Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(8f, 8f);
            sRt.offsetMax = new Vector2(-8f, -50f);

            scrollGo.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.06f, 0.6f);
            var sMask = scrollGo.GetComponent<Mask>();
            sMask.showMaskGraphic = false;

            var sRect = scrollGo.GetComponent<ScrollRect>();
            sRect.horizontal = false;
            sRect.vertical = true;
            sRect.scrollSensitivity = 25f;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            tacticalThreatCardsContainer = contentGo.transform;
            var cRt = contentGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 6, 6);
            vlg.spacing = 6f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sRect.content = cRt;
        }

        private void PopulateTacticalOverlay(RectTransform overlay, Font font)
        {
            if (overlay == null) return;
            for (int i = overlay.childCount - 1; i >= 0; i--)
            {
                Destroy(overlay.GetChild(i).gameObject);
            }

            if (tacticalThreatCardsContainer != null)
            {
                for (int i = tacticalThreatCardsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(tacticalThreatCardsContainer.GetChild(i).gameObject);
                }
            }

            activeThreats.Clear();
            var detected = ScanAllSceneThreats();
            activeThreats.AddRange(detected);

            if (tacticalThreatCountText != null)
            {
                tacticalThreatCountText.text = $"⚠️ {activeThreats.Count} Active Hazards Detected by NASA EOSDIS";
            }

            Vector3 playerPos = trackedPlayer != null ? trackedPlayer.position : Vector3.zero;

            foreach (var t in activeThreats)
            {
                (float lat, float lon) = WorldToNasaCoordinates(t.worldPos.x, t.worldPos.z);
                string decCoord = FormatNasaCoordinatesDecimal(lat, lon);
                string dmsCoord = FormatNasaCoordinatesDMS(lat, lon);

                // 1. Tactical Danger Circle
                if (t.riskTier != "SAFE HAVEN")
                {
                    t.circleRt = CreateTacticalDangerCircle(overlay, font, t.worldPos.x, t.worldPos.z, t.radius, t.title, t.color, decCoord);
                }

                // 2. Tactical Danger Pin
                t.pinRt = CreateTacticalPin(overlay, font, t.worldPos.x, t.worldPos.z, t.title, t.color, decCoord, t.riskTier);

                // 3. Threat Card in Telemetry Drawer
                if (tacticalThreatCardsContainer != null)
                {
                    CreateThreatDrawerCard(tacticalThreatCardsContainer, font, t, decCoord, dmsCoord, playerPos);
                }
            }
        }

        private System.Collections.Generic.List<DetectedThreatData> ScanAllSceneThreats()
        {
            var list = new System.Collections.Generic.List<DetectedThreatData>();

            // 1. 3D Poacher Gunmen Threats
            var gunmen = FindObjectsByType<GunmanThreat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int gIdx = 1;
            foreach (var gm in gunmen)
            {
                if (gm == null) continue;
                list.Add(new DetectedThreatData
                {
                    id = $"gunman_{gIdx}",
                    title = $"🎯 Poacher Rifle Ambush #{gIdx}",
                    category = "Poacher Rifle Ambush",
                    riskTier = "LETHAL",
                    color = new Color(0.95f, 0.15f, 0.15f),
                    worldPos = gm.transform.position,
                    radius = gm.detectionRadius > 0 ? gm.detectionRadius : 24f,
                    damage = gm.shotDamage,
                    damageInterval = gm.shotCooldown,
                    nasaSensor = "NASA VIIRS Thermal & Fire Radiative Power (FIRMS Poacher Patrol)",
                    description = "High-caliber scoped rifle with laser sight targeting Deer and Tigers.",
                    defenseTip = "Flee outside 24m within 5s or sneak & disarm with Tiger Takedown."
                });
                gIdx++;
            }

            // 2. Illegal River Nets & Accordion Box Traps
            var nets = FindObjectsByType<IllegalNetObstacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int nIdx = 1;
            foreach (var net in nets)
            {
                if (net == null) continue;
                string netName = net.netType == RiverNetType.MonofilamentGillnet ? "Banned Gillnet (কারেন্ট জাল)" : "China Box Trap (চাই/বেহুন্দি)";
                list.Add(new DetectedThreatData
                {
                    id = $"net_{nIdx}",
                    title = $"🕸️ {netName} #{nIdx}",
                    category = "Illegal River Net",
                    riskTier = net.isCleared ? "CLEARED" : "HIGH RISK",
                    color = net.isCleared ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.95f, 0.55f, 0.10f),
                    worldPos = net.transform.position,
                    radius = net.interactionRadius > 0 ? net.interactionRadius : 10f,
                    damage = net.hitDamage,
                    damageInterval = 0f,
                    nasaSensor = "NASA Landsat-9 SWIR & Sentinel-1 SAR River Obstruction",
                    description = "Banned monofilament netting strung across river migration channel.",
                    defenseTip = "Slice with Net Cutter Knife [2] or breach over with dolphin jump.",
                    isNeutralized = net.isCleared
                });
                nIdx++;
            }

            // 3. Saltwater Crocodiles
            var crocs = FindObjectsByType<CrocodileThreat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int cIdx = 1;
            foreach (var croc in crocs)
            {
                if (croc == null) continue;
                list.Add(new DetectedThreatData
                {
                    id = $"croc_{cIdx}",
                    title = $"🐊 Saltwater Crocodile Crossing #{cIdx}",
                    category = "Apex Estuarine Predator",
                    riskTier = "HIGH RISK",
                    color = new Color(0.92f, 0.38f, 0.12f),
                    worldPos = croc.transform.position,
                    radius = croc.attackRadius > 0 ? croc.attackRadius : 10f,
                    damage = croc.biteDamage,
                    damageInterval = 0f,
                    nasaSensor = "NASA MODIS MOD13Q1 Estuarine Mangrove Habitat Monitoring",
                    description = "Apex estuarine predator lurking along tidal mudbanks and channels.",
                    defenseTip = "Stay on high muddy riverbanks or swim rapidly outside patrol path."
                });
                cIdx++;
            }

            // 4. Motorized Trawlers
            var boats = FindObjectsByType<BoatThreat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int bIdx = 1;
            foreach (var boat in boats)
            {
                if (boat == null) continue;
                list.Add(new DetectedThreatData
                {
                    id = $"boat_{bIdx}",
                    title = $"🚢 Motorized Trawler Channel #{bIdx}",
                    category = "Acoustic & Propeller Hazard",
                    riskTier = "LETHAL",
                    color = new Color(0.95f, 0.25f, 0.35f),
                    worldPos = boat.transform.position,
                    radius = 15f,
                    damage = boat.directHitDamage,
                    damageInterval = 1f,
                    nasaSensor = "NASA VIIRS Boat Detection (VBD) Telemetry",
                    description = "High-speed trawler causing propeller strikes and acoustic disruption.",
                    defenseTip = "Dive deep under hull passage or swim outside central navigation lane."
                });
                bIdx++;
            }

            // 5. Poacher Wire Snares
            var traps = FindObjectsByType<TrapObstacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int tIdx = 1;
            foreach (var trap in traps)
            {
                if (trap == null) continue;
                list.Add(new DetectedThreatData
                {
                    id = $"trap_{tIdx}",
                    title = $"🪤 Poacher Wire Snare #{tIdx}",
                    category = "Poacher Wire Snare",
                    riskTier = trap.isCleared ? "CLEARED" : "HIGH RISK",
                    color = trap.isCleared ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.95f, 0.70f, 0.15f),
                    worldPos = trap.transform.position,
                    radius = trap.interactionRadius > 0 ? trap.interactionRadius : 5f,
                    damage = trap.trapDamage,
                    damageInterval = 0f,
                    nasaSensor = "NASA Landsat-9 Ground Disturbance Pattern",
                    description = "Concealed steel wire noose snare set on forest game trail.",
                    defenseTip = "Dismantle and neutralize with Trap Destroyer Tool [4].",
                    isNeutralized = trap.isCleared
                });
                tIdx++;
            }

            // 6. Deforested Canopy Zones & Salinity Triggers
            var worldZones = FindObjectsByType<WorldDangerZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int wzIdx = 1;
            foreach (var wz in worldZones)
            {
                if (wz == null) continue;
                list.Add(new DetectedThreatData
                {
                    id = $"world_zone_{wzIdx}",
                    title = $"🔥 {wz.zoneName} #{wzIdx}",
                    category = "Canopy Loss Hazard",
                    riskTier = wz.isFinished ? "RESTORED" : wz.riskTier.ToUpperInvariant(),
                    color = wz.isFinished ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.85f, 0.20f, 0.40f),
                    worldPos = wz.transform.position,
                    radius = wz.interactionRadius > 0 ? wz.interactionRadius : 18f,
                    damage = wz.damagePerSecond,
                    damageInterval = 1f,
                    nasaSensor = "NASA Landsat-8/9 OLI Surface Reflectance NDVI Index",
                    description = "Severe canopy loss exposing wildlife to critical heat stress.",
                    defenseTip = "Restore canopy by planting Keora Mangrove Seeds [1].",
                    isNeutralized = wz.isFinished
                });
                wzIdx++;
            }

            var trigZones = FindObjectsByType<HazardZoneTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int tzIdx = 1;
            foreach (var tz in trigZones)
            {
                if (tz == null) continue;
                string cat = tz.rawDataUnit == "PSU" ? "Hypersaline Mudflat" : "Habitat Loss Zone";
                list.Add(new DetectedThreatData
                {
                    id = $"trig_zone_{tzIdx}",
                    title = $"☠️ {cat} #{tzIdx}",
                    category = cat,
                    riskTier = tz.isCleared ? "RESTORED" : tz.riskTier.ToUpperInvariant(),
                    color = tz.isCleared ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.70f, 0.25f, 0.85f),
                    worldPos = tz.transform.position,
                    radius = 16f,
                    damage = tz.highDamagePerSec,
                    damageInterval = 1f,
                    nasaSensor = "NASA PO.DAAC SMAP Salinity & Landsat NDVI",
                    description = "Hypersaline soil degradation causing mangrove tree dieback.",
                    defenseTip = "Neutralize salinity with Water Cleanser [3].",
                    isNeutralized = tz.isCleared
                });
                tzIdx++;
            }

            // 7. LevelData fallback zones
            var ld = (gameManager != null) ? gameManager.CurrentLevelData : null;
            if (ld != null && ld.hazardZones != null)
            {
                int lzIdx = 1;
                foreach (var hz in ld.hazardZones)
                {
                    Vector3 pos = new Vector3(hz.centerX, 0f, hz.centerZ);
                    bool alreadyExists = false;
                    foreach (var item in list)
                    {
                        if (Vector3.Distance(item.worldPos, pos) < 8f) { alreadyExists = true; break; }
                    }
                    if (!alreadyExists)
                    {
                        list.Add(new DetectedThreatData
                        {
                            id = $"level_hz_{lzIdx}",
                            title = $"⚠️ {hz.zoneId.Replace("_", " ").ToUpperInvariant()}",
                            category = "Environmental Stress",
                            riskTier = hz.riskTier.ToUpperInvariant(),
                            color = hz.riskTier == "high" ? new Color(0.95f, 0.20f, 0.20f) : new Color(0.95f, 0.65f, 0.15f),
                            worldPos = pos,
                            radius = hz.radius > 0 ? hz.radius : 15f,
                            damage = 8f,
                            damageInterval = 1f,
                            nasaSensor = "NASA Landsat-9 OLI Multispectral Telemetry",
                            description = "Environmental stress zone detected by satellite telemetry.",
                            defenseTip = "Navigate with caution or apply corresponding eco-tool."
                        });
                    }
                    lzIdx++;
                }
            }

            // 8. Sanctuary Extraction Safe Haven Goal
            Vector3 goalPos = new Vector3(75f, 0f, 270f);
            if (ld != null && ld.goalZone != null)
            {
                goalPos = new Vector3(ld.goalZone.centerX, 0f, ld.goalZone.centerZ);
            }
            list.Add(new DetectedThreatData
            {
                id = "sanctuary_goal",
                title = "⭐ Kotka Wildlife Sanctuary Haven",
                category = "Protected Extraction Haven",
                riskTier = "SAFE HAVEN",
                color = new Color(0.20f, 0.95f, 0.45f),
                worldPos = goalPos,
                radius = (ld != null && ld.goalZone != null && ld.goalZone.radius > 0) ? ld.goalZone.radius : 20f,
                damage = 0f,
                damageInterval = 0f,
                nasaSensor = "NASA Earth Observatory Kotka Wildlife Sanctuary Core Grid",
                description = "Primary mission objective. Safe conservation sanctuary refuge.",
                defenseTip = "Reach extraction coordinate with vital health intact to win!"
            });

            return list;
        }

        private RectTransform CreateTacticalPin(RectTransform overlay, Font font, float worldX, float worldZ, string label, Color color, string nasaCoord, string riskTier)
        {
            Vector2 tacNorm = WorldToTacticalMapNormalized(worldX, worldZ);
            float normU = tacNorm.x;
            float normV = tacNorm.y;

            GameObject pinGo = new GameObject($"Pin_{label}", typeof(RectTransform), typeof(Image));
            pinGo.transform.SetParent(overlay, false);
            var pRt = pinGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(normU, normV);
            pRt.anchorMax = new Vector2(normU, normV);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.sizeDelta = new Vector2(175f, 32f);

            var pImg = pinGo.GetComponent<Image>();
            pImg.color = new Color(0.04f, 0.08f, 0.12f, 0.92f);
            var pOutline = pinGo.AddComponent<Outline>();
            pOutline.effectColor = new Color(color.r, color.g, color.b, 0.95f);
            pOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title line
            GameObject t1Go = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            t1Go.transform.SetParent(pinGo.transform, false);
            var t1 = t1Go.GetComponent<Text>();
            t1.font = font;
            t1.fontSize = 9;
            t1.fontStyle = FontStyle.Bold;
            t1.alignment = TextAnchor.MiddleLeft;
            t1.color = color;
            t1.text = label;
            var t1Rt = t1Go.GetComponent<RectTransform>();
            t1Rt.anchorMin = new Vector2(0f, 0.45f);
            t1Rt.anchorMax = new Vector2(1f, 1f);
            t1Rt.offsetMin = new Vector2(6f, 0f);
            t1Rt.offsetMax = new Vector2(-6f, 0f);

            // NASA Coordinates line
            GameObject t2Go = new GameObject("CoordText", typeof(RectTransform), typeof(Text));
            t2Go.transform.SetParent(pinGo.transform, false);
            var t2 = t2Go.GetComponent<Text>();
            t2.font = font;
            t2.fontSize = 8;
            t2.fontStyle = FontStyle.Normal;
            t2.alignment = TextAnchor.MiddleLeft;
            t2.color = new Color(0.92f, 0.96f, 0.98f, 0.90f);
            t2.text = $"📍 NASA: {nasaCoord}";
            var t2Rt = t2Go.GetComponent<RectTransform>();
            t2Rt.anchorMin = new Vector2(0f, 0f);
            t2Rt.anchorMax = new Vector2(1f, 0.45f);
            t2Rt.offsetMin = new Vector2(6f, 1f);
            t2Rt.offsetMax = new Vector2(-6f, 0f);

            return pRt;
        }

        private RectTransform CreateTacticalDangerCircle(RectTransform overlay, Font font, float worldX, float worldZ, float worldRadius, string title, Color color, string nasaCoord)
        {
            Vector2 tacNorm = WorldToTacticalMapNormalized(worldX, worldZ);
            float normU = tacNorm.x;
            float normV = tacNorm.y;

            GameObject circleGo = new GameObject($"Danger_{title}", typeof(RectTransform), typeof(Image));
            circleGo.transform.SetParent(overlay, false);
            var cRt = circleGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(normU, normV);
            cRt.anchorMax = new Vector2(normU, normV);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;

            float mapSpanX = mapWorldMaxX - mapWorldMinX;
            float pxRadius = Mathf.Clamp((worldRadius / mapSpanX) * 450f, 16f, 140f);
            cRt.sizeDelta = new Vector2(pxRadius * 2f, pxRadius * 2f);

            var img = circleGo.GetComponent<Image>();
            if (_cachedCircleFillSprite == null) _cachedCircleFillSprite = GetOrCreateCircleSprite(64, false);
            img.sprite = _cachedCircleFillSprite;
            img.color = new Color(color.r, color.g, color.b, 0.22f);

            var outl = circleGo.AddComponent<Outline>();
            outl.effectColor = new Color(color.r, color.g, color.b, 0.85f);
            outl.effectDistance = new Vector2(1.8f, -1.8f);

            return cRt;
        }

        private void CreateThreatDrawerCard(Transform container, Font font, DetectedThreatData threat, string decCoord, string dmsCoord, Vector3 playerPos)
        {
            GameObject cardGo = new GameObject($"Card_{threat.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(container, false);
            var cRt = cardGo.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(0f, 72f);

            var cImg = cardGo.GetComponent<Image>();
            cImg.color = new Color(0.06f, 0.11f, 0.16f, 0.95f);
            var cOut = cardGo.AddComponent<Outline>();
            cOut.effectColor = new Color(threat.color.r, threat.color.g, threat.color.b, 0.80f);
            cOut.effectDistance = new Vector2(1.2f, -1.2f);

            // Row 1: Title and Risk Tier Badge
            GameObject r1Go = new GameObject("Row1", typeof(RectTransform), typeof(Text));
            r1Go.transform.SetParent(cardGo.transform, false);
            var r1 = r1Go.GetComponent<Text>();
            r1.font = font;
            r1.fontSize = 10;
            r1.fontStyle = FontStyle.Bold;
            r1.color = threat.color;
            r1.text = $"{threat.title}  [{threat.riskTier}]";
            var r1Rt = r1Go.GetComponent<RectTransform>();
            r1Rt.anchorMin = new Vector2(0f, 0.68f);
            r1Rt.anchorMax = new Vector2(1f, 1f);
            r1Rt.offsetMin = new Vector2(8f, 0f);
            r1Rt.offsetMax = new Vector2(-8f, -2f);

            // Row 2: NASA Coordinates
            GameObject r2Go = new GameObject("Row2", typeof(RectTransform), typeof(Text));
            r2Go.transform.SetParent(cardGo.transform, false);
            var r2 = r2Go.GetComponent<Text>();
            r2.font = font;
            r2.fontSize = 9;
            r2.fontStyle = FontStyle.Normal;
            r2.color = new Color(0.95f, 0.88f, 0.40f);
            r2.text = $"📍 NASA: {decCoord} ({dmsCoord})";
            var r2Rt = r2Go.GetComponent<RectTransform>();
            r2Rt.anchorMin = new Vector2(0f, 0.40f);
            r2Rt.anchorMax = new Vector2(1f, 0.68f);
            r2Rt.offsetMin = new Vector2(8f, 0f);
            r2Rt.offsetMax = new Vector2(-8f, 0f);

            // Row 3: Sensor & Distance from player
            float dist = Vector3.Distance(playerPos, threat.worldPos);
            GameObject r3Go = new GameObject("Row3", typeof(RectTransform), typeof(Text));
            r3Go.transform.SetParent(cardGo.transform, false);
            var r3 = r3Go.GetComponent<Text>();
            threat.cardDistText = r3;
            r3.font = font;
            r3.fontSize = 8;
            r3.fontStyle = FontStyle.Normal;
            r3.color = new Color(0.75f, 0.88f, 0.95f);
            string closeTag = dist < threat.radius ? "<color=#FF3333>[INSIDE THREAT]</color>" : dist < threat.radius * 2f ? "<color=#FFAA33>[DANGER CLOSE]</color>" : "";
            r3.text = $"🛰️ {threat.nasaSensor} | Dist: {dist:F0}m {closeTag}";
            var r3Rt = r3Go.GetComponent<RectTransform>();
            r3Rt.anchorMin = new Vector2(0f, 0.16f);
            r3Rt.anchorMax = new Vector2(1f, 0.40f);
            r3Rt.offsetMin = new Vector2(8f, 0f);
            r3Rt.offsetMax = new Vector2(-8f, 0f);

            // Row 4: Action/Tip
            GameObject r4Go = new GameObject("Row4", typeof(RectTransform), typeof(Text));
            r4Go.transform.SetParent(cardGo.transform, false);
            var r4 = r4Go.GetComponent<Text>();
            r4.font = font;
            r4.fontSize = 8;
            r4.fontStyle = FontStyle.Normal;
            r4.color = new Color(0.65f, 0.80f, 0.85f, 0.85f);
            r4.text = $"⚠️ {threat.defenseTip}";
            var r4Rt = r4Go.GetComponent<RectTransform>();
            r4Rt.anchorMin = new Vector2(0f, 0f);
            r4Rt.anchorMax = new Vector2(1f, 0.16f);
            r4Rt.offsetMin = new Vector2(8f, 2f);
            r4Rt.offsetMax = new Vector2(-8f, 0f);

            var cBtn = cardGo.GetComponent<Button>();
            cBtn.onClick.AddListener(() => {
                if (threat.pinRt != null)
                {
                    StartCoroutine(PunchScale(threat.pinRt));
                }
            });
        }

        private void UpdateTacticalMapPopup()
        {
            if (tacticalMapContent == null || tacticalPlayerMarker == null) return;

            Vector3 playerPos = trackedPlayer != null ? trackedPlayer.position : Vector3.zero;
            float heading = trackedPlayer != null ? trackedPlayer.eulerAngles.y : 0f;

            Vector2 tacNorm = WorldToTacticalMapNormalized(playerPos.x, playerPos.z);
            float normU = tacNorm.x;
            float normV = tacNorm.y;

            float mapW = tacticalMapContent.rect.width;
            float mapH = tacticalMapContent.rect.height;
            tacticalPlayerMarker.anchoredPosition = new Vector2((normU - 0.5f) * mapW, (normV - 0.5f) * mapH);
            tacticalPlayerMarker.localEulerAngles = new Vector3(0f, 0f, -heading);

            // Live NASA coordinates
            (float pLat, float pLon) = WorldToNasaCoordinates(playerPos.x, playerPos.z);
            string pDec = FormatNasaCoordinatesDecimal(pLat, pLon);
            string pDms = FormatNasaCoordinatesDMS(pLat, pLon);

            if (tacticalPlayerCoordsText != null)
            {
                tacticalPlayerCoordsText.text = $"📍 LIVE SENSOR FIX: LAT {pLat:F4}°N | LON {pLon:F4}°E ({pDms}) [LANDSAT-9 / MODIS TERRA]";
            }

            // Sanctuary Goal distance and bearing
            Vector3 goalPos = new Vector3(75f, 0f, 270f);
            var ld = (gameManager != null) ? gameManager.CurrentLevelData : null;
            if (ld != null && ld.goalZone != null)
            {
                goalPos = new Vector3(ld.goalZone.centerX, 0f, ld.goalZone.centerZ);
            }
            float distToGoal = Vector3.Distance(playerPos, goalPos);
            Vector3 dirToGoal = (goalPos - playerPos).normalized;
            float bearing = (Mathf.Atan2(dirToGoal.x, dirToGoal.z) * Mathf.Rad2Deg + 360f) % 360f;

            if (tacticalGoalDistText != null)
            {
                (float gLat, float gLon) = WorldToNasaCoordinates(goalPos.x, goalPos.z);
                tacticalGoalDistText.text = $"🎯 SANCTUARY EXTRACTION GOAL: [{goalPos.x:F0}, {goalPos.z:F0}] ({FormatNasaCoordinatesDecimal(gLat, gLon)}) | DISTANCE: {distToGoal:F0}m | BEARING: {bearing:F0}° | STATUS: ACTIVE MISSION";
            }

            // Real-time distance update in Telemetry Drawer cards & danger pulse
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f);
            foreach (var t in activeThreats)
            {
                if (t == null) continue;
                if (t.cardDistText != null && trackedPlayer != null)
                {
                    float d = Vector3.Distance(playerPos, t.worldPos);
                    string closeTag = d < t.radius ? "<color=#FF3333>[INSIDE THREAT]</color>" : d < t.radius * 2f ? "<color=#FFAA33>[DANGER CLOSE]</color>" : "";
                    t.cardDistText.text = $"🛰️ {t.nasaSensor} | Dist: {d:F0}m {closeTag}";
                }

                if (t.circleRt != null)
                {
                    var img = t.circleRt.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = new Color(t.color.r, t.color.g, t.color.b, Mathf.Lerp(0.15f, 0.32f, pulse));
                    }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrapHUD()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            if (hud == null)
            {
                var hudGo = new GameObject("HUD");
                hud = hudGo.AddComponent<HudController>();
                Object.DontDestroyOnLoad(hudGo);
            }
        }

        private IEnumerator PunchScale(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 orig = tr.localScale;
            tr.localScale = orig * 1.18f;
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.Lerp(orig * 1.18f, orig, t / 0.12f);
                yield return null;
            }
            tr.localScale = orig;
        }

        private void OnDestroy()
        {
            ToolPickupItem.PickupSpawned -= SpawnMinimapToolMarker;
            ToolPickupItem.PickupCollected -= RemoveMinimapToolMarker;
            SundarbanGuardian.Environment.FoodItem.FoodSpawned -= SpawnMinimapFoodMarker;
            SundarbanGuardian.Environment.FoodItem.FoodEaten -= RemoveMinimapFoodMarker;

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

            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.RoundStarted -= OnCompetitiveRoundStarted;
                CompetitiveRoundManager.Instance.RoundTimerTick -= OnCompetitiveRoundTimerTick;
                CompetitiveRoundManager.Instance.RoundCompleted -= ShowCompetitiveRoundResults;
                CompetitiveRoundManager.Instance.TournamentCompleted -= ShowChampionshipPodium;
                CompetitiveRoundManager.Instance.RoundFailed -= ShowCompetitiveRoundFailed;
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
                activeBackpack.FoodCountChanged += OnFoodCountChanged;

                for (int i = 0; i < 4; i++)
                {
                    OnToolCountChanged((ToolType)i, activeBackpack.GetCount((ToolType)i));
                }
                OnActiveToolChanged(activeBackpack.activeTool);
                OnFoodCountChanged(activeBackpack.foodCount);
            }
        }

        private void UnhookBackpack()
        {
            if (activeBackpack != null)
            {
                activeBackpack.ToolCountChanged -= OnToolCountChanged;
                activeBackpack.ActiveToolChanged -= OnActiveToolChanged;
                activeBackpack.NearbyObstacleChanged -= OnNearbyObstacleChanged;
                activeBackpack.FoodCountChanged -= OnFoodCountChanged;
                activeBackpack = null;
            }
        }

        private void OnFoodCountChanged(int count)
        {
            if (foodCountBadgeText != null)
            {
                foodCountBadgeText.text = count.ToString();
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
                bool isSelected = ((ToolType)i == active);
                if (toolSlotOutlines[i] != null)
                {
                    toolSlotOutlines[i].enabled = isSelected;
                    toolSlotOutlines[i].effectColor = new Color(1.0f, 0.85f, 0.25f, 0.95f);
                }
                if (toolSlotBgs[i] != null)
                {
                    toolSlotBgs[i].color = isSelected
                        ? new Color(0.95f, 0.75f, 0.15f, 0.35f)
                        : new Color(0.06f, 0.12f, 0.18f, 0.38f);
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
            rt.anchoredPosition = new Vector2(-20f, -60f);
            rt.sizeDelta = new Vector2(476f, 84f);

            Sprite cardSprite = null;
#if UNITY_EDITOR
            cardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/rounded_card_12px.png");
#endif
            if (cardSprite == null) cardSprite = Resources.Load<Sprite>("Textures/UI/rounded_card_12px");

            var bg = boxGo.GetComponent<Image>();
            if (cardSprite != null) { bg.sprite = cardSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.03f, 0.08f, 0.14f, 0.45f); // transparent dark glass

            var outline = boxGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.75f, 0.95f, 0.50f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 4 Tool Buttons in Horizontal Row: SEED | KNIFE | CLEAN | TRAP DESTROY
            // User requested: "make these visible 2X size, transparent toollike button"
            float slotW = 74f;
            float slotH = 72f;
            float spacing = 6f;
            float startX = 6f;
            float startY = -6f;

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
            cachedToolSprites = toolSprites;

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
                if (cardSprite != null) { slotImg.sprite = cardSprite; slotImg.type = Image.Type.Sliced; }
                slotImg.color = (i == 0)
                    ? new Color(0.95f, 0.75f, 0.15f, 0.35f)
                    : new Color(0.06f, 0.12f, 0.18f, 0.38f);
                toolSlotBgs[i] = slotImg;

                var sOutline = btnGo.AddComponent<Outline>();
                sOutline.effectColor = new Color(1.0f, 0.85f, 0.25f, 0.95f);
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
                    StartCoroutine(PunchScale(btnGo.transform));
                });
                toolSlotButtons[i] = btn;

                // Right-click support to drop this specific tool
                var dropTrigger = btnGo.AddComponent<ToolSlotPointerDrop>();
                dropTrigger.onRightClick = () => {
                    if (activeBackpack != null)
                    {
                        activeBackpack.DropTool((ToolType)capturedIdx, 1);
                    }
                    StartCoroutine(PunchScale(btnGo.transform));
                };

                // Center Tool Graphic (2X size: 56px!)
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
                iconRt.anchoredPosition = new Vector2(0f, 0f);
                iconRt.sizeDelta = new Vector2(56f, 56f); // 2X size!

                // Hotkey badge in top-left ([1], [2], [3], [4])
                GameObject hotkeyGo = new GameObject("Hotkey", typeof(RectTransform), typeof(Text));
                hotkeyGo.transform.SetParent(btnGo.transform, false);
                var hotkeyTxt = hotkeyGo.GetComponent<Text>();
                hotkeyTxt.font = font;
                hotkeyTxt.fontSize = 10;
                hotkeyTxt.fontStyle = FontStyle.Bold;
                hotkeyTxt.alignment = TextAnchor.UpperLeft;
                hotkeyTxt.color = new Color(0.70f, 0.85f, 1.0f, 0.75f);
                hotkeyTxt.text = $"[{i + 1}]";
                hotkeyTxt.raycastTarget = false;
                var hkRt = hotkeyGo.GetComponent<RectTransform>();
                hkRt.anchorMin = new Vector2(0f, 1f);
                hkRt.anchorMax = new Vector2(0f, 1f);
                hkRt.pivot = new Vector2(0f, 1f);
                hkRt.anchoredPosition = new Vector2(5f, -3f);
                hkRt.sizeDelta = new Vector2(24f, 16f);

                // Fallback Emoji text (if sprite isn't loaded)
                GameObject emojiGo = new GameObject("Emoji", typeof(RectTransform), typeof(Text));
                emojiGo.transform.SetParent(btnGo.transform, false);
                var emojiTxt = emojiGo.GetComponent<Text>();
                emojiTxt.font = font;
                emojiTxt.fontSize = 28;
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
                countTxt.fontSize = 14;
                countTxt.fontStyle = FontStyle.Bold;
                countTxt.alignment = TextAnchor.LowerRight;
                countTxt.color = new Color(1f, 0.95f, 0.40f, 1f);
                countTxt.text = (activeBackpack != null) ? activeBackpack.GetCount((ToolType)i).ToString() : "0";
                countTxt.raycastTarget = false;
                toolSlotCountTexts[i] = countTxt;

                var countOutline = countGo.AddComponent<Outline>();
                countOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                countOutline.effectDistance = new Vector2(1.2f, -1.2f);

                var countRt = countGo.GetComponent<RectTransform>();
                countRt.anchorMin = new Vector2(0f, 0f);
                countRt.anchorMax = new Vector2(1f, 0f);
                countRt.pivot = new Vector2(1f, 0f);
                countRt.anchoredPosition = new Vector2(-6f, 3f);
            }

            // 5th Dedicated DROP Button [DROP ⏏]
            float dropW = 66f;
            GameObject dropBtnGo = new GameObject("Btn_DropTool", typeof(RectTransform), typeof(Image), typeof(Button));
            dropBtnGo.transform.SetParent(boxGo.transform, false);

            var dRt = dropBtnGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 1f);
            dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.anchoredPosition = new Vector2(startX + 4 * (slotW + spacing), startY);
            dRt.sizeDelta = new Vector2(dropW, slotH);

            var dropImg = dropBtnGo.GetComponent<Image>();
            if (cardSprite != null) { dropImg.sprite = cardSprite; dropImg.type = Image.Type.Sliced; }
            dropImg.color = new Color(0.24f, 0.10f, 0.12f, 0.50f); // warm red/amber glass

            var dOutline = dropBtnGo.AddComponent<Outline>();
            dOutline.effectColor = new Color(0.95f, 0.50f, 0.25f, 0.85f);
            dOutline.effectDistance = new Vector2(1.8f, -1.8f);

            GameObject dTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            dTxtGo.transform.SetParent(dropBtnGo.transform, false);
            var dTxt = dTxtGo.GetComponent<Text>();
            dTxt.font = font;
            dTxt.fontSize = 11;
            dTxt.fontStyle = FontStyle.Bold;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = new Color(1f, 0.85f, 0.75f);
            dTxt.text = "DROP\n[G]";
            dTxt.raycastTarget = false;

            var dtRt = dTxtGo.GetComponent<RectTransform>();
            dtRt.anchorMin = Vector2.zero;
            dtRt.anchorMax = Vector2.one;
            dtRt.sizeDelta = Vector2.zero;

            var dropBtn = dropBtnGo.GetComponent<Button>();
            dropBtn.onClick.AddListener(() => {
                if (activeBackpack != null)
                {
                    activeBackpack.DropActiveTool();
                }
                StartCoroutine(PunchScale(dropBtnGo.transform));
            });

            // 6th Dedicated EAT FOOD Button [EAT 🍗 [E]]
            float eatW = 66f;
            GameObject eatBtnGo = new GameObject("Btn_EatFood", typeof(RectTransform), typeof(Image), typeof(Button));
            eatBtnGo.transform.SetParent(boxGo.transform, false);

            var eRt = eatBtnGo.GetComponent<RectTransform>();
            eRt.anchorMin = new Vector2(0f, 1f);
            eRt.anchorMax = new Vector2(0f, 1f);
            eRt.pivot = new Vector2(0f, 1f);
            eRt.anchoredPosition = new Vector2(startX + 4 * (slotW + spacing) + dropW + spacing, startY);
            eRt.sizeDelta = new Vector2(eatW, slotH);

            var eatImg = eatBtnGo.GetComponent<Image>();
            if (cardSprite != null) { eatImg.sprite = cardSprite; eatImg.type = Image.Type.Sliced; }
            eatImg.color = new Color(0.08f, 0.28f, 0.16f, 0.55f); // emerald nourishment glass

            var eOutline = eatBtnGo.AddComponent<Outline>();
            eOutline.effectColor = new Color(0.35f, 0.95f, 0.50f, 0.90f);
            eOutline.effectDistance = new Vector2(1.8f, -1.8f);

            GameObject eTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            eTxtGo.transform.SetParent(eatBtnGo.transform, false);
            var eTxt = eTxtGo.GetComponent<Text>();
            eTxt.font = font;
            eTxt.fontSize = 11;
            eTxt.fontStyle = FontStyle.Bold;
            eTxt.alignment = TextAnchor.MiddleCenter;
            eTxt.color = new Color(0.85f, 1f, 0.90f);
            eTxt.text = "EAT\n🍗 [E]";
            eTxt.raycastTarget = false;

            var etRt = eTxtGo.GetComponent<RectTransform>();
            etRt.anchorMin = Vector2.zero;
            etRt.anchorMax = Vector2.one;
            etRt.sizeDelta = Vector2.zero;

            // Food count corner badge
            GameObject foodBadgeGo = new GameObject("FoodBadge", typeof(RectTransform), typeof(Text));
            foodBadgeGo.transform.SetParent(eatBtnGo.transform, false);
            foodCountBadgeText = foodBadgeGo.GetComponent<Text>();
            foodCountBadgeText.font = font;
            foodCountBadgeText.fontSize = 14;
            foodCountBadgeText.fontStyle = FontStyle.Bold;
            foodCountBadgeText.alignment = TextAnchor.LowerRight;
            foodCountBadgeText.color = new Color(0.40f, 1.0f, 0.60f, 1f);
            foodCountBadgeText.text = (activeBackpack != null) ? activeBackpack.foodCount.ToString() : "1";
            foodCountBadgeText.raycastTarget = false;

            var foodBadgeOutline = foodBadgeGo.AddComponent<Outline>();
            foodBadgeOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            foodBadgeOutline.effectDistance = new Vector2(1.2f, -1.2f);

            var fbRt = foodBadgeGo.GetComponent<RectTransform>();
            fbRt.anchorMin = new Vector2(0f, 0f);
            fbRt.anchorMax = new Vector2(1f, 0f);
            fbRt.pivot = new Vector2(1f, 0f);
            fbRt.anchoredPosition = new Vector2(-6f, 3f);

            eatFoodBtn = eatBtnGo.GetComponent<Button>();
            eatFoodBtn.onClick.AddListener(() => {
                if (activeBackpack != null)
                {
                    activeBackpack.TryEatFood();
                }
                StartCoroutine(PunchScale(eatBtnGo.transform));
            });
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
            rt.sizeDelta = new Vector2(560f, 52f);

            var bg = toastGo.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.28f, 0.16f, 0.88f); // vibrant emerald glassmorphism

            var outline = toastGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 1.0f, 0.65f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject txtGo = new GameObject("Toast_Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(toastGo.transform, false);
            restorationToastText = txtGo.GetComponent<Text>();
            restorationToastText.font = font;
            restorationToastText.fontSize = 16;
            restorationToastText.fontStyle = FontStyle.Bold;
            restorationToastText.alignment = TextAnchor.MiddleCenter;
            restorationToastText.color = new Color(0.92f, 1.0f, 0.96f);
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
            // Load Free Fire style UI sprites from Resources with editor fallback
            Sprite joyBaseSp = Resources.Load<Sprite>("Textures/UI/joystick_base");
            Sprite joyKnobSp = Resources.Load<Sprite>("Textures/UI/joystick_knob");
            Sprite glassBtnSp = Resources.Load<Sprite>("Textures/UI/btn_circle_glass");
            Sprite autoRunIconSp = Resources.Load<Sprite>("Textures/UI/icon_autorun");
            Sprite fireIconSp = Resources.Load<Sprite>("Textures/UI/icon_fire");
            Sprite jumpIconSp = Resources.Load<Sprite>("Textures/UI/icon_jump");

#if UNITY_EDITOR
            if (joyBaseSp == null) joyBaseSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/joystick_base.png");
            if (joyKnobSp == null) joyKnobSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/joystick_knob.png");
            if (glassBtnSp == null) glassBtnSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/btn_circle_glass.png");
            if (autoRunIconSp == null) autoRunIconSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_autorun.png");
            if (fireIconSp == null) fireIconSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_fire.png");
            if (jumpIconSp == null) jumpIconSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/icon_jump.png");
#endif

            // 1. Touch Look Zone covering whole screen behind other UI
            GameObject lookZoneGo = new GameObject("Touch_Look_Zone", typeof(RectTransform), typeof(Image), typeof(SundarbanGuardian.UI.TouchLookZone));
            lookZoneGo.transform.SetParent(parent, false);
            lookZoneGo.transform.SetAsFirstSibling();
            var lRt = lookZoneGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.32f, 0f);
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
            kImg.raycastTarget = false;

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
                StartCoroutine(PunchScale(autoRunGo.transform));
            });

            SundarbanGuardian.Player.MobileInputManager.AutoRunStateChanged += OnAutoRunChanged;

            // 3.5 Circular Glass JUMP Button placed beside Auto-Run
            GameObject jumpGo = new GameObject("Btn_Jump", typeof(RectTransform), typeof(Image), typeof(Button));
            jumpGo.transform.SetParent(parent, false);
            var jmpRt = jumpGo.GetComponent<RectTransform>();
            jmpRt.anchorMin = new Vector2(0f, 0f);
            jmpRt.anchorMax = new Vector2(0f, 0f);
            jmpRt.pivot = new Vector2(0.5f, 0.5f);
            jmpRt.anchoredPosition = new Vector2(138f, 235f);
            jmpRt.sizeDelta = new Vector2(56f, 56f);

            var jumpImg = jumpGo.GetComponent<Image>();
            if (glassBtnSp != null)
            {
                jumpImg.sprite = glassBtnSp;
                jumpImg.color = Color.white;
            }
            else
            {
                jumpImg.color = new Color(0.08f, 0.10f, 0.12f, 0.65f);
            }

            var jumpOutline = jumpGo.AddComponent<Outline>();
            jumpOutline.effectColor = new Color(0.35f, 0.85f, 0.95f, 0.65f);
            jumpOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Child Jump Icon Image
            GameObject jIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            jIconGo.transform.SetParent(jumpGo.transform, false);
            var jIconImg = jIconGo.GetComponent<Image>();
            if (jumpIconSp != null)
            {
                jIconImg.sprite = jumpIconSp;
                jIconImg.preserveAspect = true;
                jIconImg.color = Color.white;
            }
            jIconImg.raycastTarget = false;
            var jIconRt = jIconGo.GetComponent<RectTransform>();
            jIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            jIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            jIconRt.pivot = new Vector2(0.5f, 0.5f);
            jIconRt.anchoredPosition = new Vector2(0f, 6f);
            jIconRt.sizeDelta = new Vector2(32f, 32f);

            // Text Label "JUMP"
            GameObject jTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            jTxtGo.transform.SetParent(jumpGo.transform, false);
            var jTxt = jTxtGo.GetComponent<Text>();
            jTxt.font = font;
            jTxt.fontSize = 10;
            jTxt.fontStyle = FontStyle.Bold;
            jTxt.alignment = TextAnchor.LowerCenter;
            jTxt.color = new Color(0.92f, 0.96f, 1f, 0.95f);
            jTxt.text = "JUMP";
            jTxt.raycastTarget = false;
            var jTxtRt = jTxtGo.GetComponent<RectTransform>();
            jTxtRt.anchorMin = new Vector2(0f, 0f);
            jTxtRt.anchorMax = new Vector2(1f, 0f);
            jTxtRt.pivot = new Vector2(0.5f, 0f);
            jTxtRt.anchoredPosition = new Vector2(0f, 4f);
            jTxtRt.sizeDelta = new Vector2(0f, 14f);

            var jumpBtn = jumpGo.GetComponent<Button>();
            jumpBtn.onClick.AddListener(() => {
                SundarbanGuardian.Player.MobileInputManager.TriggerJump();
                StartCoroutine(PunchScale(jumpGo.transform));
            });
            var jumpPtr = jumpGo.AddComponent<ActionPointerHandler>();
            jumpPtr.onAction = () => {
                SundarbanGuardian.Player.MobileInputManager.TriggerJump();
                StartCoroutine(PunchScale(jumpGo.transform));
            };

            // 3.6 Right-Thumb JUMP Button (Right Hand Cluster beside FIRE button - Free Fire / PUBG standard)
            GameObject jumpRightGo = new GameObject("Btn_Jump_Right", typeof(RectTransform), typeof(Image), typeof(Button));
            jumpRightGo.transform.SetParent(parent, false);
            var jrRt = jumpRightGo.GetComponent<RectTransform>();
            jrRt.anchorMin = new Vector2(1f, 0f);
            jrRt.anchorMax = new Vector2(1f, 0f);
            jrRt.pivot = new Vector2(0.5f, 0.5f);
            jrRt.anchoredPosition = new Vector2(-225f, 105f);
            jrRt.sizeDelta = new Vector2(72f, 72f);

            var jrImg = jumpRightGo.GetComponent<Image>();
            if (glassBtnSp != null)
            {
                jrImg.sprite = glassBtnSp;
                jrImg.color = Color.white;
            }
            else
            {
                jrImg.color = new Color(0.08f, 0.10f, 0.12f, 0.65f);
            }

            var jrOutline = jumpRightGo.AddComponent<Outline>();
            jrOutline.effectColor = new Color(0.35f, 0.85f, 0.95f, 0.75f);
            jrOutline.effectDistance = new Vector2(1.8f, -1.8f);

            // Right Jump Icon
            GameObject jrIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            jrIconGo.transform.SetParent(jumpRightGo.transform, false);
            var jrIconImg = jrIconGo.GetComponent<Image>();
            if (jumpIconSp != null)
            {
                jrIconImg.sprite = jumpIconSp;
                jrIconImg.preserveAspect = true;
                jrIconImg.color = Color.white;
            }
            jrIconImg.raycastTarget = false;
            var jrIconRt = jrIconGo.GetComponent<RectTransform>();
            jrIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            jrIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            jrIconRt.pivot = new Vector2(0.5f, 0.5f);
            jrIconRt.anchoredPosition = new Vector2(0f, 7f);
            jrIconRt.sizeDelta = new Vector2(40f, 40f);

            // Right Jump Text
            GameObject jrTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            jrTxtGo.transform.SetParent(jumpRightGo.transform, false);
            var jrTxt = jrTxtGo.GetComponent<Text>();
            jrTxt.font = font;
            jrTxt.fontSize = 11;
            jrTxt.fontStyle = FontStyle.Bold;
            jrTxt.alignment = TextAnchor.LowerCenter;
            jrTxt.color = new Color(0.92f, 0.96f, 1f, 0.95f);
            jrTxt.text = "JUMP";
            jrTxt.raycastTarget = false;
            var jrTxtRt = jrTxtGo.GetComponent<RectTransform>();
            jrTxtRt.anchorMin = new Vector2(0f, 0f);
            jrTxtRt.anchorMax = new Vector2(1f, 0f);
            jrTxtRt.pivot = new Vector2(0.5f, 0f);
            jrTxtRt.anchoredPosition = new Vector2(0f, 5f);
            jrTxtRt.sizeDelta = new Vector2(0f, 16f);

            var jrBtn = jumpRightGo.GetComponent<Button>();
            jrBtn.onClick.AddListener(() => {
                SundarbanGuardian.Player.MobileInputManager.TriggerJump();
                StartCoroutine(PunchScale(jumpRightGo.transform));
            });
            var jrPtr = jumpRightGo.AddComponent<ActionPointerHandler>();
            jrPtr.onAction = () => {
                SundarbanGuardian.Player.MobileInputManager.TriggerJump();
                StartCoroutine(PunchScale(jumpRightGo.transform));
            };

            // 4. Large Circular FIRE Button (Bottom-Right / Right Thumb Area per Image 1)
            GameObject hitGo = new GameObject("Btn_Fire", typeof(RectTransform), typeof(Image), typeof(Button));
            hitGo.transform.SetParent(parent, false);
            var hRt = hitGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(1f, 0f);
            hRt.anchorMax = new Vector2(1f, 0f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = new Vector2(-105f, 105f);
            hRt.sizeDelta = new Vector2(100f, 100f);

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
            hitActionBtnText.raycastTarget = false;
            var hTxtRt = hTxtGo.GetComponent<RectTransform>();
            hTxtRt.anchorMin = new Vector2(0f, 0f);
            hTxtRt.anchorMax = new Vector2(1f, 0f);
            hTxtRt.pivot = new Vector2(0.5f, 0f);
            hTxtRt.anchoredPosition = new Vector2(0f, 8f);
            hTxtRt.sizeDelta = new Vector2(0f, 18f);

            hitActionBtn = hitGo.GetComponent<Button>();
            hitActionBtn.onClick.AddListener(() => {
                SundarbanGuardian.Player.MobileInputManager.TriggerHitAction();
                StartCoroutine(PunchScale(hitGo.transform));
            });
            var hitPtr = hitGo.AddComponent<ActionPointerHandler>();
            hitPtr.onAction = () => {
                SundarbanGuardian.Player.MobileInputManager.TriggerHitAction();
                StartCoroutine(PunchScale(hitGo.transform));
            };
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

        // ──────────────────────────────────────────────────────────────────────────
        // 🏆 COMPETITIVE TOURNAMENT & ROUND NAVIGATION SYSTEM
        // ──────────────────────────────────────────────────────────────────────────

        private void BuildCompetitiveTopBar(Transform parent, Font font)
        {
            GameObject barGo = new GameObject("Competitive_Top_Bar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(parent, false);
            competitiveTopBar = barGo.GetComponent<RectTransform>();
            competitiveTopBar.anchorMin = new Vector2(0.5f, 1f);
            competitiveTopBar.anchorMax = new Vector2(0.5f, 1f);
            competitiveTopBar.pivot = new Vector2(0.5f, 1f);
            competitiveTopBar.anchoredPosition = new Vector2(0f, -52f);
            competitiveTopBar.sizeDelta = new Vector2(510f, 32f);

            var barBg = barGo.GetComponent<Image>();
            barBg.color = new Color(0.04f, 0.08f, 0.12f, 0.94f);
            var barOutline = barGo.AddComponent<Outline>();
            barOutline.effectColor = new Color(0.20f, 0.75f, 0.90f, 0.85f);
            barOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Left: Species/Round Badge
            GameObject badgeGo = new GameObject("Round_Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(barGo.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 0f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(4f, 0f);
            badgeRt.sizeDelta = new Vector2(175f, -4f);
            roundBadgeBg = badgeGo.GetComponent<Image>();
            roundBadgeBg.color = new Color(0.96f, 0.58f, 0.12f, 0.95f);

            GameObject badgeTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            badgeTxtGo.transform.SetParent(badgeGo.transform, false);
            roundBadgeText = badgeTxtGo.GetComponent<Text>();
            roundBadgeText.font = font;
            roundBadgeText.fontSize = 12;
            roundBadgeText.fontStyle = FontStyle.Bold;
            roundBadgeText.alignment = TextAnchor.MiddleCenter;
            roundBadgeText.color = Color.white;
            roundBadgeText.text = "ROUND 1/4 • TIGER";
            var btRt = badgeTxtGo.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // Center: Live Stopwatch Timer
            GameObject timerGo = new GameObject("Timer_Text", typeof(RectTransform), typeof(Text));
            timerGo.transform.SetParent(barGo.transform, false);
            var timerRt = timerGo.GetComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.36f, 0f);
            timerRt.anchorMax = new Vector2(0.62f, 1f);
            timerRt.anchoredPosition = Vector2.zero;
            timerRt.sizeDelta = Vector2.zero;
            roundTimerText = timerGo.GetComponent<Text>();
            roundTimerText.font = font;
            roundTimerText.fontSize = 15;
            roundTimerText.fontStyle = FontStyle.Bold;
            roundTimerText.alignment = TextAnchor.MiddleCenter;
            roundTimerText.color = new Color(1.0f, 0.92f, 0.40f);
            roundTimerText.text = "⏱ 00:00.00";

            // Center-Right: Target Par Time
            GameObject parGo = new GameObject("Par_Text", typeof(RectTransform), typeof(Text));
            parGo.transform.SetParent(barGo.transform, false);
            var parRt = parGo.GetComponent<RectTransform>();
            parRt.anchorMin = new Vector2(0.63f, 0f);
            parRt.anchorMax = new Vector2(0.79f, 1f);
            parRt.anchoredPosition = Vector2.zero;
            parRt.sizeDelta = Vector2.zero;
            roundParText = parGo.GetComponent<Text>();
            roundParText.font = font;
            roundParText.fontSize = 11;
            roundParText.fontStyle = FontStyle.Bold;
            roundParText.alignment = TextAnchor.MiddleCenter;
            roundParText.color = new Color(0.75f, 0.85f, 0.92f);
            roundParText.text = "PAR 01:45";

            // Right: Interactive [ROUNDS] Navigation Button
            GameObject btnGo = new GameObject("Btn_Rounds_Nav", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(barGo.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-4f, 0f);
            btnRt.sizeDelta = new Vector2(98f, 24f);

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.12f, 0.65f, 0.45f, 0.95f);
            var btnOutline = btnGo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.35f, 0.95f, 0.70f, 0.95f);
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            roundSelectorNavBtn = btnGo.GetComponent<Button>();
            roundSelectorNavBtn.onClick.AddListener(ToggleRoundSelectorModal);

            GameObject btnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxt = btnTxtGo.GetComponent<Text>();
            btnTxt.font = font;
            btnTxt.fontSize = 11;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
            btnTxt.text = "🗺 ROUNDS";
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.sizeDelta = Vector2.zero;
        }

        private void BuildCompetitiveRoundResultsModal(Transform parent, Font font)
        {
            roundResultsModal = new GameObject("Competitive_Round_Results_Modal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            roundResultsModal.transform.SetParent(parent, false);
            var rt = roundResultsModal.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            roundResultsModal.GetComponent<Image>().color = new Color(0.01f, 0.03f, 0.06f, 0.88f);

            // Dialog Card
            GameObject card = new GameObject("Results_Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(roundResultsModal.transform, false);
            var cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(860f, 560f);

            card.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.13f, 0.98f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.95f, 0.78f, 0.22f, 0.95f);
            cardOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // Header Banner
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(card.transform, false);
            resultTitleText = titleGo.GetComponent<Text>();
            resultTitleText.font = font;
            resultTitleText.fontSize = 24;
            resultTitleText.fontStyle = FontStyle.Bold;
            resultTitleText.alignment = TextAnchor.MiddleCenter;
            resultTitleText.color = new Color(1.0f, 0.85f, 0.25f);
            resultTitleText.text = "ROUND 1 COMPLETE!";
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.90f);
            tRt.anchorMax = new Vector2(0.95f, 0.98f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = Vector2.zero;

            // Subtitle
            GameObject subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(card.transform, false);
            resultSubtitleText = subGo.GetComponent<Text>();
            resultSubtitleText.font = font;
            resultSubtitleText.fontSize = 13;
            resultSubtitleText.fontStyle = FontStyle.Bold;
            resultSubtitleText.alignment = TextAnchor.MiddleCenter;
            resultSubtitleText.color = new Color(0.35f, 0.92f, 0.85f);
            resultSubtitleText.text = "ROYAL BENGAL TIGER • MISSION ACCOMPLISHED";
            var sRt = subGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.84f);
            sRt.anchorMax = new Vector2(0.95f, 0.90f);
            sRt.anchoredPosition = Vector2.zero;
            sRt.sizeDelta = Vector2.zero;

            // Stars Rating & Rank Banner
            GameObject starsGo = new GameObject("Stars_Text", typeof(RectTransform), typeof(Text));
            starsGo.transform.SetParent(card.transform, false);
            resultStarsText = starsGo.GetComponent<Text>();
            resultStarsText.font = font;
            resultStarsText.fontSize = 32;
            resultStarsText.fontStyle = FontStyle.Bold;
            resultStarsText.alignment = TextAnchor.MiddleCenter;
            resultStarsText.color = new Color(1f, 0.85f, 0.2f);
            resultStarsText.text = "★★★";
            var stRt = starsGo.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.15f, 0.75f);
            stRt.anchorMax = new Vector2(0.48f, 0.84f);
            stRt.anchoredPosition = Vector2.zero;
            stRt.sizeDelta = Vector2.zero;

            GameObject rankGo = new GameObject("Rank_Badge_Text", typeof(RectTransform), typeof(Text));
            rankGo.transform.SetParent(card.transform, false);
            resultRankBadgeText = rankGo.GetComponent<Text>();
            resultRankBadgeText.font = font;
            resultRankBadgeText.fontSize = 15;
            resultRankBadgeText.fontStyle = FontStyle.Bold;
            resultRankBadgeText.alignment = TextAnchor.MiddleCenter;
            resultRankBadgeText.color = new Color(0.25f, 0.95f, 0.55f);
            resultRankBadgeText.text = "APEX GUARDIAN (RANK S)";
            var rkRt = rankGo.GetComponent<RectTransform>();
            rkRt.anchorMin = new Vector2(0.52f, 0.75f);
            rkRt.anchorMax = new Vector2(0.85f, 0.84f);
            rkRt.anchoredPosition = Vector2.zero;
            rkRt.sizeDelta = Vector2.zero;

            // 4 Stats Cards Grid
            float[] gridXMin = { 0.04f, 0.28f, 0.52f, 0.76f };
            float[] gridXMax = { 0.26f, 0.50f, 0.74f, 0.96f };

            // Card 1: Time
            BuildStatCard(card.transform, font, gridXMin[0], gridXMax[0], "ROUND TIME", out resultTimeValueText, out resultParValueText);
            // Card 2: Time Bonus
            BuildStatCard(card.transform, font, gridXMin[1], gridXMax[1], "TIME BONUS", out resultTimeBonusText, out Text dummy1);
            // Card 3: Vitality Bonus
            BuildStatCard(card.transform, font, gridXMin[2], gridXMax[2], "VITALITY BONUS", out resultVitalityBonusText, out Text dummy2);
            // Card 4: Total Score
            BuildStatCard(card.transform, font, gridXMin[3], gridXMax[3], "ROUND SCORE", out resultScoreValueText, out Text dummy3, true);

            // New Record Badge
            resultNewRecordBadge = new GameObject("New_Record_Badge", typeof(RectTransform), typeof(Image));
            resultNewRecordBadge.transform.SetParent(card.transform, false);
            var nrbRt = resultNewRecordBadge.GetComponent<RectTransform>();
            nrbRt.anchorMin = new Vector2(0.5f, 0.50f);
            nrbRt.anchorMax = new Vector2(0.5f, 0.50f);
            nrbRt.pivot = new Vector2(0.5f, 0.5f);
            nrbRt.sizeDelta = new Vector2(280f, 26f);
            resultNewRecordBadge.GetComponent<Image>().color = new Color(0.95f, 0.65f, 0.12f, 0.95f);
            var nrbOutline = resultNewRecordBadge.AddComponent<Outline>();
            nrbOutline.effectColor = Color.white;
            nrbOutline.effectDistance = new Vector2(1f, -1f);

            GameObject nrbTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            nrbTxtGo.transform.SetParent(resultNewRecordBadge.transform, false);
            var nrbTxt = nrbTxtGo.GetComponent<Text>();
            nrbTxt.font = font;
            nrbTxt.fontSize = 12;
            nrbTxt.fontStyle = FontStyle.Bold;
            nrbTxt.alignment = TextAnchor.MiddleCenter;
            nrbTxt.color = Color.white;
            nrbTxt.text = "🏆 NEW PERSONAL BEST RECORD!";
            var nrbTxtRt = nrbTxtGo.GetComponent<RectTransform>();
            nrbTxtRt.anchorMin = Vector2.zero;
            nrbTxtRt.anchorMax = Vector2.one;
            nrbTxtRt.sizeDelta = Vector2.zero;

            // Simulated Leaderboard Container Box
            GameObject lbBox = new GameObject("Leaderboard_Box", typeof(RectTransform), typeof(Image));
            lbBox.transform.SetParent(card.transform, false);
            var lbRt = lbBox.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0.04f, 0.14f);
            lbRt.anchorMax = new Vector2(0.96f, 0.46f);
            lbRt.anchoredPosition = Vector2.zero;
            lbRt.sizeDelta = Vector2.zero;
            lbBox.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.08f, 0.95f);
            var lbOutline = lbBox.AddComponent<Outline>();
            lbOutline.effectColor = new Color(0.20f, 0.40f, 0.55f, 0.70f);
            lbOutline.effectDistance = new Vector2(1f, -1f);

            // Leaderboard Header
            GameObject lbHead = new GameObject("Header", typeof(RectTransform), typeof(Text));
            lbHead.transform.SetParent(lbBox.transform, false);
            var lbHeadTxt = lbHead.GetComponent<Text>();
            lbHeadTxt.font = font;
            lbHeadTxt.fontSize = 11;
            lbHeadTxt.fontStyle = FontStyle.Bold;
            lbHeadTxt.alignment = TextAnchor.MiddleLeft;
            lbHeadTxt.color = new Color(0.40f, 0.90f, 0.75f);
            lbHeadTxt.text = "  RANK       CONSERVATION RANGER                     TIME                  SCORE            STATUS";
            var lbHeadRt = lbHead.GetComponent<RectTransform>();
            lbHeadRt.anchorMin = new Vector2(0f, 0.82f);
            lbHeadRt.anchorMax = new Vector2(1f, 1f);
            lbHeadRt.anchoredPosition = Vector2.zero;
            lbHeadRt.sizeDelta = Vector2.zero;

            // Rows Root
            GameObject rowsRoot = new GameObject("Rows_Container", typeof(RectTransform));
            rowsRoot.transform.SetParent(lbBox.transform, false);
            leaderboardRowsContainer = rowsRoot.transform;
            var rowsRt = rowsRoot.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 0.80f);
            rowsRt.anchoredPosition = Vector2.zero;
            rowsRt.sizeDelta = Vector2.zero;

            // Action Buttons
            // 1. Next Round (Primary)
            GameObject btnNext = new GameObject("Btn_Next_Round", typeof(RectTransform), typeof(Image), typeof(Button));
            btnNext.transform.SetParent(card.transform, false);
            var bnRt = btnNext.GetComponent<RectTransform>();
            bnRt.anchorMin = new Vector2(0.55f, 0.025f);
            bnRt.anchorMax = new Vector2(0.96f, 0.11f);
            bnRt.anchoredPosition = Vector2.zero;
            bnRt.sizeDelta = Vector2.zero;
            btnNext.GetComponent<Image>().color = new Color(0.12f, 0.72f, 0.40f, 0.98f);
            var bnOutline = btnNext.AddComponent<Outline>();
            bnOutline.effectColor = new Color(0.45f, 0.98f, 0.75f, 0.95f);
            bnOutline.effectDistance = new Vector2(2f, -2f);

            btnNextRoundAction = btnNext.GetComponent<Button>();
            btnNextRoundAction.onClick.AddListener(() =>
            {
                CloseRoundResultsModal();
                if (CompetitiveRoundManager.Instance != null)
                {
                    CompetitiveRoundManager.Instance.AdvanceToNextRound();
                }
            });

            GameObject bnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            bnTxtGo.transform.SetParent(btnNext.transform, false);
            btnNextRoundText = bnTxtGo.GetComponent<Text>();
            btnNextRoundText.font = font;
            btnNextRoundText.fontSize = 14;
            btnNextRoundText.fontStyle = FontStyle.Bold;
            btnNextRoundText.alignment = TextAnchor.MiddleCenter;
            btnNextRoundText.color = Color.white;
            btnNextRoundText.text = "NEXT ROUND: ROUND 2 (DEER) ▶";
            var bntRt = bnTxtGo.GetComponent<RectTransform>();
            bntRt.anchorMin = Vector2.zero;
            bntRt.anchorMax = Vector2.one;
            bntRt.sizeDelta = Vector2.zero;

            // 2. Retry Round
            GameObject btnRetry = new GameObject("Btn_Retry_Round", typeof(RectTransform), typeof(Image), typeof(Button));
            btnRetry.transform.SetParent(card.transform, false);
            var brRt = btnRetry.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0.28f, 0.025f);
            brRt.anchorMax = new Vector2(0.53f, 0.11f);
            brRt.anchoredPosition = Vector2.zero;
            brRt.sizeDelta = Vector2.zero;
            btnRetry.GetComponent<Image>().color = new Color(0.22f, 0.28f, 0.36f, 0.95f);

            btnRetryRoundAction = btnRetry.GetComponent<Button>();
            btnRetryRoundAction.onClick.AddListener(() =>
            {
                CloseRoundResultsModal();
                if (CompetitiveRoundManager.Instance != null)
                {
                    CompetitiveRoundManager.Instance.RestartCurrentRound();
                }
            });

            GameObject brTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            brTxtGo.transform.SetParent(btnRetry.transform, false);
            var brTxt = brTxtGo.GetComponent<Text>();
            brTxt.font = font;
            brTxt.fontSize = 13;
            brTxt.fontStyle = FontStyle.Bold;
            brTxt.alignment = TextAnchor.MiddleCenter;
            brTxt.color = new Color(0.95f, 0.88f, 0.65f);
            brTxt.text = "RETRY ROUND ↺";
            var brtRt = brTxtGo.GetComponent<RectTransform>();
            brtRt.anchorMin = Vector2.zero;
            brtRt.anchorMax = Vector2.one;
            brtRt.sizeDelta = Vector2.zero;

            // 3. Tournament Bracket / Round Selector
            GameObject btnBracket = new GameObject("Btn_Bracket_Nav", typeof(RectTransform), typeof(Image), typeof(Button));
            btnBracket.transform.SetParent(card.transform, false);
            var bbRt = btnBracket.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0.04f, 0.025f);
            bbRt.anchorMax = new Vector2(0.26f, 0.11f);
            bbRt.anchoredPosition = Vector2.zero;
            bbRt.sizeDelta = Vector2.zero;
            btnBracket.GetComponent<Image>().color = new Color(0.14f, 0.38f, 0.58f, 0.95f);

            btnOpenBracketAction = btnBracket.GetComponent<Button>();
            btnOpenBracketAction.onClick.AddListener(() =>
            {
                CloseRoundResultsModal();
                OpenRoundSelectorModal();
            });

            GameObject bbTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            bbTxtGo.transform.SetParent(btnBracket.transform, false);
            var bbTxt = bbTxtGo.GetComponent<Text>();
            bbTxt.font = font;
            bbTxt.fontSize = 13;
            bbTxt.fontStyle = FontStyle.Bold;
            bbTxt.alignment = TextAnchor.MiddleCenter;
            bbTxt.color = Color.white;
            bbTxt.text = "ROUNDS 🗺";
            var bbtRt = bbTxtGo.GetComponent<RectTransform>();
            bbtRt.anchorMin = Vector2.zero;
            bbtRt.anchorMax = Vector2.one;
            bbtRt.sizeDelta = Vector2.zero;
        }

        private void BuildStatCard(Transform parent, Font font, float xMin, float xMax, string label, out Text valText, out Text subText, bool isGold = false)
        {
            GameObject sc = new GameObject("Card_" + label, typeof(RectTransform), typeof(Image));
            sc.transform.SetParent(parent, false);
            var scRt = sc.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(xMin, 0.55f);
            scRt.anchorMax = new Vector2(xMax, 0.72f);
            scRt.anchoredPosition = Vector2.zero;
            scRt.sizeDelta = Vector2.zero;
            sc.GetComponent<Image>().color = new Color(0.07f, 0.12f, 0.18f, 0.95f);

            var outline = sc.AddComponent<Outline>();
            outline.effectColor = isGold ? new Color(0.95f, 0.75f, 0.20f, 0.85f) : new Color(0.25f, 0.45f, 0.65f, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Label
            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lblGo.transform.SetParent(sc.transform, false);
            var lbl = lblGo.GetComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 11;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = isGold ? new Color(1.0f, 0.85f, 0.35f) : new Color(0.65f, 0.80f, 0.95f);
            lbl.text = label;
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.65f);
            lRt.anchorMax = new Vector2(1f, 1f);
            lRt.anchoredPosition = Vector2.zero;
            lRt.sizeDelta = Vector2.zero;

            // Value
            GameObject valGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
            valGo.transform.SetParent(sc.transform, false);
            valText = valGo.GetComponent<Text>();
            valText.font = font;
            valText.fontSize = isGold ? 19 : 17;
            valText.fontStyle = FontStyle.Bold;
            valText.alignment = TextAnchor.MiddleCenter;
            valText.color = isGold ? new Color(1f, 0.90f, 0.25f) : Color.white;
            valText.text = "--";
            var vRt = valGo.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0f, 0.20f);
            vRt.anchorMax = new Vector2(1f, 0.70f);
            vRt.anchoredPosition = Vector2.zero;
            vRt.sizeDelta = Vector2.zero;

            // Sub
            GameObject subGo = new GameObject("Sub", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(sc.transform, false);
            subText = subGo.GetComponent<Text>();
            subText.font = font;
            subText.fontSize = 10;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.45f, 0.85f, 0.70f);
            subText.text = "";
            var sRt = subGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 0.25f);
            sRt.anchoredPosition = Vector2.zero;
            sRt.sizeDelta = Vector2.zero;
        }

        private void BuildChampionshipPodiumModal(Transform parent, Font font)
        {
            championshipModal = new GameObject("Championship_Podium_Modal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            championshipModal.transform.SetParent(parent, false);
            var rt = championshipModal.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            championshipModal.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.92f);

            // Dialog Card
            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(championshipModal.transform, false);
            var cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(920f, 580f);

            card.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.12f, 0.98f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(1.0f, 0.82f, 0.20f, 0.98f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Crown / Title
            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(card.transform, false);
            var t = title.GetComponent<Text>();
            t.font = font;
            t.fontSize = 25;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.85f, 0.2f);
            t.text = "🏆 APEX SUNDARBAN GUARDIAN CHAMPION 🏆";
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.89f);
            tRt.anchorMax = new Vector2(0.95f, 0.98f);
            tRt.sizeDelta = Vector2.zero;

            GameObject sub = new GameObject("Sub", typeof(RectTransform), typeof(Text));
            sub.transform.SetParent(card.transform, false);
            var st = sub.GetComponent<Text>();
            st.font = font;
            st.fontSize = 13;
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.color = new Color(0.35f, 0.95f, 0.85f);
            st.text = "ALL 4 SPECIES SAFELY PRESERVED • CONSERVATION TOURNAMENT COMPLETED!";
            var stRt = sub.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.05f, 0.83f);
            stRt.anchorMax = new Vector2(0.95f, 0.89f);
            stRt.sizeDelta = Vector2.zero;

            // Grand Summary Row
            GameObject summBox = new GameObject("Summary_Box", typeof(RectTransform), typeof(Image));
            summBox.transform.SetParent(card.transform, false);
            var sbRt = summBox.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.05f, 0.69f);
            sbRt.anchorMax = new Vector2(0.95f, 0.82f);
            sbRt.sizeDelta = Vector2.zero;
            summBox.GetComponent<Image>().color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

            GameObject gtxtGo = new GameObject("Grade_Text", typeof(RectTransform), typeof(Text));
            gtxtGo.transform.SetParent(summBox.transform, false);
            tourneyGradeText = gtxtGo.GetComponent<Text>();
            tourneyGradeText.font = font;
            tourneyGradeText.fontSize = 17;
            tourneyGradeText.fontStyle = FontStyle.Bold;
            tourneyGradeText.alignment = TextAnchor.MiddleCenter;
            tourneyGradeText.color = new Color(0.35f, 0.98f, 0.65f);
            tourneyGradeText.text = "CHAMPION OF THE SUNDARBANS (SSS-TIER)";
            var gtRt = gtxtGo.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0f, 0.5f);
            gtRt.anchorMax = new Vector2(1f, 1f);
            gtRt.sizeDelta = Vector2.zero;

            GameObject statTxtGo = new GameObject("Stats_Text", typeof(RectTransform), typeof(Text));
            statTxtGo.transform.SetParent(summBox.transform, false);
            tourneyTotalScoreText = statTxtGo.GetComponent<Text>();
            tourneyTotalScoreText.font = font;
            tourneyTotalScoreText.fontSize = 13;
            tourneyTotalScoreText.fontStyle = FontStyle.Bold;
            tourneyTotalScoreText.alignment = TextAnchor.MiddleCenter;
            tourneyTotalScoreText.color = Color.white;
            tourneyTotalScoreText.text = "TOTAL TIME: 04:32.18   |   TOURNAMENT SCORE: 10,480 PTS   |   STARS: 12 / 12 ★";
            var sttRt = statTxtGo.GetComponent<RectTransform>();
            sttRt.anchorMin = new Vector2(0f, 0f);
            sttRt.anchorMax = new Vector2(1f, 0.5f);
            sttRt.sizeDelta = Vector2.zero;

            // 4 Rounds Matrix Container
            GameObject matrixRoot = new GameObject("Matrix_Root", typeof(RectTransform));
            matrixRoot.transform.SetParent(card.transform, false);
            tourneyCardsContainer = matrixRoot.transform;
            var mRt = matrixRoot.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.05f, 0.16f);
            mRt.anchorMax = new Vector2(0.95f, 0.66f);
            mRt.sizeDelta = Vector2.zero;

            // Action Buttons
            GameObject btnRestart = new GameObject("Btn_Restart_Tourney", typeof(RectTransform), typeof(Image), typeof(Button));
            btnRestart.transform.SetParent(card.transform, false);
            var brtRt = btnRestart.GetComponent<RectTransform>();
            brtRt.anchorMin = new Vector2(0.52f, 0.035f);
            brtRt.anchorMax = new Vector2(0.95f, 0.125f);
            brtRt.sizeDelta = Vector2.zero;
            btnRestart.GetComponent<Image>().color = new Color(0.12f, 0.70f, 0.40f, 0.98f);
            btnRestartTourneyAction = btnRestart.GetComponent<Button>();
            btnRestartTourneyAction.onClick.AddListener(() =>
            {
                CloseChampionshipPodium();
                if (CompetitiveRoundManager.Instance != null)
                {
                    CompetitiveRoundManager.Instance.LaunchRound(0);
                }
            });

            GameObject brTxt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            brTxt.transform.SetParent(btnRestart.transform, false);
            var brt = brTxt.GetComponent<Text>();
            brt.font = font;
            brt.fontSize = 14;
            brt.fontStyle = FontStyle.Bold;
            brt.alignment = TextAnchor.MiddleCenter;
            brt.color = Color.white;
            brt.text = "RESTART TOURNAMENT ↺";
            var brtTextRt = brTxt.GetComponent<RectTransform>();
            brtTextRt.anchorMin = Vector2.zero;
            brtTextRt.anchorMax = Vector2.one;
            brtTextRt.sizeDelta = Vector2.zero;

            GameObject btnSelect = new GameObject("Btn_Tourney_Select", typeof(RectTransform), typeof(Image), typeof(Button));
            btnSelect.transform.SetParent(card.transform, false);
            var bsRt = btnSelect.GetComponent<RectTransform>();
            bsRt.anchorMin = new Vector2(0.05f, 0.035f);
            bsRt.anchorMax = new Vector2(0.48f, 0.125f);
            bsRt.sizeDelta = Vector2.zero;
            btnSelect.GetComponent<Image>().color = new Color(0.15f, 0.38f, 0.60f, 0.95f);
            btnTourneyRoundSelectAction = btnSelect.GetComponent<Button>();
            btnTourneyRoundSelectAction.onClick.AddListener(() =>
            {
                CloseChampionshipPodium();
                OpenRoundSelectorModal();
            });

            GameObject bsTxt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            bsTxt.transform.SetParent(btnSelect.transform, false);
            var bst = bsTxt.GetComponent<Text>();
            bst.font = font;
            bst.fontSize = 14;
            bst.fontStyle = FontStyle.Bold;
            bst.alignment = TextAnchor.MiddleCenter;
            bst.color = Color.white;
            bst.text = "SELECT ROUND 🗺";
            var bsTextRt = bsTxt.GetComponent<RectTransform>();
            bsTextRt.anchorMin = Vector2.zero;
            bsTextRt.anchorMax = Vector2.one;
            bsTextRt.sizeDelta = Vector2.zero;
        }

        private void BuildRoundSelectorModal(Transform parent, Font font)
        {
            roundSelectorModal = new GameObject("Round_Selector_Modal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            roundSelectorModal.transform.SetParent(parent, false);
            var rt = roundSelectorModal.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            roundSelectorModal.GetComponent<Image>().color = new Color(0.01f, 0.03f, 0.06f, 0.90f);

            // Dialog Card
            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(roundSelectorModal.transform, false);
            var cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(980f, 570f);

            card.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.13f, 0.98f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.20f, 0.75f, 0.90f, 0.95f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            // Header
            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(card.transform, false);
            var t = title.GetComponent<Text>();
            t.font = font;
            t.fontSize = 24;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.98f, 0.88f, 0.35f);
            t.text = "SUNDARBAN TOURNAMENT — ROUND SELECTION";
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.90f);
            tRt.anchorMax = new Vector2(0.95f, 0.98f);
            tRt.sizeDelta = Vector2.zero;

            GameObject sub = new GameObject("Sub", typeof(RectTransform), typeof(Text));
            sub.transform.SetParent(card.transform, false);
            var st = sub.GetComponent<Text>();
            st.font = font;
            st.fontSize = 12;
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.color = new Color(0.65f, 0.85f, 0.95f);
            st.text = "Select a round to challenge your best time, earn 3 stars, and dominate the leaderboard";
            var stRt = sub.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.05f, 0.84f);
            stRt.anchorMax = new Vector2(0.95f, 0.90f);
            stRt.sizeDelta = Vector2.zero;

            // Cards Container
            GameObject container = new GameObject("Cards_Container", typeof(RectTransform));
            container.transform.SetParent(card.transform, false);
            roundCardsContainer = container.transform;
            var cnRt = container.GetComponent<RectTransform>();
            cnRt.anchorMin = new Vector2(0.03f, 0.13f);
            cnRt.anchorMax = new Vector2(0.97f, 0.83f);
            cnRt.sizeDelta = Vector2.zero;

            // Close Button
            GameObject btnClose = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            btnClose.transform.SetParent(card.transform, false);
            var bcRt = btnClose.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.38f, 0.03f);
            bcRt.anchorMax = new Vector2(0.62f, 0.105f);
            bcRt.sizeDelta = Vector2.zero;
            btnClose.GetComponent<Image>().color = new Color(0.20f, 0.25f, 0.32f, 0.95f);
            btnCloseRoundSelector = btnClose.GetComponent<Button>();
            btnCloseRoundSelector.onClick.AddListener(CloseRoundSelectorModal);

            GameObject bcTxt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            bcTxt.transform.SetParent(btnClose.transform, false);
            var bct = bcTxt.GetComponent<Text>();
            bct.font = font;
            bct.fontSize = 13;
            bct.fontStyle = FontStyle.Bold;
            bct.alignment = TextAnchor.MiddleCenter;
            bct.color = Color.white;
            bct.text = "✕ RESUME GAME";
            var bctRt = bcTxt.GetComponent<RectTransform>();
            bctRt.anchorMin = Vector2.zero;
            bctRt.anchorMax = Vector2.one;
            bctRt.sizeDelta = Vector2.zero;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // ⚡ COMPETITIVE ROUND LOGIC & EVENT HANDLERS
        // ──────────────────────────────────────────────────────────────────────────

        public void UpdateCompetitiveHUD(RoundConfig config)
        {
            if (config == null) return;

            if (roundBadgeText != null)
            {
                roundBadgeText.text = $"{config.roundNumberText} • {config.speciesName.ToUpper()}";
            }
            if (roundBadgeBg != null)
            {
                roundBadgeBg.color = config.themeColor;
            }
            if (roundParText != null)
            {
                roundParText.text = $"PAR {CompetitiveRoundManager.FormatTime(config.parTime)}";
            }
        }

        private void OnCompetitiveRoundStarted(RoundConfig config)
        {
            UpdateCompetitiveHUD(config);
            if (roundTimerText != null)
            {
                roundTimerText.text = "⏱ 00:00.00";
                roundTimerText.color = new Color(1.0f, 0.92f, 0.40f);
            }
        }

        private void OnCompetitiveRoundTimerTick(float elapsed, float par)
        {
            if (roundTimerText != null)
            {
                roundTimerText.text = $"⏱ {CompetitiveRoundManager.FormatTime(elapsed)}";

                // Green if under par, orange if close, red if overtime
                if (elapsed <= par * 0.85f)
                    roundTimerText.color = new Color(0.35f, 0.95f, 0.55f);
                else if (elapsed <= par)
                    roundTimerText.color = new Color(1.0f, 0.85f, 0.25f);
                else
                    roundTimerText.color = new Color(0.95f, 0.35f, 0.35f);
            }
        }

        public void ShowCompetitiveRoundResults(RoundScoreResult result)
        {
            if (roundResultsModal == null) return;

            // Fill header & titles
            if (resultTitleText != null)
            {
                resultTitleText.text = $"🏆 {result.config.roundNumberText} COMPLETE!";
                resultTitleText.color = result.config.themeColor;
            }
            if (resultSubtitleText != null)
            {
                resultSubtitleText.text = $"{result.config.speciesName.ToUpper()} • {result.config.missionTitle.ToUpper()}";
            }

            // Stars string
            if (resultStarsText != null)
            {
                string sStr = "";
                for (int i = 0; i < 3; i++) sStr += (i < result.starsEarned) ? "★" : "☆";
                resultStarsText.text = sStr;
            }

            if (resultRankBadgeText != null)
            {
                resultRankBadgeText.text = $"{result.rankTitle} (RANK {result.rankGrade})";
            }

            // Stat values
            if (resultTimeValueText != null) resultTimeValueText.text = CompetitiveRoundManager.FormatTime(result.timeTaken);
            if (resultParValueText != null) resultParValueText.text = $"PAR: {CompetitiveRoundManager.FormatTime(result.parTime)}";
            if (resultTimeBonusText != null) resultTimeBonusText.text = $"+{result.timeBonus} PTS";
            if (resultVitalityBonusText != null) resultVitalityBonusText.text = $"+{result.vitalityBonus} PTS";
            if (resultScoreValueText != null) resultScoreValueText.text = $"{result.totalScore:N0} PTS";

            // PB Badge
            if (resultNewRecordBadge != null)
            {
                resultNewRecordBadge.SetActive(result.isNewBestTime || result.isNewHighScore);
            }

            // Populate Leaderboard rows
            if (leaderboardRowsContainer != null)
            {
                for (int i = leaderboardRowsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(leaderboardRowsContainer.GetChild(i).gameObject);
                }

                var board = CompetitiveRoundManager.Instance.GetLeaderboardForRound(result.roundIndex, result.totalScore, result.timeTaken);
                Font f = cachedDefaultFont != null ? cachedDefaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                float rowH = 26f;
                for (int i = 0; i < board.Count; i++)
                {
                    var entry = board[i];
                    GameObject row = new GameObject("Row_" + i, typeof(RectTransform), typeof(Image));
                    row.transform.SetParent(leaderboardRowsContainer, false);
                    var rRt = row.GetComponent<RectTransform>();
                    rRt.anchorMin = new Vector2(0f, 1f - ((i + 1) * 0.23f));
                    rRt.anchorMax = new Vector2(1f, 1f - (i * 0.23f));
                    rRt.anchoredPosition = Vector2.zero;
                    rRt.sizeDelta = Vector2.zero;

                    var rImg = row.GetComponent<Image>();
                    rImg.color = entry.isPlayer ? new Color(0.12f, 0.45f, 0.28f, 0.85f) : new Color(0.04f, 0.08f, 0.12f, 0.65f);

                    if (entry.isPlayer)
                    {
                        var rowOutline = row.AddComponent<Outline>();
                        rowOutline.effectColor = new Color(0.35f, 0.95f, 0.65f, 0.95f);
                        rowOutline.effectDistance = new Vector2(1.5f, -1.5f);
                    }

                    GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    txtGo.transform.SetParent(row.transform, false);
                    var txt = txtGo.GetComponent<Text>();
                    txt.font = f;
                    txt.fontSize = 11;
                    txt.fontStyle = entry.isPlayer ? FontStyle.Bold : FontStyle.Normal;
                    txt.alignment = TextAnchor.MiddleLeft;
                    txt.color = entry.isPlayer ? new Color(1f, 0.92f, 0.35f) : new Color(0.85f, 0.90f, 0.95f);
                    string status = entry.isPlayer ? "★ YOU (ACTIVE)" : "VERIFIED RANGER";
                    txt.text = $"  #{entry.rank}     {entry.rangerName,-28}   {CompetitiveRoundManager.FormatTime(entry.timeTaken),-10}   {entry.score,6:N0} PTS   {status}";
                    var tRt = txtGo.GetComponent<RectTransform>();
                    tRt.anchorMin = Vector2.zero;
                    tRt.anchorMax = Vector2.one;
                    tRt.sizeDelta = Vector2.zero;
                }
            }

            // Next button text
            if (btnNextRoundText != null)
            {
                if (result.roundIndex < 3)
                {
                    var nextCfg = CompetitiveRoundManager.Instance.GetConfig(result.roundIndex + 1);
                    btnNextRoundText.text = $"NEXT ROUND: {nextCfg.speciesName.ToUpper()} ▶";
                }
                else
                {
                    btnNextRoundText.text = "CLAIM CHAMPIONSHIP PODIUM 🏆 ▶";
                }
            }

            roundResultsModal.transform.SetAsLastSibling();
            roundResultsModal.SetActive(true);
        }

        public void CloseRoundResultsModal()
        {
            if (roundResultsModal != null)
            {
                roundResultsModal.SetActive(false);
            }
        }

        public void ShowChampionshipPodium(TournamentResult result)
        {
            if (championshipModal == null) return;

            if (tourneyGradeText != null)
            {
                tourneyGradeText.text = result.overallGrade;
            }

            if (tourneyTotalScoreText != null)
            {
                tourneyTotalScoreText.text = $"TOTAL TIME: {CompetitiveRoundManager.FormatTime(result.totalTournamentTime)}   |   TOURNAMENT SCORE: {result.totalTournamentScore:N0} PTS   |   STARS: {result.totalStars} / 12 ★";
            }

            // Populate 4 cards
            if (tourneyCardsContainer != null)
            {
                for (int i = tourneyCardsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(tourneyCardsContainer.GetChild(i).gameObject);
                }

                Font f = cachedDefaultFont != null ? cachedDefaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                float[] xMins = { 0f, 0.26f, 0.52f, 0.77f };
                float[] xMaxs = { 0.23f, 0.49f, 0.74f, 1.0f };

                for (int i = 0; i < 4; i++)
                {
                    var cfg = CompetitiveRoundManager.Instance.GetConfig(i);
                    var rec = CompetitiveRoundManager.Instance.GetRecord(i);

                    GameObject card = new GameObject("RoundCard_" + i, typeof(RectTransform), typeof(Image));
                    card.transform.SetParent(tourneyCardsContainer, false);
                    var crt = card.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(xMins[i], 0f);
                    crt.anchorMax = new Vector2(xMaxs[i], 1f);
                    crt.anchoredPosition = Vector2.zero;
                    crt.sizeDelta = Vector2.zero;
                    card.GetComponent<Image>().color = new Color(0.06f, 0.10f, 0.16f, 0.95f);

                    var cardOutline = card.AddComponent<Outline>();
                    cardOutline.effectColor = cfg.themeColor;
                    cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

                    GameObject ctGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    ctGo.transform.SetParent(card.transform, false);
                    var ct = ctGo.GetComponent<Text>();
                    ct.font = f;
                    ct.fontSize = 11;
                    ct.fontStyle = FontStyle.Bold;
                    ct.alignment = TextAnchor.MiddleCenter;
                    ct.color = Color.white;

                    string stars = "";
                    for (int s = 0; s < 3; s++) stars += (s < rec.stars) ? "★" : "☆";

                    ct.text = $"{cfg.roundNumberText}\n<color=#{ColorUtility.ToHtmlStringRGB(cfg.themeColor)}>{cfg.speciesName}</color>\n\n" +
                              $"BEST TIME:\n<b>{CompetitiveRoundManager.FormatTime(rec.bestTime)}</b>\n\n" +
                              $"HIGH SCORE:\n<b>{rec.highScore:N0} PTS</b>\n\n" +
                              $"RATING: <color=yellow>{stars}</color>";

                    var ctRt = ctGo.GetComponent<RectTransform>();
                    ctRt.anchorMin = Vector2.zero;
                    ctRt.anchorMax = Vector2.one;
                    ctRt.sizeDelta = Vector2.zero;
                }
            }

            championshipModal.transform.SetAsLastSibling();
            championshipModal.SetActive(true);
        }

        public void CloseChampionshipPodium()
        {
            if (championshipModal != null)
            {
                championshipModal.SetActive(false);
            }
        }

        public void ToggleRoundSelectorModal()
        {
            if (roundSelectorModal == null) return;
            if (roundSelectorModal.activeSelf) CloseRoundSelectorModal();
            else OpenRoundSelectorModal();
        }

        public void OpenRoundSelectorModal()
        {
            if (roundSelectorModal == null) return;

            RefreshRoundSelectorCards();
            roundSelectorModal.transform.SetAsLastSibling();
            roundSelectorModal.SetActive(true);
        }

        public void CloseRoundSelectorModal()
        {
            if (roundSelectorModal != null)
            {
                roundSelectorModal.SetActive(false);
            }
        }

        private void RefreshRoundSelectorCards()
        {
            if (roundCardsContainer == null) return;

            for (int i = roundCardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(roundCardsContainer.GetChild(i).gameObject);
            }

            Font f = cachedDefaultFont != null ? cachedDefaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float[] xMins = { 0f, 0.26f, 0.52f, 0.77f };
            float[] xMaxs = { 0.23f, 0.49f, 0.74f, 1.0f };

            for (int i = 0; i < 4; i++)
            {
                int rIndex = i;
                var cfg = CompetitiveRoundManager.Instance.GetConfig(i);
                var rec = CompetitiveRoundManager.Instance.GetRecord(i);
                bool isActive = (CompetitiveRoundManager.ActiveRoundIndex == i);

                GameObject card = new GameObject("SelectCard_" + i, typeof(RectTransform), typeof(Image));
                card.transform.SetParent(roundCardsContainer, false);
                var crt = card.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(xMins[i], 0f);
                crt.anchorMax = new Vector2(xMaxs[i], 1f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = Vector2.zero;

                card.GetComponent<Image>().color = isActive ? new Color(0.08f, 0.16f, 0.24f, 0.98f) : new Color(0.05f, 0.09f, 0.14f, 0.95f);

                var cardOutline = card.AddComponent<Outline>();
                cardOutline.effectColor = isActive ? Color.white : cfg.themeColor;
                cardOutline.effectDistance = isActive ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);

                // Round Header
                GameObject hGo = new GameObject("Header", typeof(RectTransform), typeof(Text));
                hGo.transform.SetParent(card.transform, false);
                var ht = hGo.GetComponent<Text>();
                ht.font = f;
                ht.fontSize = 13;
                ht.fontStyle = FontStyle.Bold;
                ht.alignment = TextAnchor.MiddleCenter;
                ht.color = cfg.themeColor;
                ht.text = $"{cfg.roundNumberText}\n{cfg.speciesName}";
                var htRt = hGo.GetComponent<RectTransform>();
                htRt.anchorMin = new Vector2(0.04f, 0.75f);
                htRt.anchorMax = new Vector2(0.96f, 0.98f);
                htRt.sizeDelta = Vector2.zero;

                // Summary
                GameObject sGo = new GameObject("Summary", typeof(RectTransform), typeof(Text));
                sGo.transform.SetParent(card.transform, false);
                var st = sGo.GetComponent<Text>();
                st.font = f;
                st.fontSize = 10;
                st.fontStyle = FontStyle.Normal;
                st.alignment = TextAnchor.MiddleCenter;
                st.color = new Color(0.75f, 0.85f, 0.92f);
                st.text = cfg.missionSummary;
                var stRt = sGo.GetComponent<RectTransform>();
                stRt.anchorMin = new Vector2(0.04f, 0.48f);
                stRt.anchorMax = new Vector2(0.96f, 0.75f);
                stRt.sizeDelta = Vector2.zero;

                // Records Box
                GameObject recGo = new GameObject("Records", typeof(RectTransform), typeof(Text));
                recGo.transform.SetParent(card.transform, false);
                var rtTxt = recGo.GetComponent<Text>();
                rtTxt.font = f;
                rtTxt.fontSize = 11;
                rtTxt.fontStyle = FontStyle.Bold;
                rtTxt.alignment = TextAnchor.MiddleCenter;
                rtTxt.color = Color.white;

                string stars = "";
                for (int s = 0; s < 3; s++) stars += (s < rec.stars) ? "★" : "☆";

                rtTxt.text = $"PAR TIME: {CompetitiveRoundManager.FormatTime(cfg.parTime)}\n" +
                             $"PB: <color=yellow>{(rec.bestTime > 0f ? CompetitiveRoundManager.FormatTime(rec.bestTime) : "--:--")}</color>\n" +
                             $"HIGH SCORE: <color=yellow>{rec.highScore:N0}</color>\n" +
                             $"RATING: <color=yellow>{stars}</color>";

                var rtRt = recGo.GetComponent<RectTransform>();
                rtRt.anchorMin = new Vector2(0.04f, 0.20f);
                rtRt.anchorMax = new Vector2(0.96f, 0.48f);
                rtRt.sizeDelta = Vector2.zero;

                // Play Button
                GameObject playBtnGo = new GameObject("Btn_Play", typeof(RectTransform), typeof(Image), typeof(Button));
                playBtnGo.transform.SetParent(card.transform, false);
                var pbRt = playBtnGo.GetComponent<RectTransform>();
                pbRt.anchorMin = new Vector2(0.08f, 0.04f);
                pbRt.anchorMax = new Vector2(0.92f, 0.17f);
                pbRt.sizeDelta = Vector2.zero;

                var pbImg = playBtnGo.GetComponent<Image>();
                pbImg.color = isActive ? new Color(0.20f, 0.55f, 0.85f, 0.95f) : new Color(0.12f, 0.65f, 0.40f, 0.95f);
                var pbOutline = playBtnGo.AddComponent<Outline>();
                pbOutline.effectColor = Color.white;
                pbOutline.effectDistance = new Vector2(1f, -1f);

                var playBtn = playBtnGo.GetComponent<Button>();
                playBtn.onClick.AddListener(() =>
                {
                    CloseRoundSelectorModal();
                    if (CompetitiveRoundManager.Instance != null)
                    {
                        CompetitiveRoundManager.Instance.LaunchRound(rIndex);
                    }
                });

                GameObject pbTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                pbTxtGo.transform.SetParent(playBtnGo.transform, false);
                var pbTxt = pbTxtGo.GetComponent<Text>();
                pbTxt.font = f;
                pbTxt.fontSize = 12;
                pbTxt.fontStyle = FontStyle.Bold;
                pbTxt.alignment = TextAnchor.MiddleCenter;
                pbTxt.color = Color.white;
                pbTxt.text = isActive ? "CURRENT ROUND ▶" : $"PLAY ROUND {rIndex + 1} ▶";
                var pbtRt = pbTxtGo.GetComponent<RectTransform>();
                pbtRt.anchorMin = Vector2.zero;
                pbtRt.anchorMax = Vector2.one;
                pbtRt.sizeDelta = Vector2.zero;
            }
        }

        private void ShowCompetitiveRoundFailed(RoundConfig config, float timeSurvived)
        {
            // CRITICAL: Don't show round failed popup if the character still has HP remaining!
            if (activeHealth != null && activeHealth.currentHealth > 0.01f)
            {
                Debug.LogWarning($"[HUD] Suppressed false RoundFailed popup because active player has {activeHealth.currentHealth:F1} HP remaining.");
                return;
            }

            if (factCardPanel != null)
            {
                ShowEndCard($"ROUND FAILED: {config.speciesName.ToUpper()}", new Color(0.92f, 0.18f, 0.18f));
            }
        }
    }

    /// <summary>
    /// Fallback click/touch handler attached directly to the NASA button.
    /// Guarantees that whether EventSystem clicks, pointer down, or pointer clicks fire,
    /// the NASA Learning Hub opens without relying solely on uGUI Button.onClick.
    /// </summary>
    public class NasaPopupDirectPointer : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public System.Action onTrigger;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            onTrigger?.Invoke();
        }
    }

    /// <summary>
    /// Right-click pointer handler for dropping tools directly from the HUD slot.
    /// </summary>
    public class ToolSlotPointerDrop : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public System.Action onRightClick;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
            {
                onRightClick?.Invoke();
            }
        }
    }

    /// <summary>
    /// Direct instant-response pointer handler for action buttons (Jump, Fire, Auto-Run).
    /// Fires immediately on PointerDown (touch/mouse press) with zero drag lag or dropped clicks.
    /// </summary>
    public class ActionPointerHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerClickHandler
    {
        public System.Action onAction;
        private float lastTriggerTime = -1f;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            Trigger();
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            Trigger();
        }

        private void Trigger()
        {
            if (Time.time - lastTriggerTime < 0.15f) return;
            lastTriggerTime = Time.time;
            onAction?.Invoke();
        }
    }
}
