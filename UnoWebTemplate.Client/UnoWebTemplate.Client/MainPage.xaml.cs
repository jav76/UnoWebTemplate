using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using UnoWebTemplate.Shared.Serialization;

namespace UnoWebTemplate.Client;

public sealed partial class MainPage : Page
{
    private readonly HttpClient _httpClient = new();

    public MainPage()
    {
        this.InitializeComponent();
    }

    private Uri GetBaseUri()
    {
        var baseUri = new Uri("http://localhost:5000");
#if __WASM__
        var configuredApiUrl = Uno.Foundation.WebAssemblyRuntime.InvokeJS("window.UnoAppConfig?.apiUrl || ''");
        if (!string.IsNullOrEmpty(configuredApiUrl))
        {
            baseUri = new Uri(configuredApiUrl);
        }
        else
        {
            var location = Uno.Foundation.WebAssemblyRuntime.InvokeJS("window.location.origin");
            if (!string.IsNullOrEmpty(location))
            {
                baseUri = new Uri(location);
            }
        }
#endif
        return baseUri;
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double pageWidth = e.NewSize.Width;
        if (pageWidth <= 0) return;

        // Breakpoint at 760px page width
        if (pageWidth >= 760)
        {
            // 1. Configure Grid Definitions for Desktop (2 Columns, 2 Rows)
            DashboardGrid.ColumnDefinitions.Clear();
            DashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            DashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            DashboardGrid.RowDefinitions.Clear();
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 2. Position ConnectivityCard (Column 0, Row 0)
            Grid.SetRow(ConnectivityCard, 0);
            Grid.SetColumn(ConnectivityCard, 0);
            ConnectivityCard.Margin = new Thickness(0, 0, 16, 16);

            // 3. Position TiltCard (Column 0, Row 1)
            Grid.SetRow(TiltCard, 1);
            Grid.SetColumn(TiltCard, 0);
            TiltCard.Margin = new Thickness(0, 0, 16, 0);

            // 4. Position ParticleCard (Column 1, Row 0, spanning 2 Rows)
            Grid.SetRow(ParticleCard, 0);
            Grid.SetColumn(ParticleCard, 1);
            Grid.SetRowSpan(ParticleCard, 2);
            ParticleCard.Height = 410;

            // 5. Adjust bottom info layout
            InfoGrid.ColumnDefinitions.Clear();
            InfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(420) });
            InfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            InfoGrid.RowDefinitions.Clear();
            InfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(TechStackPanel, 0);
            Grid.SetRow(TechStackPanel, 0);
            TechStackPanel.Margin = new Thickness(0);

            Grid.SetColumn(DiagnosticsPanel, 1);
            Grid.SetRow(DiagnosticsPanel, 0);
            DiagnosticsPanel.Margin = new Thickness(40, 0, 0, 0);
        }
        else
        {
            // 1. Configure Grid Definitions for Mobile (1 Column, 3 Rows)
            DashboardGrid.ColumnDefinitions.Clear();
            DashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            DashboardGrid.RowDefinitions.Clear();
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 2. Position ConnectivityCard (Row 0)
            Grid.SetRow(ConnectivityCard, 0);
            Grid.SetColumn(ConnectivityCard, 0);
            ConnectivityCard.Margin = new Thickness(0, 0, 0, 16);

            // 3. Position TiltCard (Row 1)
            Grid.SetRow(TiltCard, 1);
            Grid.SetColumn(TiltCard, 0);
            TiltCard.Margin = new Thickness(0, 0, 0, 16);

            // 4. Position ParticleCard (Row 2)
            Grid.SetRow(ParticleCard, 2);
            Grid.SetColumn(ParticleCard, 0);
            Grid.SetRowSpan(ParticleCard, 1);
            ParticleCard.Height = 380;

            // 5. Adjust bottom info layout
            InfoGrid.ColumnDefinitions.Clear();
            InfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            InfoGrid.RowDefinitions.Clear();
            InfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            InfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(TechStackPanel, 0);
            Grid.SetRow(TechStackPanel, 0);
            TechStackPanel.Margin = new Thickness(0);

            Grid.SetColumn(DiagnosticsPanel, 0);
            Grid.SetRow(DiagnosticsPanel, 1);
            DiagnosticsPanel.Margin = new Thickness(0, 20, 0, 0);
        }
    }

    private async void StatusButtonHttp_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Querying API server via HTTP...";
        try
        {
            var baseUri = GetBaseUri();
            var response = await _httpClient.GetStringAsync(new Uri(baseUri, "api/status"));
            using var doc = JsonDocument.Parse(response);
            var status = doc.RootElement.GetProperty("status").GetString();
            var time = doc.RootElement.GetProperty("timestamp").GetString();

            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            StatusText.Text = $"Backend Status (HTTP): {status} ({time})";
        }
        catch (Exception ex)
        {
            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void StatusButtonSignalR_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Connecting to SignalR hub...";
        try
        {
            var baseUri = GetBaseUri();
            var hubUri = new Uri(baseUri, "hubs/status");

            await using var connection = new HubConnectionBuilder()
                .WithUrl(hubUri)
                .WithAutomaticReconnect()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                })
                .Build();

            await connection.StartAsync();

            StatusText.Text = "Querying API server via SignalR...";

            var result = await connection.InvokeAsync<JsonElement>("GetStatus");
            var status = result.GetProperty("status").GetString();
            var time = result.GetProperty("timestamp").GetString();

            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            StatusText.Text = $"Backend Status (SignalR): {status} ({time})";

            await connection.StopAsync();
        }
        catch (Exception ex)
        {
            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}

