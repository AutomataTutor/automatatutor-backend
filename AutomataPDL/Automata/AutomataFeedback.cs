using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutomataPDL.Utilities;

namespace AutomataPDL.Automata
{
    public static class AutomataFeedback
    {

        //  Powerset Construction

        public static Tuple<int, List<string>> FeedbackForPowersetConstruction<C>(NFA<C, string> N, DFA<C, string> attemptDFA)
        {
            var correctDFA = AutomataUtilities.RelabelStatesFromSetsToStrings<C>(N.NFAtoDFA());

            return FeedbackForArbitraryConstructions<C,string>(correctDFA, attemptDFA);
        }

        //  Product Construction

        public static Tuple<int, List<string>> FeedbackForProductConstruction<C>(List<DFA<C, string>> automataList, BooleanOperation boolOp, DFA<C, string> attemptDFA)
        {
            var correctDFA = AutomataUtilities.RelabelStatesFromNTuplesToStrings<C>(AutomataUtilities.ExecuteSetOperation(automataList, boolOp));

            return FeedbackForArbitraryConstructions<C,string>(correctDFA, attemptDFA);
        }

        //  Feedback for arbitrary constructed automata

        public static Tuple<int, List<string>> FeedbackForArbitraryConstructions<C, S>(DFA<C, S> correctDFA, DFA<C, S> attemptDFA)
        {
            var feed = new List<string>();

            var M = new HashSet<State<S>>();
            var attemptStateDict = new Dictionary<S, State<S>>();
            int noCorrectTransitions = 0;

            var q_0_c = correctDFA.q_0;
            var q_0_a = attemptDFA.q_0;

            if (!q_0_c.label.Equals(q_0_a.label))
            {
                feed.Add("You should adjust your initial state!");
            }

            foreach (var q_a in attemptDFA.Q)
            {
                attemptStateDict.Add(q_a.label, q_a);
            }

            foreach(var q_c in correctDFA.Q)
            {
                State<S> q_a;
                if (attemptStateDict.TryGetValue(q_c.label, out q_a))
                {
                    foreach (C a in correctDFA.Sigma)
                    {
                        State<S> q_c_dash, q_a_dash;
                        if (!correctDFA.delta.TryGetValue(new TwoTuple<State<S>, C>(q_c, a), out q_c_dash)) { }
                        if (!attemptDFA.delta.TryGetValue(new TwoTuple<State<S>, C>(q_a, a), out q_a_dash)) { }

                        if (!q_c_dash.label.Equals(q_a_dash.label))
                        {
                            M.Add(q_a);
                        }
                        else
                        {
                            noCorrectTransitions++;
                        }
                    }
                }
            }

            float sim = 1.0f * noCorrectTransitions / correctDFA.delta.Count;
            int finalPenalty = 0;
            foreach (var q_c in correctDFA.Q)
            {
                State<S> q_a;
                if (!attemptStateDict.TryGetValue(q_c.label, out q_a)) { continue; }

                bool final_c = correctDFA.F.Contains(q_c);
                bool final_a = attemptDFA.F.Contains(q_a);

                if ((final_a && !final_c) || (!final_a && final_c))
                {
                    if(finalPenalty < 2)
                    {
                        finalPenalty++;
                    }
                }
            }

            int grade = (int)(10.0f * sim);
            grade -= finalPenalty;
            if (grade < 0)
                grade = 0;
            
            for (int i = 0; i < M.Count; i++)
            {
                var q = M.ElementAt(i);
                feed.Add("You should take a look at the transitions going out of state [" + q.label + "]");
            }

            switch (finalPenalty)
            {
                case 0:
                    break;
                case 1:
                    feed.Add("You need to change the acceptance condition of a state.");
                    break;
                default:
                    feed.Add("You need to change the acceptance condition of some states.");
                    break;
            }

            if (grade == 10)
            {
                feed = new List<string>();
                feed.Add("CORRECT!!");
            }

            return new Tuple<int, List<string>>(grade, feed);

        }

