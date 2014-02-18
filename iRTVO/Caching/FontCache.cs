using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace iRTVO.Caching
{
    public class FontCache : CacheBase<FontFamily>
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        string path;

        public FontCache() : base()
        {
            foreach (FontFamily f in Fonts.SystemFontFamilies)
            {

                logger.Trace("{0}={1}", f.BaseUri, f.Source);
            }
            Get("Arial");
        }
        public void SetPath(string p)
        {
            path = p;
        }

        public override FontFamily Get(string key)
        {
            if ( Contains(key) )
                return base.Get(key);
            FontFamily f;
            bool exists = (from ff in Fonts.SystemFontFamilies where ff.Source.Equals(key) select ff).Any();
            if ( exists )
                f = new FontFamily(key);
            else
            {
                logger.Info("Loading font '{0}' from {1} ", key, new Uri(path));
                f = new System.Windows.Media.FontFamily(new Uri(path+"./#" + key+".ttf"),key);
            }

            if (f == null)
            {
                logger.Warn("Could not load font family '{0}'. falling back to Arial", key);
                return base.Get("Arial");
            }
            logger.Info("Added '{0}' to FontCache", key);
            Add(key, f);
            return f;
        }
    }
}
