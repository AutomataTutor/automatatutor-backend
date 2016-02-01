using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

using MSOZ3;

using SFAz3 = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Term, Microsoft.Z3.Sort>;
using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Term, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Term>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Term, Microsoft.Z3.Sort>;

namespace PDLTest
{
    [TestClass]
    public class WS1STest
    {
        [TestMethod]
        public void WS1SLabelDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f = new WS1SLabel("X", 'a', solver);
            WS1SFormula phi = new WS1SExists("X", f, solver);

            var dfa = phi.getDFA(al);

            var test = solver.Convert(@"^(a|b)*$").Determinize(solver).Minimize(solver);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/sigmastar";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing            
        }

        [TestMethod]
        public void WS1SSingletonDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SLabel("X", 'a', solver);
            WS1SFormula f2 = new WS1SSingleton("X", solver);
            WS1SFormula phi = new WS1SExists("X", new WS1SAnd(f1,f2,solver), solver);

            var dfa = phi.getDFA(al);

            var test = solver.Convert(@"^(a|b)*a(a|b)*$").Determinize(solver).Minimize(solver);

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/singletona";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }


        [TestMethod]
        public void WS1SSuccDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding
            
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SLabel("X", 'a', solver);
            WS1SFormula f2 = new WS1SLabel("Y", 'b', solver);
            WS1SFormula f3 = new WS1SSucc("X", "Y", solver);
            WS1SFormula f = new WS1SAnd(new WS1SAnd(f1, f2, solver), f3, solver);

            WS1SFormula phi = new WS1SExists("X", new WS1SExists("Y", f, solver), solver);

            var dfa = phi.getDFA(al);
            
            var test = solver.Convert(@"^(a|b)*ab(a|b)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/containsab";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void WS1SNotDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);            

            WS1SFormula f1 = new WS1SLabel("X", 'a', solver);
            WS1SFormula f2 = new WS1SSingleton("X", solver);
            WS1SFormula f = new WS1SAnd(f1, f2, solver);

            WS1SFormula phi = new WS1SNot(new WS1SExists("X", f,solver),solver);

            var dfa = phi.getDFA(al);

            var test = solver.Convert(@"^b*$");

            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/nota";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void WS1SSubsetDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SLabel("X", 'a', solver);           
            WS1SFormula f2 = new WS1SLabel("Y1", 'b', solver);
            WS1SFormula f3 = new WS1SLabel("Z", 'a', solver);
            WS1SFormula f = new WS1SAnd(f1, new WS1SAnd(f2,f3,solver), solver);

            WS1SFormula s1 = new WS1SSucc("X","Y1", solver);
            WS1SFormula s2 = new WS1SSucc("Y2", "Z", solver);
            WS1SFormula s3 = new WS1SSubset("Y1", "Y2", solver);
            WS1SFormula s = new WS1SAnd(new WS1SAnd(s1,s2,solver),s3,solver);

            WS1SFormula phi = new WS1SExists("X",
                                new WS1SExists("Y1", 
                                    new WS1SExists("Y2", 
                                        new WS1SExists("Z",
                                            new WS1SAnd(f, s, solver), solver), solver), solver), solver);

            var dfa = phi.getDFA(al);

            var test = solver.Convert(@"^(a|b)*aba(a|b)*$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/aba";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }

        [TestMethod]
        public void WS1SFormulaDFA()
        {
            var solver = new CharSetSolver(CharacterEncoding.BV32);  //new solver using ASCII encoding

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            WS1SFormula f1 = new WS1SLabel("X", 'a', solver);
            WS1SFormula f2 = new WS1SLabel("Y", 'b', solver);
            WS1SFormula f3 = new WS1SSucc("X", "Y", solver);
            WS1SFormula phi = new WS1SAnd(new WS1SAnd(f1, f2, solver), f3, solver);

            WS1SFormula psi = new WS1SSucc("Y", "Z", solver);

            WS1SFormula formula = new WS1SExists("X", new WS1SExists("Y", 
                new WS1SAnd(
                    phi,
                    new WS1SNot(
                        new WS1SExists("Z", psi, solver),
                        solver),
                    solver),
                solver),solver);

            StringBuilder sb = new StringBuilder();
            formula.ToString(sb);
            Console.WriteLine(sb.ToString());

            var dfa = formula.getDFA(al);

            var test = solver.Convert(@"^(a|b)*ab$");

            Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

            string file = "../../../PDLTest/DotFiles/endsinab";

            solver.SaveAsDot(dfa, file);   //extension .dot  is added automatically when missing
        }


    }
}
