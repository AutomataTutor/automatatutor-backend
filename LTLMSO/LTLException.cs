using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSOZ3
{
    [Serializable()]
    class LTLException: Exception
    {
        public LTLException() : base() { }
        public LTLException(string message) : base(message) { }
        public LTLException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected LTLException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
