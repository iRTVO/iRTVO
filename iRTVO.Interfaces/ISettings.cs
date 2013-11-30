using System;
namespace iRTVO.Interfaces
{
    public interface ISettings
    {
        void Load();
        void Save();
        string getValue(string Section, string Entry, bool CaseSensitive, string defaultValue, bool add);        
        bool setValue(string Section, string Entry, string Value, bool CaseSensitive);
    }
}
