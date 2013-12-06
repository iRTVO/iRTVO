using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Demo of an extension Window for iRTVO which saves its location to options.ini
    /// </summary>
    public partial class MainWindow : Window
    {
        private ISettings iRTVOSettings
        {
            get
            {
                if ((myExtension.SharedData == null) || (myExtension.SharedData.Settings == null))
                    throw new NotImplementedException();
                return myExtension.SharedData.Settings;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Left = Convert.ToDouble(iRTVOSettings.getValue("WindowExtension", "X", false, "0", true), CultureInfo.InvariantCulture);
            this.Top = Convert.ToDouble(iRTVOSettings.getValue("WindowExtension", "Y", false, "0", true), CultureInfo.InvariantCulture);
            this.Width = Convert.ToDouble(iRTVOSettings.getValue("WindowExtension", "W", false, "0", true), CultureInfo.InvariantCulture);
            this.Height = Convert.ToDouble(iRTVOSettings.getValue("WindowExtension", "H", false, "0", true), CultureInfo.InvariantCulture);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            iRTVOSettings.setValue("WindowExtension", "X", this.Left.ToString(), false);
            iRTVOSettings.setValue("WindowExtension", "Y", this.Top.ToString(), false);   
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            iRTVOSettings.Save();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            iRTVOSettings.setValue("WindowExtension", "W", this.Width.ToString(),false);
            iRTVOSettings.setValue("WindowExtension", "H", this.Height.ToString(), false);            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            

        }
    }
}
