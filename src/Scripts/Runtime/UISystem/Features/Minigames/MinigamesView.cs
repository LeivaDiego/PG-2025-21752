using System;
using UnityEngine.UIElements;

/// <summary>
/// View for the Minigames screen in the UI system.
/// </summary>
public sealed class MinigamesView : IScreenView
{
    public VisualElement Root { get; }
    public event Action OnBreakout,
        OnTrivia,
        OnFlappy,
        OnExit;

    VisualElement breakout,
        trivia,
        flappy,
        exit;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinigamesView"/> class.
    /// </summary>
    /// <param name="root">The root visual element of the minigames view.</param>
    public MinigamesView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        breakout = Root.Q<VisualElement>("Breakout");
        trivia = Root.Q<VisualElement>("TriviaUVG");
        flappy = Root.Q<VisualElement>("FlappyJack");
        exit = Root.Q<VisualElement>("Exit");

        breakout?.RegisterCallback<ClickEvent>(_ => OnBreakout?.Invoke());
        trivia?.RegisterCallback<ClickEvent>(_ => OnTrivia?.Invoke());
        flappy?.RegisterCallback<ClickEvent>(_ => OnFlappy?.Invoke());
        exit?.RegisterCallback<ClickEvent>(_ => OnExit?.Invoke());
    }

    public void Unbind()
    {
        breakout?.UnregisterCallback<ClickEvent>(_ => OnBreakout?.Invoke());
        trivia?.UnregisterCallback<ClickEvent>(_ => OnTrivia?.Invoke());
        flappy?.UnregisterCallback<ClickEvent>(_ => OnFlappy?.Invoke());
        exit?.UnregisterCallback<ClickEvent>(_ => OnExit?.Invoke());
    }
}
