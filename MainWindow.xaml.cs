using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace ImageOverlay
{
    public partial class MainWindow : Window
    {
        private double aspectRatio = 1.0;
        private Forms.NotifyIcon? notifyIcon;
        private bool isLocked = false;

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public MainWindow()
        {
            InitializeComponent();
            SetupTrayIcon();
            this.Closed += MainWindow_Closed;
        }

        private void SetupTrayIcon()
        {
            notifyIcon = new Forms.NotifyIcon();
            notifyIcon.Text = "Image Overlay";
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
            notifyIcon.Visible = true;

            var contextMenu = new Forms.ContextMenuStrip();
            
            var unlockItem = new Forms.ToolStripMenuItem("Unlock (Disable Click-Through)");
            unlockItem.Click += (s, e) => UnlockWindow();
            
            var closeItem = new Forms.ToolStripMenuItem("Exit");
            closeItem.Click += (s, e) => this.Close();

            contextMenu.Items.Add(unlockItem);
            contextMenu.Items.Add(closeItem);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => UnlockWindow();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
                        this.Width = this.Height * aspectRatio;
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
            
            // Calculate proportional resizing based on horizontal drag
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

        private void UnlockWindow()
        {
            if (Dispatcher.CheckAccess())
            {
                MakeClickThrough(false);
            }
            else
            {
                Dispatcher.Invoke(() => MakeClickThrough(false));
            }
        }

        private void MakeClickThrough(bool enable)
        {
            isLocked = enable;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            if (enable)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                ControlsPanel.Visibility = Visibility.Hidden;
                ResizeThumb.Visibility = Visibility.Hidden;
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                ControlsPanel.Visibility = Visibility.Visible;
                ResizeThumb.Visibility = Visibility.Visible;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}