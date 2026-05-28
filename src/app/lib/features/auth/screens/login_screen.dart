import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../stores/auth_store.dart';
import '../../../api/api.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

enum _LoginTab { password, code }

class _LoginScreenState extends ConsumerState<LoginScreen>
    with SingleTickerProviderStateMixin {
  final _pwdFormKey  = GlobalKey<FormState>();
  final _codeFormKey = GlobalKey<FormState>();

  // 密码登录
  final _identifierCtrl = TextEditingController();
  final _passwordCtrl   = TextEditingController();
  bool _obscure = true;

  // 验证码登录
  final _targetCtrl = TextEditingController();
  final _codeCtrl   = TextEditingController();
  bool _sendingCode = false;
  int  _codeCooldown = 0;

  _LoginTab _tab = _LoginTab.password;

  @override
  void dispose() {
    _identifierCtrl.dispose();
    _passwordCtrl.dispose();
    _targetCtrl.dispose();
    _codeCtrl.dispose();
    super.dispose();
  }

  Future<void> _submitPassword() async {
    if (!_pwdFormKey.currentState!.validate()) return;
    final ok = await ref.read(authStoreProvider.notifier).login(
      _identifierCtrl.text.trim(),
      _passwordCtrl.text,
    );
    if (ok && mounted) context.go('/home');
  }

  Future<void> _sendCode() async {
    final target = _targetCtrl.text.trim();
    if (target.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('请输入手机号或邮箱')));
      return;
    }
    setState(() => _sendingCode = true);
    try {
      final devCode = await authApi.sendCode(target);
      if (devCode != null && mounted) {
        _codeCtrl.text = devCode;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('[开发] 验证码：$devCode')));
      } else if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('验证码已发送')));
      }
      // Start 60s cooldown
      setState(() { _codeCooldown = 60; _sendingCode = false; });
      _startCooldown();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceAll('Exception:', '').trim())));
        setState(() => _sendingCode = false);
      }
    }
  }

  void _startCooldown() {
    Future.doWhile(() async {
      await Future.delayed(const Duration(seconds: 1));
      if (!mounted) return false;
      setState(() => _codeCooldown--);
      return _codeCooldown > 0;
    });
  }

  Future<void> _submitCode() async {
    if (!_codeFormKey.currentState!.validate()) return;
    final ok = await ref.read(authStoreProvider.notifier).loginWithCode(
      _targetCtrl.text.trim(),
      _codeCtrl.text.trim(),
    );
    if (ok && mounted) context.go('/home');
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authStoreProvider);
    final cs    = Theme.of(context).colorScheme;
    final tt    = Theme.of(context).textTheme;

    return Scaffold(
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [cs.primary, cs.primaryContainer.withOpacity(0.8)],
          ),
        ),
        child: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 40),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // ── 标题区 ───────────────────────────────────────────
                Row(children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: Colors.white.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(Icons.school_rounded, color: cs.onPrimary, size: 28),
                  ),
                  const SizedBox(width: 12),
                  Text('考试系统',
                      style: tt.titleLarge?.copyWith(
                          color: cs.onPrimary, fontWeight: FontWeight.bold)),
                ]),
                const SizedBox(height: 28),
                Text('欢迎回来',
                    style: tt.headlineMedium?.copyWith(
                        color: cs.onPrimary, fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text('登录以参加考试 · 查看成绩 · 管理课程',
                    style: tt.bodyMedium?.copyWith(
                        color: cs.onPrimary.withOpacity(0.75))),
                const SizedBox(height: 32),

                // ── 卡片 ─────────────────────────────────────────────
                Card(
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(20)),
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // 登录方式 Tab
                        Container(
                          decoration: BoxDecoration(
                            color: cs.surfaceVariant.withOpacity(0.5),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          padding: const EdgeInsets.all(4),
                          child: Row(children: [
                            _TabBtn(
                              label: '账号密码',
                              selected: _tab == _LoginTab.password,
                              onTap: () => setState(() => _tab = _LoginTab.password),
                            ),
                            _TabBtn(
                              label: '验证码登录',
                              selected: _tab == _LoginTab.code,
                              onTap: () => setState(() => _tab = _LoginTab.code),
                            ),
                          ]),
                        ),
                        const SizedBox(height: 20),

                        // 密码登录表单
                        if (_tab == _LoginTab.password)
                          Form(
                            key: _pwdFormKey,
                            child: Column(children: [
                              _inputField(
                                controller: _identifierCtrl,
                                label: '用户名 / 邮箱 / 手机号',
                                icon: Icons.person_outline,
                                validator: (v) =>
                                    v == null || v.isEmpty ? '请输入账号' : null,
                              ),
                              const SizedBox(height: 12),
                              TextFormField(
                                controller: _passwordCtrl,
                                obscureText: _obscure,
                                decoration: _inputDec(
                                  context,
                                  label: '密码',
                                  icon: Icons.lock_outline,
                                ).copyWith(
                                  suffixIcon: IconButton(
                                    icon: Icon(_obscure
                                        ? Icons.visibility_off : Icons.visibility),
                                    onPressed: () =>
                                        setState(() => _obscure = !_obscure),
                                  ),
                                ),
                                onFieldSubmitted: (_) => _submitPassword(),
                                validator: (v) =>
                                    v == null || v.isEmpty ? '请输入密码' : null,
                              ),
                            ]),
                          ),

                        // 验证码登录表单
                        if (_tab == _LoginTab.code)
                          Form(
                            key: _codeFormKey,
                            child: Column(children: [
                              _inputField(
                                controller: _targetCtrl,
                                label: '手机号 或 邮箱',
                                icon: Icons.phone_outlined,
                                keyboardType: TextInputType.emailAddress,
                                validator: (v) {
                                  if (v == null || v.isEmpty) return '请输入手机号或邮箱';
                                  final phone = RegExp(r'^1[3-9]\d{9}$').hasMatch(v);
                                  final email = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(v);
                                  if (!phone && !email) return '请输入有效手机号或邮箱';
                                  return null;
                                },
                              ),
                              const SizedBox(height: 12),
                              Row(children: [
                                Expanded(
                                  child: TextFormField(
                                    controller: _codeCtrl,
                                    keyboardType: TextInputType.number,
                                    decoration: _inputDec(context,
                                        label: '6 位验证码',
                                        icon: Icons.key_outlined),
                                    validator: (v) {
                                      if (v == null || v.isEmpty) return '请输入验证码';
                                      if (v.length != 6) return '验证码为 6 位';
                                      return null;
                                    },
                                  ),
                                ),
                                const SizedBox(width: 8),
                                SizedBox(
                                  height: 52,
                                  child: FilledButton.tonal(
                                    onPressed: (_sendingCode || _codeCooldown > 0)
                                        ? null
                                        : _sendCode,
                                    style: FilledButton.styleFrom(
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 12)),
                                    child: _sendingCode
                                        ? const SizedBox(
                                            width: 16, height: 16,
                                            child: CircularProgressIndicator(strokeWidth: 2))
                                        : Text(_codeCooldown > 0
                                            ? '${_codeCooldown}s'
                                            : '获取验证码',
                                            style: const TextStyle(fontSize: 13)),
                                  ),
                                ),
                              ]),
                              Padding(
                                padding: const EdgeInsets.only(top: 6),
                                child: Text('首次登录将自动注册账号',
                                    style: TextStyle(
                                        fontSize: 12,
                                        color: cs.onSurfaceVariant)),
                              ),
                            ]),
                          ),

                        if (state.error != null) ...[
                          const SizedBox(height: 8),
                          Text(state.error!,
                              style: TextStyle(color: cs.error, fontSize: 13)),
                        ],
                        const SizedBox(height: 20),

                        SizedBox(
                          width: double.infinity,
                          height: 48,
                          child: FilledButton(
                            onPressed: state.isLoading
                                ? null
                                : (_tab == _LoginTab.password
                                    ? _submitPassword
                                    : _submitCode),
                            style: FilledButton.styleFrom(
                              shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12))),
                            child: state.isLoading
                                ? const SizedBox(
                                    height: 20, width: 20,
                                    child: CircularProgressIndicator(
                                        strokeWidth: 2, color: Colors.white))
                                : const Text('登 录',
                                    style: TextStyle(fontSize: 16)),
                          ),
                        ),
                        const SizedBox(height: 16),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text('还没有账号？',
                                style: TextStyle(color: cs.onSurfaceVariant)),
                            TextButton(
                              onPressed: () => context.go('/register'),
                              child: const Text('立即注册'),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 24),
                Center(
                  child: TextButton(
                    onPressed: () => context.go('/home'),
                    style: TextButton.styleFrom(
                        foregroundColor: cs.onPrimary.withOpacity(0.8)),
                    child: const Text('暂不登录，先看看 →'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  InputDecoration _inputDec(BuildContext context,
      {required String label, required IconData icon}) {
    final cs = Theme.of(context).colorScheme;
    return InputDecoration(
      labelText: label,
      prefixIcon: Icon(icon),
      filled: true,
      fillColor: cs.surfaceVariant.withOpacity(0.4),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide.none,
      ),
    );
  }

  Widget _inputField({
    required TextEditingController controller,
    required String label,
    required IconData icon,
    TextInputType? keyboardType,
    String? Function(String?)? validator,
    VoidCallback? onSubmit,
  }) {
    final cs = Theme.of(context).colorScheme;
    return TextFormField(
      controller: controller,
      keyboardType: keyboardType,
      decoration: _inputDec(context, label: label, icon: icon),
      validator: validator,
      onFieldSubmitted: onSubmit != null ? (_) => onSubmit() : null,
    );
  }
}

class _TabBtn extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;
  const _TabBtn({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          padding: const EdgeInsets.symmetric(vertical: 8),
          decoration: BoxDecoration(
            color: selected ? cs.surface : Colors.transparent,
            borderRadius: BorderRadius.circular(8),
            boxShadow: selected
                ? [BoxShadow(color: Colors.black.withOpacity(0.08), blurRadius: 4)]
                : null,
          ),
          alignment: Alignment.center,
          child: Text(
            label,
            style: TextStyle(
              fontSize: 14,
              fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
              color: selected ? cs.primary : cs.onSurfaceVariant,
            ),
          ),
        ),
      ),
    );
  }
}
