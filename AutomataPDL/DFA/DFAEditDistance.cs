using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using System.Diagnostics;
using System.Threading;

using System.Diagnostics.Contracts;

using Microsoft.Automata;
using Microsoft.Z3;

namespace AutomataPDL
{
    public static class DFAEditDistance
    {
        #region DFA edit distance

        /// <summary>
        /// Finds min edit distance script between DFAs if operation
        /// takes less than timeout ms
        /// </summary>
        /// <param name="dfa1"></param>
        /// <param name="dfa2"></param>
        /// <param name="al"></param>
        /// <param name="solver"></param>
        /// <param name="timeout"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        public static DFAEditScript GetDFAOptimalEdit( // copy
            Automaton<BDD> dfa1, Automaton<BDD> dfa2,
            HashSet<char> al, CharSetSolver solver, long timeout,
            StringBuilder sb)
        {
            //Contract.Assert(dfa1.IsDeterministic);
            //Contract.Assert(dfa2.IsDeterministic);

            DFAEditScript editScript = new DFAEditScript();

            #region Add states to dfa2 to make it at least as dfa1
            BDD fullAlphabetCondition = BDDOf(al, solver);

            //Normalize the DFA giving only names from 0 to |States|-1
            var normDfaPair = DFAUtilities.normalizeDFA(dfa2);
            var dfa2augmented = normDfaPair.First;
            //solver.SaveAsDot(dfa2augmented, "aaaa");
            var stateNamesMapping = normDfaPair.Second;

            //Add states to make dfa2 have the |dfa2.States|>= |dfa1.States|
            var newMoves = new List<Move<BDD>>(dfa2augmented.GetMoves());

            for (int i = 1; i <= dfa1.StateCount - dfa2augmented.StateCount; i++)
            {
                int newStateName = dfa2augmented.MaxState + i;
                //Pick the next available name to be added
                stateNamesMapping[newStateName] = dfa2.MaxState + i;
                //save the operation in the script
                editScript.script.Insert(0, new DFAAddState(dfa2.MaxState + i));
                newMoves.Add(new Move<BDD>(newStateName, newStateName, fullAlphabetCondition));
                newStateName++;
            }
            //Create the new DFA with the added states
            dfa2augmented = Automaton<BDD>.Create(dfa2augmented.InitialState, dfa2augmented.GetFinalStates().ToList(), newMoves);
            #endregion

            int maxScore = (dfa1.StateCount + dfa2augmented.StateCount) * (al.Count + 1);
            int oldScirptSize = editScript.script.Count;

            //Start with the internal script equals to null, at the end bestScript.Script will contain the best script
            DFAEditScript bestScript = new DFAEditScript();
            bestScript.script = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Iteratively check if there exists an edit of a given depth            
            for (int depth = 1; true; depth++)
            {
                var editList = new List<DFAEdit>();
                if (GetDFAEditScriptTimeout(
                    dfa1, dfa2augmented, al, solver,
                    new List<long>(), editScript.script,
                    depth, timeout, sw, DFAUtilities.MyHillTestGeneration(al, dfa1, solver),
                    DFADensity.GetDFADensity(dfa1, al, solver),
                    editScript.GetCost(), bestScript, stateNamesMapping))
                {
                    // if hits timeout break and return null
                    break;
                }
                if (bestScript.script != null)
                {
                    bestScript.script.Reverse();
                    sw.Stop();
                    return bestScript;
                }
            }

            sw.Stop();
            return null;
        }

