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
    public class SynthesisMulti
    {
        public const int timeout = 1000;

        [TestMethod]
        public void Exists()
        {
            PDLPred phi = new PDLExistsFO("x", new PDLAtPos('a', new PDLPosVar("x")));

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "Exists");
        }

        [TestMethod]
        public void intEq2()
        {
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLAllPos(), 2);
            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "intEq2");
        }
        

        [TestMethod]
        public void FirstLastEq()
        {
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLPosEq(new PDLFirst(), new PDLLast());
            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "FirstLastEq");
        }


        [TestMethod]
        public void allpos()
        {
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLAllPos(), 3);

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "allpos");
        }

        [TestMethod]
        public void mod2()
        {
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 2, 1);

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "mod2");
        }

        [TestMethod]
        public void mod3()
        {
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 3, 2);

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "mod3");
        }       


        [TestMethod]
        public void OddA()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLForallFO("p", new PDLIf(new PDLModSetEq(
                new PDLAllPosBefore(new PDLPosVar("p")), 2, 1), new PDLAtPos('a', new PDLPosVar("p"))));

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "OddA");
        }

        [TestMethod]
        public void SameFirstLast()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLAnd(new PDLIf(new PDLAtPos('a', new PDLFirst()), new PDLAtPos('a', new PDLLast())),
                new PDLIf(new PDLAtPos('b', new PDLFirst()), new PDLAtPos('b', new PDLLast())));

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "SameFirstLast");
        }

        [TestMethod]
        public void abTwice()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLIndicesOf("ab"), 2);
            var dfa = phi.GetDFA(al, solver);

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "abTwice");
        }

        [TestMethod]
        public void abcSubStr()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLNot(new PDLIntLeq(new PDLIndicesOf("abc"), 0));

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "abcSubStr");
        }

        [TestMethod]
        public void div2or3()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLOr(new PDLModSetEq(new PDLAllPos(), 2, 0), new PDLModSetEq(new PDLAllPos(), 3, 0));

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "div2or3");
        }

        [TestMethod]
        public void contains_aa_end_ab()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //new PDL

            PDLPred phi1 = new PDLNot(new PDLIntLeq(new PDLIndicesOf("aa"), 0));
            PDLPred phi2 = new PDLAnd(new PDLAtPos('a', new PDLPredecessor(new PDLLast())), new PDLAtPos('b', new PDLLast()));
            PDLPred phi = new PDLAnd(phi1, phi2);

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "contains_aa_end_ab");
        }

        [TestMethod]
        public void contains_aa_notend_ab()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //new PDL

            PDLPred phi1 = new PDLNot(new PDLIntLeq(new PDLIndicesOf("aa"), 0));
            PDLPred phi2 = (new PDLBelongs(new PDLPredecessor(new PDLLast()), new PDLIndicesOf("ab")));
            //PDLpred phi2 = new PDLAnd(new PDLatPos('a', new PDLprev(new PDLlast())), new PDLatPos('b', new PDLlast()));
            PDLPred phi = new PDLAnd(phi1, new PDLNot(phi2));
            //new PDLAnd(new PDLatPos('a', new PDLprev(new PDLlast())), new PDLatPos('b', new PDLlast())));

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "contains_aa_notend_ab");
        }

        [TestMethod]
        public void once_aaa()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLIndicesOf("aaa"), 1);

            
            StringBuilder sb = new StringBuilder();

            List<Pair<PDLPred, long>> pairs = SynthTimer(phi, al, sb);

            Output(sb, "once_aaa");
        }


        //ACCESSORIES METHODS

        private static void Output(StringBuilder sb, string nameFile)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"../../../TestPDL/MultipleResults/" + nameFile + (nameFile.EndsWith(".txt") ? "" : ".txt"));
            file.Write(sb.ToString());
            file.Close();
        }

        private static T Execute<T>(Func<T> func, int timeout)
        {
            T result;
            TryExecute(func, timeout, out result);
            return result;
        }

        private static bool TryExecute<T>(Func<T> func, int timeout, out T result)
        {
            var t = default(T);
            var thread = new Thread(() => t = func());
            thread.Start();
            var completed = thread.Join(timeout);
            if (!completed) thread.Abort();
            result = t;
            return completed;
        }

        private List<Pair<PDLPred, long>> SynthTimer(PDLPred phi, HashSet<char> al, StringBuilder sb)
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();

            var solver = new CharSetSolver(BitWidth.BV64);
            Stopwatch sw = new Stopwatch();
            var dfa = phi.GetDFA(al, solver);

            List<Pair<PDLPred, long>> predList = new List<Pair<PDLPred, long>>();
            List<PDLPred> phiList = new List<PDLPred>();

            PDLPred v;
            var func = new Func<PDLPred>(() =>
            {
                sw.Start();
                foreach (var p in pdlEnumerator.SynthesizePDL(al, dfa, solver,new StringBuilder(),5000))
                {
                    sw.Stop();
                    predList.Add(new Pair<PDLPred, long>(p, sw.ElapsedMilliseconds));                    
                    sw.Start();
                }
                return null;
                
            });            
            
            
            var test = TryExecute(func, timeout, out v);

            phi.ToString(sb);
            sb.AppendLine();
            sb.AppendLine("=");
            foreach (var pair in predList)
            {
                sb.AppendLine();
                pair.First.ToString(sb);
                sb.AppendLine();
                sb.AppendLine("Elapsed Time: " + pair.Second + " ms");
            }
            return predList;
        }
    }
}
