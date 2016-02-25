using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Z3;
using AutomataPDL;

namespace Mona
{

    public abstract class MonaFormula : MonaStat
    {
        public abstract PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub);

        public virtual bool IsEquivalentWith(MonaFormula phi, HashSet<Char> alphabet, CharSetSolver solver)
        {
            var p1 = new MonaIff(phi, this);
            var dfa = p1.GetDFA(alphabet, solver);
            return !dfa.IsEmpty;
        }

        public virtual bool Issatisfiable(HashSet<Char> alphabet, CharSetSolver solver)
        {
            var dfa = GetDFA(alphabet, solver);
            return !dfa.IsEmpty;
        }

        /// <summary>
        /// Compute the DFA corresponding to the Monapred, null if it can't find it
        /// </summary>
        /// <param name="alphabet">DFA alphabet</param>
        /// <param name="solver">Char solver</param>
        /// <returns>the DFA corresponding to the Monapred, null if it can't find it</returns>
        public virtual Automaton<BDD> GetDFA(HashSet<Char> alphabet, CharSetSolver solver)
        {
            try
            {
                return ToPDL(null, null).GetDFA(alphabet, solver);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    public class MonaMacroApp : MonaFormula
    {
        internal string name;
        internal List<string> variables;

        public MonaMacroApp(string name, List<string> variables)
        {
            this.name = name;
            this.variables = new List<string>(variables);
        }        

        public override void ToString(StringBuilder sb)
        {
            sb.Append(name);
            sb.Append(" in ");
            foreach(var v in variables)
                sb.Append(v+" ");
        }
        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            foreach(var macro in macros)
                if (macro.name == name)
                {
                    var s = new Dictionary<string, string>();
                    for (int i = 0; i < variables.Count; i++)
                        s[macro.variables[i]] = variables[i];
                    
                    return macro.phi.ToPDL(new List<MonaMacro>(), s);
                }
            return null;
        }
    }

    public class MonaBelongs : MonaFormula
    {
        internal MonaPos pos;
        internal MonaSet set;

        public MonaBelongs(MonaPos p, MonaSet S)
        {
            this.pos = p;
            this.set = S;
        }        

        public override void ToString(StringBuilder sb)
        {
            pos.ToString(sb);
            sb.Append(" in ");
            set.ToString(sb);
        }
        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            return new PDLBelongs(pos.ToPDL(macros, sub), set.ToPDL(macros,  sub));
        }
    }

    #region MonaBinaryPosFormula
    public class MonaBinaryPosFormula : MonaFormula
    {
        internal MonaPos pos1;
        internal MonaPos pos2;
        internal MonaPosComparisonOperator op;