        public static Tuple<int, List<string>> FeedbackForArbitraryConstruction<C,S>(DFA<C, S> correctDFA, DFA<C, S> attemptDFA)
        {
            bool madeMistake = true;

            int grade = 10;
            var feedbackList = new List<string>();

            var W = new HashSet<State<S>>();
            var CheckedDict = new Dictionary<State<S>, State<S>>();

            var q_0_c = correctDFA.q_0;
            var q_0_a = attemptDFA.q_0;

            if (!q_0_c.label.Equals(q_0_a.label))
            {
                feedbackList.Add("You should adjust your initial state!");
                madeMistake = false;
            }

            W.Add(q_0_c);
            CheckedDict.Add(q_0_c, q_0_a);

            while (W.Count > 0 && madeMistake)
            {
                var q_c = W.ElementAt(0);
                W.Remove(q_c);

                State<S> q_a;
                if (!CheckedDict.TryGetValue(q_c, out q_a)) { }

                foreach (C a in correctDFA.Sigma)
                {
                    State<S> q_c_dash, q_a_dash;
                    if (!correctDFA.delta.TryGetValue(new TwoTuple<State<S>, C>(q_c, a), out q_c_dash)) { }
                    if (!attemptDFA.delta.TryGetValue(new TwoTuple<State<S>, C>(q_a, a), out q_a_dash)) { }

                    if (!q_c_dash.label.Equals(q_a_dash.label))
                    {
                        feedbackList.Add("You should take a look at the transitions going out of state [" + q_a.label + "]");
                        madeMistake = false;
                    }
                    else if (!CheckedDict.ContainsKey(q_c_dash))
                    {
                        W.Add(q_c_dash);
                        CheckedDict.Add(q_c_dash, q_a_dash);
                    }
                }
            }

            bool acceptingCorrect = true;
            foreach (var q_c in correctDFA.Q)
            {
                State<S> q_a;
                if (!CheckedDict.TryGetValue(q_c, out q_a)) { continue; }

                bool final_c = correctDFA.F.Contains(q_c);
                bool final_a = attemptDFA.F.Contains(q_a);

                if ((final_a && !final_c) || (!final_a && final_c))
                    acceptingCorrect = false;
            }

            if (!acceptingCorrect)
                feedbackList.Add("You'll need to change the accepting condition of some states!");

            return new Tuple<int, List<string>>(grade, feedbackList);
        }

        //  Minimization

        public static Tuple<int, List<string>> FeedbackForMinimization(bool[] tableCorrect, bool[] tableAttempt)
        {
            int grade = 10;
            var feedbackList = new List<string>();
            if (tableAttempt.Length != tableCorrect.Length)
                throw new ArgumentException("Both minimization tables should have the same length!");

            int l = tableCorrect.Length;
            for (int n = 0; n < l; n++)
            {
                bool a = tableAttempt[n];
                bool c = tableCorrect[n];
                int j = (int)(Math.Sqrt(2*n + 1.75) + 0.5);
                int i = n - (j*j - j)/2;
                if (!a && c)
                {
                    feedbackList.Add("Take a look at states " + i + " and " + j + " again!");
                }
                if (a && !c)
                {
                    feedbackList.Add("Take a look at states " + i + " and " + j + " again!");
                }
            }

            return new Tuple<int, List<string>>(grade, feedbackList);
        }

        public static Tuple<int, List<string>> FeedbackForMinimizationTable<S>(string[] T_C, string[] T_A, DFA<char,S> D)
        {
            var feedbackList = new List<string>();

            var G = BuildDependencyGraph(D);
            int maxLength = -1;
            for (int i = 0; i < T_C.Length; i++)
            {
                if (T_C[i] != null && T_C[i].Length > maxLength)
                    maxLength = T_C[i].Length;
            }
            for (int i = 0; i < T_A.Length; i++)
            {
                if (T_A[i] != null && T_A[i].Length > maxLength)
                    maxLength = T_A[i].Length;
            }

            for (int i = 0; i <= maxLength; i++)
            {
                for (int j = 0; j <= D.Q.Count; j++)
                {
                    for (int k = j + 1; k <= D.Q.Count; k++)
                    {
                        var p = D.Q.ElementAt(j);
                        var q = D.Q.ElementAt(k);

                        int p_id = p.id;
                        int q_id = q.id;

                        if (p_id > q_id)
                        {
                            int tmp = p_id;
                            p_id = q_id;
                            q_id = tmp;

                            var tmp2 = p;
                            p = q;
                            q = tmp2;
                        }

                        string w_A = T_A[(q_id * q_id) - q_id / 2 + p_id];
                        string w_C = T_C[(q_id * q_id) - q_id / 2 + p_id];
                        int n_A = (w_A != null) ? w_A.Length : Int32.MaxValue;
                        int n_C = (w_C != null) ? w_C.Length : Int32.MaxValue;

                        if (Math.Min(n_A, n_C) == i)
                        {
                            if (IsMistake(w_A, p, q, D))
                            {
                                feedbackList.Add(feedbackCases<S>(w_A, w_C, i, p, q, D));
                                markRed<S>(w_A, p, q, G, T_A, D);
                            }
                            else
                            {
                                mark<char,S>(p, q, G);
                            }
                        }
                    }
                }
            }

            int grade = 10;

            return new Tuple<int, List<string>>(grade, feedbackList);
        }

