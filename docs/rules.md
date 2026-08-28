# Clean Architecture + CQRS + API Guidelines — .NET 10

## Architecture Principles

### Layer Dependencies
- **Domain Layer**: No dependencies on other layers (pure business logic)
- **Application Layer**: Depends only on Domain
- **Infrastructure Layer**: Implements Domain interfaces
- **API Layer**: Depends on Application, Infrastructure, and CrossCutting
- **CrossCutting Layer**: Can be used by any layer

### Constants Location Rule
- **ALL constants MUST be created in `CrossCutting/Constants/`**
- ❌ Do NOT create constants in Application, Domain, or Infrastructure layers
- ✅ Create specific constant classes per domain/feature in CrossCutting
- Example structure:
  ```
  CrossCutting/
  └── Constants/
      ├── NameClassConstants.cs
  ```

### CQRS Pattern
- **Commands**: Write operations (Create, Update, Delete)
- **Queries**: Read operations (Get, List)
- **MediatR**: Mediates between API and handlers
- **Handlers**: One handler per command/query

---

## Type Conventions

### Domain Entities vs DTOs
- **Domain Entities (Domain Layer)**: Use `class` keyword
  - Rich domain models with behavior and business logic
  - Private setters to enforce encapsulation
  - Example: `public class SinistroMestre { ... }`
  
- **DTOs (Application Layer)**: Use `record` keyword
  - Immutable data transfer objects
  - No business logic, only data structure
  - Example: `public record CadastrarAvisoRequestDto(...);`

```csharp
// ✅ Domain Entity - use class
public class Order
{
    public int Id { get; private set; }
    public decimal Total { get; private set; }
    
    public void CalculateTotal() { /* business logic */ }
}

// ✅ DTO - use record
public record OrderDto(int Id, decimal Total);
public record CreateOrderRequestDto(string CustomerName, decimal Amount);
```

---

## Naming Conventions

### Entities
- PascalCase: `Reclamante`, `Order`, `Protocol`
- Location: `Domain/Entities/`

### Repositories
- Interface: `I{Entity}Repository` → `Domain/Interfaces/`
- Implementation: `{Entity}Repository` → `Infrastructure/Repositories/`

### Commands
- Pattern: `{Action}{Entity}Command`
- Location: `Application/Features/{Entity}/Commands/{Action}{Entity}/`

### Queries
- Pattern: `Get{Entity}By{Criteria}Query`
- Location: `Application/Features/{Entity}/Queries/Get{Entity}By{Criteria}/`

### Handlers
- Pattern: `{Action}{Entity}Handler`
- Same folder as command/query

### Validators
- Pattern: `{Action}{Entity}Validator`
- Inherits: `AbstractValidator<{Command}>`
- Same folder as command

### DTOs
- Request: `{Action}{Entity}RequestDto`
- Response: `{Action}{Entity}ResponseDto`
- General: `{Entity}Dto`
- Location: `Application/Features/{Entity}/DTOs/`
- JSON fields: **camelCase**, full words (no abbreviations)
- List fields: always **plural**, never use "lista" or "array" as suffix

### Mappings
- Class: `{Entity}Mappings` (static extension methods)
- Location: `Application/Features/{Entity}/Mappings/`

### Endpoints
- Class: `{Entity}Endpoints` (static, extension method `Map{Entity}EndpointsV{Version}`)
- Location: `Api/Endpoints/V{Version}/`

### Gateways (External HTTP Clients)
- Interface: `I{Nome}Gateway` (ex.: `IApoliceGateway`, `IAgenciaGateway`)
- Implementation: `{Nome}Client` (ex.: `ApoliceClient`, `AgenciaClient`)
- Interface location: `Application/Gateways/`
- Implementation location: `Infrastructure/Gateways/`
- One gateway per API/resource — do NOT mix different resources in a single client
- Always use `_basePath` (injected via configuration) to build URLs — never hardcode full paths
- Return `Result<T>` from `Csh.Shared.Common` for operations that can fail

