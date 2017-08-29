using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Linq;

using AutomataPDL.Utilities;

namespace AutomataPDL.Automata
{
    public static class AutomataUtilities
    {

        /*  METHOD TO EXECUTE ARBITRARY SET OPERATIONS ON THE LANGUAGES OF AUTOMATA
        *
        *   Input:  automataList:   The list of deterministic automata on which the set operation is executed
        *           boolOp:         The boolean operation describing the set operation, that is to be executed
        *
        *   Output: Deterministic automaton, that encodes the language obtained from applying the specified set operation on the languages of the input automata
        */
        public static DFA<C, NTuple<State<S>>> ExecuteSetOperation<C, S>(List<DFA<C, S>> automataList, BooleanOperation boolOp)
        {
            int noAutomata = automataList.Count;

            //Initialize all five components (Q, Sigma, delta, q_0, F) of the resulting automaton:
            var Q = new HashSet<State<NTuple<State<S>>>>();
            var Sigma = automataList[0].Sigma;
            var delta = new Dictionary<TwoTuple<State<NTuple<State<S>>>, C>, State<NTuple<State<S>>>>();
            var F = new HashSet<State<NTuple<State<S>>>>();

            var TupleStateDict = new Dictionary<NTuple<State<S>>, State<NTuple<State<S>>>>();

            var q_0_as_array = new State<S>[noAutomata];
            for (int i = 0; i < noAutomata; i++)
            {
                q_0_as_array[i] = automataList[i].q_0;
            }
            var q_0_as_ntuple = new NTuple<State<S>>(q_0_as_array);

            //Initialize workset with q_0. The workset contains all state (in NTuple-form) that still need to be processed
            var W = new HashSet<NTuple<State<S>>>();
            W.Add(q_0_as_ntuple);

            int id = 0;
            var q_0 = new State<NTuple<State<S>>>(id, q_0_as_ntuple); id++;
            Q.Add(q_0);
            TupleStateDict.Add(q_0_as_ntuple, q_0);

            //Main loop:
            while (W.Count > 0)
            {
                var q_tuple = W.ElementAt(0);
                W.Remove(q_tuple);

                State<NTuple<State<S>>> q;
                if (!TupleStateDict.TryGetValue(q_tuple, out q))
                {
                    //TODO: Exception ??
                }

                //If 'q_tuple' is final, add it to 'F_Tuples'
                bool[] q_acceptance_array = new bool[noAutomata];
                for (int i = 0; i < noAutomata; i++)
                {
                    q_acceptance_array[i] = automataList[i].F.Contains(q_tuple.content[i]);
                }
                if (boolOp.IsTrueForInterpretation(q_acceptance_array))
                    F.Add(q);

                //Determine where transitions out of 'q_tuple' need to go
                foreach (C a in Sigma)
                {
                    var q_dash_array = new State<S>[noAutomata];
                    for (int i = 0; i < noAutomata; i++)
                    {
                        State<S> q_i_dash;
                        var q_i_a_tuple = new TwoTuple<State<S>, C>(q_tuple.content[i], a);
                        if (!automataList[i].delta.TryGetValue(q_i_a_tuple, out q_i_dash))
                        {
                            //TODO: Throw exception. Non-total transition function!
                        }
                        q_dash_array[i] = q_i_dash;
                    }
                    var q_dash_tuple = new NTuple<State<S>>(q_dash_array);
                    State<NTuple<State<S>>> q_dash;
                    if (!TupleStateDict.ContainsKey(q_dash_tuple))
                    {
                        W.Add(q_dash_tuple);
                        q_dash = new State<NTuple<State<S>>>(id, q_dash_tuple); id++;
                        Q.Add(q_dash);
                        TupleStateDict.Add(q_dash_tuple, q_dash);
                    }
                    else if (!TupleStateDict.TryGetValue(q_dash_tuple, out q_dash))
                    {
                        //TODO: Exception. That'd be weird.
                    }
                    delta.Add(new TwoTuple<State<NTuple<State<S>>>, C>(q, a), q_dash);
                }
            }

            return new DFA<C, NTuple<State<S>>>(Q, Sigma, delta, q_0, F);
        }

