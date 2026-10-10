import 'dart:async';

import 'package:bcrypt/bcrypt.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/checkin_provider.dart';

// ---------------------------------------------------------------------------
// Kiosk state
// ---------------------------------------------------------------------------
enum _KioskState { idle, loading, success, failure }

class _KioskData {
  final _KioskState state;
  final String? memberName;
  final String? memberPhotoUrl;
  final String? errorMessage;
  final bool isOffline;

  const _KioskData({
    required this.state,
    this.memberName,
    this.memberPhotoUrl,
    this.errorMessage,
    this.isOffline = false,
  });

  static const _KioskData idle = _KioskData(state: _KioskState.idle);
}

/// Returns true if the exception represents a no-connectivity failure.
bool _isConnectivityError(Object e) {
  if (e is ApiException) {
    return e.errorCode == 'NETWORK_ERROR' ||
        e.errorCode == 'TIMEOUT' ||
        e.statusCode == null;
  }
  if (e is DioException) {
    return e.type == DioExceptionType.connectionError ||
        e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.sendTimeout ||
        e.type == DioExceptionType.receiveTimeout;
  }
  return false;
}

// ---------------------------------------------------------------------------
// Kiosk Screen  (full screen, no navigation bar, no status bar)
// ---------------------------------------------------------------------------
class CheckInKioskScreen extends ConsumerStatefulWidget {
  const CheckInKioskScreen({super.key});

  @override
  ConsumerState<CheckInKioskScreen> createState() => _CheckInKioskScreenState();
}

