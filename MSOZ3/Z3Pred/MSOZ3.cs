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

    public static class MSOConsts
    {
        public const string defaultVar = "?";
    }

    public abstract class MSOZ3Formula
    {
        public WS1SZ3Formula WS1SZ3;

        public abstract void ToString(StringBuilder sb);

        public abstract WS1SZ3Formula ToWS1SZ3();

        public bool CheckUseOfVars()
        {
            return CheckUseOfVars(new List<string>(), new List<string>());
        }

        internal abstract bool CheckUseOfVars(List<string> fovar, List<string> sovar);

        public Automaton<Expr> getAutomata(Z3Provider z3p, Expr universe, Expr var, Sort sort)
        {
            return this.ToWS1SZ3().getAutomata(z3p, universe, var, sort);
        }

    }

    public class MSOZ3Exists : MSOZ3Formula
    {
        String variable;
        MSOZ3Formula phi;

        public MSOZ3Exists(String variable, MSOZ3Formula phi)
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
            if (!fovar.Contains(variable))
            {
                var sov1 = sovar.ToArray().ToList();
                sov1.Add(variable);
                if (phi.CheckUseOfVars(fovar, sov1)) return true;
            }
            return false;
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Exists(variable, phi.ToWS1SZ3());
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3ExistsFO : MSOZ3Formula
    {
        String variable;
        MSOZ3Formula phi;

        public MSOZ3ExistsFO(String variable, MSOZ3Formula phi)
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

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Exists(variable, new WS1SZ3And(new WS1SZ3Singleton(variable), phi.ToWS1SZ3()));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex1 " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3ExistsSO : MSOZ3Formula
    {
        String variable;
        MSOZ3Formula phi;

        public MSOZ3ExistsSO(String variable, MSOZ3Formula phi)
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

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Exists(variable, phi.ToWS1SZ3());
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("ex2 " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3Forall : MSOZ3Formula
    {
        String variable;
        MSOZ3Formula phi;

        public MSOZ3Forall(String variable, MSOZ3Formula phi)
        {
            this.phi = phi;
            this.variable = variable;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            if (!sovar.Contains(variable))
            {
                var fov1 = fovar;
                fov1.Add(variable);
                if (phi.CheckUseOfVars(fov1, sovar)) return true;
            }
            if (!fovar.Contains(variable))
            {
                var sov1 = sovar;
                sov1.Add(variable);
                if (phi.CheckUseOfVars(fovar, sov1)) return true;
            }
            return false;
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Not(new WS1SZ3Exists(variable, new WS1SZ3Not(phi.ToWS1SZ3())));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("all " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3ForallFO : MSOZ3Formula
    {
        String variable;
        MSOZ3Formula phi;

        public MSOZ3ForallFO(String variable, MSOZ3Formula phi)
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

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Not(new WS1SZ3Exists(variable, new WS1SZ3And(new WS1SZ3Singleton(variable), new WS1SZ3Not(phi.ToWS1SZ3()))));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("all1 " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3ForallSO : MSOZ3Formula
    {
        String variable;
        MSOZ3Formula phi;

        public MSOZ3ForallSO(String variable, MSOZ3Formula phi)
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

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Not(new WS1SZ3Exists(variable, new WS1SZ3Not(phi.ToWS1SZ3())));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("all2 " + variable + ".");
            sb.Append("(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3And : MSOZ3Formula
    {
        MSOZ3Formula left;
        MSOZ3Formula right;

        public MSOZ3And(MSOZ3Formula left, MSOZ3Formula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3And(left.ToWS1SZ3(), right.ToWS1SZ3());
            return WS1SZ3;
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

    public class MSOZ3Or : MSOZ3Formula
    {
        MSOZ3Formula left;
        MSOZ3Formula right;

        public MSOZ3Or(MSOZ3Formula left, MSOZ3Formula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            return new WS1SZ3Not(new WS1SZ3And(new WS1SZ3Not(left.ToWS1SZ3()), new WS1SZ3Not(right.ToWS1SZ3())));
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            //sb.Append(")");
            sb.Append(" V ");
            //sb.Append("(");
            right.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3If : MSOZ3Formula
    {
        MSOZ3Formula left;
        MSOZ3Formula right;

        public MSOZ3If(MSOZ3Formula left, MSOZ3Formula right)
        {
            this.left = left;
            this.right = right;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return left.CheckUseOfVars(fovar, sovar) && right.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Not(new WS1SZ3And(left.ToWS1SZ3(), new WS1SZ3Not(right.ToWS1SZ3())));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            left.ToString(sb);
            //sb.Append(")");
            sb.Append(" \u2799 ");
            //sb.Append("(");
            right.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3Not : MSOZ3Formula
    {
        MSOZ3Formula phi;

        public MSOZ3Not(MSOZ3Formula phi)
        {
            this.phi = phi;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return phi.CheckUseOfVars(fovar, sovar);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Not(phi.ToWS1SZ3());
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" \u00AC(");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    public class MSOZ3Subset : MSOZ3Formula
    {
        string set1, set2;

        public MSOZ3Subset(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return sovar.Contains(set1) && sovar.Contains(set2);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Subset(set1, set2);
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(" + set1 + " in " + set2 + ")");
        }
    }

    public class MSOZ3Singleton : MSOZ3Formula
    {
        string set;

        public MSOZ3Singleton(string set)
        {
            this.set = set;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return sovar.Contains(set);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Singleton(set);
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("Sing[" + set + "]");
        }
    }

    public class MSOZ3Predicate : MSOZ3Formula
    {
        string set;
        Expr predicate;

        public MSOZ3Predicate(string set, Expr predicate)
        {
            this.set = set;
            this.predicate = predicate;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(set) || sovar.Contains(set);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            return new WS1SZ3Predicate(set, predicate);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(predicate + "[" + set + "]");
        }
    }

    public class MSOZ3Succ : MSOZ3Formula
    {
        string set1;
        string set2;

        public MSOZ3Succ(string set1, string set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(set1);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Succ(set1, set2);
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" " + set2 + "=" + set1 + "+1 ");
        }
    }

    public class MSOZ3Last : MSOZ3Formula
    {
        string var1;

        public MSOZ3Last(string var1)
        {
            this.var1 = var1;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3And(new WS1SZ3Singleton(var1), new WS1SZ3Not(new WS1SZ3Exists(MSOConsts.defaultVar, new WS1SZ3Succ(var1, MSOConsts.defaultVar))));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("last(" + var1 + ")");
        }
    }

    public class MSOZ3First : MSOZ3Formula
    {
        string var1;

        public MSOZ3First(string var1)
        {
            this.var1 = var1;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3And(new WS1SZ3Singleton(var1), new WS1SZ3Not(new WS1SZ3Exists(MSOConsts.defaultVar, new WS1SZ3Succ(MSOConsts.defaultVar, var1))));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("first(" + var1 + ")");
        }
    }

    public class MSOZ3Belong : MSOZ3Formula
    {
        string var1;
        string var2;

        public MSOZ3Belong(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1) && sovar.Contains(var2);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3And(new WS1SZ3Singleton(var1), new WS1SZ3Subset(var1, var2));
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + " in " + var2);
        }
    }

    public class MSOZ3Equal : MSOZ3Formula
    {
        string var1;
        string var2;

        public MSOZ3Equal(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return (fovar.Contains(var1) && fovar.Contains(var2)) || (sovar.Contains(var1) && sovar.Contains(var2));
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            return new WS1SZ3And(
                    new WS1SZ3Subset(var1, var2),
                    new WS1SZ3Subset(var2, var1));
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + "=" + var2);
        }
    }

    public class MSOZ3Less : MSOZ3Formula
    {
        string var1;
        string var2;

        public MSOZ3Less(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool CheckUseOfVars(List<string> fovar, List<string> sovar)
        {
            return fovar.Contains(var1) && fovar.Contains(var2);
        }

        public override WS1SZ3Formula ToWS1SZ3()
        {
            if (WS1SZ3 == null)
                WS1SZ3 = new WS1SZ3Less(var1, var2);
            return WS1SZ3;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var1 + " < " + var2);
        }
    }

}