| Role           | Name pattern       | Example                       |
|----------------|--------------------|-------------------------------|
| Interface      | `I{Nome}Gateway`   | `IApoliceGateway`             |
| Implementation | `{Nome}Client`     | `ApoliceClient`               |
| Location       | `Gateways/`        | `Gateways/IApoliceGateway.cs` |

### Async Methods
- **All async methods MUST have the `Async` suffix**
- Applies to: repositories, handlers, gateways, services, and any other async method

✅ Correct:
```csharp
Task<{Entity}?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
Task<Result<int>> GetRamoEmissorAsync(string numApolice, CancellationToken cancellationToken = default);
Task<PagedResult<{Entity}Dto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
```

❌ Wrong:
```csharp
Task<{Entity}?> GetById(int id);
Task<Result<int>> GetRamoEmissor(string numApolice);
```

---

## API URL Structure (PRIMARY RULE)

### Pattern
```
/{capability}/{recurso}/v{n}
```

- `{capability}`: business domain group — always **plural** (ex.: `apolices`, `clientes`)
- `{recurso}`: resource name — always **plural** (ex.: `contratos`, `perfis`)
- `v{n}`: version at the **end** of the path

✅ Correct:
```
/apolices/contratos/v1
/clientes/perfis/v1
/sinistros/registros/v1
```

❌ Anti-patterns:
```
/api/v1/apolices/contratos    ← version prefix at the start (wrong)
/apolices/contratos           ← missing version
/apolices/contrato/v1         ← recurso no singular
/criarCliente                 ← verbs in path
/spring/apolices              ← technology exposure
/api-java/clientes            ← technology exposure
```

### Naming
- Path segments: **kebab-case**
- Query parameters: **camelCase**
- Resource IDs: **always in the path**, never in query string

✅ Examples:
```
/imoveis/avaliacoes-imovel/v1
/apolices/contratos/v1/{contratoId}/sinistros
?page=1&pageSize=20
```

### Resource Design
- Routes represent **nouns (resources)**, not actions
- Action is expressed via HTTP method
- Resource segments are always **plural**

✅:
```
GET    /clientes/perfis/v1
POST   /sinistros/registros/v1
PUT    /apolices/contratos/v1/{contratoId}
DELETE /pagamentos/transacoes/v1/{transacaoId}
```

❌:
```
/obterCliente
/criarContrato
/deletarSinistro
/apolices/contrato/v1     ← singular
```

### Hierarchy
- Maximum **3 levels** deep (before `/v{n}`)
- Represent direct relationships only

✅:
```
/apolices/contratos/v1/{contratoId}/sinistros
```

❌:
```
/clientes/{id}/contratos/{contratoId}/sinistros/{sinistroId}/documentos/v1
```

### Capability Groups
| Capability  | Resources                    |
|-------------|------------------------------|
| clientes    | perfis, enderecos            |
| apolices    | contratos, coberturas        |
| sinistros   | casos, registros             |
| pagamentos  | transacoes                   |

---

## HTTP Status Codes

| Status | Usage                        |
|--------|------------------------------|
| 200    | Success with response body   |
| 201    | Resource created (POST)      |
| 204    | Success without body         |
| 400    | Validation / bad request     |
| 401    | Not authenticated            |
| 403    | Forbidden / no permission    |
| 404    | Resource not found           |
| 500    | Internal server error        |

---

## API Response Pattern

### ⚠️ MANDATORY: All Endpoints MUST Use ApiResponse<T>

**EVERY endpoint response MUST be wrapped in `ApiResponse<T>` from `Csh.Shared.Common`**

- ✅ All success responses use `ApiResponse<T>.Ok()` or `ApiResponse<T>.Created()`
- ✅ All error responses use `ApiResponse<T>.Fail()`
- ❌ NEVER return raw DTOs or plain objects directly
- ❌ NEVER return `Results.Ok(dto)` without ApiResponse wrapper

