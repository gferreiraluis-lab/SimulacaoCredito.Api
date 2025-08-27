using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;

namespace SimulacaoCredito.Api.Services;

public class EventHubProducer : IEventHubProducer, IAsyncDisposable
{
    private readonly EventHubProducerClient _client;

    public EventHubProducer(IConfiguration cfg)
    {
        var conn = cfg["EventHub:ConnectionString"]
                   ?? throw new InvalidOperationException("EventHub:ConnectionString not set.");
        _client = new EventHubProducerClient(conn);
    }

    public async Task SendAsync(string json)
    {
        using var batch = await _client.CreateBatchAsync();
        if (!batch.TryAdd(new EventData(new BinaryData(json))))
            throw new InvalidOperationException("Mensagem excede o tamanho de um batch.");
        await _client.SendAsync(batch);
    }

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}
