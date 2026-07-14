using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;

namespace ImageOverlay
{
    public partial class MainWindow : Window
    {
        private double aspectRatio = 1.0;
        private bool isLocked = false;
        private bool isImageLoaded = false;

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only allow dragging if we are not locked
            if (!isLocked)
                this.DragMove();
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
                    
                    if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                    {
                        aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                        
                        // Limit initial loaded size if it's too huge, otherwise just use image size
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
                        // First time image loaded: remove initial background and swap panels
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

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OverlayImage != null)
            {
                this.Opacity = e.NewValue;
            }
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
                PinButton.Opacity = 0.5;
            }
            else
            {
                OverlayImage.IsHitTestVisible = true;
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