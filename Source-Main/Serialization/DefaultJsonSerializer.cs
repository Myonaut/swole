#if !(UNITY_EDITOR || UNITY_STANDALONE)
#define USE_NEWTONSOFT
using Newtonsoft.Json;
#endif

using System;
using System.Text;

namespace Swole
{

    public static class DefaultJsonSerializer
    {

        public static Encoding StringEncoder => Encoding.UTF8;

        public static string ToJson(object obj, bool prettyPrint = false) 
        {
#if USE_NEWTONSOFT
            return JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None);
#else
            return string.Empty;
#endif
        }

        public static object FromJson(string json, Type type) 
        {
#if USE_NEWTONSOFT
            return JsonConvert.DeserializeObject(json, type);
#else
            return default;
#endif
        }

        public static T FromJson<T>(string json) 
        {
#if USE_NEWTONSOFT
            return JsonConvert.DeserializeObject<T>(json);
#else
            return default;
#endif
        }

    }

}
