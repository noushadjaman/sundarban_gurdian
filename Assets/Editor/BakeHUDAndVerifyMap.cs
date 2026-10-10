using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;
using UnityEngine.UI;

namespace SundarbanGuardian.Editor
{
    public static class BakeHUDAndVerifyMap
    {
        [MenuItem("Sundarban/Bake HUD & Verify NASA Tactical Map")]
        public static void Execute()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Debug.Log($"[BakeHUDAndVerifyMap] Opened scene: {scene.name}");

            // 1. Ensure HUD exists and bake UI
            var hud = Object.FindAnyObjectByType<HudController>();
            if (hud == null)
            {
                var hudGo = GameObject.Find("HUD");
                if (hudGo == null) hudGo = new GameObject("HUD");
                hud = hudGo.GetComponent<HudController>();
                if (hud == null) hud = hudGo.AddComponent<HudController>();
            }

            hud.EnsureCanvasAndBuildUI();
            Debug.Log("[BakeHUDAndVerifyMap] Successfully built Canvas and HUD elements!");

            // 2. Verify Minimap Tap Area
            var minimapTapArea = GameObject.Find("Minimap_Full_Tap_Area");
            if (minimapTapArea == null)
            {
                Debug.LogError("[BakeHUDAndVerifyMap] ERROR: Minimap_Full_Tap_Area not found!");
            }
            else
            {
                var btn = minimapTapArea.GetComponent<Button>();
                Debug.Log($"[BakeHUDAndVerifyMap] SUCCESS: Minimap_Full_Tap_Area found with Button (target: {btn != null})!");
            }

            // 3. Verify Tactical Map Modal
            var canvasGo = GameObject.Find("HUD_Canvas");
            var modal = canvasGo != null ? canvasGo.transform.Find("Tactical_Map_Modal")?.gameObject : GameObject.Find("Tactical_Map_Modal");
            if (modal == null)
            {
                Debug.LogError("[BakeHUDAndVerifyMap] ERROR: Tactical_Map_Modal not found!");
            }
            else
            {
                Debug.Log("[BakeHUDAndVerifyMap] SUCCESS: Tactical_Map_Modal found in hierarchy!");
            }

            // 4. Test Minimap Tap (OpenTacticalMapPopup)
            hud.OpenTacticalMapPopup();
            Debug.Log("[BakeHUDAndVerifyMap] Triggered OpenTacticalMapPopup()!");

            // 5. Verify NASA Coordinate Conversion across key world points
            var samplePoints = new (string name, float x, float z)[]
            {
                ("Center World", 0f, 0f),
                ("Poacher Gunman #1", 25f, 40f),
                ("River Mouth Net", -20f, -270f),
                ("Kotka Sanctuary Goal", 75f, 270f)
            };

            foreach (var sp in samplePoints)
            {
                (float lat, float lon) = HudController.WorldToNasaCoordinates(sp.x, sp.z);
                string dec = HudController.FormatNasaCoordinatesDecimal(lat, lon);
                string dms = HudController.FormatNasaCoordinatesDMS(lat, lon);
                Debug.Log($"[NASA Coordinates] {sp.name} ({sp.x}, {sp.z}) => {dec} | {dms}");

                if (lat < 21.70f || lat > 22.55f || lon < 89.15f || lon > 89.95f)
                {
                    Debug.LogError($"[NASA Coordinates] Coordinate out of authentic Sundarbans bounds: {lat}, {lon}");
                }
            }

            // 6. Verify Detection of Threats in Scene
            var gunmen = Object.FindObjectsByType<GunmanThreat>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var nets = Object.FindObjectsByType<IllegalNetObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var crocs = Object.FindObjectsByType<CrocodileThreat>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var boats = Object.FindObjectsByType<BoatThreat>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log($"[Threat Scan] Found {gunmen.Length} Gunmen, {nets.Length} River Nets, {crocs.Length} Crocodiles, {boats.Length} Boats.");

            // Close Tactical Map Modal before saving so it's clean on start
            hud.CloseTacticalMapPopup();

            // Save Scene
            EditorUtility.SetDirty(hud.gameObject);
            if (canvasGo == null) canvasGo = GameObject.Find("HUD_Canvas");
            if (canvasGo != null) EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[BakeHUDAndVerifyMap] SUCCESS: Scene saved with baked HUD and verified NASA tactical map!");
        }
    }
}
