using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReliableWebhookProcessor.Domain.Webhooks;

namespace ReliableWebhookProcessor.Application.Webhooks.Services;

/// <summary>
/// Implementação temporária para armazenar webhooks na memória RAM da API.
/// Isso é útil apenas para entendermos o fluxo HTTP antes de configurar um banco de dados real.
/// </summary>
public sealed class WebhookInMemoryStore : IWebhookInMemoryStore
{
    // ConcurrentDictionary é thread-safe, ou seja, permite que múltiplas requisições HTTP
    // leiam e escrevam ao mesmo tempo sem corromper a memória (algo que o List comum não garante).
    private readonly ConcurrentDictionary<string, WebhookEvent> _store = new();

    public Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        // Tenta adicionar no dicionário usando o EventId como chave.
        _store.TryAdd(webhookEvent.EventId, webhookEvent);
        return Task.CompletedTask;
    }

    public Task<WebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken)
    {
        // Busca pela chave (EventId). Retorna null se não encontrar.
        _store.TryGetValue(eventId, out var webhookEvent);
        return Task.FromResult(webhookEvent);
    }

    public Task<IReadOnlyList<WebhookEvent>> GetAllAsync(CancellationToken cancellationToken)
    {
        // Pega todos os valores do dicionário
        var all = _store.Values.ToList();
        return Task.FromResult<IReadOnlyList<WebhookEvent>>(all);
    }
}
