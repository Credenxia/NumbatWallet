# Flutter SDK Documentation

**Version:** 1.0.0  
**POA Phase:** Week 1  
**Last Updated:** September 10, 2025

## Table of Contents
- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Core Components](#core-components)
- [API Reference](#api-reference)
- [UI Components](#ui-components)
- [Security](#security)
- [ServiceWA Integration](#servicewa-integration)
- [Testing](#testing)

## Overview

The NumbatWallet Flutter SDK provides a complete solution for integrating digital wallet and verifiable credential functionality into mobile applications. Designed specifically for integration with ServiceWA and other government mobile applications.

### Key Features
- 🔐 Secure credential storage with biometric protection
- 📱 Native iOS and Android support
- 🔄 Offline verification capabilities
- 📊 QR code generation and scanning
- 🎨 Customizable UI components
- 🔒 Hardware-backed key storage
- 📡 Real-time synchronization
- 🌐 Multi-language support

### Requirements
- Flutter SDK: >=3.10.0 <4.0.0
- Dart SDK: >=3.0.0 <4.0.0
- iOS: 13.0+
- Android: API 24+ (Android 7.0)

## Installation

### 1. Add Dependency

```yaml
# pubspec.yaml
dependencies:
  numbat_wallet_sdk: ^1.0.0
```

### 2. Platform-Specific Setup

#### iOS Setup
```xml
<!-- ios/Runner/Info.plist -->
<key>NSCameraUsageDescription</key>
<string>Camera access is required for QR code scanning</string>
<key>NSFaceIDUsageDescription</key>
<string>Face ID is used to protect your digital credentials</string>
```

#### Android Setup
```xml
<!-- android/app/src/main/AndroidManifest.xml -->
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.USE_BIOMETRIC" />
<uses-permission android:name="android.permission.INTERNET" />
```

```gradle
// android/app/build.gradle
android {
    defaultConfig {
        minSdkVersion 24
        targetSdkVersion 33
    }
}
```

### 3. Initialize SDK

```dart
import 'package:numbat_wallet_sdk/numbat_wallet_sdk.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Initialize the SDK
  await NumbatWallet.initialize(
    config: WalletConfig(
      apiBaseUrl: 'https://api.wallet.wa.gov.au',
      tenantId: 'wa_transport',
      environment: Environment.production,
      enableBiometrics: true,
      enableOfflineMode: true,
    ),
  );
  
  runApp(MyApp());
}
```

## Quick Start

### Basic Integration Example

```dart
import 'package:flutter/material.dart';
import 'package:numbat_wallet_sdk/numbat_wallet_sdk.dart';

class WalletScreen extends StatefulWidget {
  @override
  _WalletScreenState createState() => _WalletScreenState();
}

class _WalletScreenState extends State<WalletScreen> {
  final _wallet = NumbatWallet.instance;
  List<Credential> _credentials = [];
  
  @override
  void initState() {
    super.initState();
    _loadCredentials();
  }
  
  Future<void> _loadCredentials() async {
    try {
      // Authenticate user
      final authenticated = await _wallet.authenticate(
        method: AuthMethod.biometric,
      );
      
      if (authenticated) {
        // Fetch credentials
        final credentials = await _wallet.getCredentials();
        setState(() {
          _credentials = credentials;
        });
      }
    } catch (e) {
      print('Error loading credentials: $e');
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('My Digital Wallet'),
      ),
      body: ListView.builder(
        itemCount: _credentials.length,
        itemBuilder: (context, index) {
          final credential = _credentials[index];
          return CredentialCard(
            credential: credential,
            onTap: () => _showCredential(credential),
          );
        },
      ),
    );
  }
  
  void _showCredential(Credential credential) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => CredentialDetailScreen(credential: credential),
      ),
    );
  }
}
```

## Architecture

### SDK Structure

```
numbat_wallet_sdk/
├── lib/
│   ├── src/
│   │   ├── core/
│   │   │   ├── wallet.dart
│   │   │   ├── config.dart
│   │   │   └── exceptions.dart
│   │   ├── models/
│   │   │   ├── credential.dart
│   │   │   ├── wallet_holder.dart
│   │   │   └── presentation.dart
│   │   ├── services/
│   │   │   ├── api_service.dart
│   │   │   ├── storage_service.dart
│   │   │   ├── biometric_service.dart
│   │   │   └── qr_service.dart
│   │   ├── ui/
│   │   │   ├── widgets/
│   │   │   ├── screens/
│   │   │   └── themes/
│   │   └── utils/
│   │       ├── crypto.dart
│   │       └── validators.dart
│   └── numbat_wallet_sdk.dart
└── test/
```

### State Management

```dart
// Using Provider pattern for state management
class WalletProvider extends ChangeNotifier {
  final NumbatWallet _wallet = NumbatWallet.instance;
  
  List<Credential> _credentials = [];
  bool _isLoading = false;
  String? _error;
  
  List<Credential> get credentials => _credentials;
  bool get isLoading => _isLoading;
  String? get error => _error;
  
  Future<void> loadCredentials() async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    
    try {
      _credentials = await _wallet.getCredentials();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
  
  Future<void> refreshCredential(String credentialId) async {
    try {
      final updated = await _wallet.syncCredential(credentialId);
      final index = _credentials.indexWhere((c) => c.id == credentialId);
      if (index != -1) {
        _credentials[index] = updated;
        notifyListeners();
      }
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }
}
```

## Core Components

### Wallet Manager

```dart
class NumbatWallet {
  static NumbatWallet? _instance;
  static NumbatWallet get instance => _instance!;
  
  // Initialize wallet
  static Future<void> initialize({
    required WalletConfig config,
  }) async {
    _instance = NumbatWallet._internal(config);
    await _instance!._init();
  }
  
  // Authentication
  Future<bool> authenticate({
    required AuthMethod method,
    String? pin,
  }) async {
    switch (method) {
      case AuthMethod.biometric:
        return await _biometricService.authenticate();
      case AuthMethod.pin:
        return await _validatePin(pin!);
      case AuthMethod.pattern:
        throw UnimplementedError('Pattern auth not yet supported');
    }
  }
  
  // Credential Management
  Future<List<Credential>> getCredentials({
    CredentialFilter? filter,
  }) async {
    // Check local cache first
    var credentials = await _storage.getCredentials();
    
    // Sync with server if online
    if (await _isOnline()) {
      credentials = await _syncCredentials();
    }
    
    // Apply filter if provided
    if (filter != null) {
      credentials = _applyFilter(credentials, filter);
    }
    
    return credentials;
  }
  
  Future<Credential> getCredentialById(String id) async {
    final credential = await _storage.getCredential(id);
    if (credential == null) {
      throw CredentialNotFoundException('Credential not found: $id');
    }
    return credential;
  }
  
  // Presentation
  Future<PresentationToken> presentCredential({
    required String credentialId,
    required List<String> disclosedAttributes,
    String? verifierId,
  }) async {
    final credential = await getCredentialById(credentialId);
    
    // Create selective disclosure
    final presentation = PresentationBuilder()
      .setCredential(credential)
      .selectAttributes(disclosedAttributes)
      .setVerifier(verifierId)
      .build();
    
    // Generate cryptographic proof
    final proof = await _cryptoService.generateProof(presentation);
    
    // Log presentation
    await _apiService.logPresentation(
      credentialId: credentialId,
      verifierId: verifierId,
      attributes: disclosedAttributes,
    );
    
    return PresentationToken(
      presentation: presentation,
      proof: proof,
      qrCode: await _qrService.generate(presentation),
    );
  }
  
  // QR Code Operations
  Future<String> generateQRCode({
    required String credentialId,
    List<String>? attributes,
    Duration? validity,
  }) async {
    final credential = await getCredentialById(credentialId);
    
    final qrData = QRData(
      credentialId: credential.id,
      attributes: attributes ?? credential.attributeKeys,
      validUntil: validity != null 
        ? DateTime.now().add(validity)
        : DateTime.now().add(Duration(minutes: 5)),
      signature: await _cryptoService.sign(credential),
    );
    
    return _qrService.encode(qrData);
  }
  
  Future<VerificationResult> scanQRCode(String qrCode) async {
    try {
      final qrData = _qrService.decode(qrCode);
      
      // Verify signature
      final isValid = await _cryptoService.verify(
        data: qrData.credentialId,
        signature: qrData.signature,
      );
      
      if (!isValid) {
        return VerificationResult.invalid('Invalid signature');
      }
      
      // Check expiry
      if (qrData.validUntil.isBefore(DateTime.now())) {
        return VerificationResult.expired();
      }
      
      // Fetch and verify credential
      final result = await _apiService.verifyCredential(
        credentialId: qrData.credentialId,
        attributes: qrData.attributes,
      );
      
      return VerificationResult.success(result);
    } catch (e) {
      return VerificationResult.error(e.toString());
    }
  }
}
```

### Models

```dart
// Credential Model
class Credential {
  final String id;
  final String type;
  final CredentialStatus status;
  final Map<String, dynamic> subject;
  final DateTime issuedAt;
  final DateTime? expiresAt;
  final String issuer;
  final Map<String, dynamic> metadata;
  
  Credential({
    required this.id,
    required this.type,
    required this.status,
    required this.subject,
    required this.issuedAt,
    this.expiresAt,
    required this.issuer,
    this.metadata = const {},
  });
  
  bool get isExpired => 
    expiresAt != null && expiresAt!.isBefore(DateTime.now());
  
  bool get isActive => 
    status == CredentialStatus.active && !isExpired;
  
  List<String> get attributeKeys => subject.keys.toList();
  
  dynamic getAttribute(String key) => subject[key];
  
  factory Credential.fromJson(Map<String, dynamic> json) {
    return Credential(
      id: json['id'],
      type: json['type'],
      status: CredentialStatus.values.firstWhere(
        (s) => s.name == json['status'],
      ),
      subject: json['subject'],
      issuedAt: DateTime.parse(json['issuedAt']),
      expiresAt: json['expiresAt'] != null 
        ? DateTime.parse(json['expiresAt'])
        : null,
      issuer: json['issuer'],
      metadata: json['metadata'] ?? {},
    );
  }
}

// Credential Status
enum CredentialStatus {
  active,
  suspended,
  revoked,
  expired,
}

// Presentation Model
class PresentationToken {
  final String id;
  final Credential credential;
  final List<String> disclosedAttributes;
  final String proof;
  final String qrCode;
  final DateTime createdAt;
  final DateTime expiresAt;
  
  PresentationToken({
    required this.credential,
    required this.disclosedAttributes,
    required this.proof,
    required this.qrCode,
    DateTime? createdAt,
    DateTime? expiresAt,
  }) : 
    id = _generateId(),
    createdAt = createdAt ?? DateTime.now(),
    expiresAt = expiresAt ?? DateTime.now().add(Duration(minutes: 5));
  
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'credentialId': credential.id,
      'disclosed': disclosedAttributes,
      'proof': proof,
      'createdAt': createdAt.toIso8601String(),
      'expiresAt': expiresAt.toIso8601String(),
    };
  }
  
  static String _generateId() {
    return 'pres_${DateTime.now().millisecondsSinceEpoch}';
  }
}
```

## API Reference

### WalletConfig

```dart
class WalletConfig {
  final String apiBaseUrl;
  final String tenantId;
  final Environment environment;
  final bool enableBiometrics;
  final bool enableOfflineMode;
  final Duration syncInterval;
  final int maxRetries;
  final Duration timeout;
  
  const WalletConfig({
    required this.apiBaseUrl,
    required this.tenantId,
    this.environment = Environment.production,
    this.enableBiometrics = true,
    this.enableOfflineMode = true,
    this.syncInterval = const Duration(minutes: 15),
    this.maxRetries = 3,
    this.timeout = const Duration(seconds: 30),
  });
}
```

### API Service

```dart
class ApiService {
  final Dio _dio;
  final WalletConfig _config;
  final TokenManager _tokenManager;
  
  ApiService(this._config) : _dio = Dio() {
    _dio.options = BaseOptions(
      baseUrl: _config.apiBaseUrl,
      connectTimeout: _config.timeout,
      receiveTimeout: _config.timeout,
      headers: {
        'X-Tenant-Id': _config.tenantId,
        'X-SDK-Version': '1.0.0',
        'X-Platform': Platform.operatingSystem,
      },
    );
    
    // Add interceptors
    _dio.interceptors.add(AuthInterceptor(_tokenManager));
    _dio.interceptors.add(RetryInterceptor(maxRetries: _config.maxRetries));
    _dio.interceptors.add(LogInterceptor());
  }
  
  Future<List<Credential>> getCredentials() async {
    try {
      final response = await _dio.get('/v1/credentials');
      return (response.data['data'] as List)
        .map((json) => Credential.fromJson(json))
        .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }
  
  Future<Credential> getCredential(String id) async {
    try {
      final response = await _dio.get('/v1/credentials/$id');
      return Credential.fromJson(response.data['data']);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }
  
  Future<VerificationResult> verifyCredential({
    required String credentialId,
    required List<String> attributes,
  }) async {
    try {
      final response = await _dio.post(
        '/v1/credentials/$credentialId/verify',
        data: {
          'attributes': attributes,
          'timestamp': DateTime.now().toIso8601String(),
        },
      );
      
      return VerificationResult.fromJson(response.data);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }
  
  Exception _handleError(DioException error) {
    if (error.response != null) {
      switch (error.response!.statusCode) {
        case 401:
          return UnauthorizedException('Authentication required');
        case 403:
          return ForbiddenException('Access denied');
        case 404:
          return NotFoundException('Resource not found');
        case 422:
          return ValidationException(error.response!.data['errors']);
        case 429:
          return RateLimitException('Too many requests');
        default:
          return ServerException('Server error: ${error.response!.statusCode}');
      }
    }
    return NetworkException('Network error: ${error.message}');
  }
}
```

## UI Components

### Credential Card Widget

```dart
class CredentialCard extends StatelessWidget {
  final Credential credential;
  final VoidCallback? onTap;
  final bool showStatus;
  
  const CredentialCard({
    Key? key,
    required this.credential,
    this.onTap,
    this.showStatus = true,
  }) : super(key: key);
  
  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: EdgeInsets.all(16),
          child: Row(
            children: [
              _buildIcon(),
              SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      _getCredentialTitle(),
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    SizedBox(height: 4),
                    Text(
                      _getCredentialSubtitle(),
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                    if (showStatus) ...[
                      SizedBox(height: 8),
                      _buildStatusChip(context),
                    ],
                  ],
                ),
              ),
              Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
  
  Widget _buildIcon() {
    IconData icon;
    Color color;
    
    switch (credential.type) {
      case 'DriverLicense':
        icon = Icons.drive_eta;
        color = Colors.blue;
        break;
      case 'ProofOfAge':
        icon = Icons.cake;
        color = Colors.orange;
        break;
      case 'StudentCard':
        icon = Icons.school;
        color = Colors.green;
        break;
      default:
        icon = Icons.badge;
        color = Colors.grey;
    }
    
    return Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Icon(icon, color: color),
    );
  }
  
  Widget _buildStatusChip(BuildContext context) {
    Color color;
    String label;
    
    switch (credential.status) {
      case CredentialStatus.active:
        color = Colors.green;
        label = 'Active';
        break;
      case CredentialStatus.suspended:
        color = Colors.orange;
        label = 'Suspended';
        break;
      case CredentialStatus.revoked:
        color = Colors.red;
        label = 'Revoked';
        break;
      case CredentialStatus.expired:
        color = Colors.grey;
        label = 'Expired';
        break;
    }
    
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
  
  String _getCredentialTitle() {
    switch (credential.type) {
      case 'DriverLicense':
        return 'Driver License';
      case 'ProofOfAge':
        return 'Proof of Age Card';
      case 'StudentCard':
        return 'Student ID';
      default:
        return credential.type;
    }
  }
  
  String _getCredentialSubtitle() {
    final name = credential.subject['fullName'] ?? 
                 '${credential.subject['firstName']} ${credential.subject['lastName']}';
    return name ?? 'Digital Credential';
  }
}
```

### QR Scanner Screen

```dart
class QRScannerScreen extends StatefulWidget {
  final Function(String) onScan;
  
  const QRScannerScreen({
    Key? key,
    required this.onScan,
  }) : super(key: key);
  
  @override
  _QRScannerScreenState createState() => _QRScannerScreenState();
}

class _QRScannerScreenState extends State<QRScannerScreen> {
  final GlobalKey qrKey = GlobalKey(debugLabel: 'QR');
  QRViewController? controller;
  bool _isProcessing = false;
  
  @override
  void reassemble() {
    super.reassemble();
    if (Platform.isAndroid) {
      controller?.pauseCamera();
    } else if (Platform.isIOS) {
      controller?.resumeCamera();
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Scan QR Code'),
        backgroundColor: Colors.transparent,
        elevation: 0,
      ),
      extendBodyBehindAppBar: true,
      body: Stack(
        children: [
          QRView(
            key: qrKey,
            onQRViewCreated: _onQRViewCreated,
            overlay: QrScannerOverlayShape(
              borderColor: Theme.of(context).primaryColor,
              borderRadius: 10,
              borderLength: 30,
              borderWidth: 10,
              cutOutSize: 300,
            ),
          ),
          if (_isProcessing)
            Container(
              color: Colors.black54,
              child: Center(
                child: CircularProgressIndicator(),
              ),
            ),
        ],
      ),
    );
  }
  
  void _onQRViewCreated(QRViewController controller) {
    this.controller = controller;
    controller.scannedDataStream.listen((scanData) async {
      if (!_isProcessing && scanData.code != null) {
        setState(() => _isProcessing = true);
        
        // Haptic feedback
        HapticFeedback.mediumImpact();
        
        // Process QR code
        await widget.onScan(scanData.code!);
        
        // Close scanner
        Navigator.of(context).pop();
      }
    });
  }
  
  @override
  void dispose() {
    controller?.dispose();
    super.dispose();
  }
}
```

## Security

### Secure Storage

```dart
class SecureStorageService {
  static const _storage = FlutterSecureStorage();
  
  // Encryption options
  static const _encryptionOptions = IOSOptions(
    accessibility: IOSAccessibility.first_unlock_this_device,
  );
  
  static const _androidOptions = AndroidOptions(
    encryptedSharedPreferences: true,
  );
  
  // Store credential securely
  static Future<void> storeCredential(Credential credential) async {
    final encrypted = await _encryptCredential(credential);
    await _storage.write(
      key: 'credential_${credential.id}',
      value: encrypted,
      iOptions: _encryptionOptions,
      aOptions: _androidOptions,
    );
  }
  
  // Retrieve credential
  static Future<Credential?> getCredential(String id) async {
    final encrypted = await _storage.read(
      key: 'credential_$id',
      iOptions: _encryptionOptions,
      aOptions: _androidOptions,
    );
    
    if (encrypted == null) return null;
    
    return _decryptCredential(encrypted);
  }
  
  // Biometric protection
  static Future<bool> authenticateWithBiometrics() async {
    final LocalAuthentication auth = LocalAuthentication();
    
    // Check if biometrics available
    final bool canCheckBiometrics = await auth.canCheckBiometrics;
    if (!canCheckBiometrics) return false;
    
    // Authenticate
    try {
      final bool authenticated = await auth.authenticate(
        localizedReason: 'Authenticate to access your digital wallet',
        options: AuthenticationOptions(
          biometricOnly: true,
          stickyAuth: true,
        ),
      );
      
      return authenticated;
    } catch (e) {
      print('Biometric authentication error: $e');
      return false;
    }
  }
  
  // Encryption helpers
  static Future<String> _encryptCredential(Credential credential) async {
    // Implementation would use platform-specific encryption
    // iOS: Keychain Services
    // Android: Android Keystore
    return jsonEncode(credential.toJson()); // Simplified
  }
  
  static Future<Credential> _decryptCredential(String encrypted) async {
    // Implementation would decrypt using platform keys
    return Credential.fromJson(jsonDecode(encrypted)); // Simplified
  }
}
```

## ServiceWA Integration

### Integration Guide

```dart
// ServiceWA App Integration
class ServiceWAIntegration {
  // 1. Add to ServiceWA pubspec.yaml
  // dependencies:
  //   numbat_wallet_sdk: ^1.0.0
  
  // 2. Initialize in main.dart
  static Future<void> initializeWallet() async {
    await NumbatWallet.initialize(
      config: WalletConfig(
        apiBaseUrl: 'https://api.wallet.wa.gov.au',
        tenantId: 'service_wa',
        environment: Environment.production,
        enableBiometrics: true,
        enableOfflineMode: true,
      ),
    );
  }
  
  // 3. Add wallet section to ServiceWA
  static Widget buildWalletSection(BuildContext context) {
    return WalletSection(
      onCredentialTap: (credential) {
        // Navigate to ServiceWA credential viewer
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => ServiceWACredentialViewer(
              credential: credential,
            ),
          ),
        );
      },
    );
  }
  
  // 4. Handle deep links
  static void handleDeepLink(Uri uri) {
    if (uri.scheme == 'servicewa' && uri.host == 'wallet') {
      final action = uri.pathSegments.first;
      
      switch (action) {
        case 'credential':
          final credentialId = uri.pathSegments[1];
          _openCredential(credentialId);
          break;
        case 'verify':
          final qrCode = uri.queryParameters['code'];
          _verifyQRCode(qrCode!);
          break;
      }
    }
  }
  
  // 5. Share credentials with other ServiceWA features
  static Future<void> shareCredentialWithService({
    required String credentialId,
    required String serviceId,
  }) async {
    final wallet = NumbatWallet.instance;
    
    // Get credential
    final credential = await wallet.getCredentialById(credentialId);
    
    // Generate sharing token
    final token = await wallet.generateSharingToken(
      credentialId: credentialId,
      recipientId: serviceId,
      validity: Duration(minutes: 10),
    );
    
    // Send to service
    await ServiceWAAPI.shareCredential(
      serviceId: serviceId,
      token: token,
    );
  }
}
```

## Testing

### Unit Tests

```dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:numbat_wallet_sdk/numbat_wallet_sdk.dart';

class MockApiService extends Mock implements ApiService {}
class MockStorageService extends Mock implements StorageService {}

void main() {
  group('NumbatWallet', () {
    late NumbatWallet wallet;
    late MockApiService mockApi;
    late MockStorageService mockStorage;
    
    setUp(() {
      mockApi = MockApiService();
      mockStorage = MockStorageService();
      wallet = NumbatWallet.testInstance(
        apiService: mockApi,
        storageService: mockStorage,
      );
    });
    
    test('getCredentials returns cached credentials when offline', () async {
      // Arrange
      final cachedCredentials = [
        TestData.createCredential(id: '1'),
        TestData.createCredential(id: '2'),
      ];
      
      when(mockStorage.getCredentials())
        .thenAnswer((_) async => cachedCredentials);
      when(mockApi.isOnline())
        .thenAnswer((_) async => false);
      
      // Act
      final credentials = await wallet.getCredentials();
      
      // Assert
      expect(credentials, equals(cachedCredentials));
      verify(mockStorage.getCredentials()).called(1);
      verifyNever(mockApi.getCredentials());
    });
    
    test('presentCredential creates valid presentation', () async {
      // Arrange
      final credential = TestData.createCredential();
      final attributes = ['name', 'dateOfBirth'];
      
      when(mockStorage.getCredential(credential.id))
        .thenAnswer((_) async => credential);
      
      // Act
      final presentation = await wallet.presentCredential(
        credentialId: credential.id,
        disclosedAttributes: attributes,
      );
      
      // Assert
      expect(presentation.credential.id, equals(credential.id));
      expect(presentation.disclosedAttributes, equals(attributes));
      expect(presentation.proof, isNotEmpty);
      expect(presentation.qrCode, isNotEmpty);
    });
  });
}
```

### Widget Tests

```dart
void main() {
  testWidgets('CredentialCard displays credential information', 
    (WidgetTester tester) async {
    // Arrange
    final credential = Credential(
      id: 'cred_123',
      type: 'DriverLicense',
      status: CredentialStatus.active,
      subject: {
        'firstName': 'John',
        'lastName': 'Doe',
      },
      issuedAt: DateTime.now(),
      issuer: 'WA Transport',
    );
    
    // Act
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: CredentialCard(
            credential: credential,
            onTap: () {},
          ),
        ),
      ),
    );
    
    // Assert
    expect(find.text('Driver License'), findsOneWidget);
    expect(find.text('John Doe'), findsOneWidget);
    expect(find.text('Active'), findsOneWidget);
    expect(find.byIcon(Icons.drive_eta), findsOneWidget);
  });
}
```

### Integration Tests

```dart
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  
  group('Wallet Integration Tests', () {
    testWidgets('Complete credential flow', (WidgetTester tester) async {
      // Initialize app
      await tester.pumpWidget(TestApp());
      
      // Authenticate
      await tester.tap(find.byType(BiometricButton));
      await tester.pumpAndSettle();
      
      // Verify credentials loaded
      expect(find.byType(CredentialCard), findsWidgets);
      
      // Tap on credential
      await tester.tap(find.byType(CredentialCard).first);
      await tester.pumpAndSettle();
      
      // Verify detail screen
      expect(find.byType(CredentialDetailScreen), findsOneWidget);
      
      // Generate QR code
      await tester.tap(find.byIcon(Icons.qr_code));
      await tester.pumpAndSettle();
      
      // Verify QR displayed
      expect(find.byType(QrImage), findsOneWidget);
    });
  });
}
```

## Troubleshooting

### Common Issues

1. **Biometric authentication failing**
   - Ensure biometric permissions are granted
   - Check if device has biometric hardware
   - Fallback to PIN authentication

2. **QR scanning not working**
   - Check camera permissions
   - Ensure adequate lighting
   - Try different QR code distances

3. **Offline mode issues**
   - Verify local storage permissions
   - Check available device storage
   - Force sync when online

4. **ServiceWA integration problems**
   - Verify SDK version compatibility
   - Check tenant ID configuration
   - Review deep link setup

## Migration Guide

### From Existing Wallet
```dart
// Migration from existing wallet implementation
class WalletMigration {
  static Future<void> migrate() async {
    // 1. Export existing credentials
    final oldCredentials = await OldWallet.exportCredentials();
    
    // 2. Initialize NumbatWallet
    await NumbatWallet.initialize(config: WalletConfig(...));
    
    // 3. Import credentials
    for (final cred in oldCredentials) {
      await NumbatWallet.instance.importCredential(
        _mapOldCredentialToNew(cred),
      );
    }
    
    // 4. Clean up old wallet
    await OldWallet.clear();
  }
}
```

## Support

- **Documentation:** https://docs.wallet.wa.gov.au
- **API Reference:** https://api.wallet.wa.gov.au/docs
- **Sample App:** https://github.com/numbat/wallet-sample
- **Issues:** https://github.com/numbat/flutter-sdk/issues
- **Email:** sdk-support@wallet.wa.gov.au