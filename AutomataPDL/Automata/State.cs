using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public class State<S>
    {
        public int id { get; set; }

        public S label { get; set; }

        public State(int id_in, S label_in)
        {
            id = id_in;
            label = label_in;
        }

        public State(S label_in)
        {
            id = 0;
            label = label_in;
        }
    }
}
