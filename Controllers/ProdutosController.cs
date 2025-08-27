using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SimulacaoCredito.Api.Models;

namespace SimulacaoCredito.Api.Controllers;

[ApiController]
[Route("produtos")]
public class ProdutosController : ControllerBase
{
    private readonly string _connStr;
    public ProdutosController(string connStr) => _connStr = connStr;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produto>>> Get()
    {
        using var conn = new SqlConnection(_connStr);
        var itens = await conn.QueryAsync<Produto>("SELECT * FROM dbo.PRODUTO WITH (NOLOCK)");
        return Ok(itens);
    }
}
