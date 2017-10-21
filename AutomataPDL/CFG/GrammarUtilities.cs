using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Automata;

namespace AutomataPDL.CFG
{
    public static class GrammarUtilities
    {
        /// <summary>
        /// Genereates warnings for useless variables.
        /// </summary>
        /// <param name="g">the grammar</param>
        /// <returns></returns>
        public static List<string> getGrammarWarnings(ContextFreeGrammar g)
        {
            List<string> res = new List<string>();
            HashSet<string> variables = new HashSet<string>();
            foreach (var n in g.Variables) variables.Add(n.ToString());

            var productiv = g.GetUsefulNonterminals(true);
            var unproductiv = variables.Except(productiv);
            if (unproductiv.Count() > 0) res.Add(string.Format("Warning: There are unproductive variables! ({0})", string.Join(", ",unproductiv)));

            var reachable = new HashSet<string>();
            //Lemma 4.2, p. 89, Hopcroft-Ullman
            Stack<Nonterminal> stack = new Stack<Nonterminal>();
            stack.Push(g.StartSymbol);
            reachable.Add(g.StartSymbol.ToString());
            while (stack.Count > 0)
            {
                Nonterminal v = stack.Pop();
                foreach (Production p in g.GetProductions(v))
                    foreach (Nonterminal u in p.GetVariables())
                        if (!reachable.Contains(u.ToString()))
                        {
                            reachable.Add(u.ToString());
                            stack.Push(u);
                        }
            }
            var unreachable = variables.Except(reachable);
            if (unproductiv.Count() > 0) res.Add(string.Format("Warning: There are unreachable variables! ({0})", string.Join(", ", unreachable)));
            
            return res;
        }

        /// <summary>
        /// Generates a CNF for a given grammar or returns null if the Grammar doesn't produce any words.
        /// </summary>
        /// <param name="g">the original grammar</param>
        /// <returns>the CNF or null</returns>
        public static ContextFreeGrammar getEquivalentCNF(ContextFreeGrammar g)
        {
            if (g == null) return null;
            if (g.IsInCNF()) return g;
            
            try
            {
                ContextFreeGrammar res = ContextFreeGrammar.MkCNF(g);

                //handle empty string
                res.setAcceptanceForEmptyString( g.acceptsEmptyString() );
                
                return res;
            }
            catch (AutomataException e)
            {
                if (g.acceptsEmptyString())
                {
                    var res = new ContextFreeGrammar(new Nonterminal("S"), new Production[] { new Production(new Nonterminal("S"), new GrammarSymbol[] {new Nonterminal("S") , new Nonterminal("S") }) });
                    res.setAcceptanceForEmptyString(true);
                    return res;
                }
                return null;
            }
        }

        /// <summary>
        /// Generates a CNF that accepts the prefix closure of a given grammar.
        /// </summary>
        /// <param name="g">the original grammar</param>
        /// <returns>the prefix closure</returns>
        public static ContextFreeGrammar getCNFPrefixClosure(ContextFreeGrammar g)
        {
            if (g == null) return g;
            if (!g.IsInCNF()) g = getEquivalentCNF(g);
            if (g == null) return g;

            var prefixClosure = getPrefixClosure(g);
            prefixClosure = getEquivalentCNF(prefixClosure); // !!ATTENTION!! this may remove old productions

            var productions = g.GetProductions();
            productions = productions.Concat(prefixClosure.GetProductions());

            return new ContextFreeGrammar(prefixClosure.StartSymbol, productions);
        }

        /// <summary>
        /// Generates a CFG that accepts the prefix closure of a given grammar.
        /// </summary>
        /// <param name="g">the original grammar</param>
        /// <returns>the prefix closure</returns>
        public static ContextFreeGrammar getPrefixClosure(ContextFreeGrammar g)
        {
            Func<Nonterminal, Nonterminal> prefixFor = delegate (Nonterminal x)
            {
                return new Nonterminal(x.Name + "PREFIX");
            };

            if (g == null) return g;
            if (!g.IsInCNF()) g = getEquivalentCNF(g);
            if (g == null) return g;
            Nonterminal prefixStart = prefixFor(g.StartSymbol);
            var prefixProductions = new List<Production>();

            foreach (Production p in g.GetProductions())
            {
                //add original
                prefixProductions.Add(p);
                
                Nonterminal prefixNT = prefixFor(p.Lhs);
                if (p.Rhs.Length == 2) // case:  X->AB      ==>     X' ->A' | AB'
                {
                    prefixProductions.Add(new Production(prefixNT, new GrammarSymbol[] { p.Rhs[0], prefixFor((Nonterminal)p.Rhs[1]) }));
                    prefixProductions.Add(new Production(prefixNT, new GrammarSymbol[] { prefixFor((Nonterminal)p.Rhs[0]) }));
                }
                else // case:  X->a   ==>    X'->a
                {
                    prefixProductions.Add(new Production(prefixNT, new GrammarSymbol[] { p.Rhs[0]}));
                }
            }

            var res = new ContextFreeGrammar(prefixStart, prefixProductions);
            res.setAcceptanceForEmptyString(true);

            return res;
        }

