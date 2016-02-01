using AutomataPDL;
using System.Collections.Generic;

namespace ProblemGenerationTest
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
    
    class TestPredicateFactory
    {
        public static IEnumerable<Testcase> createTestCases()
        {
            yield return createTestFormula1();
            yield return createTestFormula2();
            yield return createTestFormula3();
            yield return createTestFormula4();
            yield return createTestFormula5();
            yield return createTestFormula6();
            yield return createTestFormula7();
            yield return createTestFormula8();
            yield return createTestFormula9();
            yield return createTestFormula10();
            yield return createTestFormula11();
            yield return createTestFormula12();
            yield return createTestFormula13();
            yield return createTestFormula14();
            yield return createTestFormula15();
            yield return createTestFormula16();
            yield return createTestFormula17();
            yield return createTestFormula18();
            yield return createTestFormula19();
            yield return createTestFormula20();
            yield return createTestFormula21();
            yield return createTestFormula22();
        }

        public static Testcase createTestFormula1()
        {
            return new Testcase(1,createAlphabet10(), new PDLEndsWith("00"));
        }

        public static Testcase createTestFormula2()
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

            return new Testcase(2,createAlphabet10(), language);
        }

        public static Testcase createTestFormula3()
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

            return new Testcase(3,createAlphabet10(), language);
        }

        public static Testcase createTestFormula4()
        {
            return new Testcase(4,createAlphabet10(), new PDLIntGe(new PDLIndicesOf("001"), 0));
        }

        public static Testcase createTestFormula5()
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
            return new Testcase(5,createAlphabet10(), language);
        }

        public static Testcase createTestFormula6()
        {
            PDLPred language = new PDLAtPos('1', posMinusN(new PDLLast(), 9));
            return new Testcase(6,createAlphabet10(), language);
        }

        public static Testcase createTestFormula7()
        {
            PDLPred language = new PDLOr(new PDLStartsWith("01"), new PDLEndsWith("01"));
            return new Testcase(7,createAlphabet10(), language);
        }

        public static Testcase createTestFormula8()
        {
            PDLPred language = new PDLAnd(
                new PDLModSetEq(new PDLIndicesOf("0"), 5, 0),
                new PDLModSetEq(new PDLIndicesOf("1"), 3, 0));

            return new Testcase(8,createAlphabet10(), language);
        }

        public static Testcase createTestFormula9()
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

            return new Testcase(9,new char[] { 'a', 'b', 'c' }, language);
        }
        public static Testcase createTestFormula10()
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

            return new Testcase(10,new char[] { 'a', 'b', 'c' }, language);
        }

        public static Testcase createTestFormula11()
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

            return new Testcase(11,createAlphabet10(), language);
        }

        public static Testcase createTestFormula12()
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
            return new Testcase(12,new char[] { 'a','b','c' }, problem);
        }

        public static Testcase createTestFormula13()
        {
            // TODO: Go over formula again
            return new Testcase(13,null, new PDLTrue());
        }

        public static Testcase createTestFormula14()
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

            return new Testcase(14, createAlphabet10(), language);
        }

        public static Testcase createTestFormula15()
        {
            PDLPred language = new PDLAnd(
                new PDLExistsFO("x", new PDLAtPos('a', new PDLPosVar("x"))),
                new PDLExistsFO("y", new PDLAtPos('b', new PDLPosVar("y")))
            );

            return new Testcase(15, createAlphabetAB(), language);
        }

        public static Testcase createTestFormula16()
        {
            PDLPred language = new PDLAtPos('1', posMinusN(new PDLLast(), 9));
            return new Testcase(16, createAlphabet10(), language);
        }

        public static Testcase createTestFormula17()
        {
            PDLPred language = new PDLAnd(
                new PDLIntGe(new PDLIndicesOf("a"), 0),
                new PDLIntGe(new PDLIndicesOf("b"), 0)
            );

            return new Testcase(17, createAlphabetAB(), language);
        }

        public static Testcase createTestFormula18()
        {
            PDLPred language = new PDLIntLeq(new PDLIndicesOf("11"), 1);
            return new Testcase(18, createAlphabet10(), language);
        }

        public static Testcase createTestFormula19()
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

            return new Testcase(19, createAlphabet10(), language);
        }

        public static Testcase createTestFormula20()
        {
            PDLPred language = new PDLModSetEq(new PDLIndicesOf("0"), 5, 0);
            return new Testcase(20,createAlphabet10(), language);
        }

        public static Testcase createTestFormula21()
        {
            PDLPred language = new PDLIntEq(new PDLIndicesOf("101"), 0);
            return new Testcase(21, createAlphabet10(), language);
        }

        public static Testcase createTestFormula22()
        {
            PDLPred language = new PDLAnd(
                new PDLModSetEq(new PDLIndicesOf("0"), 5, 0),
                new PDLModSetEq(new PDLIndicesOf("1"), 2, 0)
            );

            return new Testcase(22,createAlphabet10(), language);
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

        private static IEnumerable<char> createAlphabetAB()
        {
            return new char[] { 'a', 'b' };
        }

        private static IEnumerable<char> createAlphabet10()
        {
            return new char[] { '0', '1' };
        }
    }
}
