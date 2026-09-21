using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SundarbanGuardian.Core;
using SundarbanGuardian.Player;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    // [InitializeOnLoad] DISABLED
    public static class UpgradeSundarbanAesthetics
    {
        static UpgradeSundarbanAesthetics()
        {
            EditorApplication.delayCall += () =>
            {
                ApplyAesthetics();
            };
        }

        [MenuItem("Sundarban/Apply Realistic Wetland Aesthetics")]
        public static void ApplyAesthetics()
        {
            Debug.Log("[UpgradeSundarbanAesthetics] Applying authentic Sundarban mangrove scenery with 5 decorator trees, freshwater pond, and PBR pneumatophore mud surface...");

            // =========================================================================
            // 1. DIRECTIONAL LIGHTING & WARM HUMID ATMOSPHERE (BENGAL TROPICAL COAST)
            // =========================================================================
            var sun = RenderSettings.sun;
            if (sun == null)
            {
                var lightGo = GameObject.Find("Directional Light");
                if (lightGo != null) sun = lightGo.GetComponent<Light>();
            }

            if (sun != null)
            {
                // Warm tropical sun filtering through canopy (golden warmth #fff2df)
                sun.color = new Color(1.0f, 0.95f, 0.86f);
                sun.intensity = 1.28f;
                sun.transform.rotation = Quaternion.Euler(45f, -32f, 0f);
                sun.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(sun);
            }

            // Humid coastal sea mist / river haze
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 450f;
            RenderSettings.fogColor = new Color(0.72f, 0.80f, 0.84f);

            // Camera settings
            var cam = Camera.main;
            if (cam != null)
            {
                cam.farClipPlane = 1200f;
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData != null)
                {
                    camData.requiresDepthTexture = true;
                    camData.requiresColorTexture = true;
                }
            }

            // Global Volume Post-Processing
            var volumeGo = GameObject.Find("Global Volume");
            if (volumeGo != null)
            {
                var vol = volumeGo.GetComponent<Volume>();
                if (vol != null && vol.profile != null)
                {
                    if (vol.profile.TryGet<ColorAdjustments>(out var colorAdj))
                    {
                        colorAdj.colorFilter.value = new Color(1.0f, 0.98f, 0.94f);
                        colorAdj.saturation.value = 18f;
                        colorAdj.contrast.value = 16f;
                        colorAdj.postExposure.value = 0.20f;
                    }
                    if (vol.profile.TryGet<WhiteBalance>(out var wb))
                    {
                        wb.temperature.value = 8f;
                    }
                }
            }

            // =========================================================================
            // 2. GROUND MATERIALS: AUTHENTIC PBR MUD & PNEUMATOPHORES
            // =========================================================================
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Ground.mat");
            var riverbankMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Riverbank_Ground.mat");
            var eastSwampMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_EastSwamp.mat");
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Bark.mat");
            var leavesLushMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Leaves_Lush.mat");

            var gt = GameObject.Find("Ground_Terrain");
            if (gt != null && groundMat != null)
            {
                gt.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }

            var spit = GameObject.Find("Estuary_Mud_Spit");
            if (spit != null && groundMat != null)
            {
                spit.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }

            var eastLand = GameObject.Find("Eastern_Riverbank_Land");
            if (eastLand != null && eastSwampMat != null)
            {
                eastLand.GetComponent<MeshRenderer>().sharedMaterial = eastSwampMat;
            }

            // =========================================================================
            // 3. RIVER WATER: REFLECTIVE SEDIMENT-RICH TROPICAL TIDAL WATERWAY
            // =========================================================================
            var waterShader = Shader.Find("Custom/SundarbanRiverWater");
            var riverMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            var waterNormTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ocean_Water_Normal.tga");

            if (riverMat != null && waterShader != null)
            {
                riverMat.shader = waterShader;
                riverMat.SetColor("_ShallowColor", new Color(0.42f, 0.46f, 0.28f, 0.78f));
                riverMat.SetColor("_DeepColor", new Color(0.18f, 0.24f, 0.14f, 0.95f));
                riverMat.SetColor("_FoamColor", new Color(0.55f, 0.58f, 0.44f, 0.85f));
                riverMat.SetFloat("_FoamDistance", 0.60f);
                riverMat.SetFloat("_Smoothness", 0.90f);
                riverMat.SetFloat("_NormalTiling", 6.0f);
                riverMat.SetFloat("_BumpScale", 0.70f);
                riverMat.SetVector("_WaveSpeed", new Vector4(0.02f, 0.015f, -0.015f, 0.02f));
                if (waterNormTex != null) riverMat.SetTexture("_BumpMap", waterNormTex);
                EditorUtility.SetDirty(riverMat);
            }

            var riverBed = GameObject.Find("River_Bed_Underlay");
            if (riverBed != null && riverMat != null)
            {
                riverBed.GetComponent<MeshRenderer>().sharedMaterial = riverMat;
            }

            // =========================================================================
            // 4. CLEANUP OLD UNTEXTURED / BARE MODELS
            // =========================================================================
            // Search and remove any instances of the untextured RedMangrove_Tree (53MB bare white trunk)
            // and Beach Debris 4 (bare white branch) across the entire scene
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
            int removedBare = 0;
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                string goName = r.gameObject.name;
                if (goName.Contains("RedMangrove") || goName.Contains("Mangrove_StandbyTree") ||
                    goName.Contains("Beach Debris") || goName.Contains("230501_Beach Debris"))
                {
                    Object.DestroyImmediate(r.gameObject);
                    removedBare++;
                }
            }
            if (removedBare > 0)
            {
                Debug.Log($"[UpgradeSundarbanAesthetics] Cleaned up {removedBare} untextured/bare white objects.");
            }

            // =========================================================================
            // 5. LOAD THE 5 LIGHTWEIGHT DECORATOR TREES & 1 POND
            // =========================================================================
            var commonTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Tree by Quaternius - QVOop92WmG/CommonTree_3.fbx");
            var twistedTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Twisted Tree by Quaternius - 7PDBpElkQr/TwistedTree_4.fbx");
            var deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Dead Tree by Quaternius - Mcd2zYqyww/DeadTree_2.fbx");
            var ladyPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Lady palm tree by Poly by Google - e0AbYA90ux5/LadyPalmTree.obj");
            var thatchPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Thatch palm tree by Poly by Google - 74GpKCkUqcn/KeyThatchPalmTree.obj");
            var pondPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Pond.obj");

            var pondMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Pond.mat");
            var pondWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_PondWater.mat");
            if (pondWaterMat == null) pondWaterMat = riverMat;
            var ladyPalmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_LadyPalm.mat");
            var thatchPalmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_ThatchPalm.mat");

            if (commonTreePrefab == null || twistedTreePrefab == null)
            {
                Debug.LogError("[UpgradeSundarbanAesthetics] Could not find Quaternius decorator tree prefabs!");
                return;
            }

            // =========================================================================
            // 6. REBUILD SCENERY: CANOPY, WATERWAY, PALMS & POND
            // =========================================================================
            var forestParent = GameObject.Find("Mangrove_Forest");
            if (forestParent == null) forestParent = new GameObject("Mangrove_Forest");

            // Clean previous decorator scenery groups
            string[] groupsToRebuild = {
                "Riverbank_Mangroves", "Decorator_Canopy_Arches",
                "Freshwater_Pond_Sanctuary", "Pneumatophore_Root_Beds",
                "Mudflat_Palms_And_Fringes", "Cyclone_Deadwood_Snags"
            };
            foreach (var g in groupsToRebuild)
            {
                var tr = forestParent.transform.Find(g);
                if (tr != null) Object.DestroyImmediate(tr.gameObject);
            }

            var archesGroup = new GameObject("Decorator_Canopy_Arches").transform;
            archesGroup.SetParent(forestParent.transform);

            var riverbankGroup = new GameObject("Riverbank_Mangroves").transform;
            riverbankGroup.SetParent(forestParent.transform);

            var palmsGroup = new GameObject("Mudflat_Palms_And_Fringes").transform;
            palmsGroup.SetParent(forestParent.transform);

            var pondGroup = new GameObject("Freshwater_Pond_Sanctuary").transform;
            pondGroup.SetParent(forestParent.transform);

            var rootsGroup = new GameObject("Pneumatophore_Root_Beds").transform;
            rootsGroup.SetParent(forestParent.transform);

            var snagsGroup = new GameObject("Cyclone_Deadwood_Snags").transform;
            snagsGroup.SetParent(forestParent.transform);

            Random.InitState(1971);

            // â”€â”€ A) ARCHING MANGROVE CANOPY OVER RIVER (IMAGE 2 STYLE) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // West riverbank (X: 18 to 23) and East riverbank (X: 47 to 52)
            // Trees lean 15Â° to 25Â° towards the water (+X for west, -X for east)
            for (float z = -80f; z <= 210f; z += 4.8f)
            {
                // West Bank: Leaning arching tree over the water
                float xWest = Random.Range(18.5f, 22.5f);
                Vector3 posWest = new Vector3(xWest, 0f, z + Random.Range(-1.5f, 1.5f));
                float leanWest = Random.Range(14f, 26f); // lean towards river (+X)
                float rotYWest = Random.Range(-25f, 25f);
                var rotWest = Quaternion.Euler(Random.Range(-5f, 5f), rotYWest, -leanWest);

                var archWest = (GameObject)Object.Instantiate(twistedTreePrefab, posWest, rotWest, archesGroup);
                archWest.name = $"Mangrove_Arch_West_{z:F0}";
                float sWest = Random.Range(1.05f, 1.45f);
                archWest.transform.localScale = Vector3.one * sWest;
                EnsureTreeMaterials(archWest, barkMat, leavesLushMat);

                // West Bank: Dense background canopy tree (CommonTree_3)
                if (Random.value > 0.25f)
                {
                    Vector3 bgPos = posWest + new Vector3(Random.Range(-4.5f, -2.5f), 0f, Random.Range(-2f, 2f));
                    var bgTree = (GameObject)Object.Instantiate(commonTreePrefab, bgPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbankGroup);
                    bgTree.name = $"Mangrove_Canopy_West_{z:F0}";
                    bgTree.transform.localScale = Vector3.one * Random.Range(1.1f, 1.5f);
                    EnsureTreeMaterials(bgTree, barkMat, leavesLushMat);
                }

                // East Bank: Leaning arching tree over the water
                float xEast = Random.Range(48.5f, 52.5f);
                Vector3 posEast = new Vector3(xEast, 0f, z + Random.Range(-1.5f, 1.5f));
                float leanEast = Random.Range(14f, 26f); // lean towards river (-X)
                float rotYEast = Random.Range(155f, 205f);
                var rotEast = Quaternion.Euler(Random.Range(-5f, 5f), rotYEast, leanEast);

                var archEast = (GameObject)Object.Instantiate(twistedTreePrefab, posEast, rotEast, archesGroup);
                archEast.name = $"Mangrove_Arch_East_{z:F0}";
                float sEast = Random.Range(1.05f, 1.45f);
                archEast.transform.localScale = Vector3.one * sEast;
                EnsureTreeMaterials(archEast, barkMat, leavesLushMat);

                // East Bank: Dense canopy tree
                if (Random.value > 0.25f)
                {
                    Vector3 bgPosEast = posEast + new Vector3(Random.Range(2.5f, 4.5f), 0f, Random.Range(-2f, 2f));
                    var bgTreeEast = (GameObject)Object.Instantiate(commonTreePrefab, bgPosEast, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbankGroup);
                    bgTreeEast.name = $"Mangrove_Canopy_East_{z:F0}";
                    bgTreeEast.transform.localScale = Vector3.one * Random.Range(1.1f, 1.5f);
                    EnsureTreeMaterials(bgTreeEast, barkMat, leavesLushMat);
                }

                // Riverbank Palms (Golpata / Thatch Palm & Lady Palm)
                if (Random.value > 0.40f && thatchPalmPrefab != null)
                {
                    Vector3 palmPos = new Vector3(Random.Range(16.5f, 19.5f), 0f, z + Random.Range(-1.5f, 1.5f));
                    var palm = (GameObject)Object.Instantiate(thatchPalmPrefab, palmPos, Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(4f, 12f)), palmsGroup);
                    palm.name = $"Golpata_Palm_West_{z:F0}";
                    palm.transform.localScale = Vector3.one * Random.Range(0.65f, 0.95f);
                    ApplyMaterialToHierarchy(palm, thatchPalmMat);
                }

                if (Random.value > 0.55f && ladyPalmPrefab != null)
                {
                    Vector3 palmPos = new Vector3(Random.Range(50f, 54f), 0f, z + Random.Range(-1.5f, 1.5f));
                    var palm = (GameObject)Object.Instantiate(ladyPalmPrefab, palmPos, Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(4f, 12f)), palmsGroup);
                    palm.name = $"Lady_Palm_East_{z:F0}";
                    palm.transform.localScale = Vector3.one * Random.Range(0.50f, 0.75f);
                    ApplyMaterialToHierarchy(palm, ladyPalmMat);
                }
            }

            // â”€â”€ B) 3D PNEUMATOPHORE (BREATHING ROOT) SPIKE CLUSTERS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // Along the mudbanks (X: 20 to 24), instantiate clusters of 3D spike roots
            // This gives authentic physical 3D parallax depth in close-up exactly like Image 2!
            Mesh pneumatophoreMesh = BuildPneumatophoreClusterMesh();
            for (float z = -75f; z <= 200f; z += 5.5f)
            {
                // West Mudbank Root Bed
                Vector3 bedPos = new Vector3(Random.Range(20.2f, 23.5f), 0.01f, z + Random.Range(-1.5f, 1.5f));
                var bedGO = new GameObject($"Pneumatophores_West_{z:F0}");
                bedGO.transform.SetParent(rootsGroup);
                bedGO.transform.position = bedPos;
                bedGO.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var mf = bedGO.AddComponent<MeshFilter>();
                mf.sharedMesh = pneumatophoreMesh;
                var mr = bedGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = barkMat;

                // East Mudbank Root Bed
                if (Random.value > 0.35f)
                {
                    Vector3 bedPosE = new Vector3(Random.Range(47.5f, 49.5f), 0.01f, z + Random.Range(-1.5f, 1.5f));
                    var bedGOE = new GameObject($"Pneumatophores_East_{z:F0}");
                    bedGOE.transform.SetParent(rootsGroup);
                    bedGOE.transform.position = bedPosE;
                    bedGOE.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    var mfE = bedGOE.AddComponent<MeshFilter>();
                    mfE.sharedMesh = pneumatophoreMesh;
                    var mrE = bedGOE.AddComponent<MeshRenderer>();
                    mrE.sharedMaterial = barkMat;
                }
            }

            // â”€â”€ C) FRESHWATER POND HAVEN (ROUNDGLASS SUSTAIN REFERENCE) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // A tranquil watering hole where wildlife gathers to drink
            if (pondPrefab != null)
            {
                Vector3 pondCenter = new Vector3(-86f, 0.05f, 42f); // Mandarbaria Haven
                var pondInstance = (GameObject)Object.Instantiate(pondPrefab, pondCenter, Quaternion.Euler(0f, 45f, 0f), pondGroup);
                pondInstance.name = "Freshwater_Pond_Mandarbaria";
                pondInstance.transform.localScale = new Vector3(6.5f, 3.2f, 6.5f);
                ApplyMaterialToHierarchy(pondInstance, pondMat);
                AttachWaterSurfaceToPond(pondInstance, pondWaterMat);

                // Add leaning mangrove tree over the pond
                var pTree = (GameObject)Object.Instantiate(twistedTreePrefab, pondCenter + new Vector3(4.5f, 0f, 2.5f), Quaternion.Euler(5f, -120f, 18f), pondGroup);
                pTree.name = "Pond_Overhanging_Mangrove";
                pTree.transform.localScale = Vector3.one * 1.35f;
                EnsureTreeMaterials(pTree, barkMat, leavesLushMat);

                // Add surrounding Golpata & Lady palms
                if (thatchPalmPrefab != null)
                {
                    var p1 = (GameObject)Object.Instantiate(thatchPalmPrefab, pondCenter + new Vector3(-4.2f, 0f, 3.8f), Quaternion.Euler(0f, 75f, 8f), pondGroup);
                    p1.transform.localScale = Vector3.one * 0.85f;
                    ApplyMaterialToHierarchy(p1, thatchPalmMat);

                    var p2 = (GameObject)Object.Instantiate(thatchPalmPrefab, pondCenter + new Vector3(-3.5f, 0f, -4.0f), Quaternion.Euler(0f, 195f, 10f), pondGroup);
                    p2.transform.localScale = Vector3.one * 0.75f;
                    ApplyMaterialToHierarchy(p2, thatchPalmMat);
                }

                if (ladyPalmPrefab != null)
                {
                    var lp1 = (GameObject)Object.Instantiate(ladyPalmPrefab, pondCenter + new Vector3(3.8f, 0f, -3.5f), Quaternion.Euler(0f, 130f, 6f), pondGroup);
                    lp1.transform.localScale = Vector3.one * 0.65f;
                    ApplyMaterialToHierarchy(lp1, ladyPalmMat);
                }

                // Add second smaller pond near Kotka Sanctuary
                Vector3 kotkaPondPos = new Vector3(-42f, 0.05f, 25f);
                var kotkaPond = (GameObject)Object.Instantiate(pondPrefab, kotkaPondPos, Quaternion.Euler(0f, -60f, 0f), pondGroup);
                kotkaPond.name = "Freshwater_Pond_Kotka";
                kotkaPond.transform.localScale = new Vector3(5.5f, 2.8f, 5.5f);
                ApplyMaterialToHierarchy(kotkaPond, pondMat);
                AttachWaterSurfaceToPond(kotkaPond, pondWaterMat);
            }

            // Also attach water surface to ANY other existing pond across the scene
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name.ToLower().Contains("pond") && !go.name.Contains("Water") && !go.name.Contains("Sanctuary"))
                {
                    AttachWaterSurfaceToPond(go, pondWaterMat);
                }
            }

            // â”€â”€ D) CYCLONE WEATHERED DEADWOOD SNAGS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // Scattered naturally in high salinity / cyclone impacted zones
            if (deadTreePrefab != null)
            {
                Vector3[] snagLocations = new Vector3[] {
                    new Vector3(-58f, 0f, 66f),   // Deforestation bottleneck edge
                    new Vector3(-62f, 0f, 72f),
                    new Vector3(-18f, 0f, 98f),   // Near mudflat edge
                    new Vector3(-24f, 0f, 140f),
                    new Vector3(-75f, 0f, 110f),
                    new Vector3(5f, 0f, 30f),     // River spit
                    new Vector3(8f, 0f, 85f),
                    new Vector3(-105f, 0f, 15f)   // Start glade perimeter
                };

                for (int d = 0; d < snagLocations.Length; d++)
                {
                    Vector3 dPos = snagLocations[d] + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                    var snag = (GameObject)Object.Instantiate(deadTreePrefab, dPos, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)), snagsGroup);
                    snag.name = $"Cyclone_Deadwood_Snag_{d + 1}";
                    snag.transform.localScale = Vector3.one * Random.Range(0.85f, 1.25f);
                    EnsureTreeMaterials(snag, barkMat, leavesLushMat);
                }
            }

            // â”€â”€ E) INLAND FOREST VEGETATION WITH OPEN NAVIGABLE PATH â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // Scatter CommonTree_3 and palms in the forest, keeping paths clear
            for (float ix = -105f; ix <= 12f; ix += 11f)
            {
                for (float iz = -65f; iz <= 165f; iz += 11f)
                {
                    Vector3 pos = new Vector3(ix + Random.Range(-3.5f, 3.5f), 0f, iz + Random.Range(-3.5f, 3.5f));

                    // Keep player spawn clear
                    if (pos.magnitude < 10f) continue;
                    // Keep StartZone and GoalZone open
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(-120f, 30f)) < 16f) continue;
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(-40f, 155f)) < 18f) continue;
                    // Keep ponds clear
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(-86f, 42f)) < 12f) continue;
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(-42f, 25f)) < 10f) continue;

                    // Plant lush mangrove tree
                    if (Random.value > 0.45f && commonTreePrefab != null)
                    {
                        var tree = (GameObject)Object.Instantiate(commonTreePrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbankGroup);
                        tree.name = $"Inland_Mangrove_{ix:F0}_{iz:F0}";
                        tree.transform.localScale = Vector3.one * Random.Range(0.95f, 1.35f);
                        EnsureTreeMaterials(tree, barkMat, leavesLushMat);
                    }

                    // Occasional palm grove cluster
                    if (Random.value > 0.70f)
                    {
                        var pPrefab = (Random.value > 0.5f) ? thatchPalmPrefab : ladyPalmPrefab;
                        var pMat = (pPrefab == thatchPalmPrefab) ? thatchPalmMat : ladyPalmMat;
                        if (pPrefab != null)
                        {
                            var palm = (GameObject)Object.Instantiate(pPrefab, pos + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)), Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(3f, 10f)), palmsGroup);
                            palm.name = $"Inland_Palm_{ix:F0}_{iz:F0}";
                            palm.transform.localScale = Vector3.one * Random.Range(0.6f, 0.9f);
                            ApplyMaterialToHierarchy(palm, pMat);
                        }
                    }
                }
            }

            // Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[UpgradeSundarbanAesthetics] Authentic Sundarban mangrove scenery successfully created! Arched river canopy, 3D pneumatophore root beds, freshwater ponds, and lush foliage are now live.");
        }

        private static void EnsureTreeMaterials(GameObject root, Material barkMat, Material leavesMat)
        {
            if (root == null) return;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var shared = mr.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    string mName = (shared[i] != null) ? shared[i].name.ToLower() : "";
                    if (mName.Contains("leaf") || mName.Contains("leaves"))
                    {
                        shared[i] = leavesMat;
                    }
                    else if (mName.Contains("bark") || mName.Contains("wood") || mName.Contains("trunk") || mName.Contains("dead"))
                    {
                        shared[i] = barkMat;
                    }
                    else
                    {
                        // Default to bark if single slot or trunk
                        if (i == 0 && shared.Length > 1) shared[i] = barkMat;
                        else if (shared.Length == 1) shared[i] = barkMat;
                        else shared[i] = leavesMat;
                    }
                }
                mr.sharedMaterials = shared;
            }
        }

        private static void ApplyMaterialToHierarchy(GameObject root, Material mat)
        {
            if (root == null || mat == null) return;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var shared = mr.sharedMaterials;
                for (int i = 0; i < shared.Length; i++) shared[i] = mat;
                mr.sharedMaterials = shared;
            }
        }

        /// <summary>
        /// Generates a lightweight cluster mesh of 20 vertical mangrove breathing roots (pneumatophores).
        /// Total vertices ~160, single draw call per cluster, casts real 3D shadows and gives authentic close-up depth.
        /// </summary>
        private static Mesh BuildPneumatophoreClusterMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Pneumatophore_Cluster_Mesh";

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            Random.InitState(42);
            int rootCount = 18;
            float radius = 1.8f;

            for (int r = 0; r < rootCount; r++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Mathf.Sqrt(Random.Range(0.04f, 1f)) * radius;
                float cx = Mathf.Cos(angle) * dist;
                float cz = Mathf.Sin(angle) * dist;

                // Height of pneumatophore: 0.14m to 0.32m (pencil spike)
                float h = Random.Range(0.14f, 0.32f);
                float w = Random.Range(0.015f, 0.028f); // base radius

                // Slight natural tilt (1 to 5 degrees)
                float tiltX = Random.Range(-0.02f, 0.02f);
                float tiltZ = Random.Range(-0.02f, 0.02f);

                int baseIndex = verts.Count;

                // 4-sided pyramid / spike
                Vector3 b0 = new Vector3(cx - w, 0f, cz - w);
                Vector3 b1 = new Vector3(cx + w, 0f, cz - w);
                Vector3 b2 = new Vector3(cx + w, 0f, cz + w);
                Vector3 b3 = new Vector3(cx - w, 0f, cz + w);
                Vector3 tip = new Vector3(cx + tiltX, h, cz + tiltZ);

                verts.Add(b0); verts.Add(b1); verts.Add(b2); verts.Add(b3); verts.Add(tip);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0.5f, 1f));

                // 4 triangle sides
                // side 0: b0, tip, b1
                tris.Add(baseIndex + 0); tris.Add(baseIndex + 4); tris.Add(baseIndex + 1);
                // side 1: b1, tip, b2
                tris.Add(baseIndex + 1); tris.Add(baseIndex + 4); tris.Add(baseIndex + 2);
                // side 2: b2, tip, b3
                tris.Add(baseIndex + 2); tris.Add(baseIndex + 4); tris.Add(baseIndex + 3);
                // side 3: b3, tip, b0
                tris.Add(baseIndex + 3); tris.Add(baseIndex + 4); tris.Add(baseIndex + 0);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static void AttachWaterSurfaceToPond(GameObject pondInstance, Material waterMat)
        {
            if (pondInstance == null) return;
            Transform existing = pondInstance.transform.Find("Pond_Water_Surface");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // Create circular / cylindrical water sheet fitted inside pond basin
            GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterGO.name = "Pond_Water_Surface";
            waterGO.transform.SetParent(pondInstance.transform, false);

            waterGO.transform.localPosition = new Vector3(0.06f, 0.22f, 0.04f);
            waterGO.transform.localRotation = Quaternion.identity;
            waterGO.transform.localScale = new Vector3(1.72f, 0.012f, 1.48f);

            var col = waterGO.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var mr = waterGO.GetComponent<MeshRenderer>();
            if (mr != null && waterMat != null)
            {
                mr.sharedMaterial = waterMat;
            }

            var injector = pondInstance.GetComponent<PondWaterInjector>();
            if (injector == null) injector = pondInstance.AddComponent<PondWaterInjector>();
            injector.waterMaterial = waterMat;
            injector.waterHeight = 0.22f;
            injector.waterScaleXZ = new Vector2(1.72f, 1.48f);
            injector.waterOffsetXZ = new Vector2(0.06f, 0.04f);
        }
    }
}
