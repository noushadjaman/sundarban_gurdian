using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SundarbanGuardian.Editor
{
    public static class SetupNewVisualAssets
    {
        private const string PrimaryDir = "Assets/Textures/UI";
        private const string ResourcesDir = "Assets/Resources/Textures/UI";

        [MenuItem("Sundarban/Setup New Visual Assets and Textures")]
        public static void RunSetup()
        {
            if (!Directory.Exists(PrimaryDir)) Directory.CreateDirectory(PrimaryDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);

            GenerateJumpIcon();
            GenerateTriangleMarkerIcon();
            SyncResourcesUITextures();
            UpdateTerrainAndWaterMaterials();

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[SetupNewVisualAssets] Completed setting up UI textures, materials, and sprites!</color>");
        }

        public static void GenerateJumpIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 0f;

                    // 1. Upward arched chevron / leap jump arrow: Apex at (cx, 195)
                    // Left arm from (cx - 70, 115) to (cx, 195)
                    // Right arm from (cx + 70, 115) to (cx, 195)
                    float dLeftArm = DistanceToSegment(new Vector2(x, y), new Vector2(cx - 65, 110), new Vector2(cx, 195)) - 14f;
                    float dRightArm = DistanceToSegment(new Vector2(x, y), new Vector2(cx + 65, 110), new Vector2(cx, 195)) - 14f;
                    float dChevron = Mathf.Min(dLeftArm, dRightArm);
                    if (dChevron <= 0f) alpha = Mathf.Max(alpha, 1f);
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dChevron));

                    // 2. Second lower echo chevron: Apex at (cx, 150)
                    float dLeftEcho = DistanceToSegment(new Vector2(x, y), new Vector2(cx - 52, 85), new Vector2(cx, 150)) - 11f;
                    float dRightEcho = DistanceToSegment(new Vector2(x, y), new Vector2(cx + 52, 85), new Vector2(cx, 150)) - 11f;
                    float dEcho = Mathf.Min(dLeftEcho, dRightEcho);
                    if (dEcho <= 0f) alpha = Mathf.Max(alpha, 0.85f);
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dEcho) * 0.85f);

                    // 3. Ground launch ripple bar at base: (cx - 40, 55) to (cx + 40, 55)
                    float dBase = DistanceToSegment(new Vector2(x, y), new Vector2(cx - 38, 55), new Vector2(cx + 38, 55)) - 8f;
                    if (dBase <= 0f) alpha = Mathf.Max(alpha, 0.75f);
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dBase) * 0.75f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            SaveAndImportSprite(tex, "icon_jump.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        public static void GenerateTriangleMarkerIcon()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            float cx = size * 0.5f;
            Vector2 pTop = new Vector2(cx, 115f);       // Apex pointing UP
            Vector2 pLeft = new Vector2(25f, 22f);      // Bottom-Left
            Vector2 pRight = new Vector2(size - 25f, 22f); // Bottom-Right
            Vector2 pIndent = new Vector2(cx, 44f);     // Stealth arrowhead inner notch

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x, y);

                    // Stealth arrowhead polygon: Left wing (pTop, pLeft, pIndent) & Right wing (pTop, pIndent, pRight)
                    bool inLeftWing = PointInTriangle(pt, pTop, pLeft, pIndent);
                    bool inRightWing = PointInTriangle(pt, pTop, pIndent, pRight);

                    if (inLeftWing || inRightWing)
                    {
                        // Gradient from apex (pure white/cyan) to base (emerald green)
                        float tY = Mathf.Clamp01((y - 22f) / (115f - 22f));
                        Color col = Color.Lerp(new Color(0.12f, 0.90f, 0.45f, 1f), new Color(0.95f, 1.0f, 0.98f, 1f), tY);

                        // Highlight along center ridge
                        float distToCenter = Mathf.Abs(x - cx);
                        if (distToCenter < 2.5f) col = Color.white;

                        pixels[y * size + x] = col;
                    }
                    else
                    {
                        // Check outline border
                        float d1 = DistanceToSegment(pt, pTop, pLeft);
                        float d2 = DistanceToSegment(pt, pTop, pRight);
                        float d3 = DistanceToSegment(pt, pLeft, pIndent);
                        float d4 = DistanceToSegment(pt, pIndent, pRight);
                        float minEdgeDist = Mathf.Min(Mathf.Min(d1, d2), Mathf.Min(d3, d4));

                        if (minEdgeDist <= 3.5f)
                        {
                            float outlineAlpha = Mathf.Clamp01(3.5f - minEdgeDist);
                            pixels[y * size + x] = new Color(0.02f, 0.15f, 0.08f, 0.95f * outlineAlpha);
                        }
                        else
                        {
                            pixels[y * size + x] = new Color(0, 0, 0, 0);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            SaveAndImportSprite(tex, "icon_triangle_marker.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void SaveAndImportSprite(Texture2D tex, string fileName)
        {
            byte[] png = tex.EncodeToPNG();
            string p1 = Path.Combine(PrimaryDir, fileName);
            string p2 = Path.Combine(ResourcesDir, fileName);

            File.WriteAllBytes(p1, png);
            File.WriteAllBytes(p2, png);

            ConfigureSprite(p1);
            ConfigureSprite(p2);
        }

        private static void ConfigureSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        public static void SyncResourcesUITextures()
        {
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);
            var srcFiles = Directory.GetFiles(PrimaryDir, "*.png");
            foreach (var f in srcFiles)
            {
                string fn = Path.GetFileName(f);
                string dst = Path.Combine(ResourcesDir, fn);
                if (!File.Exists(dst) || File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(dst))
                {
                    File.Copy(f, dst, true);
                }
                ConfigureSprite(dst);
            }
        }

        public static void UpdateTerrainAndWaterMaterials()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            var richMudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sundarban_Ground_Rich.jpg");
            var mudNormTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MangroveMud_NormalMap.png");

            // 1. Mangrove_Intertidal_Mud.mat
            var intertidalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Intertidal_Mud.mat");
            if (intertidalMat != null)
            {
                intertidalMat.shader = urpLit;
                if (richMudTex != null)
                {
                    intertidalMat.SetTexture("_BaseMap", richMudTex);
                    intertidalMat.SetTextureScale("_BaseMap", new Vector2(6f, 10f));
                }
                if (mudNormTex != null)
                {
                    intertidalMat.SetTexture("_BumpMap", mudNormTex);
                    intertidalMat.SetFloat("_BumpScale", 1.6f);
                    intertidalMat.SetTextureScale("_BumpMap", new Vector2(6f, 10f));
                    intertidalMat.EnableKeyword("_NORMALMAP");
                }
                intertidalMat.SetColor("_BaseColor", new Color(0.22f, 0.18f, 0.13f, 1.0f));
                intertidalMat.SetFloat("_Smoothness", 0.65f);
                intertidalMat.SetFloat("_Metallic", 0.0f);
                EditorUtility.SetDirty(intertidalMat);
            }

            // 2. Sundarban_BeachSand.mat
            var sandMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_BeachSand.mat");
            if (sandMat != null)
            {
                sandMat.shader = urpLit;
                if (richMudTex != null)
                {
                    sandMat.SetTexture("_BaseMap", richMudTex);
                    sandMat.SetTextureScale("_BaseMap", new Vector2(6f, 10f));
                }
                sandMat.SetColor("_BaseColor", new Color(0.22f, 0.18f, 0.13f, 1.0f));
                sandMat.SetFloat("_Smoothness", 0.65f);
                EditorUtility.SetDirty(sandMat);
            }

            // 3. Sundarban_RiverWater.mat (URP Transparent)
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            if (waterMat != null)
            {
                waterMat.shader = urpLit;
                waterMat.SetFloat("_Surface", 1f);   // 1 = Transparent
                waterMat.SetFloat("_Blend", 0f);     // 0 = Alpha blend
                waterMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetFloat("_ZWrite", 0f);
                waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                waterMat.SetOverrideTag("RenderType", "Transparent");

                // Transparent mangrove brackish water (clear enough to see tiger legs and riverbed)
                waterMat.SetColor("_BaseColor", new Color(0.18f, 0.28f, 0.27f, 0.38f));
                waterMat.SetFloat("_Smoothness", 0.92f);
                waterMat.SetFloat("_Metallic", 0.05f);
                EditorUtility.SetDirty(waterMat);
            }

            AssetDatabase.SaveAssets();
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + ab * t);
        }

        private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1 = Sign(pt, v1, v2);
            float d2 = Sign(pt, v2, v3);
            float d3 = Sign(pt, v3, v1);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
