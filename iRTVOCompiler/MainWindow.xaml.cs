using CSScriptLibrary;
using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace iRTVOCompiler
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CSScript.AssemblyResolvingEnabled = true;
        }

        string filename = String.Empty;
        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 

            dlg.DefaultExt = ".cs"; // Default file extension
            dlg.Filter = "iRTVO Script Files (.cs)|*.cs"; // Filter files by extension
            // Display OpenFileDialog by calling ShowDialog method 

            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                tbResults.Text = String.Format("Loading script {0} \r\n", filename);
                RecompileScript_Click(sender, e);
            }
        }

        private void RecompileScript_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(filename))
            {
                 tbResults.Text = "Load a script first!\r\n";
                 return;
            }
            try
            {
                tbResults.Text += "Compiling...\r\n";                
                Assembly script = CSScript.Load(filename, null, true);
                foreach (var t in script.GetTypes())
                {
                    Type tp = t.GetInterface("IScript");
                    if (tp != null)
                    {
                        IScript sc = Activator.CreateInstance(t) as IScript;
                        String scname = sc.init(new Host());
                        tbResults.Text += "Success.\r\n";
                    }
                }
            }
            catch (Exception ex)
            {
                tbResults.Text += ex.ToString();
            }
        }

        private void ClearScript_Click(object sender, RoutedEventArgs e)
        {
            tbResults.Text = String.Empty;
        }
    }
}
