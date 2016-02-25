using System;
using System.Collections.Generic;
using System.Text;

using MSOZ3;
using Microsoft.Automata;

namespace LTLMSO
{
    public abstract class LTLFormula
    {
        public abstract void ToString(StringBuilder sb, bool isSpot);

        public virtual string ToString(bool isSpot)
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb, isSpot);
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb, false);
            return sb.ToString();
        }

        public virtual MSOFormula ToMSO(bool monaFlag = false)
        {
            return new MSOExistsFO("i0", new MSOAnd(new MSOFirst("i0"), Simplify().ToMSO(0, monaFlag)));
        }

        public abstract MSOFormula ToMSO(int x, bool monaFlag);

        public abstract LTLFormula Simplify();

        public Automaton<BDD> getDFA(List<string> atoms, CharSetSolver solver)
        {
            var alph = solver.MkCharSetFromRange((char)0, (char)(Math.Pow(2, atoms.Count) - 1));
            return this.ToMSO().getDFA(alph, solver);
        }

        public virtual bool IsSatisfiable(List<string> atoms, CharSetSolver solver)
        {
            return !this.getDFA(atoms, solver).IsEmpty;
        }

        public virtual bool IsEquivalentWith(LTLFormula phi, List<string> atoms,  CharSetSolver solver)
        {
            return this.getDFA(atoms,solver).IsEquivalentWith(phi.getDFA(atoms,solver),solver);
        }

    }

    public class LTLPred : LTLFormula
    {
        int index;
        BDD predicate;

        public LTLPred(string atom, List<string> atoms, CharSetSolver solver)
        {
            this.index = atoms.IndexOf(atom);
            if (this.index == -1 || atoms.Count>16)
                throw new LTLException(string.Format("{0} not in the alphabet, or too many atoms", atom));
            this.predicate = solver.MkAnd(solver.MkSetWithBitTrue(index), solver.MkCharSetFromRange((char)0,(char)(Math.Pow(2,atoms.Count)-1)));          
        }

        public override MSOFormula ToMSO(int n, bool monaFlag)
        {
            var i = "i"+n;
            if(monaFlag)
                return new MSOBelong(i, "bit"+index);
            else
                return new MSOUnaryPred(i, predicate);
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            if(isSpot)
                sb.Append("p"+index);
            else
                sb.Append(predicate.ToString());
        }

        public override LTLFormula Simplify()
        {
            return this;
        }
    }

    public class LTLNot : LTLFormula
    {
        LTLFormula phi;

        public LTLNot(LTLFormula phi)
        {
            this.phi = phi;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSONot(phi.ToMSO(i,monaFlag));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append("~(");
            phi.ToString(sb, isSpot);
            sb.Append(")");
        }
        public override LTLFormula Simplify()
        {
            var p1 = phi.Simplify();
            if (p1 is LTLNot)
            {
                var c = p1 as LTLNot;
                return c.phi;
            }
            if (p1 is LTLTrue)
                return new LTLFalse();
            if (p1 is LTLFalse)
                return new LTLTrue();

            return new LTLNot(p1);
        }
    }

    public class LTLAnd : LTLFormula
    {
        LTLFormula phi1;
        LTLFormula phi2;

        public LTLAnd(LTLFormula phi1, LTLFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSOAnd(phi1.ToMSO(i, monaFlag), phi2.ToMSO(i, monaFlag));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append('(');
            phi1.ToString(sb, isSpot);
            sb.Append(" & ");
            phi2.ToString(sb, isSpot);
            sb.Append(')');
        }
        public override LTLFormula Simplify()
        {
            var p1 = phi1.Simplify();
            var p2 = phi2.Simplify();
            if (p1 is LTLFalse || p2 is LTLFalse)
                return new LTLFalse();
            if (p1 is LTLTrue)
                return p2;
            if (p2 is LTLTrue)
                return p1;

            return new LTLAnd(p1, p2);
        }
    }

    public class LTLOr : LTLFormula
    {
        LTLFormula phi1;
        LTLFormula phi2;

        public LTLOr(LTLFormula phi1, LTLFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSOOr(phi1.ToMSO(i, monaFlag), phi2.ToMSO(i, monaFlag));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append('(');
            phi1.ToString(sb, isSpot);
            sb.Append(" | ");
            phi2.ToString(sb, isSpot);
            sb.Append(')');
        }
        public override LTLFormula Simplify()
        {
            var p1 = phi1.Simplify();
            var p2 = phi2.Simplify();
            if (p1 is LTLTrue || p2 is LTLTrue)
                return new LTLTrue();
            if (p1 is LTLFalse)
                return p2;
            if (p2 is LTLFalse)
                 return p1;
            
            return new LTLOr(p1, p2);
        }
    }

    public class LTLIf : LTLFormula
    {
        LTLFormula phi1;
        LTLFormula phi2;

        public LTLIf(LTLFormula phi1, LTLFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSOIf(phi1.ToMSO(i, monaFlag), phi2.ToMSO(i, monaFlag));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append('(');
            phi1.ToString(sb, isSpot);
            sb.Append(" -> ");
            phi2.ToString(sb, isSpot);
            sb.Append(')');
        }

        public override LTLFormula Simplify()
        {
            var p1 = phi1.Simplify();
            var p2 = phi2.Simplify();
            if (p1 is LTLFalse || p2 is LTLTrue)
                return new LTLTrue();
            return new LTLIf(p1, p2);
        }
    }

    public class LTLIff : LTLFormula
    {
        LTLFormula phi1;
        LTLFormula phi2;

        public LTLIff(LTLFormula phi1, LTLFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSOIf(phi1.ToMSO(i, monaFlag), phi2.ToMSO(i, monaFlag));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append('(');
            phi1.ToString(sb, isSpot);
            sb.Append(" <-> ");
            phi2.ToString(sb, isSpot);
            sb.Append(')');
        }
        public override LTLFormula Simplify()
        {
            var p1 = phi1.Simplify();
            var p2 = phi2.Simplify();
            return new LTLIff(p1, p2);
        }
    }

    public class LTLNextN : LTLFormula
    {
        internal LTLFormula phi;
        internal int n;

        public LTLNextN(LTLFormula phi, int n)
        {
            this.phi = phi;
            this.n=n;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            string vari = "i" + i;
            string vari1 = "i" + (i + 1);
            return new MSOExistsFO(vari1, new MSOAnd(new MSOSuccN(vari, vari1,n), phi.ToMSO(i + 1, monaFlag)));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.AppendFormat("X^{0} ",n);
            phi.ToString(sb, isSpot);
        }
        public override LTLFormula Simplify()
        {
            var p1 = phi.Simplify();
            if (p1 is LTLNextN)
            {
                var c = p1 as LTLNextN;
                return new LTLNextN(c.phi,c.n+n);
            }
            return new LTLNextN(p1, n);
        }
    }

    public class LTLNext : LTLNextN
    {

        public LTLNext(LTLFormula phi):base(phi, 1){}

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            string vari="i"+i;
            string vari1="i"+(i+1);
            return new MSOExistsFO(vari1,new MSOAnd(new MSOSucc(vari,vari1), phi.ToMSO(i+1, monaFlag)));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {            
            sb.Append("X ");
            phi.ToString(sb, isSpot);
        }
    }

    public class LTLUntil : LTLFormula
    {
        LTLFormula phi1;
        LTLFormula phi2;

        public LTLUntil(LTLFormula phi1, LTLFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            string vari = "i" + i;
            string varj = "i" + (i + 1);
            string vark = "i" + (i + 2);
            int j=i+1;
            int k=i+2;

            return new MSOExistsFO(varj, new MSOAnd(new MSOLessEq(vari, varj), 
                new MSOAnd(
                    phi2.ToMSO(j, monaFlag),
                    new MSOForallFO(vark,
                        new MSOIf(
                            new MSOAnd(
                                new MSOLessEq(vari,vark),
                                new MSOLess(vark,varj)
                            ),
                            phi1.ToMSO(k, monaFlag)
                        )
                    )
                )));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append('(');
            phi1.ToString(sb, isSpot);
            sb.Append(" U ");
            phi2.ToString(sb, isSpot);
            sb.Append(')');
        }

        public override LTLFormula Simplify()
        {
            return new LTLUntil(phi1.Simplify(), phi2.Simplify());
        }
    }

    public class LTLTrue : LTLFormula
    {
        public LTLTrue(){ }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSOTrue();
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append("true");
        }
        public override LTLFormula Simplify()
        {
            return this;
        }
    }

    public class LTLFalse : LTLFormula
    {
        public LTLFalse() { }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            return new MSOFalse();
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append("false");
        }
        public override LTLFormula Simplify()
        {
            return this;
        }
    }

    public class LTLEventually : LTLFormula
    {
        LTLFormula phi;

        public LTLEventually(LTLFormula phi)
        {
            this.phi = phi;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            string vari = "i" + i;
            string varj = "i" + (i + 1);
            int j = i + 1;

            return new MSOExistsFO(varj, 
                new MSOAnd(
                    new MSOLessEq(vari, varj),
                    phi.ToMSO(j, monaFlag)
                ));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {            
            sb.Append("F ");
            phi.ToString(sb, isSpot);
        }
        public override LTLFormula Simplify()
        {
            var a = phi.Simplify();
            if (a is LTLEventually)
                return a;            
            return new LTLEventually(a);
        }
    }

    public class LTLGlobally : LTLFormula
    {
        LTLFormula phi;

        public LTLGlobally(LTLFormula phi)
        {
            this.phi = phi;
        }

        public override MSOFormula ToMSO(int i, bool monaFlag)
        {
            string vari = "i" + i;
            string varj = "i" + (i + 1);
            int j = i + 1;

            return new MSOForallFO(varj,
                new MSOIf(
                    new MSOLessEq(vari, varj),
                    phi.ToMSO(j, monaFlag)
                ));
        }

        public override void ToString(StringBuilder sb, bool isSpot)
        {
            sb.Append("G ");
            phi.ToString(sb, isSpot);
        }
        public override LTLFormula Simplify()
        {
            var a = phi.Simplify();
            if (a is LTLGlobally)
                return a;
            return new LTLGlobally(a);
        }
    }
}
