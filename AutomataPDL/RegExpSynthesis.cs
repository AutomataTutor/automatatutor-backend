using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Management;
using System.IO;

using System.Diagnostics;
using System.Threading;

using Microsoft.Automata;

namespace AutomataPDL
{
    public class RegexpSynthesis
    {
        #region sizes for search
        static int maxWidthC = 0;
        static int maxSigmaStarC = 0;

        static int maxWidth = 0;
        static int maxSigmaStar = 0;
        #endregion

        static int numStates;
        static HashSet<char> alph;
        static Regexp sigmaStar;
        static Regexp sigmaPlus;
        static Dictionary<string, Automaton<BvSet>> currUnionEls;

        static Dictionary<string, Automaton<BvSet>> memoDfa;

        static CharSetSolver solver;
        static Stopwatch timer;

        public static IEnumerable<Regexp> SynthesizeRegexp(HashSet<char> alphabet, Automaton<BvSet> dfa, CharSetSolver s, StringBuilder sb, long timeout)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\..\..\regexpenum.txt"))
            {
                solver = s;
                numStates = dfa.StateCount;
                alph = alphabet;

                #region test variables
                StringBuilder sb1 = new StringBuilder();
                int lim = 0;
                Stopwatch membershipTimer = new Stopwatch();
                Stopwatch equivTimer = new Stopwatch();
                timer = new Stopwatch();
                timer.Start();
                #endregion


                #region TestSets for equiv
                var mytests = DFAUtilities.MyHillTestGeneration(alphabet, dfa, solver);
                var posMN = mytests.First;
                var negMN = mytests.Second;
                var tests = DFAUtilities.GetTestSets(dfa, alphabet, solver);
                var positive = tests.First;
                var negative = tests.Second;
                foreach (var t in posMN)
                    positive.Remove(t);
                foreach (var t in negMN)
                    negative.Remove(t);
                #endregion

                #region Sigma Star
                bool fst = true;
                foreach (var c in alph)
                    if (fst)
                    {
                        fst = false;
                        sigmaStar = new RELabel(c);
                    }
                    else
                        sigmaStar = new REUnion(sigmaStar, new RELabel(c));
                
                sigmaPlus = new REPlus(sigmaStar);
                sigmaStar = new REStar(sigmaStar); 
                #endregion                

                #region Accessories vars
                maxWidthC = 0;
                maxSigmaStarC = 0;
                var isSubset = true;
                HashSet<string> visited = new HashSet<string>();
                HashSet<string> newReg = new HashSet<string>();
                currUnionEls = new Dictionary<string,Automaton<BvSet>>();
                memoDfa = new Dictionary<string, Automaton<BvSet>>(); 
                List<Regexp> subsetReg = new List<Regexp>();
                #endregion

                for (maxWidth = 1; true; maxWidth++)
                {
                    newReg = new HashSet<string>();
                    maxSigmaStar = 2;

                    foreach (var regexp in EnumerateRegexp())
                    {
                        #region run for at most timeout
                        if (timer.ElapsedMilliseconds > timeout)
                        {
                            sb.AppendLine("| Timeout");
                            timer.Stop();
                            yield break;
                        }
                        #endregion

                        var re = regexp.Normalize();

                        if (!(visited.Contains(re.ToString())))
                        {
                            visited.Add(re.ToString());

                            sb1 = new StringBuilder();
                            sb1.Append(re.ToString());
                            file.WriteLine(sb1);
                            lim++;

                            #region Membership test
                            membershipTimer.Start();
                            isSubset = CorrectOnNegSet(regexp, negMN);
                            membershipTimer.Stop();
                            #endregion

                            #region equivalence check
                            if (isSubset)
                            {
                                membershipTimer.Start();
                                if (CorrectOnNegSet(regexp, negative))
                                {
                                    if (CorrectOnPosSet(regexp, posMN) && CorrectOnPosSet(regexp, positive))
                                    {
                                        membershipTimer.Stop();
                                        equivTimer.Start();
                                        var rDfa = getDfa(regexp);
                                        memoDfa[regexp.ToString()] = rDfa;

                                        if (rDfa.IsEquivalentWith(dfa, solver))
                                        {
                                            isSubset = false;
                                            equivTimer.Stop();
                                            timer.Stop();

                                            sb.Append("| ");
                                            regexp.ToString(sb);
                                            sb.AppendLine("|");
                                            sb.AppendLine(string.Format("| elapsed time:    \t {0} ms", timer.ElapsedMilliseconds));
                                            sb.AppendLine(string.Format("| equivalence cost:\t {0} ms", equivTimer.ElapsedMilliseconds));
                                            sb.AppendLine(string.Format("| membership cost: \t {0} ms", membershipTimer.ElapsedMilliseconds));
                                            sb.AppendLine(string.Format("| attempts:        \t {0}", lim));
                                            yield return regexp;
                                        }
                                        else
                                        {
                                            Console.WriteLine("used dfa");
                                            equivTimer.Stop();
                                        }
                                    }
                                    else
                                    {
                                        membershipTimer.Stop();
                                    }
                                }
                                else
                                {
                                    membershipTimer.Stop();
                                    isSubset = false;
                                }
                            }
                            #endregion

                            //#region Subsets
                            //if (isSubset)
                            //{
                            //    foreach (var reg1 in subsetReg)
                            //    {
                            //        var union = (reg1.CompareTo(regexp) > 0) ? (new REUnion(reg1, regexp)) : (new REUnion(regexp, reg1));
                            //        visited.Add(union.ToString());
                            //        sb1 = new StringBuilder();
                            //        sb1.Append(union + " From union");
                            //        file.WriteLine(sb1);
                            //        lim++;

                            //        membershipTimer.Start();
                            //        if (CorrectOnPosSet(union, posMN) && CorrectOnPosSet(union, positive))
                            //        {
                            //            membershipTimer.Stop();

                            //            equivTimer.Start();
                            //            var rDfa = getDfa(union);
                            //            memoDfa[union.ToString()] = rDfa;

                            //            if (rDfa.IsEquivalentWith(dfa, solver))
                            //            {
                            //                equivTimer.Stop();
                            //                timer.Stop();

                            //                sb.Append("| ");
                            //                union.ToString(sb);
                            //                sb.AppendLine("|");
                            //                sb.AppendLine(string.Format("| elapsed time:    \t {0} ms", timer.ElapsedMilliseconds));
                            //                sb.AppendLine(string.Format("| equivalence cost:\t {0} ms", equivTimer.ElapsedMilliseconds));
                            //                sb.AppendLine(string.Format("| membership cost: \t {0} ms", membershipTimer.ElapsedMilliseconds));
                            //                sb.AppendLine(string.Format("| attempts:        \t {0}", lim));
                            //                yield return union;
                            //            }
                            //            else
                            //            {
                            //                Console.WriteLine("used dfa");
                            //                equivTimer.Stop();
                            //            }
                            //        }
                            //        else
                            //        {
                            //            membershipTimer.Stop();
                            //        }
                            //    }
                            //    subsetReg.Add(regexp);
                            //}
                            //#endregion
                        }
                    }

                    visited = new HashSet<string>(visited.Union(newReg));
                }
            }
        }

