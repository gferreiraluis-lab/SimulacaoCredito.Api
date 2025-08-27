using Microsoft.AspNetCore.Mvc;
using SimulacaoCredito.Api.Infrastructure.Persistence;
using SimulacaoCredito.Api.Models;
using SimulacaoCredito.Api.Services;

namespace SimulacaoCredito.Api.Controllers;

[ApiController]
[Route("credito/v1/telemetria")]
public class TelemetriaController : ControllerBase
{
    private readonly ISimulacaoRepository _repo;
    private readonly RequestTelemetria _telemetry;

    public TelemetriaController(ISimulacaoRepository repo, RequestTelemetria telemetry)
    {
        _repo = repo;
        _telemetry = telemetry;
    }


    [HttpGet]
    public ActionResult<TelemetriaRespostaVm> Get([FromServices] RequestTelemetria telemetry)
    {
        var vm = new TelemetriaRespostaVm
        {
            dataReferencia = DateTime.UtcNow.Date,
            listaEndpoints = telemetry.Snapshot().OrderBy(x => x.nomeApi).ToList()
        };
        return Ok(vm);
    }

}
