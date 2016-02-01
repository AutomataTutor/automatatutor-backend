using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PumpingLemma
{
    // GRAMMER:
    //   SymbolicString -> 
    //          AtomicToken | SymbolicString SymbolicString | (SymbolicString) | SymbolicString^Expr
    //   Expr ->
    //          AtomicToken | ( AtomicToken )
    static public class Parser
    {
        public static SymbolicString parseSymbolicString(String input, List<String> alphabet)
        {
            var ss = ANTLRInterface.parseSymbolicString(input, alphabet);
            return ss;
        }

        public static BooleanExpression parseCondition(String input)
        {
            var ss = ANTLRInterface.parseCondition(input);
            return ss;
        }

        public static LinearIntegerExpression parseLinearExpression(String input)
        {
            var ss = ANTLRInterface.parseLinearExpression(input);
            return ss;
        }
    }

    public static class ANTLRInterface
    {
        public static SymbolicString parseSymbolicString(String input, List<String> alphabet)
        {
            var stream = new AntlrInputStream(input);
            var lexer = new SymbolicStringsLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SymbolicStringsParser(tokens);
            parser.alphabet = alphabet;
            parser.BuildParseTree = true;
            var tree = parser.symbolic_string();
            if (parser.NumberOfSyntaxErrors == 0 && parser.word_error == null)
                return tree.value;
            return null;
        }

        public static BooleanExpression parseCondition(String input)
        {
            var stream = new AntlrInputStream(input);
            var lexer = new SymbolicStringsLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SymbolicStringsParser(tokens);
            parser.BuildParseTree = true;
            var tree = parser.condition();
            if (parser.NumberOfSyntaxErrors == 0)
                return tree.value;
            return null;
        }

        public static LinearIntegerExpression parseLinearExpression(String input)
        {
            var stream = new AntlrInputStream(input);
            var lexer = new SymbolicStringsLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SymbolicStringsParser(tokens);
            parser.BuildParseTree = true;
            var tree = parser.integer();
            if (parser.NumberOfSyntaxErrors == 0)
                return tree.value;
            return null;
        }
    }
    
    static class ParserUtils
    {
        public static List<String> splitIntoAlphabetSymbols(string text, List<String> alphabet)
        {
            var return_tokes = new List<String>();
            foreach (char c in text)
            {
                var symbol = new string(c, 1);
                if (alphabet.FindIndex(x => x == symbol) == -1)
                    return null;
                return_tokes.Add(symbol);
            }
            return return_tokes;
        }

        /*
         * // If symbols are allowed to be more than one character 
        public static List<String> splitIntoAlphabetSymbols(string text, List<String> alphabet)
        {
	        List<String> return_tokens = null;
	        if (text.Length == 0)
	        {
		        return_tokens = new List<string>();
                return return_tokens;
            }
            foreach (string symbol in alphabet)
            {
	            if (text.StartsWith(symbol))
		        {
			        List<String> sub = splitIntoAlphabetSymbols(text.Substring(symbol.Length), alphabet);
			        if (sub != null)
			        {
				        sub.Insert(0, symbol);
		                if (return_tokens != null)
			            {
					        // Multiple parses
					        return_tokens = null;
					        break;
				        }
				        else
				        {
					        return_tokens = sub;
				        }
			        }
		        }
            }
	        return return_tokens;
        }
        */

        public static SymbolicString wordToSymbolicString(List<String> symbols)
        {
            if (symbols.Count == 1)
                return SymbolicString.Symbol(symbols[0]);
            return SymbolicString.Concat(
                symbols.Select(x => SymbolicString.Symbol(x)).ToList()
                );
        }

        public static SymbolicString join(SymbolicString a, SymbolicString b)
        {
            if (a == null || b == null)
                return null;

            if (a.expression_type == SymbolicString.SymbolicStringType.Concat
                && b.expression_type == SymbolicString.SymbolicStringType.Concat)
            {
                return SymbolicString.Concat(a.sub_strings.Concat(b.sub_strings));
            }
            List<SymbolicString> sub_strings = new List<SymbolicString>();
            if (a.expression_type == SymbolicString.SymbolicStringType.Concat)
                sub_strings.AddRange(a.sub_strings);
            else
                sub_strings.Add(a);
            if (b.expression_type == SymbolicString.SymbolicStringType.Concat)
                sub_strings.AddRange(b.sub_strings);
            else
                sub_strings.Add(b);
           return SymbolicString.Concat(sub_strings);
        }

        public static SymbolicString repeat(SymbolicString s, LinearIntegerExpression e)
        {
            if (s == null || e == null)
                return null;

            return SymbolicString.Repeat(s, e);
        }

        public static SymbolicString repeatLast(SymbolicString s, LinearIntegerExpression e)
        {
            if (s == null || e == null)
                return null;

            switch(s.expression_type)
            {
                case SymbolicString.SymbolicStringType.Symbol:
                    return SymbolicString.Repeat(s, e);
                case SymbolicString.SymbolicStringType.Repeat:
                    return SymbolicString.Repeat(s, e);
                case SymbolicString.SymbolicStringType.Concat:
                    List<SymbolicString> sub_strings = new List<SymbolicString>();
                    sub_strings.AddRange(s.sub_strings.Take(s.sub_strings.Count - 1));
                    var last = s.sub_strings[s.sub_strings.Count - 1];
                    sub_strings.Add(SymbolicString.Repeat(last, e));
                    return SymbolicString.Concat(sub_strings);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
