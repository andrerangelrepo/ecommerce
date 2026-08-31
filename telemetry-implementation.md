TASK 15 — Observabilidade com OpenTelemetry: Traces + Métricas
Objetivo

Adicionar observabilidade distribuída com OpenTelemetry, complementando o Serilog implementado na TASK 14.

A divisão passa a ser:

Serilog
   ↓
Logs

OpenTelemetry
   ├── Traces
   └── Metrics

Queremos conseguir responder perguntas como:

Quanto tempo o POST /api/orders levou?
        ↓
Quanto tempo ficou no MediatR?
        ↓
Qual Command/Query foi executado?
        ↓
A requisição terminou com sucesso?

Sem colocar código de OpenTelemetry dentro do Domain e sem transformar o projeto em uma plataforma de observabilidade.

Princípio da TASK

Logging e tracing têm responsabilidades diferentes.

Logging

Exemplo:

Handling CreateOrderCommand
Handled CreateOrderCommand in 31 ms
Tracing

Representa a operação e suas relações:

POST /api/orders
     │
     └── CreateOrderCommand
             │
             └── processamento
Metrics

Representam comportamento agregado:

requisições por segundo
latência HTTP
uso de CPU
uso de memória
GC
SUBTASK 15.1 — Adicionar OpenTelemetry

Adicionar ao projeto:

OrderManagement.Api

Pacotes mínimos:

OpenTelemetry.Extensions.Hosting
OpenTelemetry.Instrumentation.AspNetCore
OpenTelemetry.Instrumentation.Runtime

Para exportação:

OpenTelemetry.Exporter.Console

Opcionalmente, se quisermos deixar preparada integração externa:

OpenTelemetry.Exporter.OpenTelemetryProtocol

Não adicionar OpenTelemetry ao Domain.

SUBTASK 15.2 — Não Acoplar Application ao OpenTelemetry

A camada Application não precisa conhecer tipos específicos da biblioteca OpenTelemetry.

Se criarmos instrumentação própria para Commands e Queries, utilizar:

System.Diagnostics.Activity
System.Diagnostics.ActivitySource

Esses tipos pertencem ao próprio .NET.

Assim:

Application
     ↓
System.Diagnostics

API
     ↓
OpenTelemetry

e não:

Application
     ↓
OpenTelemetry SDK
SUBTASK 15.3 — Domain Continua Totalmente Puro

Não deve existir no Domain:

Activity
ActivitySource
Meter
OpenTelemetry
Tracer
Span
ILogger

O domínio continua responsável apenas por negócio.

SUBTASK 15.4 — Criar Configuração Centralizada

Adicionar configuração:

{
  "OpenTelemetry": {
    "ServiceName": "OrderManagement.Api",
    "ConsoleExporterEnabled": true
  }
}

Não espalhar:

"OrderManagement.Api"

em vários arquivos.

SUBTASK 15.5 — Criar OpenTelemetryOptions

Na API:

Observability
└── OpenTelemetryOptions.cs

Exemplo:

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = string.Empty;

    public bool ConsoleExporterEnabled { get; init; }
}

A configuração deve ser validada no startup.

SUBTASK 15.6 — Definir Resource

Todo telemetry produzido deve identificar a aplicação.

Conceitualmente:

.ConfigureResource(resource =>
    resource.AddService(serviceName))

Isso permite identificar:

service.name = OrderManagement.Api

Esse atributo é fundamental caso futuramente os dados sejam enviados para:

Grafana;
Jaeger;
Tempo;
Azure Monitor;
Datadog;
New Relic;
outro collector.
SUBTASK 15.7 — Criar Extensão de DI

Para não aumentar demais o Program.cs, criar algo semelhante a:

Observability
└── OpenTelemetryExtensions.cs

Responsabilidade:

AddOpenTelemetryObservability()

O Program.cs deve apenas registrar:

builder.Services.AddOpenTelemetryObservability(
    builder.Configuration);
SUBTASK 15.8 — Configurar Tracing HTTP

Adicionar:

.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
});

Isso deve criar automaticamente spans para requisições como:

POST /auth/login

POST /api/orders

GET /api/orders

GET /api/orders/{id}

PATCH /api/orders/{id}/cancel

Não criar middleware próprio apenas para gerar span HTTP.

SUBTASK 15.9 — Não Criar Span Manual do Endpoint

Evitar:

