using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;

namespace ImageOverlay
{
    public partial class MainWindow : Window
    {
        private bool isLocked = false;
        private bool isImageLoaded = false;
        
        private WriteableBitmap? originalBmp;
        private WriteableBitmap? displayBmp;

        // Panning and zooming
        private Point? lastPanPosition;
        private double currentScale = 1.0;
        
        // Filter variables
        private double currentContrast = 1.0;
        private Color? transparentColor = null;
        private double colorTolerance = 0.1;
        private bool isEyedropperActive = false;

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;
        }
        
        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp;*.gif)|*.png;*.jpeg;*.jpg;*.bmp;*.gif|All files (*.*)|*.*";
            
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    
                    // Convert to non-premultiplied for filtering
                    FormatConvertedBitmap fcb = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                    originalBmp = new WriteableBitmap(fcb);
                    
                    // Display bitmap needs to be Pbgra32 (pre-multiplied) for WPF
                    displayBmp = new WriteableBitmap(originalBmp.PixelWidth, originalBmp.PixelHeight, originalBmp.DpiX, originalBmp.DpiY, PixelFormats.Pbgra32, null);
                    OverlayImage.Source = displayBmp;
                    
                    // Reset pan/zoom/filters state
                    currentScale = 1.0;
                    ImageScale.ScaleX = 1.0;
                    ImageScale.ScaleY = 1.0;
                    ImageTranslate.X = 0;
                    ImageTranslate.Y = 0;
                    transparentColor = null;
                    isEyedropperActive = false;
                    EyedropperBtn.Content = "Pick Color";
                    
                    // Apply filters immediately to populate displayBmp
                    ApplyFilters();

                    if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                    {
                        double screenW = SystemParameters.WorkArea.Width * 0.9;
                        double screenH = SystemParameters.WorkArea.Height * 0.9;

                        double scaleToFit = Math.Min(screenW / bitmap.PixelWidth, screenH / bitmap.PixelHeight);
                        if (scaleToFit > 1.0) scaleToFit = 1.0; // Don't enlarge

                        currentScale = scaleToFit;
                        ImageScale.ScaleX = currentScale;
                        ImageScale.ScaleY = currentScale;

                        ImageCanvas.Width = bitmap.PixelWidth;
                        ImageCanvas.Height = bitmap.PixelHeight;

                        this.Width = bitmap.PixelWidth * currentScale;
                        this.Height = bitmap.PixelHeight * currentScale;
                    }
                    
                    if (!isImageLoaded)
                    {
                        this.Background = System.Windows.Media.Brushes.Transparent;
                        InitialControlsPanel.Visibility = Visibility.Collapsed;
                        ControlsPanel.Visibility = Visibility.Visible;
                        PinButton.Visibility = Visibility.Visible;
                        ResizeThumb.Visibility = Visibility.Visible;
                        isImageLoaded = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ApplyFilters()
        {
            if (originalBmp == null || displayBmp == null) return;
            
            int width = originalBmp.PixelWidth;
            int height = originalBmp.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            originalBmp.CopyPixels(pixels, stride, 0);

            byte tr = transparentColor?.R ?? 0;
            byte tg = transparentColor?.G ?? 0;
            byte tb = transparentColor?.B ?? 0;
            double tolSq = (colorTolerance * 255) * (colorTolerance * 255);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                if (a > 0)
                {
                    // Contrast
                    if (currentContrast != 1.0)
                    {
                        double rf = (r / 255.0 - 0.5) * currentContrast + 0.5;
                        double gf = (g / 255.0 - 0.5) * currentContrast + 0.5;
                        double bf = (b / 255.0 - 0.5) * currentContrast + 0.5;
                        
                        r = (byte)Math.Max(0, Math.Min(255, rf * 255.0));
                        g = (byte)Math.Max(0, Math.Min(255, gf * 255.0));
                        b = (byte)Math.Max(0, Math.Min(255, bf * 255.0));
                    }

                    // Masking
                    if (transparentColor.HasValue)
                    {
                        double dr = r - tr;
                        double dg = g - tg;
                        double db = b - tb;
                        if ((dr * dr + dg * dg + db * db) <= tolSq * 3)
                        {
                            a = 0;
                        }
                    }

                    // Premultiply
                    if (a < 255 && a > 0)
                    {
                        r = (byte)((r * a) / 255);
                        g = (byte)((g * a) / 255);
                        b = (byte)((b * a) / 255);
                    }
                    else if (a == 0)
                    {
                        r = 0; g = 0; b = 0;
                    }

                    pixels[i] = b;
                    pixels[i + 1] = g;
                    pixels[i + 2] = r;
                    pixels[i + 3] = a;
                }
            }
            
            displayBmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        private void ToggleSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsPanel.Visibility == Visibility.Visible)
            {
                SettingsPanel.Visibility = Visibility.Collapsed;
                ToggleSettingsText.Text = "^";
            }
            else
            {
                SettingsPanel.Visibility = Visibility.Visible;
                ToggleSettingsText.Text = "v";
            }
        }
        
        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OverlayImage != null)
                this.Opacity = e.NewValue;
        }

        private void FilterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ContrastSlider != null) currentContrast = ContrastSlider.Value;
            if (ToleranceSlider != null) colorTolerance = ToleranceSlider.Value;
            
            if (isImageLoaded) ApplyFilters();
        }

        private void EyedropperBtn_Click(object sender, RoutedEventArgs e)
        {
            isEyedropperActive = !isEyedropperActive;
            EyedropperBtn.Content = isEyedropperActive ? "Click on Image" : "Pick Color";
        }

        private void ClearColorBtn_Click(object sender, RoutedEventArgs e)
        {
            transparentColor = null;
            isEyedropperActive = false;
            EyedropperBtn.Content = "Pick Color";
            ApplyFilters();
        }
        
        // Window Dragging (Left Mouse)
        private void ViewportGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            
            if (isEyedropperActive)
            {
                PickColor(e.GetPosition(OverlayImage));
                return;
            }
            
            this.DragMove();
        }

        private void ViewportGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void PickColor(Point pos)
        {
            if (originalBmp == null) return;
            
            int px = (int)Math.Round(pos.X);
            int py = (int)Math.Round(pos.Y);
            
            if (px >= 0 && px < originalBmp.PixelWidth && py >= 0 && py < originalBmp.PixelHeight)
            {
                int stride = originalBmp.PixelWidth * 4;
                byte[] pixels = new byte[4];
                originalBmp.CopyPixels(new Int32Rect(px, py, 1, 1), pixels, stride, 0);
                
                transparentColor = Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
                ApplyFilters();
            }
            
            isEyedropperActive = false;
            EyedropperBtn.Content = "Pick Color";
        }

        // Image Panning (Right Mouse)
        private void ViewportGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            lastPanPosition = e.GetPosition(this);
            ViewportGrid.CaptureMouse();
        }

        private void ViewportGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isLocked) return;
            lastPanPosition = null;
            ViewportGrid.ReleaseMouseCapture();
        }

        private void ViewportGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;

            if (lastPanPosition.HasValue)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaX = currentPosition.X - lastPanPosition.Value.X;
                double deltaY = currentPosition.Y - lastPanPosition.Value.Y;

                ImageTranslate.X += deltaX;
                ImageTranslate.Y += deltaY;

                lastPanPosition = currentPosition;
            }
        }

        // Zooming (Mouse Wheel) centered on mouse
        private void ViewportGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            Point mousePosRelativeToImage = e.GetPosition(OverlayImage);
            
            double oldScale = currentScale;
            double newScale = oldScale * zoomFactor;
            if (newScale < 0.1) newScale = 0.1;
            if (newScale > 50) newScale = 50;
            
            if (newScale == oldScale) return;
            
            currentScale = newScale;
            
            // Adjust translate to keep mouse stationary
            ImageTranslate.X -= (mousePosRelativeToImage.X * newScale - mousePosRelativeToImage.X * oldScale);
            ImageTranslate.Y -= (mousePosRelativeToImage.Y * newScale - mousePosRelativeToImage.Y * oldScale);
            
            ImageScale.ScaleX = currentScale;
            ImageScale.ScaleY = currentScale;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (isLocked) return;
            
            double proposedWidth = this.Width + e.HorizontalChange;
            double proposedHeight = this.Height + e.VerticalChange;
            
            if (proposedWidth > 50) this.Width = proposedWidth;
            if (proposedHeight > 50) this.Height = proposedHeight;
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            MakeClickThrough(!isLocked);
        }

        private void MakeClickThrough(bool enable)
        {
            isLocked = enable;

            if (enable)
            {
                ViewportGrid.IsHitTestVisible = false;
                ControlsPanel.Visibility = Visibility.Hidden;
                ResizeThumb.Visibility = Visibility.Hidden;
                PinButton.Opacity = 0.5;
            }
            else
            {
                ViewportGrid.IsHitTestVisible = true;
                ControlsPanel.Visibility = Visibility.Visible;
                ResizeThumb.Visibility = Visibility.Visible;
                PinButton.Opacity = 1.0;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}