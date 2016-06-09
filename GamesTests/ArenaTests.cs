using Microsoft.VisualStudio.TestTools.UnitTesting;
using Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Tests
{
    [TestClass()]
    public class ArenaTests
    {
        [TestMethod()]
        public void CPreZeroTest()
        {
            var arena = new Arena();
            arena.AddPlayerZeroNode(0);
            arena.AddPlayerOneNode(1);
            arena.AddPlayerZeroNode(2);
            arena.AddEdge(0, 1);
            arena.AddEdge(1, 2);
            arena.AddEdge(1, 1);
            arena.AddEdge(2, 0);

            var zeroSet = new HashSet<int>();
            zeroSet.Add(0);

            var twoSet = new HashSet<int>();
            twoSet.Add(2);

            var twoZeroSet = new HashSet<int>();
            twoZeroSet.Add(0);
            twoZeroSet.Add(2);

            Assert.IsTrue(arena.CPreZero(twoSet).Count == 0);
            Assert.IsTrue(arena.CPreZero(twoZeroSet).Count == 1 && arena.CPreZero(twoZeroSet).Contains(2));
        }

        [TestMethod()]
        public void CPreOneTest()
        {
            var arena = new Arena();
            arena.AddPlayerZeroNode(0);
            arena.AddPlayerOneNode(1);
            arena.AddPlayerZeroNode(2);
            arena.AddEdge(0, 1);
            arena.AddEdge(1, 2);
            arena.AddEdge(1, 1);
            arena.AddEdge(2, 0);

            var zeroSet = new HashSet<int>();
            zeroSet.Add(0);

            var oneSet = new HashSet<int>();
            oneSet.Add(1);

            var twoSet = new HashSet<int>();
            twoSet.Add(2);

            Assert.IsTrue(arena.CPreOne(oneSet).Count == 2 && arena.CPreOne(oneSet).Contains(1) && arena.CPreOne(oneSet).Contains(0));
            Assert.IsTrue(arena.CPreOne(twoSet).Count == 1 && arena.CPreOne(twoSet).Contains(1));
        }
    }
}