        /// <summary>
        /// Checks if a word is recognized by the given grammar. (CYK-algorithm)
        /// </summary>
        /// <param name="grammar">the grammar</param>
        /// <param name="word">the word</param>
        /// <returns>true, if there exists a dereviation from the startsymbol to the word</returns>
        public static bool isWordInGrammar(ContextFreeGrammar grammar, string word)
        {
            if (word == null || grammar == null) return false;
            if (!grammar.IsInCNF()) grammar = getEquivalentCNF(grammar);
            if (grammar == null) return false;

            //empty word
            if (word.Length == 0) return grammar.acceptsEmptyString();

            //CYK
            var cyk_table = cyk(grammar, word);

            return cyk_table[word.Length - 1][0].Item1.Contains(grammar.StartSymbol);
        }

        /// <summary>
        /// Finds the longest prefix of a given word that is still recognized by a given grammar. (CYK algorithm with prefix closure)
        /// </summary>
        /// <param name="grammar">the grammar</param>
        /// <param name="word">the word</param>
        /// <returns>-1 if the grammar is empty; -2 if the word is in the grammar; n (if the substring up to index n is the longest prefix)</returns>
        public static int longestPrefixLength(ContextFreeGrammar grammar, string word)
        {
            if (word == null || grammar == null) return -1;
            if (!grammar.IsInCNF()) grammar = getEquivalentCNF(grammar);
            if (grammar == null) return -1;

            //empty word
            if (word.Length == 0)
            {
                if (grammar.acceptsEmptyString()) return -2;
                return 0;
            }

            //prefix closure
            var prefixGrammar = getCNFPrefixClosure(grammar);

            //CYK
            var cyk_table = cyk(prefixGrammar, word);

            //check if word was in original grammar
            if (cyk_table[word.Length - 1][0].Item1.Contains(grammar.StartSymbol)) return -2;

            //check for startsymbol in first row
            for (int i = word.Length - 1; i >= 0; i--)
            {
                if (cyk_table[i][0].Item1.Contains(prefixGrammar.StartSymbol)) return i+1;
            }
            return 0;
        }

