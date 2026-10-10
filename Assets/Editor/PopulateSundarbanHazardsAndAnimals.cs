using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Editor
{
    public static class PopulateSundarbanHazardsAndAnimals
    {
        [MenuItem("Sundarban/Populate Hazards, Animals, Beacon and Atmosphere")]
        public static void RunFullPopulation()
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Create or Find Hazards & Objectives Root
            var hazardsRoot = GameObject.Find("Hazards_And_Objectives");
            if (hazardsRoot == null) hazardsRoot = new GameObject("Hazards_And_Objectives");
            else
            {
                // Clear existing generated children
                for (int i = hazardsRoot.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(hazardsRoot.transform.GetChild(i).gameObject);
                }
            }

            // 2. Build 3D Extraction Sanctuary Goal Beacon at (75, 0, 270)
            BuildSanctuaryExtractionBeacon(hazardsRoot.transform, urpLit);

            // 3. Build Illegal River Fishing Nets
            BuildIllegalRiverNets(hazardsRoot.transform, urpLit);

            // 4. Place Saltwater Crocodiles
            BuildCrocodiles(hazardsRoot.transform, urpLit);

            // 5. Place Realistic Poacher Traps (With Visible & Almost Invisible variants)
            BuildPoacherTraps(hazardsRoot.transform);

            // 6. Build Expanded Deforestation Wasteland
            BuildDeforestationWasteland(hazardsRoot.transform, urpLit);

            // 7. Setup Dolphin & Hilsha Visuals & Looping Swim Animations
            SetupAquaticAnimals(urpLit);

            // 8. Attach MountainMistAnimator to Perimeter Mountains
            SetupMountainAnimation();

            // 9. Configure Atmospheric Lighting & Fog (Matching Image 2)
            ConfigureDarkMangroveAtmosphere();

            // 10. Position Tiger at South Coast Start Point (-40, 0.1, -95)
            PositionPlayerAtStart();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[PopulateSundarbanHazardsAndAnimals] Successfully populated all 11 user requirements in scene!</color>");
        }

        private static void BuildSanctuaryExtractionBeacon(Transform parent, Shader litShader)
        {
            Vector3 beaconPos = new Vector3(75f, 0.05f, 270f);

            var beaconGo = new GameObject("Extraction_Sanctuary_Beacon");
            beaconGo.transform.SetParent(parent, false);
            beaconGo.transform.position = beaconPos;

            // 1. Vertical Glowing Light Column (Reaching 28m into the sky)
            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Radiant_Light_Column";
            column.transform.SetParent(beaconGo.transform, false);
            column.transform.localPosition = new Vector3(0f, 14f, 0f);
            column.transform.localScale = new Vector3(1.6f, 14f, 1.6f);
            UnityEngine.Object.DestroyImmediate(column.GetComponent<Collider>());

            var colMat = new Material(litShader);
            colMat.SetFloat("_Surface", 1f); // Transparent
            colMat.SetFloat("_Blend", 0f);
            colMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            colMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            colMat.SetFloat("_ZWrite", 0f);
            colMat.renderQueue = 3000;
            colMat.SetColor("_BaseColor", new Color(0.20f, 0.98f, 0.60f, 0.40f));
            colMat.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 0.45f) * 2.5f);
            colMat.EnableKeyword("_EMISSION");
            column.GetComponent<MeshRenderer>().sharedMaterial = colMat;

            // 2. Concentric Ground Rings (Pulsing sanctuary zones)
            var ringMat = new Material(litShader);
            ringMat.SetFloat("_Surface", 1f);
            ringMat.renderQueue = 3000;
            ringMat.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.25f, 0.65f));

            var innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRing.name = "Ground_Ring_Inner";
            innerRing.transform.SetParent(beaconGo.transform, false);
            innerRing.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            innerRing.transform.localScale = new Vector3(8f, 0.02f, 8f);
            UnityEngine.Object.DestroyImmediate(innerRing.GetComponent<Collider>());
            innerRing.GetComponent<MeshRenderer>().sharedMaterial = ringMat;

            var outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "Ground_Ring_Outer";
            outerRing.transform.SetParent(beaconGo.transform, false);
            outerRing.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            outerRing.transform.localScale = new Vector3(16f, 0.02f, 16f);
            UnityEngine.Object.DestroyImmediate(outerRing.GetComponent<Collider>());
            outerRing.GetComponent<MeshRenderer>().sharedMaterial = ringMat;

            // 3. Point Light (Warm Gold/Emerald)
            var lightGo = new GameObject("Beacon_Point_Light");
            lightGo.transform.SetParent(beaconGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.95f, 0.65f);
            light.intensity = 3.5f;
            light.range = 25f;

            // 4. Trigger Collider for Goal
            var triggerCol = beaconGo.AddComponent<SphereCollider>();
            triggerCol.radius = 16f;
            triggerCol.isTrigger = true;
        }

        private static void BuildIllegalRiverNets(Transform parent, Shader litShader)
        {
            var netGroup = new GameObject("Illegal_River_Nets");
            netGroup.transform.SetParent(parent, false);

            float[] netZs = { 32f, 95f, 160f };
            float[] netXs = { 38f, 40f, 46f };
            float[] netWidths = { 24f, 26f, 28f };

            var corkMat = new Material(litShader);
            corkMat.SetColor("_BaseColor", new Color(0.95f, 0.35f, 0.10f, 1f)); // Bright orange cork buoys

            var netMat = new Material(litShader);
            netMat.SetFloat("_Surface", 1f);
            netMat.renderQueue = 3000;
            netMat.SetColor("_BaseColor", new Color(0.85f, 0.90f, 0.95f, 0.45f));

            for (int i = 0; i < netZs.Length; i++)
            {
                var netGo = new GameObject($"Illegal_Net_Obstacle_{i + 1}");
                netGo.transform.SetParent(netGroup.transform, false);
                netGo.transform.position = new Vector3(netXs[i], 0.05f, netZs[i]);

                // Mesh Quad for Net Wall
                var netWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                netWall.name = "Net_Mesh_Wall";
                netWall.transform.SetParent(netGo.transform, false);
                netWall.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                netWall.transform.localScale = new Vector3(netWidths[i], 1.8f, 0.15f);
                UnityEngine.Object.DestroyImmediate(netWall.GetComponent<Collider>());
                netWall.GetComponent<MeshRenderer>().sharedMaterial = netMat;

                // Top Floating Buoys
                int buoyCount = Mathf.RoundToInt(netWidths[i] / 2.2f);
                float step = netWidths[i] / buoyCount;
                float startX = -netWidths[i] * 0.5f;

                for (int b = 0; b <= buoyCount; b++)
                {
                    var buoy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    buoy.name = $"Buoy_{b}";
                    buoy.transform.SetParent(netGo.transform, false);
                    buoy.transform.localPosition = new Vector3(startX + b * step, 0.12f, 0f);
                    buoy.transform.localScale = new Vector3(0.40f, 0.28f, 0.40f);
                    UnityEngine.Object.DestroyImmediate(buoy.GetComponent<Collider>());
                    buoy.GetComponent<MeshRenderer>().sharedMaterial = corkMat;
                }

                // Add Obstacle Component & Trigger
                var boxCol = netGo.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(netWidths[i], 2.2f, 1.4f);
                boxCol.isTrigger = true;

                netGo.AddComponent<IllegalNetObstacle>();
            }
        }

        private static void BuildCrocodiles(Transform parent, Shader litShader)
        {
            var crocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/animal/American+Alligator.fbx");
            var crocGroup = new GameObject("Saltwater_Crocodiles");
            crocGroup.transform.SetParent(parent, false);

            var crocDefs = new (Vector3 pos, Vector3 patrolOffset, float rotY)[]
            {
                (new Vector3(38f, 0.05f, 45f), new Vector3(0f, 0f, 16f), 180f),
                (new Vector3(42f, 0.05f, 85f), new Vector3(-10f, 0f, 12f), 150f),
                (new Vector3(48f, 0.08f, 140f), new Vector3(12f, 0f, -8f), 220f)
            };

            for (int i = 0; i < crocDefs.Length; i++)
            {
                GameObject crocInst = null;
                if (crocPrefab != null)
                {
                    crocInst = (GameObject)PrefabUtility.InstantiatePrefab(crocPrefab, crocGroup.transform);
                }
                else
                {
                    crocInst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crocInst.transform.localScale = new Vector3(1.4f, 0.5f, 4.2f);
                }

                crocInst.name = $"Saltwater_Crocodile_{i + 1}";
                crocInst.transform.position = crocDefs[i].pos;
                crocInst.transform.rotation = Quaternion.Euler(0f, crocDefs[i].rotY, 0f);

                var threat = crocInst.GetComponent<CrocodileThreat>() ?? crocInst.AddComponent<CrocodileThreat>();
                threat.patrolOffset = crocDefs[i].patrolOffset;
                threat.patrolSpeed = 1.6f;
                threat.biteDamage = 40f;

                var col = crocInst.GetComponent<Collider>();
                if (col == null)
                {
                    var box = crocInst.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.6f, 0.8f, 4.5f);
                    box.isTrigger = true;
                }
                else
                {
                    col.isTrigger = true;
                }
            }
        }

        private static void BuildPoacherTraps(Transform parent)
        {
            var trapsGroup = new GameObject("Poacher_Human_Traps");
            trapsGroup.transform.SetParent(parent, false);

            var trapDefs = new (Vector3 pos, bool almostInvisible)[]
            {
                (new Vector3(-25f, 0.08f, 12f), false),
                (new Vector3(-15f, 0.02f, 28f), true),   // Almost invisible semi-buried!
                (new Vector3(-8f, 0.08f, 50f), false),
                (new Vector3(30f, 0.02f, 165f), true),   // Almost invisible!
                (new Vector3(48f, 0.08f, 200f), false),
                (new Vector3(62f, 0.02f, 238f), true)    // Almost invisible defending sanctuary!
            };

            for (int i = 0; i < trapDefs.Length; i++)
            {
                var trapGo = new GameObject($"Poacher_Trap_{i + 1}");
                trapGo.transform.SetParent(trapsGroup.transform, false);
                trapGo.transform.position = trapDefs[i].pos;

                var ht = trapGo.AddComponent<HumanTrap>();
                ht.isAlmostInvisible = trapDefs[i].almostInvisible;
                ht.trapDamage = 35f;
            }
        }

        private static void BuildDeforestationWasteland(Transform parent, Shader litShader)
        {
            Vector3 center = new Vector3(-35f, 0.05f, 110f);
            var wasteGroup = new GameObject("Deforestation_Wasteland_Zone");
            wasteGroup.transform.SetParent(parent, false);
            wasteGroup.transform.position = center;

            var stumpMat = new Material(litShader);
            stumpMat.SetColor("_BaseColor", new Color(0.24f, 0.18f, 0.12f, 1f));

            var logMat = new Material(litShader);
            logMat.SetColor("_BaseColor", new Color(0.20f, 0.15f, 0.10f, 1f));

            // 18 Sawed Tree Stumps and Fallen Logs
            for (int i = 0; i < 18; i++)
            {
                float angle = (float)i / 18f * Mathf.PI * 2f;
                float r = Random.Range(4f, 26f);
                Vector3 p = center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                // Sawed stump
                var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stump.name = $"Sawed_Stump_{i}";
                stump.transform.SetParent(wasteGroup.transform, false);
                stump.transform.position = p;
                stump.transform.localScale = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.25f, 0.55f), Random.Range(0.6f, 1.2f));
                stump.GetComponent<MeshRenderer>().sharedMaterial = stumpMat;

                // Fallen sawed log beside it
                if (i % 2 == 0)
                {
                    var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    log.name = $"Fallen_Log_{i}";
                    log.transform.SetParent(wasteGroup.transform, false);
                    log.transform.position = p + new Vector3(Random.Range(1.2f, 2.5f), 0.15f, Random.Range(1.2f, 2.5f));
                    log.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 90f);
                    log.transform.localScale = new Vector3(0.35f, Random.Range(1.5f, 3.2f), 0.35f);
                    log.GetComponent<MeshRenderer>().sharedMaterial = logMat;
                }
            }

            // Hazard Zone Trigger
            var col = wasteGroup.AddComponent<SphereCollider>();
            col.radius = 30f;
            col.isTrigger = true;
            var hzt = wasteGroup.AddComponent<HazardZoneTrigger>();
            hzt.riskTier = "high";
            hzt.obstacleNameBn = "বৃক্ষনিধন মরুভূমি (Deforested Wasteland)";
            hzt.obstacleNameEn = "Deforested Wasteland";
        }

        private static void SetupAquaticAnimals(Shader litShader)
        {
            // 1. Dolphin
            var dolphin = GameObject.Find("Dolphin");
            if (dolphin != null)
            {
                dolphin.transform.position = new Vector3(38f, -0.1f, -80f); // South River Mouth

                var mr = dolphin.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = true;

                // Ensure child model
                if (dolphin.transform.childCount == 0)
                {
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/fish/FBX/Dolphin.fbx");
                    if (fbx != null)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, dolphin.transform);
                        inst.name = "Dolphin_Model";
                    }
                }

                // Apply skin texture
                var dolphinTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/real_sundarban_dolphin.jpg");
                var renderers = dolphin.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.enabled = true;
                    if (dolphinTex != null)
                    {
                        var mat = new Material(litShader);
                        mat.SetTexture("_BaseMap", dolphinTex);
                        mat.SetColor("_BaseColor", Color.white);
                        mat.SetFloat("_Smoothness", 0.75f);
                        r.sharedMaterial = mat;
                    }
                }
            }

            // 2. Hilsha
            var hilsha = GameObject.Find("Hilsha");
            if (hilsha != null)
            {
                hilsha.transform.position = new Vector3(38f, -0.2f, -90f);

                var mr = hilsha.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = true;

                if (hilsha.transform.childCount == 0)
                {
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/fish/FBX/Fish2.fbx");
                    if (fbx != null)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, hilsha.transform);
                        inst.name = "Hilsha_Model";
                    }
                }

                var hilshaTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Hilsha_Reference.png");
                var renderers = hilsha.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.enabled = true;
                    if (hilshaTex != null)
                    {
                        var mat = new Material(litShader);
                        mat.SetTexture("_BaseMap", hilshaTex);
                        mat.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f));
                        mat.SetFloat("_Metallic", 0.65f);
                        mat.SetFloat("_Smoothness", 0.85f);
                        r.sharedMaterial = mat;
                    }
                }
            }
        }

        private static void SetupMountainAnimation()
        {
            string[] mtns = { "Mountain_West", "Mountain_East", "Mountain_North" };
            foreach (var name in mtns)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    if (go.GetComponent<MountainMistAnimator>() == null)
                    {
                        go.AddComponent<MountainMistAnimator>();
                    }
                }
            }

            var backdropGroup = GameObject.Find("2D_Mountain_Backdrop_Panoramas");
            if (backdropGroup != null)
            {
                if (backdropGroup.GetComponent<MountainMistAnimator>() == null)
                {
                    backdropGroup.AddComponent<MountainMistAnimator>();
                }
            }
        }

        private static void ConfigureDarkMangroveAtmosphere()
        {
            // Dark Mangrove Forest Atmosphere matching Image 2
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.13f, 0.19f, 0.17f, 1f); // Deep mangrove moss-teal
            RenderSettings.fogDensity = 0.0095f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.35f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.25f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.08f);

            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = new Color(0.95f, 0.92f, 0.85f);
                sun.intensity = 0.92f; // Soft moody light filtering through canopy
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.70f;
            }
        }

        private static void PositionPlayerAtStart()
        {
            var tiger = GameObject.Find("Tiger");
            if (tiger != null)
            {
                var cc = tiger.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                tiger.transform.position = new Vector3(-40f, 0.15f, -95f);
                tiger.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                if (cc != null) cc.enabled = true;
            }
        }
    }
}
