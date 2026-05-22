using System;

namespace ReliableWebhookProcessor.Domain.Webhooks;

/// <summary>
/// Representa o núcleo do nosso domínio: um evento de webhook externo recebido pela API.
/// </summary>
public class WebhookEvent
{
    // Usamos 'private set' em todas as propriedades para proteger o estado interno da entidade.
    // Qualquer mudança de estado deve ocorrer através de métodos com intenção de negócio clara.

    public Guid Id { get; private set; }
    
    /// <summary>
    /// O ID original enviado pelo sistema externo. 
    /// É crítico para garantir a idempotência (não processar o mesmo evento duas vezes).
    /// </summary>
    public string EventId { get; private set; }
    
    public string EventType { get; private set; }
    
    /// <summary>
    /// Guardamos o JSON bruto (raw payload) para termos a verdade absoluta do que recebemos,
    /// facilitando o reprocessamento ou auditoria no futuro.
    /// </summary>
    public string Payload { get; private set; }
    
    public WebhookEventStatus Status { get; private set; }
    
    /// <summary>
    /// Conta quantas vezes tentamos processar este evento. Crucial para as políticas de Retry.
    /// </summary>
    public int Attempts { get; private set; }
    
    /// <summary>
    /// Define exatamente quando (no tempo) o worker deve tentar processar este evento de novo,
    /// permitindo a criação de um "Exponential Backoff" (esperar 2s, depois 4s, depois 8s...).
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    /// <summary>
    /// Preenchido apenas quando o evento alcança sucesso (Completed).
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; private set; }
    
    public string? ErrorMessage { get; private set; }

    // Construtor sem parâmetros exigido por alguns ORMs (como EF Core) caso venha a ser usado.
    // Mantemos como 'protected' para impedir que outras partes do código criem a entidade de forma inválida.
    protected WebhookEvent() { }

    /// <summary>
    /// Construtor principal para criar um novo evento válido na aplicação.
    /// </summary>
    public WebhookEvent(string eventId, string eventType, string payload)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("O EventId externo é obrigatório para idempotência.", nameof(eventId));
            
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("O Payload não pode ser vazio.", nameof(payload));

        Id = Guid.NewGuid();
        EventId = eventId;
        EventType = eventType;
        Payload = payload;
        
        // Regras de negócio iniciais:
        Status = WebhookEventStatus.Pending;
        Attempts = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    #region Métodos de Transição de Estado (Máquina de Estados)

    /// <summary>
    /// O Worker pega o evento e sinaliza que começou a trabalhar nele.
    /// </summary>
    public void MarkAsProcessing()
    {
        Status = WebhookEventStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// O processamento finalizou com sucesso.
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = WebhookEventStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>
    /// Ocorreu um erro no processamento. Registramos a falha e agendamos a próxima tentativa.
    /// </summary>
    public void MarkAsFailed(string errorMessage, DateTimeOffset nextRetryAt)
    {
        Status = WebhookEventStatus.Failed;
        ErrorMessage = errorMessage;
        NextRetryAt = nextRetryAt;
        Attempts++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// O evento esgotou todas as tentativas de retry e foi abandonado.
    /// </summary>
    public void MarkAsDeadLetter(string errorMessage)
    {
        Status = WebhookEventStatus.DeadLetter;
        ErrorMessage = errorMessage;
        NextRetryAt = null; // Não vamos tentar de novo automaticamente
        Attempts++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Usado para colocar manualmente um evento que estava falho/morto de volta na fila.
    /// </summary>
    public void ResetToPending()
    {
        Status = WebhookEventStatus.Pending;
        ErrorMessage = null;
        NextRetryAt = null;
        // Não zeramos 'Attempts' para manter o histórico de quantas vezes já falhou no total.
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    #endregion
}
