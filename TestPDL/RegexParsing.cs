using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Z3;
using MSOZ3;
using AutomataPDL;

using System.Diagnostics;
using System.Threading;
using System.IO;

namespace TestPDL
{
    [TestClass]
    public class RegexParsing
    {

        [TestMethod]
        public void ParseRegex()
        {
            try
            {

                CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
                var al = new HashSet<char>(new char[] { 'a', 'b' });
                DFAUtilities.parseRegexFromXML(XElement.Parse("<reg>-?</reg>"), XElement.Parse("<alphabet><symbol>a</symbol><symbol>b</symbol></alphabet>"), solver);

            }
            catch (PDLException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
