using Application.Handlers.Transacoes.Commands.DeleteTransacao;
using Application.Handlers.Transacoes.Commands.RealizarCompra;
using Application.Handlers.Transacoes.Commands.RegistrarAporte;
using Application.Handlers.Transacoes.Commands.UpdateTransacao;
using Application.Handlers.Transacoes.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [Route("api/transacoes")]
    [ApiController]
    public class TransacoesController : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetTransacoes([FromQuery] GetTransacoesQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        
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

        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransacao(Guid id, UpdateTransacaoCommand command)
        {
            command.Id = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransacao(Guid id)
        {
            var command = new DeleteTransacaoCommand { Id = id };
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
