using Application.Handlers.Transacoes.Commands.RealizarCompra;
using Application.Handlers.Transacoes.Commands.RegistrarAporte;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/transacoes")]
    [ApiController]
    public class TransacoesController : ApiControllerBase
    {
        [HttpPost("aporte")]
        public async Task<IActionResult> RegistrarAporte(RegistrarAporteCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("compra")]
        public async Task<IActionResult> RealizarCompra(RealizarCompraCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
