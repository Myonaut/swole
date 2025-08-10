using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swole.Networking
{
    public static class NetworkUtils
    {
        private static readonly byte[] _buff32 = new byte[sizeof(int)];

        public static byte[] GetBytesLittleEndian(this int val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            if (!BitConverter.IsLittleEndian) Array.Reverse(intBytes);
            return intBytes;
        }
        public static void SetBytesLittleEndian(this int val, byte[] buffer, int startIndex)
        {
            byte[] intBytes = GetBytesLittleEndian(val);
            Array.Copy(intBytes, 0, buffer, startIndex, sizeof(int));
        }

        public static int IntFromBytes(byte[] bytes, bool isLittleEndian = true, int startIndex = 0)
        {
            if ((isLittleEndian && !BitConverter.IsLittleEndian) || (!isLittleEndian && BitConverter.IsLittleEndian)) Array.Reverse(bytes, startIndex, sizeof(int));
            return BitConverter.ToInt32(bytes, startIndex);
        }
        public static int IntFromBytes(ICollection<byte> bytes, bool isLittleEndian = true, int startIndex = 0)
        {
            lock (_buff32)
            {
                int i = 0;
                foreach (var b in bytes)
                {
                    if (i < startIndex) continue;
                    _buff32[i] = b;
                    i++;
                    if (i >= sizeof(int)) break;
                }
                return IntFromBytes(_buff32, isLittleEndian, 0);
            }
        }
        public static int IntFromBytes(Stream bytes, bool isLittleEndian = true, int startIndex = 0)
        {
            lock (_buff32)
            {
                bytes.Seek(startIndex, SeekOrigin.Begin);
                bytes.Read(_buff32, 0, sizeof(int));
                return IntFromBytes(_buff32, isLittleEndian, 0);
            }
        }
    }
}
