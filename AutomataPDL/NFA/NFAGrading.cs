using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;

using Microsoft.Automata;
using Microsoft.Z3;

namespace AutomataPDL
{
    public class NFAGrading
    {
        /// <summary>
        /// Computes the grade for attempt using all the possible metrics
        /// </summary>
        /// <param name="solutionNFA">correct nfa</param>
        /// <param name="attemptNFA">nfa to be graded</param>
        /// <param name="alpahbet">input alphabet</param>
        /// <param name="solver">SMT solver for char set</param>
        /// <param name="timeout">timeout for the PDL enumeration (suggested > 1000)</param>
        /// <param name="maxGrade">Max grade for the homework</param>
        /// <param name="level">Feedback level</param>
        /// <returns>Grade for nfa2</returns>
        public static Pair<int, IEnumerable<NFAFeedback>> GetGrade(
            Automaton<BDD> solutionNFA, Automaton<BDD> attemptNFA, HashSet<char> alphabet,
            CharSetSolver solver, long timeout, int maxGrade, FeedbackLevel level)
        {
            var feedbacks = new List<NFAFeedback>();
            int deadStateDeduction = 0;
            int tooBigDeduction = 0;
            int incorrectDeduction = 0;

            // Remove at most a percentage of max grade when NFA is big
            double maxDeductionForTooBig = ((double)maxGrade * 0.3);
            double maxDeductionForDeadStates = ((double)maxGrade * 0.1);


            double solutionStateCount = solutionNFA.StateCount;
            double attemptStateCount = attemptNFA.StateCount;
            double solutionTransCount = solutionNFA.MoveCount;
            double attemptTransCount = attemptNFA.MoveCount;
            NFAEditDistanceProvider nfaedp = new NFAEditDistanceProvider(solutionNFA, alphabet, solver, timeout);

            //Check if using epsilon and nondeterminism
            if (solutionNFA.IsEpsilonFree)
                solutionNFA.CheckDeterminism(solver);
            if (attemptNFA.IsEpsilonFree)
                attemptNFA.CheckDeterminism(solver);

            bool shouldUseEpsilon = !solutionNFA.IsEpsilonFree && attemptNFA.IsEpsilonFree;
            bool shouldUseNonDet = !shouldUseEpsilon && !solutionNFA.isDeterministic && attemptNFA.isDeterministic;

            //Check if solution has dead states and remove if it does
            var statesBeforeDeadStatesElimination = attemptNFA.StateCount;
            attemptNFA.EliminateDeadStates();
            var solutionHasDeadStates = attemptNFA.StateCount < statesBeforeDeadStatesElimination;

            //Start checking equiv
            bool areEquivalent = solutionNFA.IsEquivalentWith(attemptNFA, solver);

            if (areEquivalent)
            {
                // prompt nfa is correct
                feedbacks.Insert(0, new NFAStringFeedback(level, alphabet, solver, "Your NFA accepts the CORRECT language."));

                #region Check number of states and decrease grade if too big
                int stateDiff = (int)(attemptStateCount - solutionStateCount);
                int transDiff = (int)(attemptTransCount - solutionTransCount);

                //If is not minimal apply deduction and compute edit
                if (stateDiff > 0 || transDiff > 0)
                {
                    #region Try to collapse for feedback
                    // Try to find a way to collaps states or remove states and transitions to make the NFA smaller  
                    NFAEditScript collapseScript = null;
                    var edit = nfaedp.NFACollapseSearch(attemptNFA);
                    if (edit != null)
                    {
                        collapseScript = new NFAEditScript();
                        collapseScript.script.Insert(0, edit);
                    }
                    feedbacks.Add(new NFANotMinimalFeedback(level, alphabet, stateDiff, transDiff, collapseScript, solver));
                    #endregion

                    #region Compute tooBigDeduction
                    if (stateDiff > 0)
                    {
                        // ((att/sol)^2)-1
                        var stateRatio = attemptStateCount / solutionStateCount;
                        var stateRatioSqM1 = Math.Pow(stateRatio, 2) - 1;
                        var sclaedStateRatio = stateRatioSqM1 * maxDeductionForTooBig / 2;

                        tooBigDeduction = (int)Math.Round(Math.Min(sclaedStateRatio, maxDeductionForTooBig));
                    }
                    else
                    {
                        if (transDiff > 0)
                        {
                            // ((att/sol)^2)-1
                            var transRatio = attemptTransCount / solutionTransCount;
                            var transRatioSqM1 = Math.Pow(transRatio, 2) - 1;
                            var sclaedTransRatio = transRatioSqM1 * maxDeductionForTooBig / 2;

                            tooBigDeduction = (int)Math.Round(Math.Min(sclaedTransRatio, maxDeductionForTooBig));
                        }
                    }
                    //Make sure deduction is positive
                    tooBigDeduction = Math.Max(tooBigDeduction, 0);
                    #endregion
                }
                #endregion

            }
            else
            {
                // prompt nfa is incorrect
                feedbacks.Add(new NFAStringFeedback(level, alphabet, solver, "Your NFA does NOT accept the correct langauge."));

                //inequivalent, try using grading metrics and based on winning metric give feedback
                int remainingGrade = maxGrade - tooBigDeduction;

                #region metric computation
                //compute deterministic versions
                var solutionNFAdet = solutionNFA.RemoveEpsilons(solver.MkOr).Determinize(solver).MakeTotal(solver).Minimize(solver);
                var attemptNFAdet = attemptNFA.RemoveEpsilons(solver.MkOr).Determinize(solver).MakeTotal(solver).Minimize(solver);
                solutionNFAdet.EliminateDeadStates();
                attemptNFAdet.EliminateDeadStates();

                //compute density
                double densityRatio = DFADensity.GetDFADifferenceRatio(solutionNFAdet, attemptNFAdet, alphabet, solver);

                //compute edit distance
                double nfaED = 2;
                var editScript = nfaedp.GetNFAOptimalEdit(attemptNFA);

                if (editScript != null)
                    nfaED = ((double)(editScript.GetCost())) / ((double)((solutionNFA.StateCount + 1) * alphabet.Count));
                #endregion

                #region metrics scaling
                var scalingSquareDensity = 1; var multv2 = 0.5;
                var scalingSquareDFAED = 1.03;

                var scaledDensityRatio = (scalingSquareDensity + (multv2 * densityRatio)) * (scalingSquareDensity + (multv2 * densityRatio)) - scalingSquareDensity * scalingSquareDensity;
                var scaledNfaED = (scalingSquareDFAED + nfaED) * (scalingSquareDFAED + nfaED) - scalingSquareDFAED * scalingSquareDFAED;

                //If the edit script was not computed make sure it loses.
                if (editScript == null)
                    scaledNfaED = Double.MaxValue;

                //Select dominating Feedback based on grade
                double unscaledGrade = Math.Min(scaledDensityRatio, scaledNfaED);
                var dfaedwins = scaledNfaED <= scaledDensityRatio;
                var densitywins = scaledDensityRatio <= scaledNfaED;

                incorrectDeduction = (int)Math.Round(unscaledGrade * (double)(maxGrade));
                #endregion


                //If edit distance search works, provides feedback based upon result
                //Otherwise, gives counterexample feedback
                if (level != FeedbackLevel.Minimal)
                {
                    if (dfaedwins)
                        feedbacks.Add(new NFAEDFeedback(solutionNFA, attemptNFA, level, alphabet, editScript, solver));
                    else
                        feedbacks.Add(new NFACounterexampleFeedback(level, alphabet, solutionNFAdet, attemptNFAdet, solver));
                }

            }

            // Feedback related to nondeterminism and epislon
            if (shouldUseEpsilon)
                feedbacks.Add(new NFAStringFeedback(level, alphabet, solver, "You should try using epsilon transitions."));
            if (shouldUseNonDet)
                feedbacks.Add(new NFAStringFeedback(level, alphabet, solver, "You should try using nondeterminism."));

            // Deduct points and prompt feedback is solution has dead states
            if (solutionHasDeadStates)
            {
                deadStateDeduction = (int)maxDeductionForDeadStates;
                feedbacks.Add(new NFAStringFeedback(level, alphabet, solver, "Your NFA has some dead states."));
            }

            //Grade computation
            int grade = Math.Max(maxGrade - deadStateDeduction - tooBigDeduction - incorrectDeduction, 0);

            return new Pair<int, IEnumerable<NFAFeedback>>(grade, feedbacks);
        }

    }
}
