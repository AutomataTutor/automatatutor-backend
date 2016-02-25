using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

using System.Diagnostics;
using System.Threading;

using Microsoft.Automata;
using Microsoft.Z3;
using AutomataPDL;

namespace TestPDL
{
    [TestClass]
    public class GradingTest
    {
        static int timeout = 2000;

        [TestMethod]
        public void GradingTest1()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, a));
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 1, b));
            moves.Add(new Move<BDD>(1, 2, a));
            moves.Add(new Move<BDD>(2, 2, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 3, b));
            moves.Add(new Move<BDD>(3, 4, a));
            moves.Add(new Move<BDD>(4, 4, a));
            moves.Add(new Move<BDD>(4, 5, b));
            moves.Add(new Move<BDD>(5, 6, a));
            moves.Add(new Move<BDD>(5, 5, b));
            moves.Add(new Move<BDD>(6, 6, a));
            moves.Add(new Move<BDD>(6, 6, b));

            var dfa1 = Automaton<BDD>.Create(0, new int[] { 4,5 }, moves);

            var moves1 = new List<Move<BDD>>();

            moves1.Add(new Move<BDD>(0, 0, a));
            moves1.Add(new Move<BDD>(0, 1, b));
            moves1.Add(new Move<BDD>(1, 1, b));
            moves1.Add(new Move<BDD>(1, 2, a));
            moves1.Add(new Move<BDD>(2, 2, a));
            moves1.Add(new Move<BDD>(2, 3, b));
            moves1.Add(new Move<BDD>(3, 3, b));
            moves1.Add(new Move<BDD>(3, 4, a));
            moves1.Add(new Move<BDD>(4, 4, a));
            moves1.Add(new Move<BDD>(4, 4, b));

            var dfa2 = Automaton<BDD>.Create(0, new int[] { 4 }, moves1);         

            var v1 = DFAGrading.GetGrade(dfa1, dfa2, al, solver, timeout, 10, FeedbackLevel.Hint);
            

            Console.WriteLine("grade0: {0}, ", v1);
        }

        [TestMethod]
        public void DileepTest1()
        {
            PDLEnumerator pdlEnumerator = new PDLEnumerator();
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLModSetEq(new PDLIndicesOf("a"), 2, 1);
            phi = new PDLAnd(new PDLStartsWith("a"), phi);
            var dfa1 = phi.GetDFA(al, solver);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, a));
            moves.Add(new Move<BDD>(0, 5, a));
            moves.Add(new Move<BDD>(5, 0, a));
            moves.Add(new Move<BDD>(5, 5, b));

            var dfa2 = Automaton<BDD>.Create(0, new int[] { 5 }, moves);
            var feedbackGrade = DFAGrading.GetGrade(dfa1, dfa2, al, solver, timeout, 10, FeedbackLevel.Solution, true, false, false);
            var feedString = "<ul>";
            foreach (var feed in feedbackGrade.Second)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";

            Console.Write( string.Format("<div>Grade: {0} <br /> Feedback: {1}</div>", feedbackGrade.First, feedString));
        }

        [TestMethod]
        public void LorisTest()
        {
            HashSet<char> al = new HashSet<char>(new char[] { 'a', 'b' });
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            string rexpr1 = "(a|b)*";

            var escapedRexpr = string.Format("^({0})$", rexpr1);
            Automaton<BDD> aut1 = null;
            try
            {
                aut1 = solver.Convert(escapedRexpr);
            }
            catch (ArgumentException e)
            {
                throw new PDLException("The input is not a well formatted regular expression.\n" + e.ToString());
            }
            catch (AutomataException e)
            {
                throw new PDLException("The input is not a well formatted regular expression.\n" + e.ToString());
            }

            var diff = aut1.Minus(solver.Convert("^(a|b)*$"), solver);
            if (!diff.IsEmpty)
                throw new PDLException("The regular expression should only accept strings over (a|b)*.");

            string rexpr2 = "(a|b)+";

            escapedRexpr = string.Format("^({0})$", rexpr2);
            Automaton<BDD> aut2 = null;
            try
            {
                aut2 = solver.Convert(escapedRexpr);
            }
            catch (ArgumentException e)
            {
                throw new PDLException("The input is not a well formatted regular expression.\n" + e.ToString());
            }
            catch (AutomataException e)
            {
                throw new PDLException("The input is not a well formatted regular expression.\n" + e.ToString());
            }

            diff = aut2.Minus(solver.Convert("^(a|b)*$"), solver);
            if (!diff.IsEmpty)
                throw new PDLException("The regular expression should only accept strings over (a|b)*.");


            var feedbackGrade = DFAGrading.GetGrade(aut1, aut2, al, solver, 2000, 10, FeedbackLevel.Solution, false, true, true);

            var feedString = "<ul>";
            foreach (var feed in feedbackGrade.Second)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";

            Console.WriteLine(string.Format("<div>Grade: {0} <br /> Feedback: {1}</div>", feedbackGrade.First, feedString));
        }

        [TestMethod]
        public void DileepTest2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLIntGeq(new PDLIndicesOf("ba"), 3);
            var dfaCorr = phi.GetDFA(al, solver);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 0, a));
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 2, a));
            moves.Add(new Move<BDD>(1, 0, b));
            moves.Add(new Move<BDD>(2, 2, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3, 4, a));
            moves.Add(new Move<BDD>(3, 2, b));
            moves.Add(new Move<BDD>(4, 4, a));
            moves.Add(new Move<BDD>(4, 5, b));
            moves.Add(new Move<BDD>(5, 6, a));
            moves.Add(new Move<BDD>(5, 4, b));
            moves.Add(new Move<BDD>(6, 6, a));
            moves.Add(new Move<BDD>(6, 6, b)); 

            var dfa2 = Automaton<BDD>.Create(0, new int[] { 4,5 }, moves);

            solver.SaveAsDot(dfaCorr, "aa", "trytrycorr");
            solver.SaveAsDot(dfa2, "aa", "trytry");

            //var v0 = DFADensity.GetDFADifferenceRatio(dfa1, dfa2, al, solver);
            //var v1 = PDLEditDistance.GetMinimalFormulaEditDistanceRatio(dfa1, dfa2, al, solver, timeout);
            //var v2 = DFAEditDistance.GetDFAOptimalEdit(dfa1, dfa2, al, solver, 4, new StringBuilder());
            //Console.WriteLine("density ratio: {0}; pdl edit distance: {1}; dfa edit distance: {2}", v0, v1, v2);

            var gr = DFAGrading.GetGrade(dfaCorr, dfa2, al, solver, 2000,10,FeedbackLevel.Hint,true,true,true);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }

        [TestMethod]
        public void DileepTest3()
        {
            //XElement dfaCorrectDesc = XElement.Parse("<automaton xmlns=\"http://automatagrader.com/\">        <alphabet type=\"basic\">      <symbol>a</symbol><symbol>b</symbol>      </alphabet>        <stateSet>          <state sid=\"0\"><label>0</label></state><state sid=\"1\"><label>1</label></state>        </stateSet>        <transitionSet>          <transition tid=\"0\">                          <from>0</from>                          <to>0</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"1\">                          <from>0</from>                          <to>1</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"2\">                          <from>1</from>                          <to>1</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"3\">                          <from>1</from>                          <to>0</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition>        </transitionSet>        <acceptingSet>          <state sid=\"1\"></state>        </acceptingSet>        <initState>          <state sid=\"0\"></state>        </initState>      </automaton>");
            XElement dfaCorrectDesc = XElement.Parse("<automaton xmlns=\"http://automatagrader.com/\">        <alphabet type=\"basic\">      <symbol>a</symbol><symbol>b</symbol>      </alphabet>        <stateSet>          <state sid=\"0\"><label>0</label></state><state sid=\"1\"><label>1</label></state><state sid=\"2\"><label>2</label></state><state sid=\"3\"><label>3</label></state><state sid=\"4\"><label>4</label></state><state sid=\"5\"><label>5</label></state><state sid=\"6\"><label>6</label></state>        </stateSet>        <transitionSet>          <transition tid=\"0\">                          <from>0</from>                          <to>0</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"1\">                          <from>0</from>                          <to>1</to>                          <read>a</read>                          <edgeDistance>0</edgeDistance>                        </transition><transition tid=\"2\">                          <from>1</from>                          <to>1</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"3\">                          <from>1</from>                          <to>2</to>                          <read>b</read>                          <edgeDistance>0</edgeDistance>                        </transition><transition tid=\"4\">                          <from>2</from>                          <to>2</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"5\">                          <from>2</from>                          <to>3</to>                          <read>a</read>                          <edgeDistance>0</edgeDistance>                        </transition><transition tid=\"6\">                          <from>3</from>                          <to>3</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"7\">                          <from>3</from>                          <to>4</to>                          <read>b</read>                          <edgeDistance>0</edgeDistance>                        </transition><transition tid=\"8\">                          <from>4</from>                          <to>4</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"9\">                          <from>4</from>                          <to>5</to>                          <read>a</read>                          <edgeDistance>0</edgeDistance>                        </transition><transition tid=\"10\">                          <from>5</from>                          <to>5</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"11\">                          <from>5</from>                          <to>6</to>                          <read>b</read>                          <edgeDistance>0</edgeDistance>                        </transition><transition tid=\"12\">                          <from>6</from>                          <to>6</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"13\">                          <from>6</from>                          <to>6</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition>        </transitionSet>        <acceptingSet>          <state sid=\"4\"></state><state sid=\"5\"></state>        </acceptingSet>        <initState>          <state sid=\"0\"></state>        </initState>      </automaton>");
            XElement dfaAttemptDesc = XElement.Parse("<automaton xmlns=\"http://automatagrader.com/\">        <alphabet type=\"basic\">      <symbol>a</symbol><symbol>b</symbol>      </alphabet>        <stateSet>          <state sid=\"0\"><label>0</label></state>        </stateSet>        <transitionSet>          <transition tid=\"0\">                          <from>0</from>                          <to>0</to>                          <read>a</read>                          <edgeDistance>30</edgeDistance>                        </transition><transition tid=\"1\">                          <from>0</from>                          <to>0</to>                          <read>b</read>                          <edgeDistance>30</edgeDistance>                        </transition>        </transitionSet>        <acceptingSet>                  </acceptingSet>        <initState>          <state sid=\"0\"></state>        </initState>      </automaton>");
            XElement feedbackLevel = XElement.Parse("<level xmlns=\"http://automatagrader.com/\">smallhint</level>");
            XElement enabledFeedbacks = XElement.Parse("<metrics xmlns=\"http://automatagrader.com/\">dfaedit,moseledit,density</metrics>");
        
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            //XElement dfaAttemptDesc = dfaAttemptHint.Element("Automaton");
            //FeedbackLevel level = (FeedbackLevel)Convert.ToInt32(dfaAttemptHint.Element("FeedbackLevel").Value);

            var dfaCorrectPair = DFAUtilities.parseDFAFromXML(dfaCorrectDesc, solver);

            Console.WriteLine(feedbackLevel);
            Console.WriteLine(enabledFeedbacks);

            var dfaAttemptPair = DFAUtilities.parseDFAFromXML(dfaAttemptDesc, solver);

            //var sb = new StringBuilder();


            var level = (FeedbackLevel) Enum.Parse(typeof(FeedbackLevel), feedbackLevel.Value, true);

            var enabList = (enabledFeedbacks.Value).Split(',').ToList<String>();

            bool dfaedit = enabList.Contains("dfaedit"), moseledit = enabList.Contains("moseledit"), density = enabList.Contains("density");



            var feedbackGrade = DFAGrading.GetGrade(dfaCorrectPair.Second, dfaAttemptPair.Second, dfaCorrectPair.First, solver, 2000, 10, level, dfaedit, moseledit, density);

            var feedString = "<ul>";
            foreach (var feed in feedbackGrade.Second)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";

            Console.WriteLine(string.Format("<div>Grade: {0} <br /> Feedback: {1}</div>", feedbackGrade.First, feedString));
        
        }

        [TestMethod]
        public void Grade2DFAs()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            PDLPred phi = new PDLAnd(new PDLIntGeq(new PDLIndicesOf("a"), 2), new PDLIntGeq(new PDLIndicesOf("b"), 2));

            //PDLPred phi2 = new PDLIf(new PDLStartsWith("b"), new PDLEndsWith("b"));
            var dfaCorr = phi.GetDFA(al, solver);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0, 1, a));
            moves.Add(new Move<BDD>(0, 1, b));
            moves.Add(new Move<BDD>(1, 2, a));
            moves.Add(new Move<BDD>(1, 2, b));
            moves.Add(new Move<BDD>(2, 3, a));
            moves.Add(new Move<BDD>(2, 3, b));
            moves.Add(new Move<BDD>(3,3, a));
            moves.Add(new Move<BDD>(3, 3, b));
            //moves.Add(new Move<BDD>(3, 4, a));
            //moves.Add(new Move<BDD>(3, 2, b));
            //moves.Add(new Move<BDD>(4, 4, a));
            //moves.Add(new Move<BDD>(4, 5, b));
            //moves.Add(new Move<BDD>(5, 6, a));
            //moves.Add(new Move<BDD>(5, 4, b));
            //moves.Add(new Move<BDD>(6, 6, a));
            //moves.Add(new Move<BDD>(6, 6, b));

            var dfa2 = Automaton<BDD>.Create(0, new int[] { 3 }, moves);            
            //Assert.IsTrue(phi2.GetDFA(al,solver).IsEquivalentWith(dfa2,solver));

            solver.SaveAsDot(dfaCorr, "aa", "corr");
            solver.SaveAsDot(dfa2, "aa", "wrong");

            //var v0 = DFADensity.GetDFADifferenceRatio(dfa1, dfa2, al, solver);
            //var v1 = PDLEditDistance.GetMinimalFormulaEditDistanceRatio(dfa1, dfa2, al, solver, timeout);
            //var v2 = DFAEditDistance.GetDFAOptimalEdit(dfa1, dfa2, al, solver, 4, new StringBuilder());
            //Console.WriteLine("density ratio: {0}; pdl edit distance: {1}; dfa edit distance: {2}", v0, v1, v2);

            var gr = DFAGrading.GetGrade(dfaCorr, dfa2, al, solver, 2000, 10, FeedbackLevel.Hint, true, true, true);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }

        [TestMethod]
        public void VladKlimkivDFAs()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            //PDLPred phi2 = new PDLIf(new PDLStartsWith("b"), new PDLEndsWith("b"));



            var movesSolution = new List<Move<BDD>>();

            movesSolution.Add(new Move<BDD>(0, 1, a));
            movesSolution.Add(new Move<BDD>(0, 2, b));
            movesSolution.Add(new Move<BDD>(1, 0, a));
            movesSolution.Add(new Move<BDD>(1, 3, b));
            movesSolution.Add(new Move<BDD>(2, 3, a));
            movesSolution.Add(new Move<BDD>(2, 5, b));
            movesSolution.Add(new Move<BDD>(3, 2, a));
            movesSolution.Add(new Move<BDD>(3, 4, b));
            movesSolution.Add(new Move<BDD>(4, 5, a));
            movesSolution.Add(new Move<BDD>(4, 1, b));
            movesSolution.Add(new Move<BDD>(5, 4, a));
            movesSolution.Add(new Move<BDD>(5, 1, b));


            var dfaSolution = Automaton<BDD>.Create(0, new int[] { 2 }, movesSolution);

            var movesAttempt = new List<Move<BDD>>();

            movesAttempt.Add(new Move<BDD>(0, 1, b));
            movesAttempt.Add(new Move<BDD>(0, 2, a));
            movesAttempt.Add(new Move<BDD>(1, 0, b));
            movesAttempt.Add(new Move<BDD>(1, 3, a));
            movesAttempt.Add(new Move<BDD>(2, 3, b));
            movesAttempt.Add(new Move<BDD>(2, 5, a));
            movesAttempt.Add(new Move<BDD>(3, 2, b));
            movesAttempt.Add(new Move<BDD>(3, 4, a));
            movesAttempt.Add(new Move<BDD>(4, 5, b));
            movesAttempt.Add(new Move<BDD>(4, 1, a));
            movesAttempt.Add(new Move<BDD>(5, 4, b));
            movesAttempt.Add(new Move<BDD>(5, 1, a));
            var dfaAttempt = Automaton<BDD>.Create(0, new int[] { 2 }, movesAttempt);

            var gr = DFAGrading.GetGrade(dfaSolution, dfaAttempt, al, solver, 1500, 10, FeedbackLevel.Hint, true, true, true);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

            gr = DFAGrading.GetGrade(dfaAttempt, dfaSolution, al, solver, 1500, 10, FeedbackLevel.Hint, true, true, true);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }

        [TestMethod]
        public void MarioBianucciNFAs()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, '0');
            var b = solver.MkCharConstraint(false, '1');

            //PDLPred phi2 = new PDLIf(new PDLStartsWith("b"), new PDLEndsWith("b"));



            var movesSolution = new List<Move<BDD>>();

            movesSolution.Add(new Move<BDD>(0, 0, a));
            movesSolution.Add(new Move<BDD>(0, 1, b));
            movesSolution.Add(new Move<BDD>(1, 2, a));
            movesSolution.Add(new Move<BDD>(1, 1, b));
            movesSolution.Add(new Move<BDD>(2, 2, a));
            movesSolution.Add(new Move<BDD>(2, 3, b));
            movesSolution.Add(new Move<BDD>(3, 0, a));
            movesSolution.Add(new Move<BDD>(3, 3, b));


            var dfaSolution = Automaton<BDD>.Create(0, new int[] {0,1 }, movesSolution);

            var movesAttempt = new List<Move<BDD>>();

            movesAttempt.Add(new Move<BDD>(0, 0, a));
            movesAttempt.Add(new Move<BDD>(0, 1, b));
            movesAttempt.Add(new Move<BDD>(0, 1, null));
            movesAttempt.Add(new Move<BDD>(1, 2, a));
            movesAttempt.Add(new Move<BDD>(1, 1, b));
            movesAttempt.Add(new Move<BDD>(2, 2, a));
            movesAttempt.Add(new Move<BDD>(2, 3, b));
            movesAttempt.Add(new Move<BDD>(3, 0, a));
            movesAttempt.Add(new Move<BDD>(3, 3, b));
            var dfaAttempt = Automaton<BDD>.Create(0, new int[] { 0,1 }, movesAttempt);

            var gr = NFAGrading.GetGrade(dfaSolution, dfaAttempt, al, solver, 4000, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

            gr = NFAGrading.GetGrade(dfaAttempt, dfaSolution, al, solver, 4000, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }

        [TestMethod]
        public void AgutssonNFAs()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            var movesSolution = new List<Move<BDD>>();

            movesSolution.Add(new Move<BDD>(0, 1, a));
            movesSolution.Add(new Move<BDD>(1, 2, a));
            movesSolution.Add(new Move<BDD>(0, 3, a));
            movesSolution.Add(new Move<BDD>(0, 3, b));
            movesSolution.Add(new Move<BDD>(3, 0, a));
            movesSolution.Add(new Move<BDD>(3, 0, b));


            var dfaSolution = Automaton<BDD>.Create(0, new int[] { 2 }, movesSolution);

            var movesAttempt = new List<Move<BDD>>();

            movesAttempt.Add(new Move<BDD>(0, 0, a));
            movesAttempt.Add(new Move<BDD>(0, 0, b));
            movesAttempt.Add(new Move<BDD>(0, 1, a));
            movesAttempt.Add(new Move<BDD>(1, 2, a));
            movesAttempt.Add(new Move<BDD>(2, 3, a));
            var dfaAttempt = Automaton<BDD>.Create(0, new int[] { 3 }, movesAttempt);

            var gr = NFAGrading.GetGrade(dfaSolution, dfaAttempt, al, solver, 1500, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

            gr = NFAGrading.GetGrade(dfaAttempt, dfaSolution, al, solver, 1500, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }

        [TestMethod]
        public void GradingNFANoEps()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var movesSol = new List<Move<BDD>>();

            movesSol.Add(new Move<BDD>(0, 0, a));
            movesSol.Add(new Move<BDD>(0, 0, b));
            movesSol.Add(new Move<BDD>(0, 1, a));
            movesSol.Add(new Move<BDD>(1, 2, b));

            var nfa1 = Automaton<BDD>.Create(0, new int[] { 2 }, movesSol);

            var movesAtt = new List<Move<BDD>>();

            movesAtt.Add(new Move<BDD>(0, 0, a));
            movesAtt.Add(new Move<BDD>(0, 1, b));
            movesAtt.Add(new Move<BDD>(1, 1, b));
            movesAtt.Add(new Move<BDD>(1, 2, a));
            movesAtt.Add(new Move<BDD>(2, 2, a));
            movesAtt.Add(new Move<BDD>(2, 3, b));
            movesAtt.Add(new Move<BDD>(3, 3, b));
            movesAtt.Add(new Move<BDD>(3, 4, a));
            movesAtt.Add(new Move<BDD>(4, 4, a));
            movesAtt.Add(new Move<BDD>(4, 4, b));

            var nfa2 = Automaton<BDD>.Create(0, new int[] { 4 }, movesAtt);

            List<Pair<int, IEnumerable<NFAFeedback>>> tests = new List<Pair<int, IEnumerable<NFAFeedback>>>();
            tests.Insert(0, NFAGrading.GetGrade(nfa1, nfa2, al, solver, timeout, 10, FeedbackLevel.Hint));
            tests.Insert(0, NFAGrading.GetGrade(nfa2, nfa1, al, solver, timeout, 10, FeedbackLevel.Hint));
            tests.Insert(0, NFAGrading.GetGrade(nfa1, nfa1, al, solver, timeout, 10, FeedbackLevel.Hint));
            tests.Insert(0, NFAGrading.GetGrade(nfa2, nfa2, al, solver, timeout, 10, FeedbackLevel.Hint));

            foreach (var test in tests)
            {
                Console.WriteLine("grade: {0}, ", test.First);
                foreach (var f in test.Second)
                    Console.WriteLine(f.ToString());
            }
        }

        [TestMethod]
        public void GradingNFAEps()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var movesSol = new List<Move<BDD>>();

            movesSol.Add(new Move<BDD>(5, 0, null));
            movesSol.Add(new Move<BDD>(0, 0, a));
            movesSol.Add(new Move<BDD>(0, 0, b));
            movesSol.Add(new Move<BDD>(0, 1, a));
            movesSol.Add(new Move<BDD>(1, 2, b));

            var nfa1 = Automaton<BDD>.Create(5, new int[] { 2 }, movesSol);

            var movesAtt = new List<Move<BDD>>();

            movesAtt.Add(new Move<BDD>(0, 0, a));
            movesAtt.Add(new Move<BDD>(0, 1, b));
            movesAtt.Add(new Move<BDD>(1, 1, b));
            movesAtt.Add(new Move<BDD>(1, 2, a));
            movesAtt.Add(new Move<BDD>(2, 2, a));
            movesAtt.Add(new Move<BDD>(2, 3, b));
            movesAtt.Add(new Move<BDD>(3, 3, b));
            movesAtt.Add(new Move<BDD>(3, 4, a));
            movesAtt.Add(new Move<BDD>(4, 4, a));
            movesAtt.Add(new Move<BDD>(4, 4, b));

            var nfa2 = Automaton<BDD>.Create(0, new int[] { 4 }, movesAtt);

            List<Pair<int, IEnumerable<NFAFeedback>>> tests = new List<Pair<int, IEnumerable<NFAFeedback>>>();
            tests.Insert(0,NFAGrading.GetGrade(nfa1, nfa2, al, solver, timeout, 10, FeedbackLevel.Hint));
            tests.Insert(0, NFAGrading.GetGrade(nfa2, nfa1, al, solver, timeout, 10, FeedbackLevel.Hint));
            tests.Insert(0, NFAGrading.GetGrade(nfa1, nfa1, al, solver, timeout, 10, FeedbackLevel.Hint));
            tests.Insert(0, NFAGrading.GetGrade(nfa2, nfa2, al, solver, timeout, 10, FeedbackLevel.Hint));
            
            foreach (var test in tests)
            {
                Console.WriteLine("grade: {0}, ", test.First);
                foreach (var f in test.Second)
                    Console.WriteLine(f.ToString());
            }
        }

        [TestMethod]
        public void FlabioNFAs()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { 'G', 'O', 'L', 'E', 'X'  };
            HashSet<char> al = new HashSet<char>(alph);

            var g = solver.MkCharConstraint(false, 'G');
            var o = solver.MkCharConstraint(false, 'O');
            var l = solver.MkCharConstraint(false, 'L');
            var e = solver.MkCharConstraint(false, 'E');

            var movesSolution = new List<Move<BDD>>();

            movesSolution.Add(new Move<BDD>(0, 1, g));
            movesSolution.Add(new Move<BDD>(1, 1, o));
            movesSolution.Add(new Move<BDD>(1, 2, g));
            movesSolution.Add(new Move<BDD>(2, 3, l));
            movesSolution.Add(new Move<BDD>(3, 4, e));            


            var dfaSolution = Automaton<BDD>.Create(0, new int[] { 4 }, movesSolution);

            var movesAttempt = new List<Move<BDD>>();

            movesAttempt.Add(new Move<BDD>(0, 1, g));
            movesAttempt.Add(new Move<BDD>(1, 2, null));
            movesAttempt.Add(new Move<BDD>(2, 2, o));
            movesAttempt.Add(new Move<BDD>(2, 3, g));
            movesAttempt.Add(new Move<BDD>(3, 4, l));
            movesAttempt.Add(new Move<BDD>(4, 5, e));
            var dfaAttempt = Automaton<BDD>.Create(0, new int[] { 5 }, movesAttempt);

            var gr = NFAGrading.GetGrade(dfaSolution, dfaAttempt, al, solver, 1500, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

            gr = NFAGrading.GetGrade(dfaAttempt, dfaSolution, al, solver, 1500, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }

        [TestMethod]
        public void FlabioNFA2()
        {
            var solver = new CharSetSolver(BitWidth.BV64);
            List<char> alph = new List<char> { '1', '0' };
            HashSet<char> al = new HashSet<char>(alph);

            var o = solver.MkCharConstraint(false, '1');
            var z = solver.MkCharConstraint(false, '0');            

            var movesSolution = new List<Move<BDD>>();

            movesSolution.Add(new Move<BDD>(0, 1, null));
            movesSolution.Add(new Move<BDD>(0, 2, null));
            movesSolution.Add(new Move<BDD>(1, 1, z));
            movesSolution.Add(new Move<BDD>(1, 1, o));
            movesSolution.Add(new Move<BDD>(1, 3, z));
            movesSolution.Add(new Move<BDD>(2, 2, o));


            var dfaSolution = Automaton<BDD>.Create(0, new int[] { 2, 3 }, movesSolution);

            var movesAttempt = new List<Move<BDD>>();

            movesAttempt.Add(new Move<BDD>(0, 1, null));
            movesAttempt.Add(new Move<BDD>(0, 3, null));
            movesAttempt.Add(new Move<BDD>(1, 2, z));
            movesAttempt.Add(new Move<BDD>(2, 1, o));
            movesAttempt.Add(new Move<BDD>(2, 2, z));
            movesAttempt.Add(new Move<BDD>(1, 1, o));
            movesAttempt.Add(new Move<BDD>(3, 4, o));
            movesAttempt.Add(new Move<BDD>(4, 4, o));
            var dfaAttempt = Automaton<BDD>.Create(0, new int[] { 4,2 }, movesAttempt);

            var gr = NFAGrading.GetGrade(dfaSolution, dfaAttempt, al, solver, 1500, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

            gr = NFAGrading.GetGrade(dfaAttempt, dfaSolution, al, solver, 1500, 10, FeedbackLevel.Hint);
            Console.WriteLine(gr.First);
            foreach (var f in gr.Second)
                Console.WriteLine(f.ToString());

        }
    }
}
