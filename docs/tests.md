# 🧪 Resultados de Testes e Cobertura

> Voltar para o documento principal: [README.md](../README.md)

Este documento consolida a evidência da última execução de testes automatizados e os percentuais de cobertura coletados no repositório.

---

## ✅ Execução realizada

Data/hora da execução: **27/04/2026 11:26 (UTC-3)**  
Comando executado:

```bash
dotnet test Ambev.DeveloperEvaluation.sln --logger "console;verbosity=minimal"
```

### Resultado geral

- **Total de testes executados:** `510`
- **Aprovados:** `510`
- **Com falha:** `0`
- **Ignorados:** `0`

### Resultado por suíte

- **Functional:** `1/1` aprovado
- **Integration:** `4/4` aprovados
- **Unit:** `505/505` aprovados

### Destaque da validação

- O teste crítico assíncrono de criação de venda passou com sucesso:
  `AsyncCreateSaleApiIntegrationTests.PostSales_WithValidCart_ReturnsAcceptedAndConsumerPersistsSale`

---

## 📊 Cobertura de código (última coleta)

Data/hora da coleta: **27/04/2026 11:18 (UTC-3)**  
Comando executado:

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj --collect:"XPlat Code Coverage" --settings tests/Ambev.DeveloperEvaluation.Unit/coverlet.runsettings --logger "console;verbosity=minimal"
```

Arquivo de evidência:

- `tests/Ambev.DeveloperEvaluation.Unit/TestResults/a5be6bfe-8d2c-4ff9-8e1b-04e0b12676e1/coverage.cobertura.xml`

### Cobertura global (unit tests)

- **Line coverage:** `66.33%` (`861/1298`)
- **Branch coverage:** `56.19%` (`127/226`)

### Cobertura por assembly

- **Ambev.DeveloperEvaluation.Application** — line: `97.46%`, branch: `98.52%`
- **Ambev.DeveloperEvaluation.Common** — line: `97.34%`, branch: `84.21%`
- **Ambev.DeveloperEvaluation.Domain** — line: `99.54%`, branch: `96.77%`
- **Ambev.DeveloperEvaluation.WebApi** — line: `55.22%`, branch: `27.77%`

---

## 🔁 Como reproduzir

```bash
# Todos os testes
dotnet test Ambev.DeveloperEvaluation.sln --logger "console;verbosity=minimal"

# Somente unitários com cobertura
dotnet test tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj --collect:"XPlat Code Coverage" --settings tests/Ambev.DeveloperEvaluation.Unit/coverlet.runsettings --logger "console;verbosity=minimal"
```

---

## 🧭 Observações

- Todas as suítes passaram na execução mais recente.
- Estratégia de testes: priorizamos cobertura nas camadas `Domain` e `Application`, pois concentram regras de negócio, invariantes e fluxos críticos da solução.
- Essa priorização reduz risco de regressão funcional e aumenta confiabilidade do comportamento do sistema; a cobertura de borda (`WebApi` e `Common`) evolui de forma incremental.
- Cobertura de domínio e aplicação segue alta; houve avanço consistente em `Common` e `WebApi` após novos testes focados.
- Recomenda-se manter este documento sincronizado a cada alteração relevante de regras de negócio ou infraestrutura.
