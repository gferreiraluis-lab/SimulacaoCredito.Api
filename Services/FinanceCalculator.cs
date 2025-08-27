namespace SimulacaoCredito.Api.Services;

public static class FinanceCalculators
{
    static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public static List<(decimal amort, decimal juros, decimal prest)>
        CalcularSAC(decimal principal, int n, decimal i)
    {
        var list = new List<(decimal, decimal, decimal)>(n);
        var amort = principal / n;
        for (int k = 1; k <= n; k++)
        {
            var saldoAntes = principal - amort * (k - 1);
            var juros = saldoAntes * i;
            list.Add((R2(amort), R2(juros), R2(amort + juros)));
        }
        return list;
    }

    public static List<(decimal amort, decimal juros, decimal prest)>
        CalcularPRICE(decimal principal, int n, decimal i)
    {
        var list = new List<(decimal, decimal, decimal)>(n);
        double P = (double)principal, im = (double)i;
        double pmt = im == 0 ? P / n : P * im / (1 - Math.Pow(1 + im, -n));

        decimal saldo = principal;
        for (int k = 1; k <= n; k++)
        {
            var juros = saldo * i;
            var amort = (decimal)pmt - juros;
            saldo -= amort;
            list.Add((R2(amort), R2(juros), R2((decimal)pmt)));
        }
        return list;
    }
}
