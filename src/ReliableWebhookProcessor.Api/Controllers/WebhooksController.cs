using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReliableWebhookProcessor.Application.Webhooks.Contracts;
using ReliableWebhookProcessor.Application.Webhooks.Services;
using ReliableWebhookProcessor.Domain.Webhooks;

namespace ReliableWebhookProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookInMemoryStore _store;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookInMemoryStore store, ILogger<WebhooksController> logger)
    {
        _store = store;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] CreateWebhookRequest request, 
        CancellationToken cancellationToken)
    {
        // 1. Validação básica (Fail Fast)
        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            _logger.LogWarning("Recebido webhook sem EventId.");
            return BadRequest("O EventId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            _logger.LogWarning("Recebido webhook sem EventType para o EventId: {EventId}", request.EventId);
            return BadRequest("O EventType é obrigatório.");
        }

        // 2. Idempotência Inicial
        // Verificamos se já existe um evento com este EventId no nosso banco em memória.
        var existingEvent = await _store.GetByEventIdAsync(request.EventId, cancellationToken);
        if (existingEvent != null)
        {
            _logger.LogInformation(
                "Idempotência: Webhook duplicado ignorado. EventId: {EventId}", request.EventId);
            
            // Retornamos 202 Accepted para o cliente externo achar que recebemos com sucesso,
            // evitando que ele fique em loop tentando reenviar algo que já temos.
            return Accepted(MapToResponse(existingEvent));
        }

        // 3. Serialização do Payload
        // Pegamos o JsonElement flexível e transformamos na string bruta que a nossa Entidade pede.
        var rawPayload = JsonSerializer.Serialize(request.Payload);

        // 4. Criação da Entidade de Domínio
        var newEvent = new WebhookEvent(request.EventId, request.EventType, rawPayload);

        // 5. Salvar na Memória
        await _store.AddAsync(newEvent, cancellationToken);
        _logger.LogInformation("Webhook recebido e salvo com sucesso. EventId: {EventId}", request.EventId);

        // 6. Resposta
        // Retornamos 202 Accepted pois salvamos, mas ainda NÃO processamos a regra de negócio do evento (Background).
        return Accepted(MapToResponse(newEvent));
    }

    // Método auxiliar privado para mapear a Entidade para DTO e não expor o Domínio diretamente na API.
    private static WebhookResponse MapToResponse(WebhookEvent webhookEvent)
    {
        return new WebhookResponse
        {
            Id = webhookEvent.Id,
            EventId = webhookEvent.EventId,
            EventType = webhookEvent.EventType,
            Status = webhookEvent.Status.ToString(),
            Attempts = webhookEvent.Attempts,
            CreatedAt = webhookEvent.CreatedAt,
            ProcessedAt = webhookEvent.ProcessedAt,
            ErrorMessage = webhookEvent.ErrorMessage
        };
    }
}
