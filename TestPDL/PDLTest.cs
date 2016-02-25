using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Diagnostics;
using System.Threading;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

using MSOZ3;
using AutomataPDL;

namespace TestPDL
{
    [TestClass]
    public class PDLTest
    {
        [TestMethod]
        public void Exists()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLExistsFO("x", new PDLAtPos('a', new PDLPosVar("x")));

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^b*a(a|b)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            ////string file = "../../../TestPDL/DotFiles/exists";

            ////solver.SaveAsDot(dfa, "aut", file);

        }

        [TestMethod]
        public void intEq2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLAllPos(), 2);

            StringBuilder sb = new StringBuilder();


            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(a|b|c){2}$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            ////string file = "../../../TestPDL/DotFiles/IntEq2";

            //solver.SaveAsDot(dfa, "aut", file);

        }


        [TestMethod]
        public void FirstLast()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIsSuccessor(new PDLFirst(), new PDLLast());

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(a|b){2}$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            //string file = "../../../TestPDL/DotFiles/FirstLast";

            //solver.SaveAsDot(dfa, "aut", file);

        }

        [TestMethod]
        public void FirstLastEq()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLPosEq(new PDLFirst(), new PDLLast());

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(a|b){1}$");

            Console.WriteLine(phi.ToMSO(new FreshGen()).ToWS1S(solver).ToString());

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            //string file = "../../../TestPDL/DotFiles/FirstLastEq";

            //solver.SaveAsDot(dfa, "aut", file);

        }

        [TestMethod]
        public void check()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var X = "X0";
            var x = "x";

            //PDLpred phi = (new PDLindicesOf("ab")).contains(
            MSOFormula phi = new MSOExistsSO(X, new MSOAnd(new MSOForallFO(x, (new MSOBelong(x, X))), new MSOForallFO(x,
            new MSONot(new MSOBelong(x, X)))));
            //PDLpred phi = new PDLbelongs(new PDLlast(), new PDLallPos(), solver);
            //PDLpred phi = new PDLnot(new PDLForallFO("x", new PDLnot(new PDLPosEq(new PDLposVar("x", solver), new PDLposVar("x")))));
            //PDLpred phi = new PDLnot(new PDLForallFO("x", new PDLSucc(new PDLposVar("x",solver), new PDLposVar("x"))), solver);
            //PDLpred phi = new PDLatPos('a', new PDLfirst(), solver);

            //System.Console.WriteLine(AutomataPDL.BitVecUtil.CountBits(15, 0, 32));

            StringBuilder sb = new StringBuilder();

            phi.ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.getDFA(al, solver);

            //string file = "../../../TestPDL/DotFiles/check";
            System.Console.WriteLine("x" + 1.ToString());

            //solver.SaveAsDot(dfa, "aut", file);

        }

        [TestMethod]
        public void succ1()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPos f = new PDLFirst();
            PDLPos l = new PDLLast();
            PDLPos p = new PDLPosVar("x");

            PDLPred phi = new PDLExistsFO("x", new PDLAnd(new PDLIsSuccessor(f, p), new PDLIsSuccessor(p, l)));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(a|b){3}$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            //string file = "../../../TestPDL/DotFiles/succ1";

            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void succ2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPos f = new PDLFirst();
            PDLPos l = new PDLLast();
            PDLPos p = new PDLPosVar("x");

            PDLPred phi = new PDLExistsFO("x", new PDLAnd(new PDLIsSuccessor(f, p), new PDLIsSuccessor(p, l)));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            //string file = "../../../TestPDL/DotFiles/succ2";

            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void size()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPos f = new PDLFirst();
            PDLPos l = new PDLLast();
            PDLPos p = new PDLPosVar("x");

            PDLPred phi = new PDLAnd(new PDLIntEq(new PDLIndicesOf("ba"),2),new PDLEndsWith("a"));

            Console.WriteLine(phi.GetFormulaSize());
            //StringBuilder sb = new StringBuilder();

            //phi.ToMSO(new FreshGen()).ToString(sb);

            //System.Console.WriteLine(sb);

            //var dfa = phi.GetDFA(al, solver);

            ////string file = "../../../TestPDL/DotFiles/succ2";

            ////solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void mod5()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);


            PDLPred phi = new PDLModSetEq(new PDLIndicesOf("a"), 5, 3);

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^((b*ab*){5})*(b*ab*){3}$").Determinize(solver).Minimize(solver);

            //string file = "../../../TestPDL/DotFiles/mod5";

            //solver.SaveAsDot(dfa, "aut", file);


            //solver.SaveAsDot(test, "aut", file+"t");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));            

            

        }

        [TestMethod]
        public void mod2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);


            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 5, 1);

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var dfa = phi.GetDFA(al, solver);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();
            //var dfa1 = phi.GetDFA(al, solver);
            //sw1.Stop();
            //Console.WriteLine(sw1.ElapsedMilliseconds);

            var test = solver.Convert(@"^((a|b){5})*(a|b){1}$");

            //string file = "../../../TestPDL/DotFiles/mod5";

            //solver.SaveAsDot(dfa, "aut", file);


            Console.Write(phi.ToMSO().ToString());
            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void mod6()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);


            PDLPred phi = new PDLModSetEq(new PDLAllPos(), 6, 3);

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var dfa = phi.GetDFA(al, solver);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();
            //var dfa1 = phi.GetDFA(al, solver);
            //sw1.Stop();
            //Console.WriteLine(sw1.ElapsedMilliseconds);

            var test = solver.Convert(@"^((a|b){6})*(a|b){3}$").Determinize(solver).Minimize(solver);

            //string file = "../../../TestPDL/DotFiles/mod6";

            //solver.SaveAsDot(dfa, "aut", file);
            // solver.SaveAsDot(test, "aut", file+"t");


            Console.Write(phi.ToMSO().ToString());
            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void OddA()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLForallFO("p", new PDLIf(new PDLModSetEq(
                new PDLAllPosUpto(new PDLPosVar("p")), 2, 1), new PDLAtPos('a', new PDLPosVar("p"))));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(a(a|b))*a?$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            //string file = "../../../TestPDL/DotFiles/OddA";

            //solver.SaveAsDot(dfa, "aut", file);
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

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^((b(a|b)*b)|(a(a|b)*a)|a|b)?$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            //string file = "../../../TestPDL/DotFiles/SameFirstLast";

            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void abTwice()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLIndicesOf("ab"), 2);

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^b*a+b+a+b+a*$");

            //string file = "../../../TestPDL/DotFiles/abTwice";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void abcSubStr()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLNot(new PDLIntEq(new PDLIndicesOf("abc"), 0));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^[a-c]*abc[a-c]*$");

            //string file = "../../../TestPDL/DotFiles/abcSubStr";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void div2or3()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLOr(new PDLModSetEq(new PDLAllPos(), 2, 0), new PDLModSetEq(new PDLAllPos(), 3, 0));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(((a|b){2})*|((a|b){3})*)$");

            //string file = "../../../TestPDL/DotFiles/div2or3";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
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
            //new PDLAnd(new PDLatPos('a', new PDLprev(new PDLlast())), new PDLatPos('b', new PDLlast())));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(((a|b)*aa(a|b)*ab)|(a|b)*aab)$");

            //string file = "../../../TestPDL/DotFiles/contains_aa_end_ab";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
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

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(a|b)*aa(((a|b)*(aa|ba|bb))|(a*))$");

            //string file = "../../../TestPDL/DotFiles/contains_aa_notend_ab";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void once_aaa()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntEq(new PDLIndicesOf("aaa"), 1);

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^b*(ab+|aab+)*aaa(b+a|b+aa)*b*$");

            //string file = "../../../TestPDL/DotFiles/once_aaa";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void a2b1() //atleast 2 a's and atmost one b
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLAnd(new PDLNot(new PDLIntLeq(new PDLIndicesOf("a"), 1)), new PDLIntLeq(new PDLIndicesOf("b"), 1));

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);
            

            var test = solver.Convert(@"^(aa+|baa+|aba+|aa+ba*)$");

            //string file = "../../../TestPDL/DotFiles/a2b1";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void evenSpacingA() //any two a's are separated by even number of symbols
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPos px = new PDLPosVar("x");
            PDLPos py = new PDLPosVar("y");

            //TODO - is it possible to consider string as type PDLvar?
            PDLPred phi = new PDLForallFO("x", new PDLForallFO("y", new PDLIf(
                new PDLAnd(new PDLAnd(new PDLPosLe(px, py), new PDLAtPos('a', px)), new PDLAtPos('a', py)),
                    new PDLModSetEq(new PDLIntersect(new PDLAllPosAfter(px), new PDLAllPosBefore(py)), 2, 0))));

            StringBuilder sb = new StringBuilder();

            phi.ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^(b*|b*ab*|b*a(bb)*ab*)$").Determinize(solver).Minimize(solver);

            //string file = "../../../TestPDL/DotFiles/evenSpacingA";

            //solver.SaveAsDot(dfa, "aut", file);
            //solver.SaveAsDot(test, "aut", file+"t");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void checkEval()
        {
            PDLPred phi = new PDLIntEq(new PDLIndicesOf("aaa"), 1);
            System.Console.WriteLine("Exactly once aaa:");
            System.Console.WriteLine(phi.Eval("baaaabbb", new Dictionary<string, int>()));

            PDLPred phi1 = new PDLNot(new PDLIntLeq(new PDLIndicesOf("aa"), 0));
            PDLPred phi2 = (new PDLBelongs(new PDLPredecessor(new PDLLast()), new PDLIndicesOf("ab")));
            //System.Console.WriteLine("prevLast " + (new PDLprev(new PDLlast())).Eval("aaabbccab", new Dictionary<string, int>()));
            //System.Console.WriteLine("indab " + (new PDLindicesOf("ab")).Eval("aaabbccab", new Dictionary<string, int>()));


            //System.Console.WriteLine("phi2 " + phi2.Eval("aaabbccab", new Dictionary<string, int>()));

            phi = new PDLAnd(phi1, new PDLNot(phi2));
            System.Console.WriteLine("Contains aa and not end ab:");
            System.Console.WriteLine(phi.Eval("aaabbccabc", new Dictionary<string, int>()));

            phi = new PDLAnd(new PDLIf(new PDLAtPos('a', new PDLFirst()), new PDLAtPos('a', new PDLLast())),
                new PDLIf(new PDLAtPos('b', new PDLFirst()), new PDLAtPos('b', new PDLLast())));
            System.Console.WriteLine("Same First Last:");
            System.Console.WriteLine(phi.Eval("abbba", new Dictionary<string, int>()));

            phi = new PDLExistsFO("x0", new PDLAtPos('a', new PDLPosVar("x0")));
            System.Console.WriteLine("exists a:");
            System.Console.WriteLine(phi.Eval("ab", new Dictionary<string, int>()));

            phi = new PDLEndsWith("abc");
            System.Console.WriteLine("ends with abc:");
            System.Console.WriteLine(phi.Eval("bc", new Dictionary<string, int>()));

            phi = new PDLStartsWith("abc");
            System.Console.WriteLine("starts with abc:");
            System.Console.WriteLine(phi.Eval("abcababcccabc", new Dictionary<string, int>()));


        }

        [TestMethod]
        public void StartsWith()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLStartsWith("abc");

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^abc[a-c]*$");

            //string file = "../../../TestPDL/DotFiles/StartsWithAbc";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

        }

        [TestMethod]
        public void EndsWith()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLEndsWith("abc");

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^[a-c]*abc$");

            //string file = "../../../TestPDL/DotFiles/EndsWithAbc";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

        }

        [TestMethod]
        public void EmptyStr()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLEmptyString();

            StringBuilder sb = new StringBuilder();

            phi.ToMSO(new FreshGen()).ToString(sb);

            System.Console.WriteLine(sb);

            var dfa = phi.GetDFA(al, solver);

            var test = solver.Convert(@"^$");

            //string file = "../../../TestPDL/DotFiles/EmptyStr";

            //solver.SaveAsDot(dfa, "aut", file);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

        }

        [TestMethod]
        public void aAtEven()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLAtSet('a', new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(new PDLPosVar("p")), 2, 0)));

            StringBuilder sb = new StringBuilder();
            phi.ToString(sb);
            System.Console.WriteLine(sb);

            System.Console.WriteLine(phi.Eval("babababb", new Dictionary<string, int>()));

            var dfa = phi.GetDFA(al, solver);
            //string file = "../../../TestPDL/DotFiles/aAtEven";
            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void mod3inBinary()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //(number of 1's in even postion mod 3) is same as (number of 1's in odd position mod 3)
            int i;
            PDLPosVar p = new PDLPosVar("p");
            PDLPred phi = new PDLFalse();

            for(i=0;i<3;i++)
            {
                phi = new PDLOr(phi, new PDLAnd(
                    new PDLModSetEq(new PDLIntersect
                    (new PDLPredSet("p", new PDLAtPos('b', p)),
                    (new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(p), 2, 0)))), 3, i),
                    new PDLModSetEq(new PDLIntersect
                    (new PDLPredSet("p", new PDLAtPos('b', p)),
                    (new PDLPredSet("p", new PDLModSetEq(new PDLAllPosUpto(p), 2, 1)))), 3, i)));

            }

            StringBuilder sb = new StringBuilder();
            phi.ToString(sb);
            System.Console.WriteLine(sb);
            //System.Console.WriteLine(Convert.ToString(43, 2));
            //System.Console.WriteLine(phi.Eval(Convert.ToString(57, 2), new Dictionary<string, int>()));

            var dfa = phi.GetDFA(al, solver);
            ////string file = "../../../TestPDL/DotFiles/mod3inBinary";
            ////solver.SaveAsDot(dfa, "aut", file);

        }

        [TestMethod]
        public void eqString() 
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIsString("cab");

            StringBuilder sb = new StringBuilder();
            phi.ToString(sb);
            System.Console.WriteLine(sb);

            System.Console.WriteLine(phi.Eval("aac", new Dictionary<string, int>()));

            var dfa = phi.GetDFA(al, solver);
            //string file = "../../../TestPDL/DotFiles/eqString";
            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void firstOcc()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLAnd(new PDLNot(new PDLIntLeq(new PDLIndicesOf("abc"), 0)),
                new PDLIntLeq(new PDLAllPosBefore(new PDLFirstOcc("abc")), 2));

            StringBuilder sb = new StringBuilder();
            phi.ToString(sb);
            System.Console.WriteLine(sb);

            System.Console.WriteLine(phi.Eval("acabc", new Dictionary<string, int>()));

            var dfa = phi.GetDFA(al, solver);
            //string file = "../../../TestPDL/DotFiles/firstOcc";
            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void lastOcc()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPos p = new PDLLastOcc("ab");
            PDLPred phi = new PDLPosLe(new PDLLastOcc("c"), new PDLLastOcc("ab"));

            System.Console.WriteLine(phi.Eval("aababb", new Dictionary<string, int>()));

            var dfa = phi.GetDFA(al, solver);
            //string file = "../../../TestPDL/DotFiles/lastOcc";
            //solver.SaveAsDot(dfa, "aut", file);

        }

        [TestMethod]
        public void EvenAfterFirstA()
        {
            PDLPred phi = new PDLModSetEq(new PDLIntersect(new PDLIndicesOf("b"),new PDLAllPosAfter(new PDLFirstOcc("a"))), 2, 0);

            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var dfa = phi.GetDFA(al, solver);
            //string file = "../../../TestPDL/DotFiles/EvenAfterFirstA";
            //solver.SaveAsDot(dfa, "aut", file);
        }

        [TestMethod]
        public void OddAfterFirstA()
        {
            PDLPred phi = new PDLModSetEq(new PDLIntersect(new PDLIndicesOf("b"), new PDLAllPosAfter(new PDLFirstOcc("a"))), 2, 1);

            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var dfa = phi.GetDFA(al, solver);
            //string file = "../../../TestPDL/DotFiles/OddAfterFirstA";
            //solver.SaveAsDot(dfa, "aut", file);


        }

        //[TestMethod]
        //public void TestJFLAP()
        //{
        //    string fileName = "C:/Users/Dileep/Desktop/dfa.jff";
        //    var aut = DFAUtilities.parseDFAFromJFLAP(fileName, new CharSetSolver(BitWidth.BV64));
        //    var sb = new StringBuilder();
        //    DFAUtilities.printDFA(aut.Second, aut.First, sb);
        //    Console.WriteLine(sb);
        //}

        [TestMethod]
        public void DileepTest()
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            PDLPred phi = new PDLIntGeq(new PDLIndicesOf("ab"), 2);

            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var dfa = phi.GetDFA(al, solver);

            PDLPred synthPhi = null;
            StringBuilder sb = new StringBuilder();

            foreach (var phi1 in pdlEnumerator.SynthesizePDL(al, dfa, solver, sb, 10000))
            {
                synthPhi = phi1;
                break;
            }

            Console.WriteLine(sb);

        }

        [TestMethod]
        public void DileepTest1()
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            PDLPred phi = new PDLAtSet('a', new PDLPredSet("x", new PDLModSetEq(new PDLAllPosBefore(new PDLPosVar("x")), 2, 1)));

            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var dfa = phi.GetDFA(al, solver);

            //solver.SaveAsDot(dfa, "C:/Users/Dileep/Desktop/oddPos.dot");

            PDLPred synthPhi = null;
            StringBuilder sb = new StringBuilder();

            foreach (var phi1 in pdlEnumerator.SynthesizePDL(al, dfa, solver, sb, 10000))
            {
                synthPhi = phi1;
                break;
            }

            Console.WriteLine(sb);

        }
        [TestMethod]
        public void DileepTest2()
        {
            PDLPred phi = new PDLOr(new PDLIntGeq(new PDLIndicesOf("a"), 3), new PDLIntGeq(new PDLIndicesOf("b"), 3));

            var solver = new CharSetSolver(BitWidth.BV64);

            Console.WriteLine(phi.ToMSO());

            var al = new HashSet<char>(new char[]{'a','b'});

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var dfa = phi.GetDFA(al, solver);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);


            //PDLPred synthPhi = null;
            //StringBuilder sb = new StringBuilder();

            //var count = 0;
            //foreach (var phi1 in Enumeration.SynthesizePDL(al, dfa, solver, sb, 1000))
            //{
            //    synthPhi = phi1;
            //    count++;
            //    if (count > 30)
            //        break;
            //}

            //Console.WriteLine(sb);

        }

    }
}