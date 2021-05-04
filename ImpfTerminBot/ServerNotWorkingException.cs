using System;
using System.Runtime.Serialization;

namespace ImpfTerminBot
{
    [Serializable]
    public class ServerNotWorkingException : Exception
    {
        public ServerNotWorkingException()
        {
        }

        public ServerNotWorkingException(string message) : base(message)
        {
        }

        public ServerNotWorkingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ServerNotWorkingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}