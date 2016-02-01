using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Automata;
using Microsoft.Z3;

using MSOZ3;

namespace AutomataPDL
{
    public abstract class PDLSet : PDL
    {
        public override string GetNodeName()
        {
            return "se:" + name;
        }

        public abstract MSOFormula ToMSO(FreshGen fg, string V);//set variable V

        public abstract int Eval(string str, Dictionary<string, int> A); // A is the assignment

        public abstract MSOFormula Contains(FreshGen fg, string v);// FO variable v

        public abstract int CompareTo(PDLSet set);

        public abstract CPDLSet GetCPDL();
    }

    public class PDLSetVar : PDLSet, IMatchable<string>
    {
        internal string var;

        string IMatchable<string>.GetArg() { return var; }

        public PDLSetVar(string str)
        {
            var = str;
            name = "SetVar";
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            //Debug.Assert(A.ContainsKey(str), "SO variable \"" + var + "\" not instantiated ");
            return A[this.var];
        }

        public override MSOFormula ToMSO(FreshGen fg, string V)
        {
            return new MSOEqual(var, V);
        }

        public override MSOFormula Contains(FreshGen fg, string v)
        {
            return new MSOBelong(v, var);
        }

        public override bool ContainsVar(string vName)
        {
            return vName == this.var;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(this.var);
        }

        public override int CompareTo(PDLSet set)
        {
            var pp = set as PDLSetVar;
            if (pp != null)
                return var.CompareTo(pp.var);

            return name.CompareTo(set.name);
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
            return true;
        }

        public override CPDLSet GetCPDL()
        {
            return new CPDLVariableSet(this.var);
        }

        public override bool Equals(object obj)
        {
            PDLSetVar other = obj as PDLSetVar;
            if (other == null) { return false; }
            if (!this.var.Equals(other.var)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.var.GetHashCode();
            return hashCode;
        }
    }

    public class PDLIndicesOf : PDLSet, IMatchable<string> //the set of indices in where the substring str appears
    {
        internal string str;
        string IMatchable<string>.GetArg() { return str; }

        public PDLIndicesOf(string str)
        {
            if (str == "")
                throw new System.ApplicationException("Cannot instantiate PDLindicesOf with empty string");

            this.str = str;
            name = "indicesOf";
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            int ret = 0;
            for (int i = 0; i <= str.Length - this.str.Length; i++)
            {
                if (this.str == str.Substring(i, this.str.Length))
                    ret |= (1 << i);
            }
            return ret;
        }

        public override MSOFormula Contains(FreshGen fg, string v)
        {
            string zi, ziPrev, z0 = "_z0";
            int i = str.Length - 1;

            MSOFormula phi = new MSOTrue();

            while (i > 0)
            {
                zi = "_z" + i.ToString();
                ziPrev = "_z" + (i - 1).ToString();
                phi = new MSOExistsFO(zi, new MSOAnd(phi, new MSOAnd(new MSOLabel(zi, str[i]), new MSOSucc(ziPrev, zi))));
                i--;
            }
            phi = new MSOExistsFO(z0, new MSOAnd(phi, new MSOAnd(new MSOLabel(z0, str[0]), new MSOEqual(z0, v))));

            return phi;
        }

        public override MSOFormula ToMSO(FreshGen fg, string V)
        {
            int c = fg.get();
            string x = "_x_" + c.ToString();
            string y = "_y_" + c.ToString();
            string X = "_X_" + c.ToString();


            int i = str.Length - 1;
            string z_i, z_im;
            MSOFormula phi = null;

            phi = new MSOLabel("z0", str[0]);

            while (i > 0)
            {
                z_i = "z" + i.ToString();
                z_im = "z" + (i - 1).ToString();
                phi = new MSOExistsFO(z_i, new MSOAnd(new MSOSucc(z_im, z_i),
                    new MSOAnd(new MSOLabel(z_i, str[i]), phi)));
                i--;
            }

            return new MSOForallFO("z0", new MSOIff(new MSOBelong("z0", V), phi));
        }