        //Old version of method 'ExecuteSetOperation':
        public static DFA<C,List<State<S>>> ExecuteSetOperationOld<C,S>(List<DFA<C,S>> automataList, BooleanOperation boolOp)
        {
            int noAutomata = automataList.Count;

            //Give every state in each automaton 'Q' in 'automataList' a unique id between '0' and 'Q.Count' (included and excluded):
            foreach (DFA<C, S> D in automataList)
                D.Enumerate();

            //Array, thats i-th component contains the number of states of 'automataList[i]':
            int[] automataSizes = new int[noAutomata];
            for (int i = 0; i < noAutomata; i++)
                automataSizes[i] = automataList[i].Q.Count;

            //Stores references to all states of product-automaton, that have already been created:
            Array Q_reference_array = Array.CreateInstance(typeof(State<List<State<S>>>), automataSizes);

            //Initialize all five components (Q,Sigma,delta,q_0,F) of the resulting automaton:
            HashSet<State<List<State<S>>>> Q = new HashSet<State<List<State<S>>>>();
            HashSet<C> Sigma = automataList[0].Sigma;
            Dictionary< TwoTuple<State<List<State<S>>>,C> , State<List<State<S>>> > delta = new Dictionary<TwoTuple<State<List<State<S>>>, C>, State<List<State<S>>>>();
            HashSet<State<List<State<S>>>> F = new HashSet<State<List<State<S>>>>();

            List<State<S>> q_0_as_list = new List<State<S>>();
            foreach (DFA<C,S> D in automataList)
            {
                q_0_as_list.Add(D.q_0);
            }

            //Initialize workset with the initial state [q_0_1, q_0_2, ...]:
            HashSet<List<State<S>>> W = new HashSet<List<State<S>>>();
            W.Add(q_0_as_list);

            while (W.Count > 0)
            {
                //Pick 'q = [q_1,q_2,...]' from 'W' and add it to 'Q'.
                //Mark in 'Q_reference_array', that 'q' has been added, by referencing it at the position determined by the id's of the states 'q_1', 'q_2', ... respectively:
                List<State<S>> q_list = W.ElementAt(0);
                W.Remove(q_list);

                int[] indices_q = new int[noAutomata];
                for (int i = 0; i < noAutomata; i++)
                    indices_q[i] = q_list[i].id;

                State<List<State<S>>> q;
                if (Q_reference_array.GetValue(indices_q) == null)
                {
                    q = new State<List<State<S>>>(q_list);
                    Q_reference_array.SetValue(q, indices_q);
                }
                else
                {
                    q = (State<List<State<S>>>)Q_reference_array.GetValue(indices_q);
                }
                Q.Add(q);

                //Add q to F if it is accepting
                bool[] q_acceptance_array = new bool[noAutomata];
                for(int i = 0; i < noAutomata; i++)
                {
                    q_acceptance_array[i] = automataList[i].F.Contains(q_list[i]);
                }
                if (boolOp.IsTrueForInterpretation(q_acceptance_array))
                {
                    F.Add(q);
                }

                //Determine, where transitions out of 'q' need to go:
                foreach (C c in Sigma)
                {
                    //Determine 'q_new = delta(q, c)' and its id-array ('indices'):
                    List<State<S>> q_new_list = new List<State<S>>();
                    int[] indices = new int[noAutomata];
                    for(int i = 0; i < noAutomata; i++)
                    {
                        State<S> q_temp;
                        TwoTuple<State<S>, C> tuple = new TwoTuple<State<S>, C>(q_list[i], c);
                        if (!automataList[i].delta.TryGetValue(tuple, out q_temp))
                            return null; //TODO: throw proper exception rather than returning null


                        q_new_list.Add(q_temp);
                        indices[i] = q_temp.id;
                    }
                    
                    //If not yet in there, add 'q_new' to 'Q':
                    State<List<State<S>>> q_new;
                    if (Q_reference_array.GetValue(indices) == null)
                    {
                        q_new = new State<List<State<S>>>(q_new_list);
                        Q_reference_array.SetValue(q_new, indices);
                        W.Add(q_new_list);
                    }
                    else
                    {
                        q_new = (State<List<State<S>>>) Q_reference_array.GetValue(indices);
                    }

                    //Add new transition to 'delta':
                    TwoTuple<State<List<State<S>>>, C> key = new TwoTuple<State<List<State<S>>>, C>(q, c);
                    delta.Add(key, q_new);
                }
            }

            //Determine id-array of initial state:
            int[] indices_q_0 = new int[noAutomata];
            for (int i = 0; i < noAutomata; i++)
                indices_q_0[i] = automataList[i].q_0.id;

            State<List<State<S>>> q_0 = (State<List<State<S>>>) Q_reference_array.GetValue(indices_q_0);

            //Return new DFA composed of calculated sets:
            return new DFA<C, List<State<S>>>(Q, Sigma, delta, q_0, F);
        }

