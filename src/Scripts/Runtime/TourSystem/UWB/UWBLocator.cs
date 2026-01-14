using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Struct to hold coordinate data from UWB system.
/// </summary>
[Serializable]
public struct Coordinate
{
    public float x; // UWB x coordinate
    public float y; // UWB y coordinate
}

/// <summary>
/// UWBLocator provides methods to interact with the UWB positioning system.
/// It allows setting anchor maps and retrieving real-time position data.
/// </summary>
/// <remarks>
/// This class uses platform-specific native plugins for iOS devices.
/// On unsupported platforms, it provides stub implementations.
/// </remarks>
public static class UWBLocator
{
    // State to track initialization and current anchor map
    public static bool IsInitialized => isInitialized;
    private static bool isInitialized = false;
    private static string currentAnchorMap;

#if UNITY_IOS && !UNITY_EDITOR
    // Platform-specific native method bindings (iOS)
    [DllImport("__Internal")]
    private static extern IntPtr getCoords();

    [DllImport("__Internal")]
    private static extern void freeCString(IntPtr ptr);

    [DllImport("__Internal")]
    private static extern void setAnchorMap(string jsonUtf8);

    [DllImport("__Internal")]
    private static extern void start();
#elif UNITY_EDITOR && !UNITY_IOS
    // Stub implementations for unsupported platforms
    private static bool hasWarned = false;

    private static IntPtr getCoords() => IntPtr.Zero;

    private static void freeCString(IntPtr ptr) { }

    private static void setAnchorMap(string jsonUtf8) { }

    private static void start() { }
#else
    // Fallback stubs for all other platforms
    private static IntPtr getCoords() => IntPtr.Zero;

    private static void freeCString(IntPtr ptr) { }

    private static void setAnchorMap(string jsonUtf8) { }

    private static void start() { }
#endif

    /// <summary>
    /// Attempts to get the current position from the UWB system.
    /// </summary>
    /// <param name="position"></param>
    /// <returns> True if the position was successfully retrieved; otherwise, false. </returns>
    public static bool TryGetPosition(out Vector3 position)
    {
        position = default;

#if UNITY_EDITOR && !UNITY_IOS
        // Warn once in the editor about unsupported platform
        if (!hasWarned)
        {
            Debug.LogWarning(
                "[UWBLocator] Real time positioning is supported only on iOS device builds."
            );
            hasWarned = true;
        }
        return false;

#elif UNITY_IOS && !UNITY_EDITOR
        // Call the native plugin to get coordinates
        IntPtr coordsPtr = getCoords();

        // Check for null pointer
        if (coordsPtr == IntPtr.Zero)
        {
            Debug.LogWarning("[UWBLocator] Received null pointer for coordinates from UWB plugin.");
            return false;
        }

        try
        {
            // Convert the C string to a managed string and parse JSON
            string json = Marshal.PtrToStringAnsi(coordsPtr);
            Debug.Log($"[UWBLocator] JSON from plugin: {json}");

            // Validate JSON content
            if (string.IsNullOrEmpty(json) || json == "{}" || json.Contains("null"))
            {
                Debug.LogWarning(
                    "[UWBLocator] Received invalid JSON or null coordinates from UWB plugin."
                );
                return false;
            }

            // Deserialize JSON to Coordinate struct
            var uwbPosition = JsonUtility.FromJson<Coordinate>(json);
            Debug.Log($"[UWBLocator] Parsed UWB Position - x: {uwbPosition.x}, y: {uwbPosition.y}");
            position = new Vector3(uwbPosition.x, 0f, uwbPosition.y);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UWBLocator] Failed to parse UWB position JSON: {ex.Message}");
            return false;
        }
        finally
        {
            // Free the allocated C string to prevent memory leaks
            Debug.Log($"[UWBLocator] Freeing allocated string for coordinates.");
            freeCString(coordsPtr);
        }
#else
        Debug.LogWarning(
            "[UWBLocator] Real time positioning is supported only on iOS device builds."
        );
        return false;
#endif
    }

    /// <summary>
    /// Sets the anchor map for the UWB system.
    /// </summary>
    /// <param name="anchorMap">The anchor map in JSON format.</param>
    /// <remarks>
    /// ThE Anchor Map should be a JSON string representing the beacons coordinates in real world coordinates.
    /// </remarks>
    public static void SetAnchorMap(string anchorMap)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(anchorMap))
        {
            Debug.LogWarning("[UWBLocator] SetAnchorMap: Anchor map is null or empty.");
            return;
        }
        if (currentAnchorMap == anchorMap)
        {
            Debug.Log("[UWBLocator] SetAnchorMap: same Anchor map, no change.");
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            // Call the native plugin to set the anchor map
            setAnchorMap(anchorMap);
            currentAnchorMap = anchorMap;
            Debug.Log($"[UWBLocator] SetAnchorMap: Anchor map set to {anchorMap}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UWBLocator] SetAnchorMap: Failed setting anchor map: {ex.Message}");
        }
#else
        // On unsupported platforms, just store the anchor map
        currentAnchorMap = anchorMap;
        Debug.Log("[UWBLocator] Not supported on this platform.");
#endif
    }

    /// <summary>
    /// Starts the UWB locator system.
    /// </summary>
    public static void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            // Call the native plugin to start the UWB system
            start();
            Debug.Log("[UWBLocator] Native Plugin started.");
            isInitialized = true;
        }
        catch (Exception ex)
        {
            // If starting fails, log the error and set isInitialized to false
            isInitialized = false;
            Debug.LogError($"[UWBLocator] Start failed: {ex.Message}");
        }
#endif
    }
}
