import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final _emailCtrl    = TextEditingController();
  final _phoneCtrl    = TextEditingController();
  final _codeCtrl     = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _obscurePassword = true;
  bool _isLoading  = false;
  int  _cooldown   = 0;
  Timer? _cooldownTimer;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    _codeCtrl.dispose();
    _passwordCtrl.dispose();
    _cooldownTimer?.cancel();
    super.dispose();
  }

  void _startCooldown() {
    setState(() => _cooldown = 60);
    _cooldownTimer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (_cooldown <= 1) {
        t.cancel();
        setState(() => _cooldown = 0);
      } else {
        setState(() => _cooldown--);
      }
    });
  }

  Future<void> _sendCode() async {
    final target = _tabController.index == 0 ? _emailCtrl.text.trim() : _phoneCtrl.text.trim();
    if (target.isEmpty) return;
    _startCooldown();
    // TODO: call API
  }

  Future<void> _submit() async {
    final target = _tabController.index == 0 ? _emailCtrl.text.trim() : _phoneCtrl.text.trim();
    final code = _codeCtrl.text.trim();
    final password = _passwordCtrl.text.trim();
    if (target.isEmpty || code.isEmpty || password.isEmpty) return;

    setState(() => _isLoading = true);
    try {
      await authApi.resetPassword(target: target, code: code, newPassword: password);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('密码重置成功，请重新登录')));
        context.go('/login');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('重置失败：$e')));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: cs.surface,
      appBar: AppBar(
        backgroundColor: cs.surface,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
        title: const Text('找回密码', style: TextStyle(fontWeight: FontWeight.bold)),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('重置你的密码', style: tt.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            Text('输入注册时使用的邮箱或手机号，我们将发送验证码。',
                style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
            const SizedBox(height: 24),

            // ── Tab 切换 ────────────────────────────────────────────
            Container(
              decoration: BoxDecoration(
                color: cs.surfaceContainerLowest,
                borderRadius: BorderRadius.circular(10),
              ),
              child: TabBar(
                controller: _tabController,
                labelColor: cs.primary,
                unselectedLabelColor: cs.onSurfaceVariant,
                indicatorColor: cs.primary,
                dividerColor: Colors.transparent,
                tabs: const [Tab(text: '邮箱'), Tab(text: '手机')],
              ),
            ),
            const SizedBox(height: 20),

            // ── 邮箱或手机输入 ──────────────────────────────────────
            AnimatedBuilder(
              animation: _tabController,
              builder: (_, __) {
                final isEmail = _tabController.index == 0;
                return TextField(
                  controller: isEmail ? _emailCtrl : _phoneCtrl,
                  keyboardType: isEmail ? TextInputType.emailAddress : TextInputType.phone,
                  decoration: InputDecoration(
                    labelText: isEmail ? '电子邮箱' : '手机号码',
                    prefixIcon: Icon(isEmail ? Icons.alternate_email : Icons.phone_outlined),
                    hintText: isEmail ? 'user@example.com' : '+86 13800000000',
                  ),
                );
              },
            ),
            const SizedBox(height: 16),

            // ── 验证码 ──────────────────────────────────────────────
            Row(children: [
              Expanded(
                child: TextField(
                  controller: _codeCtrl,
                  keyboardType: TextInputType.number,
                  maxLength: 6,
                  decoration: const InputDecoration(
                    labelText: '验证码',
                    prefixIcon: Icon(Icons.verified_user_outlined),
                    counterText: '',
                  ),
                ),
              ),
              const SizedBox(width: 12),
              SizedBox(
                height: 56,
                child: OutlinedButton(
                  onPressed: _cooldown > 0 ? null : _sendCode,
                  style: OutlinedButton.styleFrom(padding: const EdgeInsets.symmetric(horizontal: 16)),
                  child: Text(_cooldown > 0 ? '${_cooldown}s' : '获取验证码',
                      style: const TextStyle(fontSize: 13)),
                ),
              ),
            ]),
            const SizedBox(height: 16),

            // ── 新密码 ──────────────────────────────────────────────
            TextField(
              controller: _passwordCtrl,
              obscureText: _obscurePassword,
              decoration: InputDecoration(
                labelText: '新密码',
                prefixIcon: const Icon(Icons.lock_outline),
                suffixIcon: IconButton(
                  icon: Icon(_obscurePassword ? Icons.visibility_off_outlined : Icons.visibility_outlined),
                  onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                ),
              ),
            ),
            const SizedBox(height: 28),

            // ── 重置按钮 ────────────────────────────────────────────
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _isLoading ? null : _submit,
                icon: _isLoading
                    ? const SizedBox(width: 16, height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Icon(Icons.arrow_forward),
                label: const Text('重置密码'),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                ),
              ),
            ),
            const SizedBox(height: 20),

            // ── 返回登录 ────────────────────────────────────────────
            Center(
              child: TextButton.icon(
                onPressed: () => context.go('/login'),
                icon: const Icon(Icons.arrow_back, size: 16),
                label: const Text('返回登录'),
              ),
            ),
            const SizedBox(height: 32),

            // ── 帮助区 ──────────────────────────────────────────────
            Row(mainAxisAlignment: MainAxisAlignment.center, children: [
              TextButton(onPressed: () {}, child: const Text('在线客服')),
              Text('·', style: TextStyle(color: cs.outlineVariant)),
              TextButton(onPressed: () {}, child: const Text('常见问题')),
            ]),
          ],
        ),
      ),
    );
  }
}