        /*  METHOD THAT CONVERTS A POWERSET-AUTOMATON WITH SETS OF STATES AS LABELS FOR ITS STATES
        *   TO A DFA WITH STRINGS AS LABELS FOR ITS STATES
        *
        *   Output: New automaton with strings as labels for its states
        */
        public static DFA<C, string> RelabelStatesFromSetsToStrings<C>(DFA<C, Set<State<string>>> inDFA)
        {
            var Q_new = new HashSet<State<string>>();
            var delta_new = new Dictionary<TwoTuple<State<string>, C>, State<string>>();
            var F_new = new HashSet<State<string>>();

            var OldStateNewStateDict = new Dictionary<State<Set<State<string>>>, State<string>>();

            foreach (var q in inDFA.Q)
            {
                var state_array = q.label.content.ToArray();
                var id_array = new int[state_array.Length];
                for (int i = 0; i < state_array.Length; i++)
                {
                    id_array[i] = Int32.Parse(state_array[i].label);
                }
                Array.Sort(id_array);
                string new_label = "";
                for (int i = 0; i < id_array.Length - 1; i++)
                {
                    new_label += id_array[i] + ",";
                }
                new_label += id_array[id_array.Length - 1];
                var q_new = new State<string>(q.id, new_label);

                Q_new.Add(q_new);
                OldStateNewStateDict.Add(q, q_new);
            }

            foreach (var transition in inDFA.delta)
            {
                var p = transition.Key.first;
                var a = transition.Key.second;
                var q = transition.Value;

                State<string> p_new, q_new;
                if (!OldStateNewStateDict.TryGetValue(p, out p_new) | !OldStateNewStateDict.TryGetValue(q, out q_new)) { }

                delta_new.Add(new TwoTuple<State<string>, C>(p_new, a), q_new);
            }

            foreach (var q_f in inDFA.F)
            {
                State<string> q_f_new;
                if (!OldStateNewStateDict.TryGetValue(q_f, out q_f_new)) { }

                F_new.Add(q_f_new);
            }

            State<string> q_0_new;
            if (!OldStateNewStateDict.TryGetValue(inDFA.q_0, out q_0_new)) { }

            return new DFA<C, string>(Q_new, inDFA.Sigma, delta_new, q_0_new, F_new);
        }

