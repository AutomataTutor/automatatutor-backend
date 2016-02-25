using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;

using Microsoft.Z3;

using MSOZ3;

using SFAz3 = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Expr>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

namespace MSOZ3Test
{
    [TestClass]
    public class WS1STest
    {
        [TestMethod]
        public void WS1SLabelTest()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f = new WS1SUnaryPred("X", solver.MkCharConstraint(false, 'a'));
            WS1SFormula phi = new WS1SExists("X", f);

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)*$").Determinize(solver).Minimize(solver);            

            string file = "../../../MSOZ3Test/DotFiles/sigmastar";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing            

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void WS1SSingleton()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SSingleton("X");
            WS1SFormula phi = new WS1SExists("X", f1);

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)+$").Determinize(solver).Minimize(solver);

            string file = "../../../MSOZ3Test/DotFiles/singletona";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }


        [TestMethod]
        public void WS1SSucc()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f3 = new WS1SSucc("X", "Y");

            WS1SFormula phi = new WS1SExists("X", new WS1SExists("Y", f3));

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b){2,}$");

            string file = "../../../MSOZ3Test/DotFiles/containsab";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void WS1STrue()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula phi = new WS1SExists("X", new WS1STrue());

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)*$");

            string file = "../../../MSOZ3Test/DotFiles/true";

            solver.SaveAsDot(dfa, "true", file);   //extension .dot  is added automatically when missing

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));            
        }

        [TestMethod]
        public void WS1SNot()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SUnaryPred("X", solver.MkCharConstraint(false, 'a'));
            WS1SFormula f2 = new WS1SSingleton("X");
            WS1SFormula f = new WS1SAnd(f1, f2);

            WS1SFormula phi = new WS1SNot(new WS1SExists("X", f));

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^b*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/nota";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void WS1SSubset()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SUnaryPred("X", solver.MkCharConstraint(false, 'a'));
            WS1SFormula f2 = new WS1SUnaryPred("Y1", solver.MkCharConstraint(false, 'b'));
            WS1SFormula f3 = new WS1SUnaryPred("Z", solver.MkCharConstraint(false, 'a'));
            WS1SFormula f = new WS1SAnd(f1, new WS1SAnd(f2, f3));

            WS1SFormula s1 = new WS1SSucc("X", "Y1");
            WS1SFormula s2 = new WS1SSucc("Y2", "Z");
            WS1SFormula s3 = new WS1SSubset("Y1", "Y2");
            WS1SFormula s = new WS1SAnd(new WS1SAnd(s1, s2), s3);

            WS1SFormula phi = new WS1SExists("X",
                                new WS1SExists("Y1",
                                    new WS1SExists("Y2",
                                        new WS1SExists("Z",
                                            new WS1SAnd(f, s)))));

            WS1SFormula phit = new WS1SExists("X",
                                       new WS1SExists("Y",
                                           new WS1SSubset("X", "Y")));
            var dd = phit.getDFA(al, solver);

            //solver.SaveAsDot(phit.getDFA(al, solver), "bla","bla.dot");

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)*aba(a|b)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../MSOZ3Test/DotFiles/aba";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void WS1SFormula()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SUnaryPred("X", solver.MkCharConstraint(false, 'a'));
            WS1SFormula f2 = new WS1SUnaryPred("Y", solver.MkCharConstraint(false, 'b'));
            WS1SFormula f3 = new WS1SSucc("X", "Y");
            WS1SFormula phi = new WS1SAnd(new WS1SAnd(f1, f2), f3);

            WS1SFormula psi = new WS1SSucc("Y", "Z");

            WS1SFormula formula = new WS1SExists("X", new WS1SExists("Y",
                new WS1SAnd(phi, new WS1SNot(new WS1SExists("Z", psi)))));

            StringBuilder sb = new StringBuilder();
            formula.ToString(sb);
            Console.WriteLine(sb.ToString());

            var dfa = formula.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)*ab$");

            string file = "../../../MSOZ3Test/DotFiles/endsinab";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void WS1SLessEq()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula phi = new WS1SExists("X", new WS1SExists("Y", new WS1SLessOrEqual("X","Y")));

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b)+$");            

            string file = "../../../MSOZ3Test/DotFiles/leq";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }

        [TestMethod]
        public void WS1SLess()
        {
            var solver = new CharSetSolver(BitWidth.BV64);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula phi = new WS1SExists("X", new WS1SExists("Y", new WS1SLess("X", "Y")));

            var dfa = phi.getDFA(al, solver);

            var test = solver.Convert(@"^(a|b){2,}$");

            string file = "../../../MSOZ3Test/DotFiles/less";

            solver.SaveAsDot(dfa, "aut", file);   //extension .dot  is added automatically when missing

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));
        }


    }
}
