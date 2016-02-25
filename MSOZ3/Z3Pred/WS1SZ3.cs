using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

using SFAz3 = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Expr>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

namespace MSOZ3
{
    public class BVConst
    {
        public const uint BVSIZE = 24;
    }


    public abstract class WS1SZ3Formula
    {                        

        internal abstract Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort);

        public abstract void ToString(StringBuilder sb);

        public Automaton<Expr> getAutomata(Z3Provider z3p, Expr universe, Expr var, Sort sort)
        {   //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            var dfapair = this.Normalize().PushQuantifiers().getAutomata(z3p, new List<string>(), universe, var, sort);            

            //Compute the new moves by dropping the last bit of every element in the phiMoves
            var newMoves = Automaton<Expr>.Empty.GetMoves().ToList();
            foreach (var oldMove in dfapair.GetMoves())
            {
                var oldCond = oldMove.Label;                             

                //Compute the new condition as ()
                Expr newCond = oldCond;
                

                //Update the new set of moves
                newMoves.Add(new Move<Expr>(oldMove.SourceState, oldMove.TargetState, newCond));
            }

            //Build the new dfa with the new moves
            var automaton = Automaton<Expr>.Create(dfapair.InitialState, dfapair.GetFinalStates(), newMoves);

            return automaton.Determinize(z3p).Minimize(z3p);
        }

        public virtual WS1SZ3Formula Normalize()
        {
            return this;
        }

