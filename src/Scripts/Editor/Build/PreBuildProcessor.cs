#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor;

/// <summary>
/// Pre-build processor to set the iOS bundle identifier.
/// </summary>
public class BundleIdPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    /// <summary>
    /// Called before the build process starts to set the iOS bundle identifier.
    /// </summary>
    /// <param name="report">The build report containing build details.</param>
    public void OnPreprocessBuild(BuildReport report)
    {
#if UNITY_IOS
        // Set the iOS bundle identifier
        const string bundleId = "tour.diego.testapp";
        // Using NamedBuildTarget for compatibility with Unity 2023.1 and later
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId);
        // Log the bundle identifier for verification
        UnityEngine.Debug.Log($"[BundleIdPreprocessor] iOS bundle id set to: {bundleId}");
#endif
    }
}
#endif
