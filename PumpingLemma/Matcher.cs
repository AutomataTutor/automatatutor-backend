using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PumpingLemma
{
    public class Match {
        public readonly BooleanExpression constraint;
        public readonly SymbolicString remaining1;
        public readonly SymbolicString remaining2;

        private Match(BooleanExpression _c, SymbolicString _s1, SymbolicString _s2)
        {
            this.constraint = _c;
            this.remaining1 = _s1;
            this.remaining2 = _s2;
        }

        public Match reverse()
        {
            return new Match(constraint, remaining2, remaining1);
        }

        #region Factory Methods
        internal static Match MakeMatch(BooleanExpression _c, SymbolicString remaining1, SymbolicString remaining2)
        {
            return new Match(_c, remaining1, remaining2);
        }

        public static Match FullMatch(BooleanExpression _c)
        {
            return new Match(
                _c,
                SymbolicString.Epsilon(),
                SymbolicString.Epsilon()
                );
        }
        public static Match PartialFirst(BooleanExpression _c, SymbolicString remaining1)
        {
            return new Match(
                _c,
                remaining1,
                SymbolicString.Epsilon()
                );
        }
        public static Match PartialSecond(BooleanExpression _c, SymbolicString remaining2)
        {
            return new Match(
                _c,
                SymbolicString.Epsilon(),
                remaining2
                );
        }
        #endregion

        internal Match withAdditionalConstraint(BooleanExpression additionalConstraint)
        {
            return new Match(
                LogicalExpression.And(this.constraint, additionalConstraint),
                this.remaining1,
                this.remaining2
                );
        }
        internal IEnumerable<Match> continueMatch(SymbolicString rest1, SymbolicString rest2)
        {
            var cont1 = SymbolicString.Concat(this.remaining1, rest1);
            var cont2 = SymbolicString.Concat(this.remaining2, rest2);
            foreach (var mp in Matcher.match(cont1, cont2))
            {
                yield return mp.withAdditionalConstraint(this.constraint);
            }
        }
        public Match forceFinish()
        {
            BooleanExpression additionalConstraint = LogicalExpression.True();
            if (FirstRemaining)
                additionalConstraint = LogicalExpression.And(
                    additionalConstraint,
                    ComparisonExpression.Equal(remaining1.length(), 0)
                    );
            if (SecondRemaining)
                additionalConstraint = LogicalExpression.And(
                    additionalConstraint,
                    ComparisonExpression.Equal(remaining2.length(), 0)
                    );
            return FullMatch(LogicalExpression.And(this.constraint, additionalConstraint));
        }

        public override string ToString()
        {
            return "(" + remaining1.ToString() + ", " + remaining2.ToString() + ") when " + constraint.ToString();
        }
        public bool isFeasible()
        {
            return this.constraint.isSatisfiable();
        }
        public bool FirstRemaining
        { 
            get { return !this.remaining1.isEpsilon();  }
        }
        public bool SecondRemaining
        {
            get { return !this.remaining2.isEpsilon();  }
        }
    }

    public static class Matcher
    {

        // Just dispatches to the appropriate helper method
        public static IEnumerable<Match> match(SymbolicString s1, SymbolicString s2)
        {
            Debug.Assert(s1.isFlat() && s2.isFlat());
            Func<SymbolicString.SymbolicStringType, string> toSignature = (ss_type) => {
                switch(ss_type) {
                    case SymbolicString.SymbolicStringType.Symbol: return "S";
                    case SymbolicString.SymbolicStringType.Repeat: return "R";
                    case SymbolicString.SymbolicStringType.Concat: return "C";
                    default: throw new ArgumentException();
                }
            };

            switch (toSignature(s1.expression_type) + toSignature(s2.expression_type))
            {
                case "RS":
                case "CS":
                case "CR":
                    return match(s2, s1).Select(x => x.reverse());
                case "SS":
                    return matchSymbolSymbol(s1, s2);
                case "SR":
                    return matchSymbolRepeat(s1, s2);
                case "SC":
                    return matchSymbolConcat(s1, s2);
                case "RR":
                    return matchRepeatRepeat(s1, s2);
                case "RC":
                    return matchRepeatConcat(s1, s2);
                case "CC":
                    return matchConcatConcat(s1, s2);
                default:
                    throw new ArgumentException();
            }
        }

        // Match only if both symbols are same
        private static IEnumerable<Match> matchSymbolSymbol(SymbolicString s1, SymbolicString s2)
        {
            if (s2.atomic_symbol == s1.atomic_symbol)
                yield return Match.FullMatch(LogicalExpression.True());
        }

        // Either consume symbol with first symbol of repeat and say repeat is positive
        // Or don't consume symbol and say repeat is zero
        private static IEnumerable<Match> matchSymbolRepeat(SymbolicString s1, SymbolicString s2)
        {
            Debug.Assert(s2.isFlat());

            // When repeat is 0
            yield return Match.PartialFirst(ComparisonExpression.Equal(s2.repeat, 0), s1);
            // When repeat is non-zero
            foreach (var m in match(s1, s2.root))
            {
                // Because the root is a word, we immediately know if the symbol matches or not
                // and get an m if and only if the symbol matches
                Debug.Assert(!m.FirstRemaining); 
                yield return Match.PartialSecond(
                    LogicalExpression.And(m.constraint, ComparisonExpression.GreaterThan(s2.repeat, 0)),
                    SymbolicString.Concat(m.remaining2, SymbolicString.Repeat(s2.root, s2.repeat - 1))
                    );
            }
        }

        private static IEnumerable<Match> matchSymbolConcat(SymbolicString s1, SymbolicString s2)
        {
            if (s2.isEpsilon())
            {
                yield return Match.PartialFirst(LogicalExpression.True(), s1);
                yield break;
            }
            var rest = SymbolicString.Concat(s2.sub_strings.Skip(1));
            foreach (var m in match(s1, s2.sub_strings.First()))
                foreach (var mp in m.continueMatch(SymbolicString.Epsilon(), rest))
                    yield return mp;
        }

        private static IEnumerable<Match> matchConcatConcat(SymbolicString s1, SymbolicString s2)
        {
            if (s1.isEpsilon() || s2.isEpsilon())
            {
                yield return Match.MakeMatch(LogicalExpression.True(), s1, s2);
                yield break;
            }
            // Quick match prefix symbols
            int i;
            for (i = 0; i < s1.sub_strings.Count && i < s2.sub_strings.Count; i++)
            {
                if (s1.sub_strings[i].expression_type != SymbolicString.SymbolicStringType.Symbol ||
                    s2.sub_strings[i].expression_type != SymbolicString.SymbolicStringType.Symbol)
                    break;
                if (s1.sub_strings[i].atomic_symbol != s2.sub_strings[i].atomic_symbol)
                    yield break;
            }

            var sub1 = s1.sub_strings.Skip(i);
            var sub2 = s2.sub_strings.Skip(i);
            // If we have consumed something fully, just return the remaining bits
            if (i == s1.sub_strings.Count || i == s2.sub_strings.Count)
            {
                yield return Match.MakeMatch(
                    LogicalExpression.True(),
                    SymbolicString.Concat(sub1),
                    SymbolicString.Concat(sub2));
                yield break;
            }

            var rest1 = SymbolicString.Concat(sub1.Skip(1));
            var rest2 = SymbolicString.Concat(sub2.Skip(1));
            foreach (var m in match(sub1.First(), sub2.First()))
                foreach (var mp in m.continueMatch(rest1, rest2))
                    yield return mp;
        }

        private static IEnumerable<Match> matchRepeatConcat(SymbolicString s1, SymbolicString s2)
        {
            if (s2.isEpsilon())
            {
                yield return Match.FullMatch(ComparisonExpression.Equal(s1.length(), 0));
                yield return Match.PartialFirst(ComparisonExpression.GreaterThan(s1.length(), 0), s1);
                yield break;
            }
            var first = s2.sub_strings.First();
            var rest = SymbolicString.Concat(s2.sub_strings.Skip(1));
            foreach (var m in match(s1, first))
                foreach (var mp in m.continueMatch(SymbolicString.Epsilon(), rest))
                    yield return mp;
        }

        private static IEnumerable<Match> matchRepeatRepeat(SymbolicString s1, SymbolicString s2)
        {
            Debug.Assert(s1.root.isWord());
            Debug.Assert(s2.root.isWord());

            // Assume s1 is fully consumed
            foreach (var m in allLeftMatches(s1, s2))
                yield return m;

            // Assume s2 is fully consumed
            foreach (var m in allLeftMatches(s2, s1))
                yield return m.reverse();

            // Assume both are fully consumed
            if (omegaEqual(s1.root, s2.root))
            {
                yield return Match.FullMatch(ComparisonExpression.Equal(
                    LinearIntegerExpression.Times(s1.root.wordLength(), s1.repeat),
                    LinearIntegerExpression.Times(s2.root.wordLength(), s2.repeat)
                    ));
            }
            else
            {
                yield return Match.FullMatch(LogicalExpression.And(
                    ComparisonExpression.Equal(s2.length(), 0),
                    ComparisonExpression.Equal(s1.length(), 0)
                    ));
            }
        }

        #region Repeat Repeat match helpers
        private static int gcd(int a, int b)
        {
            while (a % b != 0)
            {
                int temp = a;
                a = b; b = temp % b;
            }
            return b;
        }

        // s1 and s2 are omega equal if s1^\omega = s2^\omega
        // Theorem: s1 and s2 are omega equal if and only if there
        // exists a w such that s1 = w^(l1/g) and s2 = w^(l2/g)
        // where l1, l2 are lengths of s1 and s2, and g = gcd(l1, l2)
        private static bool omegaEqual(SymbolicString s1, SymbolicString s2)
        {
            Debug.Assert(s1.isWord() && s2.isWord());

            var l1 = s1.wordLength();
            var l2 = s2.wordLength();
            var g = gcd(l1, l2);

            var w1 = s1.word().ToArray();
            for (int i = 0; i < l1; i++)
                if (w1[i] != w1[i % g]) return false;

            var w2 = s2.word().ToArray();
            for (int i = 0; i < l2; i++)
                if (w2[i] != w2[i % g]) return false;

            for (int i = 0; i < g; i++)
                if (w1[i] != w2[i]) return false;

            return true;
        }

        // Call only when omegaEqual(s1, s2) holds
        // TODO: There is definitely a more optimal implementation of this
        private static int getFirstMismatch(SymbolicString s1, SymbolicString s2)
        {
            Debug.Assert(s1.isWord() && s2.isWord());

            var l1 = s1.wordLength();
            var l2 = s2.wordLength();
            var w1 = s1.word().ToArray();
            var w2 = s2.word().ToArray();
            for (int i = 0; true; i++)
            {
                if (w1[i % l1] != w2[i % l2])
                    return i;
            }
        }
        private static IEnumerable<Match> shortLeftMatches(SymbolicString s1, SymbolicString s2, int firstMismatch)
        {
            int l1 = s1.root.wordLength();
            for (int i = 1; l1 * i <= firstMismatch; i++)
            {
                var s1r = SymbolicString.Concat(Enumerable.Repeat(s1.root, i));
                foreach (var m in match(s1r, s2))
                {
                    if (m.FirstRemaining)
                        continue;
                    yield return m.withAdditionalConstraint(LogicalExpression.And(
                        ComparisonExpression.Equal(s1.length(), s1r.length()),
                        ComparisonExpression.GreaterThan(m.remaining2.length(), 0)
                        ));
                }
            }
        }

        private static IEnumerable<Match> longLeftMatches(SymbolicString s1, SymbolicString s2)
        {
            var l1 = s1.root.wordLength();
            var l2 = s2.root.wordLength();
            var g = gcd(l1, l2);
            var v_beg = LinearIntegerExpression.FreshVariable();
            var v_end = LinearIntegerExpression.FreshVariable();

            // Split exactly at s2 root border
            yield return Match.PartialSecond(
                LogicalExpression.And(
                    // the beginning matches the s1
                    ComparisonExpression.Equal(
                        LinearIntegerExpression.Times(l2, v_beg), 
                        LinearIntegerExpression.Times(l1, s1.repeat)
                    ),
                    // left over right bit is nonempty
                    ComparisonExpression.GreaterThan(
                        LinearIntegerExpression.Times(l2, v_end), 
                        0
                    ),
                    // beginning and end match s2
                    ComparisonExpression.Equal(v_beg + v_end, s2.repeat),
                    ComparisonExpression.GreaterThanOrEqual(v_beg, 0),
                    ComparisonExpression.GreaterThanOrEqual(v_end, 0)
                ),
                SymbolicString.Repeat(s2.root, v_end)
            );
            // Split in the middle of s2 root
            if (l2 != 1)
            {
                for (int i = g; i < l2; i += g)
                {
                    var suffix = SymbolicString.Concat(s2.root.sub_strings.Skip(i));
                    yield return Match.PartialSecond(
                        LogicalExpression.And(
                            ComparisonExpression.Equal(
                                LinearIntegerExpression.Times(l2, v_beg) + g,
                                LinearIntegerExpression.Times(l1, s1.repeat)
                            ),
                            ComparisonExpression.GreaterThan(
                                LinearIntegerExpression.Times(l2, v_beg) + suffix.length(),
                                0
                            ),
                            ComparisonExpression.Equal(v_beg + v_end + 1, s2.repeat),
                            ComparisonExpression.GreaterThanOrEqual(v_beg, 0),
                            ComparisonExpression.GreaterThanOrEqual(v_end, 0)
                        ),
                        SymbolicString.Concat(suffix, SymbolicString.Repeat(s2.root, v_end))
                    );
                }
            }
        }

        // Returns only those matches where s1 is consumed fully
        private static IEnumerable<Match> allLeftMatches(SymbolicString s1, SymbolicString s2)
        {
            // First empty
            yield return Match.PartialSecond(
                LogicalExpression.And(
                    ComparisonExpression.Equal(s1.length(), 0), 
                    ComparisonExpression.GreaterThan(s2.length(), 0)
                ),
                s2
            );

            // First non-empty
            if (!omegaEqual(s1.root, s2.root))
            { // They will mismatch at some point. So, just get small matches up to that point
                int firstMismatch = getFirstMismatch(s1.root, s2.root);
                foreach (var m in shortLeftMatches(s1, s2, firstMismatch))
                    yield return m.withAdditionalConstraint(ComparisonExpression.GreaterThan(s1.length(), 0));
            }
            else
            { // They will never mismatch. So, split s2 into two parts where s1 = first part
                foreach (var m in longLeftMatches(s1, s2))
                    yield return m.withAdditionalConstraint(ComparisonExpression.GreaterThan(s1.length(), 0));
            }
        }
        #endregion
    }
}
