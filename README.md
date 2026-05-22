# Processador Confiável de Webhooks

API .NET inspirada em cenários reais de produção para processamento confiável de webhooks, com PostgreSQL, idempotência, workers em background, retries e Dead Letter.

## Funcionalidades

### 💾 Armazenamento e Controle
- **`WebhookEvent`**: Recebe o payload bruto, armazena com status inicial (`Pending`).
- **`WebhookEventTrigger`**: Ativa o processamento do webhook.

###  idempotência
- **`EnsureIdempotency`**: Impede processamentos duplicados baseados no header `X-Signature-SHA256`.
- **`ValidateIdempotency`**: Garante que o webhook não foi processado anteriormente.

### 🧠 Workers em Background (Mediation Pattern)
- **`WebhookProcessingWorker`**: Processa o webhook de forma assíncrona.
- **`WebhookFailedRetryWorker`**: Tenta novamente webhooks falhados automaticamente.
- **`WebhookFailedMoveWorker`**: Move webhooks falhados após retries para Dead Letter.
- **`WebhookRetryTimerWorker`**: Executa tarefas recorrentes (ex: limpeza de logs).

### 🔄 Retries e Dead Letter
- **Limite de Retries**: Configurado em `appsettings.json`.
- **Delay Exponential**: Aumenta o tempo entre tentativas (1s, 2s, 4s, ...).
- **Dead Letter**: Webhooks que falham após todos os retries são movidos para "parar de incomodar".

### 🧪 Testes e Qualidade
- **Testes Unitários**: Focados em lógica e fluxo de dados.
- **Testes de Integração**: Verificação de API e workers.
- **Cobertura de Código**: Busca atingir alta cobertura.

## 🛠️ Pré-requisitos

- .NET 9
- Docker e Docker Compose (para o banco de dados)
- SQL Server LocalDB (opcional)

## 🚀 Instalação e Execução

### 1.Banco de Dados

**Opção A: Docker (Recomendado)**
```bash
docker compose up -d
```

**Opção B: LocalDB**
Certifique-se de ter o SQL Server LocalDB instalado e o serviço rodando.

### 2. Migrations
Aplique as migrações ao banco de dados:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Como Executar a Aplicação

Você pode rodar a API de diferentes formas, dependendo do seu fluxo de desenvolvimento:

**Opção A: .NET Watch com Hot Reload (Recomendado)**
Inicia o servidor, **abre o navegador automaticamente** no Swagger e reinicia a API sozinha toda vez que você salva um arquivo `.cs` alterado.
```bash
dotnet watch --project src/ReliableWebhookProcessor.Api
```

**Opção B: Run Clássico (Terminal)**
Apenas liga o servidor silenciosamente no terminal. Você precisará clicar no link gerado e adicionar `/swagger` no navegador manualmente.
```bash
dotnet run --project src/ReliableWebhookProcessor.Api
```

**Opção C: Pelo VS Code (F5 / Debugger)**
Se o seu VS Code estiver configurado (arquivos `launch.json` e `tasks.json`), basta apertar `F5`. Ele fará o build, conectará o debugger para inspecionar variáveis e abrirá o Swagger sozinho.

## 📦 Endpoints Principais

### POST /api/webhook
Recebe webhooks.

**Header Exemplo:**
```http
X-Signature-SHA256: <sha256_hash>
```

### POST /api/webhook/trigger/{id}
Ativa manualmente o processamento de um webhook.

### GET /api/webhook/{id}
Busca detalhes de um webhook.

## 🏗️ Arquitetura

- **Controllers**: Interface da API (HTTP).
- **Services**: Lógica de negócio (Processamento, Idempotência).
- **Workers**: Processamento assíncrono em background.
- **Data**: Repositórios e Entidades.
- **Migrations**: Controle de versão do schema do banco de dados.