### Success
```csharp
// ✅ Correct - wrapped in ApiResponse
return Results.Ok(ApiResponse<TDto>.Ok(data, "Success message"));

// ❌ Wrong - raw DTO
return Results.Ok(dto);
```

### Created
```csharp
// ✅ Correct - wrapped in ApiResponse
// URL pattern: /{capability}/{recurso}/v{n}/{id}
return Results.Created(
    $"/apolices/contratos/v1/{contratoId}",
    ApiResponse<TDto>.Created(data, "Created message")
);

// ❌ Wrong - raw DTO
return Results.Created($"/apolices/contratos/v1/{contratoId}", dto);
```

### Error (structured JSON)
```json
{
  "erro": "RequisicaoInvalida",
  "mensagem": "Descrição do problema ocorrido."
}
```
```csharp
// ✅ Correct - wrapped in ApiResponse
return Results.BadRequest(
    ApiResponse<TDto>.Fail("Error message", errors)
);

// ❌ Wrong - raw error object
return Results.BadRequest(new { error = "message" });
```

### Mapping Result<T> to HTTP status (endpoints)
Use `result.Error.Code` prefix to determine the correct HTTP status:

```csharp
if (result.IsFailure)
{
    return result.Error.Code.StartsWith("NOT_FOUND")
        ? Results.NotFound(ApiResponse<TDto>.Fail(result.Error))
        : result.Error.Code.StartsWith("CONFLICT")
            ? Results.Conflict(ApiResponse<TDto>.Fail(result.Error))
            : Results.UnprocessableEntity(ApiResponse<TDto>.Fail(result.Error));
}
```

| `Error.Code` prefix | HTTP Status              |
|---------------------|--------------------------|
| `NOT_FOUND.*`       | 404 Not Found            |
| `CONFLICT.*`        | 409 Conflict             |
| `VALIDATION.*`      | 400 Bad Request          |
| `UNAUTHORIZED.*`    | 401 Unauthorized         |
| `INTERNAL.*`        | 500 (via middleware)     |
| other               | 422 Unprocessable Entity |

> ✅ Do NOT use `try/catch` in endpoints — unhandled exceptions are caught by `ExceptionHandlingMiddleware` from `Csh.Shared`.

---

## Pagination

### When to consider pagination

> **⚠️ ANALYSIS REQUIRED:** Before implementing a list endpoint, the developer must evaluate whether pagination is appropriate for the context. The decision should be based on the criteria below.

**Consider paginating when:**
- The data volume can grow indefinitely (e.g.: transaction history, audit records)
- The list may return hundreds or thousands of records
- The API consumer needs to navigate through pages (e.g.: tables in UI screens)

**Pagination may be omitted when:**
- The dataset is small and fixed (e.g.: status list, document types)
- The list is always returned in full by business rule
- The endpoint is internal and the volume is controlled

**If you choose to paginate**, use the standard parameters:
- Query parameters: `pageNumber` and `pageSize`
- Return `PagedResult<T>` from `Csh.Shared.Common`

### Query parameter pattern
```
?pageNumber=1&pageSize=20
```

### Return type
When pagination is adopted, the endpoint **MUST** return `PagedResult<T>` from `Csh.Shared.Common` — do NOT create a local pagination class.

```csharp
// PagedResult<T> is provided by Csh.Shared.Common
// Properties available:
// - Items: IEnumerable<T>      → current page items
// - PageNumber: int            → current page (1-based)
// - PageSize: int              → items per page
// - TotalCount: int            → total items (unpaged)
// - TotalPages: int            → computed: ceil(TotalCount / PageSize)
// - HasPreviousPage: bool      → PageNumber > 1
// - HasNextPage: bool          → PageNumber < TotalPages
```

### Endpoint pattern

