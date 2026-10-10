using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SundarbanGuardian.Core;
using SundarbanGuardian.Player;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Cinematics
{
    /// <summary>
    /// Automatic 90-Second High-Intensity Competition Showcase Director.
    /// Orchestrates an adrenaline-fueled showcase featuring all key gameplay mechanics
    /// for competition judges:
    /// 00:00 - 00:15: Satellite Delta Telemetry & Intro
    /// 00:15 - 00:38: Tiger Level 1 - Poacher Gunman Evasion & Trap Destruction
    /// 00:38 - 00:58: Deer Level 2 - Seed Foraging & Deforestation Restoration (Red -> Green)
    /// 00:58 - 01:18: Dolphin Level 3 - Net Cutter Knife Action, Water Leap & Deep Purple Pollution
    /// 01:18 - 01:30: Ocean Sanctuary Victory Finale & NASA Title Card
    /// </summary>
    public class CompetitionShowcaseRecorder : MonoBehaviour
    {
        public static CompetitionShowcaseRecorder Instance { get; private set; }

        [Header("Cinematic Settings")]
        public bool autoPlayOnStart = false;
        public float totalDuration = 90.0f;
        public bool isPlayingShowcase = false;

        [Header("Frame Recording (Optional PNG Sequence)")]
        public bool recordPngSequence = false;
        public int targetFps = 30;

        private Camera mainCamera;
        private CameraFollow cameraFollow;
        private GameManager gameManager;
        private Canvas cinematicCanvas;
        private Text titleText;
        private Text subtitleText;
        private CanvasGroup titleCg;
        private float showcaseTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            gameManager = FindAnyObjectByType<GameManager>();
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraFollow = mainCamera.GetComponent<CameraFollow>();
            }

            BuildCinematicOverlay();
        }

        private void Start()
        {
            if (autoPlayOnStart)
            {
                StartCoroutine(Play90SecondShowcaseRoutine());
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.cKey.wasPressedThisFrame && !isPlayingShowcase)
                {
                    StartShowcase();
                }
            }
            catch (System.Exception) { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.C) && !isPlayingShowcase)
                {
                    StartShowcase();
                }
            }
            catch (System.Exception) { }
