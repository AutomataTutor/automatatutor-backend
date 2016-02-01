using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.Automata;
using Microsoft.Z3;
using AutomataPDL;
using Microsoft.VisualBasic.FileIO;

namespace GradingData
{
    class Test
    {
        static void OldMain(string[] args)
        {
            #region Example
            /*
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var movescorrect = new List<Move<BvSet>>();

            int st00 = 0, st01 = 1, st10 = 2, st20 = 3, st11 = 4, st02 = 5, st30 = 6, st21 = 7, st12 = 8, st31 = 9, st22 = 10, st32 = 11;

            movescorrect.Add(new Move<BvSet>(st00, st10, a));
            movescorrect.Add(new Move<BvSet>(st00, st01, b));
            movescorrect.Add(new Move<BvSet>(st10, st20, a));
            movescorrect.Add(new Move<BvSet>(st10, st11, b));
            movescorrect.Add(new Move<BvSet>(st01, st11, a));
            movescorrect.Add(new Move<BvSet>(st01, st02, b));
            movescorrect.Add(new Move<BvSet>(st20, st30, a));
            movescorrect.Add(new Move<BvSet>(st20, st21, b));
            movescorrect.Add(new Move<BvSet>(st11, st21, a));
            movescorrect.Add(new Move<BvSet>(st11, st12, b));
            movescorrect.Add(new Move<BvSet>(st02, st02, b));
            movescorrect.Add(new Move<BvSet>(st02, st12, a));
            movescorrect.Add(new Move<BvSet>(st30, st30, a));
            movescorrect.Add(new Move<BvSet>(st30, st31, b));
            movescorrect.Add(new Move<BvSet>(st21, st31, a));
            movescorrect.Add(new Move<BvSet>(st21, st22, b));
            movescorrect.Add(new Move<BvSet>(st12, st12, b));
            movescorrect.Add(new Move<BvSet>(st12, st22, a));
            movescorrect.Add(new Move<BvSet>(st31, st31, a));
            movescorrect.Add(new Move<BvSet>(st31, st32, b));
            movescorrect.Add(new Move<BvSet>(st22, st22, b));
            movescorrect.Add(new Move<BvSet>(st22, st32, a));
            movescorrect.Add(new Move<BvSet>(st32, st32, b));
            movescorrect.Add(new Move<BvSet>(st32, st32, a));

            var dfa_correct = Automaton<BvSet>.Create(st00, new int[] { st32 }, movescorrect);

            var moves1 = new List<Move<BvSet>>();

            moves1.Add(new Move<BvSet>(0, 1, a));
            moves1.Add(new Move<BvSet>(1, 2, a));
            moves1.Add(new Move<BvSet>(2, 3, a));
            moves1.Add(new Move<BvSet>(3, 4, b));
            moves1.Add(new Move<BvSet>(4, 5, b));
            moves1.Add(new Move<BvSet>(5, 5, a));
            moves1.Add(new Move<BvSet>(5, 5, b));

            var dfa1 = Automaton<BvSet>.Create(0, new int[] { 5 }, moves1);

            var moves2 = new List<Move<BvSet>>();

            moves2.Add(new Move<BvSet>(0, 0, b));
            moves2.Add(new Move<BvSet>(0, 1, a));
            moves2.Add(new Move<BvSet>(1, 1, b));
            moves2.Add(new Move<BvSet>(1, 2, a));
            moves2.Add(new Move<BvSet>(2, 2, b));
            moves2.Add(new Move<BvSet>(2, 3, a));
            moves2.Add(new Move<BvSet>(3, 3, a));
            moves2.Add(new Move<BvSet>(3, 4, b));
            moves2.Add(new Move<BvSet>(4, 4, a));
            moves2.Add(new Move<BvSet>(4, 5, b));
            moves2.Add(new Move<BvSet>(5, 5, a));
            moves2.Add(new Move<BvSet>(5, 5, b));

            var dfa2 = Automaton<BvSet>.Create(0, new int[] { 5 }, moves2);

            var moves3 = new List<Move<BvSet>>();

            int st03 = 12, st40 = 13, st13 = 14, st04 = 15, st50 = 16, st41 = 17, st23 = 18, st14 = 19, st05 = 20;

            moves3.Add(new Move<BvSet>(st00, st10, a));
            moves3.Add(new Move<BvSet>(st00, st01, b));
            moves3.Add(new Move<BvSet>(st10, st20, a));
            moves3.Add(new Move<BvSet>(st10, st11, b));
            moves3.Add(new Move<BvSet>(st01, st11, a));
            moves3.Add(new Move<BvSet>(st01, st02, b));
            moves3.Add(new Move<BvSet>(st20, st30, a));
            moves3.Add(new Move<BvSet>(st20, st21, b));
            moves3.Add(new Move<BvSet>(st11, st21, a));
            moves3.Add(new Move<BvSet>(st11, st12, b));
            moves3.Add(new Move<BvSet>(st02, st12, a));
            moves3.Add(new Move<BvSet>(st02, st03, b));
            moves3.Add(new Move<BvSet>(st30, st40, a));
            moves3.Add(new Move<BvSet>(st30, st31, b));
            moves3.Add(new Move<BvSet>(st21, st31, a));
            moves3.Add(new Move<BvSet>(st21, st22, b));
            moves3.Add(new Move<BvSet>(st12, st22, a));
            moves3.Add(new Move<BvSet>(st12, st13, b));
            moves3.Add(new Move<BvSet>(st03, st13, a));
            moves3.Add(new Move<BvSet>(st03, st04, b));
            moves3.Add(new Move<BvSet>(st40, st50, a));
            moves3.Add(new Move<BvSet>(st40, st41, b));
            moves3.Add(new Move<BvSet>(st31, st32, b));
            moves3.Add(new Move<BvSet>(st22, st32, a));
            moves3.Add(new Move<BvSet>(st13, st23, a));
            moves3.Add(new Move<BvSet>(st13, st14, b));
            moves3.Add(new Move<BvSet>(st04, st14, a));
            moves3.Add(new Move<BvSet>(st04, st05, b));
            moves3.Add(new Move<BvSet>(st50, st41, b));
            moves3.Add(new Move<BvSet>(st41, st50, a));
            moves3.Add(new Move<BvSet>(st41, st32, b));
            moves3.Add(new Move<BvSet>(st32, st32, a));
            moves3.Add(new Move<BvSet>(st32, st32, b));
            moves3.Add(new Move<BvSet>(st23, st32, a));
            moves3.Add(new Move<BvSet>(st23, st23, b));
            moves3.Add(new Move<BvSet>(st14, st23, a));
            moves3.Add(new Move<BvSet>(st14, st14, b));
            moves3.Add(new Move<BvSet>(st05, st14, a));

            var dfa3 = Automaton<BvSet>.Create(st00, new int[] { st32 }, moves3);

            var moves4 = new List<Move<BvSet>>();

            moves4.Add(new Move<BvSet>(st00, st10, a));
            moves4.Add(new Move<BvSet>(st00, st01, b));
            moves4.Add(new Move<BvSet>(st10, st20, a));
            moves4.Add(new Move<BvSet>(st10, st11, b));
            moves4.Add(new Move<BvSet>(st01, st11, a));
            moves4.Add(new Move<BvSet>(st01, st02, b));
            moves4.Add(new Move<BvSet>(st20, st30, a));
            moves4.Add(new Move<BvSet>(st20, st21, b));
            moves4.Add(new Move<BvSet>(st11, st21, a));
            moves4.Add(new Move<BvSet>(st11, st12, b));
            moves4.Add(new Move<BvSet>(st02, st12, a));
            //moves4.Add(new Move<BvSet>(st02, st03, b));
            //moves4.Add(new Move<BvSet>(st30, st40, a));
            moves4.Add(new Move<BvSet>(st30, st31, b));
            moves4.Add(new Move<BvSet>(st21, st31, a));
            moves4.Add(new Move<BvSet>(st21, st22, b));
            moves4.Add(new Move<BvSet>(st12, st22, a));
            //moves4.Add(new Move<BvSet>(st12, st13, b));
            //moves4.Add(new Move<BvSet>(st03, st13, a));
            //moves4.Add(new Move<BvSet>(st03, st04, b));
            //moves4.Add(new Move<BvSet>(st40, st50, a));
            //moves4.Add(new Move<BvSet>(st40, st41, b));
            moves4.Add(new Move<BvSet>(st31, st32, b));
            moves4.Add(new Move<BvSet>(st22, st32, a));
            //moves4.Add(new Move<BvSet>(st13, st23, a));
            //moves4.Add(new Move<BvSet>(st13, st14, b));
            //moves4.Add(new Move<BvSet>(st04, st14, a));
            //moves4.Add(new Move<BvSet>(st04, st05, b));
            //moves4.Add(new Move<BvSet>(st50, st41, b));
            //moves4.Add(new Move<BvSet>(st41, st50, a));
            //moves4.Add(new Move<BvSet>(st41, st32, b));
            moves4.Add(new Move<BvSet>(st32, st32, a));
            moves4.Add(new Move<BvSet>(st32, st32, b));
            //moves4.Add(new Move<BvSet>(st23, st32, a));
            //moves4.Add(new Move<BvSet>(st23, st23, b));
            //moves4.Add(new Move<BvSet>(st14, st23, a));
            //moves4.Add(new Move<BvSet>(st14, st14, b));
            //moves4.Add(new Move<BvSet>(st05, st14, a));

            var dfa4 = Automaton<BvSet>.Create(st00, new int[] { st32 }, moves4);

            var v1 = DFAGrading.GetGrade(dfa_correct, dfa1, al, solver, timeout, 10);
            var v2 = DFAGrading.GetGrade(dfa_correct, dfa2, al, solver, timeout, 10);
            var v3 = DFAGrading.GetGrade(dfa_correct, dfa3, al, solver, timeout, 10);
            var v4 = DFAGrading.GetGrade(dfa_correct, dfa4, al, solver, timeout, 10);


            Console.WriteLine("grade1: {0}, grade2: {1}; grade3: {2}; grade4: {3}", v1, v2, v3, v4);
            //Assert.IsTrue(v1 > v2 && v2 > v3);
            */
            #endregion

            List<char> alph = new List<char> {'a', 'b'};
            HashSet<char> al = new HashSet<char>(alph);

            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            //Console.WriteLine(Clean());

            //scores(1, "Rajeev", "Loris");
            //Grade(1, 1000);
            //Console.WriteLine(inversions(1, "Rajeev", "Loris", 2));

            int p = 3;
            int caseS = 4;
            switch (caseS)
            {
                #region initial
                case 1:
                    //Console.WriteLine(inversions(p, "Rajeev", "Tool", 0));
                    //Console.WriteLine(inversions(p, "Rajeev", "Loris", 0));
                    //Console.WriteLine(inversions(p, "Random", "Loris", 0));
                    //Console.WriteLine(inversions(p, "Rajeev", "Random", 0));
                    RandomGrade(p);
                    Console.WriteLine(inversions(p, "Random", "Tool", 0));
                    Console.WriteLine(inversions(p, "Mahesh", "Tool", 0));
                    break;
                case 2:
                    CreateDotFiles(p);
                    break;
                #endregion
                case 3:
                    Grade(p, 1000);
                    break;
                case 4:
                    LazyGrade(1); Differences(1, "Rajeev", "Loris");
                    LazyGrade(3); Differences(3, "Loris", "Dileep");
                    LazyGrade(4); Differences(4, "Mahesh", "Loris");
                    LazyGrade(5); Differences(5, "Mahesh", "Loris");
                    LazyGrade(6); Differences(6, "Loris", "Dileep");
                    LazyGrade(16);Differences(16, "Loris", "Dileep");
                    break;
                case 5:
                    IsoTest(16, "Loris");
                    //Console.WriteLine("Exceptions:{0}", iso.Second);
                    //Console.WriteLine("# of different solutions:", iso.First.Count);
                    break;
                default:
                    //Console.WriteLine(GradeSingle("C:/Users/Dileep/Desktop/Answers/ans-3.xml", "C:/Users/Dileep/Desktop/Attempts/3/23/235.xml"));
                    //Console.WriteLine("Default");
                    LazyGrade(1);
                    break;
            }
        }

