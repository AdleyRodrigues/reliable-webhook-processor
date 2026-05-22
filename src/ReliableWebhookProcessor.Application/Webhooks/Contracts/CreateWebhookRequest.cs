using System.Text.Json;

namespace ReliableWebhookProcessor.Application.Webhooks.Contracts;

/// <summary>
/// DTO (Data Transfer Object) de entrada. 
/// Usado exclusivamente para receber e validar os dados brutos da requisição HTTP.
/// </summary>
public sealed class CreateWebhookRequest
{
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    
    /// <summary>
    /// Recebe o corpo do webhook como JsonElement para não precisarmos saber a estrutura exata agora.
    /// Isso nos dá flexibilidade para aceitar qualquer JSON válido.
    /// </summary>
    public JsonElement Payload { get; init; }
}
