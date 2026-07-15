using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using UnoWebTemplate.Shared.Serialization;
using Windows.Foundation;

namespace UnoWebTemplate.Client.Widgets
{
    public sealed partial class ConnectivityWidget : UserControl
    {
        private readonly HttpClient _httpClient = new();
        private readonly DispatcherTimer _autoPingTimer = new();
        private readonly List<double> _latencyHistory = new();
        private readonly int _maxPoints = 15;
        
        private HubConnection _hubConnection;
        private bool _isConnectingSignalR;

        public ConnectivityWidget()
        {
            this.InitializeComponent();
            
            // Start pulsing animation
            PulseStoryboard.Begin();
            
            // Setup auto ping timer
            _autoPingTimer.Interval = TimeSpan.FromSeconds(2);
            _autoPingTimer.Tick += AutoPingTimer_Tick;

            // Handle resizing to redraw the sparkline dynamically
            GraphCanvas.SizeChanged += (s, e) => DrawSparkline();

            // Run initial status check
            _ = PerformPing(isHttp: true, silent: true);
        }

        private Uri GetBaseUri()
        {
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

        private async Task EnsureSignalRConnected()
        {
            if (_hubConnection != null)
            {
                if (_hubConnection.State == HubConnectionState.Disconnected && !_isConnectingSignalR)
                {
                    _isConnectingSignalR = true;
                    try
                    {
                        await _hubConnection.StartAsync();
                    }
                    finally
                    {
                        _isConnectingSignalR = false;
                    }
                }
                return;
            }

            var baseUri = GetBaseUri();
            var hubUri = new Uri(baseUri, "hubs/status");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUri)
                .WithAutomaticReconnect()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                })
                .Build();

            _isConnectingSignalR = true;
            try
            {
                await _hubConnection.StartAsync();
            }
            finally
            {
                _isConnectingSignalR = false;
            }
        }

        private async Task PerformPing(bool isHttp, bool silent = false)
        {
            if (!silent)
            {
                ConsoleOutput.Text = $"Querying backend via {(isHttp ? "HTTP" : "SignalR")}...";
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (isHttp)
                {
                    var baseUri = GetBaseUri();
                    var response = await _httpClient.GetStringAsync(new Uri(baseUri, "api/status"));
                    using var doc = JsonDocument.Parse(response);
                    var status = doc.RootElement.GetProperty("status").GetString();
                }
                else
                {
                    await EnsureSignalRConnected();
                    var result = await _hubConnection.InvokeAsync<JsonElement>("GetStatus");
                    var status = result.GetProperty("status").GetString();
                }

                stopwatch.Stop();
                double latency = stopwatch.Elapsed.TotalMilliseconds;
                
                // Update UI state to Healthy
                SetConnectionState(isConnected: true, $"{(isHttp ? "HTTP" : "SignalR")} Ping success: {latency:F0}ms");
                AddLatencyPoint(latency);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                SetConnectionState(isConnected: false, $"Error: {ex.Message}");
            }
        }

        private void SetConnectionState(bool isConnected, string message)
        {
            ConsoleOutput.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (isConnected)
            {
                SolidCircle.Fill = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                PulseCircle.Fill = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                IndicatorText.Text = "CONNECTED";
                IndicatorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            }
            else
            {
                SolidCircle.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PulseCircle.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
                IndicatorText.Text = "OFFLINE";
                IndicatorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        private void AddLatencyPoint(double latency)
        {
            _latencyHistory.Add(latency);
            if (_latencyHistory.Count > _maxPoints)
            {
                _latencyHistory.RemoveAt(0);
            }
            DrawSparkline();
        }

        private void DrawSparkline()
        {
            if (_latencyHistory.Count == 0) return;

            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;

            // If layout hasn't completed yet, defer drawing
            if (width <= 0 || height <= 0) return;

            double min = _latencyHistory.Min();
            double max = _latencyHistory.Max();
            double avg = _latencyHistory.Average();

            MinMaxText.Text = $"Min: {min:F0}ms  |  Max: {max:F0}ms  |  Avg: {avg:F0}ms";

            double range = max - min;
            if (range < 1.0) range = 1.0; // Avoid divide by zero

            var points = new PointCollection();
            double stepX = width / Math.Max(1, _latencyHistory.Count - 1);

            for (int i = 0; i < _latencyHistory.Count; i++)
            {
                double x = i * stepX;
                // Scale Y to leave a 10px margin at top and bottom
                double value = _latencyHistory[i];
                double relativeY = (value - min) / range;
                double y = height - 10 - (relativeY * (height - 20));
                
                points.Add(new Point(x, y));
            }

            LatencyLine.Points = points;
        }

        private async void HttpPingButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformPing(isHttp: true);
        }

        private async void SignalRPingButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformPing(isHttp: false);
        }

        private void AutoPingToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (AutoPingToggle.IsOn)
            {
                _autoPingTimer.Start();
                ConsoleOutput.Text = "Auto-ping started.";
            }
            else
            {
                _autoPingTimer.Stop();
                ConsoleOutput.Text = "Auto-ping stopped.";
            }
        }

        private async void AutoPingTimer_Tick(object sender, object e)
        {
            // Alternate between HTTP and SignalR pings to verify both pipelines
            bool useHttp = _latencyHistory.Count % 2 == 0;
            await PerformPing(isHttp: useHttp, silent: true);
        }
    }
}