using var activity =
    activitySource.StartActivity("POST /api/orders");

dentro do endpoint.

ASP.NET Core já possui instrumentação apropriada.

Duplicar isso produziria:

POST /api/orders
  └── POST /api/orders

sem valor.

SUBTASK 15.10 — Criar Instrumentação do MediatR

Aqui temos uma oportunidade útil.

Além do span HTTP:

POST /api/orders

queremos enxergar:

CreateOrderCommand

dentro dele.

Criar:

Application
└── Behaviors
    └── TracingBehavior.cs
SUBTASK 15.11 — Criar ActivitySource

Podemos criar uma classe simples no Application:

Observability
└── ApplicationTelemetry.cs

Conceitualmente:

public static class ApplicationTelemetry
{
    public const string ActivitySourceName =
        "OrderManagement.Application";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}

Esse código depende apenas de:

System.Diagnostics
SUBTASK 15.12 — Implementar TracingBehavior

Conceitualmente:

public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity =
            ApplicationTelemetry.ActivitySource.StartActivity(
                requestName,
                ActivityKind.Internal);

        return await next();
    }
}

Isso cria uma hierarquia:

HTTP POST /api/orders
        ↓
CreateOrderCommand
SUBTASK 15.13 — Adicionar Tags Úteis

Adicionar poucas propriedades estruturadas.

Por exemplo:

activity?.SetTag(
    "application.request.name",
    requestName);

Podemos também identificar o tipo:

command
query

se for possível fazê-lo sem criar lógica frágil.

Não é obrigatório.

SUBTASK 15.14 — Não Adicionar Payload ao Span

Proibido fazer:

activity?.SetTag(
    "request.payload",
    JsonSerializer.Serialize(request));

Isso pode expor:

senha;
IDs desnecessários;
dados pessoais;
payloads grandes.

O tracing deve registrar metadados operacionais, não copiar objetos inteiros.

SUBTASK 15.15 — Não Adicionar JWT ao Span

Nunca utilizar tags contendo:

Authorization
Bearer Token
AccessToken
JWT
Password
Jwt:Key

Isso vale tanto para logs quanto para traces.

SUBTASK 15.16 — Registrar ActivitySource

Na configuração de tracing da API:

tracing.AddSource(
    ApplicationTelemetry.ActivitySourceName);

Sem isso, os Activities criados pelo Application podem não ser coletados pelo provider configurado.

SUBTASK 15.17 — Marcar Erros no Span

Quando ocorrer exceção:

TracingBehavior
       ↓
exception

o span deve terminar marcado como erro.

Conceitualmente:

catch (Exception)
{
    activity?.SetStatus(
        ActivityStatusCode.Error);

    throw;
}

O Behavior não deve engolir a exceção.

SUBTASK 15.18 — Não Duplicar Stack Trace Manualmente

Não precisamos fazer algo como:

activity.SetTag("exception.stacktrace", ...)

manualmente.

Evite gravar detalhes demais.

O Serilog já cobre logging da exceção e exporters/instrumentações podem possuir mecanismos próprios.

SUBTASK 15.19 — Ordem dos Behaviors

Agora temos:

LoggingBehavior
TracingBehavior
ValidationBehavior
Handler

Sugestão:

LoggingBehavior
      ↓
TracingBehavior
      ↓
ValidationBehavior
      ↓
Handler

Assim:

logging mede toda a request Application;
tracing cria o span;
validação ocorre dentro do span;
Handler só executa se válido.
Fluxo Final do MediatR
Command / Query
      │
      ▼
LoggingBehavior
      │
      ▼
TracingBehavior
      │
      ▼
ValidationBehavior
      │
      ▼
Handler
SUBTASK 15.20 — Registrar o Behavior

No AddApplication():

services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(
        typeof(DependencyInjection).Assembly);

    configuration.AddOpenBehavior(
        typeof(LoggingBehavior<,>));

    configuration.AddOpenBehavior(
        typeof(TracingBehavior<,>));

    configuration.AddOpenBehavior(
        typeof(ValidationBehavior<,>));
});

Adaptar à API da versão real do MediatR.

SUBTASK 15.21 — Configurar Métricas HTTP

Adicionar:

.WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation();
});

Isso permite coletar métricas relacionadas ao ASP.NET Core sem criar contador manual.

SUBTASK 15.22 — Adicionar Runtime Metrics

Adicionar:

