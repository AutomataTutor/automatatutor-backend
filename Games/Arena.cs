using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Games
{
    public class Arena
    {
        private ISet<int> playerZeroNodes = new HashSet<int>();
        private ISet<int> playerOneNodes = new HashSet<int>();
        private ISet<Tuple<int,int>> edges = new HashSet<Tuple<int,int>>();

        public ISet<int> GetNodes()
        {
            var retVal = new HashSet<int>();
            retVal.UnionWith(playerZeroNodes);
            retVal.UnionWith(playerOneNodes);
            return retVal;
        }

        public Arena AddPlayerZeroNode(int id)
        {
            Debug.Assert(!playerZeroNodes.Contains(id));
            Debug.Assert(!playerOneNodes.Contains(id));
            playerZeroNodes.Add(id);
            return this;
        }

        public Arena AddPlayerOneNode(int id)
        {
            Debug.Assert(!playerZeroNodes.Contains(id));
            Debug.Assert(!playerOneNodes.Contains(id));
            playerOneNodes.Add(id);
            return this;
        }

        public Arena AddEdge(int from, int to)
        {
            Debug.Assert(playerZeroNodes.Contains(from) || playerOneNodes.Contains(from));
            Debug.Assert(playerZeroNodes.Contains(to) || playerOneNodes.Contains(to));
            var candEdge = new Tuple<int, int>(from, to);
            Debug.Assert(!edges.Contains(candEdge));
            edges.Add(candEdge);
            return this;
        }

        public Solution CPreZero(ISet<int> target)
        {
            var retVal = new HashSet<int>();
            // Speaking in math: retVal += { cand \in V_0 | exists (v, v') \in E. v = cand && v' \in target }
            retVal.UnionWith(from cand in playerZeroNodes where edges.Any(edge => edge.Item1 == cand && target.Contains(edge.Item2)) select cand);
            var sigma = new PositionalStrategy();
            foreach(var p0Node in retVal)
            {
                var targetEnumerator = target.Where(potentialTargetNode => GetSuccessors(p0Node).Contains(potentialTargetNode)).AsEnumerable().GetEnumerator();
                targetEnumerator.MoveNext();
                sigma.Update(p0Node, targetEnumerator.Current);
            }
            // Speaking in math: retVal += { cand \in V_1 | forall (v, v') \in E. v = cand => v' \in target }
            retVal.UnionWith(from cand in playerOneNodes where edges.All(edge => edge.Item1 != cand || target.Contains(edge.Item2)) select cand);
            return new Solution(retVal, sigma);
        }

        public Solution CPreOne(ISet<int> target)
        {
            var retVal = new HashSet<int>();
            // Speaking in math: retVal += { cand \in V_0 | forall (v, v') \in E. v = cand => v' \in target }
            retVal.UnionWith(from cand in playerZeroNodes where edges.All(edge => edge.Item1 != cand || target.Contains(edge.Item2)) select cand);
            // Speaking in math: retVal += { cand \in V_1 | exists (v, v') \in E. v = cand && v' \in target }
            retVal.UnionWith(from cand in playerOneNodes where edges.Any(edge => edge.Item1 == cand && target.Contains(edge.Item2)) select cand);
            var tau = new PositionalStrategy();
            foreach(var p1Node in retVal.Where(node => IsPlayerOneNode(node)))
            {
                var targetEnumerator = target.Where(potentialTargetNode => GetSuccessors(p1Node).Contains(potentialTargetNode)).AsEnumerable().GetEnumerator();
                targetEnumerator.MoveNext();
                tau.Update(p1Node, targetEnumerator.Current);
            }
            return new Solution(retVal, tau);
        }

        public Solution CPre(int player, ISet<int> target)
        {
            return player == 0 ? CPreZero(target) : CPreOne(target);
        }

        public Solution ZeroAttractor(ISet<int> target)
        {
            var currentSolution = new Solution(target, new PositionalStrategy());
            var nextSolution = this.CPreZero(currentSolution.getWinningRegion());
            while (!nextSolution.getWinningRegion().IsSubsetOf(currentSolution.getWinningRegion()))
            {
                currentSolution = currentSolution.Extend(nextSolution);
                nextSolution = CPreZero(currentSolution.getWinningRegion());
            }
            return currentSolution;
        }

        public Solution OneAttractor(ISet<int> target)
        {
            var currentSolution = new Solution(target, new PositionalStrategy());
            var nextSolution = this.CPreOne(currentSolution.getWinningRegion());
            while (!nextSolution.getWinningRegion().IsSubsetOf(currentSolution.getWinningRegion()))
            {
                currentSolution = currentSolution.Extend(nextSolution);
                nextSolution = CPreOne(currentSolution.getWinningRegion());
            }
            return currentSolution;
        }

        public Solution Attractor(int player, ISet<int> target)
        {
            return player == 0 ? ZeroAttractor(target) : OneAttractor(target);
        }

        public bool IsPlayerOneNode(int node)
        {
            return this.playerOneNodes.Contains(node);
        }

        internal ISet<int> GetSuccessors(int node)
        {
            ISet<int> retVal = new HashSet<int>();
            foreach(var succ in this.GetNodes().Where(succ => edges.Contains(new Tuple<int,int>(node, succ))))
            {
                retVal.Add(succ);
            }
            return retVal;
        }
    }

    public class Solution
    {
        private PositionalStrategy strategy;
        private ISet<int> winningRegion;

        public Solution(ISet<int> winningRegion, PositionalStrategy strategy)
        {
            this.strategy = strategy;
            this.winningRegion = winningRegion;
        }

        public PositionalStrategy getStrategy()
        {
            return this.strategy;
        }

        public ISet<int> getWinningRegion()
        {
            return this.winningRegion;
        }

        public Solution Extend(Solution other)
        {
            ISet<int> newWinningRegion = new HashSet<int>(this.winningRegion);
            newWinningRegion.UnionWith(other.winningRegion);
            PositionalStrategy newStrategy = this.strategy;
            foreach(int from in other.strategy.GetDomain().Where(node => !winningRegion.Contains(node)))
            {
                newStrategy.Update(from, other.strategy.GetNextMove(from));
            }
            return new Solution(newWinningRegion, newStrategy);
        }
    }

    public class PositionalStrategy
    {
        private IDictionary<int, int> nextVertex = new Dictionary<int, int>();

        public void Update(int from, int to)
        {
            this.nextVertex.Add(from, to);
        }

        public ICollection<int> GetDomain()
        {
            return this.nextVertex.Keys;
        }
        
        public bool HasNextMove(int from)
        {
            return nextVertex.ContainsKey(from);
        }

        public int GetNextMove(int from)
        {
            Debug.Assert(HasNextMove(from));
            int nextMove;
            nextVertex.TryGetValue(from, out nextMove);
            return nextMove;
        }

        public void Restrict(ICollection<int> newDomain)
        {
            var newStrat = new Dictionary<int, int>();
            foreach(var move in nextVertex.AsEnumerable().Where(move => newDomain.Contains(move.Key)))
            {
                newStrat.Add(move.Key, move.Value);
            }
            nextVertex = newStrat;
        }
    }
}
