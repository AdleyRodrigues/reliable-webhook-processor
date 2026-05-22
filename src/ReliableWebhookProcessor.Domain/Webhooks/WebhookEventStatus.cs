namespace ReliableWebhookProcessor.Domain.Webhooks;

/// <summary>
/// Representa o ciclo de vida e o estado atual de um evento de webhook no sistema.
/// </summary>
public enum WebhookEventStatus
{
    /// <summary>
    /// O evento foi recebido e salvo com sucesso, mas ainda não começou a ser processado.
    /// É o estado inicial padrão de qualquer webhook.
    /// </summary>
    Pending,

    /// <summary>
    /// Um worker (processo em background) pegou o evento e está tentando processá-lo neste exato momento.
    /// Impede que dois workers processem o mesmo evento simultaneamente.
    /// </summary>
    Processing,

    /// <summary>
    /// O webhook foi processado com sucesso. O ciclo de vida terminou feliz.
    /// </summary>
    Completed,

    /// <summary>
    /// O processamento falhou (ex: API de destino fora do ar). 
    /// O evento aguardará para ser tentado novamente com base no NextRetryAt.
    /// </summary>
    Failed,

    /// <summary>
    /// O webhook falhou em todas as tentativas de retry (limite máximo atingido).
    /// Ele é movido para este status final para parar de consumir recursos e aguardar intervenção manual.
    /// </summary>
    DeadLetter
}
