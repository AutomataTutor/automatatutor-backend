using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public class TwoTuple<T1, T2>
    {
        public T1 first { get; set; }
        public T2 second { get; set; }

        public TwoTuple(T1 first_in, T2 second_in)
        {
            first = first_in;
            second = second_in;
        }

        public override bool Equals(object obj)
        {
            var other = (TwoTuple<T1, T2>) obj;

            return this.first.Equals(other.first) && this.second.Equals(other.second);
        }

        public override int GetHashCode()
        {
            return first.GetHashCode() + second.GetHashCode();
        }
    }
}
