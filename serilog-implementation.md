TASK 14 — Logging Estruturado com Serilog + LoggingBehavior
Objetivo

Adicionar logging estruturado à aplicação usando:

Serilog;
integração com ASP.NET Core;
logging de requests do MediatR através de LoggingBehavior;
correlação mínima por request;
logs úteis sem vazar dados sensíveis;
configuração centralizada;
impacto zero nas regras de negócio.

A meta é sair de logs genéricos como:

Starting request
Finished request

para algo útil e pesquisável:

Handling CreateOrderCommand
Handled CreateOrderCommand in 42 ms

com propriedades estruturadas.

SUBTASK 14.1 — Adicionar Serilog na API

Adicionar ao projeto:

OrderManagement.Api

Pacotes sugeridos:

Serilog.AspNetCore
Serilog.Sinks.Console

Opcionalmente:

Serilog.Settings.Configuration

caso a versão utilizada exija explicitamente.

Não adicionar Serilog ao Domain.

SUBTASK 14.2 — Manter Logging desacoplado do Domain

O Domain não deve conhecer:

Serilog
ILogger
ASP.NET Core
MediatR logging
HTTP

Não fazer:

public class Order
{
    private readonly ILogger<Order> _logger;
}

Entidades de domínio não devem depender de infraestrutura de logging.

SUBTASK 14.3 — Configurar Serilog no Bootstrap

Na API, configurar Serilog logo no startup.

Conceitualmente:

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

A sintaxe exata pode variar conforme a versão.

O importante é:

ASP.NET Core
   ↓
Serilog
   ↓
Console estruturado
SUBTASK 14.4 — Configurar via appsettings.json

Evitar configurar tudo hardcoded no Program.cs.

Exemplo:

{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ],
    "Enrich": [
      "FromLogContext"
    ]
  }
}

Isso permite alterar verbosidade sem recompilar.

SUBTASK 14.5 — Evitar Logs Excessivos do Framework

Sem overrides, logs de:

Microsoft
EF Core
ASP.NET Core

podem poluir bastante a saída.

Usar nível adequado, por exemplo:

Microsoft → Warning
Microsoft.AspNetCore → Warning
Microsoft.EntityFrameworkCore → Warning

Não silenciar erros.

SUBTASK 14.6 — Adicionar Request Logging do Serilog

Registrar:

app.UseSerilogRequestLogging();

Isso permite logs HTTP como:

HTTP POST /api/orders responded 201 in 54 ms

com propriedades estruturadas.

SUBTASK 14.7 — Posicionar Middleware Corretamente

O request logging deve ser posicionado de forma que capture o pipeline relevante.

Conceitualmente:

UseSerilogRequestLogging
        ↓
UseExceptionHandler
        ↓
UseAuthentication
        ↓
UseAuthorization
        ↓
Endpoints

A ordem exata deve respeitar o pipeline atual da aplicação.

SUBTASK 14.8 — Criar LoggingBehavior

Na camada Application:

Behaviors
└── LoggingBehavior.cs

Ele deve implementar:

IPipelineBehavior<TRequest, TResponse>

Responsabilidade:

registrar início;
medir duração;
registrar fim;
registrar falhas.
SUBTASK 14.9 — Usar ILogger<T>, não Serilog diretamente no Application

No Application, preferir:

ILogger<LoggingBehavior<TRequest, TResponse>>

em vez de:

Serilog.Log
ILogger do Serilog

Isso preserva a abstração:

Application
   ↓
Microsoft.Extensions.Logging abstractions

e deixa Serilog como implementação externa na API.

SUBTASK 14.10 — Estrutura do Behavior

