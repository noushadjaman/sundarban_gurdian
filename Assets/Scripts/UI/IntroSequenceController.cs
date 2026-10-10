using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

namespace SundarbanGuardian.UI
{
    /// <summary>
    /// Full-screen animated intro sequence showing real 2000-vs-now Sundarbans ecological data.
    /// Hybrid approach: All data/statistic slides, headlines, and animated counters use English with numerals (0-9)
    /// to guarantee 100% crystal-clear typography for competition judges without text-shaping artifacts.
    /// Features Ken Burns orbital/cinematic pan-and-zoom motion, authentic NASA space Earth observation backgrounds
    /// for NASA data, real Sundarbans wildlife/river photography, and synchronized Timeline cutscene sequences.
    /// 
    /// GUARANTEED LEVEL / SLIDE ISOLATION:
    /// Each slide (0..5) is 100% isolated. No slide ever renders or overlaps on top of another.
    /// Dedicated animation sequences are implemented for each individual level/slide.
    /// </summary>
    public class IntroSequenceController : MonoBehaviour
    {
        public static IntroSequenceController Instance { get; private set; }

        [Header("Configuration")]
        public bool playOnStart = true;
        public bool forceReplayForTesting = false;

        private Canvas introCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform backgroundRect;
        private RawImage backgroundImage;
        private RawImage backgroundSecondaryImage;
        private CanvasGroup backgroundSecondaryCg;
        private GameObject orbitalTelemetryBadge;
        private Text orbitalTelemetryText;

        // Background Textures (Authentic NASA & Real Sundarban Photography)
        private Texture2D nasaSpaceTex;
        private Texture2D nasaLandsatTex;
        private Texture2D realSundarbanTigerTex;
        private Texture2D realSundarbanDeerTex;
        private Texture2D realSundarbanRiverTex;
        private Texture2D realSundarbanMangroveTex;
        private Texture2D realSundarbanDolphinTex;
        private Texture2D realSundarbanBoatTex;
        private Texture2D hilshaTex;
        private Texture2D defaultSatTex;

        // UI Sprites (Sleek Modern Glassmorphism & Rounded Styling)
        private Sprite roundedCardSprite;
        private Sprite roundedCapsuleSprite;
        private Sprite gradientScrimSprite;

        // Slide GameObjects & Elements
        private readonly GameObject[] slides = new GameObject[6];
        private int currentSlideIndex = 0;
        private bool isAnimating = false;
        private bool isTransitioning = false;
        private Coroutine currentSlideAnimCoroutine;
        private Coroutine autoAdvanceCoroutine;
        private Coroutine slideTransitionCoroutine;
        private Coroutine kenBurnsCoroutine;

        // Navigation elements
        private Button btnSkip;
        private Button btnNext;
        private Button btnPrev;
        private Image autoAdvanceFill;
        private readonly Image[] stepDots = new Image[6];

        // Slide 1 (Title) animated elements
        private GameObject titleCard;
        private CanvasGroup titleCitationsCg;

        // Slide 2 (Forest Cover) animated elements
        private Text forestPercentText;
        private RectTransform forestBarFill;

        // Slide 3 (Tiger Count) animated elements
        private Text tigerCountText;
        private Text tigerYearBadge;
        private readonly GameObject[] tigerStepCards = new GameObject[4];

        // Slide 4 (Deer Count) animated elements
        private Text deerCountText;
        private RectTransform deerBarFill;

        // Slide 5 (Dolphin Count) animated elements
        private Text dolphinGangesCountText;
        private RectTransform dolphinGangesBarFill;
        private Text dolphinIrrawaddyCountText;
        private RectTransform dolphinIrrawaddyBarFill;
        private GameObject dolphinQuoteBox;

        // Slide 6 (Mission Briefing) animated elements
        private readonly GameObject[] speciesCards = new GameObject[4];
        private Button btnStartMission;

        // Font
        private Font mainFont;

        public bool IsIntroActive { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            SundarbanGuardian.Core.HudController.EnsureEventSystem();
            LoadFont();
            LoadBackgroundTextures();
            LoadUISprites();
            EnsureUIBuilt();
        }

        private void Start()
        {
            bool hasSeen = PlayerPrefs.GetInt("HasSeenIntroDataVideo", 0) == 1;
            if ((playOnStart && !hasSeen) || forceReplayForTesting)
            {
                StartCoroutine(DelayedStartIntro());
            }
            else
            {
                if (introCanvas != null) introCanvas.gameObject.SetActive(false);
            }
        }

        private IEnumerator DelayedStartIntro()
        {
            yield return null;
            PlayIntro();
        }

        private void Update()
        {
            if (!IsIntroActive) return;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    NextSlide();
                }
                else if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SkipIntro();
                }
            }
