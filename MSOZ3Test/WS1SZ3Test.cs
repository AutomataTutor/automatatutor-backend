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
    public class WS1SZ3Test
    {

        //[TestMethod]
        //public void WS1SNot()
        //{
        //    var z3p = new Z3Provider();

        //    //Sort for pairs (input theory, BV)
        //    var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
        //    var isort = z3p.Z3.MkRealSort();
        //    var pairSort = z3p.MkTupleSort(isort, bv);

        //    var v = z3p.MkVar(0, pairSort);
        //    var v1 = z3p.MkProj(0,v);
        //    var pred = z3p.MkGt(v1, z3p.Z3.MkReal("99/100"));
        //    var univ = z3p.MkLt(v1, z3p.Z3.MkReal("6.0"));

        //    //var funcDec = z3p.MkFreshFuncDecl("F", new Sort[] { pairSort }, z3p.BoolSort);
        //    //z3p.MkEq(z3p.MkApp(funcDec, v), pred);
            

        //    //var univ = Automaton<Expr>.Create(0, new int[] { 1 }, new Move<Expr>[] { new Move<Expr>(0, 1, pred) });

        //    MSOZ3Formula f1 = new MSOZ3ExistsFO("x",
        //                        new MSOZ3Predicate("x", pred)
        //                );

        //    var sfa = f1.getAutomata(z3p, univ, v, isort);                        

        //    if (!sfa.IsEmpty)
        //    {
        //        var path = sfa.ChoosePathToSomeFinalState(new Chooser());
        //        foreach (var el in path)
        //        {
        //            //Console.WriteLine(z3p.IsSatisfiable(el));
        //            //foreach (var af in z3p.GetVars(el))
        //            //{

        //            //}

        //            Console.WriteLine(z3p.Simplify(z3p.MkProj(0, z3p.FindOneMember(el).Value)));

        //            //var pp = z3p.GetModel(el);
        //            //foreach(var pp1 in pp){
        //            //Console.WriteLine(pp1
        //            //}
        //            //.First().Value);
        //        }
        //    }
        //    else
        //    {
        //        Console.Write("Formula unsat");
        //    }

        //    z3p.Dispose();
        //}

        //[TestMethod]
        //public void NElements()
        //{
        //    var z3p = new Z3Provider();

        //    //Sort for pairs (input theory, BV)
        //    var bv = z3p.Z3.MkBitVecSort(BVConst.BVSIZE);
        //    var sort = z3p.Z3.MkBoolSort();
        //    var pairSort = z3p.MkTupleSort(sort, bv);

        //    var v = z3p.MkVar(0, pairSort);            
        //    var univ = z3p.True;

        //    //ex1 p1,p2,p3,p4,p5:
        //    //    p1<p2 & p2<p3 & p3<p4 & p4<p5 &
        //    //    A = {p1,p2,p3,p4,p5};

        //    List<char> alph = new List<char> { };
        //    HashSet<char> al = new HashSet<char>(alph);

        //    Stopwatch timer = new Stopwatch();
        //    int nestedquant = 3;
        //    int test_length = nestedquant + 1;
        //    for (int i = 2; i < test_length; i++)
        //    {
        //        MSOZ3Formula formula = new MSOZ3Less("p1", "p2");
        //        for (int j = 2; j < i; j++)
        //        {
        //            formula = new MSOZ3And(formula, new MSOZ3Less("p" + j, "p" + (j + 1)));
        //        }
        //        for (int j = 1; j <= i; j++)
        //        {
        //            formula = new MSOZ3Exists("p" + j, formula);
        //        }
        //        var WS1S = formula.ToWS1SZ3();

        //        Assert.IsTrue(formula.CheckUseOfVars());
        //        if (i > 0)
        //        {
        //            var dfa = WS1S.getAutomata(z3p, univ, v, sort);
        //            var acc = 0L;
        //            int tt = 5;
        //            for (int k = 0; k < tt; k++)
        //            {
        //                timer.Reset();
        //                timer.Start();
        //                dfa = WS1S.getAutomata(z3p, univ, v, sort);
        //                timer.Stop();
        //                acc += timer.ElapsedMilliseconds;
        //            }
        //            Console.WriteLine(i + " vars: " + acc / tt + " ms");
        //            //var test = solver.Convert(@"^(a){" + i + @",}$");

        //            //Assert.IsTrue(dfa.IsEquivalentWith(test, solver));

        //            //string file = "../../../PDLTest/DotFiles/el" + i;
        //            //solver.SaveAsDot(dfa, "aut", file);
        //        }


        //    }

        //    //ex x. first(x)

        //    //var test = solver.Convert(@"^(a|b){" + 10 + @",}$");
        //    //string file = "../../../PDLTest/DotFiles/fiveel";

        //    //solver.SaveAsDot(test, file);   //extension .dot  is added automatically when missing
        //}
    }
}
