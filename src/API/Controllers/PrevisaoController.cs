using Application.Common.Models;
using Application.Handlers.Previsoes.Queries;
using Application.Handlers.Previsoes.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/previsao")]
    [ApiController]
    public class PrevisaoController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Response<PrevisaoRetornoDto>>> GetPrevisao([FromQuery] GetPrevisaoQuery request) 
        {
            var resposta = await Mediator.Send(request);
            return HandleResult(resposta);
        }
    }
}
