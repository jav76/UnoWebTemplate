using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace UnoWebTemplate.Client.Widgets
{
    public sealed partial class TiltCardWidget : UserControl
    {
        private const double MaxSkewAngle = 11.0;   // Max skew angle simulating 3D rotation
        private const double HoverSkewAngle = 2.5;  // Subtle skew hint on hover
        private const double LerpFactor = 0.12;     // Damping/inertia coefficient

        private double _targetSkewX = 0;
        private double _targetSkewY = 0;
        private double _targetScaleX = 1.0;
        private double _targetScaleY = 1.0;
        private double _targetSheenOpacity = 0;

        private double _targetSheenStartX = 0.0;
        private double _targetSheenStartY = 0.0;
        private double _targetSheenEndX = 1.0;
        private double _targetSheenEndY = 1.0;

        private bool _isLoopActive = false;
        private bool _isPressed = false;

        public TiltCardWidget()
        {
            this.InitializeComponent();
        }

        private void StartLoop()
        {
            if (!_isLoopActive)
            {
                _isLoopActive = true;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private void StopLoop()
        {
            if (_isLoopActive)
            {
                CompositionTarget.Rendering -= OnRendering;
                _isLoopActive = false;
            }
        }

        private void OnRendering(object sender, object e)
        {
            // Lerp Skew angles (simulating X/Y 3D rotations)
            double currentSkewX = CardTransform.SkewX;
            double currentSkewY = CardTransform.SkewY;
            double nextSkewX = currentSkewX + (_targetSkewX - currentSkewX) * LerpFactor;
            double nextSkewY = currentSkewY + (_targetSkewY - currentSkewY) * LerpFactor;
            CardTransform.SkewX = nextSkewX;
            CardTransform.SkewY = nextSkewY;

            // Lerp Scale (simulating Z depth change on click/hover)
            double currentScaleX = CardTransform.ScaleX;
            double currentScaleY = CardTransform.ScaleY;
            double nextScaleX = currentScaleX + (_targetScaleX - currentScaleX) * LerpFactor;
            double nextScaleY = currentScaleY + (_targetScaleY - currentScaleY) * LerpFactor;
            CardTransform.ScaleX = nextScaleX;
            CardTransform.ScaleY = nextScaleY;

            // Lerp sheen opacity
            double currentOpacity = SheenOverlay.Opacity;
            double nextOpacity = currentOpacity + (_targetSheenOpacity - currentOpacity) * LerpFactor;
            SheenOverlay.Opacity = nextOpacity;

            // Lerp sheen gradient coordinates
            Point currentStart = SheenBrush.StartPoint;
            Point currentEnd = SheenBrush.EndPoint;
            double nextStartX = currentStart.X + (_targetSheenStartX - currentStart.X) * LerpFactor;
            double nextStartY = currentStart.Y + (_targetSheenStartY - currentStart.Y) * LerpFactor;
            double nextEndX = currentEnd.X + (_targetSheenEndX - currentEnd.X) * LerpFactor;
            double nextEndY = currentEnd.Y + (_targetSheenEndY - currentEnd.Y) * LerpFactor;

            SheenBrush.StartPoint = new Point(nextStartX, nextStartY);
            SheenBrush.EndPoint = new Point(nextEndX, nextEndY);

            // Optimization: Stop loop when elements settle close to target
            if (Math.Abs(_targetSkewX - nextSkewX) < 0.01 &&
                Math.Abs(_targetSkewY - nextSkewY) < 0.01 &&
                Math.Abs(_targetScaleX - nextScaleX) < 0.002 &&
                Math.Abs(_targetSheenOpacity - nextOpacity) < 0.005)
            {
                CardTransform.SkewX = _targetSkewX;
                CardTransform.SkewY = _targetSkewY;
                CardTransform.ScaleX = _targetScaleX;
                CardTransform.ScaleY = _targetScaleY;
                SheenOverlay.Opacity = _targetSheenOpacity;

                StopLoop();
            }
        }

        private void UpdateTargets(Point position)
        {
            double w = CardRoot.ActualWidth;
            double h = CardRoot.ActualHeight;

            if (w <= 0 || h <= 0) return;

            // Normalize coordinate offsets from card center: ranges from -0.5 to 0.5
            double dx = (position.X / w) - 0.5;
            double dy = (position.Y / h) - 0.5;

            // Clamp offsets to prevent layout clipping at extreme drag distances
            dx = Math.Clamp(dx, -0.7, 0.7);
            dy = Math.Clamp(dy, -0.7, 0.7);

            if (_isPressed)
            {
                // clicked and dragged: depress/recess card slightly and apply large skew angles
                _targetSkewX = -dy * 2.0 * MaxSkewAngle;
                _targetSkewY = dx * 2.0 * MaxSkewAngle;
                _targetScaleX = 0.94;
                _targetScaleY = 0.94;
                _targetSheenOpacity = 0.65; // Bright glare
            }
            else
            {
                // Hovering: pop card out slightly and apply subtle tilt hints
                _targetSkewX = -dy * 2.0 * HoverSkewAngle;
                _targetSkewY = dx * 2.0 * HoverSkewAngle;
                _targetScaleX = 1.03;
                _targetScaleY = 1.03;
                _targetSheenOpacity = 0.18; // Soft glare
            }

            // Translate reflection glare spotlight in opposition to the drag offset
            _targetSheenStartX = -dx;
            _targetSheenStartY = -dy;
            _targetSheenEndX = 1.0 - dx;
            _targetSheenEndY = 1.0 - dy;
        }

        private void CardRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPressed)
            {
                _targetScaleX = 1.03;
                _targetScaleY = 1.03;
                _targetSheenOpacity = 0.18;
                StartLoop();
            }
        }

        private void CardRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint(CardRoot).Position;
            UpdateTargets(position);
            StartLoop();
        }

        private void CardRoot_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPressed)
            {
                // Reset to default flat state
                _targetSkewX = 0;
                _targetSkewY = 0;
                _targetScaleX = 1.0;
                _targetScaleY = 1.0;
                _targetSheenOpacity = 0;

                _targetSheenStartX = 0.0;
                _targetSheenStartY = 0.0;
                _targetSheenEndX = 1.0;
                _targetSheenEndY = 1.0;

                StartLoop();
            }
        }

        private void CardRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isPressed = true;
            CardRoot.CapturePointer(e.Pointer);

            var position = e.GetCurrentPoint(CardRoot).Position;
            UpdateTargets(position);
            StartLoop();
        }

        private void CardRoot_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isPressed)
            {
                _isPressed = false;
                CardRoot.ReleasePointerCapture(e.Pointer);

                var position = e.GetCurrentPoint(CardRoot).Position;
                double w = CardRoot.ActualWidth;
                double h = CardRoot.ActualHeight;

                // Check if letting go inside or outside card
                if (position.X >= 0 && position.X <= w && position.Y >= 0 && position.Y <= h)
                {
                    // Snap back to normal hover targets
                    UpdateTargets(position);
                }
                else
                {
                    // Snap back to flat state
                    _targetSkewX = 0;
                    _targetSkewY = 0;
                    _targetScaleX = 1.0;
                    _targetScaleY = 1.0;
                    _targetSheenOpacity = 0;

                    _targetSheenStartX = 0.0;
                    _targetSheenStartY = 0.0;
                    _targetSheenEndX = 1.0;
                    _targetSheenEndY = 1.0;
                }

                StartLoop();
            }
        }

        private void CardRoot_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isPressed = false;

            // Reset back to flat state
            _targetSkewX = 0;
            _targetSkewY = 0;
            _targetScaleX = 1.0;
            _targetScaleY = 1.0;
            _targetSheenOpacity = 0;

            _targetSheenStartX = 0.0;
            _targetSheenStartY = 0.0;
            _targetSheenEndX = 1.0;
            _targetSheenEndY = 1.0;

            StartLoop();
        }
    }
}
