using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Xml.Linq;
using System.Text;

using Microsoft.Automata;
using AutomataPDL;
using AutomataPDL.CFG;

using System.Diagnostics;

namespace WebServicePDL
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://automatagrader.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service1 : System.Web.Services.WebService
    {

        [WebMethod]
        public XElement ComputeFeedbackXML(XElement dfaCorrectDesc, XElement dfaAttemptDesc, XElement maxGrade, XElement feedbackLevel, XElement enabledFeedbacks)
        {
            #region Check if item is in cache
            StringBuilder key = new StringBuilder();
            key.Append("feed");
            key.Append(dfaCorrectDesc.ToString());
            key.Append(dfaAttemptDesc.ToString());
            key.Append(feedbackLevel.ToString());
            key.Append(enabledFeedbacks.ToString());
            string keystr = key.ToString();

            var cachedValue = HttpContext.Current.Cache.Get(key.ToString());
            if (cachedValue != null)
            {
                HttpContext.Current.Cache.Remove(keystr);
                HttpContext.Current.Cache.Add(keystr, cachedValue, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(30), System.Web.Caching.CacheItemPriority.Normal, null);
                return (XElement)cachedValue;
            } 
            #endregion
            
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            //Read input 
            var dfaCorrectPair = DFAUtilities.parseDFAFromXML(dfaCorrectDesc, solver);
            var dfaAttemptPair = DFAUtilities.parseDFAFromXML(dfaAttemptDesc, solver);

            var level = (FeedbackLevel) Enum.Parse(typeof(FeedbackLevel), feedbackLevel.Value, true);
            var enabList = (enabledFeedbacks.Value).Split(',').ToList<String>();
            //bool dfaedit = enabList.Contains("dfaedit"), moseledit = enabList.Contains("moseledit"), density = enabList.Contains("density");
            bool dfaedit =true, moseledit = true, density = true;

            var maxG = int.Parse(maxGrade.Value);

            //Compute feedback
            var feedbackGrade = DFAGrading.GetGrade(dfaCorrectPair.Second, dfaAttemptPair.Second, dfaCorrectPair.First, solver, 1500, maxG, level, dfaedit, density, moseledit);

            //Pretty print feedback
            var feedString = "<ul>";
            foreach (var feed in feedbackGrade.Second)
            {
                feedString += string.Format("<li>{0}</li>", feed);
                break;
            }
            feedString += "</ul>";

            //var output = string.Format("<result><grade>{0}</grade><feedString>{1}</feedString></result>", feedbackGrade.First, feedString);
            var outXML = new XElement("result",  
                                    new XElement("grade", feedbackGrade.First),
                                    new XElement("feedString", XElement.Parse(feedString)));
            //XElement outXML = XElement.Parse(output);
            //Add this element to chace and return it
            HttpContext.Current.Cache.Add(key.ToString(), outXML, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(30), System.Web.Caching.CacheItemPriority.Normal, null);

            return outXML;
        }

        


        [WebMethod]
        public XElement ComputeFeedbackNFAXML(XElement nfaCorrectDesc, XElement nfaAttemptDesc, XElement maxGrade, XElement feedbackLevel, XElement enabledFeedbacks, XElement userId)
        {
            #region Check if item is in cache
            StringBuilder key = new StringBuilder();
            key.Append("feed");
            key.Append(nfaCorrectDesc.ToString());
            key.Append(nfaAttemptDesc.ToString());
            key.Append(feedbackLevel.ToString());
            key.Append(enabledFeedbacks.ToString());
            string keystr = key.ToString();

            var cachedValue = HttpContext.Current.Cache.Get(key.ToString());
            if (cachedValue != null)
            {
                HttpContext.Current.Cache.Remove(keystr);
                HttpContext.Current.Cache.Add(keystr, cachedValue, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(30), System.Web.Caching.CacheItemPriority.Normal, null);
                return (XElement)cachedValue;
            }
            #endregion

            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            //Read input 
            var nfaCorrectPair = DFAUtilities.parseNFAFromXML(nfaCorrectDesc, solver);
            var nfaAttemptPair = DFAUtilities.parseNFAFromXML(nfaAttemptDesc, solver);

            var level = (FeedbackLevel)Enum.Parse(typeof(FeedbackLevel), feedbackLevel.Value, true);
            var enabList = (enabledFeedbacks.Value).Split(',').ToList<String>();

            var maxG = int.Parse(maxGrade.Value);

            //Use this for generating 2 classes of feedback for reyjkiavik study
            var studentIdModule = int.Parse(userId.Value) % 2;
            //if (studentIdModule == 0)
            //    level = FeedbackLevel.Minimal;
            //else
            //    level = FeedbackLevel.Hint;
            //Give hints to everyone
            level = FeedbackLevel.Hint;

            //Compute feedback
            var feedbackGrade = NFAGrading.GetGrade(nfaCorrectPair.Second, nfaAttemptPair.Second, nfaCorrectPair.First, solver, 1500, maxG, level);

            //Pretty print feedback
            var feedString = "<ul>";
            foreach (var feed in feedbackGrade.Second)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";

            var output = string.Format("<div><grade>{0}</grade><feedString>{1}</feedString></div>", feedbackGrade.First, feedString);

            XElement outXML = XElement.Parse(output);
            //Add this element to chace and return it
            HttpContext.Current.Cache.Add(key.ToString(), outXML, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(30), System.Web.Caching.CacheItemPriority.Normal, null);

            return outXML;
        }

        [WebMethod]
        public XElement ComputeFeedbackNfaToDfa(XElement nfaCorrectDesc, XElement dfaAttemptDesc, XElement maxGrade)
        {
            #region Check if item is in cache
            StringBuilder key = new StringBuilder();
            key.Append("feedNFADFA");
            key.Append(nfaCorrectDesc.ToString());
            key.Append(dfaAttemptDesc.ToString());
            string keystr = key.ToString();

            var cachedValue = HttpContext.Current.Cache.Get(key.ToString());
            if (cachedValue != null)
            {
                HttpContext.Current.Cache.Remove(keystr);
                HttpContext.Current.Cache.Add(keystr, cachedValue, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(30), System.Web.Caching.CacheItemPriority.Normal, null);
                return (XElement)cachedValue;
            }
            #endregion

            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            //Read input 
            var nfaCorrectPair = DFAUtilities.parseNFAFromXML(nfaCorrectDesc, solver);
            var dfaCorrect = nfaCorrectPair.Second.RemoveEpsilons(solver.MkOr).Determinize(solver).Minimize(solver);

            var dfaAttemptPair = DFAUtilities.parseDFAFromXML(dfaAttemptDesc, solver);

            var level = FeedbackLevel.Hint;

            var maxG = int.Parse(maxGrade.Value);            

            //Compute feedback
            var feedbackGrade = DFAGrading.GetGrade(dfaCorrect, dfaAttemptPair.Second, nfaCorrectPair.First, solver, 1500, maxG, level);

            //Pretty print feedback
            var feedString = "<ul>";
            foreach (var feed in feedbackGrade.Second)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";

            var output = string.Format("<div><grade>{0}</grade><feedString>{1}</feedString></div>", feedbackGrade.First, feedString);

            XElement outXML = XElement.Parse(output);
            //Add this element to chace and return it
            HttpContext.Current.Cache.Add(key.ToString(), outXML, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(30), System.Web.Caching.CacheItemPriority.Normal, null);

            return outXML;
        }

        //---------------------------
        // Pumping lemma methods
        //---------------------------

        [WebMethod]
        public XElement ComputeFeedbackRegexp(XElement regexCorrectDesc, XElement regexAttemptDesc, XElement alphabet, XElement feedbackLevel, XElement enabledFeedbacks, XElement maxGrade)
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            try
            {
                var dfaCorrectPair = DFAUtilities.parseRegexFromXML(regexCorrectDesc, alphabet, solver);
                var dfaAttemptPair = DFAUtilities.parseRegexFromXML(regexAttemptDesc, alphabet, solver);

                var level = (FeedbackLevel)Enum.Parse(typeof(FeedbackLevel), feedbackLevel.Value, true);

                var enabList = (enabledFeedbacks.Value).Split(',').ToList<String>();
                bool dfaedit = false, moseledit = false, density = true;
                int maxG = int.Parse(maxGrade.Value);

                var feedbackGrade = DFAGrading.GetGrade(dfaCorrectPair.Second, dfaAttemptPair.Second, dfaCorrectPair.First, solver, 1500, maxG, level, dfaedit, moseledit, density);

                var feedString = "<ul>";
                foreach (var feed in feedbackGrade.Second)
                    feedString += string.Format("<li>{0}</li>", feed);
                feedString += "</ul>";


                return XElement.Parse(string.Format("<div><grade>{0}</grade><feedback>{1}</feedback></div>", feedbackGrade.First, feedString));
            }
            catch (PDLException pdlex)
            {
                return XElement.Parse(string.Format("<div>Error: {0} </div>", pdlex.Message));
            }
        }

        [WebMethod]
        public XElement CheckRegexp(XElement regexDesc, XElement alphabet)
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);

            try
            {
                var dfaCorrectPair = DFAUtilities.parseRegexFromXML(regexDesc, alphabet, solver);
                return XElement.Parse(string.Format("<div>CorrectRegex</div>"));
            }
            catch (PDLException pdlex)
            {
                return XElement.Parse(string.Format("<div>Error: {0} </div>", pdlex.Message));
            }
        }

        //---------------------------
        // Grammar methods
        //---------------------------
        private static Func<char, char> terminalCreation = delegate (char x)
        {
            return x;
        };

        [WebMethod]
        public XElement CheckGrammar(XElement grammar)
        {
            try
            {
                var parsed = AutomataPDL.CFG.GrammarParser<char>.Parse(terminalCreation, grammar.Value);

                return XElement.Parse(string.Format("<div>CorrectGrammar</div>"));
            }
            catch (AutomataPDL.CFG.ParseException ex)
            {
                return XElement.Parse(string.Format("<div>Error: {0} </div>", ex.Message));
            }
        }

        [WebMethod]
        public XElement isCNF(XElement grammar)
        {
            try
            {
                var parsed = AutomataPDL.CFG.GrammarParser<char>.Parse(terminalCreation, grammar.Value);

                var feedString = "<ul>";
                bool allCNF = true;
                List<String> feedback = new List<String>();
                foreach (Production p in parsed.GetProductions())
                {
                    if (!p.IsCNF)
                    {
                        allCNF = false;
                        feedString += string.Format("<li>The production \"{0}\" is not in CNF...</li>", p);
                    }
                }
                feedString += "</ul>";

                if (allCNF) return XElement.Parse("<div><res>y</res><feedback></feedback></div>");

                return XElement.Parse(string.Format("<div><res>n</res><feedback>{0}</feedback></div>", feedString));
            }
            catch (AutomataPDL.CFG.ParseException ex)
            {
                return XElement.Parse("<div><res>n</res><feedback>not parseable</feedback></div>");
            }
        }

        [WebMethod]
        public XElement ComputeWordsInGrammarFeedback(XElement grammar, XElement wordsIn, XElement wordsOut, XElement maxGrade)
        {
            //read inputs
            ContextFreeGrammar g;
            try
            {
                g = AutomataPDL.CFG.GrammarParser<char>.Parse(terminalCreation, grammar.Value);
            }
            catch (AutomataPDL.CFG.ParseException ex)
            {
                return XElement.Parse(string.Format("<div>Error: {0} </div>", ex.Message));
            }
            int maxG = int.Parse(maxGrade.Value);
            List<String> wordsInList = new List<String>(), wordsOutList = new List<String>();
            foreach (var wordElement in wordsIn.Elements())
            {
                wordsInList.Add(wordElement.Value);
            }
            foreach (var wordElement in wordsOut.Elements())
            {
                wordsOutList.Add(wordElement.Value);
            }

            //grade
            var result = GrammarGrading.gradeWordsInGrammar(g, wordsInList, wordsOutList, maxG);

            //build return value
            var feedString = "<ul>";
            foreach (var feed in result.Item2)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";
            int grade = result.Item1;

            return XElement.Parse(string.Format("<div><grade>{0}</grade><feedback>{1}</feedback></div>", grade, feedString));
        }

        [WebMethod]
        public XElement ComputeGrammarEqualityFeedback(XElement solution, XElement attempt, XElement maxGrade, XElement checkEmptyWord)
        {
            //read inputs
            ContextFreeGrammar sol, att;
            try
            {
                sol = AutomataPDL.CFG.GrammarParser<char>.Parse(terminalCreation, solution.Value);
                att = AutomataPDL.CFG.GrammarParser<char>.Parse(terminalCreation, attempt.Value);
            }
            catch (AutomataPDL.CFG.ParseException ex)
            {
                return XElement.Parse(string.Format("<div><grade>{0}</grade><feedback>{1}</feedback></div>", -1, ex.Message));
            }
            int maxG = int.Parse(maxGrade.Value);
            bool checkEW = bool.Parse(checkEmptyWord.Value);

            //ignore empty string?
            if (!checkEW)
            {
                att.setAcceptanceForEmptyString(sol.acceptsEmptyString());
            }

            //grade
            var result = GrammarGrading.gradeGrammarEquality(sol, att, maxG, 1000);

            //build return value
            var feedString = "<ul>";
            foreach (var feed in result.Item2)
                feedString += string.Format("<li>{0}</li>", feed);
            feedString += "</ul>";
            int grade = result.Item1;

            return XElement.Parse(string.Format("<div><grade>{0}</grade><feedback>{1}</feedback></div>", grade, feedString));
        }

        //---------------------------
        // Pumping lemma methods
        //---------------------------

        // Checks whether an arithmetic language description parses correctly
        [WebMethod]
        public XElement CheckArithLanguageDescription(
            XElement languageDesc,
            XElement constraintDesc, 
            XElement alphabet,
            XElement pumpingString)
        {

            // Please change this to return only if the pumping string
            // is a solution to the pumping problem
            try
            {
                // This is super shady 
                var symbols = alphabet.Descendants().Where(e => e.Name.LocalName == "symbol");
                List<string> alphabetList = symbols.Select(x => x.Value).ToList();
                var language = PumpingLemma.ArithmeticLanguage.FromTextDescriptions(alphabetList, languageDesc.Value, constraintDesc.Value);

                var pumpingSymString = PumpingLemma.SymbolicString.FromTextDescription(alphabetList, pumpingString.Value);
                if (pumpingSymString.GetIntegerVariables().Count > 1)
                    throw new PumpingLemma.PumpingLemmaException("Only one variable allowed in the pumping string!");

                return XElement.Parse(string.Format("<div>CorrectLanguageDescription</div>"));
                // if (PumpingLemma.ProofChecker.check(language, pumpingSymString))
                    // return XElement.Parse(string.Format("<div>CorrectLanguageDescription</div>"));
                // else
                    // throw new PumpingLemma.PumpingLemmaException("Unable to prove non-regularity of language using the pumping string!");
            }
            catch (PumpingLemma.PumpingLemmaException ex)
            {
                return XElement.Parse(string.Format("<div>Error: {0} </div>", ex.Message));
            }
            catch (Exception ex)
            {
                return XElement.Parse(string.Format("<div>Internal Error: {0} </div>", ex.ToString()));
            }
        }

        // Checks whether an arithmetic language description parses correctly
        [WebMethod]
        public XElement GenerateStringSplits(
            XElement languageDesc,
            XElement constraintDesc,
            XElement alphabet,
            XElement pumpingString)
        {
            try
            {
                var symbols = alphabet.Descendants().Where(e => e.Name.LocalName == "symbol");
                List<string> alphabetList = symbols.Select(x => x.Value).ToList();

                var language = PumpingLemma.ArithmeticLanguage.FromTextDescriptions(alphabetList, languageDesc.Value, constraintDesc.Value);

                var pumpingSymString = PumpingLemma.SymbolicString.FromTextDescription(alphabetList, pumpingString.Value);
                if (pumpingSymString.GetIntegerVariables().Count > 1)
                    throw new PumpingLemma.PumpingLemmaException("Only one variable allowed in the pumping string!");

                var pumpingLength = pumpingSymString.GetIntegerVariables().First();
                var pumpingLengthVariable = PumpingLemma.LinearIntegerExpression
                    .SingleTerm(1, pumpingLength);
                var additionalConstraint = PumpingLemma.ComparisonExpression.GreaterThanOrEqual(
                    pumpingLengthVariable,
                    PumpingLemma.LinearIntegerExpression.Constant(0)
                    );
                return pumpingSymString.SplitDisplayXML(pumpingLength, additionalConstraint);
            }
            catch (PumpingLemma.PumpingLemmaException e)
            {
                return XElement.Parse(string.Format("<error>Error: {0} </error>", e.Message));
            }
            catch (PDLException pdlex)
            {
                return XElement.Parse(string.Format("<error>Error: {0} </error>", pdlex.Message));
            }
                /*
            catch (Exception e)
            {
                return XElement.Parse(string.Format("<error>Internal Error: {0} </error>", e.Message));
            }
                 */
        }

        // Checks whether an arithmetic language description parses correctly
        [WebMethod]
        public XElement GetPumpingLemmaFeedback(
            XElement languageDesc,
            XElement constraintDesc,
            XElement alphabet,
            XElement pumpingString,
            XElement pumpingNumbers)
        {
            throw new NotImplementedException();

            //pumping numbers come as <pumps><pump>5</pump><pump>-1</pump><pump>p</pump></pumps>
            try
            {
                // Parse the language
                // Parse the constraintDesc
                // Make sure all the variables are bound

                // Verify using pumping string
                // Return whether the string is correct

                return XElement.Parse(string.Format("<result><grade>{0}</grade><feedback>{1}</feedback></result>", "10", "Try again"));
            }
            catch (PDLException pdlex)
            {
                return XElement.Parse(string.Format("<error>Error: {0} </error>", "you can only pump by a function of p"));
            }
        }
    }
}