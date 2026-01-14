#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

/// <summary>
/// Post-build processor to modify the Info.plist for iOS builds.
/// </summary>
public static class PostBuildProcessor
{
    /// <summary>
    /// Called after the iOS build is complete to modify the Info.plist file.
    /// </summary>
    /// <param name="target">The build target platform.</param>
    /// <param name="path">The path to the built project.</param>
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        // Only process for iOS builds
        if (target != BuildTarget.iOS)
            return;

        // Path to the Info.plist file
        string plistPath = Path.Combine(path, "Info.plist"); 
        var plist = new PlistDocument(); 
        plist.ReadFromFile(plistPath); 
        var root = plist.root; 

        // Add required usage descriptions
        root.SetString(
            "NSNearbyInteractionUsageDescription",
            "Used to perform precise ranging with nearby devices/beacons."
        );
        root.SetString(
            "NSNearbyInteractionAllowOnceUsageDescription",
            "Used to perform precise ranging with nearby devices/beacons."
        );
        root.SetString(
            "NSBluetoothAlwaysUsageDescription",
            "Bluetooth is required to communicate with nearby accessories."
        );
        root.SetString(
            "NSCameraUsageDescription",
            "Camera is used by ARKit for spatial understanding."
        );
        // Write the modified plist back to file
        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif
