using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Automata;

//the abstract classes refer to the different types in PDL

namespace AutomataPDL
{

    #region Enums
    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum PDLStringPosOperator
    {
        FirstOcc, LastOcc
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum PDLBinarySetOperator
    {
        Intersection, Union
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum PDLComparisonOperator
    {
        Le, Leq, Ge, Geq, Eq
    }

    /// <summary>
    /// Pos Comparison operators
    /// </summary>
    public enum PDLPosComparisonOperator
    {
        Le, Leq, Ge, Geq, Eq, Succ, Pred
    }

    /// <summary>
    /// Pos unary constructor
    /// </summary>
    public enum PDLPosUnaryConstructor
    {
        Succ, Pred
    }

    /// <summary>
    /// Pos constants
    /// </summary>
    public enum PDLPosConstantName
    {
        First, Last
    }

    /// <summary>
    /// Logical operators
    /// </summary>
    public enum PDLLogicalOperator
    {
        And, Or, If, Iff
    }

    /// <summary>
    /// Quantifiers
    /// </summary>
    public enum PDLQuantifier
    {
        ExistsFO, ForallFO, ExistsSO, ForallSO
    }

    /// <summary>
    /// String queries
    /// </summary>
    public enum PDLStringQueryOp
    {
        StartsWith, EndsWith, Contains, IsString
    }

    public static class PDLEnumUtil
    {
        public static bool IsSymmetric(PDLPosComparisonOperator op)
        {
            return op == PDLPosComparisonOperator.Eq;
        }

        public static bool IsSymmetric(PDLComparisonOperator op)
        {
            return op == PDLComparisonOperator.Eq;
        }

        public static bool IsSymmetric(PDLLogicalOperator op)
        {
            return op != PDLLogicalOperator.If;
        }

        public static int GetIndex(PDLStringQueryOp op)
        {
            switch (op)
            {
                case PDLStringQueryOp.Contains: return 0;
                case PDLStringQueryOp.EndsWith: return 1;
                case PDLStringQueryOp.StartsWith: return 2;
                case PDLStringQueryOp.IsString: return 3;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLBinarySetOperator op)
        {
            switch (op)
            {
                case PDLBinarySetOperator.Intersection: return 0;
                case PDLBinarySetOperator.Union: return 1;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLPosUnaryConstructor op)
        {
            switch (op)
            {
                case PDLPosUnaryConstructor.Pred: return 0;
                case PDLPosUnaryConstructor.Succ: return 1;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLPosConstantName op)
        {
            switch (op)
            {
                case PDLPosConstantName.First: return 0;
                case PDLPosConstantName.Last: return 1;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLStringPosOperator op)
        {
            switch (op)
            {
                case PDLStringPosOperator.FirstOcc: return 0;
                case PDLStringPosOperator.LastOcc: return 1;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLComparisonOperator op)
        {
            switch (op)
            {
                case PDLComparisonOperator.Eq: return 0;
                case PDLComparisonOperator.Ge: return 1;
                case PDLComparisonOperator.Geq: return 2;
                case PDLComparisonOperator.Le: return 3;
                case PDLComparisonOperator.Leq: return 4;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLPosComparisonOperator op)
        {
            switch (op)
            {
                case PDLPosComparisonOperator.Eq: return 0;
                case PDLPosComparisonOperator.Ge: return 1;
                case PDLPosComparisonOperator.Geq: return 2;
                case PDLPosComparisonOperator.Le: return 3;
                case PDLPosComparisonOperator.Leq: return 4;
                case PDLPosComparisonOperator.Succ: return 5;
                case PDLPosComparisonOperator.Pred: return 6;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLLogicalOperator op)
        {
            switch (op)
            {
                case PDLLogicalOperator.And: return 0;
                case PDLLogicalOperator.If: return 1;
                case PDLLogicalOperator.Iff: return 2;
                case PDLLogicalOperator.Or: return 3;
                default: throw new PDLException("undefined operator");
            }
        }

        public static int GetIndex(PDLQuantifier q)
        {
            switch (q)
            {
                case PDLQuantifier.ExistsFO: return 0;
                case PDLQuantifier.ExistsSO: return 1;
                case PDLQuantifier.ForallFO: return 2;
                case PDLQuantifier.ForallSO: return 3;
                default: throw new PDLException("undefined quantifier");
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

    public abstract class PDL
    {
        internal string name;        

        public abstract void ToString(StringBuilder sb);

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        //Return true if the formula is complex and the English description will look bad
        public abstract bool IsComplex();

        public abstract bool ContainsVar(String vName);        

        public abstract int GetFormulaSize();

        public abstract string GetNodeName();

        public abstract void getPDLClosure(HashSet<PDL> set);

        public virtual IEnumerable<PDL> getPDLClosure(){
            HashSet<PDL> set = new HashSet<PDL>();
            getPDLClosure(set);
            return set;
        }

        public abstract void ToTreeString(FreshGen fg , StringBuilder sb, int ind, Dictionary<string, Pair<PDL, string>> nodes);

        public virtual string ToTreeString(Dictionary<string, Pair<PDL, string>> nodes)
        {
            FreshGen fg = new FreshGen();
            StringBuilder sb = new StringBuilder();
            fg.reset();
            ToTreeString(fg,sb,0,nodes);
            nodes.Add("AAA:0",new Pair<PDL,string>(null,""));
            return "AAA:0-" + this.GetNodeName() + ":0;" + sb.ToString();
        }

        //public static PDLPred ParseString(string s)
        //{
        //    ANTLRStringStream Input = new ANTLRStringStream(s);
        //    PDLGrammarLexer Lexer = new PDLGrammarLexer(Input);
        //    CommonTokenStream Tokens = new CommonTokenStream(Lexer);
        //    PDLGrammarParser Parser = new PDLGrammarParser(Tokens);
        //    return Parser.parse();            
        //}
    }
    
}