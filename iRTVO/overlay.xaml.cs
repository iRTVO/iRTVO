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
//using System.Runtime.InteropServices;
//using System.Diagnostics;
using System.Windows.Threading;
using System.IO;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        // overlay update timer
        DispatcherTimer overlayUpdateTimer = new DispatcherTimer();

        // API thread
        Thread thApi;

        // i18n
        localization i18n = new localization();

        // theme
        Theme theme;

        private enum overlayTypes
        {
            main            = 0,
            driver          = 1,
            sessionstate    = 2,
            replay          = 3,
            results         = 4,
            sidepanel       = 5
        }

        private string[] themeFiles = new string[6] {
            "main.png",
            "driver.png",
            "laptimer.png",
            "replay.png",
            "results.png",
            "sidepanel.png"
        };

        private Image[] themeImages = new Image[6];

        public Overlay()
        {
            InitializeComponent();
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
            thApi = new Thread(new ThreadStart(getData));
            thApi.IsBackground = true;
            thApi.Start();

            // overlay update timer
            overlayUpdateTimer.Tick += new EventHandler(overlayUpdate);
            overlayUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 33); // update freq 33 ms = 30 Hz or fps
            overlayUpdateTimer.Start();

            resizeOverlay(overlay.Width, overlay.Height);
        }

        private void loadTheme(string themeName)
        {
            theme = new Theme(themeName);

            canvas.Children.Clear();
            
            // load images
            for(int i = 0; i < themeImages.Length; i++) {
                if (File.Exists(Directory.GetCurrentDirectory() + "\\" + theme.path + "\\" + themeFiles[i]))
                {
                    themeImages[i] = new Image();
                    loadImage(themeImages[i], themeFiles[i]);
                    themeImages[i].Width = theme.width;
                    themeImages[i].Height = theme.height;
                    canvas.Children.Add(themeImages[i]);
                }
                else
                    MessageBox.Show("Unable to load image file \"" +theme.path + "\\" + themeFiles[i] + "\"",
                        "Image load error", MessageBoxButton.OK);

            }

            // show main image
            themeImages[(int)overlayTypes.main].Visibility = System.Windows.Visibility.Visible;

            // create sidepanel canvas
            sidepanel = new Canvas();
            sidepanel.Margin = new Thickness(theme.sidepanel.left, theme.sidepanel.top, 0, 0);
            sidepanel.Width = theme.sidepanel.width;
            sidepanel.Height = theme.sidepanel.height;
            canvas.Children.Add(sidepanel);

            // create label arrays
            sidepanelPosLabel = new Label[theme.sidepanel.size];
            sidepanelNameLabel = new Label[theme.sidepanel.size];
            sidepanelDiffLabel = new Label[theme.sidepanel.size];
            
            for (int i = 0; i < theme.sidepanel.size; i++)
            {

                sidepanelPosLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Num);
                sidepanelNameLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Name);
                sidepanelDiffLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Diff);

                Thickness margin = sidepanelPosLabel[i].Margin;
                margin.Top = theme.sidepanel.Num.top + i * theme.sidepanel.itemHeight;
                sidepanelPosLabel[i].Margin = margin;
                
                margin = sidepanelNameLabel[i].Margin;
                margin.Top = theme.sidepanel.Name.top + i * theme.sidepanel.itemHeight;
                sidepanelNameLabel[i].Margin = margin;
                
                margin = sidepanelDiffLabel[i].Margin;
                margin.Top = theme.sidepanel.Diff.top + i * theme.sidepanel.itemHeight;
                sidepanelDiffLabel[i].Margin = margin;
                
                sidepanel.Children.Add(sidepanelDiffLabel[i]);
                sidepanel.Children.Add(sidepanelPosLabel[i]);
                sidepanel.Children.Add(sidepanelNameLabel[i]);
            }

            // create driver canvas
            driver = new Canvas();
            driver.Margin = new Thickness(theme.driver.left, theme.driver.top, 0, 0);
            driver.Width = theme.driver.width;
            driver.Height = theme.driver.height;

            canvas.Children.Add(driver);

            driverPosLabel = new Label();
            driverNameLabel = new Label();
            driverDiffLabel = new Label();

            driverPosLabel = DrawLabel(driver, theme.driver.Num);
            driverNameLabel = DrawLabel(driver, theme.driver.Name);
            driverDiffLabel = DrawLabel(driver, theme.driver.Diff);

            driver.Children.Add(driverPosLabel);
            driver.Children.Add(driverNameLabel);
            driver.Children.Add(driverDiffLabel);

            // create results canvas
            results = new Canvas();
            results.Margin = new Thickness(theme.results.left, theme.results.top, 0, 0);
            results.Width = theme.results.width;
            results.Height = theme.results.height;
            canvas.Children.Add(results);

            // create headers
            resultsHeader = DrawLabel(results, theme.resultsHeader);
            results.Children.Add(resultsHeader);
            resultsSubHeader = DrawLabel(results, theme.resultsSubHeader);
            results.Children.Add(resultsSubHeader);

            // create label arrays
            resultsPosLabel = new Label[theme.results.size];
            resultsNameLabel = new Label[theme.results.size];
            resultsDiffLabel = new Label[theme.results.size];

            for (int i = 0; i < theme.results.size; i++)
            {

                resultsPosLabel[i] = DrawLabel(results, theme.results.Num);
                resultsNameLabel[i] = DrawLabel(results, theme.results.Name);
                resultsDiffLabel[i] = DrawLabel(results, theme.results.Diff);

                Thickness margin = resultsPosLabel[i].Margin;
                margin.Top = theme.results.Num.top + i * theme.results.itemHeight;
                resultsPosLabel[i].Margin = margin;

                margin = resultsNameLabel[i].Margin;
                margin.Top = theme.results.Name.top + i * theme.results.itemHeight;
                resultsNameLabel[i].Margin = margin;

                margin = resultsDiffLabel[i].Margin;
                margin.Top = theme.results.Diff.top + i * theme.results.itemHeight;
                resultsDiffLabel[i].Margin = margin;

                results.Children.Add(resultsDiffLabel[i]);
                results.Children.Add(resultsPosLabel[i]);
                results.Children.Add(resultsNameLabel[i]);
            }
        }

        private void loadImage(Image img, string filename)
        {
            img.Source = new BitmapImage(new Uri(theme.path + "\\" + filename, UriKind.Relative));
            img.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Size_Changed(object sender, SizeChangedEventArgs e)
        {
            if (theme != null)
                resizeOverlay(e.NewSize.Width, e.NewSize.Height);
        }

        private void resizeOverlay(double width, double height)
        {
            /*
            for (int i = 0; i < themeImages.Length; i++)
            {
                themeImages[i].Width = width;
                themeImages[i].Height = height;
            }
            */

            viewbox.Width = width;
            viewbox.Height = height;

            // scale canvases
            //sidepanel.Width = width * theme.sidepanel.width / theme.width;
            //sidepanel.Height = height * theme.sidepanel.height / theme.height;
            //sidepanel.RenderTransform = new ScaleTransform(theme.sidepanel.width / theme.width, theme.sidepanel.height / theme.height);
        }
    }
}
