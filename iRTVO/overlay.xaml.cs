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

        // theme
        Theme theme;

        private Image[] themeImages = new Image[Theme.filenames.Length];

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
            thApi = new Thread(new ThreadStart(getData));
            thApi.IsBackground = true;
            thApi.Start();

            // overlay update timer
            overlayUpdateTimer.Tick += new EventHandler(overlayUpdate);
            overlayUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Math.Round(1000/(double)Properties.Settings.Default.UpdateFrequency));
            overlayUpdateTimer.Start();

            resizeOverlay(overlay.Width, overlay.Height);
        }

        private void loadTheme(string themeName)
        {
            overlayUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Math.Round(1000 / (double)Properties.Settings.Default.UpdateFrequency));

            // disable overlay update
            SharedData.runOverlay = false;

            theme = new Theme(themeName);

            canvas.Children.Clear();
            
            // load images
            for(int i = 0; i < themeImages.Length; i++) {
                themeImages[i] = new Image();
                loadImage(themeImages[i], Theme.filenames[i]);
                themeImages[i].Width = theme.width;
                themeImages[i].Height = theme.height;
                canvas.Children.Add(themeImages[i]);
                //Canvas.SetZIndex(themeImages[i], 1000);
            }

            // show main image
            themeImages[(int)Theme.overlayTypes.main].Visibility = System.Windows.Visibility.Visible;

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
            sidepanelInfoLabel = new Label[theme.sidepanel.size];
            
            for (int i = 0; i < theme.sidepanel.size; i++)
            {
                sidepanelPosLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Num);
                sidepanelNameLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Name);
                sidepanelDiffLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Diff);
                sidepanelInfoLabel[i] = DrawLabel(sidepanel, theme.sidepanel.Info);

                Thickness margin;

                margin = sidepanelPosLabel[i].Margin;
                margin.Top = theme.sidepanel.Num.top + i * theme.sidepanel.itemHeight;
                sidepanelPosLabel[i].Margin = margin;
                
                margin = sidepanelNameLabel[i].Margin;
                margin.Top = theme.sidepanel.Name.top + i * theme.sidepanel.itemHeight;
                sidepanelNameLabel[i].Margin = margin;
                
                margin = sidepanelDiffLabel[i].Margin;
                margin.Top = theme.sidepanel.Diff.top + i * theme.sidepanel.itemHeight;
                sidepanelDiffLabel[i].Margin = margin;

                margin = sidepanelInfoLabel[i].Margin;
                margin.Top = theme.sidepanel.Info.top + i * theme.sidepanel.itemHeight;
                sidepanelInfoLabel[i].Margin = margin;
                
                sidepanel.Children.Add(sidepanelDiffLabel[i]);
                sidepanel.Children.Add(sidepanelPosLabel[i]);
                sidepanel.Children.Add(sidepanelNameLabel[i]);
                sidepanel.Children.Add(sidepanelInfoLabel[i]);
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
            driverInfoLabel = new Label();

            driverPosLabel = DrawLabel(driver, theme.driver.Num);
            driverNameLabel = DrawLabel(driver, theme.driver.Name);
            driverDiffLabel = DrawLabel(driver, theme.driver.Diff);
            driverInfoLabel = DrawLabel(driver, theme.driver.Info);

            driver.Children.Add(driverPosLabel);
            driver.Children.Add(driverNameLabel);
            driver.Children.Add(driverDiffLabel);
            driver.Children.Add(driverInfoLabel);

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
            resultsInfoLabel = new Label[theme.results.size];

            for (int i = 0; i < theme.results.size; i++)
            {
                resultsPosLabel[i] = DrawLabel(results, theme.results.Num);
                resultsNameLabel[i] = DrawLabel(results, theme.results.Name);
                resultsDiffLabel[i] = DrawLabel(results, theme.results.Diff);
                resultsInfoLabel[i] = DrawLabel(results, theme.results.Info);

                Thickness margin;
                
                margin = resultsPosLabel[i].Margin;
                margin.Top = theme.results.Num.top + i * theme.results.itemHeight;
                resultsPosLabel[i].Margin = margin;

                margin = resultsNameLabel[i].Margin;
                margin.Top = theme.results.Name.top + i * theme.results.itemHeight;
                resultsNameLabel[i].Margin = margin;

                margin = resultsDiffLabel[i].Margin;
                margin.Top = theme.results.Diff.top + i * theme.results.itemHeight;
                resultsDiffLabel[i].Margin = margin;

                margin = resultsInfoLabel[i].Margin;
                margin.Top = theme.results.Info.top + i * theme.results.itemHeight;
                resultsInfoLabel[i].Margin = margin;

                results.Children.Add(resultsDiffLabel[i]);
                results.Children.Add(resultsPosLabel[i]);
                results.Children.Add(resultsNameLabel[i]);
                results.Children.Add(resultsInfoLabel[i]);
            }

            // create session state
            sessionstateText = DrawLabel(canvas, theme.sessionstateText);
            canvas.Children.Add(sessionstateText);

            // create ticker
            ticker = new StackPanel();
            ticker.Margin = new Thickness(0);
            ticker.Height = theme.ticker.height;
            ticker.Orientation = Orientation.Horizontal;
            //canvas.Children.Add(ticker);

            // test
            Canvas tickerCanvas = new Canvas();
            tickerCanvas.Width = theme.ticker.width;
            tickerCanvas.Height = theme.ticker.height;
            tickerCanvas.Margin = new Thickness(theme.ticker.left, theme.ticker.top, 0, 0);
            tickerCanvas.ClipToBounds = true;
            tickerCanvas.Children.Add(ticker);
            canvas.Children.Add(tickerCanvas);

            // create lap time
            laptimeText = DrawLabel(canvas, theme.laptimeText);
            canvas.Children.Add(laptimeText);

            // enable overlay update
           // SharedData.runOverlay = true;
        }

        private void loadImage(Image img, string filename)
        {
            if (File.Exists(@Directory.GetCurrentDirectory() + "\\" + theme.path + "\\" + filename))
                img.Source = new BitmapImage(new Uri(@Directory.GetCurrentDirectory() + "\\" + theme.path + "\\" + filename));
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

            label.FontWeight = prop.FontBold;
            label.FontStyle = prop.FontItalic;

            label.HorizontalContentAlignment = prop.TextAlign;

            label.Padding = new Thickness(0);

            Canvas.SetZIndex(label, 100);

            if (Properties.Settings.Default.ShowBorders)
            {
                label.BorderBrush = System.Windows.Media.Brushes.Yellow;
                label.BorderThickness = new Thickness(1);
                //label.Margin = new Thickness(label.Margin.Left - 1, label.Margin.Top - 1, 0, 0);
                //label.Padding = new Thickness(-1);
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

            label.FontWeight = prop.FontBold;
            label.FontStyle = prop.FontItalic;

            label.HorizontalContentAlignment = prop.TextAlign;

            label.Padding = new Thickness(0);

            //Canvas.SetZIndex(label, 100);

            if (Properties.Settings.Default.ShowBorders)
            {
                label.BorderBrush = System.Windows.Media.Brushes.Yellow;
                label.BorderThickness = new Thickness(1);
                //label.Margin = new Thickness(label.Margin.Left - 1, label.Margin.Top - 1, 0, 0);
                //label.Padding = new Thickness(-1);
            }


            return label;
        }

        private void Size_Changed(object sender, SizeChangedEventArgs e)
        {
            if (theme != null)
                resizeOverlay(e.NewSize.Width, e.NewSize.Height);
        }

        private void resizeOverlay(double width, double height)
        {
            viewbox.Width = width;
            viewbox.Height = height;
        }
    }
}
