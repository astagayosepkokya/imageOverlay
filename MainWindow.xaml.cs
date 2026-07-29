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
        private Point? lastWindowDragPosition;
        private double currentScale = 1.0;

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
                OverlayImage.IsHitTestVisible = false;
                
                ControlsPanel.Visibility = Visibility.Hidden;
                ResizeThumb.Visibility = Visibility.Hidden;
                MoveThumb.Visibility = Visibility.Hidden;
                
                PinButton.Opacity = 0.5;
            }
            else
            {
                OverlayImage.IsHitTestVisible = true;
                
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

        // Window Dragging (Right Mouse drag)
        private void ViewportGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLocked || !isImageLoaded) return;
            lastWindowDragPosition = this.PointToScreen(e.GetPosition(this));
            ViewportGrid.CaptureMouse();
        }

        private void ViewportGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isLocked) return;
            lastWindowDragPosition = null;
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

            if (lastWindowDragPosition.HasValue)
            {
                Point currentScreenPosition = this.PointToScreen(e.GetPosition(this));
                double deltaX = currentScreenPosition.X - lastWindowDragPosition.Value.X;
                double deltaY = currentScreenPosition.Y - lastWindowDragPosition.Value.Y;

                this.Left += deltaX;
                this.Top += deltaY;

                lastWindowDragPosition = currentScreenPosition;
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