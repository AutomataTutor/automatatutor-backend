using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace PDLWcfService
{
    [DataContract(Namespace = "http://automatagrader.com")]
    public class RequestData
    {
        [DataMember(Order=1)]
        public XElement CorrectAut { get; set; }

        [DataMember(Order=2)]
        public XElement AttemptAut { get; set; }
    }
}