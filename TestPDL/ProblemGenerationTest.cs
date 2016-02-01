using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutomataPDL;

namespace TestPDL
{
    public class Testcase
    {
        public readonly int id;
        public readonly IEnumerable<char> alphabet;
        public readonly PDLPred language;

        public Testcase(int id, IEnumerable<char> alphabet, PDLPred language)
        {
            this.id = id;
            this.alphabet = alphabet;
            this.language = language;
        }
    }

    public class NonexistingTestcase : Testcase
    {
        public NonexistingTestcase() : base(-1, new char[] { }, new PDLTrue()) { }
    }

    public static class Testcases
    {
        private static IEnumerable<char> abAlphabet = new char[] { 'a', 'b' };
        private static IEnumerable<char> oneZeroAlphabet = new char[] { '1', '0' };

        public static Testcase createTestcase0()
        {
            return new Testcase(0, oneZeroAlphabet, new PDLEndsWith("00"));
        }

        public static Testcase createTestcase1()
        {
            // 0@x
            PDLPred zeroAtX = new PDLAtPos('0', new PDLPosVar("x"));
            // 0@x+1
            PDLPred zeroAtXPlus1 = new PDLAtPos('0', new PDLSuccessor(new PDLPosVar("x")));
            // 0@x+2
            PDLPred zeroAtXPlus2 = new PDLAtPos('0', new PDLSuccessor(new PDLSuccessor(new PDLPosVar("x"))));

            // \exists x. 0@x && 0@x+1 && 0@x+2
            PDLPred language = new PDLExistsFO("x",
                new PDLAnd(zeroAtX, new PDLAnd(zeroAtXPlus1, zeroAtXPlus2))
            );

            return new Testcase(1,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase2()
        {
            // 0@x
            PDLPred zeroAtX = new PDLAtPos('0', new PDLPosVar("x"));
            // 1@x+1
            PDLPred zeroAtXPlus1 = new PDLAtPos('1', new PDLSuccessor(new PDLPosVar("x")));
            // 1@x+2
            PDLPred zeroAtXPlus2 = new PDLAtPos('1', new PDLSuccessor(new PDLSuccessor(new PDLPosVar("x"))));

            // \exists x. 0@x && 1@x+1 && 1@x+2
            PDLPred language = new PDLExistsFO("x",
                new PDLAnd(zeroAtX, new PDLAnd(zeroAtXPlus1, zeroAtXPlus2))
            );

            return new Testcase(2,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase3()
        {
            return new Testcase(3,oneZeroAlphabet, new PDLIntGe(new PDLIndicesOf("001"), 0));
        }

        public static Testcase createTestcase4()
        {
            PDLPos lastMinusFour = posMinusN(new PDLLast(), 4);
            PDLPred xLeqLastMinusFour = new PDLPosLeq(new PDLPosVar("x"), lastMinusFour);

            PDLPred xLeqY = new PDLPosLeq(new PDLPosVar("x"), new PDLPosVar("y"));
            PDLPred xLeqZ = new PDLPosLeq(new PDLPosVar("x"), new PDLPosVar("z"));
            PDLPred yLeqXPlusFour = new PDLPosLeq(new PDLPosVar("y"), posPlusN(new PDLPosVar("x"), 4));
            PDLPred zLeqXPlusFour = new PDLPosLeq(new PDLPosVar("z"), posPlusN(new PDLPosVar("x"), 4));
            PDLPred yNeqZ = new PDLNot(new PDLPosEq(new PDLPosVar("y"), new PDLPosVar("z")));
            PDLPred zeroAtY = new PDLAtPos('0', new PDLPosVar("y"));
            PDLPred zeroAtZ = new PDLAtPos('0', new PDLPosVar("z"));
            PDLPred consequence = new PDLAnd(xLeqY, new PDLAnd(xLeqZ, new PDLAnd(yLeqXPlusFour, new PDLAnd(zLeqXPlusFour, new PDLAnd(yNeqZ, new PDLAnd(zeroAtY, zeroAtZ))))));
            PDLPred quantConsequence = new PDLExistsFO("y", new PDLExistsFO("z", consequence));

            PDLPred language = new PDLForallFO("x", new PDLIf(xLeqLastMinusFour, quantConsequence));
            return new Testcase(4,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase5()
        {
            PDLPred language = new PDLAtPos('1', posMinusN(new PDLLast(), 9));
            return new Testcase(5,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase6()
        {
            PDLPred language = new PDLOr(new PDLStartsWith("01"), new PDLEndsWith("01"));
            return new Testcase(6,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase7()
        {
            PDLPred language = new PDLAnd(
                new PDLModSetEq(new PDLIndicesOf("0"), 5, 0),
                new PDLModSetEq(new PDLIndicesOf("1"), 3, 0));

            return new Testcase(7,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase8()
        {
            PDLPred language = new PDLAnd(
                new PDLIf(
                    new PDLAtPos('a', new PDLLast()),
                    new PDLExistsFO(
                        "x",
                        new PDLAnd(
                            new PDLPosLe(new PDLPosVar("x"), new PDLLast()),
                            new PDLAtPos('a', new PDLPosVar("x"))
                        )
                    )
                ),
                new PDLAnd(
                    new PDLIf(
                        new PDLAtPos('b', new PDLLast()),
                        new PDLExistsFO(
                            "x",
                            new PDLAnd(
                                new PDLPosLe(new PDLPosVar("x"), new PDLLast()),
                                new PDLAtPos('b', new PDLPosVar("x"))
                            )
                        )
                    ),
                    new PDLIf(
                        new PDLAtPos('c', new PDLLast()),
                        new PDLExistsFO(
                            "x",
                            new PDLAnd(
                                new PDLPosLe(new PDLPosVar("x"), new PDLLast()),
                                new PDLAtPos('c', new PDLPosVar("x"))
                            )
                        )
                    )
                    )
               );

            return new Testcase(8,new char[] { 'a', 'b', 'c' }, language);
        }
        public static Testcase createTestcase9()
        {
            PDLPred language = new PDLAnd(
                new PDLIf(
                    new PDLAtPos('a', new PDLLast()),
                    new PDLNot(
                        new PDLExistsFO(
                            "x",
                            new PDLAnd(
                                new PDLPosLe(new PDLPosVar("x"), new PDLLast()),
                                new PDLAtPos('a', new PDLPosVar("x"))
                            )
                        )
                    )
                ),
                new PDLAnd(
                    new PDLIf(
                        new PDLAtPos('b', new PDLLast()),
                        new PDLNot(
                            new PDLExistsFO(
                                "x",
                                new PDLAnd(
                                    new PDLPosLe(new PDLPosVar("x"), new PDLLast()),
                                    new PDLAtPos('b', new PDLPosVar("x"))
                                )
                            )
                        )
                    ),
                    new PDLIf(
                        new PDLAtPos('c', new PDLLast()),
                        new PDLNot(
                            new PDLExistsFO(
                                "x",
                                new PDLAnd(
                                    new PDLPosLe(new PDLPosVar("x"), new PDLLast()),
                                    new PDLAtPos('c', new PDLPosVar("x"))
                                )
                            )
                        )
                    )
                )
           );

            return new Testcase(9,new char[] { 'a', 'b', 'c' }, language);
        }

        public static Testcase createTestcase10()
        {
            PDLPred language = new PDLExistsFO("x",
                new PDLAnd(
                    new PDLAtPos('0', new PDLPosVar("x")),
                    new PDLForallSO("X",
                        new PDLIf(
                            new PDLAnd(
                                new PDLBelongs(new PDLPosVar("x"), new PDLSetVar("X")),
                                new PDLForallFO("y",
                                    new PDLIf(
                                        new PDLBelongs(new PDLPosVar("y"), new PDLSetVar("X")),
                                        new PDLBelongs(posPlusN(new PDLPosVar("y"), 4), new PDLSetVar("X"))
                                    )
                                )
                            ),
                            new PDLExistsFO("y",
                                new PDLAnd(
                                    new PDLNot(new PDLPosEq(new PDLPosVar("x"), new PDLPosVar("y"))),
                                    new PDLAnd(
                                        new PDLAtPos('0', new PDLPosVar("y")),
                                        new PDLBelongs(new PDLPosVar("y"), new PDLSetVar("X"))
                                    )
                                )
                            )
                        )
                    )
                )
            );

            return new Testcase(10,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase11()
        {
            PDLPred xInA = new PDLBelongs(new PDLPosVar("x"), new PDLSetVar("A"));
            PDLPred xInB = new PDLBelongs(new PDLPosVar("x"), new PDLSetVar("B"));
            PDLPred xInC = new PDLBelongs(new PDLPosVar("x"), new PDLSetVar("C"));
            PDLPred yInA = new PDLBelongs(new PDLPosVar("y"), new PDLSetVar("A"));
            PDLPred yInB = new PDLBelongs(new PDLPosVar("y"), new PDLSetVar("B"));
            PDLPred yInC = new PDLBelongs(new PDLPosVar("y"), new PDLSetVar("C"));
            PDLPred zInA = new PDLBelongs(new PDLPosVar("z"), new PDLSetVar("A"));
            PDLPred zInB = new PDLBelongs(new PDLPosVar("z"), new PDLSetVar("B"));
            PDLPred zInC = new PDLBelongs(new PDLPosVar("z"), new PDLSetVar("C"));
            PDLPred xOnlyInA = new PDLAnd(xInA, new PDLAnd(new PDLNot(xInB), new PDLNot(xInC)));
            PDLPred xOnlyInB = new PDLAnd(new PDLNot(xInA), new PDLAnd(xInB, new PDLNot(xInC)));
            PDLPred xOnlyInC = new PDLAnd(new PDLNot(xInA), new PDLAnd(new PDLNot(xInB), xInC));

            PDLPred problem = new PDLForallSO("A", new PDLForallSO("B", new PDLForallSO("C",
                new PDLIf(
                    new PDLForallFO("x",
                        new PDLAnd(
                            new PDLIf(new PDLAtPos('a', new PDLPosVar("x")), xOnlyInA),
                        new PDLAnd(
                            new PDLIf(new PDLAtPos('b', new PDLPosVar("x")), xOnlyInB),
                            new PDLIf(new PDLAtPos('c', new PDLPosVar("x")), xOnlyInC)
                        ) )
                    ),
                    new PDLForallFO("x", new PDLForallFO("y", 
                        new PDLAnd(
                            new PDLIf(new PDLAnd(xInA, yInB), new PDLPosLe(new PDLPosVar("x"), new PDLPosVar("y"))),
                            new PDLIf(new PDLAnd(xInB, yInC), new PDLPosLe(new PDLPosVar("x"), new PDLPosVar("y")))
                        )
                    ) )
                )
            ) ) );
            return new Testcase(11,new char[] { 'a','b','c' }, problem);
        }

        public static Testcase createTestcase12()
        {
            // TODO: Go over formula again
            return new NonexistingTestcase();
        }

        public static Testcase createTestcase13()
        {
            PDLPred language = new PDLExistsFO("x",
                new PDLAnd(
                    new PDLAtPos('1', new PDLPosVar("x")),
                    new PDLPosGeq(
                        new PDLPosVar("x"),
                        posMinusN(new PDLLast(), 9)
                    )
                )
            );

            return new Testcase(13, oneZeroAlphabet, language);
        }

        public static Testcase createTestcase14()
        {
            PDLPred language = new PDLAnd(
                new PDLExistsFO("x", new PDLAtPos('a', new PDLPosVar("x"))),
                new PDLExistsFO("y", new PDLAtPos('b', new PDLPosVar("y")))
            );

            return new Testcase(14, abAlphabet, language);
        }

        public static Testcase createTestcase15()
        {
            PDLPred language = new PDLAtPos('1', posMinusN(new PDLLast(), 9));
            return new Testcase(15, oneZeroAlphabet, language);
        }

        public static Testcase createTestcase16()
        {
            PDLPred language = new PDLAnd(
                new PDLIntGe(new PDLIndicesOf("a"), 0),
                new PDLIntGe(new PDLIndicesOf("b"), 0)
            );

            return new Testcase(16, abAlphabet, language);
        }

        public static Testcase createTestcase17()
        {
            PDLPred language = new PDLIntLeq(new PDLIndicesOf("11"), 1);
            return new Testcase(17, oneZeroAlphabet, language);
        }

        public static Testcase createTestcase18()
        {
            PDLPred language = new PDLForallFO("x",
            new PDLForallFO("y",
                new PDLIf(
                    new PDLAnd(
                        new PDLBelongs(new PDLPosVar("x"), new PDLIndicesOf("00")),
                        new PDLBelongs(new PDLPosVar("y"), new PDLIndicesOf("11"))
                    ),
                    new PDLPosLe(new PDLPosVar("x"), new PDLPosVar("y"))
                )
            ));

            return new Testcase(18, oneZeroAlphabet, language);
        }

        public static Testcase createTestcase19()
        {
            PDLPred language = new PDLModSetEq(new PDLIndicesOf("0"), 5, 0);
            return new Testcase(19,oneZeroAlphabet, language);
        }

        public static Testcase createTestcase20()
        {
            PDLPred language = new PDLIntEq(new PDLIndicesOf("101"), 0);
            return new Testcase(20, oneZeroAlphabet, language);
        }

        public static Testcase createTestcase21()
        {
            PDLPred language = new PDLAnd(
                new PDLModSetEq(new PDLIndicesOf("0"), 5, 0),
                new PDLModSetEq(new PDLIndicesOf("1"), 2, 0)
            );

            return new Testcase(21,oneZeroAlphabet, language);
        }

        public static PDLPos posPlusN(PDLPos position, int n)
        {
            if (n == 0)
            {
                return position;
            }
            else
            {
                return posPlusN(new PDLSuccessor(position), n - 1);
            }
        }

        public static PDLPos posMinusN(PDLPos position, int n)
        {
            if (n == 0)
            {
                return position;
            }
            else
            {
                return posMinusN(new PDLPredecessor(position), n - 1);
            }
        }
    }

    [TestClass]
    public class ProblemGenerationTest
    {
        /// <summary>
        /// Just checks that the given testcase can be run without any runtime errors. Checking that the correct
        /// formulas are generated is infeasible, due to the large number of generated formulas
        /// </summary>
        /// <param name="testcase">Some testcase</param>
        public void RunTestcase(Testcase testcase)
        {
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.NONE, PdlFilter.Filtermode.NONE);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.NONE, PdlFilter.Filtermode.TRIVIAL);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.NONE, PdlFilter.Filtermode.STATEBASED);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.NONE, PdlFilter.Filtermode.BOTH);

            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.EQUAL, PdlFilter.Filtermode.NONE);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.EQUAL, PdlFilter.Filtermode.TRIVIAL);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.EQUAL, PdlFilter.Filtermode.STATEBASED);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.EQUAL, PdlFilter.Filtermode.BOTH);

            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.INEQUAL, PdlFilter.Filtermode.NONE);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.INEQUAL, PdlFilter.Filtermode.TRIVIAL);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.INEQUAL, PdlFilter.Filtermode.STATEBASED);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.INEQUAL, PdlFilter.Filtermode.BOTH);

            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.BOTH, PdlFilter.Filtermode.NONE);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.BOTH, PdlFilter.Filtermode.TRIVIAL);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.BOTH, PdlFilter.Filtermode.STATEBASED);
            ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, VariableCache.ConstraintMode.BOTH, PdlFilter.Filtermode.BOTH);
        }

        [TestMethod]
		public void TestTestcase0()
		{
			Testcase testcase = Testcases.createTestcase0();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase1()
		{
			Testcase testcase = Testcases.createTestcase1();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase2()
		{
			Testcase testcase = Testcases.createTestcase2();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase3()
		{
			Testcase testcase = Testcases.createTestcase3();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase4()
		{
			Testcase testcase = Testcases.createTestcase4();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase5()
		{
			Testcase testcase = Testcases.createTestcase5();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase6()
		{
			Testcase testcase = Testcases.createTestcase6();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase7()
		{
			Testcase testcase = Testcases.createTestcase7();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase8()
		{
			Testcase testcase = Testcases.createTestcase8();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase9()
		{
			Testcase testcase = Testcases.createTestcase9();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase10()
		{
			Testcase testcase = Testcases.createTestcase10();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase11()
		{
			Testcase testcase = Testcases.createTestcase11();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase12()
		{
			Testcase testcase = Testcases.createTestcase12();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase13()
		{
			Testcase testcase = Testcases.createTestcase13();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase14()
		{
			Testcase testcase = Testcases.createTestcase14();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase15()
		{
			Testcase testcase = Testcases.createTestcase15();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase16()
		{
			Testcase testcase = Testcases.createTestcase16();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase17()
		{
			Testcase testcase = Testcases.createTestcase17();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase18()
		{
			Testcase testcase = Testcases.createTestcase18();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase19()
		{
			Testcase testcase = Testcases.createTestcase19();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase20()
		{
			Testcase testcase = Testcases.createTestcase20();
			RunTestcase(testcase);
		}
		[TestMethod]
		public void TestTestcase21()
		{
			Testcase testcase = Testcases.createTestcase21();
			RunTestcase(testcase);
		}

    }
}
