namespace SimulacaoCredito.Api.Models;

public class SimulacaoEntrada { public decimal valorDesejado { get; set; } public int prazo { get; set; } }

public class ParcelaDTO { 
    public int numero { get; set; } 
    public decimal valorAmortizacao { get; set; } 
    public decimal valorJuros { get; set; } 
    public decimal valorPrestacao { get; set; } 
}

public class ResultadoDTO { 
        public string tipo { get; set; } = "";
        public List<ParcelaDTO> parcelas { get; set; } = new(); 
 }

public class SimulacaoResposta
{
    public string idSimulacao { get; set; } = "";
    public int codigoProduto { get; set; }
    public string descricaoProduto { get; set; } = "";
    public decimal taxaJuros { get; set; }
    public List<ResultadoDTO> resultadoSimulacao { get; set; } = new();
}
