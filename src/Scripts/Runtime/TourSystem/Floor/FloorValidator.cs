using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Validates that the floor scene is correctly set up at runtime.
/// </summary>
[DefaultExecutionOrder(300)]
public class FloorValidator : MonoBehaviour
{
    [SerializeField]
    private FloorDefinition floor;

    /// <summary>
    /// Performs validation checks on start.
    /// </summary>
    private void Start()
    {
        bool ok = true;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            Debug.LogError("[FloorValidator] No GameObject tagged 'Player' in scene.");
            ok = false;
        }
        else
        {
            if (!player.GetComponent<MovementAgent>())
            {
                Debug.LogError("[FloorValidator] Player missing MovementAgent.");
                ok = false;
            }
            if (!player.GetComponent<PathProvider>())
            {
                Debug.LogError("[FloorValidator] Player missing PathProvider.");
                ok = false;
            }
        }

        var reg = FindFirstObjectByType<AreaRegistry>(FindObjectsInactive.Include);
        if (!reg)
        {
            Debug.LogError("[FloorValidator] Missing AreaRegistry.");
            ok = false;
        }
        else
        {
            reg.Refresh();
            if (!floor)
            {
                Debug.LogError("[FloorValidator] Missing FloorDefinition reference.");
                ok = false;
            }
            else
            {
                int found = 0,
                    missing = 0;
                foreach (var def in floor.OrderedAreas)
                {
                    if (!def)
                        continue;

                    if (reg.TryGet(def, out var go))
                    {
                        found++;
                    }
                    else
                    {
                        Debug.LogError(
                            $"[FloorValidator] Missing AreaInstance for '{def.name}' in this scene."
                        );
                        missing++;
                    }
                }

                if (found == 0)
                {
                    Debug.LogError("[FloorValidator] No floor areas resolved.");
                    ok = false;
                }
            }
        }

        var tri = NavMesh.CalculateTriangulation();
        if (tri.vertices == null || tri.vertices.Length == 0)
        {
            Debug.LogError("[FloorValidator] No baked NavMesh in scene.");
            ok = false;
        }

        if (ok)
            Debug.Log("[FloorValidator] OK: player, registry, areas, and NavMesh are valid.");
    }
}
