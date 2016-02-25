using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Microsoft.Automata;
using Microsoft.Z3;

using System.Diagnostics;

using System.Text.RegularExpressions;

namespace AutomataPDL
{
    public static class NFAUtilities
    {
        public static Pair<HashSet<char>, Automaton<BDD>> parseNFAFromXML(XElement Automaton1, CharSetSolver solver)
        {
            HashSet<char> al = new HashSet<char>();

            var moves = new List<Move<BDD>>();
            var finalStates = new List<int>();
            int start = 0;

            XElement Automaton = XElement.Parse(RemoveAllNamespaces(Automaton1.ToString()));
            XElement xmlAlphabet = Automaton.Element("alphabet");
            foreach (XElement child in xmlAlphabet.Elements())
            {
                char element = Convert.ToChar(child.Value);
                if (element != 'ε' && element != '?')
                    al.Add(element);
            }


            XElement trans = Automaton.Element("transitionSet");

            foreach (XElement child in trans.Elements())
            {
                if (child.Name == "transition")
                {
                    char element = Convert.ToChar(child.Element("read").Value);
                    if (element != 'ε' && element != '?')
                        moves.Add(new Move<BDD>(Convert.ToInt32(child.Element("from").Value), Convert.ToInt32(child.Element("to").Value),
                            solver.MkCharConstraint(false, element)));
                    else
                        moves.Add(Move<BDD>.Epsilon(Convert.ToInt32(child.Element("from").Value), Convert.ToInt32(child.Element("to").Value)));

                    
                }
            }

            XElement acc = Automaton.Element("acceptingSet");
            foreach (XElement child in acc.Elements())
            {
                if (child.Name == "state")
                {
                    finalStates.Add((int)child.Attribute("sid"));
                }
            }

            XElement states = Automaton.Element("initState");
            foreach (XElement child in states.Elements())
            {
                if (child.Name == "state")
                {
                    start = (int)child.Attribute("sid");
                }
            }

            return new Pair<HashSet<char>, Automaton<BDD>>(al, Automaton<BDD>.Create(start, finalStates, moves));

        }

        public static Automaton<BDD> parseForTest(string Automaton1, CharSetSolver solver)
        {           
            var moves = new List<Move<BDD>>();
            var finalStates = new List<int>();
            int start = 0;

            XElement Automaton = XElement.Parse(Automaton1);

            XElement trans = Automaton.Element("transitionSet");

            foreach (XElement child in trans.Elements())
            {
                if (child.Name == "transition")
                {
                    moves.Add(new Move<BDD>(Convert.ToInt32(child.Element("from").Value), Convert.ToInt32(child.Element("to").Value),
                        solver.MkCharConstraint(false, Convert.ToChar(child.Element("read").Value))));
                }
            }

            XElement acc = Automaton.Element("acceptingSet");
            foreach (XElement child in acc.Elements())
            {
                if (child.Name == "state")
                {
                    finalStates.Add((int)child.Attribute("sid"));
                }
            }

            XElement states = Automaton.Element("initState");
            foreach (XElement child in states.Elements())
            {
                if (child.Name == "state")
                {
                    start = (int)child.Attribute("sid");
                }
            }

            return Automaton<BDD>.Create(start, finalStates, moves);

        }

        public static Pair<HashSet<char>, Automaton<BDD>> parseRegexFromXML(XElement regex, XElement alphabet, CharSetSolver solver)
        {
            HashSet<char> al = new HashSet<char>();
            XElement xmlAlphabet = XElement.Parse(RemoveAllNamespaces(alphabet.ToString()));

            string alRex = "";
            bool first = true;
            foreach (XElement child in xmlAlphabet.Elements())
            {
                char element = Convert.ToChar(child.Value);
                al.Add(element);
                if (first)
                {
                    first = false;
                    alRex += element;
                }
                else
                {
                    alRex += "|"+element;
                }
            }

            XElement Regex = XElement.Parse(RemoveAllNamespaces(regex.ToString()));

            string rexpr = Regex.Value.Trim();

            var escapedRexpr = string.Format(@"^({0})$",rexpr);
            Automaton<BDD> aut = null;
            try
            {
                aut = solver.Convert(escapedRexpr);
            }
            catch (ArgumentException e)
            {
                throw new PDLException("The input is not a well formatted regular expression: "+e.Message);
            }
            catch (AutomataException e)
            {
                throw new PDLException("The input is not a well formatted regular expression: " + e.Message);
            }



            var diff = aut.Intersect(solver.Convert(@"^("+alRex+@")*$").Complement(solver), solver);
            if(!diff.IsEmpty)
                throw new PDLException(
                    "The regular expression should only accept strings over ("+alRex+")*. Yours accepts the string '"+DFAUtilities.GenerateShortTerm(diff.Determinize(solver),solver)+"'");

            return new Pair<HashSet<char>, Automaton<BDD>>(al, aut);

        }

