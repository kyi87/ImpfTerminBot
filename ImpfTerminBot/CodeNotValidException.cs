using System;
using System.Runtime.Serialization;

namespace ImpfTerminBot
{
    [Serializable]
    public class CodeNotValidException : Exception
    {
        public CodeNotValidException()
        {
        }

        public CodeNotValidException(string message) : base(message)
        {
        }

        public CodeNotValidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CodeNotValidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}