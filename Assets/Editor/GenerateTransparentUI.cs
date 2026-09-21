using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SundarbanGuardian.Editor
{
    public static class GenerateTransparentUI
    {
        private const string TextureDir = "Assets/Textures/UI";

        [MenuItem("Sundarban/Generate Transparent UI Sprites")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(TextureDir))
            {
                Directory.CreateDirectory(TextureDir);
            }

            GenerateGlassButtonDisc();
            GenerateJoystickBaseDial();
            GenerateJoystickKnobPuck();
            ConvertRunnerIcon();
            ConvertFireBulletIcon();

            GenerateSeedIcon();
            GenerateKnifeIcon();
            GenerateCleanFlaskIcon();
            GenerateTrapIcon();

            AssetDatabase.Refresh();
            Debug.Log("[GenerateTransparentUI] All transparent Free Fire UI sprites generated successfully!");
        }

        // 6. Tool 0: Seed / Sprout Icon (🌱)
        private static void GenerateSeedIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 0f;

                    // Seed pellet at base (128, 65)
                    float dSeed = DistanceToEllipse(x, y, 128f, 65f, 22f, 16f, 0f);
                    if (dSeed <= 0f) alpha = 1f;
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dSeed));

                    // Stem: quadratic curve from (128, 70) through (124, 120) to (128, 175)
                    float dStem = DistanceToQuadBezier(x, y, new Vector2(128, 70), new Vector2(122, 125), new Vector2(128, 175)) - 4.5f;
                    if (dStem <= 0f) alpha = 1f;
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dStem));

                    // Left leaf: oval tilted left at (92, 148), angle -35 deg
                    float dLeftLeaf = DistanceToEllipse(x, y, 92f, 148f, 34f, 17f, -38f);
                    if (dLeftLeaf <= 0f) alpha = 1f;
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dLeftLeaf));

                    // Right leaf: oval tilted right at (164, 154), angle 35 deg
                    float dRightLeaf = DistanceToEllipse(x, y, 164f, 154f, 36f, 18f, 38f);
                    if (dRightLeaf <= 0f) alpha = 1f;
                    else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dRightLeaf));

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "icon_tool_seed.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        // 7. Tool 1: Knife / Net-Cutter Blade Icon (🔪)
        private static void GenerateKnifeIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Rotate 45 deg around center
                    float dx = x - 128f;
                    float dy = y - 128f;
                    float rx = (dx + dy) * 0.7071f;
                    float ry = (-dx + dy) * 0.7071f;

                    float alpha = 0f;

                    // Handle: from rx = -75 to rx = -15, width = 16 (ry from -8 to 8)
                    if (rx >= -75f && rx <= -15f)
                    {
                        float dH = Mathf.Abs(ry) - 7.5f;
                        if (dH <= 0f) alpha = 1f;
                        else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dH));
                    }

                    // Guard: rx between -16 and -10, width = 28
                    if (rx >= -16f && rx <= -10f)
                    {
                        float dG = Mathf.Abs(ry) - 13.5f;
                        if (dG <= 0f) alpha = 1f;
                        else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dG));
                    }

                    // Blade: rx from -10 to 75
                    if (rx >= -10f && rx <= 75f)
                    {
                        // Straight spine at ry = 8
                        // Curved cutting edge tapering to tip at (75, 5)
                        float norm = (rx - (-10f)) / 85f;
                        float spineY = 8f;
                        float edgeY = -8f + (norm * norm) * 13f;

                        if (ry >= edgeY && ry <= spineY)
                        {
                            alpha = 1f;
                        }
                        else
                        {
                            float dEdge = (ry < edgeY) ? (edgeY - ry) : (ry - spineY);
                            alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dEdge));
                        }
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "icon_tool_knife.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        // 8. Tool 2: Cleaning Liquid Chemical Flask Icon (🧪)
        private static void GenerateCleanFlaskIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - 128f);
                    float alpha = 0f;

                    // Lip rim at top (y: 200 to 208, w = 36)
                    if (y >= 200 && y <= 208)
                    {
                        float d = dx - 18f;
                        if (d <= 0f) alpha = 1f;
                        else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - d));
                    }
                    // Neck (y: 160 to 200, w = 22)
                    else if (y >= 160 && y < 200)
                    {
                        float d = dx - 11f;
                        if (d <= 0f) alpha = 1f;
                        else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - d));
                    }
                    // Conical Body (y: 60 to 160, widening from 22 to 104)
                    else if (y >= 60 && y < 160)
                    {
                        float bodyT = (160f - y) / 100f;
                        float halfW = Mathf.Lerp(11f, 52f, bodyT);
                        float d = dx - halfW;
                        if (d <= 0f) alpha = 1f;
                        else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - d));
                    }
                    // Bottom rounded base (y: 52 to 60)
                    else if (y >= 52 && y < 60)
                    {
                        float d = dx - 48f;
                        if (d <= 0f) alpha = 1f;
                        else alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - d));
                    }

                    // Cut out inner hollow / bubbles in upper empty chamber
                    if (y >= 135 && y < 198 && alpha > 0.05f)
                    {
                        float innerHalfW = (y >= 160) ? 7f : Mathf.Lerp(7f, 38f, (160f - y) / 100f);
                        if (dx < innerHalfW)
                        {
                            // Glass outline style in upper dry part
                            alpha = Mathf.Clamp01(Mathf.Abs(dx - innerHalfW) / 2.5f);
                        }
                    }

                    // Liquid bubbles
                    float dBub1 = Mathf.Sqrt((x - 118f) * (x - 118f) + (y - 92f) * (y - 92f)) - 4.5f;
                    float dBub2 = Mathf.Sqrt((x - 138f) * (x - 138f) + (y - 114f) * (y - 114f)) - 5.5f;
                    if (dBub1 <= 0f || dBub2 <= 0f) alpha = 0.15f;

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "icon_tool_clean.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        // 9. Tool 3: Trap Destroyer Snare Breaker Icon (🪤)
        private static void GenerateTrapIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 128f;
                    float dy = y - 110f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;

                    // Base bar at y = 60 to 72, w = 120
                    if (y >= 60 && y <= 72 && Mathf.Abs(dx) <= 60f)
                    {
                        alpha = 1f;
                    }

                    // Arched serrated jaws (outer radius 62, inner radius 48, upper semicircle)
                    if (dy >= -10f && dist <= 64f && dist >= 44f)
                    {
                        // Teeth modulation
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        float tooth = Mathf.Sin(angle * 0.18f) * 6f;
                        if (dist <= 62f && dist >= 48f - tooth)
                        {
                            alpha = 1f;
                        }
                    }

                    // Central pressure plate (circle at center r = 20)
                    if (dist <= 20f && dy >= -8f)
                    {
                        alpha = 1f;
                    }

                    // Pivot spring cylinder at (128, 66)
                    float dPivot = Mathf.Sqrt(dx * dx + (y - 66f) * (y - 66f)) - 10f;
                    if (dPivot <= 0f) alpha = 1f;

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "icon_tool_trap.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        private static float DistanceToEllipse(float x, float y, float cx, float cy, float a, float b, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float tx = x - cx;
            float ty = y - cy;
            float rx = tx * cos + ty * sin;
            float ry = -tx * sin + ty * cos;
            float norm = (rx * rx) / (a * a) + (ry * ry) / (b * b);
            return (Mathf.Sqrt(norm) - 1.0f) * Mathf.Min(a, b);
        }

        private static float DistanceToQuadBezier(float px, float py, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float minDist = float.MaxValue;
            Vector2 p = new Vector2(px, py);
            int steps = 16;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float u = 1f - t;
                Vector2 pt = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                float d = Vector2.Distance(p, pt);
                if (d < minDist) minDist = d;
            }
            return minDist;
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
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

        // 1. Circular Dark Translucent Glass Button with crisp rim
        private static void GenerateGlassButtonDisc()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float outerR = size * 0.465f;
            float rimThickness = 4.0f;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > outerR + 1.5f)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else
                    {
                        // Anti-aliased outer edge
                        float edgeAlpha = Mathf.Clamp01(outerR + 1.5f - dist);

                        // Rim region
                        if (dist >= outerR - rimThickness)
                        {
                            float rimT = Mathf.InverseLerp(outerR - rimThickness, outerR, dist);
                            // Bright silver/white rim highlight with subtle gradient
                            float rimVal = Mathf.Lerp(0.95f, 0.70f, rimT);
                            float rimA = Mathf.Lerp(0.85f, 0.45f, rimT) * edgeAlpha;
                            pixels[y * size + x] = new Color(rimVal, rimVal, 0.98f, rimA);
                        }
                        else if (dist >= outerR - rimThickness - 3.5f)
                        {
                            // Inner subtle bevel highlight
                            float innerT = Mathf.InverseLerp(outerR - rimThickness - 3.5f, outerR - rimThickness, dist);
                            float a = Mathf.Lerp(0.42f, 0.75f, innerT);
                            pixels[y * size + x] = new Color(0.35f, 0.42f, 0.50f, a);
                        }
                        else
                        {
                            // Translucent dark body - transparent glass design!
                            // Matches Free Fire reference: ground is clearly visible behind!
                            float normDist = dist / (outerR - rimThickness);
                            // Very subtle radial vignette
                            float alpha = Mathf.Lerp(0.34f, 0.44f, normDist);
                            // Soft tint: dark slate smoke
                            float darkTone = Mathf.Lerp(0.08f, 0.12f, normDist);
                            pixels[y * size + x] = new Color(darkTone, darkTone + 0.02f, darkTone + 0.04f, alpha);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "btn_circle_glass.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        // 2. Free Fire Virtual Joystick Base Dial
        private static void GenerateJoystickBaseDial()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f;
            float cy = size * 0.5f;

            float rOuter = 240f;
            float rDotted = 226f;
            float rInnerHole = 92f;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    if (dist > rOuter + 1.5f)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    float edgeAlpha = Mathf.Clamp01(rOuter + 1.5f - dist);

                    // Outer border ring (dist between rOuter - 3 and rOuter)
                    if (dist >= rOuter - 3.5f)
                    {
                        float rimA = Mathf.Clamp01((rOuter - dist) / 1.5f) * Mathf.Clamp01((dist - (rOuter - 3.5f)) / 1.5f);
                        pixels[y * size + x] = new Color(0.80f, 0.85f, 0.90f, 0.65f * rimA * edgeAlpha);
                        continue;
                    }

                    // Dotted track ring at rDotted
                    float dotDist = Mathf.Abs(dist - rDotted);
                    bool isDot = false;
                    if (dotDist <= 2.2f)
                    {
                        // Spaced every 5 degrees
                        float angleMod = angle % 5.0f;
                        if (angleMod < 1.4f || angleMod > 3.6f)
                        {
                            isDot = true;
                        }
                    }

                    if (isDot && dotDist <= 2.0f)
                    {
                        float dotA = Mathf.Clamp01(2.0f - dotDist);
                        pixels[y * size + x] = new Color(0.75f, 0.82f, 0.90f, 0.45f * dotA);
                        continue;
                    }

                    // 4 Directional Chevron Arrowheads (Up, Down, Left, Right)
                    bool inChevron = false;
                    float chevAlpha = 0f;

                    // Cardinal coordinates
                    // Top (Up): dx near 0, dy around 150
                    CheckChevron(dx, dy - 150f, 0f, ref inChevron, ref chevAlpha);
                    // Bottom (Down): dx near 0, dy around -150
                    CheckChevron(dx, dy + 150f, 180f, ref inChevron, ref chevAlpha);
                    // Right: dx around 150, dy near 0 (pointing right)
                    CheckChevron(dx - 150f, dy, 90f, ref inChevron, ref chevAlpha);
                    // Left: dx around -150, dy near 0 (pointing left)
                    CheckChevron(dx + 150f, dy, -90f, ref inChevron, ref chevAlpha);

                    if (inChevron)
                    {
                        pixels[y * size + x] = new Color(0.92f, 0.95f, 1.0f, chevAlpha * 0.90f);
                        continue;
                    }

                    // Inner central hole
                    if (dist < rInnerHole)
                    {
                        // Hollow dark core with soft inner border
                        if (dist >= rInnerHole - 3f)
                        {
                            float holeRimA = Mathf.InverseLerp(rInnerHole - 3f, rInnerHole, dist);
                            pixels[y * size + x] = new Color(0.55f, 0.65f, 0.75f, Mathf.Lerp(0.35f, 0.60f, holeRimA));
                        }
                        else
                        {
                            // Central cutout - slightly darker translucent hole
                            pixels[y * size + x] = new Color(0.04f, 0.05f, 0.07f, 0.52f);
                        }
                        continue;
                    }

                    // Track body between inner hole and outer rim
                    float trackNorm = Mathf.InverseLerp(rInnerHole, rOuter, dist);
                    float bodyA = Mathf.Lerp(0.32f, 0.40f, trackNorm);
                    float bodyTone = Mathf.Lerp(0.07f, 0.11f, trackNorm);
                    pixels[y * size + x] = new Color(bodyTone, bodyTone + 0.02f, bodyTone + 0.04f, bodyA);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "joystick_base.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        private static void CheckChevron(float lx, float ly, float rotDeg, ref bool inChev, ref float alpha)
        {
            // Rotate local coords
            float rad = rotDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float rx = lx * cos - ly * sin;
            float ry = lx * sin + ly * cos;

            // Chevron shape pointing up (ry positive):
            // Two angled arms forming an inverted V
            // Tip at (0, 14), arms go down to (-16, -6) and (16, -6)
            float w = 18f;
            float thickness = 4.2f;

            if (Mathf.Abs(rx) <= w && ry >= -10f && ry <= 18f)
            {
                float armSlope = 1.0f;
                float expectedY = 12f - Mathf.Abs(rx) * armSlope;
                float dY = Mathf.Abs(ry - expectedY);

                if (dY <= thickness * 0.5f + 1.2f)
                {
                    inChev = true;
                    float a = Mathf.Clamp01((thickness * 0.5f + 1.2f - dY) / 1.2f);
                    float tipBoost = Mathf.Lerp(0.7f, 1.0f, Mathf.InverseLerp(-10f, 18f, ry));
                    alpha = Mathf.Max(alpha, a * tipBoost);
                }
            }
        }

        // 3. Joystick Knob Puck (Metallic/Pearl smooth white puck)
        private static void GenerateJoystickKnobPuck()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float radius = size * 0.44f;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius + 1.5f)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    float edgeAlpha = Mathf.Clamp01(radius + 1.5f - dist);

                    // Outer bevel rim
                    if (dist >= radius - 3.5f)
                    {
                        float rimT = Mathf.InverseLerp(radius - 3.5f, radius, dist);
                        float rA = Mathf.Lerp(0.85f, 0.45f, rimT) * edgeAlpha;
                        pixels[y * size + x] = new Color(0.65f, 0.70f, 0.76f, rA);
                        continue;
                    }

                    // Soft specular sheen centered at upper-left (-18, 18)
                    float specDx = x - (cx - 24f);
                    float specDy = y - (cy + 24f);
                    float specDist = Mathf.Sqrt(specDx * specDx + specDy * specDy);
                    float spec = Mathf.Clamp01(1f - (specDist / (radius * 0.75f)));
                    spec = spec * spec; // smooth quadratic falloff

                    // Base gradient: smooth white/silver puck
                    float normDist = dist / radius;
                    float baseTone = Mathf.Lerp(0.96f, 0.82f, normDist);
                    float tone = Mathf.Clamp01(baseTone + spec * 0.12f);

                    pixels[y * size + x] = new Color(tone, tone, tone * 1.02f, 0.94f * edgeAlpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            string path = Path.Combine(TextureDir, "joystick_knob.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            ConfigureSpriteImporter(path);
        }

        // 4. Convert Runner Silhouette JPG into transparent PNG sprite
        private static void ConvertRunnerIcon()
        {
            string srcPath = @"C:\Users\RAJ\.gemini\antigravity-ide\brain\42cfaa4d-d654-4f0d-b526-c569ebf43515\icon_running_silhouette_1789844864223.jpg";
            string dstPath = Path.Combine(TextureDir, "icon_autorun.png");

            if (!File.Exists(srcPath))
            {
                Debug.LogWarning($"[GenerateTransparentUI] Source file not found: {srcPath}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(srcPath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(bytes);

            int w = srcTex.width;
            int h = srcTex.height;
            Texture2D dstTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color[] srcPixels = srcTex.GetPixels();
            Color[] dstPixels = new Color[srcPixels.Length];

            float threshold = 0.38f;
            float feather = 0.20f;

            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color c = srcPixels[i];
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

                if (lum <= threshold)
                {
                    dstPixels[i] = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    float alpha = Mathf.Clamp01((lum - threshold) / feather);
                    dstPixels[i] = new Color(1f, 1f, 1f, alpha);
                }
            }

            dstTex.SetPixels(dstPixels);
            dstTex.Apply();
            File.WriteAllBytes(dstPath, dstTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(srcTex);
            UnityEngine.Object.DestroyImmediate(dstTex);
            ConfigureSpriteImporter(dstPath);
        }

        // 5. Convert Bullet / Fire Action JPG into transparent PNG sprite
        private static void ConvertFireBulletIcon()
        {
            string srcPath = @"C:\Users\RAJ\.gemini\antigravity-ide\brain\42cfaa4d-d654-4f0d-b526-c569ebf43515\icon_bullet_sparks_1789845014571.jpg";
            string dstPath = Path.Combine(TextureDir, "icon_fire.png");

            if (!File.Exists(srcPath))
            {
                Debug.LogWarning($"[GenerateTransparentUI] Source file not found: {srcPath}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(srcPath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(bytes);

            int w = srcTex.width;
            int h = srcTex.height;
            Texture2D dstTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color[] srcPixels = srcTex.GetPixels();
            Color[] dstPixels = new Color[srcPixels.Length];

            float threshold = 0.15f;
            float feather = 0.20f;

            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color c = srcPixels[i];
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

                if (lum <= threshold)
                {
                    dstPixels[i] = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    float alpha = Mathf.Clamp01((lum - threshold) / feather);
                    dstPixels[i] = new Color(1f, 1f, 1f, alpha);
                }
            }

            dstTex.SetPixels(dstPixels);
            dstTex.Apply();
            File.WriteAllBytes(dstPath, dstTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(srcTex);
            UnityEngine.Object.DestroyImmediate(dstTex);
            ConfigureSpriteImporter(dstPath);
        }
    }
}
