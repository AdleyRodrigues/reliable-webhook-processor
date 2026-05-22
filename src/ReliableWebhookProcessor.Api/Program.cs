/// <summary>
/// Arquivo principal de inicialização da API (Entry Point).
/// Na Clean Architecture, a camada Api (Presentation) é responsável apenas por 
/// expor as rotas HTTP e configurar a Injeção de Dependências.
/// </summary>

var builder = WebApplication.CreateBuilder(args);

// 1. Adiciona os serviços ao contêiner de Injeção de Dependência (DI Container).
// É aqui que futuramente vamos registrar nossos Repositórios (Infrastructure) e Casos de Uso (Application).
builder.Services.AddEndpointsApiExplorer(); // Permite que o Swagger descubra os endpoints
builder.Services.AddSwaggerGen();           // Gera a documentação visual da API (Swagger)

// 2. Constrói a aplicação com as configurações acima.
var app = builder.Build();

// 3. Configura o Pipeline de Requisições HTTP (Middlewares).
// A ordem aqui importa. Uma requisição passa por esses middlewares antes de chegar no seu Endpoint.
if (app.Environment.IsDevelopment())
{
    // Habilita o Swagger apenas em ambiente de desenvolvimento (localhost)
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Força o redirecionamento de chamadas HTTP para HTTPS (mais seguro)
app.UseHttpsRedirection();

// Nota: O código de exemplo "WeatherForecast" foi removido para mantermos o projeto 
// 100% focado no nosso domínio de Webhooks, que criaremos na Etapa 2.

// 4. Roda a aplicação, ficando "escutando" por requisições HTTP.
app.Run();
