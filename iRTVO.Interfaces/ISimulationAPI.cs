using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Interfaces
{
    public enum ReplayPositionModeTypes
    {
        Begin = 0,
        Current = 1,
        End = 2,
    }

    public enum ReplaySearchModeTypes
    {
        ToStart = 0,
        ToEnd = 1,
        PreviousSession = 2,
        NextSession = 3,
        PreviousLap = 4,
        NextLap = 5,
        PreviousFrame = 6,
        NextFrame = 7,
        PreviousIncident = 8,
        NextIncident = 9,
    }

    public interface ISimulationAPI : IDisposable
    {
        bool IsConnected { get; }
        bool ConnectAPI();
        bool UpdateAPIData();

        //TODO: Data API's
        object GetData(string key);        
        // String GetDataString(string key);
        // Int32 GetDataInt(string key);
        // Single GetDataFloat(string key);

        void HideUI();
        void SwitchCamera(int driver, int camera);
        void ReplaySetPlaySpeed(int playspeed, int slowmotion);
        void ReplaySetPlayPosition(ReplayPositionModeTypes mode, int position);
        void ReplaySearch(ReplaySearchModeTypes mode, int position);

        //
        void Pause();
        void Play();
    }
}