metrics.AddRuntimeInstrumentation();

Isso traz informações úteis sobre o runtime .NET, como:

GC
heap
allocations
threads
runtime

sem código manual.

SUBTASK 15.23 — Não Criar Métricas de Negócio Arbitrariamente

Não criar agora:

orders_created_total
orders_cancelled_total
order_total_amount
customers_total

só porque podemos.

Essas seriam métricas de negócio e precisariam de uma decisão consciente sobre semântica e cardinalidade.

O objetivo desta task é observabilidade técnica.

SUBTASK 15.24 — Evitar Métricas com Alta Cardinalidade

Não usar como label/tag de métrica:

OrderId
CustomerId
JWT subject
ProductName

Isso criaria séries quase ilimitadas.

Para métricas:

Cardinalidade baixa é fundamental.

SUBTASK 15.25 — Console Exporter

Para demonstrar o recurso sem exigir infraestrutura externa, utilizar inicialmente:

Console Exporter

Isso permite executar a aplicação e visualizar telemetry localmente.

Conceitualmente:

if (options.ConsoleExporterEnabled)
{
    tracing.AddConsoleExporter();
}

E equivalente para metrics.

SUBTASK 15.26 — Evitar Produção Dependente do Console Exporter

O Console Exporter deve ser tratado como recurso de desenvolvimento/demonstração.

Não precisamos assumir que seria a estratégia de observabilidade de produção.

O README pode explicar:

O Console Exporter é utilizado para demonstração local e pode ser substituído por OTLP sem alterar Application ou Domain.

SUBTASK 15.27 — Preparar OTLP sem Obrigar Collector

Se quisermos preparar integração real, podemos adicionar configuração opcional:

{
  "OpenTelemetry": {
    "ServiceName": "OrderManagement.Api",
    "ConsoleExporterEnabled": true,
    "Otlp": {
      "Enabled": false,
      "Endpoint": "http://localhost:4317"
    }
  }
}

Somente registrar:

AddOtlpExporter

quando:

Enabled = true

Assim o projeto não depende de collector para funcionar.

SUBTASK 15.28 — Não Adicionar Jaeger/Collector ao Docker Compose Agora

Evitar ampliar o Compose para:

api
otel-collector
jaeger
prometheus
grafana

Isso mudaria bastante o escopo da entrega.

Nesta task queremos:

aplicação instrumentada

e não:

plataforma completa de observabilidade
SUBTASK 15.29 — Correlacionar Logs e Traces

Uma grande vantagem do Activity é que o ASP.NET Core mantém:

TraceId
SpanId

no contexto.

O Serilog pode enriquecer os logs para incluir esses identificadores.

Resultado conceitual:

[INF] Handling CreateOrderCommand
TraceId=5f42...
SpanId=732a...

Isso permite sair de um log e encontrar o trace correspondente.

SUBTASK 15.30 — Adicionar Enriquecimento de TraceId ao Serilog

Se a integração atual não exibir os valores automaticamente, podemos criar um enriquecimento simples ou utilizar propriedades disponíveis no LogContext.

Não adicionar pacote somente por conveniência se pudermos aproveitar Activity.Current.

O objetivo é obter:

TraceId
SpanId

sem criar infraestrutura complexa.

SUBTASK 15.31 — Não Criar Correlation ID Paralelo

Agora que temos:

TraceId

evitar adicionar simultaneamente:

CorrelationId
RequestId customizado
TransactionId
OperationId

sem necessidade.

Para este projeto:

TraceId é suficiente para correlação distribuída.

SUBTASK 15.32 — Trace do Login

Executar:

POST /auth/login

Deve existir span HTTP.

Porém não existe Command/Query caso o login permaneça implementado diretamente na API.

Isso é aceitável.

Não mover login para MediatR apenas para gerar span.

SUBTASK 15.33 — Trace da Criação

Executar:

POST /api/orders

Esperado conceitualmente:

POST /api/orders
│
└── CreateOrderCommand
SUBTASK 15.34 — Trace da Listagem

Executar:

GET /api/orders

Esperado:

GET /api/orders
│
└── GetOrdersQuery
SUBTASK 15.35 — Trace do GET por ID

Esperado:

GET /api/orders/{id}
│
└── GetOrderByIdQuery
SUBTASK 15.36 — Trace do Cancelamento

Esperado:

