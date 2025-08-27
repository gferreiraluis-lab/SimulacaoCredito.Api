namespace SimulacaoCredito.Api.Models;

public class TelemetriaEndpointVm
{
    public string nomeApi { get; set; } = "";     // ex: "/credito/v1/simulacoes POST"
    public long qtdRequisicoes { get; set; }
    public double tempoMedio { get; set; }        // ms
    public double tempoMinimo { get; set; }       // ms
    public double tempoMaximo { get; set; }       // ms
    public double percentualSucesso { get; set; } // 0..1
}

public class TelemetriaRespostaVm
{
    public DateTime dataReferencia { get; set; } = DateTime.UtcNow.Date;
    public List<TelemetriaEndpointVm> listaEndpoints { get; set; } = new();
}


public class ContagemDiaVm
{
    public DateTime dia { get; set; }
    public int total { get; set; }
}
