using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

using LTLMSO;
using Microsoft.Automata;
using MSOZ3;

namespace LTLMSOTest
{    

    [TestClass]
    public class LTLTest
    {
        public static int maxc = 1;

        [TestMethod]
        public void TestTrue()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var tru = new LTLTrue();

            var dfa = tru.getDFA(atoms, solver);
            solver.SaveAsDot(dfa, "tru", @"C:\temp\tru");

            var comp = new LTLNot(tru);
            Assert.IsTrue(comp.getDFA(atoms, solver).IsEmpty);
        }

        [TestMethod]
        public void TestFalse()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var fls = new LTLFalse();

            var dfa = fls.getDFA(atoms, solver);
            solver.SaveAsDot(dfa, "fls", @"C:\temp\fls");

            Assert.IsTrue(dfa.IsEmpty);
        }

        [TestMethod]
        public void TestNext()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[]{"a","b","c"});
            var next = new LTLNext(new LTLPred("a",atoms,solver));

            var dfa = next.getDFA(atoms, solver);
            solver.SaveAsDot(dfa,"next",@"C:\temp\next");            
        }

        [TestMethod]
        public void TestUntil()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var until = new LTLUntil(new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)), new LTLPred("c", atoms, solver));

            var dfa = until.getDFA(atoms, solver);
            Console.WriteLine(until.ToMSO().ToString());

            solver.SaveAsDot(dfa, "until", @"C:\temp\until");
        }

        [TestMethod]
        public void TestEventually()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var eventually = new LTLEventually(new LTLPred("a", atoms, solver));

            var dfa = eventually.getDFA(atoms, solver);
            Console.WriteLine(eventually.ToMSO().ToString());

            solver.SaveAsDot(dfa, "eventually", @"C:\temp\eventually");
        }

        [TestMethod]
        public void TestAlways()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var always = new LTLGlobally(new LTLPred("a", atoms, solver));

            var dfa = always.getDFA(atoms, solver);
            Console.WriteLine(always.ToMSO().ToString());

            solver.SaveAsDot(dfa, "always", @"C:\temp\always");
        }

        [TestMethod]
        public void TestEqualities()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });

            //Distributivity
            //Next
            var nextaorb = new LTLNext(new LTLOr(new LTLPred("a", atoms, solver),new LTLPred("b", atoms, solver)));
            var nextaornextb = new LTLOr(new LTLNext(new LTLPred("a", atoms, solver)), new LTLNext(new LTLPred("b", atoms, solver)));
            Assert.IsTrue(nextaorb.IsEquivalentWith(nextaornextb, atoms, solver));

            var nextaandb = new LTLNext(new LTLAnd(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)));
            var nextaandnextb = new LTLAnd(new LTLNext(new LTLPred("a", atoms, solver)), new LTLNext(new LTLPred("b", atoms, solver)));
            Assert.IsTrue(nextaandb.IsEquivalentWith(nextaandnextb, atoms, solver));

            var nextauntilb = new LTLNext(new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)));
            var nextauntilnextb = new LTLUntil(new LTLNext(new LTLPred("a", atoms, solver)), new LTLNext(new LTLPred("b", atoms, solver)));
            Assert.IsTrue(nextauntilb.IsEquivalentWith(nextauntilnextb, atoms, solver));

            //Eventually
            var eventuallyaorb = new LTLEventually(new LTLOr(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)));
            var eventuallyaoreventuallyb = new LTLOr(new LTLEventually(new LTLPred("a", atoms, solver)), new LTLEventually(new LTLPred("b", atoms, solver)));
            Assert.IsTrue(eventuallyaorb.IsEquivalentWith(eventuallyaoreventuallyb, atoms, solver));

            //Always
            var alwaysaandb = new LTLGlobally(new LTLAnd(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)));
            var alwaysaandalwaysb = new LTLAnd(new LTLGlobally(new LTLPred("a", atoms, solver)), new LTLGlobally(new LTLPred("b", atoms, solver)));
            Assert.IsTrue(alwaysaandb.IsEquivalentWith(alwaysaandalwaysb, atoms, solver));

            //Until
            var auntilborc = new LTLUntil(new LTLPred("a", atoms, solver), new LTLOr(new LTLPred("b", atoms, solver), new LTLPred("c", atoms, solver)));
            var auntilborauntilc = new LTLOr(new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)), new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("c", atoms, solver)));
            Assert.IsTrue(auntilborc.IsEquivalentWith(auntilborauntilc, atoms, solver));

            var aandbuntilc = new LTLUntil(new LTLAnd(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)),  new LTLPred("c", atoms, solver));
            var auntilcandrbuntilc = new LTLAnd(new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("c", atoms, solver)), new LTLUntil(new LTLPred("b", atoms, solver), new LTLPred("c", atoms, solver)));
            Assert.IsTrue(aandbuntilc.IsEquivalentWith(auntilcandrbuntilc, atoms, solver));

            //Negation Propagation
            //Next (this fails in finite traces)
            var notnexta = new LTLNot(new LTLNext(new LTLPred("a", atoms, solver)));
            var nextnota = new LTLNext(new LTLNot(new LTLPred("a", atoms, solver)));
            Assert.IsFalse(notnexta.IsEquivalentWith(nextnota, atoms, solver));

            //Always
            var notalwaysa = new LTLNot(new LTLGlobally(new LTLPred("a", atoms, solver)));
            var eventuallynota = new LTLEventually(new LTLNot(new LTLPred("a", atoms, solver)));
            Assert.IsTrue(notalwaysa.IsEquivalentWith(eventuallynota, atoms, solver));

            //Eventually
            var noteventuallysa = new LTLNot(new LTLEventually(new LTLPred("a", atoms, solver)));
            var alwaysnota = new LTLGlobally(new LTLNot(new LTLPred("a", atoms, solver)));
            Assert.IsTrue(noteventuallysa.IsEquivalentWith(alwaysnota, atoms, solver));

            //Special
            //Eventually
            var eventuallya = new LTLEventually(new LTLPred("a", atoms, solver));
            var eventuallyeventuallya = new LTLEventually(new LTLEventually(new LTLPred("a", atoms, solver)));            
            Assert.IsTrue(eventuallya.IsEquivalentWith(eventuallyeventuallya, atoms, solver));

            var aornexteventuallya = new LTLOr(new LTLPred("a", atoms, solver), new LTLNext(new LTLEventually(new LTLPred("a", atoms, solver))));
            Assert.IsTrue(eventuallya.IsEquivalentWith(aornexteventuallya, atoms, solver));

            //Always
            var alwaysa = new LTLGlobally(new LTLPred("a", atoms, solver));
            var alwaysalwaysa = new LTLGlobally(new LTLGlobally(new LTLPred("a", atoms, solver)));
            Assert.IsTrue(alwaysa.IsEquivalentWith(alwaysalwaysa, atoms, solver));

            //this is false in finite traces
            var aandnextalwaysa = new LTLAnd(new LTLPred("a", atoms, solver), new LTLNext(new LTLGlobally(new LTLPred("a", atoms, solver))));
            Assert.IsFalse(alwaysa.IsEquivalentWith(aandnextalwaysa, atoms, solver));

            //Until
            var auntilb = new LTLUntil(new LTLPred("a", atoms, solver),new LTLPred("b", atoms, solver));
            var auntilauntilb = new LTLUntil(new LTLPred("a", atoms, solver), new LTLUntil(new LTLPred("a", atoms, solver),new LTLPred("b", atoms, solver)));
            Assert.IsTrue(auntilb.IsEquivalentWith(auntilauntilb, atoms, solver));

            var boraandnextauntilb = 
                new LTLOr(
                    new LTLPred("b", atoms, solver),
                    new LTLAnd(
                        new LTLPred("a", atoms, solver),
                        new LTLNext(new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver)))
                    )
                );
            Assert.IsTrue(auntilb.IsEquivalentWith(boraandnextauntilb, atoms, solver));
        }

        [TestMethod]
        public void TestUntilMonaGen()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var until = 
                new LTLAnd(
                        new LTLNot(new LTLPred("b", atoms, solver)),
                        new LTLAnd(
                            new LTLNext(new LTLNot(new LTLPred("b", atoms, solver))),
                            new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver))
                        )                   
               );

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:/cygwin/home/lorisdan/Mona/until.mona");
            file.WriteLine(until.ToMSO(true).ToMonaString(atoms, solver));
            file.Close();
            
        }

        [TestMethod]
        public void TestUntilSpot()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });                        
            var until =
                new LTLAnd(
                        new LTLNot(new LTLPred("b", atoms, solver)),
                        new LTLAnd(
                            new LTLNext(new LTLNot(new LTLPred("b", atoms, solver))),
                            new LTLUntil(new LTLPred("a", atoms, solver), new LTLPred("b", atoms, solver))
                        )
               );

            Console.WriteLine(until.ToString(true));
            var dfa = until.getDFA(atoms, solver);
            solver.SaveAsDot(dfa, "untilspot", @"C:\temp\untilspot");
        }

        [TestMethod]
        public void TestAlwaysMonaGen()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var atoms = new List<string>(new string[] { "a", "b", "c" });
            var always = new LTLGlobally(new LTLPred("a", atoms, solver));

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:/cygwin/home/lorisdan/Mona/always.mona");
            file.WriteLine(always.ToMSO(true).ToMonaString(atoms, solver));
            file.Close();

        }

        [TestMethod]
        public void TestSonali()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            var ala = new List<string>(new string[] { "a"});
            var alab = new List<string>(new string[] { "a", "b"});
            var alabc = new List<string>(new string[] { "a", "b", "c"});
            var al = new List<string>(new string[] { "a", "b", "c" , "d"});

            var path = @"C:\temp\sonali\";
            var a = new LTLPred("a", al, solver);
            var b = new LTLPred("b", al, solver);
            var c = new LTLPred("c", al, solver);
            var d = new LTLPred("d", al, solver);
            var fa =new LTLEventually(a);
            var fb =new LTLEventually(b);
            var fc =new LTLEventually(c);
            var ga =new LTLGlobally(a);
            var gb =new LTLGlobally(b);
            var gc =new LTLGlobally(c);
            var gd =new LTLGlobally(d);
            var aub = new LTLUntil(a, b);
            var na = new LTLNext(a);
            var nb = new LTLNext(b);

            //Console.WriteLine(new LTLUntil(aub,c).ToMSO());

            List<Pair<LTLFormula,List<string>>> formulas = new List<Pair<LTLFormula,List<string>>>();

            formulas.Add(new Pair<LTLFormula,List<string>>(fa, ala));
            formulas.Add(new Pair<LTLFormula, List<string>>(ga, ala));
            formulas.Add(new Pair<LTLFormula, List<string>>(aub, alab));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLGlobally(fa), ala));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLEventually(ga), ala));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(
                            a,
                            new LTLOr(
                                new LTLOr(
                                    b,
                                    nb
                                ),
                                new LTLNext(nb)
                            )
                            )
                         , alab));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(
                            a,
                            new LTLOr(
                                b,
                                new LTLNext(new LTLOr(b, na))
                            )
                            )
                         , alab));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLUntil(aub, c), alabc));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLAnd(new LTLAnd(
                    new LTLOr(fa, gb),
                    new LTLOr(fb, gc)
                    ),
                    new LTLOr(fc, gd)
                    ), al));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLUntil(a, new LTLUntil(b, c)), alabc));

            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLOr(new LTLNext(ga), new LTLNext(fb)), alab));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(new LTLUntil(b, c), aub), alabc));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLAnd(new LTLAnd(na, new LTLNext(na)), new LTLOr(b, c)), alabc));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLUntil(na, gb), alab));
            formulas.Add(new Pair<LTLFormula, List<string>>(new LTLOr(new LTLAnd(fa, gb), new LTLAnd(fb, gc)), alabc));

            var i=1;
            foreach(var f in formulas){
                solver.SaveAsDot(f.First.getDFA(f.Second, solver), "tru", path + i.ToString());
                i++;
            }
        }

        [TestMethod]
        public void TestScalingLU()
        {
            var solver = new CharSetSolver(BitWidth.BV64);


            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 7;
            int maxk = 7;

            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i);
                LTLFormula f = new LTLPred("1", al, solver);
                for (int j = 2; j <= i; j++)
                    f = new LTLUntil(f, new LTLPred(j.ToString(), al, solver));
                formulas.Add(new Pair<LTLFormula, List<string>>(f, Alph(i)));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\uniontest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingRU()
        {
            var solver = new CharSetSolver(BitWidth.BV64);


            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 10;

            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i);
                LTLFormula f = new LTLPred("1", al, solver);
                for (int j = 2; j <= i; j++)
                    f = new LTLUntil(new LTLPred(j.ToString(), al, solver),f);
                formulas.Add(new Pair<LTLFormula, List<string>>(f, Alph(i)));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\uniontest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingFG()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 15;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i+1);
                LTLFormula f = new LTLOr(new LTLEventually(new LTLPred("1", al, solver)),new LTLGlobally(new LTLPred("2", al, solver)));
                for (int j = 2; j <= i; j++)
                {
                    var conj = new LTLOr(new LTLEventually(new LTLPred(i.ToString(), al, solver)), new LTLGlobally(new LTLPred((i+1).ToString(), al, solver)));
                    f = new LTLAnd(f, conj);
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(f, al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\fgtest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingGFOr()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 15;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i + 1);
                LTLFormula f = new LTLGlobally(new LTLEventually(new LTLPred("1", al, solver)));
                for (int j = 2; j <= i; j++)
                {
                    f = new LTLOr(new LTLGlobally(new LTLEventually(new LTLPred(j.ToString(), al, solver))),f);
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(f, al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\gftest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingGFAnd()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 15;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i + 1);
                LTLFormula f = new LTLGlobally(new LTLEventually(new LTLPred("1", al, solver)));
                for (int j = 2; j <= i; j++)
                {
                    f = new LTLAnd(new LTLGlobally(new LTLEventually(new LTLPred(j.ToString(), al, solver))), f);
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(f, al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\gfandtest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingFAnd()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 10;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i);
                LTLFormula f = new LTLEventually(new LTLPred("1", al, solver));
                for (int j = 2; j <= i; j++)
                {
                    f = new LTLAnd(new LTLEventually(new LTLPred(j.ToString(), al, solver)), f);
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(f, al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\fandtest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingSS()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 8;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i);
                LTLFormula f = new LTLGlobally(new LTLPred("1", al, solver));
                for (int j = 2; j <= i; j++)
                {
                    f = new LTLOr(new LTLGlobally(new LTLPred(j.ToString(), al, solver)), f);
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(f, al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\fandtest.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }        

        [TestMethod]
        public void TestScalingN()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 40;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(2);
                LTLFormula f = new LTLPred("2", al, solver);
                for (int j = 1; j < i; j++)
                {
                    LTLFormula n = new LTLPred("2", al, solver);
                    for (int s = 0; s < j; s++)
                    {
                        n = new LTLNext(n);
                    }
                    f =  new LTLOr(f, n);
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(new LTLPred("1", al, solver),f), al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\next.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingNN()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 30;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(2);
                LTLFormula f = new LTLPred("2", al, solver);
                for (int j = 1; j < i; j++)
                {
                    LTLFormula n = new LTLPred("2", al, solver);
                    f = new LTLOr(n, new LTLNext(f));
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(new LTLPred("1", al, solver), f), al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\next.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingGei()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 20;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(2);
                LTLFormula f = new LTLPred("2", al, solver);
                for (int j = 1; j < i; j++)
                {
                    LTLFormula n = new LTLPred("2", al, solver);
                    f = new LTLAnd(f, new LTLNextN(n,j));
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(new LTLPred("1", al, solver), f), al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\next.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestScalingGni()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            //int maxc = 1;

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 20;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(2);
                LTLFormula f = new LTLPred("2", al, solver);
                for (int j = 1; j < i; j++)
                {
                    LTLFormula n = new LTLPred("2", al, solver);
                    f = new LTLAnd(f, new LTLNext(n));
                }
                formulas.Add(new Pair<LTLFormula, List<string>>(new LTLIf(new LTLPred("1", al, solver), f), al));
            }

            var k = 1;
            Stopwatch sw = new Stopwatch();
            foreach (var f in formulas)
            {

                sb.AppendFormat("{0},", k);
                runFormula(f.First, f.Second, solver, sb, maxc);
                sb.AppendLine();
                k++;
            }

            string filepath = @"C:\temp\next.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            file.WriteLine(sb.ToString());
            file.Close();

            Console.WriteLine(sb.ToString());
        }       

        [TestMethod]
        public void TestScalingGU()
        {
            var solver = new CharSetSolver(BitWidth.BV64);

            //var path = @"C:\temp\testunion\";
            StringBuilder sb = new StringBuilder();

            List<Pair<LTLFormula, List<string>>> formulas = new List<Pair<LTLFormula, List<string>>>();

            int mink = 1;
            int maxk = 5;
            for (int i = mink; i <= maxk; i++)
            {
                var al = Alph(i+1);
                LTLFormula f = new LTLPred((i+1).ToString(), al, solver);

                for (int j = i; j > 1; j--)
                {
                    var pp = new LTLPred(j.ToString(), al, solver);
                     f = new LTLAnd(pp, new LTLUntil(pp, f));
                }
                f=new LTLGlobally(new LTLIf(new LTLPred("1", al, solver),f));
                formulas.Add(new Pair<LTLFormula, List<string>>(f, al));

                Console.WriteLine(f.ToMSO().ToString());
            }

            //var k = 1;
            //Stopwatch sw = new Stopwatch();
            //foreach (var f in formulas)
            //{

            //    sb.AppendFormat("{0},", k);
            //    runFormula(f.First, f.Second, solver, sb, maxc);
            //    sb.AppendLine();
            //    k++;
            //}

            //string filepath = @"C:\temp\fgtest.txt";
            //System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
            //file.WriteLine(sb.ToString());
            //file.Close();

            //Console.WriteLine(sb.ToString());
        }

        private List<string> Alph(int n)
        {
            var l = new List<string>();
            for (int i = 1; i <= n; i++)
                l.Add(i.ToString());

            return l;
        }

        static int c = 1;
        static bool skipmona = false;
        static bool skipmy = false;
        private static void runFormula(LTLFormula phi, List<string> al, CharSetSolver solver, StringBuilder sb, int maxc)
        {
            Stopwatch sw = new Stopwatch();

            if (!skipmy)
            {
                sw.Restart();
                for (int i = 0; i < maxc; i++)
                {
                    var dfa = phi.getDFA(al, solver);
                    solver.SaveAsDot(dfa, "c" + c, @"c:/temp/c" + c++);
                }
                sw.Stop();
                sb.AppendFormat("{0},", sw.ElapsedMilliseconds / maxc);
            }

            if (!skipmona)
            {
                string filepath = @"tmp.mona";
                System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
                file.WriteLine(phi.ToMSO(true).ToMonaString(al, solver));
                file.Close();

                var psi = new ProcessStartInfo
                {
                    FileName = @"c:/cygwin/home/lorisdan/Mona/mona.exe",
                    Arguments = "" + filepath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };
                long time = 0;
                var to = false;
                for (int i = 0; i < maxc; i++)
                {
                    var process = Process.Start(psi);
                    var result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    System.IO.StreamWriter ff = new System.IO.StreamWriter(@"c:/temp/out.txt");
                    ff.WriteLine(result);
                    ff.Close();
                    //Console.WriteLine(result);
                    result = result.Remove(result.Length - 1);
                    var a = result.Split(':');
                    if (a[a.Length - 1].Contains("***"))
                    {
                        to = true;
                        break;
                    }
                    else
                    {
                        var b = a[a.Length - 6].Split('.', '\n');
                        var cs = int.Parse(b[1]);
                        var secs = int.Parse(b[0]);
                        time += 1000 * secs + cs * 10;
                    }
                }
                sb.AppendFormat("{0}",  to?"TO":(time / maxc).ToString());
            }

            if(printphi)
                sb.AppendFormat(",{0}",  phi.Simplify().ToString(true));

        }
        static bool printphi = false;
    }
}