```csharp
group.MapGet("/", GetAllAsync)
    .WithSummary("Listar {entities}")
    .WithDescription("Retorna lista paginada de {entities}")
    .Produces<ApiResponse<PagedResult<{Entity}Dto>>>(StatusCodes.Status200OK);

private static async Task<IResult> GetAllAsync(
    int pageNumber = 1,
    int pageSize = 20,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var query = new GetAll{Entities}Query(pageNumber, pageSize);
    var result = await mediator.Send(query, cancellationToken);

    if (result.IsFailure)
        return Results.UnprocessableEntity(ApiResponse<PagedResult<{Entity}Dto>>.Fail(result.Error));

    return Results.Ok(ApiResponse<PagedResult<{Entity}Dto>>.Ok(result.Value));
}
```

### Repository pattern for pagination

```csharp
// Interface
Task<PagedResult<{Entity}>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

// Implementation with Dapper
public async Task<PagedResult<{Entity}>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
{
    var offset = (pageNumber - 1) * pageSize;

    const string countSql = "SELECT COUNT(*) FROM SCHEMA.TABLE WITH (NOLOCK)";
    const string dataSql  = @"
        SELECT * FROM SCHEMA.TABLE WITH (NOLOCK)
        ORDER BY ID
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    using var conn = _dapperContext.CreateConnection();
    var total = await conn.ExecuteScalarAsync<int>(countSql);
    var items = await conn.QueryAsync<{Entity}>(dataSql, new { Offset = offset, PageSize = pageSize });

    return new PagedResult<{Entity}>(items, total, pageNumber, pageSize);
}
```

---

## Endpoint Implementation Rules

### API Versioning — Centralized Setup (Program.cs)

Versioning is configured once in `Program.cs` using `ApiVersionSet`. The version segment is at the **end** of the path, after `/{capability}/{recurso}`:

```csharp
// Program.cs — configure once
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

// Root group carries /{capability}/{recurso} prefix; version appended per endpoint group
var apiVersioned = app.MapGroup("")
    .WithApiVersionSet(versionSet);

// Register endpoint groups
apiVersioned.Map{Entity}EndpointsV1();
apiVersioned.Map{Entity}EndpointsV2();
```

### MapGroup — capability, recurso and version

Each endpoint class appends `/{capability}/{recurso}/v{n}` — all segments following the rules:

```csharp
// ✅ Correct — final URL: /{capability}/{recurso}/v{n}
public static IEndpointRouteBuilder Map{Entity}EndpointsV1(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/{capability}/{recurso}/v1")  // plural segments, version at end
        .WithTags("{Entity} V1")
        .MapToApiVersion(1, 0);
    ...
}

// ❌ Wrong
var group = app.MapGroup("/api/v1/{recurso}")   // version at start, missing capability
var group = app.MapGroup("/{recurso}/v1")       // missing capability
var group = app.MapGroup("/{capability}/{recurso}") // missing version
```

### All endpoint handler methods must be async with the Async suffix

```csharp
// ✅ Correct
group.MapGet("/", GetAll{Entities}Async);
group.MapGet("/{id:int}", Get{Entity}ByIdAsync);
group.MapPost("/", Create{Entity}Async);

private static async Task<IResult> GetAll{Entities}Async(...) { }
private static async Task<IResult> Get{Entity}ByIdAsync(...) { }
private static async Task<IResult> Create{Entity}Async(...) { }

// ❌ Wrong — no Async suffix
group.MapGet("/", Get);
group.MapPost("/", Create);
```

