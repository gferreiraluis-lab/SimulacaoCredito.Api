using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SimulacaoCredito.Api.Models;
using SimulacaoCredito.Api.Services;
using SimulacaoCredito.Api.Domain;
using SimulacaoCredito.Api.Infrastructure.Persistence;
using System.Text.Json;
using SimulacaoCredito.Api.Services;

namespace SimulacaoCredito.Api.Controllers;

[ApiController]
[Route("credito/v1/simulacoes")]
public class SimulacoesController : ControllerBase
{
    private readonly string _connProdutos;                 // banco do desafio (somente leitura de PRODUTO)
    private readonly ISimulacaoRepository _repo;         // seu banco local para salvar a simulação
    private readonly IEventHubProducer _eh;

    public SimulacoesController(IConfiguration cfg, ISimulacaoRepository repo, IEventHubProducer eh)
    {
        _connProdutos = cfg.GetConnectionString("SqlProdutos")!;
        _repo = repo;
        _eh = eh;
    }


  

    // ÚNICO POST (calcula + persiste)
    [HttpPost]
    public async Task<ActionResult<SimulacaoResposta>> Post([FromBody] SimulacaoEntrada body)
    {
        // 1) validação
        if (body is null || body.valorDesejado <= 0 || body.prazo <= 0)
            return BadRequest(new { mensagem = "Informe valorDesejado > 0 e prazo > 0." });

        // 2) selecionar produto no banco do desafio
        const string sql = @"
SELECT TOP 1 * FROM dbo.PRODUTO WITH (NOLOCK)
WHERE @valor BETWEEN VR_MINIMO AND ISNULL(VR_MAXIMO, 999999999999)
  AND @prazo BETWEEN NU_MINIMO_MESES AND ISNULL(NU_MAXIMO_MESES, 32767)
ORDER BY CO_PRODUTO;";

        Produto? produto;
        using (var conn = new SqlConnection(_connProdutos))
        {
            produto = await conn.QueryFirstOrDefaultAsync<Produto>(
                sql, new { valor = body.valorDesejado, prazo = body.prazo });
        }

        if (produto is null)
            return BadRequest(new { mensagem = "Nenhum produto atende aos limites de valor/prazo." });

        // 3) cálculos
        var i = produto.PC_TAXA_JUROS;
        var sac = FinanceCalculators.CalcularSAC(body.valorDesejado, body.prazo, i);
        var price = FinanceCalculators.CalcularPRICE(body.valorDesejado, body.prazo, i);

        // 4) montar resposta
        var resp = new SimulacaoResposta
        {
            idSimulacao = Guid.NewGuid().ToString("N"),
            codigoProduto = produto.CO_PRODUTO,
            descricaoProduto = produto.NO_PRODUTO,
            taxaJuros = produto.PC_TAXA_JUROS,
            resultadoSimulacao = new()
            {
                new ResultadoDTO {
                    tipo = "SAC",
                    parcelas = sac.Select((x, idx) => new ParcelaDTO {
                        numero = idx + 1,
                        valorAmortizacao = x.amort,
                        valorJuros       = x.juros,
                        valorPrestacao   = x.prest
                    }).ToList()
                },
                new ResultadoDTO {
                    tipo = "PRICE",
                    parcelas = price.Select((x, idx) => new ParcelaDTO {
                        numero = idx + 1,
                        valorAmortizacao = x.amort,
                        valorJuros       = x.juros,
                        valorPrestacao   = x.prest
                    }).ToList()
                }
            }
        };

        // 5) persistir no seu banco local
        var id = Guid.Parse(resp.idSimulacao);
        var simEntity = new SimulacaoEntity
        {
            IdSimulacao = id,
            DtCriacao = DateTime.UtcNow,
            ValorDesejado = body.valorDesejado,
            Prazo = body.prazo,
            CodigoProduto = resp.codigoProduto,
            DescricaoProduto = resp.descricaoProduto,
            TaxaJuros = resp.taxaJuros
        };

        var parcelasEntities = resp.resultadoSimulacao
            .SelectMany(r => r.parcelas.Select(p => new SimulacaoParcelaEntity
            {
                IdSimulacao = id,
                Tipo = r.tipo,
                Numero = p.numero,
                ValorAmortizacao = p.valorAmortizacao,
                ValorJuros = p.valorJuros,
                ValorPrestacao = p.valorPrestacao
            }));

        await _repo.SalvarAsync(simEntity, parcelasEntities);

       

        // ao final do método POST, depois do _repo.SalvarAsync(...)
        var json = JsonSerializer.Serialize(resp, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // casa com o modelo do PDF
            WriteIndented = false
        });
        await _eh.SendAsync(json);

        // 6) retornar 201 com o envelope
        return Created(string.Empty, resp);
    }

    [HttpGet]
    public async Task<ActionResult<PageVm<SimulacaoResumoVm>>> Get([FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var (itens, total) = await _repo.ListarAsync(offset, limit);

        var page = new PageVm<SimulacaoResumoVm>
        {
            total = total,
            offset = offset,
            limit = limit,
            items = itens.ToList()
        };

        // 206 quando a página não cobre tudo; 200 quando couber
        var status = (offset + itens.Count < total) ? StatusCodes.Status206PartialContent
                                                    : StatusCodes.Status200OK;
        return StatusCode(status, page);
    }

    // GET /credito/v1/simulacoes/volume?data=2025-08-21 (opcional)
    [HttpGet("volume")]
    public async Task<ActionResult<List<VolumePorDiaVm>>> GetVolume([FromQuery] DateOnly? data)
    {
        var lista = await _repo.VolumePorDiaAsync(data);
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SimulacaoResposta>> GetById(Guid id)
    {
        var s = await _repo.ObterPorIdAsync(id);
        if (s is null) return NotFound(new { mensagem = "Simulação não encontrada." });

        // monta no mesmo formato do POST
        var resp = new SimulacaoResposta
        {
            idSimulacao = s.IdSimulacao.ToString("N"),
            codigoProduto = s.CodigoProduto,
            descricaoProduto = s.DescricaoProduto,
            taxaJuros = s.TaxaJuros,
            resultadoSimulacao = new()
        };

        // separa parcelas por tipo, ordenadas
        var grupos = s.Parcelas
            .OrderBy(p => p.Tipo)
            .ThenBy(p => p.Numero)
            .GroupBy(p => p.Tipo);

        foreach (var g in grupos)
        {
            resp.resultadoSimulacao.Add(new ResultadoDTO
            {
                tipo = g.Key,
                parcelas = g.Select(p => new ParcelaDTO
                {
                    numero = p.Numero,
                    valorAmortizacao = p.ValorAmortizacao,
                    valorJuros = p.ValorJuros,
                    valorPrestacao = p.ValorPrestacao
                }).ToList()
            });
        }

        return Ok(resp);
    }

    


}



