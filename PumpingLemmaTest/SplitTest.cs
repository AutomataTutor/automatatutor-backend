using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PumpingLemmaTest
{
    [TestClass]
    public class SplitDisplayTest
    {
        List<String> alphabet = new String[] { "a", "b", "c", "d" }.ToList();

        private XElement test(PumpingLemma.SymbolicString s)
        {
            Console.WriteLine("" + s + " splits into: ");
            Assert.AreEqual(1, s.GetIntegerVariables().Count);
            var variable = s.GetIntegerVariables().First();
            var additionalConstraint = PumpingLemma.ComparisonExpression.GreaterThanOrEqual(
                PumpingLemma.LinearIntegerExpression.SingleTerm(1, variable),
                PumpingLemma.LinearIntegerExpression.Constant(0));

            var ans = s.SplitDisplayXML(variable, additionalConstraint);
            Console.WriteLine(ans.ToString());
            return ans;
        }

        /*
        [TestMethod]
        public void TestSplitSymbol()
        {
            test(PumpingLemma.Parser.parseSymbolicString("a", alphabet));
        }

        [TestMethod]
        public void TestSplitEpsilon()
        {
            Assert.AreEqual(1, test(PumpingLemma.SymbolicString.Epsilon()));
        }
         */

        [TestMethod]
        public void TestSplitRepeat()
        {
            test(PumpingLemma.Parser.parseSymbolicString("(ab)^p", alphabet));
        }

        [TestMethod]
        public void TestSplitConcat()
        {
            test(PumpingLemma.Parser.parseSymbolicString("a^p b^p", alphabet));
        }

    }

    [TestClass]
    public class TestSplit
    {
        List<String> alphabet = new String[] { "a", "b", "c", "d" }.ToList();

        private int test(PumpingLemma.SymbolicString s)
        {
            Console.WriteLine("" + s + " splits into: ");
            var splits = s.Splits();
            foreach (var split in splits)
                Console.WriteLine("\t" + split.ToString());
            return splits.Count();
        }

        [TestMethod]
        public void TestSplitSymbol()
        {
            Assert.AreEqual(3, test(PumpingLemma.Parser.parseSymbolicString("a", alphabet)));
        }

        [TestMethod]
        public void TestSplitEpsilon()
        {
            Assert.AreEqual(1, test(PumpingLemma.SymbolicString.Epsilon()));
        }

        [TestMethod]
        public void TestSplitRepeat()
        {
            Assert.AreEqual(4, test(PumpingLemma.Parser.parseSymbolicString("(ab)^n", alphabet)));
        }

        [TestMethod]
        public void TestSplitConcat()
        {
            Assert.AreEqual((("abbddd".Length + 1) * ("abbddd".Length + 2)) / 2, test(PumpingLemma.Parser.parseSymbolicString("abbddd", alphabet)));

            // I can't think of a simple way to compute the answer for these, but the splits look right 
            // Assert.AreEqual(??, test(PumpingLemma.Parser.parseSymbolicString("d(ab)^n d", alphabet)));
            // Assert.AreEqual(??, test(PumpingLemma.Parser.parseSymbolicString("d(ab)^n d (ab)^m d", alphabet)));
            Console.WriteLine(test(PumpingLemma.Parser.parseSymbolicString("d(ab)^n d", alphabet)));
            Console.WriteLine(test(PumpingLemma.Parser.parseSymbolicString("d(ab)^n d (ab)^m d", alphabet)));
        }
    }

    [TestClass]
    public class TwoSplitTest
    {
        List<String> alphabet = new String[] { "a", "b", "c", "d" }.ToList();

        private int test(PumpingLemma.SymbolicString s)
        {
            Console.WriteLine("" + s + " two-splits into: ");
            var twoSplits = s.TwoSplits();
            foreach (var twoSplit in twoSplits)
                Console.WriteLine("\t" + twoSplit.ToString());
            return twoSplits.Count();
        }

        [TestMethod]
        public void TestTwoSplitSymbol()
        {
            Assert.AreEqual(2, test(PumpingLemma.Parser.parseSymbolicString("a", alphabet)));
        }

        [TestMethod]
        public void TestTwoSplitRepeat()
        {
            Assert.AreEqual(3, test(PumpingLemma.Parser.parseSymbolicString("(abc)^n", alphabet)));
        }

        [TestMethod]
        public void TestTwoSplitEpsilon()
        {
            Assert.AreEqual(1, test(PumpingLemma.SymbolicString.Epsilon()));
        }

        [TestMethod]
        public void TestTwoSplitConcat()
        {
            Assert.AreEqual("abbddd".Length + 1, test(PumpingLemma.Parser.parseSymbolicString("abbddd", alphabet)));
            Assert.AreEqual(10, test(PumpingLemma.Parser.parseSymbolicString("abb(abc)^n ddd", alphabet)));
        }
    }
}
