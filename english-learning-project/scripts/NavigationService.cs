using System;
using System.Collections.Generic;
using Godot;

public sealed class NavigationService
{
    private readonly Control _routeHost;
    private readonly Dictionary<AppRoute, PackedScene> _scenes = new();
    private readonly Dictionary<AppRoute, Control> _pages = new();

    public NavigationService(Control routeHost)
    {
        _routeHost = routeHost ?? throw new ArgumentNullException(nameof(routeHost));
    }

    public AppRoute? CurrentRoute { get; private set; }

    public void Register(AppRoute route, PackedScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (!_scenes.TryAdd(route, scene))
        {
            throw new InvalidOperationException($"Route '{route}' is already registered.");
        }
    }

    public Control Navigate(AppRoute route)
    {
        if (CurrentRoute == route && _pages.TryGetValue(route, out var currentPage))
        {
            return currentPage;
        }

        if (!_scenes.TryGetValue(route, out var scene))
        {
            throw new InvalidOperationException($"Route '{route}' is not registered.");
        }

        if (!_pages.TryGetValue(route, out var page))
        {
            var instance = scene.Instantiate();
            if (instance is not Control control)
            {
                instance.Free();
                throw new InvalidOperationException(
                    $"The root node for route '{route}' must inherit Control.");
            }

            page = control;

            page.Visible = false;
            _routeHost.AddChild(page);
            _pages.Add(route, page);
        }

        foreach (var cachedPage in _pages.Values)
        {
            cachedPage.Visible = ReferenceEquals(cachedPage, page);
        }

        CurrentRoute = route;
        GD.Print($"Navigated to: {route}");
        return page;
    }
}