        private static void mark<C, S>(State<S> p, State<S> q, Dictionary<Set<State<S>>, TwoTuple<bool, List<Set<State<S>>>>> G)
        {
            TwoTuple<bool, List<Set<State<S>>>> t;
            var M = new Set<State<S>>();
            M.content.Add(p);
            M.content.Add(q);
            if (G.TryGetValue(M, out t))
            {
                t.first = true;
            }
        }

        private static void markRed<S>(string w, State<S> p, State<S> q, Dictionary<Set<State<S>>, TwoTuple<bool, List<Set<State<S>>>>> G, string[] T, DFA<char, S> D)
        {
            TwoTuple<bool, List<Set<State<S>>>> t;
            var M = new Set<State<S>>();
            M.content.Add(p);
            M.content.Add(q);
            if (G.TryGetValue(M, out t))
            {
                t.first = true;

                foreach (var X in t.second)
                {
                    TwoTuple<bool, List<Set<State<S>>>> t_dash;
                    if (G.TryGetValue(X, out t_dash) && !t_dash.first && X.content.Count == 2)
                    {
                        var p_dash = X.content.ElementAt(0);
                        var q_dash = X.content.ElementAt(1);

                        int p_id = p_dash.id;
                        int q_id = q_dash.id;

                        if (p_id > q_id)
                        {
                            int tmp = p_id;
                            p_id = q_id;
                            q_id = tmp;

                            var tmp2 = p_dash;
                            p_dash = q_dash;
                            q_dash = tmp2;
                        }
                        string w_dash = T[(q_id * q_id) - q_id / 2 + p_id];

                        if (w_dash != null && w_dash.Length > 0)
                        {
                            char a = w_dash.ElementAt(0);
                            string w2 = w_dash.Substring(1);

                            State<S> delta_p_dash_a, delta_q_dash_a;
                            if (D.delta.TryGetValue(new TwoTuple<State<S>, char>(p_dash, a), out delta_p_dash_a)) { }
                            if (D.delta.TryGetValue(new TwoTuple<State<S>, char>(q_dash, a), out delta_q_dash_a)) { }
                            if (w.Equals(w2) && delta_p_dash_a.Equals(p_dash) && delta_q_dash_a.Equals(q_dash) && IsMistake(w_dash, p_dash, q_dash, D))
                            {
                                markRed(w_dash, p_dash, q_dash, G, T, D);
                            }
                        }
                    }
                }
            }
        }

        private static string feedbackCases<S>(string w_A, string w_C, int n, State<S> p, State<S> q, DFA<char, S> D)
        {
            string outstring = "";

            int n_A = (w_A != null) ? w_A.Length : -1;
            int n_C = (w_C != null) ? w_C.Length : -1;

            var w_A_sequence = w_A.ToList();

            int p_id = p.id;
            int q_id = q.id;

            var p_dash = D.ReadWordFrom(w_A_sequence, p);
            var q_dash = D.ReadWordFrom(w_A_sequence, q);

            int p_dash_id = p_dash.id;
            int q_dash_id = q_dash.id;

            bool p_acc = D.F.Contains(p_dash);
            bool q_acc = D.F.Contains(q_dash);

            if (n == 0)
            {
                if (n_A == 0) //w_A == epsilon
                {
                    if (n_C == -1)
                    {
                        outstring += "Both " + p_id + " and " + q_id + " are ";
                        outstring += (p_acc) ? "" : "non-";
                        outstring += "accepting states and therefore can't be distinguished by 'epsilon'.";
                        return outstring;
                    }
                    else //n_C >= 1
                    {
                        outstring += "Both " + p_id + " and " + q_id + " are ";
                        outstring += (p_acc) ? "" : "non-";
                        outstring += "accepting states and therefore can't be distinguished by 'epsilon'.\n\t";
                        outstring += "Maybe a longer word distinguishes them, though.";
                        return outstring;
                    }
                }
                else //w_C == epsilon
                {
                    if (n_A == -1)
                    {
                        outstring += p_id + " and " + q_id + " are distinguishable.";
                        return outstring;
                    }
                    else //n_A >= 1
                    {
                        if (p_acc == q_acc)
                        {
                            //delta(p,w_A) and delta(q,w_A) must have same acceptance condition
                            outstring += "Reading " + w_A + " from states (" + p_id + q_id + ") gets you to states (" + p_dash_id + q_dash_id + "), both of which are ";
                            outstring += (p_acc) ? "" : "not ";
                            outstring += "accepting.\n\t A different word may distinguish " + p_id + " and " + q_id + " though.";
                            return outstring;
                        }
                        else
                        {
                            outstring += "The given word '" + w_A + "' distinguishes states " + p_id + " and " + q_id + ".\n\t There is a shorter distinguishing word, though.";
                            return outstring;
                        }
                    }
                }
            }
            else //n > 0
            {
                if (n_A == n)
                {
                    if (n_C == -1)
                    {
                        //delta(p,w_A) and delta(q,w_A) must have same acceptance condition
                        outstring += "Reading " + w_A + " from states (" + p_id + q_id + ") gets you to states (" + p_dash_id + q_dash_id + "), both of which are ";
                        outstring += (p_acc) ? "" : "not ";
                        outstring += "accepting.";
                        return outstring;
                    }
                    else if (n_C > n)
                    {
                        //delta(p,w_A) and delta(q,w_A) must have same acceptance condition
                        outstring += "Reading " + w_A + " from states (" + p_id + q_id + ") gets you to states (" + p_dash_id + q_dash_id + "), both of which are ";
                        outstring += (p_acc) ? "" : "not ";
                        outstring += "accepting.\n\t A longer word may distinguish " + p_id + " and " + q_id + " though.";
                        return outstring;
                    }
                    else if (p_acc == q_acc) //Means n_C == n.
                    {
                        //delta(p,w_A) and delta(q,w_A) must have same acceptance condition
                        outstring += "Reading " + w_A + " from states (" + p_id + q_id + ") gets you to states (" + p_dash_id + q_dash_id + "), both of which are ";
                        outstring += (p_acc) ? "" : "not ";
                        outstring += "accepting.\n\t A different word may distinguish " + p_id + " and " + q_id + " though.";
                        return outstring;
                    }
                }
                else //Means n_C == n.
                {
                    if (n_A == -1)
                    {
                        outstring += p_id + " and " + q_id + " are distinguishable.";
                        return outstring;
                    }
                    else //Means n_A > n.
                    {
                        if (p_acc == q_acc)
                        {
                            //delta(p,w_A) and delta(q,w_A) must have same acceptance condition
                            outstring += "Reading " + w_A + " from states (" + p_id + q_id + ") gets you to states (" + p_dash_id + q_dash_id + "), both of which are ";
                            outstring += (p_acc) ? "" : "not ";
                            outstring += "accepting.\n\t A different word may distinguish " + p_id + " and " + q_id + " though.";
                            return outstring;
                        }
                        else
                        {
                            outstring += "The given word '" + w_A + "' distinguishes states " + p_id + " and " + q_id + ".\n\t There is a shorter distinguishing word, though.";
                            return outstring;
                        }
                    }
                }
            }

            return "";
        }