        /*  METHOD THAT CONVERTS A PRODUCT-AUTOMATON WITH N-TUPLES OF STATES AS LABELS FOR ITS STATES
        *   TO A DFA WITH STRINGS AS LABELS FOR ITS STATES
        *
        *   Output: New automaton with strings as labels of its states
        */
        public static DFA<C, string> RelabelStatesFromNTuplesToStrings<C>(DFA<C, NTuple<State<string>>> inDFA)
        {
            var Q_new = new HashSet<State<string>>();
            var delta_new = new Dictionary<TwoTuple<State<string>, C>, State<string>>();
            var F_new = new HashSet<State<string>>();

            var OldStateNewStateDict = new Dictionary<State<NTuple<State<string>>>, State<string>>();

            foreach (var q in inDFA.Q)
            {
                var states = q.label.content;
                string new_label = "";
                for (int i = 0; i < states.Length - 1; i++)
                {
                    new_label += states[i].label + ",";
                }
                new_label += states[states.Length - 1].label;
                var q_new = new State<string>(q.id, new_label);

                Q_new.Add(q_new);
                OldStateNewStateDict.Add(q, q_new);
            }

            foreach (var transition in inDFA.delta)
            {
                var p = transition.Key.first;
                var a = transition.Key.second;
                var q = transition.Value;

                State<string> p_new, q_new;
                if (!OldStateNewStateDict.TryGetValue(p, out p_new) | !OldStateNewStateDict.TryGetValue(q, out q_new)) { }

                delta_new.Add(new TwoTuple<State<string>, C>(p_new, a), q_new);
            }

            foreach (var q_f in inDFA.F)
            {
                State<string> q_f_new;
                if (!OldStateNewStateDict.TryGetValue(q_f, out q_f_new)) { }

                F_new.Add(q_f_new);
            }

            State<string> q_0_new;
            if (!OldStateNewStateDict.TryGetValue(inDFA.q_0, out q_0_new)) { }

            return new DFA<C, string>(Q_new, inDFA.Sigma, delta_new, q_0_new, F_new);
        }

        /*  METHOD THAT CREATES A MINIMIZATION TABLE GIVEN A LANGUAGE PARTITION P
        */
        public static bool[] GetTableFromPartition<S>(HashSet<HashSet<State<S>>> P)
        {
            int n = 0;
            foreach (var M in P)
            {
                n += M.Count;
            }

            bool[] table = new bool[(n * n - n) / 2];
            for (int i = 0; i < table.Length; i++)
                table[i] = true;

            foreach (var M in P)
            {
                for (int i = 0; i < M.Count; i++)
                {
                    for (int j = i + 1; j < M.Count; j++)
                    {
                        int k = M.ElementAt(i).id;
                        int l = M.ElementAt(j).id;
                        table[(l * l - l) / 2 + k] = false;
                    }
                }
            }

            return null;
        }

        /*  METHOD THAT CREATES A NEW DFA-OBJECT BASED ON A XML_DESCRIPTION
        *   THE ALPHABET IS OVER CHARACTERS, THE STATES ARE LABELLED WITH INTEGERS
        *
        *   Output: The created DFA
        */
        public static DFA<char, string> ParseDFAFromXML(XElement automaton_wrapped)
        {

            var Q = new HashSet<State<string>>();
            var Sigma = new HashSet<char>();
            var delta = new Dictionary<TwoTuple<State<string>, char>, State<string>>();
            State<string> q_0 = null;
            var F = new HashSet<State<string>>();

            var IdStateDict = new Dictionary<int, State<string>>();

            XElement automaton = XElement.Parse(RemoveAllNamespaces(automaton_wrapped.ToString()));

            //Parse Q:
            XElement stateSet = automaton.Element("stateSet");
            foreach (XElement child in stateSet.Elements())
            {
                if (child.Name == "state")
                {
                    try
                    {
                        int id = Int32.Parse(child.Element("id").Value);
                        string label = child.Element("label").Value;
                        var q = new State<string>(id, label);
                        Q.Add(q);
                        IdStateDict.Add(id, q);
                    }
                    catch (NullReferenceException e)
                    {
                        int x = 0;
                    }
                }
            }

            //Parse Sigma:
            XElement alphabet = automaton.Element("alphabet");
            foreach (XElement child in alphabet.Elements())
            {
                if (child.Name == "symbol")
                {
                    Sigma.Add(Convert.ToChar(child.Value));
                }
            }

            //Parse delta:
            XElement transitionSet = automaton.Element("transitionSet");
            foreach (XElement child in transitionSet.Elements())
            {
                if (child.Name == "transition")
                {
                    int from_id = Int32.Parse(child.Element("from").Value);
                    int to_id = Int32.Parse(child.Element("to").Value);
                    char c = Convert.ToChar(child.Element("read").Value);

                    State<string> q1, q2;
                    if (!IdStateDict.TryGetValue(from_id, out q1) | !IdStateDict.TryGetValue(to_id, out q2))
                    {
                        //TODO: throw exception
                    }

                    delta.Add(new TwoTuple<State<string>, char>(q1, c), q2);
                }
            }

            //Parse q_0:
            XElement initState = automaton.Element("initState");
            foreach (XElement child in initState.Elements())
            {
                if (child.Name == "state")
                {
                    int id = (int) child.Attribute("sid");
                    if (!IdStateDict.TryGetValue(id, out q_0))
                    {
                        //TODO: Throw even more exceptions
                    }
                }
            }

            //Parse F:
            XElement acceptingSet = automaton.Element("acceptingSet");
            foreach (XElement child in acceptingSet.Elements())
            {
                if (child.Name == "state")
                {
                    State<string> q;
                    int id = (int)child.Attribute("sid");
                    if (!IdStateDict.TryGetValue(id, out q))
                    {
                        //TODO: Throw even more exceptions
                    }

                    F.Add(q);
                }
            }

            return new DFA<char, string>(Q, Sigma, delta, q_0, F);
        }

