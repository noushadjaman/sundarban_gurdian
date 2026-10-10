using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SundarbanGuardian.Editor
{
    public static class DecorateWithMangroveEnvironment
    {
        private const string GlbAssetPath = "Assets/ExternalModels/mangrove_environment.glb";

        [MenuItem("Sundarban/Decorate Forest with Mangrove Environment GLB")]
        public static void DecorateForest()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log("[DecorateForest] Loading mangrove_environment.glb from " + GlbAssetPath);

            var glbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlbAssetPath);
            if (glbPrefab == null)
            {
                Debug.LogError("[DecorateForest] Failed to load GLB prefab at " + GlbAssetPath);
                return;
            }

            // 1. Prepare authentic vertex-colored Mud Material
            Material mudMat = PrepareMudMaterial();

            // 2. Prepare authentic flowing river Water Material
            Material waterMat = PrepareWaterMaterial();

            // 3. Deactivate old procedural/placeholder forest and land meshes that bury the asset
            string[] meshesToDeactivate = new string[] {
                "Dense_Mangrove_Forest",
                "Main_River_Shibsa",
                "Sundarban_Riverbanks_Land",
                "West_Riverbank_Land",
                "East_Riverbank_Land",
                "River_Water_Surface",
                "RiverBed",
                "River_Bed_Underlay",
                "Ground_Terrain",
                "Forest_Mud_Paths",
                "Mud_Trail_1",
                "Mud_Trail_2",
                "Lush_Greenery_and_Grass",
                "Inland_Trees",
                "Dense_Riverbank_Mangrove_Canopy"
            };

            foreach (var meshName in meshesToDeactivate)
            {
                var go = GameObject.Find(meshName);
                if (go != null)
                {
                    go.SetActive(false);
                    Debug.Log("[DecorateForest] Deactivated legacy mesh: " + meshName);
                }
            }

            // Also check all root GameObjects
            foreach (var root in activeScene.GetRootGameObjects())
            {
                string rName = root.name;
                if (rName.Contains("Dense_Mangrove_Forest") ||
                    rName.Contains("Main_River") ||
                    rName.Contains("Riverbanks_Land") ||
                    rName.Contains("River_Water") ||
                    rName.Contains("Lush_Greenery") ||
                    rName.Contains("Ground_Terrain") ||
                    rName.Contains("Mud_Trail") ||
                    rName.Contains("Inland_Trees"))
                {
                    root.SetActive(false);
                }
            }

            // 4. Remove previous instance of Mangrove_Environment_Decorations if present
            var existingEnv = GameObject.Find("Mangrove_Environment_Decorations");
            if (existingEnv != null)
            {
                Undo.DestroyObjectImmediate(existingEnv);
            }

            // 5. Instantiate the GLB environment model
            Debug.Log("[DecorateForest] Instantiating mangrove_environment.glb...");
            GameObject envInstance = Object.Instantiate(glbPrefab);
            envInstance.name = "Mangrove_Environment_Decorations";
            envInstance.transform.position = Vector3.zero;
            envInstance.transform.rotation = Quaternion.identity;
            envInstance.transform.localScale = Vector3.one;

            // 6. Disable catalogue parked outside map
            var catalogue = envInstance.transform.Find("Prefabs_Catalogue");
            if (catalogue != null) catalogue.gameObject.SetActive(false);

            // 7. Configure Mud Terrain with vertex colors + colliders
            var terrain = envInstance.transform.Find("Terrain");
            if (terrain != null)
            {
                terrain.gameObject.SetActive(true);
                int colCount = 0;
                for (int i = 0; i < terrain.childCount; i++)
                {
                    var chunk = terrain.GetChild(i).gameObject;
                    chunk.SetActive(true);
                    if (mudMat != null)
                    {
                        var mr = chunk.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = mudMat;
                    }

                    if (chunk.GetComponent<MeshCollider>() == null)
                    {
                        var mf = chunk.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            var mc = chunk.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            colCount++;
                        }
                    }
                }
                Debug.Log($"<color=orange>[DecorateForest] Configured {terrain.childCount} Mud Chunks with vertex-color PBR material and {colCount} colliders!</color>");
            }

            // 8. Configure River Water with transparent flowing shader
            var water = envInstance.transform.Find("Water");
            if (water != null)
            {
                water.gameObject.SetActive(true);
                for (int i = 0; i < water.childCount; i++)
                {
                    var wChunk = water.GetChild(i).gameObject;
                    wChunk.SetActive(true);
                    if (waterMat != null)
                    {
                        var mr = wChunk.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = waterMat;
                    }
                }
                Debug.Log($"<color=cyan>[DecorateForest] Activated {water.childCount} River Water chunks with custom river shader!</color>");
            }

            // 9. Ensure Trees & Undergrowth are active
            var trees = envInstance.transform.Find("Trees");
            if (trees != null) trees.gameObject.SetActive(true);

            var undergrowth = envInstance.transform.Find("Undergrowth");
            if (undergrowth != null) undergrowth.gameObject.SetActive(true);

            // 10. Align Sun Direction to author's natural mangrove sun angle: (-0.45, 0.79, 0.41)
            AlignSunDirection();

            // 11. Ensure player & camera placement directly in the authentic river channel at (-47, -18)
            SetupPlayerAndCamera();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[DecorateForest] SUCCESS! Sundarban decorated with authentic GLB Mud & River!</color>");
        }

        private static Material PrepareMudMaterial()
        {
            var mudShader = Shader.Find("Custom/MangroveMudSurface");
            if (mudShader == null) mudShader = Shader.Find("Universal Render Pipeline/Lit");

            var mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Mud_Surface.mat");
            if (mudMat == null)
            {
                mudMat = new Material(mudShader);
                AssetDatabase.CreateAsset(mudMat, "Assets/Materials/Mangrove_Mud_Surface.mat");
            }
            mudMat.shader = mudShader;

            var baseMudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/extracted_mud_base.png");
            if (baseMudTex == null) baseMudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MangroveMud_BaseMap.png");
            var norMudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/extracted_mud_normal.png");
            if (norMudTex == null) norMudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MangroveMud_NormalMap.png");

            if (baseMudTex != null) mudMat.SetTexture("_BaseMap", baseMudTex);
            if (norMudTex != null) mudMat.SetTexture("_BumpMap", norMudTex);
            mudMat.SetFloat("_BumpScale", 1.5f);
            mudMat.SetFloat("_Smoothness", 0.58f);
            mudMat.SetFloat("_WetMudDarken", 1.15f);
            mudMat.enableInstancing = true;
            EditorUtility.SetDirty(mudMat);
            return mudMat;
        }

        private static Material PrepareWaterMaterial()
        {
            var waterShader = Shader.Find("Custom/SundarbanRiverWater");
            if (waterShader == null) waterShader = Shader.Find("Universal Render Pipeline/Lit");

            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Asset_Water.mat");
            if (waterMat == null)
            {
                waterMat = new Material(waterShader);
                AssetDatabase.CreateAsset(waterMat, "Assets/Materials/Mangrove_Asset_Water.mat");
            }
            waterMat.shader = waterShader;

            var waterNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ocean_Water_Normal.tga");
            if (waterNor != null) waterMat.SetTexture("_BumpMap", waterNor);
            waterMat.SetFloat("_BumpScale", 0.90f);
            waterMat.SetFloat("_Smoothness", 0.94f);
            waterMat.SetFloat("_NormalTiling", 6.0f);
            waterMat.enableInstancing = true;
            EditorUtility.SetDirty(waterMat);
            return waterMat;
        }

        private static void AlignSunDirection()
        {
            var sun = RenderSettings.sun;
            if (sun == null)
            {
                var sunGo = GameObject.Find("Directional Light");
                if (sunGo != null) sun = sunGo.GetComponent<Light>();
            }
            if (sun != null)
            {
                Vector3 sunDir = new Vector3(0.45f, -0.79f, -0.41f).normalized;
                sun.transform.rotation = Quaternion.LookRotation(sunDir, Vector3.up);
                sun.color = new Color(1.0f, 0.96f, 0.88f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(sun);
            }
        }

        private static void SetupPlayerAndCamera()
        {
            var tiger = GameObject.Find("Tiger");
            if (tiger == null)
            {
                Debug.LogWarning("[DecorateForest] Tiger GameObject not found!");
                return;
            }

            tiger.SetActive(true);

            var cc = tiger.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Authentic river channel position where tiger wades belly-deep:
            Vector3 riverPos = new Vector3(-47.0f, -0.15f, -18.0f);
            Ray ray = new Ray(new Vector3(-47.0f, 25f, -18.0f), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                // Stand directly on riverbed floor so tiger wades through water surface at Y = 0
                riverPos = hit.point + Vector3.up * 0.05f;
                Debug.Log($"[DecorateForest] Positioned Tiger on riverbed at Y = {riverPos.y:F2}");
            }

            tiger.transform.position = riverPos;
            tiger.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
            EditorUtility.SetDirty(tiger);

            if (cc != null) cc.enabled = true;

            // Position Main Camera close behind over the right shoulder looking at tiger
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(-49.8f, 1.35f, -20.5f);
                cam.transform.rotation = Quaternion.LookRotation((new Vector3(-47f, 0.5f, -18f) - cam.transform.position).normalized);

                var cf = cam.GetComponent<SundarbanGuardian.Player.CameraFollow>();
                if (cf != null)
                {
                    cf.target = tiger.transform;
                    cf.currentYaw = 65f;
                    cf.currentPitch = 8.5f;
                    cf.defaultDistance = 3.5f;
                    cf.pivotOffset = new Vector3(0f, 1.40f, 0f);
                    cf.shoulderOffset = new Vector3(0.50f, 0.25f, 0f);
                    EditorUtility.SetDirty(cf);
                }
                EditorUtility.SetDirty(cam);
            }

            // Configure GameManager references
            var gm = Object.FindAnyObjectByType<SundarbanGuardian.Core.GameManager>();
            if (gm != null)
            {
                gm.currentSpecies = "tiger";
                SundarbanGuardian.Core.GameManager.selectedSpecies = "tiger";
                gm.playerObject = tiger;
                var ll = Object.FindAnyObjectByType<SundarbanGuardian.Core.LevelLoader>();
                if (ll != null) gm.levelLoader = ll;
                EditorUtility.SetDirty(gm);
            }
        }
    }
}
