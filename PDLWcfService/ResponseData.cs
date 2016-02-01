using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace PDLWcfService
{
    [DataContract]
    public class ResponseData
    {
        [DataMember]
        public string Feedback { get; set; }
    }
}