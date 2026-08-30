# ECommerce — Sistema de Gestão de Pedidos

Backend API para um e-commerce simples, implementado em .NET 10 com Clean Architecture, CQRS + MediatR e Minimal API.

## Arquitetura

O projeto segue **Clean Architecture** com as seguintes camadas:

```
src/
  Domain/              — Entidades, enums, invariantes (zero dependências)
  Application/         — Commands, Queries, Handlers, DTOs, Validators
  Infrastructure/      — Persistência, JWT, implementações concretas
  Api/                 — DI, autenticação, endpoints (Minimal API)

tests/
  ECommerce.Tests/     — Testes unitários e integração
```

### Dependências entre camadas

```
Domain ← Application
Domain ← Infrastructure
Application + Infrastructure ← Api
```

**Nenhuma dependência invertida.**

## Stack Técnico

- **.NET 10**
- **Minimal API** — endpoints sem Controllers, reduzindo código cerimonial
- **CQRS + MediatR** — separação clara entre Commands e Queries
- **FluentValidation** — validação de entrada via Pipeline Behavior
- **Entity Framework Core 9.0** — persistência (SQLite)
- **OpenAPI nativo + Swagger UI** — contrato gerado pelo ASP.NET Core com interface interativa em Development
- **xUnit** — testes unitários
- **Central Package Management (CPM)** — versionamento centralizado

## Configuração Inicial

### Pré-requisitos

- .NET 10 SDK
- Visual Studio 2022 ou VS Code

### Restaurar e compilar

```bash
dotnet restore
dotnet build
```

### Executar testes

```bash
dotnet test
```

## Decisões Técnicas

### Por que Minimal API?

Minimal APIs foram escolhidas devido ao pequeno número de endpoints — cinco no escopo atual — e à simplicidade do serviço. Controllers seriam igualmente válidos, mas adicionariam estrutura sem benefício relevante neste cenário. As responsabilidades de negócio permanecem isoladas na camada Application por meio de CQRS/MediatR; os endpoints atuam somente como adaptadores entre HTTP e os casos de uso.

### Por que separar endpoints e contratos do `Program.cs`?

O `Program.cs` permanece como composition root da aplicação. Endpoints são organizados por recurso e seus contratos HTTP ficam separados dos Commands e DTOs da Application. Rotas de pedidos compartilham o prefixo `/api/orders` por meio de `MapGroup`, centralizando apenas configuração comum ao recurso. Essa organização evita o crescimento do arquivo de inicialização sem introduzir um framework próprio ou uma abstração genérica de endpoints e mantém explícita a fronteira entre API e Application.

### Por que manter o `Program.cs` mínimo?

O startup declara apenas a composição necessária para o escopo já implementado: Application, Infrastructure, tratamento global de erros, migrations, documentação da API e endpoints. O suporte e a configuração de JWT são adicionados conforme os requisitos de autenticação; CORS permanece sem configuração antecipada. Isso mantém o composition root legível e evita dependências e configurações sem uso concreto.

### Por que utilizar o OpenAPI nativo?

O contrato continua sendo gerado pelo suporte oficial do ASP.NET Core por meio de `AddOpenApi` e `MapOpenApi`. Em Development, o documento fica disponível em `/openapi/v1.json` e o Swagger UI em `/swagger`. Apenas o pacote de interface do Swashbuckle é utilizado, apontando para o documento nativo; assim evitamos manter dois geradores OpenAPI concorrentes. Fora de Development, documento e interface não são publicados.

### Por que separar Request HTTP e Command?

Os requests da API representam contratos externos sujeitos à evolução do protocolo HTTP, enquanto os Commands representam intenções e casos de uso da Application. Mesmo quando possuem os mesmos campos, tipos distintos evitam expor MediatR como contrato público e permitem que API e Application evoluam sem acoplamento desnecessário.

### Por que utilizar um Response HTTP dedicado?

A API não expõe entidades do Domain nem retorna diretamente resultados internos da Application. Um response próprio mantém o contrato HTTP estável e permite representar `OrderStatus` como texto legível sem alterar a persistência, que continua usando o mapeamento inteiro padrão do EF Core.

### Por que utilizar Problem Details?

Erros HTTP utilizam o suporte nativo do ASP.NET Core a Problem Details, seguindo o formato padronizado `application/problem+json`. Isso oferece respostas interoperáveis sem criar envelopes genéricos como `ApiResponse<T>` ou `ErrorResponse<T>`. O tratamento global das exceções será responsável por traduzir cada categoria de falha para o status HTTP apropriado.

