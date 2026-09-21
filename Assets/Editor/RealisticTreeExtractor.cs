using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SundarbanGuardian.Editor
{
    /// <summary>
    /// Extracts and configures the 7 realistic trees from Trees.fbx into individual,
    /// properly centered, textured URP prefabs ready for dense world placement.
    /// </summary>
    public static class RealisticTreeExtractor
    {
        private const string FbxPath = "Assets/ExternalModels/Nature/decorator/FBX/Fbx/Trees.fbx";
        private const string FbmDir = "Assets/ExternalModels/Nature/decorator/FBX/Fbx/Trees.fbm";
        private const string OutPrefabDir = "Assets/Prefabs/RealisticTrees";
        private const string OutMatDir = "Assets/Materials/RealisticTrees";

        [MenuItem("Sundarban/Extract 7 Realistic Tree Prefabs")]
        public static void ExtractAndBuildPrefabs()
        {
            Debug.Log("[RealisticTreeExtractor] Loading Trees.fbx and setting up realistic tree prefabs...");

            if (!Directory.Exists(OutPrefabDir)) Directory.CreateDirectory(OutPrefabDir);
            if (!Directory.Exists(OutMatDir)) Directory.CreateDirectory(OutMatDir);

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx == null)
            {
                Debug.LogError($"[RealisticTreeExtractor] Could not load FBX at {FbxPath}!");
                return;
            }

            // 1. Textures from Trees.fbm
            var barkTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/wood_0027_color_1k.jpg");
            var pineBarkTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/Bark_Pine_baseColor.jpg");
            var willowLeafTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/willowleaf.png");
            var mapleLeafTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/leaf4.png");
            var lushLeafTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/L1F.png");
            var birchLeafTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/pngegg.png");
            var cherryLeafTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FbmDir}/CherryLeaff.png");

            // 2. Create high-quality URP Lit materials
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");

            var willowBarkMat = GetOrCreateBarkMat("Mat_Bark_Willow", barkTex, urpShader, 0.45f);
            var pineBarkMat = GetOrCreateBarkMat("Mat_Bark_Pine", pineBarkTex, urpShader, 0.40f);
            var birchBarkMat = GetOrCreateBarkMat("Mat_Bark_Birch", barkTex, urpShader, 0.50f, new Color(0.92f, 0.90f, 0.85f));

            var willowLeafMat = GetOrCreateLeafMat("Mat_Leaves_Willow", willowLeafTex != null ? willowLeafTex : lushLeafTex, urpShader, new Color(0.42f, 0.58f, 0.30f));
            var greenMapleLeafMat = GetOrCreateLeafMat("Mat_Leaves_GreenMaple", mapleLeafTex != null ? mapleLeafTex : lushLeafTex, urpShader, new Color(0.32f, 0.54f, 0.22f));
            var redMapleLeafMat = GetOrCreateLeafMat("Mat_Leaves_RedMaple", mapleLeafTex != null ? mapleLeafTex : lushLeafTex, urpShader, new Color(0.68f, 0.32f, 0.20f));
            var birchLeafMat = GetOrCreateLeafMat("Mat_Leaves_Birch", birchLeafTex != null ? birchLeafTex : lushLeafTex, urpShader, new Color(0.48f, 0.62f, 0.26f));
            var pineNeedleMat = GetOrCreateLeafMat("Mat_Needles_Pine", lushLeafTex != null ? lushLeafTex : mapleLeafTex, urpShader, new Color(0.24f, 0.42f, 0.22f));
            var cherryLeafMat = GetOrCreateLeafMat("Mat_Leaves_Cherry", cherryLeafTex != null ? cherryLeafTex : lushLeafTex, urpShader, new Color(0.55f, 0.65f, 0.35f));

            AssetDatabase.SaveAssets();

            // 3. Instantiate FBX in temporary scene hierarchy to parse trees
            var instance = (GameObject)Object.Instantiate(fbx, Vector3.zero, Quaternion.identity);
            instance.name = "Trees_Temp_Extractor";

            string[] treeNames = {
                "Weeping_Willow", "Green_Maple", "Red_Maple",
                "White_Birch", "Big_Pine", "Small_Pine", "Japan_Cherry"
            };

            int extractedCount = 0;

            for (int i = 0; i < instance.transform.childCount; i++)
            {
                var child = instance.transform.GetChild(i);
                string cName = child.name;

                // Match with one of the 7 trees
                string matchedTree = null;
                foreach (var tName in treeNames)
                {
                    if (cName.IndexOf(tName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedTree = tName;
                        break;
                    }
                }

                if (matchedTree == null) continue;

                // Create clean isolated prefab container
                GameObject isolatedTree = new GameObject(matchedTree);
                isolatedTree.transform.position = Vector3.zero;
                isolatedTree.transform.rotation = Quaternion.identity;

                // Move child to isolatedTree and zero out offset
                child.SetParent(isolatedTree.transform, true);
                child.localPosition = Vector3.zero;

                // Select appropriate materials
                Material bMat = willowBarkMat;
                Material lMat = greenMapleLeafMat;

                switch (matchedTree)
                {
                    case "Weeping_Willow":
                        bMat = willowBarkMat;
                        lMat = willowLeafMat;
                        break;
                    case "Green_Maple":
                        bMat = willowBarkMat;
                        lMat = greenMapleLeafMat;
                        break;
                    case "Red_Maple":
                        bMat = willowBarkMat;
                        lMat = redMapleLeafMat;
                        break;
                    case "White_Birch":
                        bMat = birchBarkMat;
                        lMat = birchLeafMat;
                        break;
                    case "Big_Pine":
                    case "Small_Pine":
                        bMat = pineBarkMat;
                        lMat = pineNeedleMat;
                        break;
                    case "Japan_Cherry":
                        bMat = willowBarkMat;
                        lMat = cherryLeafMat;
                        break;
                }

                // Apply materials across all mesh renderers
                foreach (var mr in isolatedTree.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string rName = mr.gameObject.name.ToLower();
                    if (rName.Contains("leaf") || rName.Contains("leaves") || rName.Contains("flower") || rName.Contains("needle"))
                    {
                        mr.sharedMaterial = lMat;
                    }
                    else
                    {
                        mr.sharedMaterial = bMat;
                    }
                }

                // Normalize scale so height is realistic (between 7m and 14m)
                var renderers = isolatedTree.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int r = 1; r < renderers.Length; r++) b.Encapsulate(renderers[r].bounds);
                    
                    float curHeight = b.size.y;
                    if (curHeight > 0.1f)
                    {
                        float targetHeight = (matchedTree.Contains("Big") || matchedTree.Contains("Willow") || matchedTree.Contains("Green")) ? 11.5f : 8.5f;
                        if (matchedTree == "Small_Pine") targetHeight = 5.5f;
                        float scaleFactor = targetHeight / curHeight;
                        child.localScale *= scaleFactor;
                        
                        // Shift so root base touches Y = 0
                        float minY = b.min.y * scaleFactor;
                        child.localPosition = new Vector3(-b.center.x * scaleFactor, -minY, -b.center.z * scaleFactor);
                    }
                }

                // Save prefab
                string prefabPath = $"{OutPrefabDir}/{matchedTree}.prefab";
                PrefabUtility.SaveAsPrefabAsset(isolatedTree, prefabPath);
                Debug.Log($"[RealisticTreeExtractor] Saved clean realistic tree prefab: {prefabPath}");
                extractedCount++;

                Object.DestroyImmediate(isolatedTree);
                i--; // adjusted for child removal
            }

            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RealisticTreeExtractor] Successfully extracted {extractedCount} realistic tree prefabs!");
        }

        private static Material GetOrCreateBarkMat(string matName, Texture2D tex, Shader shader, float smoothness, Color? tint = null)
        {
            string path = $"{OutMatDir}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = matName;
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", tint ?? Color.white);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.05f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateLeafMat(string matName, Texture2D tex, Shader shader, Color foliageTint)
        {
            string path = $"{OutMatDir}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = matName;
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", foliageTint);
            
            // Double-sided Cutout Leaf Shader properties
            mat.SetFloat("_Cutoff", 0.45f);
            mat.SetFloat("_AlphaClip", 1.0f);
            mat.SetFloat("_Cull", 0.0f); // Two-sided / No backface culling for realistic leaf canopy
            mat.SetFloat("_Smoothness", 0.35f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
