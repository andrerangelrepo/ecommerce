TASK 16 — Análise Estática e Qualidade com SonarQube / SonarCloud
Objetivo

Adicionar análise estática de código ao projeto para identificar:

bugs potenciais;
code smells;
vulnerabilidades;
duplicações;
problemas de manutenção;
cobertura de testes;
violações de qualidade antes da entrega.

A ideia é usar Sonar como uma camada adicional de qualidade, sem transformar o projeto em um pipeline DevOps complexo.

Ao final queremos algo assim:

Código
  ↓
Build
  ↓
Tests
  ↓
Coverage
  ↓
Sonar Analysis
  ↓
Quality Gate
Princípio da TASK

Sonar deve ajudar a identificar problemas reais.

Ele não deve virar motivo para:

refatorar código bom apenas para satisfazer métrica;
criar abstrações artificiais;
buscar 100% de cobertura;
ignorar conscientemente regras importantes;
encher o projeto de suppressions.

A prioridade continua sendo:

arquitetura simples, comportamento correto e código legível.

SUBTASK 16.1 — Escolher SonarQube ou SonarCloud

Para o teste técnico, existem duas alternativas válidas.

Opção A — SonarCloud

Mais simples caso o repositório esteja hospedado em:

GitHub
Azure DevOps
Bitbucket

Vantagens:

sem infraestrutura local;
dashboard pronto;
integração simples com CI;
bom para demonstrar qualidade do repositório.
Opção B — SonarQube Local

Útil caso não queiramos depender de serviço externo.

Pode rodar via Docker.

Porém isso adiciona mais infraestrutura.

Para este teste, eu priorizaria:

SonarCloud

se houver repositório público ou integração simples.

Caso contrário:

SonarQube local opcional
SUBTASK 16.2 — Não Tornar Sonar Obrigatório Para Rodar a Aplicação

A aplicação deve continuar funcionando normalmente com:

dotnet run

e:

docker compose up

sem Sonar.

Sonar é ferramenta de análise, não dependência de runtime.

SUBTASK 16.3 — Adicionar Scanner

Para .NET, utilizar:

dotnet-sonarscanner

Instalação local/global conforme estratégia escolhida.

Exemplo:

dotnet tool install --global dotnet-sonarscanner

ou preferencialmente em CI.

Não adicionar scanner como package do projeto Domain/Application.

SUBTASK 16.4 — Fluxo de Análise

O fluxo esperado é:

sonarscanner begin
        ↓
dotnet build
        ↓
dotnet test + coverage
        ↓
sonarscanner end

A ordem é importante.

SUBTASK 16.5 — Gerar Cobertura de Testes

Adicionar suporte a cobertura no projeto de testes.

Podemos utilizar:

coverlet.collector

nos projetos de teste.

Isso permite:

dotnet test --collect:"XPlat Code Coverage"
SUBTASK 16.6 — Preferir Cobertura em Formato Compatível

Para Sonar, utilizar formato:

OpenCover

ou o formato compatível com a configuração adotada.

Exemplo conceitual:

dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

Se necessário, configurar:

Format=opencover

de acordo com a abordagem utilizada.

SUBTASK 16.7 — Não Buscar 100% de Coverage

A métrica de cobertura deve servir como indicador.

Não como objetivo absoluto.

Não queremos testes como:

"getter retorna valor"
"record foi construído"
"enum possui valor"

apenas para aumentar porcentagem.

Cobrir principalmente:

Domain
Handlers
Validators
Behaviors
fluxos HTTP importantes
SUBTASK 16.8 — Definir Escopo da Cobertura

Cobertura importante:

OrderManagement.Domain
OrderManagement.Application

Cobertura útil, mas secundária:

OrderManagement.Api
OrderManagement.Infrastructure

Não precisamos exigir cobertura alta de:

Migrations
Program.cs
DTOs simples
Configuration classes triviais
SUBTASK 16.9 — Excluir Migrations da Cobertura

Migrations são código gerado.

Não faz sentido exigir testes unitários delas.

Configurar exclusão para algo como:

