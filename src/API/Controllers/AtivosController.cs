using Application.Handlers.Ativos.Commands.Create;
using Application.Handlers.Ativos.Queries.GetAtivos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [Route("api/ativos")]
    [ApiController]
    public class AtivosController : ApiControllerBase
    {
        
        [HttpGet]
        public async Task<ActionResult> GetAtivos([FromQuery] GetAtivosQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        
        [HttpPost]
        public async Task<ActionResult> PostAtivo([FromBody] CreateAtivoCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

    }
}