        //Returns 'true' iff w doesn't distinguish states p and q.
        private static bool IsMistake<S>(string w, State<S> p, State<S> q, DFA<char, S> D)
        {
            List<char> l = w.ToList();

            bool b = D.AcceptsFrom(l, p);
            bool c = D.AcceptsFrom(l, q);

            return (b && c) || (!b && !c);
        }

        private static Dictionary<Set<State<S>>, TwoTuple<bool, List<Set<State<S>>>>> BuildDependencyGraph<C,S>(DFA<C,S> D)
        {
            //Dependency Graph for automaton D modelled as a Dictionary
            //'Value' is set of all nodes, that 'key' has an edge to
            var G = new Dictionary<Set<State<S>>, TwoTuple<bool, List<Set<State<S>>>>>();
            for (int i = 0; i < D.Q.Count; i++)
            {
                for (int j = i + 1; j < D.Q.Count; j++)
                {
                    var p = D.Q.ElementAt(i);
                    var q = D.Q.ElementAt(j);
                    var v = new Set<State<S>>();
                    v.content.Add(p);
                    v.content.Add(q);
                    var l = new List<Set<State<S>>>();
                    G.Add(v, new TwoTuple<bool, List<Set<State<S>>>>(false, l));
                }
            }

            for (int i = 0; i < D.Q.Count; i++)
            {
                for (int j = i + 1; j < D.Q.Count; j++)
                {
                    var p_dash = D.Q.ElementAt(i);
                    var q_dash = D.Q.ElementAt(j);
                    var v_dash = new Set<State<S>>();
                    v_dash.content.Add(p_dash);
                    v_dash.content.Add(q_dash);

                    foreach(C a in D.Sigma)
                    {
                        var t1 = new TwoTuple<State<S>, C>(p_dash, a);
                        var t2 = new TwoTuple<State<S>, C>(q_dash, a);
                        State<S> p, q;
                        if (!D.delta.TryGetValue(t1, out p)) { }
                        if (!D.delta.TryGetValue(t2, out q)) { }
                        Set<State<S>> v = new Set<State<S>>();
                        v.content.Add(p);
                        v.content.Add(q);
                        TwoTuple<bool, List<Set<State<S>>>> t;
                        if (!G.TryGetValue(v, out t)) { }
                        t.second.Add(v_dash);
                    }
                }
            }

            return G;
        }
    }
}
