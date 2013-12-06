using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace iRTVO
{
    /// <summary>
    /// Klasse, um Dateien im Ini-Format
    /// zu verwalten.
    /// </summary>
    public class CfgFile
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Inhalt der Datei
        /// </summary>
        private List<String> lines = new List<string>();

        /// <summary>
        /// Voller Pfad und Name der Datei
        /// </summary>
        private String FileName = "";

        /// <summary>
        /// Gibt an, welche Zeichen als Kommentarbeginn
        /// gewertet werden sollen. Dabei wird das erste 
        /// Zeichen defaultmäßig für neue Kommentare
        /// verwendet.
        /// </summary>
        private String CommentCharacters = ";#";

        /// <summary>
        /// Regulärer Ausdruck für einen Kommentar in einer Zeile
        /// </summary>
        private String regCommentStr = "";

        /// <summary>
        /// Regulärer Ausdruck für einen Eintrag
        /// </summary>
        private Regex regEntry = null;

        /// <summary>
        /// Regulärer Ausdruck für einen Bereichskopf
        /// </summary>
        private Regex regSection = null;

        /// <summary>
        /// lastLine parsed from ini file
        /// </summary>
        private int lastLine = -1;

        /// <summary>
        /// defines if values in <see cref="readOnlyValues"/> are readOnly
        /// </summary>
        private bool internalsLocked = true;

        /// <summary>
        /// Values locked as readonly as coming from Class
        /// </summary>
        private List<string> readOnlyValues = new List<string>();

        /// <summary>
        /// Leerer Standard-Konstruktor
        /// </summary>
        public CfgFile()
        {
            regCommentStr = @"(\s*[" + CommentCharacters + "](?<comment>.*))?";
            regEntry = new Regex(@"^[ \t]*(?<entry>([^=])+)=(?<value>([^=" + CommentCharacters + "])+)" + regCommentStr + "$");
            regSection = new Regex(@"^[ \t]*(\[(?<section>([^\]])+)\]){1}" + regCommentStr + "$");
        }

        /// <summary>
        /// Konstruktor, welcher sofort eine Datei einliest
        /// </summary>
        /// <param name="filename">Name der einzulesenden Datei</param>
        public CfgFile(string filename)
            : this()
        {
            FileName = filename;
            Load();
        }

        private void IncludeFile(string filename,bool isFirst)
        {
            if (!File.Exists(FileName))
                return;
          
            logger.Info("{0} {1}",(isFirst ? "Loading":"Including"), filename);
            using (StreamReader sr = new StreamReader(filename))
            {
                while (!sr.EndOfStream)
                {
                    
                    string l = sr.ReadLine().TrimEnd();
                    if (l.StartsWith("include="))
                        IncludeFile(Path.Combine(Path.GetDirectoryName(filename),l.Substring(8)),false);
                    else
                        lines.Add(l);
                }
            }
        }

        public void Load()
        {
            lines = new List<string>();
            IncludeFile(fileName,true);
        }

        /// <summary>
        /// Datei sichern
        /// </summary>
        /// <returns></returns>
        public Boolean Save()
        {
            if (FileName == "") return false;
            try
            {
                using (StreamWriter sw = new StreamWriter(FileName))
                    foreach (String line in lines)
                        sw.WriteLine(line);
            }
            catch (IOException ex)
            {
                throw new IOException("Fehler beim Schreiben der Datei " + fileName, ex);
            }
            catch
            {
                throw new IOException("Fehler beim Schreiben der Datei " + fileName);
            }
            return true;
        }

        /// <summary>
        /// Voller Name der Datei
        /// </summary>
        /// <returns></returns>
        public String fileName
        {
            get { return FileName; }
            set { FileName = value; }
        }

        /// <summary>
        /// Verzeichnis der Datei
        /// </summary>
        /// <returns></returns>
        public String getDirectory()
        {
            return Path.GetDirectoryName(FileName);
        }

        /// <summary>
        /// Sucht die Zeilennummer (nullbasiert) 
        /// eines gewünschten Eintrages
        /// </summary>
        /// <param name="Section">Name des Bereiches</param>
        /// <param name="CaseSensitive">true = Gross-/Kleinschreibung beachten</param>
        /// <returns>Nummer der Zeile, sonst -1</returns>
        private int SearchSectionLine(String Section, Boolean CaseSensitive)
        {
            if (!CaseSensitive) Section = Section.ToLower();
            for (int i = 0; i < lines.Count; i++)
            {
                String line = lines[i].Trim();
                if (line == "") continue;
                if (!CaseSensitive) line = line.ToLower();
                // Erst den gewünschten Abschnitt suchen
                if (line == "[" + Section + "]")
                    return i;
            }
            return -1;// Bereich nicht gefunden
        }

        /// <summary>
        /// Sucht die Zeilennummer (nullbasiert) 
        /// eines gewünschten Eintrages
        /// </summary>
        /// <param name="Section">Name des Bereiches</param>
        /// <param name="Entry">Name des Eintrages</param>
        /// <param name="CaseSensitive">true = Gross-/Kleinschreibung beachten</param>
        /// <returns>Nummer der Zeile, sonst -1</returns>
        private int SearchEntryLine(String Section, String Entry, Boolean CaseSensitive)
        {
            Section = Section.ToLower();
            if (!CaseSensitive) Entry = Entry.ToLower();
            int SectionStart = SearchSectionLine(Section, false);
            if (SectionStart < 0) return -1;
            for (int i = SectionStart + 1; i < lines.Count; i++)
            {
                String line = lines[i].Trim();
                if (line == "") continue;
                if (!CaseSensitive) line = line.ToLower();
                if (line.StartsWith("["))
                    return -1;// Ende, wenn der nächste Abschnitt beginnt
                if (Regex.IsMatch(line, @"^[ \t]*[" + CommentCharacters + "]"))
                    continue; // Kommentar
                if (line.StartsWith(Entry+"="))
                    return i;// Eintrag gefunden
            }
            return -1;// Eintrag nicht gefunden
        }

        /// <summary>
        /// Kommentiert einen Wert aus
        /// </summary>
        /// <param name="Section">Name des Bereiches</param>
        /// <param name="Entry">Name des Eintrages</param>
        /// <param name="CaseSensitive">true = Gross-/Kleinschreibung beachten</param>
        /// <returns>true = Eintrag gefunden und auskommentiert</returns>
        public Boolean commentValue(String Section, String Entry, Boolean CaseSensitive)
        {
            int line = SearchEntryLine(Section, Entry, CaseSensitive);
            if (line < 0) return false;
            lines[line] = CommentCharacters[0] + lines[line];
            return true;
        }

        /// <summary>
        /// Löscht einen Wert
        /// </summary>
        /// <param name="Section">Name des Bereiches</param>
        /// <param name="Entry">Name des Eintrages</param>
        /// <param name="CaseSensitive">true = Gross-/Kleinschreibung beachten</param>
        /// <returns>true = Eintrag gefunden und gelöscht</returns>
        public Boolean deleteValue(String Section, String Entry, Boolean CaseSensitive)
        {
            int line = SearchEntryLine(Section, Entry, CaseSensitive);
            if (line < 0) return false;
            lines.RemoveAt(line);
            return true;
        }

        public String getValue(String Section, String Entry, Boolean CaseSensitive)
        {
            return getValue(Section, Entry, CaseSensitive, String.Empty,true);
        }

        /// <summary>
        /// Liest den Wert eines Eintrages aus
        /// (Erweiterung: case sensitive)
        /// </summary>
        /// <param name="Section">Name des Bereiches</param>
        /// <param name="Entry">Name des Eintrages</param>
        /// <param name="CaseSensitive">true = Gross-/Kleinschreibung beachten</param>
        /// <returns>Wert des Eintrags oder leer</returns>
        public String getValue(String Section, String Entry, Boolean CaseSensitive,String defaultValue, bool add)
        {
            int line = SearchEntryLine(Section, Entry, CaseSensitive);
            if (line < 0)
            {
                if (add) 
                    setValue(Section, Entry, defaultValue, false);
                return defaultValue;
            }
            int pos = lines[line].IndexOf("=");
            if (pos < 0)
            {
                if (add)
                    setValue(Section, Entry, defaultValue, false);
                return defaultValue;
            }
            lastLine = line;
            string val = lines[line].Substring(pos + 1).Trim();
            if (!String.IsNullOrEmpty(val))
            {
                if (val[0] == '"')
                {
                    val = val.Substring(1);
                    if (val.IndexOf('"') > 0)
                        val = val.Substring(0, val.IndexOf('"'));
                }
            }
            return val;
        }

        /// <summary>
        /// Setzt einen Wert in einem Bereich. Wenn der Wert
        /// (und der Bereich) noch nicht existiert, werden die
        /// entsprechenden Einträge erstellt.
        /// </summary>
        /// <param name="Section">Name des Bereichs</param>
        /// <param name="Entry">name des Eintrags</param>
        /// <param name="Value">Wert des Eintrags</param>
        /// <param name="CaseSensitive">true = Gross-/Kleinschreibung beachten</param>
        /// <returns>true = Eintrag erfolgreich gesetzt</returns>
        public Boolean setValue(String Section, String Entry, String Value, Boolean CaseSensitive)
        {
            if (internalsLocked && readOnlyValues.Contains(Section.ToLowerInvariant() + "." + Entry.ToLowerInvariant()))
                throw new UnauthorizedAccessException("value locked");

            Section = Section.ToLower();
            if (!CaseSensitive) Entry = Entry.ToLower();
            int lastCommentedFound = -1;
            int SectionStart = SearchSectionLine(Section, false);
            if (SectionStart < 0)
            {
                lines.Add("[" + Section + "]");
                lines.Add(Entry + "=" + Value);
                lines.Add("");
                return true;
            }
            int EntryLine = SearchEntryLine(Section, Entry, CaseSensitive);
            int i = SectionStart + 1;
            for (i = SectionStart + 1; i < lines.Count; i++)
            {
                String line = lines[i].Trim();
                if (!CaseSensitive) line = line.ToLower();
                if (line == "") continue;
                // Ende, wenn der nächste Abschnitt beginnt
                if (line.StartsWith("["))
                {
                    //lines.Insert(i, Entry + "=" + Value);
                    break;
                }                
                // Suche aukommentierte, aber gesuchte Einträge
                // (evtl. per Parameter bestimmen können?), falls
                // der Eintrag noch nicht existiert.
                if (EntryLine < 0)
                    if (Regex.IsMatch(line, @"^[ \t]*[" + CommentCharacters + "]"))
                    {
                        String tmpLine = line.Substring(1).Trim();
                        if (tmpLine.StartsWith(Entry+"="))
                        {
                            // Werte vergleichen, wenn gleich,
                            // nur Kommentarzeichen löschen
                            int pos = tmpLine.IndexOf("=");
                            if (pos > 0)
                            {
                                if (Value == tmpLine.Substring(pos + 1).Trim())
                                {
                                    lines[i] = tmpLine;
                                    return true;
                                }
                            }
                            lastCommentedFound = i;
                        }
                        continue;// Kommentar
                    }
                if (line.StartsWith(Entry+"="))
                {
                    lines[i] = Entry + "=" + Value;
                    return true;
                }
            }
            if (lastCommentedFound > 0)
                lines.Insert(lastCommentedFound + 1, Entry + "=" + Value);
            else
            {
                while ((i > 0) && (String.IsNullOrEmpty(lines[i - 1]) || String.IsNullOrWhiteSpace(lines[i - 1])))
                    i--;
                lines.Insert(i, Entry + "=" + Value);
            }
            return true;
        }

        /// <summary>
        /// Liest alle Einträge uns deren Werte eines Bereiches aus
        /// </summary>
        /// <param name="Section">Name des Bereichs</param>
        /// <returns>Sortierte Liste mit Einträgen und Werten</returns>
        public SortedList<String, String> getSection(String Section)
        {
            SortedList<String, String> result = new SortedList<string, string>();
            Boolean SectionFound = false;
            for (int i = 0; i < lines.Count; i++)
            {
                String line = lines[i].Trim();
                if (line == "") continue;
                // Erst den gewünschten Abschnitt suchen
                if (!SectionFound)
                    if (line.ToLower() != "[" + Section + "]") continue;
                    else
                    {
                        SectionFound = true;
                        continue;
                    }
                // Ende, wenn der nächste Abschnitt beginnt
                if (line.StartsWith("[")) break;
                if (Regex.IsMatch(line, @"^[ \t]*[" + CommentCharacters + "]")) continue; // Kommentar
                int pos = line.IndexOf("=");
                if (pos < 0)
                    result.Add(line, "");
                else
                    result.Add(line.Substring(0, pos).Trim(), line.Substring(pos + 1).Trim());
            }
            return result;
        }

        /// <summary>
        /// Erstellt eine Liste aller enthaltenen Bereiche
        /// </summary>
        /// <returns>Liste mit gefundenen Bereichen</returns>
        public List<string> getAllSections()
        {
            List<string> result = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                String line = lines[i];
                Match mSection = regSection.Match(lines[i]);
                if (mSection.Success)
                    result.Add(mSection.Groups["section"].Value.Trim());
            }
            return result;
        }

        /// <summary>
        /// Exportiert sämtliche Bereiche und deren Werte
        /// in ein XML-Dokument
        /// </summary>
        /// <returns>XML-Dokument</returns>
        public XmlDocument exportToXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(
                Path.GetFileNameWithoutExtension(this.fileName));
            doc.AppendChild(root);
            XmlElement Section = null;
            for (int i = 0; i < lines.Count; i++)
            {
                Match mEntry = regEntry.Match(lines[i]);
                Match mSection = regSection.Match(lines[i]);
                if (mSection.Success)
                {
                    Section = doc.CreateElement(mSection.Groups["section"].Value.Trim());
                    root.AppendChild(Section);
                    continue;
                }
                if (mEntry.Success)
                {
                    XmlElement xe = doc.CreateElement(mEntry.Groups["entry"].Value.Trim());
                    xe.InnerXml = mEntry.Groups["value"].Value.Trim();
                    if (Section == null)
                        root.AppendChild(xe);
                    else
                        Section.AppendChild(xe);
                }
            }
            return doc;
        }

        public void Serialize(object graph)
        {
            var fieldInfos = graph.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            internalsLocked = false;
            foreach (var fieldInfo in fieldInfos)
            {
                // Field has no CfgSettings attached, so ignore it
                if (!Attribute.IsDefined(fieldInfo, typeof(CfgSettingAttribute)))
                {
                    continue;
                }

                CfgSettingAttribute attr = Attribute.GetCustomAttribute(fieldInfo, typeof(CfgSettingAttribute)) as CfgSettingAttribute;
                if (!readOnlyValues.Contains(attr.ToString()))
                    readOnlyValues.Add(attr.ToString());
                if (fieldInfo.FieldType.IsPrimitive || fieldInfo.FieldType.Namespace == "System")
                {
                    this.setValue(attr.Section, attr.Entry, Convert.ToString(fieldInfo.GetValue(graph), CultureInfo.InvariantCulture), false);
                }
                else
                {
                    object oVal = fieldInfo.GetValue(graph);
                    if (oVal is IList)
                    {
                        IList l = oVal as IList;
                        string s = String.Empty;
                        if (l.Count > 0)
                        {
                            s = Convert.ToString(l[0], CultureInfo.InvariantCulture);
                            for (int i = 1; i < l.Count; i++)
                                s += "," + Convert.ToString(l[i], CultureInfo.InvariantCulture);
                        }
                        this.setValue(attr.Section, attr.Entry, s, false);
                    }
                }
            }
            internalsLocked = true;
        }

        public void Deserialize(object graph)
        {
            var fieldInfos = graph.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            internalsLocked = false;
            foreach (var fieldInfo in fieldInfos)
            {
                // Field has no CfgSettings attached, so ignore it
                if (!Attribute.IsDefined(fieldInfo, typeof(CfgSettingAttribute)))
                {
                    continue;
                }
               
                CfgSettingAttribute attr = Attribute.GetCustomAttribute(fieldInfo, typeof(CfgSettingAttribute)) as CfgSettingAttribute;
                if (!readOnlyValues.Contains(attr.ToString()))
                    readOnlyValues.Add(attr.ToString());

                try
                {
                    if (fieldInfo.FieldType.IsPrimitive || fieldInfo.FieldType.Namespace == "System")
                    {
                        fieldInfo.SetValue(graph, Convert.ChangeType(this.getValue(attr.Section, attr.Entry, false, attr.DefaultValue,true), fieldInfo.FieldType, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        object oVal = fieldInfo.GetValue(graph);
                        if (oVal is IList)
                        {
                            List<string> s = new List<string>(this.getValue(attr.Section, attr.Entry, false, attr.DefaultValue,true).Split(','));
                            fieldInfo.SetValue(graph, Convert.ChangeType(s, fieldInfo.FieldType, CultureInfo.InvariantCulture));
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Error deserializing {0} line {1} : {2} ", fileName, lastLine, ex.ToString());
                    throw new FormatException(String.Format("Error parsing {0} line {1}", fileName, lastLine));
                }
            }
            internalsLocked = true;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class CfgSettingAttribute : Attribute
    {
        private string section = "Main";
        private string entry = "Entry";
        private string defaultValue = String.Empty;        
        
        public string Section
        {
            get { return section; }
            set { section = value; }
        }

        public string Entry
        {
            get { return entry; }
            set { entry = value; }
        }

        public string DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        public override string ToString()
        {
            return section.ToLowerInvariant() + "." + entry.ToLowerInvariant();
        }
    }

   
}
