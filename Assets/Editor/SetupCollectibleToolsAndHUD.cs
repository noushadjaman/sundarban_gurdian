using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.EditorTools
{
    public static class SetupCollectibleToolsAndHUD
    {
        [MenuItem("Sundarban/Setup Danger Zones & Food Challenge")]
        [MenuItem("Sundarban/Setup Collectible Tools & Rebuild 2X HUD")]
        public static void ExecuteSetup()
        {
            // 1. Configure Backpacks to have limited inventory with 0 starting tools and 1 Emergency Food Ration
            var backpacks = Object.FindObjectsByType<Backpack>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in backpacks)
            {
                b.startingCountPerTool = 0;
                b.foodCount = 1;
                b.EnsureInitialized();
                EditorUtility.SetDirty(b);
                Debug.Log($"[Setup] Configured {b.gameObject.name} backpack: Seed=0, Food=1 (Harder Survival Mode)");
            }

            var nutritions = Object.FindObjectsByType<SundarbanGuardian.Player.PlayerNutrition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var n in nutritions)
            {
                n.foodCount = 1;
                n.healPerFood = 25f;
                n.autoEatThreshold = 0.25f;
                EditorUtility.SetDirty(n);
                Debug.Log($"[Setup] Configured {n.gameObject.name} PlayerNutrition: Food=1, Heal=25, AutoEat=0.25");
            }

            // 2. Setup Danger Zones (6 Deforestation Zones on authentic empty terrain clearings)
            BuildSundarbanDeforestationZones.BuildZones();

            // 3. Setup World Food Pickups (Keora Fruits / Nutrition Rations)
            SetupFoodPickups();

            // 4. Setup Collectible Tools (Seeds, Net-Cutters, Cleansers, Trap Destroyers)
            SetupCollectibleTools();

            // 5. Rebuild HUD Canvas with 2X visible buttons, DROP button, and EAT 🍗 button
            var hud = Object.FindAnyObjectByType<HudController>();
            if (hud != null)
            {
                hud.EnsureCanvasAndBuildUI();
                EditorUtility.SetDirty(hud);
                Debug.Log("[Setup] Rebuilt HUD Canvas with 2X tools, DROP [G], and EAT 🍗 [E] button.");
            }

            // 6. Save Scene
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[Setup] Saved scene successfully: {activeScene.path}");
        }

        public static void SetupDangerZones()
        {
            BuildSundarbanDeforestationZones.BuildZones();
        }

        public static void SetupFoodPickups()
        {
            GameObject root = GameObject.Find("[Food_Pickups]");
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
            root = new GameObject("[Food_Pickups]");

            // 3 Scarce Food Spawns (Keora Fruits / Sustenance Rations)
            var foodSpawns = new Vector3[]
            {
                // 3 Scarce Keora Fruit Pickups across the 300m corridor
                new Vector3(-38f, 0.4f, 45f),
                new Vector3(12f, 0.4f, 115f),
                new Vector3(-15f, 0.4f, 200f)
            };

            int count = 0;
            foreach (var sp in foodSpawns)
            {
                Vector3 p = sp;
                if (Physics.Raycast(p + Vector3.up * 12f, Vector3.down, out RaycastHit hit, 25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    p.y = hit.point.y + 0.35f;
                }

                GameObject itemGo = new GameObject($"FoodItem_Keora_{count}");
                itemGo.transform.SetParent(root.transform, false);
                itemGo.transform.position = p;

                var food = itemGo.AddComponent<FoodItem>();
                food.targetSpecies = "all";
                food.healAmount = 25f;
                food.Ensure3DVisualAsset();
                food.EnsureTriggerCollider();

                EditorUtility.SetDirty(itemGo);
                count++;
            }

            Debug.Log($"[Setup] Spawned {count} 3D Food Pickups (Keora Fruits) in world.");
        }

        public static void SetupCollectibleTools()
        {
            GameObject root = GameObject.Find("[Collectible_Tools]");
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
            root = new GameObject("[Collectible_Tools]");

            // 6 Scarce World Spawns (makes game challenging, unclutters minimap)
            var spawns = new (ToolType type, Vector3 pos)[]
            {
                (ToolType.Seed, new Vector3(-12f, 0.4f, 35f)),
                (ToolType.NetCutter, new Vector3(38f, 0.4f, 75f)),
                (ToolType.Seed, new Vector3(-18f, 0.4f, 95f)),
                (ToolType.TrapDestroyer, new Vector3(-55f, 0.4f, 135f)),
                (ToolType.CleaningLiquid, new Vector3(45f, 0.4f, 160f)),
                (ToolType.Seed, new Vector3(15f, 0.4f, 210f))
            };

            int count = 0;
            foreach (var sp in spawns)
            {
                Vector3 p = sp.pos;
                if (Physics.Raycast(p + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    p.y = hit.point.y + 0.35f;
                }

                GameObject itemGo = new GameObject($"Pickup_{sp.type}_{count}");
                itemGo.transform.SetParent(root.transform, false);
                itemGo.transform.position = p;

                var pickup = itemGo.AddComponent<ToolPickupItem>();
                pickup.toolType = sp.type;
                pickup.amount = (sp.type == ToolType.Seed) ? 2 : 1;
                pickup.Ensure3DVisualAsset();
                pickup.EnsureTriggerCollider();

                EditorUtility.SetDirty(itemGo);
                count++;
            }

            Debug.Log($"[Setup] Spawned {count} 3D collectible tools in world.");
        }

        [MenuItem("Sundarban/Show Given GLB Only (Deactivate Previous Layers)")]
        public static void ShowGivenGlbOnly()
        {
            string[] layersToDeactivate = new string[] {
                "Sea_Beach_Shorelines",
                "Tidal_Canals_Khari",
                "Map_Boundaries_and_Mountains",
                "BayOfBengal_Ocean_System",
                "River_Saltwater_Crocodile",
                "Ground_Terrain",
                "Estuary_Mud_Spit",
                "Main_River_Shibsa",
                "Sundarban_Riverbanks_Land",
                "Forest_Mud_Paths",
                "Dense_Mangrove_Forest",
                "Hazards_And_Objectives"
            };

            foreach (var name in layersToDeactivate)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    go.SetActive(false);
                    EditorUtility.SetDirty(go);
                    Debug.Log($"[GlbOnly] Deactivated: {name}");
                }
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.name.StartsWith("GoalMarker") || (root.name.StartsWith("HazardZone") && root.name != "[Danger_Zones]"))
                {
                    root.SetActive(false);
                    EditorUtility.SetDirty(root);
                }
            }

            var glb = GameObject.Find("Mangrove_Environment_Decorations");
            if (glb != null)
            {
                glb.SetActive(true);
                for (int i = 0; i < glb.transform.childCount; i++)
                {
                    var c = glb.transform.GetChild(i);
                    if (c.name != "Prefabs_Catalogue")
                    {
                        c.gameObject.SetActive(true);
                        EditorUtility.SetDirty(c.gameObject);
                    }
                }
                EditorUtility.SetDirty(glb);
                Debug.Log("[GlbOnly] Activated given GLB: Mangrove_Environment_Decorations");
            }

            // Ensure our challenge gameplay elements remain active
            var dzRoot = GameObject.Find("[Danger_Zones]");
            if (dzRoot != null) dzRoot.SetActive(true);

            var foodRoot = GameObject.Find("[Food_Pickups]");
            if (foodRoot != null) foodRoot.SetActive(true);

            var toolsRoot = GameObject.Find("[Collectible_Tools]");
            if (toolsRoot != null) toolsRoot.SetActive(true);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[GlbOnly] Scene saved. Only the given GLB environment and challenge gameplay are now visible!");
        }
    }
}