        //TODO: Epsilon-Transitions!
        public static NFA<char, string> ParseNFAFromXML(XElement automaton_wrapped)
        {
            var Q = new HashSet<State<string>>();
            var Sigma = new HashSet<char>();
            var delta = new Dictionary<TwoTuple<State<string>, char>, HashSet<State<string>>>();
            var Q_0 = new HashSet<State<string>>();
            var F = new HashSet<State<string>>();

            var IdStateDict = new Dictionary<int, State<string>>();

            XElement automaton = XElement.Parse(RemoveAllNamespaces(automaton_wrapped.ToString()));

            //Parse Q:
            XElement stateSet = automaton.Element("stateSet");
            foreach (XElement child in stateSet.Elements())
            {
                if (child.Name == "state")
                {
                    try
                    {
                        int id = Int32.Parse(child.Element("id").Value);
                        string label = child.Element("label").Value;
                        var q = new State<string>(id, label);
                        Q.Add(q);
                        IdStateDict.Add(id, q);
                    }
                    catch (NullReferenceException e)
                    {
                        int x = 0;
                    }
                }
            }

            //Parse Sigma:
            XElement alphabet = automaton.Element("alphabet");
            foreach (XElement child in alphabet.Elements())
            {
                if (child.Name == "symbol")
                {
                    Sigma.Add(Convert.ToChar(child.Value));
                }
            }

            //Parse delta:
            //Initialize all values of the delta-function as empty sets
            foreach (State<string> q in Q)
            {
                foreach (char c in Sigma)
                {
                    delta.Add(new TwoTuple<State<string>, char>(q, c), new HashSet<State<string>>());
                }
            }
            XElement transitionSet = automaton.Element("transitionSet");
            foreach (XElement child in transitionSet.Elements())
            {
                if (child.Name == "transition")
                {
                    int from_id = Int32.Parse(child.Element("from").Value);
                    int to_id = Int32.Parse(child.Element("to").Value);
                    char c = Convert.ToChar(child.Element("read").Value);

                    State<string> q1, q2; HashSet<State<string>> delta_q1_c;
                    if (!IdStateDict.TryGetValue(from_id, out q1)
                        | !IdStateDict.TryGetValue(to_id, out q2) 
                        | !delta.TryGetValue(new TwoTuple<State<string>, char>(q1, c), out delta_q1_c))
                    {
                        //TODO: throw exception
                    }

                    delta_q1_c.Add(q2);
                }
            }

            //Parse Q_0:
            XElement initState = automaton.Element("initState");
            foreach (XElement child in initState.Elements())
            {
                if (child.Name == "state")
                {
                    int id = (int)child.Attribute("sid");
                    State<string> q;
                    if (!IdStateDict.TryGetValue(id, out q))
                    {
                        //TODO: Throw even more exceptions
                    }
                    Q_0.Add(q);
                }
            }

            //Parse F:
            XElement acceptingSet = automaton.Element("acceptingSet");
            foreach (XElement child in acceptingSet.Elements())
            {
                if (child.Name == "state")
                {
                    State<string> q;
                    int id = (int)child.Attribute("sid");
                    if (!IdStateDict.TryGetValue(id, out q))
                    {
                        //TODO: Throw even more exceptions
                    }

                    F.Add(q);
                }
            }

            return new NFA<char, string>(Q, Sigma, delta, Q_0, F);
        }