PATCH /api/orders/{id}/cancel
│
└── CancelOrderCommand
SUBTASK 15.37 — Validação Inválida Deve Aparecer no Trace

Exemplo:

GET /api/orders?page=0

Fluxo:

HTTP span
   ↓
GetOrdersQuery span
   ↓
ValidationBehavior
   ↓
ValidationException

O span interno deve finalizar com estado de erro.

A resposta continua:

400 Bad Request
SUBTASK 15.38 — Regra de Negócio Inválida

Segundo cancelamento:

PATCH /api/orders/{id}/cancel

sobre pedido:

Cancelled

Esperado:

HTTP 409

E o trace deve mostrar falha na operação:

CancelOrderCommand

Não precisamos converter o span em linguagem HTTP dentro do Application.

SUBTASK 15.39 — Exceção Inesperada

Caso uma exceção inesperada ocorra:

TracingBehavior
      ↓
ActivityStatusCode.Error
      ↓
exception propagada
      ↓
GlobalExceptionHandler
      ↓
500

Logging e tracing continuam complementares.

SUBTASK 15.40 — Não Instrumentar Repository Manualmente Agora

Não criar:

OrderRepository.GetById span
OrderRepository.Add span
OrderRepository.Update span

manualmente nesta etapa.

Isso adicionaria ruído.

Começar com:

HTTP
+
Application Command/Query

já fornece boa visibilidade.

SUBTASK 15.41 — EF Core Instrumentation é Opcional

Se a versão de OpenTelemetry utilizada e o pacote escolhido estiverem estáveis e compatíveis, podemos adicionar:

OpenTelemetry.Instrumentation.EntityFrameworkCore

e:

tracing.AddEntityFrameworkCoreInstrumentation();

Isso é interessante para visualizar consultas SQL.

Porém deve ser tratado como opcional.

O requisito principal desta task não depende disso.

SUBTASK 15.42 — Não Expor SQL Sensível

Se EF instrumentation for habilitada, revisar configurações para não expor dados sensíveis de parâmetros.

Não habilitar por padrão mecanismos equivalentes a:

SetDbStatementForText
sensitive data logging

sem avaliar o impacto.

SUBTASK 15.43 — Não Instrumentar SQLite Manualmente

Não precisamos de:

SQLite telemetry wrapper
database interceptor customizado
DbCommand interceptor próprio

Isso seria exagero para o projeto.

SUBTASK 15.44 — Criar Health Check de Forma Independente

Se já existe:

GET /health

manter.

OpenTelemetry não deve substituir health checks.

São conceitos diferentes:

Health → serviço está funcionando?

Telemetry → como o serviço está se comportando?
SUBTASK 15.45 — Não Exportar Health Check se Gerar Ruído Excessivo

Podemos filtrar /health da instrumentação HTTP caso esteja poluindo traces.

Por exemplo conceitualmente:

Filter request.Path != "/health"

Isso é opcional.

Para um projeto pequeno não é obrigatório.

SUBTASK 15.46 — Testar TracingBehavior

Criar:

Application.Tests
└── Behaviors
    └── TracingBehaviorTests.cs

O objetivo não é testar o SDK OpenTelemetry.

Testar nosso pipeline.

SUBTASK 15.47 — Teste de Sucesso

Dado:

next() retorna response

Esperado:

next() executado exatamente uma vez;
response retornada;
nenhuma exceção introduzida pelo Behavior.
SUBTASK 15.48 — Teste de Erro

Dado:

next() lança exception

Esperado:

mesma exception é propagada

O Behavior não pode transformar exceções.

SUBTASK 15.49 — Evitar Teste Frágil de Trace

Não testar:

SpanId específico
TraceId específico
timestamp específico
duração exata
texto exato do console exporter

Esses valores são naturalmente variáveis.

SUBTASK 15.50 — Testes Existentes Devem Permanecer Verdes

Executar:

dotnet test

OpenTelemetry deve ser completamente transparente ao comportamento funcional.

Não deve alterar:

HTTP status
payload
domain rules
repository behavior
authentication
SUBTASK 15.51 — Integração com WebApplicationFactory

Os testes existentes com:

WebApplicationFactory<Program>

devem continuar funcionando mesmo que nenhum collector esteja disponível.

Isso é crítico.

Não configurar OTLP obrigatório.

SUBTASK 15.52 — Aplicação Não Pode Falhar Sem Collector

Mesmo quando OTLP estiver preparado:

