using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;

public class ArrowNavigator : MonoBehaviour
{
    [Header("Arrow")]
    public Transform arrow3D;
    public float arrowDistance = 2f;

    [Header("Compass")]
    public float currentFacing = 0f;

    [Header("Pathfinding")]
    [SerializeField]
    private PathProvider pathfindingObject; // now optional

    [Header("AR Settings")]
    public Transform arCamera;

    private NavMeshPath currentPath;
    private ARSession arSession;
    private ARCameraManager cameraManager;
    private TourRunner tourRunner;

    // === NEW: public binder API ===
    public void SetPathProvider(PathProvider provider)
    {
        if (pathfindingObject == provider)
            return;

        // Unhook old
        if (pathfindingObject != null)
            pathfindingObject.OnPathUpdated -= HandlePathUpdated;

        pathfindingObject = provider;

        // Hook new
        if (pathfindingObject != null)
        {
            pathfindingObject.OnPathUpdated += HandlePathUpdated;

            // Optional: seed current path if provider already has one
            HandlePathUpdated(pathfindingObject.CurrentPath);
        }
        else
        {
            currentPath = null;
            if (arrow3D)
            {
                arrow3D.gameObject.SetActive(false);
                Debug.Log("[AR Nav] Unbound from PathProvider");
            }
        }

        Debug.Log(
            pathfindingObject
                ? $"[AR Nav] Bound to PathProvider #{pathfindingObject.GetInstanceID()} on {pathfindingObject.gameObject.name}"
                : "[AR Nav] Unbound from PathProvider"
        );
    }

    void OnEnable()
    {
        // Hook existing provider if assigned in inspector
        if (pathfindingObject != null)
            pathfindingObject.OnPathUpdated += HandlePathUpdated;

        // === NEW: listen to TourRunner to follow floor changes ===
        tourRunner = FindFirstObjectByType<TourRunner>(FindObjectsInactive.Include);
        if (tourRunner != null)
        {
            tourRunner.FloorLoaded += OnFloorLoaded;
            tourRunner.FloorUnloaded += OnFloorUnloaded;
        }

        ARSession.stateChanged += OnARSessionStateChanged;
    }

    void OnDisable()
    {
        if (pathfindingObject != null)
            pathfindingObject.OnPathUpdated -= HandlePathUpdated;

        if (tourRunner != null)
        {
            tourRunner.FloorLoaded -= OnFloorLoaded;
            tourRunner.FloorUnloaded -= OnFloorUnloaded;
            tourRunner = null;
        }

        ARSession.stateChanged -= OnARSessionStateChanged;
    }

    void Start()
    {
        Debug.Log("[AR Nav] Start called");
        if (arrow3D)
            arrow3D.gameObject.SetActive(false);

        // Find AR Session
        arSession = FindFirstObjectByType<ARSession>();
        if (arSession == null)
            Debug.LogError("[AR] No ARSession found in scene! Add an ARSession GameObject.");
        else
            Debug.Log($"[AR] ARSession state: {ARSession.state}");

        // Find AR Camera Manager
        if (arCamera != null)
        {
            cameraManager = arCamera.GetComponent<ARCameraManager>();
            if (cameraManager == null)
                Debug.LogError("[AR] No ARCameraManager on camera! Add ARCameraManager component.");
            else
                cameraManager.enabled = true;

            var camBg = arCamera.GetComponent<ARCameraBackground>();
            if (camBg == null)
                Debug.LogError("[AR] No ARCameraBackground component on Main Camera!");
            else
                camBg.enabled = true;
        }
        else
        {
            Debug.LogError("[AR] AR Camera not assigned in Inspector!");
        }

        Input.compass.enabled = true;
        Input.location.Start();

        // If no provider yet, try to discover one in the currently loaded floor
        if (pathfindingObject == null)
            TryFindProviderInScene();
    }

