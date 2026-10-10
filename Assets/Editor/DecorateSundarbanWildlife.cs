using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    public static class DecorateSundarbanWildlife
    {
        private const string RootName = "[Sundarban_Wildlife_Decorations]";

        [MenuItem("Sundarban/Decorate Forest With Extra Wildlife")]
        public static void DecorateWildlife()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/SampleScene.unity")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // 1. Clear previous decorations root if re-running
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            var rootGo = new GameObject(RootName);
            rootGo.transform.position = Vector3.zero;

            // 2. Prepare Materials
            var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Cobra Material
            var cobraTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalModels/Animals/decorate/Cobra by Poly by Google - 40_5Xq467-U/cobraTxt.png");
            var cobraMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Cobra.mat");
            if (cobraMat == null)
            {
                cobraMat = new Material(urpLit);
                AssetDatabase.CreateAsset(cobraMat, "Assets/Materials/Wildlife_Cobra.mat");
            }
            cobraMat.shader = urpLit;
            if (cobraTex != null) cobraMat.SetTexture("_BaseMap", cobraTex);
            cobraMat.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(cobraMat);

            // Crocodile Material
            var crocTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalModels/Animals/decorate/Crocodile by Poly by Google - 2an6E2WjW3z/Tex_Crocodile.png");
            var crocMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Crocodile.mat");
            if (crocMat == null)
            {
                crocMat = new Material(urpLit);
                AssetDatabase.CreateAsset(crocMat, "Assets/Materials/Wildlife_Crocodile.mat");
            }
            crocMat.shader = urpLit;
            if (crocTex != null) crocMat.SetTexture("_BaseMap", crocTex);
            crocMat.SetFloat("_Smoothness", 0.60f);
            EditorUtility.SetDirty(crocMat);

            // Bird Materials (Kingfisher / Heron plumage)
            var birdBodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Bird_Body.mat");
            if (birdBodyMat == null)
            {
                birdBodyMat = new Material(urpLit);
                AssetDatabase.CreateAsset(birdBodyMat, "Assets/Materials/Wildlife_Bird_Body.mat");
            }
            birdBodyMat.shader = urpLit;
            birdBodyMat.SetColor("_BaseColor", new Color(0.96f, 0.74f, 0.12f)); // Warm golden kingfisher yellow
            birdBodyMat.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(birdBodyMat);

            var birdWingsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Bird_Wings.mat");
            if (birdWingsMat == null)
            {
                birdWingsMat = new Material(urpLit);
                AssetDatabase.CreateAsset(birdWingsMat, "Assets/Materials/Wildlife_Bird_Wings.mat");
            }
            birdWingsMat.shader = urpLit;
            birdWingsMat.SetColor("_BaseColor", new Color(0.15f, 0.42f, 0.65f)); // Deep cerulean azure blue
            birdWingsMat.SetFloat("_Smoothness", 0.40f);
            EditorUtility.SetDirty(birdWingsMat);

            // Wild Dog Material (Dhole rufous/russet coat)
            var dogMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_DholeDog.mat");
            if (dogMat == null)
            {
                dogMat = new Material(urpLit);
                AssetDatabase.CreateAsset(dogMat, "Assets/Materials/Wildlife_DholeDog.mat");
            }
            dogMat.shader = urpLit;
            dogMat.SetColor("_BaseColor", new Color(0.68f, 0.35f, 0.14f));
            dogMat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(dogMat);

            // Deer Material (Chitra Spotted Deer)
            var deerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Authentic_Deer.mat");
            if (deerMat == null)
            {
                deerMat = new Material(urpLit);
                var deerTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Deer/Tex_Deer.png");
                if (deerTex != null) deerMat.SetTexture("_BaseMap", deerTex);
                AssetDatabase.CreateAsset(deerMat, "Assets/Materials/Authentic_Deer.mat");
            }

            AssetDatabase.SaveAssets();

            // Load Model Assets from Decorate folder
            var birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Bird by Poly by Google - 8Ph79kHbt9s/Bird_01(1).obj");
            var cobraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Cobra by Poly by Google - 40_5Xq467-U/cobra.obj");
            var crocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Crocodile by Poly by Google - 2an6E2WjW3z/Mesh_Crocodile.obj");
            var dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Dog by madtrollstudio - H2SkxSJCfI/dogFinal (5).fbx");
            var deerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/animal/Deer by Poly by Google - fUo4AIcd8XR/Mesh_Deer.obj");

            // ── VERIFIED REALISTIC SCALES (Meters) ───────────────────────────
            // Scaled up to natural proportions matching characters:
            const float BirdScale  = 0.0035f;  // ~0.85m wingspan eagle/kingfisher perched high in trees
            const float CobraScale = 0.24f;    // ~1.3m flared king cobra
            const float CrocScale  = 0.038f;   // ~4.5m apex saltwater crocodile (visible and exciting!)
            const float DogScale   = 0.65f;    // ~0.75m Asiatic dhole wild dog
            const float DeerScale  = 0.024f;   // matches player Chitra spotted deer

            // -------------------------------------------------------------
            // 1. MANGROVE BIRDS (Perched high on canopy branches, spaced 40-50m apart)
            // -------------------------------------------------------------
            var birdsGroup = new GameObject("Birds_Mangrove_Perches").transform;
            birdsGroup.SetParent(rootGo.transform, false);

            var birdPositions = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-52.0f, 4.80f,  235.0f),  135f),
                (new Vector3( 45.0f, 5.20f,  195.0f),  -60f),
                (new Vector3(-50.0f, 4.50f,  150.0f),   45f),
                (new Vector3( 42.0f, 5.00f,  105.0f),  190f),
                (new Vector3(-48.0f, 4.60f,   60.0f),  -40f),
                (new Vector3( 40.0f, 4.90f,   15.0f),  120f),
                (new Vector3(-52.0f, 4.40f,  -35.0f),  -85f),
                (new Vector3( 38.0f, 4.70f,  -75.0f),   30f)
            };

            for (int i = 0; i < birdPositions.Length; i++)
            {
                var bp = birdPositions[i];
                var birdGo = CreateAnimalInstance(birdPrefab, $"Mangrove_Bird_{i + 1:D2}", birdsGroup, bp.pos, bp.yaw, BirdScale);
                ApplyMaterialToRenderers(birdGo, birdBodyMat);
                var idle = birdGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.Bird;
                idle.animSpeed = Random.Range(0.85f, 1.25f);
            }

            // -------------------------------------------------------------
            // 2. KING COBRAS (Coiled on shaded periphery mounds, 40-50m apart)
            // -------------------------------------------------------------
            var cobrasGroup = new GameObject("Cobras_Mangrove_Thickets").transform;
            cobrasGroup.SetParent(rootGo.transform, false);

            var cobraPositions = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-50.0f, 0f,  210.0f),   45f),
                (new Vector3( 48.0f, 0f,  145.0f), -110f),
                (new Vector3(-48.0f, 0f,   85.0f),  130f),
                (new Vector3( 46.0f, 0f,   25.0f),  -60f),
                (new Vector3(-50.0f, 0f,  -55.0f),   75f)
            };

            for (int i = 0; i < cobraPositions.Length; i++)
            {
                var cp = cobraPositions[i];
                Vector3 groundPos = SnapToTerrainGround(cp.pos, 0.04f);
                var cobraGo = CreateAnimalInstance(cobraPrefab, $"King_Cobra_{i + 1:D2}", cobrasGroup, groundPos, cp.yaw, CobraScale);
                ApplyMaterialToRenderers(cobraGo, cobraMat);
                var idle = cobraGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.Cobra;
                idle.animSpeed = Random.Range(0.8f, 1.2f);
            }

            // -------------------------------------------------------------
            // 3. SALTWATER CROCODILES - SWIMMING IN RIVER & BASKING ON MUDBANKS
            // -------------------------------------------------------------
            var riverCrocsGroup = new GameObject("River_Crocodiles_Swimming").transform;
            riverCrocsGroup.SetParent(rootGo.transform, false);

            var riverCrocPositions = new (Vector3 pos, float yaw, bool swimming)[]
            {
                (new Vector3( 16.0f, 0.25f,  215.0f), -75f, false), // North river mudbank
                (new Vector3(  6.0f, -0.06f, 170.0f),  15f,  true),  // North swimming channel
                (new Vector3(-14.0f, 0.22f,  125.0f),  80f, false), // Mid river mudbank
                (new Vector3(  8.0f, -0.06f,  75.0f), 195f,  true),  // Mid swimming channel
                (new Vector3( 18.0f, 0.20f,   20.0f), -85f, false), // South river mudbank
                (new Vector3(  5.0f, -0.06f, -30.0f),  20f,  true),  // Lower estuary swimming
                (new Vector3(-15.0f, 0.18f,  -70.0f),  65f, false)  // Coastal ocean mouth mudbank
            };

            for (int i = 0; i < riverCrocPositions.Length; i++)
            {
                var rcp = riverCrocPositions[i];
                Vector3 p = rcp.pos;
                if (!rcp.swimming)
                {
                    p = SnapToTerrainGround(p, 0.04f);
                }
                var crocGo = CreateAnimalInstance(crocPrefab, $"River_Crocodile_{i + 1:D2}", riverCrocsGroup, p, rcp.yaw, CrocScale);
                ApplyMaterialToRenderers(crocGo, crocMat);
                var idle = crocGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.Crocodile;
                idle.isSwimmingInRiver = rcp.swimming;
                idle.animSpeed = Random.Range(0.75f, 1.15f);

                var col = crocGo.GetComponent<SphereCollider>() ?? crocGo.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 120f;
            }

            // -------------------------------------------------------------
            // 4. SUNDARBAN WILD DOGS / DHOLES (Patrolling peripheral ridges, 45m apart)
            // -------------------------------------------------------------
            var dogsGroup = new GameObject("WildDogs_Forest_Trails").transform;
            dogsGroup.SetParent(rootGo.transform, false);

            var dogPositions = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-55.0f, 0f,  220.0f),   45f),
                (new Vector3( 52.0f, 0f,  160.0f),  -80f),
                (new Vector3(-52.0f, 0f,  100.0f),  120f),
                (new Vector3( 50.0f, 0f,   35.0f),  -30f),
                (new Vector3(-54.0f, 0f,  -30.0f),   65f),
                (new Vector3( 48.0f, 0f,  -70.0f),  180f)
            };

            for (int i = 0; i < dogPositions.Length; i++)
            {
                var dp = dogPositions[i];
                Vector3 groundPos = SnapToTerrainGround(dp.pos, 0.04f);
                var dogGo = CreateAnimalInstance(dogPrefab, $"Sundarban_WildDog_{i + 1:D2}", dogsGroup, groundPos, dp.yaw, DogScale);
                ApplyMaterialToRenderers(dogGo, dogMat);
                var idle = dogGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.WildDog;
                idle.animSpeed = Random.Range(0.9f, 1.25f);
            }

            // -------------------------------------------------------------
            // 5. WILD SPOTTED DEER (Distant meadow grazing pairs, 15-20m off trail)
            // -------------------------------------------------------------
            var deerGroup = new GameObject("WildDeer_Meadows_Grazing").transform;
            deerGroup.SetParent(rootGo.transform, false);

            var deerPositions = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-58.0f, 0f,  230.0f),   60f),
                (new Vector3(-62.0f, 0f,  226.0f),  110f),
                (new Vector3( 55.0f, 0f,  165.0f),  -80f),
                (new Vector3( 58.0f, 0f,  161.0f), -135f),
                (new Vector3(-56.0f, 0f,  105.0f),   45f),
                (new Vector3(-60.0f, 0f,  101.0f),   95f),
                (new Vector3( 52.0f, 0f,   45.0f),  -70f),
                (new Vector3( 56.0f, 0f,   41.0f), -120f),
                (new Vector3(-54.0f, 0f,  -40.0f),   55f),
                (new Vector3( 50.0f, 0f,  -65.0f),  -90f)
            };

            for (int i = 0; i < deerPositions.Length; i++)
            {
                var drp = deerPositions[i];
                Vector3 groundPos = SnapToTerrainGround(drp.pos, 0.04f);
                var wildDeerGo = CreateAnimalInstance(deerPrefab, $"Wild_SpottedDeer_{i + 1:D2}", deerGroup, groundPos, drp.yaw, DeerScale);
                ApplyMaterialToRenderers(wildDeerGo, deerMat);
                var idle = wildDeerGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.WildDeer;
                idle.animSpeed = Random.Range(0.85f, 1.15f);
            }

            // -------------------------------------------------------------
            // 7. OCEAN VIEW AT SEA SIDE OF MAP (Matching Southern Minimap Waters)
            // -------------------------------------------------------------
            SetupOceanViewAtSeaSide();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            int totalDecorated = birdPositions.Length + cobraPositions.Length + riverCrocPositions.Length +
                                 dogPositions.Length + deerPositions.Length;

            Debug.Log($"<color=green>[DecorateSundarbanWildlife] Successfully decorated with {totalDecorated} properly-sized animals! " +
                      $"(Birds: {birdPositions.Length}, Cobras: {cobraPositions.Length}, River/Mudbank Crocodiles: {riverCrocPositions.Length}, " +
                      $"Wild Dogs: {dogPositions.Length}, Wild Deer: {deerPositions.Length}) " +
                      $"and activated the Bay of Bengal ocean view!</color>");
        }

        private static void SetupOceanViewAtSeaSide()
        {
            // 1. Activate or create BayOfBengal_Ocean_System
            var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            GameObject oceanSys = null;
            GameObject beachSys = null;

            foreach (var r in roots)
            {
                if (r.name == "BayOfBengal_Ocean_System") oceanSys = r;
                if (r.name == "Sea_Beach_Shorelines") beachSys = r;
            }

            if (oceanSys != null)
            {
                oceanSys.SetActive(true);
                for (int i = 0; i < oceanSys.transform.childCount; i++)
                {
                    oceanSys.transform.GetChild(i).gameObject.SetActive(true);
                }
            }

            if (beachSys != null)
            {
                beachSys.SetActive(true);
                for (int i = 0; i < beachSys.transform.childCount; i++)
                {
                    beachSys.transform.GetChild(i).gameObject.SetActive(true);
                }
            }

            // 2. Ensure Ocean Material has authentic Bay of Bengal waters
            var oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_OceanWater.mat");
            var riverShader = Shader.Find("Custom/SundarbanRiverWater");
            if (oceanMat != null && riverShader != null)
            {
                oceanMat.shader = riverShader;
                oceanMat.SetColor("_OceanBlueColor", new Color(0.10f, 0.36f, 0.52f, 0.92f));
                oceanMat.SetColor("_OceanShallowBlueColor", new Color(0.24f, 0.52f, 0.64f, 0.75f));
                oceanMat.SetColor("_SkyReflectionColor", new Color(0.85f, 0.92f, 0.98f, 1.0f));
                oceanMat.SetFloat("_Smoothness", 0.96f);
                oceanMat.SetFloat("_BumpScale", 0.45f);
                oceanMat.SetVector("_WaveSpeed", new Vector4(0.014f, 0.022f, -0.010f, 0.016f));
                EditorUtility.SetDirty(oceanMat);
            }

            // 3. Ensure Ocean Mesh covers the southern sea boundary (Z = -260 to -1000)
            if (oceanSys != null)
            {
                var oceanChild = oceanSys.transform.Find("BayOfBengal_Ocean");
                if (oceanChild != null)
                {
                    oceanChild.position = new Vector3(0f, 0.02f, 0f);
                }
            }

            Debug.Log("[DecorateSundarbanWildlife] Bay of Bengal ocean view and coastal shoreline active at southern sea side!");
        }

        private static GameObject CreateAnimalInstance(GameObject prefab, string name, Transform parent, Vector3 pos, float yaw, float scale)
        {
            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, parent);
                instance.name = name;
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.name = name;
                instance.transform.SetParent(parent, false);
            }

            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;

            // Make sure colliders are triggers or non-blocking
            var colliders = instance.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.isTrigger = true;
            }

            return instance;
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material mat)
        {
            if (mat == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }
                r.sharedMaterials = mats;
            }
        }

        private static Vector3 SnapToTerrainGround(Vector3 originalPos, float yOffset)
        {
            Ray ray = new Ray(new Vector3(originalPos.x, 35f, originalPos.z), Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 70f);

            float bestY = originalPos.y;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider.isTrigger) continue;
                if (h.collider.gameObject.name.Contains("Danger") || h.collider.gameObject.name.Contains("Zone")) continue;

                if (!foundGround || h.point.y < bestY)
                {
                    bestY = h.point.y;
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                return new Vector3(originalPos.x, bestY + yOffset, originalPos.z);
            }
            return originalPos;
        }
    }
}
