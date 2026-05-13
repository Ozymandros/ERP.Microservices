# Agentic Service (`agentic-service`)

## Overview

The Agentic service is an enterprise-grade AI orchestration module that acts as a "Bot Factory" allowing the ERP to spawn context-aware AI agents for different departments. It uses a hybrid data architecture separating strict relational configuration from semantic, unstructured memory.

## Architecture

### Technology Stack

- **Target Framework:** .NET 10
- **Orchestration:** .NET Aspire & Dapr (Dapr .NET SDK)
- **AI Framework:** Microsoft Agent Framework (ready for integration)
- **Relational DB (Config):** SQL Server via Entity Framework Core
- **Semantic DB (Memory):** PostgreSQL with pgvector
- **Cache/Session State:** Dapr State Store (Redis-backed)

### Data Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Agentic Service                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────┐    ┌──────────────────────────────┐ │
│  │   SQL Server         │    │   PostgreSQL (pgvector)      │ │
│  │   (Config Store)     │    │   (Semantic Memory)          │ │
│  │                      │    │                              │ │
│  │  - AIProvider        │    │  - AgentMemory               │ │
│  │  - AIModel           │    │    (Vector Embeddings)       │ │
│  │  - Agent             │    │                              │ │
│  │  - AgentPlugin       │    └──────────────────────────────┘ │
│  │  - AgentSession      │                                       │
│  └──────────────────────┘    ┌──────────────────────────────┐ │
│                               │   Dapr State Store (Redis)   │ │
│                               │   (Short-term Session)       │ │
│                               └──────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## API Endpoints (v1)

Base route: `api/agentic/*`

### Agents

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `api/agentic/agents` | List all agents |
| GET | `api/agentic/agents/{id}` | Get agent by ID |
| POST | `api/agentic/agents` | Create a new agent |
| PUT | `api/agentic/agents/{id}` | Update agent configuration |
| DELETE | `api/agentic/agents/{id}` | Delete an agent |

### Messages

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `api/agentic/messages` | Process a message with an AI agent |

## Agent Configuration

### Core Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | string | - | Agent display name |
| `Description` | string | - | Agent description |
| `ModelId` | GUID | - | Reference to AIModel |
| `SystemInstructions` | string | - | System prompt for the AI |
| `IsActive` | bool | true | Whether agent accepts requests |
| `TenantId` | GUID? | null | Optional tenant isolation |

### AI Execution Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Temperature` | double | 0.7 | LLM temperature (0.0-2.0) |
| `TopK` | int | 3 | Number of similar memories to retrieve |
| `MaxTokens` | int | 2048 | Maximum tokens in response |
| `EmbeddingDimensions` | int | 1536 | Vector embedding dimensions |
| `EnableMemory` | bool | true | Store conversation in PostgreSQL |
| `EnableRAG` | bool | true | Use semantic search for context |
| `EmbeddingModelName` | string? | null | Custom embedding model name |

## Message Processing Flow

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Request    │────>│  Resolve     │────>│  Read API Key   │
│  (UserId     │     │  Agent Config │     │  (SQL Server)    │
│   from JWT) │     │  (SQL Server) │     └─────────────────┘
└─────────────┘     └──────────────┘               │
                          │                          │
                          v                          v
                  ┌──────────────┐     ┌─────────────────┐
                  │  Restore     │     │  Semantic Search│
                  │  Session     │────>│  (pgvector)     │
                  │  (Redis)     │     │  Top-K memories │
                  └──────────────┘     └─────────────────┘
                          │                          │
                          v                          v
                  ┌──────────────────────────────────────────┐
                  │        AI Execution Service             │
                  │  (Combine: System Instructions +         │
                  │   Context Memories + Conversation        │
                  │   History + User Message)                │
                  └──────────────────────────────────────────┘
                          │
           ┌──────────────┴──────────────┐
           v                                 v
┌─────────────────────┐         ┌─────────────────────┐
│  Dapr State Store   │         │  PostgreSQL         │
│  (Short-term cache) │         │  (Long-term memory) │
└─────────────────────┘         └─────────────────────┘
```

## Authentication & Authorization

### User Authentication

Users must be authenticated via JWT tokens. The UserId is extracted from:
1. `ClaimTypes.NameIdentifier` claim
2. Fallback to `sub` claim

### Tenant Isolation

- Agents can be scoped to a `TenantId`
- Users can only access agents within their tenant or global (null tenant) agents
- Tenant ID is extracted from `tenant_id` claim in JWT

### Permissions

The Agentic module uses the standard ERP permission system:

| Permission | Description |
|------------|-------------|
| `Agentic.Read` | View list of agents |
| `Agentic.Create` | Create new agents |
| `Agentic.Update` | Modify agent configuration |
| `Agentic.Delete` | Remove agents |
| `Agentic.Execute` | Send messages to agents |
| `Agentic.Export` | Export agent configurations |
| `Agentic.Import` | Import agent configurations |

### Permission Assignment

Permissions are seeded via `PermissionSeeder` in the Auth module. Assign permissions to roles via the Roles API.

## Dapr Integration

### State Store

The service uses Dapr State Store (`statestore`) for:
- Short-term conversation session caching
- Quick retrieval of recent conversation history

### Provider API Keys

API keys for AI providers are stored encrypted on `AIProvider` records in SQL Server (`EncryptedApiKey`).

### Pub/Sub

The service can publish events for:
- Agent created/updated/deleted
- Message processing completed

## Database Schema

### SQL Server Tables

```sql
-- AI Providers (OpenAI, Anthropic, Azure OpenAI, etc.)
CREATE TABLE AIProviders (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    BaseUrl nvarchar(500) NOT NULL,
    EncryptedApiKey nvarchar(max) NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    UpdatedAt datetime2 NULL,
    UpdatedBy nvarchar(200) NULL
);

