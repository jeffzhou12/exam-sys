<template>
  <div class="profile-page container">
    <div class="page-header">
      <h2>个人资料</h2>
      <p class="page-sub">完善资料有助于 AI 为你推荐合适的练习题和考试</p>
    </div>

    <!-- ── 基本信息区块 ──────────────────────────────────────────── -->
    <div v-loading="loading" class="profile-card">
      <div class="card-title">基本信息</div>
      <el-form :model="form" label-width="100px">
        <el-form-item label="头像">
          <div class="avatar-wrap">
            <el-avatar :size="80" :src="form.avatarUrl || undefined" :icon="UserFilled" />
            <el-upload
              :http-request="uploadAvatar"
              :show-file-list="false"
              accept="image/*"
              class="avatar-upload-btn"
            >
              <el-button size="small" :loading="avatarUploading">
                <el-icon><Upload /></el-icon>&nbsp;更换头像
              </el-button>
            </el-upload>
          </div>
        </el-form-item>
        <el-form-item label="用户名">
          <span class="username-text">{{ form.username }}</span>
        </el-form-item>
        <el-form-item label="昵称">
          <el-input v-model="form.nickname" placeholder="输入昵称" clearable style="width:200px" />
        </el-form-item>
        <el-form-item label="性别">
          <el-select v-model="form.gender" placeholder="请选择" clearable style="width:200px">
            <el-option label="男" value="male" />
            <el-option label="女" value="female" />
            <el-option label="保密" value="other" />
          </el-select>
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" placeholder="输入邮箱" clearable style="max-width:320px" />
        </el-form-item>
        <el-form-item label="手机号码">
          <div class="phone-row">
            <el-input :value="form.phone" disabled placeholder="未绑定" style="max-width:220px" />
            <el-button size="small" @click="openChangePhone">
              {{ form.phone ? '换绑手机' : '绑定手机' }}
            </el-button>
          </div>
        </el-form-item>
        <el-form-item label="地址">
          <el-input v-model="form.address" placeholder="省/市/区 + 详细地址" clearable />
        </el-form-item>
      </el-form>
    </div>

    <!-- ── 学习信息区块 ──────────────────────────────────────────── -->
    <div class="profile-card">
      <div class="card-title">学习信息</div>
      <el-form :model="form" label-width="100px">
        <el-form-item label="当前学历">
          <el-select v-model="form.educationLevel" placeholder="请选择学历" clearable style="width:200px">
            <el-option v-for="lv in EDUCATION_LEVELS" :key="lv" :label="lv" :value="lv" />
          </el-select>
        </el-form-item>
        <el-form-item label="感兴趣学科">
          <div class="subject-tags">
            <el-check-tag
              v-for="sub in allSubjects"
              :key="sub"
              :checked="form.interestedSubjects.includes(sub)"
              @change="(checked) => toggleSubject(sub, checked)"
            >{{ sub }}</el-check-tag>
            <template v-if="addingSubject">
              <el-input
                ref="customSubjectInput"
                v-model="customSubjectVal"
                size="small"
                placeholder="输入学科名"
                style="width:110px"
                @keyup.enter="confirmCustomSubject"
                @blur="confirmCustomSubject"
              />
            </template>
            <el-button v-else size="small" @click="startAddSubject">
              <el-icon><Plus /></el-icon> 自定义
            </el-button>
          </div>
          <div class="subject-hint text-muted">AI 将根据你选择的学科推荐练习题和考试</div>
        </el-form-item>
      </el-form>
    </div>

    <!-- ── 操作按钮区块 ──────────────────────────────────────────── -->
    <div>
      <el-button type="primary" :loading="saving" @click="saveProfile">保存</el-button>
      <el-button @click="resetForm">取消</el-button>
      <el-button type="warning" plain @click="openChangePassword" style="margin-left:16px">
        修改密码
      </el-button>
    </div>

    <!-- ── 换绑手机对话框 ──────────────────────────────────────────── -->
    <el-dialog v-model="phoneDialog.visible" title="换绑手机号" width="420px" :close-on-click-modal="false">
      <el-form :model="phoneDialog" label-width="90px">
        <el-form-item label="新手机号">
          <el-input v-model="phoneDialog.newPhone" placeholder="请输入新手机号" clearable />
        </el-form-item>
        <el-form-item label="验证码">
          <div class="code-row">
            <el-input v-model="phoneDialog.code" placeholder="6位验证码" maxlength="6" style="flex:1" />
            <el-button
              :disabled="!!phoneDialog.countdown || !phoneDialog.newPhone"
              :loading="phoneDialog.sending"
              @click="sendPhoneCode"
            >
              {{ phoneDialog.countdown ? `${phoneDialog.countdown}s 后重试` : '发送验证码' }}
            </el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="phoneDialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="phoneDialog.submitting" @click="submitChangePhone">确认换绑</el-button>
      </template>
    </el-dialog>

    <!-- ── 修改密码对话框 ──────────────────────────────────────────── -->
    <el-dialog v-model="pwdDialog.visible" title="修改密码" width="420px" :close-on-click-modal="false">
      <el-form :model="pwdDialog" label-width="100px">
        <el-form-item label="当前密码">
          <el-input v-model="pwdDialog.oldPassword" type="password" show-password placeholder="输入当前密码" />
        </el-form-item>
        <el-form-item label="新密码">
          <el-input v-model="pwdDialog.newPassword" type="password" show-password placeholder="至少 6 位" />
        </el-form-item>
        <el-form-item label="确认新密码">
          <el-input v-model="pwdDialog.confirmPassword" type="password" show-password placeholder="再次输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="pwdDialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="pwdDialog.submitting" @click="submitChangePassword">确认修改</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, nextTick, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { UserFilled, Upload, Plus } from '@element-plus/icons-vue'
