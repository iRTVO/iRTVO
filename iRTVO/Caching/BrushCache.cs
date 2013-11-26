using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace iRTVO.Caching
{
    public class BrushCache : CacheBase<Brush>
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public BrushCache()
        {

        }
       
        public Brush GetDynamicBrush(string primary, Brush defaultBrush)
        {
            return GetDynamicBrush(primary, String.Empty, defaultBrush);
        }

        public Brush GetDynamicBrush(string primary, string secondary, Brush defaultBrush)
        {
            logger.Debug("GetDynamicBrush('{0}','{1}',...)", primary, secondary, defaultBrush);
            if (!String.IsNullOrEmpty(primary))
            {
                if (Contains(primary))
                {
                    return Get(primary);
                }
                if (!String.IsNullOrEmpty(primary) && File.Exists(primary))
                {
                    Brush bg = new ImageBrush(new BitmapImage(new Uri(primary)));
                    Add(primary, bg);
                    return bg;
                }
            }
            if (!String.IsNullOrEmpty(secondary))
            {
                if (Contains(secondary))
                    return Get(secondary);
                if (File.Exists(secondary))
                {
                    Brush bg = new ImageBrush(new BitmapImage(new Uri(secondary)));
                    Add(primary, bg);
                    return bg;
                }
            }
            return defaultBrush;
        }
    }
}
