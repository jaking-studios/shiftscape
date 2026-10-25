using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Shiftscape.EditorTools
{
    /// <summary>
    /// Player settings + builds. Menu: Shiftscape ▸ … , or headless:
    ///   Unity -batchmode -quit -projectPath . -executeMethod Shiftscape.EditorTools.ProjectBuilder.BuildWebGL
    /// The GitHub Actions workflow calls these.
    /// </summary>
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Shiftscape/Configure player settings")]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "JaKing Studios";
            PlayerSettings.productName = "Shiftscape";
            PlayerSettings.bundleVersion = "1.0.0";
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            var standalone = UnityEditor.Build.NamedBuildTarget.Standalone;
            var ios = UnityEditor.Build.NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(android, "com.jakingstudios.shiftscape");
            PlayerSettings.SetApplicationIdentifier(standalone, "com.jakingstudios.shiftscape");
            PlayerSettings.SetApplicationIdentifier(ios, "com.jakingstudios.shiftscape");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);

            // Android: IL2CPP + ARM64 (Play Store requirement), API 24+
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.bundleVersionCode = 1;

            // WebGL: uncompressed so it runs on GitHub Pages / Unity Play without special headers
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Shiftscape] Player settings configured");
        }

        static BuildReport Build(BuildTarget target, string path, BuildOptions opts = BuildOptions.None)
        {
            ConfigurePlayer();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = path, target = target, options = opts,
            });
            Debug.Log($"[Shiftscape] Build {target}: {report.summary.result}, {report.summary.totalSize / 1e6:0.0} MB, {report.summary.totalErrors} errors -> {path}");
            return report;
        }

        [MenuItem("Shiftscape/Build WebGL")]
        public static void BuildWebGL() => Exit(Build(BuildTarget.WebGL, "Builds/WebGL"));

        [MenuItem("Shiftscape/Build Android APK")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Exit(Build(BuildTarget.Android, "Builds/Android/Shiftscape.apk"));
        }

        [MenuItem("Shiftscape/Build Android AAB (Play Store)")]
        public static void BuildAndroidAab()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            Exit(Build(BuildTarget.Android, "Builds/Android/Shiftscape.aab"));
        }

        [MenuItem("Shiftscape/Build Windows")]
        public static void BuildWindows() => Exit(Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Shiftscape.exe"));

        [MenuItem("Shiftscape/Build macOS")]
        public static void BuildMac() => Exit(Build(BuildTarget.StandaloneOSX, "Builds/macOS/Shiftscape.app"));

        [MenuItem("Shiftscape/Build Linux")]
        public static void BuildLinux() => Exit(Build(BuildTarget.StandaloneLinux64, "Builds/Linux/Shiftscape.x86_64"));

        [MenuItem("Shiftscape/Export iOS Xcode project")]
        public static void BuildIOS() => Exit(Build(BuildTarget.iOS, "Builds/iOS"));

        static void Exit(BuildReport r)
        {
            if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
