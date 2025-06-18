using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using eyetuitive.NET_SampleClient;
using GazeFirst;
using Microsoft.Extensions.Logging;
using SampleClient.UserControls;
using Serilog;
using Serilog.Extensions.Logging;

namespace SampleClient;

/// <summary>
/// MainWindow with all logic for the eyetuitive.NET sample client
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Device instance for the GazeFirst eyetuitive eye tracker
    /// </summary>
    private GazeFirst.eyetuitive device = new GazeFirst.eyetuitive();

    private Serilog.Core.Logger logger;
    private int ScreenResWidth, ScreenResHeight;
    private double ScreenSizeWidthMM, ScreenSizeHeightMM;
    private Task ConnectionTask, CalibTask;
    private bool isStreaming = false, isConnected = false;

    /// <summary>
    /// Constructor for MainWindow
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        InitLogging();

        (ScreenResWidth, ScreenResHeight) = MonitorInfo.GetPrimaryScreenResolution();
        (ScreenSizeWidthMM, ScreenSizeHeightMM) = MonitorInfo.GetPrimaryScreenSizeInMillimeters();

        Top = 0;
        Left = 0;
        mainCanvas.Width = ScreenResWidth;
        mainCanvas.Height = ScreenResHeight;

        ConnectionTask = Task.Run(Connect);
    }

    /// <summary>
    /// Initialize logging using Serilog to output logs to a RichTextBox
    /// </summary>
    private void InitLogging()
    {
        // Configure Serilog
        logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // Set minimum log level to Debug, can be adjusted as needed
            .WriteTo.RichTextBox(LogBox)
            .CreateLogger();

        ILoggerFactory loggerFactory = new SerilogLoggerFactory(logger);
        //Attach the logger to the eyetuitive library
        GazeFirst.eyetuitive.AttachLogger(loggerFactory);
    }

    /// <summary>
    /// Window loaded event handler to set up the canvas scaling based on DPI settings
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var source = PresentationSource.FromVisual(this);
        Matrix m = source.CompositionTarget.TransformToDevice;

        double DpiWidthFactor = m.M11;
        double DpiHeightFactor = m.M22;

        double scaleX = 1 / DpiWidthFactor;
        double scaleY = 1 / DpiHeightFactor;

        mainCanvas.LayoutTransform = new ScaleTransform(scaleX, scaleY);
    }

    /// <summary>
    /// Exit button click event handler to close the application
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        if (isConnected)
        {
            device.Position.StopPositionTracking(posHandler);
            device.Gaze.StopGazeTracking(gazeHandler);
        }
        device.Dispose();
        Close();
    }

    /// <summary>
    /// Helper method to place a UI element on the canvas at a specified position
    /// </summary>
    /// <param name="uIElement"></param>
    /// <param name="pos"></param>
    /// <param name="scale"></param>
    private void placeElementOnCanvas(FrameworkElement uIElement, NormedPoint2d pos, bool scale = true)
    {
        if (pos.X != 0d && pos.Y != 0d)
        {
            Dispatcher.Invoke(() =>
            {
                uIElement.Visibility = Visibility.Visible;
                double X = pos.X * (scale ? ScreenResWidth : 1d);
                double Y = pos.Y * (scale ? ScreenResHeight : 1d);
                //we need to move pos to the center of the element
                X -= uIElement.Width / 2d;
                Y -= uIElement.Height / 2d;
                Canvas.SetLeft(uIElement, X);
                Canvas.SetTop(uIElement, Y);
            });
        }
    }

    /// <summary>
    /// Connect to the eye tracker
    /// </summary>
    private async Task Connect()
    {
        bool avail = GazeFirst.eyetuitive.IsAvailable();
        logger.Information("Connecting, eyetuitive IsAvailable: {avail}", avail);
        device.ConnectedChanged += ConnectedChanged;
        if (avail)
        {
            await ConnectToDeviceAsync();
        }
        else 
            logger.Warning("No eyetuitive available to connect.");        
    }

    /// <summary>
    /// Connect to the eyetuitive device asynchronously and start tracking position and gaze data
    /// </summary>
    /// <returns></returns>
    private async Task ConnectToDeviceAsync()
    {
        bool connected = await device.ConnectAsync(15); //let's wait up to 15 seconds for the device to connect
        if (connected)
        {
            device.Position.StartPositionTracking(posHandler);
            device.Gaze.StartGazeTracking(gazeHandler);
            isConnected = true;
            logger.Information("Connected to eyetuitive device successfully.");
        }
        else
            logger.Error("Failed to connect to eyetuitive device.");
    }

    /// <summary>
    /// Connected changed event handler to update the UI based on connection status
    /// </summary>
    /// <param name="obj"></param>
    private void ConnectedChanged(bool connected)
    {
        logger.Information("Got Connected changed event: {connected}", connected);
        if(!isConnected && connected)
        {
            Task.Run(ConnectToDeviceAsync); // Start the connection in a separate task to avoid blocking
        }
    }

    /// <summary>
    /// Handle gaze data
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void gazeHandler(object sender, GazeEventArgs e)
    {
        NormedPoint2d gazepoint = e.gazePoint; //Normed as 0-1d - multiply with screen resolution if needed

        placeElementOnCanvas(GazeBubble, gazepoint);
    }

    /// <summary>
    /// Handle position data
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void posHandler(object sender, PositionEventArgs e)
    {
        NormedPoint2d leftEyePosition = e.leftEyePos; //Normed as 0-1d - multiply with screen resolution if needed
        NormedPoint2d rightEyePosition = e.rightEyePos; //Normed as 0-1d - multiply with screen resolution if needed

        placeElementOnCanvas(leftEye, leftEyePosition);
        placeElementOnCanvas(rightEye, rightEyePosition);

        double distance = e.depthInMM; // Distance in millimeters

        Dispatcher.Invoke(() =>
        {
            DistanceLabel.Content = $"Distance: {Math.Round(distance/10,0)} cm";
            DistanceLabel.Visibility = Visibility.Visible;

            leftEye.SetEyeState(e.isLeftEyeOpen);
            rightEye.SetEyeState(e.isRightEyeOpen);

            if (leftEyePosition.HasConfidence)
                leftEye.UpdateConfidence(leftEyePosition.Confidence);

            if (rightEyePosition.HasConfidence)
                rightEye.UpdateConfidence(rightEyePosition.Confidence);
        });


    }

    /// <summary>
    /// Show or hide the debug log box
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Debug_Click(object sender, RoutedEventArgs e)
    {
        if (LogBox.Visibility == Visibility.Visible)
        {
            LogBox.Visibility = Visibility.Hidden;
        }
        else
        {
            LogBox.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Start / stop video stream from the eye tracker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Video_Click(object sender, RoutedEventArgs e)
    {
        if (!isStreaming)
        {
            device.Video.StartVideoStream(streamHandler);
            isStreaming = true;
            StreamedImage.Visibility = Visibility.Visible;
        }
        else
        {
            device.Video.StopVideoStream(streamHandler);
            isStreaming = false;
            StreamedImage.Visibility = Visibility.Hidden;
        }
    }

    /// <summary>
    /// Stream handler to process the video stream frames from the eye tracker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void streamHandler(object sender, FrameArgs e)
    {
        if(!FrameConverter.CreateImageBytesFromRawFrame(e)) return;
        if (FrameConverter.ImageBytes != null && FrameConverter.ImageBytes.Length > 0)
        {
            Dispatcher.Invoke(() =>
            {
                // Create a BitmapImage from the byte array
                using (MemoryStream ms = new MemoryStream(FrameConverter.ImageBytes))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    StreamedImage.Source = bitmap;
                    StreamedImage.Width = e.width;
                    StreamedImage.Height = e.height;
                }
                placeElementOnCanvas(StreamedImage, new NormedPoint2d(0.5, 0.5));
            });
        }
        else
        {
            logger.Error("Failed to convert frame to image bytes");
        }
    }

    #region Calibration

    /// <summary>
    /// List to hold the calibration point controls
    /// </summary>
    private List<CalibrationPointControl> calibPointList = new();

    /// <summary>
    /// Calib button click event handler to start the calibration process
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Calib_Click(object sender, RoutedEventArgs e)
    {
        ResetPoints();
        ScreenDimensions dims = new ScreenDimensions
        {
            Width = ScreenSizeWidthMM,
            Height = ScreenSizeHeightMM,
        };
        CalibTask = device.Calibration.StartCalibrationAsync(device_CalibrationPointUpdate, device_CalibFinished, dims, multipoint: true);
    }

    /// <summary>
    /// CalibManual button click event handler to start the manual calibration process
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CalibManual_Click(object sender, RoutedEventArgs e)
    {
        ResetPoints();
        ScreenDimensions dims = new ScreenDimensions
        {
            Width = ScreenSizeWidthMM,
            Height = ScreenSizeHeightMM,
        };
        CalibTask = device.Calibration.StartCalibrationAsync(device_CalibrationPointUpdate, device_CalibFinished, dims, manualCalibration: true);
    }

    /// <summary>
    /// Confirm the current calibration point (only applicable during manual calibration)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ConfirmCalibPoint_Click(object sender, RoutedEventArgs e)
    {
        device.Calibration.ConfirmPoint();
    }

    /// <summary>
    /// Improve the calibration 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImproveCalib_Click(object sender, RoutedEventArgs e)
    {
        List<int> points = new List<int>();
        foreach (CalibrationPointControl p in calibPointList)
        {
            if(p.Selected)
            {
                points.Add(p.Sequence);
            }
        }
        ResetPoints();
        device.Calibration.Improve(points.ToArray());
    }

    /// <summary>
    /// Cancel / abort the calibration process (if running)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CalibCancel_Click(object sender, RoutedEventArgs e)
    {
        device.Calibration.StopCalibration();
        ResetPoints();
    }

    /// <summary>
    /// Calibration finished event handler to process the results of the calibration
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void device_CalibFinished(object sender, CalibrationFinishedArgs e)
    {
        logger.Information($"Calibration finished event: {e.success}, {e.percentageRatingPerPoint.Length} with ratings: {string.Join(", ", e.percentageRatingPerPoint)}");
        if (e.success)
        {
            for (int i = 0; i < e.percentageRatingPerPoint.Length; i++)
            {
                int rating = e.percentageRatingPerPoint[i];
                CalibrationPointControl p = getCalibPointControlFromList(i);
                if (p != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        p.ShowResult(rating, showAnimation: true);
                    });
                }
            }
        }
        else
        {
            foreach (CalibrationPointControl p in calibPointList)
            {
                Dispatcher.Invoke(p.Hide);
            }
        }
    }

    /// <summary>
    /// Handle the update of calibration points during the calibration process
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void device_CalibrationPointUpdate(object sender, CalibrationPointUpdateArgs obj)
    {
        logger.Information($"Calibration point update event: # {obj.sequenceNumber}, State = {obj.state}, Position: {obj.target.ToString()}");
        Dispatcher.Invoke(() =>
        {
            CalibrationPointControl p = getCalibPointControlFromList(obj.sequenceNumber);
            if (obj.state == CalibrationPointState.Show)
            {
                if (p == null)
                {
                    // Create a new CalibrationPointControl and add it to the list and canvas
                    CalibrationPointControl cpc = new CalibrationPointControl(obj.sequenceNumber)
                    {
                        Width = 50,
                        Height = 50,
                        Visibility = Visibility.Visible
                    };

                    calibPointList.Add(cpc);
                    mainCanvas.Children.Add(cpc);
                    p = cpc;
                }

                double X = obj.target.X * ScreenResWidth;
                double Y = obj.target.Y * ScreenResHeight;

                double centerX = mainCanvas.ActualWidth / 2d;
                double centerY = mainCanvas.ActualHeight / 2d;

                double targetX = X - p.Width / 2d;
                double targetY = Y - p.Height / 2d;

                p.Show(centerX, centerY, targetX, targetY);
            }
            else if (obj.state == CalibrationPointState.Collecting)
            {
                p?.Collecting();
            }
            else if (obj.state == CalibrationPointState.Hide)
            {
                p?.Hide();
            }
        });
    }

    /// <summary>
    /// Get the CalibrationPointControl from the list based on its sequence number
    /// </summary>
    /// <param name="seq"></param>
    /// <returns></returns>
    private CalibrationPointControl getCalibPointControlFromList(int seq)
    {
        foreach (CalibrationPointControl cpc in calibPointList)
        {
            if (cpc.Sequence == seq)
            {
                return cpc;
            }
        }
        return null;
    }

    /// <summary>
    /// Reset all calibration points to their initial state
    /// </summary>
    private void ResetPoints()
    {
        foreach (CalibrationPointControl p in calibPointList)
        {
            Dispatcher.Invoke(p.Reset);
        }
    }

    #endregion
}