### Full endpoint template
```csharp
public static class {Entity}Endpoints
{
    public static IEndpointRouteBuilder Map{Entity}EndpointsV1(this IEndpointRouteBuilder app)
    {
        // ✅ /{capability}/{recurso}/v{n} — both plural, version at end
        var group = app.MapGroup("/{capability}/{recurso}/v1")
            .WithTags("{Entity} V1")
            .MapToApiVersion(1, 0);

        group.MapGet("/", GetAll{Entities}Async)
            .WithName("GetAll{Entities}V1")
            .WithSummary("Listar {entities}")
            .Produces<ApiResponse<PagedResult<{Entity}Dto>>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", Get{Entity}ByIdAsync)
            .WithName("Get{Entity}ByIdV1")
            .WithSummary("Buscar {entity} por ID")
            .Produces<ApiResponse<{Entity}Dto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create{Entity}Async)
            .WithName("Create{Entity}V1")
            .WithSummary("Criar {entity}")
            .Produces<ApiResponse<Create{Entity}ResponseDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:int}", Update{Entity}Async)
            .WithName("Update{Entity}V1")
            .WithSummary("Atualizar {entity}")
            .Produces<ApiResponse<Update{Entity}ResponseDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete{Entity}Async)
            .WithName("Delete{Entity}V1")
            .WithSummary("Remover {entity}")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetAll{Entities}Async(
        int pageNumber = 1,
        int pageSize = 20,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetAll{Entities}Query(pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
            return Results.UnprocessableEntity(ApiResponse<PagedResult<{Entity}Dto>>.Fail(result.Error));

        return Results.Ok(ApiResponse<PagedResult<{Entity}Dto>>.Ok(result.Value));
    }

    private static async Task<IResult> Get{Entity}ByIdAsync(
        int id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new Get{Entity}ByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
            return Results.NotFound(ApiResponse<{Entity}Dto>.Fail(result.Error));

        return Results.Ok(ApiResponse<{Entity}Dto>.Ok(result.Value));
    }

    private static async Task<IResult> Create{Entity}Async(
        Create{Entity}RequestDto request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new Create{Entity}Command(request.Name, request.CpfCnpj);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code.StartsWith("NOT_FOUND")
                ? Results.NotFound(ApiResponse<Create{Entity}ResponseDto>.Fail(result.Error))
                : result.Error.Code.StartsWith("CONFLICT")
                    ? Results.Conflict(ApiResponse<Create{Entity}ResponseDto>.Fail(result.Error))
                    : result.Error.Code.StartsWith("VALIDATION")
                        ? Results.BadRequest(ApiResponse<Create{Entity}ResponseDto>.Fail(result.Error))
                        : Results.UnprocessableEntity(ApiResponse<Create{Entity}ResponseDto>.Fail(result.Error));
        }

        // ✅ URL: /{capability}/{recurso}/v{n}/{id}
        return Results.Created(
            $"/{capability}/{recurso}/v1/{result.Value.Id}",
            ApiResponse<Create{Entity}ResponseDto>.Created(result.Value, "{Entity} criado com sucesso"));
    }
}
```

---

## Database Access Rules

### Use EF Core for:
- Write operations (INSERT, UPDATE, DELETE)
- Simple queries with navigation properties
- Change tracking and transaction management

### Use Dapper for:
- Complex read queries with JOINs
- Aggregation queries (MAX, COUNT, SUM)
- Performance-critical reads and raw SQL
- Paginated queries

### Existing Tables
- ❌ NEVER create migrations for existing tables
- ✅ Use EF Core Fluent API: `ToTable("TableName", "SchemaName")`
- ✅ Map all columns explicitly with `HasColumnName()`
- ✅ Respect existing primary keys (including composite keys)

---

## Error Handling Patterns

### Domain Validation
```csharp
// ✅ Domain validation errors (in entity's Validate() method)
throw new InvalidOperationException("O nome da cidade não pode ser vazio");
// Automatically handled by ExceptionHandlingMiddleware → HTTP 422 Unprocessable Entity

// ✅ Entity not found
throw new KeyNotFoundException($"{Entity} {id} não encontrado");
// Automatically handled by ExceptionHandlingMiddleware → HTTP 404 Not Found

// ✅ Business rule violations (in handlers)
return Result.Failure("FORECAST_ALREADY_EXISTS", "Já existe previsão...");
// Handler returns Result.Failure, endpoint maps to appropriate HTTP status

// ✅ FluentValidation errors
// Handled automatically by ValidationBehavior → HTTP 400 Bad Request
```

