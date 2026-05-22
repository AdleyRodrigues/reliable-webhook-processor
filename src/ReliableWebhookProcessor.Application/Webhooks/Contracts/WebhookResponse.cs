using System;

namespace ReliableWebhookProcessor.Application.Webhooks.Contracts;

/// <summary>
/// DTO (Data Transfer Object) de saída.
/// Usado para formatar a resposta que o cliente vai receber.
/// </summary>
public sealed class WebhookResponse
{
    public Guid Id { get; init; }
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Attempts { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
