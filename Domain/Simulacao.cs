namespace SimulacaoCredito.Api.Domain;

public class SimulacaoEntity
{
    public Guid IdSimulacao { get; set; }
    public DateTime DtCriacao { get; set; }
    public decimal ValorDesejado { get; set; }
    public int Prazo { get; set; }
    public int CodigoProduto { get; set; }
    public string DescricaoProduto { get; set; } = "";
    public decimal TaxaJuros { get; set; }

    public List<SimulacaoParcelaEntity> Parcelas { get; set; } = new();
}

public class SimulacaoParcelaEntity
{
    public long Id { get; set; }                          // PK (IDENTITY em SQLite = AUTOINCREMENT)
    public Guid IdSimulacao { get; set; }                 // FK
    public string Tipo { get; set; } = "";                // "SAC" | "PRICE"
    public int Numero { get; set; }
    public decimal ValorAmortizacao { get; set; }
    public decimal ValorJuros { get; set; }
    public decimal ValorPrestacao { get; set; }

    public SimulacaoEntity? Simulacao { get; set; }       // nav
}
