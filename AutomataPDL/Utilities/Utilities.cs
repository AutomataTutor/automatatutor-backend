using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Automata;

namespace AutomataPDL
{
    public static class IntegerUtil
    {
        public static long PairToInt(long x, long y)
        {
            return ((x + y) * (x + y + 1)) / 2 + y;
        }

        public static long TripleToInt(long x, long y, long z)
        {
            return PairToInt(x,PairToInt(y,z));
        }

        public static Pair<int,int> IntToPair(int p)
        {
            int w = (int)Math.Floor((Math.Sqrt(8*p+1)-1)/2);
            int t = (w * w + w) / 2;
            int y = p - t;
            return new Pair<int,int>(w-y,y);
        }
    }


    /// <summary>
    /// Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
    /// Provides a method for performing a deep copy of an object.
    /// Binary Serialization is used to perform the copy.
    /// </summary>
    public static class ObjectCopier
    {
        /// <summary>
        /// Perform a deep Copy of the object.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
