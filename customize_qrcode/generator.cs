using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace customize_qrcode
{
    public class Generator
    {
        #region Mode Indicator
        //ECI
        private const int ECI_MODE = 0x7;
        //数字
        private const int NUMERIC_MODE = 0x1;
        //混合字符模式
        private const int ALPHANUMERIC_MODE = 0x2;
        //8bit byte
        private const int BYTE_MODE = 0x4;
        //日本汉字(KANJI)
        private const int KANJI_MODE = 0x8;
        //中国汉字(GB3212)
        private const int GB2312_MODE = 0xd;
        //结构链接
        private const int STRUCTURED_APPEND_MODE = 0x3;
        //FNC1
        private const int FNC1_MODE = 0x59;
        #endregion

        #region Char Count Indicator
        private static int _char_count_indicator(int version, int mode)
        {
            if (version < 1)
                throw new ArgumentOutOfRangeException("Version不能小于1");
            else if (version < 10)
                switch (mode)
                {
                    case NUMERIC_MODE:
                        return 10;
                    case ALPHANUMERIC_MODE:
                        return 9;
                    case BYTE_MODE:
                        return 8;
                    case KANJI_MODE:
                        return 8;
                    default:
                        throw new ArgumentException("模式错误");
                }
            else if (version < 27)
                switch (mode)
                {
                    case NUMERIC_MODE:
                        return 12;
                    case ALPHANUMERIC_MODE:
                        return 11;
                    case BYTE_MODE:
                        return 16;
                    case KANJI_MODE:
                        return 10;
                    default:
                        throw new ArgumentException("模式错误");
                }
            else if (version < 41)
                switch (mode)
                {
                    case NUMERIC_MODE:
                        return 14;
                    case ALPHANUMERIC_MODE:
                        return 13;
                    case BYTE_MODE:
                        return 16;
                    case KANJI_MODE:
                        return 12;
                    default:
                        throw new ArgumentException("模式错误");
                }
            else
                throw new ArgumentOutOfRangeException("Version不能大于40");
        }
        #endregion

        #region Capacity Table
        //这个表按CAPACITY_TABLE[version][level]获得指定的数据容量（byte）单位
        private readonly static int[][] CAPACITY_TABLE = new int[][]
        {
            new int[] {}, //version 0, not identified
            new int[] {19,16,13,9}, //version 1, L M Q H ordered
            new int[] {34,28,22,16}, //version 2
            new int[] {55,44,34,26}, //version 3
            new int[] {80,64,48,36}, //version 4
            new int[] {108,86,62,46}, //version 5
            new int[] {136,108,76,60}, //version 6
            new int[] {156,124,88,66}, //version 7
            new int[] {194,154,110,86}, //version 8
            new int[] {232,182,132,100}, //version 9
            new int[] {274,216,154,122}, //version 10
            new int[] {324,254,180,140}, //version 11
            new int[] {370,290,206,158}, //version 12
            new int[] {428,334,244,180}, //version 13
            new int[] {461,365,261,197}, //version 14
            new int[] {523,415,295,223}, //version 15
            new int[] {589,453,325,253}, //version 16
            new int[] {647,507,367,283}, //version 17
            new int[] {721,563,397,313}, //version 18
            new int[] {795,627,445,341}, //version 19
            new int[] {861,669,485,385}, //version 20
            new int[] {932,714,512,406}, //version 21
            new int[] {1006,782,568,442}, //version 22
            new int[] {1094,860,614,464}, //version 23
            new int[] {1174,914,664,514}, //version 24
            new int[] {1276,1000,718,538}, //version 25
            new int[] {1370,1062,754,596}, //version 26
            new int[] {1468,1128,808,628}, //version 27
            new int[] {1531,1193,871,661}, //version 28
            new int[] {1631,1267,911,701}, //version 29
            new int[] {1735,1373,985,745}, //version 30
            new int[] {1843,1455,1033,793}, //version 31
            new int[] {1955,1541,1115,845}, //version 32
            new int[] {2071,1631,1171,901}, //version 33
            new int[] {2191,1725,1231,961}, //version 34
            new int[] {2306,1812,1286,986}, //version 35
            new int[] {2434,1914,1354,1054}, //version 36
            new int[] {2566,1992,1426,1096}, //version 37
            new int[] {2702,2102,1502,1142}, //version 38
            new int[] {2812,2216,1582,1222}, //version 39
            new int[] {2956,2334,1666,1276} //version 40
        };
        #endregion

        #region EC Table
        //这个表按EC_TABLE[version][level]来获取对应的EC(Error Correction数据)
        //数据内容分别为：
        //[0] 第一组(Group 1)的区块数(Blocks)
        //[1] 第一组(Group 1)每个区块的数据字节数
        //[2] 第二组的区块数(不存在时为0)
        //[3] 第二组每个区块的数据字节数
        //注：EC_TABLE[0][version][level]存放的是每个区块的纠错字节数(EC Codewords Per Block)
        private readonly static int[][][] EC_TABLE = new int[][][]
        {
            new int[][] { new int[] { },
                new int[] {7,10,13,17},new int[] {10,16,22,28},new int[] {15,26,18,22},new int[] {20,18,26,16},new int[] {26,24,18,22}, //version 1 - 5
                new int[] {18,16,24,28},new int[] {20,18,18,26},new int[] {24,22,22,26},new int[] {30,22,20,24}, new int[] {18,26,24,28}, //version 6 - 10
                new int[] {20,30,28,24},new int[] {24,22,26,28},new int[] {26,22,24,22},new int[] {30,24,20,24}, new int[] {22,24,30,24}, //version 11 - 15
                new int[] {24,28,24,30},new int[] {28,28,28,28},new int[] {30,26,28,28},new int[] {28,26,26,26}, new int[] {28,26,30,28}, //version 16 - 20
                new int[] {28,26,28,30},new int[] {28,28,30,24},new int[] {30,28,30,30},new int[] {30,28,30,30}, new int[] {26,28,30,30}, //version 21 - 25
                new int[] {28,28,28,30},new int[] {30,28,30,30},new int[] {30,28,30,30},new int[] {30,28,30,30}, new int[] {30,28,30,30}, //version 26 - 30
                new int[] {30,28,30,30},new int[] {30,28,30,30},new int[] {30,28,30,30},new int[] {30,28,30,30}, new int[] {30,28,30,30}, //version 31 - 35
                new int[] {30,28,30,30},new int[] {30,28,30,30},new int[] {30,28,30,30},new int[] {30,28,30,30}, new int[] {30,28,30,30}, //version 36 - 40
            }, //EC codewords per block
            new int[][] {new int[] {1,19,0,0},new int[] {1,16,0,0},new int[] {1,13,0,0},new int[] {1,9,0,0}}, //version 1
            new int[][] {new int[] {1,34,0,0},new int[] {1,28,0,0},new int[] {1,22,0,0},new int[] {1,16,0,0}}, //version 2
            new int[][] {new int[] {1,55,0,0},new int[] {1,44,0,0},new int[] {2,17,0,0},new int[] {2,13,0,0}}, //version 3
            new int[][] {new int[] {1,80,0,0},new int[] {2,32,0,0},new int[] {2,24,0,0},new int[] {4,9,0,0}}, //version 4
            new int[][] {new int[] {1,108,0,0},new int[] {2,43,0,0},new int[] {2,15,2,16},new int[] {2,11,2,12}}, //version 5
            new int[][] {new int[] {2,68,0,0},new int[] {4,27,0,0},new int[] {4,19,0,0},new int[] {4,15,0,0}}, //version 6
            new int[][] {new int[] {2,78,0,0},new int[] {4,31,0,0},new int[] {2,14,4,15},new int[] {4,13,1,14}}, //version 7
            new int[][] {new int[] {2,97,0,0},new int[] {2,38,2,39},new int[] {4,18,2,19},new int[] {4,14,2,15}}, //version 8
            new int[][] {new int[] {2,116,0,0},new int[] {3,36,2,37},new int[] {4,16,4,17},new int[] {4,12,4,13}}, //version 9
            new int[][] {new int[] {2,68,2,69},new int[] {4,43,1,44},new int[] {6,19,2,20},new int[] {6,15,2,16}}, //version 10
            new int[][] {new int[] {4,81,0,0},new int[] {1,50,4,51},new int[] {4,22,4,23},new int[] {3,12,8,13}}, //version 11
            new int[][] {new int[] {2,92,2,93},new int[] {6,36,2,37},new int[] {4,20,6,21},new int[] {7,14,4,15}}, //version 12
            new int[][] {new int[] {4,107,0,0},new int[] {8,37,1,38},new int[] {8,20,4,21},new int[] {12,11,4,12}}, //version 13
            new int[][] {new int[] {3,115,1,116},new int[] {4,40,5,41},new int[] {11,16,5,17},new int[] {11,12,5,13}}, //version 14
            new int[][] {new int[] {5,87,1,88},new int[] {5,41,5,42},new int[] {5,24,7,25},new int[] {11,12,7,13}}, //version 15
            new int[][] {new int[] {5,98,1,99},new int[] {7,45,3,46},new int[] {15,19,2,20},new int[] {3,15,13,16}}, //version 16
            new int[][] {new int[] {1,107,5,108},new int[] {10,46,1,47},new int[] {1,22,15,23},new int[] {2,14,17,15}}, //version 17
            new int[][] {new int[] {5,120,1,121},new int[] {9,43,4,44},new int[] {17,22,1,23},new int[] {2,14,19,15}}, //version 18
            new int[][] {new int[] {3,113,4,114},new int[] {3,44,11,45},new int[] {17,21,4,22},new int[] {9,13,16,14}}, //version 19
            new int[][] {new int[] {3,107,5,108},new int[] {3,41,13,42},new int[] {15,24,5,25},new int[] {15,15,10,16}}, //version 20
            new int[][] {new int[] {4,116,4,117},new int[] {17,42,0,0},new int[] {17,22,6,23},new int[] {19,16,6,17}}, //version 21
            new int[][] {new int[] {2,111,7,112},new int[] {17,46,0,0},new int[] {7,24,16,25},new int[] {34,13,0,0}}, //version 22
            new int[][] {new int[] {4,121,5,122},new int[] {4,47,14,48},new int[] {11,24,14,25},new int[] {16,15,14,16}}, //version 23
            new int[][] {new int[] {6,117,4,118},new int[] {6,45,14,46},new int[] {11,24,16,25},new int[] {30,16,2,17}}, //version 24
            new int[][] {new int[] {8,106,4,107},new int[] {8,47,13,48},new int[] {7,24,22,25},new int[] {22,15,13,16}}, //version 25
            new int[][] {new int[] {10,114,2,115},new int[] {19,46,4,47},new int[] {28,22,6,23},new int[] {33,16,4,17}}, //version 26
            new int[][] {new int[] {8,122,4,123},new int[] {22,45,3,46},new int[] {8,23,26,24},new int[] {12,15,28,16}}, //version 27
            new int[][] {new int[] {3,117,10,118},new int[] {3,45,23,46},new int[] {4,24,31,25},new int[] {11,15,31,16}}, //version 28
            new int[][] {new int[] {7,116,7,117},new int[] {21,45,7,46},new int[] {1,23,37,24},new int[] {19,15,26,16}}, //version 29
            new int[][] {new int[] {5,115,10,116},new int[] {19,47,10,48},new int[] {15,24,25,25},new int[] {23,15,25,16}}, //version 30
            new int[][] {new int[] {13,115,3,116},new int[] {2,46,29,47},new int[] {42,24,1,25},new int[] {23,15,28,16}}, //version 31
            new int[][] {new int[] {17,115,0,0},new int[] {10,46,23,47},new int[] {10,24,35,25},new int[] {19,15,35,16}}, //version 32
            new int[][] {new int[] {17,115,1,116},new int[] {14,46,21,47},new int[] {29,24,19,25},new int[] {11,15,46,16}}, //version 33
            new int[][] {new int[] {13,115,6,116},new int[] {14,46,23,47},new int[] {44,24,7,25},new int[] {59,16,1,17}}, //version 34
            new int[][] {new int[] {12,121,7,122},new int[] {12,47,26,48},new int[] {39,24,14,25},new int[] {22,15,41,16}}, //version 35
            new int[][] {new int[] {6,121,14,122},new int[] {6,47,34,48},new int[] {46,24,10,25},new int[] {2,15,64,16}}, //version 36
            new int[][] {new int[] {17,122,4,123},new int[] {29,46,14,47},new int[] {49,24,10,25},new int[] {24,15,46,16}}, //version 37
            new int[][] {new int[] {4,122,18,123},new int[] {13,46,32,47},new int[] {48,24,14,25},new int[] {42,15,32,16}}, //version 38
            new int[][] {new int[] {20,117,4,118},new int[] {40,47,7,48},new int[] {43,24,22,25},new int[] {10,15,67,16}}, //version 39
            new int[][] {new int[] {19,118,6,119},new int[] {18,47,31,48},new int[] {34,24,34,25},new int[] {20,15,61,16}} //version 40
        };
        #endregion

        //用于校验EC表和容量表是否一致，并没有什么卵用
        private static void _check_ec_table()
        {
            for (int lv = 1; lv < 41; lv++)
            {
                for (int i = 0; i < 4; i++)
                {
                    var len = CAPACITY_TABLE[lv][i];
                    var data = EC_TABLE[lv][i];
                    if (data[0] * data[1] + data[2] * data[3] != len)
                        throw new ArgumentException("Error occured at version " + lv + ", correct level: " + i);
                }
            }
        }

        #region Galois Field Table
        //GF(256)的对数表
        private readonly static byte[] GALOIS_FIELD_LOG_TABLE = _init_galois_field_log_table();
        private static byte[] _init_galois_field_log_table()
        {
            var ret = new byte[256];
            int e = 1;
            for (int i = 0; i < 256; i++)
            {
                if (e > 255)
                    e ^= 0x11d;
                ret[e] = (byte)i;
                e <<= 1;
            }
            ret[1] = 0; //因0->1, 255->1映射冲突，故舍去1->255，保留1->0
            return ret;
        }
        //GF(256)的指数表
        private readonly static byte[] GALOIS_FIELD_EXPONENT_TABLE = _init_galois_field_exponent_table();
        private static byte[] _init_galois_field_exponent_table()
        {
            var ret = new byte[256];
            int e = 1;
            for (int i = 0; i < 256; i++)
            {
                if (e > 255)
                    e ^= 0x11d;
                ret[i] = (byte)e;
                e <<= 1;
            }
            return ret;
        }
        #endregion


        #region data encode
        private static int _encode_alphanumeric_char(char ch)
        {
            if (ch >= '0' && ch <= '9')
                return ch - '0';
            else if (ch >= 'a' && ch <= 'z')
                return ch - 'a' + 10;
            switch (ch)
            {
                case ' ':
                    return 36;
                case '$':
                    return 37;
                case '%':
                    return 38;
                case '*':
                    return 39;
                case '+':
                    return 40;
                case '-':
                    return 41;
                case '.':
                    return 42;
                case '/':
                    return 43;
                case ':':
                    return 44;
                default:
                    throw new InvalidDataException("输入字符 '" + ch + "' 未包含在混合字符表中，不能使用该模式进行编码");
            }

        }
        private static byte[] _encode_alphanumeric(string alphas, int version, int correct_level)
        {
            int data_length = alphas.Length; //源字符串长度(bytes)
            int char_count_length = _char_count_indicator(version, ALPHANUMERIC_MODE); //字符个数占用长度(bits)
            int encoded_length =
                (11 * (data_length >> 1) + ((data_length & 1) == 1 ? 6 : 0)) //encoded data
                + 4 //mode indicator
                + char_count_length //character count indicator
                ; //编码后的长度(bits, 不包含终止的0bit)

            int terminate_length = Math.Min(4, (CAPACITY_TABLE[version][correct_level] << 3) - encoded_length); //终止符长度
            if (terminate_length < 0)
                throw new ArgumentOutOfRangeException("该version的qrcode未能容纳全部内容，请增大version"); //内容溢出异常
            encoded_length += terminate_length; //编码后的长度加上终止符长度

            int padding_bits_length = ((int)Math.Ceiling(encoded_length / 8.0) << 3) - encoded_length; //字节对齐的长度（以0填充）
            encoded_length += padding_bits_length;

            int padding_bytes_length = CAPACITY_TABLE[version][correct_level] - (encoded_length >> 3); //对其指定version和纠错等级的字节数
            encoded_length += padding_bytes_length << 3;

            alphas = alphas.ToLower();
            var ret = new List<bool>(encoded_length);
            //模式标识符
            ret.AddRange(util.int_to_bytes(ALPHANUMERIC_MODE, 4));
            //长度
            ret.AddRange(util.int_to_bytes(data_length, char_count_length));
            //完整的2个字符长度的子串
            int temp = data_length >> 1;
            for (int i = 0; i < temp; i++)
            {
                char first = alphas[i << 1];
                char second = alphas[(i << 1) + 1];
                int encoded_data = _encode_alphanumeric_char(first) * 45 + _encode_alphanumeric_char(second);
                ret.AddRange(util.int_to_bytes(encoded_data, 11));
            }
            //末尾的字符
            if ((data_length & 1) == 1)
            {
                int encoded_data = _encode_alphanumeric_char(alphas[data_length - 1]);
                ret.AddRange(util.int_to_bytes(encoded_data, 6));
            }
            //填充终止符
            if (terminate_length > 0)
            {
                ret.AddRange(new bool[terminate_length]);
            }
            //byte对齐
            if (padding_bits_length > 0)
            {
                ret.AddRange(new bool[padding_bits_length]);
            }
            //填充到指定version的长度
            for (int i = 0; i < padding_bytes_length; i++)
            {
                ret.AddRange(util.int_to_bytes((i & 1) == 0 ? 0xec : 0x11, 8));
            }
            return util.bits_to_bytes(ret.ToArray());
        }
        private static byte[] _encode_numeric(string num, int version, int correct_level)
        {
            int data_length = num.Length;
            int char_count_length = _char_count_indicator(version, NUMERIC_MODE);
            int mod = data_length % 3;
            int full_count = data_length / 3;
            int[] mod_length = { 0, 4, 7 };
            int encoded_length = (10 * full_count + mod_length[mod]) + 4 + char_count_length;

            int terminate_length = Math.Min(4, (CAPACITY_TABLE[version][correct_level] << 3) - encoded_length); //终止符长度
            if (terminate_length < 0)
                throw new ArgumentOutOfRangeException("该version的qrcode未能容纳全部内容，请增大version"); //内容溢出异常
            encoded_length += terminate_length; //编码后的长度加上终止符长度

            int padding_bits_length = ((int)Math.Ceiling(encoded_length / 8.0) << 3) - encoded_length; //字节对齐的长度（以0填充）
            encoded_length += padding_bits_length;

            int padding_bytes_length = CAPACITY_TABLE[version][correct_level] - (encoded_length >> 3); //对其指定version和纠错等级的字节数
            encoded_length += padding_bytes_length << 3;

            var ret = new List<bool>(encoded_length);
            //mode
            ret.AddRange(util.int_to_bytes(NUMERIC_MODE, 4));
            //length
            ret.AddRange(util.int_to_bytes(data_length, char_count_length));
            //完整的3字符数字
            for (int i = 0; i < full_count; i++)
            {
                var temp_str = num.Substring(i * 3, 3);
                int encoded_data = int.Parse(temp_str);
                ret.AddRange(util.int_to_bytes(encoded_data, 10));
            }
            if (mod != 0)
            {
                var temp_str = num.Substring(data_length - mod, mod);
                int encoded_data = int.Parse(temp_str);
                ret.AddRange(util.int_to_bytes(encoded_data, mod_length[mod]));
            }
            //填充终止符
            if (terminate_length > 0)
            {
                ret.AddRange(new bool[terminate_length]);
            }
            //byte对齐
            if (padding_bits_length > 0)
            {
                ret.AddRange(new bool[padding_bits_length]);
            }
            //填充到指定version的长度
            for (int i = 0; i < padding_bytes_length; i++)
            {
                ret.AddRange(util.int_to_bytes((i & 1) == 0 ? 0xec : 0x11, 8));
            }
            return util.bits_to_bytes(ret.ToArray());
        }

        private static byte[] _encode_byte(string str, int version, int correct_level)
        {
            var bin = Encoding.UTF8.GetBytes(str);
            int data_length = bin.Length; //源字符串长度(bytes)
            int char_count_length = _char_count_indicator(version, BYTE_MODE); //字符个数占用长度(bits)
            int encoded_length =
                (data_length << 3) //encoded data
                + 4 //mode indicator
                + char_count_length //character count indicator
                ; //编码后的长度(bits, 不包含终止的0bit)

            int terminate_length = Math.Min(4, (CAPACITY_TABLE[version][correct_level] << 3) - encoded_length); //终止符长度
            if (terminate_length < 0)
                throw new ArgumentOutOfRangeException("该version的qrcode未能容纳全部内容，请增大version"); //内容溢出异常
            encoded_length += terminate_length; //编码后的长度加上终止符长度

            int padding_bits_length = ((int)Math.Ceiling(encoded_length / 8.0) << 3) - encoded_length; //字节对齐的长度（以0填充）
            encoded_length += padding_bits_length;

            int padding_bytes_length = CAPACITY_TABLE[version][correct_level] - (encoded_length >> 3); //对其指定version和纠错等级的字节数
            encoded_length += padding_bytes_length << 3;

            var ret = new List<bool>(encoded_length);
            //模式标识符
            ret.AddRange(util.int_to_bytes(BYTE_MODE, 4));
            //长度
            ret.AddRange(util.int_to_bytes(data_length, char_count_length));
            //数据
            for (int i = 0; i < data_length; i++)
            {
                ret.AddRange(util.int_to_bytes(bin[i], 8));
            }
            //填充终止符
            if (terminate_length > 0)
            {
                ret.AddRange(new bool[terminate_length]);
            }
            //byte对齐
            if (padding_bits_length > 0)
            {
                ret.AddRange(new bool[padding_bits_length]);
            }
            //填充到指定version的长度
            for (int i = 0; i < padding_bytes_length; i++)
            {
                ret.AddRange(util.int_to_bytes((i & 1) == 0 ? 0xec : 0x11, 8));
            }
            return util.bits_to_bytes(ret.ToArray());
        }
        #endregion


        #region error correction encode
        private static Dictionary<int, byte[]> _polynomial_cache = new Dictionary<int, byte[]>();

        //生成多项式系数
        private static byte[] _generate_polynomial(int word_count)
        {
            if (_polynomial_cache.ContainsKey(word_count))
                return _polynomial_cache[word_count];
            //递归初始点
            if (word_count == 1)
            {
                var data = new byte[] { 0, 0 };
                _polynomial_cache.Add(word_count, data);
                return data;
            }
            var last_polynomial_data = _generate_polynomial(word_count - 1); //上一个因式的系数


            var new_polynomial_data = new byte[word_count + 1];
            //因式合并
            //x^(n+1)项恒为0
            new_polynomial_data[0] = 0;
            //x^n ~ x^1项
            for (int i = 1; i < word_count; i++)
            {
                new_polynomial_data[i] = (GALOIS_FIELD_LOG_TABLE[
                    (GALOIS_FIELD_EXPONENT_TABLE[last_polynomial_data[i]]) ^
                    (GALOIS_FIELD_EXPONENT_TABLE[(last_polynomial_data[i - 1] + word_count - 1) % 255])
                    ]);
            }
            //x^0 (常数)项
            new_polynomial_data[word_count] = (byte)(word_count - 1 + last_polynomial_data[word_count - 1]);

            _polynomial_cache.Add(word_count, new_polynomial_data);
            return new_polynomial_data;
        }
        //两个多项式进行相除
        private static byte[] _polynomial_division(byte[] a, byte[] b, int loop_steps, int polynomial_length)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("多项式相除前请对齐两项系数的长度（补0）");

            byte[] temp_a = new byte[a.Length];
            Array.Copy(a, temp_a, a.Length);
            for (int step = 0; step < loop_steps; step++)
            {
                byte val = GALOIS_FIELD_LOG_TABLE[temp_a[step]];
                //将α^val乘到b的α系数上
                for (int i = 0; i < polynomial_length; i++)
                {
                    temp_a[i + step] ^= GALOIS_FIELD_EXPONENT_TABLE[(b[i] + val) % 255];
                }
            }

            return temp_a;
        }

        //生成错误校验码数据
        private static byte[] _encode_ec_data(byte[] origin, int version, int correct_level)
        {
            var dst_data_length_per_block = EC_TABLE[0][version][correct_level]; //每个区块的字节数

            var message_polynomial = origin;
            var generated_polynomial = _generate_polynomial(dst_data_length_per_block); //获取生成的多项式系数

            var padding_length = message_polynomial.Length + dst_data_length_per_block; //系数对齐
            var new_message_polynomial = new byte[padding_length];
            var new_generated_polynomial = new byte[padding_length];
            Array.Copy(message_polynomial, new_message_polynomial, message_polynomial.Length);
            Array.Copy(generated_polynomial, new_generated_polynomial, generated_polynomial.Length);

            var result = _polynomial_division(new_message_polynomial, new_generated_polynomial, message_polynomial.Length, generated_polynomial.Length);
            var padded_result = new byte[dst_data_length_per_block];
            Array.Copy(result, message_polynomial.Length, padded_result, 0, dst_data_length_per_block);

            return padded_result;
        }
        #endregion


        #region structure final message
        private readonly static int[] REMAINDER_BIT = new int[]
        {
            0, //version 0
            0,7,7,7,7,7,0,0,0,0, //version 1 - 10
            0,0,0,3,3,3,3,3,3,3, //version 11 - 20
            4,4,4,4,4,4,4,3,3,3, //version 21 - 30
            3,3,3,3,0,0,0,0,0,0 //version 31 - 40
        };
        //将数据分块
        private static byte[][] _block_dispatch(byte[] origin, int version, int correct_level)
        {
            var group_data = EC_TABLE[version][correct_level]; //分块的组数据
            int group1_block_count = group_data[0], group2_block_count = group_data[2];
            int group1_block_size = group_data[1], group2_block_size = group_data[3];

            var ret = new byte[group1_block_count + group2_block_count][];
            var offset = 0;
            for (int i = 0; i < group1_block_count; i++)
            {
                ret[i] = new byte[group1_block_size];
                Array.Copy(origin, offset, ret[i], 0, group1_block_size);
                offset += group1_block_size;
            }
            for (int i = 0; i < group2_block_count; i++)
            {
                ret[i + group1_block_count] = new byte[group2_block_size];
                Array.Copy(origin, offset, ret[i + group1_block_count], 0, group2_block_size);
                offset += group2_block_size;
            }
            return ret;
        }
        //数据交错化
        private static byte[] _interleaving_data(byte[][] origin_data, byte[][] ec_data, int version, int correct_level)
        {
            var group_data = EC_TABLE[version][correct_level];
            var block_count = origin_data.Length;
            if (block_count == 0) return new byte[0];
            if (origin_data.Length != ec_data.Length)
                throw new ArgumentException("原始数据与纠错码数据维度不匹配");
            var ret = new List<byte>();
            int max_col = Math.Max(group_data[1], group_data[3]);

            for (int col = 0; col < max_col; col++)
            {
                for (int block = 0; block < block_count; block++)
                {
                    if (col < origin_data[block].Length)
                        ret.Add(origin_data[block][col]);
                }
            }

            max_col = ec_data[0].Length;

            for (int col = 0; col < max_col; col++)
            {
                for (int block = 0; block < block_count; block++)
                {
                    ret.Add(ec_data[block][col]);
                }
            }

            return ret.ToArray();
        }
        #endregion


        #region module placement in matrix
        //增加剩下的bit
        private static bool[] _add_remainder_bits(byte[] data, int version)
        {
            var data_array = util.bytes_to_bits(data);
            if (REMAINDER_BIT[version] == 0)
                return data_array;
            var ret = new bool[data_array.Length + REMAINDER_BIT[version]];
            Array.Copy(data_array, ret, data_array.Length);
            return ret;
        }

        private const byte FLAG_FINDER_PATTERNS = 0x10;
        private const byte FLAG_SEPARATORS = 0x20;
        private const byte FLAG_ALIGNMENT_PATTERNS = 0x30;
        private const byte FLAG_TIMING_PATTERNS = 0x40;
        private const byte FLAG_DARK_MODULE = 0x50;
        private const byte FLAG_RESERVED_FORMAT_INFO = 0x60;
        private const byte FLAG_RESERVED_VERSION_INFO = 0x70;

        private const byte COLOR_WHITE = 0x02;
        private const byte COLOR_BLACK = 0x01;
        private static byte[,] _create_qr_matrix(bool[] data, int version, int level)
        {
            var length = 4 * version + 17;
            var ret = new byte[length, length];

            //placing finder pattern
            _place_finder_pattern(ret, 0, 0);
            _place_finder_pattern(ret, length - 7, 0);
            _place_finder_pattern(ret, 0, length - 7);
            //_debug_output_matrix(ret, length);

            //placing separators
            _place_seperators(ret, length);
            //_debug_output_matrix(ret, length);

            //placing alignment pattern
            var alignment_location = ALIGNMENT_LOCATION_TABLE[version];
            foreach (var x in alignment_location)
            {
                foreach (var y in alignment_location)
                {
                    _place_alignment_pattern(ret, x, y);
                }
            }
            //_debug_output_matrix(ret, length);

            //placing timing patterns
            _place_timing_pattern(ret, length);
            //_debug_output_matrix(ret, length);

            //add dark module
            ret[8, 4 * version + 9] = FLAG_DARK_MODULE | COLOR_BLACK;
            //_debug_output_matrix(ret, length);

            //reserve format info
            _reserve_format_info(ret, length);
            //_debug_output_matrix(ret, length);

            //reserve version info
            if (version >= 7)
            {
                _reserve_version_info(ret, length);
                //_debug_output_matrix(ret, length);
            }

            //placing data bits
            _place_data(ret, data, length, data.Length);
            //_debug_output_matrix(ret, length);

            //masking data
            var masked_ret = new byte[8][,];
            var penalty = new int[8];
            for (int i = 0; i < 8; i++)
            {
                masked_ret[i] = new byte[length, length];
                Array.Copy(ret, masked_ret[i], ret.Length);

                _place_format_info(masked_ret[i], level, i, length);
                //_debug_output_matrix(masked_ret[i], length);

                if (version >= 7)
                {
                    _place_version_info(masked_ret[i], version, length);
                    _debug_output_matrix(masked_ret[i], length);
                }
                masked_ret[i] = _mask_data(masked_ret[i], length, i);
                //_debug_output_matrix(masked_ret[i], length);
                penalty[i] = _calc_penalty(masked_ret[i], length);
            }

            int min_idx = 0;
            int min_penalty = penalty[0];
            for (int i = 1; i < 8; i++)
            {
                if (penalty[i] < min_penalty)
                {
                    min_penalty = penalty[i];
                    min_idx = i;
                }
            }
            //_debug_output_matrix(masked_ret[min_idx], length);
            return masked_ret[min_idx];
        }
        private static void _place_finder_pattern(byte[,] matrix, int x, int y)
        {
            for (int i = 0; i < 6; i++)
            {
                matrix[x + i, y] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                matrix[x + 6, y + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                matrix[x + 1 + i, y + 6] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                matrix[x, y + 1 + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
            }

            for (int i = 0; i < 4; i++)
            {
                matrix[x + i + 1, y + 1] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
                matrix[x + 5, y + i + 1] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
                matrix[x + 2 + i, y + 5] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
                matrix[x + 1, y + 2 + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matrix[x + 2 + i, y + 2 + j] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                }
            }
        }

        private static void _place_seperators(byte[,] matrix, int matrix_length)
        {
            for (int i = 0; i < 8; i++)
            {
                matrix[7, i] = FLAG_SEPARATORS | COLOR_WHITE;
                matrix[matrix_length - 8, i] = FLAG_SEPARATORS | COLOR_WHITE;
                matrix[7, matrix_length - 1 - i] = FLAG_SEPARATORS | COLOR_WHITE;
            }
            for (int i = 0; i < 7; i++)
            {
                matrix[i, 7] = FLAG_SEPARATORS | COLOR_WHITE;
                matrix[matrix_length - 1 - i, 7] = FLAG_SEPARATORS | COLOR_WHITE;
                matrix[i, matrix_length - 8] = FLAG_SEPARATORS | COLOR_WHITE;
            }
        }

        #region alignment location table
        private readonly static int[][] ALIGNMENT_LOCATION_TABLE = new int[][]
        {
            new int[] {}, //version 0, not identified
            new int[] {6}, //version 1
            new int[] {6,18}, //version 2
            new int[] {6,22}, //version 3
            new int[] {6,26}, //version 4
            new int[] {6,30}, //version 5
            new int[] {6,34}, //version 6
            new int[] {6,22,38}, //version 7
            new int[] {6,24,42}, //version 8
            new int[] {6,26,46}, //version 9
            new int[] {6,28,50}, //version 10
            new int[] {6,30,54}, //version 11
            new int[] {6,32,58}, //version 12
            new int[] {6,34,62}, //version 13
            new int[] {6,26,46,66}, //version 14
            new int[] {6,26,48,70}, //version 15
            new int[] {6,26,50,74}, //version 16
            new int[] {6,30,54,78}, //version 17
            new int[] {6,30,56,82}, //version 18
            new int[] {6,30,58,86}, //version 19
            new int[] {6,34,62,90}, //version 20
            new int[] {6,28,50,72,94}, //version 21
            new int[] {6,26,50,74,98}, //version 22
            new int[] {6,30,54,78,102}, //version 23
            new int[] {6,28,54,80,106}, //version 24
            new int[] {6,32,58,84,110}, //version 25
            new int[] {6,30,58,86,114}, //version 26
            new int[] {6,34,62,90,118}, //version 27
            new int[] {6,26,50,74,98,122}, //version 28
            new int[] {6,30,54,78,102,126}, //version 29
            new int[] {6,26,52,78,104,130}, //version 30
            new int[] {6,30,56,82,108,134}, //version 31
            new int[] {6,34,60,86,112,138}, //version 32
            new int[] {6,30,58,86,114,142}, //version 33
            new int[] {6,34,62,90,118,146}, //version 34
            new int[] {6,30,54,78,102,126,150}, //version 35
            new int[] {6,24,50,76,102,128,154}, //version 36
            new int[] {6,28,54,80,106,132,158}, //version 37
            new int[] {6,32,58,84,110,136,162}, //version 38
            new int[] {6,26,54,82,110,138,166}, //version 39
            new int[] {6,30,58,86,114,142,170} //version 40
        };
        #endregion

        private static void _place_alignment_pattern(byte[,] matrix, int x, int y)
        {
            if (matrix[x, y] != 0)
                return;
            for (int i = 0; i < 4; i++)
            {
                matrix[x - 2 + i, y - 2] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                matrix[x + 2, y - 2 + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                matrix[x - 1 + i, y + 2] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
                matrix[x - 2, y - 1 + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
            }
            for (int i = 0; i < 2; i++)
            {
                matrix[x - 1 + i, y - 1] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
                matrix[x + 1, y - 1 + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
                matrix[x + i, y + 1] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
                matrix[x - 1, y + i] = FLAG_ALIGNMENT_PATTERNS | COLOR_WHITE;
            }
            matrix[x, y] = FLAG_ALIGNMENT_PATTERNS | COLOR_BLACK;
        }

        private static void _place_timing_pattern(byte[,] matrix, int matrix_length)
        {
            for (int i = 8; i < matrix_length - 8; i++)
            {
                if (matrix[6, i] == 0)
                    matrix[6, i] = (byte)(FLAG_TIMING_PATTERNS | (((i & 1) == 1) ? COLOR_WHITE : COLOR_BLACK));
                if (matrix[i, 6] == 0)
                    matrix[i, 6] = (byte)(FLAG_TIMING_PATTERNS | (((i & 1) == 1) ? COLOR_WHITE : COLOR_BLACK));
            }
        }

        private static void _reserve_format_info(byte[,] matrix, int matrix_length)
        {
            for (int i = 0; i < 9; i++)
            {
                if (i == 6) continue;
                matrix[8, i] = FLAG_RESERVED_FORMAT_INFO;
                matrix[i, 8] = FLAG_RESERVED_FORMAT_INFO;
            }
            for (int i = 0; i < 8; i++)
                matrix[matrix_length - 1 - i, 8] = FLAG_RESERVED_FORMAT_INFO;
            for (int i = 0; i < 7; i++)
                matrix[8, matrix_length - 1 - i] = FLAG_RESERVED_FORMAT_INFO;
        }

        private static void _reserve_version_info(byte[,] matrix, int matrix_length)
        {
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matrix[matrix_length - 9 - j, i] = FLAG_RESERVED_VERSION_INFO;
                    matrix[i, matrix_length - 9 - j] = FLAG_RESERVED_VERSION_INFO;
                }
            }
        }

        private static void _place_data(byte[,] matrix, bool[] data, int matrix_length, int data_length)
        {
            int x = matrix_length - 1, y = matrix_length - 1, offset = 0;
            while (offset < data_length)
            {
                _place_upward(matrix, data, ref x, ref y, ref offset);
                x--;
                if (x == 6) x--; //跳过vertical timing pattern
                _place_downward(matrix, data, ref x, ref y, ref offset, matrix_length);
                x--;
            }
        }

        private static void _place_upward(byte[,] matrix, bool[] data, ref int x, ref int y, ref int index)
        {
            while (y >= 0)
            {
                if (matrix[x, y] == 0)
                    matrix[x, y] = data[index++] ? COLOR_BLACK : COLOR_WHITE;
                x--;
                if (matrix[x, y] == 0)
                    matrix[x, y] = data[index++] ? COLOR_BLACK : COLOR_WHITE;
                x++;
                y--;
            }
            x--;
            y++;
        }
        private static void _place_downward(byte[,] matrix, bool[] data, ref int x, ref int y, ref int index, int matrix_length)
        {
            while (y < matrix_length)
            {
                if (matrix[x, y] == 0)
                    matrix[x, y] = data[index++] ? COLOR_BLACK : COLOR_WHITE;
                x--;
                if (matrix[x, y] == 0)
                    matrix[x, y] = data[index++] ? COLOR_BLACK : COLOR_WHITE;
                x++;
                y++;
            }
            x--;
            y--;
        }
        private static byte[,] _mask_data(byte[,] matrix, int matrix_length, int mask_type)
        {
            var ret = new byte[matrix_length, matrix_length];
            //x refs to col, y refs to row
            switch (mask_type)
            {
                case 0:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if ((x + y) % 2 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 1:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if (y % 2 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 2:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if (x % 3 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 3:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if ((x + y) % 3 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 4:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if ((int)(Math.Floor(y / 2.0) + Math.Floor(x / 3.0)) % 2 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 5:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if (((x * y) % 2) + ((x * y) % 3) == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 6:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if ((((x * y) % 2) + ((x * y) % 3)) % 2 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                case 7:
                    for (int x = 0; x < matrix_length; x++)
                        for (int y = 0; y < matrix_length; y++)
                            if ((((x + y) % 2) + ((x * y) % 3)) % 2 == 0 && (matrix[x, y] & 0xf0) == 0)
                                ret[x, y] = (byte)(matrix[x, y] ^ (COLOR_BLACK | COLOR_WHITE));
                            else
                                ret[x, y] = matrix[x, y];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("掩码类型错误");
            }
            return ret;
        }

        private static void _place_format_info(byte[,] matrix, int correct_level, int mask_pattern, int matrix_length)
        {
            var format_bits = new List<bool>();
            var ec_bits = new byte[] { 1, 0, 3, 2 };
            //源数据多项式
            format_bits.AddRange(util.int_to_bytes(ec_bits[correct_level], 2));
            format_bits.AddRange(util.int_to_bytes(mask_pattern, 3));
            var format_bits_combined = new List<bool>(format_bits);

            //15位对齐
            format_bits.AddRange(new bool[10]);

            //移除0开头
            while (format_bits.Count > 0 && format_bits[0] == false) format_bits.RemoveAt(0);
            while (format_bits.Count > 10)
            {
                //生成的多项式
                var generated_bits = new List<bool>(util.int_to_bytes(0x537, 11));
                //生成多项式进行位对齐
                generated_bits.AddRange(new bool[format_bits.Count - generated_bits.Count]);
                //位异或
                for (int i = 0; i < generated_bits.Count; i++)
                {
                    format_bits[i] = generated_bits[i] ^ format_bits[i];
                }
                //移除0开头
                while (format_bits[0] == false) format_bits.RemoveAt(0);
            }

            if (format_bits.Count < 10)
                format_bits.InsertRange(0, new bool[10 - format_bits.Count]);
            format_bits_combined.AddRange(format_bits);
            format_bits = format_bits_combined; format_bits_combined = null; //覆盖变量

            var xor_data = util.int_to_bytes(0x5412, 15);
            for (int i = 0; i < 15; i++)
            {
                format_bits[i] ^= xor_data[i];
            }

            //placing format bits
            for (int i = 0; i < 6; i++)
            {
                matrix[i, 8] |= format_bits[i] ? COLOR_BLACK : COLOR_WHITE;
                matrix[8, matrix_length - 1 - i] |= format_bits[i] ? COLOR_BLACK : COLOR_WHITE;
            }
            matrix[7, 8] |= format_bits[6] ? COLOR_BLACK : COLOR_WHITE;
            matrix[8, 8] |= format_bits[7] ? COLOR_BLACK : COLOR_WHITE;
            matrix[8, 7] |= format_bits[8] ? COLOR_BLACK : COLOR_WHITE;
            matrix[8, matrix_length - 7] |= format_bits[6] ? COLOR_BLACK : COLOR_WHITE;
            matrix[matrix_length - 8, 8] |= format_bits[7] ? COLOR_BLACK : COLOR_WHITE;
            matrix[matrix_length - 7, 8] |= format_bits[8] ? COLOR_BLACK : COLOR_WHITE;
            for (int i = 0; i < 6; i++)
            {
                matrix[8, 5 - i] |= format_bits[i + 9] ? COLOR_BLACK : COLOR_WHITE;
                matrix[matrix_length - 6 + i, 8] |= format_bits[i + 9] ? COLOR_BLACK : COLOR_WHITE;
            }
        }
        private static void _place_version_info(byte[,] matrix, int version, int matrix_length)
        {
            var version_bits = new List<bool>(util.int_to_bytes(version, 6));
            version_bits.AddRange(new bool[12]);
            while (version_bits.Count > 0 && version_bits[0] == false) version_bits.RemoveAt(0);
            while (version_bits.Count > 12)
            {
                var generated_bits = new List<bool>(util.int_to_bytes(0x1f25, 13));
                generated_bits.AddRange(new bool[version_bits.Count - generated_bits.Count]);
                for (int i = 0; i < generated_bits.Count; i++)
                {
                    version_bits[i] = generated_bits[i] ^ version_bits[i];
                }
                while (version_bits[0] == false) version_bits.RemoveAt(0);
            }
            if (version_bits.Count < 12)
                version_bits.InsertRange(0, new bool[12 - version_bits.Count]);
            version_bits.InsertRange(0, util.int_to_bytes(version, 6));

            //placing bits
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matrix[matrix_length - 11 + j, i] |= version_bits[17 - i * 3 - j] ? COLOR_BLACK : COLOR_WHITE;
                    matrix[i, matrix_length - 11 + j] |= version_bits[17 - i * 3 - j] ? COLOR_BLACK : COLOR_WHITE;
                }
            }
        }
        private static int _calc_penalty(byte[,] matrix, int matrix_length)
        {
            int penalty1 = 0;
            for (int y = 0; y < matrix_length; y++)
            {
                for (int x = 0; x < matrix_length;)
                {
                    int begin_pos = x;
                    int begin_color = matrix[begin_pos, y] & 0x0f;
                    int end_pos = x;
                    while (++end_pos < matrix_length)
                    {
                        if ((matrix[end_pos, y] & 0x0f) != begin_color)
                            break;
                    }

                    int length = end_pos - begin_pos;
                    if (length >= 5)
                        penalty1 += length - 2;
                    x = end_pos;
                }
            }
            for (int x = 0; x < matrix_length; x++)
            {
                for (int y = 0; y < matrix_length;)
                {
                    int begin_pos = y;
                    int begin_color = matrix[x, begin_pos] & 0x0f;
                    int end_pos = y;
                    while (++end_pos < matrix_length)
                    {
                        if ((matrix[x, end_pos] & 0x0f) != begin_color)
                            break;
                    }

                    int length = end_pos - begin_pos;
                    if (length >= 5)
                        penalty1 += length - 2;
                    y = end_pos;
                }
            }

            int penalty2 = 0;
            for (int x = 0; x < matrix_length - 1; x++)
            {
                for (int y = 0; y < matrix_length - 1; y++)
                {
                    if (((matrix[x, y] ^ matrix[x + 1, y]) & 0x0f) != 0) continue;
                    if (((matrix[x, y] ^ matrix[x, y + 1]) & 0x0f) != 0) continue;
                    if (((matrix[x, y] ^ matrix[x + 1, y + 1]) & 0x0f) != 0) continue;
                    penalty2++;
                }
            }
            penalty2 *= 3;

            int penalty3 = 0;
            byte[] ptn1 = new byte[] { COLOR_BLACK, COLOR_WHITE, COLOR_BLACK, COLOR_BLACK, COLOR_BLACK, COLOR_WHITE, COLOR_BLACK, COLOR_WHITE, COLOR_WHITE, COLOR_WHITE, COLOR_WHITE };
            byte[] ptn2 = new byte[] { COLOR_WHITE, COLOR_WHITE, COLOR_WHITE, COLOR_WHITE, COLOR_BLACK, COLOR_WHITE, COLOR_BLACK, COLOR_BLACK, COLOR_BLACK, COLOR_WHITE, COLOR_BLACK };
            for (int y = 0; y < matrix_length; y++)
            {
                for (int x = 0; x <= matrix_length - ptn1.Length; x++)
                {
                    int idx1 = 0, idx2 = 0;
                    while (idx1 < ptn1.Length && (matrix[x + idx1, y] & 0x0f) == ptn1[idx1])
                        idx1++;
                    while (idx2 < ptn2.Length && (matrix[x + idx2, y] & 0x0f) == ptn2[idx2])
                        idx2++;
                    if (idx1 == ptn1.Length)
                        penalty3++;
                    else if (idx2 == ptn2.Length)
                        penalty3++;
                }
            }
            for (int x = 0; x < matrix_length; x++)
            {
                for (int y = 0; y <= matrix_length - ptn2.Length; y++)
                {
                    int idx1 = 0, idx2 = 0;
                    while (idx1 < ptn1.Length && (matrix[x, y + idx1] & 0x0f) == ptn1[idx1])
                        idx1++;
                    while (idx2 < ptn2.Length && (matrix[x, y + idx2] & 0x0f) == ptn2[idx2])
                        idx2++;
                    if (idx1 == ptn1.Length)
                        penalty3++;
                    else if (idx2 == ptn2.Length)
                        penalty3++;
                }
            }
            penalty3 *= 40;

            int dark_cell = 0;
            for (int x = 0; x < matrix_length; x++)
            {
                for (int y = 0; y < matrix_length; y++)
                {
                    if ((matrix[x, y] & COLOR_BLACK) == COLOR_BLACK)
                        dark_cell++;
                }
            }
            int percent = (int)(dark_cell * 100.0 / (matrix_length * matrix_length));
            int level = (percent / 5) * 5;
            int d1 = Math.Abs(level - 50) / 5;
            int d2 = Math.Abs(level - 45) / 5;
            int min_d = Math.Min(d1, d2);
            int penalty4 = min_d * 10;

            return penalty1 + penalty2 + penalty3 + penalty4;
        }
        #endregion

        private static void _debug_output_matrix(byte[,] matrix, int length)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    if ((matrix[x, y] & COLOR_BLACK) != 0)
                        sb.Append("■");
                    else if ((matrix[x, y] & COLOR_WHITE) != 0)
                        sb.Append("□");
                    else if ((matrix[x, y] & 0xf0) == FLAG_RESERVED_FORMAT_INFO)
                        sb.Append("FF");
                    else if ((matrix[x, y] & 0xf0) == FLAG_RESERVED_VERSION_INFO)
                        sb.Append("VV");
                    else
                        sb.Append("  ");
                }
                sb.Append("\r\n");
            }
            sb.Append("\r\n");
            Debug.Print(sb.ToString());

        }

        public static byte[,] GenerateQRCode(string str, int version, int ec_level)
        {

            var data = _encode_byte(str, version, ec_level);
            var dispatched_data = _block_dispatch(data, version, ec_level);
            var ec_data = new byte[dispatched_data.Length][];
            for (int i = 0; i < ec_data.Length; i++)
                ec_data[i] = _encode_ec_data(dispatched_data[i], version, ec_level);
            var result = _interleaving_data(dispatched_data, ec_data, version, ec_level);

            var bits = _add_remainder_bits(result, version);
            var m = _create_qr_matrix(bits, version, ec_level);
            return m;
        }

        public static void SaveQRCode(byte[,] matrix, string name, int padding = 100, int scale = 2, int quality = 60)
        {
            var length = matrix.GetLength(0);

            var img = new Bitmap(length * scale + 2 * padding, length * scale + 2 * padding);
            var gr = Graphics.FromImage(img);
            gr.FillRectangle(Brushes.White, 0, 0, length * scale + 2 * padding, length * scale + 2 * padding);
            gr.Dispose();

            for (int y = 0; y < length; y++)
                for (int x = 0; x < length; x++)
                    if ((matrix[x, y] & COLOR_BLACK) != 0)
                        for (int a = 0; a < scale; a++)
                            for (int b = 0; b < scale; b++)
                                img.SetPixel(x * scale + a + padding, y * scale + b + padding, Color.Black);

            var ep = new System.Drawing.Imaging.EncoderParameters(1);
            ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            var encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            var encoder = encoders[0];
            for (int i = 0; i < encoders.Length; i++)
                if (encoders[i].MimeType == "image/png")
                    encoder = encoders[i];
            img.Save(name, encoder, ep);
        }


        public static void SaveQRCode2(byte[,] matrix, string name, int padding = 100, int scale = 2, int quality = 60)
        {
            var length = matrix.GetLength(0);

            var img = new Bitmap(length * scale + 2 * padding, length * scale + 2 * padding);
            img.MakeTransparent(Color.Black);

            for (int y = 0; y < length; y++)
                for (int x = 0; x < length; x++)
                    if ((matrix[x, y] & 0xf0) < 0x40 && (matrix[x, y] & 0xf0) != 0)
                    {
                        for (int a = 0; a < scale; a++)
                            for (int b = 0; b < scale; b++)
                                img.SetPixel(x * scale + a + padding, y * scale + b + padding, ((matrix[x, y] & COLOR_BLACK) != 0) ? Color.Black : Color.White);
                    }
                    else
                    {
                        for (int a = scale / 3; a < 2 * scale / 3; a++)
                            for (int b = scale / 3; b < 2 * scale / 3; b++)
                                img.SetPixel(x * scale + a + padding, y * scale + b + padding, ((matrix[x, y] & COLOR_BLACK) != 0) ? Color.Black : Color.White);
                    }

            var ep = new System.Drawing.Imaging.EncoderParameters(1);
            ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            var encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            var encoder = encoders[0];
            for (int i = 0; i < encoders.Length; i++)
                if (encoders[i].MimeType == "image/png")
                    encoder = encoders[i];
            img.Save(name, encoder, ep);
            img.Dispose();
        }
        public static Image CustomizeQRCode(byte[,] matrix, int padding = 100, int scale = 9, Color? foreground = null, bool fill_padding = true)
        {
            var length = matrix.GetLength(0);

            var img = new Bitmap(length * scale + 2 * padding, length * scale + 2 * padding);
            if (fill_padding)
            {
                var gr = Graphics.FromImage(img);
                gr.FillRectangle(Brushes.White, 0, 0, img.Width, img.Height);
                gr.FillRectangle(Brushes.Black, padding, padding, img.Width - 2 * padding, img.Height - 2 * padding);
                gr.Dispose();
            }
            img.MakeTransparent(Color.Black);

            if (foreground == null)
                foreground = Color.Black;
            for (int y = 0; y < length; y++)
                for (int x = 0; x < length; x++)
                    if ((matrix[x, y] & 0xf0) < 0x40 && (matrix[x, y] & 0xf0) != 0)
                    {
                        for (int a = 0; a < scale; a++)
                            for (int b = 0; b < scale; b++)
                                img.SetPixel(x * scale + a + padding, y * scale + b + padding, ((matrix[x, y] & COLOR_BLACK) != 0) ? (Color)foreground : Color.White);
                    }
                    else
                    {
                        for (int a = scale / 3; a < 2 * scale / 3; a++)
                            for (int b = scale / 3; b < 2 * scale / 3; b++)
                                img.SetPixel(x * scale + a + padding, y * scale + b + padding, ((matrix[x, y] & COLOR_BLACK) != 0) ? (Color)foreground : Color.White);
                    }

            return img;
        }
    }
}