        public MonaBinaryPosFormula(MonaPos p1, MonaPos p2, MonaPosComparisonOperator op)
        {
            this.pos1 = p1;
            this.pos2 = p2;
            this.op = op;
        }
        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            var m1 = pos1.ToPDL(macros, sub);
            var m2 = pos2.ToPDL(macros, sub);
            switch (op)
            {
                case MonaPosComparisonOperator.Eq:
                    {
                        return new PDLPosEq(m1, m2);
                    }
                case MonaPosComparisonOperator.Ge: return new PDLPosGe(m1, m2);
                case MonaPosComparisonOperator.Geq: return new PDLPosGeq(m1, m2);
                case MonaPosComparisonOperator.Le:
                    {
                        return new PDLPosLe(m1, m2);
                    }
                case MonaPosComparisonOperator.Leq: return new PDLPosLe(m1, m2);                
                default: throw new MonaException("Undefined operator");
            }
        }        

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case MonaPosComparisonOperator.Eq: pos1.ToString(sb); sb.Append(" = "); pos2.ToString(sb);  break;
                case MonaPosComparisonOperator.Ge: pos1.ToString(sb); sb.Append(" > "); pos2.ToString(sb);  break;
                case MonaPosComparisonOperator.Geq: pos1.ToString(sb); sb.Append(" >= "); pos2.ToString(sb);  break;
                case MonaPosComparisonOperator.Le: pos1.ToString(sb); sb.Append(" < "); pos2.ToString(sb);  break;
                case MonaPosComparisonOperator.Leq: pos1.ToString(sb); sb.Append(" <= "); pos2.ToString(sb); break;
                default: throw new MonaException("Undefined operator");
            }
        }
    }

    public class MonaPosEq : MonaBinaryPosFormula{
        public MonaPosEq(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Eq) { }
    }

    public class MonaPosLe : MonaBinaryPosFormula{
        public MonaPosLe(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Le) { }
    }

    public class MonaPosLeq : MonaBinaryPosFormula{
        public MonaPosLeq(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Leq) { }
    }

    public class MonaPosGe : MonaBinaryPosFormula{
        public MonaPosGe(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Ge) { }
    }

    public class MonaPosGeq : MonaBinaryPosFormula{
        public MonaPosGeq(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Geq) { }
    }

    public class MonaIsPredecessor : MonaBinaryPosFormula{
        public MonaIsPredecessor(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Pred) { }
    }

    public class MonaIsSuccessor : MonaBinaryPosFormula
    {
        public MonaIsSuccessor(MonaPos p1, MonaPos p2) : base(p1, p2, MonaPosComparisonOperator.Succ) { }
    }
    #endregion

    public class MonaSetEq : MonaFormula
    {
       internal MonaSet set1;
       internal MonaSet set2;

       public MonaSetEq(MonaSet s1, MonaSet s2)
        {
            this.set1 = s1;
            this.set2 = s2;
        }
        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            var m1 = set1.ToPDL(macros, sub);
            var m2 = set2.ToPDL(macros, sub);
            return new PDLAnd(new PDLSubset(m1, m2), new PDLSubset(m2, m1));
        }

        public override void ToString(StringBuilder sb)
        {
            set1.ToString(sb); sb.Append(" = "); set2.ToString(sb);
        }
    }


    #region Quantified formula
    public class MonaQuantifiedFormula : MonaFormula
    {
        internal MonaFormula phi;
        internal List<String> vars;
        internal MonaQuantifier q;

        public MonaQuantifiedFormula(List<String> vars, MonaFormula phi, MonaQuantifier q)
        {
            this.phi = phi;
            this.vars = new List<string>(vars);
            this.q = q;
        }
        
        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            switch (q)
            {
                case MonaQuantifier.ExistsFO:
                    {
                        var ret = phi.ToPDL(macros, sub);
                        foreach(var v in vars)
                            return ret =  new PDLExistsFO(v, ret);
                        return ret;
                    }
                case MonaQuantifier.ExistsSO:
                    {
                        var ret = phi.ToPDL(macros, sub);
                        foreach (var v in vars)
                            return ret = new PDLExistsSO(v, ret);
                        return ret;
                    }
                case MonaQuantifier.ForallFO:
                    {
                        var ret = phi.ToPDL(macros, sub);
                        foreach (var v in vars)
                            return ret = new PDLForallFO(v, ret);
                        return ret;
                    }
                case MonaQuantifier.ForallSO:
                    {
                        var ret = phi.ToPDL(macros, sub);
                        foreach (var v in vars)
                            return ret = new PDLForallSO(v, ret);
                        return ret;
                    }
                default: throw new MonaException("Quantifier undefined");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (q)
            {
                case MonaQuantifier.ExistsFO: sb.Append("ex1 "); break;
                case MonaQuantifier.ExistsSO: sb.Append("ex2 "); break;
                case MonaQuantifier.ForallFO: sb.Append("all1 "); break;
                case MonaQuantifier.ForallSO: sb.Append("all2 "); break;
                default: throw new MonaException("Quantifier undefined");
            }
            foreach(var v in vars)
                sb.Append(v + " ");
            sb.Append(":");
            phi.ToString(sb);
        }
    }

    public class MonaForallFO : MonaQuantifiedFormula
    {
        public MonaForallFO(List<String> var, MonaFormula phi) : base(var, phi, MonaQuantifier.ForallFO) { }
    }

    public class MonaForallSO : MonaQuantifiedFormula
    {
        public MonaForallSO(List<String> var, MonaFormula phi) : base(var, phi, MonaQuantifier.ForallSO) { }
    }

    public class MonaExistsFO : MonaQuantifiedFormula
    {
        public MonaExistsFO(List<String> var, MonaFormula phi) : base(var, phi, MonaQuantifier.ExistsFO) { }
    }

    public class MonaExistsSO : MonaQuantifiedFormula
    {
        public MonaExistsSO(List<String> var, MonaFormula phi) : base(var, phi, MonaQuantifier.ExistsSO) { }
    }
    #endregion

    #region Binary logic formulae
    public class MonaBinaryFormula : MonaFormula
    {
        internal MonaFormula phi1;
        internal MonaFormula phi2;
        internal MonaLogicalOperator op;

        public MonaBinaryFormula(MonaFormula left, MonaFormula right, MonaLogicalOperator op)
        {
            this.phi1 = left;
            this.phi2 = right;
            this.op = op;
        }

        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            switch (op)
            {
                case MonaLogicalOperator.And: return new PDLAnd(phi1.ToPDL(macros,  sub), phi2.ToPDL(macros, sub));
                case MonaLogicalOperator.If: return new PDLIf(phi1.ToPDL(macros,  sub), phi2.ToPDL(macros,  sub));
                case MonaLogicalOperator.Iff: return new PDLIff(phi1.ToPDL(macros, sub), phi2.ToPDL(macros,  sub));
                case MonaLogicalOperator.Or: return new PDLOr(phi1.ToPDL(macros,  sub), phi2.ToPDL(macros,  sub));
            }
            throw new MonaException("undefined operator");
        }     

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            phi1.ToString(sb);
            switch (op)
            {
                case MonaLogicalOperator.And: sb.Append(" & "); break;
                case MonaLogicalOperator.If: sb.Append(" => "); break;
                case MonaLogicalOperator.Iff: sb.Append(" <=> "); break;
                case MonaLogicalOperator.Or: sb.Append(" | "); break;
            }
            phi2.ToString(sb);
            sb.Append(")");
        }
    }

    public class MonaAnd : MonaBinaryFormula{

        public MonaAnd(MonaFormula phi1, MonaFormula phi2)
            : base(phi1, phi2, MonaLogicalOperator.And)
        { }
    }

    public class MonaOr : MonaBinaryFormula{

        public MonaOr(MonaFormula phi1, MonaFormula phi2)
            : base(phi1, phi2, MonaLogicalOperator.Or)
        { }
    }

    public class MonaIf : MonaBinaryFormula{

        public MonaIf(MonaFormula phi1, MonaFormula phi2)
            : base(phi1, phi2, MonaLogicalOperator.If)
        { }
    }

    public class MonaIff : MonaBinaryFormula{

        public MonaIff(MonaFormula left, MonaFormula right)
            : base(left, right, MonaLogicalOperator.Iff)
        { }
    }
    #endregion

    public class MonaNot : MonaFormula
    {
        internal MonaFormula phi;

        public MonaNot(MonaFormula phi)
        {
            this.phi = phi;
        }

        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            return new PDLNot(phi.ToPDL(macros,  sub));
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(not ");
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    #region True False
    public class MonaTrue : MonaFormula
    {
        public MonaTrue()
        {
        }

        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            return new PDLTrue();
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append("true");
        }
    }

    public class MonaFalse : MonaFormula
    {
        public MonaFalse()
        {
        }

        public override PDLPred ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            return new PDLFalse();
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("false");
        }
    }
    #endregion

}