collector indisponível

não pode impedir:

API startup
HTTP requests
testes

Observabilidade não deve se tornar dependência funcional.

SUBTASK 15.53 — Docker Continua com Apenas API

Executar:

docker compose up --build

O projeto deve continuar funcionando com o Compose atual.

Não exigir:

OpenTelemetry Collector
Jaeger
Prometheus
SUBTASK 15.54 — Visualizar Telemetry no Docker

Com Console Exporter habilitado:

docker compose logs -f api

deve permitir visualizar os traces/metrics exportados.

Isso serve como demonstração simples.

SUBTASK 15.55 — Cuidado com Poluição do Console

Serilog + Console Exporter podem gerar muito conteúdo.

Por isso, manter o exporter configurável:

ConsoleExporterEnabled

Assim podemos desligá-lo facilmente.

SUBTASK 15.56 — Não Misturar Logs com Métricas

Evitar coisas como:

logger.LogInformation(
    "RequestCount = {Count}",
    count);

se o objetivo é representar métrica.

Métricas possuem mecanismo próprio.

SUBTASK 15.57 — Não Criar Contadores Globais Estáticos Improvisados

Não fazer:

static int RequestCount;

nem:

Interlocked.Increment(...)

para simular metric.

Utilizar OpenTelemetry/Meter quando houver métrica customizada real.

SUBTASK 15.58 — Não Criar TelemetryService

Evitar abstrações como:

ITelemetryService
TelemetryManager
TelemetryProvider
ObservabilityService
TracingService

sem necessidade.

Temos:

ActivitySource
+
OpenTelemetry configuration

e isso já resolve o problema.

SUBTASK 15.59 — Revisar Cardinalidade

Tags aceitáveis:

request name
activity type
service name
HTTP route
HTTP method
status code

Tags perigosas:

OrderId
CustomerId
ProductName
Password
JWT
Request body
Response body
SUBTASK 15.60 — README

Adicionar uma seção pequena:

## Observability

Explicar:

Serilog para logs;
OpenTelemetry para traces e métricas;
Console Exporter para demonstração;
possibilidade de configurar OTLP;
nenhuma infraestrutura externa é obrigatória para executar a aplicação.

Não transformar README em manual de OpenTelemetry.

Estrutura Esperada
Application
OrderManagement.Application
│
├── Behaviors
│   ├── LoggingBehavior.cs
│   ├── TracingBehavior.cs
│   └── ValidationBehavior.cs
│
├── Observability
│   └── ApplicationTelemetry.cs
│
└── DependencyInjection.cs
API
OrderManagement.Api
│
├── Observability
│   ├── OpenTelemetryOptions.cs
│   └── OpenTelemetryExtensions.cs
│
├── Program.cs
├── appsettings.json
└── ...
Pipeline de Observabilidade Final
                      HTTP REQUEST
                           │
                           ▼
                ASP.NET Core Instrumentation
                           │
                    HTTP Trace Span
                           │
                           ▼
                Serilog Request Logging
                           │
                           ▼
                        Endpoint
                           │
                           ▼
                        MediatR
                           │
                           ▼
                  LoggingBehavior
                           │
                           ▼
                  TracingBehavior
                  Application Span
                           │
                           ▼
                 ValidationBehavior
                           │
                           ▼
                        Handler
                           │
                    ┌──────┴──────┐
                    ▼             ▼
                  Domain     Infrastructure

Em paralelo:

ASP.NET Core
     │
     ├── HTTP metrics
     │
     └── Runtime metrics
Exemplo Conceitual de Trace
TraceId: 43f1c...

POST /api/orders                     52 ms
└── CreateOrderCommand               38 ms

Enquanto os logs associados podem mostrar:

[INF] Handling CreateOrderCommand
      TraceId=43f1c...

[INF] Handled CreateOrderCommand in 38 ms
      TraceId=43f1c...

[INF] HTTP POST /api/orders responded 201 in 52 ms
      TraceId=43f1c...

Esse é o ganho principal:

logs e traces passam a contar a mesma história por perspectivas diferentes.

Casos de Validação
CT01 — Criação válida
POST /api/orders

Esperado:

201

Trace:
POST /api/orders
└── CreateOrderCommand
CT02 — Query válida
GET /api/orders

Esperado:

200

Trace:
GET /api/orders
└── GetOrdersQuery
CT03 — Validação inválida
GET /api/orders?page=0

