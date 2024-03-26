using System;

namespace CloudX.Auto.Core.Exceptions
{
    [Serializable]
    public class InitializationException: Exception
    {
        public InitializationException(string message) : base(message) { }
    }
}