        public virtual WS1SZ3Formula PushQuantifiers()
        {
            //Implement for every class
            return this;
        }
    }

    public class WS1SZ3Exists : WS1SZ3Formula
    {
        String variable;
        WS1SZ3Formula phi;

        public WS1SZ3Exists(String variable, WS1SZ3Formula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {            
            //var bit1 = z3p.Z3.MkInt2Bv(1,
            //        z3p.MkInt(1));
            var bit1 = z3p.Z3.MkInt2BV(BVConst.BVSIZE, (IntExpr)z3p.MkInt(1));

            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            //Add the representation of the existential variable to the list of variables
            var newVariables = variables.ToArray().ToList();
            newVariables.Insert(0, variable);

            //Compute the DFA for the formula phi
            var phiDfa = phi.getAutomata(z3p, newVariables, universe, var, sort);

            //Compute the new moves by dropping the last bit of every element in the phiMoves
            var newMoves = Automaton<Expr>.Empty.GetMoves().ToList();
            foreach (var oldMove in phiDfa.GetMoves())
            {
                var oldCond = oldMove.Label;                               

                var t = z3p.MkProj(1,var);
                //Compute the new conditions
                var newCond0 = z3p.ApplySubstitution(oldCond, t,
                        z3p.Z3.MkBVSHL((BitVecExpr)t, (BitVecExpr)bit1));
                var newCond1 = z3p.ApplySubstitution(oldCond, t, 
                    z3p.MkBvAdd(
                        z3p.Z3.MkBVSHL((BitVecExpr)t, (BitVecExpr)bit1),
                        bit1));
                
                //Update the new set of moves
                newMoves.Add(new Move<Expr>(oldMove.SourceState, oldMove.TargetState, z3p.MkOr(z3p.Simplify(newCond0),z3p.Simplify(newCond1))));
            }

            //Build the new dfa with the new moves
            return Automaton<Expr>.Create(phiDfa.InitialState, phiDfa.GetFinalStates(), newMoves);
                //.Determinize(z3p).MinimizeClassical(z3p, int.MaxValue,false);            
        }

        public override WS1SZ3Formula Normalize()
        {
            return new WS1SZ3Exists(variable, phi.Normalize());
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex "+variable+".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class WS1SZ3And : WS1SZ3Formula
    {
        WS1SZ3Formula left;
        WS1SZ3Formula right;

        public WS1SZ3And(WS1SZ3Formula left, WS1SZ3Formula right)
        {            
            this.left = left;
            this.right = right;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            return left.getAutomata(z3p, variables, universe, var, sort).Intersect(right.getAutomata(z3p,variables, universe, var, sort), z3p).Determinize(z3p).Minimize(z3p);
        }

        public override WS1SZ3Formula Normalize()
        {
            return new WS1SZ3And(left.Normalize(), right.Normalize());
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            //sb.Append(")");
            sb.Append(" \u028C ");
            //sb.Append("(");
            right.ToString(sb);
            sb.Append(")");
        }
    }

    public class WS1SZ3Not : WS1SZ3Formula
    {
        WS1SZ3Formula phi;

        public WS1SZ3Not(WS1SZ3Formula phi)
        {
            this.phi = phi;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort,bv);

            //Create the predicate for the universe automaton          
            var pred = z3p.MkBvUlt(z3p.MkProj(1,var),
                z3p.Z3.MkInt2BV(BVConst.BVSIZE,
                    (IntExpr)z3p.MkInt((int)(Math.Pow(2.0,variables.Count)))
                ));
            pred = z3p.MkAnd(pred, universe);

            //Create the automaton with one state and one transition
            var univ = Automaton<Expr>.Create(0,new int[] {0},new Move<Expr>[] {new Move<Expr>(0,0,pred)});

            //Compute the set difference
            return univ.Minus(phi.getAutomata(z3p,variables,universe, var, sort), z3p).Determinize(z3p).Minimize(z3p);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" \u00AC(");
            phi.ToString(sb);
            sb.Append(")");
        }

        public override WS1SZ3Formula Normalize()
        {
            if (phi is WS1SZ3Not)
            {
                return (phi as WS1SZ3Not).phi.Normalize();
            }
            return new WS1SZ3Not(phi.Normalize()); ;
        }
    }

    public class WS1SZ3Subset : WS1SZ3Formula
    {
        string set1, set2;
        
        public WS1SZ3Subset(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            uint set1bit = (uint)variables.IndexOf(set1);
            uint set2bit = (uint)variables.IndexOf(set2);

            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            //Create the predicate for the universe automaton
            var pred = z3p.MkBvUlt(z3p.MkProj(1, var),
                z3p.Z3.MkInt2BV(BVConst.BVSIZE,
                    (IntExpr)z3p.MkInt((int)(Math.Pow(2.0, variables.Count)))
                ));
            pred = z3p.MkAnd(pred, universe);
            var b1 = z3p.MkBvExtract(set1bit,set1bit, z3p.MkProj(1, var));
            var b2 = z3p.MkBvExtract(set2bit, set2bit, z3p.MkProj(1, var));
            pred = z3p.MkAnd(pred, z3p.MkBvUle(b1, b2));

            //Create the automaton with one state and one transition
            var subset = Automaton<Expr>.Create(0, new int[] { 0 }, new Move<Expr>[] { new Move<Expr>(0, 0, pred) });

            //Minimize and output
            return subset;
            //return subset.Minimize(z3p);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 +" in " +set2+ ")");
        }
    }


    public class WS1SZ3Singleton : WS1SZ3Formula
    {
        string set;

        public WS1SZ3Singleton(string set)
        {
            this.set = set;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            uint setbit = (uint)variables.IndexOf(set);

            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            //Create the predicates for the characters with bit 1 and 0 in position setbit
            var pred = z3p.MkBvUlt(z3p.MkProj(1, var),
                z3p.Z3.MkInt2BV(BVConst.BVSIZE,
                    (IntExpr)z3p.MkInt((int)(Math.Pow(2.0, variables.Count)))
                ));
            pred = z3p.MkAnd(pred, universe);
            var bit = z3p.Z3.MkExtract(setbit, setbit, (BitVecExpr)z3p.MkProj(1, var));
            var b0 =z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(0));
            var b1 =z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(1));

            var pred0 = z3p.MkAnd(pred, z3p.MkBvUge(bit, b0));
            pred0 = z3p.MkAnd(pred0, z3p.MkBvUle(bit, b0));
            var pred1 = z3p.MkAnd(pred, z3p.MkBvUge(bit, b1));
            pred1 = z3p.MkAnd(pred1, z3p.MkBvUle(bit, b1));

            //Create the automaton with one state and one transition
            var singleton = Automaton<Expr>.Create(0, new int[] { 1 }, new Move<Expr>[] { 
                new Move<Expr>(0, 0, pred0),
                new Move<Expr>(0, 1, pred1),
                new Move<Expr>(1, 1, pred0)                 
            });

            //Minimize and output
            return singleton;
            //return singleton.Minimize(z3p);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("Sing[" + set + "]");
        }
    }


    public class WS1SZ3Predicate : WS1SZ3Formula
    {
        string set;
        Expr predicate;

        public WS1SZ3Predicate(string set, Expr predicate)
        {
            this.set = set;
            this.predicate = predicate;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            uint setbit = (uint)variables.IndexOf(set);

            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            //Create the predicates for the characters with bit 1 and 0 in position setbit
            var pred = z3p.MkBvUlt(z3p.MkProj(1, var),
                z3p.Z3.MkInt2BV(BVConst.BVSIZE,
                    (IntExpr)z3p.MkInt((int)(Math.Pow(2.0, variables.Count)))
                ));
            pred = z3p.MkAnd(pred, universe);
            var bit = z3p.MkBvExtract(setbit, setbit, z3p.MkProj(1, var));
            var b0 = z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(0));
            var b1 = z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(1));

            var pred0 = z3p.MkAnd(pred, z3p.MkBvUle(bit, b0));            
            var pred1 = z3p.MkAnd(pred, z3p.MkBvUge(bit, b1));
            pred1 = z3p.MkAnd(pred1, predicate);

            //Create the automaton with one state and one transition
            var predAutomata = Automaton<Expr>.Create(0, new int[] { 0 }, new Move<Expr>[] { 
                new Move<Expr>(0, 0, pred0),
                new Move<Expr>(0, 0, pred1)               
            });

            //Minimize and output
            return predAutomata.Determinize(z3p).Minimize(z3p);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(predicate + "[" + set + "]");
        }
    }

    public class WS1SZ3Succ : WS1SZ3Formula
    {
        string set1;
        string set2;

        public WS1SZ3Succ(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            uint set1bit = (uint)variables.IndexOf(set1);
            uint set2bit = (uint)variables.IndexOf(set2);

            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            //Create the predicate for the two singleton sets
            var pred = z3p.MkBvUlt(z3p.MkProj(1, var),
                z3p.Z3.MkInt2BV(BVConst.BVSIZE,
                    (IntExpr)z3p.MkInt((int)(Math.Pow(2.0, variables.Count)))
                ));
            pred = z3p.MkAnd(pred, universe);
            var b1 = z3p.MkBvExtract(set1bit, set1bit, z3p.MkProj(1, var));
            var b2 = z3p.MkBvExtract(set2bit, set2bit, z3p.MkProj(1, var));

            var bit0 = z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(0));
            var bit1 = z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(1));

            var pred0 = z3p.MkAnd(pred, z3p.MkBvUle(b1, bit0));
            pred0 = z3p.MkAnd(pred, z3p.MkBvUle(b2, bit0));
            var pred1 = z3p.MkAnd(pred, z3p.MkBvUge(b1, bit1));
            pred0 = z3p.MkAnd(pred, z3p.MkBvUle(b2, bit0));
            var pred2 = z3p.MkAnd(pred, z3p.MkBvUle(b1, bit0));
            pred0 = z3p.MkAnd(pred, z3p.MkBvUge(b2, bit1));

            //Create the automaton with one state and one transition
            var succ = Automaton<Expr>.Create(0, new int[] { 2 }, new Move<Expr>[] { 
                new Move<Expr>(0, 0, pred0),
                new Move<Expr>(0, 1, pred1),
                new Move<Expr>(1, 2, pred2),
                new Move<Expr>(2, 2, pred0) 
            });

            //Minimize and output
            return succ;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("S(" + set1 + "," + set2 + ")");
        }
    }

    public class WS1SZ3Less : WS1SZ3Formula
    {
        string set1;
        string set2;

        public WS1SZ3Less(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal override Automaton<Expr> getAutomata(Z3Provider z3p, List<string> variables, Expr universe, Expr var, Sort sort)
        {
            uint set1bit = (uint)variables.IndexOf(set1);
            uint set2bit = (uint)variables.IndexOf(set2);

            //Sort for pairs (input theory, BV)
            var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
            var pairSort = z3p.MkTupleSort(sort, bv);

            //Create the predicate for the two singleton sets
            var pred = z3p.MkBvUlt(z3p.MkProj(1, var),
                z3p.Z3.MkInt2BV(BVConst.BVSIZE,
                    (IntExpr)z3p.MkInt((int)(Math.Pow(2.0, variables.Count)))
                ));
            pred = z3p.MkAnd(pred, universe);
            var b1 = z3p.MkBvExtract(set1bit, set1bit, z3p.MkProj(1, var));
            var b2 = z3p.MkBvExtract(set2bit, set2bit, z3p.MkProj(1, var));

            var bit0 = z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(0));
            var bit1 = z3p.Z3.MkInt2BV(1,
                    (IntExpr)z3p.MkInt(1));

            var pred0 = z3p.MkAnd(pred, z3p.MkBvUle(b1, bit0));
            pred0 = z3p.MkAnd(pred, z3p.MkBvUle(b2, bit0));
            var pred1 = z3p.MkAnd(pred, z3p.MkBvUge(b1, bit1));
            pred0 = z3p.MkAnd(pred, z3p.MkBvUle(b2, bit0));
            var pred2 = z3p.MkAnd(pred, z3p.MkBvUle(b1, bit0));
            pred0 = z3p.MkAnd(pred, z3p.MkBvUge(b2, bit1));

            //Create the automaton with one state and one transition
            var less = Automaton<Expr>.Create(0, new int[] { 2 }, new Move<Expr>[] { 
                new Move<Expr>(0, 0, pred0),
                new Move<Expr>(0, 1, pred1),
                new Move<Expr>(1, 1, pred0),
                new Move<Expr>(1, 2, pred2),
                new Move<Expr>(2, 2, pred0) 
            });

            //Minimize and output
            return less;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " < " + set2 + ")");
        }
    }
}
