# Arquitetura e Estrutura do Projeto

Este documento explica o "porquê" de cada parte da estrutura deste projeto, que é baseada nos princípios de **Clean Architecture** (Arquitetura Limpa) e **DDD (Domain-Driven Design)**. O objetivo principal é a **separação de responsabilidades**, garantindo que as regras de negócio não se misturem com infraestrutura (como banco de dados) ou portas de entrada (como requisições HTTP).

## 1. Visão Geral em Diagramas

### Regra de Dependência (Camadas)
Todas as dependências apontam para o centro (`Domain`). As camadas externas (`Api` e `Infrastructure`) não se conhecem, elas apenas dependem do núcleo da aplicação.

```mermaid
flowchart TD
    API["🚪 API (Apresentação / Controllers)"]
    INFRA["🔌 Infrastructure (Banco de Dados)"]
    APP["⚙️ Application (Casos de Uso)"]
    DOMAIN["🫀 Domain (Entidades / Regras de Negócio)"]

    API -- "Injeta e Chama" --> APP
    INFRA -. "Implementa contratos de" .-> APP
    INFRA -. "Implementa contratos de" .-> DOMAIN
    APP -- "Orquestra" --> DOMAIN

    style DOMAIN fill:#d4edda,stroke:#28a745,stroke-width:2px,color:black
    style APP fill:#fff3cd,stroke:#ffc107,stroke-width:2px,color:black
    style API fill:#cce5ff,stroke:#007bff,stroke-width:2px,color:black
    style INFRA fill:#f8d7da,stroke:#dc3545,stroke-width:2px,color:black
```

### Fluxo de Recebimento de Webhooks (POST /api/webhooks)
Este é o fluxo detalhado exato do endpoint que recebe os eventos. Ele demonstra o padrão "Fail Fast", verificação de Idempotência e salvamento antes do processamento.

```mermaid
sequenceDiagram
    autonumber
    participant SistemaExterno as 🌐 MiniPay (Sistema Externo)
    participant Controller as 🚪 WebhooksController
    participant Domain as 🫀 WebhookEvent (Entidade)
    participant Store as 💾 IWebhookInMemoryStore

    SistemaExterno->>Controller: POST /api/webhooks (CreateWebhookRequest)
    
    note over Controller: 1. Fail Fast Validations
    alt Falta EventId ou EventType
        Controller-->>SistemaExterno: 400 Bad Request
    end

    note over Controller: 2. Checagem de Idempotência
    Controller->>Store: GetByEventIdAsync(request.EventId)
    Store-->>Controller: Retorna Webhook (Se existir)
    
    alt EventId já existe no sistema
        note over Controller: Ignora duplicata silenciosamente
        Controller-->>SistemaExterno: 202 Accepted (WebhookResponse)
    else EventId é Novo
        note over Controller: 3. Serialização e Domínio
        Controller->>Domain: new WebhookEvent(eventId, type, payload)
        Domain-->>Controller: Instância (Status = Pending)
        
        note over Controller: 4. Persistência
        Controller->>Store: AddAsync(newEvent)
        Store-->>Controller: Sucesso
        
        note over Controller: 5. Resposta Rápida
        Controller-->>SistemaExterno: 202 Accepted (WebhookResponse)
    end
```

---

## 2. A Raiz do Projeto
- **`ReliableWebhookProcessor.sln` (Solution):** É o arquivo que agrupa todos os projetos. Funciona como um "fichário" que diz à IDE quais projetos compõem o ecossistema.
- **Pasta `src/` (Source):** Onde fica todo o código de produção da aplicação.
- **Pasta `tests/`:** Onde ficam os projetos de teste. O código destas pastas **não** vai para produção.

---

## 3. A Pasta `src/` (As Camadas da Aplicação)

### 🫀 `ReliableWebhookProcessor.Domain`
- **O que é:** O coração e a razão de existir do software.
- **Para que serve:** Aqui nós modelamos o mundo real. É aqui que moram entidades como `WebhookEvent`, regras de negócio e `Enums`.
- **O porquê:** O Domínio deve ser "puro". Ele **não tem dependência** de nenhum outro projeto e não sabe o que é Entity Framework ou HTTP. Ele apenas dita os contratos (interfaces).

