using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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

            Assert.IsTrue(arena.CPreZero(twoSet).getWinningRegion().Count == 0);
            Assert.IsTrue(arena.CPreZero(twoZeroSet).getWinningRegion().Count == 1 && arena.CPreZero(twoZeroSet).getWinningRegion().Contains(2));
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

            Assert.IsTrue(arena.CPreOne(oneSet).getWinningRegion().Count == 2 && arena.CPreOne(oneSet).getWinningRegion().Contains(1) && arena.CPreOne(oneSet).getWinningRegion().Contains(0));
            Assert.IsTrue(arena.CPreOne(twoSet).getWinningRegion().Count == 1 && arena.CPreOne(twoSet).getWinningRegion().Contains(1));
        }

        [TestMethod()]
        public void ZeroAttractorTest()
        {
            var arena = new Arena()
             .AddPlayerZeroNode(0)
             .AddPlayerOneNode(1)
             .AddPlayerZeroNode(2)
             .AddEdge(0, 1)
             .AddEdge(1, 2)
             .AddEdge(1, 1)
             .AddEdge(2, 0);

            var zeroSet = new HashSet<int>();
            zeroSet.Add(0);

            var oneSet = new HashSet<int>();
            oneSet.Add(1);

            var twoSet = new HashSet<int>();
            twoSet.Add(2);

            Assert.IsTrue(arena.ZeroAttractor(zeroSet).getWinningRegion().Count == 2 &&
                arena.ZeroAttractor(zeroSet).getWinningRegion().Contains(0) &&
                arena.ZeroAttractor(zeroSet).getWinningRegion().Contains(2));
        }

        [TestMethod()]
        public void ZeroAttractorTestStrategy()
        {
            var arena = new Arena()
             .AddPlayerZeroNode(0)
             .AddPlayerOneNode(1)
             .AddPlayerOneNode(2)
             .AddPlayerZeroNode(3)
             .AddEdge(0, 1)
             .AddEdge(0, 2)
             .AddEdge(1, 3)
             .AddEdge(2, 3)
             .AddEdge(3, 0);

            var twoSet = new HashSet<int>();
            twoSet.Add(2);

            var solution = arena.ZeroAttractor(twoSet);

            Assert.IsTrue(solution.getWinningRegion().Count == 4 &&
                solution.getWinningRegion().Contains(0) &&
                solution.getWinningRegion().Contains(1) &&
                solution.getWinningRegion().Contains(2) &&
                solution.getWinningRegion().Contains(3));
            Assert.IsTrue(solution.getStrategy().HasNextMove(0));
            Assert.IsTrue(solution.getStrategy().GetNextMove(0) == 1 || solution.getStrategy().GetNextMove(0) == 2);
            Assert.IsTrue(!solution.getStrategy().HasNextMove(1));
            Assert.IsTrue(!solution.getStrategy().HasNextMove(2));
            Assert.IsTrue(solution.getStrategy().HasNextMove(3));
            Assert.IsTrue(solution.getStrategy().GetNextMove(3) == 0);
        }

        [TestMethod()]
        public void OneAttractorTest()
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

            Assert.IsTrue(arena.OneAttractor(zeroSet).getWinningRegion().Count == 3 &&
                arena.OneAttractor(zeroSet).getWinningRegion().Contains(0) &&
                arena.OneAttractor(zeroSet).getWinningRegion().Contains(1) &&
                arena.OneAttractor(zeroSet).getWinningRegion().Contains(2));

            Assert.IsTrue(arena.OneAttractor(twoSet).getWinningRegion().Count == 3 &&
                arena.OneAttractor(twoSet).getWinningRegion().Contains(0) &&
                arena.OneAttractor(twoSet).getWinningRegion().Contains(1) &&
                arena.OneAttractor(twoSet).getWinningRegion().Contains(2));
        }
    }
}