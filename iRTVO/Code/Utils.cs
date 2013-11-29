using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iRTVO
{
    static class ExtractHelper
    {

        public static IEnumerable<string> IterateProps(Type baseType)
        {
            return IteratePropsInner(baseType, baseType.Name);
        }

        private static IEnumerable<string> IteratePropsInner(Type baseType, string baseName)
        {
            var props = baseType.GetProperties();

            foreach (var property in props)
            {
                var name = property.Name;
                var type = ListArgumentOrSelf(property.PropertyType);
                if (IsMarked(type))
                    foreach (var info in IteratePropsInner(type, name))
                        yield return string.Format("{0}.{1}", baseName, info);
                else
                    yield return string.Format("{0}.{1}", baseName, property.Name);
                 
            }
        }
        
        static bool IsMarked(Type type)
        {
            return type.FullName.StartsWith("iRTVO");
//            return type.GetCustomAttributes(typeof(ExtractNameAttribute), true).Any();
        }

        
        public static Type ListArgumentOrSelf(Type type)
        {
           
            if (!type.IsGenericType)
                return type;
            if (type.GetGenericTypeDefinition() != typeof(List<>))
                throw new Exception("Only List<T> are allowed");
            return type.GetGenericArguments()[0];
        }
    }

    static class Utils
    {
        public static int padCarNum(string input)
        {
            int num = Int32.Parse(input);
            int zero = input.Length - num.ToString().Length;

            int retVal = num;
            int numPlace = 1;
            if (num > 99)
                numPlace = 3;
            else if (num > 9)
                numPlace = 2;
            if (zero > 0)
            {
                numPlace += zero;
                retVal = num + 1000 * numPlace;
            }

            return retVal;
        }

        public static string floatTime2String(Single time, Int32 showMilli, Boolean showMinutes)
        {
            time = Math.Abs(time);

            int hours = (int)Math.Floor(time / 3600);
            int minutes = (int)Math.Floor((time - (hours * 3600)) / 60);
            Double seconds = Math.Floor(time % 60);
            Double milliseconds = Math.Round(time * 1000 % 1000, 3);
            string output;

            if (time == 0.0)
                output = "-.--";
            else if (hours > 0)
            {
                output = String.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds);
            }
            else if (minutes > 0 || showMinutes)
            {
                if (showMilli > 0)
                    output = String.Format("{0}:{1:00." + "".PadLeft(showMilli, '0') + "}", minutes, seconds + milliseconds / 1000);
                else
                    output = String.Format("{0}:{1:00}", minutes, seconds);
            }

            else
            {
                if (showMilli > 0)
                    output = String.Format("{0:0." + "".PadLeft(showMilli, '0') + "}", seconds + milliseconds / 1000);
                else
                    output = String.Format("{0}", seconds);
            }

            return output;
        }

    }
   
}