**Key Points:**
- Domain validation (entity rules) → throw `InvalidOperationException` → HTTP 422
- Business rules (handler logic) → return `Result.Failure()` → HTTP status based on error code
- Entity not found → throw `KeyNotFoundException` → HTTP 404
- FluentValidation → automatic → HTTP 400

Global handling via `ExceptionHandlingMiddleware` from `Csh.Shared` (`UseSharedMiddlewares()`).

---

## Validation Order

1. **Domain validations** (entity's `Validate()` method) — throws `InvalidOperationException` → HTTP 422
   - Field presence (required fields)
   - Format validation (length, range, pattern)
   - Type-specific validation (dates, temperatures, etc.)

2. **Business rules** (handler logic) — returns `Result.Failure()` → mapped to HTTP status
   - State validation
   - Workflow rules
   - Conditional logic

3. **External dependencies** — returns `Result.Failure()` if validation fails
   - Protocol/Key existence in external systems
   - Related entity validation

4. **Duplicate/existence checks** — returns `Result.Failure()` with appropriate error code
   - Uniqueness constraints
   - Conflict detection

---

## Transaction Management

```csharp
// Standard
await _unitOfWork.Repositories.AddAsync(entity);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Explicit transaction
await _unitOfWork.BeginTransactionAsync();
try
{
    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

---

## Dependency Injection

```csharp
// Repositories
services.AddScoped<I{Entity}Repository, {Entity}Repository>();

// Gateways — interface: I{Nome}Gateway, implementation: {Nome}Client
services.AddHttpClient<I{Nome}Gateway, {Nome}Client>(client =>
{
    client.BaseAddress = new Uri(configuration["ExternalServices:{Nome}:BaseUrl"]
        ?? throw new InvalidOperationException("BaseUrl não configurada"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

// MediatR
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyMarker).Assembly)
);

// FluentValidation
services.AddValidatorsFromAssembly(typeof(Application.AssemblyMarker).Assembly);

// Shared lib (ApiKey, middlewares, etc.)
services.AddSharedServices(configuration);
```

---

## Configuration Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "ExternalServices": {
    "{ServiceName}": {
      "BaseUrl": "http://service:port",
      "Timeout": 30
    }
  }
}
```

---

## Legacy System Migration Rules

- Preserve exact error messages (traceability)
- Maintain original field names and formats
- Preserve audit fields: `NOM_PROGRAMA`, `COD_USUARIO`, `DTH_CADASTRAMENTO`

### DB2 → SQL Server Translation
| DB2                       | SQL Server               |
|---------------------------|--------------------------|
| `CURRENT TIMESTAMP`       | `GETDATE()`              |
| `VALUE(MAX(col), 0)`      | `ISNULL(MAX(col), 0)`    |
| `WITH UR`                 | `WITH (NOLOCK)`          |
| `FETCH FIRST n ROWS ONLY` | `TOP n`                  |

---

## Microservices Communication

- Use `HttpClient` typed clients (`I{Nome}Gateway` / `{Nome}Client`)
- Configure base URL in `appsettings.json`
- Always use `_basePath` to build URLs — never hardcode full paths
- No authentication (handled by API Gateway)
- Implement retry policies for resilience
- Prefer service calls over direct cross-service DB access

---

## Code Quality Standards

- **SOLID** principles enforced
- Functions < 20 lines
- No magic numbers
- DRY and KISS
- XML comments on all public members
- All async methods must have the `Async` suffix
- Swagger: `.WithSummary()`, `.WithDescription()`, all status codes documented

### Test Coverage Exclusions
- **Non-service classes in Application layer** (e.g., DTOs, mappings, validators) should be excluded from test coverage
- Add `[ExcludeFromCodeCoverage]` attribute to classes that don't require unit tests:
  - DTOs (records)
  - Mapping extension classes
  - Simple validators without complex logic

```csharp
using System.Diagnostics.CodeAnalysis;

// ✅ Exclude DTOs from coverage
[ExcludeFromCodeCoverage]
public record CreateOrderRequestDto(string CustomerName, decimal Amount);

// ✅ Exclude mapping classes from coverage
[ExcludeFromCodeCoverage]
public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new(order.Id, order.Total);
}
```

### SonarQube Quality Gate
- **Avoid false positives** in SonarQube analysis by:
  - Properly excluding non-testable code with `[ExcludeFromCodeCoverage]`
  - Reviewing and addressing legitimate code smells
  - Using `#pragma warning disable` only when justified with comments
  - Ensuring all business logic has proper test coverage

### Date Handling
- **Always use standardized date formatting library** for DATE type data
- Use `Csh.Shared.Common` date utilities for consistent formatting across services
- Never use hardcoded date format strings
- Example:
```csharp
// ✅ Correct - use shared library
using Csh.Shared.Common.Extensions;
var formattedDate = dateValue.ToStandardFormat();

// ❌ Wrong - hardcoded format
var formattedDate = dateValue.ToString("yyyy-MM-dd");
```

---

## API Quality Checklist

- [ ] URL follows `/{capability}/{recurso}/v{n}` pattern
- [ ] Both `{capability}` and `{recurso}` segments are **plural**
- [ ] Version `v{n}` is at the **end** of the path
- [ ] `MapGroup` uses `/{capability}/{recurso}/v{n}` pattern
- [ ] Path in kebab-case, query params in camelCase
- [ ] Resources are plural nouns (no verbs)
- [ ] No technology exposed in URL
- [ ] Hierarchy ≤ 3 levels (before `/v{n}`)
- [ ] IDs in path (not query string)
- [ ] Correct HTTP status codes used
- [ ] **ALL endpoints return `ApiResponse<T>` wrapper** (MANDATORY)
- [ ] Error response follows `{ "erro", "mensagem" }` structure
- [ ] **[ANALYSIS]** List endpoints: evaluated whether pagination is needed for the context
- [ ] If paginated: uses `?pageNumber=1&pageSize=20` and returns `PagedResult<T>` from `Csh.Shared.Common`
- [ ] If not paginated: justification documented (fixed volume, domain data, etc.)
- [ ] All async methods have `Async` suffix
- [ ] All endpoint handler methods are async with `Async` suffix
- [ ] JSON fields in camelCase, no abbreviations
- [ ] OpenAPI / Swagger published

## Implementation Checklist

- [ ] **ALL constants created in `CrossCutting/Constants/`** (MANDATORY)
- [ ] Domain entity with all fields (Rich Domain Model — no public setters)
- [ ] Repository interface (Domain) + implementation (Infrastructure)
- [ ] EF Core configuration (map to existing table if applicable)
- [ ] ApplicationDbContext DbSet updated
- [ ] UnitOfWork updated
- [ ] Gateways defined (`I{Nome}Gateway` in Application, `{Nome}Client` in Infrastructure)
- [ ] DTOs (Request, Response, General)
- [ ] Mapping extensions
- [ ] Validators for all commands (use `CpfCnpjValidator` from `Csh.Shared.Validators`)
- [ ] Command handlers (return `Result<T>` from `Csh.Shared.Common`)
- [ ] Query handlers returning lists: evaluated whether to use `Result<PagedResult<T>>` (paginated) or `Result<IEnumerable<T>>` (non-paginated)
- [ ] **API endpoints return `ApiResponse<T>` wrapper** (MANDATORY - all responses)
- [ ] API endpoints (plural resources, version at end, `Async` suffix on handlers)
- [ ] Dependency injection registration
- [ ] `AddSharedServices()` and `UseSharedMiddlewares()` configured
- [ ] Swagger documentation
- [ ] Unit tests (validators + handlers)
- [ ] Integration tests (endpoints)
- [ ] No local reimplementation of `ApiResponse<T>`, `Result<T>`, `PagedResult<T>`, `CpfCnpjValidator`