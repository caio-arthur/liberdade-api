using Application.Common.Models;

namespace Application.Common.Exceptions
{
    public class ValidationException : Exception
    {
        public Error Error { get; }

        public ValidationException(Error error) : base(error.Mensagem)
        {
            Error = error;
        }
    }
}