Conceitualmente:

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName}",
            requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogError(
                exception,
                "Failed handling {RequestName} after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}

A implementação pode variar.

SUBTASK 14.11 — Não Serializar o Request Inteiro

Evitar:

logger.LogInformation(
    "Request: {@Request}",
    request);

Isso pode vazar:

password;
JWT;
dados pessoais;
payloads grandes;
informações internas.

Para este projeto, logging do tipo da request já é suficiente.

SUBTASK 14.12 — Não Logar LoginRequest

Especial cuidado com:

LoginRequest

Nunca registrar:

Password
AccessToken
Authorization header
JWT completo

Exemplo proibido:

Login attempt for dev@martech.com with password Senha@123
SUBTASK 14.13 — Não Logar AccessToken

Também evitar:

Generated token: eyJhbGciOi...

ou:

Authorization: Bearer ...

Tokens são credenciais.

SUBTASK 14.14 — Logging estruturado

Preferir:

logger.LogInformation(
    "Handling {RequestName}",
    requestName);

em vez de interpolação:

logger.LogInformation(
    $"Handling {requestName}");

Motivo:

Serilog consegue indexar RequestName como propriedade
SUBTASK 14.15 — Medir Duração

Usar:

Stopwatch

ou solução equivalente.

Não usar:

DateTime.Now

para cálculo de duração.

SUBTASK 14.16 — Logar Falhas sem Engolir Exceções

O Behavior pode registrar:

Error

mas deve relançar:

throw;

Não fazer:

catch
{
    return default!;
}

O tratamento HTTP continua sendo responsabilidade do:

GlobalExceptionHandler
SUBTASK 14.17 — Evitar Duplicação Excessiva de Erro

Agora teremos potencialmente:

LoggingBehavior
+
GlobalExceptionHandler

registrando a mesma exceção.

Isso pode gerar logs duplicados.

A abordagem recomendada:

LoggingBehavior → registra contexto Application
GlobalExceptionHandler → traduz para HTTP

Se o exception handler também logar, evitar registrar a mesma exceção novamente como Error sem necessidade.

Uma das camadas pode usar nível diferente ou simplesmente não duplicar.

SUBTASK 14.18 — Ordem dos Behaviors

Hoje temos:

ValidationBehavior

e agora teremos:

LoggingBehavior

A ordem deve ser consciente.

Sugestão:

LoggingBehavior
        ↓
ValidationBehavior
        ↓
Handler

Assim até requests inválidas são medidas e registradas como tentativa de processamento.

Fluxo:

Request
 ↓
LoggingBehavior
 ↓
ValidationBehavior
 ↓
Handler
SUBTASK 14.19 — Registrar Behaviors Explicitamente

No AddApplication():

services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(
        typeof(DependencyInjection).Assembly);

    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

A API pode variar conforme a versão instalada.

O importante é preservar a ordem.

SUBTASK 14.20 — Evitar Logging Dentro de Todo Handler

Não adicionar:

_logger.LogInformation(...)

em todos os handlers para registrar:

início
fim
duração

Isso é responsabilidade transversal do Behavior.

Handlers só devem ter logs próprios quando houver algo realmente relevante ao caso de uso.

SUBTASK 14.21 — Logging específico de Handler só quando necessário

Exemplo de algo que pode fazer sentido:

Order {OrderId} not found during cancellation

Mas mesmo isso não é obrigatório.

Não transformar cada Handler em uma sequência de logs.

SUBTASK 14.22 — Correlação por Request

O Serilog request logging já trabalha com contexto HTTP.

Podemos enriquecer logs com:

RequestId
TraceIdentifier

Sem criar uma infraestrutura complexa.

Uma abordagem simples é usar:

HttpContext.TraceIdentifier

ou confiar no contexto padrão do ASP.NET Core.

SUBTASK 14.23 — Não Criar Correlation ID Customizado Sem Necessidade

Evitar nesta task:

X-Correlation-Id middleware
CorrelationIdAccessor
CorrelationIdProvider
CorrelationIdGenerator

se o framework já fornece identificador suficiente.

Podemos adicionar isso depois caso haja requisito.

SUBTASK 14.24 — Enrich com contexto

Configurar:

Enrich.FromLogContext()

Isso permite incluir propriedades adicionadas ao contexto atual.

SUBTASK 14.25 — SourceContext

Serilog deve manter:

SourceContext

automaticamente quando usamos ILogger<T>.

Isso ajuda a identificar origem dos logs.

Exemplo:

OrderManagement.Application.Behaviors.LoggingBehavior
SUBTASK 14.26 — Formato de Console

Para desenvolvimento, saída legível é suficiente.

Pode utilizar o formatter padrão.

Não precisamos adicionar:

CompactJsonFormatter
Seq
Elastic
Grafana Loki
Splunk
Application Insights

nesta task.

SUBTASK 14.27 — Logging no Docker

Como Docker captura:

stdout
stderr

o sink de console é suficiente.

Assim:

docker compose logs -f api

deve exibir os logs da aplicação.

Não criar arquivo de log dentro do container.

SUBTASK 14.28 — Não Usar File Sink em Docker

Evitar:

Serilog.Sinks.File

com:

/app/logs/log.txt

porque isso exigiria:

volume;
rotação;
limpeza;
permissões;
estratégia de persistência.

Para containers:

Console é o padrão correto.

SUBTASK 14.29 — Testar Logs de Login

Executar:

POST /auth/login

Esperado:

HTTP POST /auth/login responded 200 ...

Não esperado:

Password = Senha@123
AccessToken = ey...
SUBTASK 14.30 — Testar Logs de Command

Criar pedido.

Esperado conceitualmente:

Handling CreateOrderCommand

Handled CreateOrderCommand in 35 ms

HTTP POST /api/orders responded 201 in 48 ms
SUBTASK 14.31 — Testar Logs de Query

Executar:

GET /api/orders

Esperado:

Handling GetOrdersQuery
Handled GetOrdersQuery in ...
SUBTASK 14.32 — Testar Falha de Validação

Executar request inválida.

Exemplo:

page = 0

Esperado:

Handling GetOrdersQuery
Failed handling GetOrdersQuery after ...

e resposta:

400
SUBTASK 14.33 — Cuidado com nível de validação

Uma ValidationException não é necessariamente um erro operacional do sistema.

Portanto pode não fazer sentido registrá-la como:

Error

Uma implementação melhor pode distinguir:

ValidationException → Warning
Domain business exception → Warning
Unexpected exception → Error

Isso reduz ruído operacional.

SUBTASK 14.34 — Classificar Falhas Conhecidas

No LoggingBehavior, podemos tratar:

ValidationException
OrderCannotBeCancelledException

como falhas conhecidas.

Exemplo conceitual:

Warning

Enquanto:

Exception inesperada

fica em:

Error

Não precisa criar uma hierarquia complexa.

SUBTASK 14.35 — Não Acoplar Behavior ao HTTP

Mesmo classificando exceções, o Behavior não deve conhecer:

400
409
500
ProblemDetails

Ele conhece exceções, não status HTTP.

SUBTASK 14.36 — Preferir Eventos Semânticos

Mensagens sugeridas:

Handling {RequestName}
Handled {RequestName} in {ElapsedMilliseconds} ms
Request {RequestName} failed validation after {ElapsedMilliseconds} ms
Request {RequestName} failed after {ElapsedMilliseconds} ms

Evitar mensagens vagas:

Started
Done
Error occurred
SUBTASK 14.37 — Não Logar Response Inteira

Evitar:

logger.LogInformation(
    "Response {@Response}",
    response);

Mesmos motivos do request:

exposição de dados;
payload grande;
duplicação;
custo.
SUBTASK 14.38 — Testar LoggingBehavior

Criar:

Application.Tests
└── Behaviors
    └── LoggingBehaviorTests.cs

Não precisamos validar internals do Serilog.

Testamos nosso Behavior.

SUBTASK 14.39 — Teste de sucesso

Cenário:

next() retorna sucesso

Esperado:

next() executado exatamente uma vez;
resultado retornado sem alteração;
nenhuma exceção.

Não é necessário testar string exata de log se isso tornar o teste frágil.

SUBTASK 14.40 — Teste de falha

Cenário:

next() lança exception

Esperado:

mesma exceção é relançada

O Behavior não pode engolir ou substituir a exceção.

SUBTASK 14.41 — Testar que Handler Continua Funcionando

Após adicionar Behavior:

dotnet test

Todos os testes antigos devem continuar verdes.

Logging não pode alterar comportamento funcional.

SUBTASK 14.42 — Teste de integração

Os testes de integração existentes devem continuar passando sem alteração de contrato.

Especialmente:

POST /api/orders → 201
GET /api/orders → 200
PATCH cancel → comportamento existente
SUBTASK 14.43 — Não Testar Saída de Console em Integração

Evitar testes que dependam de texto exato produzido no terminal.

Logs são preocupação operacional.

Os testes devem focar:

pipeline continua funcional
SUBTASK 14.44 — Configuração de Development

Podemos ter nível:

Information

para nossa aplicação.

Isso permite visualizar commands e queries facilmente.

SUBTASK 14.45 — Configuração de Production

Mesmo que o teste não tenha ambiente real de produção, manter possibilidade de override via:

appsettings.Production.json
environment variables

Não criar arquivo de produção sem necessidade.

SUBTASK 14.46 — Evitar Sensitive Data Logging do EF

Não habilitar:

EnableSensitiveDataLogging()

na configuração normal.

Isso pode imprimir valores de parâmetros SQL.

Também evitar:

EnableDetailedErrors()

indiscriminadamente em produção.

SUBTASK 14.47 — Procurar Console.WriteLine

Após Serilog, pesquisar:

Console.WriteLine
Console.Error.WriteLine
Debug.WriteLine

Não deve haver logging manual perdido pela aplicação.

Use:

ILogger<T>

quando necessário.

SUBTASK 14.48 — Não Usar Serilog Estático

Evitar espalhar:

Log.Information(...)
Log.Error(...)

pelo Application.

Preferir DI:

ILogger<T>

O uso estático pode ser aceitável apenas no bootstrap para erro crítico de inicialização, se necessário.

SUBTASK 14.49 — Shutdown Correto

Dependendo da integração usada, garantir flush apropriado do Serilog ao finalizar.

A integração:

UseSerilog()

normalmente gerencia o ciclo de vida.

Se utilizar bootstrap logger manual, garantir:

Log.CloseAndFlush()

somente quando realmente necessário.

Não complicar o startup sem motivo.

SUBTASK 14.50 — Falha no Startup

Opcionalmente, erros críticos na inicialização podem ser capturados pelo bootstrap logger.

Mas isso só vale se não tornar Program.cs significativamente mais complexo.

Para este teste, simplicidade continua sendo prioridade.

Estrutura Esperada
Application
OrderManagement.Application
│
├── Behaviors
│   ├── LoggingBehavior.cs
│   └── ValidationBehavior.cs
│
└── DependencyInjection.cs
API
OrderManagement.Api
│
├── Program.cs
├── appsettings.json
└── ...

Não precisamos criar:

Logging/
Serilog/
LoggingService/
LogManager/

sem necessidade.

Pipeline Final

Teremos duas camadas diferentes de logging.

HTTP
Serilog Request Logging

Exemplo:

POST /api/orders → 201 → 48ms
Application
LoggingBehavior

Exemplo:

CreateOrderCommand → 35ms

Fluxo:

HTTP Request
     ↓
Serilog Request Logging
     ↓
Authentication
     ↓
Endpoint
     ↓
MediatR
     ↓
LoggingBehavior
     ↓
ValidationBehavior
     ↓
Handler
     ↓
Domain / Repository
     ↓
LoggingBehavior
     ↓
Endpoint
     ↓
Serilog Request Logging
Exemplo de Logs Esperados

Para criação válida:

[INF] Handling CreateOrderCommand
[INF] Handled CreateOrderCommand in 31 ms
[INF] HTTP POST /api/orders responded 201 in 46 ms

Para validação inválida:

[INF] Handling CreateOrderCommand
[WRN] Request CreateOrderCommand failed validation after 3 ms
[INF] HTTP POST /api/orders responded 400 in 9 ms

Para falha inesperada:

[INF] Handling CreateOrderCommand
[ERR] Request CreateOrderCommand failed after 27 ms
System.Exception: ...
[INF] HTTP POST /api/orders responded 500 in 34 ms
O Que NÃO Deve Aparecer nos Logs
Senha@123
Authorization: Bearer ey...
accessToken completo
Jwt:Key
connection string com segredo
payload completo do login
stack trace para validação normal
dados sensíveis desnecessários
Critérios de Aceite — TASK 14
CA	Critério
CA-14.1	Serilog integrado à API
CA-14.2	Console sink configurado
CA-14.3	Configuração vem de appsettings
CA-14.4	Enrich.FromLogContext() configurado
CA-14.5	ASP.NET Core request logging habilitado
CA-14.6	Existe LoggingBehavior<TRequest,TResponse>
CA-14.7	Behavior utiliza ILogger<T>
CA-14.8	Application não depende diretamente de Serilog
CA-14.9	Domain não possui dependência de logging
CA-14.10	Behavior registra início da request
CA-14.11	Behavior registra duração
CA-14.12	Behavior registra conclusão
CA-14.13	Behavior registra falha
CA-14.14	Exceções são relançadas
CA-14.15	CancellationToken continua propagado
CA-14.16	LoggingBehavior executa antes do ValidationBehavior
CA-14.17	Requests inválidas não executam Handler
CA-14.18	Validações podem ser registradas como Warning
CA-14.19	Falhas inesperadas são Error
CA-14.20	Nenhum request é serializado integralmente nos logs
CA-14.21	Nenhuma response é serializada integralmente
CA-14.22	Password nunca é logada
CA-14.23	JWT nunca é logado
CA-14.24	Chave JWT nunca é logada
CA-14.25	Não existe Console.WriteLine usado como logging
CA-14.26	Não existe file sink desnecessário
CA-14.27	Logs funcionam via docker compose logs
CA-14.28	Existe teste do LoggingBehavior
CA-14.29	Todos os testes antigos continuam verdes
CA-14.30	Contrato HTTP permanece inalterado
CA-14.31	dotnet build verde
CA-14.32	dotnet test verde
Validação Manual

Executar:

dotnet clean
dotnet restore
dotnet build
dotnet test

Depois:

dotnet run --project src/OrderManagement.Api

Executar:

POST /auth/login
POST /api/orders
GET /api/orders
GET /api/orders/{id}
PATCH /api/orders/{id}/cancel

Validar no console:

HTTP request
+
Command/Query
+
duração
+
resultado

sem dados sensíveis.

Validação Docker

Executar:

docker compose up --build

Depois:

docker compose logs -f api

Executar alguns endpoints.

Esperado:

logs ASP.NET
+
logs do LoggingBehavior

diretamente no stdout do container.

Busca de Hardening

Pesquisar globalmente:

Console.WriteLine
Log.Information
Log.Error
{@Request}
{@Response}
Password
AccessToken
Authorization
EnableSensitiveDataLogging
Serilog.Sinks.File

Cada ocorrência deve ser analisada.

O Que Não Fazer Nesta Task

Não adicionar:

Seq;
Elasticsearch;
Kibana;
Loki;
Grafana;
Application Insights;
OpenTelemetry;
file sink;
banco de logs;
correlation middleware complexo;
auditoria de negócio;
persistência de logs;
logging de payload integral;
middleware customizado para substituir o Serilog Request Logging.
Resultado Esperado

Ao final teremos observabilidade básica, porém profissional:

                 HTTP
                  │
                  ▼
       Serilog Request Logging
                  │
                  ▼
               MediatR
                  │
                  ▼
           LoggingBehavior
                  │
                  ▼
         ValidationBehavior
                  │
                  ▼
               Handler
                  │
                  ▼
        Domain / Infrastructure

Com isso o projeto passa a ter:

✅ Logging estruturado
✅ HTTP request logging
✅ Command/Query logging
✅ duração
✅ falhas
✅ integração com Docker
✅ proteção contra logging de dados sensíveis