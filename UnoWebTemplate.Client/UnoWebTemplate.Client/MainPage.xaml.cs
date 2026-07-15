using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace UnoWebTemplate.Client;

public partial class MainPage : Page
{
    private readonly HttpClient _httpClient = new();

    public MainPage()
    {
        this.InitializeComponent();
    }

    private Uri GetBaseUri()
    {
        // Calculate base address dynamically based on window location in WASM, or fallback to localhost
        var baseUri = new Uri("http://localhost:8080");
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
