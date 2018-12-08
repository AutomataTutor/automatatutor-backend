using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public class NFA<C,S> : AbstractAutomaton<C,S>
    {
        public Dictionary<TwoTuple<State<S>, C>, HashSet<State<S>>> delta { get; set; }
        public HashSet<State<S>> Q_0 { get; set; }

        public NFA(HashSet<State<S>> Q_in, HashSet<C> Sigma_in, Dictionary<TwoTuple<State<S>, C>, HashSet<State<S>>> delta_in, HashSet<State<S>> Q_0_in, HashSet<State<S>> F_in)
        {
            Q = Q_in;
            Sigma = Sigma_in;
            delta = delta_in;
            Q_0 = Q_0_in;
            F = F_in;

            //TODO: Test for "Q_0 \subseteq Q" and "F \subseteq Q"
        }

        public DFA<C, Set<State<S>>> NFAtoDFA()
        {
            var Q_new = new HashSet<State<Set<State<S>>>>();
            var delta_new = new Dictionary<TwoTuple<State<Set<State<S>>>, C>, State<Set<State<S>>>>();
            var F_new = new HashSet<State<Set<State<S>>>>();

            var W = new HashSet<Set<State<S>>>();
            var StateDict = new Dictionary<Set<State<S>>, State<Set<State<S>>>>();

            int id = 0;

            var Q_0_set = new Set<State<S>>(Q_0);
            var q_0 = new State<Set<State<S>>>(id, Q_0_set); id++;
            W.Add(Q_0_set);
            Q_new.Add(q_0);
            StateDict.Add(Q_0_set, q_0);

            while (W.Count > 0)
            {
                var Q_dash_set = W.ElementAt(0);
                W.Remove(Q_dash_set);
                State<Set<State<S>>> Q_dash;
                if (!StateDict.TryGetValue(Q_dash_set, out Q_dash)) { }
                if (((Q_dash_set.content.Intersect<State<S>>(F))).Count<State<S>>() > 0)
                    F_new.Add(Q_dash);

                foreach (C a in Sigma)
                {
                    var Q_dash_dash_set = new Set<State<S>>();
                    foreach (State<S> q in Q_dash_set.content)
                    {
                        HashSet<State<S>> delta_set;
                        if (!delta.TryGetValue(new TwoTuple<State<S>, C>(q, a), out delta_set)) { continue; }
                        Q_dash_dash_set.content.UnionWith(delta_set);
                    }

                    State<Set<State<S>>> Q_dash_dash;
                    if (!StateDict.TryGetValue(Q_dash_dash_set, out Q_dash_dash))
                    {
                        Q_dash_dash = new State<Set<State<S>>>(id, Q_dash_dash_set); id++;
                        W.Add(Q_dash_dash_set);
                        Q_new.Add(Q_dash_dash);
                        StateDict.Add(Q_dash_dash_set, Q_dash_dash);
                    }

                    delta_new.Add(new TwoTuple<State<Set<State<S>>>, C>(Q_dash, a), Q_dash_dash);
                }
            }

            return new DFA<C, Set<State<S>>>(Q_new, Sigma, delta_new, q_0, F_new);
        }
    }
}
