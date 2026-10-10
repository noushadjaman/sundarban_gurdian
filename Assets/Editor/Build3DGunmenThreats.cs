using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    public static class Build3DGunmenThreats
    {
        private const string RootName = "[Poacher_Gunmen_Threats]";
        private const string TextureDir = "Assets/Textures/Characters";
        private const string MaterialDir = "Assets/Materials";

        [MenuItem("Sundarban/Build 3D Gunmen Hunter Threats")]
        public static void BuildGunmen()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/SampleScene.unity")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // 1. Ensure Textures & Materials
            EnsureDirectories();
            GenerateCamoTexture();
            CreateMaterials();

            var camoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Camo.mat");
            var vestMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_OrangeVest.mat");
            var rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Rifle.mat");
            var laserMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Laser_Sight_Red.mat");
            var bootMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Boots.mat");
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Skin.mat");

            // 2. Clear old gunmen root if present
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            var rootGo = new GameObject(RootName);
            rootGo.transform.position = Vector3.zero;

            // 3. Strategic Gunmen Ambush Locations across North-to-South Ocean Journey:
            var spawnPoints = new (Vector3 pos, float yaw, string name)[]
            {
                (new Vector3(-32f, 0f, 225f), 180f, "Poacher_Gunman_NorthRidge"),
                (new Vector3( 26f, 0f, 185f), 195f, "Poacher_Gunman_NorthCrossing"),
                (new Vector3(-35f, 0f, 135f), 170f, "Poacher_Gunman_LoggingBasin"),
                (new Vector3( 36f, 0f,  80f), 190f, "Poacher_Gunman_EastRiverbank"),
                (new Vector3(-18f, 0f,  30f), 175f, "Poacher_Gunman_StreamCrossing"),
                (new Vector3( 22f, 0f, -25f), 195f, "Poacher_Gunman_EstuaryTrail"),
                (new Vector3(-36f, 0f, -70f), 180f, "Poacher_Gunman_CoastalApproach")
            };

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var pt = spawnPoints[i];

                // Raycast down to find ground elevation
                Vector3 groundPos = pt.pos;
                Ray ray = new Ray(new Vector3(pt.pos.x, 30f, pt.pos.z), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    groundPos.y = hit.point.y;
                }

                BuildSingleGunman(
                    parent: rootGo.transform,
                    name: pt.name,
                    pos: groundPos,
                    yaw: pt.yaw,
                    camoMat: camoMat,
                    vestMat: vestMat,
                    rifleMat: rifleMat,
                    laserMat: laserMat,
                    bootMat: bootMat,
                    skinMat: skinMat
                );
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=green>[Build3DGunmenThreats] Successfully built {spawnPoints.Length} 3D Hunter Gunmen across Sundarbans!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(TextureDir)) Directory.CreateDirectory(TextureDir);
            if (!Directory.Exists(MaterialDir)) Directory.CreateDirectory(MaterialDir);
        }

        private static void GenerateCamoTexture()
        {
            string path = Path.Combine(TextureDir, "Hunter_Camo.png");
            if (File.Exists(path)) return;

            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color[] pixels = new Color[size * size];

            Color deepGreen = new Color(0.14f, 0.22f, 0.12f);
            Color forestBrown = new Color(0.28f, 0.20f, 0.14f);
            Color khaki = new Color(0.42f, 0.38f, 0.26f);
            Color darkBlack = new Color(0.08f, 0.09f, 0.07f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                    float n2 = Mathf.PerlinNoise((x + 200) * 0.05f, (y + 200) * 0.05f);
                    float combined = n1 * 0.65f + n2 * 0.35f;

                    Color c;
                    if (combined < 0.28f) c = darkBlack;
                    else if (combined < 0.52f) c = deepGreen;
                    else if (combined < 0.72f) c = forestBrown;
                    else c = khaki;

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void CreateMaterials()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            // 1. Camo Material
            var camoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Camo.mat");
            if (camoMat == null)
            {
                camoMat = new Material(urpLit);
                AssetDatabase.CreateAsset(camoMat, "Assets/Materials/Hunter_Camo.mat");
            }
            camoMat.shader = urpLit;
            var camoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TextureDir, "Hunter_Camo.png"));
            if (camoTex != null) camoMat.SetTexture("_BaseMap", camoTex);
            camoMat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(camoMat);

            // 2. High-Visibility Blaze Orange Hunter Vest & Cap (Matching User Photo)
            var vestMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_OrangeVest.mat");
            if (vestMat == null)
            {
                vestMat = new Material(urpLit);
                AssetDatabase.CreateAsset(vestMat, "Assets/Materials/Hunter_OrangeVest.mat");
            }
            vestMat.shader = urpLit;
            vestMat.SetColor("_BaseColor", new Color(1.0f, 0.34f, 0.02f, 1.0f)); // Bright blaze orange
            vestMat.SetFloat("_Smoothness", 0.20f);
            EditorUtility.SetDirty(vestMat);

            // 3. Hunting Rifle Steel & Composite Stock
            var rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Rifle.mat");
            if (rifleMat == null)
            {
                rifleMat = new Material(urpLit);
                AssetDatabase.CreateAsset(rifleMat, "Assets/Materials/Hunter_Rifle.mat");
            }
            rifleMat.shader = urpLit;
            rifleMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.13f, 1.0f));
            rifleMat.SetFloat("_Metallic", 0.85f);
            rifleMat.SetFloat("_Smoothness", 0.65f);
            EditorUtility.SetDirty(rifleMat);

            // 4. Laser Sight Red (Unlit bright red)
            var laserMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Laser_Sight_Red.mat");
            if (laserMat == null)
            {
                laserMat = new Material(urpUnlit);
                AssetDatabase.CreateAsset(laserMat, "Assets/Materials/Laser_Sight_Red.mat");
            }
            laserMat.shader = urpUnlit;
            laserMat.SetColor("_BaseColor", new Color(1.0f, 0.05f, 0.05f, 1.0f));
            EditorUtility.SetDirty(laserMat);

            // 5. Boots
            var bootMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Boots.mat");
            if (bootMat == null)
            {
                bootMat = new Material(urpLit);
                AssetDatabase.CreateAsset(bootMat, "Assets/Materials/Hunter_Boots.mat");
            }
            bootMat.shader = urpLit;
            bootMat.SetColor("_BaseColor", new Color(0.18f, 0.14f, 0.10f, 1.0f));
            EditorUtility.SetDirty(bootMat);

            // 6. Skin
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hunter_Skin.mat");
            if (skinMat == null)
            {
                skinMat = new Material(urpLit);
                AssetDatabase.CreateAsset(skinMat, "Assets/Materials/Hunter_Skin.mat");
            }
            skinMat.shader = urpLit;
            skinMat.SetColor("_BaseColor", new Color(0.82f, 0.64f, 0.50f, 1.0f));
            EditorUtility.SetDirty(skinMat);

            AssetDatabase.SaveAssets();
        }

        private static void BuildSingleGunman(Transform parent, string name, Vector3 pos, float yaw,
            Material camoMat, Material vestMat, Material rifleMat, Material laserMat, Material bootMat, Material skinMat)
        {
            var hunterGo = new GameObject(name);
            hunterGo.transform.SetParent(parent, false);
            hunterGo.transform.position = pos;
            hunterGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Add Collider & Threat Component
            var col = hunterGo.AddComponent<CapsuleCollider>();
            col.height = 1.85f;
            col.radius = 0.40f;
            col.center = new Vector3(0f, 0.92f, 0f);

            var threat = hunterGo.AddComponent<GunmanThreat>();
            threat.detectionRadius = 24.0f;
            threat.shotCooldown = 5.0f;
            threat.shotDamage = 50.0f;
            threat.canBeEliminatedByTiger = true;
            threat.tigerTakedownRadius = 3.6f;

            // ---------------------------------------------------------
            // BODY VISUALS (Matching User Photo: Camo + Orange Vest & Cap)
            // ---------------------------------------------------------
            var bodyRoot = new GameObject("Visual_Body");
            bodyRoot.transform.SetParent(hunterGo.transform, false);

            // 1. Legs (Camo hunting pants)
            CreateLimb(bodyRoot.transform, "Leg_L", new Vector3(-0.16f, 0.45f, 0f), new Vector3(0.18f, 0.85f, 0.18f), camoMat);
            CreateLimb(bodyRoot.transform, "Leg_R", new Vector3( 0.16f, 0.45f, -0.05f), new Vector3(0.18f, 0.85f, 0.18f), camoMat);

            // Boots
            CreateLimb(bodyRoot.transform, "Boot_L", new Vector3(-0.16f, 0.08f, 0.04f), new Vector3(0.20f, 0.16f, 0.28f), bootMat);
            CreateLimb(bodyRoot.transform, "Boot_R", new Vector3( 0.16f, 0.08f, -0.01f), new Vector3(0.20f, 0.16f, 0.28f), bootMat);

            // 2. Torso (Inner Camo + Blaze Orange Safety Vest)
            // Inner Camo Jacket
            CreateLimb(bodyRoot.transform, "Torso_Jacket", new Vector3(0f, 1.15f, 0f), new Vector3(0.44f, 0.58f, 0.28f), camoMat);
            // Outer Blaze Orange Vest (Front and Back, sleeveless like in photo!)
            CreateLimb(bodyRoot.transform, "Hunter_Orange_Vest", new Vector3(0f, 1.15f, 0f), new Vector3(0.48f, 0.54f, 0.32f), vestMat);

            // 3. Head & Orange Cap
            var head = CreateLimb(bodyRoot.transform, "Head", new Vector3(0f, 1.58f, 0.05f), new Vector3(0.22f, 0.25f, 0.22f), skinMat);

            // Blaze Orange Baseball Cap Dome
            var capDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            capDome.name = "Orange_Cap_Dome";
            capDome.transform.SetParent(head.transform, false);
            capDome.transform.localPosition = new Vector3(0f, 0.35f, 0.02f);
            capDome.transform.localScale = new Vector3(1.12f, 0.65f, 1.15f);
            Object.DestroyImmediate(capDome.GetComponent<Collider>());
            capDome.GetComponent<MeshRenderer>().sharedMaterial = vestMat;

            // Cap Visor / Brim extending forward
            var capVisor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            capVisor.name = "Cap_Visor";
            capVisor.transform.SetParent(head.transform, false);
            capVisor.transform.localPosition = new Vector3(0f, 0.30f, 0.62f);
            capVisor.transform.localScale = new Vector3(0.85f, 0.06f, 0.60f);
            Object.DestroyImmediate(capVisor.GetComponent<Collider>());
            capVisor.GetComponent<MeshRenderer>().sharedMaterial = vestMat;

            // 4. Arms (Aiming Hunting Stance)
            // Left Arm extending forward supporting the rifle
            CreateLimb(bodyRoot.transform, "Arm_Left", new Vector3(-0.18f, 1.30f, 0.28f), new Vector3(0.14f, 0.48f, 0.14f), camoMat, new Vector3(65f, -20f, 0f));
            // Right Arm tucked holding the trigger grip
            CreateLimb(bodyRoot.transform, "Arm_Right", new Vector3(0.24f, 1.25f, 0.12f), new Vector3(0.14f, 0.44f, 0.14f), camoMat, new Vector3(50f, 15f, 0f));

            // ---------------------------------------------------------
            // SCOPED HUNTING RIFLE WITH RED LASER SIGHT
            // ---------------------------------------------------------
            var riflePivot = new GameObject("Rifle_Pivot");
            riflePivot.transform.SetParent(hunterGo.transform, false);
            riflePivot.transform.localPosition = new Vector3(0.08f, 1.36f, 0.22f);

            // Rifle Stock & Body
            var stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Rifle_Stock";
            stock.transform.SetParent(riflePivot.transform, false);
            stock.transform.localPosition = new Vector3(0f, -0.04f, -0.15f);
            stock.transform.localScale = new Vector3(0.06f, 0.14f, 0.42f);
            Object.DestroyImmediate(stock.GetComponent<Collider>());
            stock.GetComponent<MeshRenderer>().sharedMaterial = rifleMat;

            // Receiver & Action
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Rifle_Receiver";
            receiver.transform.SetParent(riflePivot.transform, false);
            receiver.transform.localPosition = new Vector3(0f, 0.02f, 0.12f);
            receiver.transform.localScale = new Vector3(0.05f, 0.08f, 0.25f);
            Object.DestroyImmediate(receiver.GetComponent<Collider>());
            receiver.GetComponent<MeshRenderer>().sharedMaterial = rifleMat;

            // Long Hunting Barrel
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Rifle_Barrel";
            barrel.transform.SetParent(riflePivot.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.03f, 0.52f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.035f, 0.35f, 0.035f);
            Object.DestroyImmediate(barrel.GetComponent<Collider>());
            barrel.GetComponent<MeshRenderer>().sharedMaterial = rifleMat;

            // Elevated Optical Scope (Image features large hunting rifle scope)
            var scope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scope.name = "Optical_Scope";
            scope.transform.SetParent(riflePivot.transform, false);
            scope.transform.localPosition = new Vector3(0f, 0.10f, 0.10f);
            scope.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            scope.transform.localScale = new Vector3(0.042f, 0.16f, 0.042f);
            Object.DestroyImmediate(scope.GetComponent<Collider>());
            scope.GetComponent<MeshRenderer>().sharedMaterial = rifleMat;

            // Scope Rings / Mounts
            var ring1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ring1.transform.SetParent(riflePivot.transform, false);
            ring1.transform.localPosition = new Vector3(0f, 0.065f, 0.04f);
            ring1.transform.localScale = new Vector3(0.03f, 0.05f, 0.03f);
            Object.DestroyImmediate(ring1.GetComponent<Collider>());
            ring1.GetComponent<MeshRenderer>().sharedMaterial = rifleMat;

            var ring2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ring2.transform.SetParent(riflePivot.transform, false);
            ring2.transform.localPosition = new Vector3(0f, 0.065f, 0.16f);
            ring2.transform.localScale = new Vector3(0.03f, 0.05f, 0.03f);
            Object.DestroyImmediate(ring2.GetComponent<Collider>());
            ring2.GetComponent<MeshRenderer>().sharedMaterial = rifleMat;

            // Muzzle Point
            var muzzleGo = new GameObject("Muzzle_Point");
            muzzleGo.transform.SetParent(riflePivot.transform, false);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.03f, 0.90f);

            // Muzzle Flash Light
            var muzzleLightGo = new GameObject("Muzzle_Flash_Light");
            muzzleLightGo.transform.SetParent(muzzleGo.transform, false);
            var mLight = muzzleLightGo.AddComponent<Light>();
            mLight.type = LightType.Point;
            mLight.color = new Color(1.0f, 0.75f, 0.25f);
            mLight.intensity = 5.0f;
            mLight.range = 8.0f;
            mLight.enabled = false;

            // Red Laser Sight LineRenderer
            var laserLine = hunterGo.AddComponent<LineRenderer>();
            laserLine.sharedMaterial = laserMat;
            laserLine.startWidth = 0.04f;
            laserLine.endWidth = 0.04f;
            laserLine.positionCount = 2;
            laserLine.useWorldSpace = true;
            laserLine.enabled = false;

            // Wire references into GunmanThreat
            threat.rifleTransform = riflePivot.transform;
            threat.muzzleTransform = muzzleGo.transform;
            threat.muzzleLight = mLight;
            threat.laserLine = laserLine;
        }

        private static GameObject CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Vector3? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (rotation.HasValue)
            {
                go.transform.localRotation = Quaternion.Euler(rotation.Value);
            }
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }
    }
}
