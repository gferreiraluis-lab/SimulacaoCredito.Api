namespace SimulacaoCredito.Api.Models;

public class SimulacaoResumoVm
{
    public Guid idSimulacao { get; set; }
    public DateTime data { get; set; }
    public decimal valorDesejado { get; set; }
    public int prazo { get; set; }
    public string produto { get; set; } = "";
}

public class PageVm<T>
{
    public int total { get; set; }
    public int offset { get; set; }
    public int limit { get; set; }
    public List<T> items { get; set; } = new();
}