### Por que centralizar o tratamento de exceções?

A API utiliza o mecanismo nativo `IExceptionHandler` do ASP.NET Core. Exceções atravessam um único ponto de tradução para Problem Details, são registradas com o contexto da requisição e recebem um `traceId` para correlação. Falhas do FluentValidation produzem HTTP 400 com mensagens agrupadas pelo caminho da propriedade; falhas inesperadas não expõem detalhes internos e produzem HTTP 500. Os endpoints permanecem focados na adaptação HTTP, sem blocos `try/catch` repetidos.

### Por que CQRS?

Separação clara entre operações de leitura (Queries) e escrita (Commands), facilitando testes, escalabilidade e manutenção.

### Por que FluentValidation via Pipeline Behavior?

Validação centralizada e reutilizável. Todos os Commands/Queries passam pelo mesmo pipeline, garantindo consistência.

### Por que Central Package Management?

Simplifica manutenção de versões em projetos multi-camadas. Uma única fonte de verdade em `Directory.Packages.props`.

### Por que não criar `PagedResult<T>`?

`GetOrdersResult` é um record concreto (`Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`), não uma abstração genérica de paginação. Hoje existe apenas um caso de listagem paginada no projeto; generalizar para `PagedResult<T>` antes de existir um segundo ou terceiro consumidor seria design especulativo — a abstração certa só fica clara depois de ver casos reais o suficiente para saber o que de fato varia entre eles. Se outra listagem paginada surgir, essa decisão é reavaliada então, com exemplos concretos guiando a forma da abstração em vez de suposição antecipada.

### Por que não criar `IUnitOfWork`?

`IOrderRepository` continua a única abstração de persistência — `AddAsync`/`UpdateAsync` chamam `SaveChangesAsync` diretamente, sem um `IUnitOfWork`/`ITransactionManager` por cima. Cada caso de uso implementado até agora (criar, cancelar) altera um único aggregate dentro do mesmo `DbContext` por requisição; um Unit of Work explícito só se justificaria coordenando múltiplos repositórios numa mesma transação, cenário que ainda não existe aqui. Introduzir essa camada agora seria abstrair uma necessidade hipotética, não uma real.

### Por que um teste de integração HTTP real para confirmar a persistência do cancelamento?

Os testes de Handler (Moq) provam que `UpdateAsync` foi chamado, mas não provam que a alteração sobrevive além de uma única requisição — é só a mesma instância de `DbContext`/mock em memória. Para confirmar de fato que `POST → GET → PATCH cancel → GET` reflete `Cancelled` numa leitura *separada*, foi adicionado `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`), hospedando a API real em processo contra um arquivo SQLite isolado por execução de teste (criado em `Path.GetTempPath()`, apagado no `Dispose`). `Program.cs` ganhou `public partial class Program;` no final — necessário porque top-level statements geram uma classe `Program` `internal` por padrão, e o `WebApplicationFactory<Program>` do projeto de testes precisa enxergá-la. Isso não é uma abstração nova nem generaliza nada; é o único jeito de testar "a mudança foi persistida de verdade" sem depender de inspecionar mocks.

### Por que não tratar concorrência no cancelamento?

`OrderRepository.UpdateAsync` não usa `rowversion`/concurrency token, nem trata `DbUpdateConcurrencyException`, nem há lock distribuído ou retry em volta de `GetByIdForUpdateAsync`/`Cancel()`/`UpdateAsync`. O teste não exige concorrência (múltiplos clientes cancelando o mesmo pedido simultaneamente), e adicionar esse controle agora seria complexidade sem requisito por trás — um `DbUpdateConcurrencyException` nesse cenário hoje se traduziria no `500` genérico do `GlobalExceptionHandler`, o que é aceitável para o escopo atual. Se concorrência real for exigida depois, a mudança fica isolada em `OrderConfiguration` (coluna de token) e no `catch` de `UpdateAsync`, sem afetar Handler ou Domain.

### Por que Queries retornam `null` em vez de lançar exceção para registro inexistente?

