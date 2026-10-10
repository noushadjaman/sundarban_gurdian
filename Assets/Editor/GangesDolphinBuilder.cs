using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using SundarbanGuardian.Player;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Editor
{
    public static class GangesDolphinBuilder
    {
        private const string TexturePath = "Assets/Textures/Tex_Ganges_Dolphin_2K.png";
        private const string MaterialPath = "Assets/Materials/Mat_Ganges_Dolphin_Body.mat";

        [MenuItem("Sundarban/Build Ganges River Dolphin")]
        public static void BuildDolphin()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log("[GangesDolphinBuilder] Starting Ganges River Dolphin visual & locomotion revamp...");

            // 1. Generate 2K PBR Ganges Dolphin Texture
            var tex = GenerateGangesDolphinTexture();

            // 2. Create / Update PBR Lit Material with wet river sheen
            var mat = CreateOrUpdateMaterial(tex);

            // 3. Find and Configure Dolphin GameObject
            var dolphinGO = GameObject.Find("Dolphin");
            if (dolphinGO == null)
            {
                Debug.LogError("[GangesDolphinBuilder] Dolphin GameObject not found in active scene!");
                return;
            }

            // 4. Moderate Size Scaling (2.42m biological length)
            dolphinGO.transform.localScale = Vector3.one * 0.26f;

            // 5. Morph Mesh: Generate UVs, elongate rostrum beak with teeth line, shape melon forehead, lower dorsal ridge
            ConfigureDolphinMesh(dolphinGO, mat);

            // 6. Setup Locomotion, Physics, and Swimming Components
            ConfigureComponents(dolphinGO);

            // 7. Position Dolphin in the Big River Channel facing downstream toward the Bay of Bengal
            float startZ = 110f;
            float startX = BuildSundarbanFullMap.RiverCenterX(startZ);
            dolphinGO.transform.position = new Vector3(startX, -0.65f, startZ);
            dolphinGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing South down the river

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[GangesDolphinBuilder] Ganges River Dolphin successfully revamped with authentic visuals, moderate scale, and biological swimming locomotion!");
        }

        private static Texture2D GenerateGangesDolphinTexture()
        {
            int width = 2048;
            int height = 2048;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.name = "Tex_Ganges_Dolphin_2K";

            Color slateDorsal = new Color(0.28f, 0.31f, 0.33f, 1f);       // River mud slate-grey
            Color oliveFlank  = new Color(0.38f, 0.40f, 0.40f, 1f);       // Subtle olive silt tone
            Color pinkVentral = new Color(0.88f, 0.82f, 0.80f, 1f);       // Pale pinkish-cream underbelly/throat
            Color darkSnout   = new Color(0.18f, 0.19f, 0.20f, 1f);       // Dark rostrum
            Color toothIvory  = new Color(0.96f, 0.95f, 0.90f, 1f);       // Interlocking needle-like teeth
            Color eyeDot      = new Color(0.06f, 0.06f, 0.07f, 1f);       // Rudimentary pinhole eye
            Color eyeRing     = new Color(0.48f, 0.46f, 0.44f, 1f);

            var colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height; // Longitudinal position: 0 = rostrum/snout, 1 = tail flukes

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width; // Radial position around body: 0 = dorsal top, 0.5 = ventral belly, 1.0 = dorsal top
                    float distFromVentral = Mathf.Abs(u - 0.5f); // 0 at belly, 0.5 at dorsal spine

                    // Base skin gradient: Slate grey dorsum -> pale pinkish belly
                    float dorsalFactor = Mathf.Clamp01((distFromVentral - 0.12f) / 0.28f);
                    dorsalFactor = Mathf.SmoothStep(0f, 1f, dorsalFactor);
                    Color skinCol = Color.Lerp(pinkVentral, Color.Lerp(oliveFlank, slateDorsal, dorsalFactor), dorsalFactor);

                    // Organic sediment / skin mottling noise
                    float n1 = Mathf.PerlinNoise(x * 0.045f, y * 0.045f) * 0.08f - 0.04f;
                    float n2 = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.04f - 0.02f;
                    skinCol.r = Mathf.Clamp01(skinCol.r + n1 + n2);
                    skinCol.g = Mathf.Clamp01(skinCol.g + n1 + n2);
                    skinCol.b = Mathf.Clamp01(skinCol.b + n1 + n2);

                    // Rostrum (Snout) zone: v < 0.22
                    if (v < 0.22f)
                    {
                        float snoutT = 1f - (v / 0.22f);
                        skinCol = Color.Lerp(skinCol, darkSnout, snoutT * 0.85f);

                        // Rostrum Jaw Line & Dental Fringe (sharp visible teeth along the lateral jawline at u ≈ 0.25 and u ≈ 0.75)
                        bool nearLeftJaw  = Mathf.Abs(u - 0.25f) < 0.025f;
                        bool nearRightJaw = Mathf.Abs(u - 0.75f) < 0.025f;
                        if (nearLeftJaw || nearRightJaw)
                        {
                            // Distinct pale gumline
                            skinCol = Color.Lerp(skinCol, new Color(0.65f, 0.60f, 0.58f), 0.6f);

                            // Interlocking tooth spikes (spaced periodically along rostrum length)
                            float toothCycle = Mathf.Sin(v * 480f);
                            if (toothCycle > 0.45f && (Mathf.Abs(u - 0.25f) < 0.012f || Mathf.Abs(u - 0.75f) < 0.012f))
                            {
                                skinCol = toothIvory;
                            }
                        }

                        // Tiny pinhole eyes (at v ≈ 0.19, u ≈ 0.20 and u ≈ 0.80)
                        float dEyeL = Mathf.Sqrt(Mathf.Pow((u - 0.20f) * 1.5f, 2f) + Mathf.Pow((v - 0.19f) * 3f, 2f));
                        float dEyeR = Mathf.Sqrt(Mathf.Pow((u - 0.80f) * 1.5f, 2f) + Mathf.Pow((v - 0.19f) * 3f, 2f));
                        float minEyeD = Mathf.Min(dEyeL, dEyeR);

                        if (minEyeD < 0.007f)
                        {
                            skinCol = eyeDot; // Tiny black pinhole pupil
                        }
                        else if (minEyeD < 0.016f)
                        {
                            skinCol = eyeRing; // Pale orbital ring
                        }
                    }

                    // Transverse Neck Creases behind the melon forehead (v ≈ 0.25 to 0.29)
                    if (v > 0.24f && v < 0.30f && dorsalFactor > 0.3f)
                    {
                        float crease1 = Mathf.Abs(v - 0.265f);
                        float crease2 = Mathf.Abs(v - 0.285f);
                        if (crease1 < 0.005f || crease2 < 0.005f)
                        {
                            skinCol *= 0.82f; // Subtle shadow fold
                        }
                    }

                    // Tail flukes (v > 0.88): Darker slate grey with fine ray striations
                    if (v > 0.88f)
                    {
                        float flukeT = (v - 0.88f) / 0.12f;
                        skinCol = Color.Lerp(skinCol, slateDorsal * 0.85f, flukeT * 0.7f);
                        float ray = Mathf.Sin(u * 240f) * 0.03f;
                        skinCol.r = Mathf.Clamp01(skinCol.r + ray);
                        skinCol.g = Mathf.Clamp01(skinCol.g + ray);
                        skinCol.b = Mathf.Clamp01(skinCol.b + ray);
                    }

                    colors[y * width + x] = skinCol;
                }
            }

            tex.SetPixels(colors);
            tex.Apply(true);

            // Save PNG to Assets/Textures
            byte[] bytes = tex.EncodeToPNG();
            string dir = Path.GetDirectoryName(TexturePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(TexturePath, bytes);
            AssetDatabase.ImportAsset(TexturePath);

            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }

            Debug.Log("[GangesDolphinBuilder] Generated 2K Ganges River Dolphin PBR texture at " + TexturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static Material CreateOrUpdateMaterial(Texture2D tex)
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                string dir = Path.GetDirectoryName(MaterialPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            mat.shader = urpLit;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.72f); // Wet river aquatic sheen
            mat.SetFloat("_Metallic", 0.04f);   // Organic leathery skin

            // Subtle normal perturbation for wet skin
            var bumpTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ocean_Water_Normal.tga");
            if (bumpTex != null)
            {
                mat.SetTexture("_BumpMap", bumpTex);
                mat.SetFloat("_BumpScale", 0.15f);
                mat.SetTextureScale("_BumpMap", new Vector2(10f, 20f));
                mat.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void ConfigureDolphinMesh(GameObject dolphinGO, Material mat)
        {
            var smr = dolphinGO.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null) return;

            Mesh origMesh = smr.sharedMesh;
            Mesh customMesh = Object.Instantiate(origMesh);
            customMesh.name = "Mesh_Ganges_Dolphin_Platanista";

            Vector3[] verts = customMesh.vertices;
            Vector3[] normals = customMesh.normals;
            Vector2[] uvs = new Vector2[verts.Length];

            // Local mesh bounds:
            // Y is length (-0.038 Head/Snout to +0.051 Tail)
            // Z is vertical (-0.015 Ventral belly to +0.022 Dorsal ridge)
            // X is width (-0.018 to +0.018)

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];

                // 1. Generate Clean Cylindrical UV mapping
                float normY = Mathf.InverseLerp(-0.045f, 0.050f, v.y); // 0 at snout, 1 at tail
                float angle = Mathf.Atan2(v.x, v.z); // -PI to PI
                float normU = Mathf.Repeat((angle / (Mathf.PI * 2f)) + 0.5f, 1.0f);
                uvs[i] = new Vector2(normU, normY);

                // 2. Ganges Dolphin Long Slender Rostrum Beak:
                // Platanista gangetica has a remarkably elongated, slender needle-like rostrum
                if (v.y < -0.014f)
                {
                    float snoutRatio = Mathf.Clamp01((-0.014f - v.y) / 0.030f); // 0 at base, 1 at tip
                    // Elongate rostrum forward significantly
                    v.y -= snoutRatio * 0.024f;
                    // Slenderize rostrum width (needle beak)
                    v.x *= Mathf.Lerp(1.0f, 0.42f, snoutRatio);
                    // Compress rostrum height
                    v.z *= Mathf.Lerp(1.0f, 0.65f, snoutRatio);
                }

                // 3. Prominent Bulbous Melon Forehead (echolocation acoustic organ):
                // Pronounced convex dome curving smoothly above rostrum base
                if (v.y >= -0.028f && v.y <= -0.006f && v.z > 0.001f)
                {
                    float melonCenter = -0.017f;
                    float melonDist = Mathf.Abs(v.y - melonCenter) / 0.011f;
                    if (melonDist < 1.0f)
                    {
                        float dome = Mathf.Cos(melonDist * Mathf.PI * 0.5f);
                        v.z += dome * 0.0055f;
                    }
                }

                // 4. Low Triangular Dorsal Ridge (No tall oceanic fin):
                // Flatten dorsal fin into the authentic low fleshy hump of Platanista
                if (v.z > 0.005f && v.y > -0.008f && v.y < 0.028f)
                {
                    float finHeight = v.z - 0.005f;
                    v.z = 0.005f + finHeight * 0.32f;
                }

                verts[i] = v;
            }

            customMesh.vertices = verts;
            customMesh.uv = uvs;
            customMesh.RecalculateNormals();
            customMesh.RecalculateBounds();

            smr.sharedMesh = customMesh;

            // Apply authentic Ganges Dolphin material to all submeshes
            Material[] mats = new Material[smr.sharedMesh.subMeshCount];
            for (int s = 0; s < mats.Length; s++)
            {
                mats[s] = mat;
            }
            smr.sharedMaterials = mats;
        }

        private static void ConfigureComponents(GameObject dolphinGO)
        {
            // Rigidbody
            var rb = dolphinGO.GetComponent<Rigidbody>();
            if (rb == null) rb = dolphinGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // CapsuleCollider
            var col = dolphinGO.GetComponent<CapsuleCollider>();
            if (col == null) col = dolphinGO.AddComponent<CapsuleCollider>();
            col.direction = 2; // Z-axis (Forward in world space)
            col.radius = 0.42f;
            col.height = 2.45f;
            col.center = new Vector3(0f, 0f, 0f);

            // DolphinSwimController
            var swimCtrl = dolphinGO.GetComponent<DolphinSwimController>();
            if (swimCtrl == null) swimCtrl = dolphinGO.AddComponent<DolphinSwimController>();
            swimCtrl.swimSpeed = 10f;
            swimCtrl.sprintMultiplier = 1.65f;
            swimCtrl.turnSpeedDeg = 280f;
            swimCtrl.surfaceY = 0.04f;
            swimCtrl.diveY = -1.25f;
            swimCtrl.minX = -500f;
            swimCtrl.maxX = 500f;
            swimCtrl.minZ = -900f;
            swimCtrl.maxZ = 300f;

            // GangesDolphinProceduralAnimator
            var procAnim = dolphinGO.GetComponent<GangesDolphinProceduralAnimator>();
            if (procAnim == null) procAnim = dolphinGO.AddComponent<GangesDolphinProceduralAnimator>();
            procAnim.editorPreview = true;
            procAnim.previewSpeed = 1.0f;
            procAnim.swimFrequency = 1.6f;
            procAnim.spineFlexionAngle = 14f;
            procAnim.tailStrokeAngle = 26f;
            procAnim.headScanYaw = 9f;
            procAnim.InitializeBones();

            // SpeciesHealth
            var health = dolphinGO.GetComponent<SundarbanGuardian.Core.SpeciesHealth>();
            if (health == null) health = dolphinGO.AddComponent<SundarbanGuardian.Core.SpeciesHealth>();
            health.maxHealth = 100f;
        }
    }
}
