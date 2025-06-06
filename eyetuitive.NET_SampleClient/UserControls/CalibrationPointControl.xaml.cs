using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Input;

namespace SampleClient.UserControls
{
    /// <summary>
    /// CalibrationPointControl is a user control that represents a single calibration point in the calibration process
    /// </summary>
    public partial class CalibrationPointControl : UserControl
    {
        private bool _isSelected = false;

        /// <summary>
        /// Sequence number of the calibration point, used to identify the point
        /// </summary>
        internal int Sequence { get; } = -1;

        internal bool Shown = false;

        public bool Selected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                UpdateSelectionVisual();
            }
        }

        /// <summary>
        /// Ctor with sequence number
        /// </summary>
        /// <param name="sequence"></param>
        public CalibrationPointControl(int sequence)
        {
            InitializeComponent();
            Sequence = sequence;
        }

        /// <summary>
        /// Shows the calibration point control with an animation from a specified start position to a target position
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="targetX"></param>
        /// <param name="targetY"></param>
        public void Show(double startX, double startY, double targetX, double targetY)
        {
            if (Shown) return;

            var showAnimation = (Storyboard)FindResource("ShowAnimation");

            var showAnimationX = (DoubleAnimation)showAnimation.Children[1];
            showAnimationX.From = startX;
            showAnimationX.To = targetX;

            var showAnimationY = (DoubleAnimation)showAnimation.Children[2];
            showAnimationY.From = startY;
            showAnimationY.To = targetY;

            showAnimation.Begin(this);
            Shown = true;
        }

        /// <summary>
        /// Collecting animation to indicate the calibration point is being processed
        /// </summary>
        public void Collecting()
        {
            var collectingAnimation = (Storyboard)FindResource("CollectingAnimation");
            collectingAnimation.Begin(this);
        }

        /// <summary>
        /// Hides the calibration point control with an animation
        /// </summary>
        public void Hide()
        {
            var hideAnimation = (Storyboard)FindResource("HideAnimation");
            hideAnimation.Begin(this);
        }

        /// <summary>
        /// Shows the calibration result with percentage and color-coded background
        /// </summary>
        /// <param name="percentage">Percentage value (0-100)</param>
        /// <param name="showAnimation">Whether to animate the appearance of the result</param>
        public void ShowResult(double percentage, bool showAnimation = true)
        {
            // Clamp percentage to valid range
            percentage = Math.Max(0, Math.Min(100, percentage));

            // Update the label text
            ResultLabel.Content = $"{percentage:F0}%";

            // Set color based on percentage
            Brush fillBrush;
            if (percentage >= 80)
            {
                fillBrush = new SolidColorBrush(Colors.Green);
            }
            else if (percentage >= 60)
            {
                fillBrush = new SolidColorBrush(Colors.Orange);
            }
            else if (percentage >= 40)
            {
                fillBrush = new SolidColorBrush(Colors.Red);
            }
            else
            {
                fillBrush = new SolidColorBrush(Colors.DarkRed);
            }

            ResultEllipse.Fill = fillBrush;

            if (showAnimation)
            {
                var showResultAnimation = (Storyboard)FindResource("ShowResultAnimation");
                showResultAnimation.Begin(this);
            }
            else
            {
                ResultEllipse.Opacity = 1;
                ResultLabel.Opacity = 1;
            }
        }

        /// <summary>
        /// Hides the calibration result
        /// </summary>
        /// <param name="hideAnimation">Whether to animate the hiding of the result</param>
        public void HideResult(bool hideAnimation = true)
        {
            if (hideAnimation)
            {
                var hideResultAnimation = (Storyboard)FindResource("HideResultAnimation");
                hideResultAnimation.Begin(this);
            }
            else
            {
                ResultEllipse.Opacity = 0;
                ResultLabel.Opacity = 0;
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only handle clicks when results are visible
            if (ResultEllipse.Opacity > 0)
            {
                Selected = !Selected; // Toggle selection
            }
        }

        private void UpdateSelectionVisual()
        {
            if (_isSelected)
            {
                SelectionRing.Opacity = 1;
                // Optional: Add pulsing animation for selected state
                var pulseAnimation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(0.8),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                SelectionRing.BeginAnimation(OpacityProperty, pulseAnimation);
            }
            else
            {
                SelectionRing.BeginAnimation(OpacityProperty, null); // Stop animation
                SelectionRing.Opacity = 0;
            }
        }

        /// <summary>
        /// Resets the calibration point control to its initial state
        /// </summary>
        public void Reset()
        {
            //Cancel all animations
            var storyboard = (Storyboard)FindResource("ShowAnimation");
            storyboard.Stop(this);
            storyboard = (Storyboard)FindResource("HideAnimation");
            storyboard.Stop(this);
            storyboard = (Storyboard)FindResource("CollectingAnimation");
            storyboard.Stop(this);
            storyboard = (Storyboard)FindResource("ShowResultAnimation");
            storyboard.Stop(this);
            HideResult(true);

            Shown = false;
            Selected = false;
            ResultEllipse.Opacity = 0;
            ResultLabel.Opacity = 0;
            SelectionRing.Opacity = 0;
            ResultLabel.Content = string.Empty;
            Point.Opacity = 1; // Ensure point is visible after reset
            Point.RenderTransform = new TranslateTransform(0, 0); // Reset position

        }
    }
}
