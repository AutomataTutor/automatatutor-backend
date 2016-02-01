using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;
using AutomataPDL;



namespace TestPDL
{
    [TestClass]
    public class DFAExamples
    {
        [TestMethod]
        public void TestMethod1()
        {
            var solver = new CharSetSolver(CharacterEncoding.Unicode);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<CharSet>>();

            moves.Add(new Move<CharSet>(0, 0, a));
            moves.Add(new Move<CharSet>(0, 1, b));
            moves.Add(new Move<CharSet>(1, 1, b));
            moves.Add(new Move<CharSet>(1, 2, a));
            moves.Add(new Move<CharSet>(2, 1, a));
            moves.Add(new Move<CharSet>(2, 1, b));

            var dfa  = Automaton<CharSet>.Create(0, new int[] {2}, moves).Determinize(solver).Minimize(solver);
            var sb = new StringBuilder();

            DFAUtilities.printDFA(dfa, al, sb);

            System.Console.WriteLine(sb);

        }

        [TestMethod]
        public void TextP46()
        {
            var solver = new CharSetSolver(CharacterEncoding.Unicode);
            List<char> alph = new List<char> { 'a', 'b' };
            HashSet<char> al = new HashSet<char>(alph);

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');
            var moves = new List<Move<CharSet>>();

            moves.Add(new Move<CharSet>(0, 0, b));
            moves.Add(new Move<CharSet>(0, 1, a));
            moves.Add(new Move<CharSet>(1, 1, a));
            moves.Add(new Move<CharSet>(1, 2, b));
            moves.Add(new Move<CharSet>(2, 2, a));
            moves.Add(new Move<CharSet>(2, 2, b));

            var dfa = Automaton<CharSet>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);
            var sb = new StringBuilder();

            DFAUtilities.printDFA(dfa, al, sb);

            System.Console.WriteLine(sb);
        }

        [TestMethod]
        public void TextP48()
        {
            var solver = new CharSetSolver(CharacterEncoding.Unicode);
            var alph = new List<char> { 'a', 'b' };
            var al = new HashSet<char>(alph);

            var moves = new List<Move<CharSet>>();

            var a = solver.MkCharConstraint(false, 'a');
            var b = solver.MkCharConstraint(false, 'b');

            moves.Add(new Move<CharSet>(0, 1, a));
            moves.Add(new Move<CharSet>(0, 4, b));
            moves.Add(new Move<CharSet>(1, 4, a));
            moves.Add(new Move<CharSet>(1, 2, b));
            moves.Add(new Move<CharSet>(2, 3, a));
            moves.Add(new Move<CharSet>(2, 3, b));
            moves.Add(new Move<CharSet>(3, 2, a));
            moves.Add(new Move<CharSet>(3, 2, b));
            moves.Add(new Move<CharSet>(4, 4, a));
            moves.Add(new Move<CharSet>(4, 4, b));

            var dfa = Automaton<CharSet>.Create(0, new int[] { 2 }, moves).Determinize(solver).Minimize(solver);
            var sb = new StringBuilder();

            DFAUtilities.printDFA(dfa, al, sb);
            System.Console.WriteLine(sb);
            
        }


    }
}
