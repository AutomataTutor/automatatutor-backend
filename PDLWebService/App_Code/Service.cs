using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using AutomataPDL;

using Microsoft.Automata.Z3;
using Microsoft.Automata;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
// [System.Web.Script.Services.ScriptService]

public class Service : System.Web.Services.WebService
{
    public Service () {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    [WebMethod]
    public string ComputeFeedback(string dfaCorrectDesc, string dfaAttemptDesc) {

        CharSetSolver solver = new CharSetSolver(CharacterEncoding.Unicode);

        var dfaCorrectPair = DFAUtilities.parseDFAFromString(dfaCorrectDesc, solver);
        var dfaAttemptPair = DFAUtilities.parseDFAFromString(dfaAttemptDesc, solver);

        var feedbackGrade = Grading.GetGrade(dfaCorrectPair.Second, dfaAttemptPair.Second, dfaCorrectPair.First, solver, 2000, 10, FeedbackLevel.Hint);

        return string.Format("Grade: {0}, Feedback: {1}",feedbackGrade.First,feedbackGrade.Second.ToString());
    }
    
}