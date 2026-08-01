using Godot;
using System;
using System.Collections.Generic;

public partial class AppRoot : Control
{
    private readonly Dictionary<AppRoute, Button> _navigationButtons = new();
    private NavigationService _navigationService = null!;

    public override void _Ready()
    {
        GD.Print("GameLexicon AppRoot initialized.");

        var routeHost = GetNodeOrNull<Control>("AppLayout/ContentHost/RouteHost")
            ?? throw new InvalidOperationException("RouteHost is missing from App.tscn.");

        _navigationService = new NavigationService(routeHost);
        RegisterRoutes(_navigationService);
        RegisterButtons();
        Navigate(AppRoute.Dashboard);

        GD.Print("GameLexicon navigation initialized.");
    }

    private static void RegisterRoutes(NavigationService navigationService)
    {
        navigationService.Register(AppRoute.Dashboard, LoadPage("res://scenes/dashboard/DashboardView.tscn"));
        navigationService.Register(AppRoute.CaptureInbox, LoadPage("res://scenes/capture_inbox/CaptureInboxView.tscn"));
        navigationService.Register(AppRoute.Library, LoadPage("res://scenes/library/LibraryView.tscn"));
        navigationService.Register(AppRoute.Review, LoadPage("res://scenes/review/ReviewView.tscn"));
        navigationService.Register(AppRoute.Statistics, LoadPage("res://scenes/statistics/StatisticsView.tscn"));
        navigationService.Register(AppRoute.Settings, LoadPage("res://scenes/settings/SettingsView.tscn"));
    }

    private void RegisterButtons()
    {
        RegisterButton(AppRoute.Dashboard, "AppLayout/Sidebar/SidebarMargin/SidebarContent/NavigationList/DashboardButton");
        RegisterButton(AppRoute.CaptureInbox, "AppLayout/Sidebar/SidebarMargin/SidebarContent/NavigationList/CaptureInboxButton");
        RegisterButton(AppRoute.Library, "AppLayout/Sidebar/SidebarMargin/SidebarContent/NavigationList/LibraryButton");
        RegisterButton(AppRoute.Review, "AppLayout/Sidebar/SidebarMargin/SidebarContent/NavigationList/ReviewButton");
        RegisterButton(AppRoute.Statistics, "AppLayout/Sidebar/SidebarMargin/SidebarContent/NavigationList/StatisticsButton");
        RegisterButton(AppRoute.Settings, "AppLayout/Sidebar/SidebarMargin/SidebarContent/NavigationList/SettingsButton");
    }

    private void RegisterButton(AppRoute route, NodePath path)
    {
        var button = GetNodeOrNull<Button>(path)
            ?? throw new InvalidOperationException($"Navigation button for route '{route}' is missing.");

        _navigationButtons.Add(route, button);
        button.Pressed += () => Navigate(route);
    }

    private void Navigate(AppRoute route)
    {
        if (_navigationService is null)
        {
            throw new InvalidOperationException("NavigationService has not been initialized.");
        }

        _navigationService.Navigate(route);

        foreach (var (buttonRoute, button) in _navigationButtons)
        {
            button.ButtonPressed = buttonRoute == route;
        }
    }

    private static PackedScene LoadPage(string path)
    {
        return GD.Load<PackedScene>(path)
            ?? throw new InvalidOperationException($"Unable to load navigation page '{path}'.");
    }
}
