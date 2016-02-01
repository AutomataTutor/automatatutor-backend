using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PumpingLemma;

namespace PumpingLemmaTest
{
    [TestClass]
    public class ArithmeticLanguageTests
    {
        void BuildAndExpectFailure(string alphabetText, string languageText, string constraintText)
        {
            try
            {
                Console.WriteLine("Building for (" + alphabetText + "), (" + languageText + "), (" + constraintText + ")");
                ArithmeticLanguage.FromTextDescriptions(alphabetText, languageText, constraintText);
                Assert.Fail();
            }
            catch (PumpingLemmaException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception: " + e.ToString());
                Assert.Fail();
            }
        }
        void BuildAndExpectSuccess(string alphabetText, string languageText, string constraintText)
        {
            try
            {
                ArithmeticLanguage.FromTextDescriptions(alphabetText, languageText, constraintText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestBuilds()
        {
            BuildAndExpectSuccess("a b", "a^i b^j", "i = j");
            BuildAndExpectSuccess("a  b", "a^i b^j", "i = j");
            BuildAndExpectFailure("a cc b", "a^i b^j", "i = j");
            BuildAndExpectFailure("a b", "a^i b^j", "i = k");
        }
    }
}
