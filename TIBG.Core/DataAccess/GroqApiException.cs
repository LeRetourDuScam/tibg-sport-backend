using System;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Represents errors when communicating with the Groq API.
    /// </summary>
    public class GroqApiException : Exception
    {
        public GroqApiException(string message) : base(message)
        {
        }

        public GroqApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
