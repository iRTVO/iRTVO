/*
 * overlay.xaml.cs
 * 
 * The overlay window.
 * 
 * On load the theme is loaded, API-thread and overlay updater are started.
 * 
 * loadTheme() resets the overlay, load images and labels.
 * 
 * loadImage() returns the wanted image from theme folder.
 * 
 * DrawLabel() takes the Theme.LabelProperties as an argument and returns a label
 * with according properties.
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

// additional
using System.Threading;
using System.Windows.Threading;
using System.IO;
using System.Windows.Interop;
using iRTVO;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        // overlay update timer
        DispatcherTimer overlayUpdateTimer = new DispatcherTimer();
        DispatcherTimer tickerScroller = new DispatcherTimer();

        // API thread
        iRacingAPI API;
        Thread thApi;

        // Objects & labels
        Canvas[] objects;
        Label[][] labels;
        Image[] images;
        Canvas[] tickers;
        Label[][] tickerLabels;
        Label[] tickerHeaders;
        Label[] tickerFooters;
        StackPanel[] tickerStackpanels;
        StackPanel[][] tickerRowpanels;
        MediaElement[] videos;
        Rectangle[] videoBoxes;
        VisualBrush[] videoBrushes;

        /*
        ThicknessAnimation[] tickerAnimations;
        Storyboard[] tickerStoryboards;
        Canvas[] tickerScrolls;
        */
        int updateMs;

        public Overlay()
        {
            InitializeComponent();
        }

        // overlay click through
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load theme
            loadTheme(Properties.Settings.Default.theme);
            
            // size and position
            overlay.Left = Properties.Settings.Default.OverlayLocationX;
            overlay.Top = Properties.Settings.Default.OverlayLocationY;
            overlay.Width = Properties.Settings.Default.OverlayWidth;
            overlay.Height = Properties.Settings.Default.OverlayHeight;

            // start api thread
            API = new iRTVO.iRacingAPI();
            thApi = new Thread(new ThreadStart(API.getData));
            thApi.IsBackground = true;
            thApi.Start();

            // overlay update timer
            overlayUpdateTimer.Tick += new EventHandler(overlayUpdate);
            overlayUpdateTimer.Start();

            tickerScroller.Interval = TimeSpan.FromMilliseconds(16.0);
            tickerScroller.Tick += new EventHandler(scrollTickers);
            tickerScroller.Start();

            resizeOverlay(overlay.Width, overlay.Height);

        }

        private void loadTheme(string themeName)
        {
            updateMs = (int)Math.Round(1000 / (double)Properties.Settings.Default.UpdateFrequency);
            overlayUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, updateMs);

            // web timing
            SharedData.webError = "";
            SharedData.web = new webTiming(Properties.Settings.Default.webTimingUrl);
            /*
            for (int i = 0; i < SharedData.webUpdateWait.Length; i++)
            {
                SharedData.webUpdateWait[i] = true;
            }
            */
            // disable overlay update
            SharedData.runOverlay = false;

            SharedData.theme = new Theme(themeName);

            SharedData.theme.readExternalData();

            canvas.Children.Clear();

            objects = new Canvas[SharedData.theme.objects.Length];
            labels = new Label[SharedData.theme.objects.Length][];
            images = new Image[SharedData.theme.images.Length];
            tickers = new Canvas[SharedData.theme.tickers.Length];
            tickerLabels = new Label[SharedData.theme.tickers.Length][];
            tickerHeaders = new Label[SharedData.theme.tickers.Length];
            tickerFooters = new Label[SharedData.theme.tickers.Length];
            tickerStackpanels = new StackPanel[SharedData.theme.tickers.Length];
            tickerRowpanels = new StackPanel[SharedData.theme.tickers.Length][];
            videos = new MediaElement[SharedData.theme.videos.Length];
            videoBoxes = new Rectangle[SharedData.theme.videos.Length];
            videoBrushes = new VisualBrush[SharedData.theme.videos.Length];
            /*
            tickerAnimations = new ThicknessAnimation[SharedData.theme.tickers.Length];
            tickerStoryboards = new Storyboard[SharedData.theme.tickers.Length];
            tickerScrolls = new Canvas[SharedData.theme.tickers.Length];
            */
            SharedData.lastPage = new Boolean[SharedData.theme.objects.Length];

            // create images
            for (int i = 0; i < SharedData.theme.images.Length; i++)
            {
                images[i] = new Image();
                loadImage(images[i], SharedData.theme.images[i]);
                /*
                images[i].Width = SharedData.theme.width;
                images[i].Height = SharedData.theme.height;
                */
                canvas.Children.Add(images[i]);
                Canvas.SetZIndex(images[i], SharedData.theme.images[i].zIndex);
            }

            // create videos
            for (int i = 0; i < SharedData.theme.videos.Length; i++)
            {
                videos[i] = new MediaElement();

                videoBrushes[i] = new VisualBrush();
                videoBrushes[i].Visual = videos[i];

                videoBoxes[i] = new Rectangle();
                videoBoxes[i] = new System.Windows.Shapes.Rectangle();
                videoBoxes[i].Fill = videoBrushes[i];
                videoBoxes[i].Height = SharedData.theme.height;
                videoBoxes[i].Width = SharedData.theme.width;

                canvas.Children.Add(videoBoxes[i]);
                Canvas.SetZIndex(videoBoxes[i], SharedData.theme.videos[i].zIndex);
            }

            // create objects
            for (int i = 0; i < SharedData.theme.objects.Length; i++)
            {
                // init canvas
                objects[i] = new Canvas();
                objects[i].Margin = new Thickness(SharedData.theme.objects[i].left, SharedData.theme.objects[i].top, 0, 0);
                objects[i].Width = SharedData.theme.objects[i].width;
                objects[i].Height = SharedData.theme.objects[i].height;
                objects[i].ClipToBounds = true;

                // create labels
                if (SharedData.theme.objects[i].dataset == Theme.dataset.standing)
                {
                    labels[i] = new Label[SharedData.theme.objects[i].labels.Length * SharedData.theme.objects[i].itemCount];

                    for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++) // items
                    {
                        // fix top preaddition
                        //SharedData.theme.objects[i].labels[j].top -= SharedData.theme.objects[i].itemSize;
                        for (int k = 0; k < SharedData.theme.objects[i].itemCount; k++) // subitems
                        {
                            Theme.LabelProperties label = Theme.setLabelPosition(SharedData.theme.objects[i], SharedData.theme.objects[i].labels[j], k);
                            //SharedData.theme.objects[i].labels[j].top += SharedData.theme.objects[i].itemSize;
                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k] = DrawLabel(label);
                            objects[i].Children.Add(labels[i][(j * SharedData.theme.objects[i].itemCount) + k]);
                        }
                    }
                }
                else
                {
                    labels[i] = new Label[SharedData.theme.objects[i].labels.Length];

                    for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                    {
                        labels[i][j] = DrawLabel(SharedData.theme.objects[i].labels[j]);
                        objects[i].Children.Add(labels[i][j]);
                    }

                }

                canvas.Children.Add(objects[i]);
                Canvas.SetZIndex(objects[i], SharedData.theme.objects[i].zIndex);

                /*
                 * 
                 * if (Properties.Settings.Default.ShowBorders)
                {
                    objects[i].BorderBrush = System.Windows.Media.Brushes.LightGreen;
                    objects[i].BorderThickness = new Thickness(1);
                }*/
            }

            // create tickers
            for (int i = 0; i < SharedData.theme.tickers.Length; i++)
            {
                // init canvas
                tickers[i] = new Canvas();
                tickers[i].Margin = new Thickness(SharedData.theme.tickers[i].left, SharedData.theme.tickers[i].top, 0, 0);
                tickers[i].Width = SharedData.theme.tickers[i].width;
                tickers[i].Height = SharedData.theme.tickers[i].height;
                tickers[i].ClipToBounds = true;

                //tickerScrolls[i] = new Canvas();

                tickerStackpanels[i] = new StackPanel();

                //tickerAnimations[i] = new ThicknessAnimation();
                //tickerStoryboards[i] = new Storyboard();

                canvas.Children.Add(tickers[i]);
                Canvas.SetZIndex(tickers[i], SharedData.theme.tickers[i].zIndex);

                //tickers[i].Children.Add(tickerScrolls[i]);

                tickerHeaders[i] = new Label();
                tickerFooters[i] = new Label();
            }
           
        }

        private void loadImage(Image img, Theme.ImageProperties prop)
        {
            string filename;

            if (prop.dynamic && SharedData.apiConnected == true)
            {
                Theme.LabelProperties label = new Theme.LabelProperties();
                label.text = prop.filename;

                filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                    label,
                    SharedData.Sessions.SessionList[SharedData.overlaySession].FollowedDriver,
                    SharedData.Sessions.SessionList[SharedData.overlaySession]
                );
            }
            else
            {
                filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.filename;
            }

            if (!File.Exists(filename) && prop.dynamic) {
                filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.defaultFile;
            }

            if (File.Exists(filename))
            {
                img.Source = new BitmapImage(new Uri(filename));
                if (prop.width != 0 && prop.height != 0)
                {
                    img.Width = prop.width;
                    img.Height = prop.height;
                }
                else
                {
                    img.Width = SharedData.theme.width;
                    img.Height = SharedData.theme.height;
                }
                img.Margin = new Thickness(prop.left, prop.top, 0, 0);
            }
        }

        private Label DrawLabel(Canvas canvas, Theme.LabelProperties prop)
        {
            Label label = new Label();
            label.Width = prop.width;
            label.Height = prop.height;
            label.Foreground = prop.fontColor;
            label.Margin = new Thickness(prop.left, prop.top, 0, 0);
            label.FontSize = prop.fontSize;
            label.FontFamily = prop.font;
            label.VerticalContentAlignment = System.Windows.VerticalAlignment.Top;

            label.FontWeight = prop.fontBold;
            label.FontStyle = prop.fontItalic;

            label.HorizontalContentAlignment = prop.textAlign;

            label.Padding = new Thickness(0);

            Canvas.SetZIndex(label, 100);

            if (Properties.Settings.Default.ShowBorders)
            {
                label.BorderBrush = System.Windows.Media.Brushes.Yellow;
                label.BorderThickness = new Thickness(1);
                //label.Margin = new Thickness(label.Margin.Left - 1, label.Margin.Top - 1, 0, 0);
                //label.Padding = new Thickness(-1);
            }

            if (prop.backgroundImage != null)
            {
                string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.backgroundImage;
                if (File.Exists(filename))
                {
                    Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                    label.Background = bg;
                }
                else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.defaultBackgroundImage))
                {
                    Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.defaultBackgroundImage)));
                    label.Background = bg;
                }
                else if(prop.dynamic == false)
                {
                    MessageBox.Show("Unable to load image:\n" + filename);
                }
            }
            else
            {
                label.Background = prop.backgroundColor;
            }

            return label;
        }

        private Label DrawLabel(Theme.LabelProperties prop)
        {
            Label label = new Label();
            label.Width = prop.width;
            label.Height = prop.height;
            label.Foreground = prop.fontColor;
            label.Margin = new Thickness(prop.left, prop.top, 0, 0);
            label.FontSize = prop.fontSize;
            label.FontFamily = prop.font;
            label.VerticalContentAlignment = System.Windows.VerticalAlignment.Top;

            label.FontWeight = prop.fontBold;
            label.FontStyle = prop.fontItalic;

            label.HorizontalContentAlignment = prop.textAlign;

            label.Padding = new Thickness(0);

            //Canvas.SetZIndex(label, 100);

            if (Properties.Settings.Default.ShowBorders)
            {
                label.BorderBrush = System.Windows.Media.Brushes.Yellow;
                label.BorderThickness = new Thickness(1);
                //label.Margin = new Thickness(label.Margin.Left - 1, label.Margin.Top - 1, 0, 0);
                //label.Padding = new Thickness(-1);
            }

            if (prop.backgroundImage != null)
            {
                string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.backgroundImage;
                if (File.Exists(filename)) 
                {
                    Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                    label.Background = bg;
                }
                else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.defaultBackgroundImage))
                {
                    Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + prop.defaultBackgroundImage)));
                    label.Background = bg;
                }
                else if (prop.dynamic == false) 
                {
                    MessageBox.Show("Unable to load image:\n" + filename);
                }
            }
            else 
            {
                label.Background = prop.backgroundColor;
            }

            label.Padding = new Thickness(prop.padding[0], prop.padding[1], prop.padding[2], prop.padding[3]);

            return label;
        }

        private void Size_Changed(object sender, SizeChangedEventArgs e)
        {
            if (SharedData.theme != null)
                resizeOverlay(e.NewSize.Width, e.NewSize.Height);
        }

        private void resizeOverlay(double width, double height)
        {
            viewbox.Width = width;
            viewbox.Height = height;
        }
    }
}
