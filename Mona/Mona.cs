using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Automata;
using Microsoft.Z3;

using Antlr.Runtime;
using Antlr.Runtime.Tree;

//the abstract classes refer to the different types in Mona

namespace Mona
{

    #region Enums
    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum MonaStringPosOperator
    {
        FirstOcc, LastOcc
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum MonaBinarySetOperator
    {
        Intersection, Union
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum MonaComparisonOperator
    {
        Le, Leq, Ge, Geq, Eq
    }

    /// <summary>
    /// Pos Comparison operators
    /// </summary>
    public enum MonaPosComparisonOperator
    {
        Le, Leq, Ge, Geq, Eq, Succ, Pred
    }

    /// <summary>
    /// Pos unary constructor
    /// </summary>
    public enum MonaPosUnaryConstructor
    {
        Succ, Pred
    }

    /// <summary>
    /// Pos constants
    /// </summary>
    public enum MonaPosConstantName
    {
        First, Last
    }

    /// <summary>
    /// Logical operators
    /// </summary>
    public enum MonaLogicalOperator
    {
        And, Or, If, Iff
    }

    /// <summary>
    /// Quantifiers
    /// </summary>
    public enum MonaQuantifier
    {
        ExistsFO, ForallFO, ExistsSO, ForallSO
    }

    /// <summary>
    /// String queries
    /// </summary>
    public enum MonaStringQueryOp
    {
        StartsWith, EndsWith, Contains, IsString
    }

    public static class MonaEnumUtil
    {
        public static bool IsSymmetric(MonaPosComparisonOperator op)
        {
            return op == MonaPosComparisonOperator.Eq;
        }

        public static bool IsSymmetric(MonaComparisonOperator op)
        {
            return op == MonaComparisonOperator.Eq;
        }

        public static bool IsSymmetric(MonaLogicalOperator op)
        {
            return op != MonaLogicalOperator.If;
        }

        public static int GetIndex(MonaStringQueryOp op)
        {
            switch (op)
            {
                case MonaStringQueryOp.Contains: return 0;
                case MonaStringQueryOp.EndsWith: return 1;
                case MonaStringQueryOp.StartsWith: return 2;
                case MonaStringQueryOp.IsString: return 3;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaBinarySetOperator op)
        {
            switch (op)
            {
                case MonaBinarySetOperator.Intersection: return 0;
                case MonaBinarySetOperator.Union: return 1;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaPosUnaryConstructor op)
        {
            switch (op)
            {
                case MonaPosUnaryConstructor.Pred: return 0;
                case MonaPosUnaryConstructor.Succ: return 1;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaPosConstantName op)
        {
            switch (op)
            {
                case MonaPosConstantName.First: return 0;
                case MonaPosConstantName.Last: return 1;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaStringPosOperator op)
        {
            switch (op)
            {
                case MonaStringPosOperator.FirstOcc: return 0;
                case MonaStringPosOperator.LastOcc: return 1;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaComparisonOperator op)
        {
            switch (op)
            {
                case MonaComparisonOperator.Eq: return 0;
                case MonaComparisonOperator.Ge: return 1;
                case MonaComparisonOperator.Geq: return 2;
                case MonaComparisonOperator.Le: return 3;
                case MonaComparisonOperator.Leq: return 4;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaPosComparisonOperator op)
        {
            switch (op)
            {
                case MonaPosComparisonOperator.Eq: return 0;
                case MonaPosComparisonOperator.Ge: return 1;
                case MonaPosComparisonOperator.Geq: return 2;
                case MonaPosComparisonOperator.Le: return 3;
                case MonaPosComparisonOperator.Leq: return 4;
                case MonaPosComparisonOperator.Succ: return 5;
                case MonaPosComparisonOperator.Pred: return 6;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaLogicalOperator op)
        {
            switch (op)
            {
                case MonaLogicalOperator.And: return 0;
                case MonaLogicalOperator.If: return 1;
                case MonaLogicalOperator.Iff: return 2;
                case MonaLogicalOperator.Or: return 3;
                default: throw new MonaException("undefined operator");
            }
        }

        public static int GetIndex(MonaQuantifier q)
        {
            switch (q)
            {
                case MonaQuantifier.ExistsFO: return 0;
                case MonaQuantifier.ExistsSO: return 1;
                case MonaQuantifier.ForallFO: return 2;
                case MonaQuantifier.ForallSO: return 3;
                default: throw new MonaException("undefined quantifier");
            }
        }
    } 
    #endregion

    public class FreshGen
    {
        int count = 0;

        public FreshGen()
        {
            count = 0;
        }

        public int reset()
        {
            return count=0;
        }

        public int get()
        {
            return ++count;
        }
    }

    public abstract class MonaStat
    {   

        public abstract void ToString(StringBuilder sb);

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }
       
    }

    public class MonaMacro : MonaStat
    {
        internal string name;
        internal List<string> variables;
        internal MonaFormula phi;

        public MonaMacro(string name, List<string> variables, MonaFormula phi)
        {
            this.name = name;
            this.variables = new List<string>(variables);
            this.phi = phi;
        }        

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" macro ");
            sb.Append(name);
            sb.Append(" ( ");
            foreach(var v in variables)
                sb.Append(v+" ");
            sb.Append(" ) =  ");
            phi.ToString(sb);
        }
    }
    public class MonaPred : MonaStat
    {
        internal string name;
        internal List<string> variables;
        internal MonaFormula phi;

        public MonaPred(string name, List<string> variables, MonaFormula phi)
        {
            this.name = name;
            this.variables = new List<string>(variables);
            this.phi = phi;
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(" macro ");
            sb.Append(name);
            sb.Append(" ( ");
            foreach (var v in variables)
                sb.Append(v + " ");
            sb.Append(" ) =  ");
            phi.ToString(sb);
        }
    }    
}