### ⚙️ `ReliableWebhookProcessor.Application`
- **O que é:** Os Casos de Uso (Use Cases) e os fluxos da aplicação.
- **Para que serve:** Orquestra as regras. Por exemplo: *"Verifica idempotência -> Salva no banco -> Dispara processamento"*.
- **O porquê:** Separa o "fluxo do sistema" das "regras do negócio".

### 🔌 `ReliableWebhookProcessor.Infrastructure`
- **O que é:** O contato com o mundo externo (Hardware, Banco de Dados, APIs de terceiros).
- **Para que serve:** É onde o "trabalho sujo" acontece (ex: Entity Framework salvando no PostgreSQL).
- **O porquê:** Se decidirmos trocar o banco de dados, só mexemos nesta camada.

### 🚪 `ReliableWebhookProcessor.Api`
- **O que é:** A porta de entrada do usuário/cliente (Presentation Layer).
- **Para que serve:** Recebe requisições HTTP, valida o JSON, aciona a `Application` e retorna a resposta HTTP.
- **O porquê:** A regra de negócio não deveria saber o que é HTTP.

---

## 4. A Pasta `tests/`

### 🔬 `ReliableWebhookProcessor.UnitTests`
- **O que é:** Testes de Unidade. Testam uma classe ou método isoladamente usando mocks.
- **O porquê:** São extremamente rápidos e dão confiança imediata.

### 🏗️ `ReliableWebhookProcessor.IntegrationTests`
- **O que é:** Testes de Integração ou Ponta-a-Ponta (End-to-End).
- **O porquê:** Garantem que as configurações como Entity Framework, conexões e injeção de dependências estejam funcionando corretamente.

---

## 5. Visão do Ecossistema Completo (End-to-End)

Este diagrama demonstra o fluxo completo desde o momento em que algo acontece na "MiniPay" (como um pagamento aprovado) até o processamento real no background da nossa API, passando pelo banco de dados.

```mermaid
flowchart TD
    subgraph MundoExterno ["Mundo Externo"]
        User(("Usuário"))
        MiniPay["🌐 Plataforma MiniPay"]
    end

    subgraph NossaAPI ["Nossa API (ReliableWebhookProcessor)"]
        WebhookReceiver["🚪 Endpoint: POST /api/webhooks"]
        DB[("🗄️ PostgreSQL")]
        Worker["⚙️ Background Worker"]
        BusinessLogic["🧠 Regra de Negócio Final<br/>(Ex: Liberar Curso)"]
    end

    User -- "1. Compra um curso" --> MiniPay
    MiniPay -- "2. Dispara Webhook<br/>(payment.approved)" --> WebhookReceiver
    
    WebhookReceiver -- "3. Salva Status: Pending" --> DB
    DB -. "3.1 Confirma a Gravação" .-> WebhookReceiver
    
    WebhookReceiver -. "4. Responde 202 Accepted" .-> MiniPay
    
    Worker -- "5. Fica escutando/buscando eventos<br/>Pending no banco" --> DB
    DB -. "5.1 Devolve os eventos Pending" .-> Worker
    
    Worker -- "6. Repassa para a<br/>Regra de Negócio" --> BusinessLogic
    BusinessLogic -- "7. Processamento OK.<br/>Atualiza Status p/ Completed" --> DB
```

---

## 6. Ciclo de Vida do Webhook (Máquina de Estados)

Todo evento recebido pela nossa API segue um ciclo de vida estrito. Ele nasce como `Pending` e viaja pelos estados até o sucesso (`Completed`) ou até desistirmos dele (`DeadLetter`).

```mermaid
stateDiagram-v2
    [*] --> Pending : Recebido do MiniPay
    
    Pending --> Processing : Worker inicia o trabalho
    
    Processing --> Completed : Sucesso!
    Completed --> [*]
    
    Processing --> Failed : Erro (Ex: BD fora do ar)
    
    Failed --> Pending : Retry (Espera o NextRetryAt passar)
    
    Processing --> DeadLetter : Erro repetido (Limite de Retries)
    DeadLetter --> Pending : Reprocessamento Manual
```
