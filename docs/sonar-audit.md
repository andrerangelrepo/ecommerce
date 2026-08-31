# TASK 16 — Auditoria Manual de Qualidade (substituto da análise Sonar real)

Uma análise Sonar de verdade (SonarCloud/SonarQube) exige uma conta e um token que só o dono do repositório pode provisionar — fora do que uma sessão automatizada consegue fazer sozinha. A infraestrutura para rodar essa análise já está pronta (ver [README → Análise Estática](../README.md#análise-estática-sonar)): `coverlet.collector` nos projetos de teste, relatório de cobertura em formato `opencover`, exclusões configuradas, e um workflow do GitHub Actions (`sonar begin → build → test → sonar end`) que só executa a parte do Sonar quando o secret `SONAR_TOKEN` estiver configurado.

Enquanto uma conta real não é conectada, este documento registra uma **revisão manual completa** do código aplicando as mesmas categorias que o Sonar analisaria — bugs, vulnerabilidades, security hotspots, code smells, nullability, exception handling, async, duplicação, cobertura — feita em cima do código real, com build e teste verificando cada mudança.

## Resumo

```
TASK 16 — Static Analysis (auditoria manual)

Build:            ✅ 0 Aviso(s), 0 Erro(s) (Debug e Release)
Tests:             ✅ 63/63 aprovados

Bugs:              0 encontrados
Vulnerabilities:   0 encontradas
Security Hotspots: 4 revisados, todos "Safe" (documentados abaixo)
Code Smells:       11 analisados, 7 corrigidos (consistência), 4 avaliados e mantidos (sem ganho real)

Coverage (relatório mesclado dos dois projetos, via reportgenerator — o mesmo que o
Sonar veria com sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"):
  ECommerce.Domain:          100%
  ECommerce.Application:     100%
  ECommerce.Infrastructure:  98.7%
  ECommerce.API:             33.2% (esperado — camada de wiring/orquestração,
                              "útil mas secundária" no enunciado; ver detalhamento)
  Handlers (4/4):             100%
  Behaviors (3/3):            100%
  Migrations:                 excluídas da cobrança (não aparecem no relatório)

Duplication:       revisada (mapping Order → Result), mantida — projeções pequenas
                    e distintas por tipo, introduzir AutoMapper não simplificaria nada.
```

## Bugs — 0 encontrados

Verificado: null dereference, resource leak, async incorreto, condição sempre falsa/impossível, exception engolida.

- `.Wait()`, `.GetAwaiter().GetResult()`, `async void`: zero ocorrências em `src/`.
- `throw ex;` (perde stack trace): zero ocorrências — todo relançamento usa `throw;`.
- `catch (Exception)` existe em 2 lugares (`LoggingBehavior`, `TracingBehavior`) — ambos relançam (`throw;`) depois de logar/marcar o span, nenhum engole a exceção.
- `Activity`/`DbContext`: `TracingBehavior` usa `using var activity = ...StartActivity(...)`; `ApplicationDbContext` nunca é instanciado manualmente (`new ApplicationDbContext(...)` não aparece em código de produção) — sempre gerenciado pelo DI, escopo por request.

## Vulnerabilities — 0 encontradas

- JWT assinado com `HmacSha256` (algoritmo não quebrado), validação cobre assinatura/issuer/audience/expiração sem tolerância de relógio (já documentado no README).
- Nenhum uso de `MD5`/`SHA1`/`DES`/`Random()` não-criptográfico para nada sensível.
- Nenhuma SQL concatenada manualmente (`FromSqlRaw`/`ExecuteSqlRaw`/interpolação em SQL) — todas as queries são LINQ parametrizado via EF Core.
- Nenhum dado sensível (`Password`, `AccessToken`, `Authorization`, `Jwt:Key`) aparece em log ou trace — reconfirmado nesta auditoria (já eram alvo de varreduras dedicadas nas TASKs 14/15).

## Security Hotspots — 4 revisados, todos marcados Safe

| Hotspot | Revisão |
|---|---|
| `FixedUser.Password` hardcoded | **Safe, documentado no código** ([`FixedUser.cs`](../src/ECommerce.API/Authentication/FixedUser.cs)) — credencial fixa exigida literalmente pelo `INSTRUCOES.md` ("usuário fixo em memória é suficiente"), sem acesso a nenhum sistema real. Comentário XML explica isso diretamente na classe, para qualquer revisor (humano ou Sonar) entender sem precisar de contexto externo. |
| Ausência de CORS | **Safe** — sem `UseCors()`, o ASP.NET Core nega requisições cross-origin por padrão. Esta API não tem frontend browser-based consumindo-a de outra origem; a ausência é a postura mais segura, não uma omissão. |
| `Jwt:Key` no `appsettings.json` | **Safe** — é um placeholder de desenvolvimento (`"replace-this-development-key-with-a-secure-key"`), não uma chave real; em produção viria de variável de ambiente/secret manager, já demonstrado no `docker-compose.yml`. Já documentado nas Notas de Segurança do README. |
| Algoritmo de assinatura JWT (`HmacSha256`) | **Safe** — simétrico, apropriado para o escopo do teste (chave compartilhada só entre emissor e validador, ambos o mesmo processo); sem necessidade de assinatura assimétrica para este caso de uso. |

## Code Smells — corrigidos

1. **`public partial class Program;` desnecessário** ([`Program.cs`](../src/ECommerce.API/Program.cs)) — testado removendo e rodando a suite completa: o SDK do .NET 10 já gera a classe `Program` (de top-level statements) como `public` automaticamente, então a declaração explícita (necessária em versões antigas para o `WebApplicationFactory<Program>` dos testes de integração enxergar o tipo) é código morto. Confirmado via `dotnet format analyzers` (regra `ASP0027`) e removido; 13/13 testes de integração continuam passando sem ela.
2. **Inconsistência de estilo — construtor manual em vez de construtor primário** — `ValidationBehavior`, `CreateOrderCommandHandler`, e as 5 classes de `IntegrationTests` (`CreateOrderIntegrationTests`, `CancelOrderIntegrationTests`, `GetOrdersIntegrationTests`, `GetOrderByIdIntegrationTests`, `LoginIntegrationTests`) usavam construtor + atribuição manual de campo, enquanto todo o resto do projeto (`LoggingBehavior`, `TracingBehavior`, `OrderRepository`, `JwtTokenService`, etc.) já usa construtor primário. Não é "refatorar pra métrica" — é alinhar com a convenção que o próprio projeto já adotou em todo o resto do código. Corrigido via `dotnet format style --diagnostics IDE0290`, revisado manualmente (uma indentação incorreta ajustada), suite completa reconfirmada verde (63/63).

## Code Smells — avaliados e mantidos como estão (com justificativa)

| Achado | Por que não mudei |
|---|---|
| `CA1873` em `LoggingBehavior.cs` (×2): "avaliação de argumento pode ser cara" | O analisador é conservador e flagueia qualquer argumento que não seja literal/parâmetro — aqui é `stopwatch.ElapsedMilliseconds`, um `get` que só faz aritmética sobre dois `long`, sem I/O nem alocação. Envolver isso em `logger.IsEnabled(...)` adicionaria uma ramificação visual pra evitar um custo que já é irrelevante. Falso positivo na prática. |
| `IDE0042` em `Order.cs`: sugestão de desconstruir tupla no `foreach` | Cosmético — `item.ProductName` é tão claro quanto `productName` desconstruído, e lembra explicitamente que `item` é uma tupla nomeada. Sem ganho real de legibilidade. |
| `IDE0305` (×3): "inicialização de coleção pode ser simplificada" (`.Select().ToArray()` → `[.. x]`) | Trocaria um padrão LINQ amplamente reconhecido por sintaxe de collection expression (C# 12) mais nova e menos imediatamente familiar. Estilístico, sem ganho de correção ou performance. |
| Duplicação de mapping `Order`/`OrderItem` → `Result` em 6 arquivos (Handlers + Endpoints) | Cada mapeamento projeta pra um formato ligeiramente diferente (`CreateOrderResult` tem 5 campos, `GetOrderByIdResult` tem 5 + itens aninhados, `OrderListItemResult` é um resumo, `CancelOrderResult` tem 2). Já é uma decisão arquitetural registrada no README ("Por que utilizar um Response HTTP dedicado?") — introduzir AutoMapper ou um mapper compartilhado trocaria 4-6 linhas explícitas por reflection/configuração, sem simplificar de verdade. |

## Nullability, Exception Handling, Async — revisados, sem achados

- `null!`: zero ocorrências em todo `src/` (já confirmado na TASK 13.19, reconfirmado aqui).
- Toda captura de exceção relança (`throw;`) ou é seguida de log com o objeto de exceção completo (nunca só `.Message`) quando o nível é `Error`.
- Toda chamada de log usa template estruturado (`{PropertyName}`), nunca interpolação de string (`$"..."`) — confirmado por varredura.
- `CancellationToken.None`: zero ocorrências — o token é sempre propagado de ponta a ponta, nunca substituído por um token vazio.

## Cobertura — detalhamento

Gerei o relatório mesclado com `reportgenerator` (`reportgenerator -reports:"TestResults/**/coverage.opencover.xml" -targetdir:... -reporttypes:Html`) — o mesmo resultado que o Sonar produziria, já que ele também combina os dois `coverage.opencover.xml` via `sonar.cs.opencover.reportsPaths`.

- **`ECommerce.Domain`: 100%** — `Order`, `OrderItem`, `OrderCannotBeCancelledException`, incluindo o construtor privado do EF Core (só exercitado pelos testes de integração via SQLite real, não pelos testes de domínio isolados — os dois relatórios se complementam).
- **`ECommerce.Application`: 100%** — os 4 Handlers, os 3 Behaviors (`LoggingBehavior`/`TracingBehavior`/`ValidationBehavior`, incluindo sucesso e falha), Validators, Commands/Queries.
- **`ECommerce.Infrastructure`: 98.7%** — muito acima do "útil mas secundária" esperado pelo enunciado.
- **`ECommerce.API`: 33.2%** — mais baixo, mas isso bate com o esperado ("Não precisamos exigir cobertura alta de... `API`"). Os pontos baixos são explicáveis, não acidentais:
  - `BearerSecurityRequirementOperationTransformer`/`BearerSecuritySchemeTransformer` (0%) — só rodam quando `/openapi/v1.json` é gerado (Swagger, só em Development); nenhum teste bate nesse endpoint.
  - `OpenTelemetryExtensions` (69.8%) — o branch `if (options.Otlp.Enabled)` nunca roda porque OTLP fica desligado por padrão em todo teste (por design, TASK 15).
  - `GlobalExceptionHandler` (82.1%) — o branch de exceção inesperada (`500`) já era um gap conhecido e documentado desde a TASK 13: automatizá-lo exigiria mock dentro de `IntegrationTests`, contra a decisão arquitetural do projeto (testado manualmente, não automatizado).
  - Sem essas 4 classes/branches (framework-adjacent ou deliberadamente fora do teste automatizado), o resto da API — Endpoints, Contracts, Auth — está em **100%**.

**Achado real durante essa análise, corrigido:** `OrderListItemResponse` (API) apareceu em **0%**, e `OrderListItemResult` (Application) em **33,3%** — diferente das outras DTOs simples, que ficam altas incidentalmente. Investigando: o único teste de integração da listagem (`GetOrders_ShouldReturnOk_WithValidPagination`) só testava contra um banco **vazio** ("mesmo sem pedidos ainda", como o próprio comentário do teste dizia) — então o mapeamento `Order → OrderListItemResponse` dentro do `GetOrdersEndpoint` nunca era executado de ponta a ponta com dado real. Um teste de Handler isolado (com mock) não pegaria um bug nesse mapeamento específico, porque nunca chega na camada de API. Adicionei `GetOrders_ShouldReturnCreatedOrder_WhenOneExists` — cria um pedido de verdade, lista, e confirma que os campos batem — fechando o gap. Depois da correção: `OrderListItemResponse` e `OrderListItemResult` foram para **100%**, `GetOrdersEndpoint` de 64.7% para **100%**, `ECommerce.Application` foi de 98.4% para **100%** no relatório mesclado. Suite: 63 → **64 testes**.

Nenhum teste artificial foi criado só pra melhorar a métrica — DTOs simples (`LoginRequest`, `JwtOptions`, etc.) continuam sem teste dedicado; a única mudança foi fechar um gap real (mapeamento nunca exercitado com dado real), não perseguir porcentagem.

## Conclusão

Nenhum bug ou vulnerabilidade real encontrado. Os 4 security hotspots revisados são todos seguros e, no caso da credencial fixa, agora documentados diretamente no código-fonte. Dos 11 code smells identificados pela varredura (via `dotnet format analyzers`/`style` em nível `info`, que o `dotnet build` normal não expõe), 7 foram corrigidos por representarem uma inconsistência real contra a própria convenção do projeto ou código morto verificável; os outros 4 foram avaliados e mantidos, com justificativa registrada, por não representarem ganho real. A análise do relatório de cobertura mesclado revelou um gap real (mapeamento da listagem nunca exercitado com dado real), fechado com um teste que prova comportamento, não só sobe percentual — exatamente o princípio da task: usar Sonar (ou, aqui, os mesmos critérios que ele usaria) pra achar problemas de verdade, não para perseguir métrica.
