using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P
{
    class AES
    {
        static byte Nk, Nr;
        static byte[] Key;
        static byte[] RoundKey;
        static byte[,] state = new byte[4, 4];
        static byte[,] MultTable = new byte[256, 256];
        static byte[] sbox =   {
                                  0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76,
                                  0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0,
                                  0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15,
                                  0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75,
                                  0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84,
                                  0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf,
                                  0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8,
                                  0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2,
                                  0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73,
                                  0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb,
                                  0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79,
                                  0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08,
                                  0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a,
                                  0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e,
                                  0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf,
                                  0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16 };

        static byte[] rsbox =  {
                                  0x52, 0x09, 0x6a, 0xd5, 0x30, 0x36, 0xa5, 0x38, 0xbf, 0x40, 0xa3, 0x9e, 0x81, 0xf3, 0xd7, 0xfb,
                                  0x7c, 0xe3, 0x39, 0x82, 0x9b, 0x2f, 0xff, 0x87, 0x34, 0x8e, 0x43, 0x44, 0xc4, 0xde, 0xe9, 0xcb,
                                  0x54, 0x7b, 0x94, 0x32, 0xa6, 0xc2, 0x23, 0x3d, 0xee, 0x4c, 0x95, 0x0b, 0x42, 0xfa, 0xc3, 0x4e,
                                  0x08, 0x2e, 0xa1, 0x66, 0x28, 0xd9, 0x24, 0xb2, 0x76, 0x5b, 0xa2, 0x49, 0x6d, 0x8b, 0xd1, 0x25,
                                  0x72, 0xf8, 0xf6, 0x64, 0x86, 0x68, 0x98, 0x16, 0xd4, 0xa4, 0x5c, 0xcc, 0x5d, 0x65, 0xb6, 0x92,
                                  0x6c, 0x70, 0x48, 0x50, 0xfd, 0xed, 0xb9, 0xda, 0x5e, 0x15, 0x46, 0x57, 0xa7, 0x8d, 0x9d, 0x84,
                                  0x90, 0xd8, 0xab, 0x00, 0x8c, 0xbc, 0xd3, 0x0a, 0xf7, 0xe4, 0x58, 0x05, 0xb8, 0xb3, 0x45, 0x06,
                                  0xd0, 0x2c, 0x1e, 0x8f, 0xca, 0x3f, 0x0f, 0x02, 0xc1, 0xaf, 0xbd, 0x03, 0x01, 0x13, 0x8a, 0x6b,
                                  0x3a, 0x91, 0x11, 0x41, 0x4f, 0x67, 0xdc, 0xea, 0x97, 0xf2, 0xcf, 0xce, 0xf0, 0xb4, 0xe6, 0x73,
                                  0x96, 0xac, 0x74, 0x22, 0xe7, 0xad, 0x35, 0x85, 0xe2, 0xf9, 0x37, 0xe8, 0x1c, 0x75, 0xdf, 0x6e,
                                  0x47, 0xf1, 0x1a, 0x71, 0x1d, 0x29, 0xc5, 0x89, 0x6f, 0xb7, 0x62, 0x0e, 0xaa, 0x18, 0xbe, 0x1b,
                                  0xfc, 0x56, 0x3e, 0x4b, 0xc6, 0xd2, 0x79, 0x20, 0x9a, 0xdb, 0xc0, 0xfe, 0x78, 0xcd, 0x5a, 0xf4,
                                  0x1f, 0xdd, 0xa8, 0x33, 0x88, 0x07, 0xc7, 0x31, 0xb1, 0x12, 0x10, 0x59, 0x27, 0x80, 0xec, 0x5f,
                                  0x60, 0x51, 0x7f, 0xa9, 0x19, 0xb5, 0x4a, 0x0d, 0x2d, 0xe5, 0x7a, 0x9f, 0x93, 0xc9, 0x9c, 0xef,
                                  0xa0, 0xe0, 0x3b, 0x4d, 0xae, 0x2a, 0xf5, 0xb0, 0xc8, 0xeb, 0xbb, 0x3c, 0x83, 0x53, 0x99, 0x61,
                                  0x17, 0x2b, 0x04, 0x7e, 0xba, 0x77, 0xd6, 0x26, 0xe1, 0x69, 0x14, 0x63, 0x55, 0x21, 0x0c, 0x7d };

        static byte[] Rcon = {
                                  0x8d, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36, 0x6c, 0xd8, 0xab, 0x4d, 0x9a,
                                  0x2f, 0x5e, 0xbc, 0x63, 0xc6, 0x97, 0x35, 0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef, 0xc5, 0x91, 0x39,
                                  0x72, 0xe4, 0xd3, 0xbd, 0x61, 0xc2, 0x9f, 0x25, 0x4a, 0x94, 0x33, 0x66, 0xcc, 0x83, 0x1d, 0x3a,
                                  0x74, 0xe8, 0xcb, 0x8d, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36, 0x6c, 0xd8,
                                  0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63, 0xc6, 0x97, 0x35, 0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef,
                                  0xc5, 0x91, 0x39, 0x72, 0xe4, 0xd3, 0xbd, 0x61, 0xc2, 0x9f, 0x25, 0x4a, 0x94, 0x33, 0x66, 0xcc,
                                  0x83, 0x1d, 0x3a, 0x74, 0xe8, 0xcb, 0x8d, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b,
                                  0x36, 0x6c, 0xd8, 0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63, 0xc6, 0x97, 0x35, 0x6a, 0xd4, 0xb3,
                                  0x7d, 0xfa, 0xef, 0xc5, 0x91, 0x39, 0x72, 0xe4, 0xd3, 0xbd, 0x61, 0xc2, 0x9f, 0x25, 0x4a, 0x94,
                                  0x33, 0x66, 0xcc, 0x83, 0x1d, 0x3a, 0x74, 0xe8, 0xcb, 0x8d, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20,
                                  0x40, 0x80, 0x1b, 0x36, 0x6c, 0xd8, 0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63, 0xc6, 0x97, 0x35,
                                  0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef, 0xc5, 0x91, 0x39, 0x72, 0xe4, 0xd3, 0xbd, 0x61, 0xc2, 0x9f,
                                  0x25, 0x4a, 0x94, 0x33, 0x66, 0xcc, 0x83, 0x1d, 0x3a, 0x74, 0xe8, 0xcb, 0x8d, 0x01, 0x02, 0x04,
                                  0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36, 0x6c, 0xd8, 0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63,
                                  0xc6, 0x97, 0x35, 0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef, 0xc5, 0x91, 0x39, 0x72, 0xe4, 0xd3, 0xbd,
                                  0x61, 0xc2, 0x9f, 0x25, 0x4a, 0x94, 0x33, 0x66, 0xcc, 0x83, 0x1d, 0x3a, 0x74, 0xe8, 0xcb, 0x8d };
        //получает значение из таблицы SBox
        static byte getSBoxValue(byte num)
        {
            return sbox[num];
        }
        //получает значение из таблицы InvSBox
        static byte getSBoxInvert(byte num)
        {
            return rsbox[num];
        }
        //выполняет генерацию раундовых ключей
        static void KeyExpansion()
        {
            for (int i = 0; i < 15; i++)
            {
                RoundKey[i] = Key[i];

            }

            int j, m;

            for (int i = Nk; i < 4 * (Nr + 1); i++)
            {
                j = i * 4;
                if (i % 4 == 0)
                {
                    RotByte(j);
                    m = i;
                    for (int k = j; k < j + 4; k++)
                    {
                        RoundKey[k] = getSBoxValue(RoundKey[k]);
                        RoundKey[k] = (byte)(RoundKey[k] ^ Rcon[i / 4]);
                        m++;
                    }

                }
                else
                {
                    for (int k = 0; k < 4; k++)
                    {
                        RoundKey[j + k] = (byte)(RoundKey[j + k - 4] ^ RoundKey[j + k - 16]);

                    }


                }


            }
        }
        //циклический сдвиг слова,состоящего из 4-х байт
        static void RotByte(int i)
        {
            RoundKey[i] = RoundKey[i - 1];
            RoundKey[i + 1] = RoundKey[i - 3];
            RoundKey[i + 2] = RoundKey[i - 2];
            RoundKey[i + 3] = RoundKey[i - 4]; ;

        }
        //добавляет раундовый ключ к массиву state
        static void AddRoundKey(int round)
        {
            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    state[i, j] ^= RoundKey[round * 16 + i * 4 + j];
        }
        //выполяет замену байтов в массиве state по таблице SBox
        static void SubBytes()
        {
            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    state[j, i] = getSBoxValue(state[j, i]);
        }

        //циклический сдвиг байтов в массиве state 
        static void ShiftRows()
        {
            byte[,] b = new byte[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    b[i, j] = state[i, j];
            for (int i = 1; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    state[j, i] = b[(j + i) % 4, i];


        }

        static byte xtime(byte x)
        {
            return (byte)((x << 1) ^ (((x >> 7) & 1) * 0x1b));
        }

        // заменяет каждый столбец state на результат умножения этого столбца на на многочлен 3x^3+x^2+x+2
        //заменяет столбец в массиве блока текста на другой столбец,исходный столбец рассматривается как многочлен и умножается на некоторый фиксированный многочлен
        void MixColumns()
        {
            byte[,] b = new byte[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    b[i, j] = state[i, j];

            for (int i = 0; i < 4; i++)
            {
                state[i, 0] = (byte)(mult(0x02, b[i, 0]) ^ mult(0x03, b[i, 1]) ^ mult(0x01, b[i, 2]) ^ mult(0x01, b[i, 3]));
                state[i, 1] = (byte)(mult(0x01, b[i, 0]) ^ mult(0x02, b[i, 1]) ^ mult(0x03, b[i, 2]) ^ mult(0x01, b[i, 3]));
                state[i, 2] = (byte)(mult(0x01, b[i, 0]) ^ mult(0x01, b[i, 1]) ^ mult(0x02, b[i, 2]) ^ mult(0x03, b[i, 3]));
                state[i, 3] = (byte)(mult(0x03, b[i, 0]) ^ mult(0x01, b[i, 1]) ^ mult(0x01, b[i, 2]) ^ mult(0x02, b[i, 3]));
            }
        }

        //получает результат умножения двух многочленов из таблицы умножения MultTable
        byte mult(byte a, byte b)
        {
            return MultTable[a, b];
        }
        //вычисляет таблицу умножения в поле Галуа GF256
        public void SetMultTable()
        {

            for (int i = 0; i < 256; i++)
                for (int j = 0; j < 256; j++)
                    MultTable[j, i] = multiply((byte)j, (byte)i);


        }
        //осуществляет умножение элементов в поле Галуа GF256
        byte multiply(byte a, byte b)
        {
            byte[] c = new byte[8];
            byte c1;
            byte c2;
            byte d;
            byte m = 0x1b;
            int j = 0;

            if (a > b)
            {
                c1 = a;
                c2 = b;
            }
            else
            {
                c1 = b;
                c2 = a;
            }
            if (c2 == 0x00) return 0x00;
            if (c2 == 0x01) return c1;

            for (int i = 0; i < 8; i++)
            {

                if ((((c2 >> (7 - i)) & 0x01) == 0x01) && (j == 0))
                {
                    j = 7 - i;
                    break;
                }


            }
            c[0] = c1;
            for (int i = 1; i <= j; i++)
            {
                if (((c[i - 1] >> 7) & 0x01) == 0x01)
                {
                    c[i] = (byte)((c[i - 1] << 1) ^ m);
                }
                else
                {
                    c[i] = (byte)((c[i - 1] << 1));
                }

            }
            d = c[j];
            for (int i = 0; i < j; i++)
            {

                if ((((c2 >> i) & 0x01) == 0x01))
                {
                    d ^= c[i];

                }

            }
            return d;
        }
        // заменяет каждый столбец state на результат умножения этого столбца на на многочлен 0b*x^3+0d*x^2+09*x+0e
        void InvMixColumns()
        {
            byte[,] b = new byte[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    b[i, j] = state[i, j];

            for (int i = 0; i < 4; i++)
            {
                state[i, 0] = (byte)(mult(0x0e, b[i, 0]) ^ mult(0x0b, b[i, 1]) ^ mult(0x0d, b[i, 2]) ^ mult(0x09, b[i, 3]));
                state[i, 1] = (byte)(mult(0x09, b[i, 0]) ^ mult(0x0e, b[i, 1]) ^ mult(0x0b, b[i, 2]) ^ mult(0x0d, b[i, 3]));
                state[i, 2] = (byte)(mult(0x0d, b[i, 0]) ^ mult(0x09, b[i, 1]) ^ mult(0x0e, b[i, 2]) ^ mult(0x0b, b[i, 3]));
                state[i, 3] = (byte)(mult(0x0b, b[i, 0]) ^ mult(0x0d, b[i, 1]) ^ mult(0x09, b[i, 2]) ^ mult(0x0e, b[i, 3]));
            }

        }

        //выполяет замену байтов в массиве state по таблице InvSBox
        static void InvSubBytes()
        {
            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    state[j, i] = getSBoxInvert(state[j, i]);

        }
        //обратный циклический сдвиг байтов в массиве state 
        void InvShiftRows()
        {
            byte[,] b = new byte[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    b[i, j] = state[i, j];
            for (int i = 1; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    state[j, i] = b[(4 - i + j) % 4, i];
        }

        // шифрование блока текста state
        void Cipher()
        {
            AddRoundKey(0);

            for (int round = 1; round < Nr; ++round)
            {
                SubBytes();
                ShiftRows();
                MixColumns();
                AddRoundKey(round);
            }

            SubBytes();
            ShiftRows();
            AddRoundKey(Nr);
        }

        // расшифровка блока текста state
        void InvCipher()
        {
            AddRoundKey(Nr);

            for (int round = Nr - 1; round > 0; round--)
            {
                InvShiftRows();
                InvSubBytes();
                AddRoundKey(round);
                InvMixColumns();
            }

            InvShiftRows();
            InvSubBytes();
            AddRoundKey(0);
        }

        //шифрование
        public byte[] AES_encrypt(byte[] text, byte[] key)
        {
            Key = key;
            RoundKey = new byte[(Nr + 1) * Nk * 4];
            KeyExpansion();
            for (int i = 0; i < text.Length / 16; i++)
            {
                //переносит 16 байт текста в state
                for (int k = 0; k < 4; k++)
                    for (int j = 0; j < 4; j++)
                        state[k, j] = text[16 * i + k * 4 + j];

                Cipher();// Шифрование блока state
                //переносит результат преобразования state в i-e 16 байт текста 
                for (int k = 0; k < 4; k++)
                    for (int j = 0; j < 4; j++)
                        text[16 * i + k * 4 + j] = state[k, j];



            }
            return text;

        }
        //расшифровка
        public byte[] AES_decrypt(byte[] text, byte[] key)
        {

            Key = key;
            RoundKey = new byte[(Nr + 1) * Nk * 4];
            KeyExpansion();
            for (int i = 0; i < text.Length / 16; i++)
            {
                //переносит 16 байт текста в state
                for (int k = 0; k < 4; k++)
                    for (int j = 0; j < 4; j++)
                        state[k, j] = text[16 * i + k * 4 + j];
                InvCipher();// расшифровка блока state 
                //переносит результат преобразования state в i-e 16 байт текста 
                for (int k = 0; k < 4; k++)
                    for (int j = 0; j < 4; j++)
                        text[16 * i + k * 4 + j] = state[k, j];
            }
            return text;

        }
        //устанавливает параметры  Nr и  Nk 
        public void SetParameters(byte nr, byte nk)
        {
            Nr = nr;
            Nk = nk;

        }
        //складывает два блока из 16 байт по модулю два
        byte[,] BlockXor(byte[,] block1, byte[,] block2)
        {
            byte[,] result = new byte[4, 4];
            for (int k = 0; k < 4; k++)
                for (int j = 0; j < 4; j++)
                    result[k, j] = (byte)(block1[k, j] ^ block2[k, j]);
            return result;
        }

    }
}
