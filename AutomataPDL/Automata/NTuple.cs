using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public class NTuple<T>
    {
        public T[] content { get; set; }

        public NTuple(List<T> content_in)
        {
            content = content_in.ToArray();
        }

        public NTuple(T[] content_in)
        {
            content = content_in;
        }

        public override bool Equals(object obj)
        {
            var other = (NTuple<T>) obj;

            int n1 = this.content.Length;
            int n2 = other.content.Length;
            bool is_equal = (n1 == n2);
            if (is_equal)
            {
                for (int i = 0; i < n1; i++)
                {
                    if (!this.content[i].Equals(other.content[i]))
                        is_equal = false;
                }
            }

            return is_equal;
        }

        public override int GetHashCode()
        {
            int hash_code = 0;
            for (int i = 0; i < content.Length; i++)
            {
                hash_code += content[i].GetHashCode();
            }

            return hash_code;
        }
    }
}
