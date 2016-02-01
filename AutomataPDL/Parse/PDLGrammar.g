grammar PDLGrammar;

options{
	language=CSharp2;	
	backtrack=true;
}

@namespace { AutomataPDL.Parse }
@header {using System.Text;}


@rulecatch {
    catch (RecognitionException re){
        if(re is NoViableAltException)
       {
            var e = re as NoViableAltException;
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Position {0}: bad input after '{1}'", e.CharPositionInLine, e.Token!=null? e.Token.Text: ((char)e.Character).ToString());
                throw new Parse.PDLParseException(msg.ToString());
       }
       else{
            if (re is MismatchedTokenException)
            {
            	var e = re as MismatchedTokenException;
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Position {0}: unexpected input '{1}', expecting {2}", e.CharPositionInLine, e.Token!=null? e.Token.Text: ((char)e.Character).ToString(),
                    e.TokenNames != null ? e.TokenNames[e.Expecting] : ((char)e.Expecting).ToString());
                throw new Parse.PDLParseException(msg.ToString());
            }
            else
            if(re is EarlyExitException)
            {
            	var e = re as EarlyExitException;
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Position {0}: something missing at input '{1}'", e.CharPositionInLine, e.Token.Text);
                throw new Parse.PDLParseException(msg.ToString());
            }
            else
                throw new PDLParseException(re.ToString());
       }
    }
}

@lexer::namespace { AutomataPDL.Parse }
@lexer::header {using System.Text;}

@lexer::members {
  public override void  ReportError(RecognitionException re) {
       if(re is NoViableAltException)
       {
            var e = re as NoViableAltException;
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Position {0}: bad input after  '{1}'", e.CharPositionInLine, e.Token!=null? e.Token.Text: ((char)e.Character).ToString());
                throw new Parse.PDLParseException(msg.ToString());
       }
       else{
            if (re is MismatchedTokenException)
            {
            	var e = re as MismatchedTokenException;
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Position {0}: unexpected input '{1}', expecting {2}", e.CharPositionInLine, e.Token!=null? e.Token.Text: ((char)e.Character).ToString(),
                    e.TokenNames != null ? e.TokenNames[e.Expecting] : ((char)e.Expecting).ToString());
                throw new Parse.PDLParseException(msg.ToString());
            }
            else
            if(re is EarlyExitException)
            {
            	var e = re as EarlyExitException;
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Position {0}: something missing at input '{1}'", e.CharPositionInLine, e.Token.Text);
                throw new Parse.PDLParseException(msg.ToString());
            }
            else
                throw new PDLParseException(re.ToString());
       }
  }
}

public parse returns [PDLPred phi]:
	phi1=pred EOF {phi=phi1;}
;	

/*------------------------------------------------------------------
 * PARSER RULES
 *------------------------------------------------------------------*/
