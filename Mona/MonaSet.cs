using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Automata;
using Microsoft.Z3;

using AutomataPDL;

namespace Mona
{
    public abstract class MonaSet : MonaStat
    {
        public abstract PDLSet ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub);
    }

    public class MonaSetVar : MonaSet
    {
        internal string var;

        public MonaSetVar(string str)
        {
            var = str;
        }

        public override PDLSet ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            return new PDLSetVar(var);
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append(this.var);
        }
    }

    #region MonaSetBinary
    public class MonaSetBinary : MonaSet
    {
        internal MonaSet set1;
        internal MonaSet set2;
        internal MonaBinarySetOperator op;

        public MonaSetBinary(MonaSet set1, MonaSet set2, MonaBinarySetOperator op)
        {
            this.set1 = set1;
            this.set2 = set2;
            this.op = op;
        }

        public override PDLSet ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            switch (op)
            {
                case MonaBinarySetOperator.Intersection:
                    {
                        return new PDLIntersect(set1.ToPDL(macros, sub), set2.ToPDL(macros, sub));
                    }
                case MonaBinarySetOperator.Union:
                    {
                        return new PDLUnion(set1.ToPDL(macros, sub), set2.ToPDL(macros, sub));

                    }
                default: throw new MonaException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            set1.ToString(sb);
            switch (op)
            {
                case MonaBinarySetOperator.Intersection:
                    sb.Append(" inters "); break;
                case MonaBinarySetOperator.Union:
                    sb.Append(" U "); break;
                default: throw new MonaException("undefined operator");
            }
            set2.ToString(sb);
            sb.Append(")");
        }
    }

    public class MonaIntersect : MonaSetBinary
    {
        public MonaIntersect(MonaSet left, MonaSet right) : base(left, right, MonaBinarySetOperator.Intersection) { }
    }

    public class MonaUnion : MonaSetBinary
    {
        public MonaUnion(MonaSet left, MonaSet right) : base(left, right, MonaBinarySetOperator.Union) { }
    }
    #endregion    
}
