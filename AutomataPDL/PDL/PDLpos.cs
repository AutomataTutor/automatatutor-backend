using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Z3;

using MSOZ3;

namespace AutomataPDL
{
    public abstract class PDLPos : PDL
    {
        public override string GetNodeName()
        {
            return "po:" + name;
        }

        /* while constructing the formula, you need to mention the free
         * variables with which you want it
         */
        public abstract MSOFormula ToMSO(FreshGen fg, string v);

        public abstract int Eval(string str, Dictionary<string, int> A); // A is the assignment

        public abstract int CompareTo(PDLPos pos);

        public abstract CPDLPos GetCPDL();
    }

    public class PDLPosVar : PDLPos, IMatchable<string>
    {
        internal string var;

        string IMatchable<string>.GetArg() { return var; }

        public PDLPosVar(string var)
        {
            this.var = var;
            name = "PosVar";
        }

        public override MSOFormula ToMSO(FreshGen fg, string v)
        {
            return new MSOEqual(v, var);
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            return A[var];
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var);
        }

        public override int CompareTo(PDLPos pos)
        {
            var pp = pos as PDLPosVar;
            if (pp != null)
                return var.CompareTo(pp.var);

            return name.CompareTo(pos.name);
        }

        public override bool ContainsVar(String vName)
        {
            return vName == this.var;
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

        public override CPDLPos GetCPDL()
        {
            return new CPDLFirstOrderVarPos(this.var);
        }

        public override bool Equals(object obj)
        {
            PDLPosVar other = obj as PDLPosVar;
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

    #region PDLPosConstant
    public class PDLPosConstant : PDLPos, IMatchable<PDLPosConstantName>
    {
        internal PDLPosConstantName op;

        PDLPosConstantName IMatchable<PDLPosConstantName>.GetArg() { return op; }

        public PDLPosConstant(PDLPosConstantName op)
        {
            this.op = op;
            switch (op)
            {
                case PDLPosConstantName.First: name = "First"; break;
                case PDLPosConstantName.Last: name = "Last"; break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg, string v)
        {
            switch (op)
            {
                case PDLPosConstantName.First: return new MSOFirst(v);
                case PDLPosConstantName.Last: return new MSOLast(v);
                default: throw new PDLException("undefined operator");
            }
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            if (str == "")
                return -1; // 0 if using bitvec
            switch (op)
            {
                case PDLPosConstantName.First: return 0;
                case PDLPosConstantName.Last: return str.Length-1;
                default: throw new PDLException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case PDLPosConstantName.First: sb.Append("first"); break;
                case PDLPosConstantName.Last: sb.Append("last"); break;
                default: throw new PDLException("undefined operator");
            }            
        }


        public override int CompareTo(PDLPos pos)
        {
            return name.CompareTo(pos.name);
        }

        public override bool ContainsVar(String vName)
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

        public override CPDLPos GetCPDL()
        {
            return new CPDLFirstOrLastPos();
        }

        public override bool Equals(object obj)
        {
            PDLPosConstant other = obj as PDLPosConstant;
            if (other == null) { return false; }
            if (!this.op.Equals(other.op)) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = this.name.GetHashCode();
            hashCode = (hashCode * 31) + this.op.GetHashCode();
            return hashCode;
        }
    }

    public class PDLFirst : PDLPosConstant
    {
        public PDLFirst() : base(PDLPosConstantName.First) { }        
    }

    public class PDLLast : PDLPosConstant
    {
        public PDLLast() : base(PDLPosConstantName.Last) { }        
    } 
    #endregion

    #region PDLPosUnary
    public abstract class PDLPosUnary : PDLPos, IMatchable<PDLPos, PDLPosUnaryConstructor>
    {
        internal PDLPos pos;
        internal PDLPosUnaryConstructor op;

        PDLPos IMatchable<PDLPos, PDLPosUnaryConstructor>.GetArg1() { return pos; }
        PDLPosUnaryConstructor IMatchable<PDLPos, PDLPosUnaryConstructor>.GetArg2() { return op; }

        public PDLPosUnary(PDLPos p, PDLPosUnaryConstructor op)
        {
            this.pos = p;
            this.op = op;
            switch (op)
            {
                case PDLPosUnaryConstructor.Pred: name = "Pred"; break;
                case PDLPosUnaryConstructor.Succ: name = "Succ"; break;
                default: throw new PDLException("undefined operator");
            } 
        }

        public override MSOFormula ToMSO(FreshGen fg, string v)
        {
            switch (op)
            {
                case PDLPosUnaryConstructor.Pred:
                    {
                        int c = fg.get();
                        string x1 = "_x1_" + c.ToString();
                        return new MSOExistsFO(x1, new MSOAnd(pos.ToMSO(fg, x1), new MSOSucc(v, x1)));
                    }
                case PDLPosUnaryConstructor.Succ:
                    {
                        int c = fg.get();
                        string x = "_x_" + c.ToString();
                        return new MSOExistsFO(x, new MSOAnd(pos.ToMSO(fg,x), new MSOSucc(x, v)));
                    }
                default: throw new PDLException("undefined operator");
            }
        }

        public override int Eval(string str, Dictionary<string, int> A)
        {
            int s = pos.Eval(str, A);
            switch (op)
            {
                case PDLPosUnaryConstructor.Pred: return s - 1;
                case PDLPosUnaryConstructor.Succ: return s + 1;
                default: throw new PDLException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case PDLPosUnaryConstructor.Pred: sb.Append("P("); break;
                case PDLPosUnaryConstructor.Succ: sb.Append("S("); break;
                default: throw new PDLException("undefined operator");
            }  
            pos.ToString(sb);
            sb.Append(")");
                      
        }

        public override int CompareTo(PDLPos pos)
        {
            var pp = pos as PDLPosUnary;
            if (pp != null && name==pos.name)
                return pos.CompareTo(pp.pos);

            return name.CompareTo(pos.name);
        }

        public override bool ContainsVar(String vName)
        {
            return pos.ContainsVar(vName);
        }

        public override void ToTreeString(FreshGen fg, StringBuilder sb, int index, Dictionary<string, Pair<PDL, string>> nodes)
        {
            nodes.Add(this.GetNodeName() + ":" + index, new Pair<PDL, string>(this, "")); 
            int x1 = fg.get();
            sb.Append(string.Format("{0}:{2}-{1}:{3};", this.GetNodeName(), pos.GetNodeName(), index, x1));
            pos.ToTreeString(fg, sb, x1,nodes);
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
            return true;
        }

        public override bool Equals(object obj)
        {
            PDLPosUnary other = obj as PDLPosUnary;
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

    public class PDLSuccessor : PDLPosUnary, IMatchable<PDLPos>
    {
        PDLPos IMatchable<PDLPos>.GetArg() { return pos; }
        public PDLSuccessor(PDLPos p) : base(p, PDLPosUnaryConstructor.Succ) { }
        public override CPDLPos GetCPDL()
        {
            PDLSuccessor posUnary = this;
            int repetitions = 1;
            while (posUnary.pos is PDLSuccessor)
            {
                posUnary = posUnary.pos as PDLSuccessor;
                repetitions += 1;
            }
            CPDLInteger repetitionsChoice = new CPDLInteger(repetitions);
            CPDLPos pos = posUnary.pos.GetCPDL();

            return new CPDLPredOrSuccPos(repetitionsChoice, pos);
        }
    }

    public class PDLPredecessor : PDLPosUnary, IMatchable<PDLPos>
    {
        PDLPos IMatchable<PDLPos>.GetArg() { return pos; }
        public PDLPredecessor(PDLPos p) : base(p, PDLPosUnaryConstructor.Pred) { }
        public override CPDLPos GetCPDL()
        {
            PDLPredecessor posUnary = this;
            int repetitions = 1;
            while (posUnary.pos is PDLPredecessor)
            {
                posUnary = posUnary.pos as PDLPredecessor;
                repetitions += 1;
            }
            CPDLInteger repetitionsChoice = new CPDLInteger(repetitions);
            CPDLPos pos = posUnary.pos.GetCPDL();

            return new CPDLPredOrSuccPos(repetitionsChoice, pos);
        }
    }
    #endregion

    #region PDLStringPos
    public class PDLStringPos : PDLPos, IMatchable<string, PDLStringPosOperator>
    {
        internal string str;
        internal PDLStringPosOperator op;

        string IMatchable<string, PDLStringPosOperator>.GetArg1() { return str; }
        PDLStringPosOperator IMatchable<string, PDLStringPosOperator>.GetArg2() { return op; }

        public PDLStringPos(string str, PDLStringPosOperator op)
        {
            this.str = str;
            this.op = op;
            switch (op)
            {
                case PDLStringPosOperator.FirstOcc: name = "FirstOcc"; break;
                case PDLStringPosOperator.LastOcc: name = "LastOcc"; break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override int Eval(string input, Dictionary<string, int> A)
        {
            if (input == "")
                return -1;
            switch (op)
            {
                case PDLStringPosOperator.FirstOcc: return input.IndexOf(this.str);
                case PDLStringPosOperator.LastOcc: return input.LastIndexOf(this.str);
                default: throw new PDLException("undefined operator");
            }
        }

        public override MSOFormula ToMSO(FreshGen fg, string v)
        {
            switch (op)
            {
                case PDLStringPosOperator.FirstOcc:
                    {
                        int i = str.Length - 1;
                        string z_i, z_im;
                        MSOFormula phi = null;//new MSOForallFO("x", new MSOEqual("x", "x"));

                        //TODO make sure input string is non empty that is i/=0

                        phi = new MSOLabel("z0", str[0]);

                        while (i > 0)
                        {
                            z_i = "z" + i.ToString();
                            z_im = "z" + (i - 1).ToString();
                            phi = new MSOExistsFO(z_i, new MSOAnd(new MSOSucc(z_im, z_i),
                                new MSOAnd(new MSOLabel(z_i, str[i]), phi)));
                            i--;
                        }

                        return new MSOAnd(new MSOForallFO("z0", new MSOIf(phi, new MSOLessEq(v, "z0"))),
                            new MSOExistsFO("z0", new MSOAnd(phi, new MSOEqual(v, "z0"))));
                    }
                case PDLStringPosOperator.LastOcc:
                    {
                        int i = str.Length - 1;
                        string z_i, z_im;
                        MSOFormula phi = null;//new MSOForallFO("x", new MSOEqual("x", "x"));

                        //TODO make sure input string is non empty that is i/=0

                        phi = new MSOLabel("z0", str[0]);

                        while (i > 0)
                        {
                            z_i = "z" + i.ToString();
                            z_im = "z" + (i - 1).ToString();
                            phi = new MSOExistsFO(z_i, new MSOAnd(new MSOSucc(z_im, z_i),
                                new MSOAnd(new MSOLabel(z_i, str[i]), phi)));
                            i--;
                        }

                        return new MSOAnd(new MSOForallFO("z0", new MSOIf(phi, new MSOLessEq("z0", v))),
                            new MSOExistsFO("z0", new MSOAnd(phi, new MSOEqual(v, "z0"))));
                    }
                default: throw new PDLException("undefined operator");
            }

        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case PDLStringPosOperator.FirstOcc: sb.Append("(firstOcc '" + this.str + "')"); break;
                case PDLStringPosOperator.LastOcc: sb.Append("(lastOcc '" + this.str + "')"); break;
                default: throw new PDLException("undefined operator");
            }
        }

        public override int CompareTo(PDLPos pos)
        {
            var pp = pos as PDLStringPos;
            if (pp != null && name == pos.name)
                return this.str.CompareTo(pp.str);
            return name.CompareTo(pos.name);
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

        public override CPDLPos GetCPDL()
        {
            CPDLString stringChoice = new CPDLString(this.str);
            return new CPDLFirstOrLastOccPos(stringChoice);
        }

        public override bool Equals(object obj)
        {
            PDLStringPos other = obj as PDLStringPos;
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

    public class PDLFirstOcc : PDLStringPos, IMatchable<string>
    {
        string IMatchable<string>.GetArg() { return str; }
        public PDLFirstOcc(string str) : base(str, PDLStringPosOperator.FirstOcc) { }
    }

    public class PDLLastOcc : PDLStringPos, IMatchable<string>
    {
        string IMatchable<string>.GetArg() { return str; }
        public PDLLastOcc(string str) : base(str, PDLStringPosOperator.LastOcc) { }
    } 
    #endregion

}
