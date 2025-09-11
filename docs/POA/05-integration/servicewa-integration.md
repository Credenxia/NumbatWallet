# ServiceWA Integration Guide

**Version:** 1.0  
**Date:** September 10, 2025  
**Phase:** Proof-of-Operation (POA)

## Table of Contents
- [Overview](#overview)
- [Integration Architecture](#integration-architecture)
- [Flutter SDK Integration](#flutter-sdk-integration)
- [Authentication Flow](#authentication-flow)
- [API Integration](#api-integration)
- [Deep Linking](#deep-linking)
- [Push Notifications](#push-notifications)
- [Error Handling](#error-handling)
- [Testing Strategy](#testing-strategy)
- [Migration Path](#migration-path)

## Overview

This document provides comprehensive guidance for integrating the NumbatWallet SDK with the ServiceWA mobile application, enabling digital credential management within the existing app infrastructure.

### Integration Objectives
- **Seamless Experience** - Native feel within ServiceWA
- **Minimal Disruption** - Non-breaking changes
- **Progressive Enhancement** - Gradual feature rollout
- **Backward Compatibility** - Support existing users
- **Performance** - <100ms SDK initialization

### Integration Scope
- Flutter SDK embedding
- Authentication bridge
- UI component integration
- State management
- Navigation flows
- Data synchronization

## Integration Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────┐
│                   ServiceWA App                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │              ServiceWA Native Code               │  │
│  │  ┌──────────────────────────────────────────┐  │  │
│  │  │         NumbatWallet Flutter SDK         │  │  │
│  │  │  ┌────────────────────────────────────┐  │  │  │
│  │  │  │    Credential Manager Widget       │  │  │  │
│  │  │  ├────────────────────────────────────┤  │  │  │
│  │  │  │    Verification Scanner           │  │  │  │
│  │  │  ├────────────────────────────────────┤  │  │  │
│  │  │  │    Secure Storage Service         │  │  │  │
│  │  │  └────────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
                  NumbatWallet Backend API
```

### SDK Dependencies

```yaml
dependencies:
  numbat_wallet_sdk: ^1.0.0
  
  # Required peer dependencies
  flutter_secure_storage: ^9.0.0
  local_auth: ^2.1.6
  qr_code_scanner: ^1.0.1
  dio: ^5.3.2
  json_annotation: ^4.8.1
  
  # Optional dependencies
  flutter_svg: ^2.0.7
  cached_network_image: ^3.2.3
  share_plus: ^7.1.0
```

## Flutter SDK Integration

### Step 1: SDK Initialization

```dart
// main.dart or app initialization
import 'package:numbat_wallet_sdk/numbat_wallet_sdk.dart';

class ServiceWAApp extends StatefulWidget {
  @override
  _ServiceWAAppState createState() => _ServiceWAAppState();
}

class _ServiceWAAppState extends State<ServiceWAApp> {
  late NumbatWalletSDK _walletSDK;
  
  @override
  void initState() {
    super.initState();
    _initializeWalletSDK();
  }
  
  Future<void> _initializeWalletSDK() async {
    try {
      _walletSDK = await NumbatWalletSDK.initialize(
        config: NumbatConfig(
          apiUrl: 'https://api.wallet.wa.gov.au',
          tenantId: 'servicewa-prod',
          environment: Environment.production,
          enableBiometrics: true,
          enableOfflineMode: true,
          logLevel: LogLevel.warning,
        ),
        authProvider: ServiceWAAuthProvider(), // Bridge to existing auth
      );
      
      // Register SDK with service locator
      GetIt.I.registerSingleton<NumbatWalletSDK>(_walletSDK);
      
      // Setup event listeners
      _walletSDK.events.listen(_handleWalletEvent);
      
    } catch (e) {
      logger.error('Failed to initialize wallet SDK', error: e);
      // Graceful degradation - wallet features disabled
    }
  }
  
  void _handleWalletEvent(WalletEvent event) {
    switch (event.type) {
      case WalletEventType.credentialAdded:
        _showNotification('New credential added to your wallet');
        break;
      case WalletEventType.verificationRequested:
        _navigateToVerification(event.data);
        break;
      case WalletEventType.syncCompleted:
        setState(() {
          _lastSyncTime = DateTime.now();
        });
        break;
    }
  }
}
```

### Step 2: Authentication Bridge

```dart
// Bridge between ServiceWA auth and NumbatWallet
class ServiceWAAuthProvider implements AuthProvider {
  final ServiceWAAuthService _authService;
  
  ServiceWAAuthProvider({
    required ServiceWAAuthService authService,
  }) : _authService = authService;
  
  @override
  Future<AuthToken?> getAccessToken() async {
    // Get existing ServiceWA token
    final serviceToken = await _authService.getCurrentToken();
    if (serviceToken == null) return null;
    
    // Exchange for wallet token
    final walletToken = await _exchangeToken(serviceToken);
    
    return AuthToken(
      accessToken: walletToken.accessToken,
      refreshToken: walletToken.refreshToken,
      expiresAt: walletToken.expiresAt,
      scope: 'wallet.read wallet.write',
    );
  }
  
  @override
  Future<AuthToken?> refreshToken(String refreshToken) async {
    try {
      final response = await dio.post(
        '/auth/refresh',
        data: {'refresh_token': refreshToken},
      );
      
      return AuthToken.fromJson(response.data);
    } catch (e) {
      // Fall back to full authentication
      return getAccessToken();
    }
  }
  
  @override
  Future<void> logout() async {
    await _authService.logout();
    await NumbatWalletSDK.instance.clearLocalData();
  }
  
  Future<WalletToken> _exchangeToken(ServiceToken token) async {
    final response = await dio.post(
      '/auth/exchange',
      headers: {'Authorization': 'Bearer ${token.value}'},
      data: {
        'grant_type': 'token_exchange',
        'subject_token': token.value,
        'subject_token_type': 'servicewa_access_token',
        'requested_token_type': 'wallet_access_token',
      },
    );
    
    return WalletToken.fromJson(response.data);
  }
}
```

### Step 3: UI Integration

```dart
// Wallet section in ServiceWA app
class WalletScreen extends StatefulWidget {
  @override
  _WalletScreenState createState() => _WalletScreenState();
}

class _WalletScreenState extends State<WalletScreen> {
  final _walletSDK = GetIt.I<NumbatWalletSDK>();
  List<Credential> _credentials = [];
  bool _isLoading = true;
  
  @override
  void initState() {
    super.initState();
    _loadCredentials();
  }
  
  Future<void> _loadCredentials() async {
    try {
      setState(() => _isLoading = true);
      
      final credentials = await _walletSDK.credentials.list();
      
      setState(() {
        _credentials = credentials;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      _showError('Failed to load credentials');
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Digital Wallet'),
        actions: [
          IconButton(
            icon: Icon(Icons.qr_code_scanner),
            onPressed: _openScanner,
          ),
          IconButton(
            icon: Icon(Icons.add),
            onPressed: _addCredential,
          ),
        ],
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : _buildCredentialList(),
    );
  }
  
  Widget _buildCredentialList() {
    if (_credentials.isEmpty) {
      return _buildEmptyState();
    }
    
    return RefreshIndicator(
      onRefresh: _loadCredentials,
      child: ListView.builder(
        itemCount: _credentials.length,
        itemBuilder: (context, index) {
          final credential = _credentials[index];
          
          // Use NumbatWallet UI component
          return CredentialCard(
            credential: credential,
            onTap: () => _showCredentialDetails(credential),
            onShare: () => _shareCredential(credential),
            onDelete: () => _deleteCredential(credential),
          );
        },
      ),
    );
  }
  
  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.account_balance_wallet, size: 64, color: Colors.grey),
          SizedBox(height: 16),
          Text('No credentials yet'),
          SizedBox(height: 8),
          ElevatedButton(
            onPressed: _addCredential,
            child: Text('Add your first credential'),
          ),
        ],
      ),
    );
  }
  
  void _showCredentialDetails(Credential credential) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => CredentialDetailScreen(
          credential: credential,
          walletSDK: _walletSDK,
        ),
      ),
    );
  }
}
```

### Step 4: Navigation Integration

```dart
// Deep navigation from ServiceWA to wallet features
class ServiceWARouter {
  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      case '/wallet':
        return MaterialPageRoute(builder: (_) => WalletScreen());
        
      case '/wallet/credential':
        final credentialId = settings.arguments as String;
        return MaterialPageRoute(
          builder: (_) => CredentialDetailScreen(credentialId: credentialId),
        );
        
      case '/wallet/verify':
        return MaterialPageRoute(builder: (_) => VerificationScreen());
        
      case '/wallet/add':
        final type = settings.arguments as CredentialType?;
        return MaterialPageRoute(
          builder: (_) => AddCredentialScreen(type: type),
        );
        
      default:
        return _existingServiceWARoutes(settings);
    }
  }
}
```

## Authentication Flow

### SSO Integration

```dart
class WalletAuthFlow {
  Future<bool> authenticateUser() async {
    // Step 1: Check ServiceWA session
    final hasSession = await ServiceWAAuth.hasValidSession();
    if (!hasSession) {
      // Redirect to ServiceWA login
      final result = await ServiceWAAuth.login();
      if (!result) return false;
    }
    
    // Step 2: Get ServiceWA token
    final serviceToken = await ServiceWAAuth.getToken();
    
    // Step 3: Exchange for wallet token
    final walletToken = await NumbatWalletSDK.auth.exchangeToken(
      serviceToken: serviceToken,
      scopes: ['wallet.read', 'wallet.write'],
    );
    
    // Step 4: Initialize wallet session
    await NumbatWalletSDK.auth.initSession(walletToken);
    
    // Step 5: Setup biometric auth (optional)
    if (await LocalAuthentication().canCheckBiometrics) {
      await _setupBiometricAuth();
    }
    
    return true;
  }
  
  Future<void> _setupBiometricAuth() async {
    final prefs = await SharedPreferences.getInstance();
    final hasSetup = prefs.getBool('biometric_setup') ?? false;
    
    if (!hasSetup) {
      final result = await showDialog<bool>(
        context: context,
        builder: (context) => BiometricSetupDialog(),
      );
      
      if (result == true) {
        await NumbatWalletSDK.security.enableBiometric();
        await prefs.setBool('biometric_setup', true);
      }
    }
  }
}
```

## API Integration

### Service Layer

```dart
// Wrapper service for ServiceWA integration
class WalletIntegrationService {
  final NumbatWalletSDK _sdk;
  final ServiceWAApi _serviceApi;
  
  // Fetch and sync credentials
  Future<List<Credential>> syncCredentials() async {
    try {
      // Get credentials from wallet
      final walletCreds = await _sdk.credentials.list();
      
      // Get linked services from ServiceWA
      final linkedServices = await _serviceApi.getLinkedServices();
      
      // Merge and reconcile
      final merged = _mergeCredentials(walletCreds, linkedServices);
      
      // Update local cache
      await _updateLocalCache(merged);
      
      return merged;
    } catch (e) {
      logger.error('Credential sync failed', error: e);
      // Return cached credentials
      return _getCachedCredentials();
    }
  }
  
  // Issue new credential through ServiceWA
  Future<Credential> issueCredential(CredentialRequest request) async {
    // Validate eligibility through ServiceWA
    final eligible = await _serviceApi.checkEligibility(
      userId: currentUser.id,
      credentialType: request.type,
    );
    
    if (!eligible) {
      throw CredentialException('Not eligible for ${request.type}');
    }
    
    // Request credential from wallet API
    final credential = await _sdk.credentials.request(
      type: request.type,
      claims: request.claims,
      evidence: request.evidence,
    );
    
    // Update ServiceWA records
    await _serviceApi.recordCredentialIssuance(
      userId: currentUser.id,
      credentialId: credential.id,
      type: credential.type,
    );
    
    return credential;
  }
}
```

### Event Synchronization

```dart
// Keep ServiceWA and Wallet in sync
class SyncManager {
  final StreamController<SyncEvent> _syncController = StreamController.broadcast();
  Timer? _syncTimer;
  
  void startSync() {
    // Initial sync
    _performSync();
    
    // Periodic sync every 5 minutes
    _syncTimer = Timer.periodic(Duration(minutes: 5), (_) {
      _performSync();
    });
    
    // Listen for wallet events
    NumbatWalletSDK.instance.events.listen((event) {
      if (event.requiresSync) {
        _performSync();
      }
    });
    
    // Listen for ServiceWA events
    ServiceWAEventBus.instance.listen((event) {
      if (event.type == ServiceEventType.profileUpdated) {
        _syncProfile();
      }
    });
  }
  
  Future<void> _performSync() async {
    _syncController.add(SyncEvent.started());
    
    try {
      // Sync credentials
      await _syncCredentials();
      
      // Sync profile
      await _syncProfile();
      
      // Sync preferences
      await _syncPreferences();
      
      _syncController.add(SyncEvent.completed());
    } catch (e) {
      _syncController.add(SyncEvent.failed(e));
    }
  }
}
```

## Deep Linking

### URL Scheme Registration

```xml
<!-- iOS: Info.plist -->
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>servicewa</string>
            <string>numbat-wallet</string>
        </array>
    </dict>
</array>

<!-- Android: AndroidManifest.xml -->
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    
    <data android:scheme="servicewa" />
    <data android:scheme="numbat-wallet" />
    <data android:host="wallet" />
</intent-filter>
```

### Deep Link Handler

```dart
class DeepLinkHandler {
  void handleDeepLink(String url) {
    final uri = Uri.parse(url);
    
    switch (uri.host) {
      case 'wallet':
        _handleWalletDeepLink(uri);
        break;
      case 'verify':
        _handleVerificationDeepLink(uri);
        break;
      case 'credential':
        _handleCredentialDeepLink(uri);
        break;
      default:
        // Let ServiceWA handle
        ServiceWADeepLinkHandler.handle(url);
    }
  }
  
  void _handleWalletDeepLink(Uri uri) {
    final action = uri.pathSegments.firstOrNull;
    
    switch (action) {
      case 'add':
        final type = uri.queryParameters['type'];
        Navigator.pushNamed(context, '/wallet/add', arguments: type);
        break;
        
      case 'share':
        final credentialId = uri.queryParameters['id'];
        _shareCredential(credentialId);
        break;
        
      case 'import':
        final data = uri.queryParameters['data'];
        _importCredential(data);
        break;
    }
  }
}
```

## Push Notifications

### Notification Integration

```dart
class WalletNotificationService {
  final FirebaseMessaging _messaging = FirebaseMessaging.instance;
  
  Future<void> initialize() async {
    // Request permissions
    final settings = await _messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );
    
    if (settings.authorizationStatus == AuthorizationStatus.authorized) {
      // Get FCM token
      final token = await _messaging.getToken();
      
      // Register with wallet service
      await NumbatWalletSDK.instance.notifications.registerDevice(
        token: token,
        platform: Platform.operatingSystem,
      );
      
      // Handle messages
      FirebaseMessaging.onMessage.listen(_handleMessage);
      FirebaseMessaging.onMessageOpenedApp.listen(_handleMessageTap);
    }
  }
  
  void _handleMessage(RemoteMessage message) {
    final data = message.data;
    
    if (data['source'] == 'numbat_wallet') {
      final type = data['type'];
      
      switch (type) {
        case 'credential_issued':
          _showCredentialNotification(data);
          break;
          
        case 'verification_request':
          _showVerificationRequest(data);
          break;
          
        case 'credential_expiring':
          _showExpiryWarning(data);
          break;
      }
    }
  }
  
  void _showCredentialNotification(Map<String, dynamic> data) {
    showLocalNotification(
      title: 'New Credential Available',
      body: 'Your ${data['credential_type']} is ready',
      payload: {
        'action': 'open_credential',
        'credential_id': data['credential_id'],
      },
    );
  }
}
```

## Error Handling

### Integration Error Recovery

```dart
class IntegrationErrorHandler {
  static Future<T> handleIntegrationCall<T>(
    Future<T> Function() call, {
    int maxRetries = 3,
    Duration retryDelay = const Duration(seconds: 1),
  }) async {
    int attempts = 0;
    
    while (attempts < maxRetries) {
      try {
        return await call();
      } catch (e) {
        attempts++;
        
        if (e is WalletException) {
          return _handleWalletError(e);
        } else if (e is ServiceWAException) {
          return _handleServiceError(e);
        } else if (e is NetworkException) {
          if (attempts < maxRetries) {
            await Future.delayed(retryDelay * attempts);
            continue;
          }
        }
        
        // Log error
        logger.error('Integration call failed', error: e);
        
        // Show user-friendly error
        if (attempts >= maxRetries) {
          showErrorDialog(
            title: 'Connection Error',
            message: 'Unable to connect to wallet service. Please try again.',
          );
        }
        
        rethrow;
      }
    }
    
    throw MaxRetriesExceededException();
  }
  
  static T _handleWalletError<T>(WalletException error) {
    switch (error.code) {
      case 'UNAUTHORIZED':
        // Re-authenticate
        ServiceWAAuth.refreshSession();
        throw RetryableException();
        
      case 'TENANT_NOT_FOUND':
        // Configuration error
        throw ConfigurationException('Wallet service not configured');
        
      default:
        throw error;
    }
  }
}
```

### Graceful Degradation

```dart
class WalletFeatureFlags {
  static bool isWalletEnabled = true;
  static bool isOfflineModeEnabled = true;
  static bool isBiometricEnabled = true;
  
  static Future<void> checkFeatureAvailability() async {
    try {
      // Check wallet service health
      final health = await NumbatWalletSDK.instance.checkHealth();
      isWalletEnabled = health.isHealthy;
      
      // Check offline capability
      isOfflineModeEnabled = await _checkOfflineCapability();
      
      // Check biometric support
      isBiometricEnabled = await LocalAuthentication().canCheckBiometrics;
      
    } catch (e) {
      // Disable wallet features on initialization failure
      isWalletEnabled = false;
      logger.error('Wallet features disabled', error: e);
    }
  }
  
  static Widget conditionalWalletFeature({
    required Widget child,
    Widget? fallback,
  }) {
    if (isWalletEnabled) {
      return child;
    }
    return fallback ?? SizedBox.shrink();
  }
}
```

## Testing Strategy

### Integration Tests

```dart
// Integration test suite
void main() {
  group('ServiceWA Wallet Integration', () {
    late ServiceWAApp app;
    late NumbatWalletSDK sdk;
    
    setUp(() async {
      app = await ServiceWATestHelper.createApp();
      sdk = await NumbatWalletSDK.initialize(
        config: testConfig,
        authProvider: MockAuthProvider(),
      );
    });
    
    testWidgets('Should display wallet section', (tester) async {
      await tester.pumpWidget(app);
      await tester.pumpAndSettle();
      
      // Navigate to wallet
      await tester.tap(find.text('Wallet'));
      await tester.pumpAndSettle();
      
      // Verify wallet screen
      expect(find.byType(WalletScreen), findsOneWidget);
      expect(find.text('Digital Wallet'), findsOneWidget);
    });
    
    testWidgets('Should sync credentials', (tester) async {
      // Mock credentials
      when(sdk.credentials.list()).thenAnswer(
        (_) async => [mockDriverLicense, mockProofOfAge],
      );
      
      await tester.pumpWidget(app);
      await tester.navigateToWallet();
      
      // Wait for sync
      await tester.pumpAndSettle();
      
      // Verify credentials displayed
      expect(find.byType(CredentialCard), findsNWidgets(2));
      expect(find.text('Driver Licence'), findsOneWidget);
      expect(find.text('Proof of Age'), findsOneWidget);
    });
  });
}
```

### Mock Service

```dart
class MockWalletService implements WalletService {
  final List<Credential> _credentials = [];
  
  @override
  Future<List<Credential>> listCredentials() async {
    await Future.delayed(Duration(milliseconds: 100)); // Simulate network
    return _credentials;
  }
  
  @override
  Future<Credential> issueCredential(CredentialRequest request) async {
    final credential = Credential(
      id: Uuid().v4(),
      type: request.type,
      status: CredentialStatus.active,
      claims: request.claims,
      issuedAt: DateTime.now(),
    );
    
    _credentials.add(credential);
    return credential;
  }
  
  @override
  Future<VerificationResult> verifyCredential(String credentialId) async {
    final credential = _credentials.firstWhere((c) => c.id == credentialId);
    
    return VerificationResult(
      isValid: credential.status == CredentialStatus.active,
      credential: credential,
      verifiedAt: DateTime.now(),
    );
  }
}
```

## Migration Path

### Phased Rollout

```yaml
Phase 1 - Foundation (Week 1):
  - SDK integration in development branch
  - Basic authentication bridge
  - Feature flag setup
  - Internal testing

Phase 2 - Core Features (Week 2):
  - Wallet screen implementation
  - Credential listing
  - QR scanner integration
  - UAT deployment

Phase 3 - Advanced Features (Week 3):
  - Offline verification
  - Biometric authentication
  - Push notifications
  - Beta testing

Phase 4 - Production Rollout (Week 4-5):
  - Gradual rollout (5% → 25% → 50% → 100%)
  - Performance monitoring
  - Error tracking
  - User feedback collection
```

### Rollback Plan

```dart
class WalletRollback {
  static Future<void> disableWalletFeatures() async {
    try {
      // Disable feature flags
      await RemoteConfig.instance.setDefaults({
        'wallet_enabled': false,
        'wallet_ui_visible': false,
      });
      
      // Clear local wallet data
      await NumbatWalletSDK.instance.clearLocalData();
      
      // Remove wallet navigation
      ServiceWARouter.removeRoute('/wallet');
      
      // Notify users
      showInfoBanner(
        'Wallet features are temporarily unavailable. We apologize for the inconvenience.',
      );
      
      // Log rollback
      await Analytics.logEvent('wallet_rollback', {
        'reason': 'manual_trigger',
        'timestamp': DateTime.now().toIso8601String(),
      });
      
    } catch (e) {
      logger.error('Rollback failed', error: e);
    }
  }
}
```

## Performance Optimization

### Lazy Loading

```dart
class LazyWalletLoader {
  static NumbatWalletSDK? _instance;
  static final Completer<NumbatWalletSDK> _completer = Completer();
  
  static Future<NumbatWalletSDK> getInstance() async {
    if (_instance != null) return _instance!;
    
    if (!_completer.isCompleted) {
      // Initialize in background
      _initializeInBackground();
    }
    
    return _completer.future;
  }
  
  static void _initializeInBackground() async {
    try {
      _instance = await NumbatWalletSDK.initialize(
        config: await _loadConfig(),
        authProvider: ServiceWAAuthProvider(),
      );
      
      _completer.complete(_instance);
    } catch (e) {
      _completer.completeError(e);
    }
  }
  
  static void preload() {
    // Trigger initialization early
    getInstance();
  }
}
```

## Support Resources

### Integration Checklist

```markdown
## Pre-Integration
- [ ] SDK package available in repository
- [ ] Authentication endpoint configured
- [ ] Test environment provisioned
- [ ] Feature flags configured

## Integration Steps
- [ ] Add SDK dependency
- [ ] Initialize SDK in app startup
- [ ] Implement auth bridge
- [ ] Add wallet navigation
- [ ] Integrate UI components
- [ ] Setup error handling
- [ ] Configure deep links
- [ ] Setup push notifications

## Testing
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] UAT sign-off
- [ ] Performance benchmarks met
- [ ] Security scan completed

## Deployment
- [ ] Feature flags enabled for test
- [ ] Monitoring configured
- [ ] Rollback plan tested
- [ ] Documentation updated
- [ ] Support team trained
```

### Contact Support

- **Technical Issues**: sdk-support@numbat.com
- **Integration Help**: integration@numbat.com
- **Emergency**: +61 1800 NUMBAT (24/7 during POA)