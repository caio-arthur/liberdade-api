using Application.Common.Models;
using Application.Handlers.Notificacoes.Commands;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/notificacoes")]
    [ApiController]
    public class NotificacoesController : ApiControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<Response>> PostNotificacao([FromBody] EnviarNotificacaoCommand command)
        {
            var result =  await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
