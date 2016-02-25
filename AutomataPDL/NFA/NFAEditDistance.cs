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
    public class NFAEditDistanceProvider
    {

        Automaton<BDD> nfa1;
        HashSet<char> al; 
        CharSetSolver solver;
        long timeout;
        Dictionary<char, int> alphabetMap;
        Stopwatch sw;
        Pair<IEnumerable<string>, IEnumerable<string>> tests;
        double nfa1density;       

        /// <summary>
        /// create new instance of NFAEdit distance and assigns a number to each character
        /// </summary>
        /// <param name="nfa1"></param>
        /// <param name="nfa2"></param>
        /// <param name="al"></param>
        /// <param name="solver"></param>
        /// <param name="timeout"></param>
        public NFAEditDistanceProvider(Automaton<BDD> nfa1, 
            HashSet<char> al, CharSetSolver solver, long timeout)
        {
            this.nfa1 = nfa1;

            this.al = al;
            this.solver = solver;
            this.timeout = timeout;
            this.alphabetMap = new Dictionary<char, int>();
            int index = 0;
            foreach(var c in al){
                this.alphabetMap[c] = index;
                index++;
            }
            this.sw = new Stopwatch();
            this.tests = NFAUtilities.MyHillTestGeneration(al, nfa1.Determinize(solver), solver);
            var dfa1 = nfa1.RemoveEpsilons(solver.MkOr).Determinize(solver).Minimize(solver);
            this.nfa1density = DFADensity.GetDFADensity(dfa1, al, solver);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NFAEditScript GetNFAOptimalEdit(Automaton<BDD> nfa2)
        {
            NFAEditScript editScript = new NFAEditScript();           

            //Start timer
            sw.Start();

            //Normalize NFAs
            var normNfaPair = DFAUtilities.normalizeDFA(nfa2);
            var normNfa2 = normNfaPair.First;
            var stateNamesMapping = normNfaPair.Second;


            NFAEditScript bestScript = new NFAEditScript();
            bestScript.script = null;

            // increase depth up to maxMoves
            for (int depth = 1; true; depth++)
            {
                var editList = new List<NFAEdit>();
                if(GetNFAEditScriptTimeout(
                    depth, -1, normNfa2,
                    editScript.script, editScript.GetCost(), bestScript))
                {
                    // if hits timeout break and return null
                    break;
                }
                if (bestScript.script != null)
                {
                    bestScript.script.Reverse();
                    sw.Stop();
                    var mappedEditList = new List<NFAEdit>();

                    //fix states name because of normalization
                    foreach (var edit in bestScript.script)
                    {   
                        NFAEdit mappedEdit = null;
                        if(edit is NFAEditState){
                            var castEdit = edit as NFAEditState;
                            mappedEdit = new NFAEditState(stateNamesMapping[castEdit.state], castEdit.makeFinal);
                        }
                        if(edit is NFAEditMove){
                            var castEdit = edit as NFAEditMove;
                            mappedEdit = new NFAEditMove(
                                stateNamesMapping[castEdit.sourceState], 
                                stateNamesMapping[castEdit.newTargetState],
                                castEdit.ch);
                        }
                        mappedEditList.Add(mappedEdit);
                    }
                    return bestScript;
                }
            }

            return null;
        }

        // looks for an edit at depth "depth" 
        // returns false and null in bestScript if no edit is found at depth "depth"
        // returns false and not null in bestScript if found
        // returns true if timeout
        internal bool GetNFAEditScriptTimeout(
            int depth, long lastEditHash, 
            Automaton<BDD> currentNfa2,
            List<NFAEdit> editList, int scriptCost,
            NFAEditScript bestScript)
        {
            // if timeout return true
            if (sw.ElapsedMilliseconds > timeout)
                return true;


            //Stop if no more moves left
            if (depth == 0)
            {
                if (DFAUtilities.ApproximateMNEquivalent(tests, nfa1density, currentNfa2, al, solver) && currentNfa2.IsEquivalentWith(nfa1, solver))
                    //check if totalCost < finalScript cost and replace if needed
                    if (bestScript.script == null || scriptCost < bestScript.GetCost())
                        bestScript.script = ObjectCopier.Clone<List<NFAEdit>>(editList);
                return false;
            }

            NFAEdit edit = null;

            long thisEditHash = 0;

            #region Flip one state from fin to non fin
            foreach (var state in currentNfa2.States)
            {
                thisEditHash = state;
                if (CanAdd(thisEditHash, lastEditHash))
                {
                    //flip its final non final status

                    var newFinalStates = new HashSet<int>(currentNfa2.GetFinalStates());
                    Automaton<BDD> nfa2new = null;
                    if (currentNfa2.GetFinalStates().Contains(state))
                    {
                        edit = new NFAEditState(state, false);
                        editList.Insert(0, edit);
                        newFinalStates.Remove(state);
                        nfa2new = Automaton<BDD>.Create(currentNfa2.InitialState, newFinalStates, currentNfa2.GetMoves());
                    }
                    else
                    {
                        edit = new NFAEditState(state, true);
                        editList.Insert(0, edit);
                        newFinalStates.Add(state);
                        nfa2new = Automaton<BDD>.Create(currentNfa2.InitialState, newFinalStates, currentNfa2.GetMoves());
                    }

                    if (GetNFAEditScriptTimeout(depth - 1, thisEditHash, nfa2new, editList, scriptCost + edit.GetCost(), bestScript))
                        return true;

                    editList.RemoveAt(0);
                }
            }
            #endregion

            #region Change transition from source state
            currentNfa2 = NFAUtilities.normalizeMoves(currentNfa2, solver);
            foreach (var sourceState in currentNfa2.States)
            {
                HashSet<int> unreachedStates = new HashSet<int>(currentNfa2.States);
                foreach (var moveFromSource in currentNfa2.GetMovesFrom(sourceState))
                {
                    // take all chars in alphabet
                    foreach (var c in al)
                    {
                        long moveHash =  currentNfa2.StateCount + IntegerUtil.TripleToInt(sourceState, moveFromSource.TargetState, alphabetMap[c]);
                        thisEditHash = currentNfa2.StateCount + moveHash;
                        if (CanAdd(thisEditHash, lastEditHash))
                        {
                            BDD cCond = solver.False;
                            BDD newCond = solver.False;

                            //skip epsilon moves
                            if (moveFromSource.Label != null)
                            {
                                // if c in move, remove it and recursion
                                if (solver.Contains(moveFromSource.Label, c))
                                {
                                    cCond = solver.MkNot(solver.MkCharConstraint(false, c));
                                    newCond = solver.MkAnd(moveFromSource.Label, cCond);
                                }
                                else // if c not in move, add it and recursion
                                {
                                    cCond = solver.MkCharConstraint(false, c);
                                    newCond = solver.MkOr(moveFromSource.Label, cCond);
                                }

                                var newMoves = new List<Move<BDD>>(currentNfa2.GetMoves());
                                newMoves.Remove(moveFromSource);
                                newMoves.Add(new Move<BDD>(sourceState, moveFromSource.TargetState, newCond));
                                var nfa2new = Automaton<BDD>.Create(currentNfa2.InitialState, currentNfa2.GetFinalStates(), newMoves);

                                edit = new NFAEditMove(sourceState, moveFromSource.TargetState, c);
                                editList.Insert(0, edit);

                                if (GetNFAEditScriptTimeout(depth - 1, thisEditHash, nfa2new, editList, scriptCost + edit.GetCost(), bestScript))
                                    return true;

                                editList.RemoveAt(0);
                            }
                        }
                    }

                    unreachedStates.Remove(moveFromSource.TargetState);
                }

                foreach (var targetState in unreachedStates)
                {
                    //try adding a symbol not in transition
                    foreach (var c in al)
                    {
                        long moveHash = IntegerUtil.TripleToInt(sourceState, targetState, alphabetMap[c]);
                        thisEditHash = currentNfa2.StateCount + moveHash;

                        var moveCond = solver.MkCharConstraint(false, c);
                        var newMoves = new List<Move<BDD>>(currentNfa2.GetMoves()); 
                        newMoves.Add(new Move<BDD>(sourceState, targetState, moveCond));
                        var nfa2new = Automaton<BDD>.Create(currentNfa2.InitialState, currentNfa2.GetFinalStates(), newMoves);

                        edit = new NFAEditMove(sourceState, targetState, c);
                        editList.Insert(0, edit);

                        //TODO put correct hash
                        if (GetNFAEditScriptTimeout(depth - 1, thisEditHash, nfa2new, editList, scriptCost + edit.GetCost(), bestScript))
                            return true;

                        editList.RemoveAt(0);
                    }
                }
            }            
            #endregion

            return false;
        }

        // Returns first collapse edit found that shrinks nfa2
        // Returns null if none found
        // Returns null if nfa2 is not equivalent with nfa1
        public NFAEdit NFACollapseSearch(Automaton<BDD> nfa2)
        {
            //Makes sure the two nfas are equivalent
            if(!nfa1.IsEquivalentWith(nfa2, solver))
                return null;

            nfa2 = NFAUtilities.normalizeMoves(nfa2, solver);

            // checks if any state can be removed            
            foreach (var state in nfa2.States)
                if (state != nfa2.InitialState)
                    if (NFAUtilities.canRemoveState(nfa2, state, solver, tests, al))
                        return new NFARemoveState(state);

            // checks if any move can be removed
            foreach (var state1 in nfa2.States)
                foreach (var state2 in nfa2.States)
                    if (state1 < state2)
                        if (NFAUtilities.canCollapseStates(nfa2, state1, state2, solver, tests, al))
                            return new NFACollapseStates(state1, state2);

            // checks if any move can be removed
            foreach (var state1 in nfa2.States)
                foreach (var state2 in nfa2.States)
                    if (NFAUtilities.canRemoveEdge(nfa2, state1, state2, solver, tests, al))
                        return new NFARemoveEdge(state1, state2);

            //returns null if search fails
            return null;
        }

        //Keep list ordered only add ordered sequences to avoid repetitions
        private static bool CanAdd(long thisEditHash, long lastEditHash)
        {
            return thisEditHash > lastEditHash;
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
    }

    #region Edits and Edit Script
    public class NFAEditScript
    {
        public List<NFAEdit> script;

        public NFAEditScript()
        {
            script = new List<NFAEdit>();
        }

        public int GetCost()
        {
            int cost = 0;
            int stateChanges = 0;
            foreach (var edit in script)
            {
                if (edit is NFAEditState)
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
                if (edit is NFAAddState)
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
    public abstract class NFAEdit
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
    public class NFAAddState : NFAEdit
    {
        public int state;
        public NFAAddState(int s)
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
    public class NFAEditState : NFAEdit
    {
        public int state;
        public bool makeFinal;
        public NFAEditState(int state, bool makeFinal)
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
    public class NFAEditMove : NFAEdit
    {
        public int sourceState;
        public int newTargetState;
        public char ch;

        public NFAEditMove(int sourceState, int newTargetState, char ch)
        {
            this.sourceState = sourceState;
            this.newTargetState = newTargetState;
            this.ch = ch;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("delta({0},{2}) should act differently on {1}; ", sourceState, ch, newTargetState);
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("delta({0},{2}) is incorrect; ", sourceState, ch, newTargetState);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    //Edits for Collapse script, when students NFA is correct by too big
    [Serializable()]
    public class NFACollapseStates : NFAEdit
    {
        public int state1;
        public int state2;

        public NFACollapseStates(int state1, int state2)
        {
            this.state1 = state1;
            this.state2 = state2;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("States {0} can be merged with another state.", state1);
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("States {0} can be merged with another state.", state1);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    [Serializable()]
    public class NFARemoveState : NFAEdit
    {
        public int state;
        public NFARemoveState(int s)
        {
            state = s;
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("One state can be removed; ");
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("One state can be removed; ");
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    [Serializable()]
    public class NFARemoveEdge : NFAEdit
    {
        public int state1;
        public int state2;

        public NFARemoveEdge(int state1, int state2)
        {
            this.state1 = state1;
            this.state2 = state2;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("One edge from state {0} can be removed.", state1);
        }

        public override void ToHintString(StringBuilder sb)
        {
            sb.AppendFormat("One edge from state {0} can be removed.", state1);
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
