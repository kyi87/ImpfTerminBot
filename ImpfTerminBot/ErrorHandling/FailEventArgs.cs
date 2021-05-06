using System;

namespace ImpfTerminBot.ErrorHandling
{

    public class FailEventArgs : EventArgs
    {
        public eErrorType eErrorType { get; set; }
        public string ErrorText { get; set; }
    }
}
