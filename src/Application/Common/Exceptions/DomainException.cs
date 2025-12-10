using Application.Common.Models;

namespace Application.Common.Exceptions
{
    public class DomainException : Exception
    {
        public Error Error { get; }

        public DomainException(Error error) : base(error.Mensagem)
        {
            Error = error;
        }
    }
}