`GetOrderByIdQueryHandler` retorna `GetOrderByIdResult?` e devolve `null` quando o pedido não existe, sem `OrderNotFoundException` nem qualquer outro tipo de exceção. Não encontrar um registro é um resultado normal de uma consulta, não uma falha excepcional — criar uma exceção só para isso adicionaria uma camada de tratamento (captura no `IExceptionHandler`, mapeamento para status HTTP) para expressar algo que um retorno nullable já expressa com mais clareza e menos custo. A tradução `null → 404 Not Found` é responsabilidade do endpoint HTTP, mantendo a Application indiferente a códigos de status, do mesmo jeito que já é indiferente a JWT.

### Por que `OrderCannotBeCancelledException` vira 409 e não 400?

`400 Bad Request` indicaria problema no formato ou nos dados da requisição em si. Não é o caso aqui: o request é válido, o pedido existe, mas o estado atual dele (`Cancelled`/`Confirmed`) é incompatível com a operação pedida — request válido + recurso existente + estado incompatível é exatamente a definição de conflito, não de entrada malformada. `409 Conflict` comunica isso de forma mais expressiva ao cliente da API do que um `400` genérico, que ficaria ambíguo entre "você mandou algo errado" e "o recurso não está no estado certo".

### Por que validar Issuer, Audience e assinatura no JWT?

O `TokenValidationParameters` habilita explicitamente `ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey` e `ValidateLifetime`. Confiar apenas na assinatura não seria suficiente: sem validar issuer/audience, um token assinado pela própria aplicação mas emitido com outro propósito ainda seria aceito. Os valores de comparação (`Issuer`, `Audience`, `Key`) vêm de `JwtOptions`, vinculado à seção `Jwt` da configuração, evitando strings mágicas espalhadas pelo código.

### Por que ClockSkew = TimeSpan.Zero?

Por padrão, o `JwtBearerHandler` aplica uma tolerância de 5 minutos na validação de expiração, aceitando tokens já expirados dentro dessa janela. Para um teste técnico, essa tolerância reduz a previsibilidade dos testes de expiração. Zerar o `ClockSkew` faz o token expirar exatamente em `ExpirationMinutes`, sem margem adicional — não é uma exigência de segurança, mas é uma decisão simples e fácil de justificar.

### Por que a ordem UseAuthentication → UseAuthorization?

`UseAuthentication` identifica quem é o chamador, populando `HttpContext.User` a partir do token JWT; `UseAuthorization` decide se esse chamador pode acessar o recurso. A segunda depende do resultado da primeira, então a ordem inversa faria toda decisão de autorização cair sempre no caminho de "não autenticado". `UseExceptionHandler` é registrado antes de ambos para que falhas ao longo de todo o pipeline, inclusive de autenticação/autorização, sejam traduzidas para Problem Details.

### Por que Bearer no OpenAPI via Document/Operation Transformer em vez de trocar de biblioteca?

O contrato OpenAPI continua gerado pelo suporte nativo do ASP.NET Core (`AddOpenApi`), sem introduzir Swashbuckle.AspNetCore.SwaggerGen só para ganhar suporte a esquemas de segurança — o Swashbuckle usado permanece exclusivamente a interface (`SwaggerUI`), que renderiza qualquer documento OpenAPI válido, incluindo o gerado nativamente. `BearerSecuritySchemeTransformer` (`IOpenApiDocumentTransformer`) registra o esquema `Bearer` em `components.securitySchemes`; `BearerSecurityRequirementOperationTransformer` (`IOpenApiOperationTransformer`) adiciona o requisito de segurança apenas às operações cujo endpoint tem `IAuthorizeData` nos metadados — ou seja, só `/api/orders`, não `/auth/login`. Isso evita marcar todos os endpoints com o cadeado do Swagger indiscriminadamente e mantém a documentação sincronizada automaticamente com `.RequireAuthorization()`: se um novo endpoint protegido for adicionado ao grupo, o cadeado aparece sem precisar tocar nesses transformers.

### Por que validar `JwtOptions` no startup?

`Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience` binding para `string` sempre resultam em um valor não nulo (`string.Empty` quando ausentes), então `jwtSection.Get<JwtOptions>() ?? throw ...` sozinho não pega o caso de configuração ausente ou em branco — a aplicação subiria normalmente com uma chave de assinatura vazia. `JwtOptions` usa `DataAnnotations` (`[Required(AllowEmptyStrings = false)]` nos três campos, `[Range(1, int.MaxValue)]` em `ExpirationMinutes`) e `Program.cs` valida o objeto com `Validator.TryValidateObject` logo após o bind, antes de qualquer registro de serviço que dependa desses valores. Preferi validação manual simples a `AddOptions<T>().ValidateOnStart()` porque o mesmo `jwtOptions` já é extraído manualmente da configuração para montar o `TokenValidationParameters` antes de `builder.Build()` — validar esse objeto diretamente cobre também o `IOptions<JwtOptions>` injetado no `JwtTokenService`, já que os dois vêm da mesma leitura da seção `Jwt`, sem precisar de um segundo mecanismo de validação nem do pacote `Microsoft.Extensions.Options.DataAnnotations`.

