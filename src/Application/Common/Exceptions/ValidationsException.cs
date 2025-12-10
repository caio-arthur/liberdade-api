using Application.Common.Models;

namespace Application.Common.Exceptions
{
    public class ValidationsException : Exception
    {
        public Error Error { get; }

        public ValidationsException(Error error) : base(error.Mensagem)
        {
            Error = error;
        }
    }
}
