using Microsoft.EntityFrameworkCore;
using SimulacaoCredito.Api.Domain;
using SimulacaoCredito.Api.Models;

namespace SimulacaoCredito.Api.Infrastructure.Persistence;

public interface ISimulacaoRepository
{
    Task SalvarAsync(SimulacaoEntity sim, IEnumerable<SimulacaoParcelaEntity> parcelas);

    Task<(IReadOnlyList<SimulacaoResumoVm> itens, int total)> ListarAsync(int offset, int limit);

    Task<IReadOnlyList<VolumePorDiaVm>> VolumePorDiaAsync(DateOnly? data);

    Task<SimulacaoEntity?> ObterPorIdAsync(Guid id);

    Task<int> TotalAsync();
    Task<IReadOnlyList<ContagemDiaVm>> Ultimos7DiasAsync();


}

public class EfSimulacaoRepository : ISimulacaoRepository
{
    private readonly AppDbContext _db;
    public EfSimulacaoRepository(AppDbContext db) => _db = db;

    public async Task SalvarAsync(SimulacaoEntity sim, IEnumerable<SimulacaoParcelaEntity> parcelas)
    {
        // anexa as parcelas à simulação
        sim.Parcelas = parcelas.ToList();

        _db.Simulacoes.Add(sim);
        await _db.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<SimulacaoResumoVm> itens, int total)>
    ListarAsync(int offset, int limit)
    {
        // total
        var total = await _db.Simulacoes.AsNoTracking().CountAsync();

        // página (ordem mais nova primeiro)
        var query = _db.Simulacoes.AsNoTracking()
            .OrderByDescending(s => s.DtCriacao)
            .Skip(offset)
            .Take(limit)
            .Select(s => new SimulacaoResumoVm
            {
                idSimulacao = s.IdSimulacao,
                data = s.DtCriacao,
                valorDesejado = s.ValorDesejado,
                prazo = s.Prazo,
                produto = s.DescricaoProduto
            });

        var itens = await query.ToListAsync();
        return (itens, total);
    }

    public async Task<IReadOnlyList<VolumePorDiaVm>> VolumePorDiaAsync(DateOnly? data)
    {
        var q = _db.Simulacoes.AsNoTracking();

        if (data.HasValue)
        {
            // filtra pelo dia informado (UTC)
            var inicio = data.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var fim = inicio.AddDays(1);
            q = q.Where(s => s.DtCriacao >= inicio && s.DtCriacao < fim);
        }

        var res = await q
            .GroupBy(s => new { Dia = s.DtCriacao.Date, s.CodigoProduto, s.DescricaoProduto })
            .Select(g => new VolumePorDiaVm
            {
                dia = g.Key.Dia,
                codigoProduto = g.Key.CodigoProduto,
                produto = g.Key.DescricaoProduto,
                totalSimulacoes = g.Count(),
                somaValores = g.Sum(x => x.ValorDesejado),
                mediaValor = Math.Round(g.Average(x => x.ValorDesejado), 2, MidpointRounding.AwayFromZero),
                prazoMedio = (int)Math.Round(g.Average(x => x.Prazo))
            })
            .OrderByDescending(x => x.dia)
            .ThenBy(x => x.codigoProduto)
            .ToListAsync();

        return res;
    }

    public async Task<SimulacaoEntity?> ObterPorIdAsync(Guid id)
    {
        return await _db.Simulacoes
            .AsNoTracking()
            .Include(s => s.Parcelas)
            .FirstOrDefaultAsync(s => s.IdSimulacao == id);
    }

    // total de simulações
    public async Task<int> TotalAsync()
        => await _db.Simulacoes.AsNoTracking().CountAsync();

    // contagem por dia (últimos 7 dias)
    public async Task<IReadOnlyList<ContagemDiaVm>> Ultimos7DiasAsync()
    {
        var hojeUtc = DateTime.UtcNow.Date;
        var ini = hojeUtc.AddDays(-6); // inclui hoje

        var lista = await _db.Simulacoes.AsNoTracking()
            .Where(s => s.DtCriacao.Date >= ini && s.DtCriacao.Date <= hojeUtc)
            .GroupBy(s => s.DtCriacao.Date)
            .Select(g => new ContagemDiaVm { dia = g.Key, total = g.Count() })
            .OrderBy(x => x.dia)
            .ToListAsync();

        // garante dias sem movimento com zero
        var map = lista.ToDictionary(x => x.dia.Date, x => x.total);
        var filled = new List<ContagemDiaVm>();
        for (var d = ini; d <= hojeUtc; d = d.AddDays(1))
            filled.Add(new ContagemDiaVm { dia = d, total = map.TryGetValue(d, out var v) ? v : 0 });

        return filled;
    }
}




