using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iRTVO
{
    public class localization
    {
        public enum Language 
        { 
            Finnish = 0, 
            English = 1
        }
        private Language lang = Language.English;

        public localization()
        {
            Language lang = (Language)Properties.Settings.Default.language;
        }

        void setLang(Language newLang) {
            lang = newLang;
        }

        public string lap_short
        {
            get
            {
                switch (lang)
                {
                    case Language.Finnish:
                        return " kier.";
                    default:
                        return " lap";
                }
            }
        }

        public string out_short
        {
            get
            {
                switch (lang)
                {
                    case Language.Finnish:
                        return "keskeyt.";
                    default:
                        return "out";
                }
            }
        }

        public string practice_results
        {
            get
            {
                switch (lang)
                {
                    case Language.Finnish:
                        return "Harjoitusten tulokset";
                    default:
                        return "Practice results";
                }
            }
        }

        public string qualify_results
        {
            get
            {
                switch (lang)
                {
                    case Language.Finnish:
                        return "Aika-ajon tulokset";
                    default:
                        return "Qualify results";
                }
            }
        }

        public string race_results
        {
            get
            {
                switch (lang)
                {
                    case Language.Finnish:
                        return "Kilpailun tulokset";
                    default:
                        return "Race results";
                }
            }
        }
    }
}
