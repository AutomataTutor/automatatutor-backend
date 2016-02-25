using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Z3;
using MSOZ3;
using AutomataPDL;

using System.Diagnostics;
using System.Threading;

namespace MSOZ3Test
{
    [TestClass]
    public class SynthesisTime
    {
        public const int iterations = 1;
        public const int timeout = 2000;


        [TestMethod]
        public void MyTest()
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 1, a));
            moves.Add(new Move<BDD>(0, 3, b));
            moves.Add(new Move<BDD>(1,2, b));
            moves.Add(new Move<BDD>(2, 1, a));
            moves.Add(new Move<BDD>(1, 1, a));
            moves.Add(new Move<BDD>(2, 2, b));

            moves.Add(new Move<BDD>(3, 4, a));
            moves.Add(new Move<BDD>(4, 3, b));
            moves.Add(new Move<BDD>(3, 3, b));
            moves.Add(new Move<BDD>(4, 4, a));

            var dfa1 = Automaton<BDD>.Create(0, new int[] { 0,1,3 }, moves).Determinize(solver).Minimize(solver);
            foreach (var v in pdlEnumerator.SynthesizePDL(al, dfa1, solver, new StringBuilder(), 5000))
            {
                Console.WriteLine(PDLUtil.ToEnglishString(v));
                break;
            }
        }

        [TestMethod]
        public void Test1()
        {
            runTest("Test1");
        }

        [TestMethod]
        public void Test2()
        {
            runTest("Test2");
        }

        [TestMethod]
        public void Test3()
        {
            runTest("Test3");
        }

        [TestMethod]
        public void Test4()
        {
            runTest("Test4");
        }

        [TestMethod]
        public void Test5()
        {
            runTest("Test5");
        }

        [TestMethod]
        public void Test6()
        {
            runTest("Test6");
        }

        [TestMethod]
        public void Test7()
        {
            runTest("Test7");
        }

        [TestMethod]
        public void Test8()
        {
            runTest("Test8");
        }

        [TestMethod]
        public void Test9()
        {
            runTest("Test9");
        }

        [TestMethod]
        public void Test10()
        {
            runTest("Test10");
        }

        [TestMethod]
        public void Test11()
        {
            runTest("Test11");
        }

        [TestMethod]
        public void Test12()
        {
            runTest("Test12");
        }

        [TestMethod]
        public void Test13()
        {
            runTest("Test13");
        }

        [TestMethod]
        public void Test14()
        {
            runTest("Test14");
        }

        [TestMethod]
        public void Test15()
        {
            runTest("Test15");
        }

        [TestMethod]
        public void Test16()
        {
            runTest("Test16");
        }

        [TestMethod]
        public void Test17()
        {
            runTest("Test17");
        }

        [TestMethod]
        public void Test18()
        {
            runTest("Test18");
        }

        [TestMethod]
        public void Test19()
        {
            runTest("Test19");
        }

        [TestMethod]
        public void Test20()
        {
            runTest("Test20");
        }

        [TestMethod]
        public void Test21()
        {
            runTest("Test21");
        }

        [TestMethod]
        public void Test22()
        {
            runTest("Test22");
        }

        [TestMethod]
        public void Test23()
        {
            runTest("Test23");
        }

        [TestMethod]
        public void Test24()
        {
            runTest("Test24");
        }

        [TestMethod]
        public void Test25()
        {
            runTest("Test25");
        }

        [TestMethod]
        public void Test26()
        {
            runTest("Test26");
        }

        [TestMethod]
        public void Test27()
        {
            runTest("Test27");
        }

        [TestMethod]
        public void Test28()
        {
            runTest("Test28");
        }

        [TestMethod]
        public void Test29()
        {
            runTest("Test29");
        }

        [TestMethod]
        public void Test30()
        {
            runTest("Test30");
        }

        [TestMethod]
        public void Test31()
        {
            runTest("Test31");
        }

        [TestMethod]
        public void Test32()
        {
            runTest("Test32");
        }

        [TestMethod]
        public void Test33()
        {
            runTest("Test33");
        }

        [TestMethod]
        public void Test34()
        {
            runTest("Test34");
        }

        [TestMethod]
        public void Test35()
        {
            runTest("Test35");
        }

        [TestMethod]
        public void Test36()
        {
            runTest("Test36");
        }

        [TestMethod]
        public void Test37()
        {
            runTest("Test37");
        }

        [TestMethod]
        public void Test38()
        {
            runTest("Test38");
        }

        [TestMethod]
        public void Test39()
        {
            runTest("Test39");
        }

        [TestMethod]
        public void Test40()
        {
            runTest("Test40");
        }

        [TestMethod]
        public void Test41()
        {
            runTest("Test41");
        }

        [TestMethod]
        public void Test42()
        {
            runTest("Test42");
        }        

        private void runTest(string testName)
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_al = ReadDFA(testName, solver);

            PDLPred synthPhi = null;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("*------------------------------------");
            sb.AppendLine("| " + testName);
            sb.AppendLine("|------------------------------------");

            foreach (var phi in pdlEnumerator.SynthesizePDL(dfa_al.First, dfa_al.Second, solver, sb, timeout))
            {
                synthPhi = phi;
                break;
            }

            sb.AppendLine("*------------------------------------");
            sb.AppendLine();

            System.Console.WriteLine(sb);
        }




        #region ACCESSORIES METHODS
        private static void PrintDFA(Automaton<BDD> dfa, string name, HashSet<char> al)
        {
            var sb = new StringBuilder();
            DFAUtilities.printDFA(dfa, al, sb);

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"../../../TestPDL/DFAs/" + name + ".txt");
            file.WriteLine(sb);
            file.Close();
        }

        private static Pair<HashSet<char>, Automaton<BDD>> ReadDFA(string name, CharSetSolver solver)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../TestPDL/DFAs/" + name + ".txt");
            string res = file.ReadToEnd();
            file.Close();
            return DFAUtilities.parseDFAFromString(res, solver);
        }
        #endregion

    }
}
