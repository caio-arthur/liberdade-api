using Application.Common.DTOs;
using Application.Common.Models;
using Application.Handlers.Ativos.Commands.Create;
using Application.Handlers.Ativos.Queries.GetAtivos;
using Gridify;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/ativos")]
    [ApiController]
    public class AtivosController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Response<Paging<AtivoDto>>>> GetAtivos([FromQuery] GetAtivosQuery query)
        {
            var result = await Mediator.Send(query);
            if (result.Sucesso)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [HttpPost]
        public async Task<ActionResult<Response>> PostAtivo([FromBody] CreateAtivoCommand command)
        {
            var result = await Mediator.Send(command);
            if (result.Sucesso)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
    }
}