pred returns [PDLPred phi]
  :  a=CHAR '@' p1=position {phi = new PDLAtPos(a.Text[0],p1);}
    |a=CHAR '@' s1=set {phi = new PDLAtSet(a.Text[0],s1);}
    | p1=position 'belTo' s1=set {phi = new PDLBelongs(p1,s1);}
    | p1=position ('=' |'==') p2=position {phi = new PDLPosEq(p1,p2);}
    | p1=position '<=' p2=position {phi = new PDLPosLeq(p1,p2);}
    | p1=position '<' p2=position {phi = new PDLPosLe(p1,p2);}
    | p1=position '>' p2=position {phi = new PDLPosGe(p1,p2);}        
    | p1=position '>=' p2=position {phi = new PDLPosGeq(p1,p2);}            
    | 'IsSucc' '(' p1=position ',' p2=position ')' {phi = new PDLIsSuccessor(p1,p2);}            
    | 'IsPred' '(' p1=position ',' p2=position ')' {phi = new PDLIsPredecessor(p1,p2);}                    
    | '|' s1=set '|' '%' n=INT ('=' |'==') m=INT {phi = new PDLModSetEq(s1,int.Parse(n.Text),int.Parse(m.Text));}            
    | '|' s1=set '|' '%' n=INT '<=' m=INT  {phi = new PDLModSetLeq(s1,int.Parse(n.Text),int.Parse(m.Text));}                
    | '|' s1=set '|' '%' n=INT '<' m=INT  {phi = new PDLModSetLe(s1,int.Parse(n.Text),int.Parse(m.Text));}                
    | '|' s1=set '|' '%' n=INT '>' m=INT  {phi = new PDLModSetGe(s1,int.Parse(n.Text),int.Parse(m.Text));}                
    | '|' s1=set '|' '%' n=INT '>=' m=INT  {phi = new PDLModSetGeq(s1,int.Parse(n.Text),int.Parse(m.Text));}                            
    | '|' s1=set '|' ('=' |'==') m=INT  {phi = new PDLIntEq(s1,int.Parse(m.Text));}            
    | '|' s1=set '|' '<=' m=INT  {phi = new PDLIntLeq(s1,int.Parse(m.Text));}                
    | '|' s1=set '|' '<' m=INT  {phi = new PDLIntLe(s1,int.Parse(m.Text));}                
    | '|' s1=set '|' '>' m=INT  {phi = new PDLIntGe(s1,int.Parse(m.Text));}                
    | '|' s1=set '|' '>=' m=INT  {phi = new PDLIntGeq(s1,int.Parse(m.Text));}     
    | ('all1'|'all') x=FOVARIABLE '.' phi1=pred {phi = new PDLForallFO(x.Text,phi1);}         
    | ('ex1' | 'ex') x=FOVARIABLE '.' phi1=pred {phi = new PDLExistsFO(x.Text,phi1);}  
    | 'all2' x=SOVARIABLE '.' phi1=pred {phi = new PDLForallSO(x.Text,phi1);}         
    | 'ex2' x=SOVARIABLE '.' phi1=pred {phi = new PDLExistsSO(x.Text,phi1);}                      
    | '('phi1=pred ('&'|'and') phi2=pred')' {phi = new PDLAnd(phi1,phi2);}             
    | '('phi1=pred ('V'|'|'|'or') phi2=pred')' {phi = new PDLOr(phi1,phi2);}                 
    | '('phi1=pred ('->'|'-->') phi2=pred')' {phi = new PDLIf(phi1,phi2);}                 
    | '('phi1=pred '<->' phi2=pred')' {phi = new PDLIff(phi1,phi2);}                         
    | ('!'|'not') phi1=pred {phi = new PDLNot(phi1);}         
    | 'true' {phi = new PDLTrue();}         
    | 'false' {phi = new PDLFalse();}             
    | 'startsWith' '\'' s=(STRING | CHAR) '\'' {phi = new PDLStartsWith(s.Text);}         
    | 'endsWith' '\'' s=(STRING | CHAR) '\'' {phi = new PDLEndsWith(s.Text);}     
    | 'isString' '\'' s=(STRING | CHAR) '\'' {phi = new PDLIsString(s.Text);}         
    | 'contains' '\'' s=(STRING | CHAR) '\'' {phi = new PDLEndsWith(s.Text);}
    | 'emptyStr' {phi = new PDLEmptyString();}      
    | '(' phi1=pred ')' {phi = phi1;}      
  ;

position returns [PDLPos pp]:
	s=FOVARIABLE {pp=new PDLPosVar(s.Text);}
    | 'first' {pp=new PDLFirst();}	
    | 'last' {pp=new PDLLast();}	    
    | 'S' '(' p1=position ')' {pp=new PDLSuccessor(p1);}	        
    | 'P' '(' p1=position ')' {pp=new PDLPredecessor(p1);}	   
    | 'firstOcc' '\'' str=(STRING | CHAR) '\'' {pp = new PDLFirstOcc(str.Text);}    
    | 'lastOcc' '\'' str=(STRING | CHAR) '\'' {pp = new PDLLastOcc(str.Text);}     
    | '(' p1=position ')' {pp = p1;}             
;

set returns [PDLSet ss]:
       s=SOVARIABLE {ss=new PDLSetVar(s.Text);} 
    | 'indOf' '\'' str=(STRING | CHAR) '\'' {ss = new PDLIndicesOf(str.Text);}   
    | 'allPos' {ss = new PDLAllPos();}       
    | 'allBefore' p1=position {ss = new PDLAllPosBefore(p1);}          
    | 'allAfter' p1=position {ss = new PDLAllPosAfter(p1);}              
    | 'allUpto' p1=position {ss = new PDLAllPosUpto(p1);}          
    | 'allFrom' p1=position {ss = new PDLAllPosFrom(p1);}                        
    | '(' s1=set ('inters') s2=set')' {ss = new PDLIntersect(s1,s2);}      
    | '(' s1=set ('union'|'U') s2=set')' {ss = new PDLUnion(s1,s2);}          
    | '{' s=FOVARIABLE '|' phi=pred'}' {ss = new PDLPredSet(s.Text,phi);}    
    | '(' s1=set ')' {ss = s1;}                           
   ;

/*------------------------------------------------------------------
 * LEXER RULES
 *------------------------------------------------------------------*/

FOVARIABLE: ('x'..'z')('0'..'9')* ;
SOVARIABLE: ('X'..'Z')('0'..'9')* ;
CHAR: ('a'|'b') ;
STRING	: CHAR+;
INT: ('0'..'9');

WS  :   ( ' '
        | '\t'
        | '\r'
        | '\n'
        ) {$channel=Hidden;}
    ;