using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomataPDL.CFG
{
    public static class GrammarGrading
    {
        public static Tuple<int, IEnumerable<String>> gradeWordsInGrammar(ContextFreeGrammar grammar, IEnumerable<String> wordsIn, IEnumerable<String> wordsOut, int maxGrade)
        {
            int cases = 0;
            double correct = 0;
            var terminals = new List<char>();
            foreach (GrammarSymbol s in grammar.GetNonVariableSymbols())
            {
                terminals.Add(s.ToString()[0]);
            }
            List<String> feedback = new List<String>();
            
            HashSet<String> done = new HashSet<String>(); //for duplicate checking

            foreach(String w in wordsIn)
            {
                cases++;
                //handle duplicates
                if (done.Contains(w))
                {
                    feedback.Add(String.Format("The word \"{0}\" was used more than once!", w));
                    continue;
                }
                else done.Add(w);

                int prefixLength = GrammarUtilities.longestPrefixLength(grammar, w);

                if (prefixLength < 0) correct++; //correct
                else //wrong
                {
                    feedback.Add( String.Format("The word \"{0}\" isn't in the grammar! (hint: the word '{1}' is still possible prefix)", w, w.Substring(0, prefixLength)) );
                }
            }
            foreach (String w in wordsOut)
            {
                cases++;
                //handle duplicates
                if (done.Contains(w))
                {
                    feedback.Add(String.Format("The word \"{0}\" was used more than once!", w));
                    continue;
                }
                else done.Add(w);

                if (!GrammarUtilities.isWordInGrammar(grammar, w)) //correct
                {
                    //only useful terminals?
                    bool allUsefull = true;
                    char problem = 'a';
                    foreach (char c in w)
                    {
                        if (!terminals.Contains(c))
                        {
                            allUsefull = false;
                            problem = c;
                            break;
                        }
                    }

                    if (allUsefull) correct += 1; //full points
                    else //only half the points
                    {
                        correct += 0.5;
                        feedback.Add(String.Format("The word \"{0}\" uses the useless terminal '{1}'...", w, problem));
                    }
                } else //wrong
                {
                    feedback.Add(String.Format("The word \"{0}\" is in the grammar!", w));
                }
            }

            int grade = (int)Math.Floor(correct * maxGrade / (double) cases);

            //all correct?
            if (grade == maxGrade) feedback.Add("Correct!");

            return Tuple.Create(grade, (IEnumerable<String>) feedback);
        }

        public static Tuple<int, IEnumerable<String>> gradeGrammarEquality(ContextFreeGrammar solution, ContextFreeGrammar attempt, int maxGrade, long timelimit)
        {
            List<String> feedback = new List<String>();

            Tuple<long, List<String>, List<String>> res = GrammarUtilities.findDifferenceWithTimelimit(solution, attempt, true, timelimit);
            long correct = res.Item1;
            List<String> missing = res.Item2;
            List<String> tooMuch = res.Item3;
            long allChecked = correct + missing.Count + tooMuch.Count;

            if (missing.Count == 0 && tooMuch.Count == 0) //correct
            {
                feedback.Add("Correct!");
                return Tuple.Create(maxGrade, (IEnumerable<String>)feedback);
            }

            //wrong
            int grade = (int)Math.Floor(correct * maxGrade / (double)allChecked);
            double percMissing = missing.Count * 100 / (double)allChecked;
            double percTooMuch = tooMuch.Count * 100 / (double)allChecked;
            if (missing.Count > 0) feedback.Add(String.Format("Your solution misses some words (~{0:F2}%) e.g. you should accept \"{1}\"", percMissing, missing[0]));
            if (tooMuch.Count > 0) feedback.Add(String.Format("Your solution accepts too many words (~{0:F2}%) e.g. you shouldn't accept \"{1}\"", percTooMuch, tooMuch[0]));

            return Tuple.Create(grade, (IEnumerable<String>)feedback);
        }

        public static Tuple<int, IEnumerable<String>> gradeCYK(ContextFreeGrammar grammar, String word, HashSet<Nonterminal>[][] attempt, int maxGrade, bool withExample)
        {
            List<String> feedback = new List<String>();

            int n = word.Length;
            int checked_length = 0;
            HashSet<Nonterminal>[][] sol = GrammarUtilities.cyk(grammar, word);
            bool all_correct_sofar = true;

            for (int len = 1; len <= n; len++)
            {
                for(int start = 0; start + len <= n; start++)
                {
                    HashSet<Nonterminal> must = sol[len - 1][start];
                    HashSet<Nonterminal> was = attempt[len - 1][start];

                    Nonterminal missingExample = null;
                    int missing = 0;
                    Nonterminal tooMuchExample = null;
                    int tooMuch = 0;

                    //check if all must are present
                    foreach(Nonterminal nt in must)
                    {
                        if (!was.Contains(nt))
                        {
                            missingExample = nt;
                            missing++;
                            all_correct_sofar = false;
                        }
                    }

                    //check if all given are correct
                    foreach (Nonterminal nt in was)
                    {
                        if (!must.Contains(nt))
                        {
                            tooMuchExample = nt;
                            tooMuch++;
                            all_correct_sofar = false;
                        }
                    }

                    //feedback
                    String fieldName = String.Format("({0},{1})", start + 1, start + len);
                    if (withExample)
                    {
                        if (missing != 0) feedback.Add(String.Format("You are missing some nonterminals in field {0} e.g. {1}", fieldName, missingExample));
                        if (tooMuch != 0) feedback.Add(String.Format("You have nonterminals in field {0} that don't belong there... e.g. {1}", fieldName, tooMuchExample));
                    } else
                    {
                        if (missing != 0) feedback.Add(String.Format("You are missing some nonterminals in field {0}...", fieldName));
                        if (tooMuch != 0) feedback.Add(String.Format("You have nonterminals in field {0} that don't belong there...", fieldName));
                    }
                }
                
                if (!all_correct_sofar) break;
                checked_length = len;
            }

            //grade
            int grade = (int)Math.Floor(checked_length * maxGrade / (double) n);

            //all correct?
            if (feedback.Count == 0) feedback.Add("Correct!");

            return Tuple.Create(grade, (IEnumerable<String>)feedback);
        }
    }
}
