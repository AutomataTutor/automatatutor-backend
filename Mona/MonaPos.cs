using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Z3;

using AutomataPDL;

namespace Mona
{
    public abstract class MonaPos : MonaStat
    {
        public abstract PDLPos ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub);
    }

    public class MonaPosVar : MonaPos
    {
        string var;

        public MonaPosVar(string var)
        {
            this.var = var;
        }

        public override PDLPos ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            return new PDLPosVar(var);
        }

        public override void ToString(StringBuilder sb)
        {
            sb.Append(var);
        }
    }

    #region MonaPosConstant
    public class MonaPosConstant : MonaPos
    {
        internal MonaPosConstantName op;        

        public MonaPosConstant(MonaPosConstantName op)
        {
            this.op = op;
        }

        public override PDLPos ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            switch (op)
            {
                case MonaPosConstantName.First: return new PDLFirst();
                case MonaPosConstantName.Last: return new PDLLast();
                default: throw new MonaException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case MonaPosConstantName.First: sb.Append("first"); break;
                case MonaPosConstantName.Last: sb.Append("last"); break;
                default: throw new MonaException("undefined operator");
            }            
        }
    }

    public class MonaFirst : MonaPosConstant
    {
        public MonaFirst() : base(MonaPosConstantName.First) { }        
    }

    public class MonaLast : MonaPosConstant
    {
        public MonaLast() : base(MonaPosConstantName.Last) { }        
    } 
    #endregion

    #region MonaPosUnary
    public class MonaPosUnary : MonaPos
    {
        internal MonaPos pos;
        internal MonaPosUnaryConstructor op;

        public MonaPosUnary(MonaPos p, MonaPosUnaryConstructor op)
        {
            this.pos = p;
            this.op = op;
        }

        public override PDLPos ToPDL(List<MonaMacro> macros, Dictionary<string, string> sub)
        {
            switch (op)
            {
                case MonaPosUnaryConstructor.Succ:
                    {
                        return new PDLSuccessor(pos.ToPDL(macros, sub));
                    }
                default: throw new MonaException("undefined operator");
            }
        }

        public override void ToString(StringBuilder sb)
        {
            switch (op)
            {
                case MonaPosUnaryConstructor.Pred: sb.Append("P("); break;
                case MonaPosUnaryConstructor.Succ: sb.Append("S("); break;
                default: throw new MonaException("undefined operator");
            }  
            pos.ToString(sb);
            sb.Append(")");
                      
        }
    }

    public class MonaSuccessor : MonaPosUnary
    {
        public MonaSuccessor(MonaPos p) : base(p, MonaPosUnaryConstructor.Succ) { }
    }

    public class MonaPredecessor : MonaPosUnary
    {
        public MonaPredecessor(MonaPos p) : base(p, MonaPosUnaryConstructor.Pred) { }
    }
    #endregion
}
