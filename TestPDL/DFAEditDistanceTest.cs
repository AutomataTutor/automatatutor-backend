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
    public class DFAEditDistanceTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi1 = new PDLContains("b");
            var dfa1 = phi1.GetDFA(al, solver);

            PDLPred phi2 = new PDLModSetEq(new PDLPredSet("x", new PDLAtPos('b', new PDLPosVar("x"))), 2, 1);
            var dfa2 = phi2.GetDFA(al, solver);

            StringBuilder sb = new StringBuilder();
            DFAEditDistance.GetDFAOptimalEdit(dfa1, dfa2, al, solver, 3000, sb);
            Console.WriteLine(sb);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi1 = new PDLForallFO("p", new PDLIf(new PDLModSetEq(
                new PDLAllPosBefore(new PDLPosVar("p")), 2, 1), new PDLAtPos('a', new PDLPosVar("p"))));
            var dfa1 = phi1.GetDFA(al, solver);

            PDLPred phi2 = new PDLForallFO("p", new PDLIf(new PDLModSetEq(
                new PDLAllPosBefore(new PDLPosVar("p")), 2, 1), new PDLAtPos('b', new PDLPosVar("p"))));
            var dfa2 = phi2.GetDFA(al, solver);

            StringBuilder sb = new StringBuilder();
            DFAEditDistance.GetDFAOptimalEdit(dfa1, dfa2, al, solver, 3000, sb);
            Console.WriteLine(sb);
        }
    }
}