        private static void LazyGrade(int probId)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/" + probId + "/";
            string ansFile = "C:/Users/Dileep/Desktop/Answers/ans-" + probId + ".xml";
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_correct = DFAUtilities.parseDFAfromTutor(ansFile, solver);

            using (StreamReader reader = new StreamReader(dir + "clean.txt"))
            using (StreamWriter writer = new StreamWriter(dir + "Lazy-" + probId + ".txt", false))
            {
                string id;
                string[] file;
                int g;
                RandomGen.SetSeedFromSystemTime();
                while ((id = reader.ReadLine()) != null)
                {
                    //CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
                    file = Directory.GetFiles(dir, id + ".xml", System.IO.SearchOption.AllDirectories);
                    Console.WriteLine(file[0]);
                    var dfa = DFAUtilities.parseDFAfromEvent(file[0], solver);
                    if (dfa_correct.Second.IsEquivalentWith(dfa.Second, solver))
                        g = (RandomGen.GetUniform() > 0.5 ? 10 : 9);
                    else
                        g = (int)Math.Max(Math.Min(8.0 - Math.Abs(dfa_correct.Second.StateCount - dfa.Second.StateCount) * 2.0 + RandomGen.GetNormal(0, 2), 10.0), 0.0);
                    //var g = DFAGrading.GetGrade(dfa_correct.Second, dfa.Second, al, solver, timeout, 10);
                    Console.WriteLine("---- {0},{1}", id, g);
                    writer.WriteLine("{0},{1}", id, g);
                }
            }
        }

