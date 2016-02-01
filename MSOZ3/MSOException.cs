using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSOZ3
{
    [Serializable()]
    class MSOException: Exception
    {
        public MSOException() : base() { }
        public MSOException(string message) : base(message) { }
        public MSOException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected MSOException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
