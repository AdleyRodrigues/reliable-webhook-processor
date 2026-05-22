using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReliableWebhookProcessor.Domain.Webhooks;

namespace ReliableWebhookProcessor.Application.Webhooks.Services;

/// <summary>
/// Contrato (Interface) para gerenciar o armazenamento de webhooks.
/// </summary>
public interface IWebhookInMemoryStore
{
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
    Task<WebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WebhookEvent>> GetAllAsync(CancellationToken cancellationToken);
}
