#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// Post-processes the generated Xcode project for iOS builds:
/// 1. Links libsqlite3.tbd so SqliteNative P/Invoke calls resolve cleanly.
/// 2. Ensures NSCameraUsageDescription and NSLocationWhenInUseUsageDescription are present in Info.plist.
/// </summary>
public static class iOSBuildPostprocessor
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        // 1. Configure PBXProject for SQLite linkage
        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projectPath);

        string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
        if (string.IsNullOrEmpty(frameworkTargetGuid))
        {
            frameworkTargetGuid = proj.GetUnityMainTargetGuid();
        }

        // Link libsqlite3.tbd
        if (!proj.ContainsFramework(frameworkTargetGuid, "libsqlite3.tbd"))
        {
            proj.AddFrameworkToProject(frameworkTargetGuid, "libsqlite3.tbd", false);
        }

        proj.WriteToFile(projectPath);

        // 2. Configure Info.plist privacy descriptions
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        if (File.Exists(plistPath))
        {
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;

            // Camera usage is required for Vuforia AR
            if (!rootDict.values.ContainsKey("NSCameraUsageDescription"))
            {
                rootDict.SetString("NSCameraUsageDescription", "Camera access is required for Augmented Reality navigation and tracking.");
            }

            // Location usage for navigation
            if (!rootDict.values.ContainsKey("NSLocationWhenInUseUsageDescription"))
            {
                rootDict.SetString("NSLocationWhenInUseUsageDescription", "Location access is used for indoor positioning and navigation.");
            }

            plist.WriteToFile(plistPath);
        }
    }
}
#endif
