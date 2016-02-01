using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutomataPDL;
using AutomataPDL.Parse;
using Microsoft.Automata;

namespace TestPDL
{
    [TestClass]
    public class PDLParserTest
    {
        [TestMethod]
        public void ParseSimple()
        {
            string s = "a @ {x | (|allUpto x| % 2=0) }";
            //s = "endsWith 'a'";
            Console.WriteLine(PDL.ParseString(s).ToString());

        }

        [TestMethod]
        public void ParseAnd()
        {
            string s = "(all x0. ((x0==x0) V (true & false)))";
            try
            {
                Console.WriteLine(PDL.ParseString(s).ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [TestMethod]
        public void ParseBig()
        {
            string s = "(((| X0 | % 2 > 1)) & (all x0. ((x0==x0) V (true & false))))";
            //s = "S(first) < ?x";
            try
            {
                var v = PDL.ParseString(s);
                Console.WriteLine(v.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [TestMethod]
        public void ParseMany()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            for (int i = 1; i < 43;i++ )
            {                
                var pair = runTest("Test" + i,solver);
                var phi = pair.First;
                var al = pair.Second;
                if (pair.First != null)
                {
                    var str = pair.First.ToString();
                    var psi = PDL.ParseString(str);
                    Assert.IsTrue(phi.IsEquivalentWith(psi, al, solver));
                    Console.WriteLine("{0} == {1}",phi,psi);
                }
            }            
        }

        long timeout = 1000;
        private Pair<PDLPred,HashSet<char>> runTest(string testName, CharSetSolver solver)
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            
            var dfa_al = ReadDFA(testName, solver);

            PDLPred synthPhi = null;
            if (dfa_al.First.Count == 2)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var phi in pdlEnumerator.SynthesizePDL(dfa_al.First, dfa_al.Second, solver, sb, timeout))
                {
                    synthPhi = phi;
                    break;
                }
            }

            return new Pair<PDLPred,HashSet<char>>(synthPhi,dfa_al.First);            
        }

        #region ACCESSORIES METHODS
        private static void PrintDFA(Automaton<BvSet> dfa, string name, HashSet<char> al)
        {
            var sb = new StringBuilder();
            DFAUtilities.printDFA(dfa, al, sb);

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"../../../TestPDL/DFAs/" + name + ".txt");
            file.WriteLine(sb);
            file.Close();
        }

        private static Pair<HashSet<char>, Automaton<BvSet>> ReadDFA(string name, CharSetSolver solver)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../TestPDL/DFAs/" + name + ".txt");
            string res = file.ReadToEnd();
            file.Close();
            return DFAUtilities.parseDFAFromString(res, solver);
        }
        #endregion
    }
}
