using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Z3;

using MSOZ3;

namespace AutomataPDL
{

    public abstract class PDLPred : PDL
    {
        public override string GetNodeName()
        {
            return "pr:" + name;
        }

        public abstract MSOFormula ToMSO(FreshGen fg);

        public virtual MSOFormula ToMSO()
        {
            return this.ToMSO(new FreshGen());
        }

        public virtual bool IsEquivalentWith(PDLPred phi, HashSet<Char> alphabet, CharSetSolver solver)
        {
            var p1 = new PDLIff(phi, this);
            var dfa = p1.GetDFA(alphabet, solver);
            return !dfa.IsEmpty;
        }

        public virtual bool Issatisfiable(HashSet<Char> alphabet, CharSetSolver solver)
        {
            var dfa = GetDFA(alphabet, solver);
            return !dfa.IsEmpty;
        }

        /// <summary>
        /// Compute the DFA corresponding to the PDLpred, null if it can't find it
        /// </summary>
        /// <param name="alphabet">DFA alphabet</param>
        /// <param name="solver">Char solver</param>
        /// <returns>the DFA corresponding to the PDLpred, null if it can't find it</returns>
        public virtual Automaton<BDD> GetDFA(HashSet<Char> alphabet, CharSetSolver solver)
        {
            try
            {
                return ToMSO(new FreshGen()).getDFA(alphabet, solver);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public abstract bool Eval(string str, Dictionary<string, int> A);// A represents the assignment

        public abstract int CompareTo(PDLPred pred);

        public abstract CPDLPred GetCPDL();
    }

    public class PDLAtPos : PDLPred, IMatchable<char,PDLPos>
    {
        internal char label;
        internal PDLPos pos;

        char IMatchable<char,PDLPos>.GetArg1() { return label; }
        PDLPos IMatchable<char, PDLPos>.GetArg2() { return pos; }
        
        public PDLAtPos(char label, PDLPos p)
        {
            this.label = label;
            this.pos = p;
            name = "atPos";
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            int l = pos.Eval(str, A);
            return (l >= 0) && (l < str.Length) && (str[l] == label);
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            int c = fg.get();
            string x = "_x_" + c.ToString();
            return new MSOExistsFO(x, new MSOAnd(pos.ToMSO(fg, x), new MSOLabel(x, label)));
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLAtPos;
            if (pp != null)
            {
                var vlab = label.CompareTo(pp.label);
                return (vlab == 0) ? pos.CompareTo(pp.pos) : vlab;
            }
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(label + " @ ");
            pos.ToString(sb);
        }

        public override bool ContainsVar(String vName)
        {
            return pos.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() +":"+ index, new Pair<PDL,string>(this,""));            
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pos.GetNodeName(), index, x1));
            int x2 = fg.get();
            nodes.Add(label + ":" + x2, new Pair<PDL, string>(this, "label"));
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), label, index, x2));
            pos.ToTreeString(fg, sb, x1, nodes);
        }
        public override int GetFormulaSize()
        {
            return pos.GetFormulaSize() + 2;
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(pos);
        }
        public override bool IsComplex()
        {
            return pos.IsComplex();
        }

        public override CPDLPred GetCPDL()
        {
            CPDLChar character = new CPDLChar(this.label);
            CPDLPos position = this.pos.GetCPDL();
            return new CPDLAtPosPred(character, position);
        }

        public override bool Equals(object obj)
        {
            PDLAtPos other = obj as PDLAtPos;
            if (other == null) { return false; }
            if (this.label != other.label) { return false; }
            if (!this.pos.Equals(other.pos)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.label.GetHashCode();
            hashCode = (hashCode * 31) + this.pos.GetHashCode();
            return hashCode;
        }
    }

    public class PDLAtSet : PDLPred, IMatchable<char, PDLSet>
    {
        internal char label;//label
        internal PDLSet set;

        char IMatchable<char, PDLSet>.GetArg1() { return label; }
        PDLSet IMatchable<char, PDLSet>.GetArg2() { return set; }

        public PDLAtSet(char label, PDLSet set)
        {
            this.label = label;
            this.set = set;
            name = "atSet";
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            int s = set.Eval(str, A);
            for (int i = 0; i < str.Length; i++)
            {
                if ((((s >> i) & 1) == 1) && (str[i] != label))
                    return false;
            }
            return true;
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            //forall x, if set.contains(x) then l @ x
            int c = fg.get();
            string x = "_x_" + c.ToString();
            return new MSOForallFO(x, new MSOIf(set.Contains(fg, x), new MSOLabel(x, label)));
        }

        public override bool ContainsVar(string vName)
        {
            return set.ContainsVar(vName);
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLAtSet;
            if (pp != null)
                return set.CompareTo(pp.set);

            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(label + " @ ");
            set.ToString(sb);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, ""));            
            int x1 = fg.get();            
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), set.GetNodeName(), index, x1));
            int x2 = fg.get();
            nodes.Add(label + ":" + x2, new Pair<PDL, string>(this, "label"));      
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), label, index, x2));
            set.ToTreeString(fg,sb, x1, nodes);
        }
        public override int GetFormulaSize()
        {
            return 2 + set.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.set);
        }
        public override bool IsComplex()
        {
            return set.IsComplex();
        }

        public override CPDLPred GetCPDL()
        {
            CPDLChar character = new CPDLChar(this.label);
            CPDLSet set = this.set.GetCPDL();
            return new CPDLAtSetPred(character, set);
        }

        public override bool Equals(object obj)
        {
            PDLAtSet other = obj as PDLAtSet;
            if (other == null) { return false; }
            if (this.label != other.label) { return false; }
            if (!this.set.Equals(other.set)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.label.GetHashCode();
            hashCode = (hashCode * 31) + this.set.GetHashCode();
            return hashCode;
        }
    }

    public class PDLBelongs : PDLPred, IMatchable<PDLPos, PDLSet>
    {
        internal PDLPos pos;
        internal PDLSet set;

        PDLPos IMatchable<PDLPos, PDLSet>.GetArg1() { return pos; }
        PDLSet IMatchable<PDLPos, PDLSet>.GetArg2() { return set; }

        public PDLBelongs(PDLPos p, PDLSet S)
        {
            this.pos = p;
            this.set = S;
            this.name = "belongs";
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            int l = pos.Eval(str, A);
            return (l >= 0) && (l < str.Length) && (BitVecUtil.GetIthBit(set.Eval(str, A), l) == 1);
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            int c = fg.get();
            string X = "_X_" + c.ToString();
            string x = "_x_" + c.ToString();
            return new MSOExistsSO(X, new MSOAnd(set.ToMSO(fg,X),
                new MSOExistsFO(x, new MSOAnd(new MSOBelong(x, X), pos.ToMSO(fg,x)))));
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLBelongs;
            if (pp != null)
            {
                var v1 = pos.CompareTo(pp.pos);
                return (v1 == 0) ? set.CompareTo(pp.set) : v1;
            }
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            pos.ToString(sb);
            sb.Append(" belTo ");
            set.ToString(sb);
        }

        public override bool ContainsVar(String vName)
        {
            return pos.ContainsVar(vName) || set.ContainsVar(vName);
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pos.GetNodeName(), index, x1));
            int x2 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), set.GetNodeName(), index, x2));
            pos.ToTreeString(fg,sb, x1, nodes);
            set.ToTreeString(fg,sb, x2, nodes);
        }
        public override int GetFormulaSize()
        {
            return 1 + pos.GetFormulaSize() + set.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this); 
            set.Add(this.pos);
            set.Add(this.set);
        }
        public override bool IsComplex()
        {
            return true;
        }

        public override CPDLPred GetCPDL()
        {
            CPDLPos position = this.pos.GetCPDL();
            CPDLSet set = this.set.GetCPDL();
            return new CPDLBelongs(position, set);
        }

        public override bool Equals(object obj)
        {
            PDLBelongs other = obj as PDLBelongs;
            if(other == null) { return false; }
            if(!this.pos.Equals(other.pos)) { return false; }
            if(!this.set.Equals(other.set)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.pos.GetHashCode();
            hashCode = (hashCode * 31) + this.set.GetHashCode();
            return hashCode;
        }
    }

    public class PDLSubset : PDLPred, IMatchable<PDLSet, PDLSet>
    {
        internal PDLSet set1;
        internal PDLSet set2;

        PDLSet IMatchable<PDLSet, PDLSet>.GetArg1() { return set1; }
        PDLSet IMatchable<PDLSet, PDLSet>.GetArg2() { return set2; }

        public PDLSubset(PDLSet set1, PDLSet set2)
        {
            this.set1 = set1;
            this.set2 = set2;
            this.name = "subset";
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            throw new NotImplementedException();
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            int c1 = fg.get();
            int c2 = fg.get();
            string X1 = "_X_" + c1.ToString();
            string X2 = "_X_" + c2.ToString();
            return new MSOExistsSO(X1, new MSOAnd(set1.ToMSO(fg, X1),
                new MSOExistsSO(X2, new MSOAnd(set2.ToMSO(fg, X2),
                        new MSOSubset(X1, X2)))));
        }

        public override int CompareTo(PDLPred pred)
        {
            throw new NotImplementedException();
        }

        public override void ToString(StringBuilder sb)
        {
            set1.ToString(sb);
            sb.Append(" sub ");
            set2.ToString(sb);
        }

        public override bool ContainsVar(String vName)
        {
            return set1.ContainsVar(vName) || set2.ContainsVar(vName);
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            throw new NotImplementedException();
        }
        public override int GetFormulaSize()
        {
            return 1 + set1.GetFormulaSize() + set2.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.set1);
            set.Add(this.set2);
        }
        public override bool IsComplex()
        {
            return true;
        }
        public override CPDLPred GetCPDL()
        {
            CPDLSet set1 = this.set1.GetCPDL();
            CPDLSet set2 = this.set2.GetCPDL();
            return new CPDLSubsetPred(set1, set2);
        }

        public override bool Equals(object obj)
        {
            PDLSubset other = obj as PDLSubset;
            if (other == null) { return false; }
            if (!this.set1.Equals(other.set1)) { return false; }
            if (!this.set2.Equals(other.set2)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.set1.GetHashCode();
            hashCode = (hashCode * 31) + this.set2.GetHashCode();
            return hashCode;
        }
    }

    #region PDLBinaryPosFormula
    public class PDLBinaryPosFormula : PDLPred, IMatchable<PDLPos, PDLPos, PDLPosComparisonOperator>
    {
        internal PDLPos pos1;
        internal PDLPos pos2;
        internal PDLPosComparisonOperator op;

        PDLPos IMatchable<PDLPos, PDLPos, PDLPosComparisonOperator>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos, PDLPosComparisonOperator>.GetArg2() { return pos2; }
        PDLPosComparisonOperator IMatchable<PDLPos, PDLPos, PDLPosComparisonOperator>.GetArg3() { return op; }


        public PDLBinaryPosFormula(PDLPos p1, PDLPos p2, PDLPosComparisonOperator op)
        {
            this.pos1 = p1;
            this.pos2 = p2;
            this.op = op;
            switch (op)
            {
                case PDLPosComparisonOperator.Eq: name = "="; break;
                case PDLPosComparisonOperator.Ge: name = ">"; break;
                case PDLPosComparisonOperator.Geq: name = ">="; break;
                case PDLPosComparisonOperator.Le: name = "<"; break;
                case PDLPosComparisonOperator.Leq: name = "<="; break;
                case PDLPosComparisonOperator.Pred: name = "IsPred"; break;
                case PDLPosComparisonOperator.Succ: name = "IsSucc"; break;
                default: throw new PDLException("Undefined operator");
            }
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            int l1 = pos1.Eval(str, A);
            int l2 = pos2.Eval(str, A);

            switch (op)
            {
                case PDLPosComparisonOperator.Eq: return (l1 == l2) && (l1 >= 0) && (l1 < str.Length);
                case PDLPosComparisonOperator.Ge: return (l1 > l2) && (l2 >= 0) && (l1 <= str.Length);
                case PDLPosComparisonOperator.Geq: return (l1 >= l2) && (l2 >= 0) && (l1 < str.Length);
                case PDLPosComparisonOperator.Le: return (l1 < l2) && (l1 >= 0) && (l2 <= str.Length);
                case PDLPosComparisonOperator.Leq: return (l1 <= l2) && (l1 >= 0) && (l2 < str.Length);
                case PDLPosComparisonOperator.Pred: return (l1 == l2 - 1) && (l1 >= 0) && (l2 < str.Length);
                case PDLPosComparisonOperator.Succ: return (l1 == l2 + 1) && (l2 >= 0) && (l1 < str.Length);
                default: throw new PDLException("Undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            switch (op)
            {
                case PDLPosComparisonOperator.Eq:
                    {
                        int c = fg.get();
                        string x = "_x_" + c.ToString();

                        MSOFormula m1 = pos1.ToMSO(fg, x);
                        MSOFormula m2 = pos2.ToMSO(fg, x);

                        return new MSOExistsFO(x, new MSOAnd(m1, m2));
                    }
                case PDLPosComparisonOperator.Ge: return new PDLNot(new PDLPosLeq(pos1, pos2)).ToMSO(fg);
                case PDLPosComparisonOperator.Geq: return new PDLNot(new PDLPosLe(pos1, pos2)).ToMSO(fg);
                case PDLPosComparisonOperator.Le:
                    {
                        int c = fg.get();
                        string x1 = "_x1_" + c.ToString();
                        string x2 = "_x2_" + c.ToString();

                        MSOFormula m1 = pos1.ToMSO(fg, x1);
                        MSOFormula m2 = pos2.ToMSO(fg, x2);
                        return new MSOExistsFO(x1, new MSOAnd(m1, new MSOExistsFO(x2, new MSOAnd(m2, new MSOLess(x1, x2)))));
                    }
                case PDLPosComparisonOperator.Leq: return new PDLPosLe(pos1, new PDLSuccessor(pos2)).ToMSO(fg);
                case PDLPosComparisonOperator.Pred: return new PDLIsSuccessor(pos2, pos1).ToMSO(fg);
                case PDLPosComparisonOperator.Succ:
                    {
                        int c = fg.get();
                        string x1 = "_x1_" + c.ToString();
                        string x2 = "_x2_" + c.ToString();
                        return new MSOExistsFO(x1, new MSOAnd(pos1.ToMSO(fg, x1), new MSOExistsFO(x2,
                            new MSOAnd(pos2.ToMSO(fg, x2), new MSOSucc(x1, x2)))));
                    }
                default: throw new PDLException("Undefined operator");
            }
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLBinaryPosFormula;
            if (pp != null && name == pred.name)
            {
                if (PDLEnumUtil.IsSymmetric(op))
                {
                    var v21 = pos1.CompareTo(pp.pos2);
                    var v22 = pos2.CompareTo(pp.pos1);
                    if ((v21 == 0 && v22 == 0))
                        return 0;
                }

                var v11 = pos1.CompareTo(pp.pos1);
                var v12 = pos2.CompareTo(pp.pos2);
                return (v11 == 0) ? v12 : v11;
            }
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case PDLPosComparisonOperator.Eq: pos1.ToString(sb); sb.Append(" = "); pos2.ToString(sb);  break;
                case PDLPosComparisonOperator.Ge: pos1.ToString(sb); sb.Append(" > "); pos2.ToString(sb);  break;
                case PDLPosComparisonOperator.Geq: pos1.ToString(sb); sb.Append(" >= "); pos2.ToString(sb);  break;
                case PDLPosComparisonOperator.Le: pos1.ToString(sb); sb.Append(" < "); pos2.ToString(sb);  break;
                case PDLPosComparisonOperator.Leq: pos1.ToString(sb); sb.Append(" <= "); pos2.ToString(sb); break;
                case PDLPosComparisonOperator.Pred: pos1.ToString(sb); sb.Append(", "); pos2.ToString(sb);  break;
                case PDLPosComparisonOperator.Succ: pos1.ToString(sb); sb.Append(", "); pos2.ToString(sb);  break;
                default: throw new PDLException("Undefined operator");
            }
        }

        public override bool ContainsVar(String vName)
        {
            return pos1.ContainsVar(vName) || pos2.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pos1.GetNodeName(), index, x1));
            int x2 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pos2.GetNodeName(), index, x2));
            pos1.ToTreeString(fg, sb, x1, nodes);
            pos2.ToTreeString(fg, sb, x2, nodes);
        }

        public override int GetFormulaSize()
        {
            return pos1.GetFormulaSize() + pos2.GetFormulaSize() + 1;
        }

        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.pos1);
            set.Add(this.pos2);
        }
        public override bool IsComplex()
        {
            return true;
        }

        public override CPDLPred GetCPDL()
        {
            CPDLPos lhs = this.pos1.GetCPDL();
            CPDLPos rhs = this.pos2.GetCPDL();
            return new CPDLBinaryPosPred(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            PDLBinaryPosFormula other = obj as PDLBinaryPosFormula;
            if (other == null) { return false; }
            if (!this.pos1.Equals(other.pos1)) { return false; }
            if (!this.pos2.Equals(other.pos2)) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.pos1.GetHashCode();
            hashCode = (hashCode * 31) + this.pos2.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLPosEq : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLPosEq(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Eq) { }
    }

    public class PDLPosLe : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLPosLe(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Le) { }
    }

    public class PDLPosLeq : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLPosLeq(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Leq) { }
    }

    public class PDLPosGe : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLPosGe(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Ge) { }
    }

    public class PDLPosGeq : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLPosGeq(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Geq) { }
    }

    public class PDLIsPredecessor : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLIsPredecessor(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Pred) { }
    }

    public class PDLIsSuccessor : PDLBinaryPosFormula, IMatchable<PDLPos, PDLPos>
    {
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg1() { return pos1; }
        PDLPos IMatchable<PDLPos, PDLPos>.GetArg2() { return pos2; }
        public PDLIsSuccessor(PDLPos p1, PDLPos p2) : base(p1, p2, PDLPosComparisonOperator.Succ) { }
    }
    #endregion

    #region PDLSetModuleComparison
    public class PDLSetModuleComparison : PDLPred, IMatchable<PDLSet, int, int, PDLComparisonOperator>// |S|(mod m) CMP n
    {
        internal PDLSet set;
        internal int m;
        internal int n;
        internal PDLComparisonOperator op;

        PDLSet IMatchable<PDLSet, int, int, PDLComparisonOperator>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, int, PDLComparisonOperator>.GetArg2() { return m; }
        int IMatchable<PDLSet, int, int, PDLComparisonOperator>.GetArg3() { return n; }
        PDLComparisonOperator IMatchable<PDLSet, int, int, PDLComparisonOperator>.GetArg4() { return op; }

        public PDLSetModuleComparison(PDLSet S, int m, int n, PDLComparisonOperator op)
        {
            this.set = S;
            this.m = m;
            this.n = n;
            this.op = op;
            switch (op)
            {
                case PDLComparisonOperator.Eq: name = "mod="; break;
                case PDLComparisonOperator.Ge: name = "mod>"; break;
                case PDLComparisonOperator.Geq: name = "mod>="; break;
                case PDLComparisonOperator.Le: name = "mod<"; break;
                case PDLComparisonOperator.Leq: name = "mod<="; break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            var size = BitVecUtil.CountBits(set.Eval(str, A), 0, str.Length) % m;
            switch (op)
            {
                case PDLComparisonOperator.Eq: return size == n;
                case PDLComparisonOperator.Ge: return size > n;
                case PDLComparisonOperator.Geq: return size >= n;
                case PDLComparisonOperator.Le: return size < n;
                case PDLComparisonOperator.Leq: return size <= n;
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            switch (op)
            {
                case PDLComparisonOperator.Eq:
                    {
                        #region MSO eq
                        int c = fg.get();
                        string x = "x"; //"_x_"+c.ToString();
                        string y = "y";
                        string z = "z";
                        string X = "X"; //"_X_"+c.ToString()+"_";
                        string Xs = "_X_" + c.ToString();
                        string w = "_w_" + c.ToString();

                        StringBuilder sb = new StringBuilder();

                        MSOFormula part = null;//part should say X0....X{m-1} is a covering of Xs
                        for (int i = 0; i < m; i++)
                        {
                            var disjunct = new MSOBelong(x, X + i.ToString());
                            if (part == null)
                                part = disjunct;
                            else
                                part = new MSOOr(part, disjunct);
                        }
                        if (part == null)
                            part = new MSOTrue();
                        else
                            part = new MSOForallFO(x, new MSOIff(new MSOBelong(x, Xs), part));


                        MSOFormula disj = null; // X0...X_{m-1} are disjoint
                        for (int i = 0; i < m; i++)
                        {
                            for (int j = i + 1; j < m; j++)
                            {
                                var disjunct = new MSONot(new MSOAnd(new MSOBelong(x, X + i.ToString()),
                                        new MSOBelong(x, X + j.ToString())));
                                if (disj == null)
                                    disj=disjunct;
                                else
                                    disj = new MSOAnd(disj, disjunct);
                            }
                        }
                        if (disj == null)
                            disj = new MSOTrue();

                        disj = new MSOForallFO(x, disj);

                        MSOFormula init; // First position of X is in X1
                        init =
                            new MSOForallFO(x, new MSOIf(new MSOAnd(new MSOBelong(x, Xs),
                                new MSOForallFO(y, new MSOIf(new MSOLess(y, x), new MSONot(new MSOBelong(y, Xs))))),
                                    new MSOBelong(x, X + 1.ToString())));

                        MSOFormula trans =null; // if x \in Xi and y is succ of x in X then y in X{i+1}
                        for (int i = m - 1; i >= 0; i--)
                        {
                            var d = new MSOForallFO(x, new MSOIf(new MSOBelong(x, X + i.ToString()),
                                new MSOForallFO(y, new MSOIf(new MSOAnd(new MSOAnd(new MSOBelong(y, Xs), new MSOLess(x, y)),
                                    new MSOForallFO(z, new MSOIf(new MSOAnd(new MSOLess(z, y), new MSOLess(x, z)),
                                        new MSONot(new MSOBelong(z, Xs))))),
                                        new MSOBelong(y, X + ((i + 1) % m).ToString())))));
                            if (trans == null)
                                trans = d;
                            else
                                trans = new MSOAnd(trans, d);
                        }

                        MSOFormula final;// last position in X should be in Xn
                        final =
                            new MSOForallFO(x, new MSOIf(new MSOAnd(new MSOBelong(x, Xs),
                                new MSOForallFO(y, new MSOIf(new MSOLess(x, y), new MSONot(new MSOBelong(y, Xs))))), new MSOBelong(x, X + n.ToString())));


                        MSOFormula ret = new MSOAnd(part, new MSOAnd(disj, new MSOAnd(init, new MSOAnd(trans, final))));

                        for (int i = m - 1; i >= 0; i--)
                        {
                            ret = new MSOExistsSO(X + i.ToString(), ret);
                        }

                        ret = new MSOExistsSO(Xs, new MSOAnd(set.ToMSO(fg,Xs), ret));

                        // ret as it is accepts the empty set, which we dont want if n>0 so the following
                        if (n > 0)
                            ret = new MSOAnd(ret, new MSOExistsFO(w, set.Contains(fg, w)));

                        return ret;
                        #endregion
                    }

                case PDLComparisonOperator.Ge: return new PDLNot(new PDLModSetLe(set, m, n + 1)).ToMSO(fg);
                case PDLComparisonOperator.Geq: return new PDLNot(new PDLModSetLe(set, m, n)).ToMSO(fg);
                case PDLComparisonOperator.Le:
                    {
                        #region MSO le
                        int c = fg.get();
                        string x = "x"; //"_x_"+c.ToString();
                        string y = "y";
                        string z = "z";
                        string X = "X"; //"_X_"+c.ToString()+"_";
                        string Xs = "_X_" + c.ToString();

                        StringBuilder sb = new StringBuilder();

                        if (n < 1)
                            return new MSOFalse();

                        MSOFormula part = new MSOBelong(x, X + 0.ToString());//part should say X0....X{m-1} is a covering of Xs
                        for (int i = 1; i < m; i++)
                        {
                            part = new MSOOr(part, new MSOBelong(x, X + i.ToString()));
                        }
                        part = new MSOAnd(
                            new MSOForallFO(x, new MSOIf(new MSOBelong(x, Xs), part)),
                            new MSOForallFO(x, new MSOIf(part, new MSOBelong(x, Xs))));


                        MSOFormula disj = null; // X0...X_{m-1} are disjoint
                        for (int i = 0; i < m; i++)
                        {
                            for (int j = i + 1; j < m; j++)
                            {
                                if (disj == null)
                                {
                                    disj = new MSONot(new MSOAnd(new MSOBelong(x, X + i.ToString()),
                                        new MSOBelong(x, X + j.ToString())));
                                }
                                else
                                {
                                    disj = new MSOAnd(disj, new MSONot(new MSOAnd(new MSOBelong(x, X + i.ToString()),
                                        new MSOBelong(x, X + j.ToString()))));
                                }
                            }
                        }
                        if (disj == null)
                            disj = new MSOTrue();
                        else
                            disj = new MSOForallFO(x, disj);

                        MSOFormula init; // First position of X is in X1
                        init =
                            new MSOForallFO(x, new MSOIf(new MSOAnd(new MSOBelong(x, Xs),
                                new MSOForallFO(y, new MSOIf(new MSOLess(y, x), new MSONot(new MSOBelong(y, Xs))))),
                                    new MSOBelong(x, X + 1.ToString())));

                        MSOFormula trans; // if x \in Xi and y is succ of x in X then y in X{i+1}
                        trans = new MSOForallFO(x, new MSOIf(new MSOBelong(x, X + (m - 1).ToString()),
                                new MSOForallFO(y, new MSOIf(new MSOAnd(new MSOAnd(new MSOBelong(y, Xs), new MSOLess(x, y)),
                                    new MSOForallFO(z, new MSOIf(new MSOAnd(new MSOLess(z, y), new MSOLess(x, z)),
                                        new MSONot(new MSOBelong(z, Xs))))),
                                        new MSOBelong(y, X + 0.ToString())))));

                        for (int i = m - 2; i >= 0; i--)
                        {
                            trans = new MSOAnd(trans, new MSOForallFO(x, new MSOIf(new MSOBelong(x, X + i.ToString()),
                                new MSOForallFO(y, new MSOIf(new MSOAnd(new MSOAnd(new MSOBelong(y, Xs), new MSOLess(x, y)),
                                    new MSOForallFO(z, new MSOIf(new MSOAnd(new MSOLess(z, y), new MSOLess(x, z)),
                                        new MSONot(new MSOBelong(z, Xs))))),
                                        new MSOBelong(y, X + (i + 1).ToString()))))));
                        }

                        MSOFormula final;// last position in X should be in X1...X{n-1}

                        MSOFormula dest = new MSOBelong(x, X + 0.ToString());
                        for (int i = 1; i < n; i++)
                            dest = new MSOOr(dest, new MSOBelong(x, X + i.ToString()));

                        final =
                            new MSOForallFO(x, new MSOIf(new MSOAnd(new MSOBelong(x, Xs),
                                new MSOForallFO(y, new MSOIf(new MSOLess(x, y), new MSONot(new MSOBelong(y, Xs))))), dest));

                        MSOFormula ret = new MSOAnd(part, new MSOAnd(disj, new MSOAnd(init, new MSOAnd(trans, final))));

                        for (int i = m - 1; i >= 0; i--)
                        {
                            ret = new MSOExistsSO(X + i.ToString(), ret);
                        }

                        ret = new MSOExistsSO(Xs, new MSOAnd(set.ToMSO(fg,Xs), ret));

                        return ret;
                        #endregion
                    }

                case PDLComparisonOperator.Leq: return new PDLModSetLe(set, m, n + 1).ToMSO(fg);
                default: throw new PDLException("undefined operator");
            }
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLSetModuleComparison;
            if (pp != null && name == pred.name)
            {
                var v1 = set.CompareTo(pp.set);
                return (v1 == 0) ? ((n - pp.n == 0) ? m - pp.m : n - pp.n) : v1;
            }
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("|");
            set.ToString(sb);
            sb.Append("| % " + m);
            switch (op)
            {
                case PDLComparisonOperator.Eq: sb.Append("="); break;
                case PDLComparisonOperator.Ge: sb.Append(">"); break;
                case PDLComparisonOperator.Geq: sb.Append(">="); break;
                case PDLComparisonOperator.Le: sb.Append("<"); break;
                case PDLComparisonOperator.Leq: sb.Append("<="); break;
                default: throw new PDLException("undefined operator");
            }
            sb.Append(n);
        }

        public override bool ContainsVar(String vName)
        {
            return set.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), set.GetNodeName(), index, x1));
            int x2 = fg.get();
            nodes.Add(m + ":" + x2, new Pair<PDL, string>(this, "m")); 
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), m, index, x2));
            int x3 = fg.get();
            nodes.Add(n+":"+x3, new Pair<PDL, string>(this, "n")); 
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), n, index, x3));
            set.ToTreeString(fg, sb, x1, nodes);
        }

        public override int GetFormulaSize()
        {
            return 3 + set.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.set);
        }
        public override bool IsComplex()
        {
            return set.IsComplex();
        }

        public override CPDLPred GetCPDL()
        {
            CPDLSet set = this.set.GetCPDL();
            CPDLInteger n = new CPDLInteger(this.n);
            CPDLInteger m = new CPDLInteger(this.m, false);
            return new CPDLSetCardinalityModule(set, n, m);
        }

        public override bool Equals(object obj)
        {
            PDLSetModuleComparison other = obj as PDLSetModuleComparison;
            if (other == null) { return false; }
            if (!this.set.Equals(other.set)) { return false; }
            if (this.m != other.m) { return false; }
            if (this.n != other.n) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.set.GetHashCode();
            hashCode = (hashCode * 31) + this.m.GetHashCode();
            hashCode = (hashCode * 31) + this.n.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLModSetEq : PDLSetModuleComparison, IMatchable<PDLSet, int, int> // |S|(mod m) = n
    {
        PDLSet IMatchable<PDLSet, int, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, int>.GetArg2() { return m; }
        int IMatchable<PDLSet, int, int>.GetArg3() { return n; }
        public PDLModSetEq(PDLSet S, int m, int n) : base(S, m, n, PDLComparisonOperator.Eq) { }
    }

    public class PDLModSetGe : PDLSetModuleComparison, IMatchable<PDLSet, int, int> // |S|(mod m) = n
    {
        PDLSet IMatchable<PDLSet, int, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, int>.GetArg2() { return m; }
        int IMatchable<PDLSet, int, int>.GetArg3() { return n; }
        public PDLModSetGe(PDLSet S, int m, int n) : base(S, m, n, PDLComparisonOperator.Ge) { }
    }

    public class PDLModSetGeq : PDLSetModuleComparison, IMatchable<PDLSet, int, int> // |S|(mod m) = n
    {
        PDLSet IMatchable<PDLSet, int, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, int>.GetArg2() { return m; }
        int IMatchable<PDLSet, int, int>.GetArg3() { return n; }
        public PDLModSetGeq(PDLSet S, int m, int n) : base(S, m, n, PDLComparisonOperator.Geq) { }
    }

    public class PDLModSetLe : PDLSetModuleComparison, IMatchable<PDLSet, int, int> // |S|(mod m) = n
    {
        PDLSet IMatchable<PDLSet, int, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, int>.GetArg2() { return m; }
        int IMatchable<PDLSet, int, int>.GetArg3() { return n; }
        public PDLModSetLe(PDLSet S, int m, int n) : base(S, m, n, PDLComparisonOperator.Le) { }
    }

    public class PDLModSetLeq : PDLSetModuleComparison, IMatchable<PDLSet, int, int> // |S|(mod m) = n
    {
        PDLSet IMatchable<PDLSet, int, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, int>.GetArg2() { return m; }
        int IMatchable<PDLSet, int, int>.GetArg3() { return n; }
        public PDLModSetLeq(PDLSet S, int m, int n) : base(S, m, n, PDLComparisonOperator.Leq) { }
    }
    #endregion

    #region Set Cardinality operations
    //Index between [7, 11]
    public class PDLSetCardinality : PDLPred, IMatchable<PDLSet, int, PDLComparisonOperator> // |S| = n
    {
        internal PDLSet set;
        internal int n;
        internal PDLComparisonOperator op;

        PDLSet IMatchable<PDLSet, int, PDLComparisonOperator>.GetArg1() { return set; }
        int IMatchable<PDLSet, int, PDLComparisonOperator>.GetArg2() { return n; }
        PDLComparisonOperator IMatchable<PDLSet, int, PDLComparisonOperator>.GetArg3() { return op; }

        /// <summary>
        /// is true iff S exists and |S| = n
        /// </summary>
        /// <param name="S"></param>
        /// <param name="n"></param>
        public PDLSetCardinality(PDLSet S, int n, PDLComparisonOperator op)
        {
            this.set = S;
            this.n = n;
            this.op = op;
            switch (op)
            {
                case PDLComparisonOperator.Eq: name = "setcard="; break;
                case PDLComparisonOperator.Ge: name = "setcard>"; break;
                case PDLComparisonOperator.Geq: name = "setcard>="; break;
                case PDLComparisonOperator.Le: name = "setcard<"; break;
                case PDLComparisonOperator.Leq: name = "setcard<="; break;
                default: throw new PDLException("Undefined operator");
            }
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            switch (op)
            {
                case PDLComparisonOperator.Eq: return BitVecUtil.CountBits(set.Eval(str, A), 0, str.Length) == n;
                case PDLComparisonOperator.Ge: return BitVecUtil.CountBits(set.Eval(str, A), 0, str.Length) > n;
                case PDLComparisonOperator.Geq: return BitVecUtil.CountBits(set.Eval(str, A), 0, str.Length) >= n;
                case PDLComparisonOperator.Le: return BitVecUtil.CountBits(set.Eval(str, A), 0, str.Length) < n;
                case PDLComparisonOperator.Leq: return BitVecUtil.CountBits(set.Eval(str, A), 0, str.Length) <= n;
                default: throw new PDLException("Undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            switch (op)
            {
                case PDLComparisonOperator.Eq:
                    {
                        int c = fg.get();
                        string X = "_X_" + c.ToString();
                        string x = "x";
                        //x1 ... xn are distinct
                        MSOFormula dis = null;
                        for (int i = 1; i <= n; i++)
                        {
                            for (int j = i + 1; j <= n; j++)
                            {
                                var d = new MSONot(new MSOEqual(x + i.ToString(), x + j.ToString()));
                                if (dis == null)
                                    dis = d;
                                else
                                    dis = new MSOAnd(dis, d);
                            }
                        }
                        if (dis == null)
                            dis = new MSOTrue();

                        //x \in S iff x=xi for some i
                        MSOFormula eq = null;
                        for (int i = 1; i <= n; i++) // x=xi
                        {
                            var d = new MSOEqual(x, x + i.ToString());
                            if (eq == null)
                                eq = d;
                            else
                                eq = new MSOOr(eq, d);
                        }
                        if(eq==null)
                            eq = new MSOFalse();

                        MSOFormula phi = new MSOForallFO(x, new MSOIff(new MSOBelong(x, X), eq));

                        phi = new MSOAnd(dis, phi);

                        for (int i = 1; i <= n; i++)
                        {
                            phi = new MSOExistsFO(x + i.ToString(), phi);
                        }

                        phi = new MSOExistsSO(X, new MSOAnd(set.ToMSO(fg,X), phi));

                        return phi;
                    }
                case PDLComparisonOperator.Ge:
                    {
                        return (new PDLNot(new PDLIntLeq(set, n))).ToMSO(fg);
                    }
                case PDLComparisonOperator.Geq:
                    {
                        return (new PDLNot(new PDLIntLe(set, n))).ToMSO(fg);
                    }
                case PDLComparisonOperator.Le:
                    {
                        return new PDLBinaryFormula(
                        new PDLIntLeq(set, n),
                        new PDLNot(new PDLIntEq(set, n)),
                        PDLLogicalOperator.And).ToMSO(fg);
                    }
                case PDLComparisonOperator.Leq:
                    {
                        int c = fg.get();
                        string X = "_X_" + c.ToString();
                        string x = "x";

                        MSOFormula d;

                        //x \in S iff x=xi for some i \in [1,n]
                        MSOFormula eq = null;
                        for (int i = 1; i <= n; i++)
                        {
                            d = new MSOEqual(x, x + i.ToString());
                            if (eq == null)
                                eq = d;
                            else
                                eq = new MSOOr(eq, d);
                        }
                        if (eq == null)
                            eq = new MSOFalse();

                        //phi says that x_i's cover S
                        MSOFormula phi = new MSOExistsSO(X, new MSOAnd(set.ToMSO(fg,X),
                            new MSOForallFO(x, new MSOIff(new MSOBelong(x, X), eq))));

                        for (int i = 1; i <= n; i++)
                        {
                            phi = new MSOExistsFO(x + i.ToString(), phi);
                        }

                        //TODO include empty string if n>0, need MSOEmptyStringFirstLast	TestPDL	
                        if (n > 0) phi = new MSOOr(phi, new MSOExistsSO(X, new MSOAnd(set.ToMSO(fg,X), new MSOForallFO(x,
                            new MSONot(new MSOBelong(x, X))))));

                        return phi;
                    }
                default: throw new PDLException("Undefined operator");
            }
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLSetCardinality;
            if (pp != null && name == pred.name)
            {
                var v1 = set.CompareTo(pp.set);
                return (v1 == 0) ? (n - pp.n) : v1;
            }
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(|");
            set.ToString(sb);
            sb.Append("|");
            switch (op)
            {
                case PDLComparisonOperator.Eq: sb.Append(" = "); break;
                case PDLComparisonOperator.Ge: sb.Append(" > "); break;
                case PDLComparisonOperator.Geq: sb.Append(" >= "); break;
                case PDLComparisonOperator.Le: sb.Append(" < "); break;
                case PDLComparisonOperator.Leq: sb.Append(" <= "); break;
                default: throw new PDLException("Undefined operator");
            }
            sb.Append(n + ")");
        }

        public override bool ContainsVar(String vName)
        {
            return set.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), set.GetNodeName(), index, x1));
            int x2 = fg.get();
            nodes.Add(n + ":" + x2, new Pair<PDL, string>(this, "n")); 
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), n, index, x2));
            set.ToTreeString(fg, sb, x1, nodes);
        }

        public override int GetFormulaSize()
        {
            return 2 + set.GetFormulaSize();
        }

        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.set);
        }

        public override bool IsComplex()
        {
            if (set is PDLAllPos && (this.op==PDLComparisonOperator.Eq || this.op==PDLComparisonOperator.Leq)  && this.n<=2)
                return true;
            return set.IsComplex();
        }

        public override CPDLPred GetCPDL()
        {
            CPDLSet set = this.set.GetCPDL();
            CPDLInteger n = new CPDLInteger(this.n);
            return new CPDLSetCardinality(set, n);
        }

        public override bool Equals(object obj)
        {
            PDLSetCardinality other = obj as PDLSetCardinality;
            if (other == null) { return false; }
            if (!this.set.Equals(other.set)) { return false; }
            if (this.n != other.n) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.set.GetHashCode();
            hashCode = (hashCode * 31) + this.n.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLIntEq : PDLSetCardinality, IMatchable<PDLSet, int> // |S| = n
    {
        PDLSet IMatchable<PDLSet, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int>.GetArg2() { return n; }
        public PDLIntEq(PDLSet S, int n) : base(S, n, PDLComparisonOperator.Eq) { }
    }

    public class PDLIntLeq : PDLSetCardinality, IMatchable<PDLSet, int>  // |S| <= n
    {
        PDLSet IMatchable<PDLSet, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int>.GetArg2() { return n; }
        public PDLIntLeq(PDLSet S, int n) : base(S, n, PDLComparisonOperator.Leq) { }
    }

    public class PDLIntLe : PDLSetCardinality, IMatchable<PDLSet, int>  // |S| < n
    {
        PDLSet IMatchable<PDLSet, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int>.GetArg2() { return n; }
        public PDLIntLe(PDLSet S, int n) : base(S, n, PDLComparisonOperator.Le) { }
    }

    public class PDLIntGeq : PDLSetCardinality, IMatchable<PDLSet, int>  // |S| >= n
    {
        PDLSet IMatchable<PDLSet, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int>.GetArg2() { return n; }
        public PDLIntGeq(PDLSet S, int n) : base(S, n, PDLComparisonOperator.Geq) { }
    }

    public class PDLIntGe : PDLSetCardinality, IMatchable<PDLSet, int>  // |S| >= n
    {
        PDLSet IMatchable<PDLSet, int>.GetArg1() { return set; }
        int IMatchable<PDLSet, int>.GetArg2() { return n; }
        public PDLIntGe(PDLSet S, int n) : base(S, n, PDLComparisonOperator.Ge) { }
    }
    #endregion

    #region Quantified formula
    public abstract class PDLQuantifiedFormula : PDLPred, IMatchable<PDLPred, String, PDLQuantifier> 
    {
        internal PDLPred phi;
        internal String var;
        internal PDLQuantifier q;

        PDLPred IMatchable<PDLPred, String, PDLQuantifier>.GetArg1() { return phi; }
        String IMatchable<PDLPred, String, PDLQuantifier>.GetArg2() { return var; }
        PDLQuantifier IMatchable<PDLPred, String, PDLQuantifier>.GetArg3() { return q; }

        public PDLQuantifiedFormula(String var, PDLPred phi, PDLQuantifier q)
        {
            this.phi = phi;
            this.var = var;
            this.q = q;
            switch (q)
            {
                case PDLQuantifier.ExistsFO: name = "ex1"; break;
                case PDLQuantifier.ExistsSO: name = "ex2"; break;
                case PDLQuantifier.ForallFO: name = "all1"; break;
                case PDLQuantifier.ForallSO: name = "all2"; break;
                default: throw new PDLException("Quantifier undefined");
            }
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            switch (q)
            {
                case PDLQuantifier.ExistsFO:
                    {
                        for (int i = 0; i < str.Length; i++)
                        {
                            A.Add(var, i); // 1 << i if bitvec
                            if (phi.Eval(str, A))
                            {
                                A.Remove(var);
                                return true;
                            }
                            A.Remove(var);
                        }
                        return false;
                    }
                case PDLQuantifier.ExistsSO:
                    {
                        for (int i = 0; i < Math.Pow(2, str.Length + 1); i++) //try to use bitwise operators
                        {
                            A.Add(var, i);
                            if (phi.Eval(str, A))
                            {
                                A.Remove(var);
                                return true;
                            }
                            A.Remove(var);
                        }
                        return false;
                    }
                case PDLQuantifier.ForallFO:
                    {
                        for (int i = 0; i < str.Length; i++)
                        {
                            A.Add(var, i);
                            if (!phi.Eval(str, A))
                            {
                                A.Remove(var);
                                return false;
                            }
                            A.Remove(var);
                        }
                        return true;
                    }
                case PDLQuantifier.ForallSO:
                    {
                        for (int i = 0; i < Math.Pow(2, str.Length + 1); i++)
                        {
                            A.Add(var, i);
                            if (!phi.Eval(str, A))
                            {
                                A.Remove(var);
                                return false;
                            }
                            A.Remove(var);
                        }
                        return true;
                    }
                default: throw new PDLException("Quantifier undefined");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            switch (q)
            {
                case PDLQuantifier.ExistsFO: return new MSOExistsFO(var, phi.ToMSO(fg));
                case PDLQuantifier.ExistsSO: return new MSOExistsSO(var, phi.ToMSO(fg));
                case PDLQuantifier.ForallFO: return new MSOForallFO(var, phi.ToMSO(fg));
                case PDLQuantifier.ForallSO: return new MSOForallFO(var, phi.ToMSO(fg));
                default: throw new PDLException("Quantifier undefined");
            }
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLQuantifiedFormula;
            if (pp != null && name == pp.name)
            {
                var v1 = phi.CompareTo(pp.phi);
                return (v1 == 0) ? (var.CompareTo(pp.var)) : v1;
            }
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            switch (q)
            {
                case PDLQuantifier.ExistsFO: sb.Append("ex1 "); break;
                case PDLQuantifier.ExistsSO: sb.Append("ex2 "); break;
                case PDLQuantifier.ForallFO: sb.Append("all1 "); break;
                case PDLQuantifier.ForallSO: sb.Append("all2 "); break;
                default: throw new PDLException("Quantifier undefined");
            }
            sb.Append(var + ".");
            phi.ToString(sb);
        }
        public override bool ContainsVar(String vName)
        {
            return vName != var && phi.ContainsVar(vName);
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), phi.GetNodeName(), index, x1));
            phi.ToTreeString(fg, sb, x1, nodes);
        }
        public override int GetFormulaSize()
        {
            return 1 + phi.GetFormulaSize();
        }

        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.phi);
        }

        public override bool IsComplex()
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            PDLQuantifiedFormula other = obj as PDLQuantifiedFormula;
            if (other == null) { return false; }
            if (!this.phi.Equals(other.phi)) { return false; }
            if (!this.var.Equals(other.var)) { return false; }
            if (!this.q.Equals(other.q)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.phi.GetHashCode();
            hashCode = (hashCode * 31) + this.var.GetHashCode();
            hashCode = (hashCode * 31) + this.q.GetHashCode();
            return hashCode;
        }
    }

    public class PDLForallFO : PDLQuantifiedFormula, IMatchable<PDLPred, String>
    {
        PDLPred IMatchable<PDLPred, String>.GetArg1() { return phi; }
        String IMatchable<PDLPred, String>.GetArg2() { return var; }
        public PDLForallFO(String var, PDLPred phi) : base(var, phi, PDLQuantifier.ForallFO) { }
        public override CPDLPred GetCPDL()
        {
            CPDLPred operand = this.phi.GetCPDL();
            return new CPDLFirstOrderQuantifierPred(this.var, operand);
        }
    }

    public class PDLForallSO : PDLQuantifiedFormula, IMatchable<PDLPred, String>
    {
        PDLPred IMatchable<PDLPred, String>.GetArg1() { return phi; }
        String IMatchable<PDLPred, String>.GetArg2() { return var; }
        public PDLForallSO(String var, PDLPred phi) : base(var, phi, PDLQuantifier.ForallSO) { }
        public override CPDLPred GetCPDL()
        {
            CPDLPred operand = this.phi.GetCPDL();
            return new CPDLSecondOrderQuantifierPred(this.var, operand);
        }
    }

    public class PDLExistsFO : PDLQuantifiedFormula, IMatchable<PDLPred, String>
    {
        PDLPred IMatchable<PDLPred, String>.GetArg1() { return phi; }
        String IMatchable<PDLPred, String>.GetArg2() { return var; }
        public PDLExistsFO(String var, PDLPred phi) : base(var, phi, PDLQuantifier.ExistsFO) { }
        public override CPDLPred GetCPDL()
        {
            CPDLPred operand = this.phi.GetCPDL();
            return new CPDLFirstOrderQuantifierPred(this.var, operand);
        }
    }

    public class PDLExistsSO : PDLQuantifiedFormula, IMatchable<PDLPred, String>
    {
        PDLPred IMatchable<PDLPred, String>.GetArg1() { return phi; }
        String IMatchable<PDLPred, String>.GetArg2() { return var; }
        public PDLExistsSO(String var, PDLPred phi) : base(var, phi, PDLQuantifier.ExistsSO) { }
        public override CPDLPred GetCPDL()
        {
            CPDLPred operand = this.phi.GetCPDL();
            return new CPDLSecondOrderQuantifierPred(this.var, operand);
        }
    }
    #endregion

    #region Binary logic formulae
    public class PDLBinaryFormula : PDLPred, IMatchable<PDLPred, PDLPred, PDLLogicalOperator>
    {
        internal PDLPred phi1;
        internal PDLPred phi2;
        internal PDLLogicalOperator op;

        PDLPred IMatchable<PDLPred, PDLPred, PDLLogicalOperator>.GetArg1() { return phi1; }
        PDLPred IMatchable<PDLPred, PDLPred, PDLLogicalOperator>.GetArg2() { return phi2; }
        PDLLogicalOperator IMatchable<PDLPred, PDLPred, PDLLogicalOperator>.GetArg3() { return op; }

        public PDLBinaryFormula(PDLPred left, PDLPred right, PDLLogicalOperator op)
        {
            this.phi1 = left;
            this.phi2 = right;
            this.op = op;
            switch (op)
            {
                case PDLLogicalOperator.And: name = "and"; break;
                case PDLLogicalOperator.If: name = "if"; break;
                case PDLLogicalOperator.Iff: name = "iff"; break;
                case PDLLogicalOperator.Or: name = "or"; break;
                default: throw new PDLException("undefined operator");
            }

        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            switch (op)
            {
                case PDLLogicalOperator.And:
                    return (phi1.Eval(str, A) && phi2.Eval(str, A));
                case PDLLogicalOperator.If:
                    return (!(phi1.Eval(str, A)) || phi2.Eval(str, A));
                case PDLLogicalOperator.Iff:
                    return ((!(phi1.Eval(str, A)) || phi2.Eval(str, A))) && ((!(phi2.Eval(str, A)) || phi1.Eval(str, A)));;
                case PDLLogicalOperator.Or:
                    return phi1.Eval(str, A) || phi2.Eval(str, A);
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            switch (op)
            {
                case PDLLogicalOperator.And: return new MSOAnd(phi1.ToMSO(fg), phi2.ToMSO(fg));
                case PDLLogicalOperator.If: return new MSOIf(phi1.ToMSO(fg), phi2.ToMSO(fg));
                case PDLLogicalOperator.Iff: return new MSOIff(phi1.ToMSO(fg), phi2.ToMSO(fg));
                case PDLLogicalOperator.Or: return new MSOOr(phi1.ToMSO(fg), phi2.ToMSO(fg));
            }
            throw new PDLException("undefined operator");
        }

        public override int CompareTo(PDLPred pred)
        {
            var orderDiff = name.CompareTo(pred.name);
            var pp = pred as PDLBinaryFormula;
            if (pp != null && orderDiff == 0)
            {
                if (PDLEnumUtil.IsSymmetric(op))
                {
                    var vlr = phi1.CompareTo(pp.phi2);
                    var vrl = phi2.CompareTo(pp.phi1);
                    if ((vlr == 0 && vrl == 0))
                        return 0;
                }

                var vll = phi1.CompareTo(pp.phi1);
                var vrr = phi2.CompareTo(pp.phi2);
                return (vll == 0) ? vrr : vll;
            }
            return orderDiff;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            phi1.ToString(sb);
            switch (op)
            {
                case PDLLogicalOperator.And: sb.Append(" and "); break;
                case PDLLogicalOperator.If: sb.Append(" --> "); break;
                case PDLLogicalOperator.Iff: sb.Append(" <-> "); break;
                case PDLLogicalOperator.Or: sb.Append(" or "); break;
            }
            phi2.ToString(sb);
            sb.Append(")");
        }

        public override bool ContainsVar(String vName)
        {
            return phi1.ContainsVar(vName) || phi2.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), phi1.GetNodeName(), index, x1));
            int x2 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), phi2.GetNodeName(), index, x2));
            phi1.ToTreeString(fg, sb, x1, nodes);
            phi2.ToTreeString(fg, sb, x2, nodes);
        }
        public override int GetFormulaSize()
        {
            return 1 + phi1.GetFormulaSize() + phi2.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.phi1);
            set.Add(this.phi2);
        }
        public override bool IsComplex()
        {
            return phi1.IsComplex() || phi2.IsComplex();
        }

        public override CPDLPred GetCPDL()
        {
            CPDLPred lhs = this.phi1.GetCPDL();
            CPDLPred rhs = this.phi2.GetCPDL();
            return new CPDLLogConnPred(lhs, rhs, this.op);
        }

        public override bool Equals(object obj)
        {
            PDLBinaryFormula other = obj as PDLBinaryFormula;
            if (other == null) { return false; }
            if (!this.phi1.Equals(other.phi1)) { return false; }
            if (!this.phi2.Equals(other.phi2)) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.phi1.GetHashCode();
            hashCode = (hashCode * 31) + this.phi2.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLAnd : PDLBinaryFormula, IMatchable<PDLPred, PDLPred>
    {
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg1() { return phi1; }
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg2() { return phi2; }

        public PDLAnd(PDLPred phi1, PDLPred phi2)
            : base(phi1, phi2, PDLLogicalOperator.And)
        { }
    }

    public class PDLOr : PDLBinaryFormula, IMatchable<PDLPred, PDLPred>
    {
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg1() { return phi1; }
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg2() { return phi2; }

        public PDLOr(PDLPred phi1, PDLPred phi2)
            : base(phi1, phi2, PDLLogicalOperator.Or)
        { }
    }

    public class PDLIf : PDLBinaryFormula, IMatchable<PDLPred, PDLPred>
    {
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg1() { return phi1; }
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg2() { return phi2; }

        public PDLIf(PDLPred phi1, PDLPred phi2)
            : base(phi1, phi2, PDLLogicalOperator.If)
        { }
    }

    public class PDLIff : PDLBinaryFormula, IMatchable<PDLPred, PDLPred>
    {
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg1() { return phi1; }
        PDLPred IMatchable<PDLPred, PDLPred>.GetArg2() { return phi2; }

        public PDLIff(PDLPred left, PDLPred right)
            : base(left, right, PDLLogicalOperator.Iff)
        { }
    }
    #endregion

    public class PDLNot : PDLPred, IMatchable<PDLPred>
    {
        internal PDLPred phi;

        PDLPred IMatchable<PDLPred>.GetArg() { return phi; }

        public PDLNot(PDLPred phi)
        {
            this.phi = phi;
            name = "not";
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            return !phi.Eval(str, A);
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            return new MSONot(phi.ToMSO(fg));
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLNot;
            if (pp != null)
                return phi.CompareTo(pp.phi);

            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(not ");
            phi.ToString(sb);
            sb.Append(")");
        }

        public override bool ContainsVar(String vName)
        {
            return phi.ContainsVar(vName);
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), phi.GetNodeName(), index, x1));
            phi.ToTreeString(fg, sb, x1, nodes);
        }
        public override int GetFormulaSize()
        {
            return 1 + phi.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.phi);
        }

        public override bool IsComplex()
        {
            return phi.IsComplex();
        }

        public override CPDLPred GetCPDL()
        {
            CPDLPred operand = this.phi.GetCPDL();
            return new CPDLNegationPred(operand);
        }

        public override bool Equals(object obj)
        {
            PDLNot other = obj as PDLNot;
            if (other == null) { return false; }
            if (!this.phi.Equals(other.phi)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.phi.GetHashCode();
            return hashCode;
        }
    }

    #region True False
    public class PDLTrue : PDLPred
    {
        public PDLTrue()
        {
            name = "true";
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            return new MSOTrue();
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            return true;
        }

        public override bool ContainsVar(string vName)
        {
            return false;
        }

        public override int CompareTo(PDLPred pred)
        {
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("true");
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
        }
        public override int GetFormulaSize()
        {
            return 2;
        }

        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
        }
        public override bool IsComplex()
        {
            return false;
        }

        public override CPDLPred GetCPDL()
        {
            return new CPDLConstantPred();
        }

        public override bool Equals(object obj)
        {
            PDLTrue other = obj as PDLTrue;
            if (other == null) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }

    public class PDLFalse : PDLPred
    {
        public PDLFalse()
        {
            name = "false";
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            return new MSOFalse();
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            return false;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("false");
        }

        public override bool ContainsVar(string vName)
        {
            return false;
        }

        public override int CompareTo(PDLPred pred)
        {
            return name.CompareTo(pred.name);
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
        }
        public override int GetFormulaSize()
        {
            return 2;
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
        }
        public override bool IsComplex()
        {
            return false;
        }
        public override CPDLPred GetCPDL()
        {
            return new CPDLConstantPred();
        }

        public override bool Equals(object obj)
        {
            PDLFalse other = obj as PDLFalse;
            if (other == null) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }
    #endregion

    #region String queries
    public abstract class PDLStringQuery : PDLPred, IMatchable<string, PDLStringQueryOp>
    {
        internal string str;
        internal PDLStringQueryOp op;
            
        string IMatchable<string, PDLStringQueryOp>.GetArg1() { return str; }
        PDLStringQueryOp IMatchable<string, PDLStringQueryOp>.GetArg2() { return op; }

        public PDLStringQuery(string str, PDLStringQueryOp op)
        {
            this.op = op;
            this.str = str;
            switch (op)
            {
                case PDLStringQueryOp.Contains: name = "contains"; break;
                case PDLStringQueryOp.EndsWith: name = "endsWith"; break;
                case PDLStringQueryOp.IsString: name = "isString"; break;
                case PDLStringQueryOp.StartsWith: name = "startsWith"; break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override bool Eval(string input, Dictionary<string, int> A)
        {
            switch (op)
            {
                case PDLStringQueryOp.Contains:
                    return input.Contains(this.str);

                case PDLStringQueryOp.EndsWith:
                    return input.EndsWith(this.str);

                case PDLStringQueryOp.IsString:
                    return input == this.str;

                case PDLStringQueryOp.StartsWith:
                    return input.StartsWith(this.str);

                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            switch (op)
            {
                case PDLStringQueryOp.Contains:
                    return new PDLIntGeq(new PDLIndicesOf(str), 1).ToMSO(fg);

                case PDLStringQueryOp.EndsWith:
                    {
                        string xi, xiSucc, xLast;

                        MSOFormula phi = new MSOTrue();
                        for (int i = 0; i < str.Length - 1; i++)
                        {
                            xi = "x" + i.ToString();
                            xiSucc = "x" + (i + 1).ToString();
                            phi = new MSOExistsFO(xi, new MSOAnd(new MSOSucc(xi, xiSucc), new MSOAnd(new MSOLabel(xi, str[i]), phi)));
                        }
                        xLast = "x" + (str.Length - 1).ToString();
                        phi = new MSOExistsFO(xLast, new MSOAnd(new MSOAnd(new MSOLast(xLast), new MSOLabel(xLast, str[str.Length - 1])), phi));

                        return phi;
                    }

                case PDLStringQueryOp.IsString:
                    {
                        string xi, xj;
                        if (this.str == "")
                            return new MSOForallFO("x", new MSOFalse());
                        xi = "x0";

                        MSOFormula phi = new MSOAnd(new MSOFirst(xi), new MSOLabel(xi, this.str[0]));
                        for (int i = 1; i < this.str.Length; i++)
                        {
                            xi = "x" + i.ToString();
                            xj = "x" + (i - 1).ToString();
                            phi = new MSOAnd(phi, new MSOAnd(new MSOSucc(xj, xi), new MSOLabel(xi, this.str[i])));
                        }

                        phi = new MSOAnd(phi, new MSOLast(xi));

                        for (int i = 0; i < this.str.Length; i++)
                        {
                            xi = "x" + i.ToString();
                            phi = new MSOExistsFO(xi, phi);
                        }
                        return phi;
                    }

                case PDLStringQueryOp.StartsWith:
                    {
                        string xi, xiPrev;
                        MSOFormula phi;

                        phi = new MSOTrue();
                        for (int i = str.Length - 1; i > 0; i--)
                        {
                            xi = "x" + i.ToString();
                            xiPrev = "x" + (i - 1).ToString();
                            phi = new MSOExistsFO(xi, new MSOAnd(new MSOAnd(new MSOSucc(xiPrev, xi), new MSOLabel(xi, str[i])), phi));
                        }
                        phi = new MSOExistsFO("x0", new MSOAnd(new MSOFirst("x0"), new MSOAnd(new MSOLabel("x0", str[0]), phi)));

                        return phi;
                    }
                default: throw new PDLException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case PDLStringQueryOp.Contains: sb.Append("contains "); break;
                case PDLStringQueryOp.EndsWith: sb.Append("endsWith "); break;
                case PDLStringQueryOp.IsString: sb.Append("isString  "); break;
                case PDLStringQueryOp.StartsWith: sb.Append("startsWith "); break;
                default: throw new PDLException("undefined operator");
            }
            sb.Append("'" + str + "'");
        }

        public override int CompareTo(PDLPred pred)
        {
            var pp = pred as PDLStringQuery;
            if (pp != null && name == pred.name)
                return str.CompareTo(pp.str);
            return name.CompareTo(pred.name);
        }

        public override bool ContainsVar(string vName)
        {
            return false;
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            nodes.Add(str + ":" + x1, new Pair<PDL, string>(this, "str")); 
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), str, index, x1));
        }

        public override int GetFormulaSize()
        {
            return 2;
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
        }
        public override bool IsComplex()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            PDLStringQuery other = obj as PDLStringQuery;
            if (other == null) { return false; }
            if (!this.str.Equals(other.str)) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.str.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLStartsWith : PDLStringQuery, IMatchable<string>
    {
        string IMatchable<string>.GetArg() { return str; }
        public PDLStartsWith(string str) : base(str, PDLStringQueryOp.StartsWith) { }
        public override CPDLPred GetCPDL()
        {
            CPDLString queriedString = new CPDLString(this.str);
            return new CPDLBegEndWithPred(queriedString);
        }
    }

    public class PDLEndsWith : PDLStringQuery, IMatchable<string>
    {
        string IMatchable<string>.GetArg() { return str; }
        public PDLEndsWith(string str) : base(str, PDLStringQueryOp.EndsWith) { }
        public override CPDLPred GetCPDL()
        {
            CPDLString queriedString = new CPDLString(this.str);
            return new CPDLBegEndWithPred(queriedString);
        }
    }

    public class PDLIsString : PDLStringQuery, IMatchable<string>
    {
        string IMatchable<string>.GetArg() { return str; }
        public PDLIsString(string str) : base(str, PDLStringQueryOp.IsString) { }
        public override CPDLPred GetCPDL()
        {
            CPDLString choiceString = new CPDLString(this.str);
            return new CPDLIsStringPred(choiceString);
        }
    }

    public class PDLContains : PDLStringQuery, IMatchable<string>
    {
        string IMatchable<string>.GetArg() { return str; }
        public PDLContains(string str) : base(str, PDLStringQueryOp.Contains) { }
        public override CPDLPred GetCPDL()
        {
            CPDLString choiceString = new CPDLString(this.str);
            return new CPDLContainsPred(choiceString);
        }
    }
    #endregion

    public class PDLEmptyString : PDLPred
    {
        public PDLEmptyString()
        {
            name = "emptyStr";
        }

        public override MSOFormula ToMSO(FreshGen fg)
        {
            return new MSOForallFO("x", new MSOFalse());
        }

        public override bool Eval(string str, Dictionary<string, int> A)
        {
            return (str == "");
        }

        public override int CompareTo(PDLPred pred)
        {
            return name.CompareTo(pred.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("emptyStr");
        }

        public override bool ContainsVar(string vName)
        {
            return false;
        }
        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
        }

        public override int GetFormulaSize()
        {
            return 1;
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
        }
        public override bool IsComplex()
        {
            return false;
        }

        public override CPDLPred GetCPDL()
        {
            return new CPDLIsEmptyPred();
        }

        public override bool Equals(object obj)
        {
            PDLEmptyString other = obj as PDLEmptyString;
            if (other == null) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }

}
