using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using Microsoft.Automata;
using AutomataPDL;
using System.Xml.Linq;

namespace PDLWcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ServicePDL" in code, svc and config file together.
    public class ServicePDL : IServicePDL
    {
        public ResponseData ComputeFeedback(RequestData rData)
        {

            CharSetSolver solver = new CharSetSolver(BitWidth.BV64);
            
            var correct = rData.CorrectAut;
            var attempt = rData.AttemptAut;

            var dfaCorrectPair = DFAUtilities.parseDFAFromXML(correct, solver);
            var dfaAttemptPair = DFAUtilities.parseDFAFromXML(attempt, solver);

            var feedbackGrade = DFAGrading.GetGrade(dfaCorrectPair.Second, dfaAttemptPair.Second, dfaCorrectPair.First, solver, 1000, 10, FeedbackLevel.Hint);
            
            var response = new ResponseData { Feedback = string.Format("Grade: {0}, Feedback: {1}", feedbackGrade.First, feedbackGrade.Second.ToString()) };
            
            //var sb = new StringBuilder();
            //sb.Append(rData.CorrectAut);
            //sb.Append(rData.AttemptAut);
            //var response = new ResponseData { Feedback = sb.ToString() };
            return response;
        }
    }
}