class _CheckInKioskScreenState extends ConsumerState<CheckInKioskScreen>
    with TickerProviderStateMixin {
  int _tabIndex = 0; // 0 = QR, 1 = Numpad
  _KioskData _kioskData = _KioskData.idle;
  Timer? _resetTimer;
  bool _qrProcessing = false;

  // Animation controllers
  late AnimationController _successAnimController;
  late AnimationController _errorAnimController;
  late Animation<double> _scaleAnim;
  late Animation<double> _errorScaleAnim;

  // Offline queue sync
  late OfflineQueueService _offlineQueue;
  StreamSubscription<bool>? _connectivitySub;
  final ConnectivityService _connectivityService = ConnectivityService();

  // Kiosk PIN state (null hash + isSet=false ⇒ no exit lock)
  bool _kioskPinIsSet = true; // pessimistic until we learn otherwise
  String? _kioskPinHash;

  @override
  void initState() {
    super.initState();
    _enterKioskMode();
    _loadKioskPinState();

    _successAnimController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 400),
    );
    _errorAnimController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 400),
    );
    _scaleAnim = Tween<double>(begin: 0.5, end: 1.0).animate(
      CurvedAnimation(parent: _successAnimController, curve: Curves.elasticOut),
    );
    _errorScaleAnim = Tween<double>(begin: 0.5, end: 1.0).animate(
      CurvedAnimation(parent: _errorAnimController, curve: Curves.elasticOut),
    );

    _initOfflineQueue();
  }

  Future<void> _initOfflineQueue() async {
    _offlineQueue = OfflineQueueService();
    await _offlineQueue.initialize();
    await _connectivityService.initialize();

    // Sync queued check-ins whenever connectivity is restored.
    _connectivitySub = _connectivityService.onConnectivityChanged.listen(
      (isConnected) {
        if (isConnected) _syncOfflineCheckIns();
      },
    );
  }

  @override
  void dispose() {
    _exitKioskMode();
    _resetTimer?.cancel();
    _successAnimController.dispose();
    _errorAnimController.dispose();
    _connectivitySub?.cancel();
    _connectivityService.dispose();
    super.dispose();
  }

  void _enterKioskMode() {
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
    SystemChrome.setPreferredOrientations([
      DeviceOrientation.landscapeLeft,
      DeviceOrientation.landscapeRight,
      DeviceOrientation.portraitUp,
      DeviceOrientation.portraitDown,
    ]);
  }

  void _exitKioskMode() {
    SystemChrome.setEnabledSystemUIMode(
      SystemUiMode.manual,
      overlays: SystemUiOverlay.values,
    );
  }

  // ---------------------------------------------------------------------------
  // QR Check-in
  // ---------------------------------------------------------------------------
  Future<void> _onQrDetected(String qrData) async {
    if (_qrProcessing || _kioskData.state != _KioskState.idle) return;
    _qrProcessing = true;
    setState(() => _kioskData = _KioskData(state: _KioskState.loading));

    final gymId =
        ref.read(currentGymIdProvider);

    try {
      final apiClient = ref.read(apiClientProvider);
      final staffId = ref.read(authProvider).valueOrNull?.user?.id;
      final result = await apiClient.post<CheckInResultModel>(
        ApiEndpoints.checkIn(gymId),
        data: {
          'method': 'QR',
          'qrToken': qrData,
          if (staffId != null) 'staffId': staffId,
        },
        parser: (data) =>
            CheckInResultModel.fromJson(data as Map<String, dynamic>),
      );
      _handleResult(result);
    } catch (e) {
      if (_isConnectivityError(e)) {
        await _queueCheckIn(
          gymId: gymId,
          identifier: qrData,
          identifierType: 'qr',
        );
        _handleOfflineSuccess();
      } else {
        _handleError(e is ApiException ? e.message : e.toString());
      }
    } finally {
      _qrProcessing = false;
    }
  }

  // ---------------------------------------------------------------------------
  // Numpad Check-in
  // ---------------------------------------------------------------------------
  Future<void> _onNumpadSubmit(String number) async {
    if (_kioskData.state != _KioskState.idle) return;
    setState(() => _kioskData = _KioskData(state: _KioskState.loading));

    final gymId =
        ref.read(currentGymIdProvider);

    try {
      final apiClient = ref.read(apiClientProvider);
      final staffId = ref.read(authProvider).valueOrNull?.user?.id;
      final result = await apiClient.post<CheckInResultModel>(
        ApiEndpoints.checkIn(gymId),
        data: {
          'method': 'MemberNumber',
          'memberNumber': number,
          if (staffId != null) 'staffId': staffId,
        },
        parser: (data) =>
            CheckInResultModel.fromJson(data as Map<String, dynamic>),
      );
      _handleResult(result);
    } catch (e) {
      if (_isConnectivityError(e)) {
        await _queueCheckIn(
          gymId: gymId,
          identifier: number,
          identifierType: 'number',
        );
        _handleOfflineSuccess();
      } else {
        _handleError(e is ApiException ? e.message : e.toString());
      }
    }
  }

  // ---------------------------------------------------------------------------
  // Offline queue helpers
  // ---------------------------------------------------------------------------
  Future<void> _queueCheckIn({
    required String gymId,
    required String identifier,
    required String identifierType,
  }) async {
    final staffId = ref.read(authProvider).valueOrNull?.user?.id;
    final op = OfflineOperation(
      id: DateTime.now().microsecondsSinceEpoch.toString(),
      method: 'POST',
      path: ApiEndpoints.checkIn(gymId),
      body: {
        'method': identifierType == 'qr' ? 'QR' : 'MemberNumber',
        if (identifierType == 'qr') 'qrToken': identifier,
        if (identifierType != 'qr') 'memberNumber': identifier,
        if (staffId != null) 'staffId': staffId,
      },
      timestamp: DateTime.now(),
      resourceType: 'check_in',
      resourceId: gymId,
    );
    await _offlineQueue.enqueue(op);
  }

  void _handleOfflineSuccess() {
    if (!mounted) return;
    setState(() => _kioskData = _KioskData(
          state: _KioskState.success,
          isOffline: true,
        ));
    _successAnimController.forward(from: 0);
    _scheduleReset(const Duration(seconds: 5));
  }

  Future<void> _syncOfflineCheckIns() async {
    final pending = _offlineQueue
        .getPendingOperations()
        .where((op) => op.resourceType == 'check_in')
        .toList();

    if (pending.isEmpty) return;

    final apiClient = ref.read(apiClientProvider);

    for (final op in pending) {
      try {
        await apiClient.post<CheckInResultModel>(
          op.path,
          data: op.body,
          parser: (data) =>
              CheckInResultModel.fromJson(data as Map<String, dynamic>),
        );
        await _offlineQueue.removeOperation(op.id);
      } catch (_) {
        // Leave in queue for next connectivity restore.
      }
    }

    if (mounted) {
      ref.invalidate(checkInLogProvider);
    }
  }

  void _handleResult(CheckInResultModel result) {
    if (!mounted) return;
    if (result.success) {
      setState(() => _kioskData = _KioskData(
            state: _KioskState.success,
            memberName: result.memberName,
            memberPhotoUrl: result.memberPhotoUrl,
          ));
      _successAnimController.forward(from: 0);
      ref.invalidate(checkInLogProvider);
      _scheduleReset(const Duration(seconds: 5));
    } else {
      _handleError(result.errorMessage ?? 'Unknown error');
    }
  }

  void _handleError(String message) {
    if (!mounted) return;
    setState(() => _kioskData = _KioskData(
          state: _KioskState.failure,
          errorMessage: message,
        ));
    _errorAnimController.forward(from: 0);
    _scheduleReset(const Duration(seconds: 3));
  }

  void _scheduleReset(Duration delay) {
    _resetTimer?.cancel();
    _resetTimer = Timer(delay, () {
      if (mounted) {
        setState(() => _kioskData = _KioskData.idle);
      }
    });
  }

  // ---------------------------------------------------------------------------
  // Exit PIN — server-verified, with a cached BCrypt hash for offline fallback.
  // ---------------------------------------------------------------------------
  /// Loads the gym's kiosk PIN state. Tries the server first and caches the
  /// hash in secure storage; on network failure falls back to the last cached
  /// hash so the kiosk can still be exited offline.
  Future<void> _loadKioskPinState() async {
    final gymId = ref.read(currentGymIdProvider);
    final storage = ref.read(secureStorageProvider);
    final cacheKey = StorageKeys.kioskPinHashFor(gymId);

    try {
      final apiClient = ref.read(apiClientProvider);
      final response = await apiClient.dio.get(
        ApiEndpoints.kioskPin(gymId),
        options: Options(
          receiveTimeout: const Duration(seconds: 3),
          sendTimeout: const Duration(seconds: 3),
        ),
      );
      final data = (response.data['data'] ?? response.data) as Map<String, dynamic>;
      final isSet = data['isSet'] as bool? ?? false;
      final hash = data['hash'] as String?;
      if (isSet && hash != null) {
        await storage.write(cacheKey, hash);
      } else {
        await storage.deleteKey(cacheKey);
      }
      if (!mounted) return;
      setState(() {
        _kioskPinIsSet = isSet;
        _kioskPinHash = hash;
      });
    } catch (_) {
      // Offline / timeout / error: fall back to cached hash.
      final cached = await storage.read(cacheKey);
      if (!mounted) return;
      setState(() {
        _kioskPinIsSet = cached != null;
        _kioskPinHash = cached;
      });
    }
  }

  /// Verifies a PIN against the server with a short timeout, falling back to
  /// BCrypt.checkpw against the cached hash if the network is unreachable.
  Future<bool> _verifyPin(String pin) async {
    final gymId = ref.read(currentGymIdProvider);
    try {
      final apiClient = ref.read(apiClientProvider);
      final response = await apiClient.dio.post(
        ApiEndpoints.kioskPinVerify(gymId),
        data: {'pin': pin},
        options: Options(
          receiveTimeout: const Duration(seconds: 3),
          sendTimeout: const Duration(seconds: 3),
        ),
      );
      final data = (response.data['data'] ?? response.data) as Map<String, dynamic>;
      return data['verified'] as bool? ?? false;
    } catch (e) {
      // Only fall back for connectivity / timeout errors. Anything else
      // (400/401/403 etc.) means the server answered — don't bypass it.
      if (!_isConnectivityError(e) || _kioskPinHash == null) rethrow;
      return BCrypt.checkpw(pin, _kioskPinHash!);
    }
  }

  Future<void> _showExitPinDialog() async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    // No PIN configured ⇒ exit without prompting.
    if (!_kioskPinIsSet) {
      _exitKioskMode();
      if (mounted) context.go(RoutePaths.dashboard);
      return;
    }

    final pinController = TextEditingController();
    String? error;
    bool verifying = false;

    await showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(isArabic ? 'تاكيد الخروج' : 'Exit Kiosk'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                isArabic
                    ? 'ادخل رقم PIN للخروج من وضع الكيوسك'
                    : 'Enter PIN to exit kiosk mode',
              ),
              const SizedBox(height: 12),
              TextField(
                controller: pinController,
                obscureText: true,
                keyboardType: TextInputType.number,
                maxLength: 8,
                enabled: !verifying,
                decoration: InputDecoration(
                  hintText: '••••',
                  errorText: error,
                  counterText: '',
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed:
                  verifying ? null : () => Navigator.of(ctx).pop(),
              child: Text(isArabic ? 'الغاء' : 'Cancel'),
            ),
            TextButton(
              onPressed: verifying
                  ? null
                  : () async {
                      setState(() {
                        verifying = true;
                        error = null;
                      });
                      try {
                        final ok = await _verifyPin(pinController.text);
                        if (ok) {
                          if (!ctx.mounted) return;
                          Navigator.of(ctx).pop();
                          _exitKioskMode();
                          if (mounted) context.go(RoutePaths.dashboard);
                          return;
                        }
                        setState(() {
                          verifying = false;
                          error = isArabic ? 'PIN غير صحيح' : 'Incorrect PIN';
                        });
                      } catch (_) {
                        setState(() {
                          verifying = false;
                          error = isArabic
                              ? 'تعذر التحقق. لا يوجد اتصال.'
                              : 'Verification failed. Check your connection.';
                        });
                      }
                    },
              child: verifying
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(isArabic ? 'خروج' : 'Exit'),
            ),
          ],
        ),
      ),
    );
  }

  // ---------------------------------------------------------------------------
  // Build
  // ---------------------------------------------------------------------------
  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    // Overlay when in loading/success/failure state
    if (_kioskData.state != _KioskState.idle) {
      return PopScope(
        canPop: false,
        child: _KioskResultOverlay(
          kioskData: _kioskData,
          isArabic: isArabic,
          successAnim: _scaleAnim,
          errorAnim: _errorScaleAnim,
        ),
      );
    }

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) _showExitPinDialog();
      },
      child: Scaffold(
        backgroundColor: const Color(0xFF0D0D0D),
        body: SafeArea(
        child: Stack(
          children: [
            Column(
              children: [
                // Header
                _KioskHeader(
                  isArabic: isArabic,
                  onExit: _showExitPinDialog,
                ),

                // Tab bar
                _KioskTabBar(
                  selectedIndex: _tabIndex,
                  isArabic: isArabic,
                  onTabChanged: (i) => setState(() => _tabIndex = i),
                ),

                // Content
                Expanded(
                  child: _tabIndex == 0
                      ? _QrScanTab(onQrDetected: _onQrDetected)
                      : _NumpadTab(onSubmit: _onNumpadSubmit, isArabic: isArabic),
                ),
              ],
            ),
          ],
        ),
      ),
    ),
    );
  }
}