    private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        Debug.Log($"[AR] Session state changed to: {args.state}");
    }

    // === NEW: rebind when a floor scene loads/unloads ===
    private void OnFloorLoaded(FloorDefinition floor, FloorManager fm)
    {
        // Prefer the MovementAgent’s provider if present
        var ma = fm ? fm.GetComponentInChildren<MovementAgent>(true) : null;
        var pp = ma ? ma.GetComponent<PathProvider>() : null;

        if (pp == null)
        {
            // Fallbacks
            pp = FindFirstObjectByType<PathProvider>(FindObjectsInactive.Include);
            if (pp == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player)
                    pp = player.GetComponent<PathProvider>();
            }
        }

        SetPathProvider(pp);
    }

    private void OnFloorUnloaded(FloorDefinition _)
    {
        SetPathProvider(null);
    }

    private void TryFindProviderInScene()
    {
        // Mirrors TourBinder.BindPathProvider logic
        var ma = FindFirstObjectByType<MovementAgent>(FindObjectsInactive.Include);
        var pp = ma ? ma.GetComponent<PathProvider>() : null;

        if (pp == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player)
                pp = player.GetComponent<PathProvider>();
        }

        if (pp == null)
            pp = FindFirstObjectByType<PathProvider>(FindObjectsInactive.Include);

        if (pp != null)
            SetPathProvider(pp);
        else
            Debug.LogWarning("[AR Nav] No PathProvider found to bind.");
    }

    private void HandlePathUpdated(NavMeshPath path)
    {
        currentPath = path;
        bool hasPath =
            currentPath != null && currentPath.corners != null && currentPath.corners.Length >= 2;
        arrow3D.gameObject.SetActive(hasPath);

        if (!hasPath)
        {
            Debug.Log("[AR Nav] No valid path available. Arrow hidden.");
            return;
        }

        Debug.Log($"[AR Nav] Path updated: {path.corners.Length} corners");
    }

    void Update()
    {
        if (!arrow3D || !arCamera)
            return;

        // 1. Siempre mantener la flecha enfrente de la cámara
        arrow3D.position = arCamera.position + arCamera.forward * arrowDistance;

        // 2. Si no hay path válido, ocultar / no rotar
        if (currentPath == null || currentPath.corners == null || currentPath.corners.Length < 2)
        {
            if (arrow3D.gameObject.activeSelf)
                arrow3D.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!arrow3D.gameObject.activeSelf)
                arrow3D.gameObject.SetActive(true);
        }

        // 3. Obtener el punto "playerPos" y el "nextCorner" del path
        Vector3 playerPos = currentPath.corners[0];
        Vector3 nextCorner = currentPath.corners[1];

        // 4. Vector hacia el siguiente corner, pero aplanado (sin Y)
        Vector3 flatDir = new Vector3(nextCorner.x - playerPos.x, 0f, nextCorner.z - playerPos.z);

        // Si el vector es casi cero, no intentemos girar
        if (flatDir.sqrMagnitude < 0.0001f)
            return;

        // 5. Yaw absoluto en mundo hacia el siguiente corner
        //    atan2(x,z) (ojo, no z,x) para obtener ángulo en grados relativos al +Z
        float worldYawDeg = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;

        // 6. CORRECCIÓN DE NORTE / BRÚJULA
        // Tu offset actual: northOffset = 360 - currentFacing;
        // luego: compassHeading = trueHeading + northOffset;
        // Esto básicamente te alinea mundo Unity vs mundo real.
        // Ese ángulo es la diferencia entre "norte real del usuario" y "forward +Z de la escena".
        //
        // Entonces restamos ese heading para que la flecha apunte a donde tiene que ir,
        // pero expresado en el frame de referencia del usuario.
        float compassHeading = (Input.compass.trueHeading + (360f - currentFacing)) % 360f;

        // 7. Queremos una rotación PLANA (solo yaw) que le diga al usuario
        // "gira X grados desde donde estás mirando ahora".
        //
        // El usuario está mirando en algún yaw actual. Sacamos el yaw actual de la cámara.
        Vector3 camFwdFlat = new Vector3(arCamera.forward.x, 0f, arCamera.forward.z);
        if (camFwdFlat.sqrMagnitude < 0.0001f)
            camFwdFlat = arCamera.transform.rotation * Vector3.forward; // fallback
        camFwdFlat.Normalize();

        float camYawDeg = Mathf.Atan2(camFwdFlat.x, camFwdFlat.z) * Mathf.Rad2Deg;

        // 8. Diferencia entre hacia dónde DEBE IR (worldYawDeg) y hacia dónde ESTÁ MIRANDO el usuario (camYawDeg)
        float relativeYawDeg = Mathf.DeltaAngle(camYawDeg, worldYawDeg);

        // 9. Creamos una rotación PLANA solo con ese yaw relativo.
        // OJO: aquí la flecha rota en su propio mundo, pero sin pitch/roll.
        Quaternion flatRot = Quaternion.Euler(0f, camYawDeg + relativeYawDeg, 0f);

        arrow3D.rotation = flatRot;
    }
}
