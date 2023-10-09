using System;
using Newtonsoft.Json;

namespace Swole
{

    public static class DefaultJsonSerializer
    {

        public static string ToJson(object obj, bool prettyPrint = false) { return ""; }

        public static object FromJson(string json, Type type) { return null; }

        public static T FromJson<T>(string json) 
        { 
            return default; 
        }

    }

}
