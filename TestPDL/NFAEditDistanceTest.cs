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
    public class NFAEditDistanceTest
    {
        public static long timeout = 3500;
    
        [TestMethod]
        public void TestStateToggling()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, a));
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 1, a));
            moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 2, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 3, a));
            moves.Add(new Move<BDD>(3, 3, b));

            var nfa1 = Automaton<BDD>.Create(0, new int[] { 0, 3 }, moves);

            var moves1 = new List<Move<BDD>>();

            moves1.Add(new Move<BDD>(0, 0, a));
            moves1.Add(new Move<BDD>(0, 1, b));
            moves1.Add(new Move<BDD>(1, 1, a));
            moves1.Add(new Move<BDD>(1, 2, b));
            moves1.Add(new Move<BDD>(2, 2, a));
            moves1.Add(new Move<BDD>(2, 3, b));
            moves1.Add(new Move<BDD>(3, 3, a));
            moves1.Add(new Move<BDD>(3, 3, b));

            var nfa2 = Automaton<BDD>.Create(0, new int[] { 3 }, moves1);

            NFAEditDistanceProvider nfaedp = new NFAEditDistanceProvider(nfa1, al, solver, timeout);
            var distanceNfa1Nfa2 = nfaedp.GetNFAOptimalEdit(nfa2);
            Assert.IsTrue(distanceNfa1Nfa2.GetCost() == 1);
        }

        [TestMethod]
        public void TestMoveCharToggling1()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, a));
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 1, a));
            moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 2, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 3, a));
            moves.Add(new Move<BDD>(3, 3, b));

            var nfa1 = Automaton<BDD>.Create(0, new int[] { 0, 3 }, moves);

            

            var moves3 = new List<Move<BDD>>();

            moves3.Add(new Move<BDD>(0, 1, b));
            moves3.Add(new Move<BDD>(1, 1, a));
            moves3.Add(new Move<BDD>(1, 2, b));
            moves3.Add(new Move<BDD>(2, 2, a));
            moves3.Add(new Move<BDD>(3, 3, a));
            moves3.Add(new Move<BDD>(3, 3, b));

            var nfa3 = Automaton<BDD>.Create(0, new int[] { 0, 3 }, moves3);

            var sb = new StringBuilder();

            NFAEditDistanceProvider nfaedp = new NFAEditDistanceProvider(nfa1, al, solver, timeout);
            var distanceNfa1Nfa3 = nfaedp.GetNFAOptimalEdit(nfa3);
            Assert.IsTrue(distanceNfa1Nfa3.GetCost() == 2);
        }

        [TestMethod]
        public void TestMoveCharToggling2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, a));
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 1, a));
            moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 2, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 3, b));

            var nfa1 = Automaton<BDD>.Create(0, new int[] { 0, 3 }, moves);



            var moves3 = new List<Move<BDD>>();

            moves3.Add(new Move<BDD>(0, 1, b));
            moves3.Add(new Move<BDD>(1, 1, a));
            moves3.Add(new Move<BDD>(1, 2, b));
            moves3.Add(new Move<BDD>(2, 2, a));
            moves3.Add(new Move<BDD>(2, 3, b));
            moves3.Add(new Move<BDD>(3, 3, a));
            moves3.Add(new Move<BDD>(3, 3, b));

            var nfa3 = Automaton<BDD>.Create(0, new int[] { 0, 3 }, moves3);

            var sb = new StringBuilder();

            NFAEditDistanceProvider nfaedp = new NFAEditDistanceProvider(nfa3, al, solver, timeout);
            var distanceNfa3Nfa1 = nfaedp.GetNFAOptimalEdit(nfa1);
            Assert.IsTrue(distanceNfa3Nfa1.GetCost() == 2);
        }

        [TestMethod]
        public void TestMoveCharToggling3()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves1 = new List<Move<BDD>>();

            moves1.Add(new Move<BDD>(0, 1, b));
            moves1.Add(new Move<BDD>(0, 2, a));
            moves1.Add(new Move<BDD>(1, 2, a));
            moves1.Add(new Move<BDD>(1, 2, b));

            var nfa1 = Automaton<BDD>.Create(0, new int[] { 2 }, moves1);

            var moves2 = new List<Move<BDD>>();

            moves2.Add(new Move<BDD>(0, 1, b));
            moves2.Add(new Move<BDD>(1, 2, a));
            moves2.Add(new Move<BDD>(1, 2, b));

            var nfa2 = Automaton<BDD>.Create(0, new int[] { 2 }, moves2);

            var sb = new StringBuilder();

            NFAEditDistanceProvider nfaedp = new NFAEditDistanceProvider(nfa1, al, solver, timeout);
            var distanceNfa1Nfa2 = nfaedp.GetNFAOptimalEdit(nfa2);
            Assert.IsTrue(distanceNfa1Nfa2.GetCost() == 1);
        }
    }
}
