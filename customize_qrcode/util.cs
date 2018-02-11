using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace customize_qrcode
{
    public class util
    {
        public static bool[] int_to_bytes(int data, int length)
        {
            return long_to_bytes(data, length);
        }
        public static bool[] long_to_bytes(long data, int length)
        {
            var ret = new bool[length];
            for (int i = length - 1; i >= 0; i--)
            {
                ret[i] = (data & 1) != 0 ? true : false;
                data >>= 1;
            }
            return ret;
        }

        public static byte[] bits_to_bytes(bool[] bits)
        {
            var ret = new byte[bits.Length >> 3];
            if ((bits.Length & 7) != 0)
                throw new ArgumentException("在转成byte数组前请填充对齐到byte");
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = (byte)(
                    (bits[i << 3] ? 128 : 0) |
                    (bits[(i << 3) + 1] ? 64 : 0) |
                    (bits[(i << 3) + 2] ? 32 : 0) |
                    (bits[(i << 3) + 3] ? 16 : 0) |
                    (bits[(i << 3) + 4] ? 8 : 0) |
                    (bits[(i << 3) + 5] ? 4 : 0) |
                    (bits[(i << 3) + 6] ? 2 : 0) |
                    (bits[(i << 3) + 7] ? 1 : 0)
                    );
            }
            return ret;
        }
        public static bool[] bytes_to_bits(byte[] bytes)
        {
            var ret = new bool[bytes.Length << 3];
            for (int i = 0; i < bytes.Length; i++)
            {
                ret[i << 3] = (bytes[i] & 128) != 0;
                ret[(i << 3) + 1] = (bytes[i] & 64) != 0;
                ret[(i << 3) + 2] = (bytes[i] & 32) != 0;
                ret[(i << 3) + 3] = (bytes[i] & 16) != 0;
                ret[(i << 3) + 4] = (bytes[i] & 8) != 0;
                ret[(i << 3) + 5] = (bytes[i] & 4) != 0;
                ret[(i << 3) + 6] = (bytes[i] & 2) != 0;
                ret[(i << 3) + 7] = (bytes[i] & 1) != 0;
            }
            return ret;
        }
    }
}
