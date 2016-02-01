using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace AutomataPDL
{
    public class BitVecUtil
    {
        /// <summary>Returns the bit representation of a character c
        /// <param name="c"> the charater </param>        
        /// </summary>
        public static string GetIntBinaryString(char c)
        {
            char[] b = new char[32];
            int pos = 31;
            int i = 0;

            while (i < 32)
            {
                if ((c & (1 << i)) != 0)
                    b[pos] = '1';
                else
                    b[pos] = '0';
                pos--;
                i++;
            }
            return new string(b);
        }

        /// <summary>Returns the bits of n between position a and b with n=010010b01001a01010 (bits are numbered starting from 0)
        /// <param name="n"> number </param>
        /// <param name="a"> starting position </param>
        /// <param name="b"> ending position </param>
        /// </summary>
        internal static int GetBits(int n, int a, int b)
        {
            if (b - a == 0)
                return 0;
            return (n >> a) & ((1 << b) - 1);
        }

        /// <summary>Counts the bits of n between position from a to b (incl a excl b) b should be strictly less than 32
        /// <param name="n"> number </param>
        /// <param name="a"> starting position </param>
        /// <param name="b"> ending position </param>
        /// </summary>
        internal static int CountBits(int n, int a, int b)
        {
            int v =  ((n & ((1 << b) - 1)) >> a);
            int c;
            c = (v & 0x55555555) + ((v >> 1) & 0x55555555);
            c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
            c = (c & 0x0F0F0F0F) + ((c >> 4) & 0x0F0F0F0F);
            c = (c & 0x00FF00FF) + ((c >> 8) & 0x00FF00FF);
            c = (c & 0x0000FFFF) + ((c >> 16) & 0x0000FFFF);
            return c;
        }

        /// <summary>Gets the position of the most significant bit in n if n > 0, if n = 0 returns -1
        /// <param name="n"> number </param>
        /// </summary>
        internal static int GetMostSignificantBitPosition(int n)
        {
            Debug.Assert(n >= 0, "left most bit should be 0, or equivalently n > 0");
            if (n == 0) return -1;
            int p = 0;
            if ((n & 0xFFFF0000) != 0)
            {
                p += 16;
                n >>= 16;
            }
            if ((n & 0x0000FF00) != 0)
            {
                p += 8;
                n >>= 8;
            }
            if ((n & 0x000000F0) != 0)
            {
                p += 4;
                n >>= 4;
            }
            if ((n & 0x0000000C) != 0)
            {
                p += 2;
                n >>= 2;
            }
            if ((n & 0x00000002) != 0)
            {
                p += 1;
                n >>= 1;
            }
            return p;
        }

        /// <summary>Returns the most significants bits of n at positions greater than i (bits are numbered starting from 0)
        /// <param name="n"> number </param>
        /// <param name="i"> starting position </param>
        /// </summary>
        internal static int GetLastBits(int n, int i)
        {
            if (i == 0)
                return n;
            return (n & (~((1 << i) - 1))) >> i;
        }

        /// <summary>Returns the i-th less significant bit of n (bits are numbered starting from 0)
        /// <param name="n"> number </param>
        /// <param name="i"> bit position </param>
        /// </summary>
        internal static int GetIthBit(int n, int i)
        {
            return ((n & (1 << i)) >> i);
        }
    }
}
