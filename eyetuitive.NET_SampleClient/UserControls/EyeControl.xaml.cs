using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SampleClient.UserControls
{
    public partial class EyeControl : UserControl
    {
        public EyeControl()
        {
            InitializeComponent();
        }

        public void UpdateConfidence(double confidence)
        {
            ConfidenceScore.Content = $"{confidence:P0}";

            // Clamp confidence
            confidence = Math.Max(0.3, Math.Min(1.0, confidence));

            double normalizedConfidence = (confidence - 0.5) / 0.5;

            // Interpolate between red and green
            byte red = (byte)(255 * (1 - normalizedConfidence));   // 255 at 0.5, 0 at 1.0
            byte green = (byte)(255 * normalizedConfidence);       // 0 at 0.5, 255 at 1.0
            byte blue = 0;

            Color confidenceColor = Color.FromRgb(red, green, blue);
            SolidColorBrush confidenceBrush = new SolidColorBrush(confidenceColor);

            IrisPupil.Stroke = confidenceBrush;
        }

        public void SetEyeState(bool isOpen)
        {
            if (isOpen)
            {
                IrisPupil.Visibility = Visibility.Visible;
            }
            else
            {
                IrisPupil.Visibility = Visibility.Hidden;
            }
        }
    }
}
