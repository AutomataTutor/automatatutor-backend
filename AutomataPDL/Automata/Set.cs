using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public class Set<T>
    {
        public HashSet<T> content { get; set; }

        public Set()
        {
            content = new HashSet<T>();
        }

        public Set(HashSet<T> content_in)
        {
            content = content_in;
        }

        public override bool Equals(object obj)
        {
            var other = (Set<T>) obj;
            return this.content.SetEquals(other.content);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;

            foreach (T t in content)
            {
                hashCode += t.GetHashCode();
            }

            return hashCode;
        }
    }
}
