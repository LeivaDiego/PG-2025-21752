using UnityEngine;

/// <summary>
/// Ensures that the GameObject this script is attached to
/// persists across scene loads.
/// </summary>
public sealed class AppLifetime : MonoBehaviour
{
    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        // Detach from parent to avoid being destroyed with it
        if (transform.parent != null)
            transform.SetParent(null);
        // Prevent this GameObject from being destroyed on scene load
        DontDestroyOnLoad(gameObject);
    }
}
