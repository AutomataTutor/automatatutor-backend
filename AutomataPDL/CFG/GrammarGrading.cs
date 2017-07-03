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

            foreach(String w in wordsIn)
            {
                cases++;
                int prefixLength = GrammarUtilities.longestPrefixLength(grammar, w);

                if (prefixLength == -2) correct++; //correct
                else //wrong
                {
                    feedback.Add( String.Format("The word \"{0}\" isn't in the grammar but should be! (hint: the word '{1}' is still possible prefix)", w, w.Substring(0, prefixLength)) );
                }
            }
            foreach (String w in wordsOut)
            {
                cases++;
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
                    feedback.Add(String.Format("The word \"{0}\" is in the grammar but shouldn't be!", w));
                }
            }

            int grade = (int)Math.Floor(correct * maxGrade / (double) cases);
            return Tuple.Create(grade, (IEnumerable<String>) feedback);
        }

        public static Tuple<int, IEnumerable<String>> gradeGrammarEquality(ContextFreeGrammar solution, ContextFreeGrammar attempt, int maxGrade, long timelimit)
        {
            //TODO
            return null;
        }
    }
}
