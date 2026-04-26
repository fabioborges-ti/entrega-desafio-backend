# Secrets and Environment Configuration

[Back to main README](./README.md)

Este documento descreve como configurar dados sensíveis para execução local com Docker Compose sem expor segredos em `appsettings`.

## Objetivo

- Manter `appsettings.json` e `appsettings.Development.json` sem dados sensíveis.
- Injetar segredos via variáveis de ambiente.
- Usar `.env` apenas para desenvolvimento local.

## Estratégia adotada

- Segredos removidos de arquivos versionados.
- `docker-compose.yml` configurado para ler `.env` e mapear variáveis para a aplicação.
- Arquivo `.env.example` fornecido como modelo.
- `.env` ignorado no git (`.gitignore`).

## Variáveis esperadas

Banco de dados:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`

RabbitMQ:

- `RABBITMQ_USER`
- `RABBITMQ_PASSWORD`

Aplicação:

- `JWT_SECRET_KEY`

## Como executar localmente com Compose

> O `.env.example` já contém valores de demonstração funcionais.
> Não é necessário editar nada para rodar localmente.

**Linux/macOS**
```bash
cp .env.example .env
docker compose up --build
```

**Windows (PowerShell)**
```powershell
copy .env.example .env
docker compose up --build
```

## Observações para produção/homologação

- Não usar `.env` em ambientes produtivos.
- Migrar segredos para Secret Manager/plataforma (ex.: AWS Secrets Manager, Azure Key Vault, etc.).
- Rotacionar segredos periodicamente e limitar acesso por ambiente.
