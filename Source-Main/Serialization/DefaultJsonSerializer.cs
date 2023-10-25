using System;
using System.Text;

#if SWOLE_ENV
using Newtonsoft.Json;
#endif

namespace Swole
{

    public static class DefaultJsonSerializer
    {

        public static Encoding StringEncoder => Encoding.UTF8;

        public static string ToJson(object obj, bool prettyPrint = false) 
        {
#if SWOLE_ENV
            return JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None);
#else
            return string.Empty;
#endif
        }

        public static object FromJson(string json, Type type) 
        {
#if SWOLE_ENV
            return JsonConvert.DeserializeObject(json, type);
#else
            return default;
#endif
        }

        public static T FromJson<T>(string json) 
        {
#if SWOLE_ENV
            return JsonConvert.DeserializeObject<T>(json);
#else
            return default;
#endif
        }

    }

}