        /*  METHOD THAT PARSES A MINIMIZATION TABLE FROM A GIVEN XML-FILE
        *
        */
        public static bool[] ParseMinimizationTableFromXML(XElement minimization_table_wrapped)
        {
            XElement minimization_table = XElement.Parse(RemoveAllNamespaces(minimization_table_wrapped.ToString()));

            int stateCount = Int32.Parse(minimization_table.Element("stateCount").Value);
            bool[] table = new bool[(stateCount * stateCount - stateCount) / 2];

            XElement entries = minimization_table.Element("entries");
            foreach (XElement child in entries.Elements())
            {
                if (child.Name == "entry")
                {
                    int i = Int32.Parse(minimization_table.Element("i").Value);
                    int j = Int32.Parse(minimization_table.Element("j").Value);
                    if (j < i)
                    {
                        int temp = i;
                        i = j;
                        j = temp;
                    }
                    if (i == j)
                    {
                        //TODO: throw exception
                    }
                    table[(j * j - j) / 2 + i] = true;
                }
            }

            return table;
        }

        public static string[] ParseMinimizationTableShortestWordsFromXML(XElement minimization_table_wrapped)
        {
            XElement minimization_table = XElement.Parse(RemoveAllNamespaces(minimization_table_wrapped.ToString()));

            int stateCount = Int32.Parse(minimization_table.Element("stateCount").Value);
            string[] table = new string[(stateCount * stateCount - stateCount) / 2];

            XElement entries = minimization_table.Element("entries");
            foreach (XElement child in entries.Elements())
            {
                if (child.Name == "entry")
                {
                    int i = Int32.Parse(child.Element("i").Value);
                    int j = Int32.Parse(child.Element("j").Value);
                    if (j < i)
                    {
                        int temp = i;
                        i = j;
                        j = temp;
                    }
                    if (i == j)
                    {
                        //TODO: throw exception
                    }
                    string s = child.Element("word").Value;
                    if (s.Equals("epsilon"))
                    {
                        table[(j * j - j) / 2 + i] = "";
                    }
                    else if (s.Equals(""))
                    {
                        table[(j * j - j) / 2 + i] = null;
                    }
                    else
                    {
                        table[(j * j - j) / 2 + i] = s;
                    }
                }
            }

            return table;
        }

        /*  METHOD THAT PARSES A LANGUAGE PARTITION FROM A GIVEN XML-FILE
        *
        *   
        */
        public static HashSet<Set<int>> ParsePartitionFromXML(XElement partition_wrapped)
        {
            XElement partition = XElement.Parse(RemoveAllNamespaces(partition_wrapped.ToString()));

            var P = new HashSet<Set<int>>();
            foreach (XElement set in partition.Elements())
            {
                if (set.Name == "set")
                {
                    var M = new Set<int>();
                    P.Add(M);
                    foreach (XElement state in set.Elements())
                    {
                        if (state.Name == "state")
                        {
                            int id = (int) state.Attribute("sid");
                            M.content.Add(id);
                        }
                    }
                }
            }

            return P;
        }

        /*  METHOD TO PARSE LIST OF AUTOMATA
        *
        *   Output: List of automata
        */
        public static List<DFA<char, string>> ParseDFAListFromXML(XElement automataList)
        {
            var outList = new List<DFA<char, string>>();
            foreach (XElement Automaton in automataList.Elements())
            {
                outList.Add(ParseDFAFromXML(Automaton.Elements().First()));
            }

            return outList;
        }

        #region XML parsing helpers
        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

            return xmlDocumentWithoutNs.ToString();
        }

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                {
                    if (!attribute.IsNamespaceDeclaration)
                    {
                        xElement.Add(attribute);
                    }
                }

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }
        #endregion
    }
}
