#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public static class ExternalUtils
    {

        /// <summary>
        /// Source: https://stackoverflow.com/questions/7568147/compare-version-numbers-without-using-split-function
        /// Compare two version strings, e.g.  "3.2.1.0.b40" and "3.10.1.a".
        /// V1 and V2 can have different number of components.
        /// Components must be delimited by dot.
        /// </summary>
        /// <remarks>
        /// This doesn't do any null/empty checks so please don't pass dumb parameters
        /// </remarks>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>
        /// -1 if v1 is lower version number than v2,
        /// 0 if v1 == v2,
        /// 1 if v1 is higher version number than v2,
        /// -1000 if we couldn't figure it out (something went wrong)
        /// </returns>
        public static int CompareVersionStrings(string v1, string v2)
        {
            int rc = -1000;

            v1 = v1.ToLower();
            v2 = v2.ToLower();

            if (v1 == v2)
                return 0;

            string[] v1parts = v1.Split('.');
            string[] v2parts = v2.Split('.');

            for (int i = 0; i < v1parts.Length; i++)
            {
                if (v2parts.Length < i + 1)
                    break; // we're done here

                string v1Token = v1parts[i];
                string v2Token = v2parts[i];

                int x;
                bool v1Numeric = int.TryParse(v1Token, out x);
                bool v2Numeric = int.TryParse(v2Token, out x);

                // handle scenario {"2" versus "20"} by prepending zeroes, e.g. it would become {"02" versus "20"}
                if (v1Numeric && v2Numeric)
                {
                    while (v1Token.Length < v2Token.Length)
                        v1Token = "0" + v1Token;
                    while (v2Token.Length < v1Token.Length)
                        v2Token = "0" + v2Token;
                }

                rc = String.Compare(v1Token, v2Token, StringComparison.Ordinal);
                //Console.WriteLine("v1Token=" + v1Token + " v2Token=" + v2Token + " rc=" + rc);
                if (rc != 0)
                    break;
            }

            if (rc == 0)
            {
                // catch this scenario: v1="1.0.1" v2="1.0"
                if (v1parts.Length > v2parts.Length)
                    rc = 1; // v1 is higher version than v2
                            // catch this scenario: v1="1.0" v2="1.0.1"
                else if (v2parts.Length > v1parts.Length)
                    rc = -1; // v1 is lower version than v2
            }

            if (rc == 0 || rc == -1000)
                return rc;
            else
                return rc < 0 ? -1 : 1;

        }

        public class VersionComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return CompareVersionStrings(x, y);
            }

        }

        /// <summary>
        /// Source: https://stackoverflow.com/questions/21769532/copying-all-class-fields-and-properties-to-another-class
        /// </summary>
        public static void CopyAll<T>(T source, T target)
        {
            var type = typeof(T);
            foreach (var sourceProperty in type.GetProperties())
            {
                try
                {
                    var targetProperty = type.GetProperty(sourceProperty.Name);
                    if (targetProperty.GetSetMethod() == null || targetProperty.GetGetMethod() == null) continue;
                    targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
                }
                catch { }
            }
            foreach (var sourceField in type.GetFields())
            {
                try
                {
                    var targetField = type.GetField(sourceField.Name);
                    targetField.SetValue(target, sourceField.GetValue(source));
                }
                catch { }

            }
        }

    }

}

#endif