        /// <summary>
        /// Performs the CYK-algorithm
        /// </summary>
        /// <param name="grammar">the grammar (in CNF)</param>
        /// <param name="word">the word (not null)</param>
        /// <returns>the filled table of the cyk-algorithm</returns>
        public static Tuple<HashSet<Nonterminal>, List<Tuple<Production, int>>>[][] cyk(ContextFreeGrammar grammar, string word)
        {
            /*
             * Every entry in the table consists of 2 parts:
             *      1. The HasSet of all Nonterminals that produce the corresponding subword
             *      2. All possible subtrees encodes as pair (p,x) 
             *          where p is the applicable production and 
             *          x is the lengt of the word produced by the first grammarsymbol on the right hand side of p
             */

            //prepare CYK table
            int n = word.Length;
            Tuple<HashSet<Nonterminal>, List<Tuple<Production, int>>>[][] cyk = new Tuple<HashSet<Nonterminal>, List<Tuple<Production, int>>>[n][];
            for (int i = 0; i < n; i++)
            {
                cyk[i] = new Tuple<HashSet<Nonterminal>, List<Tuple<Production, int>>>[n - i];
                for (int j = 0; j < n - i; j++) cyk[i][j] = new Tuple<HashSet<Nonterminal>, List<Tuple<Production, int>>>(new HashSet<Nonterminal>(), new List<Tuple<Production, int>>());
            }

            //prepare lookups (productions for a given NT or pair of NTs)
            Dictionary<Tuple<Nonterminal, Nonterminal>, HashSet<Production>> lookupNT = new Dictionary<Tuple<Nonterminal, Nonterminal>, HashSet<Production>>();
            Dictionary<string, HashSet<Production>> lookupT = new Dictionary<string, HashSet<Production>>();
            foreach (Production p in grammar.GetProductions())
            {
                if (p.IsSingleExprinal) //form: X -> a
                {
                    HashSet<Production> hashset = null;
                    if (!lookupT.TryGetValue(p.Rhs[0].Name, out hashset))
                    {
                        hashset = new HashSet<Production>();
                        lookupT.Add(p.Rhs[0].Name, hashset);
                    }
                    hashset.Add(p);
                }
                else if (p.Rhs.Length == 2)//form: X -> A B
                {
                    HashSet<Production> hashset = null;
                    var tuple = new Tuple<Nonterminal, Nonterminal>((Nonterminal)p.Rhs[0], (Nonterminal)p.Rhs[1]);
                    if (!lookupNT.TryGetValue(tuple, out hashset))
                    {
                        hashset = new HashSet<Production>();
                        lookupNT.Add(tuple, hashset);
                    }
                    hashset.Add(p);
                }
            }

            //CYK algorithm
            //first row (check for Productions X -> a)
            for (int i = 0; i < n; i++)
            {
                HashSet<Production> applicable = null;
                if (lookupT.TryGetValue(word.Substring(i, 1), out applicable))
                {
                    foreach(Production p in applicable)
                    {
                        cyk[0][i].Item1.Add(p.Lhs);
                        cyk[0][i].Item2.Add(new Tuple<Production, int>(p, 1));
                    }
                }
            }
            //fill rest
            for (int length = 1; length < n; length++)
            {
                for (int start = 0; start + length < n; start++)
                {
                    //to_fill: cyk[length][start]
                    for (int part1 = 0; part1 < length; part1++)
                    {
                        var left = cyk[part1][start].Item1;
                        var right = cyk[length - 1 - part1][start + 1 + part1].Item1;
                        if (left.Count > 0 && right.Count > 0)
                        {
                            foreach (Nonterminal leftNT in left)
                            {
                                foreach (Nonterminal rightNT in right)
                                {
                                    var tuple = new Tuple<Nonterminal, Nonterminal>(leftNT, rightNT);
                                    HashSet<Production> applicable = null;
                                    if (lookupNT.TryGetValue(new Tuple<Nonterminal, Nonterminal>(leftNT, rightNT), out applicable))
                                    {
                                        foreach (Production p in applicable)
                                        {
                                            cyk[length][start].Item1.Add(p.Lhs);
                                            cyk[length][start].Item2.Add(new Tuple<Production, int>(p, part1+1));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return cyk;
        }

        /// <summary>
        /// Finds the difference of 2 grammars.
        /// </summary>
        /// <param name="grammar1">the first grammar</param>
        /// <param name="grammar2">the second grammar</param>
        /// <param name="multiple">true (find all), false (just 1)</param>
        /// <param name="max_length">the maximal word length to be checked</param>
        /// <returns>a 3-Tuple, first = number of found words that are in both grammars, second = list of words that are only in grammar 1, third = list of words only in grammar 2</returns>
        public static Tuple<long, List<String>, List<String>> findDifference(ContextFreeGrammar grammar1, ContextFreeGrammar grammar2, bool multiple, int max_length)
        {
            var cnf1 = getEquivalentCNF(grammar1);
            var cnf2 = getEquivalentCNF(grammar2);

            long correct = 0;
            List<String> g1extra = new List<String>();
            List<String> g2extra = new List<String>();

            if (cnf1 == null && cnf2 == null) return Tuple.Create(correct, g1extra, g2extra); ; //both empty

            //check for empty word
            if (cnf1.acceptsEmptyString() && !cnf2.acceptsEmptyString()) g1extra.Add("");
            else if (!cnf1.acceptsEmptyString() && cnf2.acceptsEmptyString()) g2extra.Add("");
            else correct++;


            Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp1 = new Dictionary<Nonterminal, Dictionary<int, HashSet<string>>>();
            Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp2 = new Dictionary<Nonterminal, Dictionary<int, HashSet<string>>>();
            for (int length = 1; length <= max_length; length++)
            {
                var words1 = generateWordsWithLength(cnf1, length, dp1);
                var words2 = generateWordsWithLength(cnf2, length, dp2);
                foreach(string w1 in words1)
                {
                    if (!words2.Contains(w1))
                    {
                        g1extra.Add(w1);
                        if (!multiple) return Tuple.Create(correct, g1extra, g2extra);
                    }
                    else correct++;
                }
                foreach (string w2 in words2)
                {
                    if (!words1.Contains(w2))
                    {
                        g2extra.Add(w2);
                        if (!multiple) return Tuple.Create(correct, g1extra, g2extra);
                    }
                }
            }

            return Tuple.Create(correct, g1extra, g2extra);
        }

        /// <summary>
        /// Finds the difference of 2 grammars.
        /// </summary>
        /// <param name="grammar1">the first grammar</param>
        /// <param name="grammar2">the second grammar</param>
        /// <param name="multiple">true (find all), false (just 1)</param>
        /// <param name="timelimit">the time after with the check ends (CARE: there can still be a long rekusion step at the end such that the </param>
        /// <returns>a 3-Tuple, first = number of found words that are in both grammars, second = list of words that are only in grammar 1, third = list of words only in grammar 2</returns>
        public static Tuple<long, List<String>, List<String>> findDifferenceWithTimelimit(ContextFreeGrammar grammar1, ContextFreeGrammar grammar2, bool multiple, long timelimit)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var cnf1 = getEquivalentCNF(grammar1);
            var cnf2 = getEquivalentCNF(grammar2);

            long correct = 0;
            List<String> g1extra = new List<String>();
            List<String> g2extra = new List<String>();
            List<String> ggg = new List<String>();

            if (cnf1 == null && cnf2 == null) return Tuple.Create(correct, g1extra, g2extra); ; //both empty

            Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp1 = new Dictionary<Nonterminal, Dictionary<int, HashSet<string>>>();
            Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp2 = new Dictionary<Nonterminal, Dictionary<int, HashSet<string>>>();

            int length = 0;
            while (watch.ElapsedMilliseconds < timelimit)
            {
                var words1 = generateWordsWithLength(cnf1, length, dp1);
                var words2 = generateWordsWithLength(cnf2, length, dp2);
                foreach (string w1 in words1)
                {
                    if (!words2.Contains(w1))
                    {
                        g1extra.Add(w1);
                        if (!multiple) return Tuple.Create(correct, g1extra, g2extra);
                    }
                    else
                    {
                        ggg.Add(w1);
                        correct++;
                    }
                }
                foreach (string w2 in words2)
                {
                    if (!words1.Contains(w2))
                    {
                        g2extra.Add(w2);
                        if (!multiple) return Tuple.Create(correct, g1extra, g2extra);
                    }
                }

                length++;
            }

            return Tuple.Create(correct, g1extra, g2extra);
        }

        private static HashSet<string> generateWordsWithLength(ContextFreeGrammar cnf, int length, Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp)
        {
            HashSet<string> res = new HashSet<string>();
            if (cnf == null) return res; //empty grammar -> can't generate any words
            if (length == 0) //case: length = 0
            {
                if (cnf.acceptsEmptyString()) res.Add("");
            }
            else if (length == 1) //case: length = 1
            {
                foreach (Nonterminal nt in cnf.Variables)
                {
                    //init dp[nt]
                    Dictionary<int, HashSet<string>> curDP = new Dictionary<int, HashSet<string>>();
                    dp.Add(nt, curDP);

                    //find words of length 1
                    HashSet<string> l = new HashSet<string>();
                    foreach (Production p in cnf.GetProductions(nt))
                    {
                        if (p.IsSingleExprinal) l.Add(p.Rhs[0].ToString());
                    }
                    curDP.Add(1, l);
                    if (nt.Equals(cnf.StartSymbol)) res = l;
                }
            }
            else //case: length > 1
            {
                foreach (KeyValuePair<Nonterminal, Dictionary<int, HashSet<string>>> entry in dp)
                {
                    Nonterminal cur = entry.Key;
                    Dictionary<int, HashSet<string>> curDP = entry.Value;
                    HashSet<string> curSet = new HashSet<string>();
                    curDP.Add(length, curSet);
                    if (cur.Equals(cnf.StartSymbol)) res = curSet;

                    foreach (Production p in cnf.GetProductions(entry.Key))
                    {
                        if (p.Rhs.Length != 2) continue; //ignore productions that don't have form X->AB

                        Nonterminal left = (Nonterminal)p.Rhs[0];
                        Dictionary<int, HashSet<string>> leftDP = null;
                        dp.TryGetValue(left, out leftDP);

                        Nonterminal right = (Nonterminal)p.Rhs[1];
                        Dictionary<int, HashSet<string>> rightDP = null;
                        dp.TryGetValue(right, out rightDP);

                        for(int leftPart = 1; leftPart < length; leftPart++)
                        {
                            int rightPart = length - leftPart;

                            HashSet<string> leftPossibilities = null;
                            leftDP.TryGetValue(leftPart, out leftPossibilities);
                            HashSet<string> rightPossibilities = null;
                            rightDP.TryGetValue(rightPart, out rightPossibilities);

                            foreach(string leftString in leftPossibilities)
                            {
                                foreach (string rightString in rightPossibilities) curSet.Add(leftString + rightString);
                            }
                        }
                    }
                }
            }

            return res;
        }
    }
}