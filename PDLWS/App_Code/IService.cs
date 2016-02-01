using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Xml.Linq;

// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService" in both code and config file together.
[ServiceContract]
public interface IService
{
    [OperationContract]
    [WebInvoke(UriTemplate = "", Method ="POST")]
    string ComputeFeedback(CorrectAttempt ca);

    [OperationContract]
    [WebInvoke(UriTemplate = "add", Method = "POST")]
    int Add(NumberPair np);
}

[DataContract(Namespace = "")]
public class NumberPair
{
    [DataMember]
    public int First { get; set; }

    [DataMember]
    public int Second { get; set; }

}

[DataContract(Namespace="")]
public class CorrectAttempt
{
    XElement dfaCorrectDesc;
    XElement dfaAttemptDesc;

    [DataMember]
    public XElement DfaCorrectDesc
    {
        get { return dfaCorrectDesc; }
        set { dfaCorrectDesc = value; }
    }

    [DataMember]
    public XElement DfaAttemptDesc
    {
        get { return dfaAttemptDesc; }
        set { dfaAttemptDesc = value; }
    }
}