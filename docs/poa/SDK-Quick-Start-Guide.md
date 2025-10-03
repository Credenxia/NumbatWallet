# NumbatWallet SDK - Quick Start Guide

**Date**: October 3, 2025
**Backend Version**: Commit `2366746`
**GraphQL Endpoint**: http://localhost:5042/graphql

---

## 🚀 Quick Start (5 Minutes)

### 1. Verify Backend is Running

```bash
# Test backend connectivity
curl -X POST http://localhost:5042/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __typename }"}'
```

**Expected Response**:
```json
{
  "data": {
    "__typename": "Query"
  }
}
```

### 2. Access GraphQL Playground

**URL**: http://localhost:5042/graphql/

**Features**:
- 🎨 Interactive GraphQL explorer
- 📚 Complete schema documentation
- 🔍 Query builder with autocomplete
- 📥 Export schema to SDL
- ✅ Query validation

**No authentication required** in Development environment.

### 3. Run Your First Query

Open http://localhost:5042/graphql/ and paste:

```graphql
query GetWallets {
  wallets(first: 10) {
    nodes {
      id
      personName
      status
      credentialCount
    }
    pageInfo {
      hasNextPage
      endCursor
    }
    totalCount
  }
}
```

Click **Run** ▶️

---

## 📖 Common Operations

### Query: Get Credential by ID

```graphql
query GetCredential($id: UUID!) {
  credential(id: $id) {
    id
    type
    status
    claims
    issuedAt
    expiresAt
    walletId
    issuer
  }
}
```

**Variables**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000"
}
```

### Mutation: Issue Credential

**⚠️ IMPORTANT**: Use `claimsJson` as a **JSON string**, not an object!

```graphql
mutation IssueCredential($input: IssueCredentialInput!) {
  issueCredential(input: $input) {
    id
    type
    status
    claims
    issuedAt
    expiresAt
  }
}
```

**Variables** (note `claimsJson` is a string):
```json
{
  "input": {
    "walletId": "123e4567-e89b-12d3-a456-426614174000",
    "credentialType": "DRIVERS_LICENSE",
    "subject": "did:example:123",
    "claimsJson": "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30,\"licenseNumber\":\"DL123456\"}",
    "issuerOrganizationId": "223e4567-e89b-12d3-a456-426614174000"
  }
}
```

**Common Mistake** ❌:
```json
// DON'T DO THIS - claimsJson must be a string!
"claimsJson": {
  "firstName": "John",
  "lastName": "Doe"
}
```

**Correct** ✅:
```json
// Escape the JSON string properly
"claimsJson": "{\"firstName\":\"John\",\"lastName\":\"Doe\"}"
```

### Mutation: Bulk Issue Credentials

```graphql
mutation BulkIssue($input: BulkIssueCredentialsInput!) {
  bulkIssueCredentials(input: $input) {
    totalRequested
    successCount
    failureCount
    issuedCredentialIds
    errors {
      walletId
      errorMessage
    }
  }
}
```

**Variables**:
```json
{
  "input": {
    "walletIds": [
      "111e4567-e89b-12d3-a456-426614174000",
      "222e4567-e89b-12d3-a456-426614174000"
    ],
    "credentialType": "PROOF_OF_AGE",
    "templateJson": "{\"issuer\":\"WA Government\",\"credentialSubject\":{\"givenName\":\"{firstName}\",\"familyName\":\"{lastName}\"}}",
    "issuerOrganizationId": "333e4567-e89b-12d3-a456-426614174000"
  }
}
```

---

## 🔐 Authentication

### Development Environment

**No authentication required** for GraphQL endpoint in Development.

### Production Environment

**API Key Authentication**:

```bash
curl -X POST http://localhost:5042/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{"query":"{ __typename }"}'
```

**C# SDK Client**:
```csharp
var client = new NumbatWalletClient(new NumbatWalletClientOptions
{
    BaseUrl = "http://localhost:5042/graphql",
    ApiKey = "sdk-test-key-12345"
});
```

---

## 📋 Schema Introspection

### Method 1: Banana Cake Pop UI

1. Open http://localhost:5042/graphql/
2. Click **Schema** tab (top right)
3. Browse types, fields, and descriptions
4. Click **Export SDL** to download schema file

### Method 2: GraphQL Introspection Query

```bash
curl -X POST http://localhost:5042/graphql \
  -H "Content-Type: application/json" \
  -d @- << 'EOF'
{
  "query": "query IntrospectionQuery { __schema { queryType { name } mutationType { name } types { name kind description } } }"
}
EOF
```

### Method 3: Pre-exported Files

Backend team has exported the schema:

1. **JSON Introspection**: `docs/poa/graphql-schema-introspection.json` (141KB)
2. **SDL Format**: `docs/poa/graphql-schema-sdl.json` (129KB)

```bash
# View schema types
jq '.data.__schema.types[] | select(.name | startswith("__") | not) | .name' \
  docs/poa/graphql-schema-introspection.json
```

---

## 🧪 Testing & Debugging

### Test Query Execution

```bash
# Simple test
curl -X POST http://localhost:5042/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __typename }"}'

