param(
    [string]$OutputDir = "images/diagrams",
    [string]$BaseName = "fluxo-worker-inventario"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $repoRoot $OutputDir
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$diagramPath = Join-Path $targetDir ("{0}.mmd" -f $BaseName)
$svgPath = Join-Path $targetDir ("{0}.svg" -f $BaseName)
$pngPath = Join-Path $targetDir ("{0}.png" -f $BaseName)

$diagram = @"
flowchart LR
    A[StockAlertHostedService inicia] --> B[Espera 30s apos start da API]
    B --> C[Executa verificacao periodica]
    C --> D[Consulta inventario abaixo do minimo]
    D --> E{Ha produtos criticos?}
    E -- Nao --> F[Registra log e aguarda proximo ciclo]
    E -- Sim --> G[Monta email consolidado]
    G --> H[Envia alerta ao administrador]
    H --> I[Registra sucesso ou falha no envio]
    F --> C
    I --> C
"@

Set-Content -Path $diagramPath -Value $diagram -Encoding UTF8

Write-Host "Arquivo Mermaid gerado:"
Write-Host " - $diagramPath"

if (Get-Command npx -ErrorAction SilentlyContinue) {
    npx -y @mermaid-js/mermaid-cli -i $diagramPath -o $svgPath -b transparent
    npx -y @mermaid-js/mermaid-cli -i $diagramPath -o $pngPath -w 2200 -H 1400 -b white

    Write-Host "Arquivos de imagem gerados:"
    Write-Host " - $svgPath"
    Write-Host " - $pngPath"
}
else {
    Write-Warning "npx nao encontrado. Apenas o .mmd foi gerado."
    Write-Host "Para exportar SVG/PNG, instale Node.js e execute novamente."
}
