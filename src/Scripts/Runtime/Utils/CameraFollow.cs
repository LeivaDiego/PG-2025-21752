using UnityEngine;

/// <summary>
/// Makes the camera follow the player with a specified offset.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // Reference to the player's transform
    Transform player;

    // Offset from the player's position
    public Vector3 offset = new Vector3(0, 10, 0);

    /// <summary>
    /// Initializes the player reference.
    /// </summary>
    void Start()
    {
        // Find the player GameObject by tag and get its transform
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /// <summary>
    /// Updates the camera's position and rotation after all other updates.
    /// </summary>
    void LateUpdate()
    {
        // If player is not found, do nothing
        if (player == null)
            return;
        // Set the camera's position to follow the player with the specified offset
        transform.position = player.position + offset;
        // Set the camera's rotation to look straight down
        transform.rotation = Quaternion.Euler(90, 0, 0);
    }
}
