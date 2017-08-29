using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.Automata
{
    public class DFA<C, S> : AbstractAutomaton<C, S>
    {
        public Dictionary<TwoTuple<State<S>, C>, State<S>> delta { get; set; }
        public State<S> q_0 { get; set; }

        public DFA(HashSet<State<S>> Q_in, HashSet<C> Sigma_in, Dictionary<TwoTuple<State<S>, C>, State<S>> delta_in, State<S> q_0_in, HashSet<State<S>> F_in)
        {
            Q = Q_in;
            Sigma = Sigma_in;
            delta = delta_in;
            q_0 = q_0_in;
            F = F_in;

            //TODO: Test for "q_0 \in Q" and "F \subseteq Q"
        }

        /*  METHOD THAT RUNS THIS DFA ON A WORD
        *
        *   Input:  sequence:   Word over the alphabet of characters of type C
        *
        *   Output: true, if this DFA accepts the word 'sequence'
        *           false, otherwise
        */
        public bool AcceptsFrom(List<C> sequence, State<S> q)
        {
            State<S> current = q;

            for (int i = 0; i < sequence.Count; i++)
            {
                TwoTuple<State<S>, C> tuple = new TwoTuple<State<S>, C>(current, sequence[i]);
                if (!delta.TryGetValue(tuple, out current))
                    return false;
            }

            return F.Contains(current);
        }

        public bool Accepts(List<C> sequence)
        {
            return AcceptsFrom(sequence, q_0);
        }

        public State<S> ReadWordFrom(List<C> sequence, State<S> q)
        {
            State<S> current = q;

            for (int i = 0; i < sequence.Count; i++)
            {
                TwoTuple<State<S>, C> tuple = new TwoTuple<State<S>, C>(current, sequence[i]);
                if (!delta.TryGetValue(tuple, out current)) {
                    return null;
                }
            }

            return current;
        }

        public State<S> ReadWord(List<C> sequence)
        {
            return ReadWordFrom(sequence, q_0);
        }

        public string[] GetMinimizationTable()
        {
            int n = Q.Count;
            string[] T = new string[(n * n - n) / 2];
            bool stop = true;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var p = Q.ElementAt(i);
                    var q = Q.ElementAt(j);

                    if (p.id > q.id)
                    {
                        var tmp = p;
                        p = q;
                        q = tmp;
                    }

                    bool b = F.Contains(p);
                    bool c = F.Contains(q);

                    if ((b && !c) || (!b && c))
                    {
                        T[(q.id * q.id - q.id) / 2 + p.id] = "";
                        stop = false;
                    }
                }
            }

            while (!stop)
            {
                stop = true;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        var p = Q.ElementAt(i);
                        var q = Q.ElementAt(j);

                        if (p.id > q.id)
                        {
                            var tmp = p;
                            p = q;
                            q = tmp;
                        }

                        if (T[(q.id * q.id - q.id) / 2 + p.id] == null)
                        {
                            foreach (C a in Sigma)
                            {
                                State<S> p_dash, q_dash;
                                if (!delta.TryGetValue(new TwoTuple<State<S>, C>(p, a), out p_dash)) { }
                                if (!delta.TryGetValue(new TwoTuple<State<S>, C>(q, a), out q_dash)) { }

                                if (!p_dash.Equals(q_dash))
                                {
                                    if (p_dash.id > q_dash.id)
                                    {
                                        var tmp = p_dash;
                                        p_dash = q_dash;
                                        q_dash = tmp;
                                    }

                                    string s = T[(q_dash.id * q_dash.id - q_dash.id) / 2 + p_dash.id];

                                    if (s != null)
                                    {
                                        T[(q.id * q.id - q.id) / 2 + p.id] = a.ToString() + s;
                                        stop = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return T;
        }

        /*  METHOD THAT MINIMIZES THIS DFA USING HOPCROFT'S ALGORITHM
        *
        *   Output: Minimal DFA recognizing the same language as this one.
        *           States are labelled as sets of states of this automaton.
        */
        public DFA<C, HashSet<State<S>>> MinimizeHopcroft()
        {
            int no_states = Q.Count;

            //Stores for state q (key) in which set (value) of the state partition it is
            //(the value is the new state made out of this set).
            var state_dict = new Dictionary<State<S>, State<HashSet<State<S>>>>();

            var P = ComputeLanguagePartition();
            var Q_new = new HashSet<State<HashSet<State<S>>>>();
            var delta_new = new Dictionary<TwoTuple<State<HashSet<State<S>>>, C>, State<HashSet<State<S>>>>();
            var F_new = new HashSet<State<HashSet<State<S>>>>();

            State<HashSet<State<S>>> q_0_new = null;

            int i = 0;
            foreach (HashSet<State<S>> B in P)
            {
                var B_as_State = new State<HashSet<State<S>>>(i, B);

                Q_new.Add(B_as_State);
                if (F.Contains(B.ElementAt(0)))
                    F_new.Add(B_as_State);

                foreach (State<S> q in B)
                {
                    state_dict.Add(q, B_as_State);
                    if (q.Equals(q_0))
                        q_0_new = B_as_State;
                }
                i++;
            }

            foreach (var c in delta)
            {
                State<HashSet<State<S>>> B1, B2;
                State<S> q1 = c.Key.first, q2 = c.Value;
                C a = c.Key.second;

                if (!state_dict.TryGetValue(q1, out B1) | !state_dict.TryGetValue(q2, out B2))
                {
                    //TODO: Throw some exception
                }
                var tuple = new TwoTuple<State<HashSet<State<S>>>, C>(B1, a);
                State<HashSet<State<S>>> B_out;
                if(!delta_new.TryGetValue(tuple, out B_out))
                    delta_new.Add(new TwoTuple<State<HashSet<State<S>>>, C>(B1, a), B2);
            }

            if (q_0_new == null)
            {
                //TODO: throw some exception or another
            }

            return new DFA<C, HashSet<State<S>>>(Q_new, Sigma, delta_new, q_0_new, F_new);
        }

        /*  METHOD THAT COMPUTES THE LANGUAGE PARTITION OF THIS DFA
        *
        *   Output: Set of sets of states, that are equivalent
        */
        public HashSet<HashSet<State<S>>> ComputeLanguagePartition()
        {
            //Trivial cases: Either F or Q\F is empty
            bool return_immediately = false;
            if (F.Count == 0)
                return_immediately = true;

            var Q_without_F = new HashSet<State<S>>();
            foreach (State<S> q in Q)
            {
                if (!F.Contains(q))
                    Q_without_F.Add(q);
            }
            
            if (Q_without_F.Count == 0)
                return_immediately = true;

            if (return_immediately)
            {
                var set_containing_only_Q = new HashSet<HashSet<State<S>>>();
                set_containing_only_Q.Add(Q);
                return set_containing_only_Q;
            }

            //Initialize partition and workset (i.e. set of possible splitters):
            var P = new HashSet<HashSet<State<S>>>();
            P.Add(F);
            P.Add(Q_without_F);

            HashSet<Splitter> W = new HashSet<Splitter>();
            HashSet<State<S>> smaller_set;
            if (F.Count < Q_without_F.Count)
                smaller_set = F;
            else
                smaller_set = Q_without_F;

            foreach (C c in Sigma)
            {
                W.Add(new Splitter(c, smaller_set));
            }

            //Main Loop
            while (W.Count > 0)
            {
                Splitter splitter = W.ElementAt(0);
                W.Remove(splitter);
                C a = splitter.a;
                HashSet<State<S>> B_dash = splitter.B;

                //Creating a copy of P for iteration over all sets in P is necessary,
                //since P is changed within 'foreach'-loop. 
                HashSet<HashSet<State<S>>> Copy_of_P = new HashSet<HashSet<State<S>>>();
                foreach (HashSet<State<S>> B in P)
                {
                    Copy_of_P.Add(B);
                }

                foreach (HashSet<State<S>> B in Copy_of_P)
                {
                    HashSet<State<S>> B_0, B_1;
                    if (IsSplitBy(B, splitter, out B_0, out B_1))
                    {
                        //Replace B by B_0 and B_1 in P
                        P.Remove(B);
                        P.Add(B_0);
                        P.Add(B_1);

                        //Add new possible splitter(s) to W
                        foreach (C b in Sigma)
                        {
                            Splitter new_splitter = new Splitter(b, B);
                            if (W.Contains(splitter))
                            {
                                W.Remove(splitter);
                                W.Add(new Splitter(b, B_0));
                                W.Add(new Splitter(b, B_1));
                            }
                            else if (B_0.Count < B_1.Count)
                            {
                                W.Add(new Splitter(b, B_0));
                            }
                            else
                            {
                                W.Add(new Splitter(b, B_1));
                            }
                        }
                    }
                }
            }

            return P;
        }


        private bool IsSplitBy(HashSet<State<S>> B, Splitter splitter, out HashSet<State<S>> B_0, out HashSet<State<S>> B_1)
        {
            C a = splitter.a;
            HashSet<State<S>> B_dash = splitter.B;

            B_0 = new HashSet<State<S>>();
            B_1 = new HashSet<State<S>>();

            foreach (State<S> q in B)
            {
                State<S> delta_q_a;
                if (!delta.TryGetValue(new TwoTuple<State<S>, C>(q, a), out delta_q_a))
                {
                    //When you're here it means that the transition function delta is not total!
                }
                if (B_dash.Contains(delta_q_a))
                {
                    B_0.Add(q);
                }
                else
                {
                    B_1.Add(q);
                }
            }

            return B_0.Count > 0 && B_1.Count > 0;
        }

        private class Splitter
        {
            public C a;
            public HashSet<State<S>> B;

            public Splitter(C a_in, HashSet<State<S>> B_in)
            {
                a = a_in;
                B = B_in;
            }

            public override bool Equals(object obj)
            {
                var other = (Splitter)obj;

                return this.a.Equals(other.a) && this.B.Equals(other.B);
            }

            public override int GetHashCode()
            {
                return a.GetHashCode() + B.GetHashCode();
            }
        }
    }
}
