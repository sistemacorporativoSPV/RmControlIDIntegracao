# 📋 Documentação Técnica - API_RMxControliD

## 1. 💻 Linguagens de Programação

- **Backend**: C# (.NET 8.0)
- **Frontend**: Não aplicável (API REST)

---

## 2. 🛠️ Frameworks e Bibliotecas

### 2.1 Principais
- **ASP.NET Core 8.0** (Web API)
- **Swashbuckle.AspNetCore** (Swagger / OpenAPI)
- **NLog** (Logging)
- **HttpClient / IHttpClientFactory**

### 2.2 Observações
- Projeto focado em **integração entre sistemas**
- Não utiliza ORM ou acesso direto a banco de dados

---

## 3. 🗄️ Persistência de Dados

### 3.1 Modelo de Persistência

Este projeto **não possui banco de dados próprio**.

A persistência e origem dos dados ocorre por meio de **APIs externas**, como:
- **RHiD**
- **ControliD**

### 3.2 Configurações
- **Protocolo**: HTTP / HTTPS
- **Formato**: JSON
- **Timeout padrão**: Configurável via `HttpClient`

---

## 4. 🌐 Serviços Externos

### 4.1 RHiD
- **Tipo**: API externa
- **Autenticação**: Credenciais + Token
- **Responsabilidade**:
  - Autenticação
  - Consulta de funcionários
  - Consulta de cargos, departamentos e centros de custo

### 4.2 ControliD
- **Tipo**: API externa
- **Responsabilidade**:
  - Sincronização de dados de funcionários
  - Atualizações cadastrais

---

## 5. 🏗️ Arquitetura da Aplicação

### 5.1 Padrão Arquitetural

**Clean Architecture (by the book)**

- Separação clara de responsabilidades
- Dependências apontando sempre para o domínio

---

### 5.2 Diagrama de Arquitetura Atual

```
┌──────────────────────────────────────────────┐
│              CAMADA DE API                  │
│                                              │
│  API_RMxControliD.API                         │
│  • Controllers                               │
│  • Endpoints REST                            │
│  • Swagger                                  │
└───────────────────────┬──────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────┐
│           CAMADA DE APLICAÇÃO                 │
│                                              │
│  API_RMxControliD.Application                 │
│  • UseCases / Services                       │
│  • DTOs                                     │
│  • Interfaces                               │
└───────────────────────┬──────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────┐
│           CAMADA DE DOMÍNIO                   │
│                                              │
│  API_RMxControliD.Domain                      │
│  • Entities                                  │
│  • Regras de Negócio                          │
└───────────────────────▲──────────────────────┘
                        │
                        │ implementa
                        │
┌──────────────────────────────────────────────┐
│         CAMADA DE INFRAESTRUTURA              │
│                                              │
│  API_RMxControliD.Infrastructure              │
│  • Integrações HTTP                           │
│  • Logging (NLog)                             │
│  • Clients de APIs externas                  │
└──────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│    RHiD      │ │  ControliD   │ │ Outras APIs  │
│     API      │ │     API      │ │              │
└──────────────┘ └──────────────┘ └──────────────┘
```

---

### 5.3 Estrutura de Camadas

| Camada | Responsabilidade | Dependências |
|------|------------------|--------------|
| **Domain** | Entidades e regras de negócio | Nenhuma |
| **Application** | Casos de uso, DTOs, Interfaces | Domain |
| **Infrastructure** | Integrações externas, HTTP, Logs | Application, Domain |
| **API** | Controllers e endpoints REST | Application, Infrastructure |

---

### 5.4 Padrões de Design
- Clean Architecture
- Dependency Injection
- DTO Pattern
- Adapter Pattern (integrações externas)

---

## 6. 🔒 Segurança

### 6.1 Comunicação
- HTTPS obrigatório
- Certificados SSL válidos

### 6.2 Autenticação Externa
- Autenticação baseada em **token** para consumo das APIs externas
- Tokens armazenados apenas em memória

---

## 7. ⚙️ Configuração

### 7.1 Ambientes
- `appsettings.json`
- `appsettings.Development.json`

### 7.2 Configurações Principais
- URLs das APIs externas
- Credenciais
- Configurações de logging (NLog)

---

## 8. 📁 Estrutura de Projeto

```
API_RMxControliD/
│
├── 📦 API/                         # Camada de Apresentação (API)
│   └── Controllers/
│       └── IntegraController.cs
│
├── 📦 Application/                 # Camada de Aplicação
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│
├── 📦 Domain/                      # Camada de Domínio
│   └── Entities/
│
├── 📦 Infrastructure/              # Camada de Infraestrutura (sugerida)
│   ├── Integrations/
│   ├── Http/
│   ├── Logging/
│   └── DependencyInjection.cs
│
├── appsettings.json
├── nlog.config
└── Program.cs
```

---

## 9. 📊 Versões Principais

| Componente | Versão |
|----------|--------|
| .NET | 8.0 |
| ASP.NET Core | 8.0 |
| NLog | Atual |
| Swashbuckle | Atual |

---

## 10. ⚠️ Observações Importantes

### 10.1 Integração com Sistemas Externos
- Falhas externas são tratadas com logging
- Timeouts e erros HTTP são monitorados

### 10.2 Evolução do Projeto
- Criação do projeto **Infrastructure** físico
- Implementação de testes unitários
- Padronização de erros com ProblemDetails
- Implementação de autenticação JWT, se necessário

---

**Última atualização**: Documentação baseada na estrutura atual do projeto API_RMxControliD.

👤 **Autor**: Cleiton Silva

