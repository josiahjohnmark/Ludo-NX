using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android build entry points for Ludo NX.
///
/// Google Play takes an .aab (App Bundle), not an .apk, so
/// <see cref="BuildAppBundle"/> forces <c>EditorUserBuildSettings.buildAppBundle</c>
/// on before building - that flag lives in Library/EditorUserBuildSettings.asset
/// and is therefore lost whenever the Library cache is cleared.
///
/// Everything is checked up front by <see cref="Preflight"/>. A misconfigured
/// Android build usually fails in Gradle after ~30 minutes of work, so it is
/// worth failing in two seconds instead.
/// </summary>
public static class LudoNXBuild
{
    private const string OutputDir = "Builds/Android";

    [MenuItem("Ludo NX/Build/Check Readiness", priority = 1)]
    public static void CheckReadiness()
    {
        var problems = Preflight();
        if (problems.Count == 0)
        {
            Debug.Log($"[Ludo NX] Ready to build.\n{Describe()}");
            EditorUtility.DisplayDialog("Ludo NX", "Ready to build.\n\n" + Describe(), "OK");
            return;
        }

        string text = string.Join("\n", problems.Select(p => "- " + p));
        Debug.LogError($"[Ludo NX] Not ready to build:\n{text}");
        EditorUtility.DisplayDialog("Ludo NX - not ready", text, "OK");
    }

    [MenuItem("Ludo NX/Build/Android App Bundle (.aab)", priority = 2)]
    public static void BuildAppBundle() => Run(appBundle: true);

    [MenuItem("Ludo NX/Build/Android APK (test install)", priority = 3)]
    public static void BuildApk() => Run(appBundle: false);

    private static void Run(bool appBundle)
    {
        var problems = Preflight();
        if (problems.Count > 0)
        {
            string text = string.Join("\n", problems.Select(p => "- " + p));
            Debug.LogError($"[Ludo NX] Build cancelled:\n{text}");
            EditorUtility.DisplayDialog("Ludo NX - build cancelled", text, "OK");
            return;
        }

        // Play Console accepts the bundle; the APK is only for sideloading.
        EditorUserBuildSettings.buildAppBundle = appBundle;

        Directory.CreateDirectory(OutputDir);
        string extension = appBundle ? "aab" : "apk";
        string name = $"LudoNX_v{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}.{extension}";
        string path = Path.Combine(OutputDir, name);

        var options = new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = path,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        Debug.Log($"[Ludo NX] Building {extension.ToUpperInvariant()} -> {path}\n{Describe()}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            double mb = summary.totalSize / (1024.0 * 1024.0);
            Debug.Log($"[Ludo NX] BUILD SUCCEEDED in {summary.totalTime:hh\\:mm\\:ss}\n" +
                      $"  {Path.GetFullPath(path)}\n  {mb:N1} MB");
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }
        else
        {
            Debug.LogError($"[Ludo NX] BUILD {summary.result} after {summary.totalTime:hh\\:mm\\:ss} " +
                           $"with {summary.totalErrors} error(s). See the Console and Editor.log.");
        }
    }

    /// <summary>Everything that would otherwise surface as a late Gradle failure.</summary>
    private static List<string> Preflight()
    {
        var problems = new List<string>();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            problems.Add($"Active build target is {EditorUserBuildSettings.activeBuildTarget}, not Android. " +
                         "Switch platform in File > Build Profiles.");
        }

        if (EnabledScenes().Length == 0)
        {
            problems.Add("No scenes are enabled in Build Settings.");
        }

        string id = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("com.DefaultCompany", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add($"Package name is not set properly (currently '{id}').");
        }

        // Signing: Play rejects anything not signed with the upload key.
        if (!PlayerSettings.Android.useCustomKeystore)
        {
            problems.Add("Custom keystore is off - the build would be debug-signed and Play would reject it.");
        }
        else
        {
            string keystore = PlayerSettings.Android.keystoreName;
            if (string.IsNullOrWhiteSpace(keystore))
            {
                problems.Add("No keystore selected (Player > Publishing Settings).");
            }
            else if (!File.Exists(keystore))
            {
                problems.Add($"Keystore file not found: {keystore}");
            }

            if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
            {
                problems.Add("Keystore password is empty. Enter it in Player > Publishing Settings " +
                             "(it is not saved in the project, so it is cleared whenever Library is deleted).");
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
            {
                problems.Add("Key alias is not selected (Player > Publishing Settings).");
            }

            if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                problems.Add("Key alias password is empty (Player > Publishing Settings).");
            }
        }

        // "Both" is rejected outright on Android and cancels the build immediately.
        // There is no public API for this one, and GetPropertyInt logs a noisy
        // "Unknown Property" warning, so read the serialized setting instead.
        if (ActiveInputHandler() == 2)
        {
            problems.Add("Active Input Handling is 'Both', which Android does not support. " +
                         "Set it to 'Input Manager (Old)' in Player Settings.");
        }

        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
        {
            problems.Add("ARM64 is not enabled. Google Play requires a 64-bit build.");
        }

        return problems;
    }

    /// <summary>0 = Input Manager, 1 = Input System package, 2 = Both. -1 if unreadable.</summary>
    private static int ActiveInputHandler()
    {
        try
        {
            foreach (string line in File.ReadAllLines("ProjectSettings/ProjectSettings.asset"))
            {
                string t = line.Trim();
                if (!t.StartsWith("activeInputHandler:", StringComparison.Ordinal)) continue;
                return int.TryParse(t.Substring("activeInputHandler:".Length).Trim(), out int v) ? v : -1;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ludo NX] Could not read Active Input Handling: {e.Message}");
        }
        return -1;
    }

    private static string[] EnabledScenes() =>
        EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

    private static string Describe()
    {
        return
            $"  package      {PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android)}\n" +
            $"  version      {PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})\n" +
            $"  min / target {PlayerSettings.Android.minSdkVersion} / {PlayerSettings.Android.targetSdkVersion}\n" +
            $"  scripting    {PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android)}\n" +
            $"  arch         {PlayerSettings.Android.targetArchitectures}\n" +
            $"  keystore     {PlayerSettings.Android.keystoreName} (alias {PlayerSettings.Android.keyaliasName})\n" +
            $"  scenes       {EnabledScenes().Length}";
    }
}
