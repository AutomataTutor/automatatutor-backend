using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

using System.Diagnostics;
using System.Threading;

using AutomataPDL;

namespace TestPDL
{
    [TestClass]
    public class PDLBenchmarkTest
    {

        public const int timeout = 1000;


        //[TestMethod]
        //public void SynthTime()
        //{
        //    PDLEnumerator pdlEnumerator = new PDLEnumerator();
        //    List<string> failingCases = new List<string>();
        //    string path = @"../../../TestPDL/DFAs/";
        //    var solver = new CharSetSolver(BitWidth.BV64);
        //    System.IO.StreamReader file;
        //    Dictionary<string, Pair<HashSet<char>, Automaton<BDD>>> dfas = new Dictionary<string,Pair<HashSet<char>, Automaton<BDD>>>();
        //    PDLPred synthPhi;
        //    StringBuilder sb = new StringBuilder();

        //    int tot = 0;
        //    int passed = 0;
        //    int failed = 0;
            
        //    foreach (string nameFile in Directory.EnumerateFiles(path, "*.txt"))
        //    {
        //        tot++;

        //        file = new System.IO.StreamReader(nameFile);
        //        var dfapair = DFAUtilities.parseDFAFromString(file.ReadToEnd(), solver);
        //        file.Close();

        //        sb.AppendLine("*------------------------------------");
        //        sb.AppendLine("| "+nameFile);
        //        sb.AppendLine("|------------------------------------");

        //        synthPhi=null;
        //        foreach (var phi in pdlEnumerator.SynthesizePDL(dfapair.First, dfapair.Second, solver, sb, timeout))
        //        {
        //            synthPhi=phi;
        //            break;
        //        }
        //        sb.AppendLine("*------------------------------------");
        //        sb.AppendLine();

        //        if (synthPhi == null)
        //        {
        //            failingCases.Add(nameFile);
        //            failed++;
        //        }
        //        else
        //            passed++;

        //        dfas[nameFile]=dfapair;
        //    }

        //    Console.WriteLine("Timing Synthesis");
        //    Console.WriteLine("timeout: {0} ms", timeout);
        //    Console.WriteLine("failed: {0}, passed: {1}", failed, passed);
        //    Console.WriteLine();
        //    Console.WriteLine("Failing cases:");
        //    foreach (var f in failingCases)
        //        Console.WriteLine(f);

        //    StreamWriter sw = new StreamWriter(@"../../../TestPDL/synthResults.txt");
        //    sw.Write(sb);
        //    sw.Close();
        //}


        //[TestMethod]
        //public void SynthStates()
        //{
        //    PDLEnumerator pdlEnumerator = new PDLEnumerator();

        //    List<string> failingCases = new List<string>();
        //    string path = @"../../../TestPDL/DFAs/";
        //    var solver = new CharSetSolver(BitWidth.BV64);
        //    System.IO.StreamReader file;
        //    Dictionary<string, Pair<HashSet<char>, Automaton<BDD>>> dfas = new Dictionary<string, Pair<HashSet<char>, Automaton<BDD>>>();
        //    PDLPred synthPhi;
        //    StringBuilder sb = new StringBuilder();

        //    int tot = 0;
        //    int passed = 0;
        //    int failed = 0;
        //    string ff = "";

        //    foreach (string nameFile in Directory.EnumerateFiles(path, "*.txt"))
        //    {
        //        file = new System.IO.StreamReader(nameFile);
        //        var dfapair = DFAUtilities.parseDFAFromString(file.ReadToEnd(), solver);
        //        file.Close();


        //        List<Pair<int, Pair<PDLPred, long>>> predList = new List<Pair<int, Pair<PDLPred, long>>>();
        //        List<PDLPred> phiList = new List<PDLPred>();
        //        var al = dfapair.First;
        //        var dfa = dfapair.Second;

        //        sb.AppendLine("*------------------------------------");
        //        sb.AppendLine("| " + nameFile);
        //        sb.AppendLine("|------------------------------------");
        //        ff="";
        //        foreach (var state in dfa.States)
        //        {

