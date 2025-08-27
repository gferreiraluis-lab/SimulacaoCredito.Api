using System.Threading.Tasks;

namespace SimulacaoCredito.Api.Services;

public interface IEventHubProducer
{
    Task SendAsync(string json);
}
