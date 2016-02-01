using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using Microsoft.Automata;
using AutomataPDL;

// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service" in code, svc and config file together.
public class Service : IService
{
    public string ComputeFeedback(CorrectAttempt ca)
    {
        if (ca == null)
        {
            throw new ArgumentNullException("composite");
        }

        XElement dfaCorrectDesc = ca.DfaCorrectDesc;
        XElement dfaAttemptDesc = ca.DfaAttemptDesc;
        CharSetSolver solver = new CharSetSolver(CharacterEncoding.Unicode);

        //XElement dfaAttemptDesc = dfaAttemptHint.Element("Automaton");
        //FeedbackLevel level = (FeedbackLevel)Convert.ToInt32(dfaAttemptHint.Element("FeedbackLevel").Value);

        var dfaCorrectPair = DFAUtilities.parseDFAFromXML(dfaCorrectDesc, solver);
        var dfaAttemptPair = DFAUtilities.parseDFAFromXML(dfaAttemptDesc, solver);

        var feedbackGrade = Grading.GetGrade(dfaCorrectPair.Second, dfaAttemptPair.Second, dfaCorrectPair.First, solver, 1000, 10, FeedbackLevel.Hint);

        return string.Format("Grade: {0}, Feedback: {1}", feedbackGrade.First, feedbackGrade.Second.ToString());
    }

    public int Add(NumberPair np)
    {
        return np.First + np.Second;
    }
}
