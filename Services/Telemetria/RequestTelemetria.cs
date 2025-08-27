using SimulacaoCredito.Api.Models;
using System.Collections.Concurrent;

namespace SimulacaoCredito.Api.Services;

public class RequestTelemetria
{
    private class Acc
    {
        public long Count;
        public long Success;
        public double TotalMs;
        public double MinMs = double.MaxValue;
        public double MaxMs = 0;
    }

    private readonly ConcurrentDictionary<string, Acc> _map = new();

    public void Record(string endpoint, double elapsedMs, int statusCode)
    {
        var acc = _map.GetOrAdd(endpoint, _ => new Acc());
        lock (acc)
        {
            acc.Count++;
            if (statusCode >= 200 && statusCode < 300) acc.Success++;
            acc.TotalMs += elapsedMs;
            if (elapsedMs < acc.MinMs) acc.MinMs = elapsedMs;
            if (elapsedMs > acc.MaxMs) acc.MaxMs = elapsedMs;
        }
    }

    public IEnumerable<TelemetriaEndpointVm> Snapshot()
    {
        foreach (var (key, acc) in _map)
        {
            lock (acc)
            {
                var avg = acc.Count > 0 ? acc.TotalMs / acc.Count : 0;
                var pct = acc.Count > 0 ? (double)acc.Success / acc.Count : 0;
                yield return new TelemetriaEndpointVm
                {
                    nomeApi = key,
                    qtdRequisicoes = acc.Count,
                    tempoMedio = Math.Round(avg, 2),
                    tempoMinimo = Math.Round(acc.MinMs == double.MaxValue ? 0 : acc.MinMs, 2),
                    tempoMaximo = Math.Round(acc.MaxMs, 2),
                    percentualSucesso = Math.Round(pct, 4)
                };
            }
        }
    }
}
