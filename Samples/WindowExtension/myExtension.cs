using iRTVO.Interfaces;
using System.Windows;

namespace WpfApplication1
{
    public class myExtension : IExtensionWindow
    {
        Window myWindow;
        internal static ISimulationAPI API;
        internal static ISharedData SharedData;

        public string ButtonText
        {
            get { return "Extension Window"; }
        }

        public bool ShowWindow()
        {
            if (myWindow == null || !myWindow.IsVisible)
            {
                myWindow = new WpfApplication1.MainWindow();
                myWindow.Show();
            }
            myWindow.Activate();
            return true;
        }

        public void CloseWindow()
        {
            if (myWindow != null)
                myWindow.Close();
            myWindow = null;
        }

        public ExtensionTypes ExtensionType
        {
            get { return ExtensionTypes.Window; }
        }

        public string Name
        {
            get { return "Sample WindowsExtension"; }
        }


        public void InitializeExtension(ISimulationAPI api, ISharedData sharedData)
        {
            API = api;
            SharedData = sharedData;
        }

        public void ShutdownExtension()
        {
            CloseWindow();
            API = null;
            SharedData = null;
        }
    }
}
