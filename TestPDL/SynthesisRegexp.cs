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

namespace TestPDL
{

    [TestClass]
    public class SynthesisRegexp
    {
        public const long timeout = 10000;

        [TestMethod]
        public void Test1()
        {
            runTest("Test1");
        }

        //[TestMethod]
        //public void Test18()
        //{
        //    runTest("Test18");
        //}

        //[TestMethod]
        //public void Test20()
        //{
        //    runTest("Test20");
        //}

        [TestMethod]
        public void Test21()
        {
            runTest("Test21");
        }

        //[TestMethod]
        //public void Test29()
        //{
        //    runTest("Test29");
        //}

        private void runTest(string testName)
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_al = ReadDFA(testName, solver);

            Regexp regexp = null;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("*------------------------------------");
            sb.AppendLine("| " + testName);
            sb.AppendLine("|------------------------------------");

            foreach (var r in RegexpSynthesis.SynthesizeRegexp(dfa_al.First, dfa_al.Second, solver, sb, timeout))
            {
                regexp = r;
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