// ---------------------------------------------------------------------------
// Header
// ---------------------------------------------------------------------------
class _KioskHeader extends StatelessWidget {
  final bool isArabic;
  final VoidCallback onExit;

  const _KioskHeader({required this.isArabic, required this.onExit});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
      color: const Color(0xFF1A1A1A),
      child: Row(
        children: [
          const Icon(Icons.fitness_center, color: Colors.white, size: 28),
          const SizedBox(width: 12),
          Text(
            isArabic ? 'تسجيل الحضور' : 'Check-In',
            style: const TextStyle(
              color: Colors.white,
              fontSize: 20,
              fontWeight: FontWeight.w700,
            ),
          ),
          const Spacer(),
          GestureDetector(
            onTap: onExit,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                border: Border.all(color: Colors.white24),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.exit_to_app, color: Colors.white54, size: 16),
                  const SizedBox(width: 4),
                  Text(
                    isArabic ? 'خروج' : 'Exit',
                    style: const TextStyle(color: Colors.white54, fontSize: 13),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Tab Bar
// ---------------------------------------------------------------------------
class _KioskTabBar extends StatelessWidget {
  final int selectedIndex;
  final bool isArabic;
  final ValueChanged<int> onTabChanged;

  const _KioskTabBar({
    required this.selectedIndex,
    required this.isArabic,
    required this.onTabChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFF1A1A1A),
      child: Row(
        children: [
          _Tab(
            icon: Icons.qr_code_scanner,
            label: isArabic ? 'QR Code' : 'QR Scan',
            selected: selectedIndex == 0,
            onTap: () => onTabChanged(0),
          ),
          _Tab(
            icon: Icons.dialpad,
            label: isArabic ? 'رقم العضو' : 'Member Number',
            selected: selectedIndex == 1,
            onTap: () => onTabChanged(1),
          ),
        ],
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _Tab({
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 12),
          decoration: BoxDecoration(
            border: Border(
              bottom: BorderSide(
                color: selected
                    ? const Color(0xFFC41E3A)
                    : Colors.transparent,
                width: 3,
              ),
            ),
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon,
                  color: selected ? Colors.white : Colors.white38, size: 20),
              const SizedBox(width: 8),
              Text(
                label,
                style: TextStyle(
                  color: selected ? Colors.white : Colors.white38,
                  fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// QR Scan Tab
// ---------------------------------------------------------------------------
class _QrScanTab extends StatefulWidget {
  final Future<void> Function(String qrData) onQrDetected;

  const _QrScanTab({required this.onQrDetected});

  @override
  State<_QrScanTab> createState() => _QrScanTabState();
}

class _QrScanTabState extends State<_QrScanTab> {
  final MobileScannerController _controller = MobileScannerController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        MobileScanner(
          controller: _controller,
          onDetect: (capture) {
            final barcode = capture.barcodes.firstOrNull;
            if (barcode?.rawValue != null) {
              widget.onQrDetected(barcode!.rawValue!);
            }
          },
        ),
        // QR Frame overlay
        Center(
          child: Container(
            width: 220,
            height: 220,
            decoration: BoxDecoration(
              border: Border.all(color: const Color(0xFFC41E3A), width: 3),
              borderRadius: BorderRadius.circular(16),
            ),
          ),
        ),
        // Scanning line animation
        const _ScanningLine(),
        // Instructions
        Positioned(
          bottom: 32,
          left: 0,
          right: 0,
          child: Center(
            child: Text(
              Localizations.localeOf(context).languageCode == 'ar'
                  ? 'وجه كود QR نحو الكاميرا'
                  : 'Point QR code at camera',
              style: const TextStyle(
                color: Colors.white70,
                fontSize: 16,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

// ---------------------------------------------------------------------------
// Scanning Line Animation
// ---------------------------------------------------------------------------
class _ScanningLine extends StatefulWidget {
  const _ScanningLine();

  @override
  State<_ScanningLine> createState() => _ScanningLineState();
}

class _ScanningLineState extends State<_ScanningLine>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 2),
    )..repeat(reverse: true);
    _animation = Tween<double>(begin: -100, end: 100).animate(_controller);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: SizedBox(
        width: 220,
        height: 220,
        child: AnimatedBuilder(
          animation: _animation,
          builder: (context, child) {
            return Stack(
              children: [
                Positioned(
                  top: 110 + _animation.value,
                  left: 0,
                  right: 0,
                  child: Container(
                    height: 2,
                    color: const Color(0xFFC41E3A).withOpacity(0.7),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Numpad Tab
// ---------------------------------------------------------------------------
class _NumpadTab extends StatefulWidget {
  final Future<void> Function(String number) onSubmit;
  final bool isArabic;

  const _NumpadTab({required this.onSubmit, required this.isArabic});

  @override
  State<_NumpadTab> createState() => _NumpadTabState();
}

class _NumpadTabState extends State<_NumpadTab> {
  String _input = '';

  void _onKey(String key) {
    if (_input.length >= 8) return;
    setState(() => _input += key);
  }

  void _onDelete() {
    if (_input.isEmpty) return;
    setState(() => _input = _input.substring(0, _input.length - 1));
  }

  void _onClear() {
    setState(() => _input = '');
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;

    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        // Display
        Container(
          margin: const EdgeInsets.symmetric(horizontal: 40, vertical: 16),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          decoration: BoxDecoration(
            color: const Color(0xFF1A1A1A),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: Colors.white12),
          ),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  _input.isEmpty
                      ? (isArabic ? 'ادخل رقم العضو' : 'Enter member number')
                      : _input,
                  style: TextStyle(
                    color: _input.isEmpty ? Colors.white38 : Colors.white,
                    fontSize: 28,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 4,
                  ),
                  textAlign: TextAlign.center,
                ),
              ),
              if (_input.isNotEmpty)
                GestureDetector(
                  onTap: _onDelete,
                  child: const Icon(Icons.backspace_outlined,
                      color: Colors.white54, size: 24),
                ),
            ],
          ),
        ),

        // Numpad. Digits keep the phone-keypad order (1 2 3 left to right)
        // in Arabic too; mirroring them with the RTL layout puts every key
        // where muscle memory does not expect it.
        Expanded(
          child: Directionality(
            textDirection: TextDirection.ltr,
            child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 60),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _buildNumpadRow(['1', '2', '3']),
                const SizedBox(height: 12),
                _buildNumpadRow(['4', '5', '6']),
                const SizedBox(height: 12),
                _buildNumpadRow(['7', '8', '9']),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: _NumpadKey(
                        label: isArabic ? 'مسح' : 'Clear',
                        isAction: true,
                        onTap: _onClear,
                        color: Colors.white24,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _NumpadKey(
                        label: '0',
                        onTap: () => _onKey('0'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _NumpadKey(
                        label: isArabic ? 'تسجيل' : 'Check\nIn',
                        isAction: true,
                        color: _input.isNotEmpty
                            ? const Color(0xFFC41E3A)
                            : Colors.white24,
                        onTap: _input.isNotEmpty
                            ? () {
                                final number = _input;
                                setState(() => _input = '');
                                widget.onSubmit(number);
                              }
                            : null,
                      ),
                    ),
                  ],
                ),
              ],
            ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildNumpadRow(List<String> keys) {
    return Row(
      children: keys
          .map((k) => [
                Expanded(child: _NumpadKey(label: k, onTap: () => _onKey(k))),
                if (k != keys.last) const SizedBox(width: 12),
              ])
          .expand((e) => e)
          .toList(),
    );
  }
}

class _NumpadKey extends StatelessWidget {
  final String label;
  final VoidCallback? onTap;
  final bool isAction;
  final Color? color;

  const _NumpadKey({
    required this.label,
    this.onTap,
    this.isAction = false,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      enabled: onTap != null,
      label: label,
      excludeSemantics: true,
      child: GestureDetector(
      onTap: onTap,
      child: Container(
        height: 64,
        decoration: BoxDecoration(
          color: color ?? const Color(0xFF2A2A2A),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Center(
          child: Text(
            label,
            style: TextStyle(
              color: onTap != null ? Colors.white : Colors.white38,
              fontSize: isAction ? 14 : 24,
              fontWeight: FontWeight.w600,
            ),
            textAlign: TextAlign.center,
          ),
        ),
      ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Result Overlay (success / failure / loading)
// ---------------------------------------------------------------------------
class _KioskResultOverlay extends StatelessWidget {
  final _KioskData kioskData;
  final bool isArabic;
  final Animation<double> successAnim;
  final Animation<double> errorAnim;

  const _KioskResultOverlay({
    required this.kioskData,
    required this.isArabic,
    required this.successAnim,
    required this.errorAnim,
  });

  @override
  Widget build(BuildContext context) {
    if (kioskData.state == _KioskState.loading) {
      return const Scaffold(
        backgroundColor: Color(0xFF0D0D0D),
        body: Center(
          child: CircularProgressIndicator(color: Color(0xFFC41E3A)),
        ),
      );
    }

    final isSuccess = kioskData.state == _KioskState.success;
    final bgColor = isSuccess
        ? const Color(0xFF0A2E1A)
        : const Color(0xFF2E0A0A);
    final icon = isSuccess ? Icons.check_circle : Icons.cancel;
    final iconColor = isSuccess ? AppColors.success : AppColors.error;
    final title = isSuccess
        ? (isArabic ? 'اهلا وسهلا' : 'Welcome!')
        : (isArabic ? 'تعذر تسجيل الحضور' : 'Check-in Failed');
    final anim = isSuccess ? successAnim : errorAnim;

    return Scaffold(
      backgroundColor: bgColor,
      body: SafeArea(
        child: Column(
          children: [
            // Amber offline banner shown only for offline-queued check-ins
            if (isSuccess && kioskData.isOffline)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 8,
                ),
                color: Colors.amber.shade700,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.wifi_off,
                        color: Colors.white, size: 16),
                    const SizedBox(width: 8),
                    Flexible(
                      child: Text(
                        isArabic
                            ? 'تم حفظ الحضور بدون انترنت — سيتم المزامنة عند الاتصال'
                            : 'Check-in saved offline — will sync when connected',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                        textAlign: TextAlign.center,
                      ),
                    ),
                  ],
                ),
              ),

            Expanded(
              child: Center(
                child: ScaleTransition(
                  scale: anim,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(icon, size: 100, color: iconColor),
                      const SizedBox(height: 24),
                      Text(
                        title,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 36,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      if (kioskData.memberName != null) ...[
                        const SizedBox(height: 16),
                        if (kioskData.memberPhotoUrl != null)
                          ClipOval(
                            child: AvatarWidget(
                              imageUrl: kioskData.memberPhotoUrl,
                              name: kioskData.memberName!,
                              size: 80,
                            ),
                          ),
                        const SizedBox(height: 12),
                        Text(
                          kioskData.memberName!,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 24,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                      if (!isSuccess && kioskData.errorMessage != null) ...[
                        const SizedBox(height: 12),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 40),
                          child: Text(
                            _localizeError(kioskData.errorMessage!, isArabic),
                            style: const TextStyle(
                              color: Colors.white70,
                              fontSize: 16,
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                      ],
                      const SizedBox(height: 32),
                      Text(
                        isArabic
                            ? (isSuccess
                                ? 'سيتم الاعادة خلال 5 ثواني...'
                                : 'سيتم الاعادة خلال 3 ثواني...')
                            : (isSuccess
                                ? 'Resetting in 5 seconds...'
                                : 'Resetting in 3 seconds...'),
                        style: const TextStyle(
                          color: Colors.white38,
                          fontSize: 14,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _localizeError(String error, bool isArabic) {
    if (error.contains('not_member') || error.contains('not a member')) {
      return isArabic ? 'هذا الشخص ليس عضوا في الصالة' : 'This person is not a gym member';
    }
    if (error.contains('already') || error.contains('duplicate')) {
      return isArabic ? 'تم تسجيل الحضور مسبقا اليوم' : 'Already checked in today';
    }
    if (error.contains('expired') || error.contains('membership')) {
      return isArabic ? 'العضوية منتهية' : 'Membership expired';
    }
    return isArabic ? 'رقم غير صحيح او حدث خطأ' : error;
  }
}
