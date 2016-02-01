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

namespace PDLTest
{
    [TestClass]
    public class BenchmarkTest
    {

        [TestMethod]
        public void NElements()
        {
            var solver = new CharSetSolver(CharacterEncoding.Integer);  //new solver using ASCII encoding

            //ex1 p1,p2,p3,p4,p5:
            //    p1<p2 & p2<p3 & p3<p4 & p4<p5 &
            //    A = {p1,p2,p3,p4,p5};

            List<char> alph = new List<char> {};
            HashSet<char> al = new HashSet<char>(alph);

            Stopwatch timer = new Stopwatch();
            int nestedquant = 13;
            int test_length = nestedquant+1;
            for (int i = 2; i < test_length; i++)
            {
                MSOFormula formula = new MSOLess("p1", "p2", solver);
                for (int j = 2; j < i; j++)
                {
                    formula = new MSOAnd(formula, new MSOLess("p" + j, "p" + (j + 1), solver), solver);
                }
                for (int j = 1; j <= i; j++)
                {
                    formula = new MSOExists("p" + j, formula, solver);
                }
                var WS1S = formula.ToWS1S();

                Assert.IsTrue(formula.CheckUseOfVars());
                if (i > 0)
                {
                    var dfa = WS1S.getDFA(al);
                    var acc = 0L;
                    int tt = 25;
                    for (int k = 0; k < tt; k++)
                    {
                        timer.Reset();
                        timer.Start();
                        dfa = WS1S.getDFA(al);
                        timer.Stop();
                        acc += timer.ElapsedMilliseconds;
                    }
                    Console.WriteLine(i + " vars: " + acc / tt + " ms");
                    //var test = solver.Convert(@"^(a){" + i + @",}$");

                    //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

                    //string file = "../../../PDLTest/DotFiles/el" + i;
                    //solver.SaveAsDot(dfa, file);
                }

                
            }

            //ex x. first(x)

            //var test = solver.Convert(@"^(a|b){" + 10 + @",}$");
            //string file = "../../../PDLTest/DotFiles/fiveel";

            //solver.SaveAsDot(test, file);   //extension .dot  is added automatically when missing
        }


    }
}
