using Application.Handlers.Rotinas.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [Route("api/rotinas")]
    public class RotinasController : ApiControllerBase
    {
        
        [HttpPost("atualizar-mercado")]
        public async Task<ActionResult> AtualizarMercado()
        {
            await Mediator.Send(new AtualizarMercadoCommand());
            return Ok(new { Message = "Rotina de atualização de mercado executada com sucesso." });
        }

        
        [HttpPost("diario-financeiro")]
        public async Task<ActionResult> DiarioFinanceiro()
        {
            await Mediator.Send(new GerarDiarioFinanceiroCommand());
            return Ok(new { Message = "Rotina de diário financeiro executada com sucesso." });
        }
    }
}
