import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../api/api.dart';
import '../../../api/models/profile_models.dart';

final _profileProvider = FutureProvider<UserProfile>((ref) => profileApi.getProfile());

class EditProfileScreen extends ConsumerStatefulWidget {
  const EditProfileScreen({super.key});

  @override
  ConsumerState<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends ConsumerState<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();

  final _nicknameCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();

  String? _gender;
  String? _educationLevel;
  List<String> _interestedSubjects = [];

  bool _initialized = false;
  bool _submitting = false;

  static const _educationLevels = ['小学', '初中', '高中', '大学', '研究生', '博士'];
  static const _subjectOptions = ['语文', '数学', '英语', '物理', '化学', '生物', '历史', '地理', '政治', '计算机'];

  void _initFromProfile(UserProfile p) {
    if (_initialized) return;
    _initialized = true;
    _nicknameCtrl.text = p.nickname ?? '';
    _addressCtrl.text = p.address ?? '';
    _gender = p.gender;
    _educationLevel = p.educationLevel;
    _interestedSubjects = List<String>.from(p.interestedSubjects);
  }

  @override
  void dispose() {
    _nicknameCtrl.dispose();
    _addressCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _submitting = true);
    try {
      await profileApi.updateProfile(
        nickname: _nicknameCtrl.text.trim().isEmpty ? null : _nicknameCtrl.text.trim(),
        gender: _gender,
        address: _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
        educationLevel: _educationLevel,
        interestedSubjects: _interestedSubjects,
      );
      ref.invalidate(_profileProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('个人资料已更新'), backgroundColor: Colors.green),
        );
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('更新失败：$e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final profileAsync = ref.watch(_profileProvider);
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Scaffold(
      appBar: AppBar(
        title: const Text('编辑个人资料'),
        actions: [
          TextButton(
            onPressed: _submitting ? null : _save,
            child: _submitting
                ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                : const Text('保存'),
          ),
        ],
      ),
      body: profileAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('加载失败：$e')),
        data: (profile) {
          _initFromProfile(profile);
          return Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // 基本信息卡片
                _Section(
                  title: '基本信息',
                  cs: cs,
                  tt: tt,
                  children: [
                    _ReadOnlyField(label: '用户名', value: profile.username),
                    if (profile.email != null)
                      _ReadOnlyField(label: '邮箱', value: profile.email!),
                    if (profile.phoneNumber != null)
                      _ReadOnlyField(label: '手机号', value: profile.phoneNumber!),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _nicknameCtrl,
                      decoration: const InputDecoration(
                        labelText: '昵称',
                        border: OutlineInputBorder(),
                        hintText: '请输入昵称',
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _addressCtrl,
                      decoration: const InputDecoration(
                        labelText: '地址',
                        border: OutlineInputBorder(),
                        hintText: '请输入地址',
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // 性别
                _Section(
                  title: '性别',
                  cs: cs,
                  tt: tt,
                  children: [
                    Wrap(
                      spacing: 8,
                      children: ['男', '女', '保密'].map((g) => ChoiceChip(
                        label: Text(g),
                        selected: _gender == g,
                        onSelected: (_) => setState(() => _gender = g),
                      )).toList(),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // 学历
                _Section(
                  title: '最高学历',
                  cs: cs,
                  tt: tt,
                  children: [
                    DropdownButtonFormField<String>(
                      value: _educationLevel,
                      decoration: const InputDecoration(
                        border: OutlineInputBorder(),
                        hintText: '请选择学历',
                      ),
                      items: _educationLevels.map((lv) => DropdownMenuItem(
                        value: lv,
                        child: Text(lv),
                      )).toList(),
                      onChanged: (v) => setState(() => _educationLevel = v),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // 感兴趣的学科
                _Section(
                  title: '感兴趣的学科',
                  cs: cs,
                  tt: tt,
                  children: [
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      children: _subjectOptions.map((s) => FilterChip(
                        label: Text(s),
                        selected: _interestedSubjects.contains(s),
                        onSelected: (selected) {
                          setState(() {
                            if (selected) {
                              _interestedSubjects.add(s);
                            } else {
                              _interestedSubjects.remove(s);
                            }
                          });
                        },
                      )).toList(),
                    ),
                  ],
                ),
                const SizedBox(height: 32),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _Section extends StatelessWidget {
  final String title;
  final List<Widget> children;
  final ColorScheme cs;
  final TextTheme tt;

  const _Section({
    required this.title,
    required this.children,
    required this.cs,
    required this.tt,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: tt.titleSmall?.copyWith(color: cs.primary, fontWeight: FontWeight.w600)),
          const SizedBox(height: 12),
          ...children,
        ],
      ),
    );
  }
}

class _ReadOnlyField extends StatelessWidget {
  final String label;
  final String value;

  const _ReadOnlyField({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    final tt = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          SizedBox(
            width: 70,
            child: Text(label, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
          ),
          Expanded(child: Text(value, style: tt.bodyMedium)),
        ],
      ),
    );
  }
}
