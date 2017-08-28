using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Z3;
using MSOZ3;
using AutomataPDL;
using AutomataPDL.Automata;
using AutomataPDL.Utilities;

using System.Diagnostics;
using System.Threading;
using System.IO;

namespace TestPDL
{
    [TestClass]
    public class AutomataTest
    {

        [TestMethod]
        public void TestRandomStuff()
        {
            bool[] b = new bool[13];
        }

        [TestMethod]
        public void FirstTest()
        {
            Tuple<int, int> t1 = new Tuple<int, int>(0, 0);
            Tuple<int, int> t2 = new Tuple<int, int>(0, 0);

            bool b = t1 == t2;
            bool c = t1.Equals(t2);

            HashSet<Tuple<int, int>> h = new HashSet<Tuple<int, int>>();
            h.Add(t1);
            bool d = h.Contains(t2);
        }

        [TestMethod]
        public void TestDFAAcceptance()
        {
            HashSet<State<int>> Q = new HashSet<State<int>>();
            State<int> q_0 = new State<int>(0, 0);
            State<int> q_1 = new State<int>(1, 1);
            State<int> q_2 = new State<int>(2, 2);
            Q.Add(q_0);
            Q.Add(q_1);
            Q.Add(q_2);

            HashSet<char> Sigma = new HashSet<char>();
            Sigma.Add('a');
            Sigma.Add('b');

            Dictionary<TwoTuple<State<int>, char>, State<int>> delta = new Dictionary<TwoTuple<State<int>, char>, State<int>>();
            delta.Add(new TwoTuple<State<int>, char>(q_0, 'a'), q_1);
            delta.Add(new TwoTuple<State<int>, char>(q_1, 'a'), q_2);
            delta.Add(new TwoTuple<State<int>, char>(q_2, 'a'), q_0);
            delta.Add(new TwoTuple<State<int>, char>(q_0, 'b'), q_0);
            delta.Add(new TwoTuple<State<int>, char>(q_1, 'b'), q_1);
            delta.Add(new TwoTuple<State<int>, char>(q_2, 'b'), q_2);

            HashSet<State<int>> F = new HashSet<State<int>>();
            F.Add(q_0);

            DFA<char, int> D = new DFA<char, int>(Q, Sigma, delta, q_0, F);

            List<char> w = new List<char>();
            w.Add('a');
            w.Add('a');
            w.Add('a');

            List<char> u = new List<char>();
            u.Add('b');
            u.Add('a');
            u.Add('a');
            u.Add('b');
            u.Add('a');

            List<char> v = new List<char>();
            v.Add('a');
            v.Add('a');
            v.Add('b');

            bool b = D.Accepts(w);
            bool c = D.Accepts(u);
            bool d = D.Accepts(v);

            bool just_to_be_sure = true;

            List<char> x;
            int n = Sigma.Count;
            for (int i = 0; i < 7; i++)
            {
                x = new List<char>();
                for (int j = 0; j < i; j++)
                {
                    x.Add(Sigma.ElementAt(0));
                }

                bool at_last_word = false;
                while (!at_last_word)
                {
                    int a_count = 0;
                    for (int k = 0; k < x.Count; k++)
                    {
                        if (x.ElementAt(k) == 'a')
                            a_count++;
                    }

                    bool b1 = a_count % 3 == 0;
                    bool b2 = D.Accepts(x);
                    Assert.IsTrue((b1 && b2) || (!b1 && !b2));
                    if (!((b1 && b2) || (!b1 && !b2)))
                        just_to_be_sure = false;

                    bool change = true;
                    int pos_from_back = 1;
                    while (change)
                    {
                        if (pos_from_back > i)
                        {
                            change = false;
                            at_last_word = true;
                            break;
                        }
                        else if (x.ElementAt(i - pos_from_back) == 'a')
                        {
                            x.RemoveAt(i - pos_from_back);
                            x.Insert(i - pos_from_back, 'b');
                            change = false;
                        }
                        else
                        {
                            pos_from_back++;
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestDFAProduct()
        {
            //DFA 1
            HashSet<State<int>> Q1 = new HashSet<State<int>>();
            State<int> q_01 = new State<int>(0, 0);
            State<int> q_11 = new State<int>(1, 1);
            State<int> q_21 = new State<int>(2, 2);
            Q1.Add(q_01);
            Q1.Add(q_11);
            Q1.Add(q_21);

            HashSet<char> Sigma = new HashSet<char>();
            Sigma.Add('a');
            Sigma.Add('b');

            Dictionary<TwoTuple<State<int>, char>, State<int>> delta1 = new Dictionary<TwoTuple<State<int>, char>, State<int>>();
            delta1.Add(new TwoTuple<State<int>, char>(q_01, 'a'), q_11);
            delta1.Add(new TwoTuple<State<int>, char>(q_11, 'a'), q_21);
            delta1.Add(new TwoTuple<State<int>, char>(q_21, 'a'), q_01);
            delta1.Add(new TwoTuple<State<int>, char>(q_01, 'b'), q_01);
            delta1.Add(new TwoTuple<State<int>, char>(q_11, 'b'), q_11);
            delta1.Add(new TwoTuple<State<int>, char>(q_21, 'b'), q_21);

            HashSet<State<int>> F1 = new HashSet<State<int>>();
            F1.Add(q_01);

            DFA<char, int> D1 = new DFA<char, int>(Q1, Sigma, delta1, q_01, F1);

            //DFA 2
            HashSet<State<int>> Q2 = new HashSet<State<int>>();
            State<int> q_02 = new State<int>(0, 0);
            State<int> q_12 = new State<int>(1, 1);
            State<int> q_22 = new State<int>(2, 2);
            State<int> q_32 = new State<int>(3, 3);
            Q2.Add(q_02);
            Q2.Add(q_12);
            Q2.Add(q_22);
            Q2.Add(q_32);

            Dictionary<TwoTuple<State<int>, char>, State<int>> delta2 = new Dictionary<TwoTuple<State<int>, char>, State<int>>();
            delta2.Add(new TwoTuple<State<int>, char>(q_02, 'a'), q_12);
            delta2.Add(new TwoTuple<State<int>, char>(q_02, 'b'), q_22);
            delta2.Add(new TwoTuple<State<int>, char>(q_12, 'a'), q_32);
            delta2.Add(new TwoTuple<State<int>, char>(q_12, 'b'), q_32);
            delta2.Add(new TwoTuple<State<int>, char>(q_22, 'a'), q_32);
            delta2.Add(new TwoTuple<State<int>, char>(q_22, 'b'), q_32);
            delta2.Add(new TwoTuple<State<int>, char>(q_32, 'a'), q_32);
            delta2.Add(new TwoTuple<State<int>, char>(q_32, 'b'), q_32);

            HashSet<State<int>> F2 = new HashSet<State<int>>();
            F2.Add(q_32);

            DFA<char, int> D2 = new DFA<char, int>(Q2, Sigma, delta2, q_02, F2);
            var automataList = new List<DFA<char, int>>();
            automataList.Add(D1);
            automataList.Add(D2);
            automataList.Add(D2);

            BooleanOperation boolOp = BooleanOperation.parseBooleanOperationFromString("0&1&2");

            var D = AutomataUtilities.ExecuteSetOperation<char, int>(automataList, boolOp);
        }

        [TestMethod]
        public void TestDFAMinimization()
        {
            HashSet<State<int>> Q = new HashSet<State<int>>();
            State<int> q_0 = new State<int>(0, 0);
            State<int> q_1 = new State<int>(1, 1);
            State<int> q_2 = new State<int>(2, 2);
            State<int> q_3 = new State<int>(3, 3);
            Q.Add(q_0);
            Q.Add(q_1);
            Q.Add(q_2);
            Q.Add(q_3);

            HashSet<char> Sigma = new HashSet<char>();
            Sigma.Add('a');
            Sigma.Add('b');

            Dictionary<TwoTuple<State<int>, char>, State<int>> delta = new Dictionary<TwoTuple<State<int>, char>, State<int>>();
            delta.Add(new TwoTuple<State<int>, char>(q_0, 'a'), q_1);
            delta.Add(new TwoTuple<State<int>, char>(q_0, 'b'), q_2);
            delta.Add(new TwoTuple<State<int>, char>(q_1, 'a'), q_3);
            delta.Add(new TwoTuple<State<int>, char>(q_1, 'b'), q_3);
            delta.Add(new TwoTuple<State<int>, char>(q_2, 'a'), q_3);
            delta.Add(new TwoTuple<State<int>, char>(q_2, 'b'), q_3);
            delta.Add(new TwoTuple<State<int>, char>(q_3, 'a'), q_3);
            delta.Add(new TwoTuple<State<int>, char>(q_3, 'b'), q_3);

            HashSet<State<int>> F = new HashSet<State<int>>();
            F.Add(q_3);

            DFA<char, int> D = new DFA<char, int>(Q, Sigma, delta, q_0, F);

            var D_min = D.MinimizeHopcroft();
        }

        [TestMethod]
        public void TestNFAtoDFA()
        {
            HashSet<State<int>> Q = new HashSet<State<int>>();
            State<int> q_0 = new State<int>(0, 0);
            State<int> q_1 = new State<int>(1, 1);
            State<int> q_2 = new State<int>(2, 2);
            State<int> q_3 = new State<int>(3, 3);
            Q.Add(q_0);
            Q.Add(q_1);
            Q.Add(q_2);
            Q.Add(q_3);

            HashSet<char> Sigma = new HashSet<char>();
            Sigma.Add('a');
            Sigma.Add('b');

            var delta = new Dictionary<TwoTuple<State<int>, char>, HashSet<State<int>>>();
            var q_0_a = new HashSet<State<int>>();
            q_0_a.Add(q_0);
            q_0_a.Add(q_1);
            var q_0_b = new HashSet<State<int>>();
            q_0_b.Add(q_0);
            var q_1_a = new HashSet<State<int>>();
            q_1_a.Add(q_2);
            var q_1_b = new HashSet<State<int>>();
            q_1_b.Add(q_2);
            var q_2_a = new HashSet<State<int>>();
            q_2_a.Add(q_3);
            var q_2_b = new HashSet<State<int>>();
            q_2_b.Add(q_3);
            var q_3_a = new HashSet<State<int>>();
            var q_3_b = new HashSet<State<int>>();

            delta.Add(new TwoTuple<State<int>, char>(q_0, 'a'), q_0_a);
            delta.Add(new TwoTuple<State<int>, char>(q_0, 'b'), q_0_b);

            delta.Add(new TwoTuple<State<int>, char>(q_1, 'a'), q_1_a);
            delta.Add(new TwoTuple<State<int>, char>(q_1, 'b'), q_1_b);

            delta.Add(new TwoTuple<State<int>, char>(q_2, 'a'), q_2_a);
            delta.Add(new TwoTuple<State<int>, char>(q_2, 'b'), q_2_b);

            delta.Add(new TwoTuple<State<int>, char>(q_3, 'a'), q_3_a);
            delta.Add(new TwoTuple<State<int>, char>(q_3, 'b'), q_3_b);

            var Q_0 = new HashSet<State<int>>();
            Q_0.Add(q_0);

            var F = new HashSet<State<int>>();
            F.Add(q_3);

            var N = new NFA<char, int>(Q, Sigma, delta, Q_0, F);

            var D = N.NFAtoDFA();
        }
    }
}