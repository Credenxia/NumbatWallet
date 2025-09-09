# Credenxia JavaScript/TypeScript SDK

![npm version](https://img.shields.io/npm/v/@credenxia/sdk)
![TypeScript](https://img.shields.io/badge/TypeScript-4.9+-blue)
![License](https://img.shields.io/badge/license-Proprietary-red)
![Bundle Size](https://img.shields.io/bundlephobia/minzip/@credenxia/sdk)

Official JavaScript/TypeScript SDK for the Credenxia Digital Wallet and Verifiable Credentials platform.

## Features

- 🌐 **Browser & Node.js** - Works in browsers and Node.js environments
- 📝 **TypeScript First** - Full TypeScript support with complete type definitions
- 🔒 **WebCrypto API** - Native browser cryptography for security
- 📦 **Tree-Shakeable** - Only bundle what you use
- 🔄 **Real-time Updates** - WebSocket support for live updates
- 🧩 **Framework Agnostic** - Works with React, Vue, Angular, or vanilla JS
- 🎣 **React Hooks** - Optional React hooks for easy integration

## Installation

### npm

```bash
npm install @credenxia/sdk
```

### yarn

```bash
yarn add @credenxia/sdk
```

### pnpm

```bash
pnpm add @credenxia/sdk
```

### CDN

```html
<!-- UMD build -->
<script src="https://unpkg.com/@credenxia/sdk@latest/dist/credenxia.umd.js"></script>

<!-- ES Module -->
<script type="module">
  import { CredenxiaClient } from 'https://unpkg.com/@credenxia/sdk@latest/dist/credenxia.esm.js';
</script>
```

## Quick Start

### Basic Setup (TypeScript)

```typescript
import { CredenxiaClient, CredenxiaConfig } from '@credenxia/sdk';

// Initialize the client
const client = new CredenxiaClient({
  apiKey: 'your-api-key',
  environment: 'production',
  baseUrl: 'https://api.credenxia.gov.au'
});

// Create a wallet
async function createWallet() {
  try {
    const wallet = await client.wallets.create({
      userId: 'user-123',
      type: 'personal'
    });
    
    console.log('Wallet created:', wallet.did);
    return wallet;
  } catch (error) {
    console.error('Error creating wallet:', error);
    throw error;
  }
}
```

### Basic Setup (JavaScript)

```javascript
const { CredenxiaClient } = require('@credenxia/sdk');

// Initialize the client
const client = new CredenxiaClient({
  apiKey: 'your-api-key',
  environment: 'production'
});

// Create a wallet
async function createWallet() {
  try {
    const wallet = await client.wallets.create({
      userId: 'user-123',
      type: 'personal'
    });
    
    console.log('Wallet created:', wallet.did);
    return wallet;
  } catch (error) {
    console.error('Error creating wallet:', error);
    throw error;
  }
}
```

## React Integration

### Setup with React Hooks

```tsx
import React from 'react';
import { CredenxiaProvider, useCredenxia } from '@credenxia/sdk/react';

// Wrap your app with the provider
function App() {
  return (
    <CredenxiaProvider
      config={{
        apiKey: process.env.REACT_APP_CREDENXIA_API_KEY,
        environment: 'production'
      }}
    >
      <WalletManager />
    </CredenxiaProvider>
  );
}

// Use hooks in components
function WalletManager() {
  const { client, isReady } = useCredenxia();
  const [wallet, setWallet] = React.useState(null);
  const [loading, setLoading] = React.useState(false);
  
  const createWallet = async () => {
    if (!isReady) return;
    
    setLoading(true);
    try {
      const newWallet = await client.wallets.create({
        userId: 'current-user',
        type: 'personal'
      });
      setWallet(newWallet);
    } catch (error) {
      console.error('Failed to create wallet:', error);
    } finally {
      setLoading(false);
    }
  };
  
  return (
    <div>
      {wallet ? (
        <div>Wallet DID: {wallet.did}</div>
      ) : (
        <button onClick={createWallet} disabled={loading}>
          {loading ? 'Creating...' : 'Create Wallet'}
        </button>
      )}
    </div>
  );
}
```

### Custom Hooks

```tsx
import { useWallet, useCredentials, useVerification } from '@credenxia/sdk/react';

function CredentialManager() {
  const { wallet, loading: walletLoading } = useWallet('user-123');
  const { credentials, fetchCredentials } = useCredentials(wallet?.id);
  const { verify, verifying, result } = useVerification();
  
  const handleVerification = async (credentialId: string) => {
    const credential = credentials.find(c => c.id === credentialId);
    if (credential) {
      await verify(credential);
    }
  };
  
  // Component logic...
}
```

## Vue.js Integration

```vue
<template>
  <div>
    <button @click="createWallet" :disabled="loading">
      {{ loading ? 'Creating...' : 'Create Wallet' }}
    </button>
    <div v-if="wallet">
      Wallet DID: {{ wallet.did }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { CredenxiaClient } from '@credenxia/sdk';

const client = new CredenxiaClient({
  apiKey: import.meta.env.VITE_CREDENXIA_API_KEY,
  environment: 'production'
});

const wallet = ref(null);
const loading = ref(false);

const createWallet = async () => {
  loading.value = true;
  try {
    wallet.value = await client.wallets.create({
      userId: 'current-user',
      type: 'personal'
    });
  } catch (error) {
    console.error('Failed to create wallet:', error);
  } finally {
    loading.value = false;
  }
};
</script>
```

## Angular Integration

```typescript
import { Injectable } from '@angular/core';
import { CredenxiaClient, Wallet, Credential } from '@credenxia/sdk';
import { Observable, from } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WalletService {
  private client: CredenxiaClient;
  
  constructor() {
    this.client = new CredenxiaClient({
      apiKey: environment.credenxiaApiKey,
      environment: 'production'
    });
  }
  
  createWallet(userId: string): Observable<Wallet> {
    return from(this.client.wallets.create({
      userId,
      type: 'personal'
    }));
  }
  
  getCredentials(walletId: string): Observable<Credential[]> {
    return from(this.client.credentials.list({
      walletId
    }));
  }
}
```

## Advanced Features

### WebSocket Real-time Updates

```typescript
// Enable WebSocket connection
const client = new CredenxiaClient({
  apiKey: 'your-api-key',
  enableWebSocket: true
});

// Subscribe to wallet events
client.on('wallet.created', (wallet) => {
  console.log('New wallet created:', wallet);
});

client.on('credential.issued', (credential) => {
  console.log('New credential issued:', credential);
});

client.on('credential.revoked', ({ credentialId, reason }) => {
  console.log(`Credential ${credentialId} revoked: ${reason}`);
});

// Unsubscribe when done
client.off('wallet.created');
```

### Offline Support with IndexedDB

```typescript
import { CredenxiaClient, OfflineStorage } from '@credenxia/sdk';

// Enable offline support
const client = new CredenxiaClient({
  apiKey: 'your-api-key',
  offline: {
    enabled: true,
    storage: new OfflineStorage({
      dbName: 'credenxia-offline',
      version: 1
    })
  }
});

// Works offline - data syncs when online
const credentials = await client.credentials.list({
  walletId: 'wallet-123'
});

// Check sync status
client.offline.on('sync:start', () => {
  console.log('Syncing with server...');
});

client.offline.on('sync:complete', (result) => {
  console.log(`Synced ${result.count} items`);
});
```

### Browser Crypto Operations

```typescript
import { CryptoService } from '@credenxia/sdk/crypto';

const crypto = new CryptoService();

// Generate key pair
const keyPair = await crypto.generateKeyPair('ECDSA', 'P-256');

// Sign data
const signature = await crypto.sign(
  data,
  keyPair.privateKey,
  { algorithm: 'ECDSA' }
);

// Verify signature
const isValid = await crypto.verify(
  data,
  signature,
  keyPair.publicKey
);

// Encrypt/Decrypt
const encrypted = await crypto.encrypt(data, publicKey);
const decrypted = await crypto.decrypt(encrypted, privateKey);
```

### QR Code Generation and Scanning

```typescript
import { QRCodeService } from '@credenxia/sdk/qr';

const qrService = new QRCodeService();

// Generate QR code for presentation request
const presentationRequest = {
  challenge: 'random-challenge',
  requestedCredentials: ['DriversLicense']
};

const qrCodeDataUrl = await qrService.generate(presentationRequest);

// Display QR code
document.getElementById('qr-code').src = qrCodeDataUrl;

// Scan QR code (requires camera permission)
const scanner = await qrService.createScanner();
scanner.on('scan', async (data) => {
  const presentation = await client.presentations.parse(data);
  console.log('Scanned presentation:', presentation);
});

scanner.start(document.getElementById('camera-preview'));
```

## TypeScript Support

### Type Definitions

```typescript
import type {
  Wallet,
  Credential,
  VerifiablePresentation,
  VerificationResult,
  IssuerProfile,
  TrustRegistry
} from '@credenxia/sdk';

// Strongly typed API responses
const wallet: Wallet = await client.wallets.create({
  userId: 'user-123',
  type: 'personal'
});

// Type guards
if (isCredential(data)) {
  // data is typed as Credential
  console.log(data.credentialSubject);
}

// Generic types for custom claims
interface CustomClaims {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
}

const credential = await client.credentials.issue<CustomClaims>({
  walletId: wallet.id,
  type: 'CustomCredential',
  subject: {
    firstName: 'John',
    lastName: 'Doe',
    dateOfBirth: '1990-01-01'
  }
});
```

### Custom Types

```typescript
// Extend SDK types
interface ExtendedWallet extends Wallet {
  customField: string;
}

// Type-safe configuration
const config: CredenxiaConfig = {
  apiKey: process.env.API_KEY!,
  environment: 'production',
  retry: {
    maxAttempts: 3,
    backoffMultiplier: 2
  },
  cache: {
    enabled: true,
    ttl: 300000 // 5 minutes
  }
};
```

## Error Handling

```typescript
import { 
  CredenxiaError,
  NetworkError,
  ValidationError,
  AuthenticationError 
} from '@credenxia/sdk/errors';

try {
  await client.wallets.create({ userId: 'user-123' });
} catch (error) {
  if (error instanceof ValidationError) {
    console.error('Validation failed:', error.errors);
  } else if (error instanceof AuthenticationError) {
    console.error('Authentication failed:', error.message);
    // Redirect to login
  } else if (error instanceof NetworkError) {
    console.error('Network error:', error.message);
    // Retry or show offline message
  } else if (error instanceof CredenxiaError) {
    console.error('SDK error:', error.code, error.message);
  } else {
    console.error('Unknown error:', error);
  }
}
```

## Testing

### Unit Testing with Jest

```typescript
import { CredenxiaClient } from '@credenxia/sdk';
import { mockClient } from '@credenxia/sdk/testing';

describe('WalletService', () => {
  let client: CredenxiaClient;
  
  beforeEach(() => {
    client = mockClient();
  });
  
  test('creates wallet successfully', async () => {
    const wallet = await client.wallets.create({
      userId: 'test-user',
      type: 'personal'
    });
    
    expect(wallet).toBeDefined();
    expect(wallet.did).toMatch(/^did:credenxia:/);
  });
});
```

### E2E Testing with Playwright

```typescript
import { test, expect } from '@playwright/test';

test('wallet creation flow', async ({ page }) => {
  await page.goto('/wallet/create');
  
  // Fill form
  await page.fill('[name="userId"]', 'test-user');
  await page.selectOption('[name="type"]', 'personal');
  
  // Submit
  await page.click('button[type="submit"]');
  
  // Wait for wallet creation
  await page.waitForSelector('[data-testid="wallet-did"]');
  
  const did = await page.textContent('[data-testid="wallet-did"]');
  expect(did).toMatch(/^did:credenxia:/);
});
```

## Performance Optimization

### Code Splitting

```typescript
// Lazy load heavy features
const QRScanner = lazy(() => import('@credenxia/sdk/qr'));
const CryptoService = lazy(() => import('@credenxia/sdk/crypto'));

// Use when needed
async function scanQRCode() {
  const { QRCodeScanner } = await import('@credenxia/sdk/qr');
  const scanner = new QRCodeScanner();
  // ...
}
```

### Caching

```typescript
const client = new CredenxiaClient({
  apiKey: 'your-api-key',
  cache: {
    enabled: true,
    strategy: 'memory', // or 'localStorage', 'sessionStorage'
    ttl: 5 * 60 * 1000, // 5 minutes
    maxSize: 50 // Maximum cached items
  }
});

// Force cache refresh
await client.credentials.list({ walletId: 'wallet-123' }, { 
  cache: 'reload' 
});
```

## Browser Support

| Browser | Version |
|---------|---------|
| Chrome | 90+ |
| Firefox | 88+ |
| Safari | 14+ |
| Edge | 90+ |
| Opera | 76+ |

### Polyfills

```javascript
// For older browsers, include polyfills
import '@credenxia/sdk/polyfills';

// Or selectively import what you need
import '@credenxia/sdk/polyfills/webcrypto';
import '@credenxia/sdk/polyfills/indexeddb';
```

## Bundle Size

| Package | Size (minified) | Size (gzipped) |
|---------|----------------|----------------|
| Core | 45 KB | 12 KB |
| React | 8 KB | 3 KB |
| Crypto | 15 KB | 5 KB |
| QR | 25 KB | 8 KB |
| Full | 93 KB | 28 KB |

## Migration Guide

### From v0.x to v1.0

```typescript
// Old (v0.x)
import Credenxia from 'credenxia-sdk';
const sdk = new Credenxia('api-key');
const wallet = await sdk.createWallet('user-123');

// New (v1.0)
import { CredenxiaClient } from '@credenxia/sdk';
const client = new CredenxiaClient({ apiKey: 'api-key' });
const wallet = await client.wallets.create({ 
  userId: 'user-123',
  type: 'personal'
});
```

## API Reference

Full API documentation: https://docs.credenxia.gov.au/sdk/javascript

## Examples

- [React Example](https://github.com/credenxia/sdk-examples/tree/main/react)
- [Vue Example](https://github.com/credenxia/sdk-examples/tree/main/vue)
- [Angular Example](https://github.com/credenxia/sdk-examples/tree/main/angular)
- [Node.js Example](https://github.com/credenxia/sdk-examples/tree/main/nodejs)
- [Next.js Example](https://github.com/credenxia/sdk-examples/tree/main/nextjs)

## Support

- **Documentation**: https://docs.credenxia.gov.au
- **npm Package**: https://www.npmjs.com/package/@credenxia/sdk
- **GitHub**: https://github.com/credenxia/javascript-sdk
- **Discord**: https://discord.gg/credenxia
- **Email**: sdk-support@credenxia.gov.au

## License

This SDK is proprietary software. See LICENSE file for details.

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

Built with TypeScript for modern web applications 🚀