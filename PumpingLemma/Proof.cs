using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Z3;

namespace PumpingLemma
{
    public class ProofChecker
    {
        public static bool checkPumping(
            ArithmeticLanguage language,
            SymbolicString pumpingString,
            Split split,
            LinearIntegerExpression pump)
        {
            if (pump.isConstant())
            {
                var k = pump.constant;
                var pumpedMid = SymbolicString.Concat(Enumerable.Repeat(split.mid, k));
                var pumpedString = SymbolicString.Concat(split.start, pumpedMid, split.end);

                return checkNonContainment(pumpedString, language, split.constraints);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static bool check(ArithmeticLanguage language, SymbolicString pumpingString)
        {
            if (pumpingString.GetIntegerVariables().Count() != 1)
                return false;
            var pumpingLength = pumpingString.GetIntegerVariables().First();
            var pumpingLengthVariable = PumpingLemma.LinearIntegerExpression
                .SingleTerm(1, pumpingLength);
            var additionalConstraint = LogicalExpression.And(
                PumpingLemma.ComparisonExpression.GreaterThan(pumpingLengthVariable, 0),
                LogicalExpression.And(pumpingString.repeats().Select(x => ComparisonExpression.GreaterThanOrEqual(x, 0)))
            );

            // 0. Need to check if pumping string grows unboundedly with p
            var pumpingStringLength = pumpingString.length();
            if (pumpingStringLength.isConstant() || pumpingStringLength.coefficients[pumpingLength] < 0)
                return false;
            foreach (var r in pumpingString.repeats())
                if (r.coefficients[pumpingLength] < 0)
                    return false;

            // 1. Check that the pumping string is in the language for all p
            if (!checkContainment(pumpingString, language, additionalConstraint))
                return false;

            Console.WriteLine("Language is non-regular if all the following splits are good:");
            int i = 0;
            // 2. Check that each split of the pumping string has an valid pumping length 
            foreach (var split in pumpingString.ValidSplits(pumpingLengthVariable, additionalConstraint))
            {
                Console.WriteLine("\t" + (i++) + ": " + split + " when " + additionalConstraint);
                if (!splitGood(split, language, additionalConstraint))
                    return false;
            }

            return true;
        }

        public static BooleanExpression containmentCondition(SymbolicString s, ArithmeticLanguage l, BooleanExpression additionalConstraints)
        {
            // Console.WriteLine("Checking containment of " + s + " in " + l);
            var goodMatches = Matcher
                .match(s, l.symbolic_string)
                .Select(x => x.forceFinish())
                .Select(x => x.withAdditionalConstraint(l.constraint))
                .Where(x => x.isFeasible())
                ;
            if (goodMatches.Count() == 0)
                return LogicalExpression.False();

            var matchCondition = LogicalExpression.False();
            foreach (var match in goodMatches)
            {
                // Console.WriteLine("Got good match: " + match);
                matchCondition = LogicalExpression.Or(matchCondition, match.constraint);
            }
            var condition = LogicalExpression.Implies(additionalConstraints, matchCondition);

            var variables = condition.GetVariables();
            var pVariables = s.GetIntegerVariables(); // Most of the times, should be 1
            var nonPVariables = variables.Where(x => !pVariables.Contains(x));

            var eCondition = QuantifiedExpression.Exists(nonPVariables, condition);
            var aCondition = QuantifiedExpression.Forall(pVariables, eCondition);

            return aCondition;
        }

        public static bool checkContainment(SymbolicString s, ArithmeticLanguage l, BooleanExpression additionalConstraints)
        {
            var condition = containmentCondition(s, l, additionalConstraints);
            // Check if the containment condition is valid
            return !LogicalExpression.Not(condition).isSatisfiable();
        }

        public static bool checkNonContainment(SymbolicString s, ArithmeticLanguage l, BooleanExpression additionalConstraints)
        {
            var condition = containmentCondition(s, l, additionalConstraints);
            // Check if the containment condition is unsatisfiable
            return !condition.isSatisfiable();
        }

        // Returns true if start.(mid)^witness.end is not in the language for all models
        // that satisfy the additionalConstraint
        public static bool splitGoodWithWitness(
            Split split,
            ArithmeticLanguage language,
            BooleanExpression additionalConstraint, 
            LinearIntegerExpression pumpingWitness)
        {
            // We just need to find a k such that (x y^k z) \not\in L
            // If we cannot find such a k for some p, it is a bad split

            // Say i, j are the variables in the language constraint and 
            // v_1, v_2 are variables in split constraints
            // Therefore, if for all p, v_1, \ldots, v_n that satisfies split constraints
            // and additional constraints and exists k such that for all i, j language 
            // constraint does not hold, then split is bad
            Console.WriteLine("\t\tSplit is good if none of the following mids can be pumped: ");

            // We will definitely go through the loop at least once as match is working
            var beginningMatches = Matcher
                .match(split.start, language.symbolic_string)
                .Where(x => !x.FirstRemaining)
                .Select(x => x.withAdditionalConstraint(language.constraint))
                .Select(x => x.withAdditionalConstraint(additionalConstraint))
                .Where(x => x.isFeasible());
            foreach (var beginMatch in beginningMatches)
            {
                var remainingLanguage = beginMatch.remaining2;
                var endMatches = Matcher
                    .match(split.end.reverse(), remainingLanguage.reverse())
                    .Where(x => !x.FirstRemaining)
                    .Select(x => x.withAdditionalConstraint(language.constraint))
                    .Select(x => x.withAdditionalConstraint(additionalConstraint))
                    .Where(x => x.isFeasible());

                foreach (var endMatch in endMatches)
                {
                    var fullConstraint = LogicalExpression.And(beginMatch.constraint, endMatch.constraint);
                    if (!fullConstraint.isSatisfiable())
                        continue;
                    var midLanguage = endMatch.remaining2.reverse();
                    var midSplit = split.mid;
                    var ctx = new Context();
                    // Console.WriteLine("\t\t" + midLanguage + " ===== " + midSplit + " when " + fullConstraint);
                    // var z3exp = fullConstraint.toZ3(ctx).Simplify();
                    // Console.WriteLine("\t\t" + midLanguage + " ===== " + midSplit + " when " + z3exp);
                    if (!canMismatchWitness(midLanguage, midSplit, fullConstraint, pumpingWitness))
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        // Strategy:
        // a) Match the prefix and suffix of the language with the x and z 
        // b) See if y can be pumped against the middle part
        // The horrifying part is the quantifiers
        public static bool splitGood(Split split, ArithmeticLanguage language, BooleanExpression additionalConstraint)
        {
            // We just need to find a k such that (x y^k z) \not\in L
            // If we cannot find such a k for some p, it is a bad split

            // Say i, j are the variables in the language constraint and 
            // v_1, v_2 are variables in split constraints
            // Therefore, if for all p, v_1, \ldots, v_n that satisfies split constraints
            // and additional constraints and exists k such that for all i, j language 
            // constraint does not hold, then split is bad
            Console.WriteLine("\t\tSplit is good if none of the following mids can be pumped: ");

            // We will definitely go through the loop at least once as match is working
            var beginningMatches = Matcher
                .match(split.start, language.symbolic_string)
                .Where(x => !x.FirstRemaining)
                .Select(x => x.withAdditionalConstraint(language.constraint))
                .Select(x => x.withAdditionalConstraint(additionalConstraint))
                .Where(x => x.isFeasible());
            foreach (var beginMatch in beginningMatches)
            {
                var remainingLanguage = beginMatch.remaining2;
                var endMatches = Matcher
                    .match(split.end.reverse(), remainingLanguage.reverse())
                    .Where(x => !x.FirstRemaining)
                    .Select(x => x.withAdditionalConstraint(language.constraint))
                    .Select(x => x.withAdditionalConstraint(additionalConstraint))
                    .Where(x => x.isFeasible());

                foreach (var endMatch in endMatches)
                {
                    var fullConstraint = LogicalExpression.And(beginMatch.constraint, endMatch.constraint);
                    if (!fullConstraint.isSatisfiable())
                        continue;
                    var midLanguage = endMatch.remaining2.reverse();
                    var midSplit = split.mid;
                    var ctx = new Context();
                    // Console.WriteLine("\t\t" + midLanguage + " ===== " + midSplit + " when " + fullConstraint);
                    // var z3exp = fullConstraint.toZ3(ctx).Simplify();
                    // Console.WriteLine("\t\t" + midLanguage + " ===== " + midSplit + " when " + z3exp);
                    if (!canMismatchOne(midLanguage, midSplit, fullConstraint))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool canMismatchOne(SymbolicString language, SymbolicString midSplit, BooleanExpression fullConstraint)
        {
            /*
            var matches = Matcher
                .matchRepeatedFull(language, midSplit)
                .Select(x => x.finishMatch());
            foreach (var match in matches)
            {
                var x = LogicalExpression.And(match.constraint, fullConstraint);
                // Do something here
            }
            */
            throw new NotImplementedException();
        }

        public static bool canMismatchWitness(
            SymbolicString midLanguage,
            SymbolicString midSplit,
            BooleanExpression fullConstraint,
            LinearIntegerExpression pumpingWitness
            )
        {
            throw new NotImplementedException();
        }
    }

    public class Split
    {
        public SymbolicString start;
        public SymbolicString mid;
        public SymbolicString end;
        public BooleanExpression constraints;

        private Split(SymbolicString s, SymbolicString m, SymbolicString e, BooleanExpression c)
        {
            start = s;
            mid = m;
            end = e;
            start.flatten();
            mid.flatten();
            end.flatten();
            constraints = c;
        }

        public static Split MakeSplit(SymbolicString s, SymbolicString m, SymbolicString e, BooleanExpression c)
        {
            return new Split(s, m, e, c);
        }

        public override string ToString()
        {
            return String.Format("{0}  ---  {1}  ---  {2} [{3}]", start, mid, end, constraints);
        }

        public void AddConstraint(BooleanExpression c)
        {
            this.constraints = LogicalExpression.And(this.constraints, c);
        }
    }

    public class TwoSplit
    {
        public SymbolicString start;
        public SymbolicString end;
        public BooleanExpression constraints;

        private TwoSplit(SymbolicString s, SymbolicString e, BooleanExpression c)
        {
            start = s;
            end = e;
            constraints = c;
        }

        public static TwoSplit MakeSplit(SymbolicString s, SymbolicString e, BooleanExpression c)
        {
            return new TwoSplit(s, e, c);
        }

        public override string ToString()
        {
            return String.Format("{0}  ---  {1}  [{2}]", start, end, constraints);
        }

        public TwoSplit extend(SymbolicString prefix, SymbolicString suffix)
        {
            var new_start = SymbolicString.Concat(prefix, start);
            var new_end = SymbolicString.Concat(end, suffix);
            new_start.flatten();
            new_end.flatten();
            return MakeSplit(new_start, new_end, constraints);
        }
    }
}
