<#
.SYNOPSIS
    Remove o banco PostgreSQL da aplicação e reaplica todas as migrations (recriação total).

.DESCRIPTION
    1) dotnet ef database drop --force  -> apaga o banco inteiro (inclui todas as tabelas e __EFMigrationsHistory).
    2) dotnet ef database update       -> recria o banco (se o servidor permitir) e aplica todas as migrations do zero.

    Defina a connection string como no desenvolvimento habitual, por exemplo:
      $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=...;Username=...;Password=..."

    Encerre a API e outros clientes conectados ao mesmo banco antes de executar (o drop pode falhar com sessões ativas).

.EXAMPLE
    cd template\backend
    .\scripts\Reset-Database.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$backendRoot = Split-Path -Parent $PSScriptRoot
Set-Location $backendRoot

$efArgs = @(
    '--project', 'src\Ambev.DeveloperEvaluation.ORM\Ambev.DeveloperEvaluation.ORM.csproj',
    '--startup-project', 'src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj',
    '--context', 'DefaultContext'
)

Write-Host ''
Write-Host 'Recriacao total do banco (drop + migrations).' -ForegroundColor Cyan
Write-Host "Diretorio: $backendRoot" -ForegroundColor DarkGray
Write-Host ''

Write-Host '[1/2] dotnet ef database drop --force ...' -ForegroundColor Yellow
& dotnet ef database drop --force @efArgs
if ($LASTEXITCODE -ne 0) {
    throw 'Falha no drop. Verifique a connection string, permissões e se não há conexões abertas ao banco.'
}

Write-Host ''
Write-Host '[2/2] dotnet ef database update ...' -ForegroundColor Green
& dotnet ef database update @efArgs
if ($LASTEXITCODE -ne 0) {
    throw 'Falha ao aplicar migrations. Se o banco não existir após o drop, crie-o no PostgreSQL e execute novamente (somente o passo update).'
}

Write-Host ''
Write-Host 'Concluido. Tabelas e historico de migrations foram recriados a partir do projeto.' -ForegroundColor Green
Write-Host 'Suba a WebApi para seeds em startup (admin ApiSecrets, customers/branches se vazio, catalogo em Development).' -ForegroundColor DarkGray
Write-Host ''