#endif
        }

        [ContextMenu("Start 90s Competition Showcase")]
        public void StartShowcase()
        {
            if (isPlayingShowcase) return;
            StartCoroutine(Play90SecondShowcaseRoutine());
        }

        private IEnumerator Play90SecondShowcaseRoutine()
        {
            isPlayingShowcase = true;
            showcaseTimer = 0f;
            Debug.Log("<color=cyan>[Showcase] 🎬 STARTING 90-SECOND COMPETITION SHOWCASE CLIP!</color>");

            if (recordPngSequence)
            {
                Time.captureFramerate = targetFps;
                System.IO.Directory.CreateDirectory("Recordings/CompetitionClip_90s");
            }

            // =========================================================================
            // ACT 1 (0s - 15s): NASA SATELLITE INTRO & THE DELTA
            // =========================================================================
            ShowBanner("SUNDARBAN GUARDIAN", "NASA EARTH SCIENCE & WILDLIFE CONSERVATION CHALLENGE", 4.0f);
            if (cameraFollow != null) cameraFollow.enabled = false;

            // Sweeping orbital establishing pan from sky looking down at delta
            float t = 0f;
            Vector3 startCamPos = new Vector3(0f, 65f, 290f);
            Vector3 endCamPos = new Vector3(-35f, 25f, 240f);
            Quaternion startCamRot = Quaternion.Euler(45f, 180f, 0f);
            Quaternion endCamRot = Quaternion.Euler(22f, 180f, 0f);

            while (t < 14f)
            {
                t += Time.deltaTime;
                showcaseTimer += Time.deltaTime;
                float progress = t / 14f;
                if (mainCamera != null)
                {
                    mainCamera.transform.position = Vector3.Lerp(startCamPos, endCamPos, Mathf.SmoothStep(0f, 1f, progress));
                    mainCamera.transform.rotation = Quaternion.Slerp(startCamRot, endCamRot, Mathf.SmoothStep(0f, 1f, progress));
                }
                yield return null;
            }

            // =========================================================================
            // ACT 2 (15s - 38s): TIGER LEVEL 1 — POACHER AMBUSH & TRAP DESTRUCTION
            // =========================================================================
            if (gameManager != null)
            {
                gameManager.SwitchToSpeciesSeamlessly("tiger");
            }
            if (cameraFollow != null) cameraFollow.enabled = true;

            ShowBanner("LEVEL 1: ROYAL BENGAL TIGER", "GOAL: DESTROY 10 POACHER TRAPS & REACH OCEAN SANCTUARY", 3.5f);

            var tiger = GameObject.Find("Tiger");
            if (tiger != null)
            {
                // Auto-drive Tiger forward through forest trail
                float tigerTime = 0f;
                var lac = tiger.GetComponent<LandAnimalController>();
                var toolCtrl = tiger.GetComponent<ToolActionController>();

                while (tigerTime < 22f)
                {
                    tigerTime += Time.deltaTime;
                    showcaseTimer += Time.deltaTime;

                    // Move tiger southward
                    var cc = tiger.GetComponent<CharacterController>();
                    if (cc != null && cc.enabled)
                    {
                        cc.Move((Vector3.back * 9.5f + Vector3.down * 4f) * Time.deltaTime);
                    }
                    else
                    {
                        tiger.transform.position += Vector3.back * 9.5f * Time.deltaTime;
                    }
                    tiger.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    // Periodically swipe at traps
                    if (tigerTime > 4f && tigerTime < 5f && toolCtrl != null) toolCtrl.OnHitPressed();
                    if (tigerTime > 11f && tigerTime < 12f && toolCtrl != null) toolCtrl.OnHitPressed();
                    if (tigerTime > 17f && tigerTime < 18f && toolCtrl != null) toolCtrl.OnHitPressed();

                    yield return null;
                }
            }

            // =========================================================================
            // ACT 3 (38s - 60s): DEER LEVEL 2 — SEED FORAGING & REFORESTATION
            // =========================================================================
            if (gameManager != null)
            {
                gameManager.SwitchToSpeciesSeamlessly("deer");
            }

            ShowBanner("LEVEL 2: CHITRA SPOTTED DEER", "FORAGE SEEDS & RESTORE 3 DEFORESTATION ZONES (RED -> GREEN)", 3.5f);

            var deer = GameObject.Find("Deer");
            if (deer != null)
            {
                var bp = deer.GetComponent<Backpack>();
                if (bp != null) bp.AddTool(ToolType.Seed, 5); // Give seeds for showcase planting

                float deerTime = 0f;
                var toolCtrl = deer.GetComponent<ToolActionController>();

                while (deerTime < 21f)
                {
                    deerTime += Time.deltaTime;
                    showcaseTimer += Time.deltaTime;

                    var cc = deer.GetComponent<CharacterController>();
                    if (cc != null && cc.enabled)
                    {
                        cc.Move((Vector3.back * 11f + Vector3.down * 4f) * Time.deltaTime);
                    }
                    else
                    {
                        deer.transform.position += Vector3.back * 11f * Time.deltaTime;
                    }
                    deer.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    // Plant seeds into deforestation zone at 8s
                    if (deerTime > 7.5f && deerTime < 8.5f && toolCtrl != null)
                    {
                        toolCtrl.OnHitPressed();
                    }

                    yield return null;
                }
            }

            // =========================================================================
            // ACT 4 (60s - 78s): DOLPHIN LEVEL 3 — NET CUTTER KNIFE & POLLUTED WATER
            // =========================================================================
            if (gameManager != null)
            {
                gameManager.SwitchToSpeciesSeamlessly("dolphin");
            }

            ShowBanner("LEVEL 3: GANGETIC RIVER DOLPHIN", "SLICING ILLEGAL NETS (KNIFE) & NAVIGATING DEEP PURPLE POLLUTED WATER", 3.5f);

            var dolphin = GameObject.Find("Dolphin");
            if (dolphin != null)
            {
                var bp = dolphin.GetComponent<Backpack>();
                if (bp != null)
                {
                    if (bp.GetCount(ToolType.NetCutter) < 3) bp.AddTool(ToolType.NetCutter, 3);
                    bp.SelectTool(ToolType.NetCutter);
                }

                float dolphinTime = 0f;
                var dsc = dolphin.GetComponent<DolphinSwimController>();
                var toolCtrl = dolphin.GetComponent<ToolActionController>();

                while (dolphinTime < 18f)
                {
                    dolphinTime += Time.deltaTime;
                    showcaseTimer += Time.deltaTime;

                    // Dolphin swimming downstream
                    var rb = dolphin.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.MovePosition(rb.position + Vector3.back * 10.5f * Time.deltaTime);
                    }
                    else
                    {
                        dolphin.transform.position += Vector3.back * 10.5f * Time.deltaTime;
                    }
                    dolphin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    // Action 1: Slice fishing net with knife at 5s
                    if (dolphinTime > 4.8f && dolphinTime < 5.8f && toolCtrl != null)
                    {
                        toolCtrl.OnHitPressed();
                    }

                    // Action 2: Acrobatic breach / leap over obstacles at 11s
                    if (dolphinTime > 10.5f && dolphinTime < 11.5f && dsc != null)
                    {
                        dsc.TriggerLeap();
                    }

                    yield return null;
                }
            }

            // =========================================================================
            // ACT 5 (78s - 90s): OCEAN SANCTUARY FINALE & CHAMPIONSHIP VICTORY
            // =========================================================================
            ShowBanner("ECOSYSTEM DEFENDED • MISSION VICTORY", "100% SUNDARBANS HABITAT RESTORED • NASA SPACE APPS 2026", 8.0f);

            if (cameraFollow != null) cameraFollow.enabled = false;
            float finaleTime = 0f;
            Vector3 finStartCam = (mainCamera != null) ? mainCamera.transform.position : Vector3.zero;
            Vector3 finEndCam = finStartCam + new Vector3(0f, 18f, -25f);

            while (finaleTime < 12f)
            {
                finaleTime += Time.deltaTime;
                showcaseTimer += Time.deltaTime;
                float progress = finaleTime / 12f;

                if (mainCamera != null)
                {
                    mainCamera.transform.position = Vector3.Lerp(finStartCam, finEndCam, Mathf.SmoothStep(0f, 1f, progress));
                    mainCamera.transform.LookAt(finStartCam + Vector3.back * 30f);
                }

                yield return null;
            }

            if (recordPngSequence)
            {
                Time.captureFramerate = 0;
            }

            isPlayingShowcase = false;
            if (cameraFollow != null) cameraFollow.enabled = true;
            Debug.Log("<color=green>[Showcase] 🎬 90-SECOND COMPETITION SHOWCASE FINISHED SUCCESSFULLY!</color>");
        }

        private void ShowBanner(string main, string sub, float duration)
        {
            if (titleText != null) titleText.text = main;
            if (subtitleText != null) subtitleText.text = sub;
            StartCoroutine(FadeBannerRoutine(duration));
        }

        private IEnumerator FadeBannerRoutine(float duration)
        {
            if (titleCg == null) yield break;
            float elapsed = 0f;

            // Fade in
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                titleCg.alpha = Mathf.Clamp01(elapsed / 0.5f);
                yield return null;
            }
            titleCg.alpha = 1f;

            yield return new WaitForSeconds(duration);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                titleCg.alpha = 1f - Mathf.Clamp01(elapsed / 0.6f);
                yield return null;
            }
            titleCg.alpha = 0f;
        }

        private void BuildCinematicOverlay()
        {
            var canvasGo = new GameObject("Cinematic_Showcase_Canvas");
            canvasGo.transform.SetParent(transform, false);
            cinematicCanvas = canvasGo.AddComponent<Canvas>();
            cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cinematicCanvas.sortingOrder = 999;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bannerGo = new GameObject("Showcase_Banner_Box", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            bannerGo.transform.SetParent(canvasGo.transform, false);
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.88f);
            rt.anchorMax = new Vector2(0.5f, 0.88f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(850f, 75f);
            bannerGo.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.12f, 0.88f);

            var outline = bannerGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.95f, 0.55f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            titleCg = bannerGo.GetComponent<CanvasGroup>();
            titleCg.alpha = 0f;

            Font font = Resources.Load<Font>("Fonts/SegoeUI");
            if (font == null) { try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {} }

            // Main Title
            var titleGo = new GameObject("Main_Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(bannerGo.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.02f, 0.45f);
            tRt.anchorMax = new Vector2(0.98f, 0.95f);
            tRt.sizeDelta = Vector2.zero;
            titleText = titleGo.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.35f, 0.98f, 0.75f, 1f);

            // Subtitle
            var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(bannerGo.transform, false);
            var sRt = subGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.02f, 0.05f);
            sRt.anchorMax = new Vector2(0.98f, 0.45f);
            sRt.sizeDelta = Vector2.zero;
            subtitleText = subGo.GetComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 12;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.85f, 0.92f, 0.98f, 0.95f);
        }
    }
}
