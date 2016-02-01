using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PumpingLemma;

namespace PumpingLemmaTest
{
    [TestClass]
    public class MatcherTest
    {
        // Words
        static SymbolicString a = SymbolicString.Symbol("a");
        static SymbolicString b = SymbolicString.Symbol("b");
        static SymbolicString ab = SymbolicString.Concat(a, b);
        static SymbolicString ba = SymbolicString.Concat(b, a);
        static SymbolicString abab = SymbolicString.Concat(a, b, a, b);
        static SymbolicString ababab = SymbolicString.Concat(a, b, a, b, a, b);
        static SymbolicString aa = SymbolicString.Concat(a, a);
        static SymbolicString aab = SymbolicString.Concat(a, a, b);
        static SymbolicString aaba = SymbolicString.Concat(a, a, b, a);

        static LinearIntegerExpression n = LinearIntegerExpression.Variable("n");
        static LinearIntegerExpression n1 = n + 1;
        static LinearIntegerExpression p = LinearIntegerExpression.Variable("p");
        static LinearIntegerExpression m = LinearIntegerExpression.Variable("m");

        static SymbolicString an = SymbolicString.Repeat(a, n);
        static SymbolicString abn = SymbolicString.Repeat(ab, n);
        static SymbolicString abn1 = SymbolicString.Repeat(ab, n1);
        static SymbolicString bn = SymbolicString.Repeat(b, n);
        static SymbolicString ban = SymbolicString.Repeat(ba, n);
        static SymbolicString ababm = SymbolicString.Repeat(abab, m);
        static SymbolicString abababn = SymbolicString.Repeat(ababab, n);
        static SymbolicString aabn = SymbolicString.Repeat(aab, n);
        static SymbolicString aabm = SymbolicString.Repeat(aab, m);
        static SymbolicString aabam = SymbolicString.Repeat(aaba, m);

        static SymbolicString banabn = SymbolicString.Concat(ban, abn);

        int getNumberOfMatches(SymbolicString s1, SymbolicString s2)
        {
            Console.WriteLine("Matching " + s1 + " with " + s2);
            int count = 0;
            foreach (var m in Matcher.match(s1, s2))
            {
                Console.WriteLine("\t" + m);
                count++;
            }
            return count;
        }
        int getNumberOfFeasibleMatches(SymbolicString s1, SymbolicString s2)
        {
            Console.WriteLine("Matching " + s1 + " with " + s2);
            int count = 0;
            foreach (var m in Matcher.match(s1, s2))
            {
                if (m.isFeasible())
                {
                    Console.WriteLine("\t" + m);
                    count++;
                }
            }
            return count;
        }

        [TestMethod]
        public void TestSymbolMatches()
        {
            // Match symbol with symbol
            Assert.AreEqual(0, getNumberOfMatches(a, b));
            Assert.AreEqual(1, getNumberOfMatches(a, a));

            // Match symbol with repeat
            Assert.AreEqual(2, getNumberOfMatches(a, an));
            Assert.AreEqual(2, getNumberOfMatches(a, abn));
            Assert.AreEqual(1, getNumberOfMatches(a, bn));
            Assert.AreEqual(1, getNumberOfMatches(a, ban));

            // Match symbol with concat
            Assert.AreEqual(1, getNumberOfMatches(a, ab));
            Assert.AreEqual(0, getNumberOfMatches(a, ba));
            Assert.AreEqual(2, getNumberOfMatches(a, banabn));
            Assert.AreEqual(1, getNumberOfFeasibleMatches(a, banabn));
        }

        [TestMethod]
        public void TestRepeatRepeat()
        {
            // Not omega equal cases
            
            // First mismatch is too early, no short matches
            Assert.AreEqual(3, getNumberOfMatches(abn, ban));
            Assert.AreEqual(1, getNumberOfFeasibleMatches(abn, ban));
            Assert.AreEqual(3, getNumberOfMatches(abn1, ban));
            Assert.AreEqual(1, getNumberOfFeasibleMatches(abn1, ban));

            // First mismatch is late, we have some short matches
            //   1 // first string empty, second non-empty
            // + 1 // second string empty, first empty
            // + 1 // both empty
            // + 1 // aab consumed by aaba ...
            // + 1 // aaba consumed by aab aab ...
            Assert.AreEqual(5, getNumberOfFeasibleMatches(aabn, aabam));

            Assert.AreEqual(5, getNumberOfFeasibleMatches(an, aabm));

            // Omega equal cases

            // If the words are omega equal, the number of matches is given by
            //   1        // first empty
            // + (l2/g)   // first non-empty, match second at multiples of gcd
            // + 1        // second empty
            // + (l1/g)   // first non-empty, match second at multiples of gcd
            // + 1        // Match both full (empty + non-empty)
            Assert.AreEqual(6, getNumberOfMatches(abn, ababm));
            Assert.AreEqual(7, getNumberOfMatches(abn, abababn));

            // (ab)^n fully consumed in each case
            // (ababab)^n not fully consumed in 3 case
            // (ababab)^n not fully consumed in 1 case (when n = 0)
            Assert.AreEqual(4, getNumberOfFeasibleMatches(abn, abababn));
        }

        [TestMethod]
        public void TestConcat()
        {
            Assert.AreEqual(2, getNumberOfMatches(abn, SymbolicString.Epsilon()));
            Assert.AreEqual(0, getNumberOfMatches(ab, ba));
            Assert.AreEqual(1, getNumberOfMatches(aa, aab));
            Assert.AreEqual(1, getNumberOfMatches(aab, aa));
        }
    }
}
