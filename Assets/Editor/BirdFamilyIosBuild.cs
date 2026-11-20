using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;

internal static class BirdFamilyIosBuild
{
    private const string DemoScene = "Assets/Scenes/TheWayHome.unity";

    [MenuItem("Bird Family/Export iOS Demo")]
    public static void Export()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            ReportSetup("Install iOS Build Support for this Unity editor through Unity Hub.");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
        {
            ReportSetup("Switch the active platform to iOS in File > Build Settings, then choose Export iOS Demo again.");
            return;
        }

        string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "iOS");
        Directory.CreateDirectory(root);
        string output = Path.Combine(root, "BirdFamily-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));

        var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.iOS);
        bool initializeXr = xr != null && xr.InitManagerOnStart;
        UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
        bool allowPortrait = PlayerSettings.allowedAutorotateToPortrait;
        bool allowUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        bool allowLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
        bool allowRight = PlayerSettings.allowedAutorotateToLandscapeRight;
        try
        {
            if (xr != null)
                xr.InitManagerOnStart = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { DemoScene },
                locationPathName = output,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Bird Family export ended with " + report.summary.result + ". See the Console for details.");

            Debug.Log("Bird Family iOS project exported to " + output);
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(Path.Combine(output, "Unity-iPhone.xcodeproj"));
        }
        finally
        {
            if (xr != null)
                xr.InitManagerOnStart = initializeXr;
            PlayerSettings.defaultInterfaceOrientation = orientation;
            PlayerSettings.allowedAutorotateToPortrait = allowPortrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = allowUpsideDown;
            PlayerSettings.allowedAutorotateToLandscapeLeft = allowLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight = allowRight;
        }
    }

    private static void ReportSetup(string message)
    {
        if (Application.isBatchMode)
            throw new BuildFailedException(message);

        EditorUtility.DisplayDialog("Export Bird Family for iOS", message, "OK");
    }
}
