using System.IO;
using UnityEngine;
using UnityEditor;
using SundarbanGuardian.Core;
using SundarbanGuardian.Editor;
using SundarbanGuardian.Inventory;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.EditorTools
{
    public static class ChitraDeerVisualAndMovementBuilder
    {
        [MenuItem("Sundarban/Prepare Chitra Deer (Visuals & Realistic Locomotion)")]
        public static void PrepareChitraDeer()
        {
            Debug.Log("<color=cyan>[ChitraDeer] Beginning comprehensive preparation of Spotted Deer (Axis axis)...</color>");

            // 1. Generate & Apply Authentic Chitra Deer 2K Texture
            GenerateAuthenticChitraTexture();

            // 2. Configure Authentic_Deer Material
            ConfigureDeerMaterial();

            // 3. Build & Configure Deer Rig & ChitraDeerProceduralAnimator
            ConfigureDeerGameObject();

            Debug.Log("<color=green>[ChitraDeer] Preparation COMPLETE! Chitra Deer now has realistic spotted body, dark spine stripe, white throat bib, white underbelly, and biological ungulate high-step trot locomotion!</color>");
        }

        public static void GenerateAuthenticChitraTexture()
        {
            string outPath = "Assets/Textures/Tex_Chitra_Deer_2K.png";
            int size = 2048;

            Debug.Log("<color=cyan>[ChitraDeer] Baking 2048x2048 anatomical Chitra texture via 3D surface projection...</color>");

            // 1. Obtain Mesh geometry
            var deer = GameObject.Find("Deer");
            SkinnedMeshRenderer smr = deer != null ? deer.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            Mesh mesh = smr != null ? smr.sharedMesh : null;
            if (mesh == null)
            {
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Mesh_Deer.obj");
            }
            if (mesh == null)
            {
                Debug.LogError("[ChitraDeer] Mesh_Deer not found! Cannot bake 3D texture.");
                return;
            }

            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] tris = mesh.triangles;

            // 2. 3D Position buffer mapped to UV space
            Vector3[] posBuffer = new Vector3[size * size];
            bool[] hasData = new bool[size * size];

            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                Vector2 u0 = new Vector2(uvs[i0].x * (size - 1), uvs[i0].y * (size - 1));
                Vector2 u1 = new Vector2(uvs[i1].x * (size - 1), uvs[i1].y * (size - 1));
                Vector2 u2 = new Vector2(uvs[i2].x * (size - 1), uvs[i2].y * (size - 1));

                Vector3 v0 = verts[i0], v1 = verts[i1], v2 = verts[i2];

                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(u0.x, Mathf.Min(u1.x, u2.x))), 0, size - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(u0.x, Mathf.Max(u1.x, u2.x))), 0, size - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(u0.y, Mathf.Min(u1.y, u2.y))), 0, size - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(u0.y, Mathf.Max(u1.y, u2.y))), 0, size - 1);

                float denom = (u1.y - u2.y) * (u0.x - u2.x) + (u2.x - u1.x) * (u0.y - u2.y);
                if (Mathf.Abs(denom) < 0.00001f) continue;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float w0 = ((u1.y - u2.y) * (x - u2.x) + (u2.x - u1.x) * (y - u2.y)) / denom;
                        float w1 = ((u2.y - u0.y) * (x - u2.x) + (u0.x - u2.x) * (y - u2.y)) / denom;
                        float w2 = 1.0f - w0 - w1;

                        if (w0 >= -0.015f && w1 >= -0.015f && w2 >= -0.015f)
                        {
                            int idx = y * size + x;
                            posBuffer[idx] = w0 * v0 + w1 * v1 + w2 * v2;
                            hasData[idx] = true;
                        }
                    }
                }
            }

            // 3. Define 3D Spot Centers for Spotted Deer ( চিত্রা হরিণ / Axis axis )
            // Biological layout: 6 longitudinal parallel rows down each flank + haunch & shoulder clusters
            var spotCenters = new System.Collections.Generic.List<Vector3>();

            float[] rowHeights = new float[] { 17.5f, 13.8f, 9.8f, 5.8f, 1.8f, -2.0f };
            int[] spotsPerRow  = new int[]   { 8,     9,     10,   10,   8,    7 };
            float[] rowZMin    = new float[] { -30f,  -34f,  -36f, -36f, -33f, -28f };
            float[] rowZMax    = new float[] { 17f,   19f,   21f,  21f,  18f,  13f };

            for (int r = 0; r < rowHeights.Length; r++)
            {
                float ry = rowHeights[r];
                int count = spotsPerRow[r];
                float zMin = rowZMin[r];
                float zMax = rowZMax[r];

                for (int s = 0; s < count; s++)
                {
                    float frac = count > 1 ? (float)s / (count - 1) : 0.5f;
                    float sz = Mathf.Lerp(zMin, zMax, frac) + Mathf.Sin(r * 2.5f + s) * 0.9f;
                    float sy = ry + Mathf.Sin(s * 2.1f) * 0.7f;
                    // Flank lateral offset in 3D: ~14.5 units
                    spotCenters.Add(new Vector3(14.5f, sy, sz));
                }
            }

            // Haunch cluster (hindquarter thigh)
            System.Random prng = new System.Random(42);
            for (int i = 0; i < 18; i++)
            {
                float hx = 11.5f + (float)prng.NextDouble() * 5.0f;
                float hy = -2.5f + (float)prng.NextDouble() * 17.0f;
                float hz = -37.0f + (float)prng.NextDouble() * 15.0f;
                spotCenters.Add(new Vector3(hx, hy, hz));
            }

            // Shoulder cluster (withers)
            for (int i = 0; i < 10; i++)
            {
                float sx = 12.0f + (float)prng.NextDouble() * 5.5f;
                float sy = 1.0f + (float)prng.NextDouble() * 15.0f;
                float sz = 14.0f + (float)prng.NextDouble() * 10.0f;
                spotCenters.Add(new Vector3(sx, sy, sz));
            }

            // 4. Color Palette for Axis axis (Sundarban Chitra Horin)
            Color goldenAmberFur = new Color(0.85f, 0.53f, 0.22f, 1f);     // Deep warm golden rufous
            Color dorsalDarkStripe = new Color(0.12f, 0.08f, 0.05f, 1f);   // Rich dark espresso/black
            Color whiteSpotColor = new Color(0.99f, 0.99f, 0.98f, 1f);     // Crisp chalk-white spots
            Color spotRimDark = new Color(0.24f, 0.15f, 0.08f, 1f);        // Subtle depth rim around spots
            Color throatBibWhite = new Color(0.98f, 0.97f, 0.94f, 1f);     // White crescent throat bib
            Color bellyWhite = new Color(0.96f, 0.95f, 0.91f, 1f);         // Creamy white underside
            Color bellyWarmEdge = new Color(0.88f, 0.76f, 0.60f, 1f);      // Warm cream blend
            Color darkHoof = new Color(0.10f, 0.08f, 0.08f, 1f);           // Charcoal cloven hoof
            Color wetNose = new Color(0.06f, 0.05f, 0.05f, 1f);            // Wet black nose leather
            Color antlerBeam = new Color(0.72f, 0.60f, 0.46f, 1f);         // Natural bone horn tan
            Color antlerBurr = new Color(0.35f, 0.23f, 0.15f, 1f);         // Rough dark base burr
            Color antlerTip = new Color(0.93f, 0.89f, 0.82f, 1f);          // Polished ivory tip

            // Try loading Deer_Coat.jpg for organic fur micro-grain
            Texture2D coatBase = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Deer_Coat.jpg");
            Color[] coatPixels = null;
            if (coatBase != null)
            {
                string coatPath = AssetDatabase.GetAssetPath(coatBase);
                var imp = AssetImporter.GetAtPath(coatPath) as TextureImporter;
                if (imp != null && !imp.isReadable)
                {
                    imp.isReadable = true;
                    imp.SaveAndReimport();
                    coatBase = AssetDatabase.LoadAssetAtPath<Texture2D>(coatPath);
                }
                try { coatPixels = coatBase.GetPixels(); } catch {}
            }

            Color[] finalPixels = new Color[size * size];

            // 5. Evaluate Color per Pixel
            float spotRadius = 2.15f; // In 3D units (~3.5cm radius)

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    if (!hasData[idx])
                    {
                        // Background fallback (will be dilated later)
                        finalPixels[idx] = Color.clear;
                        continue;
                    }

                    Vector3 pos = posBuffer[idx];
                    float absX = Mathf.Abs(pos.x);
                    Color pixelCol = goldenAmberFur;

                    // A. Antlers (Y > 45.0f, Z > 15.0f)
                    if (pos.y > 45.0f && pos.z > 15.0f)
                    {
                        if (pos.y < 52.0f)
                            pixelCol = Color.Lerp(antlerBurr, antlerBeam, (pos.y - 45.0f) / 7.0f);
                        else if (pos.y < 64.0f)
                            pixelCol = antlerBeam;
                        else
                            pixelCol = Color.Lerp(antlerBeam, antlerTip, Mathf.Clamp01((pos.y - 64.0f) / 4.0f));

                        // Horn longitudinal grain
                        float grain = (Mathf.PerlinNoise(pos.x * 0.8f, pos.y * 0.15f) - 0.5f) * 0.06f;
                        pixelCol.r += grain; pixelCol.g += grain; pixelCol.b += grain;
                    }
                    // B. Cloven Hooves (Y < -57.0f)
                    else if (pos.y < -57.0f)
                    {
                        pixelCol = darkHoof;
                        float hoofShine = (Mathf.PerlinNoise(pos.x * 0.3f, pos.y * 0.3f) - 0.5f) * 0.04f;
                        pixelCol.r += hoofShine; pixelCol.g += hoofShine; pixelCol.b += hoofShine;
                    }
                    // C. Wet Nose Pad & Muzzle (Z > 63.0f, Y: 26..40, absX < 4.5f)
                    else if (pos.z > 63.0f && pos.y >= 26.0f && pos.y <= 40.0f && absX < 4.5f)
                    {
                        pixelCol = wetNose;
                    }
                    // D. Body / Torso / Legs / Head
                    else
                    {
                        // Blend with organic fur texture if available
                        if (coatPixels != null && coatBase != null)
                        {
                            int cx = (x * coatBase.width) / size;
                            int cy = (y * coatBase.height) / size;
                            Color furGrain = coatPixels[cy * coatBase.width + cx];
                            pixelCol = Color.Lerp(pixelCol, furGrain, 0.35f);
                            pixelCol = Color.Lerp(pixelCol, goldenAmberFur, 0.40f);
                        }

                        // Organic fur variation
                        float furNoise = (Mathf.PerlinNoise(pos.x * 0.25f + pos.z * 0.15f, pos.y * 0.25f) - 0.5f) * 0.05f;
                        pixelCol.r += furNoise; pixelCol.g += furNoise; pixelCol.b += furNoise;

                        // 1. Underbelly & Inner Legs (Creamy White)
                        bool isUnderbelly = (pos.y < -5.0f && absX < 12.0f && pos.z >= -35.0f && pos.z <= 25.0f) ||
                                           (pos.y < -15.0f && absX < 6.5f);
                        if (isUnderbelly)
                        {
                            float bellyBlend = Mathf.Clamp01((-pos.y - 5.0f) / 6.0f);
                            Color bCol = Color.Lerp(bellyWarmEdge, bellyWhite, bellyBlend);
                            pixelCol = Color.Lerp(pixelCol, bCol, 0.92f);
                        }

                        // 2. White Crescent Throat Bib (Z: 22..42, Y: 10..28, absX < 5.0)
                        if (pos.z >= 22.0f && pos.z <= 42.0f && pos.y >= 10.0f && pos.y <= 28.0f && absX < 5.0f)
                        {
                            float bibDist = Mathf.Sqrt((absX / 4.5f) * (absX / 4.5f) + ((pos.y - 19.0f) / 8.0f) * ((pos.y - 19.0f) / 8.0f));
                            if (bibDist < 1.0f)
                            {
                                float bAlpha = Mathf.SmoothStep(1.0f, 0.0f, bibDist);
                                pixelCol = Color.Lerp(pixelCol, throatBibWhite, bAlpha * 0.95f);
                            }
                        }

                        // 3. Dark Dorsal Spine Stripe (absX < 3.2f, Y > 10.0f, Z: -45..28)
                        if (absX < 3.2f && pos.y > 10.0f && pos.z >= -45.0f && pos.z <= 28.0f)
                        {
                            float spineFactor = Mathf.Clamp01(1.0f - (absX / 3.0f));
                            spineFactor = Mathf.SmoothStep(0f, 1f, spineFactor);
                            pixelCol = Color.Lerp(pixelCol, dorsalDarkStripe, spineFactor * 0.88f);
                        }

                        // 4. Crisp Chitra White Spots ( চিত্রা হরিণ )
                        // Flank condition: must be on sides (absX > 4.5f) and not on underside
                        if (absX > 4.5f && pos.y >= -6.0f && pos.y <= 20.0f && pos.z >= -42.0f && pos.z <= 26.0f)
                        {
                            float minSpotDist = 999f;
                            Vector3 pSym = new Vector3(absX, pos.y, pos.z);

                            for (int i = 0; i < spotCenters.Count; i++)
                            {
                                Vector3 sc = spotCenters[i];
                                // Elliptical distance: slightly elongated along length of body (Z)
                                float dZ = (pSym.z - sc.z) / 1.15f;
                                float dY = (pSym.y - sc.y);
                                float dX = (pSym.x - sc.x) * 0.7f;
                                float dist = Mathf.Sqrt(dX * dX + dY * dY + dZ * dZ);

                                if (dist < minSpotDist) minSpotDist = dist;
                            }

                            if (minSpotDist < spotRadius)
                            {
                                float sNorm = minSpotDist / spotRadius;
                                float spotAlpha;
                                if (sNorm < 0.65f)
                                {
                                    spotAlpha = 1.0f;
                                }
                                else
                                {
                                    spotAlpha = Mathf.SmoothStep(1.0f, 0.0f, (sNorm - 0.65f) / 0.35f);
                                }

                                // Subtle dark depth rim around the outer edge (0.70 < sNorm < 1.25)
                                if (sNorm > 0.70f && sNorm < 1.25f)
                                {
                                    float rimNorm = (sNorm - 0.70f) / 0.55f;
                                    float rimFactor = Mathf.Sin(rimNorm * Mathf.PI);
                                    pixelCol = Color.Lerp(pixelCol, spotRimDark, rimFactor * 0.75f);
                                }

                                pixelCol = Color.Lerp(pixelCol, whiteSpotColor, spotAlpha * 0.99f);
                            }
                        }
                    }

                    finalPixels[idx] = pixelCol;
                }
            }

            // 6. Dilation Pass to eliminate UV seam artifacts
            // Dilate 8 pixels into empty background
            for (int pass = 0; pass < 8; pass++)
            {
                Color[] passPixels = (Color[])finalPixels.Clone();
                bool anyFilled = false;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int idx = y * size + x;
                        if (finalPixels[idx].a > 0.01f) continue;

                        // Check 4-neighbors
                        Color neighbor = Color.clear;
                        if (x > 0 && passPixels[idx - 1].a > 0.5f) neighbor = passPixels[idx - 1];
                        else if (x < size - 1 && passPixels[idx + 1].a > 0.5f) neighbor = passPixels[idx + 1];
                        else if (y > 0 && passPixels[idx - size].a > 0.5f) neighbor = passPixels[idx - size];
                        else if (y < size - 1 && passPixels[idx + size].a > 0.5f) neighbor = passPixels[idx + size];

                        if (neighbor.a > 0.5f)
                        {
                            finalPixels[idx] = neighbor;
                            anyFilled = true;
                        }
                    }
                }

                if (!anyFilled) break;
            }

            // Fill any remaining empty background with goldenAmberFur
            for (int i = 0; i < finalPixels.Length; i++)
            {
                if (finalPixels[i].a < 0.01f) finalPixels[i] = goldenAmberFur;
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.SetPixels(finalPixels);
            tex.Apply(true);

            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(outPath, pngBytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
            }

            Debug.Log($"<color=green>[ChitraDeer] Successfully baked 3D anatomical Chitra texture to: {outPath}</color>");
        }

        public static void ConfigureDeerMaterial()
        {
            string matPath = "Assets/Materials/Authentic_Deer.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Tex_Chitra_Deer_2K.png");
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }

            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.20f); // Soft organic fur sheen
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ChitraDeer] Configured material '{matPath}' with 2K Chitra texture & organic fur smoothness.");
        }

        public static void ConfigureDeerGameObject()
        {
            var deer = GameObject.Find("Deer");
            if (deer == null)
            {
                Debug.LogError("[ChitraDeer] Deer GameObject not found in active scene!");
                return;
            }

            // Ensure Deer Rig is built with 20-bone skeleton
            DeerRigBuilder.BuildRig();

            // Replace old QuadrupedProceduralAnimator with dedicated ChitraDeerProceduralAnimator
            var oldAnim = deer.GetComponent<QuadrupedProceduralAnimator>();
            if (oldAnim != null)
            {
                Object.DestroyImmediate(oldAnim);
            }

            var deerAnim = deer.GetComponent<ChitraDeerProceduralAnimator>();
            if (deerAnim == null) deerAnim = deer.AddComponent<ChitraDeerProceduralAnimator>();

            // Find Skeleton bones under VisualModel/Deer_Skeleton
            var skeleton = deer.transform.Find("VisualModel/Deer_Skeleton");
            if (skeleton != null)
            {
                deerAnim.rootBone       = skeleton;
                deerAnim.pelvisBone     = skeleton.Find("Bone_Pelvis");
                if (deerAnim.pelvisBone != null)
                {
                    deerAnim.spineBone  = deerAnim.pelvisBone.Find("Bone_Spine");
                    deerAnim.tailBaseBone = deerAnim.pelvisBone.Find("Bone_Tail_Base");
                    if (deerAnim.tailBaseBone != null)
                    {
                        deerAnim.tailMidBone = deerAnim.tailBaseBone.Find("Bone_Tail_Mid");
                        if (deerAnim.tailMidBone != null)
                            deerAnim.tailTipBone = deerAnim.tailMidBone.Find("Bone_Tail_Tip");
                    }

                    deerAnim.legBLUpper = deerAnim.pelvisBone.Find("Bone_Leg_BL_Upper");
                    if (deerAnim.legBLUpper != null)
                    {
                        deerAnim.legBLLower = deerAnim.legBLUpper.Find("Bone_Leg_BL_Lower");
                        if (deerAnim.legBLLower != null)
                            deerAnim.hoofBL = deerAnim.legBLLower.Find("Bone_Hoof_BL");
                    }

                    deerAnim.legBRUpper = deerAnim.pelvisBone.Find("Bone_Leg_BR_Upper");
                    if (deerAnim.legBRUpper != null)
                    {
                        deerAnim.legBRLower = deerAnim.legBRUpper.Find("Bone_Leg_BR_Lower");
                        if (deerAnim.legBRLower != null)
                            deerAnim.hoofBR = deerAnim.legBRLower.Find("Bone_Hoof_BR");
                    }
                }

                if (deerAnim.spineBone != null)
                {
                    deerAnim.chestBone = deerAnim.spineBone.Find("Bone_Chest");
                    if (deerAnim.chestBone != null)
                    {
                        deerAnim.neckBone = deerAnim.chestBone.Find("Bone_Neck");
                        if (deerAnim.neckBone != null)
                            deerAnim.headBone = deerAnim.neckBone.Find("Bone_Head");

                        deerAnim.legFLUpper = deerAnim.chestBone.Find("Bone_Leg_FL_Upper");
                        if (deerAnim.legFLUpper != null)
                        {
                            deerAnim.legFLLower = deerAnim.legFLUpper.Find("Bone_Leg_FL_Lower");
                            if (deerAnim.legFLLower != null)
                                deerAnim.hoofFL = deerAnim.legFLLower.Find("Bone_Hoof_FL");
                        }

                        deerAnim.legFRUpper = deerAnim.chestBone.Find("Bone_Leg_FR_Upper");
                        if (deerAnim.legFRUpper != null)
                        {
                            deerAnim.legFRLower = deerAnim.legFRUpper.Find("Bone_Leg_FR_Lower");
                            if (deerAnim.legFRLower != null)
                                deerAnim.hoofFR = deerAnim.legFRLower.Find("Bone_Hoof_FR");
                        }
                    }
                }
            }

            // Tune biological ungulate deer movement parameters
            deerAnim.strideFrequency     = 4.8f;  // Nimble rapid trot
            deerAnim.legSwingAngle       = 32.0f; // Long athletic stride
            deerAnim.kneeLiftAngle       = 52.0f; // High-knee carpus lift over mangrove mud
            deerAnim.hockBendAngle       = 44.0f; // Hock spring push
            deerAnim.hoofFlexAngle       = 24.0f; // Delicate hoof touchdown
            deerAnim.bodyBounceHeight    = 0.055f;// Double-bounce ungulate suspension
            deerAnim.alertNeckElevation  = 14.0f; // Proud alert upright neck
            deerAnim.headNodAngle        = 5.2f;  // Rhythmic energetic head nod
            deerAnim.tailCockAngle       = 22.0f; // Alert cocked short deer tail
            deerAnim.tailFlickAngle      = 16.0f; // Tail flicking
            deerAnim.headScanAngle       = 18.0f; // Alert predator scanning
            deerAnim.idleBreathSpeed     = 2.4f;
            deerAnim.idleBreathAmplitude = 1.0f;

            deerAnim.CacheInitialTransforms();

            // Configure LandAnimalController physics
            var lac = deer.GetComponent<LandAnimalController>();
            if (lac == null) lac = deer.AddComponent<LandAnimalController>();
            lac.moveSpeed = 8.5f;       // Agile runner (faster than tiger's 7.0f)
            lac.sprintMultiplier = 1.9f;// Fast bounding gallop
            lac.jumpSpeed = 10.5f;      // High graceful arching steeplechase leap
            lac.turnSpeedDeg = 520f;    // Swift skittish pivots

            // Configure CharacterController collision
            var cc = deer.GetComponent<CharacterController>();
            if (cc == null) cc = deer.AddComponent<CharacterController>();
            cc.height = 1.35f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 0.68f, 0.05f);

            // Configure Backpack & SpeciesHealth
            var bp = deer.GetComponent<Backpack>();
            if (bp != null)
            {
                bp.startingCountPerTool = 0;
                bp.foodCount = 1;
                bp.EnsureInitialized();
                EditorUtility.SetDirty(bp);
            }

            var health = deer.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.maxHealth = 150f;
                health.travelHealRate = 0.5f;
                EditorUtility.SetDirty(health);
            }

            // Ensure SkinnedMeshRenderer uses Authentic_Deer material with the new 2K Chitra texture
            var smr = deer.GetComponentInChildren<SkinnedMeshRenderer>();
            var authenticMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Authentic_Deer.mat");
            if (smr != null && authenticMat != null)
            {
                smr.sharedMaterial = authenticMat;
                EditorUtility.SetDirty(smr);
            }

            // Position deer at Kotka Deer Glade clearing (safe open mud bank, unobstructed)
            Vector3 deerStartPos = new Vector3(-33.0f, 1.69f, 22.5f);
            if (cc != null) cc.enabled = false;
            deer.transform.position = deerStartPos;
            deer.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            if (cc != null) cc.enabled = true;

            EditorUtility.SetDirty(deer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(deer.scene);

            Debug.Log($"[ChitraDeer] Configured Deer at {deerStartPos} with ChitraDeerProceduralAnimator & Authentic_Deer material.");
        }
    }
}
