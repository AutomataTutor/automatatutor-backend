using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

using MSOZ3;

using System.Diagnostics;

using SFAz3 = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Expr>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

namespace MSOZ3Test
{
    [TestClass]
    public class MSOTest
    {

        [TestMethod]
        public void MSOFirst()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. first(x)
            MSOFormula formula = new MSOExistsFO("x", new MSOFirst("x"));

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)+$");

            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/exfirst";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOLast()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. first(x)
            MSOFormula formula = new MSOExistsFO("x", new MSOLast("x"));

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)+$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/exlast";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOFirstLastSucc()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. first(x)
            MSOFormula first = new MSOFirst("x");
            MSOFormula last = new MSOLast("y");
            MSOFormula succ = new MSOSucc("x", "y");
            MSOFormula and = new MSOAnd(new MSOAnd(first, last), succ);
            MSOFormula formula = new MSOExistsFO("x", new MSOExistsFO("y", and));

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b){2}$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/ab";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOOr()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOExistsFO("x",
                                    new MSOAnd(
                                        new MSOOr(
                                            new MSOLabel("x", 'a'),
                                            new MSOLabel("x", 'b')
                                        ),
                                        new MSOFirst("x"))
                                 );

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)(a|b|c)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/startaorb";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOIf()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOExistsFO("x",
                                    new MSOAnd(
                                        new MSOIf(
                                            new MSOFirst("x"),
                                            new MSOLabel("x", 'b')),
                                        new MSOFirst("x")));

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^b(a|b)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/startb";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOForall()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            int a2 = 'a' * 2;
            int b2 = 'b' * 2;
            List<char> alph2 = new List<char> { (char)a2, (char)b2, (char)(a2+1), (char)(b2+1) };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOForallFO("x",
                                    new MSOLabel("x", 'b'));

            Assert.IsTrue(formula.CheckUseOfVars());         

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^b*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/bstar";
            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing            
        }

        [TestMethod]
        public void MSOEqual()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            MSOFormula formula = new MSOExistsFO("x",
                                    new MSOForallFO("y",
                                        new MSOEqual("x", "y")));

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);
            var test = solver.Convert(@"^(a|b)$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/lengthone";
            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOLe()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOExistsFO("x",
                                    new MSOAnd(
                                        new MSOLabel("x", 'a'),
                                        new MSOForallFO("y",
                                            new MSOOr(
                                                new MSOEqual("x", "y"),
                                                new MSOLess("y", "x")))));

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)*a$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/endsina";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSO2a2b()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            MSOFormula formula1 = new MSOExistsFO("x1",
                                    new MSOExistsFO("x2",
                                        new MSOAnd(
                                            new MSOAnd(
                                                new MSOLabel("x1", 'a'),
                                                new MSOLabel("x2", 'a')),
                                            new MSONot(
                                                new MSOEqual("x1", "x2")))));
            MSOFormula formula2 = new MSOExistsFO("x1",
                                    new MSOExistsFO("x2",
                                        new MSOAnd(
                                            new MSOAnd(
                                                new MSOLabel("x1", 'b'),
                                                new MSOLabel("x2", 'b')),
                                            new MSONot(
                                                new MSOEqual("x1", "x2")))));

            MSOFormula formula = new MSOAnd(formula1, formula2);


            Assert.IsTrue(formula.CheckUseOfVars());

            var WS1S = formula.ToWS1S(solver);
            var dfa = WS1S.getDFA(al, solver);
            var timer = new Stopwatch();
            var tt = 100;
            var acc = 0L;
            for (int k = 0; k < tt; k++)
            {
                timer.Reset();
                timer.Start();
                dfa = WS1S.getDFA(al, solver);
                timer.Stop();
                acc += timer.ElapsedMilliseconds;
            }
            Console.WriteLine("time: " + acc / tt + " ms");

            //var test = solver.Convert(@"^(a|b)$");

            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/a2b2";
            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }



        [TestMethod]
        public void MSOaafterb()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            MSOFormula formula = new MSOForallFO("x1",
                                    new MSOIf(
                                        new MSOLabel("x1", 'b'),
                                        new MSOExistsFO("x2",
                                            new MSOAnd(
                                                new MSOLess("x1", "x2"),
                                                new MSOLabel("x2", 'a')))));



            Assert.IsTrue(formula.CheckUseOfVars());

            var WS1S = formula.ToWS1S(solver);
            var dfa = WS1S.getDFA(al, solver);
            var timer = new Stopwatch();
            var tt = 100;
            var acc = 0L;
            for (int k = 0; k < tt; k++)
            {
                timer.Reset();
                timer.Start();
                dfa = WS1S.getDFA(al, solver);
                timer.Stop();
                acc += timer.ElapsedMilliseconds;
            }
            Console.WriteLine("time: " + acc / tt + " ms");

            //var test = solver.Convert(@"^(a|b)$");

            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/aafterb";
            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }



    }
}