# Expected: {"data":{"__typename":"Query"}}
```

### Test Mutation with Variables

```bash
curl -X POST http://localhost:5042/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation($input: IssueCredentialInput!) { issueCredential(input: $input) { id } }",
    "variables": {
      "input": {
        "walletId": "123e4567-e89b-12d3-a456-426614174000",
        "credentialType": "DRIVERS_LICENSE",
        "subject": "did:example:123",
        "claimsJson": "{\"name\":\"Test\"}",
        "issuerOrganizationId": "223e4567-e89b-12d3-a456-426614174000"
      }
    }
  }'
```

### Common Errors

#### Error: HC0015 "No document or document id"

**Cause**: Missing `query` field in request body.

**Fix**:
```diff
{
- "operationName": "GetWallets"
+ "query": "query GetWallets { ... }"
}
```

#### Error: "Variable $input of type IssueCredentialInput! was provided invalid value"

**Cause**: `claimsJson` provided as object instead of string.

**Fix**:
```diff
{
  "input": {
-   "claimsJson": { "name": "John" }
+   "claimsJson": "{\"name\":\"John\"}"
  }
}
```

#### Error: "Invalid JSON string in claimsJson"

**Cause**: JSON string not properly escaped.

**Fix**:
```bash
# Use jq to properly escape JSON
echo '{"name":"John","age":30}' | jq -c '.' | jq -R '.'
# Output: "{\"name\":\"John\",\"age\":30}"
```

---

## 📦 SDK Code Generation

### Using GraphQL Code Generator

```bash
# Install graphql-codegen
npm install -g @graphql-codegen/cli

# Generate types from introspection
graphql-codegen \
  --schema http://localhost:5042/graphql \
  --documents './queries/**/*.graphql' \
  --generates types.ts
```

### Using StrawberryShake (.NET)

```bash
# Add StrawberryShake to project
dotnet add package StrawberryShake.Tools

# Generate client
dotnet graphql init http://localhost:5042/graphql -n NumbatWalletClient
```

---

## 🔍 Available GraphQL Types

### Core Types

- `Wallet` - Digital wallet entity
- `Credential` - Verifiable credential
- `Person` - User/citizen entity
- `Organization` - Issuing organization
- `Issuance` - Credential issuance request

### Input Types (Mutations)

- `IssueCredentialInput` - **Uses `claimsJson: String!`**
- `BulkIssueCredentialsInput` - **Uses `templateJson: String!`**
- `CreateIssuanceInput` - **Uses `additionalDataJson: String?`**
- `CreateWalletInput`
- `UpdateWalletInput`

### Pagination Types

- `PageInfo` - Relay cursor pagination metadata
- `PagedResult<T>` - Generic paged response
- `CursorEdge<T>` - Relay edge with cursor

### Enum Types

- `CredentialType` - DRIVERS_LICENSE, PROOF_OF_AGE, etc.
- `CredentialStatus` - ACTIVE, REVOKED, EXPIRED
- `WalletStatus` - ACTIVE, SUSPENDED, CLOSED

---

## 📚 Example Queries

### Get Wallets with Pagination

```graphql
query GetWallets($first: Int!, $after: String) {
  wallets(first: $first, after: $after) {
    nodes {
      id
      personName
      status
      credentialCount
      createdAt
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
    totalCount
  }
}
```

### Get Credentials by Type

```graphql
query GetCredentialsByType($type: CredentialType!) {
  credentialsByType(type: $type, first: 20) {
    nodes {
      id
      type
      status
      claims
      issuedAt
      expiresAt
      issuer
    }
    totalCount
  }
}
```

### Verify Credential

```graphql
mutation VerifyCredential($credentialId: UUID!) {
  verifyCredential(credentialId: $credentialId) {
    isValid
    issuer
    issuedAt
    expiresAt
    errors
    claims
  }
}
```

---

## 🛠️ Troubleshooting

### Backend Not Responding

```bash
# Check if backend is running
lsof -ti:5042

# If not running, start it
cd /Users/rodrigolmiranda/repo/NumbatWallet
dotnet run --project src/NumbatWallet.Web.Api
```

### GraphQL Endpoint Not Found

**Verify URL**: http://localhost:5042/graphql (no trailing slash)

**Common mistakes**:
- ❌ http://localhost:5042/graphql/schema
- ❌ http://localhost:5042/api/graphql
- ✅ http://localhost:5042/graphql

### Introspection Disabled

If introspection fails in production:

```bash
# Check environment
echo $ASPNETCORE_ENVIRONMENT

# Production disables introspection by default
# Use pre-exported schema files instead
```

---

## 📞 Support

**Backend Team**: rodrigolmiranda@gmail.com
**GraphQL Endpoint**: http://localhost:5042/graphql
**Banana Cake Pop**: http://localhost:5042/graphql/
**Schema Files**: `docs/poa/graphql-schema-*.json`

**Documentation**:
- Breaking Changes: `docs/poa/SDK-Breaking-Changes-Report.md`
- Schema Files: `docs/poa/graphql-schema-*.json`

---

**Last Updated**: October 3, 2025
**Status**: ✅ **Ready for SDK Integration**
