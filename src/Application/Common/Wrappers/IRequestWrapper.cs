using Application.Common.Models;
using MediatR;

namespace Application.Common.Wrappers
{
    public interface IRequestWrapper<TResponse> : IRequest<Response<TResponse>>
    {
    }

}