### Por que login não passa pelo MediatR/Application?

`LoginEndpoint` valida a credencial fixa e emite o token chamando `ITokenService` diretamente, sem `LoginCommand`. Autenticação é uma preocupação do boundary HTTP, não uma regra de negócio do domínio de pedidos: não há entidade, invariante ou persistência envolvida, só validar credencial e gerar um JWT. Criar um Command só para isso adicionaria uma camada de indireção sem trazer nenhum dos benefícios de CQRS. Como consequência, a camada Application nunca importa nada relacionado a JWT — nem para validar tokens (isso é 100% ASP.NET Core/`JwtBearerHandler`), nem para emiti-los.

## Mudanças Realizadas (Etapa A — Esqueleto Funcional)

### ✅ Atualização para .NET 10

- `Directory.Build.props` configurado com `TargetFramework` net10.0
- Todos os projetos (.csproj) atualizados

### ✅ Central Package Management (CPM)

- Criado `Directory.Packages.props` com versionamento centralizado
- Dependências obrigatórias:
  - **MediatR** 12.4.1
  - **FluentValidation** 11.10.0
  - **Entity Framework Core** 9.0.0
  - **Microsoft.EntityFrameworkCore.Sqlite** 9.0.0
  - **Microsoft.EntityFrameworkCore.Design** 9.0.0

### ✅ Configuração de Program.cs

- Composition root limitado às camadas, erros globais, migrations e endpoints
- Sem Controllers ou configuração antecipada de CORS
- OpenAPI registrado com o suporte nativo do ASP.NET Core e disponibilizado pelo Swagger UI
- MediatR e FluentValidation registrados pela extensão da Application

### ✅ Limpeza de estrutura

- Removidos `.gitkeep` de todas as camadas
- Estrutura de pastas mantida vazia mas pronta para implementação

### ✅ Compilação validada

```
dotnet build → SUCESSO
Warnings: 8 (vulnerabilidade transitiva SQLite, XML comments menores)
Erros: 0
```

## Próximos Passos (Etapa B — Implementação de Negócio)

1. **Implementar Domínio**
   - Entidades: `Order`, `OrderItem`
   - Enums: `OrderStatus`
   - Invariantes e comportamento

2. **Implementar Application**
   - Commands: `CreateOrderCommand`, `CancelOrderCommand`
   - Queries: `GetOrderByIdQuery`, `GetOrdersQuery`
   - Handlers e Validators
   - DTOs

3. **Implementar Infrastructure**
   - `OrderRepository` com EF Core
   - `JwtTokenService`
   - DbContext e migrations

4. **Implementar Endpoints (Minimal API)**
   - `POST /auth/login`
   - `POST /api/orders`
   - `GET /api/orders?page=1&pageSize=10`
   - `GET /api/orders/{id}`
   - `PATCH /api/orders/{id}/cancel`

5. **Testes**
   - Testes unitários de Handlers
   - Testes de Domínio
   - Testes de integração com `WebApplicationFactory`

6. **Infraestrutura Final**
   - Migrations automáticas no startup
   - Docker + docker-compose
   - SonarQube
   - Serilog + OpenTelemetry

## Validação de Arquitetura

✅ Estrutura de Clean Architecture respeitada  
✅ Nenhuma dependência invertida  
✅ .NET 10 configurado  
✅ MediatR e FluentValidation prontos  
✅ Minimal API estruturada  
✅ CPM funcional  
✅ Build sem erros críticos  

## Notas

- A chave JWT presente em `appsettings.json` é apenas um placeholder de desenvolvimento e não representa um segredo real
- Em produção, `Jwt:Key` deve ser fornecida por variável de ambiente (por exemplo, `Jwt__Key`) ou por um secret manager, sem versionar a chave ou tokens gerados
- SQLite será embedded; sem container de banco separado
- Migrations serão aplicadas automaticamente no startup (Etapa B)
- Serilog e OpenTelemetry serão adicionados na Etapa B conforme necessário
