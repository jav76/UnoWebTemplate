using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoWebTemplate.Client;

public partial class MainPage : Page
{
    private readonly HttpClient _httpClient = new();

    public MainPage()
    {
        this.InitializeComponent();
    }

    private async void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Querying API server...";
        try
        {
            // Calculate base address dynamically based on window location in WASM, or fallback to localhost
            var baseUri = new Uri("http://localhost:8080");
#if __WASM__
            var location = Uno.Foundation.WebAssemblyRuntime.InvokeJS("window.location.origin");
            if (!string.IsNullOrEmpty(location))
            {
                baseUri = new Uri(location);
            }
#endif
            var response = await _httpClient.GetStringAsync(new Uri(baseUri, "api/status"));
            using var doc = JsonDocument.Parse(response);
            var status = doc.RootElement.GetProperty("status").GetString();
            var time = doc.RootElement.GetProperty("timestamp").GetString();

            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            StatusText.Text = $"Backend Status: {status} ({time})";
        }
        catch (Exception ex)
        {
            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
