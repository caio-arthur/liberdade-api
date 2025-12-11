using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        private IMediator _mediator;

        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();

        protected ActionResult HandleResult<T>(Response<T> result)
        {
            if (result.Sucesso)
            {
                return Ok(result);
            }

            var statusCode = result.Erro?.Codigo ?? 400;
            return StatusCode(statusCode, result);
        }

        protected ActionResult HandleResult(Response result)
        {
            if (result.Sucesso)
            {
                return Ok(result);
            }

            var statusCode = result.Erro?.Codigo ?? 400;
            return StatusCode(statusCode, result);
        }
    }
}
