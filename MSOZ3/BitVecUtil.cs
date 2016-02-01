using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MSOZ3
{
    internal class BitVecUtil
    {

        /// <summary>Returns the bit representation of a character c
        /// <param name="c"> the charater </param>        
        /// </summary>
        internal static string GetIntBinaryString(char c)
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

        /// <summary>Returns the most significants bits of n at positions greater than i (bits are numbered starting from 0)
        /// <param name="n"> number </param>
        /// <param name="i"> starting position </param>
        /// </summary>
        internal static int LastBits(int n, int i)
        {
            if (i == 0)
                return n;
            return (n & (~((1 << i) - 1))) >> i;
        }

        /// <summary>Returns the i-th less significant bit of n (bits are numbered starting from 0)
        /// <param name="n"> number </param>
        /// <param name="i"> bit position </param>
        /// </summary>
        internal static int GetBit(int n, int i)
        {
            return ((n & (1 << i)) >> i);
        }
    }



    //Accessory class for extracting characters from the alphabet augmented with the bitvector
    internal static class TupleChar
    {
        internal const int offset = 91;

        /// <summary>Given a number 'num' with 'n' bits and a character 'label', it appends the bits of 'label' as most significant
        /// bits after the 'num'
        /// <param name="label"> character </param>
        /// <param name="num"> bit vector </param>
        /// <param name="n"> number of bits in 'num' </param>
        /// </summary>
        internal static char getExtendedChar(char label, int num, int n)
        {
            //return (char)(((label - offset) << n) + num);
            return (char)(((label) << n) + num);
        }

        internal static char getExtendedCharSubtracted(char label, int num, int n)
        {
            return (char)(((label - offset) << n) + num);
            //return (char)(((label) << n) + num);
        }

        internal static string escape(char c)
        {
            return Regex.Escape("" + c);
            //if (c == '[' || c == '\\' || c == '^' || c == '$'
            //    || c == '.' || c == '|' || c == '?' || c == '*'
            //       || c == '+' || c == '(' || c == ')')
            //    return "\\"+c;
            //else
            //    return ""+c;
        }
    }
}