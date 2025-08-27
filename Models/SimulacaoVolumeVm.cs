namespace SimulacaoCredito.Api.Models;

public class VolumePorDiaVm
{
    public DateTime dia { get; set; }
    public int codigoProduto { get; set; }
    public string produto { get; set; } = "";
    public int totalSimulacoes { get; set; }
    public decimal somaValores { get; set; }
    public decimal mediaValor { get; set; }
    public int prazoMedio { get; set; }
}
