using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole
{
    public static class CSharpExtensions
    {

        public static System.Array Add(this System.Array array, object element, int index = -1)
        {

            int l = array.Length;

            System.Array newArray = System.Array.CreateInstance(array.GetType().GetElementType(), l + 1);

            index = index < 0 ? l : (index > l ? l : index);

            for (int a = 0; a < index; a++) newArray.SetValue(array.GetValue(a), a);

            for (int a = index; a < l; a++) newArray.SetValue(array.GetValue(a), a + 1);

            newArray.SetValue(element, index);

            return newArray;

        }

        public static System.Array Remove(this System.Array array, int index)
        {

            int l = array.Length;

            System.Array newArray = System.Array.CreateInstance(array.GetType().GetElementType(), l - 1);

            index = index < 0 ? 0 : (index >= l ? (l - 1) : index);

            for (int a = 0; a < index; a++) newArray.SetValue(array.GetValue(a), a);

            for (int a = index + 1; a < l; a++) newArray.SetValue(array.GetValue(a), a - 1);

            return newArray;

        }

        public static System.Array Invert(this System.Array array)
        {

            System.Array newArray = System.Array.CreateInstance(array.GetType().GetElementType(), array.Length);

            for (int a = 0; a < newArray.Length; a++)
            {

                newArray.SetValue(array.GetValue((newArray.Length - 1) - a), a);

            }

            return newArray;

        }

        public static bool TryParseFloatStrict(this string str, out float value)
        {
            value = 0;
            str = str.Trim();
            if (string.IsNullOrWhiteSpace(str) || str.StartsWith('.') || str.EndsWith('.')) return false;
            if (float.TryParse(str, out value)) return true;
            return false;
        }

    }
}