Esperado:

400

GetOrdersQuery span marcado com erro
CT04 — Regra de negócio inválida

Cancelar pedido já cancelado:

409

CancelOrderCommand span marcado com erro
CT05 — Sem JWT
GET /api/orders

Esperado:

401

Deve existir span HTTP.

Como o endpoint/MediatR não executou:

não deve existir GetOrdersQuery span

Isso demonstra corretamente onde a requisição foi interrompida.

CT06 — Login
POST /auth/login

Esperado:

200
HTTP span presente
nenhuma senha/token presente no trace
Critérios de Aceite — TASK 15
CA	Critério
CA-15.1	OpenTelemetry configurado na API
CA-15.2	Domain não depende de OpenTelemetry
CA-15.3	Application não depende do OpenTelemetry SDK
CA-15.4	Application utiliza apenas ActivitySource para tracing próprio
CA-15.5	Resource possui service.name
CA-15.6	ASP.NET Core tracing está habilitado
CA-15.7	ASP.NET Core metrics estão habilitadas
CA-15.8	Runtime metrics estão habilitadas
CA-15.9	Existe TracingBehavior
CA-15.10	Existe ActivitySource próprio do Application
CA-15.11	ActivitySource está registrado no OpenTelemetry
CA-15.12	Commands geram spans internos
CA-15.13	Queries geram spans internos
CA-15.14	Erros marcam o span adequadamente
CA-15.15	Exceções continuam sendo propagadas
CA-15.16	LoggingBehavior continua funcionando
CA-15.17	Ordem dos Behaviors é explícita
CA-15.18	ValidationBehavior continua impedindo Handler inválido
CA-15.19	Payload completo não é adicionado aos spans
CA-15.20	Password não aparece nos traces
CA-15.21	JWT não aparece nos traces
CA-15.22	CustomerId/OrderId não são usados como tags de métricas
CA-15.23	Console exporter é configurável
CA-15.24	Nenhum collector externo é obrigatório
CA-15.25	Aplicação funciona sem OTLP
CA-15.26	WebApplicationFactory continua funcionando
CA-15.27	Docker continua funcionando sem serviços adicionais
CA-15.28	Existe teste do TracingBehavior
CA-15.29	Logs e traces podem ser correlacionados por TraceId quando configurado
CA-15.30	Contratos HTTP permanecem inalterados
CA-15.31	dotnet build verde
CA-15.32	dotnet test verde
CA-15.33	Zero novo warning relevante
Validação Técnica

Executar:

dotnet clean
dotnet restore
dotnet build
dotnet test

Depois:

dotnet run --project src/OrderManagement.Api

Testar:

POST  /auth/login
POST  /api/orders
GET   /api/orders
GET   /api/orders/{id}
PATCH /api/orders/{id}/cancel

Validar que traces são produzidos e que:

password
access token
JWT key
request body completo
response body completo

não aparecem.

Validação Docker

Executar:

docker compose up --build

Depois:

docker compose logs -f api

A API deve funcionar normalmente sem:

Jaeger
Prometheus
Grafana
OpenTelemetry Collector
Busca de Hardening

Pesquisar globalmente:

ActivitySource
Activity.Current
SetTag
OpenTelemetry
AddSource
AddAspNetCoreInstrumentation
AddRuntimeInstrumentation
AddConsoleExporter
Password
AccessToken
Authorization
CustomerId
OrderId
JsonSerializer.Serialize

Revisar cada tag adicionada para garantir que ela realmente pertence à telemetry.

O Que Não Fazer Nesta Task

Não adicionar:

Jaeger;
Grafana;
Prometheus;
Tempo;
Zipkin;
Elasticsearch;
OpenTelemetry Collector obrigatório;
dashboards;
alertas;
tracing manual de todos os métodos;
spans em entidades do Domain;
spans para getters/setters;
payloads como tags;
IDs de alta cardinalidade como métricas;
ITelemetryService;
wrappers genéricos de tracing.
Resultado Esperado

Ao final teremos três pilares básicos de observabilidade:

OBSERVABILITY

Logs
└── Serilog

Traces
├── ASP.NET Core
└── MediatR / Application

Metrics
├── ASP.NET Core
└── .NET Runtime

Sem comprometer nossa arquitetura:

Domain
     ↑
Application
     ↑
Infrastructure

API
 ├── Serilog
 └── OpenTelemetry