using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
namespace Games.Tests
{

    public class ReachabilitySolverTests
    {

        [TestMethod()]
        public void ReachabilitySolverTestZero()
        {
            var arena = new Arena()
             .AddPlayerZeroNode(0)
             .AddPlayerOneNode(1)
             .AddPlayerOneNode(2)
             .AddPlayerZeroNode(3)
             .AddEdge(0, 1)
             .AddEdge(0, 2)
             .AddEdge(1, 1)
             .AddEdge(1, 3)
             .AddEdge(2, 2)
             .AddEdge(2, 3)
             .AddEdge(3, 0);

            var threeSet = new HashSet<int>();
            threeSet.Add(3);

            var solution = new ReachabilitySolver().ComputeWinningRegionZero(new ReachabilityGame(arena, threeSet));

            Assert.IsTrue(solution.getWinningRegion().Count == 1 &&
                solution.getWinningRegion().Contains(3));
            Assert.IsTrue(!solution.getStrategy().HasNextMove(0));
            Assert.IsTrue(!solution.getStrategy().HasNextMove(1));
            Assert.IsTrue(!solution.getStrategy().HasNextMove(2));
            Assert.IsTrue(!solution.getStrategy().HasNextMove(3));
        }

        [TestMethod()]
        public void ReachabilitySolverTestOne()
        {
            var arena = new Arena()
             .AddPlayerZeroNode(0)
             .AddPlayerOneNode(1)
             .AddPlayerOneNode(2)
             .AddPlayerZeroNode(3)
             .AddEdge(0, 1)
             .AddEdge(0, 2)
             .AddEdge(1, 1)
             .AddEdge(1, 3)
             .AddEdge(2, 2)
             .AddEdge(2, 3)
             .AddEdge(3, 0);

            var threeSet = new HashSet<int>();
            threeSet.Add(3);

            var solution = new ReachabilitySolver().ComputeWinningRegionOne(new ReachabilityGame(arena, threeSet));

            Assert.IsTrue(solution.getWinningRegion().Count == 3 &&
                solution.getWinningRegion().Contains(0) &&
                solution.getWinningRegion().Contains(1) &&
                solution.getWinningRegion().Contains(2));
            Assert.IsTrue(!solution.getStrategy().HasNextMove(0));
            Assert.IsTrue(solution.getStrategy().HasNextMove(1));
            Assert.IsTrue(solution.getStrategy().GetNextMove(1) == 1);
            Assert.IsTrue(solution.getStrategy().HasNextMove(2));
            Assert.IsTrue(solution.getStrategy().GetNextMove(2) == 2);
            Assert.IsTrue(!solution.getStrategy().HasNextMove(3));
        }

    }
}