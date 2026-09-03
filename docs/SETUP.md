# QuantLab — Setup

## Prerequisites

- Git
- Python 3.11+
- .NET 8+ SDK
- Docker Desktop
- Node.js 20+ later for React

Verify:

```bash
git --version
python3 --version
dotnet --version
docker --version
```

## Python

```bash
python3 -m venv .venv
source .venv/bin/activate
python -m pip install --upgrade pip
pip install pandas numpy scipy pytest
```

## PostgreSQL

```bash
docker compose -f docker/docker-compose.yml up -d
docker compose -f docker/docker-compose.yml ps
```

Stop it with:

```bash
docker compose -f docker/docker-compose.yml down
```

## Environment

```bash
cp .env.example .env
```

Never commit `.env`.

## API

```bash
cd backend
dotnet restore
dotnet run
```

## Tests

```bash
pytest
dotnet test
```

## First milestone

```text
CSV → Python loader → Validation → PostgreSQL → .NET API → Response
```
