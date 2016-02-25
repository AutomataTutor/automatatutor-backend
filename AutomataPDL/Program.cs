/*
 *  The samples 1..9 illustrate typical use of the library Microsoft.Automata.Z3.dll
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;



using Microsoft.Automata;
using Microsoft.Z3;


using System.Xml;
using System.Threading;


namespace AutomataPDL
{
    class Program
    {
        static void Main(string[] args)
        {
            HashSet<char> al = new HashSet<char>(new char[]{'a','b'});
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            string rexpr = "a|b";

            var escapedRexpr = string.Format("^{0}$", rexpr);
            Automaton<BDD> aut = null;
            try
            {
                aut = solver.Convert(escapedRexpr);
            }
            catch (AutomataException e)
            {
                throw new PDLException("The input is not a well formatted regular expression."+e.Message);
            }

            var diff = aut.Minus(solver.Convert("^c$"), solver);
            if (!diff.IsEmpty)
                throw new PDLException("The regular expression should only accept strings over (a|b)*.");

            var auttt =  new Pair<HashSet<char>, Automaton<BDD>>(al, aut);
        }
       
    }
}
