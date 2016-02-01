using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomataPDL.Parse
{
    [Serializable()]
    public class PDLParseException : Exception
    {
        public PDLParseException() : base() { }
        public PDLParseException(string message) : base(message) { }
        public PDLParseException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected PDLParseException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
