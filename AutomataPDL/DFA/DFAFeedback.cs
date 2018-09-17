using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace AutomataPDL
{
    public enum StringFeedbackType
    {
        Correct,
        Wrong
    }
    public enum FeedbackLevel
    {
        Minimal,
        SmallHint,
        Hint,
        Solution
    }
    public enum FeedbackType
    {
        DFAED,
        PDLED,
        Density,
        String
    }

    /// <summary>
    /// Feedback class
    /// </summary>
    public abstract class DFAFeedback
    {
        public double utility;
        public FeedbackLevel level;
        public FeedbackType type;
        public CharSetSolver solver;
        public HashSet<char> alphabet;

        private DFAFeedback() {
            this.level = FeedbackLevel.Minimal;
        }

        public DFAFeedback(FeedbackLevel level, HashSet<char> alphabet, double utility, CharSetSolver solver)
        {
            this.alphabet = alphabet;
            this.level = level;
            this.solver = solver;
            this.utility = Math.Round(Math.Max(1-utility,0)*100);
        }

        public abstract override string ToString();
    }

    /// <summary>
    /// This basic feedback is used for simple correct or wrong messages.
    /// </summary>
    public class StringFeedback : DFAFeedback
    {
        public StringFeedbackType sfType;

        public StringFeedback(FeedbackLevel level, StringFeedbackType sfType, HashSet<char> alphabet, CharSetSolver solver)
            : base(level, alphabet, 1, solver)
        {
            this.type = FeedbackType.String;
            this.sfType = sfType;
        }

        public override string ToString()
        {
            switch(sfType){
                case StringFeedbackType.Correct: return "CORRECT!!";
                case StringFeedbackType.Wrong: return "Try again";
                default: throw new PDLException(string.Format("Undefined type {0}",sfType));
            }
        }
    }

    /// <summary>
    /// This feedback outputs a set of edits that, if applied to the attempt DFA will
    /// transform it into a correct one.
    /// </summary>
    class DFAEDFeedback : DFAFeedback
    {
        string counterexample;
        DFAEditScript script;

        public DFAEDFeedback(Automaton<BDD> dfaGoal, Automaton<BDD> dfaAttempt, FeedbackLevel level, HashSet<char> alphabet, DFAEditScript script, double utility, CharSetSolver solver)
            : base(level, alphabet, utility, solver)
        {
            var positiveDifference = dfaGoal.Minus(dfaAttempt, solver).Determinize(solver).Minimize(solver);
            var negativeDifference = dfaAttempt.Minus(dfaGoal, solver).Determinize(solver).Minimize(solver);
            this.counterexample = DFAUtilities.GenerateShortTerm(positiveDifference.IsEmpty ? negativeDifference : positiveDifference, solver);
            this.type = FeedbackType.DFAED;
            this.script = script;
        }

        public override string ToString()
        {
            string result = "";//string.Format("U: {0}%. ", utility);
            switch (level)
            {
                case FeedbackLevel.Hint:
                case FeedbackLevel.SmallHint:
                    {
                        int statesToogleCount = 0;
                        int statesAddCount = 0;
                        int editMoveCount = 0;
                        Dictionary<int, int> transitions = new Dictionary<int, int>();
                        foreach (var edit in script.script)
                            if (edit is DFAAddState)
                                statesAddCount++;
                            else
                                if (edit is DFAEditState)
                                    statesToogleCount++;
                                else
                                {
                                    editMoveCount++;
                                    var cedit = edit as DFAEditMove;
                                    if (!transitions.ContainsKey(cedit.sourceState))
                                        transitions[cedit.sourceState] = 0;
                                    transitions[cedit.sourceState]++;
                                }

                        if (statesAddCount > 0)
                            result += string.Format("Your DFA does not contain enough states; ");

                        if (statesToogleCount > 0)
                        {
                            if (statesToogleCount == 1)                            
                                result += string.Format("You need to change the acceptance condition of one state; ");                            
                            else                            
                                result += string.Format("You need to change the set of final states; ");                          
                        }

                        if (editMoveCount > 0)
                            foreach (var key in transitions.Keys)
                                result += string.Format("Check the transitions out of state {0}; ", key);

                        result += "<br /> The following counterexample might help you: ";
                        result += counterexample == "" ? "the empty string" : string.Format("<i>{0}</i>", counterexample);
                            
                        break;
                    }
                case FeedbackLevel.Minimal: result += string.Format("Your solutions needs {0} {1}.", script.script.Count, (script.script.Count == 1 ? "edit" : "edits")); break;
                //case FeedbackLevel.SmallHint:
                //    {
                //        int states = 0;
                //        int transitions = 0;
                //        foreach (var edit in script.script)
                //            if (edit is DFAAddState || edit is DFAEditState)
                //                states++;
                //            else
                //                transitions++;
                //        if (states == 0)
                //        {
                //            result += string.Format("You need to edit {0} {1}.", transitions, (transitions == 1 ? "transition" : "transitions")); break;
                //        }
                //        else
                //        {
                //            if (transitions == 0)
                //            {
                //                result += string.Format("You need to add/change {0} {1}.", states, (states == 1 ? "state" : "states")); break;
                //            }
                //            else
                //            {
                //                result += string.Format("You need to add/change {0} {1}, and edit {2} {3}", states, (states == 1 ? "state" : "states"), transitions, (transitions == 1 ? "transition" : "transitions")); break;
                //            }
                //        }
                        
                //    }
                case FeedbackLevel.Solution: result += script.ToString(); break;

                default: throw new PDLException(string.Format("Undefined level {0}", level));
            }
            return result;
        }
    }

    /// <summary>
    /// This feedback outputs a colored description of the correct/wrong language highlighting
    /// the parts that should be changed in order to fix the language description.
    /// </summary>
    class PDLEDFeedback : DFAFeedback
    {
        Transformation transformation;

        public PDLEDFeedback(FeedbackLevel level, HashSet<char> alphabet, Transformation transformation, double utility, CharSetSolver solver)
            : base(level, alphabet, utility,solver)
        {
            this.transformation = transformation;
            this.type = FeedbackType.PDLED;
        }

        public override string ToString()
        {
            if (transformation == null)
                throw new PDLException("Transformation is null");

            string result = "";// string.Format("U: {0}%. ", utility);
            switch (level)
            {
                case FeedbackLevel.Hint:
                    {
                        if (transformation.pdlB is PDLEmptyString)
                            result += string.Format("Your DFA only accepts the empty string");
                        else
                            if (transformation.pdlB is PDLFalse)
                                result += string.Format("Your DFA doesn't accept any string");
                            else
                                if (transformation.pdlB is PDLTrue)
                                    result += string.Format("Your DFA accepts every string");
                                else
                                    result += string.Format("This is the set of strings accepted by your DFA:<br /> <div align='center'>{0}</div>",
                                    transformation.totalCost < 5 ? transformation.ToHTMLColoredStringBtoA("red", "blue") : transformation.ToEnglishString(transformation.pdlB));
                        break;
                    }
                case FeedbackLevel.Minimal: result += "The language accepted by your solution is not quite correct."; break;
                case FeedbackLevel.SmallHint:
                    {
                        result += string.Format("The set of strings your DFA should accept is the following:<br /> <div align='center'>{0}</div>",
                        transformation.totalCost < 5 ? transformation.ToHTMLColoredStringAtoB("red", "blue") : transformation.ToEnglishString(transformation.pdlA)); break;
                    }
                case FeedbackLevel.Solution:
                    {
                        result += string.Format("Your solution accepts the set of strings: <br /><div align='center'>{0}</div>instead of the set:<br /><div align='center'>{1}</div>",
                            transformation.totalCost < 5 ?                           
                            transformation.ToHTMLColoredStringBtoA("red", "blue") :
                            transformation.ToEnglishString(transformation.pdlB),

                            transformation.totalCost < 5 ?
                            transformation.ToHTMLColoredStringAtoB("red", "blue") :
                            transformation.ToEnglishString(transformation.pdlA));

                        break;
                    }
                default: throw new PDLException(string.Format("Undefined level {0}", level));
            }
            return result;
        }
    }

    /// <summary>
    /// This feedback corresponds to a description of the language difference between the student
    /// attempt and a correct solution. Based on the level of feedback, the output is
    /// either a language description, and underapproximation of the difference, or a counterexample.
    /// </summary>
    class DensityFeedback : DFAFeedback
    {
        Automaton<BDD> positiveDifference;
        Automaton<BDD> negativeDifference;
        Automaton<BDD> symmetricDifference;

        public DensityFeedback(FeedbackLevel level, HashSet<char> alphabet, Automaton<BDD> dfaGoal, Automaton<BDD> dfaAttempt, double utility, CharSetSolver solver)
            : base(level, alphabet, utility,solver)
        {
            BDD pred = solver.False;
            foreach (var el in alphabet)
                pred=solver.MkOr(pred,solver.MkCharConstraint(false,el));

            var dfaAll = Automaton<BDD>.Create(0,new int[]{0},new Move<BDD>[]{new Move<BDD>(0,0,pred)});
            this.type = FeedbackType.Density;
            this.positiveDifference = dfaGoal.Minus(dfaAttempt, solver).Determinize(solver).Minimize(solver);
            this.negativeDifference = dfaAttempt.Minus(dfaGoal, solver).Determinize(solver).Minimize(solver);
            this.symmetricDifference = dfaAll.Minus(dfaAll.Minus(positiveDifference,solver).Intersect(dfaAll.Minus(negativeDifference,solver),solver),solver).Determinize(solver).Minimize(solver);                
            this.solver = solver;
        }

        public override string ToString()
        {
            long enumTimeout = 1000L;
            #region feedback components
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            PDLPred symmPhi = null;
            PDLPred underPhi = null;
            string posWitness = null;
            string negWitness = null;
            //If hint or solution try computing the description of the symmdiff
            if (level == FeedbackLevel.Hint || level == FeedbackLevel.Solution)
            {
                //Avoid formulas that are too complex
                var maxSize = 7;
                if(symmetricDifference.StateCount<15)
                    foreach (var phi1 in pdlEnumerator.SynthesizePDL(alphabet, symmetricDifference, solver, new StringBuilder(), enumTimeout))
                    {
                        var sizePhi1 = phi1.GetFormulaSize();
                        if (sizePhi1 < maxSize && !phi1.IsComplex())
                        {
                            maxSize = sizePhi1;
                            symmPhi = phi1;
                        }
                    }
            }
            //Avoid empty string case and particular string
            if (symmPhi is PDLEmptyString || symmPhi is PDLIsString)
                symmPhi = null;

            //If not minimal try computing and underapprox of symmdiff
            if (symmPhi == null && level != FeedbackLevel.Minimal)
            {
                //Avoid formulas that are too complex
                var minSize = 9;
                if (symmetricDifference.StateCount < 15)
                    foreach (var phi2 in pdlEnumerator.SynthesizeUnderapproximationPDL(alphabet, symmetricDifference, solver, new StringBuilder(), enumTimeout))
                    {
                        var formula = phi2.First;
                        var sizeForm = formula.GetFormulaSize();
                        if (sizeForm < minSize && !formula.IsComplex())
                        {
                            minSize = sizeForm;
                            underPhi = formula;
                        }

                        break;
                    }
            }
            //Avoid empty string case and particular string
            if (underPhi is PDLEmptyString || underPhi is PDLIsString)
                underPhi = null;

            if (!positiveDifference.IsEmpty)
                posWitness = DFAUtilities.GenerateShortTerm(positiveDifference, solver);
            else
                negWitness = DFAUtilities.GenerateShortTerm(negativeDifference, solver);           
            #endregion

            string result = ""; //string.Format("U: {0}%. ", utility);
            if (symmPhi != null)
            {
                if (symmPhi is PDLEmptyString)
                    result += "Your solution does not behave correctly on the empty string";                
                else
                    result += string.Format("Your solution is not correct on this set of strings: <br /> <div align='center'>{0}</div>", PDLUtil.ToEnglishString(symmPhi));               
            }
            else
                if (underPhi != null)
                {
                    if (underPhi is PDLEmptyString)
                        result += "Your solution does not behave correctly on the empty string";         
                    else
                        result += string.Format("Your solution is not correct on this set of strings: <br /> <div align='center'>{0}</div>",
                                        PDLUtil.ToEnglishString(underPhi));                    
                }
                else
                {
                    if (posWitness != null)
                        result += string.Format("Your solution does not accept the {0} while the correct solution does.",
                                    posWitness != "" ? "string '<i>" + posWitness + "</i>'" : "empty string");
                    else
                        result += string.Format("Your solution accepts the {0} while the correct solution doesn't.",
                                    negWitness != "" ? "string '<i>" + negWitness + "</i>'" : "empty string");
                }
            return result;
        }
    }
}
