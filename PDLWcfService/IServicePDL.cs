using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Xml.Linq;

namespace PDLWcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IServicePDL" in both code and config file together.
    [ServiceContract]
    public interface IServicePDL
    {
        [OperationContract]
        [WebInvoke(Method = "POST",
            //BodyStyle = WebMessageBodyStyle.Bare,
            ResponseFormat = WebMessageFormat.Xml,
            RequestFormat = WebMessageFormat.Xml,
            UriTemplate = "feedback")]
        ResponseData ComputeFeedback(RequestData rData);
    }
}
