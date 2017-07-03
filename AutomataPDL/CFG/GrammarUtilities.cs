using System;
using System.Collections.Generic;

using Microsoft.Automata;

namespace AutomataPDL.CFG
{
    public static class GrammarUtilities
    {


        /// <summary>
        /// Generates a CNF for a given grammar or returns null if the Grammar doesn't produce any words.
        /// </summary>
        /// <param name="g">the original grammar</param>
        /// <returns>the CNF or null</returns>
        public static ContextFreeGrammar getEquivalentCNF(ContextFreeGrammar g)
        {
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

            return cyk_table[word.Length - 1][0].Contains(grammar.StartSymbol);
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
            if (word.Length == 0) return 0;

            //prefix closure
            Nonterminal originalStart = grammar.StartSymbol;
            var prefixGrammar = getPrefixClosure(grammar);
            prefixGrammar = getEquivalentCNF(prefixGrammar);

            //CYK
            var cyk_table = cyk(prefixGrammar, word);

            //check if word was in original grammar
            if (cyk_table[word.Length - 1][0].Contains(grammar.StartSymbol)) return -2;

            //check for startsymbol in first row
            for (int i = word.Length - 1; i >= 0; i--)
            {
                if (cyk_table[i][0].Contains(prefixGrammar.StartSymbol)) return i+1;
            }
            return 0;
        }

        /// <summary>
        /// Performs the CYK-algorithm
        /// </summary>
        /// <param name="grammar">the grammar (in CNF)</param>
        /// <param name="word">the word (not null)</param>
        /// <returns>the filled table of the cyk-algorithm</returns>
        private static HashSet<Nonterminal>[][] cyk(ContextFreeGrammar grammar, string word)
        {
            //CYK algorithm
            int n = word.Length;
            HashSet<Nonterminal>[][] cyk = new HashSet<Nonterminal>[n][];
            for (int i = 0; i < n; i++)
            {
                cyk[i] = new HashSet<Nonterminal>[n - i];
                for (int j = 0; j < n - i; j++) cyk[i][j] = new HashSet<Nonterminal>();
            }

            //prepare lookups
            Dictionary<Tuple<Nonterminal, Nonterminal>, HashSet<Nonterminal>> lookupNT = new Dictionary<Tuple<Nonterminal, Nonterminal>, HashSet<Nonterminal>>();
            Dictionary<string, HashSet<Nonterminal>> lookupT = new Dictionary<string, HashSet<Nonterminal>>();
            foreach (Production p in grammar.GetProductions())
            {
                if (p.IsSingleExprinal) //form: X -> a
                {
                    HashSet<Nonterminal> hashset = null;
                    if (!lookupT.TryGetValue(p.Rhs[0].Name, out hashset))
                    {
                        hashset = new HashSet<Nonterminal>();
                        lookupT.Add(p.Rhs[0].Name, hashset);
                    }
                    hashset.Add(p.Lhs);
                }
                else if (p.Rhs.Length == 2)//form: X -> A B
                {
                    HashSet<Nonterminal> hashset = null;
                    var tuple = new Tuple<Nonterminal, Nonterminal>((Nonterminal)p.Rhs[0], (Nonterminal)p.Rhs[1]);
                    if (!lookupNT.TryGetValue(tuple, out hashset))
                    {
                        hashset = new HashSet<Nonterminal>();
                        lookupNT.Add(tuple, hashset);
                    }
                    hashset.Add(p.Lhs);
                }
            }

            //first row (check for Productions X -> a)
            for (int i = 0; i < n; i++)
            {
                if (!lookupT.TryGetValue(word.Substring(i, 1), out cyk[0][i]))
                {
                    cyk[0][i] = new HashSet<Nonterminal>();
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
                        var left = cyk[part1][start];
                        var right = cyk[length - 1 - part1][start + 1 + part1];
                        if (left.Count > 0 && right.Count > 0)
                        {
                            foreach (Nonterminal leftNT in left)
                            {
                                foreach (Nonterminal rightNT in right)
                                {
                                    var tuple = new Tuple<Nonterminal, Nonterminal>(leftNT, rightNT);
                                    HashSet<Nonterminal> found = null;
                                    if (lookupNT.TryGetValue(new Tuple<Nonterminal, Nonterminal>(leftNT, rightNT), out found))
                                    {
                                        cyk[length][start].UnionWith(found);
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

            if (cnf1 == null && cnf2 == null) return Tuple.Create(correct, g1extra, g2extra); ; //both empty

            //check for empty word
            if (cnf1.acceptsEmptyString() && !cnf2.acceptsEmptyString()) g1extra.Add("");
            else if (!cnf1.acceptsEmptyString() && cnf2.acceptsEmptyString()) g2extra.Add("");
            else correct++;


            Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp1 = new Dictionary<Nonterminal, Dictionary<int, HashSet<string>>>();
            Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp2 = new Dictionary<Nonterminal, Dictionary<int, HashSet<string>>>();

            int length = 1;
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

                length++;
            }

            return Tuple.Create(correct, g1extra, g2extra);
        }

        private static HashSet<string> generateWordsWithLength(ContextFreeGrammar cnf, int length, Dictionary<Nonterminal, Dictionary<int, HashSet<string>>> dp)
        {
            HashSet<string> res = null;
            if (length == 1) //case: length = 1
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