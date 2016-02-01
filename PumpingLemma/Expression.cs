using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Z3;
using System.Diagnostics.Contracts;
using System.Xml.Linq;

namespace PumpingLemma
{
    public class VariableType : Expression
    {
        private readonly string name;

        private static Dictionary<string, VariableType> allVariables =
            new Dictionary<string,VariableType>();

        private VariableType(string n)
        {
            name = n;
        }
        public override string ToString()
        {
            return name;
        }
        public static VariableType Variable(String name)
        {
            if (allVariables.ContainsKey(name))
                return allVariables[name];
            var ret = new VariableType(name);
            allVariables[name] = ret;
            return ret;
        }

        public override Expr toZ3(Context ctx)
        {
            return toZ3Int(ctx);
        }
        public ArithExpr toZ3Int(Context ctx)
        {
            return ctx.MkIntConst(name);
        }
        public override HashSet<VariableType> GetVariables()
        {
            return new HashSet<VariableType>(new []{ this });
        }

        static int fresh_variable_number = 0;
        public static VariableType FreshVariable()
        {
#if DEBUG
            var variable_name = "V_" + fresh_variable_number;
            fresh_variable_number = fresh_variable_number + 1;
#else
            var variable_name = "V_" + Guid.NewGuid().ToString("N");
#endif
            return Variable(variable_name);
        }


        public override XElement ToDisplayXML()
        {
            var root = new XElement("text");

            var tspan = new XElement("tspan");

            var x = this.ToString().Split('_');
            if (x.Count() != 2)
                tspan.Value = this.ToString();
            else
            {
                tspan.Add(new XElement("tspan", x[0]));
                var sub = new XElement("tspan", x[1]);
                sub.SetAttributeValue("dominant-baseline", "text-after-edge");
                sub.SetAttributeValue("font-size", "50%");
                tspan.Add(sub);
            }

            root.Add(tspan);
            return root;
        }
    }

    public abstract class Expression
    {
        public enum ExpressionType { Boolean, Integer };
        public ExpressionType expression_type;

        abstract public HashSet<VariableType> GetVariables();

        abstract public Expr toZ3(Context ctx);

        abstract public XElement ToDisplayXML();
    }

    // Should probably split this class into multiple ones
    // Let's see if we run into trouble later on
    abstract public class BooleanExpression : Expression
    {
        public enum OperatorType { Logical, Comparison, Quantifier };
        public OperatorType boolean_expression_type;

        public enum OperatorArity { Zero, One, Two, None };
        public OperatorArity boolean_operation_arity = OperatorArity.None;

        public Expression operand1;
        public Expression operand2;

