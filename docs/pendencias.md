# Pendências — Aderência às INSTRUÇÕES

Checklist do que falta para aderência completa ao [`INSTRUCOES.md`](../INSTRUCOES.md) (o "Teste Prático", contrato soberano). Levantado por revisão da implementação atual; ver histórico de conversa para o detalhamento de cada gap.

Marcar `[x]` conforme cada item for implementado.

---

## 1. Docker (Stack Obrigatória)

- [x] `Dockerfile` ([Dockerfile](../Dockerfile))
  - [x] multi-stage build — testado com `docker build`, cache de `restore` confirmado
  - [x] SDK do .NET presente somente na etapa de build
  - [x] imagem final contém apenas o runtime — confirmado por inspeção do container (`dotnet --list-sdks` vazio, sem `/src`)
  - [x] nenhum segredo (ex.: `Jwt:Key`) hardcoded na imagem — a chave vem só via `Jwt__Key` no `docker-compose.yml`, nunca do Dockerfile/imagem
- [x] `docker-compose.yml` ([docker-compose.yml](../docker-compose.yml))
  - [x] serviço da API
  - [x] SQLite permanece embarcado (sem container de banco separado)
  - [x] volume para o arquivo SQLite — testado com container removido (`down` sem `-v`) e recriado, dados persistiram
  - [x] `Jwt:Key` (e demais segredos) injetados via variável de ambiente, não hardcoded no compose — `Jwt__Key: "development-only-key-change-in-production"`, testado com um token assinado com a chave do `appsettings.json` sendo rejeitado (`401`), confirmando que o override é o valor efetivo

## 2. README — instruções de execução via Docker (Stack Obrigatória)

- [x] Seção "Executar com Docker" no README ([README.md](../README.md))
  - [x] comando de build da imagem isolado (`docker build -t ecommerce-api .`), além do `docker compose up --build`
  - [x] comando de subida via `docker compose`, incluindo a diferença entre `down` e `down -v`
  - [x] variáveis de ambiente necessárias (`Jwt__Key`) e como fornecê-las

## 3. Testes unitários de todos os Handlers (Stack Obrigatória / "O que Não Fazer")

- [x] `CreateOrderCommandHandlerTests` ([tests/.../CreateOrder/CreateOrderCommandHandlerTests.cs](../tests/ECommerce.Application.Tests/Features/Orders/Commands/CreateOrder/CreateOrderCommandHandlerTests.cs))
  - [x] happy path: pedido é persistido via `IOrderRepository.AddAsync`
  - [x] mapeamento `Order` → `CreateOrderResult` correto (`Id`, `CustomerId`, `Status`, `CreatedAt`, `TotalAmount`)
  - [x] `CancellationToken` recebido é propagado para `AddAsync`
- [x] `GetOrdersQueryValidatorTests` — achado durante a TASK 10, fora do escopo original desta lista, mas mesmo tipo de gap (validator sem teste dedicado) ([tests/.../GetOrders/GetOrdersQueryValidatorTests.cs](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryValidatorTests.cs))

## 4. Desejáveis — não eliminatórios

- [x] Logging com Serilog (TASK 14)
  - [x] `UseSerilog()` configurado em `Program.cs`, lendo de `appsettings.json` (`Serilog.AspNetCore`/`Serilog.Sinks.Console`/`Serilog.Settings.Configuration`)
  - [x] `LoggingBehavior` no pipeline do MediatR, registrando:
    - [x] Command/Query recebido (`Handling {RequestName}`)
    - [x] Tempo de execução (`Handled {RequestName} in {ms} ms`)
    - [x] Falhas, classificadas por severidade (`Warning` para `ValidationException`/`OrderCannotBeCancelledException`, `Error` para o resto)
  - [x] Garantido que nunca loga senha, signing key ou JWT completo — `Login` nem passa pelo MediatR/`LoggingBehavior`; requests nunca são serializados inteiros (`{@Request}` nunca usado); varredura de hardening confirmou zero ocorrências de `Password`/`AccessToken`/`Authorization`/`Jwt:Key` em qualquer log
- [x] OpenTelemetry (TASK 15)
  - [x] instrumentação ASP.NET Core (traces via `AddAspNetCoreInstrumentation`, um span por request HTTP)
  - [x] instrumentação de Commands/Queries (`TracingBehavior` + `ActivitySource` próprio do Application, span aninhado sob o span HTTP)
  - [x] métricas ASP.NET Core + runtime .NET (`AddRuntimeInstrumentation`), sem métricas de negócio arbitrárias
  - [x] exportação para console, configurável via `OpenTelemetry:ConsoleExporterEnabled`; OTLP preparado (`OpenTelemetry:Otlp`) mas desligado por padrão — nenhum collector é obrigatório para rodar/testar
  - [x] correlação log↔trace via `TraceId`/`SpanId` (`ActivityEnricher`, lendo `Activity.Current`, sem pacote de correlação extra)
- [x] SonarQube / `dotnet-sonarscanner` (TASK 16) — infraestrutura pronta, análise real pendente de conta externa
  - [x] `coverlet.collector` + [`tests/coverlet.runsettings`](../tests/coverlet.runsettings) gerando cobertura em formato `opencover`
  - [x] workflow [`quality.yml`](../.github/workflows/quality.yml) (`build → test com coverage → sonar begin/end`), condicionado à existência do secret `SONAR_TOKEN` — build e testes rodam sempre, mesmo sem Sonar configurado
  - [x] exclusões configuradas (Migrations/gerado fora da análise; Migrations/`Program.cs`/DTOs simples fora da cobrança de cobertura, sem excluir Handlers/Domain/Behaviors)
  - [x] token nunca versionado — só `${{ secrets.SONAR_TOKEN }}` no workflow
  - [ ] **análise real (Quality Gate de verdade)** — exige conectar uma conta SonarCloud própria (fora do que esta sessão consegue provisionar); enquanto isso, auditoria manual completa cobrindo as mesmas categorias em [`docs/sonar-audit.md`](sonar-audit.md) — 0 bugs, 0 vulnerabilidades, 4 hotspots revisados como seguros, 7 code smells corrigidos

---

## Outros achados (fora do escopo direto das INSTRUÇÕES, mas relevantes)

- [x] `GET /api/orders?page=abc` (valor não numérico) retornava `500` em vez de `400`. Corrigido: `GlobalExceptionHandler` agora trata `BadHttpRequestException` (lançada pelo binding do Minimal API) e mapeia para `400`, com `title: "Invalid request"` e `detail` explicando o parâmetro que falhou. Teste de regressão em [`GetOrdersIntegrationTests.cs`](../tests/ECommerce.IntegrationTests/GetOrdersIntegrationTests.cs) — necessário como integração porque o erro ocorre no binding, antes do MediatR/`ValidationBehavior` rodarem, então nenhum teste de Handler/Validator conseguiria pegar essa regressão.