        public static Pair<HashSet<char>, Automaton<BDD>> parseDFAFromJFLAP(string fileName, CharSetSolver solver)
        {
            
            HashSet<char> al = new HashSet<char>();
            XElement Structure = XElement.Load(fileName);

            XElement MType = Structure.Element("type");
            Debug.Assert(MType.Value == "fa");

            XElement Automaton = Structure.Element("automaton");

            var moves = new List<Move<BDD>>();
            var finalStates = new List<int>();
            int start = -1;

            foreach (XElement child in Automaton.Elements())
            {
                if (child.Name == "state") // make start and/or add to final
                {
                    foreach (XElement d in child.Elements())
                    {
                        if (d.Name == "initial")
                            start = (int)child.Attribute("id");
                        if (d.Name == "final")
                            finalStates.Add((int)child.Attribute("id"));
                    }
                            
                }
                if (child.Name == "transition")
                {
                    al.Add(Convert.ToChar(child.Element("read").Value));
                    moves.Add(new Move<BDD>(Convert.ToInt32(child.Element("from").Value), Convert.ToInt32(child.Element("to").Value),
                        solver.MkCharConstraint(false, Convert.ToChar(child.Element("read").Value))));
                }
            }

            Debug.Assert(start != -1);
            return new Pair<HashSet<char>, Automaton<BDD>>(al, Automaton<BDD>.Create(start, finalStates, moves));
        }

        public static bool IsEventEqual(string file1, string file2)
        {
            XElement Event1 = XElement.Load(file1);
            XElement Automaton1 = Event1.Element("automaton");

            XElement Event2 = XElement.Load(file2);
            XElement Automaton2 = Event2.Element("automaton");

            return Automaton1.ToString() == Automaton2.ToString();
        }

