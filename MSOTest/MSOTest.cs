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

using SFAz3 = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Term, Microsoft.Z3.Sort>;
using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Term, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Term>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Term, Microsoft.Z3.Sort>;

namespace MSOZ3Test
{
    [TestClass]
    public class MSOTest
    {

        [TestMethod]
        public void MSOFirstDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. first(x)
            MSOFormula formula = new MSOExists("x", new MSOFirst("x", solver),solver);

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^(a|b)+$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/exfirst";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOLastDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. first(x)
            MSOFormula formula = new MSOExists("x", new MSOLast("x", solver), solver);

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^(a|b)+$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/exlast";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOFirstLastSuccDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. first(x)
            MSOFormula first = new MSOFirst("x", solver);
            MSOFormula last = new MSOLast("y", solver);
            MSOFormula succ = new MSOSucc("x", "y", solver);
            MSOFormula and = new MSOAnd(new MSOAnd(first, last, solver),succ,solver);
            MSOFormula formula = new MSOExists("x", new MSOExists("y", and, solver), solver);

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^(a|b){2}$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/ab";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOOrDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b', 'c' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOExists("x",
                                    new MSOAnd(
                                        new MSOOr(
                                            new MSOLabel("x", 'a', solver),
                                            new MSOLabel("x", 'b', solver),
                                            solver
                                        ),
                                        new MSOFirst("x",
                                            solver
                                        ),
                                        solver
                                    ),
                                    solver
                                 );

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^(a|b)(a|b|c)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/startaorb";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOIfDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOExists("x",
                                    new MSOAnd(
                                        new MSOIf(
                                            new MSOFirst("x", solver),
                                            new MSOLabel("x", 'b', solver),
                                            solver
                                        ),
                                        new MSOFirst("x", solver),
                                        solver
                                    ),
                                    solver
                                 );

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^b(a|b)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/startb";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOForallDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOForall("x",
                                    new MSOLabel("x", 'b', solver),
                                            solver);

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^b*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/bstar";
            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOEqualDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);
            
            MSOFormula formula = new MSOExistsFO("x",
                                    new MSOForallFO("y",
                                        new MSOEqual("x", "y", solver),
                                        solver
                                    ),
                                    solver);
            
            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);
            var test = solver.Convert(@"^(a|b)$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/lengthone";
            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSOLeDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            //ex x. all y. x<=y and a(x)
            MSOFormula formula = new MSOExistsFO("x",
                                    new MSOAnd(                                         
                                        new MSOLabel("x", 'a', solver), 
                                        new MSOForallFO("y",
                                            new MSOOr(
                                                new MSOEqual("x","y", solver),                                             
                                                new MSOLess("y","x", solver),                                             
                                                solver
                                            ),
                                            solver
                                        ),
                                        solver
                                    ),
                                    solver
                                 );

            Assert.IsTrue(formula.CheckUseOfVars());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^(a|b)*a$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/endsina";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void MSO2a2bDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            MSOFormula formula1 = new MSOExistsFO("x1",
                                    new MSOExistsFO("x2",
                                        new MSOAnd(
                                            new MSOAnd(
                                                new MSOLabel("x1", 'a',solver),
                                                new MSOLabel("x2", 'a', solver),
                                                solver),
                                            new MSONot(
                                                new MSOEqual("x1","x2",solver),
                                                solver
                                                ),
                                            solver
                                        ), solver
                                    ), solver);
            MSOFormula formula2 = new MSOExistsFO("x1",
                                    new MSOExistsFO("x2",
                                        new MSOAnd(
                                            new MSOAnd(
                                                new MSOLabel("x1", 'b', solver),
                                                new MSOLabel("x2", 'b', solver),
                                                solver),
                                            new MSONot(
                                                new MSOEqual("x1", "x2", solver),
                                                solver
                                                ),
                                            solver
                                        ), solver
                                    ), solver);

            MSOFormula formula = new MSOAnd(formula1, formula2, solver);


            Assert.IsTrue(formula.CheckUseOfVars());

            var WS1S = formula.ToWS1S();
            var dfa = WS1S.getDFA(al);
            var timer = new Stopwatch();
            var tt = 100;
            var acc = 0L;
            for (int k = 0; k < tt; k++)
            {
                timer.Reset();
                timer.Start();
                dfa = WS1S.getDFA(al);
                timer.Stop();
                acc += timer.ElapsedMilliseconds;
            }
            Console.WriteLine("time: " + acc / tt + " ms");
            
            //var test = solver.Convert(@"^(a|b)$");

            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/a2b2";
            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }



        [TestMethod]
        public void MSOaafterbDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            MSOFormula formula = new MSOForallFO("x1",
                                    new MSOIf(
                                        new MSOLabel("x1",'b',solver),
                                        new MSOExistsFO("x2",
                                            new MSOAnd(
                                                new MSOLess("x1","x2",solver),
                                                new MSOLabel("x2", 'a', solver),
                                                solver
                                            ), solver
                                        ),solver
                                    ), solver);
            


            Assert.IsTrue(formula.CheckUseOfVars());

            var WS1S = formula.ToWS1S();
            var dfa = WS1S.getDFA(al);
            var timer = new Stopwatch();
            var tt = 100;
            var acc = 0L;
            for (int k = 0; k < tt; k++)
            {
                timer.Reset();
                timer.Start();
                dfa = WS1S.getDFA(al);
                timer.Stop();
                acc += timer.ElapsedMilliseconds;
            }
            Console.WriteLine("time: " + acc / tt + " ms");

            //var test = solver.Convert(@"^(a|b)$");

            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/aafterb";
            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }



    }
}
