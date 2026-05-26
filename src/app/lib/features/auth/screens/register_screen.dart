import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../stores/auth_store.dart';
import '../../../api/api.dart';
import '../../../api/models/auth_models.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();

  String _selectedRole = 'Student';
  String? _selectedTenantId;
  List<TenantItem> _tenants = [];
  bool _loadingTenants = true;
  bool _obscurePassword = true;
  bool _obscureConfirm = true;

  @override
  void initState() {
    super.initState();
    _loadTenants();
  }

  @override
  void dispose() {
    _usernameCtrl.dispose();
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    _phoneCtrl.dispose();
    _emailCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadTenants() async {
    try {
      final list = await authApi.getPublicTenants();
      if (mounted) setState(() { _tenants = list; _loadingTenants = false; });
    } catch (_) {
      if (mounted) setState(() => _loadingTenants = false);
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final ok = await ref.read(authStoreProvider.notifier).register(
      username: _usernameCtrl.text.trim(),
      password: _passwordCtrl.text,
      role: _selectedRole,
      tenantId: _selectedTenantId!,
      phoneNumber: _phoneCtrl.text.trim().isEmpty ? null : _phoneCtrl.text.trim(),
      email: _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
    );
    if (ok && mounted) context.go('/home');
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authStoreProvider);
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: cs.primary,
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [cs.primary, cs.primaryContainer],
          ),
        ),
        child: SafeArea(
          child: CustomScrollView(
            slivers: [
              SliverAppBar(
                backgroundColor: Colors.transparent,
                foregroundColor: cs.onPrimary,
                title: const Text('创建账号'),
                pinned: false,
              ),
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
                  child: Card(
                    elevation: 0,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            // ── 身份选择 ──────────────────────────────
                            Text('我的身份', style: tt.titleSmall?.copyWith(
                              color: cs.onSurfaceVariant, fontWeight: FontWeight.w600)),
                            const SizedBox(height: 12),
                            Row(children: [
                              _RoleChip(
                                emoji: '🎓',
                                label: '学生',
                                value: 'Student',
                                selected: _selectedRole == 'Student',
                                onTap: () => setState(() => _selectedRole = 'Student'),
                              ),
                              const SizedBox(width: 12),
                              _RoleChip(
                                emoji: '📚',
                                label: '教师',
                                value: 'Teacher',
                                selected: _selectedRole == 'Teacher',
                                onTap: () => setState(() => _selectedRole = 'Teacher'),
                              ),
                            ]),
                            const SizedBox(height: 20),

                            // ── 租户选择 ──────────────────────────────
                            Text(
                              _selectedRole == 'Teacher' ? '任教机构' : '就读机构',
                              style: tt.titleSmall?.copyWith(
                                color: cs.onSurfaceVariant, fontWeight: FontWeight.w600)),
                            const SizedBox(height: 8),
                            if (_loadingTenants)
                              const Center(child: Padding(
                                padding: EdgeInsets.all(12),
                                child: CircularProgressIndicator(),
                              ))
                            else
                              DropdownButtonFormField<String>(
                                value: _selectedTenantId,
                                decoration: InputDecoration(
                                  hintText: '请选择机构',
                                  prefixIcon: const Icon(Icons.business_outlined),
                                  filled: true,
                                  fillColor: cs.surfaceVariant.withOpacity(0.4),
                                  border: OutlineInputBorder(
                                    borderRadius: BorderRadius.circular(12),
                                    borderSide: BorderSide.none,
                                  ),
                                ),
                                isExpanded: true,
                                items: _tenants.map((t) => DropdownMenuItem(
                                  value: t.id,
                                  child: Text(t.name, overflow: TextOverflow.ellipsis),
                                )).toList(),
                                onChanged: (v) => setState(() => _selectedTenantId = v),
                                validator: (v) => v == null ? '请选择所属机构' : null,
                              ),
                            const SizedBox(height: 20),

                            // ── 账号信息 ──────────────────────────────
                            Text('账号信息', style: tt.titleSmall?.copyWith(
                              color: cs.onSurfaceVariant, fontWeight: FontWeight.w600)),
                            const SizedBox(height: 8),
                            _Field(
                              controller: _usernameCtrl,
                              label: '用户名',
                              icon: Icons.person_outline,
                              validator: (v) {
                                if (v == null || v.isEmpty) return '请输入用户名';
                                if (v.length < 4 || v.length > 20) return '用户名长度 4-20 位';
                                return null;
                              },
                            ),
                            const SizedBox(height: 12),
                            _Field(
                              controller: _phoneCtrl,
                              label: '手机号（选填）',
                              icon: Icons.phone_outlined,
                              keyboardType: TextInputType.phone,
                              validator: (v) {
                                if (v != null && v.isNotEmpty) {
                                  if (!RegExp(r'^1[3-9]\d{9}$').hasMatch(v)) {
                                    return '请输入有效手机号';
                                  }
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 12),
                            _Field(
                              controller: _emailCtrl,
                              label: '邮箱（选填）',
                              icon: Icons.email_outlined,
                              keyboardType: TextInputType.emailAddress,
                              validator: (v) {
                                if (v != null && v.isNotEmpty) {
                                  if (!RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(v)) {
                                    return '请输入有效邮箱';
                                  }
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 12),
                            TextFormField(
                              controller: _passwordCtrl,
                              obscureText: _obscurePassword,
                              decoration: InputDecoration(
                                labelText: '密码',
                                prefixIcon: const Icon(Icons.lock_outline),
                                suffixIcon: IconButton(
                                  icon: Icon(_obscurePassword
                                      ? Icons.visibility_off : Icons.visibility),
                                  onPressed: () =>
                                      setState(() => _obscurePassword = !_obscurePassword),
                                ),
                                filled: true,
                                fillColor: cs.surfaceVariant.withOpacity(0.4),
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(12),
                                  borderSide: BorderSide.none,
                                ),
                              ),
                              validator: (v) {
                                if (v == null || v.isEmpty) return '请输入密码';
                                if (v.length < 6) return '密码至少 6 位';
                                return null;
                              },
                            ),
                            const SizedBox(height: 12),
                            TextFormField(
                              controller: _confirmCtrl,
                              obscureText: _obscureConfirm,
                              decoration: InputDecoration(
                                labelText: '确认密码',
                                prefixIcon: const Icon(Icons.lock_outline),
                                suffixIcon: IconButton(
                                  icon: Icon(_obscureConfirm
                                      ? Icons.visibility_off : Icons.visibility),
                                  onPressed: () =>
                                      setState(() => _obscureConfirm = !_obscureConfirm),
                                ),
                                filled: true,
                                fillColor: cs.surfaceVariant.withOpacity(0.4),
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(12),
                                  borderSide: BorderSide.none,
                                ),
                              ),
                              validator: (v) {
                                if (v == null || v.isEmpty) return '请确认密码';
                                if (v != _passwordCtrl.text) return '两次密码不一致';
                                return null;
                              },
                            ),
                            const SizedBox(height: 8),
                            if (state.error != null)
                              Padding(
                                padding: const EdgeInsets.only(top: 4),
                                child: Text(state.error!,
                                    style: TextStyle(color: cs.error, fontSize: 13)),
                              ),
                            const SizedBox(height: 20),
                            SizedBox(
                              width: double.infinity,
                              height: 48,
                              child: FilledButton(
                                onPressed: state.isLoading ? null : _submit,
                                style: FilledButton.styleFrom(
                                  shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(12)),
                                ),
                                child: state.isLoading
                                    ? const SizedBox(
                                        height: 20, width: 20,
                                        child: CircularProgressIndicator(
                                            strokeWidth: 2, color: Colors.white))
                                    : const Text('注 册', style: TextStyle(fontSize: 16)),
                              ),
                            ),
                            const SizedBox(height: 16),
                            Center(
                              child: TextButton(
                                onPressed: () => context.go('/login'),
                                child: Text('已有账号？去登录',
                                    style: TextStyle(color: cs.primary)),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _RoleChip extends StatelessWidget {
  final String emoji;
  final String label;
  final String value;
  final bool selected;
  final VoidCallback onTap;

  const _RoleChip({
    required this.emoji,
    required this.label,
    required this.value,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(vertical: 14),
          decoration: BoxDecoration(
            color: selected ? cs.primaryContainer : cs.surfaceVariant.withOpacity(0.4),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: selected ? cs.primary : Colors.transparent,
              width: 2,
            ),
          ),
          child: Column(
            children: [
              Text(emoji, style: const TextStyle(fontSize: 28)),
              const SizedBox(height: 4),
              Text(label,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                    color: selected ? cs.primary : cs.onSurfaceVariant,
                  )),
            ],
          ),
        ),
      ),
    );
  }
}

class _Field extends StatelessWidget {
  final TextEditingController controller;
  final String label;
  final IconData icon;
  final TextInputType? keyboardType;
  final String? Function(String?)? validator;

  const _Field({
    required this.controller,
    required this.label,
    required this.icon,
    this.keyboardType,
    this.validator,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return TextFormField(
      controller: controller,
      keyboardType: keyboardType,
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: Icon(icon),
        filled: true,
        fillColor: cs.surfaceVariant.withOpacity(0.4),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide.none,
        ),
      ),
      validator: validator,
    );
  }
}
