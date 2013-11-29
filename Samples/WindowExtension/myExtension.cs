using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WindowExtension
{
    public class myExtension : IExtensionWindow
    {
        Window myWindow;
        ISimulationAPI api;
        ISharedData sharedData;

        public string ButtonText
        {
            get { return "Extension Window"; }
        }

        public bool ShowWindow()
        {
            if (myWindow == null || myWindow.IsVisible)
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
            this.api = api;
            this.sharedData = sharedData;
        }

        public void ShutdownExtension()
        {
            CloseWindow();
            api = null;
            sharedData = null;
        }
    }
}
