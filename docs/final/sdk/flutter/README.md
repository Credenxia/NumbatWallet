# Credenxia Flutter SDK

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android-green)
![Dart](https://img.shields.io/badge/dart-%3E%3D3.0.0-blue)
![Flutter](https://img.shields.io/badge/flutter-%3E%3D3.13.0-blue)

Official Flutter SDK for the Credenxia Digital Wallet and Verifiable Credentials platform.

## Features

- 🔐 **Secure Wallet Management** - Create and manage digital wallets with hardware-backed security
- 📜 **Credential Operations** - Issue, store, and manage W3C Verifiable Credentials
- ✅ **Verification** - Online and offline credential verification
- 🔑 **Biometric Authentication** - Face ID, Touch ID, and fingerprint support
- 💾 **Offline Support** - Full offline capability with sync when connected
- 🔒 **Zero-Knowledge Proofs** - Privacy-preserving selective disclosure
- 📱 **Platform Native** - Optimized for iOS and Android

## Installation

Add this to your package's `pubspec.yaml` file:

```yaml
dependencies:
  credenxia_sdk: ^1.0.0
```

Then run:

```bash
flutter pub get
```

### iOS Setup

Add to your `Info.plist`:

```xml
<key>NSFaceIDUsageDescription</key>
<string>This app uses Face ID for secure authentication</string>
<key>NSCameraUsageDescription</key>
<string>This app uses the camera to scan QR codes</string>
```

### Android Setup

Add to your `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.USE_BIOMETRIC" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.INTERNET" />
```

## Quick Start

### Initialize the SDK

```dart
import 'package:credenxia_sdk/credenxia_sdk.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Initialize the SDK
  final sdk = CredenxiaSDK(
    config: CredenxiaConfig(
      environment: Environment.production,
      apiKey: 'your-api-key',
      enableBiometrics: true,
      enableOfflineMode: true,
    ),
  );
  
  await sdk.initialize();
  
  runApp(MyApp(sdk: sdk));
}
```

### Create a Wallet

```dart
class WalletManager {
  final CredenxiaSDK sdk;
  
  WalletManager(this.sdk);
  
  Future<Wallet> createWallet() async {
    try {
      // Check biometric availability
      final biometricAvailable = await sdk.isBiometricAvailable();
      
      // Create wallet with biometric protection
      final wallet = await sdk.createWallet(
        CreateWalletRequest(
          userId: 'user-123',
          type: WalletType.personal,
          enableBiometrics: biometricAvailable,
        ),
      );
      
      print('Wallet created: ${wallet.did}');
      return wallet;
    } catch (e) {
      print('Error creating wallet: $e');
      rethrow;
    }
  }
}
```

### Request and Store a Credential

```dart
Future<Credential> requestCredential(String credentialType) async {
  try {
    // Request credential from issuer
    final credential = await sdk.requestCredential(
      CredentialRequest(
        type: credentialType,
        walletId: currentWallet.id,
        subject: {
          'firstName': 'John',
          'lastName': 'Doe',
          'dateOfBirth': '1990-01-01',
        },
      ),
    );
    
    // Store credential securely
    await sdk.storeCredential(credential);
    
    print('Credential stored: ${credential.id}');
    return credential;
  } catch (e) {
    print('Error requesting credential: $e');
    rethrow;
  }
}
```

### Present a Credential

```dart
Future<void> presentCredential(PresentationRequest request) async {
  try {
    // Get available credentials
    final credentials = await sdk.getCredentials(
      filter: CredentialFilter(
        types: request.requestedTypes,
      ),
    );
    
    // Let user select credentials and fields
    final selected = await showCredentialSelector(
      context: context,
      credentials: credentials,
      requestedFields: request.requestedFields,
    );
    
    // Create presentation with selective disclosure
    final presentation = await sdk.createPresentation(
      PresentationBuilder()
        .addCredentials(selected.credentials)
        .withSelectiveDisclosure(selected.fields)
        .withChallenge(request.challenge)
        .build(),
    );
    
    // Submit presentation
    await sdk.submitPresentation(presentation);
    
    print('Presentation submitted successfully');
  } catch (e) {
    print('Error presenting credential: $e');
    rethrow;
  }
}
```

### Offline Verification

```dart
Future<VerificationResult> verifyOffline(String qrData) async {
  try {
    // Parse QR code data
    final presentation = PresentationData.fromQR(qrData);
    
    // Verify offline using cached trust registry
    final result = await sdk.verifyOffline(
      presentation: presentation,
      options: VerificationOptions(
        checkRevocation: true,
        checkExpiry: true,
        allowExpiredTrustAnchors: false,
      ),
    );
    
    if (result.isValid) {
      print('✅ Credential verified successfully');
    } else {
      print('❌ Verification failed: ${result.errors}');
    }
    
    return result;
  } catch (e) {
    print('Error verifying credential: $e');
    rethrow;
  }
}
```

## Advanced Features

### Backup and Recovery

```dart
// Create encrypted backup
final backup = await sdk.createBackup(
  BackupOptions(
    includeCredentials: true,
    includeKeys: true,
    encryptionPassword: userPassword,
  ),
);

// Save backup to secure storage or cloud
await saveBackupToCloud(backup);

// Restore from backup
await sdk.restoreFromBackup(
  backupData: backup,
  password: userPassword,
);
```

### Biometric Authentication

```dart
// Configure biometric authentication
await sdk.configureBiometrics(
  BiometricConfig(
    allowFaceID: true,
    allowTouchID: true,
    allowFingerprint: true,
    fallbackToPasscode: true,
  ),
);

// Authenticate before sensitive operations
final authenticated = await sdk.authenticateWithBiometrics(
  reason: 'Authenticate to access your credentials',
);

if (authenticated) {
  // Proceed with sensitive operation
}
```

### Zero-Knowledge Proofs

```dart
// Create a presentation with ZKP for age verification
final presentation = await sdk.createZKPresentation(
  ZKPresentationRequest(
    predicate: AgeProof(minimumAge: 18),
    credential: driverLicense,
    proofType: ProofType.bbs,
  ),
);

// Verifier only learns that age >= 18, not the actual date of birth
```

## SDK Architecture

```
credenxia_sdk/
├── lib/
│   ├── src/
│   │   ├── core/           # Core functionality
│   │   ├── wallet/         # Wallet management
│   │   ├── credentials/    # Credential operations
│   │   ├── crypto/         # Cryptographic operations
│   │   ├── storage/        # Secure storage
│   │   ├── network/        # API communication
│   │   └── offline/        # Offline capabilities
│   ├── models/             # Data models
│   ├── widgets/            # UI components
│   └── credenxia_sdk.dart # Main entry point
├── ios/                    # iOS specific code
├── android/               # Android specific code
└── example/               # Example application
```

## Security Considerations

- **Secure Storage**: All sensitive data is stored in platform secure storage (Keychain on iOS, Keystore on Android)
- **Encryption**: AES-256-GCM encryption for data at rest
- **Key Management**: Hardware-backed key generation when available
- **Biometric Protection**: Optional biometric authentication for wallet access
- **Certificate Pinning**: SSL certificate pinning for API communication
- **Jailbreak Detection**: Runtime application self-protection (RASP)

## Error Handling

```dart
try {
  await sdk.someOperation();
} on NetworkException catch (e) {
  // Handle network errors
  showError('Network error: ${e.message}');
} on AuthenticationException catch (e) {
  // Handle auth errors
  navigateToLogin();
} on ValidationException catch (e) {
  // Handle validation errors
  showValidationError(e.field, e.message);
} on CredenxiaException catch (e) {
  // Handle general SDK errors
  showError('Error: ${e.message}');
}
```

## Testing

### Unit Tests

```bash
flutter test
```

### Integration Tests

```bash
flutter test integration_test
```

### Test Coverage

```bash
flutter test --coverage
genhtml coverage/lcov.info -o coverage/html
```

## Migration Guide

### From v0.x to v1.0

```dart
// Old (v0.x)
final wallet = await sdk.wallet.create(userId: 'user-123');

// New (v1.0)
final wallet = await sdk.createWallet(
  CreateWalletRequest(
    userId: 'user-123',
    type: WalletType.personal,
  ),
);
```

## Performance Tips

1. **Enable Caching**: Use the built-in caching for frequently accessed data
2. **Batch Operations**: Use batch APIs when dealing with multiple credentials
3. **Lazy Loading**: Load credentials on demand rather than all at once
4. **Background Sync**: Enable background sync for offline changes
5. **Image Optimization**: Compress credential images before storage

## Troubleshooting

### Common Issues

**Issue**: Biometric authentication not working
```dart
// Solution: Check availability first
if (await sdk.isBiometricAvailable()) {
  // Use biometric auth
} else {
  // Fall back to PIN/password
}
```

**Issue**: Offline verification failing
```dart
// Solution: Ensure trust registry is cached
await sdk.updateOfflineCache();
```

**Issue**: Large credential causing memory issues
```dart
// Solution: Use streaming for large credentials
await sdk.streamCredential(credentialId);
```

## API Reference

Full API documentation is available at: https://docs.credenxia.gov.au/sdk/flutter

## Support

- **Documentation**: https://docs.credenxia.gov.au
- **Issues**: https://github.com/credenxia/flutter-sdk/issues
- **Email**: sdk-support@credenxia.gov.au
- **Discord**: https://discord.gg/credenxia

## License

This SDK is proprietary software. See LICENSE file for details.

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

Built with ❤️ for the WA Government Digital Wallet initiative