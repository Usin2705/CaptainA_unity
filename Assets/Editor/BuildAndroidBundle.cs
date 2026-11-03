using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the Android app (.aab or .apk, per "Build App Bundle") without installing it on a
// device. It is the same build as File > Build And Run, minus the run step, so it works with
// no phone connected. Added because the Build button in the Build Profiles window silently
// does nothing in this editor version.
static class BuildAndroidBundle
{
    [MenuItem("SaySuomi/Build Android (no run)")]
    static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog(
                "Build Android",
                "The active platform is not Android. Switch to Android in File > Build Profiles first.",
                "OK"
            );
            return;
        }

        // Unity forgets the keystore passwords on every editor restart, and signing then fails
        // after several minutes of building. Catch it before the build starts.
        if (
            PlayerSettings.Android.useCustomKeystore
            && (
                string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)
                || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Build Android",
                "The keystore passwords are empty (Unity clears them on every restart).\n\n"
                    + "Enter both in Project Settings > Player > Android > Publishing Settings, then try again.",
                "OK"
            );
            SettingsService.OpenProjectSettings("Project/Player");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string[] scenes = EditorBuildSettings
            .scenes.Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Build Android",
                "No scenes are enabled in the build.",
                "OK"
            );
            return;
        }

        // Reuse the location Build And Run writes to, with the extension matching the AAB/APK setting.
        string path = EditorUserBuildSettings.GetBuildLocation(BuildTarget.Android);
        if (string.IsNullOrEmpty(path))
        {
            path = Path.GetFullPath("CaptainA_Android.aab");
        }
        path = Path.ChangeExtension(path, EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk");

        BuildReport report = BuildPipeline.BuildPlayer(
            new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = EditorUserBuildSettings.development
                    ? BuildOptions.Development
                    : BuildOptions.None,
            }
        );

        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log(
                $"Android build succeeded: {path} ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime:mm\\:ss})"
            );
            EditorUtility.RevealInFinder(path);
            return;
        }

        string firstError = report
            .steps.SelectMany(step => step.messages)
            .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
            .Select(m => m.content)
            .FirstOrDefault();
        EditorUtility.DisplayDialog(
            "Build Android",
            $"Build {summary.result} with {summary.totalErrors} error(s).\n\n"
                + (firstError ?? "See the Console for details."),
            "OK"
        );
    }
}
