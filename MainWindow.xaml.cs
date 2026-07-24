using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ImageOverlay
{
    public partial class MainWindow : Window
    {
        private double aspectRatio = 1.0;
        private bool isLocked = false;
        private bool isImageLoaded = false;

        // Panning and zooming
        private Point? lastPanPosition;
        private Point? lastZoomPosition;
        private double currentScale = 1.0;

        // Hotkey
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint VK_L = 0x4C;

        private IntPtr _windowHandle;
        private HwndSource? _source;

        // WS_EX_TRANSPARENT
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_L);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Toggle lock on Ctrl+L
                MakeClickThrough(!isLocked);
                handled = true;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source?.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
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
                    OverlayImage.Source = bitmap;
                    
                    // Reset pan/zoom
                    currentScale = 1.0;
                    ImageScale.ScaleX = 1.0;
                    ImageScale.ScaleY = 1.0;
                    ImageTranslate.X = 0;
                    ImageTranslate.Y = 0;

                    if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                    {
                        aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                        
                        double initialWidth = bitmap.PixelWidth;
                        double initialHeight = bitmap.PixelHeight;
                        
                        double maxScreenHeight = SystemParameters.WorkArea.Height * 0.9;
                        double maxScreenWidth = SystemParameters.WorkArea.Width * 0.9;
                        
                        if (initialHeight > maxScreenHeight)
                        {
                            initialHeight = maxScreenHeight;
                            initialWidth = initialHeight * aspectRatio;
                        }

                        if (initialWidth > maxScreenWidth)
                        {
                            initialWidth = maxScreenWidth;
                            initialHeight = initialWidth / aspectRatio;
                        }

                        this.Width = initialWidth;
                        this.Height = initialHeight;
                    }
                    
                    if (!isImageLoaded)
                    {
                        this.Background = System.Windows.Media.Brushes.Transparent;
                        InitialControlsPanel.Visibility = Visibility.Collapsed;
                        ControlsPanel.Visibility = Visibility.Visible;
                        PinButton.Visibility = Visibility.Visible;
                        ResizeThumb.Visibility = Visibility.Visible;
                        MoveThumb.Visibility = Visibility.Visible;
                        isImageLoaded = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OverlayImage != null)
            {
                this.Opacity = e.NewValue;
            }
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (isLocked) return;
            this.Left += e.HorizontalChange;
            this.Top += e.VerticalChange;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (isLocked) return;
            
            if (aspectRatio > 0)
            {
                double proposedWidth = this.Width + e.HorizontalChange;
                if (proposedWidth > 100)
                {
                    this.Width = proposedWidth;
                    this.Height = proposedWidth / aspectRatio;
                }
            }
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            MakeClickThrough(true);
        }

        private void MakeClickThrough(bool enable)
        {
            isLocked = enable;

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            if (enable)
            {
                // Make the entire window click-through so the user can interact with apps behind it
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                
                ControlsPanel.Visibility = Visibility.Hidden;
                ResizeThumb.Visibility = Visibility.Hidden;
                MoveThumb.Visibility = Visibility.Hidden;
                
                // Keep the pin button visible to indicate it is locked, but it won't be clickable
                PinButton.Opacity = 0.5;
            }
            else
            {
                // Remove click-through
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                
                ControlsPanel.Visibility = Visibility.Visible;
                ResizeThumb.Visibility = Visibility.Visible;
                MoveThumb.Visibility = Visibility.Visible;
                
                PinButton.Opacity = 1.0;
            }
        }

        // Panning (Left Mouse)
        private void ViewportGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            lastPanPosition = e.GetPosition(this);
            ViewportGrid.CaptureMouse();
        }

        private void ViewportGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isLocked) return;
            lastPanPosition = null;
            ViewportGrid.ReleaseMouseCapture();
        }

        // Zooming (Right Mouse drag)
        private void ViewportGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            lastZoomPosition = e.GetPosition(this);
            ViewportGrid.CaptureMouse();
        }

        private void ViewportGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isLocked) return;
            lastZoomPosition = null;
            if (lastPanPosition == null)
            {
                ViewportGrid.ReleaseMouseCapture();
            }
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

            if (lastZoomPosition.HasValue)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaY = lastZoomPosition.Value.Y - currentPosition.Y; // up is positive zoom

                double zoomFactor = 1.0 + (deltaY * 0.01);
                DoZoom(zoomFactor);

                lastZoomPosition = currentPosition;
            }
        }

        // Zooming (Mouse Wheel)
        private void ViewportGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            DoZoom(zoomFactor);
        }

        private void DoZoom(double factor)
        {
            double newScale = currentScale * factor;
            if (newScale < 0.1) newScale = 0.1;
            if (newScale > 50) newScale = 50;

            currentScale = newScale;
            ImageScale.ScaleX = currentScale;
            ImageScale.ScaleY = currentScale;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}