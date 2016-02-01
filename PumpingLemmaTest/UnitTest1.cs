using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PumpingLemmaTest
{
    [TestClass]
    public class ExpressionParserTests
    {
        public void ParseAndExpectSuccess(String input)
        {
            var result = PumpingLemma.Parser.parseCondition(input);
            Assert.IsNotNull(result);
            Console.WriteLine(input + " -> " + result);
        }

        public void ParseAndExpectFailure(String input)
        {
            var result = PumpingLemma.Parser.parseCondition(input);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestSanity()
        {
            ParseAndExpectSuccess("i < j");
            ParseAndExpectSuccess("2*i < 0");
            ParseAndExpectFailure("j*i < 0");
            ParseAndExpectFailure("i ? j");
            ParseAndExpectSuccess("i < j && j <= k");
            ParseAndExpectSuccess("i < j || j <= k");
            ParseAndExpectFailure("!i < j");
            ParseAndExpectSuccess("(i < j || i >= 0) || i < 0");
        }
    }

    [TestClass]
    public class SymbolicStringParserTests
    {
        public void ParseAndExpectSuccess(String input, List<String> alphabet)
        {
            var result = PumpingLemma.Parser.parseSymbolicString(input, alphabet);
            Assert.IsNotNull(result);
            Console.WriteLine(input + " -> " + result);
        }

        public void ParseAndExpectFailure(String input, List<String> alphabet)
        {
            var result = PumpingLemma.Parser.parseSymbolicString(input, alphabet);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestSanity()
        {
            var alphabet = (new String[] {"a", "b", "c"}).ToList();
            ParseAndExpectSuccess("ab^4c^i a^3", alphabet);

            ParseAndExpectFailure("ab^", alphabet);
        }


        [TestMethod]
        public void TestInvalidCharacters()
        {
            var alphabet = (new String[] { "a", "b", "c" }).ToList();
            ParseAndExpectFailure("ab.+.c", alphabet); 
        }

        [TestMethod]
        public void TestRepetitionIndices()
        {
            var alphabet = (new String[] { "a", "b" }).ToList();
            ParseAndExpectSuccess("a^1", alphabet);
            ParseAndExpectSuccess("a^i", alphabet);
            ParseAndExpectSuccess("a^(i+j)", alphabet);
            ParseAndExpectFailure("a^i+j", alphabet);
            ParseAndExpectSuccess("a^(1+j)", alphabet);
            ParseAndExpectSuccess("a^i b^j", alphabet);
            ParseAndExpectSuccess("a^(2*i + j)", alphabet);
            ParseAndExpectFailure("a^(j*i)", alphabet);
            ParseAndExpectSuccess("a^(j+i) b^(i+i)", alphabet);
        }

        [TestMethod]
        public void TestWord()
        {
            var alphabet = (new String[] { "a", "b", "c" }).ToList();
            ParseAndExpectFailure("ab^4c^i a^3d", alphabet);

            alphabet = (new String[] { "a", "b" }).ToList();
            ParseAndExpectSuccess("ab^2", alphabet);
            ParseAndExpectSuccess("aa^2", alphabet);

            alphabet = (new String[] { "0", "1" }).ToList();
            ParseAndExpectSuccess("01^4", alphabet);
            ParseAndExpectSuccess("0^n 1^n", alphabet);
        }
    }
}
