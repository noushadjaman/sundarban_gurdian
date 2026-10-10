using System.IO;
using UnityEngine;
using UnityEditor;

namespace SundarbanGuardian.Editor
{
    public static class BuildRiverFishingNets
    {
        private const string TextureDir = "Assets/Textures/Environment";
        private const string MaterialDir = "Assets/Materials";

        [MenuItem("Sundarban/Build River Fishing Net Assets & Textures")]
        public static void GenerateNetAssets()
        {
            EnsureDirectories();

            // 1. Generate Seamless Green Diamond Net Texture (Matching Image 4)
            string netTexPath = Path.Combine(TextureDir, "FishingNet_Green_Diamond.png");
            GenerateGreenDiamondNetTexture(netTexPath);

            // 2. Generate Accordion Box Net Ribs Texture (Matching Image 2, 3, 5)
            string accordionTexPath = Path.Combine(TextureDir, "AccordionNet_Ribs.png");
            GenerateAccordionNetTexture(accordionTexPath);

            // 3. Generate Bamboo Pole Texture
            string bambooTexPath = Path.Combine(TextureDir, "Bamboo_Pole.png");
            GenerateBambooTexture(bambooTexPath);

            AssetDatabase.Refresh();

            // 4. Create Materials
            CreateNetMaterials(netTexPath, accordionTexPath, bambooTexPath);

            Debug.Log("<color=green>[BuildRiverFishingNets] All Net Textures & Materials Generated Successfully!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(TextureDir)) Directory.CreateDirectory(TextureDir);
            if (!Directory.Exists(MaterialDir)) Directory.CreateDirectory(MaterialDir);
        }

        private static void GenerateGreenDiamondNetTexture(string fullPath)
        {
            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color transparent = new Color(0.08f, 0.42f, 0.32f, 0.04f); // slight underwater mesh haze
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

            int cells = 16; // 16 diamonds across
            float step = (float)size / cells;
            float lineWidth = 4.2f;

            Color meshGreen = new Color(0.12f, 0.65f, 0.48f, 0.92f); // Authentic nylon teal/emerald green
            Color meshKnot = new Color(0.18f, 0.88f, 0.62f, 1.0f);  // Bright tied knot at diamond intersection

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Diamond pattern: (x + y) % step and (x - y + size*10) % step
                    float d1 = Mathf.Abs(((x + y) % step) - step * 0.5f);
                    float d2 = Mathf.Abs(((x - y + size * 10) % step) - step * 0.5f);

                    float distToLine = Mathf.Min(d1, d2);

                    if (distToLine < lineWidth * 0.5f)
                    {
                        float alpha = 1f - (distToLine / (lineWidth * 0.5f));
                        // Check if near an intersection knot
                        bool isKnot = (d1 < lineWidth * 1.1f && d2 < lineWidth * 1.1f);
                        Color c = isKnot ? meshKnot : meshGreen;
                        c.a = Mathf.Clamp01(alpha * c.a);

                        pixels[y * size + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateAccordionNetTexture(string fullPath)
        {
            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            Color frameWire = new Color(0.72f, 0.68f, 0.25f, 1.0f); // Yellowish bamboo/wire frame hoop
            Color fineNetGreen = new Color(0.10f, 0.52f, 0.40f, 0.85f);
            Color openSpace = new Color(0.05f, 0.28f, 0.22f, 0.08f);

            int gridSteps = 32;
            float step = (float)size / gridSteps;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Vertical hoop wire bands every 128 px
                    bool isHoop = (x % 128 < 7) || (x % 128 > 121);
                    // Horizontal frame edge bars at top and bottom
                    bool isBorder = (y < 8) || (y > size - 9);

                    if (isHoop || isBorder)
                    {
                        pixels[y * size + x] = frameWire;
                        continue;
                    }

                    // Fine mesh inside
                    float gx = Mathf.Abs((x % step) - step * 0.5f);
                    float gy = Mathf.Abs((y % step) - step * 0.5f);
                    float dist = Mathf.Min(gx, gy);

                    if (dist < 1.8f)
                    {
                        pixels[y * size + x] = fineNetGreen;
                    }
                    else
                    {
                        pixels[y * size + x] = openSpace;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateBambooTexture(string fullPath)
        {
            int width = 128;
            int height = 512;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, true);
            Color[] pixels = new Color[width * height];

            Color bambooBase = new Color(0.68f, 0.58f, 0.32f); // Golden bamboo stalk
            Color bambooNode = new Color(0.38f, 0.28f, 0.16f); // Dark node ring
            Color bambooLight = new Color(0.78f, 0.69f, 0.42f);

            for (int y = 0; y < height; y++)
            {
                bool isNode = (y % 110 < 8);
                float striation = Mathf.Sin(y * 0.3f) * 0.05f;

                for (int x = 0; x < width; x++)
                {
                    float edgeShadow = Mathf.Sin((float)x / width * Mathf.PI);
                    Color c = isNode ? bambooNode : Color.Lerp(bambooBase, bambooLight, (float)x / width + striation);
                    c *= (0.6f + 0.4f * edgeShadow);
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
        }

        private static void CreateNetMaterials(string netTexPath, string accordionTexPath, string bambooTexPath)
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Monofilament Gillnet Curtain Material (Double-Sided Transparent)
            var curtainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FishingNet_GreenCurtain.mat");
            if (curtainMat == null)
            {
                curtainMat = new Material(urpLit);
                AssetDatabase.CreateAsset(curtainMat, "Assets/Materials/FishingNet_GreenCurtain.mat");
            }
            curtainMat.shader = urpLit;
            var netTex = AssetDatabase.LoadAssetAtPath<Texture2D>(netTexPath);
            if (netTex != null) curtainMat.SetTexture("_BaseMap", netTex);
            curtainMat.SetColor("_BaseColor", new Color(0.20f, 0.90f, 0.65f, 0.88f));
            curtainMat.SetFloat("_Surface", 1f); // Transparent
            curtainMat.SetFloat("_Cull", 0f);    // Two-sided
            curtainMat.SetFloat("_Smoothness", 0.75f);
            curtainMat.renderQueue = 3000;
            curtainMat.enableInstancing = true;
            EditorUtility.SetDirty(curtainMat);

            // 2. Accordion Box Net Material
            var accordionMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AccordionNet_Mesh.mat");
            if (accordionMat == null)
            {
                accordionMat = new Material(urpLit);
                AssetDatabase.CreateAsset(accordionMat, "Assets/Materials/AccordionNet_Mesh.mat");
            }
            accordionMat.shader = urpLit;
            var accTex = AssetDatabase.LoadAssetAtPath<Texture2D>(accordionTexPath);
            if (accTex != null) accordionMat.SetTexture("_BaseMap", accTex);
            accordionMat.SetColor("_BaseColor", new Color(0.15f, 0.80f, 0.55f, 0.90f));
            accordionMat.SetFloat("_Surface", 1f);
            accordionMat.SetFloat("_Cull", 0f);
            accordionMat.renderQueue = 3000;
            accordionMat.enableInstancing = true;
            EditorUtility.SetDirty(accordionMat);

            // 3. Bamboo Pole Material
            var bambooMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Bamboo_Pole.mat");
            if (bambooMat == null)
            {
                bambooMat = new Material(urpLit);
                AssetDatabase.CreateAsset(bambooMat, "Assets/Materials/Bamboo_Pole.mat");
            }
            bambooMat.shader = urpLit;
            var bamTex = AssetDatabase.LoadAssetAtPath<Texture2D>(bambooTexPath);
            if (bamTex != null) bambooMat.SetTexture("_BaseMap", bamTex);
            bambooMat.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(bambooMat);

            // 4. Net Cork Buoy Material (Vibrant Marine Orange)
            var corkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Net_Cork_Buoy.mat");
            if (corkMat == null)
            {
                corkMat = new Material(urpLit);
                AssetDatabase.CreateAsset(corkMat, "Assets/Materials/Net_Cork_Buoy.mat");
            }
            corkMat.shader = urpLit;
            corkMat.SetColor("_BaseColor", new Color(0.96f, 0.38f, 0.12f, 1.0f));
            corkMat.SetFloat("_Smoothness", 0.5f);
            EditorUtility.SetDirty(corkMat);

            AssetDatabase.SaveAssets();
        }
    }
}