        public override int CompareTo(PDLSet set)
        {
            var pp = set as PDLIndicesOf;
            if (pp != null)
                return str.CompareTo(pp.str);
            return name.CompareTo(set.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("indOf '" + str + "'");
        }

        public override bool ContainsVar(String vName)
        {
            return false;
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x = fg.get();
            nodes.Add(str + ":" + x, new Pair<PDL, string>(this, "str")); 
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), str, index, x));
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

        public override CPDLSet GetCPDL()
        {
            CPDLString stringChoice = new CPDLString(this.str);
            return new CPDLIndOfSet(stringChoice);
        }

        public override bool Equals(object obj)
        {
            PDLIndicesOf other = obj as PDLIndicesOf;
            if (other == null) { return false; }
            if (!this.str.Equals(other.str)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.str.GetHashCode();
            return hashCode;
        }
    }

    public class PDLAllPos : PDLSet
    {
        public PDLAllPos()
        {
            name = "allPos";
        }

        public override MSOFormula ToMSO(FreshGen fg, string V)
        {
            string x = "x";
            return new MSOForallFO(x, new MSOBelong(x, V));
        }

        public override MSOFormula Contains(FreshGen fg, string v)
        {
            return new MSOTrue();
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            return -1;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("allPos");
        }

        public override bool ContainsVar(String vName)
        {
            return false;
        }

