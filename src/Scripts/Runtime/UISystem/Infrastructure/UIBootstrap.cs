using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Bootstrapper for the UI system
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class UIBootstrap : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("UXML for the Base Layout")]
    [SerializeField]
    private UIDocument uiDocument;

    [Tooltip("UXML for the UI Atlas Asset")]
    public UIAtlas uiAtlas;

    [Tooltip("Audio Atlas for UI sounds")]
    public AudioAtlas audioAtlas;

    [Header("Runtime settings")]
    [SerializeField]
    private bool forceOnboardingAlways = false;

    /// <summary>
    /// Gets the UI router.
    /// </summary>
    public UIRouter Router { get; private set; }

    /// <summary>
    /// Gets the application root visual element.
    /// </summary>
    public VisualElement AppRoot { get; private set; }

    /// <summary>
    /// Initializes the UI Bootstrapper.
    /// </summary>
    private void Awake()
    {
        if (AppPrefs.IsFirstRun())
        {
            AppPrefs.SaveVolume(50);
            AppPrefs.SaveFontPx(100);
        }

        if (!uiDocument)
        {
            uiDocument = GetComponent<UIDocument>();
        }
        if (!uiDocument || !uiAtlas)
        {
            Debug.LogError("[UIBootstrap] Missing references in UIBootstrap");
            enabled = false;
            return;
        }

        foreach (
            var d in FindObjectsByType<UIDocument>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        )
            if (d != uiDocument)
                d.enabled = false;

        var root = uiDocument.rootVisualElement;
        root.style.display = DisplayStyle.Flex; // Ensure root is visible
        AppRoot = root.Q<VisualElement>("AppRoot");
        if (AppRoot != null)
        {
            AppRoot.style.fontSize = AppPrefs.LoadFontPx();
            AppRoot.style.display = DisplayStyle.Flex; // Ensure AppRoot is visible
        }
        else
        {
            Debug.LogError("[UIBootstrap] Missing AppRoot in BaseLayout UXML.");
            enabled = false;
            return;
        }

        AudioListener.volume = AppPrefs.LoadVolume() / 100f;

        var baseLayer = AppRoot.Q<VisualElement>("BaseLayer");
        var modalLayer = AppRoot.Q<VisualElement>("ModalLayer");
        var popupLayer = AppRoot.Q<VisualElement>("PopupLayer");
        var menuLayer = AppRoot.Q<VisualElement>("MenuLayer");
        var settingsLayer = AppRoot.Q<VisualElement>("SettingsLayer");

        if (
            baseLayer == null
            || modalLayer == null
            || popupLayer == null
            || menuLayer == null
            || settingsLayer == null
        )
        {
            Debug.LogError("[UIBootstrap] Missing layers in BaseLayout UXML.");
            enabled = false;
            return;
        }

        baseLayer.style.display = DisplayStyle.Flex;
        modalLayer.style.display = DisplayStyle.None;
        popupLayer.style.display = DisplayStyle.None;
        menuLayer.style.display = DisplayStyle.None;
        settingsLayer.style.display = DisplayStyle.None;

        baseLayer.pickingMode = PickingMode.Position;
        modalLayer.pickingMode = PickingMode.Ignore;
        popupLayer.pickingMode = PickingMode.Ignore;
        menuLayer.pickingMode = PickingMode.Ignore;
        settingsLayer.pickingMode = PickingMode.Ignore;

        ApplyGlobalFontPx(AppPrefs.LoadFontPx());

        var factory = new ViewFactory(uiDocument, uiAtlas);
        Router = new UIRouter(
            baseLayer,
            modalLayer,
            popupLayer,
            menuLayer,
            settingsLayer,
            factory,
            uiDocument
        );
    }

    /// <summary>
    /// Shows the initial screen based on onboarding settings.
    /// </summary>
    void Start()
    {
        if (forceOnboardingAlways)
        {
            OnboardingGate.SetForceAlways(true);
        }

        var showOnboarding =
            uiAtlas.OnboardingSet
            && OnboardingGate.ShouldShow(uiAtlas.OnboardingSet.ShowEveryNDays);

        Router.ShowScreen(showOnboarding ? ScreenState.Onboarding : ScreenState.Home);
    }

    /// <summary>
    /// Registers to the scene loaded event to force a layout/repaint.
    /// </summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_IOS && !UNITY_EDITOR
        // iOS: force a clean layout/repaint once
        uiDocument
            .rootVisualElement.schedule.Execute(() =>
            {
                uiDocument.rootVisualElement.MarkDirtyRepaint();
            })
            .StartingIn(0);
#endif
    }

    /// <summary>
    /// Unregisters the scene loaded event.
    /// </summary>
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Registers to the scene loaded event to force a layout/repaint.
    /// </summary>
    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // first frame after additive load: force a layout + repaint
        uiDocument
            .rootVisualElement.schedule.Execute(() =>
            {
                uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
                uiDocument.rootVisualElement.MarkDirtyRepaint();
            })
            .StartingIn(0);
    }

    /// <summary>
    /// Applies the global font size in pixels.
    /// </summary>
    /// <param name="px">The font size in pixels.</param>
    public void ApplyGlobalFontPx(int px)
    {
        var root = uiDocument.rootVisualElement;
        root.RemoveFromClassList("app-font-80");
        root.RemoveFromClassList("app-font-100");
        root.RemoveFromClassList("app-font-120");
        root.AddToClassList(
            px switch
            {
                80 => "app-font-80",
                120 => "app-font-120",
                _ => "app-font-100",
            }
        );
    }
}
