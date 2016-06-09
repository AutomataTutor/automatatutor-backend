using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games
{
    public class Arena
    {
        private ISet<int> playerZeroNodes = new HashSet<int>();
        private ISet<int> playerOneNodes = new HashSet<int>();
        private ISet<Tuple<int,int>> edges = new HashSet<Tuple<int,int>>();

        public void AddPlayerZeroNode(int id)
        {
            Debug.Assert(!playerZeroNodes.Contains(id));
            Debug.Assert(!playerOneNodes.Contains(id));
            playerZeroNodes.Add(id);
        }

        public void AddPlayerOneNode(int id)
        {
            Debug.Assert(!playerZeroNodes.Contains(id));
            Debug.Assert(!playerOneNodes.Contains(id));
            playerOneNodes.Add(id);
        }

        public void AddEdge(int from, int to)
        {
            Debug.Assert(playerZeroNodes.Contains(from) || playerOneNodes.Contains(from));
            Debug.Assert(playerZeroNodes.Contains(to) || playerOneNodes.Contains(to));
            var candEdge = new Tuple<int, int>(from, to);
            Debug.Assert(!edges.Contains(candEdge));
            edges.Add(candEdge);
        }

        public ISet<int> CPreZero(ISet<int> target)
        {
            var retVal = new HashSet<int>();
            // Speaking in math: retVal += { cand \in V_0 | exists (v, v') \in E. v = cand && v' \in target }
            retVal.UnionWith(from cand in playerZeroNodes where edges.Any(edge => edge.Item1 == cand && target.Contains(edge.Item2)) select cand);
            // Speaking in math: retVal += { cand \in V_1 | forall (v, v') \in E. v = cand => v' \in target }
            retVal.UnionWith(from cand in playerOneNodes where edges.All(edge => edge.Item1 != cand || target.Contains(edge.Item2)) select cand);
            return retVal;
        }

        public ISet<int> CPreOne(ISet<int> target)
        {
            var retVal = new HashSet<int>();
            // Speaking in math: retVal += { cand \in V_0 | forall (v, v') \in E. v = cand => v' \in target }
            retVal.UnionWith(from cand in playerZeroNodes where edges.All(edge => edge.Item1 != cand || target.Contains(edge.Item2)) select cand);
            // Speaking in math: retVal += { cand \in V_1 | exists (v, v') \in E. v = cand && v' \in target }
            retVal.UnionWith(from cand in playerOneNodes where edges.Any(edge => edge.Item1 == cand && target.Contains(edge.Item2)) select cand);
            return retVal;
        }

        public ISet<int> CPre(int player, ISet<int> target)
        {
            return player == 0 ? CPreZero(target) : CPreOne(target);
        }
    }

    public class BuchiGame
    {
    }
}
