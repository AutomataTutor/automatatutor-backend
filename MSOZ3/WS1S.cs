using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace MSOZ3
{

    public abstract class WS1SFormula
    {
        internal abstract Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver);

        public abstract void ToString(StringBuilder sb);

        public Automaton<BDD> getDFA(HashSet<char> alphabet, CharSetSolver solver)
        {            
            //Predicate representing the alphabet
            var alphPred = solver.False;
            foreach (var ch in alphabet)
                alphPred = solver.MkOr(solver.MkCharConstraint(false, ch), alphPred);
            
            var dfa1 =  this.Normalize(solver).PushQuantifiers().getDFA(new List<string>(), alphPred, solver);

            var moves = new List<Move<BDD>>();
            foreach (var move in dfa1.GetMoves())
                foreach (var ch in solver.GenerateAllCharacters(solver.MkAnd(move.Label,alphPred),false))                
                    moves.Add(new Move<BDD>(move.SourceState,move.TargetState,solver.MkCharConstraint(false,ch)));

            return Automaton<BDD>.Create(dfa1.InitialState,dfa1.GetFinalStates(),moves,true,true).Determinize(solver).Minimize(solver);
        }

        public Automaton<BDD> getDFA(BDD alphabet, CharSetSolver solver)
        {
            var opt = this.Normalize(solver).PushQuantifiers();
            var dfa1= opt.getDFA(new List<string>(), alphabet, solver);

            var moves = new List<Move<BDD>>();
            foreach (var move in dfa1.GetMoves())
            {
                moves.Add(new Move<BDD>(move.SourceState, move.TargetState, solver.MkAnd(move.Label, alphabet)));                
            }
            return Automaton<BDD>.Create(dfa1.InitialState, dfa1.GetFinalStates(), moves, true, true).Determinize(solver).Minimize(solver);
        }

        public abstract WS1SFormula Normalize(CharSetSolver solver);

        public virtual WS1SFormula PushQuantifiers()
        {
            //Implement for every class
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }
    }

    public class WS1SExists : WS1SFormula
    {
        private static Dictionary<BDD, BDD> hashedSets = new Dictionary<BDD, BDD>();

        String variable;
        WS1SFormula phi;

        public WS1SExists(String variable, WS1SFormula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Automaton<BDD> for formula
            var varCopy = new List<string>(variables);
            varCopy.Insert(0, variable);
            var autPhi = phi.getDFA(varCopy, alphabet, solver);

            //Remove first bit from each move
            var newMoves = new List<Move<BDD>>();
            foreach (var move in autPhi.GetMoves())
            {
                var newCond = solver.LShiftRight(move.Label);
                newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, newCond));
            }

            var dfanew = Automaton<BDD>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves).Determinize(solver);
            var dfamin = dfanew.Minimize(solver);

            return dfamin;
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return new WS1SExists(variable, phi.Normalize(solver));
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class WS1SAnd : WS1SFormula
    {
        WS1SFormula left;
        WS1SFormula right;

        public WS1SAnd(WS1SFormula left, WS1SFormula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            var aut1 = left.getDFA(variables, alphabet,solver);
            var aut2 = right.getDFA(variables, alphabet,solver);
            return aut1.Intersect(aut2, solver).Determinize(solver).Minimize(solver);
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            var ln = left.Normalize(solver);
            var rn = right.Normalize(solver);
            if (ln is WS1SUnaryPred && rn is WS1SUnaryPred)
            {
                var cln = ln as WS1SUnaryPred;
                var crn = rn as WS1SUnaryPred;
                if (cln.set == crn.set)
                    return new WS1SUnaryPred(cln.set, solver.MkAnd(cln.pred, crn.pred));
            }
            else
            {
                if (ln is WS1SFalse || rn is WS1SFalse)
                    return new WS1SFalse();
                if (ln is WS1STrue)
                    return rn;
                if (rn is WS1STrue)
                    return ln;
            }
            return new WS1SAnd(ln,rn);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            sb.Append(" \u028C ");
            right.ToString(sb);
            sb.Append(")");
        }
    }

    public class WS1SOr : WS1SFormula
    {
        WS1SFormula left;
        WS1SFormula right;

        public WS1SOr(WS1SFormula left, WS1SFormula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            var aut1 = left.getDFA(variables, alphabet, solver);
            var aut2 = right.getDFA(variables, alphabet, solver);
            return aut1.Union(aut2, solver).Determinize(solver).Minimize(solver);
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            var ln = left.Normalize(solver);
            var rn = right.Normalize(solver);
            if (ln is WS1SUnaryPred && rn is WS1SUnaryPred)
            {
                var cln = ln as WS1SUnaryPred;
                var crn = rn as WS1SUnaryPred;
                if (cln.set == crn.set)
                    return new WS1SUnaryPred(cln.set, solver.MkOr(cln.pred, crn.pred));
            }
            else{
                if (ln is WS1STrue || rn is WS1STrue)
                    return ln;
                if (ln is WS1SFalse)
                    return rn;
                if (rn is WS1SFalse)
                    return ln;
            }
            return new WS1SOr(ln, rn);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            sb.Append(" | ");
            right.ToString(sb);
            sb.Append(")");
        }
    }

    public class WS1SNot : WS1SFormula
    {
        WS1SFormula phi;

        public WS1SNot(WS1SFormula phi)
        {
            this.phi = phi;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Create condition that only considerst bv of size |variables|
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 7) - 1), variables.Count + 7 - 1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, trueBv) };

            //True automaton and then difference
            var trueAut = Automaton<BDD>.Create(0, new int[] { 0 }, moves);
            var aut = phi.getDFA(variables, alphabet, solver);

            return trueAut.Minus(aut, solver).Determinize(solver).Minimize(solver);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" \u00AC(");
            phi.ToString(sb);
            sb.Append(")");
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            if (phi is WS1SNot)
            {
                return (phi as WS1SNot).phi.Normalize(solver);
            }
            if (phi is WS1SUnaryPred)
            {
                var cphi = phi as WS1SUnaryPred;
                return new WS1SUnaryPred(cphi.set, solver.MkNot(cphi.pred));
            }
            return new WS1SNot(phi.Normalize(solver)); ;
        }
    }

    public class WS1STrue : WS1SFormula
    {
        private static Dictionary<Pair<BDD, int>, Automaton<BDD>> hashedDfa = new Dictionary<Pair<BDD, int>, Automaton<BDD>>();

        public WS1STrue() { }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            var hash = new Pair<BDD, int>(alphabet, variables.Count);
            if (hashedDfa.ContainsKey(hash))
                return hashedDfa[hash];

            //Create condition that only considerst bv of size |variables|
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 7) - 1), variables.Count + 7-1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, trueBv) };
            //True automaton 
            var dfa = Automaton<BDD>.Create(0, new int[] { 0 }, moves);
            hashedDfa[hash] = dfa;

            return dfa;
        }


        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            return WS1STrue.computeDFA(variables, alphabet, solver);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" True ");
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }

    public class WS1SFalse : WS1SFormula
    {

        public WS1SFalse() { }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            return Automaton<BDD>.Empty;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" False ");
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }

    public class WS1SLast : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<Pair<int, int>, Automaton<BDD>>> hashedDfa = new Dictionary<BDD, Dictionary<Pair<int, int>, Automaton<BDD>>>();

        string var1;

        public WS1SLast(string var1) {
            this.var1 = var1;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            int varbit = variables.IndexOf(var1);

            Dictionary<Pair<int, int>, Automaton<BDD>> dic;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

            dic = hashedDfa[alphabet];

            var hash = new Pair<int, int>(variables.Count, varbit);
            if (dic.ContainsKey(hash))
                return dic[hash];

            //Create conditions
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 7) - 1), variables.Count + 7 - 1);
            var posis1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(varbit));
            var posis0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(varbit));

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, posis0),
                new Move<BDD>(0, 1, posis1) 
            };

            var dfa = Automaton<BDD>.Create(0, new int[] { 1 }, moves).Determinize(solver).Minimize(solver);
            dic[hash] = dfa;

            return dfa;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("last(" + var1 + ")");
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }

    public class WS1SFirst : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<Pair<int, int>, Automaton<BDD>>> hashedDfa = new Dictionary<BDD, Dictionary<Pair<int, int>, Automaton<BDD>>>();

        string var1;

        public WS1SFirst(string var1) {
            this.var1 = var1;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            int varbit = variables.IndexOf(var1);

            Dictionary<Pair<int, int>, Automaton<BDD>> dic;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

            dic = hashedDfa[alphabet];

            var hash = new Pair<int, int>(variables.Count, varbit);
            if (dic.ContainsKey(hash))
                return dic[hash];

            //Create conditions
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 7) - 1), variables.Count + 7 - 1);
            var posis1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(varbit));
            var posis0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(varbit));

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 1, posis1),
                new Move<BDD>(1, 1, posis0) 
            };

            var dfa = Automaton<BDD>.Create(0, new int[] { 1 }, moves).Determinize(solver).Minimize(solver);
            dic[hash] = dfa;

            return dfa;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("first(" + var1 + ")");
        }

        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }

    public class WS1SSubset : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>> hashedDfa = new Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>>();
        private static bool hashing = false;

        string set1, set2;

        public WS1SSubset(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set1, string set2)
        {
            var pos1 = variables.IndexOf(set1);
            var pos2 = variables.IndexOf(set2);

            Dictionary<Pair<int, int>, Automaton<BDD>> dic1 = null;
            Pair<int, int> pair = null;
            if (hashing)
            {
                if (!hashedDfa.ContainsKey(alphabet))
                    hashedDfa[alphabet] = new Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>();

                var dic = hashedDfa[alphabet];

                if (!dic.ContainsKey(variables.Count))
                    dic[variables.Count] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

                dic1 = dic[variables.Count];

                pair = new Pair<int, int>(pos1, pos2);

                if (dic1.ContainsKey(pair))
                    return dic1[pair];
            }

            //Create conditions that bit in pos1 is smaller than pos2
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 7) - 1), variables.Count + 7 - 1);
            var pos2is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos2));
            var pos1is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos1));
            var subsetCond = solver.MkOr(pos2is1, pos1is0);


            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, subsetCond) };

            var dfa = Automaton<BDD>.Create(0, new int[] { 0 }, moves).Determinize(solver).Minimize(solver);
            if(hashing)
                dic1[pair] = dfa;
            return dfa;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Build DFA corresponding to regexp
            return computeDFA(variables, alphabet, solver, set1, set2);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " in " + set2 + ")");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }

    public class WS1SSingleton : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<Pair<int, int>, Automaton<BDD>>> hashedDfa = new Dictionary<BDD, Dictionary<Pair<int, int>, Automaton<BDD>>>();
        private static bool hashing = false;

        string set;

        public WS1SSingleton(string set)
        {
            this.set = set;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set)
        {
            int setbit = variables.IndexOf(set);

            Dictionary<Pair<int, int>, Automaton<BDD>> dic;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

            dic = hashedDfa[alphabet];

            var hash = new Pair<int, int>(variables.Count, setbit);
            if (dic.ContainsKey(hash))
                return dic[hash];

            //Create conditions that bit in pos1 is smaller than pos2
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
            var posIs1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(setbit));
            var posIs0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(setbit));

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, posIs0),
                new Move<BDD>(0, 1, posIs1), 
                new Move<BDD>(1, 1, posIs0)
            };

            //Generate the dfa correpsonding to regexp
            var dfa = Automaton<BDD>.Create(0, new int[] { 1 }, moves);
            if(hashing)
                dic[hash] = dfa;
            return dfa;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            return WS1SSingleton.computeDFA(variables, alphabet, solver, this.set);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("Sing[" + set + "]");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }

    public class WS1SUnaryPred : WS1SFormula
    {        
        internal string set;
        internal BDD pred;

        public WS1SUnaryPred(string set, BDD pred)
        {
            this.set = set;
            this.pred = pred;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set, BDD pred)
        {
            int setbit = variables.IndexOf(set);

            //Compute predicates for pos-th bit is 0 or 1
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
            var posIs1 = solver.MkAnd(new BDD[] { trueBv, solver.MkSetWithBitTrue(setbit), solver.ShiftLeft(pred, variables.Count) });
            var posIs0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(setbit));

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, posIs0),
                new Move<BDD>(0, 0, posIs1)
            };

            var dfa = Automaton<BDD>.Create(0, new int[] { 0 }, moves);
            return dfa;
        }


        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            return computeDFA(variables, alphabet, solver, set, pred);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(pred.ToString() + "[" + set + "]");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            return this;
        }
    }   

    public class WS1SSuccN : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, Pair<int, int>>, Automaton<BDD>>>> hashedDfa = new Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, Pair<int, int>>, Automaton<BDD>>>>();
        private static bool hashing = false;

        internal string set1;
        internal string set2;
        internal int n;

        public WS1SSuccN(string set1, string set2, int n)
        {
            this.set1 = set1;
            this.set2 = set2;
            this.n = n;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set1, string set2, int n)
        {
            int pos1 = variables.IndexOf(set1);
            int pos2 = variables.IndexOf(set2);

            Dictionary<int, Dictionary<Pair<int, Pair<int, int>>, Automaton<BDD>>> dic1;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<int, Dictionary<Pair<int, Pair<int, int>>, Automaton<BDD>>>();

            dic1 = hashedDfa[alphabet];

            Dictionary<Pair<int, Pair<int, int>>, Automaton<BDD>> dic2;
            if (!dic1.ContainsKey(variables.Count))
                dic1[variables.Count] = new Dictionary<Pair<int, Pair<int, int>>, Automaton<BDD>>();

            dic2 = dic1[variables.Count];

            var hash = new Pair<int, Pair<int, int>>(pos1, new Pair<int, int>(pos2, n));
            if (dic2.ContainsKey(hash))
                return dic2[hash];

            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
            var pos1is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos1));
            var pos1is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos1));
            var pos2is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos2));
            var pos2is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos2));

            var both0 = solver.MkAnd(new BDD[] { pos1is0, pos2is0 });
            var pos11pos20 = solver.MkAnd(new BDD[] { pos1is1, pos2is0 });
            var pos10pos21 = solver.MkAnd(new BDD[] { pos1is0, pos2is1 });

            //Create automaton for condition
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, both0));
            moves.Add(new Move<BDD>(0, 1, pos11pos20));
            for(int i = 1;i<n;i++){
                moves.Add(new Move<BDD>(i, i+1, both0));
            }
            moves.Add(new Move<BDD>(n, n+1, pos10pos21)); 
            moves.Add(new Move<BDD>(n+1, n+1, both0));

            var dfa = Automaton<BDD>.Create(0, new int[] { n+1 }, moves).Determinize(solver).Minimize(solver);
            if(hashing)
                dic2[hash] = dfa;
            return dfa;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Build DFA corresponding to regexp
            return computeDFA(variables, alphabet, solver, set1, set2, n);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("S(" + set1 + "," + set2 + ")");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            if (set1 == set2)
                return new WS1SFalse();
            else
                return this;
        }
    }

    public class WS1SSucc : WS1SSuccN
    {
        //private static Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>> hashedDfa = new Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>>();
        //private static bool hashing = false;

        public WS1SSucc(string set1, string set2): base(set1,set2,1){}

        //internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set1, string set2)
        //{
        //    int pos1 = variables.IndexOf(set1);
        //    int pos2 = variables.IndexOf(set2);

        //    Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>> dic1;
        //    if (!hashedDfa.ContainsKey(alphabet))
        //        hashedDfa[alphabet] = new Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>();

        //    dic1 = hashedDfa[alphabet];

        //    Dictionary<Pair<int, int>, Automaton<BDD>> dic2;
        //    if (!dic1.ContainsKey(variables.Count))
        //        dic1[variables.Count] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

        //    dic2 = dic1[variables.Count];

        //    var hash = new Pair<int, int>(pos1, pos2);
        //    if (dic2.ContainsKey(hash))
        //        return dic2[hash];

        //    var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
        //    var pos1is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos1));
        //    var pos1is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos1));
        //    var pos2is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos2));
        //    var pos2is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos2));

        //    var both0 = solver.MkAnd(new BDD[] { pos1is0, pos2is0 });
        //    var pos11pos20 = solver.MkAnd(new BDD[] { pos1is1, pos2is0 });
        //    var pos10pos21 = solver.MkAnd(new BDD[] { pos1is0, pos2is1 });

        //    //Create automaton for condition
        //    var moves = new Move<BDD>[] { 
        //        new Move<BDD>(0, 0, both0),
        //        new Move<BDD>(0, 1, pos11pos20), 
        //        new Move<BDD>(1, 2, pos10pos21), 
        //        new Move<BDD>(2, 2, both0), 
        //    };

        //    var dfa = Automaton<BDD>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);
        //    if(has
        //    dic2[hash] = dfa;
        //    return dfa;
        //}

        //internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        //{
        //    //Build DFA corresponding to regexp
        //    return computeDFA(variables, alphabet, solver, set1, set2, );
        //}

        public override void ToString(StringBuilder sb)
        {
            sb.Append("S(" + set1 + "," + set2 + ")");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            if (set1 == set2)
                return new WS1SFalse();
            else
                return this;
        }
    }

    public class WS1SLess : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>> hashedDfa = new Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>>();
        private static bool hashing = false;

        string set1;
        string set2;

        public WS1SLess(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set1, string set2)
        {
            int pos1 = variables.IndexOf(set1);
            int pos2 = variables.IndexOf(set2);

            Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>> dic1;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>();

            dic1 = hashedDfa[alphabet];

            Dictionary<Pair<int, int>, Automaton<BDD>> dic2;
            if (!dic1.ContainsKey(variables.Count))
                dic1[variables.Count] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

            dic2 = dic1[variables.Count];

            var hash = new Pair<int, int>(pos1, pos2);
            if (dic2.ContainsKey(hash))
                return dic2[hash];


            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
            var pos1is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos1));
            var pos1is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos1));
            var pos2is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos2));
            var pos2is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos2));

            var both0 = solver.MkAnd(pos1is0, pos2is0);
            var pos11pos20 = solver.MkAnd(pos1is1, pos2is0);
            var pos10pos21 = solver.MkAnd(pos1is0, pos2is1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, pos11pos20), 
                new Move<BDD>(1, 1, both0),
                new Move<BDD>(1, 2, pos10pos21), 
                new Move<BDD>(2, 2, both0), 
            };
            var dfa = Automaton<BDD>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);
            if(hashing)
                dic2[hash] = dfa;
            return dfa;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Build DFA corresponding to regexp
            return computeDFA(variables, alphabet, solver, set1, set2);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " < " + set2 + ")");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            if (set1 == set2)
                return new WS1SFalse();
            else
                return this;
        }
    }

    public class WS1SLessOrEqual : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>> hashedDfa = new Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>>();
        private static bool hashing = false;

        string set1;
        string set2;

        public WS1SLessOrEqual(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set1, string set2)
        {
            int pos1 = variables.IndexOf(set1);
            int pos2 = variables.IndexOf(set2);

            Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>> dic1;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>();

            dic1 = hashedDfa[alphabet];

            Dictionary<Pair<int, int>, Automaton<BDD>> dic2;
            if (!dic1.ContainsKey(variables.Count))
                dic1[variables.Count] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

            dic2 = dic1[variables.Count];

            var hash = new Pair<int, int>(pos1, pos2);
            if (dic2.ContainsKey(hash))
                return dic2[hash];


            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
            var pos1is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos1));
            var pos1is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos1));
            var pos2is0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(pos2));
            var pos2is1 = solver.MkAnd(trueBv, solver.MkSetWithBitTrue(pos2));

            var both0 = solver.MkAnd(pos1is0, pos2is0);
            var both1 = solver.MkAnd(pos1is1, pos2is1);
            var pos11pos20 = solver.MkAnd(pos1is1, pos2is0);
            var pos10pos21 = solver.MkAnd(pos1is0, pos2is1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 2, both1),
                new Move<BDD>(0, 1, pos11pos20), 
                new Move<BDD>(1, 1, both0),
                new Move<BDD>(1, 2, pos10pos21), 
                new Move<BDD>(2, 2, both0), 
            };
            var dfa = Automaton<BDD>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);
            if(hashing)
                dic2[hash] = dfa;
            return dfa;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Build DFA corresponding to regexp
            return computeDFA(variables, alphabet, solver, set1, set2);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " <= " + set2 + ")");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            if (set1 == set2)
                return new WS1STrue();
            else
                return this;
        }
    }

    public class WS1SEqual : WS1SFormula
    {
        private static Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>> hashedDfa = new Dictionary<BDD, Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>>();
        private static bool hashing = false;

        string set1, set2;

        public WS1SEqual(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal static Automaton<BDD> computeDFA(List<string> variables, BDD alphabet, CharSetSolver solver, string set1, string set2)
        {
            int pos1 = variables.IndexOf(set1);
            int pos2 = variables.IndexOf(set2);

            Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>> dic1;
            if (!hashedDfa.ContainsKey(alphabet))
                hashedDfa[alphabet] = new Dictionary<int, Dictionary<Pair<int, int>, Automaton<BDD>>>();

            dic1 = hashedDfa[alphabet];

            Dictionary<Pair<int, int>, Automaton<BDD>> dic2;
            if (!dic1.ContainsKey(variables.Count))
                dic1[variables.Count] = new Dictionary<Pair<int, int>, Automaton<BDD>>();

            dic2 = dic1[variables.Count];

            var hash = new Pair<int, int>(pos1, pos2);
            if (dic2.ContainsKey(hash))
                return dic2[hash];

            //Create conditions that bit in pos1 is smaller than pos2
            var trueBv = solver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count + 16) - 1), variables.Count + 16 - 1);
            var both1 = solver.MkAnd(new BDD[] { trueBv, solver.MkSetWithBitTrue(pos1), solver.MkSetWithBitTrue(pos2) });
            var both0 = solver.MkAnd(new BDD[] { trueBv, solver.MkSetWithBitFalse(pos1), solver.MkSetWithBitFalse(pos2) });
            var eqCond = solver.MkOr(new BDD[] { both0, both1 });

            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, eqCond) };
            var dfa = Automaton<BDD>.Create(0, new int[] { 0 }, moves).Determinize(solver).Minimize(solver);
            if(hashing)
                dic2[hash] = dfa;
            return dfa;
        }

        internal override Automaton<BDD> getDFA(List<string> variables, BDD alphabet, CharSetSolver solver)
        {
            //Build DFA corresponding to regexp
            return computeDFA(variables, alphabet, solver, set1, set2);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " = " + set2 + ")");
        }
        public override WS1SFormula Normalize(CharSetSolver solver)
        {
            if (set1 == set2)
                return new WS1STrue();
            else
                return this;
        }
    }
}
