using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SundarbanGuardian.Core;
using SundarbanGuardian.Player;
using SundarbanGuardian.UI;

namespace SundarbanGuardian.Editor
{
    [InitializeOnLoad]
    public static class DiagnosticBlackScreenChecker
    {
        static DiagnosticBlackScreenChecker()
        {
            EditorApplication.delayCall += RunDiagnosis;
        }

        [MenuItem("Sundarban/Run Black Screen Diagnosis")]
        public static void RunDiagnosis()
        {
            Debug.Log("<color=cyan>=== STARTING BLACK SCREEN DIAGNOSIS ===</color>");

            // 1. Check Active Scene
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[Diagnosis] Current Active Scene: {scene.name} (Path: {scene.path}, IsLoaded: {scene.isLoaded})");
            if (scene.path != "Assets/Scenes/SampleScene.unity")
            {
                Debug.Log("[Diagnosis] Opening Assets/Scenes/SampleScene.unity...");
                scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // 2. Check Cameras
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[Diagnosis] Total Cameras in scene: {cameras.Length}");
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[Diagnosis] CRITICAL: Camera.main IS NULL! No camera with tag 'MainCamera' found!");
            }
            else
            {
                Debug.Log($"[Diagnosis] Main Camera: {mainCam.name}, Enabled: {mainCam.enabled}, GameObject Active: {mainCam.gameObject.activeInHierarchy}, Position: {mainCam.transform.position}, CullingMask: {mainCam.cullingMask}, ClearFlags: {mainCam.clearFlags}, BackgroundColor: {mainCam.backgroundColor}");
                var cf = mainCam.GetComponent<CameraFollow>();
                Debug.Log($"[Diagnosis] CameraFollow component: {(cf != null ? "Present" : "MISSING")}, Target: {(cf != null && cf.target != null ? cf.target.name : "NULL")}");
            }

            // 3. Check Render Pipeline Asset
            var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            Debug.Log($"[Diagnosis] Current Render Pipeline: {(currentRP != null ? currentRP.name : "NULL (Legacy Pipeline!)")}");
            var defaultRP = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            Debug.Log($"[Diagnosis] GraphicsSettings Default RP: {(defaultRP != null ? defaultRP.name : "NULL")}");

            // 4. Check Lighting
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int activeLights = 0;
            Light mainSun = null;
            foreach (var l in lights)
            {
                if (l.enabled && l.gameObject.activeInHierarchy)
                {
                    activeLights++;
                    if (l.type == LightType.Directional && mainSun == null) mainSun = l;
                }
            }
            Debug.Log($"[Diagnosis] Total Lights: {lights.Length}, Active: {activeLights}, Main Sun: {(mainSun != null ? mainSun.name + " (Intensity: " + mainSun.intensity + ", Color: " + mainSun.color + ")" : "NONE!")}");
            Debug.Log($"[Diagnosis] RenderSettings Ambient Mode: {RenderSettings.ambientMode}, Ambient Light: {RenderSettings.ambientLight}, Skybox: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "NULL")}");

            // 5. Check Canvases and Overlays
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[Diagnosis] Total Canvases: {canvases.Length}");
            foreach (var c in canvases)
            {
                var cg = c.GetComponent<CanvasGroup>();
                Debug.Log($"[Diagnosis] Canvas '{c.name}' (Active: {c.gameObject.activeInHierarchy}, Order: {c.sortingOrder}, Mode: {c.renderMode}, CanvasGroup Alpha: {(cg != null ? cg.alpha.ToString() : "N/A")})");
            }

            // 6. Check IntroSequenceController
            var intro = UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();
            if (intro != null)
            {
                Debug.Log($"[Diagnosis] IntroSequenceController found on '{intro.name}', Active: {intro.gameObject.activeInHierarchy}, playOnStart: {intro.playOnStart}, HasSeenPlayerPref: {PlayerPrefs.GetInt("HasSeenIntroDataVideo", 0)}");
            }
            else
            {
                Debug.LogWarning("[Diagnosis] IntroSequenceController NOT found in scene!");
            }

            // 7. Check Species GameObjects
            string[] speciesNames = { "Tiger", "Deer", "Dolphin", "Hilsha" };
            foreach (var sp in speciesNames)
            {
                var go = GameObject.Find(sp);
                if (go == null)
                {
                    // Search inactive
                    foreach (var cand in Resources.FindObjectsOfTypeAll<GameObject>())
                    {
                        if (cand.hideFlags == HideFlags.None && cand.scene.isLoaded && cand.name.Equals(sp, StringComparison.OrdinalIgnoreCase))
                        {
                            go = cand;
                            break;
                        }
                    }
                }
                Debug.Log($"[Diagnosis] Species '{sp}': {(go != null ? "Found (Active: " + go.activeInHierarchy + ", Pos: " + go.transform.position + ")" : "NOT FOUND!")}");
            }

            // 8. Test Render Camera to texture and check average brightness
            if (mainCam != null)
            {
                try
                {
                    RenderTexture rt = new RenderTexture(256, 256, 24);
                    mainCam.targetTexture = rt;
                    mainCam.Render();
                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(256, 256, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                    tex.Apply();
                    mainCam.targetTexture = null;
                    RenderTexture.active = null;

                    Color[] pixels = tex.GetPixels();
                    float totalBrightness = 0f;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        totalBrightness += (pixels[i].r + pixels[i].g + pixels[i].b) / 3f;
                    }
                    float avgBrightness = totalBrightness / pixels.Length;
                    Debug.Log($"[Diagnosis] Test Camera Render: Average pixel brightness = {avgBrightness:F4} (0.0 = completely black!)");

                    string previewPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectScreenshots", "diagnosis_camera_render.png");
                    File.WriteAllBytes(previewPath, tex.EncodeToPNG());
                    Debug.Log($"[Diagnosis] Saved camera render preview to: {previewPath}");
                    UnityEngine.Object.DestroyImmediate(tex);
                    UnityEngine.Object.DestroyImmediate(rt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Diagnosis] Camera render test failed with exception: {ex}");
                }
            }

            Debug.Log("<color=cyan>=== DIAGNOSIS COMPLETE ===</color>");
        }
    }
}
