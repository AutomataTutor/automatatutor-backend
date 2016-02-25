using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Z3;
using AutomataPDL;


namespace TestPDL
{
    [TestClass]
    public class Benchmarks
    {
        [TestMethod]
        public void Test1() // exists 'a'
        {
            PDLPred phi = new PDLExistsFO("x", new PDLAtPos('a', new PDLPosVar("x")));
            PrintDFA(phi, "Test1", new List<Char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test2() // length 2
        {
            PDLPred phi = new PDLIntEq(new PDLAllPos(), 2);
            PrintDFA(phi, "Test2", new List<Char> { 'a', 'b', 'c' });
        }

        [TestMethod]
        public void Test3() // first pos = last pos, length = 1
        {
            PDLPred phi = new PDLPosEq(new PDLFirst(), new PDLLast());
            PrintDFA(phi, "Test3", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test4() // second and second-last position are same, (length = 3)
        {
            PDLPred phi = new PDLPosEq(new PDLSuccessor(new PDLFirst()), new PDLPredecessor(new PDLLast()));
            PrintDFA(phi, "Test4", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test5() // all string of odd length
        {
            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 2, 1);
            PrintDFA(phi, "Test5", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test6() // all strings of length divisible by 3
        {
            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 3, 0);
            PrintDFA(phi, "Test6", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test7() // all strings of length % 4 = 3
        {
            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 4, 3);
            PrintDFA(phi, "Test7", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test8() // all string of length % 5 =3
        {
            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 5, 3);
            PrintDFA(phi, "Test8", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test9() // length % 6 = 3
        {
            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 6, 3);
            PrintDFA(phi, "Test9", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test10() // all odd postions have 'a'
        {
            PDLPred phi = new PDLForallFO("p", new PDLIf(new PDLModSetEq(
                new PDLAllPosBefore(new PDLPosVar("p")), 2, 1), new PDLAtPos('a', new PDLPosVar("p"))));
            PrintDFA(phi, "Test10", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test11() // first symbol = last symbol
        {
            PDLPred phi = new PDLAnd(new PDLIf(new PDLAtPos('a', new PDLFirst()), new PDLAtPos('a', new PDLLast())),
                new PDLIf(new PDLAtPos('b', new PDLFirst()), new PDLAtPos('b', new PDLLast())));
            PrintDFA(phi, "Test11", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test12() // ab appears twice
        {
            PDLPred phi = new PDLIntEq(new PDLIndicesOf("ab"), 2);
            PrintDFA(phi, "Test12", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test13() // abc appears as a substring
        {
            PDLPred phi = new PDLNot(new PDLIntLeq(new PDLIndicesOf("abc"), 0));
            PrintDFA(phi, "Test13", new List<char> { 'a', 'b', 'c' });
        }

        [TestMethod]
        public void Test14() // all strings whose value when interpreted as numbers in binary is divisible by 3
        {
            //(number of 1's in even postion mod 3) is same as (number of 1's in odd position mod 3)
            int i;
            PDLPosVar p = new PDLPosVar("p");
            PDLPred phi = new PDLFalse();

            for (i = 0; i < 3; i++)
            {
                phi = new PDLOr(phi, new PDLAnd(
                    new PDLModSetEq(new PDLIntersect
                    (new PDLPredSet("p", new PDLAtPos('b', p)),
                    (new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(p), 2, 0)))), 3, i),
                    new PDLModSetEq(new PDLIntersect
                    (new PDLPredSet("p", new PDLAtPos('b', p)),
                    (new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(p), 2, 1)))), 3, i)));

            }

            PrintDFA(phi, "Test14", new List<char> { 'a', 'b' });

        }

        [TestMethod]
        public void Test15() // length is either divisible by 2 or 3
        {
            PDLPred phi = new PDLOr(new PDLModSetEq(new PDLAllPos(), 2, 0), new PDLModSetEq(new PDLAllPos(), 3, 0));
            PrintDFA(phi, "Test15", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test16() // string containing aa and ending with ab
        {
            PDLPred phi1 = new PDLNot(new PDLIntLeq(new PDLIndicesOf("aa"), 0));
            PDLPred phi2 = new PDLAnd(new PDLAtPos('a', new PDLPredecessor(new PDLLast())), new PDLAtPos('b', new PDLLast()));
            PDLPred phi = new PDLAnd(phi1, phi2);

            PrintDFA(phi, "Test16", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test17() // string not containing aa but ends with ab
        {
            PDLPred phi1 = new PDLNot(new PDLIntLeq(new PDLIndicesOf("aa"), 0));
            PDLPred phi2 = (new PDLBelongs(new PDLPredecessor(new PDLLast()), new PDLIndicesOf("ab")));
            PDLPred phi = new PDLAnd(phi1, new PDLNot(phi2));

            PrintDFA(phi, "Test17", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test18() // aaa appears exactly once
        {
            PDLPred phi = new PDLIntEq(new PDLIndicesOf("aaa"), 1);
            PrintDFA(phi, "Test18", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test19() //any two a's are separated by even number of symbols
        {
            PDLPos px = new PDLPosVar("x");
            PDLPos py = new PDLPosVar("y");
            //TODO - is it possible to consider string as type PDLvar?
            PDLPred phi = new PDLForallFO("x", new PDLForallFO("y", new PDLIf(
                new PDLAnd(new PDLAtPos('a', px), new PDLAtPos('a', py)),
                    new PDLModSetEq(new PDLIntersect(new PDLAllPosAfter(px), new PDLAllPosBefore(py)), 2, 0))));

            PrintDFA(phi, "Test19", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test20() //b*aaab*
        {
            PDLPred phi = new PDLAnd(new PDLIntEq(new PDLIndicesOf("aaa"), 1), new PDLIntEq(new PDLIndicesOf("a"), 3));
            PrintDFA(phi, "Test20", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test21() // 
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, b));
            moves.Add(new Move<BDD>(0, 1, a));
            moves.Add(new Move<BDD>(1, 1, a));
            moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 2, a));
            moves.Add(new Move<BDD>(2, 2, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test21", al);
        }

        [TestMethod]
        public void Test22() //
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var moves = new List<Move<BDD>>();

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            moves.Add(new Move<BDD>(0, 1, a));
            moves.Add(new Move<BDD>(0, 4, b));
            moves.Add(new Move<BDD>(1, 4, a));
            moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 3, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 2, a));
            moves.Add(new Move<BDD>(3, 2, b));
            moves.Add(new Move<BDD>(4, 4, a));
            moves.Add(new Move<BDD>(4, 4, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test22", al);
        }

        [TestMethod]
        public void Test23() // Begins with bab
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 2, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 3, a));
            moves.Add(new Move<BDD>(3, 3, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 3 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test23", al);
        }

        [TestMethod]
        public void Test24() // contains the string ba
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, a)); moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 1, b)); moves.Add(new Move<BDD>(1, 2, a));
            moves.Add(new Move<BDD>(2, 2, a)); moves.Add(new Move<BDD>(2, 2, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test24", al);
        }

        [TestMethod]
        public void Test25() // contains the string bba
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, a)); moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 0, a)); moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 3, a)); moves.Add(new Move<BDD>(2, 2, b));
            moves.Add(new Move<BDD>(3, 3, a)); moves.Add(new Move<BDD>(3, 3, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 3 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test25", al);

        }

        [TestMethod]
        public void Test26()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 3, a)); moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 2, a)); moves.Add(new Move<BDD>(1, 1, b));
            moves.Add(new Move<BDD>(3, 3, a));

            var dfa = Automaton<BDD>.Create(0, new int[] { 2, 3 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test26", al);
        }

        [TestMethod]
        public void Test27() // # of a even and # of b mod 3 = 0
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 3, a)); moves.Add(new Move<BDD>(0, 1, b));

            moves.Add(new Move<BDD>(1, 4, a)); moves.Add(new Move<BDD>(1, 2, b));

            moves.Add(new Move<BDD>(2, 5, a)); moves.Add(new Move<BDD>(2, 0, b));

            moves.Add(new Move<BDD>(3, 0, a)); moves.Add(new Move<BDD>(3, 4, b));

            moves.Add(new Move<BDD>(4, 1, a)); moves.Add(new Move<BDD>(4, 5, b));

            moves.Add(new Move<BDD>(5, 2, a)); moves.Add(new Move<BDD>(5, 3, b));


            var dfa = Automaton<BDD>.Create(0, new int[] { 0 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test27", al);

        }

        [TestMethod]
        public void Test28() // not containing bba
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, a)); moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 0, a)); moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 2, b));
            moves.Add(new Move<BDD>(2, 3, a)); moves.Add(new Move<BDD>(3, 3, a)); moves.Add(new Move<BDD>(3, 3, b));


            var dfa = Automaton<BDD>.Create(0, new int[] { 0, 1, 2 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test28", al);

        }

        [TestMethod]
        public void Test29() // Contains a 'a', and # of 'b's appearing after the first a is odd
        {
            PDLPos x = new PDLPosVar("x");
            PDLPos p = new PDLPosVar("p");
            PDLPred phi = new PDLModSetEq(new PDLIntersect(new PDLIndicesOf("b"),
                                            new PDLPredSet("p", new PDLExistsFO("x", new PDLAnd(new PDLAtPos('a', x),
                                                new PDLPosLe(x, p))))), 2, 0);

            PrintDFA(phi, "Test29", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test30() // Length atleast 3, and third symbol is 'a', (correct but not most succint)
        {
            PDLPred phi = new PDLAtPos('a', new PDLSuccessor(new PDLSuccessor(new PDLSuccessor(new PDLFirst()))));
            PrintDFA(phi, "Test30", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test31() // Starts with a and odd length, or starts with b and even length
        {
            PDLPred phi = new PDLOr(new PDLAnd(new PDLStartsWith("a"), new PDLModSetEq(new PDLAllPos(), 2, 1)),
                new PDLAnd(new PDLStartsWith("b"), new PDLModSetEq(new PDLAllPos(), 2, 0)));

            PrintDFA(phi, "Test31", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test32() // Even # of a's or exactly 2 b's
        {
            PDLPred phi = new PDLOr(new PDLModSetEq(new PDLIndicesOf("a"), 2, 0), new PDLIntEq(new PDLIndicesOf("b"), 2));
            PrintDFA(phi, "Test32", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test33() // All b's appear consecutively and string ends with a, alternately a*b*a*a
        {
            PDLPred phi = new PDLAnd(new PDLIntLeq(new PDLIndicesOf("ba"), 1), new PDLEndsWith("a"));
            PrintDFA(phi, "Test33", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test34() // All b's appear in pairs only, a*bb(aa*bb)*a*,
        {
            var x = new PDLPosVar("x");
            PDLPred phi = new PDLForallFO("x", new PDLIf(new PDLAtPos('b', x),
                                                new PDLIff(new PDLAtPos('b', new PDLSuccessor(x)), new PDLNot(new PDLAtPos('b', new PDLPredecessor(x))))));
            PrintDFA(phi, "Test34", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test35() // (ab)*
        {
            PDLPos p = new PDLPosVar("p");
            PDLPred phi = new PDLAnd(new PDLAtSet('a', new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(p), 2, 1))),
                            new PDLAnd(new PDLAtSet('b', new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(p), 2, 0))),
                                new PDLModSetEq(new PDLAllPos(), 2, 0)));
            PrintDFA(phi, "Test35", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test36() // does not contain bba
        {
            PDLPred phi = new PDLIntEq(new PDLIndicesOf("bba"), 0);
            PrintDFA(phi, "Test36", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test37()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, a)); moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 2, a)); moves.Add(new Move<BDD>(1, 1, b));
            moves.Add(new Move<BDD>(2, 0, a)); moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 2, a)); moves.Add(new Move<BDD>(3, 1, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 3 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test37", al);
        }

        [TestMethod]
        public void Test38() // a*bb*
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, a)); moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 2, a)); moves.Add(new Move<BDD>(1, 1, b));
            moves.Add(new Move<BDD>(2, 2, a)); moves.Add(new Move<BDD>(2, 2, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 1 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test38", al);
        }

        [TestMethod]
        public void Test39()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<Char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, a)); moves.Add(new Move<BDD>(0, 1, a));
            moves.Add(new Move<BDD>(1, 1, a)); moves.Add(new Move<BDD>(1, 0, b));

            var dfa = Automaton<BDD>.Create(0, new int[] { 0 }, moves).Determinize(solver).Minimize(solver);

            PrintDFA(dfa, "Test39", al);
        }

        [TestMethod]
        public void Test40() // starts with a ends with b, or b
        {
            PDLPred phi = new PDLOr(new PDLAnd(new PDLStartsWith("a"), new PDLEndsWith("b")), new PDLIsString("b"));
            PrintDFA(phi, "Test40", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test41() // (length >= 3) --> (atmost 1 b)
        {
            PDLPred phi = new PDLIf(new PDLNot(new PDLIntLeq(new PDLAllPos(), 2)), new PDLIntLeq(new PDLIndicesOf("b"), 1));
            PrintDFA(phi, "Test41", new List<char> { 'a', 'b' });
        }

        [TestMethod]
        public void Test42() // #a's = #b's and Each even prefix has equal number of a's and b's, (ab+ba)*
        {
            var x = new PDLPosVar("x");
            var y = new PDLPosVar("y");
            PDLPred phi = new PDLForallFO("x", new PDLIf(new PDLModSetEq(new PDLAllPosUpto(x), 2, 1), new PDLExistsFO("y",
                new PDLAnd(new PDLIsSuccessor(x, y), new PDLIff(new PDLAtPos('a', x), new PDLAtPos('b', y))))));

            PrintDFA(phi, "Test42", new List<char> { 'a', 'b' });
        }

        #region Private methods
        private static void PrintDFA(PDLPred phi, string name, List<char> alph)
        {
            HashSet<char> al = new HashSet<char>(alph);
            var solver = new CharSetSolver(BitWidth.BV64);

            PrintDFA(phi.GetDFA(al, solver), name, al);
        }

        private static void PrintDFA(Automaton<BDD> dfa, string name, HashSet<char> al)
        {
            var sb = new StringBuilder();
            DFAUtilities.printDFA(dfa, al, sb);

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"../../../TestPDL/DFAs/" + name + ".txt");
            file.WriteLine(sb);
            file.Close();
        } 
        #endregion
    }
}
