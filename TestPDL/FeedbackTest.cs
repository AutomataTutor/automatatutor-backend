using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Z3;
using MSOZ3;
using AutomataPDL;

using System.Diagnostics;
using System.Threading;
using System.IO;

namespace TestPDL
{
    [TestClass]
    public class FeedbackTest
    {
        long timeout = 1500;

        private IEnumerable<Automaton<BDD>> readDFAs(CharSetSolver solver)
        {
            int c = 0;
            XElement autcontent = null;
            using (XmlReader reader = XmlReader.Create("C:/temp/feedback/group2hints-formatted.xml"))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.Name == "automaton")
                    {
                        autcontent = XElement.ReadFrom(reader) as XElement;
                        String line = autcontent.ToString();
                        c++;
                        //if (c > 10000000)
                        //    yield break;
                        yield return DFAUtilities.parseForTest(line, solver);
                    }
                }
            }
        }
        


        [TestMethod]
        public void Feedback1()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var al = new HashSet<char>(new char[] { 'a', 'b' });

            var dfa1 = new PDLModSetEq(new PDLIndicesOf("a"), 2, 0).GetDFA(al,solver);
            var dfa2 = new PDLModSetEq(new PDLIndicesOf("a"), 2, 1).GetDFA(al, solver);

            var v4 = DFAGrading.GetGrade(dfa2, dfa1, al, solver, timeout, 10, FeedbackLevel.Hint);
            Console.WriteLine("Grade: {0}", v4.First);
            foreach(var v in v4.Second)
                Console.WriteLine("Feedback: {0}", v);


        }

        [TestMethod]
        public void Feedback2()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var al = new HashSet<char>(new char[] { 'a', 'b' });

            var dfa1 = new PDLModSetEq(new PDLIndicesOf("a"), 2, 1).GetDFA(al, solver);


            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var movescorrect = new List<Move<BDD>>();

            movescorrect.Add(new Move<BDD>(0, 0, b));
            movescorrect.Add(new Move<BDD>(0, 1, a));
            movescorrect.Add(new Move<BDD>(1, 0, a));
            movescorrect.Add(new Move<BDD>(1, 0, b));

            var dfa2 = Automaton<BDD>.Create(0, new int[] { 1 }, movescorrect);

            var v4 = DFAGrading.GetGrade(dfa1, dfa2, al, solver, timeout, 10, FeedbackLevel.Hint);
            Console.WriteLine("Grade: {0}", v4.First);
            foreach (var v in v4.Second)
                Console.WriteLine("Feedback: {0}", v);


        }


        [TestMethod]
        public void UnderapproximationTest()
        {
            runUnderapproxTest("Test1");
        }

        [TestMethod]
        public void FeedbackTest1()
        {
            TreeEditDistance treeCorrector = new TreeEditDistance();
            TreeDefinition aTree = CreateTreeHelper.MakeTree(new PDLNot(new PDLTrue()));
            TreeDefinition bTree = CreateTreeHelper.MakeTree(new PDLTrue());
            Transformation transform = treeCorrector.getTransformation(aTree, bTree);
        }

        [TestMethod]
        public void ComputeFeedback()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { '0', '1' };
            HashSet<char> al = new HashSet<char>(alph);

            var correct = new PDLContains("0101");

            var dfa_correct = correct.GetDFA(al, solver);

            var o = solver.MkCharConstraint(false, '0');
            var l = solver.MkCharConstraint(false, '1');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, l));
            moves.Add(new Move<BDD>(0, 1, o));
            moves.Add(new Move<BDD>(1, 1, o));
            moves.Add(new Move<BDD>(1, 2, l));
            moves.Add(new Move<BDD>(2, 3, o));
            moves.Add(new Move<BDD>(2, 0, l));
            moves.Add(new Move<BDD>(3, 0, o));
            moves.Add(new Move<BDD>(3, 4, l));
            moves.Add(new Move<BDD>(4, 4, o));
            moves.Add(new Move<BDD>(4, 4, l));

            var dfa_wrong = Automaton<BDD>.Create(0, new int[] { 4 }, moves);

            var v1 = DFAGrading.GetGrade(dfa_correct, dfa_wrong, al, solver, timeout, 10, FeedbackLevel.Hint, true, true, true);

            Console.WriteLine("Grade: {0}", v1.First);
            foreach (var feed in v1.Second)
                Console.WriteLine(feed.ToString());

        }

        [TestMethod]
        public void CounterexampleFeedback()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var movescorrect = new List<Move<BDD>>();

            movescorrect.Add(new Move<BDD>(0, 0, a));
            movescorrect.Add(new Move<BDD>(0, 1, b));
            movescorrect.Add(new Move<BDD>(1, 1, a));
            movescorrect.Add(new Move<BDD>(1, 1, b));

            var dfa_correct = Automaton<BDD>.Create(0, new int[] { 0 }, movescorrect);

            var moves1 = new List<Move<BDD>>();

            moves1.Add(new Move<BDD>(0, 0, a));
            moves1.Add(new Move<BDD>(0, 0, b));

            var dfa1 = Automaton<BDD>.Create(0, new int[] { 0 }, moves1);

            var v1 = DFAGrading.GetGrade(dfa_correct, dfa1, al, solver, timeout, 10, FeedbackLevel.Minimal, true, false, false);

            Console.WriteLine("Grade: {0}", v1.First);
            foreach (var feed in v1.Second)
                Console.WriteLine(feed.ToString());
        }

        [TestMethod]
        public void RegexFeedbackDominik()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { '0', '1' };
            HashSet<char> al = new HashSet<char>(alph);       

            var escapedRexpr1 = string.Format(@"^(1*(01*01*01*)*)$");
            var escapedRexpr2 = string.Format(@"^((0*0*0*)*)$");
            Automaton<BDD> aut1 = null;
            Automaton<BDD> aut2 = null;
            try
            {
                aut1 = solver.Convert(escapedRexpr1).RemoveEpsilons(solver.MkOr).Determinize(solver); ;
                aut2 = solver.Convert(escapedRexpr2).RemoveEpsilons(solver.MkOr).Determinize(solver); ;
            }
            catch (ArgumentException e)
            {
                throw new PDLException("The input is not a well formatted regular expression: " + e.Message);
            }

            var v1 = DFAGrading.GetGrade(aut1, aut2, al, solver, timeout, 10, FeedbackLevel.Minimal, true, false, false);

            Console.WriteLine("Grade: {0}", v1.First);
            foreach (var feed in v1.Second)
                Console.WriteLine(feed.ToString());
        }


        [TestMethod]
        public void MaheshCase4()
        {
            // contains baba as substring

            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var movescorrect = new List<Move<BDD>>();

            int R = 10;
            movescorrect.Add(new Move<BDD>(0, 0, a));
            movescorrect.Add(new Move<BDD>(0, 1, b));
            movescorrect.Add(new Move<BDD>(1, 2, a));
            movescorrect.Add(new Move<BDD>(1, 1, b));
            movescorrect.Add(new Move<BDD>(2, 0, a));
            movescorrect.Add(new Move<BDD>(2, 3, b));
            movescorrect.Add(new Move<BDD>(3, 4, a));
            movescorrect.Add(new Move<BDD>(3, 1, b));
            movescorrect.Add(new Move<BDD>(4, 4, a));
            movescorrect.Add(new Move<BDD>(4, 4, b));

            var dfa_correct = Automaton<BDD>.Create(0, new int[] { 4,0 }, movescorrect);

            var moves1 = new List<Move<BDD>>();

            moves1.Add(new Move<BDD>(0, 0, a));
            moves1.Add(new Move<BDD>(0, 1, b));
            moves1.Add(new Move<BDD>(1, 2, a));
            moves1.Add(new Move<BDD>(1, 0, b));
            moves1.Add(new Move<BDD>(2, 0, a));
            moves1.Add(new Move<BDD>(2, 3, b));
            moves1.Add(new Move<BDD>(3, 4, a));
            moves1.Add(new Move<BDD>(3, 0, b));
            moves1.Add(new Move<BDD>(4, 4, a));
            moves1.Add(new Move<BDD>(4, 4, b));

            var dfa1 = Automaton<BDD>.Create(0, new int[] { 0,4 }, moves1);

            var moves2 = new List<Move<BDD>>();

            moves2.Add(new Move<BDD>(0, 0, a));
            moves2.Add(new Move<BDD>(0, 1, b));
            moves2.Add(new Move<BDD>(1, 2, a));
            moves2.Add(new Move<BDD>(1, 1, b));
            moves2.Add(new Move<BDD>(2, 2, a));
            moves2.Add(new Move<BDD>(2, 3, b));
            moves2.Add(new Move<BDD>(3, 4, a));
            moves2.Add(new Move<BDD>(3, 3, b));
            moves2.Add(new Move<BDD>(4, 4, a));
            moves2.Add(new Move<BDD>(4, 4, b));

            var dfa2 = Automaton<BDD>.Create(0, new int[] { 4 }, moves2);

            var moves3 = new List<Move<BDD>>();

            moves3.Add(new Move<BDD>(0, R, a));
            moves3.Add(new Move<BDD>(0, 1, b));
            moves3.Add(new Move<BDD>(1, 2, a));
            moves3.Add(new Move<BDD>(1, R, b));
            moves3.Add(new Move<BDD>(2, R, a));
            moves3.Add(new Move<BDD>(2, 3, b));
            moves3.Add(new Move<BDD>(3, 4, a));
            moves3.Add(new Move<BDD>(3, R, b));
            moves3.Add(new Move<BDD>(4, 4, a));
            moves3.Add(new Move<BDD>(4, 4, b));
            moves3.Add(new Move<BDD>(R, R, a));
            moves3.Add(new Move<BDD>(R, R, b));

            var dfa3 = Automaton<BDD>.Create(0, new int[] { 4 }, moves3);

            var moves4 = new List<Move<BDD>>();

            moves4.Add(new Move<BDD>(0, 0, a));
            moves4.Add(new Move<BDD>(0, 1, b));
            moves4.Add(new Move<BDD>(1, 2, a));
            moves4.Add(new Move<BDD>(1, 1, b));
            moves4.Add(new Move<BDD>(2, 0, a));
            moves4.Add(new Move<BDD>(2, 3, b));
            moves4.Add(new Move<BDD>(3, 3, a));
            moves4.Add(new Move<BDD>(3, 1, b));
            //moves4.Add(new Move<BDD>(4, 4, a));
            //moves4.Add(new Move<BDD>(4, 4, b));

            var dfa4 = Automaton<BDD>.Create(0, new int[] { 0 }, moves4);

            var moves5 = new List<Move<BDD>>();

            moves5.Add(new Move<BDD>(0, 5, a));
            moves5.Add(new Move<BDD>(0, 1, b));
            moves5.Add(new Move<BDD>(1, 2, a));
            moves5.Add(new Move<BDD>(1, 6, b));
            moves5.Add(new Move<BDD>(2, 7, a));
            moves5.Add(new Move<BDD>(2, 3, b));
            moves5.Add(new Move<BDD>(3, 4, a));
            moves5.Add(new Move<BDD>(3, 8, b));
            moves5.Add(new Move<BDD>(4, 4, a));
            moves5.Add(new Move<BDD>(4, 4, b));
            moves5.Add(new Move<BDD>(5, 5, a));
            moves5.Add(new Move<BDD>(5, 1, b));
            moves5.Add(new Move<BDD>(6, 2, a));
            moves5.Add(new Move<BDD>(6, 0, b));
            moves5.Add(new Move<BDD>(7, 7, a));
            moves5.Add(new Move<BDD>(7, 3, b));
            moves5.Add(new Move<BDD>(8, 4, a));
            moves5.Add(new Move<BDD>(8, 8, b));

            var dfa5 = Automaton<BDD>.Create(0, new int[] { 4 }, moves5);


            //var v1 = Grading.GetGrade(dfa_correct, dfa1, al, solver, timeout, 10);
            //var v2 = Grading.GetGrade(dfa_correct, dfa2, al, solver, timeout, 10);
            //var v3 = Grading.GetGrade(dfa_correct, dfa3, al, solver, timeout, 10);
            var v4 = DFAGrading.GetGrade(dfa_correct, dfa4, al, solver, timeout, 10, FeedbackLevel.Minimal, true, false, false);
            //var v5 = Grading.GetGrade(dfa_correct, dfa5, al, solver, timeout, 10);


            //Console.WriteLine("Grade: {0}, Feedback: {1}", v1.First, v1.Second);
            //Console.WriteLine("Grade: {0}, Feedback: {1}", v2.First, v2.Second);
            //Console.WriteLine("Grade: {0}, Feedback: {1}", v3.First, v3.Second);
            Console.WriteLine("Grade: {0}, Feedback: {1}", v4.First, v4.Second);
            //Console.WriteLine("Grade: {0}, Feedback: {1}", v5.First, v5.Second);

            //Assert.IsTrue(v1 > v2 && v2 > v3);
        }


        [TestMethod]
        public void TreeScript()
        {
            TreeEditDistance treeCorrector = new TreeEditDistance();
            TreeDefinition aTree = CreateTreeHelper.MakeTree(new PDLModSetEq(new PDLAllPos(), 3, 2));
            TreeDefinition bTree = CreateTreeHelper.MakeTree(new PDLModSetEq(new PDLAllPos(), 4, 2));
            Transformation transform = treeCorrector.getTransformation(aTree, bTree);

            Console.Write(transform.ToHTMLColoredStringAtoB("red", "blue"));
        }

        //
        private void runUnderapproxTest(string testName)
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            int printLimit = 10;

            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_al = ReadDFA(testName, solver);
            //PrintDFA(dfa_al.Second, "A", dfa_al.First);

            StringBuilder sb = new StringBuilder();

            int i = 0;
            foreach (var pair in pdlEnumerator.SynthesizeUnderapproximationPDL(dfa_al.First, dfa_al.Second, solver, sb, timeout))
            {
                i++;
                pair.First.ToString(sb);
                sb.AppendLine(" ### +"+pair.Second.ToString()+" ### +"+pair.First.GetFormulaSize());
                sb.AppendLine();
                if (i == printLimit)
                    break;
            }

            System.Console.WriteLine(sb);
        }

        #region ACCESSORIES METHODS
        private static void PrintDFA(Automaton<BDD> dfa, string name, HashSet<char> al)
        {
            var sb = new StringBuilder();
            DFAUtilities.printDFA(dfa, al, sb);

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"../../../TestPDL/DFAs/" + name + ".txt");
            file.WriteLine(sb);
            file.Close();
        }

        private static Pair<HashSet<char>, Automaton<BDD>> ReadDFA(string name, CharSetSolver solver)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../TestPDL/DFAs/" + name + ".txt");
            string res = file.ReadToEnd();
            file.Close();
            return DFAUtilities.parseDFAFromString(res, solver);
        }
        #endregion
    }
}