-- AI Models (gpt-4, claude-3, etc.)
CREATE TABLE AIModels (
    Id uniqueidentifier PRIMARY KEY,
    ProviderId uniqueidentifier NOT NULL,
    TechnicalName nvarchar(200) NOT NULL,
    TokenLimit int NOT NULL,
    Capabilities nvarchar(1000) NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    UpdatedAt datetime2 NULL,
    UpdatedBy nvarchar(200) NULL,
    FOREIGN KEY (ProviderId) REFERENCES AIProviders(Id)
);

-- Agents (Bot configurations)
CREATE TABLE Agents (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Description nvarchar(2000) NULL,
    ModelId uniqueidentifier NOT NULL,
    Temperature decimal(3,2) NOT NULL DEFAULT 0.7,
    TopK int NOT NULL DEFAULT 3,
    MaxTokens int NOT NULL DEFAULT 2048,
    EmbeddingDimensions int NOT NULL DEFAULT 1536,
    EnableMemory bit NOT NULL DEFAULT 1,
    EnableRAG bit NOT NULL DEFAULT 1,
    EmbeddingModelName nvarchar(200) NULL,
    SystemInstructions nvarchar(8000) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    TenantId uniqueidentifier NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    UpdatedAt datetime2 NULL,
    UpdatedBy nvarchar(200) NULL,
    FOREIGN KEY (ModelId) REFERENCES AIModels(Id)
);

-- Agent Plugins (Dapr app endpoints for tools)
CREATE TABLE AgentPlugins (
    Id uniqueidentifier PRIMARY KEY,
    AgentId uniqueidentifier NOT NULL,
    PluginName nvarchar(200) NOT NULL,
    DaprAppIdEndpoint nvarchar(500) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    UpdatedAt datetime2 NULL,
    UpdatedBy nvarchar(200) NULL,
    FOREIGN KEY (AgentId) REFERENCES Agents(Id)
);

-- Agent Sessions (tracking user sessions)
CREATE TABLE AgentSessions (
    Id uniqueidentifier PRIMARY KEY,
    AgentId uniqueidentifier NOT NULL,
    UserId nvarchar(200) NOT NULL,
    StartedAt datetime2 NOT NULL,
    LastMessageAt datetime2 NULL,
    Status nvarchar(20) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    UpdatedAt datetime2 NULL,
    UpdatedBy nvarchar(200) NULL,
    FOREIGN KEY (AgentId) REFERENCES Agents(Id)
);
```

### PostgreSQL Tables (with pgvector)

```sql
-- Enable vector extension
CREATE EXTENSION vector;

-- Agent Memory (semantic memory with embeddings)
CREATE TABLE AgentMemories (
    Id uuid PRIMARY KEY,
    SessionId uuid NOT NULL,
    Role varchar(20) NOT NULL,
    Content text NOT NULL,
    Embedding vector(1536) NULL,
    Metadata jsonb NULL,
    CreatedAt timestamp with time zone NOT NULL
);

-- Index for vector similarity search
CREATE INDEX ix_agentmemories_embedding 
ON AgentMemories 
USING ivfflat (Embedding vector_cosine_ops)
WITH (lists = 100);
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "agenticdb": "Server=...;Database=AgenticDB;...",
    "agentic-memory": "Host=localhost;Database=agentic-memory;..."
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "MyApp.Auth",
    "Audience": "MyApp.All"
  }
}
```

### Provider Secrets Crypto

Configure a Base64-encoded 32-byte key for provider API key encryption/decryption:

```json
{
  "SecretCrypto": {
    "MasterKey": "<BASE64_32_BYTE_KEY>"
  }
}
```

### Dapr Components

Ensure the following components are configured in your Dapr deployment:

1. **State Store** (`statestore`): Redis-backed state storage
2. **Secret Store** (`secretstore`): Optional for other secrets not managed by Agentic providers
3. **Pub Sub** (`pubsub`): For event publishing (optional)

## Error Handling

| Exception | HTTP Status | Description |
|-----------|-------------|-------------|
| `InvalidOperationException` | 404 | Agent not found or inactive |
| `UnauthorizedAccessException` | 403 | Tenant access denied |
| `KeyNotFoundException` | 500 | API key not found in secrets |

## Testing

Run unit tests:

```bash
dotnet test src/MyApp.Agentic/test/MyApp.Agentic.Application.Tests
```

### Test Coverage

- Agent CRUD operations
- Message processing with RAG
- Tenant isolation validation
- Memory enable/disable scenarios
- Permission-based access

## Migration

To add Agentic to an existing deployment:

1. Run database migrations for SQL Server
2. Ensure PostgreSQL with pgvector is available
3. Seed Agentic permissions in Auth:
   ```csharp
   await PermissionSeeder.SeedPermissionsAsync(authDbContext);
   ```
4. Configure Dapr components
5. Set provider API keys through `PUT api/agentic/providers/{id}`

## Integration Notes

### Replacing Stub Services

The implementation includes stub services that should be replaced for production:

1. **IEmbeddingService**: Replace `StubEmbeddingService` with actual embedding provider (OpenAI, Azure OpenAI, etc.)

2. **IAgentExecutionService**: Replace `StubAgentExecutionService` with Microsoft Agent Framework or direct LLM API calls

### Rate Limiting

Consider implementing rate limiting at the API gateway level for:
- Messages per user per minute
- Tokens per user per hour

## See Also

- [CRM Service Documentation](./CRM.md)
- [Architecture Overview](../architecture/ARCHITECTURE_DOCUMENTATION.md)
- [Security Best Practices](../security/SECURITY_IDENTITY_BEST_PRACTICES.md)
