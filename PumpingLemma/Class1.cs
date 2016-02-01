using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics.Contracts;
using System.Xml.Linq;
using System.Diagnostics;

namespace PumpingLemma
{
    // These should probably be case classes
    // C# does not have case classes
    public class SymbolicString
    {
        public enum SymbolicStringType { Symbol, Concat, Repeat };

        public SymbolicStringType expression_type;

        // Used if it is a symbol
        public String atomic_symbol;

        // Used if it is concat
        public List<SymbolicString> sub_strings;

        // Used if it is a repeat
        public SymbolicString root;
        public LinearIntegerExpression repeat;

        public bool isEpsilon()
        {
            return this.expression_type == SymbolicStringType.Concat && this.sub_strings.Count == 0;
        }

        public bool isFlat()
        {
            switch(this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    return true;
                case SymbolicStringType.Repeat:
                    return this.root.isWord();
                case SymbolicStringType.Concat:
                    return this.sub_strings.All(x =>
                        x.expression_type == SymbolicStringType.Symbol ||
                        x.expression_type == SymbolicStringType.Repeat && x.root.isWord());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool isWord()
        {
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    return true;
                case SymbolicStringType.Repeat:
                    return false;
                case SymbolicStringType.Concat:
                    return this.sub_strings.All(x => x.expression_type == SymbolicStringType.Symbol);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerable<String> word()
        {
            Debug.Assert(this.isWord());
            Debug.Assert(this.isFlat());
            if (this.expression_type == SymbolicStringType.Symbol)
                return new List<String> { this.atomic_symbol };
            else if (this.expression_type == SymbolicStringType.Concat)
                return this.sub_strings.Select(x => x.atomic_symbol);
            else
                throw new ArgumentException();
        }

        public int wordLength()
        {
            Debug.Assert(this.isWord());
            Debug.Assert(this.isFlat());
            if (this.expression_type == SymbolicStringType.Symbol)
                return 1;
            else if (this.expression_type == SymbolicStringType.Concat)
                return this.sub_strings.Count();
            else
                throw new ArgumentException();
        }

        public int StarHeight()
        {
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    return 0;
                case SymbolicStringType.Repeat:
                    return 1 + this.root.StarHeight();
                case SymbolicStringType.Concat:
                    return this.sub_strings.Max(x => x.StarHeight());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public LinearIntegerExpression length()
        {
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    return LinearIntegerExpression.Constant(1);
                case SymbolicStringType.Concat:
                    return LinearIntegerExpression.Plus(this.sub_strings.Select(x => x.length()));
                case SymbolicStringType.Repeat:
                    var sub = this.root.length();
                    Contract.Assert(sub.isConstant());
                    return LinearIntegerExpression.Times(sub.constant, this.repeat);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void flatten()
        {
            // Contract.Requires(this.StarHeight() <= 1);
            // Contract.Ensures(this.isFlat());
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    break;
                case SymbolicStringType.Repeat:
                    this.root.flatten();
                    break;
                case SymbolicStringType.Concat:
                    this.sub_strings.ForEach(x => x.flatten());
                    var new_substrings = new List<SymbolicString>();
                    foreach (var sub in sub_strings)
                    {
                        switch (sub.expression_type)
                        {
                            case SymbolicStringType.Symbol:
                                new_substrings.Add(sub);
                                break;
                            case SymbolicStringType.Repeat:
                                new_substrings.Add(sub);
                                break;
                            case SymbolicStringType.Concat:
                                foreach (var subsub in sub.sub_strings)
                                    new_substrings.Add(subsub);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    this.sub_strings = new_substrings;
                    if (this.sub_strings.Count == 1)
                        this.Copy(this.sub_strings.First());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        #region Splitting 
        /// All the methods in this region work only only flat symbolic strings, i.e., 
        /// use flatten before calling

        public List<LinearIntegerExpression> repeats()
        {
            var repeatLengths = new List<LinearIntegerExpression>();
            if (this.expression_type == SymbolicStringType.Concat)
            {
                foreach (var sub in this.sub_strings)
                    if (sub.expression_type == SymbolicStringType.Repeat)
                        repeatLengths.Add(sub.repeat);
            }
            if (this.expression_type == SymbolicStringType.Repeat)
                repeatLengths.Add(this.repeat); 
            return repeatLengths;
        }

        private Tuple<int, int, SymbolicString> makeSegment(Dictionary<VariableType, int> model, ref int start)
        {
            int len = this.length().Eval(model);
            int oldStart = start;
            start += len;
            return new Tuple<int, int, SymbolicString>(oldStart, start, this);
        }

        private List<Tuple<int, int, SymbolicString>> getSegments(Dictionary<VariableType, int> model, ref int start)
        {
            var ret = new List<Tuple<int, int, SymbolicString>>();
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                case SymbolicStringType.Repeat:
                    ret.Add(this.makeSegment(model, ref start));
                    break;
                case SymbolicStringType.Concat:
                    foreach (var sub in this.sub_strings)
                        ret.Add(sub.makeSegment(model, ref start));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return ret;
        }

        // Generates XML for the 
        public XElement SplitDisplayXML(
            VariableType pumpingLength,
            BooleanExpression additionalConstraints)
        {
            Contract.Assert(additionalConstraints.GetVariables().Count() <= 1);
            if (additionalConstraints.GetVariables().Count == 1)
                Contract.Assert(additionalConstraints.GetVariables().First().Equals(pumpingLength));
            var pumpingLengthVariable = LinearIntegerExpression.SingleTerm(1, pumpingLength);
        
            var repeatLengths = repeats();

            // For great and inexplicable reasons. Trust me, it looks better this way.
            var displayConstraints = LogicalExpression.And(repeatLengths.Select(x =>
                ComparisonExpression.GreaterThanOrEqual(x, LinearIntegerExpression.Constant(5))
            ));

            XElement symbstrings = new XElement("symbstrings");

            foreach (var split in this.ValidSplits(pumpingLengthVariable, additionalConstraints))
            {
                var splitRepeatLengths = split.start.repeats(); 
                splitRepeatLengths.AddRange(split.mid.repeats());
                splitRepeatLengths.AddRange(split.end.repeats());

                // Get models that don't look terrible
                var splitDisplayConstraints = LogicalExpression.And(splitRepeatLengths.Select(x =>
                    ComparisonExpression.GreaterThanOrEqual(x, LinearIntegerExpression.Constant(1))
                    ));
                var constraint = LogicalExpression.And(displayConstraints, split.constraints, splitDisplayConstraints);
                
                if (!constraint.isSatisfiable()) 
                    continue;
                var splitModel = constraint.getModel();

                var symbstr = new XElement("symbstr");
                var strings = new XElement("strings");
                var splits = new XElement("splits");

                Func<string, Tuple<int, int, SymbolicString>, XElement> handleSegment = (parentTagName, segment) => {
                    var parent = new XElement(parentTagName);
                    var from = new XElement("from"); from.Value = segment.Item1.ToString();
                    var to = new XElement("to"); to.Value = segment.Item2.ToString();
                    var label = new XElement("label"); label.Value = segment.Item3.ToString();
                    parent.Add(from);
                    parent.Add(to);
                    parent.Add(label);
                    return parent;
                };

                int strIndex = 0;
                foreach (var segment in getSegments(splitModel, ref strIndex))
                    strings.Add(handleSegment("string", segment));

                int splitIndex = 0;
                foreach (var segment in new[] { split.start, split.mid, split.end })
                    splits.Add(handleSegment("split", segment.makeSegment(splitModel, ref splitIndex))); 

                symbstr.Add(strings);
                symbstr.Add(splits);
                symbstrings.Add(symbstr);
            }

            return symbstrings;
        }

        // Generates splits (x, y, z) such that |y| > 0 and |xy| < p
        public IEnumerable<Split> ValidSplits(
            LinearIntegerExpression pumpingLengthVariable,
            BooleanExpression additionalConstraints)
        {
            foreach (var split in this.Splits())
            {
                if (split.mid.isEpsilon())
                    continue;

                var cond1 = ComparisonExpression.GreaterThan(split.mid.length(), LinearIntegerExpression.Constant(0));
                var cond2 = ComparisonExpression.LessThan(split.start.length() + split.mid.length(), pumpingLengthVariable);
                var condition = LogicalExpression.And(additionalConstraints, cond1, cond2);

                if (condition.isMaybeSatisfiable())
                {
                    split.AddConstraint(condition);
                    yield return split;
                }
            }
        }

        // Generates splits where all three parts may be empty
        public IEnumerable<Split> Splits()
        {
            // Contract.Requires<ArgumentException>(this.isFlat());
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    yield return Split.MakeSplit(this, SymbolicString.Epsilon(), SymbolicString.Epsilon(), LogicalExpression.True());
                    yield return Split.MakeSplit(SymbolicString.Epsilon(), this, SymbolicString.Epsilon(), LogicalExpression.True());
                    yield return Split.MakeSplit(SymbolicString.Epsilon(), SymbolicString.Epsilon(), this, LogicalExpression.True());
                    break;
                case SymbolicStringType.Repeat:
                    var v1 = LinearIntegerExpression.FreshVariable();
                    var v2 = LinearIntegerExpression.FreshVariable();
                    var v3 = LinearIntegerExpression.FreshVariable();
                    var sanityConstraint = LogicalExpression.And(
                        ComparisonExpression.GreaterThanOrEqual(v1, 0),
                        ComparisonExpression.GreaterThanOrEqual(v2, 0),
                        ComparisonExpression.GreaterThanOrEqual(v3, 0)
                        );
                    // Suppose the word w = a_0 a_1 \ldots a_{n-1}
                    // All splits are of the form
                    //    BEG: (a_0 \ldots a_{n-1})^i (a_0 \ldots a_p)                              = w^i . w_1
                    //    MID: (a_{p+1} \ldots a_{n-1} a_0 \ldots a_p)^j (a_{p+1} \ldots a_q)       = (w_2 w_1)^j  w_3
                    //    END: (a_{q+1} \ldots a_{n-1}) (a_0 \ldots a_{n-1})^k                          = w_4 w^k
                    var w = this.root.sub_strings;
                    var ww = w.Concat(w);
                    int n = w.Count;
                    for (int w1len = 0; w1len < n; w1len++)
                    {
                        var w_1 = w.Take(w1len);
                        var w_2 = w.Skip(w1len);
                        var beg = SymbolicString.Concat(
                                        SymbolicString.Repeat(this.root, v1),
                                        SymbolicString.Concat(w_1));
                        var mid_root = SymbolicString.Concat(
                                        SymbolicString.Concat(w_2),
                                        SymbolicString.Concat(w_1));
                        var mid_beg = SymbolicString.Repeat(mid_root, v2);
                        for (int w3len = 0; w3len < n; w3len++)
                        {
                            var w_3 = ww.Skip(w1len).Take(w3len);
                            var mid = SymbolicString.Concat(mid_beg, SymbolicString.Concat(w_3.ToList()));

                            IEnumerable<SymbolicString> w_4;
                            if (w1len + w3len == 0)
                                w_4 = new List<SymbolicString>();
                            else if (w1len + w3len <= n)
                                w_4 = w.Skip(w1len).Skip(w3len);
                            else
                                w_4 = ww.Skip(w1len).Skip(w3len);
                            var end = SymbolicString.Concat(
                                SymbolicString.Concat(w_4.ToList()),
                                SymbolicString.Repeat(this.root, v3)
                                );

                            var consumed = (w_1.Count() + w_3.Count() + w_4.Count()) / w.Count();
                            yield return Split.MakeSplit(
                                beg,
                                mid,
                                end,
                                LogicalExpression.And(
                                    ComparisonExpression.Equal(v1 + v2 + v3 + consumed, this.repeat),
                                    sanityConstraint
                                )
                            );
                        }
                    }
                    break;
                case SymbolicStringType.Concat:
                    foreach (var beg_midend in this.TwoSplits())
                    {
                        foreach (var mid_end in beg_midend.end.TwoSplits())
                        {
                            yield return Split.MakeSplit(
                                beg_midend.start,
                                mid_end.start,
                                mid_end.end,
                                LogicalExpression.And(beg_midend.constraints, mid_end.constraints));
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Generate TwoSplit's where either one may be empty
        // Generates the splits in order, i.e., by length of the prefix
        // So, for s being symbol and concat, first one will always be (\epsilon, s) and last one always (s, \epsilon)
        // So, for s being repeat (abc)^n, first will be ((abc)^i,(abc)^j) and last one always ((abc)^i ab, c(abc)^j)
        // Requires a flat symbolic string
        public IEnumerable<TwoSplit> TwoSplits()
        {
            SymbolicString prefix, suffix;
            // Contract.Requires(this.isFlat());


            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol:
                    yield return TwoSplit.MakeSplit(SymbolicString.Epsilon(), this, LogicalExpression.True());
                    yield return TwoSplit.MakeSplit(this, SymbolicString.Epsilon(), LogicalExpression.True());
                    break;
                case SymbolicStringType.Concat:
                    yield return TwoSplit.MakeSplit(SymbolicString.Epsilon(), this, LogicalExpression.True());
                    for (int i = 0; i < this.sub_strings.Count; i++)
                    {
                        prefix = SymbolicString.Concat(this.sub_strings.Take(i).ToList());
                        suffix = SymbolicString.Concat(this.sub_strings.Skip(i + 1).ToList());
                        foreach (var split in this.sub_strings[i].TwoSplits())
                        {
                            if (!split.start.isEpsilon())
                                yield return split.extend(prefix, suffix);
                        }
                    }
                    break;

                case SymbolicStringType.Repeat:
                    var v1 = LinearIntegerExpression.FreshVariable();
                    var v2 = LinearIntegerExpression.FreshVariable();
                    var sanityConstraint = LogicalExpression.And(
                        ComparisonExpression.GreaterThanOrEqual(v1, 0),
                        ComparisonExpression.GreaterThanOrEqual(v2, 0)
                        );
                    prefix = SymbolicString.Repeat(this.root, v1);
                    suffix = SymbolicString.Repeat(this.root, v2);
                    yield return TwoSplit.MakeSplit(
                        prefix,
                        suffix,
                        LogicalExpression.And(sanityConstraint, ComparisonExpression.Equal(v1 + v2, this.repeat))
                        );
                    if (this.root.expression_type != SymbolicStringType.Concat)
                        break;
                    for (int i = 1; i < this.root.sub_strings.Count; i++)
                    {
                        var split = TwoSplit.MakeSplit(
                            SymbolicString.Concat(this.root.sub_strings.Take(i)),
                            SymbolicString.Concat(this.root.sub_strings.Skip(i)),
                            LogicalExpression.And(
                                ComparisonExpression.Equal(v1 + v2 + 1, this.repeat),
                                sanityConstraint
                            )
                        );
                        yield return split.extend(prefix, suffix);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        #endregion

        public override String ToString()
        {
            switch (expression_type)
            {
                case SymbolicStringType.Concat:
                    return String.Join(" ", sub_strings.Select(x => x.ToString()));
                case SymbolicStringType.Repeat:
                    if (root.expression_type == SymbolicStringType.Symbol)
                        return root.ToString()+ "^(" + repeat.ToString() + ")";
                    else
                        return "(" + root.ToString() + ")^(" + repeat.ToString() + ")";
                case SymbolicStringType.Symbol:
                    return atomic_symbol;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public HashSet<VariableType> GetIntegerVariables()
        {
            HashSet<VariableType> ret;
            switch (expression_type)
            {
                case SymbolicStringType.Repeat:
                    ret = root.GetIntegerVariables();
                    foreach (VariableType v in repeat.GetVariables())
                        ret.Add(v);
                    break;
                case SymbolicStringType.Concat:
                    ret = new HashSet<VariableType>();
                    foreach (var sub in sub_strings)
                        foreach (VariableType v in sub.GetIntegerVariables())
                            ret.Add(v);
                    break;
                case SymbolicStringType.Symbol:
                    ret = new HashSet<VariableType>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return ret;
        }

        #region Constructors
        // Constructors
        private SymbolicString(String atomic_symbol)
        {
            this.expression_type = SymbolicStringType.Symbol;
            this.atomic_symbol = atomic_symbol;
        }
        private SymbolicString(List<SymbolicString> sub_strings)
        {
            if (sub_strings.Count == 1)
            {
                this.Copy(sub_strings.First());
            }
            else
            {
                this.expression_type = SymbolicStringType.Concat;
                this.sub_strings = sub_strings;
            }
        }
        private SymbolicString(SymbolicString root, LinearIntegerExpression repeat)
        {
            this.expression_type = SymbolicStringType.Repeat;
            this.root = root;
            this.repeat = repeat;
        }
        private void Copy(SymbolicString that)
        {
            this.expression_type = that.expression_type;
            this.atomic_symbol = that.atomic_symbol;
            this.root = that.root;
            this.repeat = that.repeat;
            this.sub_strings = that.sub_strings;
        }
        #endregion

        #region Factory Methods
        // Factory methods
        public static SymbolicString Symbol(String atomic_symbol)
        {
            return new SymbolicString(atomic_symbol);
        }
        public static SymbolicString Concat(params SymbolicString[] sub_strings)
        {
            return Concat(sub_strings.ToList());
        }
        public static SymbolicString Concat(IEnumerable<SymbolicString> sub_strings)
        {
            var ans = new SymbolicString(sub_strings.ToList());
            ans.flatten();
            return ans;
        }
        public static SymbolicString Epsilon()
        {
            return new SymbolicString(new List<SymbolicString>());
        }
        public static SymbolicString Repeat(SymbolicString root, LinearIntegerExpression repeat)
        {
            return new SymbolicString(root, repeat);
        }

        public static SymbolicString FromTextDescription(List<String> alphabet, string symbolicStringText)
        {
            var symbolPattern = new Regex(@"^[a-zA-Z0-9]$");
            var illegalSymbols = alphabet.FindAll(s => !symbolPattern.IsMatch(s));
            if (illegalSymbols.Count > 0)
            {
                var message = string.Format(
                    "Found illegal symbols {0} in alphabet. Symbols should match [a-zA-Z0-9]", 
                    string.Join(", ", illegalSymbols)
                );
                throw new PumpingLemmaException(message);
            }

            // Parse the language
            var ss = PumpingLemma.Parser.parseSymbolicString(symbolicStringText, alphabet);
            if (ss == null)
                throw new PumpingLemmaException("Unable to parse string");
            return ss;
        }

        #endregion

        public SymbolicString reverse()
        {
            switch (this.expression_type)
            {
                case SymbolicStringType.Symbol: return this;
                case SymbolicStringType.Repeat: return Repeat(this.root.reverse(), this.repeat);
                case SymbolicStringType.Concat: 
                    var reversed = new List<SymbolicString>();
                    foreach (var sub in this.sub_strings)
                        reversed.Add(sub.reverse());
                    reversed.Reverse();
                    return Concat(reversed);
                default:
                    throw new ArgumentException();
            }
        }
    }
}
