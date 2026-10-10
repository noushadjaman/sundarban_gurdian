using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.EditorTools
{
    public static class BuildSundarbanDeforestationZones
    {
        [MenuItem("Sundarban/Build Authentic Deforestation Zones")]
        public static void BuildZones()
        {
            var env = GameObject.Find("Mangrove_Environment_Decorations");
            if (env == null)
            {
                Debug.LogError("[Deforestation] Mangrove_Environment_Decorations not found in scene!");
                return;
            }

            var treesParent = env.transform.Find("Trees");
            var terrainTf = env.transform.Find("Terrain");

            // Load dead tree and log models
            var deadTree2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Dead Tree by Quaternius - Mcd2zYqyww/DeadTree_2.fbx");
            var deadTree1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/newdec/Dead Tree by Quaternius - MlmK5488ou/DeadTree_1.fbx");
            var deadMangrovePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LightweightNature/Tree_Lightweight_DeadMangrove.prefab");
            var deadTreeNewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NatureNew/Tree_New_DeadTree.prefab");
            var fallenLogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Detail_Mangrove_FallenLog.prefab");
            var beachDebrisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/MangroveRoots/source/230501_Beach Debris 4.fbx");

            var catalogueFallenLog = env.transform.Find("Prefabs_Catalogue/FallenLog")?.gameObject;

            GameObject[] deadTreePrefabs = new GameObject[] { deadTree2Prefab, deadTree1Prefab, deadMangrovePrefab, deadTreeNewPrefab };

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Shared materials for stumps and scarred ground
            Material barkMat = new Material(litShader);
            barkMat.name = "Deforestation_Stump_Bark";
            barkMat.color = new Color(0.24f, 0.17f, 0.11f);
            barkMat.SetFloat("_Smoothness", 0.15f);

            Material ringsMat = new Material(litShader);
            ringsMat.name = "Deforestation_Cut_WoodRings";
            ringsMat.color = new Color(0.68f, 0.52f, 0.34f);
            ringsMat.SetFloat("_Smoothness", 0.35f);

            Material sawdustMat = new Material(litShader);
            sawdustMat.name = "Deforestation_Sawdust";
            sawdustMat.color = new Color(0.72f, 0.58f, 0.36f);
            sawdustMat.SetFloat("_Smoothness", 0.05f);

            Material soilPatchMat = new Material(litShader);
            soilPatchMat.name = "Deforestation_Eroded_Soil";
            soilPatchMat.color = new Color(0.18f, 0.13f, 0.09f, 0.90f);
            soilPatchMat.SetFloat("_Smoothness", 0.1f);

            // Get or create [Danger_Zones]
            GameObject dangerRoot = GameObject.Find("[Danger_Zones]");
            if (dangerRoot == null) dangerRoot = new GameObject("[Danger_Zones]");

            // 7 Authentic NASA Landsat Deforestation & Hazard Zones along the North -> Ocean Trek:
            var zoneConfigs = new (string name, Vector3 pos, float radius, string tier, float dps, float ndvi, string desc)[]
            {
                (
                    "HazardZone_1_NorthRidgeLogging",
                    new Vector3(-32f, 0.40f, 220f),
                    18f,
                    "high",
                    7.5f,
                    0.15f,
                    "North Ridge Timber Felling Site — Severe Canopy Deforestation"
                ),
                (
                    "HazardZone_2_NorthCanopyCollapse",
                    new Vector3(25f, 0.50f, 175f),
                    18f,
                    "high",
                    8.0f,
                    0.13f,
                    "Northern Canopy Dieback & Logging Track"
                ),
                (
                    "HazardZone_3_MidForestClearing",
                    new Vector3(-28f, 0.45f, 130f),
                    18f,
                    "high",
                    7.5f,
                    0.16f,
                    "Mid-Forest Canopy Opening — Timber Thinning & Snares"
                ),
                (
                    "HazardZone_4_EastSalineBasin",
                    new Vector3(35f, 0.50f, 80f),
                    18f,
                    "medium",
                    6.0f,
                    0.20f,
                    "East Saline Mudflat Basin — High Salinity Canopy Stress"
                ),
                (
                    "HazardZone_5_StreamCrossing",
                    new Vector3(-12f, 0.35f, 35f),
                    16f,
                    "high",
                    8.0f,
                    0.14f,
                    "Tidal Stream Crossing — Mangrove Erosion & Trap Risk"
                ),
                (
                    "HazardZone_6_CoastalClearcut",
                    new Vector3(22f, 0.30f, -20f),
                    18f,
                    "high",
                    8.5f,
                    0.12f,
                    "Coastal Mangrove Clearcut — Habitat Degradation"
                ),
                (
                    "HazardZone_7_IntertidalQuagmire",
                    new Vector3(-25f, 0.30f, -60f),
                    18f,
                    "high",
                    9.0f,
                    0.11f,
                    "Intertidal Quagmire & Poacher Coastal Outpost"
                )
            };

            // Remove any legacy zones not in the current configuration
            for (int i = dangerRoot.transform.childCount - 1; i >= 0; i--)
            {
                var ch = dangerRoot.transform.GetChild(i);
                bool keep = false;
                foreach (var cfg in zoneConfigs)
                {
                    if (ch.name == cfg.name) { keep = true; break; }
                }
                if (!keep)
                {
                    Object.DestroyImmediate(ch.gameObject);
                }
            }

            int totalClearedHealthyTrees = 0;
            int totalDeadTreesPlaced = 0;
            int totalStumpsPlaced = 0;
            int totalLogsPlaced = 0;

            // Cache all healthy tree renderers once
            MeshRenderer[] allHealthyRenderers = (treesParent != null) ? treesParent.GetComponentsInChildren<MeshRenderer>(true) : new MeshRenderer[0];

            foreach (var cfg in zoneConfigs)
            {
                // 1. Precise Ground Height on Terrain
                Vector3 zonePos = cfg.pos;
                zonePos.y = GetGroundY(zonePos, terrainTf) + 0.05f;

                // 2. Find or create HazardZone GameObject
                Transform zoneTf = dangerRoot.transform.Find(cfg.name);
                GameObject zoneGo;
                if (zoneTf != null)
                {
                    zoneGo = zoneTf.gameObject;
                }
                else
                {
                    zoneGo = new GameObject(cfg.name);
                    zoneGo.transform.SetParent(dangerRoot.transform, false);
                }

                zoneGo.transform.position = zonePos;

                // Thoroughly purge any legacy ground rings, beacons, or soil discs
                var existingChildren = zoneGo.GetComponentsInChildren<Transform>(true);
                foreach (var ec in existingChildren)
                {
                    if (ec != null && ec != zoneGo.transform)
                    {
                        string ecn = ec.name;
                        if (ecn.Contains("Hazard_Ground_Ring") || ecn.Contains("Hazard_Beacon_Light") || 
                            ecn.Contains("Scarred_Deforestation_Soil") || ecn.Contains("Sensor_Beacon") || ecn.Contains("Warning_Post"))
                        {
                            Object.DestroyImmediate(ec.gameObject);
                        }
                    }
                }

                // 3. Configure HazardZoneTrigger component
                var trig = zoneGo.GetComponent<HazardZoneTrigger>();
                if (trig == null) trig = zoneGo.AddComponent<HazardZoneTrigger>();

                trig.riskTier = cfg.tier;
                trig.highDamagePerSec = cfg.dps;
                trig.mediumDamagePerSec = cfg.dps * 0.7f;
                trig.lowDamagePerSec = 3.0f;
                trig.rawDataValue = cfg.ndvi;
                trig.rawDataUnit = "NDVI";
                trig.requiredTool = ToolType.Seed;
                trig.obstacleNameEn = "Deforested Canopy Zone (High Risk)";
                trig.obstacleDescriptionEn = "Plant seeds to restore mangrove vegetation and stop damage!";
                trig.obstacleNameBn = "উজাড়কৃত বনভূমি (Deforested High-Risk Zone)";
                trig.obstacleDescription = "বীজ রোপণ করে ম্যানগ্রোভ বন পুনরুজ্জীবিত করুন।";
                trig.interactionRadius = cfg.radius;

                var col = zoneGo.GetComponent<CapsuleCollider>();
                if (col == null) col = zoneGo.AddComponent<CapsuleCollider>();
                col.isTrigger = true;
                col.radius = cfg.radius;
                col.height = 16f;
                col.center = new Vector3(0f, 7f, 0f);

                trig.EnsureHazardVisuals();

                // 4. CLEAR HEALTHY TREES within clearing radius!
                float clearingRadius = cfg.radius * 0.92f;
                foreach (var mr in allHealthyRenderers)
                {
                    if (mr == null) continue;
                    float hDist = Vector2.Distance(new Vector2(mr.bounds.center.x, mr.bounds.center.z), new Vector2(zonePos.x, zonePos.z));
                    if (hDist <= clearingRadius)
                    {
                        // Deactivate healthy tree
                        mr.gameObject.SetActive(false);
                        totalClearedHealthyTrees++;
                    }
                }

                // 5. Clean previous Deforestation_Visuals
                Transform prevVis = zoneGo.transform.Find("Deforestation_Visuals");
                if (prevVis != null)
                {
                    Object.DestroyImmediate(prevVis.gameObject);
                }

                GameObject visRoot = new GameObject("Deforestation_Visuals");
                visRoot.transform.SetParent(zoneGo.transform, false);
                visRoot.transform.localPosition = Vector3.zero;

                // 6. Natural Mud Terrain is preserved — no artificial ground discs
                // Stumps, dead trees, and fallen logs sit directly on the mangrove mud floor

                // 7. Spawn Cut Tree Stumps
                int stumpCount = Random.Range(7, 11);
                for (int s = 0; s < stumpCount; s++)
                {
                    float angle = (s / (float)stumpCount) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                    float dist = Mathf.Sqrt(Random.Range(0.08f, 0.85f)) * (clearingRadius * 0.82f);
                    Vector3 sPos = zonePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    sPos.y = GetGroundY(sPos, terrainTf);

                    float stumpRadius = Random.Range(0.6f, 1.15f);
                    float stumpHeight = Random.Range(0.55f, 1.20f);

                    GameObject stumpGo = new GameObject($"TreeStump_{s + 1}");
                    stumpGo.transform.SetParent(visRoot.transform, true);
                    stumpGo.transform.position = sPos;
                    stumpGo.transform.rotation = Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(0f, 360f), Random.Range(-3f, 3f));

                    // Stump trunk cylinder
                    GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    trunk.name = "Bark_Trunk";
                    trunk.transform.SetParent(stumpGo.transform, false);
                    trunk.transform.localPosition = new Vector3(0f, stumpHeight * 0.5f, 0f);
                    trunk.transform.localScale = new Vector3(stumpRadius * 2f, stumpHeight * 0.5f, stumpRadius * 2f);
                    trunk.GetComponent<MeshRenderer>().sharedMaterial = barkMat;

                    // Wood rings cut face on top
                    GameObject rings = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    rings.name = "Cut_WoodRings_Top";
                    rings.transform.SetParent(stumpGo.transform, false);
                    rings.transform.localPosition = new Vector3(0f, stumpHeight + 0.01f, 0f);
                    rings.transform.localScale = new Vector3(stumpRadius * 1.95f, 0.01f, stumpRadius * 1.95f);
                    rings.GetComponent<MeshRenderer>().sharedMaterial = ringsMat;
                    var rCol = rings.GetComponent<Collider>();
                    if (rCol != null) Object.DestroyImmediate(rCol);

                    // Sawdust shavings pile around base
                    GameObject sawdust = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    sawdust.name = "Sawdust_Pile";
                    sawdust.transform.SetParent(stumpGo.transform, false);
                    sawdust.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    sawdust.transform.localScale = new Vector3(stumpRadius * 2.8f, 0.015f, stumpRadius * 2.8f);
                    sawdust.GetComponent<MeshRenderer>().sharedMaterial = sawdustMat;
                    var sdCol = sawdust.GetComponent<Collider>();
                    if (sdCol != null) Object.DestroyImmediate(sdCol);

                    totalStumpsPlaced++;
                }

                // 8. Spawn Dead Trees (weathered, bare, leafless)
                int deadTreeCount = Random.Range(5, 8);
                for (int dt = 0; dt < deadTreeCount; dt++)
                {
                    float angle = (dt / (float)deadTreeCount) * Mathf.PI * 2f + Random.Range(0.1f, 0.4f);
                    float dist = Random.Range(clearingRadius * 0.35f, clearingRadius * 0.88f);
                    Vector3 dtPos = zonePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    dtPos.y = GetGroundY(dtPos, terrainTf);

                    GameObject prefab = deadTreePrefabs[Random.Range(0, deadTreePrefabs.Length)];
                    if (prefab == null) continue;

                    GameObject dtGo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (dtGo == null) dtGo = Object.Instantiate(prefab);

                    dtGo.name = $"DeadTree_{dt + 1}_{prefab.name}";
                    dtGo.transform.SetParent(visRoot.transform, true);
                    dtGo.transform.position = dtPos;

                    // Random leaning tilt as dead trees weather
                    float tiltX = Random.Range(-5f, 5f);
                    float tiltZ = Random.Range(-5f, 5f);
                    dtGo.transform.rotation = Quaternion.Euler(tiltX, Random.Range(0f, 360f), tiltZ);

                    float sc = Random.Range(1.1f, 1.55f);
                    dtGo.transform.localScale = new Vector3(sc, sc * Random.Range(0.95f, 1.2f), sc);

                    // Add CapsuleCollider for physical collision
                    var dtCol = dtGo.GetComponent<Collider>();
                    if (dtCol == null)
                    {
                        var cc = dtGo.AddComponent<CapsuleCollider>();
                        cc.radius = 0.5f * sc;
                        cc.height = 6.0f * sc;
                        cc.center = new Vector3(0f, 3.0f * sc, 0f);
                    }

                    totalDeadTreesPlaced++;
                }

                // 9. Spawn Fallen Rotting Logs & Mangrove Driftwood
                int logCount = Random.Range(5, 8);
                for (int l = 0; l < logCount; l++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Mathf.Sqrt(Random.Range(0.05f, 0.88f)) * (clearingRadius * 0.85f);
                    Vector3 lPos = zonePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    lPos.y = GetGroundY(lPos, terrainTf) + 0.12f;

                    GameObject logPrefab = (l % 2 == 0 && beachDebrisPrefab != null) ? beachDebrisPrefab :
                                           (fallenLogPrefab != null ? fallenLogPrefab : catalogueFallenLog);

                    if (logPrefab != null)
                    {
                        GameObject logGo = PrefabUtility.InstantiatePrefab(logPrefab) as GameObject;
                        if (logGo == null) logGo = Object.Instantiate(logPrefab);

                        logGo.name = $"FallenLog_{l + 1}_{logPrefab.name}";
                        logGo.transform.SetParent(visRoot.transform, true);
                        logGo.transform.position = lPos;
                        logGo.transform.rotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));

                        float lScale = Random.Range(0.9f, 1.4f);
                        logGo.transform.localScale = Vector3.one * lScale;

                        totalLogsPlaced++;
                    }
                }

                // 10. Natural Mud Floor — NO artificial discs, red rings, or artificial beacon lights.
                // The danger zone is tracked exclusively on the NASA Landsat map radar.

                EditorUtility.SetDirty(zoneGo);
                Debug.Log($"[Deforestation] Configured {cfg.name}: {cfg.desc} (Radius: {cfg.radius}m at Y={zonePos.y:F2})");
            }

            // Mark Scene Dirty
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[Deforestation] SUCCESS! Placed authentic deforestation props (cut stumps, fallen logs, dead trees) across 6 empty space hazard zones.");
        }

        private static float GetGroundY(Vector3 pos, Transform terrain)
        {
            var hits = Physics.RaycastAll(new Vector3(pos.x, 15f, pos.z), Vector3.down, 35f);
            float bestY = 0.38f;
            bool foundTerrain = false;

            foreach (var h in hits)
            {
                if (terrain != null && h.collider.transform.IsChildOf(terrain))
                {
                    bestY = h.point.y;
                    foundTerrain = true;
                    break;
                }
            }

            if (!foundTerrain)
            {
                float minAboveWater = float.MaxValue;
                foreach (var h in hits)
                {
                    if (!h.collider.isTrigger && h.point.y >= -0.5f && h.point.y < minAboveWater && h.point.y < 3.0f)
                    {
                        minAboveWater = h.point.y;
                    }
                }
                if (minAboveWater != float.MaxValue) bestY = minAboveWater;
            }

            return bestY;
        }
    }
}
