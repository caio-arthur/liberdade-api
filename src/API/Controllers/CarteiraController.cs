using Application.Common.Models;
using Application.Handlers.CarteiraPosicoes.Queries;
using Application.Handlers.CarteiraPosicoes.Responses;
using Application.Handlers.HistoricosPatrimonio.Queries;
using Application.Handlers.HistoricosPatrimonio.Responses;
using Application.Handlers.MetaAlocacoes.Queries;
using Application.Handlers.MetaAlocacoes.Responses;
using Application.Handlers.Previsoes.Queries;
using Application.Handlers.Previsoes.Responses;
using Application.Handlers.Recomendacoes.Queries;
using Application.Handlers.Recomendacoes.Responses;
using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [Route("api/carteira")]
    [ApiController]
    public class CarteiraController : ApiControllerBase
    {
        [HttpGet("historico/patrimonio")]
        public async Task<ActionResult<Response<Paging<HistoricoPatrimonioDto>>>> GetHistoricoPatrimonio([FromQuery] GetHistoricoPatrimonioQuery request)
        {
            var resposta = await Mediator.Send(request);
            return HandleResult(resposta);
        }

        [HttpGet("posicao")]
        public async Task<ActionResult<Response<CarteiraPosicaoDto>>> GetPosicaoCarteira([FromQuery] GetCarteiraPosicaoQuery request)
        {
            var resposta = await Mediator.Send(request);
            return HandleResult(resposta);
        }


        [HttpGet("previsao")]
        public async Task<ActionResult<Response<PrevisaoRetornoDto>>> GetPrevisao([FromQuery] GetPrevisaoQuery request)
        {
            var resposta = await Mediator.Send(request);
            return HandleResult(resposta);
        }

        [HttpGet("recomendacao")]
        public async Task<ActionResult<Response<RecomendacaoDto>>> ObterRecomendacao([FromQuery] ObterRecomendacaoInvestimentoQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);

        }

        [HttpGet("meta-alocacao")]
        public async Task<ActionResult<Response<MetaAlocacaoDto>>> ObterMetaAlocacao()
        {
            var result = await Mediator.Send(new ObterMetaAlocacaoQuery());
            return HandleResult(result);
        }

    }
}
