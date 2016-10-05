using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Games
{
    public class ReachabilityGame
    {
        private Arena arena;
        private ISet<int> reachSet;

        public ReachabilityGame(Arena arena, ISet<int> reachSet)
        {
            this.arena = arena;
            this.reachSet = reachSet;
        }

        public Arena GetArena()
        {
            return this.arena;
        }

        public ISet<int> GetReachSet()
        {
            return this.reachSet;
        }
    }

    public class ReachabilitySolver
    {
        public Solution ComputeWinningRegionZero(ReachabilityGame game)
        {
            return game.GetArena().ZeroAttractor(game.GetReachSet());
        }

        public Solution ComputeWinningRegionOne(ReachabilityGame game)
        {
            ISet<int> winningRegionZero = ComputeWinningRegionZero(game).getWinningRegion();
            ISet<int> winningRegionOne = game.GetArena().GetNodes();
            winningRegionOne.ExceptWith(winningRegionZero);
            PositionalStrategy tau = new PositionalStrategy();
            foreach (var p1Node in winningRegionOne.Where(node => game.GetArena().IsPlayerOneNode(node)))
            {
                var nextNodeEnumerator = game.GetArena().GetSuccessors(p1Node).Where(node => !winningRegionZero.Contains(node)).AsEnumerable().GetEnumerator();
                nextNodeEnumerator.MoveNext();
                tau.Update(p1Node, nextNodeEnumerator.Current);
            }

            return new Solution(winningRegionOne, tau);
        }
    }
}
