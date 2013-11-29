using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Interfaces
{
    public enum ExtensionTypes
    {
        Script,
        Extension,
        Window
    }

    public interface IExtension
    {
        ExtensionTypes ExtensionType { get; }
        string Name { get; }

        void InitializeExtension( ISimulationAPI api , ISharedData sharedData);
        void ShutdownExtension();
    }

    public interface IExtensionWindow : IExtension
    {
        string ButtonText { get; }
        bool ShowWindow();
        void CloseWindow();
    }
}