        // looks for an edit at depth "depth" 
        // returns false and null in bestScript if no edit is found at depth "depth"
        // returns false and not null in bestScript if found
        // returns true if timeout
        internal static bool GetDFAEditScriptTimeout(
            Automaton<BDD> dfa1, Automaton<BDD> dfa2,
            HashSet<char> al, CharSetSolver solver,
            List<long> editScriptHash, List<DFAEdit> editList,
            int depth, long timeout, Stopwatch sw,
            Pair<IEnumerable<string>, IEnumerable<string>> tests,
            double dfa1density,
            int totalCost,
            DFAEditScript bestScript, Dictionary<int, int> stateNamesMapping)
        {
            // check timer
            if (sw.ElapsedMilliseconds > timeout)
                return true;

            //Compute worst case distance, call finalScript with this value?
            int dist = (dfa1.StateCount + dfa2.StateCount) * (al.Count + 1);

            //Stop if no more moves left
            if (depth == 0)
            {
                if (DFAUtilities.ApproximateMNEquivalent(tests, dfa1density, dfa2, al, solver) && dfa2.IsEquivalentWith(dfa1, solver))
                    //check if totalCost < finalScript cost and replace if needed
                    if (bestScript.script == null || totalCost < bestScript.GetCost())
                        bestScript.script = ObjectCopier.Clone<List<DFAEdit>>(editList);
                return false;
            }

            DFAEdit edit = null;

            #region Flip one move target state
            foreach (var move in dfa2.GetMoves())
            {
                //Creaty copy of the moves without current move
                var movesWithoutCurrMove = dfa2.GetMoves().ToList();
                movesWithoutCurrMove.Remove(move);

                //Redirect every ch belonging to move condition
                foreach (var c in solver.GenerateAllCharacters(move.Label, false))
                {
                    long hash = IntegerUtil.PairToInt(move.SourceState, c - 97) + dfa2.StateCount;

                    if (CanAdd(hash, editScriptHash))
                    {
                        editScriptHash.Insert(0, hash);

                        //Local copy of moves
                        var newMoves = movesWithoutCurrMove.ToList();
                        var newMoveCondition = solver.MkCharConstraint(false, c);

                        #region Remove ch from current move
                        var andCond = solver.MkAnd(move.Label, solver.MkNot(newMoveCondition));
                        //add back move without ch iff satisfiable
                        if (solver.IsSatisfiable(andCond))
                            newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, andCond));
                        #endregion

                        #region Redirect c to a different state
                        foreach (var state in dfa2.States)
                            if (state != move.TargetState)
                            {
                                var newMovesComplete = newMoves.ToList();
                                newMovesComplete.Add(new Move<BDD>(move.SourceState, state, newMoveCondition));
                                var dfa2new = Automaton<BDD>.Create(dfa2.InitialState, dfa2.GetFinalStates(), newMovesComplete);

                                edit = new DFAEditMove(stateNamesMapping[move.SourceState], stateNamesMapping[state], c);
                                editList.Insert(0, edit);
                                if (GetDFAEditScriptTimeout(dfa1, dfa2new, al, solver, editScriptHash, editList, depth - 1, timeout, sw, tests, dfa1density, totalCost + edit.GetCost(), bestScript, stateNamesMapping))
                                    return true;
                                editList.RemoveAt(0);
                            }
                        #endregion

                        editScriptHash.RemoveAt(0);

                    }
                }
            }
            #endregion

            #region Flip one state from fin to non fin
            foreach (var state in dfa2.States)
            {
                if (CanAdd(state, editScriptHash))
                {
                    //flip its final non final status
                    editScriptHash.Insert(0, state);

                    var newFinalStates = new HashSet<int>(dfa2.GetFinalStates());
                    Automaton<BDD> dfa2new = null;
                    if (dfa2.GetFinalStates().Contains(state))
                    {
                        edit = new DFAEditState(stateNamesMapping[state], false);
                        editList.Insert(0, edit);
                        newFinalStates.Remove(state);
                        dfa2new = Automaton<BDD>.Create(dfa2.InitialState, newFinalStates, dfa2.GetMoves());
                    }
                    else
                    {
                        edit = new DFAEditState(stateNamesMapping[state], true);
                        editList.Insert(0, edit);
                        newFinalStates.Add(state);
                        dfa2new = Automaton<BDD>.Create(dfa2.InitialState, newFinalStates, dfa2.GetMoves());
                    }

                    if (GetDFAEditScriptTimeout(dfa1, dfa2new, al, solver, editScriptHash, editList, depth - 1, timeout, sw, tests, dfa1density, totalCost + edit.GetCost(), bestScript, stateNamesMapping))
                        return true;

                    editScriptHash.RemoveAt(0);
                    editList.RemoveAt(0);
                }
            }
            #endregion

            return false;
        }

        //Keep list ordered only add ordered sequences to avoid repetitions
        private static bool CanAdd(long elemCode, List<long> editList)
        {
            return editList.Count == 0 || elemCode < editList.ElementAt(0);
        }

        private static BDD BDDOf(IEnumerable<char> alphabet, CharSetSolver solver)
        {
            bool fst = true;
            BDD safeCharCond = null;
            foreach (var c in alphabet)
                if (fst)
                {
                    fst = false;
                    safeCharCond = solver.MkCharConstraint(false, c);
                }
                else
                    safeCharCond = solver.MkOr(safeCharCond, solver.MkCharConstraint(false, c));
            return safeCharCond;
        }
        #endregion
    }



    #region EDIT SCRIPT and EDITS

    public class DFAEditScript
    {
        public List<DFAEdit> script;

        public DFAEditScript()
        {
            script = new List<DFAEdit>();
        }

        public int GetCost()
        {
            int cost = 0;
            int stateChanges = 0;
            foreach (var edit in script)
            {
                if (edit is DFAEditState)
                {
                    stateChanges++;
                    //Only count up to 2 state changes, after that conceptual mistake
                    if (stateChanges <= 2)
                        cost += edit.GetCost();
                }
                else
                    cost += edit.GetCost();                
            }
            return cost;
        }

        //Check if a script is too complex and if it is it doesn't show it
        public bool IsComplex()
        {
            if (GetCost() > 2)
                return true;
            foreach (var edit in script)            
                if (edit is DFAAddState)               
                    return true;
             
            return false;
        }

        public void ToString(StringBuilder sb)
        {
            foreach (var edit in script)
            {
                edit.ToString(sb);
            }
            return;
        }

        public void ToHintString(StringBuilder sb)
        {
            foreach (var edit in script)
            {
                edit.ToHintString(sb);
            }
            return;
        }

        public string ToHintString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToHintString(sb);
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    [Serializable()]
    public abstract class DFAEdit
    {
        public virtual int GetCost()
        {
            return 1;
        }
        public abstract void ToString(StringBuilder sb);
        public abstract void ToHintString(StringBuilder sb);
        public abstract override string ToString();
    }

    [Serializable()]
    public class DFAAddState : DFAEdit
    {
        public int state;
        public DFAAddState(int s)
        {
            state = s;
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("add state {0}; ", state);
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("add state {0}; ", state);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    [Serializable()]
    public class DFAEditState : DFAEdit
    {
        public int state;
        public bool makeFinal;
        public DFAEditState(int state, bool makeFinal)
        {
            this.makeFinal = makeFinal;
            this.state = state;
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("Make state {0} {1}; ", state, makeFinal ? "final" : "non-final");
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("One more state should be made {0}; ", makeFinal ? "final" : "non-final");
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    [Serializable()]
    public class DFAEditMove : DFAEdit
    {
        public int sourceState;
        public int newTargetState;
        public char ch;

        public DFAEditMove(int sourceState, int newTargetState, char ch)
        {
            this.sourceState = sourceState;
            this.newTargetState = newTargetState;
            this.ch = ch;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("delta({0},{1}) should be {2}; ", sourceState, ch, newTargetState);
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("delta({0},{1}) is incorrect; ", sourceState, ch, newTargetState);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }
    #endregion
}
