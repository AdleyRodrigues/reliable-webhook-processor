/// <summary>
/// Arquivo principal de inicialização da API (Entry Point).
/// Na Clean Architecture, a camada Api (Presentation) é responsável apenas por 
/// expor as rotas HTTP e configurar a Injeção de Dependências.
/// </summary>

using ReliableWebhookProcessor.Application.Webhooks.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Adiciona os serviços ao contêiner de Injeção de Dependência (DI Container).
builder.Services.AddControllers(); // Habilita o uso de Controllers na API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registra nosso banco em memória.
// Singleton significa que existirá apenas UMA instância dessa classe enquanto a API estiver rodando.
// Todos os requests HTTP vão compartilhar a mesma memória (o mesmo ConcurrentDictionary).
builder.Services.AddSingleton<IWebhookInMemoryStore, WebhookInMemoryStore>();

// 2. Constrói a aplicação
var app = builder.Build();

// 3. Configura o Pipeline de Requisições HTTP (Middlewares).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Mapeia as rotas dos controllers (ex: /api/webhooks)
app.MapControllers();

// 4. Roda a aplicação
app.Run();
