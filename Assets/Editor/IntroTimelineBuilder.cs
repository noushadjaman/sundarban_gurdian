using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace SundarbanGuardian.Editor
{
    public static class IntroTimelineBuilder
    {
        [MenuItem("Sundarban/Build Dolphin Slide Timeline")]
        public static void BuildDolphinSlideTimeline()
        {
            string timelinesDir = "Assets/Timelines";
            if (!Directory.Exists(timelinesDir))
            {
                Directory.CreateDirectory(timelinesDir);
                AssetDatabase.Refresh();
            }

            string timelinePath = Path.Combine(timelinesDir, "Intro_Slide5_Dolphin.playable");

            // Create or load TimelineAsset
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.editorSettings.frameRate = 60;
                AssetDatabase.CreateAsset(timeline, timelinePath);
            }
            else
            {
                // Clear existing tracks for clean rebuild
                var tracks = timeline.GetOutputTracks();
                foreach (var tr in tracks)
                {
                    timeline.DeleteTrack(tr);
                }
            }

            // Find or setup the Slide 5 hierarchy in scene
            var canvasGo = GameObject.Find("Intro_Data_Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[IntroTimelineBuilder] Intro_Data_Canvas not found in scene!");
                return;
            }

            var bgGo = canvasGo.transform.Find("Background_Satellite")?.gameObject;
            var slide5Go = canvasGo.transform.Find("Slide_5_DolphinCrisis")?.gameObject;

            if (slide5Go == null)
            {
                Debug.LogError("[IntroTimelineBuilder] Slide_5_DolphinCrisis not found!");
                return;
            }

            // Ensure CanvasGroup on Slide 5
            var slideCg = slide5Go.GetComponent<CanvasGroup>();
            if (slideCg == null) slideCg = slide5Go.AddComponent<CanvasGroup>();

            // Ensure Animator on Background for Timeline AnimationTrack
            if (bgGo != null && bgGo.GetComponent<Animator>() == null)
            {
                bgGo.AddComponent<Animator>();
            }

            // Ensure Animator on Slide 5
            if (slide5Go.GetComponent<Animator>() == null)
            {
                slide5Go.AddComponent<Animator>();
            }

            // Ensure Dolphin Silhouette Actor exists inside Slide 5
            Transform dolphinActorTr = slide5Go.transform.Find("Dolphin_Silhouette_Actor");
            GameObject dolphinActorGo;
            if (dolphinActorTr == null)
            {
                dolphinActorGo = new GameObject("Dolphin_Silhouette_Actor", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Animator));
                dolphinActorGo.transform.SetParent(slide5Go.transform, false);

                var dRt = dolphinActorGo.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.5f, 0.54f);
                dRt.anchorMax = new Vector2(0.5f, 0.54f);
                dRt.pivot = new Vector2(0.5f, 0.5f);
                dRt.anchoredPosition = new Vector2(-500f, 0f);
                dRt.sizeDelta = new Vector2(230f, 115f);

                var dImg = dolphinActorGo.GetComponent<Image>();
                Sprite dSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/dolphin_silhouette.png");
                if (dSprite != null) dImg.sprite = dSprite;
                dImg.color = new Color(0.35f, 0.88f, 0.98f, 0.9f);
                dImg.raycastTarget = false;

                // Subtle glowing shadow
                var shadow = dolphinActorGo.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.1f, 0.5f, 0.7f, 0.6f);
                shadow.effectDistance = new Vector2(4f, -4f);
            }
            else
            {
                dolphinActorGo = dolphinActorTr.gameObject;
                if (dolphinActorGo.GetComponent<Animator>() == null) dolphinActorGo.AddComponent<Animator>();
                if (dolphinActorGo.GetComponent<CanvasGroup>() == null) dolphinActorGo.AddComponent<CanvasGroup>();
            }

            // Find bar fills and quote box
            var gFill = slide5Go.transform.Find("Card_Ganges/G_Track/G_Fill")?.GetComponent<RectTransform>();
            if (gFill == null) gFill = slide5Go.transform.Find("Ganges Card/G_Track/G_Fill")?.GetComponent<RectTransform>();
            if (gFill == null)
            {
                // Find by deep search
                foreach (var tr in slide5Go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (tr.gameObject.name == "G_Fill") { gFill = tr; break; }
                }
            }

            var iFill = slide5Go.transform.Find("Card_Irrawaddy/I_Track/I_Fill")?.GetComponent<RectTransform>();
            if (iFill == null)
            {
                foreach (var tr in slide5Go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (tr.gameObject.name == "I_Fill") { iFill = tr; break; }
                }
            }

            // Find child glass cards by index / structure
            Transform cardGanges = null;
            Transform cardIrrawaddy = null;
            Transform quoteBox = null;

            int glassCardCount = 0;
            for (int i = 0; i < slide5Go.transform.childCount; i++)
            {
                var ch = slide5Go.transform.GetChild(i);
                if (ch.name.StartsWith("Glass_Card") || ch.name.StartsWith("Card_") || ch.name.StartsWith("Quote_"))
                {
                    glassCardCount++;
                    if (glassCardCount == 1) { ch.name = "Card_Ganges"; cardGanges = ch; }
                    else if (glassCardCount == 2) { ch.name = "Card_Irrawaddy"; cardIrrawaddy = ch; }
                    else if (glassCardCount == 3) { ch.name = "Quote_Callout_Box"; quoteBox = ch; }
                }
            }

            if (quoteBox != null)
            {
                if (quoteBox.GetComponent<CanvasGroup>() == null) quoteBox.gameObject.AddComponent<CanvasGroup>();
                if (quoteBox.GetComponent<Animator>() == null) quoteBox.gameObject.AddComponent<Animator>();
            }

            // ─────────────────────────────────────────────────────────────────
            // ── 1. Track: Ken Burns Background Motion (Zoom & Pan) ───────────
            // ─────────────────────────────────────────────────────────────────
            var bgTrack = timeline.CreateTrack<AnimationTrack>(null, "1_KenBurns_Background");
            bgTrack.CreateInfiniteClip("KenBurns_Clip");
            if (bgGo != null)
            {
                // Scale zoom 1.0 -> 1.15 over 4.5s
                var scaleX = AnimationCurve.Linear(0f, 1.0f, 4.5f, 1.15f);
                var scaleY = AnimationCurve.Linear(0f, 1.0f, 4.5f, 1.15f);
                var posX = AnimationCurve.Linear(0f, 0f, 4.5f, -32f);
                var posY = AnimationCurve.Linear(0f, 0f, 4.5f, 18f);

                bgTrack.infiniteClip.SetCurve("", typeof(Transform), "m_LocalScale.x", scaleX);
                bgTrack.infiniteClip.SetCurve("", typeof(Transform), "m_LocalScale.y", scaleY);
                bgTrack.infiniteClip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.x", posX);
                bgTrack.infiniteClip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.y", posY);
            }

            // ─────────────────────────────────────────────────────────────────
            // ── 2. Track: Slide Header & Card Entrance (Fade + Slide) ────────
            // ─────────────────────────────────────────────────────────────────
            var entranceTrack = timeline.CreateTrack<AnimationTrack>(null, "2_Slide_Entrance");
            entranceTrack.CreateInfiniteClip("Entrance_Clip");
            var alphaIn = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2.5f), new Keyframe(0.6f, 1f, 0f, 0f));
            var posYIn = new AnimationCurve(new Keyframe(0f, -36f, 0f, 90f), new Keyframe(0.65f, 0f, 0f, 0f));
            entranceTrack.infiniteClip.SetCurve("", typeof(CanvasGroup), "m_Alpha", alphaIn);
            entranceTrack.infiniteClip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.y", posYIn);

            // ─────────────────────────────────────────────────────────────────
            // ── 3. Track: Dolphin Silhouette Swimming & Population Collapse ──
            // ─────────────────────────────────────────────────────────────────
            var dolphinTrack = timeline.CreateTrack<AnimationTrack>(null, "3_Dolphin_Silhouette_SwimAndCollapse");
            dolphinTrack.CreateInfiniteClip("Dolphin_Swim_Clip");

            // Horizontal swim: moves smoothly from -520 to +500
            var swimX = new AnimationCurve(
                new Keyframe(0f, -520f, 220f, 220f),
                new Keyframe(4.5f, 500f, 220f, 220f)
            );

            // Vertical aquatic sine bob (+/- 16px)
            var swimY = new AnimationCurve(
                new Keyframe(0.0f, 0f, 45f, 45f),
                new Keyframe(0.8f, 16f, 0f, 0f),
                new Keyframe(1.6f, -14f, 0f, 0f),
                new Keyframe(2.4f, 15f, 0f, 0f),
                new Keyframe(3.2f, -12f, 0f, 0f),
                new Keyframe(4.0f, 10f, 0f, 0f),
                new Keyframe(4.5f, 0f, 0f, 0f)
            );

            // Swimming pitch tilt angle (-3 deg to +3 deg)
            var tiltZ = new AnimationCurve(
                new Keyframe(0.0f, 0f),
                new Keyframe(0.8f, -3.5f),
                new Keyframe(1.6f, 3.5f),
                new Keyframe(2.4f, -3.0f),
                new Keyframe(3.2f, 2.5f),
                new Keyframe(4.0f, -2.0f),
                new Keyframe(4.5f, 0f)
            );

            // Scale collapse: 1.25 -> 0.45 as species crashes
            var dScale = new AnimationCurve(
                new Keyframe(0.0f, 1.25f, 0f, 0f),
                new Keyframe(0.9f, 1.25f, 0f, -0.2f),
                new Keyframe(2.2f, 0.75f, -0.3f, -0.3f),
                new Keyframe(3.6f, 0.45f, 0f, 0f),
                new Keyframe(4.5f, 0.45f, 0f, 0f)
            );

            // Alpha fade: fades in strong to 0.9, then fades to 0.18 (ghostly decline)
            var dAlpha = new AnimationCurve(
                new Keyframe(0.0f, 0.0f, 3f, 3f),
                new Keyframe(0.5f, 0.90f, 0f, 0f),
                new Keyframe(1.2f, 0.90f, 0f, -0.25f),
                new Keyframe(2.5f, 0.40f, -0.25f, -0.15f),
                new Keyframe(3.8f, 0.18f, 0f, 0f),
                new Keyframe(4.5f, 0.18f, 0f, 0f)
            );

            dolphinTrack.infiniteClip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.x", swimX);
            dolphinTrack.infiniteClip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.y", swimY);
            dolphinTrack.infiniteClip.SetCurve("", typeof(Transform), "m_LocalScale.x", dScale);
            dolphinTrack.infiniteClip.SetCurve("", typeof(Transform), "m_LocalScale.y", dScale);
            dolphinTrack.infiniteClip.SetCurve("", typeof(CanvasGroup), "m_Alpha", dAlpha);

            // ─────────────────────────────────────────────────────────────────
            // ── 4. Track: Emotional Quote Pop-In at t = 2.4s ─────────────────
            // ─────────────────────────────────────────────────────────────────
            var quoteTrack = timeline.CreateTrack<AnimationTrack>(null, "4_Mortality_Quote_Callout");
            quoteTrack.CreateInfiniteClip("Quote_Callout_Clip");

            var qAlpha = new AnimationCurve(
                new Keyframe(0.0f, 0.0f),
                new Keyframe(2.3f, 0.0f),
                new Keyframe(2.8f, 1.0f)
            );

            var qScale = new AnimationCurve(
                new Keyframe(0.0f, 0.85f),
                new Keyframe(2.3f, 0.85f),
                new Keyframe(2.85f, 1.03f),
                new Keyframe(3.1f, 1.0f)
            );

            quoteTrack.infiniteClip.SetCurve("", typeof(CanvasGroup), "m_Alpha", qAlpha);
            quoteTrack.infiniteClip.SetCurve("", typeof(Transform), "m_LocalScale.x", qScale);
            quoteTrack.infiniteClip.SetCurve("", typeof(Transform), "m_LocalScale.y", qScale);

            // ─────────────────────────────────────────────────────────────────
            // ── Setup PlayableDirector on Slide 5 & Bind Tracks ──────────────
            // ─────────────────────────────────────────────────────────────────
            var director = slide5Go.GetComponent<PlayableDirector>();
            if (director == null) director = slide5Go.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

            // Wire bindings
            director.SetGenericBinding(bgTrack, bgGo);
            director.SetGenericBinding(entranceTrack, slide5Go);
            director.SetGenericBinding(dolphinTrack, dolphinActorGo);
            if (quoteBox != null) director.SetGenericBinding(quoteTrack, quoteBox.gameObject);

            // Mark assets & scene dirty
            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(slide5Go);
            AssetDatabase.SaveAssets();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[IntroTimelineBuilder] Successfully built and wired Timeline for Dolphin slide at " + timelinePath);
        }
    }
}
