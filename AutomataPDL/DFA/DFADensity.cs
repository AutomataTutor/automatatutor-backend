using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Z3;

namespace AutomataPDL
{
    public static class DFADensity
    {
        #region language density
        /// <summary>
        /// Computes the ratio of the symmetric difference to the size of dfa1 enumerating paths up to length n (uses the complement if density is high)
        /// </summary>        
        /// <returns>size of ((dfa2-dfa1)+(dfa1-dfa2))/dfa1</returns>
        public static double GetDFADifferenceRatio(Automaton<BDD> dfa1, Automaton<BDD> dfa2, HashSet<char> al, CharSetSolver solver)
        {
            var solutionDensity = DFADensity.GetDFADensity(dfa1, al, solver);

            //Symmetric difference
            var dfadiff1 = dfa1.Minus(dfa2, solver);
            var dfadiff2 = dfa2.Minus(dfa1, solver);
            var dfatrue = Automaton<BDD>.Create(0, new int[] { 0 }, new Move<BDD>[] { new Move<BDD>(0, 0, solver.True) });
            var dfadiff = dfatrue.Minus(dfatrue.Minus(dfadiff1, solver).Intersect(dfatrue.Minus(dfadiff2, solver), solver), solver).Determinize(solver).Minimize(solver);

            //Use smallest of |dfa1| and complement of |dfa1| for cardinality base
            return GetDFARatio(dfa1.Determinize(solver).Minimize(solver), dfadiff, al, solver, solutionDensity > 0.5);
        }

        /// <summary>
        /// Computes the ratio of two dfas
        /// </summary>        
        /// <returns>size of dfa2/ size of dfa1</returns>
        internal static double GetDFARatio(Automaton<BDD> dfa1, Automaton<BDD> dfa2, HashSet<char> al, CharSetSolver solver, bool isSolDense)
        {
            var n = dfa1.StateCount;

            double multiplier = 3;
            int k = Math.Min(13, (int)(n * multiplier));

            int finalDivider = k;

            double[] paths1 = GetPathsUpToN(dfa1, al, solver, k);
            double[] paths2 = GetPathsUpToN(dfa2, al, solver, k);

            double sum = 0;
            for (int i = 0; i <= k; i++)
            {
                //TODO check grading still works
                double divider = Math.Min(paths1[i], Math.Pow(al.Count, i) - paths1[i]);
                if (divider != 0)
                    sum += (paths2[i] / divider);
                else
                {
                    sum += paths2[i];
                    if (paths2[i] == 0)
                        finalDivider--;
                }
            }

            return sum / (finalDivider + 1);
        }

        /// <summary>
        /// Computes the approximate denstity of dfa
        /// </summary>        
        /// <returns>size of dfa2/ size of dfa1</returns>
        internal static double GetDFADensity(Automaton<BDD> dfa, HashSet<char> al, CharSetSolver solver)
        {
            var n = dfa.StateCount;

            double multiplier = 3;
            int k = Math.Min(13, (int)(n * multiplier));

            double[] paths1 = GetPathsUpToN(dfa, al, solver, k);

            double sum = 0;
            for (int i = 0; i <= k; i++)
            {
                double divider = Math.Pow(al.Count, i);
                sum += (paths1[i] / divider);
            }

            return sum / (k+1);
        }

        // returns an array where a[n] is the number of paths of length n
        private static double[] GetPathsUpToN(Automaton<BDD> dfa, HashSet<char> al, CharSetSolver solver, int n)
        {
            var normDfa1 = DFAUtilities.normalizeDFA(dfa).First;

            int length = 0;

            double[] totPaths = new double[n + 1];
            var finalStates = normDfa1.GetFinalStates();

            double[] pathNum = new double[normDfa1.StateCount];
            pathNum[0] = 1;
            totPaths[0] = finalStates.Contains(0) ? 1 : 0;
            for (int i = 1; i < pathNum.Length; i++)
                pathNum[i] = 0;


            while (length < n)
            {
                double[] oldPathNum = pathNum.ToArray();
                for (int i = 0; i < pathNum.Length; i++)
                    pathNum[i] = 0;

                length++;
                foreach (var state in normDfa1.States)
                    if (oldPathNum[state] > 0)
                        foreach (var move in normDfa1.GetMovesFrom(state))
                        {
                            int size = 0;
                            //Check if epsilon transition
                            if (move.Label == null)
                                size = 1;
                            else
                                foreach (var v in solver.GenerateAllCharacters(move.Label, false))
                                    size++;

                            pathNum[move.TargetState] += oldPathNum[state] * size;
                        }

                //totPaths[length] = totPaths[length - 1];
                foreach (var state in finalStates)
                    totPaths[length] += pathNum[state];
            }

            return totPaths;
        }
        #endregion        
    }
}
