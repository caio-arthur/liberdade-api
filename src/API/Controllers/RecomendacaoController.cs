using Application.Common.Models;
using Application.Handlers.Recomendacoes.Queries;
using Application.Handlers.Recomendacoes.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/recomendacao")]
    [ApiController]
    public class RecomendacaoController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Response<RecomendacaoDto>>> ObterRecomendacao([FromQuery] ObterRecomendacaoInvestimentoQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }
    }
}
