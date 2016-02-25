using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;


namespace MSOZ3
{

    public abstract class MSOFormula
    {
        public WS1SFormula WS1S;

        public abstract void ToString(StringBuilder sb);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }

        internal abstract void ToMonaString(StringBuilder sb, CharSetSolver solver);

        public virtual string ToMonaString(List<string> props, CharSetSolver solver)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("m2l-str;");
            sb.AppendLine("var1 len where len in $ & len+1 notin $;");
            for (int p=0;p<props.Count;p++)
            {
                sb.AppendLine("var2 bit" + p + " where bit" + p + " sub $;");
                sb.AppendLine("macro is_" + p + "(var1 p) = p in bit" + p + ";");
            }
            this.ToMonaString(sb, solver);
            sb.Append(";");
            return sb.ToString();
        }

        public abstract WS1SFormula ToWS1S(CharSetSolver solver);

        public bool CheckUseOfVars()
        {
            return CheckUseOfVars(new List<string>(), new List<string>());
        }

        internal abstract bool CheckUseOfVars(List<string> fovar, List<string> sovar);

        public Automaton<BDD> getDFA(HashSet<char> alphabet, CharSetSolver solver)
        {            
            return this.ToWS1S(solver).getDFA(alphabet, solver);
        }

        public Automaton<BDD> getDFA(BDD alphabet, CharSetSolver solver)
        {
            return this.ToWS1S(solver).getDFA(alphabet, solver);
        }

    }

    public class MSOExistsFO : MSOFormula
    {
        String variable;
        MSOFormula phi;

        public MSOExistsFO(String variable, MSOFormula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            if (!sovar.Contains(variable))
            {
                var fov1 = fovar.ToArray().ToList();
                fov1.Add(variable);
                if (phi.CheckUseOfVars(fov1, sovar)) return true;
            }
            return false;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SExists(variable, new WS1SAnd(new WS1SSingleton(variable), phi.ToWS1S(solver)));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex1 " + variable + ".");            
            phi.ToString(sb);
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("ex1 " + variable + ":");
            phi.ToMonaString(sb, solver);
        }
    }

    public class MSOExistsSO : MSOFormula
    {
        String variable;
        MSOFormula phi;

        public MSOExistsSO(String variable, MSOFormula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            if (!fovar.Contains(variable))
            {
                var sov1 = sovar.ToArray().ToList();
                sov1.Add(variable);
                if (phi.CheckUseOfVars(fovar, sov1)) return true;
            }
            return false;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SExists(variable, phi.ToWS1S(solver));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex2 " + variable + ".");
            phi.ToString(sb);
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("ex2 " + variable + ":");
            phi.ToMonaString(sb, solver);
        }
    }

    public class MSOForallFO : MSOFormula
    {
        String variable;
        MSOFormula phi;

        public MSOForallFO(String variable, MSOFormula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            if (!sovar.Contains(variable))
            {
                var fov1 = fovar.ToArray().ToList();
                fov1.Add(variable);
                if (phi.CheckUseOfVars(fov1, sovar)) return true;
            }
            return false;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SNot(new WS1SExists(variable, new WS1SAnd(new WS1SSingleton(variable), new WS1SNot(phi.ToWS1S(solver)))));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("all1 " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("all1 " + variable + ":");
            phi.ToMonaString(sb, solver);
        }
    }

    public class MSOForallSO : MSOFormula
    {
        String variable;
        MSOFormula phi;

        public MSOForallSO(String variable, MSOFormula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            if (!fovar.Contains(variable))
            {
                var sov1 = sovar.ToArray().ToList();
                sov1.Add(variable);
                if (phi.CheckUseOfVars(fovar, sov1)) return true;
            }
            return false;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SNot(new WS1SExists(variable, new WS1SNot(phi.ToWS1S(solver))));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("all2 " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("all2 " + variable + ":");
            phi.ToMonaString(sb, solver);
        }
    }

    public class MSOAnd : MSOFormula
    {
        MSOFormula left;
        MSOFormula right;

        public MSOAnd(MSOFormula left, MSOFormula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SAnd(left.ToWS1S(solver), right.ToWS1S(solver));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            sb.Append(" \u028C ");
            right.ToString(sb);
            sb.Append(")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("(");
            left.ToMonaString(sb, solver);
            sb.Append(" & ");
            right.ToMonaString(sb, solver);
            sb.Append(")");
        }
    }

    public class MSOOr : MSOFormula
    {
        public MSOFormula left;
        public MSOFormula right;

        public MSOOr(MSOFormula left, MSOFormula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SOr(left.ToWS1S(solver), right.ToWS1S(solver));

            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            sb.Append(" V ");
            right.ToString(sb);
            sb.Append(")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("(");
            left.ToMonaString(sb, solver);
            sb.Append(" | ");
            right.ToMonaString(sb, solver);
            sb.Append(")");
        }
    }

    public class MSOIf : MSOFormula
    {
        MSOFormula left;
        MSOFormula right;

        public MSOIf(MSOFormula left, MSOFormula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SOr(new WS1SNot(left.ToWS1S(solver)),right.ToWS1S(solver));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            sb.Append(" \u2799 ");
            right.ToString(sb);
            sb.Append(")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("(");
            left.ToMonaString(sb, solver);
            sb.Append(" => ");
            right.ToMonaString(sb, solver);
            sb.Append(")");
        }
    }

    public class MSOIff : MSOFormula
    {
        MSOFormula left;
        MSOFormula right;

        public MSOIff(MSOFormula left, MSOFormula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
            {
                var l = new MSOIf(left, right).ToWS1S(solver);
                var r = new MSOIf(right, left).ToWS1S(solver);
                WS1S = new WS1SAnd(l,r);
            }
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            sb.Append(" <-> ");
            right.ToString(sb);
            sb.Append(")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("(");
            left.ToMonaString(sb, solver);
            sb.Append(" <=> ");
            right.ToMonaString(sb, solver);
            sb.Append(")");
        }
    }

    public class MSONot : MSOFormula
    {
        MSOFormula phi;

        public MSONot(MSOFormula phi)
        {
            this.phi = phi;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return phi.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SNot(phi.ToWS1S(solver));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("~");
            phi.ToString(sb);
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("~");
            phi.ToMonaString(sb, solver);
        }
    }

    public class MSOTrue: MSOFormula
    {        
        public MSOTrue(){}

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return true;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1STrue();
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" True ");
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("true");
        }
    }

    public class MSOFalse : MSOFormula
    {
        public MSOFalse() { }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return true;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            return new WS1SFalse();
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" False ");
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("false");
        }
    }

    public class MSOSubset : MSOFormula
    {
        string set1, set2;

        public MSOSubset(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return sovar.Contains(set1) && sovar.Contains(set2);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SSubset(set1, set2);
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " in " + set2 + ")");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("(" + set1 + " sub " + set2 + ")");
        }
    }

    public class MSOSingleton : MSOFormula
    {
        string set;

        public MSOSingleton(string set)
        {
            this.set = set;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return sovar.Contains(set);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SSingleton(set);
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("Sing[" + set + "]");
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            throw new NotImplementedException();
        }
    }
    

    public class MSOUnaryPred : MSOFormula
    {
        internal string set;
        internal BDD pred;

        public MSOUnaryPred(string set, BDD pred)
        {
            this.set = set;
            this.pred = pred;            
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(set) || sovar.Contains(set);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SUnaryPred(set, pred);
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("pr[" + set + "]");
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            var addOr = false;
            foreach (var bv in solver.GenerateAllCharacters(pred, false))
            {
                if (addOr)
                    sb.Append(" | ");
                else
                    addOr = true;

                string s = BitVecUtil.GetIntBinaryString(bv);
                sb.Append("(");
                var c = s[0];
                if (c == '1')
                    sb.AppendFormat("is_{0}({1})", c, set);
                else
                    sb.AppendFormat("~is_{0}({1})", c, set);
                for (int i = 1; i < s.Length; i++)
                {
                    sb.Append(" & ");
                    c = s[i];
                    if (c == '1')
                        sb.AppendFormat("is_{0}({1})", c, set);
                    else
                        sb.AppendFormat("~is_{0}({1})", c, set);
                }
                sb.Append(")");                
            }
        }
    }

    public class MSOLabel : MSOFormula
    {
        char label;
        string set;

        public MSOLabel(string set, char label){
            this.set = set;
            this.label = label;
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SUnaryPred(set, solver.MkCharConstraint(false,label));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(label + "[" + set + "]");
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            string s = BitVecUtil.GetIntBinaryString(label);
            var c = s[0];
            if (c == '1')
                sb.AppendFormat("is_{0}({1})", c, set);
            else
                sb.AppendFormat("~is_{0}({1})", c, set);
            for (int i = 1; i < s.Length; i++)
            {
                sb.Append(" & ");
                c = s[i];
                if (c == '1')
                    sb.AppendFormat("is_{0}({1})", c, set);
                else
                    sb.AppendFormat("~is_{0}({1})", c, set);
            }
        }
        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(set) || sovar.Contains(set);
        }
    }

    public class MSOSuccN : MSOFormula
    {
        internal string set1;
        internal string set2;
        internal int n;

        public MSOSuccN(string set1, string set2, int n)
        {
            this.set1 = set1;
            this.set2 = set2;
            this.n = n;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(set1);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SSuccN(set1, set2,n);
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" " + set2 + "=" + set1 + "+"+n);
        }

        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append(" " + set2 + "=" + set1 + "+" + n);
        }
    }

    public class MSOSucc : MSOSuccN
    {

        public MSOSucc(string set1, string set2) : base(set1, set2, 1) { }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SSucc(set1, set2);
            return WS1S;
        }
    }

    public class MSOLast : MSOFormula
    {
        string var1;

        public MSOLast(string var1)
        {
            this.var1 = var1;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            return new WS1SLast(var1);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("last(" + var1 + ")");
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append("("+var1 + "= len)");
        }
    }

    public class MSOFirst : MSOFormula
    {
        string var1;

        public MSOFirst(string var1)
        {
            this.var1 = var1;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            return new WS1SFirst(var1);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("first(" + var1 + ")");
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append(var1 + "=0");
        }
    }

    public class MSOBelong : MSOFormula
    {
        string var1;
        string var2;

        public MSOBelong(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1) && sovar.Contains(var2);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SAnd(new WS1SSingleton(var1), new WS1SSubset(var1, var2));
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + " in " + var2);
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append(var1 + " in " + var2);
        }
    }

    public class MSOEqual : MSOFormula
    {
        string var1;
        string var2;

        public MSOEqual(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return (fovar.Contains(var1) && fovar.Contains(var2)) || (sovar.Contains(var1) && sovar.Contains(var2));
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            return new WS1SEqual(var1,var2);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + "=" + var2);
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append(var1 + "=" + var2);
        }
    }

    public class MSOLess : MSOFormula
    {
        string var1;
        string var2;

        public MSOLess(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1) && fovar.Contains(var2);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SLess(var1, var2);
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + " < " + var2);
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append(var1 + "<" + var2);
        }
    }

    public class MSOLessEq : MSOFormula
    {
        string var1;
        string var2;

        public MSOLessEq(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1) && fovar.Contains(var2);
        }

        public override WS1SFormula ToWS1S(CharSetSolver solver)
        {
            if (WS1S == null)
                WS1S = new WS1SLessOrEqual(var1,var2);
            return WS1S;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + " <= " + var2);
        }
        internal override void ToMonaString(StringBuilder sb, CharSetSolver solver)
        {
            sb.Append(var1 + "<=" + var2);
        }
    }
}