        public static void printDFA(Automaton<BDD> dfa, HashSet<char> alphabet, StringBuilder sb)
        {
            var newDfa = normalizeDFA(dfa).First;

            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            sb.Append("alphabet:");
            foreach (var ch in alphabet)
                sb.Append(" " + ch);
            sb.AppendLine();

            sb.AppendLine(string.Format("{0} states", newDfa.StateCount));

            sb.Append("final states:");
            foreach (var st in newDfa.GetFinalStates())
                sb.Append(" " + st);
            sb.AppendLine();
            
            foreach (var move in newDfa.GetMoves())
            {
                var chars = solver.GenerateAllCharacters(move.Label, false).ToList();
                chars.Sort();
                foreach (var ch in chars)
                {
                    sb.AppendLine(string.Format("{0},{1},{2}", move.SourceState, move.TargetState, ch));
                }
            }
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

        #region canonical state names
        public static Pair<Automaton<BDD>,Dictionary<int,int>> normalizeDFA(Automaton<BDD> dfa)
        {
            Dictionary<int, int> stateToNewNames = new Dictionary<int, int>();
            Dictionary<int, int> newNamesToStates = new Dictionary<int, int>();
            int i = 0;
            foreach (var state in dfsStartingTimes(dfa))
            {
                newNamesToStates[i] = state;
                stateToNewNames[state] = i;
                i++;
            }

            var oldFinalStates = dfa.GetFinalStates();
            var newFinalStates = new List<int>();
            foreach (var st in oldFinalStates)
                newFinalStates.Add(stateToNewNames[st]);

            var oldMoves = dfa.GetMoves();
            var newMoves = new List<Move<BDD>>();
            foreach (var move in oldMoves)
                newMoves.Add(new Move<BDD>(stateToNewNames[move.SourceState], stateToNewNames[move.TargetState], move.Label));

            return new Pair<Automaton<BDD>, Dictionary<int, int>>(Automaton<BDD>.Create(0, newFinalStates, newMoves), newNamesToStates);
        }

        private static List<int> dfsStartingTimes(Automaton<BDD> dfa)
        {
            List<int> order = new List<int>();

            HashSet<int> discovered = new HashSet<int>();
            discovered.Add(dfa.InitialState);

            dfsRecStartingTimes(dfa, dfa.InitialState, discovered, order);

            //Deal with states not reachable from init
            foreach (var state in dfa.States)
                if (!discovered.Contains(state))
                {
                    discovered.Add(state);
                    dfsRecStartingTimes(dfa, state, discovered, order);
                }

            return order;
        }

        private static void dfsRecStartingTimes(Automaton<BDD> dfa, int currState, HashSet<int> discovered, List<int> order)
        {
            order.Add(currState);
            List<Move<BDD>> moves = new List<Move<BDD>>(dfa.GetMovesFrom(currState));
            moves.Sort(delegate(Move<BDD> c1, Move<BDD> c2) {
                return c1.Label.ToString().CompareTo(c2.Label.ToString());
            });
            foreach (var move in moves)
                if (!discovered.Contains(move.TargetState))
                {
                    discovered.Add(move.TargetState);
                    dfsRecStartingTimes(dfa, move.TargetState, discovered, order);
                }
        }
        #endregion

        #region TestSet Generation with Myhill-Nerode
        //returns a pair of string enumerable of positive and negative test set respectively
        internal static Pair<IEnumerable<string>, IEnumerable<string>> MyHillTestGeneration(HashSet<char> alphabet, Automaton<BDD> dfa, CharSetSolver solver)
        {
            Automaton<BDD> normDfa = normalizeDFA(dfa.Determinize(solver).Minimize(solver)).First;                       

            HashSet<string> pos = new HashSet<string>();
            HashSet<string> neg = new HashSet<string>();

            Automaton<BDD> ait, bif, bjf, adif;
            HashSet<string> testSet = new HashSet<string>();
            var finStates = normDfa.GetFinalStates();

            string[] a = new string[normDfa.StateCount];
            string[,] b = new string[normDfa.StateCount, normDfa.StateCount];

            #region Compute ai and bij
            foreach (var state1 in normDfa.States)
            {
                ait = Automaton<BDD>.Create(normDfa.InitialState, new int[] { state1 }, normDfa.GetMoves());
                a[state1] = GenerateShortTerm(ait, solver);

                bif = Automaton<BDD>.Create(state1, finStates, normDfa.GetMoves());

                foreach (var state2 in normDfa.States)
                {
                    bjf = Automaton<BDD>.Create(state2, finStates, new List<Move<BDD>>(normDfa.GetMoves()));

                    adif = bif.Minus(bjf, solver).Determinize(solver).Minimize(solver);

                    b[state1, state2] = GenerateShortTerm(adif, solver);
                }
            }
            #endregion

            for (int i = 0; i < normDfa.StateCount; i++)
                for (int j = 0; j < normDfa.StateCount; j++)
                {
                    if (b[i, j] != null)
                        pos.Add(a[i] + b[i, j]);
                    if (b[j, i] != null)
                        neg.Add(a[i] + b[j, i]);
                    foreach (char c in alphabet)
                    {
                        int new_i = GetNextState(i, c, normDfa, solver);
                        if (new_i!=-1 && b[new_i, j] != null)
                            pos.Add(a[i] + c + b[new_i, j]);
                        if (new_i != -1 && b[j, new_i] != null)
                            neg.Add(a[i] + c + b[j, new_i]);
                    }
                }

            return new Pair<IEnumerable<string>, IEnumerable<string>>(pos, neg);
        }

        // returns the state reached from currState when reading c
        private static int GetNextState(int currState, char c, Automaton<BDD> dfa, CharSetSolver solver)
        {
            foreach (var move in dfa.GetMovesFrom(currState))
                if (solver.IsSatisfiable(solver.MkAnd(move.Label, solver.MkCharConstraint(false, c))))
                    return move.TargetState;

            return -1;
        }

        internal static string GenerateShortTerm(Automaton<BDD> dfa, CharSetSolver solver)
        {
            if (dfa.IsEmpty)
                return null;

            Dictionary<int, string> shortStr = new Dictionary<int, string>();

            HashSet<int> reachedStates = new HashSet<int>();
            List<int> toExplore = new List<int>();


            reachedStates.Add(dfa.InitialState);
            toExplore.Add(dfa.InitialState);
            shortStr.Add(dfa.InitialState, "");
            var finSts = dfa.GetFinalStates();
            if (finSts.Contains(dfa.InitialState))
                return "";

            string sCurr = ""; char condC = 'a';
            while (toExplore.Count != 0)
            {
                var current = toExplore.First();
                toExplore.RemoveAt(0);
                shortStr.TryGetValue(current, out sCurr);

                var reachableFromCurr = dfa.GetMovesFrom(current);
                foreach (var move in reachableFromCurr)
                {
                    if (!reachedStates.Contains(move.TargetState))
                    {
                        reachedStates.Add(move.TargetState);
                        toExplore.Add(move.TargetState);

                        foreach (var v in solver.GenerateAllCharacters(move.Label, false))
                        {
                            condC = v;
                            break;
                        }
                        shortStr.Add(move.TargetState, sCurr + condC);
                        if (finSts.Contains(move.TargetState))
                        {
                            return sCurr + condC;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TestSet Generation with enumeration

        public static Pair<List<string>, List<string>> GetTestSets(
            Automaton<BDD> dfa, HashSet<char> alphabet, CharSetSolver solver)
        {
            List<string> positive = new List<string>();
            List<string> negative = new List<string>();
            var finalStates = dfa.GetFinalStates().ToList();

            ComputeModels("", dfa.InitialState, dfa, finalStates, alphabet, solver, positive, negative);

            positive.Sort();
            negative.Sort();
            return new Pair<List<string>, List<string>>(positive, negative);
        }

        internal static void ComputeModels(
            string currStr, int currState,
            Automaton<BDD> dfa, List<int> finalStates, HashSet<char> alphabet, CharSetSolver solver,
            List<string> positive, List<string> negative)
        {
            if (currStr.Length >= 8)
                return;

            if (currState == -1 || !finalStates.Contains(currState))
                negative.Add(currStr);
            else
                positive.Add(currStr);

            foreach (char ch in alphabet)
            {
                if (currState == -1)
                    ComputeModels(currStr + ch, currState, dfa, finalStates, alphabet, solver, positive, negative);
                else
                {
                    bool found = false;
                    foreach (var move in dfa.GetMovesFrom(currState))
                    {
                        if (solver.IsSatisfiable(solver.MkAnd(move.Label, solver.MkCharConstraint(false, ch))))
                        {
                            found = true;
                            ComputeModels(currStr + ch, move.TargetState, dfa, finalStates, alphabet, solver, positive, negative);
                            break;
                        }
                    }
                    if (!found)
                        ComputeModels(currStr + ch, -1, dfa, finalStates, alphabet, solver, positive, negative);
                }
            }
        }
        #endregion


        //Compute strings in cycles
        #region Compute strings in cycles
        internal static HashSet<string> getLoopingStrings(Automaton<BDD> dfa, HashSet<Char> al, CharSetSolver solver)
        {
            var cycles = getSimpleCycles(dfa);
            HashSet<string> strings = new HashSet<string>();
            foreach (var cycle in cycles)
            {
                var state = cycle.ElementAt(0);
                cycle.RemoveAt(0);
                getPathStrings(dfa, solver, cycle, "", strings, state);
            }
            return strings;
        }

        private static void getPathStrings(Automaton<BDD> dfa, CharSetSolver solver, List<int> path, string currStr, HashSet<string> strings, int prevState)
        {
            List<int> path1 = new List<int>(path);
            var currState = path1.ElementAt(0);
            path1.RemoveAt(0);
            
            foreach (var move in dfa.GetMovesFrom(prevState))
                if (move.TargetState == currState)
                    foreach(char c in solver.GenerateAllCharacters(move.Label,false))
                        if (path1.Count == 0)
                            strings.Add(currStr + c);
                        else                        
                            getPathStrings(dfa, solver, path1, currStr + c, strings, currState);                        
                    
        }
        #endregion

        // Accessory methods for SCC and cycles
        #region Accessory methods for SCC and cycles
        internal static HashSet<int> getCyclesLengths(Automaton<BDD> dfa)
        {
            HashSet<int> lengths = new HashSet<int>();
            var sccs = computeSCC(dfa);
            foreach (var scc in sccs)
            {
                HashSet<int>[] dic = new HashSet<int>[scc.Count + 1];

                foreach (var state in scc)
                {
                    for (int i = 1; i <= scc.Count; i++)
                        dic[i] = new HashSet<int>();
                    getCyclesLengthsFromNode(1, dfa, state, dic, scc.Count);
                    for (int i = 1; i <= scc.Count; i++)
                        if (dic[i].Contains(state))
                            lengths.Add(i);
                }
            }
            return lengths;
        }

        private static void getCyclesLengthsFromNode(int length,
            Automaton<BDD> dfa, int currState, HashSet<int>[] found, int max)
        {
            if (length <= max)
                foreach (var move in dfa.GetMovesFrom(currState))
                {
                    if (!found[length].Contains(move.TargetState))
                    {
                        found[length].Add(move.TargetState);
                        getCyclesLengthsFromNode(length + 1, dfa, move.TargetState, found, max);
                    }
                }
        }

        private static List<int> dfsFinishingTimes(Automaton<BDD> dfa)
        {
            List<int> order = new List<int>();

            HashSet<int> discovered = new HashSet<int>();
            discovered.Add(dfa.InitialState);

            dfsRecFinishingTimes(dfa, dfa.InitialState, discovered, order);
            //Deal with states not reachable from init
            foreach (var state in dfa.States)
                if (!discovered.Contains(state))
                {
                    discovered.Add(state);
                    dfsRecFinishingTimes(dfa, state, discovered, order);
                }

            return order;
        }

        private static void dfsRecFinishingTimes(Automaton<BDD> dfa, int currState, HashSet<int> discovered, List<int> order)
        {
            foreach (var move in dfa.GetMovesFrom(currState))
            {
                if (!discovered.Contains(move.TargetState))
                {
                    discovered.Add(move.TargetState);
                    dfsRecFinishingTimes(dfa, move.TargetState, discovered, order);
                }
            }
            order.Insert(0, currState);
        }


        internal static List<HashSet<int>> computeSCC(Automaton<BDD> dfa)
        {
            List<int> order = dfsFinishingTimes(dfa);

            var list = new List<HashSet<int>>();

            HashSet<int> discovered = new HashSet<int>();
            HashSet<int> comp = new HashSet<int>();

            while (order.Count > 0)
            {
                discovered.Add(order.ElementAt(0));
                backwardDfsRec(dfa, order.ElementAt(0), discovered, comp);
                list.Add(comp);
                foreach (var item in comp)
                    order.Remove(item);
                comp = new HashSet<int>();
            }

            return list;
        }

        private static void backwardDfsRec(Automaton<BDD> dfa, int currState, HashSet<int> discovered, HashSet<int> comp)
        {
            foreach (var move in dfa.GetMovesTo(currState))
            {
                if (!discovered.Contains(move.SourceState))
                {
                    discovered.Add(move.SourceState);
                    backwardDfsRec(dfa, move.SourceState, discovered, comp);
                }
            }
            comp.Add(currState);
        }

        #endregion

        // Accessory methods for strings
        #region Accessory methods for strings
        internal static List<string> getSimplePrefixes(Automaton<BDD> dfa, CharSetSolver solver)
        {
            List<string> strings = new List<string>();
            foreach (var path in getSimplePaths(dfa))
            {
                var currStrs = new List<string>();
                currStrs.Add("");
                var p = new List<int>(path);
                p.RemoveAt(0);
                int prevNode = dfa.InitialState;
                foreach (int node in p)
                {
                    foreach (var move in dfa.GetMovesFrom(prevNode))
                    {
                        if (node == move.TargetState)
                        {
                            var newStrs = new List<string>();
                            foreach (var el in solver.GenerateAllCharacters(move.Label, false))
                                foreach (var str in currStrs)
                                {
                                    newStrs.Add(str + el);
                                    strings.Add(str + el);
                                }
                            currStrs = new List<string>(newStrs);
                            break;
                        }
                    }
                    prevNode = node;
                }

            }
            return strings;
        }

        internal static List<string> getSimpleSuffixes(Automaton<BDD> dfa, CharSetSolver solver)
        {
            List<string> strings = new List<string>();
            foreach (var path in getSimplePaths(dfa))
            {
                var currStrs = new List<string>();
                currStrs.Add("");
                var p = new List<int>(path);
                p.Reverse();
                int prevNode = p.ElementAt(0);
                p.RemoveAt(0);

                foreach (int node in p)
                {
                    foreach (var move in dfa.GetMovesTo(prevNode))
                    {
                        if (node == move.SourceState)
                        {
                            var newStrs = new List<string>();
                            foreach (var el in solver.GenerateAllCharacters(move.Label, false))
                                foreach (var str in currStrs)
                                {
                                    newStrs.Add(el + str);
                                    strings.Add(el + str);
                                }
                            currStrs = new List<string>(newStrs);
                            break;
                        }
                    }
                    prevNode = node;
                }

            }
            return strings;

        }

        internal static List<List<int>> getSimplePaths(Automaton<BDD> dfa)
        {
            List<List<int>> paths = new List<List<int>>();
            var cp = new List<int>();
            cp.Add(dfa.InitialState);
            getSimplePathsDFS(dfa.InitialState, dfa, paths, cp);
            return paths;
        }

        internal static void getSimplePathsDFS(int currState, Automaton<BDD> dfa, List<List<int>> paths, List<int> currPath)
        {
            foreach (var move in dfa.GetMovesFrom(currState))
            {
                if (!currPath.Contains(move.TargetState))
                {
                    var npath = new List<int>(currPath);
                    npath.Add(move.TargetState);
                    if (dfa.GetFinalStates().Contains(move.TargetState))
                        paths.Add(npath);
                    getSimplePathsDFS(move.TargetState, dfa, paths, npath);
                }
            }

        }

        internal static List<List<int>> getSimpleCycles(Automaton<BDD> dfa)
        {
            List<List<int>> cycles = new List<List<int>>();
            foreach (var state in dfa.States)
            {
                var cp = new List<int>();
                cp.Add(state);
                getSimpleCyclesDFS(state, dfa, cycles, cp);
            }
            return cycles;
        }

        internal static void getSimpleCyclesDFS(int currState, Automaton<BDD> dfa, List<List<int>> cycles, List<int> currPath)
        {
            foreach (var move in dfa.GetMovesFrom(currState))
            {
                if (!currPath.Contains(move.TargetState))
                {
                    var npath = new List<int>(currPath);
                    npath.Add(move.TargetState);
                    getSimpleCyclesDFS(move.TargetState, dfa, cycles, npath);
                }
                else
                {
                    if (currPath.ElementAt(0) == move.TargetState)
                    {
                        var npath = new List<int>(currPath);
                        npath.Add(move.TargetState);
                        cycles.Add(npath);
                    }
                }
            }

        }
        #endregion        

        // Return true iff dfa2 behaves correctly on all the inputs the pair (pos,neg)
        // To be used only when testing again same dfa1 over and over
        internal static bool ApproximateMNEquivalent(
            Pair<IEnumerable<string>, IEnumerable<string>> testSets, 
            double lanDensity,
            Automaton<BDD> dfa, HashSet<char> al, CharSetSolver solver)
        {
            //Check against test cases
            var positive = testSets.First;
            var negative = testSets.Second;

            if (lanDensity < 0.5)
            {
                foreach (var s in positive)
                    if (!Accepts(dfa, s, al, solver))
                        return false;

                foreach (var s in negative)
                    if (Accepts(dfa, s, al, solver))
                        return false;
            }
            else
            {
                foreach (var s in negative)
                    if (Accepts(dfa, s, al, solver))
                        return false;

                foreach (var s in positive)
                    if (!Accepts(dfa, s, al, solver))
                        return false;
            }

            return true;
        }

        //returns true iff dfa accepts str
        private static bool Accepts(Automaton<BDD> dfa1,  string str, HashSet<char> al, CharSetSolver solver)
        {
            int currState = 0;
            for (int i = 0; i < str.Length; i++)
            {
                currState = GetNextState(currState, str[i], dfa1, solver);
                if (currState < 0)
                    return false;
            }
            return dfa1.GetFinalStates().ToList().Contains(currState);
        }

        /// <summary>
        /// Returns an automaton where transitions between states are collapsed to a single one
        /// </summary>
        /// <param name="automaton"></param>
        /// <param name="solver"></param>
        /// <returns></returns>
        public static Automaton<BDD> normalizeMoves(Automaton<BDD> automaton, CharSetSolver solver)
        {
            List<Move<BDD>> normMoves = new List<Move<BDD>>();
            foreach (var sourceState in automaton.States)
            {
                Dictionary<int, BDD> moveConditions = new Dictionary<int, BDD>();
                foreach (var moveFromSourceState in automaton.GetMovesFrom(sourceState))
                {
                    var target = moveFromSourceState.TargetState;
                    BDD oldCondition = null;
                    if (moveConditions.ContainsKey(target))                    
                        oldCondition = moveConditions[target];                    
                    else
                        oldCondition = solver.False;

                    if (!moveFromSourceState.IsEpsilon)
                        moveConditions[target] = solver.MkOr(oldCondition, moveFromSourceState.Label);
                    else
                        normMoves.Add(moveFromSourceState);
                }

                foreach (var targetState in moveConditions.Keys)             
                    normMoves.Add(new Move<BDD>(sourceState,targetState,moveConditions[targetState]));                
            }

            return Automaton<BDD>.Create(automaton.InitialState, automaton.GetFinalStates(), normMoves);
        }

        public static bool canCollapseStates(Automaton<BDD> nfa, int state1, int state2, CharSetSolver solver, Pair<IEnumerable<string>, IEnumerable<string>> tests, HashSet<char> al)
        {
            
            var density =  DFADensity.GetDFADensity(nfa, al, solver);
 
            // collapses state2 to state1
            List<Move<BDD>> newMoves = new List<Move<BDD>>();

            foreach (var move in nfa.GetMoves())
            {
                var newSource = move.SourceState;
                var newTarget = move.TargetState;

                if (newSource == state2)
                    newSource = state1;
                if (newTarget == state2)
                    newTarget = state1;
                
                newMoves.Add(new Move<BDD>(newSource, newTarget, move.Label));
            }

            // replace state2 with state1 if initial state
            // no need to remove state2 from final state list, as it is unreachable
            int newInitialState = nfa.InitialState;
            if (nfa.InitialState == state2)
                newInitialState = state1;

            //makes new Nfa and returns collapse state edit if are equiv
            var newNfa = Automaton<BDD>.Create(newInitialState, nfa.GetFinalStates(), newMoves);
            
            if(DFAUtilities.ApproximateMNEquivalent(tests, density, newNfa, al, solver)) 
                return nfa.IsEquivalentWith(newNfa, solver);
            return false;
        }

        public static bool canRemoveEdge(Automaton<BDD> nfa, int sourceState, int targetState, CharSetSolver solver, Pair<IEnumerable<string>, IEnumerable<string>> tests, HashSet<char> al)
        {
            List<Move<BDD>> newMoves = new List<Move<BDD>>();
            newMoves = nfa.GetMoves().ToList();
            bool moveExists = false;

            //goes through each move from state1 and remove it if it goes to state2 resp.
            foreach (var move in nfa.GetMovesFrom(sourceState))
                if (move.TargetState == targetState)
                {
                    newMoves.Remove(move);
                    moveExists = true;
                }

            var newNfa = Automaton<BDD>.Create(nfa.InitialState, nfa.GetFinalStates(), newMoves);

            if(moveExists)
                if (DFAUtilities.ApproximateMNEquivalent(tests, 0.5, newNfa, al, solver))
                    return nfa.IsEquivalentWith(newNfa, solver);
            return false;
        }

        public static bool canRemoveState(Automaton<BDD> nfa, int state, CharSetSolver solver, Pair<IEnumerable<string>, IEnumerable<string>> tests, HashSet<char> al)
        {
            var newNfa = Automaton<BDD>.Create(nfa.InitialState, nfa.GetFinalStates(), nfa.GetMoves());
            newNfa.RemoveTheState(state);

            if (DFAUtilities.ApproximateMNEquivalent(tests, 0.5, newNfa, al, solver))
                return nfa.IsEquivalentWith(newNfa, solver);
            return false;
        }
    }
}
