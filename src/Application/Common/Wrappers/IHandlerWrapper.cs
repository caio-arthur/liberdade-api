using Application.Common.Models;
using MediatR;

namespace Application.Common.Wrappers
{
    // Adiciona restrição para garantir que TRequest implemente IRequest<Response<TResponse>>
    public interface IHandlerWrapper<in TRequest, TResponse> : IRequestHandler<TRequest, Response<TResponse>>
        where TRequest : IRequestWrapper<TResponse>
    {
    }
}
