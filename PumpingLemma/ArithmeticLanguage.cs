using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PumpingLemma
{
    public class ArithmeticLanguage
    {
        public IEnumerable<String> alphabet;
        public SymbolicString symbolic_string;
        public BooleanExpression constraint;

        private ArithmeticLanguage(IEnumerable<String> _alphabet, SymbolicString _symbolic_string, BooleanExpression _constraint)
        {
            this.alphabet = _alphabet;
            this.symbolic_string = _symbolic_string;
            this.constraint = _constraint;
        }

        public static ArithmeticLanguage FromTextDescriptions(List<String> alphabet, string symbolicStringText, string constraintText)
        {
            var symbolPattern = new Regex(@"^[a-zA-Z0-9]$");
            var illegalSymbols = alphabet.FindAll(s => !symbolPattern.IsMatch(s));
            if (illegalSymbols.Count > 0)
            {
                var message = string.Format(
                    "Found illegal symbols {0} in alphabet. Symbols should match [a-zA-Z0-9]", 
                    string.Join(", ", illegalSymbols)
                );
                throw new PumpingLemmaException(message);
            }

            // Parse the language
            var ss = PumpingLemma.Parser.parseSymbolicString(symbolicStringText, alphabet);
            if (ss == null)
                throw new PumpingLemmaException("Unable to parse language");

            // Parse the constraintDesc
            var constraint = PumpingLemma.Parser.parseCondition(constraintText);
            if (constraint == null)
                throw new PumpingLemmaException("Unable to parse constraint");

            // Make sure all the variables are bound
            var boundVariables = ss.GetIntegerVariables();
            var constraintVariables = constraint.GetVariables();
            // Console.WriteLine("Bound variables: " + String.Join(", ", boundVariables));
            // Console.WriteLine("Constriant variables: " + String.Join(", ", constraintVariables));
            foreach (var consVar in constraintVariables)
            {
                if (!boundVariables.Contains(consVar))
                    throw new PumpingLemmaException(
                        string.Format("Constraint variable {0} not bound", consVar));
            }

            // Add constraints saying that all variables are >= 0
            BooleanExpression defaultConstraint = LogicalExpression.True();
            foreach (var consVar in constraintVariables)
            {
                defaultConstraint = LogicalExpression.And(
                    defaultConstraint,
                    ComparisonExpression.GreaterThanOrEqual(
                        LinearIntegerExpression.SingleTerm(1, consVar),
                        LinearIntegerExpression.Constant(0)
                        )
                    );
            }

            return new ArithmeticLanguage(alphabet, ss, constraint);
        }
        public static ArithmeticLanguage FromTextDescriptions(string alphabetText, string symbolicStringText, string constraintText)
        {
            // Split alphabet and ensure that the symbols are valid 
            // and don't contain special characters
            var whiteSpacePattern = new Regex(@"^\s*$");
            List<String> alphabet = alphabetText
                .Split(new char[] { ' ' })
                .Where(s => !whiteSpacePattern.IsMatch(s))
                .ToList();
            return FromTextDescriptions(alphabet, symbolicStringText, constraintText);
        }
    }

    public class PumpingLemmaException : Exception
    {
        public PumpingLemmaException() : base() { }
        public PumpingLemmaException(string message) : base(message) { }
        public PumpingLemmaException(string message, System.Exception inner) : base(message, inner) { }
    }
}