#endif

            // Breathing scale pulse on Next button for clear mobile interaction cue
            if (btnNext != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.025f;
                btnNext.transform.localScale = new Vector3(pulse, pulse, 1f);
            }

            // Breathing pulse on active slide's primary focal card
            if (currentSlideIndex >= 0 && currentSlideIndex < slides.Length && slides[currentSlideIndex] != null)
            {
                var mainCard = slides[currentSlideIndex].transform.Find("Unified_Card");
                if (mainCard == null) mainCard = slides[currentSlideIndex].transform.Find("Center_Card");
                if (mainCard == null) mainCard = slides[currentSlideIndex].transform.Find("Grand_Title_Card");
                if (mainCard == null) mainCard = slides[currentSlideIndex].transform.Find("Bar_Container");
                if (mainCard == null) mainCard = slides[currentSlideIndex].transform.Find("Glass_Card");

                if (mainCard != null)
                {
                    float cardPulse = 1f + Mathf.Sin(Time.unscaledTime * 2.0f) * 0.006f;
                    mainCard.localScale = new Vector3(cardPulse, cardPulse, 1f);
                }
            }
        }

        public Font LoadFont()
        {
            if (mainFont != null) return mainFont;
            mainFont = Resources.Load<Font>("Fonts/SegoeUI");
            if (mainFont == null) mainFont = Resources.Load<Font>("Fonts/Kalpurush");
#if UNITY_EDITOR
            if (mainFont == null)
                mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SegoeUI.ttf");
            if (mainFont == null)
                mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Kalpurush.ttf");
#endif
            if (mainFont == null)
            {
                try { mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch { }
            }
            return mainFont;
        }

        public void LoadBackgroundTextures()
        {
#if UNITY_EDITOR
            if (nasaSpaceTex == null)
                nasaSpaceTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/nasa_earth_space.jpg");
            if (nasaLandsatTex == null)
                nasaLandsatTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/nasa_sundarbans_landsat.jpg");
            if (realSundarbanTigerTex == null)
                realSundarbanTigerTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_tiger.jpg");
            if (realSundarbanDeerTex == null)
                realSundarbanDeerTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_deer.jpg");
            if (realSundarbanRiverTex == null)
                realSundarbanRiverTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_river.jpg");
            if (realSundarbanMangroveTex == null)
                realSundarbanMangroveTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_mangrove.jpg");
            if (realSundarbanDolphinTex == null)
                realSundarbanDolphinTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_dolphin.jpg");
            if (realSundarbanBoatTex == null)
                realSundarbanBoatTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_boat.jpg");
            if (hilshaTex == null)
                hilshaTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Hilsha_Reference.png");
            if (defaultSatTex == null)
                defaultSatTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sundarban_map.jpeg");
#endif
            if (nasaSpaceTex == null) nasaSpaceTex = Resources.Load<Texture2D>("Textures/nasa_earth_space");
            if (nasaLandsatTex == null) nasaLandsatTex = Resources.Load<Texture2D>("Textures/nasa_sundarbans_landsat");
            if (realSundarbanTigerTex == null) realSundarbanTigerTex = Resources.Load<Texture2D>("Textures/real_sundarban_tiger");
            if (realSundarbanDeerTex == null) realSundarbanDeerTex = Resources.Load<Texture2D>("Textures/real_sundarban_deer");
            if (realSundarbanRiverTex == null) realSundarbanRiverTex = Resources.Load<Texture2D>("Textures/real_sundarban_river");
            if (realSundarbanMangroveTex == null) realSundarbanMangroveTex = Resources.Load<Texture2D>("Textures/real_sundarban_mangrove");
            if (realSundarbanDolphinTex == null) realSundarbanDolphinTex = Resources.Load<Texture2D>("Textures/real_sundarban_dolphin");
            if (realSundarbanBoatTex == null) realSundarbanBoatTex = Resources.Load<Texture2D>("Textures/real_sundarban_boat");
            if (hilshaTex == null) hilshaTex = Resources.Load<Texture2D>("Textures/Hilsha_Reference");
            if (defaultSatTex == null) defaultSatTex = Resources.Load<Texture2D>("Textures/sundarban_map");
        }

        public void LoadUISprites()
        {
#if UNITY_EDITOR
            if (roundedCardSprite == null)
                roundedCardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/rounded_card_12px.png");
            if (roundedCapsuleSprite == null)
                roundedCapsuleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/rounded_capsule_bar.png");
            if (gradientScrimSprite == null)
                gradientScrimSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/cinematic_gradient_scrim.png");
#endif
            if (roundedCardSprite == null) roundedCardSprite = Resources.Load<Sprite>("Textures/UI/rounded_card_12px");
            if (roundedCapsuleSprite == null) roundedCapsuleSprite = Resources.Load<Sprite>("Textures/UI/rounded_capsule_bar");
            if (gradientScrimSprite == null) gradientScrimSprite = Resources.Load<Sprite>("Textures/UI/cinematic_gradient_scrim");
        }

        public Texture2D GetBackgroundTextureForSlide(int index)
        {
            switch (index)
            {
                case 0: // Slide 1 (Title / Citations) -> NASA Earth from Space (ISS)
                    return nasaSpaceTex != null ? nasaSpaceTex : defaultSatTex;
                case 1: // Slide 2 (Mangrove Canopy Loss) -> NASA Landsat 7 delta imagery
                    return nasaLandsatTex != null ? nasaLandsatTex : defaultSatTex;
                case 2: // Slide 3 (Tiger Population) -> Real Royal Bengal Tiger in Sundarbans
                    return realSundarbanTigerTex != null ? realSundarbanTigerTex : defaultSatTex;
                case 3: // Slide 4 (Spotted Deer) -> Real Spotted Deer in Sundarbans
                    return realSundarbanDeerTex != null ? realSundarbanDeerTex : defaultSatTex;
                case 4: // Slide 5 (Dolphin Crisis) -> Real Sundarbans River Estuary
                    return realSundarbanRiverTex != null ? realSundarbanRiverTex : defaultSatTex;
                case 5: // Slide 6 (Mission Overview) -> Real Sundarbans Mangrove Forest
                    return realSundarbanMangroveTex != null ? realSundarbanMangroveTex : (nasaLandsatTex != null ? nasaLandsatTex : defaultSatTex);
                default:
                    return defaultSatTex;
            }
        }

        /// <summary>
        /// Strictly isolates a slide so that only 'activeIndex' is visible and interactive.
        /// ALL other 5 slides are disabled (SetActive(false) and alpha = 0).
        /// </summary>
        public void IsolateSlide(int activeIndex)
        {
            EnsureSlideReferences();

            for (int i = 0; i < 6; i++)
            {
                if (slides[i] != null)
                {
                    bool isActive = (i == activeIndex);
                    slides[i].SetActive(isActive);
                    var cg = slides[i].GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = isActive ? 1f : 0f;
                        cg.interactable = isActive;
                        cg.blocksRaycasts = isActive;
                    }
                    var rt = slides[i].GetComponent<RectTransform>();
                    if (rt != null && isActive)
                    {
                        rt.anchoredPosition = Vector2.zero;
                        rt.localScale = Vector3.one;
                    }
                }
            }
        }

        public void PlayIntro()
        {
            EnsureUIBuilt();
            EnsureSlideReferences();
            IsolateSlide(0);

            if (introCanvas != null)
                introCanvas.gameObject.SetActive(true);

            IsIntroActive = true;
            Time.timeScale = 0f; // Pause gameplay while viewing data intro

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            ShowSlide(0);
        }

        public void SkipIntro()
        {
            PlayerPrefs.SetInt("HasSeenIntroDataVideo", 1);
            PlayerPrefs.Save();
            StartCoroutine(FadeOutAndFinish());
        }

        public void NextSlide()
        {
            if (isAnimating)
            {
                CompleteCurrentAnimationEarly();
                return;
            }

            if (currentSlideIndex < 5)
            {
                ShowSlide(currentSlideIndex + 1);
            }
            else
            {
                SkipIntro();
            }
        }

        public void PreviousSlide()
        {
            if (currentSlideIndex > 0)
            {
                ShowSlide(currentSlideIndex - 1);
            }
        }

        public void EnsureSlideReferences()
        {
            GameObject canvasGo = GameObject.Find("Intro_Data_Canvas");
            if (canvasGo == null) return;

            SundarbanGuardian.Core.HudController.EnsureEventSystem();

            Font f = LoadFont();
            var allTexts = canvasGo.GetComponentsInChildren<Text>(true);
            foreach (var t in allTexts)
            {
                if (t.font == null && f != null)
                {
                    t.font = f;
                }
                t.raycastTarget = false;
            }

            // Disable raycast target on non-interactive backdrop and decorative overlays
            Transform bgSat = canvasGo.transform.Find("Background_Satellite");
            if (bgSat != null)
            {
                var rimg = bgSat.GetComponent<RawImage>();
                if (rimg != null) rimg.raycastTarget = false;
                var sec = bgSat.Find("Background_Secondary");
                if (sec != null)
                {
                    var secRimg = sec.GetComponent<RawImage>();
                    if (secRimg != null) secRimg.raycastTarget = false;
                }
            }
            Transform scrim = canvasGo.transform.Find("Cinematic_Gradient_Scrim");
            if (scrim != null)
            {
                var img = scrim.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
            Transform vig = canvasGo.transform.Find("Vignette_Overlay");
            if (vig != null)
            {
                var img = vig.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
            Transform autoAdv = canvasGo.transform.Find("AutoAdvance_Progress_Track");
            if (autoAdv != null)
            {
                var img = autoAdv.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
                var fill = autoAdv.Find("Fill");
                if (fill != null)
                {
                    var fImg = fill.GetComponent<Image>();
                    if (fImg != null) fImg.raycastTarget = false;
                }
            }

            string[] slideNames = {
                "Slide_1_Title",
                "Slide_2_ForestDecline",
                "Slide_3_TigerCount",
                "Slide_4_DeerCount",
                "Slide_5_DolphinCrisis",
                "Slide_6_Transition"
            };

            for (int i = 0; i < 6; i++)
            {
                var tr = canvasGo.transform.Find(slideNames[i]);
                if (tr != null)
                {
                    slides[i] = tr.gameObject;
                }
            }

            // Slide 1 elements
            if (slides[0] != null)
            {
                var tCard = slides[0].transform.Find("Grand_Title_Card");
                if (tCard != null) titleCard = tCard.gameObject;
                var cCard = slides[0].transform.Find("Citations_Card");
                if (cCard != null) titleCitationsCg = cCard.GetComponent<CanvasGroup>();
            }

            // Slide 2 elements
            if (slides[1] != null)
            {
                var texts = slides[1].GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.text == "94.2%" || t.text.Contains("%")) forestPercentText = t;
                }
                var imgs = slides[1].GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject.name == "Bar_Fill") forestBarFill = img.rectTransform;
                }
            }

            // Slide 3 elements
            if (slides[2] != null)
            {
                var texts = slides[2].GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.text == "440" || t.text == "125" || t.text == "106") tigerCountText = t;
                    if (t.text.Contains("Historical Estimate") || t.text.Contains("Survey") || t.text.Contains("Rebound")) tigerYearBadge = t;
                }
                for (int i = 0; i < 4; i++)
                {
                    var st = slides[2].transform.Find($"Milestone_Step_{i}");
                    if (st != null) tigerStepCards[i] = st.gameObject;
                }
            }

            // Slide 4 elements
            if (slides[3] != null)
            {
                var texts = slides[3].GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.text == "83,000" || t.text == "140,357") deerCountText = t;
                }
                var imgs = slides[3].GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject.name == "Bar_Fill") deerBarFill = img.rectTransform;
                }
            }

            // Slide 5 elements
            if (slides[4] != null)
            {
                var gCard = slides[4].transform.Find("Card_Ganges");
                if (gCard != null)
                {
                    var texts = gCard.GetComponentsInChildren<Text>(true);
                    foreach (var t in texts)
                    {
                        if (t.text == "225" || t.text == "159") dolphinGangesCountText = t;
                    }
                    var tr = gCard.Find("G_Track/G_Fill");
                    if (tr != null) dolphinGangesBarFill = tr.GetComponent<RectTransform>();
                }
                var iCard = slides[4].transform.Find("Card_Irrawaddy");
                if (iCard != null)
                {
                    var texts = iCard.GetComponentsInChildren<Text>(true);
                    foreach (var t in texts)
                    {
                        if (t.text == "451" || t.text == "198") dolphinIrrawaddyCountText = t;
                    }
                    var tr = iCard.Find("I_Track/I_Fill");
                    if (tr != null) dolphinIrrawaddyBarFill = tr.GetComponent<RectTransform>();
                }
                var qBox = slides[4].transform.Find("Quote_Callout_Box");
                if (qBox != null) dolphinQuoteBox = qBox.gameObject;
            }

            // Slide 6 elements
            if (slides[5] != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var sc = slides[5].transform.Find($"Species_Card_{i}");
                    if (sc != null) speciesCards[i] = sc.gameObject;
                }
                var bsm = slides[5].transform.Find("Btn_Start_Mission");
                if (bsm != null)
                {
                    btnStartMission = bsm.GetComponent<Button>();
                    if (btnStartMission != null)
                    {
                        btnStartMission.onClick.RemoveAllListeners();
                        btnStartMission.onClick.AddListener(SkipIntro);
                        var img = btnStartMission.GetComponent<Image>();
                        if (img != null) img.raycastTarget = true;
                    }
                }
            }

            // Navigation elements
            if (btnNext == null)
            {
                var bNext = canvasGo.transform.Find("Bottom_Nav_Bar/Btn_Next");
                if (bNext != null) btnNext = bNext.GetComponent<Button>();
            }
            if (btnNext != null)
            {
                btnNext.onClick.RemoveAllListeners();
                btnNext.onClick.AddListener(NextSlide);
                var img = btnNext.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
            }

            if (btnPrev == null)
            {
                var bPrev = canvasGo.transform.Find("Bottom_Nav_Bar/Btn_Prev");
                if (bPrev != null) btnPrev = bPrev.GetComponent<Button>();
            }
            if (btnPrev != null)
            {
                btnPrev.onClick.RemoveAllListeners();
                btnPrev.onClick.AddListener(PreviousSlide);
                var img = btnPrev.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
            }

            if (btnSkip == null)
            {
                var bSkip = canvasGo.transform.Find("Btn_Skip");
                if (bSkip != null) btnSkip = bSkip.GetComponent<Button>();
            }
            if (btnSkip != null)
            {
                btnSkip.onClick.RemoveAllListeners();
                btnSkip.onClick.AddListener(SkipIntro);
                var img = btnSkip.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
            }
        }

        public void ShowSlide(int index)
        {
            if (index < 0 || index >= 6) return;
            if (isTransitioning) return;

            EnsureSlideReferences();

            if (currentSlideAnimCoroutine != null) StopCoroutine(currentSlideAnimCoroutine);
            if (autoAdvanceCoroutine != null) StopCoroutine(autoAdvanceCoroutine);
            if (slideTransitionCoroutine != null) StopCoroutine(slideTransitionCoroutine);

            // Hard isolation before transition: immediately deactivate any stray slide
            for (int i = 0; i < 6; i++)
            {
                if (slides[i] != null && i != currentSlideIndex && i != index)
                {
                    slides[i].SetActive(false);
                    var cg = slides[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 0f;
                }
            }

            slideTransitionCoroutine = StartCoroutine(TransitionToSlide(currentSlideIndex, index));
        }

        private IEnumerator TransitionToSlide(int fromIndex, int toIndex)
        {
            EnsureSlideReferences();
            isTransitioning = true;
            int prevIndex = currentSlideIndex;
            currentSlideIndex = toIndex;

            // STRICT ISOLATION: Only fromGo and toGo may exist during transition
            for (int i = 0; i < 6; i++)
            {
                if (slides[i] != null && i != toIndex && i != prevIndex)
                {
                    slides[i].SetActive(false);
                    var cg = slides[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 0f;
                }
            }

            // Update step dots immediately
            for (int i = 0; i < 6; i++)
            {
                if (stepDots[i] != null)
                {
                    stepDots[i].color = (i == toIndex)
                        ? new Color(0.2f, 0.95f, 0.5f, 1f)
                        : new Color(0.4f, 0.45f, 0.5f, 0.45f);
                }
            }

            if (btnPrev != null) btnPrev.gameObject.SetActive(toIndex > 0);
            if (btnNext != null)
            {
                var nTxt = btnNext.GetComponentInChildren<Text>();
                if (nTxt != null)
                {
                    nTxt.text = (toIndex == 5) ? "START MISSION ▶" : "NEXT ❯";
                }
            }

            if (autoAdvanceFill != null) autoAdvanceFill.fillAmount = 0f;

            // Background crossfade & Ken Burns orbital/pan motion
            Texture2D targetTex = GetBackgroundTextureForSlide(toIndex);
            if (targetTex != null && backgroundImage != null && backgroundImage.texture != targetTex)
            {
                StartCoroutine(CrossfadeBackground(targetTex, 0.45f));
            }
            StartKenBurnsForSlide(toIndex);
            UpdateTelemetryBadge(toIndex);

            GameObject fromGo = (prevIndex >= 0 && prevIndex < 6) ? slides[prevIndex] : null;
            GameObject toGo = (toIndex >= 0 && toIndex < 6) ? slides[toIndex] : null;

            CanvasGroup fromCg = fromGo != null ? fromGo.GetComponent<CanvasGroup>() : null;
            CanvasGroup toCg = toGo != null ? toGo.GetComponent<CanvasGroup>() : null;
            RectTransform toRt = toGo != null ? toGo.GetComponent<RectTransform>() : null;

            if (toGo != null)
            {
                toGo.SetActive(true);
                if (toCg != null) toCg.alpha = 0f;
            }

            // Cinematic crossfade dissolve + subtle upward slide entrance + scale punch
            float elapsed = 0f;
            float duration = 0.35f;
            Vector2 startOffset = new Vector2(0f, -24f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, t);

                if (fromCg != null && fromGo != toGo) fromCg.alpha = 1f - ease;
                if (toCg != null) toCg.alpha = ease;
                if (toRt != null)
                {
                    toRt.anchoredPosition = Vector2.Lerp(startOffset, Vector2.zero, ease);
                    float s = Mathf.Lerp(0.96f, 1f, ease);
                    toRt.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            // STRICT ISOLATION GUARANTEE UPON ARRIVAL
            IsolateSlide(toIndex);

            if (toRt != null)
            {
                toRt.anchoredPosition = Vector2.zero;
                toRt.localScale = Vector3.one;
            }

            isTransitioning = false;
            currentSlideAnimCoroutine = StartCoroutine(AnimateSlideContent(toIndex));
        }

        private void UpdateTelemetryBadge(int slideIndex)
        {
            if (orbitalTelemetryBadge == null) return;

            bool isNasaSlide = (slideIndex == 0 || slideIndex == 1);
            orbitalTelemetryBadge.SetActive(isNasaSlide);

            if (orbitalTelemetryText != null && isNasaSlide)
            {
                if (slideIndex == 0)
                {
                    orbitalTelemetryText.text = "🛰️ NASA EARTH OBSERVATION • ISS ORBITAL SENSOR FEED\nALT: 408 KM | 21°56'N 89°11'E | OPTICAL SPECTROMETRY";
                }
                else if (slideIndex == 1)
                {
                    orbitalTelemetryText.text = "🛰️ NASA LANDSAT 7/8/9 • MANGROVE CANOPY DEGRADATION\nBAND 4/3/2 MULTISPECTRAL | RESOLUTION: 30M | 1973–2024 SENSOR SUITE";
                }
            }
        }

        private void StartKenBurnsForSlide(int index)
        {
            if (kenBurnsCoroutine != null) StopCoroutine(kenBurnsCoroutine);

            // Slide 5 uses Timeline to animate Background_Satellite
            if (index == 4) return;

            Vector2 pan = (index % 2 == 0) ? new Vector2(-36f, 18f) : new Vector2(32f, -20f);
            float zoom = (index == 1) ? 1.15f : 1.10f; // Zooms deeper into mangrove delta on Slide 2
            kenBurnsCoroutine = StartCoroutine(AnimateKenBurns(4.5f, pan, zoom));
        }

        private IEnumerator AnimateKenBurns(float duration, Vector2 targetPan, float targetScale)
        {
            if (backgroundRect == null) yield break;

            backgroundRect.localScale = Vector3.one;
            backgroundRect.anchoredPosition = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, t);

                backgroundRect.localScale = Vector3.Lerp(Vector3.one, new Vector3(targetScale, targetScale, 1f), ease);
                backgroundRect.anchoredPosition = Vector2.Lerp(Vector2.zero, targetPan, ease);
                yield return null;
            }
        }

        private IEnumerator CrossfadeBackground(Texture2D targetTex, float duration)
        {
            if (backgroundImage == null || targetTex == null) yield break;
            if (backgroundImage.texture == targetTex) yield break;

            if (backgroundSecondaryImage != null && backgroundSecondaryCg != null)
            {
                backgroundSecondaryImage.texture = targetTex;
                backgroundSecondaryImage.gameObject.SetActive(true);
                backgroundSecondaryCg.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    backgroundSecondaryCg.alpha = Mathf.SmoothStep(0f, 1f, t);
                    yield return null;
                }

                backgroundImage.texture = targetTex;
                backgroundSecondaryCg.alpha = 0f;
                backgroundSecondaryImage.gameObject.SetActive(false);
            }
            else
            {
                backgroundImage.texture = targetTex;
            }
        }

        private void CompleteCurrentAnimationEarly()
        {
            isAnimating = false;
            if (currentSlideAnimCoroutine != null) StopCoroutine(currentSlideAnimCoroutine);

            switch (currentSlideIndex)
            {
                case 0:
                    if (titleCard != null) titleCard.transform.localScale = Vector3.one;
                    if (titleCitationsCg != null) titleCitationsCg.alpha = 1f;
                    break;
                case 1:
                    if (forestPercentText != null)
                    {
                        forestPercentText.text = "91.5%";
                        forestPercentText.color = new Color(0.95f, 0.35f, 0.25f, 0.95f);
                    }
                    if (forestBarFill != null)
                    {
                        forestBarFill.anchorMax = new Vector2(0.915f, 1f);
                        var img = forestBarFill.GetComponent<Image>();
                        if (img != null) img.color = new Color(0.95f, 0.35f, 0.25f, 0.95f);
                    }
                    break;
                case 2:
                    if (tigerCountText != null) tigerCountText.text = "125";
                    if (tigerYearBadge != null) tigerYearBadge.text = "2024: Latest Official Survey (+19 Rebound)";
                    HighlightTigerStepCard(3);
                    break;
                case 3:
                    if (deerCountText != null) deerCountText.text = "140,357";
                    if (deerBarFill != null) deerBarFill.anchorMax = new Vector2(1.0f, 1f);
                    break;
                case 4:
                    if (dolphinGangesCountText != null) dolphinGangesCountText.text = "159";
                    if (dolphinGangesBarFill != null) dolphinGangesBarFill.anchorMax = new Vector2(0.706f, 1f);
                    if (dolphinIrrawaddyCountText != null) dolphinIrrawaddyCountText.text = "198";
                    if (dolphinIrrawaddyBarFill != null) dolphinIrrawaddyBarFill.anchorMax = new Vector2(0.439f, 1f);
                    break;
                case 5:
                    for (int i = 0; i < 4; i++)
                    {
                        if (speciesCards[i] != null)
                        {
                            var cg = speciesCards[i].GetComponent<CanvasGroup>();
                            if (cg != null) cg.alpha = 1f;
                            speciesCards[i].transform.localScale = Vector3.one;
                        }
                    }
                    break;
            }

            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(3.5f));
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── SEPARATE DEDICATED LEVEL ANIMATIONS ──────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator AnimateSlideContent(int index)
        {
            isAnimating = true;

            switch (index)
            {
                case 0:
                    yield return AnimateTitleSlide();
                    isAnimating = false;
                    autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(3.8f));
                    break;

                case 1:
                    yield return AnimateForestDecline();
                    isAnimating = false;
                    autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(3.8f));
                    break;

                case 2:
                    yield return AnimateTigerMilestones();
                    isAnimating = false;
                    autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(4.2f));
                    break;

                case 3:
                    yield return AnimateDeerGrowth();
                    isAnimating = false;
                    autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(3.8f));
                    break;

                case 4:
                    PlayableDirector director = slides[4] != null ? slides[4].GetComponent<PlayableDirector>() : null;
                    if (director != null)
                    {
                        director.time = 0;
                        director.Play();
                    }
                    yield return AnimateDolphinCrisis();
                    isAnimating = false;
                    autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(4.5f));
                    break;

                case 5:
                    yield return AnimateMissionBriefing();
                    isAnimating = false;
                    break;
            }
        }

        /// <summary>
        /// LEVEL 0 ANIMATION: Grand Title Reveal & Citations Fade-in
        /// </summary>
        private IEnumerator AnimateTitleSlide()
        {
            if (titleCard != null)
            {
                var rt = titleCard.GetComponent<RectTransform>();
                Vector2 basePos = rt.anchoredPosition;
                Vector2 startPos = basePos - new Vector2(0f, 35f);
                rt.anchoredPosition = startPos;
                titleCard.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

                float elapsed = 0f;
                float dur = 0.75f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    rt.anchoredPosition = Vector2.Lerp(startPos, basePos, ease);
                    float s = Mathf.Lerp(0.92f, 1.0f, ease);
                    titleCard.transform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                rt.anchoredPosition = basePos;
                titleCard.transform.localScale = Vector3.one;
            }

            if (titleCitationsCg != null)
            {
                titleCitationsCg.alpha = 0f;
                float elapsed = 0f;
                float dur = 0.5f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    titleCitationsCg.alpha = Mathf.Clamp01(elapsed / dur);
                    yield return null;
                }
                titleCitationsCg.alpha = 1f;
            }
        }

        /// <summary>
        /// LEVEL 1 ANIMATION: NASA Landsat Canopy Degradation Meter
        /// Counts from 94.2% down to 91.5%, shifts color from green to alert red.
        /// </summary>
        private IEnumerator AnimateForestDecline()
        {
            float duration = 2.4f;
            float elapsed = 0f;
            float startVal = 94.2f;
            float endVal = 91.5f;

            Color startCol = new Color(0.22f, 0.85f, 0.45f, 0.95f);
            Color endCol = new Color(0.95f, 0.35f, 0.25f, 0.95f);
            Image fillImg = forestBarFill != null ? forestBarFill.GetComponent<Image>() : null;

            if (forestBarFill != null) forestBarFill.anchorMax = new Vector2(startVal / 100f, 1f);
            if (forestPercentText != null)
            {
                forestPercentText.text = "94.2%";
                forestPercentText.color = startCol;
            }
            if (fillImg != null) fillImg.color = startCol;

            yield return new WaitForSecondsRealtime(0.5f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curVal = Mathf.Lerp(startVal, endVal, t);
                Color curCol = Color.Lerp(startCol, endCol, t);

                if (forestPercentText != null)
                {
                    forestPercentText.text = $"{curVal:F1}%";
                    forestPercentText.color = curCol;
                }
                if (forestBarFill != null) forestBarFill.anchorMax = new Vector2(curVal / 100f, 1f);
                if (fillImg != null) fillImg.color = curCol;
                yield return null;
            }

            if (forestPercentText != null)
            {
                forestPercentText.text = "91.5%";
                forestPercentText.color = endCol;
            }
            if (forestBarFill != null) forestBarFill.anchorMax = new Vector2(0.915f, 1f);
            if (fillImg != null) fillImg.color = endCol;
        }

        /// <summary>
        /// LEVEL 2 ANIMATION: Tiger Census Milestones (2004 -> 2015 -> 2018 -> 2024)
        /// Synchronized milestone box highlighting and scale pulsation.
        /// </summary>
        private IEnumerator AnimateTigerMilestones()
        {
            HighlightTigerStepCard(0);
            if (tigerYearBadge != null) tigerYearBadge.text = "2004: Historical Estimate";
            if (tigerCountText != null)
            {
                tigerCountText.text = "440";
                tigerCountText.color = new Color(1.0f, 0.80f, 0.25f);
                tigerCountText.transform.localScale = Vector3.one;
            }
            yield return new WaitForSecondsRealtime(0.9f);

            // 2015 Crash
            HighlightTigerStepCard(1);
            if (tigerYearBadge != null) tigerYearBadge.text = "2015: Critical Poaching Drop";
            if (tigerCountText != null) tigerCountText.color = new Color(0.95f, 0.30f, 0.30f);
            yield return LerpCounterWithScale(440, 106, 1.2f, tigerCountText, 1.0f, 0.85f);
            yield return new WaitForSecondsRealtime(0.7f);

            // 2018 Rebound
            HighlightTigerStepCard(2);
            if (tigerYearBadge != null) tigerYearBadge.text = "2018: Camera-Trap Recovery";
            if (tigerCountText != null) tigerCountText.color = new Color(0.35f, 0.90f, 0.55f);
            yield return LerpCounterWithScale(106, 114, 0.9f, tigerCountText, 0.85f, 0.96f);
            yield return new WaitForSecondsRealtime(0.6f);

            // 2024 Latest Survey
            HighlightTigerStepCard(3);
            if (tigerYearBadge != null) tigerYearBadge.text = "2024: Latest Official Survey (+19 Rebound)";
            if (tigerCountText != null) tigerCountText.color = new Color(0.20f, 0.98f, 0.50f);
            yield return LerpCounterWithScale(114, 125, 0.9f, tigerCountText, 0.96f, 1.06f);
        }

        private void HighlightTigerStepCard(int stepIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (tigerStepCards[i] != null)
                {
                    var outline = tigerStepCards[i].GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectDistance = (i == stepIndex) ? new Vector2(3.5f, -3.5f) : new Vector2(1.5f, -1.5f);
                    }
                    tigerStepCards[i].transform.localScale = (i == stepIndex) ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
                }
            }
        }

        private IEnumerator LerpCounterWithScale(int from, int to, float duration, Text target, float startScale, float endScale)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                int cur = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));
                float curScale = Mathf.Lerp(startScale, endScale, ease);

                if (target != null)
                {
                    target.text = cur.ToString();
                    target.transform.localScale = new Vector3(curScale, curScale, 1f);
                }
                yield return null;
            }
            if (target != null)
            {
                target.text = to.ToString();
                target.transform.localScale = new Vector3(endScale, endScale, 1f);
            }
        }

        /// <summary>
        /// LEVEL 3 ANIMATION: Spotted Deer Population Boom (83,000 -> 140,357)
        /// Smooth ease-out counter and growth bar animation.
        /// </summary>
        private IEnumerator AnimateDeerGrowth()
        {
            if (deerCountText != null) deerCountText.text = "83,000";
            if (deerBarFill != null) deerBarFill.anchorMax = new Vector2(83000f / 140357f, 1f);

            yield return new WaitForSecondsRealtime(0.5f);

            float duration = 2.4f;
            float elapsed = 0f;
            int startCount = 83000;
            int endCount = 140357;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                int cur = Mathf.RoundToInt(Mathf.Lerp(startCount, endCount, easeOut));

                if (deerCountText != null) deerCountText.text = cur.ToString("N0");
                if (deerBarFill != null) deerBarFill.anchorMax = new Vector2(Mathf.Lerp(83000f / 140357f, 1f, easeOut), 1f);
                yield return null;
            }

            if (deerCountText != null)
            {
                deerCountText.text = "140,357";
                // Scale pulse on completion
                float pulseEl = 0f;
                while (pulseEl < 0.3f)
                {
                    pulseEl += Time.unscaledDeltaTime;
                    float s = 1f + Mathf.Sin(pulseEl / 0.3f * Mathf.PI) * 0.08f;
                    deerCountText.transform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                deerCountText.transform.localScale = Vector3.one;
            }
            if (deerBarFill != null) deerBarFill.anchorMax = new Vector2(1f, 1f);
        }

        /// <summary>
        /// LEVEL 4 ANIMATION: Dolphin Crisis Collapse (Ganges 225->159, Irrawaddy 451->198)
        /// Flashing entanglement alert box.
        /// </summary>
        private IEnumerator AnimateDolphinCrisis()
        {
            if (dolphinGangesCountText != null) dolphinGangesCountText.text = "225";
            if (dolphinIrrawaddyCountText != null) dolphinIrrawaddyCountText.text = "451";
            if (dolphinGangesBarFill != null) dolphinGangesBarFill.anchorMax = new Vector2(1f, 1f);
            if (dolphinIrrawaddyBarFill != null) dolphinIrrawaddyBarFill.anchorMax = new Vector2(1f, 1f);

            yield return new WaitForSecondsRealtime(0.6f);

            float duration = 2.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 2f);

                int gCur = Mathf.RoundToInt(Mathf.Lerp(225, 159, ease));
                int iCur = Mathf.RoundToInt(Mathf.Lerp(451, 198, ease));

                if (dolphinGangesCountText != null) dolphinGangesCountText.text = gCur.ToString();
                if (dolphinIrrawaddyCountText != null) dolphinIrrawaddyCountText.text = iCur.ToString();

                if (dolphinGangesBarFill != null) dolphinGangesBarFill.anchorMax = new Vector2(Mathf.Lerp(1f, 159f / 225f, ease), 1f);
                if (dolphinIrrawaddyBarFill != null) dolphinIrrawaddyBarFill.anchorMax = new Vector2(Mathf.Lerp(1f, 198f / 451f, ease), 1f);

                yield return null;
            }

            if (dolphinGangesCountText != null) dolphinGangesCountText.text = "159";
            if (dolphinIrrawaddyCountText != null) dolphinIrrawaddyCountText.text = "198";

            // Flash alert quote box outline
            if (dolphinQuoteBox != null)
            {
                var outline = dolphinQuoteBox.GetComponent<Outline>();
                if (outline != null)
                {
                    Color origCol = outline.effectColor;
                    for (int f = 0; f < 3; f++)
                    {
                        outline.effectColor = Color.yellow;
                        yield return new WaitForSecondsRealtime(0.12f);
                        outline.effectColor = origCol;
                        yield return new WaitForSecondsRealtime(0.12f);
                    }
                }
            }
        }

        /// <summary>
        /// LEVEL 5 ANIMATION: 4 Species Conservation Cards Stagger Entrance
        /// Each card pops in sequentially, followed by the mission button scale pulse.
        /// </summary>
        private IEnumerator AnimateMissionBriefing()
        {
            for (int i = 0; i < 4; i++)
            {
                if (speciesCards[i] != null)
                {
                    var cg = speciesCards[i].GetComponent<CanvasGroup>();
                    if (cg == null) cg = speciesCards[i].AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    speciesCards[i].transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                }
            }

            yield return new WaitForSecondsRealtime(0.2f);

            for (int i = 0; i < 4; i++)
            {
                if (speciesCards[i] != null)
                {
                    StartCoroutine(PopInSpeciesCard(speciesCards[i]));
                    yield return new WaitForSecondsRealtime(0.18f);
                }
            }

            yield return new WaitForSecondsRealtime(0.4f);

            if (btnStartMission != null)
            {
                float el = 0f;
                while (el < 0.45f)
                {
                    el += Time.unscaledDeltaTime;
                    float s = 1f + Mathf.Sin(el / 0.45f * Mathf.PI) * 0.08f;
                    btnStartMission.transform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                btnStartMission.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator PopInSpeciesCard(GameObject card)
        {
            var cg = card.GetComponent<CanvasGroup>();
            float elapsed = 0f;
            float dur = 0.35f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (cg != null) cg.alpha = ease;
                float s = Mathf.Lerp(0.85f, 1f, ease);
                card.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            if (cg != null) cg.alpha = 1f;
            card.transform.localScale = Vector3.one;
        }

        private IEnumerator AutoAdvanceTimer(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (autoAdvanceFill != null)
                {
                    autoAdvanceFill.fillAmount = Mathf.Clamp01(elapsed / seconds);
                }
                yield return null;
            }
            if (autoAdvanceFill != null) autoAdvanceFill.fillAmount = 1f;

            if (currentSlideIndex < 5)
            {
                ShowSlide(currentSlideIndex + 1);
            }
        }

        private IEnumerator FadeOutAndFinish()
        {
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                float duration = 0.5f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (introCanvas != null)
            {
                introCanvas.gameObject.SetActive(false);
            }

            IsIntroActive = false;
            Time.timeScale = 1f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── UI GENERATION / BAKING ───────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        public void EnsureUIBuilt(bool forceRebuild = false)
        {
            if (introCanvas != null && !forceRebuild) return;

            GameObject canvasGo = GameObject.Find("Intro_Data_Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Intro_Data_Canvas");
                introCanvas = canvasGo.AddComponent<Canvas>();
                introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                introCanvas.sortingOrder = 120;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
                canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            }
            else
            {
                introCanvas = canvasGo.GetComponent<Canvas>();
                canvasGroup = canvasGo.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = canvasGo.AddComponent<CanvasGroup>();

                // If canvas already has the slides baked in and not forcing rebuild, bind and isolate
                if (!forceRebuild && canvasGo.transform.Find("Slide_1_Title") != null)
                {
                    EnsureSlideReferences();
                    IsolateSlide(0);
                    return;
                }

                for (int i = canvasGo.transform.childCount - 1; i >= 0; i--)
                {
                    var child = canvasGo.transform.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            BuildBackground(canvasGo.transform);
            BuildSlide1_Title(canvasGo.transform);
            BuildSlide2_ForestDecline(canvasGo.transform);
            BuildSlide3_TigerCount(canvasGo.transform);
            BuildSlide4_DeerCount(canvasGo.transform);
            BuildSlide5_DolphinCrisis(canvasGo.transform);
            BuildSlide6_TransitionToGameplay(canvasGo.transform);
            BuildNavigationControls(canvasGo.transform);

            IsolateSlide(0);
        }

        private void BuildBackground(Transform parent)
        {
            GameObject bgGo = new GameObject("Background_Satellite", typeof(RectTransform), typeof(RawImage));
            bgGo.transform.SetParent(parent, false);
            backgroundRect = bgGo.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;

            backgroundImage = bgGo.GetComponent<RawImage>();
            LoadBackgroundTextures();

            if (nasaSpaceTex != null) backgroundImage.texture = nasaSpaceTex;
            else if (defaultSatTex != null) backgroundImage.texture = defaultSatTex;

            backgroundImage.color = new Color(0.88f, 0.90f, 0.94f, 0.96f);

            GameObject bgSecGo = new GameObject("Background_Secondary", typeof(RectTransform), typeof(RawImage), typeof(CanvasGroup));
            bgSecGo.transform.SetParent(bgGo.transform, false);
            var secRt = bgSecGo.GetComponent<RectTransform>();
            secRt.anchorMin = Vector2.zero;
            secRt.anchorMax = Vector2.one;
            secRt.sizeDelta = Vector2.zero;

            backgroundSecondaryImage = bgSecGo.GetComponent<RawImage>();
            backgroundSecondaryImage.color = new Color(0.88f, 0.90f, 0.94f, 0.96f);
            backgroundSecondaryCg = bgSecGo.GetComponent<CanvasGroup>();
            backgroundSecondaryCg.alpha = 0f;
            bgSecGo.SetActive(false);

            // Cinematic Gradient Scrim: Dark at top & bottom, crystal-clear in middle for wildlife photos
            GameObject scrimGo = new GameObject("Cinematic_Gradient_Scrim", typeof(RectTransform), typeof(Image));
            scrimGo.transform.SetParent(parent, false);
            var sRt = scrimGo.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.sizeDelta = Vector2.zero;
            var sImg = scrimGo.GetComponent<Image>();
            if (gradientScrimSprite != null)
            {
                sImg.sprite = gradientScrimSprite;
                sImg.type = Image.Type.Simple;
            }
            sImg.color = new Color(0f, 0f, 0f, 0.70f);

            GameObject vigGo = new GameObject("Vignette_Overlay", typeof(RectTransform), typeof(Image));
            vigGo.transform.SetParent(parent, false);
            var vRt = vigGo.GetComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            var vigImg = vigGo.GetComponent<Image>();
            vigImg.color = new Color(0.01f, 0.02f, 0.04f, 0.25f);

            orbitalTelemetryBadge = new GameObject("Orbital_Telemetry_Badge", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            orbitalTelemetryBadge.transform.SetParent(parent, false);
            var otRt = orbitalTelemetryBadge.GetComponent<RectTransform>();
            otRt.anchorMin = new Vector2(0.03f, 0.94f);
            otRt.anchorMax = new Vector2(0.03f, 0.94f);
            otRt.pivot = new Vector2(0f, 1f);
            otRt.sizeDelta = new Vector2(540f, 46f);

            var otImg = orbitalTelemetryBadge.GetComponent<Image>();
            otImg.color = new Color(0.05f, 0.12f, 0.18f, 0.92f);

            var otOutline = orbitalTelemetryBadge.AddComponent<Outline>();
            otOutline.effectColor = new Color(0.20f, 0.70f, 0.95f, 0.85f);
            otOutline.effectDistance = new Vector2(1.5f, -1.5f);

            orbitalTelemetryText = CreateText(orbitalTelemetryBadge.transform,
                "🛰️ NASA EARTH OBSERVATION • ISS ORBITAL SENSOR FEED\nALT: 408 KM | 21°56'N 89°11'E | OPTICAL SPECTROMETRY",
                mainFont, 13, FontStyle.Bold, new Color(0.45f, 0.90f, 1.0f), TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(520f, 40f));

            orbitalTelemetryBadge.SetActive(true);
        }

        private void BuildSlide1_Title(Transform parent)
        {
            GameObject sGo = new GameObject("Slide_1_Title", typeof(RectTransform), typeof(CanvasGroup));
            sGo.transform.SetParent(parent, false);
            var rt = sGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            slides[0] = sGo;

            CreateBadge(sGo.transform, "🛰️ NASA EARTH OBSERVATION • SUNDARBANS ORBITAL DATA",
                new Color(0.08f, 0.25f, 0.58f, 0.96f), new Vector2(0.5f, 0.86f), new Vector2(560f, 38f));

            // Grand Title Plaque (Mobile Ergonomic Scale)
            titleCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.60f), new Vector2(1280f, 220f),
                new Color(0.03f, 0.07f, 0.11f, 0.96f), new Color(0.95f, 0.80f, 0.25f, 0.95f));
            titleCard.name = "Grand_Title_Card";

            CreateText(titleCard.transform, "SUNDARBANS GUARDIAN",
                mainFont, 72, FontStyle.Bold, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.66f), new Vector2(1240f, 90f));

            CreateText(titleCard.transform, "Real Satellite Observation & Wildlife Survey Data (2000 – 2024)",
                mainFont, 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.24f), new Vector2(1240f, 48f));

            // Citations Card (Mobile Legible font 18pt)
            var card = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.36f), new Vector2(1120f, 95f),
                new Color(0.04f, 0.10f, 0.16f, 0.95f), new Color(0.25f, 0.75f, 0.95f, 0.85f));
            card.name = "Citations_Card";
            titleCitationsCg = card.GetComponent<CanvasGroup>();
            if (titleCitationsCg == null) titleCitationsCg = card.AddComponent<CanvasGroup>();

            CreateText(card.transform,
                "Official scientific citations from NASA Earth Observation, Bangladesh Forest Department, and Wildlife Conservation Society (WCS) Bangladesh.",
                mainFont, 18, FontStyle.Bold, new Color(0.90f, 0.96f, 1.0f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1080f, 75f));

            // Bottom Prompt Pill Card
            var promptCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.20f), new Vector2(620f, 48f),
                new Color(0.04f, 0.08f, 0.12f, 0.92f), new Color(0.20f, 0.85f, 0.55f, 0.80f));
            promptCard.name = "Prompt_Pill_Card";
            CreateText(promptCard.transform, "Tap NEXT ❯ or wait for automated sequence ▶",
                mainFont, 17, FontStyle.Bold, new Color(0.40f, 0.95f, 0.65f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(600f, 40f));
        }

        private void BuildSlide2_ForestDecline(Transform parent)
        {
            GameObject sGo = new GameObject("Slide_2_ForestDecline", typeof(RectTransform), typeof(CanvasGroup));
            sGo.transform.SetParent(parent, false);
            var rt = sGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            slides[1] = sGo;

            CreateBadge(sGo.transform, "🛰️ NASA LANDSAT SATELLITE DATA • CANOPY DEGRADATION",
                new Color(0.08f, 0.28f, 0.60f, 0.96f), new Vector2(0.5f, 0.88f), new Vector2(580f, 38f));

            var headCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.77f), new Vector2(1280f, 65f),
                new Color(0.04f, 0.08f, 0.12f, 0.95f), new Color(0.95f, 0.75f, 0.30f, 0.85f));
            headCard.name = "Head_Card";
            CreateText(headCard.transform,
                "1973 to 2024: Dense Mangrove Canopy Decreased from 94.2% to 91.5%",
                mainFont, 28, FontStyle.Bold, new Color(1.0f, 0.92f, 0.70f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1240f, 55f));

            var barContainer = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.53f), new Vector2(1060f, 185f),
                new Color(0.04f, 0.09f, 0.13f, 0.96f), new Color(0.85f, 0.45f, 0.25f, 0.85f));
            barContainer.name = "Bar_Container";

            CreateText(barContainer.transform, "Dense Mangrove Canopy Coverage",
                mainFont, 22, FontStyle.Bold, new Color(0.85f, 0.92f, 0.98f), TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.86f), new Vector2(650f, 32f));

            forestPercentText = CreateText(barContainer.transform, "94.2%",
                mainFont, 66, FontStyle.Bold, new Color(1.0f, 0.45f, 0.35f), TextAnchor.UpperRight,
                new Vector2(0.96f, 0.86f), new Vector2(280f, 75f));

            GameObject trackGo = new GameObject("Bar_Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(barContainer.transform, false);
            var trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.04f, 0.26f);
            trackRt.anchorMax = new Vector2(0.96f, 0.48f);
            trackRt.sizeDelta = Vector2.zero;
            trackGo.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.22f, 0.95f);

            GameObject fillGo = new GameObject("Bar_Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            forestBarFill = fillGo.GetComponent<RectTransform>();
            forestBarFill.anchorMin = Vector2.zero;
            forestBarFill.anchorMax = new Vector2(0.942f, 1f);
            forestBarFill.sizeDelta = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(0.18f, 0.82f, 0.42f, 0.95f);

            CreateText(barContainer.transform, "1973: 94.2%  →  2024: 91.5% (Over 160 sq km of primary dense canopy lost)",
                mainFont, 18, FontStyle.Bold, new Color(0.85f, 0.92f, 0.98f), TextAnchor.LowerLeft,
                new Vector2(0.04f, 0.05f), new Vector2(850f, 26f));

            var insightBox = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.29f), new Vector2(1060f, 105f),
                new Color(0.12f, 0.06f, 0.06f, 0.96f), new Color(0.95f, 0.35f, 0.25f, 0.85f));
            insightBox.name = "Insight_Box";

            GameObject mPhotoGo = new GameObject("Mangrove_Photo_Thumb", typeof(RectTransform), typeof(RawImage));
            mPhotoGo.transform.SetParent(insightBox.transform, false);
            var mpRt = mPhotoGo.GetComponent<RectTransform>();
            mpRt.anchorMin = new Vector2(0.08f, 0.5f);
            mpRt.anchorMax = new Vector2(0.08f, 0.5f);
            mpRt.pivot = new Vector2(0.5f, 0.5f);
            mpRt.sizeDelta = new Vector2(120f, 80f);
            var mpImg = mPhotoGo.GetComponent<RawImage>();
            if (realSundarbanMangroveTex != null) mpImg.texture = realSundarbanMangroveTex;
            var mpOutline = mPhotoGo.AddComponent<Outline>();
            mpOutline.effectColor = new Color(0.2f, 0.85f, 0.45f, 0.85f);
            mpOutline.effectDistance = new Vector2(2f, -2f);

            CreateText(insightBox.transform,
                "Primary canopy loss directly fragments tiger and deer breeding habitats, while weakening natural storm barriers protecting millions of coastal residents.",
                mainFont, 18, FontStyle.Bold, new Color(1.0f, 0.92f, 0.92f), TextAnchor.MiddleLeft,
                new Vector2(0.57f, 0.5f), new Vector2(860f, 85f));

            var srcCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.15f), new Vector2(880f, 40f),
                new Color(0.03f, 0.06f, 0.10f, 0.92f), new Color(0.30f, 0.65f, 0.90f, 0.70f));
            srcCard.name = "Source_Card";
            CreateText(srcCard.transform, "Source: NASA Landsat Earth Observation (1973–2024) & The Change Initiative study.",
                mainFont, 16, FontStyle.Bold, new Color(0.75f, 0.90f, 1.0f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(860f, 34f));
        }

        private void BuildSlide3_TigerCount(Transform parent)
        {
            GameObject sGo = new GameObject("Slide_3_TigerCount", typeof(RectTransform), typeof(CanvasGroup));
            sGo.transform.SetParent(parent, false);
            var rt = sGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            slides[2] = sGo;

            CreateBadge(sGo.transform, "📷 AUTHENTIC FIELD PHOTO • ROYAL BENGAL TIGER IN SUNDARBANS",
                new Color(0.18f, 0.12f, 0.05f, 0.92f), new Vector2(0.5f, 0.91f), new Vector2(660f, 32f));

            CreateBadge(sGo.transform, "ROYAL BENGAL TIGER POPULATION",
                new Color(0.95f, 0.55f, 0.15f, 0.95f), new Vector2(0.5f, 0.85f), new Vector2(380f, 34f));

            var headCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.77f), new Vector2(1220f, 62f),
                new Color(0.04f, 0.08f, 0.12f, 0.95f), new Color(0.95f, 0.65f, 0.25f, 0.85f));
            headCard.name = "Head_Card";
            CreateText(headCard.transform, "Tiger Population: From Critical Crash to Gradual Recovery",
                mainFont, 28, FontStyle.Bold, new Color(1.0f, 0.90f, 0.45f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1180f, 52f));

            var centerCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.54f), new Vector2(980f, 180f),
                new Color(0.06f, 0.10f, 0.14f, 0.96f), new Color(0.95f, 0.65f, 0.25f, 0.85f));
            centerCard.name = "Center_Card";

            GameObject tPhotoGo = new GameObject("Tiger_Photo_Inset", typeof(RectTransform), typeof(RawImage));
            tPhotoGo.transform.SetParent(centerCard.transform, false);
            var tpRt = tPhotoGo.GetComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0.12f, 0.50f);
            tpRt.anchorMax = new Vector2(0.12f, 0.50f);
            tpRt.pivot = new Vector2(0.5f, 0.5f);
            tpRt.sizeDelta = new Vector2(140f, 95f);
            var tpImg = tPhotoGo.GetComponent<RawImage>();
            if (realSundarbanTigerTex != null) tpImg.texture = realSundarbanTigerTex;
            var tpOutline = tPhotoGo.AddComponent<Outline>();
            tpOutline.effectColor = new Color(0.95f, 0.65f, 0.25f, 0.9f);
            tpOutline.effectDistance = new Vector2(2f, -2f);

            tigerYearBadge = CreateText(centerCard.transform, "2004: Historical Estimate",
                mainFont, 20, FontStyle.Bold, new Color(0.4f, 0.9f, 0.85f), TextAnchor.UpperCenter,
                new Vector2(0.58f, 0.86f), new Vector2(550f, 30f));

            tigerCountText = CreateText(centerCard.transform, "440",
                mainFont, 72, FontStyle.Bold, new Color(1.0f, 0.80f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0.58f, 0.44f), new Vector2(400f, 80f));

            CreateText(centerCard.transform, "Sundarbans Tiger Census Count",
                mainFont, 18, FontStyle.Bold, new Color(0.75f, 0.85f, 0.95f), TextAnchor.LowerCenter,
                new Vector2(0.58f, 0.08f), new Vector2(550f, 26f));

            float[] stepXs = { -360f, -120f, 120f, 360f };
            string[] years = { "2004", "2015", "2018", "2024" };
            string[] counts = { "440", "106", "114", "125" };
            string[] status = { "Estimate", "Crash", "Rebound", "Current" };
            Color[] colors = {
                new Color(0.9f, 0.7f, 0.2f),
                new Color(0.95f, 0.25f, 0.25f),
                new Color(0.3f, 0.85f, 0.5f),
                new Color(0.2f, 0.95f, 0.45f)
            };

            for (int i = 0; i < 4; i++)
            {
                var stepCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.33f), new Vector2(230f, 85f),
                    new Color(0.08f, 0.12f, 0.16f, 0.94f), colors[i]);
                stepCard.name = $"Milestone_Step_{i}";
                tigerStepCards[i] = stepCard;
                var scRt = stepCard.GetComponent<RectTransform>();
                scRt.anchoredPosition = new Vector2(stepXs[i], scRt.anchoredPosition.y);

                CreateText(stepCard.transform, $"{years[i]} ({status[i]})",
                    mainFont, 16, FontStyle.Bold, new Color(0.85f, 0.9f, 0.95f), TextAnchor.UpperCenter,
                    new Vector2(0.5f, 0.84f), new Vector2(210f, 24f));

                CreateText(stepCard.transform, counts[i],
                    mainFont, 28, FontStyle.Bold, colors[i], TextAnchor.LowerCenter,
                    new Vector2(0.5f, 0.18f), new Vector2(210f, 36f));
            }

            var capBox = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.20f), new Vector2(1040f, 56f),
                new Color(0.08f, 0.14f, 0.18f, 0.92f), new Color(0.35f, 0.75f, 0.95f, 0.6f));
            capBox.name = "Quote_Caption_Box";
            CreateText(capBox.transform,
                "\"Conservation efforts are helping tiger numbers recover, but the population remains Critically Endangered.\"",
                mainFont, 18, FontStyle.Bold, new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1000f, 44f));

            var srcCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.11f), new Vector2(740f, 38f),
                new Color(0.03f, 0.06f, 0.10f, 0.92f), new Color(0.30f, 0.65f, 0.90f, 0.70f));
            srcCard.name = "Source_Card";
            CreateText(srcCard.transform, "Source: Bangladesh Forest Department camera-trap survey, 2024.",
                mainFont, 16, FontStyle.Bold, new Color(0.75f, 0.90f, 1.0f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(720f, 32f));
        }

        private void BuildSlide4_DeerCount(Transform parent)
        {
            GameObject sGo = new GameObject("Slide_4_DeerCount", typeof(RectTransform), typeof(CanvasGroup));
            sGo.transform.SetParent(parent, false);
            var rt = sGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            slides[3] = sGo;

            // 1. Single subtle top-left corner pill tag (replaces multiple stacked boxed labels)
            CreatePillTag(sGo.transform, "🌿 POPULATION SURVEY • SPOTTED DEER (CHITAL)",
                new Vector2(0.06f, 0.92f), new Vector2(440f, 36f));

            // 2. ONE Unified Card (Subtle semi-transparent dark background ~68% opacity, softly rounded corners)
            var unifiedCard = CreateUnifiedCard(sGo.transform, new Vector2(0.5f, 0.48f), new Vector2(1220f, 570f),
                new Color(0.04f, 0.07f, 0.06f, 0.68f));
            unifiedCard.name = "Unified_Card";

            // ── SECTION 1: MAIN HEADLINE ──
            // Largest title text, bold, no background box, crisp white with strong shadow
            var headText = CreateText(unifiedCard.transform,
                "Spotted Deer: Vital Growth in Primary Prey Base",
                mainFont, 32, FontStyle.Bold, new Color(0.98f, 0.99f, 1.0f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.90f), new Vector2(1160f, 50f));
            headText.name = "Headline_Text";

            // Top subtle divider line
            CreateDividerLine(unifiedCard.transform, new Vector2(0.5f, 0.81f), 1140f, 1.5f);

            // ── SECTION 2: STAT & PHOTO CENTERPIECE ──
            // Left: Inset Real Wildlife Photo
            GameObject dPhotoGo = new GameObject("Deer_Photo_Inset", typeof(RectTransform), typeof(RawImage));
            dPhotoGo.transform.SetParent(unifiedCard.transform, false);
            var dpRt = dPhotoGo.GetComponent<RectTransform>();
            dpRt.anchorMin = new Vector2(0.16f, 0.53f);
            dpRt.anchorMax = new Vector2(0.16f, 0.53f);
            dpRt.pivot = new Vector2(0.5f, 0.5f);
            dpRt.sizeDelta = new Vector2(250f, 165f);
            var dpImg = dPhotoGo.GetComponent<RawImage>();
            if (realSundarbanDeerTex != null) dpImg.texture = realSundarbanDeerTex;
            var dpOutline = dPhotoGo.AddComponent<Outline>();
            dpOutline.effectColor = new Color(0.18f, 0.75f, 0.45f, 0.50f);
            dpOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var dpShadow = dPhotoGo.AddComponent<Shadow>();
            dpShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            dpShadow.effectDistance = new Vector2(3f, -3f);

            // Right: Milestone Label
            CreateText(unifiedCard.transform,
                "2004 Baseline: 83,000  →  2023 Survey: 140,357  (+69% Net Growth)",
                mainFont, 20, FontStyle.Bold, new Color(0.55f, 0.92f, 0.72f, 0.95f), TextAnchor.UpperLeft,
                new Vector2(0.33f, 0.72f), new Vector2(740f, 30f));

            // Right: Big Centerpiece Number (83,000) - Largest font on screen, vibrant emerald green, clean typography
            deerCountText = CreateText(unifiedCard.transform, "83,000",
                mainFont, 84, FontStyle.Bold, new Color(0.20f, 0.95f, 0.55f), TextAnchor.MiddleLeft,
                new Vector2(0.33f, 0.54f), new Vector2(500f, 92f));
            deerCountText.name = "Deer_Count_Value";

            // Right: Rounded Progress Bar (Rounded ends and soft glow/gradient fill)
            GameObject barContainer = new GameObject("Progress_Bar_Container", typeof(RectTransform));
            barContainer.transform.SetParent(unifiedCard.transform, false);
            var bcRt = barContainer.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.33f, 0.38f);
            bcRt.anchorMax = new Vector2(0.33f, 0.38f);
            bcRt.pivot = new Vector2(0f, 0.5f);
            bcRt.sizeDelta = new Vector2(720f, 26f);

            // Track (rounded capsule)
            GameObject trackGo = new GameObject("Bar_Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(barContainer.transform, false);
            var trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = Vector2.zero;
            trackRt.anchorMax = Vector2.one;
            trackRt.sizeDelta = Vector2.zero;
            var trackImg = trackGo.GetComponent<Image>();
            if (roundedCapsuleSprite != null)
            {
                trackImg.sprite = roundedCapsuleSprite;
                trackImg.type = Image.Type.Sliced;
            }
            trackImg.color = new Color(0.08f, 0.16f, 0.12f, 0.90f);

            // Fill (rounded capsule with vibrant emerald glow)
            GameObject fillGo = new GameObject("Bar_Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            deerBarFill = fillGo.GetComponent<RectTransform>();
            deerBarFill.anchorMin = Vector2.zero;
            deerBarFill.anchorMax = new Vector2(83000f / 140357f, 1f);
            deerBarFill.sizeDelta = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            if (roundedCapsuleSprite != null)
            {
                fillImg.sprite = roundedCapsuleSprite;
                fillImg.type = Image.Type.Sliced;
            }
            fillImg.color = new Color(0.18f, 0.88f, 0.50f, 0.95f);

            var fillOutline = fillGo.AddComponent<Outline>();
            fillOutline.effectColor = new Color(0.40f, 0.98f, 0.65f, 0.45f);
            fillOutline.effectDistance = new Vector2(1f, -1f);

            // Track End Markers / Labels
            CreateText(unifiedCard.transform, "2004 Baseline (83,000)",
                mainFont, 14, FontStyle.Normal, new Color(0.70f, 0.80f, 0.75f, 0.70f), TextAnchor.UpperLeft,
                new Vector2(0.33f, 0.33f), new Vector2(250f, 22f));

            CreateText(unifiedCard.transform, "2023 Survey Target (140,357)",
                mainFont, 14, FontStyle.Normal, new Color(0.70f, 0.80f, 0.75f, 0.70f), TextAnchor.UpperRight,
                new Vector2(0.92f, 0.33f), new Vector2(280f, 22f));

            // Bottom subtle divider line
            CreateDividerLine(unifiedCard.transform, new Vector2(0.5f, 0.25f), 1140f, 1.5f);

            // ── SECTION 3: CAPTION TEXT ──
            // Noticeably smaller and muted slate-cream color (17pt), high readability
            CreateText(unifiedCard.transform,
                "The 57,000+ population increase provides vital prey biomass for endangered Royal Bengal Tigers. However, coastal salinity intrusion and plastic debris continue to threaten vital freshwater grazing sanctuaries.",
                mainFont, 17, FontStyle.Normal, new Color(0.85f, 0.90f, 0.92f, 0.85f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.12f), new Vector2(1120f, 65f));

            // ── CORNER-ANCHORED CITATIONS & PHOTO CREDIT (NO BOXES) ──
            // Bottom-left: Source citation
            CreateCornerText(sGo.transform,
                "Source: Bangladesh Forest Department Wildlife Survey (2023)",
                new Vector2(0.06f, 0.05f), new Vector2(0f, 0.5f), TextAnchor.MiddleLeft,
                new Color(0.75f, 0.85f, 0.80f, 0.65f), 14);

            // Bottom-right: Photo credit
            CreateCornerText(sGo.transform,
                "Photo: Authentic Sundarbans Chital Herd Observation",
                new Vector2(0.94f, 0.05f), new Vector2(1f, 0.5f), TextAnchor.MiddleRight,
                new Color(0.75f, 0.85f, 0.80f, 0.65f), 14);
        }

        private void BuildSlide5_DolphinCrisis(Transform parent)
        {
            GameObject sGo = new GameObject("Slide_5_DolphinCrisis", typeof(RectTransform), typeof(CanvasGroup));
            sGo.transform.SetParent(parent, false);
            var rt = sGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            slides[4] = sGo;

            CreateBadge(sGo.transform, "📷 AUTHENTIC FIELD HABITAT • SUNDARBANS ESTUARY & RIVER CHANNELS",
                new Color(0.08f, 0.22f, 0.32f, 0.92f), new Vector2(0.5f, 0.91f), new Vector2(680f, 32f));

            CreateBadge(sGo.transform, "SUNDARBANS DOLPHINS: SEVERE EXTINCTION RISK",
                new Color(0.90f, 0.20f, 0.20f, 0.95f), new Vector2(0.5f, 0.85f), new Vector2(420f, 34f));

            var headCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.78f), new Vector2(1220f, 62f),
                new Color(0.04f, 0.08f, 0.12f, 0.95f), new Color(0.95f, 0.35f, 0.35f, 0.85f));
            headCard.name = "Head_Card";
            CreateText(headCard.transform, "Catastrophic Collapse of Estuary & River Dolphin Populations",
                mainFont, 28, FontStyle.Bold, new Color(1.0f, 0.65f, 0.60f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1180f, 52f));

            // Ganges Card
            var gCard = CreateGlassCard(sGo.transform, new Vector2(0.28f, 0.54f), new Vector2(530f, 185f),
                new Color(0.12f, 0.08f, 0.12f, 0.96f), new Color(0.95f, 0.35f, 0.35f, 0.85f));
            gCard.name = "Card_Ganges";

            GameObject dpGo = new GameObject("Ganges_Dolphin_Photo", typeof(RectTransform), typeof(RawImage));
            dpGo.transform.SetParent(gCard.transform, false);
            var dpRt = dpGo.GetComponent<RectTransform>();
            dpRt.anchorMin = new Vector2(0.18f, 0.52f);
            dpRt.anchorMax = new Vector2(0.18f, 0.52f);
            dpRt.pivot = new Vector2(0.5f, 0.5f);
            dpRt.sizeDelta = new Vector2(120f, 80f);
            var dpImg = dpGo.GetComponent<RawImage>();
            if (realSundarbanDolphinTex != null) dpImg.texture = realSundarbanDolphinTex;
            var dpOutline = dpGo.AddComponent<Outline>();
            dpOutline.effectColor = new Color(0.95f, 0.35f, 0.35f, 0.85f);
            dpOutline.effectDistance = new Vector2(1.5f, -1.5f);

            CreateText(gCard.transform, "Ganges River Dolphin (Platanista gangetica)",
                mainFont, 17, FontStyle.Bold, new Color(0.95f, 0.85f, 0.85f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.86f), new Vector2(490f, 26f));

            dolphinGangesCountText = CreateText(gCard.transform, "225",
                mainFont, 56, FontStyle.Bold, new Color(1.0f, 0.35f, 0.35f), TextAnchor.MiddleCenter,
                new Vector2(0.64f, 0.52f), new Vector2(230f, 75f));

            GameObject gTrack = new GameObject("G_Track", typeof(RectTransform), typeof(Image));
            gTrack.transform.SetParent(gCard.transform, false);
            var gtRt = gTrack.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0.36f, 0.16f);
            gtRt.anchorMax = new Vector2(0.92f, 0.28f);
            gtRt.sizeDelta = Vector2.zero;
            gTrack.GetComponent<Image>().color = new Color(0.25f, 0.12f, 0.12f, 0.95f);

            GameObject gFill = new GameObject("G_Fill", typeof(RectTransform), typeof(Image));
            gFill.transform.SetParent(gTrack.transform, false);
            dolphinGangesBarFill = gFill.GetComponent<RectTransform>();
            dolphinGangesBarFill.anchorMin = Vector2.zero;
            dolphinGangesBarFill.anchorMax = new Vector2(1f, 1f);
            dolphinGangesBarFill.sizeDelta = Vector2.zero;
            gFill.GetComponent<Image>().color = new Color(0.95f, 0.30f, 0.25f, 0.95f);

            CreateText(gCard.transform, "2001: 225  →  Recent Survey: 159 (-29% decline)",
                mainFont, 16, FontStyle.Bold, new Color(0.85f, 0.75f, 0.75f), TextAnchor.LowerCenter,
                new Vector2(0.5f, 0.04f), new Vector2(490f, 24f));

            // Irrawaddy Card
            var iCard = CreateGlassCard(sGo.transform, new Vector2(0.72f, 0.54f), new Vector2(530f, 185f),
                new Color(0.12f, 0.08f, 0.12f, 0.96f), new Color(0.95f, 0.35f, 0.35f, 0.85f));
            iCard.name = "Card_Irrawaddy";

            CreateText(iCard.transform, "Irrawaddy Dolphin (Orcaella brevirostris)",
                mainFont, 17, FontStyle.Bold, new Color(0.95f, 0.85f, 0.85f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.86f), new Vector2(490f, 26f));

            dolphinIrrawaddyCountText = CreateText(iCard.transform, "451",
                mainFont, 56, FontStyle.Bold, new Color(1.0f, 0.35f, 0.35f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.52f), new Vector2(320f, 75f));

            GameObject iTrack = new GameObject("I_Track", typeof(RectTransform), typeof(Image));
            iTrack.transform.SetParent(iCard.transform, false);
            var itRt = iTrack.GetComponent<RectTransform>();
            itRt.anchorMin = new Vector2(0.08f, 0.16f);
            itRt.anchorMax = new Vector2(0.92f, 0.28f);
            itRt.sizeDelta = Vector2.zero;
            iTrack.GetComponent<Image>().color = new Color(0.25f, 0.12f, 0.12f, 0.95f);

            GameObject iFill = new GameObject("I_Fill", typeof(RectTransform), typeof(Image));
            iFill.transform.SetParent(iTrack.transform, false);
            dolphinIrrawaddyBarFill = iFill.GetComponent<RectTransform>();
            dolphinIrrawaddyBarFill.anchorMin = Vector2.zero;
            dolphinIrrawaddyBarFill.anchorMax = new Vector2(1f, 1f);
            dolphinIrrawaddyBarFill.sizeDelta = Vector2.zero;
            iFill.GetComponent<Image>().color = new Color(0.95f, 0.20f, 0.20f, 0.95f);

            CreateText(iCard.transform, "2006: 451  →  Recent Survey: 198 (-56% decline)",
                mainFont, 16, FontStyle.Bold, new Color(0.85f, 0.75f, 0.75f), TextAnchor.LowerCenter,
                new Vector2(0.5f, 0.04f), new Vector2(490f, 24f));

            // Dolphin Silhouette Actor for Timeline Cutscene
            GameObject dolphinActorGo = new GameObject("Dolphin_Silhouette_Actor", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Animator));
            dolphinActorGo.transform.SetParent(sGo.transform, false);
            var dRt = dolphinActorGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.54f);
            dRt.anchorMax = new Vector2(0.5f, 0.54f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = new Vector2(-520f, 0f);
            dRt.sizeDelta = new Vector2(240f, 120f);

            var dImg = dolphinActorGo.GetComponent<Image>();
#if UNITY_EDITOR
            Sprite dSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/dolphin_silhouette.png");
            if (dSprite != null) dImg.sprite = dSprite;
#endif
            dImg.color = new Color(0.35f, 0.88f, 0.98f, 0.90f);
            dImg.raycastTarget = false;

            var dShadow = dolphinActorGo.AddComponent<Shadow>();
            dShadow.effectColor = new Color(0.08f, 0.45f, 0.65f, 0.65f);
            dShadow.effectDistance = new Vector2(4f, -4f);

            // Callout Box
            dolphinQuoteBox = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.29f), new Vector2(1060f, 85f),
                new Color(0.22f, 0.06f, 0.06f, 0.96f), new Color(1.0f, 0.25f, 0.25f, 0.95f));
            dolphinQuoteBox.name = "Quote_Callout_Box";

            CreateText(dolphinQuoteBox.transform,
                "💔 \"108 dolphin deaths were officially documented between 2007–2019, with over one-third killed by entanglement in illegal fishing nets.\"",
                mainFont, 18, FontStyle.Bold, new Color(1.0f, 0.90f, 0.90f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1020f, 70f));

            var srcCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.15f), new Vector2(760f, 38f),
                new Color(0.03f, 0.06f, 0.10f, 0.92f), new Color(0.30f, 0.65f, 0.90f, 0.70f));
            srcCard.name = "Source_Card";
            CreateText(srcCard.transform, "Source: Wildlife Conservation Society (WCS) Bangladesh survey data.",
                mainFont, 16, FontStyle.Bold, new Color(0.75f, 0.90f, 1.0f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(740f, 32f));

#if UNITY_EDITOR
            var director = sGo.GetComponent<PlayableDirector>();
            if (director == null) director = sGo.AddComponent<PlayableDirector>();
            var timeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Timeline.TimelineAsset>("Assets/Timelines/Intro_Slide5_Dolphin.playable");
            if (timeline != null)
            {
                director.playableAsset = timeline;
                director.playOnAwake = false;
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            }
#endif
        }

        private void BuildSlide6_TransitionToGameplay(Transform parent)
        {
            GameObject sGo = new GameObject("Slide_6_Transition", typeof(RectTransform), typeof(CanvasGroup));
            sGo.transform.SetParent(parent, false);
            var rt = sGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            slides[5] = sGo;

            CreateBadge(sGo.transform, "📷 AUTHENTIC SUNDARBANS HABITAT • 4 SPECIES CONSERVATION INITIATIVE",
                new Color(0.08f, 0.20f, 0.12f, 0.92f), new Vector2(0.5f, 0.91f), new Vector2(680f, 32f));

            var headCard = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.81f), new Vector2(1280f, 62f),
                new Color(0.04f, 0.08f, 0.12f, 0.95f), new Color(0.95f, 0.80f, 0.25f, 0.85f));
            headCard.name = "Head_Card";
            CreateText(headCard.transform, "The Responsibility to Protect These 4 Species is in Your Hands",
                mainFont, 30, FontStyle.Bold, new Color(0.98f, 0.90f, 0.45f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(1240f, 52f));

            float[] cardXs = { -435f, -145f, 145f, 435f };
            string[] names = { "Royal Bengal Tiger", "Spotted Deer", "Ganges Dolphin", "Mother Hilsha" };
            string[] scientificNames = { "Panthera tigris", "Axis axis", "Platanista gangetica", "Tenualosa ilisha" };
            Texture2D[] speciesPhotos = { realSundarbanTigerTex, realSundarbanDeerTex, realSundarbanDolphinTex, hilshaTex };
            string[] missions = {
                "Dismantle illegal poacher snares & clear safe forest corridors",
                "Purify toxic plastic waste & restore vital grazing habitats",
                "Cleanse water pollutants & clear dangerous river contaminants",
                "Navigate past illegal gillnets during the 22-day October spawning ban"
            };
            Color[] cardColors = {
                new Color(0.95f, 0.65f, 0.15f),
                new Color(0.25f, 0.85f, 0.45f),
                new Color(0.25f, 0.75f, 0.95f),
                new Color(0.35f, 0.95f, 0.80f)
            };

            for (int i = 0; i < 4; i++)
            {
                var card = CreateGlassCard(sGo.transform, new Vector2(0.5f, 0.49f), new Vector2(280f, 290f),
                    new Color(0.05f, 0.10f, 0.15f, 0.96f), cardColors[i]);
                card.name = $"Species_Card_{i}";
                speciesCards[i] = card;
                var cRt = card.GetComponent<RectTransform>();
                cRt.anchoredPosition = new Vector2(cardXs[i], cRt.anchoredPosition.y);

                GameObject photoBox = new GameObject("Photo_Thumbnail", typeof(RectTransform), typeof(RawImage));
                photoBox.transform.SetParent(card.transform, false);
                var pRt = photoBox.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0.5f, 0.74f);
                pRt.anchorMax = new Vector2(0.5f, 0.74f);
                pRt.pivot = new Vector2(0.5f, 0.5f);
                pRt.sizeDelta = new Vector2(255f, 110f);

                var pImg = photoBox.GetComponent<RawImage>();
                if (speciesPhotos[i] != null) pImg.texture = speciesPhotos[i];
                pImg.color = Color.white;

                var pOutline = photoBox.AddComponent<Outline>();
                pOutline.effectColor = cardColors[i];
                pOutline.effectDistance = new Vector2(1.5f, -1.5f);

                CreateText(card.transform, names[i],
                    mainFont, 18, FontStyle.Bold, cardColors[i], TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.46f), new Vector2(260f, 26f));

                CreateText(card.transform, $"({scientificNames[i]})",
                    mainFont, 13, FontStyle.Italic, new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.37f), new Vector2(260f, 20f));

                CreateText(card.transform, missions[i],
                    mainFont, 15, FontStyle.Bold, new Color(0.88f, 0.92f, 0.96f), TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.18f), new Vector2(260f, 65f));
            }

            GameObject btnStartGo = new GameObject("Btn_Start_Mission", typeof(RectTransform), typeof(Image), typeof(Button));
            btnStartGo.transform.SetParent(sGo.transform, false);
            var bsRt = btnStartGo.GetComponent<RectTransform>();
            bsRt.anchorMin = new Vector2(0.5f, 0.14f);
            bsRt.anchorMax = new Vector2(0.5f, 0.14f);
            bsRt.pivot = new Vector2(0.5f, 0.5f);
            bsRt.sizeDelta = new Vector2(380f, 65f);

            var bsImg = btnStartGo.GetComponent<Image>();
            bsImg.color = new Color(0.15f, 0.75f, 0.40f, 0.98f);
            var bsOutline = btnStartGo.AddComponent<Outline>();
            bsOutline.effectColor = new Color(0.95f, 0.85f, 0.35f, 0.95f);
            bsOutline.effectDistance = new Vector2(2.5f, -2.5f);

            btnStartMission = btnStartGo.GetComponent<Button>();
            btnStartMission.onClick.AddListener(SkipIntro);

            GameObject bsTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            bsTxtGo.transform.SetParent(btnStartGo.transform, false);
            var bsTxt = bsTxtGo.GetComponent<Text>();
            bsTxt.font = mainFont;
            bsTxt.fontSize = 22;
            bsTxt.fontStyle = FontStyle.Bold;
            bsTxt.alignment = TextAnchor.MiddleCenter;
            bsTxt.color = Color.white;
            bsTxt.text = "ENTER SUNDARBANS ▶";
            bsTxt.raycastTarget = false;
            var bsO = bsTxtGo.AddComponent<Outline>();
            bsO.effectColor = new Color(0.05f, 0.25f, 0.12f, 0.98f);
            bsO.effectDistance = new Vector2(2f, -2f);
            var bsTxtRt = bsTxtGo.GetComponent<RectTransform>();
            bsTxtRt.anchorMin = Vector2.zero;
            bsTxtRt.anchorMax = Vector2.one;
            bsTxtRt.sizeDelta = Vector2.zero;
        }

        private void BuildNavigationControls(Transform parent)
        {
            GameObject skipGo = new GameObject("Btn_Skip", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(parent, false);
            var sRt = skipGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(1f, 1f);
            sRt.anchoredPosition = new Vector2(-28f, -24f);
            sRt.sizeDelta = new Vector2(140f, 44f);

            var sImg = skipGo.GetComponent<Image>();
            sImg.color = new Color(0.12f, 0.16f, 0.20f, 0.92f);
            var sOutline = skipGo.AddComponent<Outline>();
            sOutline.effectColor = new Color(0.5f, 0.6f, 0.7f, 0.8f);
            sOutline.effectDistance = new Vector2(1.5f, -1.5f);

            btnSkip = skipGo.GetComponent<Button>();
            btnSkip.onClick.AddListener(SkipIntro);

            GameObject sTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            sTxtGo.transform.SetParent(skipGo.transform, false);
            var sTxt = sTxtGo.GetComponent<Text>();
            sTxt.font = mainFont;
            sTxt.fontSize = 15;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = new Color(0.85f, 0.90f, 0.95f);
            sTxt.text = "Skip ⏭️";
            sTxt.raycastTarget = false;
            var sO = sTxtGo.AddComponent<Outline>();
            sO.effectColor = Color.black;
            sO.effectDistance = new Vector2(1.5f, -1.5f);
            var sTxtRt = sTxtGo.GetComponent<RectTransform>();
            sTxtRt.anchorMin = Vector2.zero;
            sTxtRt.anchorMax = Vector2.one;
            sTxtRt.sizeDelta = Vector2.zero;

            GameObject barGo = new GameObject("Bottom_Nav_Bar", typeof(RectTransform));
            barGo.transform.SetParent(parent, false);
            var bRt = barGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 16f);
            bRt.sizeDelta = new Vector2(760f, 54f);

            GameObject prevGo = new GameObject("Btn_Prev", typeof(RectTransform), typeof(Image), typeof(Button));
            prevGo.transform.SetParent(barGo.transform, false);
            var pRt = prevGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0.5f);
            pRt.anchorMax = new Vector2(0f, 0.5f);
            pRt.pivot = new Vector2(0f, 0.5f);
            pRt.anchoredPosition = new Vector2(0f, 0f);
            pRt.sizeDelta = new Vector2(140f, 44f);
            prevGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
            var pOutline = prevGo.AddComponent<Outline>();
            pOutline.effectColor = new Color(0.35f, 0.45f, 0.55f, 0.7f);
            pOutline.effectDistance = new Vector2(1.5f, -1.5f);
            btnPrev = prevGo.GetComponent<Button>();
            btnPrev.onClick.AddListener(PreviousSlide);

            GameObject pTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            pTxtGo.transform.SetParent(prevGo.transform, false);
            var pTxt = pTxtGo.GetComponent<Text>();
            pTxt.font = mainFont;
            pTxt.fontSize = 15;
            pTxt.fontStyle = FontStyle.Bold;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = new Color(0.85f, 0.9f, 0.95f);
            pTxt.text = "❮ PREVIOUS";
            pTxt.raycastTarget = false;
            var pO = pTxtGo.AddComponent<Outline>();
            pO.effectColor = Color.black;
            pO.effectDistance = new Vector2(1.5f, -1.5f);
            var pTxtRt = pTxtGo.GetComponent<RectTransform>();
            pTxtRt.anchorMin = Vector2.zero;
            pTxtRt.anchorMax = Vector2.one;
            pTxtRt.sizeDelta = Vector2.zero;

            GameObject nextGo = new GameObject("Btn_Next", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGo.transform.SetParent(barGo.transform, false);
            var nRt = nextGo.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(1f, 0.5f);
            nRt.anchorMax = new Vector2(1f, 0.5f);
            nRt.pivot = new Vector2(1f, 0.5f);
            nRt.anchoredPosition = new Vector2(0f, 0f);
            nRt.sizeDelta = new Vector2(160f, 44f);
            nextGo.GetComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f, 0.98f);
            var nOutline = nextGo.AddComponent<Outline>();
            nOutline.effectColor = new Color(0.95f, 0.85f, 0.35f, 0.95f);
            nOutline.effectDistance = new Vector2(1.5f, -1.5f);
            btnNext = nextGo.GetComponent<Button>();
            btnNext.onClick.AddListener(NextSlide);

            GameObject nTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            nTxtGo.transform.SetParent(nextGo.transform, false);
            var nTxt = nTxtGo.GetComponent<Text>();
            nTxt.font = mainFont;
            nTxt.fontSize = 15;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.alignment = TextAnchor.MiddleCenter;
            nTxt.color = Color.white;
            nTxt.text = "NEXT ❯";
            nTxt.raycastTarget = false;
            var nO = nTxtGo.AddComponent<Outline>();
            nO.effectColor = new Color(0.05f, 0.25f, 0.12f, 0.98f);
            nO.effectDistance = new Vector2(1.5f, -1.5f);
            var nTxtRt = nTxtGo.GetComponent<RectTransform>();
            nTxtRt.anchorMin = Vector2.zero;
            nTxtRt.anchorMax = Vector2.one;
            nTxtRt.sizeDelta = Vector2.zero;

            float dotSpacing = 28f;
            float startX = -((6 - 1) * dotSpacing) / 2f;
            for (int i = 0; i < 6; i++)
            {
                GameObject dotGo = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotGo.transform.SetParent(barGo.transform, false);
                var dRt = dotGo.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.5f, 0.5f);
                dRt.anchorMax = new Vector2(0.5f, 0.5f);
                dRt.anchoredPosition = new Vector2(startX + i * dotSpacing, 0f);
                dRt.sizeDelta = new Vector2(16f, 6f);
                var dImg = dotGo.GetComponent<Image>();
                dImg.color = (i == 0) ? new Color(0.2f, 0.95f, 0.5f, 1f) : new Color(0.4f, 0.45f, 0.5f, 0.45f);
                stepDots[i] = dImg;
            }

            GameObject lineGo = new GameObject("AutoAdvance_Progress_Track", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(parent, false);
            var lRt = lineGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(1f, 0f);
            lRt.pivot = new Vector2(0.5f, 0f);
            lRt.anchoredPosition = Vector2.zero;
            lRt.sizeDelta = new Vector2(0f, 4f);
            lineGo.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.16f, 0.75f);

            GameObject lineFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            lineFillGo.transform.SetParent(lineGo.transform, false);
            var lfRt = lineFillGo.GetComponent<RectTransform>();
            lfRt.anchorMin = Vector2.zero;
            lfRt.anchorMax = Vector2.one;
            lfRt.sizeDelta = Vector2.zero;
            autoAdvanceFill = lineFillGo.GetComponent<Image>();
            autoAdvanceFill.type = Image.Type.Filled;
            autoAdvanceFill.fillMethod = Image.FillMethod.Horizontal;
            autoAdvanceFill.fillAmount = 0f;
            autoAdvanceFill.color = new Color(0.25f, 0.92f, 0.55f, 0.95f);
        }

        private GameObject CreateGlassCard(Transform parent, Vector2 anchor, Vector2 size, Color bgColor, Color rimColor)
        {
            GameObject cardGo = new GameObject("Glass_Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            var rt = cardGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var img = cardGo.GetComponent<Image>();
            bgColor.a = Mathf.Max(bgColor.a, 0.96f);
            img.color = bgColor;
            img.raycastTarget = false;

            var outline = cardGo.AddComponent<Outline>();
            outline.effectColor = rimColor;
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            var shadow = cardGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.98f);
            shadow.effectDistance = new Vector2(5f, -5f);

            // Cyber / High-Tech Neon Glow Top Stripe
            GameObject stripeGo = new GameObject("Top_Glow_Stripe", typeof(RectTransform), typeof(Image));
            stripeGo.transform.SetParent(cardGo.transform, false);
            var sRt = stripeGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.01f, 1f);
            sRt.anchorMax = new Vector2(0.99f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.anchoredPosition = Vector2.zero;
            sRt.sizeDelta = new Vector2(0f, 3.5f);
            var sImg = stripeGo.GetComponent<Image>();
            sImg.color = rimColor;
            sImg.raycastTarget = false;

            return cardGo;
        }

        private Text CreateText(Transform parent, string content, Font font, int fontSize, FontStyle style, Color color, TextAnchor align, Vector2 anchor, Vector2 size)
        {
            if (font == null) font = LoadFont();

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(parent, false);
            var rt = txtGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;

            float px = 0.5f;
            if (align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft) px = 0f;
            else if (align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight) px = 1f;

            float py = 0.5f;
            if (align == TextAnchor.UpperLeft || align == TextAnchor.UpperCenter || align == TextAnchor.UpperRight) py = 1f;
            else if (align == TextAnchor.LowerLeft || align == TextAnchor.LowerCenter || align == TextAnchor.LowerRight) py = 0f;

            rt.pivot = new Vector2(px, py);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = content;
            txt.raycastTarget = false;

            var outline = txtGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            return txt;
        }

        private GameObject CreateBadge(Transform parent, string label, Color bgColor, Vector2 anchor, Vector2 size)
        {
            GameObject badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            var rt = badgeGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var img = badgeGo.GetComponent<Image>();
            bgColor.a = Mathf.Max(bgColor.a, 0.96f);
            img.color = bgColor;
            img.raycastTarget = false;

            var outline = badgeGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.85f, 1.0f, 0.95f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            var shadow = badgeGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.90f);
            shadow.effectDistance = new Vector2(3f, -3f);

            CreateText(badgeGo.transform, label, LoadFont(), 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), size);

            return badgeGo;
        }

        private GameObject CreatePillTag(Transform parent, string label, Vector2 anchor, Vector2 size)
        {
            GameObject tagGo = new GameObject("Pill_Tag", typeof(RectTransform), typeof(Image));
            tagGo.transform.SetParent(parent, false);
            var rt = tagGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var img = tagGo.GetComponent<Image>();
            if (roundedCapsuleSprite != null)
            {
                img.sprite = roundedCapsuleSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.03f, 0.07f, 0.05f, 0.72f);
            img.raycastTarget = false;

            var outline = tagGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.75f, 0.45f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            var shadow = tagGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(2f, -2f);

            CreateText(tagGo.transform, label, LoadFont(), 13, FontStyle.Bold,
                new Color(0.75f, 0.95f, 0.85f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), size);

            return tagGo;
        }

        private GameObject CreateUnifiedCard(Transform parent, Vector2 anchor, Vector2 size, Color bgColor)
        {
            GameObject cardGo = new GameObject("Unified_Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            var rt = cardGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var img = cardGo.GetComponent<Image>();
            if (roundedCardSprite != null)
            {
                img.sprite = roundedCardSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = bgColor;
            img.raycastTarget = false;

            var shadow = cardGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(0f, -6f);

            return cardGo;
        }

        private GameObject CreateDividerLine(Transform parent, Vector2 anchor, float width, float height = 1.5f)
        {
            GameObject lineGo = new GameObject("Divider_Line", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(parent, false);
            var rt = lineGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);

            var img = lineGo.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            img.raycastTarget = false;

            return lineGo;
        }

        private Text CreateCornerText(Transform parent, string content, Vector2 anchor, Vector2 pivot, TextAnchor align, Color color, int fontSize = 14)
        {
            GameObject txtGo = new GameObject("Corner_Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(parent, false);
            var rt = txtGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(550f, 32f);

            var txt = txtGo.GetComponent<Text>();
            txt.font = LoadFont();
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Normal;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = content;
            txt.raycastTarget = false;

            var shadow = txtGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return txt;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Sundarban/Bake Intro Data Canvas to Scene")]
        public static void BakeIntroCanvasToSceneMenu()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();
            if (controller == null)
            {
                var go = GameObject.Find("IntroSequenceController");
                if (go == null) go = new GameObject("IntroSequenceController");
                controller = go.AddComponent<IntroSequenceController>();
            }
            controller.LoadFont();
            controller.LoadBackgroundTextures();
            controller.LoadUISprites();
            controller.EnsureUIBuilt(true);

            // Wire persistent button listeners for rock-solid scene serialization
            var canvasGo = GameObject.Find("Intro_Data_Canvas");
            if (canvasGo != null)
            {
                var bSkip = canvasGo.transform.Find("Btn_Skip")?.GetComponent<Button>();
                var bNext = canvasGo.transform.Find("Bottom_Nav_Bar/Btn_Next")?.GetComponent<Button>();
                var bPrev = canvasGo.transform.Find("Bottom_Nav_Bar/Btn_Prev")?.GetComponent<Button>();
                var bStart = canvasGo.transform.Find("Slide_6_Transition/Btn_Start_Mission")?.GetComponent<Button>();

                if (bSkip != null)
                {
                    while (bSkip.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(bSkip.onClick, 0);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(bSkip.onClick, new UnityEngine.Events.UnityAction(controller.SkipIntro));
                    UnityEditor.EditorUtility.SetDirty(bSkip.gameObject);
                }
                if (bNext != null)
                {
                    while (bNext.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(bNext.onClick, 0);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(bNext.onClick, new UnityEngine.Events.UnityAction(controller.NextSlide));
                    UnityEditor.EditorUtility.SetDirty(bNext.gameObject);
                }
                if (bPrev != null)
                {
                    while (bPrev.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(bPrev.onClick, 0);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(bPrev.onClick, new UnityEngine.Events.UnityAction(controller.PreviousSlide));
                    UnityEditor.EditorUtility.SetDirty(bPrev.gameObject);
                }
                if (bStart != null)
                {
                    while (bStart.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(bStart.onClick, 0);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(bStart.onClick, new UnityEngine.Events.UnityAction(controller.SkipIntro));
                    UnityEditor.EditorUtility.SetDirty(bStart.gameObject);
                }
            }

            // Build and wire Timeline for Slide 5 cutscene
            var editorType = System.Type.GetType("SundarbanGuardian.Editor.IntroTimelineBuilder, Assembly-CSharp-Editor");
            if (editorType != null)
            {
                var method = editorType.GetMethod("BuildDolphinSlideTimeline", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null) method.Invoke(null, null);
            }

            UnityEditor.EditorUtility.SetDirty(controller.gameObject);
            var cGo = GameObject.Find("Intro_Data_Canvas");
            if (cGo != null) UnityEditor.EditorUtility.SetDirty(cGo);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[IntroSequenceController] Successfully baked NASA space & real Sundarban Intro Data Canvas into scene!");
        }

        [UnityEditor.MenuItem("Sundarban/Intro Slides/Show Slide 1 (Title)")]
        public static void ShowSlide1InEditor() { PreviewSlideInEditor(0); }

        [UnityEditor.MenuItem("Sundarban/Intro Slides/Show Slide 2 (Forest Canopy)")]
        public static void ShowSlide2InEditor() { PreviewSlideInEditor(1); }

        [UnityEditor.MenuItem("Sundarban/Intro Slides/Show Slide 3 (Tiger Population)")]
        public static void ShowSlide3InEditor() { PreviewSlideInEditor(2); }

        [UnityEditor.MenuItem("Sundarban/Intro Slides/Show Slide 4 (Spotted Deer)")]
        public static void ShowSlide4InEditor() { PreviewSlideInEditor(3); }

        [UnityEditor.MenuItem("Sundarban/Intro Slides/Show Slide 5 (Dolphin Crisis)")]
        public static void ShowSlide5InEditor() { PreviewSlideInEditor(4); }

        [UnityEditor.MenuItem("Sundarban/Intro Slides/Show Slide 6 (Mission Briefing)")]
        public static void ShowSlide6InEditor() { PreviewSlideInEditor(5); }

        private static void PreviewSlideInEditor(int slideIndex)
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();
            if (controller == null)
            {
                Debug.LogWarning("IntroSequenceController not found in scene.");
                return;
            }
            controller.EnsureSlideReferences();
            controller.IsolateSlide(slideIndex);

            var bgImg = GameObject.Find("Background_Satellite");
            if (bgImg != null)
            {
                var rawImg = bgImg.GetComponent<RawImage>();
                if (rawImg != null) rawImg.texture = controller.GetBackgroundTextureForSlide(slideIndex);
            }

            var telemetry = GameObject.Find("Orbital_Telemetry_Badge");
            if (telemetry != null) telemetry.SetActive(slideIndex == 0 || slideIndex == 1);

            UnityEditor.SceneView.RepaintAll();
            Debug.Log($"[IntroSequenceController] Previewing isolated Slide {slideIndex + 1} in Editor.");
        }
#endif
    }
}
