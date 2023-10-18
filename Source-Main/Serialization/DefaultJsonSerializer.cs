using System;
using System.Text;

using Newtonsoft.Json;

namespace Swole
{

    public static class DefaultJsonSerializer
    {

        public static Encoding StringEncoder => Encoding.UTF8;

        public static string ToJson(object obj, bool prettyPrint = false) { return JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None); }

        public static object FromJson(string json, Type type) { return JsonConvert.DeserializeObject(json, type); }

        public static T FromJson<T>(string json) { return JsonConvert.DeserializeObject<T>(json); }

    }

}