        //            tot++;

        //            sb.AppendLine(string.Format("|+++++++ State {0} +++++++ {1}", state, (((dfa.GetFinalStates()).Contains(state)) ? ("FINAL") : (""))));
        //            sb.AppendLine("|");
        //            var dfaSt = Automaton<BDD>.Create(dfa.InitialState, new int[] { state }, dfa.GetMoves());
        //            dfaSt = dfaSt.Determinize(solver).Minimize(solver);

        //            synthPhi = null;
        //            foreach (var phi in pdlEnumerator.SynthesizePDL(dfapair.First, dfaSt, solver, sb, timeout))
        //            {
        //                synthPhi = phi;
        //                break;
        //            }
        //            sb.AppendLine("|");

        //            if (synthPhi == null)
        //            {
        //                if(ff=="")
        //                    ff=nameFile+"\t States: "+state;
        //                else
        //                    ff+=", "+state;
        //                failed++;
        //            }
        //            else
        //                passed++;

        //        }

        //        if(ff!="")
        //            failingCases.Add(ff);
                
        //        sb.AppendLine("*------------------------------------");
        //        sb.AppendLine();
        //        sb.AppendLine();
        //    }

        //    Console.WriteLine("MultiStates Synthesis");
        //    Console.WriteLine("timeout: {0} ms", timeout);
        //    Console.WriteLine("failed: {0}, passed: {1}", failed, passed);
        //    Console.WriteLine();
        //    Console.WriteLine("Failing cases:");
        //    foreach (var f in failingCases)
        //        Console.WriteLine(f);

        //    StreamWriter sw = new StreamWriter(@"../../../TestPDL/statesResults.txt");
        //    sw.Write(sb);
        //    sw.Close();
        //}


        //[TestMethod]
        //public void SynthRegexp()
        //{
        //    List<string> failingCases = new List<string>();
        //    string path = @"../../../TestPDL/DFAs/";
        //    var solver = new CharSetSolver(BitWidth.BV64);
        //    System.IO.StreamReader file;
        //    Dictionary<string, Pair<HashSet<char>, Automaton<BDD>>> dfas = new Dictionary<string, Pair<HashSet<char>, Automaton<BDD>>>();
        //    Regexp regexp;
        //    StringBuilder sb = new StringBuilder();

        //    int tot = 0;
        //    int passed = 0;
        //    int failed = 0;

        //    foreach (string nameFile in Directory.EnumerateFiles(path, "*.txt"))
        //    {
        //        tot++;

        //        file = new System.IO.StreamReader(nameFile);
        //        var dfapair = DFAUtilities.parseDFAFromString(file.ReadToEnd(), solver);
        //        file.Close();

        //        sb.AppendLine("*------------------------------------");
        //        sb.AppendLine("| " + nameFile);
        //        sb.AppendLine("|------------------------------------");

        //        regexp = null;
        //        foreach (var re in RegexpSynthesis.SynthesizeRegexp(dfapair.First, dfapair.Second, solver, sb, timeout))
        //        {
        //            regexp = re;
        //            break;
        //        }
        //        sb.AppendLine("*------------------------------------");
        //        sb.AppendLine();



        //        if (regexp == null)
        //        {
        //            failingCases.Add(nameFile);
        //            failed++;
        //        }
        //        else
        //            passed++;

        //        dfas[nameFile] = dfapair;
        //    }

        //    Console.WriteLine("Regexp Synthesis");
        //    Console.WriteLine("timeout: {0} ms", timeout);
        //    Console.WriteLine("failed: {0}, passed: {1}", failed, passed);
        //    Console.WriteLine();
        //    Console.WriteLine("Failing cases:");
        //    foreach (var f in failingCases)
        //        Console.WriteLine(f);


        //    StreamWriter sw = new StreamWriter(@"../../../TestPDL/regexpResults.txt");
        //    sw.Write(sb);
        //    sw.Close();
        //}
    }
}
