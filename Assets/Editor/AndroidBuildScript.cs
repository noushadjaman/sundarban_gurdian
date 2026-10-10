using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SundarbanGuardian.Editor
{
    public static class AndroidBuildScript
    {
        [MenuItem("Sundarban/Build Android APK")]
        public static void BuildAndroidApk()
        {
            Debug.Log("[AndroidBuild] Starting Android APK build...");

            // 1. Verify Android build target support
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[AndroidBuild] Android Build Support is not installed in this Unity Editor version (6000.6.2f1).");
                return;
            }

            // 2. Automatically configure JDK, Android SDK, and NDK paths in Unity
            ConfigureAndroidTools();

            // 3. Configure PlayerSettings
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.noushadjaman.sundarbanguardian");
            PlayerSettings.productName = "Sundarban Guardian";
            PlayerSettings.companyName = "Sundarban Guardian";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // 4. Ensure destination directory exists
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            string apkPath = Path.Combine(outputDir, "SundarbanGuardian.apk");

            // 5. Build Player Options
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            // 6. Execute Build
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuild] Build SUCCEEDED! Total size: {summary.totalSize / (1024 * 1024)} MB. Path: {apkPath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[AndroidBuild] Build FAILED with {summary.totalErrors} errors.");
            }
        }

        private static void ConfigureAndroidTools()
        {
            try
            {
                // Paths on this machine
                string jdkPath = @"C:\Users\RAJ\AppData\Local\Programs\Eclipse Adoptium\jdk-17.0.18.8-hotspot";
                if (!Directory.Exists(jdkPath))
                {
                    jdkPath = @"C:\Program Files\Android\Android Studio\jbr";
                }

                string sdkPath = @"C:\Users\RAJ\AppData\Local\Android\Sdk";
                string ndkPath = @"C:\Users\RAJ\AppData\Local\Android\Sdk\ndk\27.2.12479018";
                if (!Directory.Exists(ndkPath))
                {
                    ndkPath = @"C:\Users\RAJ\AppData\Local\Android\Sdk\ndk\27.0.12077973";
                }

                // Set EditorPrefs (both legacy and Unity 6 keys)
                EditorPrefs.SetBool("SdkUseEmbeddedJdk", false);
                EditorPrefs.SetBool("JdkUseEmbedded", false);
                EditorPrefs.SetString("JdkPath", jdkPath);
                EditorPrefs.SetBool("SdkUseEmbedded", false);
                EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
                EditorPrefs.SetBool("NdkUseEmbedded", false);
                EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
                EditorPrefs.SetString("AndroidNdkRootR27C", ndkPath);

                // Configure AndroidExternalToolsSettings and AndroidNDKRoot via reflection
                var asm = Assembly.Load("UnityEditor.Android.Extensions");
                if (asm != null)
                {
                    var ndkRootType = asm.GetType("UnityEditor.Android.AndroidNDKRoot");
                    if (ndkRootType != null)
                    {
                        var instMethod = ndkRootType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        var inst = instMethod?.Invoke(null, null);
                        if (inst != null)
                        {
                            var propUseEmbedded = ndkRootType.GetProperty("UseEmbedded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            propUseEmbedded?.SetValue(inst, false);

                            var propCustomDir = ndkRootType.GetProperty("CustomDirectory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            propCustomDir?.SetValue(inst, ndkPath);
                        }
                    }

                    var prefExtType = asm.GetType("UnityEditor.Android.AndroidPreferenceWindowExtension");
                    if (prefExtType != null)
                    {
                        var prefInst = Activator.CreateInstance(prefExtType);
                        var readMethod = prefExtType.GetMethod("ReadPreferences", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        readMethod?.Invoke(prefInst, null);

                        var ndkUseEmbeddedField = prefExtType.GetField("m_NdkUseEmbedded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        ndkUseEmbeddedField?.SetValue(prefInst, false);

                        var ndkPathField = prefExtType.GetField("m_AndroidNdkPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        ndkPathField?.SetValue(prefInst, ndkPath);

                        var writeMethod = prefExtType.GetMethod("WritePreferences", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        writeMethod?.Invoke(prefInst, null);
                    }

                    var type = asm.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                    if (type != null)
                    {
                        var propJdk = type.GetProperty("jdkRootPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (propJdk != null && propJdk.GetSetMethod(true) != null && Directory.Exists(jdkPath))
                        {
                            propJdk.SetValue(null, jdkPath);
                        }

                        var propSdk = type.GetProperty("sdkRootPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (propSdk != null && propSdk.GetSetMethod(true) != null && Directory.Exists(sdkPath))
                        {
                            propSdk.SetValue(null, sdkPath);
                        }

                        var propNdk = type.GetProperty("ndkRootPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (propNdk != null && propNdk.GetSetMethod(true) != null && Directory.Exists(ndkPath))
                        {
                            try { propNdk.SetValue(null, ndkPath); } catch { }
                        }

                        Debug.Log($"[AndroidBuild] Configured tools -> JDK: {jdkPath}, SDK: {sdkPath}, NDK: {ndkPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AndroidBuild] Error configuring Android tools: {ex.Message}");
            }
        }
    }
}
