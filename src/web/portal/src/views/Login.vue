<template>
  <div class="auth-page">
    <div class="auth-card">
      <div class="auth-logo">
        <svg width="36" height="36" viewBox="0 0 24 24" fill="white" aria-hidden="true">
          <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3z"/>
          <path d="M5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" opacity="0.8"/>
        </svg>
        <span>在线考试系统</span>
      </div>
      <h2 class="auth-title">欢迎回来</h2>

      <div class="login-tabs">
        <button
          v-for="tab in loginTabs"
          :key="tab.key"
          class="login-tab"
          :class="{ active: activeTab === tab.key }"
          @click="switchTab(tab.key)">
          {{ tab.label }}
        </button>
      </div>

      <el-form v-if="activeTab === 'password'" ref="pwdFormRef" :model="pwdForm" :rules="pwdRules" size="large">
        <el-form-item prop="identifier">
          <el-input
            v-model="pwdForm.identifier"
            placeholder="用户名 / 邮箱 / 手机号"
            prefix-icon="User"
            clearable
          />
        </el-form-item>
        <el-form-item prop="password">
          <el-input
            v-model="pwdForm.password"
            type="password"
            placeholder="密码"
            prefix-icon="Lock"
            show-password
            @keyup.enter="handlePasswordLogin"
          />
        </el-form-item>
        <el-form-item>
          <el-checkbox v-model="rememberMe" class="remember-checkbox">记住我</el-checkbox>
        </el-form-item>
        <el-button ref="pwdLoginBtnRef" type="primary" class="submit-btn" :loading="loading" @click="handlePasswordLogin">
          登 录
        </el-button>
      </el-form>

      <el-form v-else-if="activeTab === 'code'" ref="codeFormRef" :model="codeForm" :rules="codeRules" size="large">
        <el-form-item prop="target">
          <el-input
            v-model="codeForm.target"
            placeholder="手机号 或 邮箱"
            prefix-icon="Phone"
            clearable
          />
        </el-form-item>
        <el-form-item prop="code">
          <div class="code-row">
            <el-input
              v-model="codeForm.code"
              placeholder="6 位验证码"
              prefix-icon="Key"
              @keyup.enter="handleCodeLogin"
            />
            <el-button
              ref="sendCodeBtnRef"
              class="send-code-btn"
              :loading="sendingCode"
              :disabled="codeCooldown > 0"
              @click="sendCode">
              {{ codeCooldown > 0 ? `${codeCooldown}s` : '获取验证码' }}
            </el-button>
          </div>
        </el-form-item>
        <el-button ref="codeLoginBtnRef" type="primary" class="submit-btn" :loading="loading" @click="handleCodeLogin">
          登 录 / 注 册
        </el-button>
        <p class="code-hint">首次使用将自动注册账号</p>
      </el-form>

      <div v-else-if="activeTab === 'wechat'" class="wechat-area">
        <div class="wechat-qr-placeholder">
          <el-icon size="64" color="rgba(255,255,255,0.4)"><svg viewBox="0 0 24 24" fill="currentColor"><path d="M8.5 13a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm7 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zM12 2C6.48 2 2 6.12 2 11.2c0 3.15 1.62 5.95 4.14 7.77L5.5 22l3.43-1.72C9.89 20.72 10.92 21 12 21c5.52 0 10-4.12 10-9.2S17.52 2 12 2z"/></svg></el-icon>
          <p>微信扫码登录</p>
          <p style="font-size:12px;opacity:0.6">（需配置微信公众平台 OAuth）</p>
        </div>
      </div>

      <SlideCaptchaWidget ref="captchaRef" />
      <div class="auth-footer">
        还没有账号？<router-link to="/register" class="link">立即注册</router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/api/auth'
import SlideCaptchaWidget from '@/components/SlideCaptchaWidget.vue'