**/Migrations/**
SUBTASK 16.10 — Excluir Código Gerado

Também excluir:

obj/**
bin/**
Generated/**

quando aplicável.

Nunca analisar artefatos compilados como código-fonte relevante.

SUBTASK 16.11 — Não Excluir Código Só Para Melhorar Métrica

Evitar exclusões como:

**/Handlers/**
**/Domain/**
**/Repositories/**

apenas para aumentar coverage.

Exclusão deve ter justificativa real.

SUBTASK 16.12 — Configurar Projeto no Sonar

Definir:

Project Key
Organization
Project Name

Exemplo conceitual:

order-management-api

Não hardcodar token Sonar no repositório.

SUBTASK 16.13 — Token via Variável de Ambiente

Utilizar:

SONAR_TOKEN

ou secret equivalente no CI.

Nunca:

sonar.login=meu-token-real

versionado.

SUBTASK 16.14 — Não Versionar Credencial Sonar

Pesquisar:

sonar.login
sonar.token
SONAR_TOKEN=

Nenhum segredo real deve aparecer no repositório.

SUBTASK 16.15 — Configurar Analysis Begin

Exemplo conceitual para SonarCloud:

dotnet sonarscanner begin \
  /k:"order-management-api" \
  /o:"organization" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

A sintaxe real deve refletir a ferramenta e ambiente escolhidos.

SUBTASK 16.16 — Executar Build Durante Análise

Depois do begin:

dotnet restore
dotnet build --no-restore

O build deve continuar verde.

Não esconder warnings apenas porque Sonar também os identifica.

SUBTASK 16.17 — Executar Testes Durante Análise

Executar:

dotnet test --no-build

com geração de coverage.

Resultado esperado:

Failed: 0
SUBTASK 16.18 — Finalizar Análise

Depois:

dotnet sonarscanner end \
  /d:sonar.token="$SONAR_TOKEN"

O scanner deve enviar:

análise;
issues;
coverage;
métricas.
SUBTASK 16.19 — Quality Gate

Verificar o resultado do Quality Gate.

Esperado idealmente:

PASSED

Não considerar toda regra Sonar como verdade absoluta.

Mas qualquer falha precisa ser analisada.

SUBTASK 16.20 — Categorizar Issues

Separar os achados em:

Bug
Vulnerability
Security Hotspot
Code Smell
Coverage
Duplication

A prioridade deve ser:

Bug/Vulnerability
        ↓
Security
        ↓
Code Smell relevante
        ↓
Coverage
        ↓
cosmética
SUBTASK 16.21 — Bugs Devem Ser Corrigidos

Issues classificados como bug devem ser analisados prioritariamente.

Exemplos:

null dereference
resource leak
async incorreto
condition sempre falsa
exception swallowed

Não marcar como false positive sem investigar.

SUBTASK 16.22 — Vulnerabilidades

Qualquer finding de segurança deve ser revisado cuidadosamente.

Especialmente:

JWT
secrets
logging
input validation
HTTP
SQL

Não ignorar vulnerabilidade apenas porque o projeto é teste técnico.

SUBTASK 16.23 — Security Hotspots

Security Hotspot não significa necessariamente vulnerabilidade.

Ele significa:

algo que merece revisão manual.

Exemplos possíveis:

JWT signing
cryptography
password handling
CORS

Revisar e marcar como:

Safe

somente se realmente estiver seguro.

SUBTASK 16.24 — Code Smells

Code smells devem ser avaliados com julgamento.

Exemplos válidos:

método muito complexo
duplicação
unused parameter
condição redundante
classe grande

Mas não refatorar automaticamente qualquer finding.

SUBTASK 16.25 — Cognitive Complexity

Se um método tiver complexidade alta, avaliar principalmente:

GlobalExceptionHandler
LoggingBehavior
TracingBehavior
DependencyInjection
Endpoint mapping

Se estiver fazendo responsabilidades demais, dividir.

Não extrair métodos artificiais só para reduzir número.

SUBTASK 16.26 — Duplicação

Revisar duplicações reais.

Exemplo:

mapping Order → Result

pode aparecer em mais de um lugar.

Antes de criar mapper compartilhado, perguntar:

A abstração resultante é realmente mais simples?

Duas pequenas projeções parecidas podem ser aceitáveis.

SUBTASK 16.27 — Não Adicionar AutoMapper Só Por Duplicação

Não introduzir:

AutoMapper

apenas porque Sonar identificou algumas linhas duplicadas.

Nesse projeto, mapping explícito continua sendo perfeitamente adequado.

SUBTASK 16.28 — Revisar Warnings de Nullability

Sonar pode encontrar situações não capturadas pelo compilador.

Revisar especialmente:

nullable reference
possible null
uninitialized property

Não usar:

!

apenas para silenciar.

SUBTASK 16.29 — Revisar Exception Handling

Procurar problemas como:

catch(Exception) sem tratamento
exception engolida
throw ex

Correto para relançar:

throw;

Evitar:

throw ex;

porque perde stack trace original.

SUBTASK 16.30 — Revisar async

Procurar:

async sem await
.Result
.Wait()
.GetAwaiter().GetResult()

na aplicação.

Resultado esperado:

zero bloqueios síncronos desnecessários
SUBTASK 16.31 — Procurar .Result

Pesquisar globalmente:

.Result

Analisar cada ocorrência.

No fluxo async principal, preferir:

await
SUBTASK 16.32 — Procurar .Wait()

Pesquisar:

.Wait()

Evitar em:

API
Application
Infrastructure
Tests

quando houver alternativa async.

SUBTASK 16.33 — Revisar Dispose

Verificar recursos que implementam:

IDisposable
IAsyncDisposable

Principalmente:

Activity
scope
DbContext scope

Garantir using/await using quando necessário.

SUBTASK 16.34 — Revisar ActivitySource

ActivitySource pode ser estático e de longa duração.

Não criar um novo:

ActivitySource

a cada request.

SUBTASK 16.35 — Revisar Logging

Sonar pode detectar interpolação desnecessária.

Preferir:

logger.LogInformation(
    "Handling {RequestName}",
    requestName);

em vez de:

logger.LogInformation(
    $"Handling {requestName}");
SUBTASK 16.36 — Revisar Logging de Exceptions

Evitar:

logger.LogError(exception.Message);

Preferir:

logger.LogError(
    exception,
    "Request failed");

Isso preserva contexto e stack trace.

SUBTASK 16.37 — Evitar Dados Sensíveis em Logs

Continuar garantindo que não sejam registrados:

Password
JWT
AccessToken
Jwt Key
Authorization header

Se Sonar apontar hotspot relacionado, revisar.

SUBTASK 16.38 — Revisar Hardcoded Secrets

Sonar normalmente detecta padrões suspeitos.

Diferenciar:

Aceitável

Credenciais explicitamente definidas pelo desafio:

dev@martech.com
Senha@123
Não aceitável
JWT signing key real
API key
Sonar token
Cloud secret
SUBTASK 16.39 — Documentar Credencial de Teste

Caso Sonar marque:

Senha@123

como senha hardcoded, não simplesmente suprimir globalmente.

Documentar que:

É uma credencial fixa exigida pelo desafio e usada somente para demonstração.

Se necessário, realizar suppressão extremamente localizada e justificada.

SUBTASK 16.40 — Evitar SuppressMessage Global

Não criar:

GlobalSuppressions.cs

com dezenas de regras desabilitadas.

Cada suppression precisa ter justificativa real.

SUBTASK 16.41 — Não Desativar Regra Globalmente

Evitar:

sonar.issue.ignore.allfile

ou desabilitar regras importantes só para ter dashboard verde.

SUBTASK 16.42 — Revisar Public API

Sonar pode recomendar documentation comments para tudo dependendo do profile.

Não precisamos adicionar XML docs em cada propriedade trivial.

A documentação deve ser útil, não cerimonial.

SUBTASK 16.43 — Revisar Usings

Remover:

unused using

e namespaces desnecessários.

Build + analyzer devem ajudar nisso.

SUBTASK 16.44 — Revisar Código Morto

Remover:

métodos nunca usados
classes abandonadas
fields não utilizados
variáveis inutilizadas

quando for seguro.

SUBTASK 16.45 — Revisar Condições Redundantes

Exemplos:

if (value != null && value != null)

ou fluxo impossível.

Não deixar warnings triviais na entrega.

SUBTASK 16.46 — Revisar Linq

Procurar oportunidades claras como:

Count() > 0

quando:

Any()

é mais expressivo/eficiente.

Mas não micro-otimizar toda consulta.

SUBTASK 16.47 — Não Prejudicar EF Core com Refactor Cego

Algumas regras Sonar sobre LINQ podem não considerar:

IQueryable

e tradução SQL.

Antes de alterar uma query EF Core, confirmar que:

SQL continua correto
paginação continua no banco
SUBTASK 16.48 — Revisar Strings de Rotas

Evitar inconsistências:

/api/orders
/api/order
/orders

sem motivo.

Não criar constantes para cada /.

SUBTASK 16.49 — Revisar Métodos Grandes

Verificar especialmente:

Program.cs
AddInfrastructure
AddAuthentication
AddObservability
GlobalExceptionHandler

Se algum estiver acumulando responsabilidades, dividir em extensões específicas.

SUBTASK 16.50 — Revisar Complexidade dos Behaviors

Esperado:

LoggingBehavior
TracingBehavior
ValidationBehavior

cada um com uma responsabilidade clara.

Não criar um único:

ApplicationBehavior

fazendo:

logging
validation
tracing
metrics
exception handling
SUBTASK 16.51 — Revisar Cobertura dos Handlers

Sonar deve mostrar cobertura para:

CreateOrderCommandHandler
GetOrderByIdQueryHandler
GetOrdersQueryHandler
CancelOrderCommandHandler

Esperado:

cobertura relevante

Nenhum Handler obrigatório deve estar sem teste.

SUBTASK 16.52 — Revisar Cobertura do Domain

Especialmente:

Order
OrderItem
Order.Cancel
TotalAmount
invariantes

Essas regras merecem cobertura alta.

SUBTASK 16.53 — Revisar Cobertura dos Behaviors

Confirmar cobertura de:

ValidationBehavior
LoggingBehavior
TracingBehavior

Ao menos:

success
failure

quando aplicável.

SUBTASK 16.54 — Não Exigir Cobertura de DTOs

Não escrever testes para:

LoginRequest
CreateOrderResponse
JwtOptions

apenas para aumentar porcentagem.

SUBTASK 16.55 — Definir Meta de Cobertura Razoável

Se for necessário definir uma meta, algo como:

80% em New Code

é razoável.

Mas não considero obrigatório fixar um percentual rígido para o projeto inteiro.

O mais importante é:

regras críticas cobertas
SUBTASK 16.56 — Preferir Quality Gate para New Code

Em projetos novos, uma boa estratégia é exigir maior qualidade em:

New Code

em vez de tentar otimizar toda métrica global.

Como este projeto é pequeno, ambos tendem a coincidir.

SUBTASK 16.57 — CI Opcional

Se já houver GitHub Actions ou outro CI, integrar a análise.

Fluxo:

checkout
 ↓
setup .NET 10
 ↓
sonar begin
 ↓
restore
 ↓
build
 ↓
test + coverage
 ↓
sonar end
SUBTASK 16.58 — Não Criar CI Gigante

Não precisamos adicionar:

matrix de sistemas operacionais
deploy
release
docker registry
security scanning completo

só para rodar Sonar.

Manter simples.

SUBTASK 16.59 — Validar PRs

Se houver CI, o ideal é que:

build
tests
sonar

sejam executados em Pull Requests.

Isso demonstra um bom fluxo de qualidade.

SUBTASK 16.60 — Não Armazenar Token no Workflow

Usar:

${{ secrets.SONAR_TOKEN }}

ou mecanismo equivalente da plataforma.

Nunca colocar valor diretamente no YAML.

SUBTASK 16.61 — SonarQube Local Opcional

Caso opte por SonarQube local, podemos usar Docker separadamente.

Exemplo conceitual:

docker compose -f sonar-compose.yml up

Mas eu evitaria misturar SonarQube com o Compose principal da aplicação.

O Compose principal deve continuar contendo somente o necessário para rodar o sistema.

SUBTASK 16.62 — Não Adicionar SonarQube ao Compose Principal

Evitar:

api
sonarqube
postgres-sonar

no mesmo docker-compose.yml.

Isso aumentaria demais a infraestrutura de execução do projeto.

SUBTASK 16.63 — Revisar Quality Gate Após Correções

Depois de tratar issues:

rodar análise novamente

Esperado:

Quality Gate Passed

ou, caso exista alguma exceção deliberada, documentá-la.

SUBTASK 16.64 — Gerar Relatório Resumido

Ao final, produzir algo como:

TASK 16 — Static Analysis

Quality Gate:
✅ Passed

Bugs:
0

Vulnerabilities:
0

Security Hotspots:
0 pendentes

Code Smells:
X analisados
Y corrigidos

Coverage:
XX%

Duplications:
X%

Build:
✅

Tests:
✅

Não precisa versionar esse relatório se não agregar valor.

SUBTASK 16.65 — Executar Hardening Após Sonar

Após qualquer correção:

dotnet clean
dotnet restore
dotnet build
dotnet test

Depois:

dotnet build -c Release

E se alterações afetarem runtime:

docker compose up --build
Estrutura Esperada

Pode ficar algo próximo de:

OrderManagement
│
├── src
├── tests
│
├── .github
│   └── workflows
│       └── quality.yml
│       └── opcional
│
├── README.md
├── Dockerfile / compose
└── ...

Não é necessário criar arquivos específicos do Sonar no repositório se toda configuração puder ficar no CI.

Exemplo de Pipeline Conceitual
Pull Request
     │
     ▼
dotnet restore
     │
     ▼
Sonar Begin
     │
     ▼
dotnet build
     │
     ▼
dotnet test
     │
     ├── unit
     ├── integration
     └── coverage
     │
     ▼
Sonar End
     │
     ▼
Quality Gate
Principais Regras de Qualidade

O resultado ideal é:

BUGS
0

VULNERABILITIES
0

SECURITY HOTSPOTS
todos revisados

BUILD WARNINGS
0

TEST FAILURES
0

CRITICAL CODE SMELLS
0

DUPLICAÇÃO
sem duplicação relevante

COVERAGE
regras críticas cobertas
Critérios de Aceite — TASK 16
CA	Critério
CA-16.1	Scanner Sonar configurado
CA-16.2	Sonar não é dependência de runtime
CA-16.3	Token Sonar não está versionado
CA-16.4	Build participa da análise
CA-16.5	Testes participam da análise
CA-16.6	Coverage é coletado
CA-16.7	Migrations são excluídas da cobertura
CA-16.8	Código gerado é excluído quando apropriado
CA-16.9	Nenhuma exclusão artificial para aumentar coverage
CA-16.10	Todos os Bugs encontrados foram analisados
CA-16.11	Zero Bug crítico pendente
CA-16.12	Todas as Vulnerabilities foram analisadas
CA-16.13	Zero Vulnerability crítica pendente
CA-16.14	Security Hotspots foram revisados
CA-16.15	Code Smells relevantes foram analisados
CA-16.16	Duplicações relevantes foram revisadas
CA-16.17	Todos os handlers continuam cobertos
CA-16.18	Regras principais do Domain continuam cobertas
CA-16.19	Behaviors possuem cobertura adequada
CA-16.20	DTOs triviais não receberam testes artificiais
CA-16.21	Zero suppression global injustificada
CA-16.22	Zero regra Sonar desativada apenas para obter verde
CA-16.23	Zero segredo real detectável no código
CA-16.24	Zero uso indevido de .Result/.Wait()
CA-16.25	Zero exception engolida
CA-16.26	Logging estruturado preservado
CA-16.27	Dados sensíveis não aparecem em logs
CA-16.28	OpenTelemetry permanece desacoplado do Domain
CA-16.29	Query EF continua paginando no banco
CA-16.30	dotnet build verde
CA-16.31	dotnet test verde
CA-16.32	Zero novo warning relevante
CA-16.33	Quality Gate aprovado ou qualquer exceção explicitamente justificada
Validação Final

Executar primeiro:

dotnet clean
dotnet restore
dotnet build
dotnet test

Depois rodar análise Sonar.

Fluxo conceitual:

dotnet sonarscanner begin ...
dotnet build
dotnet test ...
dotnet sonarscanner end ...

E verificar o dashboard.

Busca Manual Complementar

Mesmo com Sonar, pesquisar:

TODO
FIXME
HACK
NotImplementedException
throw new Exception
.Result
.Wait()
CancellationToken.None
Console.WriteLine
EnableSensitiveDataLogging
EnsureCreated
public set

Sonar não substitui revisão arquitetural.

O Que Não Fazer Nesta Task

Não adicionar:

StyleCop completo sem necessidade;
Roslyn analyzers em excesso;
ReSharper CLI;
CodeQL;
Snyk;
Dependabot customizado;
Trivy;
OWASP Dependency Check;
mutation testing;
benchmark pipeline;
100% coverage obrigatório;
suppressions em massa;
refatorações artificiais para agradar métricas.

O objetivo é qualidade real, não pontuação.

Resultado Esperado

Ao final da TASK 16 teremos uma camada adicional de garantia:

SOURCE CODE
     │
     ├── Compiler
     │
     ├── Unit Tests
     │
     ├── Integration Tests
     │
     ├── Coverage
     │
     └── Sonar Analysis
             │
             ▼
        Quality Gate

E o projeto passa a demonstrar não só:

"funciona"

mas também:

"é analisado, testado e possui critérios objetivos de qualidade"