        //regexp enumerator
        private static IEnumerable<Regexp> EnumerateRegexp()
        {
            if (maxWidthC == maxWidth)
                yield break;

            maxWidthC++;

            if (maxSigmaStarC < maxSigmaStar)
            {
                maxSigmaStarC++;
                yield return sigmaStar;
                //yield return sigmaPlus;
                maxSigmaStarC--;
            }

            foreach (var c in alph)
                yield return new RELabel(c);

            foreach (var r1 in EnumerateRegexp())
            {
                var r1dfa = getDfa(r1);

                if (!(r1 is REPlus) && !(r1 is REStar) && !(r1 is REQMark))
                {
                    var fine = true;
                    var rr1 = r1 as REConcatenation;
                    if (rr1 != null && (rr1.left is REStar) && (rr1.right is REStar))
                        fine = false;
                    var rr2 = r1 as REUnion;
                    if (rr2 != null && (rr2.left is REStar) && (rr2.right is REStar))
                        fine = false;

                    if (fine)
                    {
                        yield return new REStar(r1);
                        yield return new REPlus(r1);
                        yield return new REQMark(r1);
                    }
                }

                foreach (var r2 in EnumerateRegexp())
                {
                    var r2dfa = getDfa(r2);

                    //var r1r2dfa = getDfa(r1r2);

                    #region Concatentation
                    //if(!(r1 is REConcatenation))
                    if(!(r1.ToString()==sigmaStar.ToString() && (r2 is REStar || r2 is REQMark)))
                        if (!(r2.ToString() == sigmaStar.ToString() && (r1 is REStar || r1 is REQMark)))
                        {
                            bool isConcFine = true;
                            if (r1 is REStar)
                            {
                                string[] strs = r1.ToString().Split('(');
                                var strend = strs[strs.Length - 1];
                                strend = strend.Substring(0, strend.Length - 2);
                                if (r2.ToString().StartsWith(strend))                      //REmove a*a coz same as a+ 
                                    isConcFine = false;
                            }
                            if (isConcFine)
                            {
                                string[] strs = r2.ToString().Split('*');
                                if (strs.Length > 1)
                                {
                                    var subr2 = r2.ToString().Substring(1, strs[0].Length - 2);
                                    if (r1.ToString().EndsWith(subr2))                         //REmove aa* coz same as a+
                                        isConcFine = false;
                                }
                            }

                            if (isConcFine)
                                yield return new REConcatenation(r1, r2);
                        }
                    #endregion

                    #region Union
                        //if (!(r1 is REUnion))
                    if (r1.CompareTo(r2) < 0)
                        if (!currUnionEls.Keys.Contains(r1.ToString()) && !currUnionEls.Keys.Contains(r2.ToString()) &&
                            !currUnionEls.Keys.Contains("(" + r1.ToString() + @")*") && !currUnionEls.Keys.Contains("(" + r2.ToString() + @")*") &&
                                !currUnionEls.Keys.Contains("(" + r1.ToString() + @")+") && !currUnionEls.Keys.Contains("(" + r2.ToString() + @")+") &&
                                    !currUnionEls.Keys.Contains("(" + r1.ToString() + @")?") && !currUnionEls.Keys.Contains("(" + r2.ToString() + @")?"))
                            if (r1 != sigmaStar && r2 != sigmaStar)
                            {
                                if (r1.ToString() != ("(" + r2.ToString() + @")*") && r2.ToString() != ("(" + r1.ToString() + @")*") &&
                                    r1.ToString() != ("(" + r2.ToString() + @")+") && r2.ToString() != ("(" + r1.ToString() + @")+") &&
                                        r1.ToString() != ("(" + r2.ToString() + @")?") && r2.ToString() != ("(" + r1.ToString() + @")?"))
                                {
                                    currUnionEls[r1.ToString()] = r1dfa;
                                    currUnionEls[r2.ToString()] = r2dfa;
                                    yield return new REUnion(r1, r2);
                                    currUnionEls.Remove(r1.ToString());
                                    currUnionEls.Remove(r2.ToString());
                                }
                            } 
                    #endregion
                }
            }
            maxWidthC--;

        }

        #region Private methods
        private static string escReg(string regexp)
        {
            return @"^" + regexp + @"$";
        }

        private static Automaton<BvSet> getDfa(Regexp regexp)
        {
            var re = regexp.Normalize();
            if (!memoDfa.Keys.Contains(re.ToString()))             
                memoDfa[re.ToString()] = re.getDFA(alph,solver);
             
            return memoDfa[re.ToString()];
        } 
        #endregion

        #region Test checks
        private static bool CorrectOnNegSet(Regexp regexp, IEnumerable<string> testSet)
        {
            foreach (var test in testSet)
                if (regexp.HasModel(test))
                {
                    return false;
                }
            return true;
        }

        private static bool CorrectOnPosSet(Regexp regexp, IEnumerable<string> testSet)
        {
            foreach (var test in testSet)
                if (!regexp.HasModel(test))
                {
                    return false;
                }
            return true;
        }
        #endregion
    }    
}