const REMEMBER_KEY = 'portal_remember_me'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const activeTab = ref('password')
const loading = ref(false)
const rememberMe = ref(false)
const sendingCode = ref(false)
const codeCooldown = ref(0)
const pwdLoginBtnRef = ref(null)
const sendCodeBtnRef = ref(null)
const codeLoginBtnRef = ref(null)
const captchaRef = ref(null)
const captchaConfig = ref({ enabled: false })
let captchaConfigPromise = Promise.resolve()

const loginTabs = [
  { key: 'password', label: '账号密码' },
  { key: 'code', label: '验证码登录' },
  { key: 'wechat', label: '微信登录' },
]

const pwdFormRef = ref(null)
const pwdForm = reactive({ identifier: '', password: '' })
const pwdRules = {
  identifier: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

const codeFormRef = ref(null)
const codeForm = reactive({ target: '', code: '' })
const codeRules = {
  target: [
    { required: true, message: '请输入手机号或邮箱', trigger: 'blur' },
    {
      validator: (_, value, callback) => {
        const phone = /^1[3-9]\d{9}$/.test(value)
        const email = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
        if (!phone && !email) callback(new Error('请输入有效手机号或邮箱'))
        else callback()
      },
      trigger: 'blur',
    },
  ],
  code: [
    { required: true, message: '请输入验证码', trigger: 'blur' },
    { len: 6, message: '验证码为 6 位', trigger: 'blur' },
  ],
}

function switchTab(key) {
  activeTab.value = key
}

function loadCaptchaConfig() {
  captchaConfigPromise = (async () => {
    try {
      captchaConfig.value = await authApi.getCaptchaConfig()
    } catch {
      captchaConfig.value = { enabled: false }
    }
  })()
}

onMounted(() => {
  try {
    const saved = localStorage.getItem(REMEMBER_KEY)
    if (saved) {
      const { identifier, password } = JSON.parse(saved)
      pwdForm.identifier = identifier || ''
      pwdForm.password = password ? atob(password) : ''
      rememberMe.value = true
    }
  } catch {
    localStorage.removeItem(REMEMBER_KEY)
  }

  if (route.query.identifier) {
    pwdForm.identifier = String(route.query.identifier)
  }

  loadCaptchaConfig()
})

async function handlePasswordLogin() {
  await pwdFormRef.value.validate()
  await captchaConfigPromise

  const doLogin = async (captchaToken) => {
    loading.value = true
    try {
      const user = await auth.login(pwdForm.identifier, pwdForm.password, captchaToken)
      if (rememberMe.value) {
        localStorage.setItem(REMEMBER_KEY, JSON.stringify({
          identifier: pwdForm.identifier,
          password: btoa(pwdForm.password),
        }))
      } else {
        localStorage.removeItem(REMEMBER_KEY)
      }
      ElMessage.success('登录成功')
      if (user.role === 'SuperAdmin' || user.role === 'Admin') {
        window.location.href = '/admin/'
        return
      }
      router.push(route.query.redirect || '/')
    } catch {
      // error handled by request interceptor
    } finally {
      loading.value = false
    }
  }

  if (captchaConfig.value.enabled) {
    captchaRef.value.open(pwdLoginBtnRef.value.$el, doLogin)
  } else {
    await doLogin(null)
  }
}

let cooldownTimer = null
async function sendCode() {
  await codeFormRef.value.validateField('target')
  if (!codeForm.target) return
  await captchaConfigPromise

  const doSend = async (captchaToken) => {
    sendingCode.value = true
    try {
      const res = await authApi.sendCode({
        target: codeForm.target,
        captchaToken,
      })
      ElMessage.success(res.message || '验证码已发送')
      if (res.devCode) {
        codeForm.code = res.devCode
        ElMessage.info(`[开发] 验证码：${res.devCode}`)
      }
      codeCooldown.value = 60
      clearInterval(cooldownTimer)
      cooldownTimer = setInterval(() => {
        codeCooldown.value--
        if (codeCooldown.value <= 0) clearInterval(cooldownTimer)
      }, 1000)
    } catch {
      // handled
    } finally {
      sendingCode.value = false
    }
  }

  if (captchaConfig.value.enabled) {
    captchaRef.value.open(sendCodeBtnRef.value.$el, doSend)
  } else {
    await doSend(null)
  }
}

async function handleCodeLogin() {
  await codeFormRef.value.validate()
  await captchaConfigPromise

  const doLogin = async (captchaToken) => {
    loading.value = true
    try {
      const result = await authApi.loginWithCode({
        target: codeForm.target,
        code: codeForm.code,
        tenantId: null,
        captchaToken,
      })
      await auth.loginWithResult(result)
      ElMessage.success('登录成功')
      if (result.role === 'SuperAdmin' || result.role === 'Admin') {
        window.location.href = '/admin/'
        return
      }
      router.push(route.query.redirect || '/')
    } catch {
      // handled
    } finally {
      loading.value = false
    }
  }

  if (captchaConfig.value.enabled) {
    captchaRef.value.open(codeLoginBtnRef.value.$el, doLogin)
  } else {
    await doLogin(null)
  }
}
</script>

<style scoped>
.auth-page {
  flex: 1;
  background: url('@/asset/bg.jfif') center/cover no-repeat;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60px 24px;
  position: relative;
}
.auth-page::before {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(30, 58, 138, 0.45);
}
.auth-card {
  position: relative;
  z-index: 1;
  background: rgba(255, 255, 255, 0.18);
  backdrop-filter: blur(18px);
  -webkit-backdrop-filter: blur(18px);
  border: 1px solid rgba(255, 255, 255, 0.35);
  border-radius: 20px;
  padding: 40px 40px 32px;
  width: 100%;
  max-width: 440px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.25);
  color: #fff;
}
.auth-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 20px;
  font-size: 18px;
  font-weight: 700;
  color: #fff;
}
.auth-title {
  font-size: 24px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 20px;
}
.login-tabs {
  display: flex;
  gap: 0;
  margin-bottom: 24px;
  background: rgba(0,0,0,0.2);
  border-radius: 10px;
  padding: 4px;
}
.login-tab {
  flex: 1;
  padding: 8px 0;
  background: transparent;
  border: none;
  border-radius: 8px;
  color: rgba(255,255,255,0.65);
  font-size: 14px;
  cursor: pointer;
  transition: all .2s;
}
.login-tab.active {
  background: rgba(255,255,255,0.25);
  color: #fff;
  font-weight: 600;
}
.code-row {
  display: flex;
  gap: 8px;
  width: 100%;
}
.code-row .el-input {
  flex: 1;
}
.send-code-btn {
  white-space: nowrap;
  min-width: 108px;
  border-radius: 8px;
}
.code-hint {
  margin: 8px 0 0;
  font-size: 12px;
  color: rgba(255,255,255,0.55);
  text-align: center;
}
.wechat-area {
  display: flex;
  justify-content: center;
  padding: 16px 0 8px;
}
.wechat-qr-placeholder {
  text-align: center;
  color: rgba(255,255,255,0.6);
  font-size: 14px;
  padding: 24px;
  border: 1px dashed rgba(255,255,255,0.3);
  border-radius: 12px;
  width: 200px;
}
.submit-btn {
  width: 100%;
  height: 44px;
  font-size: 16px;
  border-radius: 10px;
  margin-top: 4px;
}
.auth-footer {
  text-align: center;
  margin-top: 24px;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.8);
}
.link {
  color: #bfdbfe;
  font-weight: 500;
  margin-left: 4px;
}
.remember-checkbox :deep(.el-checkbox__label) {
  color: rgba(255, 255, 255, 0.85);
}
.remember-checkbox :deep(.el-checkbox__inner) {
  background-color: transparent;
  border-color: rgba(255, 255, 255, 0.6);
}
.remember-checkbox :deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
  background-color: #409eff;
  border-color: #409eff;
}
</style>
