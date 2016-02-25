using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;
using Microsoft.Z3;

namespace AutomataPDL
{

    public abstract class Regexp : IComparable<Regexp>
    {
        internal string str;

        public virtual void ToString(StringBuilder sb)
        {
            sb.AppendLine(str);
        }

        public override string ToString()
        {
            return str;
        }

        public virtual Regexp Normalize()
        {
            return this;
        }

        public virtual List<Regexp> GetDisjuncts()
        {
            var v = new List<Regexp>();
            v.Add(this);
            return v;
        }

        public virtual Automaton<BDD> getDFA(HashSet<Char> alphabet, CharSetSolver solver)
        {
            var ss =string.Format("^{0}$", str);
            return solver.Convert(string.Format("^{0}$",str));
            //var d1 = dfa.Determinize(solver);
            //var d2 = d1.Minimize(solver);
            //return dfa.Determinize(solver).Minimize(solver);
        }

        public abstract bool HasModel(string input);// A represents the assignment

        public virtual int CompareTo(Regexp re1)
        {
            return str.CompareTo(re1.str);
        }
    }

    public class REUnion : Regexp
    {
        internal Regexp left, right;

        public REUnion(Regexp left, Regexp right)
        {
            this.left = left;
            this.right = right;
            this.str = string.Format(@"(({0})|({1}))", left.str, right.str);
        }

        public override bool HasModel(string input)
        {
            return left.HasModel(input) || right.HasModel(input);
        }

        public override Regexp Normalize()
        {

            var disj = GetDisjuncts();
            disj.Sort();
            Regexp re = null;
            foreach (var d in disj)
            {
                if (re == null)
                    re = d.Normalize();
                else
                    re = new REUnion(d.Normalize(), re);
            }

            if (re.GetDisjuncts().Count == disj.Count)
                return re;
            else
                return re.Normalize();
        }

        public override List<Regexp> GetDisjuncts()
        {
            List<Regexp> disjunctsL = left.GetDisjuncts();
            return new List<Regexp>(disjunctsL.Concat<Regexp>(right.GetDisjuncts()));
        }
    }

    public class REConcatenation : Regexp
    {
        internal Regexp left, right;

        public REConcatenation(Regexp left, Regexp right)
        {
            this.left = left;
            this.right = right;
            this.str = string.Format(@"{0}{1}", left.str, right.str);
        }

        public override bool HasModel(string input)
        {

            for (int i = 0; i <= input.Length; i++)
            {
                if(left.HasModel(input.Substring(0,i)) && right.HasModel(input.Substring(i,input.Length-i)))
                    return true;
            }
            return false;
        }

        public override Regexp Normalize()
        {
            var v = left as REConcatenation;
            if (v != null)
                return new REConcatenation(v.left, new REConcatenation(v.right, right)).Normalize();

            var v1 = right as REUnion;
            if (v1 != null)
                return new REUnion(new REConcatenation(left, v1.left), new REConcatenation(left, v1.right));

            var v2 = left as REUnion;
            if (v2 != null)
                return new REUnion(new REConcatenation(v2.left,right), new REConcatenation(v2.right,right));

            return new REConcatenation(left.Normalize(), right.Normalize());
        }
    }

    public class REStar : Regexp
    {
        internal Regexp r;

        public REStar(Regexp rexp)
        {
            this.r = rexp;
            this.str = string.Format(@"({0})*", rexp.str);
        }

        public override bool HasModel(string input)
        {
            bool[] isModel = new bool[input.Length+1];
            isModel[0] = true;

            for (int i = 1; i <= input.Length; i++)
            {
                isModel[i] = false;

                for (int j = 0; j < i; j++)
                    if (isModel[j] && r.HasModel(input.Substring(j,i-j)))
                    {
                        isModel[i] = true;
                        break;
                    }
            }
            return isModel[input.Length];
        }

