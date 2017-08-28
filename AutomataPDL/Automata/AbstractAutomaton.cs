using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public abstract class AbstractAutomaton<C, S>
    {
        public HashSet<State<S>> Q { get; set; }
        public HashSet<C> Sigma { get; set; }
        public HashSet<State<S>> F { get; set; }

        public void Enumerate()
        {
            for (int i = 0; i < Q.Count; i++)
            {
                Q.ElementAt(i).id = i;
            }
        }
    }
}