import { meApi, EDUCATION_LEVELS, SUBJECT_OPTIONS } from '@/api/me'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const loading = ref(false)
const saving  = ref(false)
const avatarUploading = ref(false)
let original = null

const form = reactive({
  username:           '',
  nickname:           '',
  gender:             '',
  email:              '',
  phone:              '',
  address:            '',
  avatarUrl:          '',
  educationLevel:     '',
  interestedSubjects: [],
})

// ── 学科标签（预设 + 用户自定义） ─────────────────────────────────
const customSubjects    = ref([])
const allSubjects       = computed(() => [...SUBJECT_OPTIONS, ...customSubjects.value])
const addingSubject     = ref(false)
const customSubjectVal  = ref('')
const customSubjectInput = ref(null)

function startAddSubject() {
  addingSubject.value = true
  nextTick(() => customSubjectInput.value?.focus())
}

function confirmCustomSubject() {
  const val = customSubjectVal.value.trim()
  if (val && !allSubjects.value.includes(val)) {
    customSubjects.value.push(val)
    form.interestedSubjects.push(val)
  }
  customSubjectVal.value = ''
  addingSubject.value = false
}

function toggleSubject(sub, checked) {
  if (checked) {
    if (!form.interestedSubjects.includes(sub)) form.interestedSubjects.push(sub)
  } else {
    form.interestedSubjects = form.interestedSubjects.filter(s => s !== sub)
  }
}

// ── 头像上传 ──────────────────────────────────────────────────────
async function uploadAvatar({ file }) {
  avatarUploading.value = true
  try {
    const fd = new FormData()
    fd.append('file', file)
    const res = await meApi.uploadAvatar(fd)
    form.avatarUrl = res.url
    auth.patchUser({ avatarUrl: res.url })
    ElMessage.success('头像更新成功')
  } catch {
    // 全局拦截器已处理
  } finally {
    avatarUploading.value = false
  }
}

// ── 加载 & 保存个人资料 ───────────────────────────────────────────
onMounted(fetchProfile)