        public override int CompareTo(PDLSet set)
        {
            return name.CompareTo(set.name);
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

        public override CPDLSet GetCPDL()
        {
            return new CPDLAllSet();
        }

        public override bool Equals(object obj)
        {
            PDLAllPos other = obj as PDLAllPos;
            if (other == null) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }

    #region PDLSetCmpPos
    public class PDLSetCmpPos : PDLSet, IMatchable<PDLPos, PDLComparisonOperator> // all positions before p
    {
        internal PDLPos pos;
        internal PDLComparisonOperator op;

        PDLPos IMatchable<PDLPos, PDLComparisonOperator>.GetArg1() { return pos; }
        PDLComparisonOperator IMatchable<PDLPos, PDLComparisonOperator>.GetArg2() { return op; }

        public PDLSetCmpPos(PDLPos p, PDLComparisonOperator op)
        {
            this.pos = p;
            this.op = op;
            switch (op)
            {
                case PDLComparisonOperator.Ge: name = "allAfter "; break;
                case PDLComparisonOperator.Geq: name = "allFrom "; break;
                case PDLComparisonOperator.Le: name = "allBefore "; break;
                case PDLComparisonOperator.Leq: name = "allUpto"; break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula Contains(FreshGen fg, string v)
        {
            switch (op)
            {
                case PDLComparisonOperator.Ge:
                    {
                        int c = fg.get();
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg,y), new MSOLess(y, v)));
                    }
                case PDLComparisonOperator.Geq:
                    {
                        int c = fg.get();
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg,y), new MSOLessEq(y, v)));
                    }
                case PDLComparisonOperator.Le:
                    {
                        int c = fg.get();
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg,y), new MSOLess(v, y)));
                    }
                case PDLComparisonOperator.Leq:
                    {
                        int c = fg.get();
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg,y), new MSOLessEq(v, y)));
                    }
                default: throw new PDLException("not defined on this operator");
            }

        }

        public override MSOFormula ToMSO(FreshGen fg, string V)
        {
            switch (op)
            {
                case PDLComparisonOperator.Ge:
                    {
                        int c = fg.get();
                        string x = "_x";
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg, y), new MSOForallFO(x,
                            new MSOIff(new MSOLess(y, x), new MSOBelong(x, V)))));
                    }
                case PDLComparisonOperator.Geq:
                    {
                        int c = fg.get();
                        string x = "_x";
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg, y), new MSOForallFO(x,
                            new MSOIff(new MSOLessEq(y, x), new MSOBelong(x, V)))));
                    }
                case PDLComparisonOperator.Le:
                    {
                        int c = fg.get();
                        string x = "_x";
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg, y), new MSOForallFO(x,
                            new MSOIff(new MSOLess(x, y), new MSOBelong(x, V)))));
                    }
                case PDLComparisonOperator.Leq:
                    {
                        int c = fg.get();
                        string x = "_x";
                        string y = "_y_" + c.ToString();
                        return new MSOExistsFO(y, new MSOAnd(pos.ToMSO(fg, y), new MSOForallFO(x,
                            new MSOIff(new MSOLessEq(x, y), new MSOBelong(x, V)))));
                    }
                default: throw new PDLException("not defined on this operator");
            }
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            var i = pos.Eval(str, A);
            if (i < 0)
                return 0;
            switch (op)
            {
                case PDLComparisonOperator.Ge: return (-1 << (1 + i));
                case PDLComparisonOperator.Geq: return (-1 << i);
                case PDLComparisonOperator.Le: return (-1 ^ (-1 << i));
                case PDLComparisonOperator.Leq: return (-1 ^ (-1 << (i + 1)));
                default: throw new PDLException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case PDLComparisonOperator.Ge: sb.Append("allAfter "); break;
                case PDLComparisonOperator.Geq: sb.Append("allFrom "); break;
                case PDLComparisonOperator.Le: sb.Append("allBefore "); break;
                case PDLComparisonOperator.Leq: sb.Append("allUpto "); break;
                default: throw new PDLException("undefined operator");
            }
            pos.ToString(sb);
        }

        public override int CompareTo(PDLSet set)
        {
            var pp = set as PDLSetCmpPos;
            if (pp != null && name == set.name)
                return pos.CompareTo(pp.pos);
            return name.CompareTo(set.name);
        }

        public override bool ContainsVar(String vName)
        {
            return pos.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pos.GetNodeName(), index, x));
            pos.ToTreeString(fg,sb, x, nodes);
        }

        public override int GetFormulaSize()
        {
            return 1 + pos.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.pos);
        }
        public override bool IsComplex()
        {
            return pos.IsComplex();
        }

        public override CPDLSet GetCPDL()
        {
            CPDLPos pos = this.pos.GetCPDL();
            return new CPDLPosComparisonSet(pos);
        }

        public override bool Equals(object obj)
        {
            PDLSetCmpPos other = obj as PDLSetCmpPos;
            if (other == null) { return false; }
            if (!this.pos.Equals(other.pos)) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.pos.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLAllPosBefore : PDLSetCmpPos, IMatchable<PDLPos> // all positions before p
    {
        PDLPos IMatchable<PDLPos>.GetArg() { return pos; }
        public PDLAllPosBefore(PDLPos p) : base(p, PDLComparisonOperator.Le) { }
    }

    public class PDLAllPosAfter : PDLSetCmpPos, IMatchable<PDLPos> // all positions after  p
    {
        PDLPos IMatchable<PDLPos>.GetArg() { return pos; }
        public PDLAllPosAfter(PDLPos p) : base(p, PDLComparisonOperator.Ge) { }
    }

    public class PDLAllPosUpto : PDLSetCmpPos, IMatchable<PDLPos> // all positions before and including p
    {
        PDLPos IMatchable<PDLPos>.GetArg() { return pos; }
        public PDLAllPosUpto(PDLPos p) : base(p, PDLComparisonOperator.Leq) { }
    }

    public class PDLAllPosFrom : PDLSetCmpPos, IMatchable<PDLPos> // all positions after and including p
    {
        PDLPos IMatchable<PDLPos>.GetArg() { return pos; }
        public PDLAllPosFrom(PDLPos p) : base(p, PDLComparisonOperator.Geq) { }
    }
    #endregion

    #region PDLSetBinary
    public class PDLSetBinary : PDLSet, IMatchable<PDLSet, PDLSet, PDLBinarySetOperator>
    {
        internal PDLSet set1;
        internal PDLSet set2;
        internal PDLBinarySetOperator op;

        PDLSet IMatchable<PDLSet, PDLSet, PDLBinarySetOperator>.GetArg1() { return set1; }
        PDLSet IMatchable<PDLSet, PDLSet, PDLBinarySetOperator>.GetArg2() { return set2; }
        PDLBinarySetOperator IMatchable<PDLSet, PDLSet, PDLBinarySetOperator>.GetArg3() { return op; }

        public PDLSetBinary(PDLSet set1, PDLSet set2, PDLBinarySetOperator op)
        {
            this.set1 = set1;
            this.set2 = set2;
            this.op = op;
            switch (op)
            {
                case PDLBinarySetOperator.Intersection: name = "intersection"; break;
                case PDLBinarySetOperator.Union: name = "union"; break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            int leftEv = set1.Eval(str, A);
            int rightEv = set2.Eval(str, A);
            switch (op)
            {
                case PDLBinarySetOperator.Intersection: return leftEv & rightEv;
                case PDLBinarySetOperator.Union: return leftEv | rightEv;
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula Contains(FreshGen fg, string v)
        {
            switch (op)
            {
                case PDLBinarySetOperator.Intersection: return new MSOAnd(set1.Contains(fg,v), set2.Contains(fg,v));
                case PDLBinarySetOperator.Union: return new MSOOr(set1.Contains(fg,v), set2.Contains(fg,v));
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg, string V)
        {
            switch (op)
            {
                case PDLBinarySetOperator.Intersection:
                    {
                        int c = fg.get();

                        string X1 = "_X1_" + c.ToString();
                        string X2 = "_X2_" + c.ToString();
                        string x = "_x_" + c.ToString();

                        return new MSOForallFO(x, new MSOIff(new MSOBelong(x, V), new MSOAnd(set1.Contains(fg, x), set2.Contains(fg,x))));
                    }
                case PDLBinarySetOperator.Union:
                    {
                        int c = fg.get();

                        string X1 = "_X1_" + c.ToString();
                        string X2 = "_X2_" + c.ToString();
                        string x = "_x_" + c.ToString();

                        return new MSOForallFO(x, new MSOIff(new MSOBelong(x, V), new MSOOr(set1.Contains(fg,x), set2.Contains(fg,x))));

                    }
                default: throw new PDLException("undefined operator");
            }
        }

        public override int CompareTo(PDLSet set)
        {
            var pp = set as PDLSetBinary;
            if (pp != null && name == set.name)
            {
                var v21 = set1.CompareTo(pp.set2);
                var v22 = set2.CompareTo(pp.set1);
                if ((v21 == 0 && v22 == 0))
                    return 0;

                var v11 = set1.CompareTo(pp.set1);
                var v12 = set2.CompareTo(pp.set2);
                return (v11 == 0) ? v12 : v11;
            }
            return name.CompareTo(set.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            set1.ToString(sb);
            switch (op)
            {
                case PDLBinarySetOperator.Intersection:
                    sb.Append(" inters "); break;
                case PDLBinarySetOperator.Union:
                    sb.Append(" U "); break;
                default: throw new PDLException("undefined operator");
            }
            set2.ToString(sb);
            sb.Append(")");
        }

        public override bool ContainsVar(String vName)
        {
            return set1.ContainsVar(vName) || set2.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), set1.GetNodeName(), index, x1));
            int x2 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), set2.GetNodeName(), index, x2));
            set1.ToTreeString(fg,sb, x1, nodes);
            set2.ToTreeString(fg, sb, x2, nodes);
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
            return set1.IsComplex() || set2.IsComplex();
        }

        public override CPDLSet GetCPDL()
        {
            CPDLSet lhs = this.set1.GetCPDL();
            CPDLSet rhs = this.set2.GetCPDL();
            return new CPDLSetOperatorSet(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            PDLSetBinary other = obj as PDLSetBinary;
            if (other == null) { return false; }
            if (!this.set1.Equals(other.set1)) { return false; }
            if (!this.set2.Equals(other.set2)) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.set1.GetHashCode();
            hashCode = (hashCode * 31) + this.set2.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLIntersect : PDLSetBinary, IMatchable<PDLSet, PDLSet>
    {
        PDLSet IMatchable<PDLSet, PDLSet>.GetArg1() { return set1; }
        PDLSet IMatchable<PDLSet, PDLSet>.GetArg2() { return set2; }
        public PDLIntersect(PDLSet left, PDLSet right) : base(left, right, PDLBinarySetOperator.Intersection) { }
    }

    public class PDLUnion : PDLSetBinary, IMatchable<PDLSet, PDLSet>
    {
        PDLSet IMatchable<PDLSet, PDLSet>.GetArg1() { return set1; }
        PDLSet IMatchable<PDLSet, PDLSet>.GetArg2() { return set2; }
        public PDLUnion(PDLSet left, PDLSet right) : base(left, right, PDLBinarySetOperator.Union) { }
    }
    #endregion

    public class PDLPredSet : PDLSet, IMatchable<string, PDLPred>
    {
        internal string FOvar;
        internal PDLPred pred;

        string IMatchable<string, PDLPred>.GetArg1() { return FOvar; }
        PDLPred IMatchable<string, PDLPred>.GetArg2() { return pred; }

        public PDLPredSet(string v, PDLPred phi)
        {
            FOvar = v;
            pred = phi;
            name = "predSet";
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            int ret = 0;
            for (int i = 0; i < str.Length; i++)
            {
                A.Add(FOvar, i);
                if (pred.Eval(str, A))
                    ret |= (1 << i);
                A.Remove(FOvar);
            }
            return ret;
        }

        public override MSOFormula ToMSO(FreshGen fg, string V)
        {
            // forall FOvar (pred(FOvar) iff FOvar \in V)
            return new MSOForallFO(FOvar, new MSOIff(pred.ToMSO(fg), new MSOBelong(FOvar, V)));
        }

        public override MSOFormula Contains(FreshGen fg, string v)
        {
            //exists FOvar (pred(FOvar) and FOvar = v)
            return new MSOExistsFO(FOvar, new MSOAnd(pred.ToMSO(fg), new MSOEqual(FOvar, v)));
        }

        public override int CompareTo(PDLSet set)
        {
            var pp = set as PDLPredSet;
            if (pp != null)
                return pred.CompareTo(pp.pred);
            return name.CompareTo(set.name);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("{" + FOvar + " | "); // (all FOvar such that pred)
            pred.ToString(sb);
            sb.Append("}");
        }

        public override bool ContainsVar(string vName)
        {
            return ((FOvar == vName) || pred.ContainsVar(vName));
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            nodes.Add(FOvar + ":" + x1, new Pair<PDL, string>(this, "FOvar")); 
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), FOvar, index, x1));
            int x2 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pred.GetNodeName(), index, x2));
            pred.ToTreeString(fg, sb, x2, nodes);
        }
        public override int GetFormulaSize()
        {
            return 2 + pred.GetFormulaSize();
        }
        public override void getPDLClosure(HashSet<PDL> set)
        {
            set.Add(this);
            set.Add(this.pred);
        }
        public override bool IsComplex()
        {
            return pred.IsComplex();
        }

        public override CPDLSet GetCPDL()
        {
            CPDLPred predicate = this.pred.GetCPDL();
            return new CPDLPredSet(this.name, predicate);
        }

        public override bool Equals(object obj)
        {
            PDLPredSet other = obj as PDLPredSet;
            if (other == null) { return false; }
            if (!this.FOvar.Equals(other.FOvar)) { return false; }
            if (!this.pred.Equals(other.pred)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.FOvar.GetHashCode();
            hashCode = (hashCode * 31) + this.pred.GetHashCode();
            return hashCode;
        }
    }
}
