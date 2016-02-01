using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomataPDL
{
    [Serializable()]
    public class PDLException : Exception
    {
        public PDLException() : base() { }
        public PDLException(string message) : base(message) { }
        public PDLException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected PDLException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
