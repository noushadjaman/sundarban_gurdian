using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Player;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    public static class BuildSundarbanWorld
    {
        [MenuItem("Sundarban/Build Complete Authentic World")]
        public static void BuildWorld()
        {
            Debug.Log("[BuildSundarbanWorld] Starting world generation...");

            // 1. Load Assets
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Ground.mat");
            var riverMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            var oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_OceanWater.mat");

            var riverFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/river-scene/river.fbx");
            var normalTreesFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Normal Trees.fbx");
            var redMangroveObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/RedMangrove/RedMangrove_Tree.obj");
            var debrisFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/MangroveRoots/source/230501_Beach Debris 4.fbx");
            var coconutPalm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/CoconutPalmTree.obj");
            var evergladesPalm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/EvergladesPalmTree.obj");
            var grassFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Grass.fbx");
            var flowersFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Flowers.fbx");

            // Extract tree variants from Normal Trees.fbx
            List<GameObject> treePrefabs = new List<GameObject>();
            if (normalTreesFbx != null)
            {
                for (int i = 0; i < normalTreesFbx.transform.childCount; i++)
                {
                    var child = normalTreesFbx.transform.GetChild(i).gameObject;
                    if (child.GetComponent<MeshFilter>() != null)
                        treePrefabs.Add(child);
                }
            }

            // Extract grass & flower variants
            List<GameObject> grassPrefabs = new List<GameObject>();
            if (grassFbx != null)
            {
                for (int i = 0; i < grassFbx.transform.childCount; i++)
                {
                    var child = grassFbx.transform.GetChild(i).gameObject;
                    if (child.GetComponent<MeshFilter>() != null)
                        grassPrefabs.Add(child);
                }
            }

            List<GameObject> flowerPrefabs = new List<GameObject>();
            if (flowersFbx != null)
            {
                for (int i = 0; i < flowersFbx.transform.childCount; i++)
                {
                    var child = flowersFbx.transform.GetChild(i).gameObject;
                    if (child.GetComponent<MeshFilter>() != null)
                        flowerPrefabs.Add(child);
                }
            }

            // 2. Camera & Infinite Atmospheric Fog
            var cam = Camera.main;
            if (cam != null)
            {
                cam.farClipPlane = 1600f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 250f;
            RenderSettings.fogEndDistance = 1300f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.88f); // coastal sea mist

            // 3. Ground Terrains (Western Land & Estuary Spit)
            var groundTerrain = GameObject.Find("Ground_Terrain");
            if (groundTerrain == null)
            {
                groundTerrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
                groundTerrain.name = "Ground_Terrain";
            }
            groundTerrain.transform.position = new Vector3(-50f, 0f, 50f);
            groundTerrain.transform.localScale = new Vector3(14f, 1f, 25f);
            var gtRend = groundTerrain.GetComponent<MeshRenderer>();
            if (gtRend != null && groundMat != null) gtRend.sharedMaterial = groundMat;

            // Estuary mud spit connecting southern land to ocean
            var estuarySpit = GameObject.Find("Estuary_Mud_Spit");
            if (estuarySpit == null)
            {
                estuarySpit = GameObject.CreatePrimitive(PrimitiveType.Plane);
                estuarySpit.name = "Estuary_Mud_Spit";
            }
            estuarySpit.transform.position = new Vector3(-50f, 0f, -84f);
            estuarySpit.transform.localScale = new Vector3(14f, 1f, 1.9f);
            var esRend = estuarySpit.GetComponent<MeshRenderer>();
            if (esRend != null && groundMat != null) esRend.sharedMaterial = groundMat;

            // 4. River System (3 segments along X = 42)
            var oldRiverSystem = GameObject.Find("River_System");
            if (oldRiverSystem != null) Object.DestroyImmediate(oldRiverSystem);

            var oldFlatRiver = GameObject.Find("River_Water");
            if (oldFlatRiver != null) oldFlatRiver.SetActive(false);

            var riverParent = new GameObject("River_System");
            float[] zOffsets = new float[] { -46.4f, 50.0f, 146.4f };
            for (int i = 0; i < zOffsets.Length; i++)
            {
                var seg = Object.Instantiate(riverFbx, riverParent.transform);
                seg.name = $"River_Segment_{i + 1}";
                seg.transform.position = new Vector3(42f, -0.4f, zOffsets[i]);
                seg.transform.localScale = new Vector3(20f, 10f, 20f);

                var cam1 = seg.transform.Find("Camera");
                if (cam1 != null) Object.DestroyImmediate(cam1.gameObject);
                var cam2 = seg.transform.Find("Camera.001");
                if (cam2 != null) Object.DestroyImmediate(cam2.gameObject);

                var gTr = seg.transform.Find("Ground");
                if (gTr != null)
                {
                    var gRend = gTr.GetComponent<MeshRenderer>();
                    if (gRend != null && groundMat != null) gRend.sharedMaterial = groundMat;
                    var gCol = gTr.gameObject.GetComponent<MeshCollider>();
                    if (gCol == null) gCol = gTr.gameObject.AddComponent<MeshCollider>();
                }

                var rTr = seg.transform.Find("river");
                if (rTr != null)
                {
                    var rRend = rTr.GetComponent<MeshRenderer>();
                    if (rRend != null && riverMat != null) rRend.sharedMaterial = riverMat;
                }
            }

            // 5. Ocean System (Bay of Bengal Infinite View)
            var oldOcean = GameObject.Find("BayOfBengal_Ocean");
            if (oldOcean != null) Object.DestroyImmediate(oldOcean);

            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "BayOfBengal_Ocean";
            // Starts at Z = -93.5 and extends south to Z = -1293.5 (size 1200m), width 1400m
            ocean.transform.position = new Vector3(0f, 0.08f, -693.5f);
            ocean.transform.localScale = new Vector3(140f, 1f, 120f);

            var oRend = ocean.GetComponent<MeshRenderer>();
            if (oRend != null && oceanMat != null) oRend.sharedMaterial = oceanMat;

            if (ocean.GetComponent<OceanWaterAnimator>() == null)
                ocean.AddComponent<OceanWaterAnimator>();

            // 6. Forest & Biomes Generation
            var oldForest = GameObject.Find("Mangrove_Forest");
            if (oldForest != null) Object.DestroyImmediate(oldForest);

            var forestParent = new GameObject("Mangrove_Forest");
            var sanctuariesParent = new GameObject("Safe_Sanctuaries").transform;
            sanctuariesParent.SetParent(forestParent.transform);
            var dangerParent = new GameObject("Danger_Mudflats").transform;
            dangerParent.SetParent(forestParent.transform);
            var riverbanksParent = new GameObject("Riverbank_Mangroves").transform;
            riverbanksParent.SetParent(forestParent.transform);
            var generalParent = new GameObject("Forest_Trails").transform;
            generalParent.SetParent(forestParent.transform);
            var floraParent = new GameObject("Undergrowth_Flora").transform;
            floraParent.SetParent(forestParent.transform);
            var pollutionParent = new GameObject("River_Pollution_Hazards").transform;
            pollutionParent.SetParent(forestParent.transform);

            Random.InitState(1971);

            // Safe Sanctuary Centers (Kotka, East Core, Mandarbaria)
            Vector3[] safeCenters = new Vector3[]
            {
                new Vector3(-35f, 0f, 25f),
                new Vector3(-30f, 0f, 85f),
                new Vector3(-65f, 0f, 40f)
            };
            float[] safeRadii = new float[] { 22f, 25f, 20f };

            // Populate Safe Sanctuaries (Ultra dense canopy trees, grass, flowers)
            for (int s = 0; s < safeCenters.Length; s++)
            {
                Vector3 center = safeCenters[s];
                float radius = safeRadii[s];

                int treeCount = 75;
                for (int t = 0; t < treeCount; t++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
                    Vector3 pos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                    if (treePrefabs.Count > 0)
                    {
                        var p = treePrefabs[Random.Range(0, treePrefabs.Count)];
                        var tree = Object.Instantiate(p, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), sanctuariesParent);
                        float sVal = Random.Range(0.85f, 1.25f);
                        tree.transform.localScale = Vector3.one * sVal;
                    }

                    // Grass clumps
                    if (grassPrefabs.Count > 0 && t % 2 == 0)
                    {
                        var gp = grassPrefabs[Random.Range(0, grassPrefabs.Count)];
                        var gPos = pos + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                        var gr = Object.Instantiate(gp, gPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), floraParent);
                        gr.transform.localScale = Vector3.one * Random.Range(1.5f, 3.0f);
                    }

                    // Wild flowers
                    if (flowerPrefabs.Count > 0 && t % 5 == 0)
                    {
                        var fp = flowerPrefabs[Random.Range(0, flowerPrefabs.Count)];
                        var fPos = pos + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                        var fl = Object.Instantiate(fp, fPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), floraParent);
                        fl.transform.localScale = Vector3.one * Random.Range(1.2f, 2.0f);
                    }
                }
            }

            // Danger Zones (Poacher Mudflat, Hypersaline Flat, Encroachment Edge)
            Vector3[] dangerCenters = new Vector3[]
            {
                new Vector3(-8f, 0f, 50f),
                new Vector3(-12f, 0f, 115f),
                new Vector3(-45f, 0f, 135f)
            };
            float[] dangerRadii = new float[] { 14f, 15f, 16f };
            string[] riskTiers = new string[] { "high", "medium", "high" };
            float[] dataValues = new float[] { 0.18f, 28.5f, 0.12f };
            string[] dataUnits = new string[] { "NDVI canopy loss", "PSU hypersalinity", "NDVI deforestation" };

            for (int d = 0; d < dangerCenters.Length; d++)
            {
                Vector3 center = dangerCenters[d];
                float radius = dangerRadii[d];

                // Hazard trigger zone cylinder
                var hazardGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hazardGo.name = $"DangerZone_{riskTiers[d]}_{d + 1}";
                hazardGo.transform.SetParent(dangerParent);
                hazardGo.transform.position = new Vector3(center.x, 0.05f, center.z);
                hazardGo.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

                var col = hazardGo.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var trigger = hazardGo.AddComponent<HazardZoneTrigger>();
                trigger.riskTier = riskTiers[d];
                trigger.rawDataValue = dataValues[d];
                trigger.rawDataUnit = dataUnits[d];

                // Distinct translucent warning color
                var hRend = hazardGo.GetComponent<MeshRenderer>();
                if (hRend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    Color c = riskTiers[d] == "high" ? new Color(0.85f, 0.12f, 0.12f, 0.45f) : new Color(0.95f, 0.65f, 0.1f, 0.4f);
                    mat.color = c;
                    hRend.sharedMaterial = mat;
                }

                // Barren / dead mangrove roots & debris inside danger zone
                if (debrisFbx != null)
                {
                    for (int db = 0; db < 6; db++)
                    {
                        float a = Random.Range(0f, Mathf.PI * 2f);
                        float r = Random.Range(2f, radius * 0.8f);
                        Vector3 dbPos = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                        var debInst = Object.Instantiate(debrisFbx, dbPos, Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f)), dangerParent);
                        debInst.transform.localScale = Vector3.one * Random.Range(1.5f, 2.5f);
                    }
                }
            }

            // Riverbank Flora (Palms & Red Mangroves along western bank X = 17 to 22)
            for (float z = -80f; z <= 170f; z += 5.5f)
            {
                float x = Random.Range(17.5f, 21.5f);
                Vector3 pos = new Vector3(x, 0f, z);

                // Alternate between Coconut Palm, Everglades Palm, and Red Mangrove
                int choice = Random.Range(0, 3);
                if (choice == 0 && coconutPalm != null)
                {
                    // Tilt slightly toward the water (+X)
                    var rot = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(8f, 18f));
                    var p = Object.Instantiate(coconutPalm, pos, rot, riverbanksParent);
                    p.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                }
                else if (choice == 1 && evergladesPalm != null)
                {
                    var rot = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(6f, 15f));
                    var p = Object.Instantiate(evergladesPalm, pos, rot, riverbanksParent);
                    p.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                }
                else if (redMangroveObj != null)
                {
                    var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    var rm = Object.Instantiate(redMangroveObj, pos, rot, riverbanksParent);
                    rm.transform.localScale = Vector3.one * Random.Range(0.0022f, 0.0032f);
                }

                // Add mangrove roots debris at water's edge
                if (debrisFbx != null && Random.value > 0.4f)
                {
                    var rPos = new Vector3(Random.Range(20f, 22.5f), 0f, z + Random.Range(-2f, 2f));
                    var deb = Object.Instantiate(debrisFbx, rPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbanksParent);
                    deb.transform.localScale = Vector3.one * Random.Range(1.2f, 2.0f);
                }
            }

            // General Trails & Forest Canopy (leaving spawn (0,0,0) clear within 8m)
            for (float x = -105f; x <= 14f; x += 11f)
            {
                for (float z = -65f; z <= 165f; z += 11f)
                {
                    Vector3 pos = new Vector3(x + Random.Range(-4f, 4f), 0f, z + Random.Range(-4f, 4f));

                    // Skip spawn clearing
                    if (pos.magnitude < 8.5f) continue;

                    // Skip if inside danger zones
                    bool inDanger = false;
                    for (int d = 0; d < dangerCenters.Length; d++)
                    {
                        if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), dangerCenters[d]) < dangerRadii[d] + 2f)
                        {
                            inDanger = true;
                            break;
                        }
                    }
                    if (inDanger) continue;

                    // Scatter trees
                    if (treePrefabs.Count > 0 && Random.value > 0.35f)
                    {
                        var tp = treePrefabs[Random.Range(0, treePrefabs.Count)];
                        var tr = Object.Instantiate(tp, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), generalParent);
                        tr.transform.localScale = Vector3.one * Random.Range(0.85f, 1.15f);
                    }

                    // Scatter grass
                    if (grassPrefabs.Count > 0 && Random.value > 0.4f)
                    {
                        var gp = grassPrefabs[Random.Range(0, grassPrefabs.Count)];
                        var gr = Object.Instantiate(gp, pos + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), floraParent);
                        gr.transform.localScale = Vector3.one * Random.Range(1.5f, 2.5f);
                    }
                }
            }

            // 7. Hilsha River Pollution Hazards & Spawning Ground Marker
            // Pollution Zone 1: Industrial Runoff Slick at Z = 15
            CreateRiverPollutionZone(pollutionParent, new Vector3(42f, 0.085f, 15f), 9f, "Industrial Runoff (শিল্প বর্জ্য)", 32.5f, "PSU toxic salinity");
            // Pollution Zone 2: Microplastics & Estuarine Chemical Slick at Z = 75
            CreateRiverPollutionZone(pollutionParent, new Vector3(42f, 0.085f, 75f), 10f, "Plastic & Heavy Metals (ভারী ধাতু দূষণ)", 26.8f, "PSU chemical stress");

            // Spawning Grounds Sanctuary at Z = 160 (Clean freshwater haven)
            CreateSpawningGroundSanctuary(pollutionParent, new Vector3(42f, 0.09f, 160f));

            // 8. Configure Dolphin & Hilsha positions & controllers
            var rootDolphin = GameObject.Find("/Dolphin");
            if (rootDolphin != null)
            {
                rootDolphin.transform.position = new Vector3(42f, 0.08f, 20f);
                rootDolphin.transform.rotation = Quaternion.identity;
                var dsc = rootDolphin.GetComponent<DolphinSwimController>();
                if (dsc != null)
                {
                    dsc.minX = 26f;
                    dsc.maxX = 58f;
                    dsc.minZ = -70f;
                    dsc.maxZ = 160f;
                    dsc.surfaceY = 0.08f;
                    dsc.diveY = -0.42f;
                    dsc.swimSpeed = 6f;
                }
            }

            var rootHilsha = GameObject.Find("/Hilsha");
            if (rootHilsha != null)
            {
                // Hilsha begins its epic journey in the deep waters of the Bay of Bengal Ocean!
                rootHilsha.transform.position = new Vector3(42f, -0.15f, -140f);
                rootHilsha.transform.rotation = Quaternion.identity;
                var hmc = rootHilsha.GetComponent<HilshaMigrationController>();
                if (hmc != null)
                {
                    hmc.corridorWidth = 16f;
                    hmc.swimSpeed = 7f;
                    hmc.lateralSpeed = 4f;
                }
            }

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[BuildSundarbanWorld] Complete! Ocean, River, Forest, Biomes, and Hilsha spawning route successfully built.");
        }

        private static void CreateRiverPollutionZone(Transform parent, Vector3 center, float radius, string name, float val, string unit)
        {
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pGo.name = $"PollutionHazard_{name}";
            pGo.transform.SetParent(parent);
            pGo.transform.position = center;
            pGo.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            var col = pGo.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var trigger = pGo.AddComponent<HazardZoneTrigger>();
            trigger.riskTier = "high";
            trigger.rawDataValue = val;
            trigger.rawDataUnit = unit;
            trigger.highDamagePerSec = 10f;

            // Turbid dark iridescent oil/chemical slick
            var rend = pGo.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.25f, 0.18f, 0.12f, 0.65f); // oily brackish runoff
                mat.SetFloat("_Smoothness", 0.95f);
                mat.SetFloat("_Metallic", 0.3f);
                rend.sharedMaterial = mat;
            }
        }

        private static void CreateSpawningGroundSanctuary(Transform parent, Vector3 center)
        {
            var spawnGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spawnGo.name = "Hilsha_Freshwater_Spawning_Sanctuary";
            spawnGo.transform.SetParent(parent);
            spawnGo.transform.position = center;
            spawnGo.transform.localScale = new Vector3(24f, 0.03f, 24f);

            var col = spawnGo.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Golden glowing clean freshwater marker
            var rend = spawnGo.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.15f, 0.85f, 0.65f, 0.35f); // clear crystalline freshwater
                mat.SetFloat("_Smoothness", 0.9f);
                rend.sharedMaterial = mat;
            }
        }
    }
}
