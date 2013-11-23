using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.IO;

namespace Ini
{
    class IniFile
    {
        Dictionary<string, Dictionary<string, string>> ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

        public bool AutoSave = true;
        public string Filename { get; private set; }
        public bool isDirty { get; private set; }
        public bool isNew { get; private set; }

        public IniFile(string file) 
        {
            isDirty = false;
            isNew = false;
            Filename = file;
            string txt = String.Empty;

            if (File.Exists(file))
                txt = File.ReadAllText(file);
            else
                isNew = true;

            Dictionary<string, string> currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            ini[""] = currentSection;

            foreach (var line in txt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                   .Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Select(t => t.Trim()))
            {
                if (line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    ini[line.Substring(1, line.LastIndexOf("]") - 1)] = currentSection;
                    continue;
                }

                var idx = line.IndexOf("=");
                if ( (idx == -1) || (idx == line.Length-1) )
                    currentSection[line] = "";
                else
                {
                    string val = line.Substring(idx + 1);
                    if ( val[0] == '"' )
                        val = val.Substring(1,val.Length -2 );
                    currentSection[line.Substring(0, idx)] = val;
                }
            }
        }

        public IniFile(string file, bool autoSave) : this(file)
        {
            AutoSave = autoSave;            
        }

        public string GetValue(string key)
        {
            return GetValue("", key, "");
        }

        public string GetValue( string section, string key)
        {
            return GetValue( section,key, "");
        }

        public string GetValue(string section, string key, string @default)
        {
            if (!ini.ContainsKey(section))
                return @default;

            if (!ini[section].ContainsKey(key))
                return @default;

            return ini[section][key];
        }

        public bool HasValue(string section, string key)
        {
            return !String.IsNullOrEmpty(GetValue(section, key, String.Empty));
        }

        /// <summary>
        /// Checks if the ini contain the specif key in the section
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="key">the key name</param>
        /// <returns>true or false</returns>
        public bool HasKey(string section, string key)
        {
            if (!ini.ContainsKey(section))
                return false;
            return ini[section].ContainsKey(key);
        }

        public string[] GetKeys(string section)
        {
            if (!ini.ContainsKey(section))
                return new string[0];

            return ini[section].Keys.ToArray();
        }

        public string[] GetSections()
        {
            return ini.Keys.Where(t => t != "").ToArray();
        }

        public void SetValue(string section, string key, string value)
        {
            SetValue(section, key, value, String.Empty);
        }

        public void SetValue(string section, string key, string value,string comment)
        {
            if (!ini.ContainsKey(section))
            {
                Dictionary<string, string> currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                ini[section] = currentSection;
            }
            else 
            {
                // Only check if section is not new. New Sections will be always written
                if (HasKey(section, key) && (GetValue(section, key) == value))
                    return;
            }
            isDirty = true;
            ini[section][key] = value;
            if (AutoSave)
                SaveIniFile();
        }

        public void SaveIniFile()
        {
            if ( !isDirty )
                return;
            StringBuilder sb = new StringBuilder();

            foreach (var k in GetSections())
            {
                sb.AppendLine(String.Format("[{0}]", k));
                var currentSection = ini[k];
                foreach (var key in currentSection.Keys)
                {
                    sb.AppendLine(String.Format("{0}={1}", key, currentSection[key]));
                }
                sb.AppendLine();
            }
            File.WriteAllText(Filename, sb.ToString());
        }
    }


#if OLDCODE
    /// <summary>

    /// Create a New INI file to store or load data

    /// </summary>

    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>

        /// INIFile Constructor.

        /// </summary>

        /// <PARAM name="INIPath"></PARAM>

        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        /// <summary>

        /// Write Data to the INI File

        /// </summary>

        /// <PARAM name="Section"></PARAM>

        /// Section name

        /// <PARAM name="Key"></PARAM>

        /// Key Name

        /// <PARAM name="Value"></PARAM>

        /// Value Name

        public void SetValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>

        /// Read Data Value From the Ini File

        /// </summary>

        /// <PARAM name="Section"></PARAM>

        /// <PARAM name="Key"></PARAM>

        /// <PARAM name="Path"></PARAM>

        /// <returns></returns>

        public string GetValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(1023);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            1023, this.path);
            return temp.ToString();

        }

        public bool IniHasValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(1023);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            1023, this.path);
            return i > 0;

        }
    }
#endif
}