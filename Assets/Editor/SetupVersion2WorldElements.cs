using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Editor
{
    public static class SetupVersion2WorldElements
    {
        [MenuItem("Sundarban/Setup Version 2 Gameplay & Build APK")]
        public static void SetupAndBuildV2()
        {
            SetupV2WorldElements();
            BuildV2AndroidApk();
        }

        [MenuItem("Sundarban/Setup Version 2 World Elements (Traps, Seeds, Nets, Polluted Water)")]
        public static void SetupV2WorldElements()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log($"<color=cyan>[SetupV2] Configuring Version 2 gameplay in scene: {scene.name}</color>");

            SetupTigerTraps();
            SetupDeerSeeds();
            SetupDolphinRiverNetsAndPollution();

            // Set Version 2.0.0 in PlayerSettings
            PlayerSettings.bundleVersion = "2.0.0";
            PlayerSettings.Android.bundleVersionCode = 2;
            Debug.Log("<color=green>[SetupV2] PlayerSettings bundleVersion set to 2.0.0 (code 2)</color>");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[SetupV2] Version 2 elements placed and scene saved successfully!</color>");
        }

        private static void SetupTigerTraps()
        {
            const string rootName = "[V2_Tiger_Traps]";
            var existing = GameObject.Find(rootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var root = new GameObject(rootName);
            Vector3[] trapCoords = new Vector3[]
            {
                new Vector3(-38f, 0f, 225f),
                new Vector3(-28f, 0f, 205f),
                new Vector3(-42f, 0f, 185f),
                new Vector3(-20f, 0f, 165f),
                new Vector3(-35f, 0f, 145f),
                new Vector3(-15f, 0f, 125f),
                new Vector3(-30f, 0f, 105f),
                new Vector3(-10f, 0f, 85f),
                new Vector3(-25f, 0f, 65f),
                new Vector3(-18f, 0f, 45f),
                new Vector3(-32f, 0f, 25f),
                new Vector3(-12f, 0f, 5f),
                new Vector3(-26f, 0f, -15f),
                new Vector3(  5f, 0f, -35f),
                new Vector3(-20f, 0f, -55f),
                new Vector3( 15f, 0f, -70f),
                new Vector3( 28f, 0f, -85f)
            };

            for (int i = 0; i < trapCoords.Length; i++)
            {
                Vector3 pos = trapCoords[i];
                if (Physics.Raycast(pos + Vector3.up * 35f, Vector3.down, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    pos.y = hit.point.y + 0.02f;
                }
                else
                {
                    pos.y = 0.05f;
                }

                var trapGo = new GameObject($"HumanTrap_V2_{i + 1:D2}");
                trapGo.transform.SetParent(root.transform, true);
                trapGo.transform.position = pos;
                trapGo.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                var ht = trapGo.AddComponent<HumanTrap>();
                ht.trapDamage = 35f;
                ht.trapName = $"Poacher Wire Snare #{i + 1} (শিকারির তারের ফাঁদ)";
            }

            Debug.Log($"<color=green>[SetupV2] Placed {trapCoords.Length} HumanTraps for Tiger Level 1 mission (Target: 10 destroyed).</color>");
        }

        private static void SetupDeerSeeds()
        {
            const string rootName = "[V2_Deer_Seeds]";
            var existing = GameObject.Find(rootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var root = new GameObject(rootName);
            // Scarce, strategic mangrove seed spawns (keeps world uncluttered, realistic conservation challenges)
            Vector3[] seedClusters = new Vector3[]
            {
                new Vector3(-24f, 0f, 180f),
                new Vector3( 20f, 0f, 100f),
                new Vector3(-16f, 0f,  30f)
            };

            int seedIndex = 0;
            foreach (var cluster in seedClusters)
            {
                seedIndex++;
                Vector3 pos = cluster;

                if (Physics.Raycast(pos + Vector3.up * 35f, Vector3.down, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    pos.y = hit.point.y + 0.35f;
                }
                else
                {
                    pos.y = 0.35f;
                }

                var seedGo = new GameObject($"Seed_Pickup_V2_{seedIndex:D2}");
                seedGo.transform.SetParent(root.transform, true);
                seedGo.transform.position = pos;

                var pickup = seedGo.AddComponent<ToolPickupItem>();
                pickup.toolType = ToolType.Seed;
                pickup.amount = 2; // Each pouch provides 2 mangrove seeds
                pickup.Ensure3DVisualAsset();
                pickup.EnsureTriggerCollider();
            }

            Debug.Log($"<color=green>[SetupV2] Placed {seedIndex} scarce Mangrove Seed pickups for Deer Level 2 mission.</color>");
        }

        private static void SetupDolphinRiverNetsAndPollution()
        {
            const string rootName = "[V2_Dolphin_River_Challenges]";
            var existing = GameObject.Find(rootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var root = new GameObject(rootName);

            // 1. Fishing Nets across River (Z = 205, Z = 135, Z = 50, Z = -35)
            float[] netZs = new float[] { 205f, 135f, 50f, -35f };
            var netMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FishingNet_GreenCurtain.mat");
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (netMat == null)
            {
                netMat = new Material(litShader);
                netMat.color = new Color(0.12f, 0.65f, 0.48f, 0.75f);
            }

            for (int i = 0; i < netZs.Length; i++)
            {
                float z = netZs[i];
                var netGo = new GameObject($"FishingNet_Barrier_V2_{i + 1}");
                netGo.transform.SetParent(root.transform, true);
                netGo.transform.position = new Vector3(6f, -0.2f, z);

                // Add BoxCollider for net interaction
                var col = netGo.AddComponent<BoxCollider>();
                col.size = new Vector3(32f, 3.5f, 1.6f);
                col.center = new Vector3(0f, 0f, 0f);
                col.isTrigger = true;

                var netObs = netGo.AddComponent<IllegalNetObstacle>();
                netObs.hitDamage = 25f;
                netObs.knockbackDistance = 3.5f;

                // Visual: Top Float Beam with orange buoys
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beam.name = "Float_Beam";
                beam.transform.SetParent(netGo.transform, false);
                beam.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                beam.transform.localScale = new Vector3(0.18f, 15f, 0.18f);
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                var buoyMat = new Material(litShader);
                buoyMat.color = new Color(0.96f, 0.45f, 0.12f, 1.0f);
                beam.GetComponent<MeshRenderer>().sharedMaterial = buoyMat;

                // Visual: Net curtain quad hanging underwater
                var curtain = GameObject.CreatePrimitive(PrimitiveType.Quad);
                curtain.name = "Net_Mesh_Quad";
                curtain.transform.SetParent(netGo.transform, false);
                curtain.transform.localPosition = new Vector3(0f, -0.8f, 0f);
                curtain.transform.localScale = new Vector3(30f, 2.5f, 1f);
                Object.DestroyImmediate(curtain.GetComponent<Collider>());
                curtain.GetComponent<MeshRenderer>().sharedMaterial = netMat;
            }

            // 2. Polluted Water Zones (DPS = 5, Minimap Deep Purple)
            Vector3[] pollutedZones = new Vector3[]
            {
                new Vector3(6f, -0.4f, 160f),
                new Vector3(5f, -0.4f,  15f)
            };

            for (int p = 0; p < pollutedZones.Length; p++)
            {
                Vector3 pPos = pollutedZones[p];
                var pGo = new GameObject($"Polluted_Water_Hazard_V2_{p + 1}");
                pGo.transform.SetParent(root.transform, true);
                pGo.transform.position = pPos;

                var col = pGo.AddComponent<CapsuleCollider>();
                col.radius = 24f;
                col.height = 12f;
                col.center = new Vector3(0f, 2f, 0f);
                col.isTrigger = true;

                var wdz = pGo.AddComponent<WorldDangerZone>();
                wdz.zoneName = $"Polluted River Effluent & Toxic Runoff #{p + 1}";
                wdz.riskTier = "polluted"; // Triggers Deep Purple on Minimap!
                wdz.damagePerSecond = 5.0f; // Exactly 5 HP/sec
                wdz.interactionRadius = 24.0f;
                wdz.rawDataUnit = "PPM Toxic Chemical & Bilge Effluent";
                wdz.rawDataValue = 92.0f;

                // Murky purple water sheen visual on water surface
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Polluted_Water_Sheen";
                disc.transform.SetParent(pGo.transform, false);
                disc.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                disc.transform.localScale = new Vector3(44f, 0.05f, 44f);
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                var pMat = new Material(litShader);
                pMat.color = new Color(0.48f, 0.08f, 0.68f, 0.42f); // Deep Purple
                disc.GetComponent<MeshRenderer>().sharedMaterial = pMat;
            }

            Debug.Log($"<color=green>[SetupV2] Placed {netZs.Length} Fishing Nets and {pollutedZones.Length} Polluted Water Zones (Deep Purple, 5 HP/s) for Dolphin Level 3 mission.</color>");
        }

        public static void BuildV2AndroidApk()
        {
            string outputDir = "F:/SundarbanGuardian/Builds/Android";
            if (!System.IO.Directory.Exists(outputDir))
            {
                System.IO.Directory.CreateDirectory(outputDir);
            }

            string apkPath = System.IO.Path.Combine(outputDir, "SundarbanGuardian_v2.0.0.apk");
            Debug.Log($"[SetupV2] Starting Android IL2CPP Build -> {apkPath}");

            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[SetupV2] Android V2.0.0 Build Succeeded! File size: {report.summary.totalSize} bytes at {apkPath}</color>");
            }
            else
            {
                Debug.LogError($"[SetupV2] Android Build Failed: {report.summary.result}");
            }
        }
    }
}