async function fetchProfile() {
  loading.value = true
  try {
    const data = await meApi.getProfile()
    Object.assign(form, {
      username:           data.username           || '',
      nickname:           data.nickname           || '',
      gender:             data.gender             || '',
      email:              data.email              || '',
      phone:              data.phone              || '',
      address:            data.address            || '',
      avatarUrl:          data.avatarUrl          || '',
      educationLevel:     data.educationLevel     || '',
      interestedSubjects: data.interestedSubjects || [],
    })
    customSubjects.value = form.interestedSubjects.filter(s => !SUBJECT_OPTIONS.includes(s))
    original = { ...form, interestedSubjects: [...form.interestedSubjects] }
  } catch {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

async function saveProfile() {
  saving.value = true
  try {
    await meApi.updateProfile({
      nickname:           form.nickname           || null,
      gender:             form.gender             || null,
      email:              form.email              || null,
      address:            form.address            || null,
      educationLevel:     form.educationLevel     || null,
      interestedSubjects: form.interestedSubjects,
    })
    ElMessage.success('保存成功')
    original = { ...form, interestedSubjects: [...form.interestedSubjects] }
  } catch {
    ElMessage.error('保存失败，请稍后重试')
  } finally {
    saving.value = false
  }
}

function resetForm() {
  if (!original) return
  Object.assign(form, { ...original, interestedSubjects: [...original.interestedSubjects] })
  customSubjects.value = original.interestedSubjects.filter(s => !SUBJECT_OPTIONS.includes(s))
}

// ── 换绑手机 ──────────────────────────────────────────────────────
const phoneDialog = reactive({
  visible:    false,
  newPhone:   '',
  code:       '',
  sending:    false,
  submitting: false,
  countdown:  0,
})
let phoneTimer = null

function openChangePhone() {
  phoneDialog.newPhone  = ''
  phoneDialog.code      = ''
  phoneDialog.countdown = 0
  phoneDialog.visible   = true
}

async function sendPhoneCode() {
  if (!phoneDialog.newPhone) return
  phoneDialog.sending = true
  try {
    await meApi.sendChangePhoneCode(phoneDialog.newPhone)
    ElMessage.success('验证码已发送')
    phoneDialog.countdown = 60
    clearInterval(phoneTimer)
    phoneTimer = setInterval(() => {
      phoneDialog.countdown--
      if (phoneDialog.countdown <= 0) clearInterval(phoneTimer)
    }, 1000)
  } catch {
    // handled
  } finally {
    phoneDialog.sending = false
  }
}

async function submitChangePhone() {
  if (!phoneDialog.newPhone || !phoneDialog.code) {
    ElMessage.warning('请填写新手机号和验证码')
    return
  }
  phoneDialog.submitting = true
  try {
    await meApi.changePhone({ newPhone: phoneDialog.newPhone, code: phoneDialog.code })
    form.phone = phoneDialog.newPhone
    original && (original.phone = phoneDialog.newPhone)
    ElMessage.success('手机号换绑成功')
    phoneDialog.visible = false
  } catch {
    // handled
  } finally {
    phoneDialog.submitting = false
  }
}

// ── 修改密码 ──────────────────────────────────────────────────────
const pwdDialog = reactive({
  visible:         false,
  oldPassword:     '',
  newPassword:     '',
  confirmPassword: '',
  submitting:      false,
})

function openChangePassword() {
  pwdDialog.oldPassword     = ''
  pwdDialog.newPassword     = ''
  pwdDialog.confirmPassword = ''
  pwdDialog.visible         = true
}

async function submitChangePassword() {
  if (!pwdDialog.oldPassword || !pwdDialog.newPassword || !pwdDialog.confirmPassword) {
    ElMessage.warning('请填写所有密码字段')
    return
  }
  if (pwdDialog.newPassword !== pwdDialog.confirmPassword) {
    ElMessage.warning('两次输入的新密码不一致')
    return
  }
  if (pwdDialog.newPassword.length < 6) {
    ElMessage.warning('新密码长度至少 6 位')
    return
  }
  pwdDialog.submitting = true
  try {
    await meApi.changePassword({
      oldPassword: pwdDialog.oldPassword,
      newPassword: pwdDialog.newPassword,
    })
    ElMessage.success('密码修改成功，请重新登录')
    pwdDialog.visible = false
  } catch {
    // handled
  } finally {
    pwdDialog.submitting = false
  }
}
</script>

<style scoped>
.profile-page { padding: 24px 0; }
.page-header { margin-bottom: 24px; }
.page-header h2 { margin-bottom: 4px; }
.page-sub { color: var(--el-text-color-secondary); font-size: 13px; }

.profile-card {
  background: var(--el-bg-color);
  border-radius: 12px;
  padding: 24px;
  margin-bottom: 16px;
}

.card-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin-bottom: 20px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.profile-actions {
  background: var(--el-bg-color);
  border-radius: 12px;
  padding: 20px 24px;
}

.avatar-wrap { display: flex; align-items: center; gap: 16px; }
.avatar-upload-btn { display: inline-flex; }

.phone-row { display: flex; align-items: center; gap: 10px; }

.subject-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 6px;
  align-items: center;
}
.subject-hint { font-size: 12px; }
.text-muted { color: var(--el-text-color-secondary); }

.code-row { display: flex; gap: 8px; }
.username-text { font-size: 14px; color: var(--el-text-color-primary); line-height: 32px; }
</style>
