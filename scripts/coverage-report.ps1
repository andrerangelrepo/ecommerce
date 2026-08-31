#requires -version 5.1
<#
.SYNOPSIS
    Roda a suite de testes coletando cobertura e abre o relatório HTML.
.DESCRIPTION
    Mesmo comando serve pra primeira execução (instala o dotnet-reportgenerator-globaltool
    se ele ainda não existir) e pra todas as seguintes (pula a instalação automaticamente).
.PARAMETER SkipTests
    Pula o "dotnet test" e só regenera o HTML a partir da última cobertura coletada.
    Útil se você só quer reabrir o relatório sem rodar os testes de novo.
.EXAMPLE
    ./scripts/coverage-report.ps1
.EXAMPLE
    ./scripts/coverage-report.ps1 -SkipTests
#>
param(
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

# Sempre roda a partir da raiz do repositório, não importa de onde o script foi chamado.
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $toolInstalled = dotnet tool list --global | Select-String "dotnet-reportgenerator-globaltool"
    if (-not $toolInstalled) {
        Write-Host "==> Instalando dotnet-reportgenerator-globaltool (só acontece na primeira vez)..." -ForegroundColor Cyan
        dotnet tool install --global dotnet-reportgenerator-globaltool
    }

    if (-not $SkipTests) {
        if (Test-Path "TestResults") {
            Remove-Item -Recurse -Force "TestResults"
        }

        Write-Host "==> Rodando testes com coleta de cobertura..." -ForegroundColor Cyan
        dotnet test --settings tests/coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./TestResults
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test falhou (exit code $LASTEXITCODE) — corrija os testes antes de gerar o relatório."
        }
    }

    Write-Host "==> Gerando relatório HTML..." -ForegroundColor Cyan
    reportgenerator `
        -reports:"TestResults/**/coverage.opencover.xml" `
        -targetdir:"TestResults/CoverageReport" `
        -reporttypes:Html

    $reportPath = Join-Path $repoRoot "TestResults/CoverageReport/index.html"
    Write-Host "==> Abrindo $reportPath" -ForegroundColor Green
    Invoke-Item $reportPath
}
finally {
    Pop-Location
}
