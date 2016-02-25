using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace AutomataPDL
{

    /// <summary>
    /// Feedback class
    /// </summary>
    public abstract class NFAFeedback
    {
        public FeedbackLevel level;
        public CharSetSolver solver;
        public HashSet<char> alphabet;

        private NFAFeedback()
        {
            this.level = FeedbackLevel.Minimal;
        }

        public NFAFeedback(FeedbackLevel level, HashSet<char> alphabet, CharSetSolver solver)
        {
            this.alphabet = alphabet;
            this.level = level;
            this.solver = solver;
        }

        public abstract override string ToString();
    }

    /// <summary>
    /// This basic feedback is used for simple correct or wrong messages.
    /// </summary>
    public class NFAStringFeedback : NFAFeedback
    {
        public string message;

        public NFAStringFeedback(FeedbackLevel level, HashSet<char> alphabet, CharSetSolver solver, string message)
            : base(level, alphabet, solver)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return message;
        }
    }

    /// <summary>
    /// This feedback tells the student that the solution is not minimal and how to iprove it
    /// if the level is hint. Otherwise just say there is a smaller one.
    /// </summary>
    class NFANotMinimalFeedback : NFAFeedback
    {
        NFAEditScript script;
        int stateDiff;
        int transitionDiff;

        public NFANotMinimalFeedback (
            FeedbackLevel level, HashSet<char> alphabet,
            int stateDiff, int transitionDiff,
            NFAEditScript script,
            CharSetSolver solver)
            : base(level, alphabet, solver)
        {
            this.transitionDiff = transitionDiff;
            this.stateDiff = stateDiff;
            this.script = script;
        }

        public override string ToString()
        {
            string feedbackMessage = "";
            //if the script is null the message should be how many fewer

            //otherwise cut and paste from NFAED feedback to prompt some message on what to do
            if (stateDiff > 0 || transitionDiff > 0)
            {

                // this 
                feedbackMessage = string.Format("There exists an equivalent NFA with ", transitionDiff);
            }
            if (stateDiff > 0)
            {
                feedbackMessage += string.Format("{0} fewer states", stateDiff);
            }

            if (transitionDiff > 0)
            {
                if (stateDiff > 0)
                    feedbackMessage += " and ";
                feedbackMessage += string.Format("{0} fewer transitions.", transitionDiff);
            }

            if (feedbackMessage != null && level == FeedbackLevel.Minimal)
                return "There exists an equivalent NFA that is smaller.";

            if(script!=null)
                feedbackMessage += " " + script.ToString();

            return feedbackMessage;
        }
    }

    /// <summary>
    /// This feedback corresponds to a description of the language difference between the student
    /// attempt and a correct solution. Based on the level of feedback, the output is
    /// either a language description, and underapproximation of the difference, or a counterexample.
    /// </summary>
    class NFACounterexampleFeedback : NFAFeedback
    {
        Automaton<BDD> positiveDifference;
        Automaton<BDD> negativeDifference;

        // automata come already determinized
        public NFACounterexampleFeedback(
            FeedbackLevel level, HashSet<char> alphabet, 
            Automaton<BDD> solutionDFA, Automaton<BDD> attemptDFA, 
            CharSetSolver solver)
            : base(level, alphabet, solver)
        {
            BDD pred = solver.False;
            foreach (var el in alphabet)
                pred = solver.MkOr(pred, solver.MkCharConstraint(false, el));

            var dfaAll = Automaton<BDD>.Create(0, new int[] { 0 }, new Move<BDD>[] { new Move<BDD>(0, 0, pred) });
            this.positiveDifference = solutionDFA.Minus(attemptDFA, solver).Determinize(solver).Minimize(solver);
            this.negativeDifference = attemptDFA.Minus(solutionDFA, solver).Determinize(solver).Minimize(solver);
            this.solver = solver;
        }

        public override string ToString()
        {
            string posWitness = null;
            string negWitness = null;

            if (!positiveDifference.IsEmpty)
            {
                posWitness = DFAUtilities.GenerateShortTerm(positiveDifference, solver);
                return string.Format("Your NFA does not accept the {0} while the correct solution does.",
                            posWitness != "" ? "string '<i>" + posWitness + "</i>'" : "empty string");
            }
            else
            {
                negWitness = DFAUtilities.GenerateShortTerm(negativeDifference, solver);
                return string.Format("Your NFA accepts the {0} while the correct solution doesn't.",
                            negWitness != "" ? "string '<i>" + negWitness + "</i>'" : "empty string");
            }
        }
    }

    /// <summary>
    /// This feedback outputs a set of edits that, if applied to the attempt NFA will
    /// transform it into a correct one.
    /// </summary>
    class NFAEDFeedback : NFAFeedback
    {
        string counterexample;
        NFAEditScript script;

        public NFAEDFeedback(Automaton<BDD> nfaGoal, Automaton<BDD> nfaAttempt, 
            FeedbackLevel level, HashSet<char> alphabet, 
            NFAEditScript script, CharSetSolver solver)
            : base(level, alphabet, solver)
        {
            //TODO might have to determinize to do this operations
            var positiveDifference = nfaGoal.Minus(nfaAttempt, solver).Determinize(solver).Minimize(solver);
            var negativeDifference = nfaAttempt.Minus(nfaGoal, solver).Determinize(solver).Minimize(solver);
            this.counterexample = DFAUtilities.GenerateShortTerm(positiveDifference.IsEmpty ? negativeDifference : positiveDifference, solver);            
            this.script = script;
        }

        public override string ToString()
        {
            string feedbackMessage = "";//string.Format("U: {0}%. ", utility);
            switch (level)
            {
                case FeedbackLevel.Hint:
                case FeedbackLevel.SmallHint:
                    {
                        int statesToggleCount = 0;
                        int statesAddCount = 0;
                        int editMoveCount = 0;
                        Dictionary<int, int> transitions = new Dictionary<int, int>();
                        foreach (var edit in script.script)
                            if (edit is NFAAddState)
                                statesAddCount++;
                            else
                                if (edit is NFAEditState)
                                    statesToggleCount++;
                                else
                                {
                                    editMoveCount++;
                                    var cedit = edit as NFAEditMove;
                                    if (!transitions.ContainsKey(cedit.sourceState))
                                        transitions[cedit.sourceState] = 0;
                                    transitions[cedit.sourceState]++;
                                }

                        if (statesAddCount > 0)
                            feedbackMessage += string.Format("Your solution does not contain enough states; ");

                        if (statesToggleCount > 0)
                        {
                            if (statesToggleCount == 1)
                                feedbackMessage += string.Format("You need to change the acceptance condition of one state; ");
                            else
                                feedbackMessage += string.Format("You need to change the set of final states; ");
                        }

                        if (editMoveCount > 0)
                            foreach (var key in transitions.Keys)
                                feedbackMessage += string.Format("Check the transitions out of state {0}; ", key);

                        feedbackMessage += "<br /> The following counterexample might help you: ";
                        feedbackMessage += counterexample == "" ? "the empty string" : string.Format("<i>{0}</i>", counterexample);

                        break;
                    }
                case FeedbackLevel.Minimal: feedbackMessage += string.Format("Your solutions needs {0} {1}.", script.script.Count, (script.script.Count == 1 ? "edit" : "edits")); break;

                case FeedbackLevel.Solution: feedbackMessage += script.ToString(); break;

                default: throw new PDLException(string.Format("Undefined level {0}", level));
            }
            return feedbackMessage;
        }
    }


}