        public override HashSet<VariableType> GetVariables()
        {
            HashSet<VariableType> variables = new HashSet<VariableType>();
            Action<Expression> aggregate = (expr) => {
                foreach (VariableType var in expr.GetVariables())
                    variables.Add(var);
            };
            switch (boolean_operation_arity)
            {
                case OperatorArity.Zero:
                    break;
                case OperatorArity.One:
                    aggregate(operand1);
                    break;
                case OperatorArity.Two:
                    aggregate(operand1);
                    aggregate(operand2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return variables;
        }

        public override sealed Expr toZ3(Context ctx)
        {
            return toZ3Bool(ctx);
        }

        abstract public BoolExpr toZ3Bool(Context ctx);

        private Tuple<Context, Model> buildSolverAndCheck()
        {
            Context ctx = new Context();
            var solver = ctx.MkSolver("LIA");
            solver.Add(this.toZ3Bool(ctx));
            var ans = solver.Check(new Expr[] { });
            if (ans == Status.UNKNOWN)
                throw new Z3FailedException("Could not check satisfiability for: " + this.ToString());
            else if (ans == Status.SATISFIABLE)
                return new Tuple<Context, Model>(ctx, solver.Model);
            else
                return null;
        }

        public bool isSatisfiable()
        {
            var model = buildSolverAndCheck();
            return (model != null);
        }
        public bool isMaybeSatisfiable()
        {
            try
            {
                return this.isSatisfiable();
            }
            catch (Z3FailedException)
            {
                return true;
            }
        }


        public Dictionary<VariableType, int> getModel()
        {
            var model = buildSolverAndCheck();
            if (model != null)
            {
                var ans = new Dictionary<VariableType, int>();
                foreach (var v in this.GetVariables())
                {
                    var value = model.Item2.Eval(
                        v.toZ3Int(model.Item1),
                        true // Model completion
                        );
                    if (!value.IsIntNum)
                        throw new Z3FailedException("Z3 is doing something weird!");
                    ans[v] = ((IntNum)value).Int;
                }
                return ans;
            }
            throw new ArgumentException("Formula " + this.ToString() + " unsatisfiable!");
        }
    }

    public class LogicalExpression : BooleanExpression
    {
        public enum LogicalOperator
        {
            And,
            Or,
            Not,
            True,
            False
        };

        public LogicalOperator logical_operator;

        public BooleanExpression boolean_operand1;
        public BooleanExpression boolean_operand2;

        private LogicalExpression(LogicalOperator op, BooleanExpression oper1, BooleanExpression oper2)
        {
            boolean_expression_type = OperatorType.Logical;
            switch (op)
            {
                case LogicalOperator.True:
                    boolean_operation_arity = OperatorArity.Zero;
                    break;
                case LogicalOperator.False:
                    boolean_operation_arity = OperatorArity.Zero;
                    break;
                case LogicalOperator.And:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case LogicalOperator.Or:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case LogicalOperator.Not:
                    boolean_operation_arity = OperatorArity.One;
                    break;
            }

            logical_operator = op;
            operand1 = oper1;
            operand2 = oper2;
            boolean_operand1 = oper1;
            boolean_operand2 = oper2;
        }

        public override BoolExpr toZ3Bool(Context ctx)
        {
            switch (this.logical_operator)
            {
                case LogicalOperator.True:
                    return ctx.MkTrue();
                case LogicalOperator.False:
                    return ctx.MkFalse();
                case LogicalOperator.And:
                    return ctx.MkAnd(new BoolExpr[] { boolean_operand1.toZ3Bool(ctx), boolean_operand2.toZ3Bool(ctx) });
                case LogicalOperator.Or:
                    return ctx.MkOr(new BoolExpr[] { boolean_operand1.toZ3Bool(ctx), boolean_operand2.toZ3Bool(ctx) });
                case LogicalOperator.Not:
                    return ctx.MkNot(boolean_operand1.toZ3Bool(ctx));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static BooleanExpression And(BooleanExpression op1, BooleanExpression op2)
        {
            if (op1 as LogicalExpression != null && (op1 as LogicalExpression).logical_operator == LogicalOperator.True)
                return op2;
            if (op2 as LogicalExpression != null && (op2 as LogicalExpression).logical_operator == LogicalOperator.True)
                return op1;
            return new LogicalExpression(LogicalOperator.And, op1, op2);
        }
        public static BooleanExpression And(params BooleanExpression[] ops)
        {
            return ops.Aggregate(LogicalExpression.True() as BooleanExpression, (x, y) => And(x, y));
        }
        public static BooleanExpression Implies(BooleanExpression op1, BooleanExpression op2)
        {
            return LogicalExpression.Or(LogicalExpression.Not(op1), op2);
        }
        public static BooleanExpression And(IEnumerable<BooleanExpression> ops)
        {
            return ops.Aggregate(LogicalExpression.True() as BooleanExpression, (x, y) => And(x, y));
        }
        public static LogicalExpression Or(BooleanExpression op1, BooleanExpression op2)
        {
            return new LogicalExpression(LogicalOperator.Or, op1, op2);
        }
        public static LogicalExpression Not(BooleanExpression op1)
        {
            return new LogicalExpression(LogicalOperator.Not, op1, null);
        }
        public static BooleanExpression True()
        {
            return new LogicalExpression(LogicalOperator.True, null, null);
        }
        public static BooleanExpression False()
        {
            return new LogicalExpression(LogicalOperator.False, null, null);
        }

        public override String ToString()
        {
            switch (this.logical_operator)
            {
                case LogicalOperator.True: return "True";
                case LogicalOperator.False: return "False";
                case LogicalOperator.And: return operand1.ToString() + " && " + operand2.ToString(); 
                case LogicalOperator.Or: return operand1.ToString() + " || " + operand2.ToString();
                case LogicalOperator.Not: return "!(" + operand1.ToString() + ")";
                default: throw new ArgumentException();
            }
        }

        public override XElement ToDisplayXML()
        {
            var root = new XElement("text");

            switch (this.logical_operator)
            {
                case LogicalOperator.True:
                    root.Add(new XElement("tspan", "True"));
                    break;
                case LogicalOperator.False:
                    root.Add(new XElement("tspan", "False"));
                    break;
                case LogicalOperator.And:
                    foreach (var child in this.operand1.ToDisplayXML().Elements())
                        root.Add(child);
                    // root.Add(new XElement("tspan", System.Net.WebUtility.HtmlEncode('\u2227'.ToString())));
                    root.Add(new XElement("tspan", " && "));
                    foreach (var child in this.operand2.ToDisplayXML().Elements())
                        root.Add(child);
                    break;
                case LogicalOperator.Or:
                    foreach (var child in this.operand1.ToDisplayXML().Elements())
                        root.Add(child);
                    // root.Add(new XElement("tspan", System.Net.WebUtility.HtmlEncode('\u2228'.ToString())));
                    root.Add(new XElement("tspan", " || "));
                    foreach (var child in this.operand2.ToDisplayXML().Elements())
                        root.Add(child);
                    break;
                case LogicalOperator.Not: 
                    root.Add(new XElement("tspan", "!"));
                    break;
                default: throw new ArgumentException();
            }

            return root;
        }
    }

    public class ComparisonExpression : BooleanExpression
    {
        public enum ComparisonOperator
        {
            GT,
            GEQ,
            LT,
            LEQ,
            EQ,
            NEQ
        };

        public ComparisonOperator comparison_operator;


        public LinearIntegerExpression arithmetic_operand1;
        public LinearIntegerExpression arithmetic_operand2;

        public override XElement ToDisplayXML()
        {
            var root = new XElement("text");
            foreach (var child in operand1.ToDisplayXML().Elements())
                root.Add(child);

            string operator_string;
            switch (this.comparison_operator)
            {
                case ComparisonOperator.EQ: operator_string = " = "; break;
                case ComparisonOperator.NEQ: operator_string = " != "; break;
                case ComparisonOperator.LEQ: operator_string = " <= "; break;
                case ComparisonOperator.GT: operator_string = " > "; break;
                case ComparisonOperator.LT: operator_string = " < "; break;
                case ComparisonOperator.GEQ: operator_string = " >= "; break;

                    /*
                case ComparisonOperator.EQ: operator_string = "="; break;
                case ComparisonOperator.NEQ: operator_string = "\u2260"; break;
                case ComparisonOperator.LEQ: operator_string = "\u2264"; break;
                case ComparisonOperator.GEQ: operator_string = "\u2265"; break;
                case ComparisonOperator.GT: operator_string = ">"; break;
                case ComparisonOperator.LT: operator_string = "<"; break;
                     */

                default: throw new ArgumentException();
            }
            root.Add(new XElement("tspan", operator_string));

            foreach (var child in operand2.ToDisplayXML().Elements())
                root.Add(child);
            return root;
        }

        private ComparisonExpression(ComparisonOperator op, LinearIntegerExpression oper1, LinearIntegerExpression oper2)
        {
            boolean_expression_type = OperatorType.Comparison;
            switch (op)
            {
                case ComparisonOperator.LEQ:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case ComparisonOperator.LT:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case ComparisonOperator.GEQ:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case ComparisonOperator.GT:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case ComparisonOperator.EQ:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                case ComparisonOperator.NEQ:
                    boolean_operation_arity = OperatorArity.Two;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            comparison_operator = op;
            operand1 = oper1;
            operand2 = oper2;
            arithmetic_operand1 = oper1;
            arithmetic_operand2 = oper2;
        }

        public override BoolExpr toZ3Bool(Context ctx)
        {
            switch (this.comparison_operator)
            {
                case ComparisonOperator.EQ: return ctx.MkEq(this.arithmetic_operand1.toZ3Int(ctx), this.arithmetic_operand2.toZ3Int(ctx));
                case ComparisonOperator.NEQ: return ctx.MkNot(ctx.MkEq(this.arithmetic_operand1.toZ3Int(ctx), this.arithmetic_operand2.toZ3Int(ctx)));
                case ComparisonOperator.LEQ: return ctx.MkLe(this.arithmetic_operand1.toZ3Int(ctx), this.arithmetic_operand2.toZ3Int(ctx));
                case ComparisonOperator.LT: return ctx.MkLt(this.arithmetic_operand1.toZ3Int(ctx), this.arithmetic_operand2.toZ3Int(ctx));
                case ComparisonOperator.GEQ: return ctx.MkGe(this.arithmetic_operand1.toZ3Int(ctx), this.arithmetic_operand2.toZ3Int(ctx));
                case ComparisonOperator.GT: return ctx.MkGt(this.arithmetic_operand1.toZ3Int(ctx), this.arithmetic_operand2.toZ3Int(ctx));
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            string operator_string;
            switch (this.comparison_operator)
            {
                case ComparisonOperator.EQ: operator_string = "="; break;
                case ComparisonOperator.NEQ: operator_string = "!="; break;
                case ComparisonOperator.LEQ: operator_string = "<="; break;
                case ComparisonOperator.LT: operator_string = "<"; break;
                case ComparisonOperator.GEQ: operator_string = ">="; break;
                case ComparisonOperator.GT: operator_string = ">"; break;
                default: throw new ArgumentException();
            }
            return (
                operand1.ToString() + " " +
                operator_string + " " +
                operand2.ToString()
                );
        }

        public static ComparisonExpression LessThan(LinearIntegerExpression op1, LinearIntegerExpression op2)
        {
            return new ComparisonExpression(ComparisonOperator.LT, op1, op2);
        }
        public static ComparisonExpression LessThanOrEqual(LinearIntegerExpression op1, LinearIntegerExpression op2)
        {
            return new ComparisonExpression(ComparisonOperator.LEQ, op1, op2);
        }
        public static ComparisonExpression GreaterThan(LinearIntegerExpression op1, LinearIntegerExpression op2)
        {
            return new ComparisonExpression(ComparisonOperator.GT, op1, op2);
        }
        public static ComparisonExpression GreaterThanOrEqual(LinearIntegerExpression op1, LinearIntegerExpression op2)
        {
            return new ComparisonExpression(ComparisonOperator.GEQ, op1, op2);
        }
        public static ComparisonExpression Equal(LinearIntegerExpression op1, LinearIntegerExpression op2)
        {
            return new ComparisonExpression(ComparisonOperator.EQ, op1, op2);
        }
        public static ComparisonExpression NotEqual(LinearIntegerExpression op1, LinearIntegerExpression op2)
        {
            return new ComparisonExpression(ComparisonOperator.NEQ, op1, op2);
        }
        public static ComparisonExpression LessThan(LinearIntegerExpression op1, int op2)
        {
            return LessThan(op1, LinearIntegerExpression.Constant(op2));
        }
        public static ComparisonExpression LessThanOrEqual(LinearIntegerExpression op1, int op2)
        {
            return LessThanOrEqual(op1, LinearIntegerExpression.Constant(op2));
        }
        public static ComparisonExpression GreaterThan(LinearIntegerExpression op1, int op2)
        {
            return GreaterThan(op1, LinearIntegerExpression.Constant(op2));
        }
        public static ComparisonExpression GreaterThanOrEqual(LinearIntegerExpression op1, int op2)
        {
            return GreaterThanOrEqual(op1, LinearIntegerExpression.Constant(op2));
        }
        public static ComparisonExpression Equal(LinearIntegerExpression op1, int op2)
        {
            return Equal(op1, LinearIntegerExpression.Constant(op2));
        }
        public static ComparisonExpression NotEqual(LinearIntegerExpression op1, int op2)
        {
            return NotEqual(op1, LinearIntegerExpression.Constant(op2));
        }
    }

    public class QuantifiedExpression : BooleanExpression
    {
        public enum Quantifier { Exists, Forall };
        public Quantifier quantifier;
        public HashSet<VariableType> quantified_variables;
        public BooleanExpression inner;

        public override XElement ToDisplayXML()
        {
            throw new NotImplementedException();
        }

        private QuantifiedExpression(Quantifier q, IEnumerable<VariableType> v, BooleanExpression oper1)
        {
            quantified_variables = new HashSet<VariableType>();
            quantified_variables.UnionWith(v);
            boolean_expression_type = OperatorType.Quantifier;
            boolean_operation_arity = OperatorArity.One;
            switch (q)
            {
                case Quantifier.Exists:
                case Quantifier.Forall:
                    quantifier = q;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            operand1 = inner = oper1;
        }

        public override HashSet<VariableType> GetVariables()
        {
            var all = base.GetVariables();
            foreach (var v in quantified_variables)
                all.Remove(v);
            return all;
        }
        public override BoolExpr toZ3Bool(Context ctx)
        {
            var qf = quantified_variables.Select(x => x.toZ3(ctx)).ToArray();
            if (this.quantifier == Quantifier.Exists)
                return ctx.MkExists(qf, inner.toZ3Bool(ctx));
            else if (this.quantifier == Quantifier.Forall)
                return ctx.MkForall(qf, inner.toZ3Bool(ctx));
            else
                throw new ArgumentException();
        }
        public override String ToString()
        {
            var builder = new StringBuilder();
            if (quantifier == Quantifier.Exists)
                builder.Append("exists ");
            else if (quantifier == Quantifier.Forall)
                builder.Append("forall ");
            else
                throw new ArgumentException();
            foreach (var v in quantified_variables)
            {
                builder.Append(v);
                builder.Append(" ");
            }
            builder.Append(".");
            builder.Append(inner.ToString());
            return builder.ToString();
        }

        public static QuantifiedExpression Make(Quantifier q, IEnumerable<VariableType> vars, BooleanExpression b)
        {
            var freeVars = b.GetVariables();
            foreach (var v in vars)
                if (!freeVars.Contains(v))
                    throw new ArgumentException();
            return new QuantifiedExpression(q, vars, b);
        }
        public static QuantifiedExpression Exists(IEnumerable<VariableType> vars, BooleanExpression b)
        {
            return Make(Quantifier.Exists, vars, b);
        }
        public static QuantifiedExpression Forall(IEnumerable<VariableType> vars, BooleanExpression b)
        {
            return Make(Quantifier.Forall, vars, b);
        }
    }

    // For now, we restrict ourselves to linear expressions
    public class LinearIntegerExpression : Expression
    {
        public Dictionary<VariableType, int> coefficients;
        public int constant;

        public override HashSet<VariableType> GetVariables()
        {
            var ret = new HashSet<VariableType>();
            foreach (VariableType key in coefficients.Keys)
                ret.Add(key);
            return ret;
        }

        public override Expr toZ3(Context ctx)
        {
            return this.toZ3Int(ctx);
        }

        public ArithExpr toZ3Int(Context ctx)
        {
            var terms = this.coefficients.Select(kv => ctx.MkMul(new ArithExpr[] { kv.Key.toZ3Int(ctx), ctx.MkInt(kv.Value) })).ToList();
            terms.Add(ctx.MkInt(this.constant));
            return ctx.MkAdd(terms.ToArray());
        }

        public override String ToString()
        {
            if (this.isConstant())
                return this.constant.ToString();

            var s = new StringBuilder();
            s.Append(String.Join(" + ", coefficients.Keys.Select(v => {
                return ((coefficients[v]==1)?"":coefficients[v].ToString()) + v.ToString(); })));
            if (constant != 0)
            {
                if (coefficients.Count > 0)
                    s.Append(" + ");
                s.Append(constant.ToString());
            }
            return s.ToString();
        }

        private LinearIntegerExpression(int c, Dictionary<VariableType, int> coeff)
        {
            constant = c;
            coefficients = coeff;
            expression_type = ExpressionType.Integer;
        }

        public bool isConstant()
        {
            if (this.coefficients == null || this.coefficients.Count == 0)
                return true;
            foreach (var kv in this.coefficients)
                if (kv.Value != 0)
                    return false;
            return true;
        }

        public int Eval(Dictionary<VariableType, int> model)
        {
            int ans = this.constant;
            foreach (var kv in this.coefficients)
                ans += (kv.Value * model[kv.Key]);
            return ans;
        }

        // Factory methods
        public static LinearIntegerExpression Constant(int c)
        {
            return new LinearIntegerExpression(c, new Dictionary<VariableType, int>());
        }

        public static LinearIntegerExpression SingleTerm(int coefficient, VariableType v)
        {
            var coefficients = new Dictionary<VariableType, int>();
            coefficients[v] = coefficient;
            return new LinearIntegerExpression(0, coefficients);
        }

        public static LinearIntegerExpression Plus(IEnumerable<LinearIntegerExpression> terms)
        {
            int constant = 0;
            var coefficients = new Dictionary<VariableType, int>();
            Action<LinearIntegerExpression> accum =  (term) => {
                constant = constant + term.constant;
                foreach (VariableType v in term.coefficients.Keys)
                {
                    if (coefficients.ContainsKey(v))
                        coefficients[v] = coefficients[v] + term.coefficients[v];
                    else
                        coefficients[v] = term.coefficients[v];
                }
            };
            foreach (var term in terms)
                accum(term);
            return new LinearIntegerExpression(constant, coefficients);
        }

        public static LinearIntegerExpression FreshVariable()
        {
            return SingleTerm(1, VariableType.FreshVariable());
        }
        public static LinearIntegerExpression Variable(string name)
        {
            return SingleTerm(1, VariableType.Variable(name));
        }

        public static LinearIntegerExpression Times(int multiplier, LinearIntegerExpression b)
        {
            int constant = b.constant * multiplier;
            var coefficients = new Dictionary<VariableType, int>();
            foreach (var kv in b.coefficients)
                coefficients[kv.Key] = kv.Value * multiplier;
            return new LinearIntegerExpression(constant, coefficients);
        }
        public static LinearIntegerExpression Plus(LinearIntegerExpression a, LinearIntegerExpression b)
        {
            return Plus(new List<LinearIntegerExpression> { a, b });
        }

        public static LinearIntegerExpression operator +(LinearIntegerExpression a, LinearIntegerExpression b)
        {
            return Plus(a, b);
        }
        public static LinearIntegerExpression operator +(LinearIntegerExpression a, int b)
        {
            return Plus(a, LinearIntegerExpression.Constant(b));
        }
        public static LinearIntegerExpression operator -(LinearIntegerExpression a, LinearIntegerExpression b)
        {
            return Plus(a, LinearIntegerExpression.Times(-1, b));
        }
        public static LinearIntegerExpression operator -(LinearIntegerExpression a, int b)
        {
            return Plus(a, LinearIntegerExpression.Constant(-b));
        }

        public override XElement ToDisplayXML()
        {
            var root = new XElement("text");
            {
                int i = 0;
                foreach (var v in this.coefficients.Keys)
                {
                    if (this.coefficients[v] == 0)
                        continue;
                    if (this.coefficients[v] != 1)
                    {
                        root.Add(new XElement("tspan", this.coefficients[v].ToString()));
                    }

                    foreach (var child in v.ToDisplayXML().Elements())
                        root.Add(child);

                    i = i + 1;
                    if (i <  this.coefficients.Count())
                    {
                        root.Add(new XElement("tspan", " + "));
                    }
                }
            }
            
            if (this.constant == 0 && this.coefficients.Keys.Count == 0)
                root.Add(new XElement("tspan", this.constant.ToString()));
            else if (this.constant != 0)
                root.Add(new XElement("tspan", " + " + this.constant.ToString()));

            return root;
        }
    }

    public class Z3FailedException : Exception
    {
        public Z3FailedException() : base() { }
        public Z3FailedException(string message) : base(message) { }
        public Z3FailedException(string message, System.Exception inner) : base(message, inner) { }
    }
}