        //A test to see how many isomorphic solutions are graded differently by the same grader
        private static void IsoTest(int prob, string grader)
        {
            //get grader score, get DFA from XML for that id ,check if printDFA in Dictionary and add score to that list
            string dir = "C:/Users/Dileep/Desktop/Attempts/" + prob + "/";
            var ret = new Dictionary<string, List<Pair<int,int>>>();
            var score = new Dictionary<int, int>();
            string outFile = dir + "Iso-" + grader + ".txt";

            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);
            var solver = new CharSetSolver(BitWidth.BV64);

            using (TextFieldParser parser = new TextFieldParser(dir + grader + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    score.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1])); // has also unclean problems
                }
            }
            List<int> clean = new List<int>(); // will have ids of clean problems
            using (StreamReader reader = new StreamReader(dir + "/clean.txt"))
            {
                string line; int id;
                while ((line = reader.ReadLine()) != null)
                {
                    id = Convert.ToInt32(line);
                    clean.Add(id);
                }
            }
            // go through clean, put a list for printDFA if not present, put score in that list.
            string[] file;
            var sb = new StringBuilder();
            int ex=0;
            string dfaString;
            foreach (int i in clean)
            {
                file = Directory.GetFiles(dir, i + ".xml", System.IO.SearchOption.AllDirectories);
                try
                {
                    DFAUtilities.printDFA(DFAUtilities.parseDFAfromEvent(file[0], solver).Second, al, sb);
                    dfaString = sb.ToString();
                    if (!ret.ContainsKey(dfaString))
                    {
                        ret.Add(dfaString, new List<Pair<int,int>>());
                    }
                    ret[dfaString].Add(new Pair<int, int>(i, score[i]));
                }
                catch (Exception e)
                {
                    Console.WriteLine("EXCEPTION: "+e);
                    ex++;
                }
                sb.Clear();
            }
            using (var writer = new StreamWriter(outFile, false))
            {
                writer.WriteLine("# of string/list pairs: {0}", ret.Count);
                writer.WriteLine("# of exceptions: {0}", ex);
                Console.WriteLine("# of string/list pairs: {0}", ret.Count);
                Console.WriteLine("# of exceptions: {0}", ex);
                foreach (KeyValuePair<string, List<Pair<int, int>>> pair in ret)
                {
                    if (pair.Value.Count != 1)
                    {
                        foreach (Pair<int, int> p in pair.Value)
                        {
                            Console.Write("({0},{1})", p.First, p.Second);
                            writer.Write("({0},{1})", p.First, p.Second);
                        }
                        Console.WriteLine("");
                        writer.WriteLine("");
                    }
                }
            }
        }

        //lookup clean.txt, random grade, print
        private static void RandomGrade(int probId)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/" + probId + "/";
            string ansFile = "C:/Users/Dileep/Desktop/Answers/ans-" + probId + ".xml";
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_correct = DFAUtilities.parseDFAfromTutor(ansFile, solver);

            RandomGen.SetSeedFromSystemTime();

            using (StreamReader reader = new StreamReader(dir + "clean.txt"))
            using (StreamWriter writer = new StreamWriter(dir + "Random-" + probId + ".txt"))
            {
                string id;
                while ((id = reader.ReadLine()) != null)
                {
                    //CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
                    //file = Directory.GetFiles(dir, id + ".xml", System.IO.SearchOption.AllDirectories);
                    //Console.WriteLine(file[0]);
                    //var dfa = DFAUtilities.parseDFAfromEvent(file[0], solver);
                    //var g = DFAGrading.GetGrade(dfa_correct.Second, dfa.Second, al, solver, timeout, 10);
                    var g = RandomGen.GetNormal(6.85,3.00);
                    g = Math.Max(Math.Min(g, 10.00), 0.00);
                    g = Math.Round(g);
                    Console.WriteLine("---- {0},{1}", id, g);
                    writer.WriteLine("{0},{1}", id, g);
                }
            }
        }

        //lookup clean.txt, grade, print
        private static void Grade(int probId, long timeout)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/" + probId + "/";
            string ansFile = "C:/Users/Dileep/Desktop/Answers/ans-"+ probId + ".xml";
            List<char> alph = new List<char> {'a', 'b'};
            HashSet<char> al = new HashSet<char>(alph);
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_correct = DFAUtilities.parseDFAfromTutor(ansFile, solver);

            using (StreamReader reader = new StreamReader(dir + "clean.txt"))
            using (StreamWriter writer = new StreamWriter(dir + "Tool-" + probId +".txt", false))
            {
                string id;
                string[] file;
                while ((id = reader.ReadLine()) != null)
                {
                    //CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
                    file = Directory.GetFiles(dir, id + ".xml", System.IO.SearchOption.AllDirectories);
                    Console.WriteLine(file[0]);
                    var dfa = DFAUtilities.parseDFAfromEvent(file[0], solver);
                    var g = Grading.GetGrade(dfa_correct.Second, dfa.Second, al, solver, timeout, 10);
                    Console.WriteLine("---- {0},{1}", id, g);
                    writer.WriteLine("{0},{1}", id, g);
                }
            }
        }

        private static void GradeMetric(int probId, long timeout)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/" + probId + "/";
            string ansFile = "C:/Users/Dileep/Desktop/Answers/ans-" + probId + ".xml";
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var dfa_correct = DFAUtilities.parseDFAfromTutor(ansFile, solver);

            using (StreamReader reader = new StreamReader(dir + "clean.txt"))
            using (StreamWriter writer = new StreamWriter(dir + "Tool-Ind-" + probId + ".txt", false))
            {
                string id;
                string[] file;
                while ((id = reader.ReadLine()) != null)
                {
                    //CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
                    file = Directory.GetFiles(dir, id + ".xml", System.IO.SearchOption.AllDirectories);
                    Console.WriteLine(file[0]);
                    var dfa = DFAUtilities.parseDFAfromEvent(file[0], solver);
                    var g1 = Grading.GetGrade(dfa_correct.Second, dfa.Second, al, solver, timeout, 10, FeedbackLevel.Minimal, true,false,false);
                    var g2 = Grading.GetGrade(dfa_correct.Second, dfa.Second, al, solver, timeout, 10, FeedbackLevel.Minimal, false, false, true);
                    var g3 = Grading.GetGrade(dfa_correct.Second, dfa.Second, al, solver, timeout, 10, FeedbackLevel.Minimal, false, true, false);
                    Console.WriteLine("---- {0},{1},{2},{3}", id, g1.First, g2.First, g3.First);
                    writer.WriteLine("{0},{1},{2},{3}", id, g1.First, g2.First, g3.First);
                }
            }
        }

        //private static void Grade

        private static int GradeSingle(string file1, string file2)
        {
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            var aut1 = DFAUtilities.parseDFAfromTutor(file1, solver);
            var aut2 = DFAUtilities.parseDFAfromEvent(file2, solver);
            var dfa1 = aut1.Second; var dfa2 = aut2.Second;

            return Grading.GetGrade(dfa1, dfa2, al, solver, 20000, 10).First;
        }

        private static void Differences(int prob, string grader1, string grader2)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/";
            var s1 = new Dictionary<int, int>();
            var s2 = new Dictionary<int, int>();
            var tool = new Dictionary<int, int>();
            var lazy = new Dictionary<int, int>();
            List<int> clean = new List<int>();
            int h1h2=0,h1t=0,h1r=0;

            #region read s1
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader1 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    s1.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }
            #endregion
            #region read s2
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader2 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    s2.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }
            #endregion
            #region read clean problems
            using (StreamReader reader = new StreamReader(dir + prob + "/clean.txt"))
            {
                string line; int id;
                while ((line = reader.ReadLine()) != null)
                {
                    id = Convert.ToInt32(line);
                    clean.Add(id);
                }
            }
            #endregion
            #region read tool
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/Tool-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    tool.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }
            #endregion
            #region read lazy
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/Lazy-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    lazy.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }
            #endregion

            using (StreamWriter writer = new StreamWriter(dir + prob + "/cumulative.txt", false))
            {
                for (int i = 0; i <= 10; i++)
                {
                    h1h2 = h1t = h1r = 0;
                    foreach (int id in clean)
                    {
                        if (Math.Abs(s1[id] - s2[id]) <= i)
                            h1h2++;
                        if (Math.Abs(s1[id] - tool[id]) <= i)
                            h1t++;
                        if (Math.Abs(s1[id] - lazy[id]) <= i)
                            h1r++;
                    }
                    double p1 = 100.0 * ((double)h1h2 / clean.Count);
                    double p2 = 100.0 * ((double)h1t / clean.Count);
                    double p3 = 100.0 * ((double)h1r / clean.Count);
                    writer.WriteLine("{0}    {1}    {2}    {3}", i, p1, p2, p3);
                }
            }
        }

        //lookup id in clean and take those scores from 
        private static int inversions(int prob, string grader1, string grader2, int threshold)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/";
            //string outFile = dir + prob + "/" + grader1 + "-" + grader2 + "-" + prob + ".txt";
            //var temp1 = new Dictionary<int, int>();
            //var temp2 = new Dictionary<int, int>();
            var s1 = new Dictionary<int, int>();
            var s2 = new Dictionary<int, int>();
            int count = 0;

            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader1 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    s1.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }

            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader2 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    s2.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }

            //you have read all, now clean it up
            List<int> clean = new List<int>();
            using (StreamReader reader = new StreamReader(dir + prob + "/clean.txt"))
            {
                string line; int id;
                while ((line = reader.ReadLine()) != null)
                {
                    id = Convert.ToInt32(line);
                    clean.Add(id);
                }
            }

            foreach (int i in clean)
            {
                foreach (int j in clean)
                {
                    //if loris thinks i much better than j then it better be the case that rajeev thinks it is better than
                    //if rajeev thinks i much better than j then it better be the case that loris thinks it is much better
                    if (((s1[i] > s1[j]+2) && (s2[i] <= s2[j])) || ((s2[i] > s2[j] + 2) && (s1[i] <= s1[j])))
                        count++;
                }
            }
            

            // sort the second one
            /*
            var seq = from pair in s2
                      orderby pair.Value descending
                      select pair;

            foreach (KeyValuePair<int, int> p1 in seq)
            {
                foreach (KeyValuePair<int, int> p2 in seq)
                {
                    if (p1.Key != p2.Key && p1.Value < p2.Value - threshold && s1[p1.Key]  > s1[p2.Key] + threshold)
                        count++;
                }
            }
            */
            return count;

        }

        //print scores together
        private static void printScores(int prob, string grader1, string grader2)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/";
            string outFile = dir + prob + "/" + grader1 + "-" + grader2 + "-" + prob + ".txt";
            var scores1 = new Dictionary<int, int>();
            var scores2 = new Dictionary<int, int>();
            var scoresTool = new Dictionary<int, int>();
            var scoresLazy = new Dictionary<int, int>();
            #region grader1
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader1 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scores1.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }
            #endregion

            #region grader2
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader2 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scores2.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }
            #endregion

            #region tool
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/Tool" + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scoresTool.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }
            #endregion

            #region lazy
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/Lazy" + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scoresLazy.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
            }
            #endregion

            using (StreamReader reader = new StreamReader(dir + prob + "/clean.txt"))
            using (StreamWriter writer = new StreamWriter(outFile, false))
            {
                string line; int id;
                writer.WriteLine("eventId,{0},{1},Tool,Lazy,H1-H2,H1-T,H2-T,H1-L,H2-L", grader1, grader2);
                while ((line = reader.ReadLine()) != null)
                {
                    id = Convert.ToInt32(line);
                    writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", id, scores1[id], scores2[id],scoresTool[id], scoresLazy[id], 
                        scores1[id] - scores2[id], scores1[id] - scoresTool[id], scores2[id] - scoresTool[id], 
                        scores1[id] - scoresLazy[id], scores2[id] - scoresLazy[id]);
                }
            }

        }

        private static void printMetrics(int prob, string grader1, string grader2)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/";
            string outFile = dir + prob + "/" + grader1 + "-" + grader2 + "-Metric-" + prob + ".txt";
            var s1 = new Dictionary<int, int>();
            var s2 = new Dictionary<int, int>();
            var sDFA = new Dictionary<int, int>();
            var sDensity = new Dictionary<int, int>();
            var sPDL = new Dictionary<int, int>();

            #region grader1
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader1 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    s1.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }
            #endregion

            #region grader2
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader2 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    s2.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }
            #endregion

            #region toolInd
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/Tool-Ind-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    sDFA.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                    sDensity.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[2]));
                    sPDL.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[3]));
                }
            }
            #endregion


            using (StreamReader reader = new StreamReader(dir + prob + "/clean.txt"))
            using (StreamWriter writer = new StreamWriter(outFile, false))
            {
                string line; int id;
                writer.WriteLine("eventId,{0},{1},DFAEd,Density,PDLEd,DFA+Density,DFA+PDLed", grader1, grader2);
                while ((line = reader.ReadLine()) != null)
                {
                    id = Convert.ToInt32(line);
                    writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", id, s1[id], s2[id], sDFA[id], sDensity[id], sPDL[id],
                        Math.Max(sDFA[id], sDensity[id]), Math.Max(sDFA[id], sPDL[id]));
                }
            }

        }



        // print id,g1,g2,0.5(g1+g2),g3
        private static void printScores(int prob, string grader1, string grader2, string grader3)
        {
            string dir = "C:/Users/Dileep/Desktop/Attempts/";
            string outFile = dir + prob + "/" + grader1 + "-" + grader2 + "-" + grader3 + "-" + prob + ".txt";
            var scores1 = new Dictionary<int, int>();
            var scores2 = new Dictionary<int, int>();
            var scores3 = new Dictionary<int, int>();
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader1 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scores1.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader2 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scores2.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }
            using (TextFieldParser parser = new TextFieldParser(dir + prob + "/" + grader3 + "-" + prob + ".txt"))
            {
                parser.Delimiters = new string[] { "," };
                while (true)
                {
                    string[] parts = parser.ReadFields();
                    if (parts == null)
                        break;
                    scores3.Add(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                }
                //read questions from clean.txt and output those in that order
            }


            using (StreamReader reader = new StreamReader(dir + prob + "/clean.txt"))
            using (StreamWriter writer = new StreamWriter(outFile, false))
            {
                string line; int id;
                writer.WriteLine("eventId,{0},{1},Average,{2}", grader1, grader2,grader3);
                while ((line = reader.ReadLine()) != null)
                {
                    id = Convert.ToInt32(line);
                    writer.WriteLine("{0},{1},{2},{3},{4}", id, scores1[id], scores2[id],0.5*(scores1[id]+scores2[id]),scores3[id]);
                }
            }

        }

        //Cleaning Attempts of isomorphic consecutive submission
        //for each user if consecutively numbered mappedevents are IsEventEqual then keep a running max, when no next print
        private static int Clean()
        {
            // for each problem
            //  for each user
            //   look at file names in ascending int order
            //    (if the current != last ( save last) ) update last

            int count = 0;

            string dir = "C:/Users/Dileep/Desktop/Attempts";
            foreach(string p in Directory.GetDirectories(dir))
            {
                var cleanWriter = new StreamWriter(p + "/clean.txt", false);

                foreach (string u in Directory.GetDirectories(p))
                {
                    string[] att = Directory.GetFiles(u);
                    int[] attInt = att.Select(path => Convert.ToInt32(Path.GetFileNameWithoutExtension(path))).ToArray();
                    Array.Sort(attInt); 
                    int? last = null;

                    foreach (int current in attInt)
                    {
                        if (last != null)
                        {
                            if (current == last + 1 && DFAUtilities.IsEventEqual(u + "/" + current + ".xml", u + "/" + last + ".xml"))
                            {
                                //dont save last
                            }
                            else
                            {
                                //save last
                                cleanWriter.WriteLine(last);
                                count++;
                            }
                        }
                        last = current;
                    }
                    if (last != null)
                    {
                        cleanWriter.WriteLine(last);
                        count++;
                    }
                }

                cleanWriter.Close();
            }

            return count;

        }


        //create dotfiles for all attempts
        private static void CreateDotFiles(int probId)
        {
            string fromDir = "C:/Users/Dileep/Desktop/Attempts/" + probId + "/";
            string toDir = "C:/Users/Dileep/Desktop/Graded/" + probId + "/";
            Directory.CreateDirectory(toDir);
            var files = Directory.GetFiles(fromDir, "*.xml", System.IO.SearchOption.AllDirectories);
            string outputFile;

            foreach (string inputFile in files)
            {
                outputFile = toDir + Path.GetFileNameWithoutExtension(inputFile);
                var solver = new CharSetSolver(BitWidth.BV64);

                try
                {
                    var aut = DFAUtilities.parseDFAfromEvent(inputFile, solver);
                    solver.SaveAsDot(aut.Second, "second", outputFile);
                }
                catch (System.FormatException e)
                {
                    Console.WriteLine("EXCEPTION: {0}", e);
                    Console.WriteLine("Failed: {0}", Path.GetFileNameWithoutExtension(inputFile));
                }

            }
        }

        private static int GradeTest(int probId, long timeout)
        {
            string fromDir = "C:/Users/Dileep/Desktop/Attempts/" + probId + "/";
            string toDir = "C:/Users/Dileep/Desktop/Graded/" + probId + "/";
            Directory.CreateDirectory(toDir);
            var files = Directory.GetFiles(fromDir, "*.xml", System.IO.SearchOption.AllDirectories);
            string outputFile;
            string ansFile = "C:/Users/Dileep/Desktop/Answers/" + "ans-" + probId + ".xml";

            var solver = new CharSetSolver(BitWidth.BV64);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var ans_aut = DFAUtilities.parseDFAfromTutor(ansFile, solver);
            var dfa_correct = ans_aut.Second;

            TextWriter tmp = Console.Out;

            var count = 0;

            foreach (string inputFile in files)
            {
                outputFile = toDir + Path.GetFileNameWithoutExtension(inputFile) + "-grade.txt";

                var aut = DFAUtilities.parseDFAfromEvent(inputFile, solver);
                var dfa = aut.Second;

                //var writer = new StreamWriter(outputFile, false);
                //Console.SetOut(writer);
                var gradeFeedback = Grading.GetGrade(dfa_correct, dfa, al, solver, timeout, 10);
                var grade = gradeFeedback.First;
                var feedback = gradeFeedback.Second;
                if (grade < 10)
                    count++;
                Console.WriteLine("Wrong: #{0}", count);

                //writer.Close();
            }

            return count;

            //Console.SetOut(tmp);

        }
    }
}
