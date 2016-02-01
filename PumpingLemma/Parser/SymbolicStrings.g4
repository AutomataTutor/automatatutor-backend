grammar SymbolicStrings;




/*
 * Parser
 */

@parser::header {
}

@parser::members {

// To check appearing identifiers
public List<String> alphabet;
public Dictionary<String, List<String>> word_map = new Dictionary<String, List<String>>();
public String word_error = null;


}


term 
	returns [ PumpingLemma.LinearIntegerExpression value = null ]
	:
	INT { $value = PumpingLemma.LinearIntegerExpression.Constant($INT.int); }
	| IDENT { 
		$value = PumpingLemma.LinearIntegerExpression.Variable($IDENT.text);
		}
	| INT TIMES IDENT { 
		$value = PumpingLemma.LinearIntegerExpression.SingleTerm(
			$INT.int,
			PumpingLemma.VariableType.Variable($IDENT.text)
			);
		}
	| IDENT TIMES INT { 
		$value = PumpingLemma.LinearIntegerExpression.SingleTerm(
			$INT.int,
			PumpingLemma.VariableType.Variable($IDENT.text)
			);
		}
	| LPAREN a=terms RPAREN { $value = $a.value; }
	;

terms 
	returns [ PumpingLemma.LinearIntegerExpression value = null ]
    :
	a=term { $value = $a.value; }
	| a=term PLUS b=terms {
		 $value = PumpingLemma.LinearIntegerExpression.Plus($a.value, $b.value); 
		 }
	;

integer
	returns [ PumpingLemma.LinearIntegerExpression value = null ]
    : 
      INT { $value = PumpingLemma.LinearIntegerExpression.Constant($INT.int); }
	| IDENT { $value = PumpingLemma.LinearIntegerExpression.Variable($text); }
	| LPAREN a=terms RPAREN  { $value = $a.value; }
	;

word 
	returns [ PumpingLemma.SymbolicString value = null ]
	locals [ List<String> symbols = null ]
	:
	IDENT {
		$symbols = PumpingLemma.ParserUtils.splitIntoAlphabetSymbols($text, alphabet);
		if ($symbols == null)
			word_error = $text;
		else
			$value = PumpingLemma.ParserUtils.wordToSymbolicString($symbols);
	}
	| INT {
		$symbols = PumpingLemma.ParserUtils.splitIntoAlphabetSymbols($text, alphabet);
		if ($symbols == null)
			word_error = $text;
		else
			$value = PumpingLemma.ParserUtils.wordToSymbolicString($symbols);
	}
	;

symbolic_string 
	returns [ PumpingLemma.SymbolicString value = null ]
    :
	a=symbolic_string_h EOF {
		if ($a.value != null)
			$a.value.flatten();
		$value = $a.value;
	}
	;

symbolic_string_h 
	returns [ PumpingLemma.SymbolicString value = null ]
	:
   	  w=word { $value = $w.value; }
	| w=word ss=symbolic_string_h { $value = PumpingLemma.ParserUtils.join($w.value, $ss.value); }
	| w=word REPEAT i=integer { $value = PumpingLemma.ParserUtils.repeatLast($w.value, $i.value); }
	| w=word REPEAT i=integer ss=symbolic_string_h {
			$value = PumpingLemma.ParserUtils.join(
				PumpingLemma.ParserUtils.repeatLast($w.value, $i.value),
				$ss.value);
		}
	| LPAREN ss=symbolic_string_h RPAREN { $value = $ss.value; }
	| LPAREN ss=symbolic_string_h RPAREN ssp=symbolic_string_h {
			$value = PumpingLemma.ParserUtils.join($ss.value, $ssp.value);
		}
	| LPAREN ss=symbolic_string_h RPAREN REPEAT i=integer {
			$value = PumpingLemma.ParserUtils.repeat($ss.value, $i.value);
		}
	| LPAREN ss=symbolic_string_h RPAREN REPEAT i=integer ssp=symbolic_string_h {
			$value = PumpingLemma.ParserUtils.join(
				PumpingLemma.ParserUtils.repeat($ss.value, $i.value),
				$ssp.value);
		}
	;

condition
	returns [ PumpingLemma.BooleanExpression value = null ]
	:
	c=condition_h EOF { $value = $c.value; }
	;

condition_h
	returns [ PumpingLemma.BooleanExpression value = null ]
	:
	atom=atomic_condition { $value = $atom.value; }
	| LPAREN sub=condition_h RPAREN { $value = $sub.value; }
	| atom=atomic_condition AND sub2=condition_h { $value = PumpingLemma.LogicalExpression.And($atom.value, $sub2.value); }
	| LPAREN sub1=condition_h RPAREN AND sub2=condition_h { $value = PumpingLemma.LogicalExpression.And($sub1.value, $sub2.value); }
    | atom=atomic_condition OR sub2=condition_h { $value = PumpingLemma.LogicalExpression.Or($atom.value, $sub2.value); }
	| LPAREN sub1=condition_h RPAREN OR sub2=condition_h { $value = PumpingLemma.LogicalExpression.Or($sub1.value, $sub2.value); }
	| NOT LPAREN sub=condition_h RPAREN { $value = PumpingLemma.LogicalExpression.Not($sub.value); }
	;

atomic_condition
	returns [ PumpingLemma.ComparisonExpression value = null ]
	:
	| left=terms NEQ right=terms { $value = PumpingLemma.ComparisonExpression.NotEqual($left.value, $right.value); }
	| left=terms EQ right=terms  { $value = PumpingLemma.ComparisonExpression.Equal($left.value, $right.value); }
	| left=terms LT right=terms  { $value = PumpingLemma.ComparisonExpression.LessThan($left.value, $right.value); }
	| left=terms LEQ right=terms { $value = PumpingLemma.ComparisonExpression.LessThanOrEqual($left.value, $right.value); }
	| left=terms GT right=terms  { $value = PumpingLemma.ComparisonExpression.GreaterThan($left.value, $right.value); }
	| left=terms GEQ right=terms { $value = PumpingLemma.ComparisonExpression.GreaterThanOrEqual($left.value, $right.value); }
	;

/*
 * Lexer 
 */

// Ignore white space
WS	:	( ' ' | '\r' | '\n' ) -> skip ;


// Symbols
LPAREN : '(';
RPAREN : ')';
REPEAT : '^';
TIMES  : '*';
PLUS   : '+';
AND    : '&&';
OR     : '||';
NOT    : '!';
GEQ    : '>=';
LEQ    : '<=';
NEQ    : '!=';
GT     : '>';
LT     : '<';
EQ     : '==' | '=' ;

// Identifiers beginning with alphabet or underscore
IDENT : [a-zA-Z_][a-zA-Z0-9_]*;

// Integers
INT : [0-9]+;

// Catch everything else
INVALID : .;
