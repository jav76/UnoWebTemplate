using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using Windows.Foundation;

namespace UnoWebTemplate.Client.Widgets
{
    public sealed partial class ParticleSandboxWidget : UserControl
    {
        private class Particle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Vx { get; set; }
            public double Vy { get; set; }
            public double Radius { get; set; }
            public double MaxVelocity { get; set; } = 5.0;
            public double FadingSpeed { get; set; } = 0.0; // Decay rate per frame (0 for permanent)
            public double Opacity { get; set; } = 1.0;
            public Ellipse Shape { get; set; }
        }

        private readonly List<Particle> _particles = new();
        private readonly Random _random = new();
        private Point _mousePosition;
        private bool _isMouseOver = false;
        private bool _isInitialized = false;

        // Curated neon emerald/teal palette
        private readonly Windows.UI.Color[] _palette = new[]
        {
            Microsoft.UI.Colors.Teal,
            Windows.UI.Color.FromArgb(255, 16, 185, 129),   // #10B981 (Emerald)
            Windows.UI.Color.FromArgb(255, 52, 211, 153),   // #34D399 (Teal light)
            Windows.UI.Color.FromArgb(255, 110, 231, 183),  // #6EE7B7 (Teal lighter)
            Windows.UI.Color.FromArgb(255, 5, 150, 105),    // #059669 (Emerald dark)
        };

        public ParticleSandboxWidget()
        {
            this.InitializeComponent();
            
            this.Loaded += ParticleSandboxWidget_Loaded;
            this.Unloaded += ParticleSandboxWidget_Unloaded;
        }

        private void ParticleSandboxWidget_Loaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += OnRendering;
        }

        private void ParticleSandboxWidget_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void InitializeParticles(double width, double height)
        {
            if (_isInitialized) return;
            if (width <= 0 || height <= 0) return;

            int initialCount = 65;
            for (int i = 0; i < initialCount; i++)
            {
                SpawnParticle(
                    x: _random.NextDouble() * width,
                    y: _random.NextDouble() * height,
                    vx: (_random.NextDouble() * 2.4) - 1.2,
                    vy: (_random.NextDouble() * 2.4) - 1.2,
                    radius: 3.0 + _random.NextDouble() * 3.5,
                    fadingSpeed: 0.0
                );
            }

            _isInitialized = true;
            UpdateParticleCountText();
        }

        private void SpawnParticle(double x, double y, double vx, double vy, double radius, double fadingSpeed)
        {
            // Pick a random color from our emerald palette
            var color = _palette[_random.Next(_palette.Length)];
            
            var ellipse = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(color),
                Opacity = fadingSpeed > 0 ? 1.0 : 0.6 + _random.NextDouble() * 0.4
            };

            var p = new Particle
            {
                X = x,
                Y = y,
                Vx = vx,
                Vy = vy,
                Radius = radius,
                FadingSpeed = fadingSpeed,
                Opacity = ellipse.Opacity,
                Shape = ellipse
            };

            ParticleCanvas.Children.Add(ellipse);
            _particles.Add(p);

            // Initial positioning
            Canvas.SetLeft(ellipse, x - radius);
            Canvas.SetTop(ellipse, y - radius);
        }

        private void OnRendering(object sender, object e)
        {
            double w = ParticleCanvas.ActualWidth;
            double h = ParticleCanvas.ActualHeight;

            if (w <= 0 || h <= 0) return;

            // Lazy initialization once layout size is valid
            if (!_isInitialized)
            {
                InitializeParticles(w, h);
                return;
            }

            // Loop backwards to safely delete decaying particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];

                // 1. Hover Gravitational Attraction
                if (_isMouseOver)
                {
                    double dx = _mousePosition.X - p.X;
                    double dy = _mousePosition.Y - p.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 140.0 && dist > 1.0)
                    {
                        // Strength is inversely proportional to distance
                        double strength = (140.0 - dist) / 140.0 * 0.18;
                        p.Vx += (dx / dist) * strength;
                        p.Vy += (dy / dist) * strength;
                    }
                }

                // 2. Physics Integrator & Damping Friction
                p.Vx *= 0.982; // Dynamic drag
                p.Vy *= 0.982;

                // Drift speed floor to prevent complete stagnation
                double currentSpeed = Math.Sqrt(p.Vx * p.Vx + p.Vy * p.Vy);
                if (currentSpeed < 0.25 && p.FadingSpeed == 0)
                {
                    double angle = _random.NextDouble() * Math.PI * 2.0;
                    p.Vx = Math.Cos(angle) * 0.4;
                    p.Vy = Math.Sin(angle) * 0.4;
                }

                // Cap maximum velocity
                if (currentSpeed > p.MaxVelocity)
                {
                    p.Vx = (p.Vx / currentSpeed) * p.MaxVelocity;
                    p.Vy = (p.Vy / currentSpeed) * p.MaxVelocity;
                }

                p.X += p.Vx;
                p.Y += p.Vy;

                // 3. Boundary Collisions (bounce off edges)
                if (p.X - p.Radius < 0)
                {
                    p.X = p.Radius;
                    p.Vx = -p.Vx * 0.8; // Dampen energy slightly on bounce
                }
                else if (p.X + p.Radius > w)
                {
                    p.X = w - p.Radius;
                    p.Vx = -p.Vx * 0.8;
                }

                if (p.Y - p.Radius < 0)
                {
                    p.Y = p.Radius;
                    p.Vy = -p.Vy * 0.8;
                }
                else if (p.Y + p.Radius > h)
                {
                    p.Y = h - p.Radius;
                    p.Vy = -p.Vy * 0.8;
                }

                // 4. Decay (explosion particles fade out and self-destruct)
                if (p.FadingSpeed > 0)
                {
                    p.Opacity -= p.FadingSpeed;
                    if (p.Opacity <= 0)
                    {
                        ParticleCanvas.Children.Remove(p.Shape);
                        _particles.RemoveAt(i);
                        continue;
                    }
                    p.Shape.Opacity = p.Opacity;
                }

                // 5. Update Canvas Node Position
                Canvas.SetLeft(p.Shape, p.X - p.Radius);
                Canvas.SetTop(p.Shape, p.Y - p.Radius);
            }

            UpdateParticleCountText();
        }

        private void UpdateParticleCountText()
        {
            ParticleCountText.Text = $"Particles: {_particles.Count}";
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _mousePosition = e.GetCurrentPoint(ParticleCanvas).Position;
        }

        private void Canvas_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isMouseOver = true;
            _mousePosition = e.GetCurrentPoint(ParticleCanvas).Position;
        }

        private void Canvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isMouseOver = false;
        }

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isMouseOver = true;
            var pos = e.GetCurrentPoint(ParticleCanvas).Position;
            _mousePosition = pos;

            GestureGrid.CapturePointer(e.Pointer);

            // Click Burst: Spawn 28 tiny fast explosion particles decaying over ~40-70 frames
            int explosionCount = 28;
            for (int i = 0; i < explosionCount; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                double speed = 1.5 + _random.NextDouble() * 5.0;
                double vx = Math.Cos(angle) * speed;
                double vy = Math.Sin(angle) * speed;
                double radius = 1.5 + _random.NextDouble() * 2.2;
                double decay = 0.012 + _random.NextDouble() * 0.015;

                SpawnParticle(pos.X, pos.Y, vx, vy, radius, decay);
            }

            UpdateParticleCountText();
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isMouseOver = false;
            GestureGrid.ReleasePointerCapture(e.Pointer);
        }

        private void Canvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isMouseOver = false;
        }
    }
}