        public override Regexp Normalize()
        {
            var r1 = r.Normalize();

            Regexp dj = null;
            foreach (var d in r1.GetDisjuncts())
            {
                Regexp sol = null;
                if (!(d is REStar) && !(d is REPlus) && !(d is REQMark))
                {
                    sol = d;
                }
                else
                {
                    if (d is REStar)
                    {
                        var v = d as REStar;
                        sol = v.r;
                    }
                    else
                    {
                        if (d is REPlus)
                        {
                            var v = d as REPlus;
                            sol = v.r;
                        }
                        else
                        {
                            var v = d as REQMark;
                            sol = v.r;
                        }
                    }
                }
                if (dj == null)
                    dj = sol;
                else
                    dj = new REUnion(sol, dj);
            }
            if (dj.GetDisjuncts().Count != r.GetDisjuncts().Count)
                return new REStar(dj).Normalize();

            return new REStar(dj);
        }
    }

    public class REPlus : Regexp
    {
        internal Regexp r;

        public REPlus(Regexp rexp)
        {
            this.r = rexp;
            this.str = string.Format(@"({0})+", rexp.str);
        }

        public override bool HasModel(string input)
        {
            bool[] isModel = new bool[input.Length + 1];
            isModel[0] = false;

            for (int i = 1; i <= input.Length; i++)
            {
                isModel[i] = false;

                for (int j = 0; j < i; j++)
                    if (isModel[j] && r.HasModel(input.Substring(j, i - j)))
                    {
                        isModel[i] = true;
                        break;
                    }
            }
            return isModel[input.Length];
        }

        public override Regexp Normalize()
        {
            var r1 = r.Normalize();

            Regexp dj = null;
            foreach (var d in r1.GetDisjuncts())
            {
                Regexp sol = null;
                if (!(d is REStar) && !(d is REPlus))
                {
                    sol = d;
                }
                else
                {
                    if (d is REStar)
                    {
                        var v = d as REStar;
                        sol = v.r;
                    }
                    else
                    {
                        if (d is REPlus)
                        {
                            var v = d as REPlus;
                            sol = v.r;
                        }
                    }
                }
                if (dj == null)
                    dj = sol;
                else
                    dj = new REUnion(sol, dj);
            }
            if (dj.GetDisjuncts().Count != r.GetDisjuncts().Count)
                return new REPlus(dj).Normalize();

            return new REPlus(dj);
        }
    }

    public class REQMark : Regexp
    {
        internal Regexp r;

        public REQMark(Regexp rexp)
        {
            this.r = rexp;
            this.str = string.Format(@"({0})?", rexp.str);
        }

        public override bool HasModel(string input)
        {
            return input=="" || r.HasModel(input);
        }

        public override Regexp Normalize()
        {
            var r1 = r.Normalize();

            Regexp dj = null;
            foreach (var d in r1.GetDisjuncts())
            {
                Regexp sol = null;
                if (!(d is REStar) && !(d is REQMark))
                {
                    sol = d;
                }
                else
                {
                    if (d is REStar)
                    {
                        var v = d as REStar;
                        sol = new REPlus(v.r);
                    }
                    else
                    {
                        if (d is REQMark)
                        {
                            var v = d as REQMark;
                            sol = v.r;
                        }
                    }
                }
                if (dj == null)
                    dj = sol;
                else
                    dj = new REUnion(sol, dj);
            }
            if (dj.GetDisjuncts().Count != r.GetDisjuncts().Count)
                return new REQMark(dj).Normalize();

            return new REQMark(dj);
        }
    }


    public class RELabel : Regexp
    {
        internal char c;

        public RELabel(char c)
        {
            this.c = c;
            this.str = string.Format(@"{0}", c);
        }

        public override bool HasModel(string input)
        {
            return input.Length == 1 && input[0] == c;
        }
    }

    public class REString : Regexp
    {
        internal string s;

        public REString(string s)
        {
            this.s = s;
            this.str = string.Format(@"{0}", s);
        }

        public override bool HasModel(string input)
        {
            return s==input;
        }
    }
}
