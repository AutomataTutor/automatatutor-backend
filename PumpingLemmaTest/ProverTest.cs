using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PumpingLemma;

namespace PumpingLemmaTest
{
    [TestClass]
    public class ProverTest
    {
        static List<String> alphabet = new string[]{ "a", "b" }.ToList();
        static LinearIntegerExpression p =  LinearIntegerExpression.Variable("p");
        static BooleanExpression sane_p = ComparisonExpression.GreaterThanOrEqual(p, 0);

        static SymbolicString s1 = Parser.parseSymbolicString("a^p b^p", alphabet);
        static SymbolicString s2 = Parser.parseSymbolicString("a^(p+1) b^p", alphabet);
        static SymbolicString s3 = Parser.parseSymbolicString("a^2 b^p", alphabet);
        static ArithmeticLanguage l1 = ArithmeticLanguage.FromTextDescriptions(alphabet, "a^i b^j", "i = j && i >= 0 && j >= 0");
        static ArithmeticLanguage l2 = ArithmeticLanguage.FromTextDescriptions(alphabet, "a^i b^j", "i <= 3 && i >= 0 && j >= 0");

        [TestMethod]
        public void TestContainment()
        {

            Assert.IsTrue(ProofChecker.checkContainment(s1, l1, sane_p));
            Assert.IsFalse(ProofChecker.checkContainment(s1, l2, sane_p));

            Assert.IsFalse(ProofChecker.checkContainment(s2, l1, sane_p));
            Assert.IsFalse(ProofChecker.checkContainment(s2, l2, sane_p));

            Assert.IsFalse(ProofChecker.checkContainment(s3, l1, sane_p));
            Assert.IsTrue(ProofChecker.checkContainment(s3, l2, sane_p));
        }

        [TestMethod]
        public void TestPumpingString()
        {
            ProofChecker.check(l1, s1);
        }
    }
}
