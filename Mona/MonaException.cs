using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mona
{
    [Serializable()]
    public class MonaException : Exception
    {
        public MonaException() : base() { }
        public MonaException(string message) : base(message) { }
        public MonaException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected MonaException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
