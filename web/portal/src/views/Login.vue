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
      <p class="auth-sub">登录你的账号以参加考试</p>

      <el-form ref="formRef" :model="form" :rules="rules" size="large" @submit.prevent="handleLogin">
        <el-form-item prop="username">
          <el-input
            v-model="form.username"
            placeholder="用户名"
            prefix-icon="User"
            clearable
          />
        </el-form-item>
        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="密码"
            prefix-icon="Lock"
            show-password
            @keyup.enter="handleLogin"
          />
        </el-form-item>
        <el-form-item>
          <el-checkbox v-model="rememberMe" class="remember-checkbox">记住我</el-checkbox>
        </el-form-item>
        <el-button
          type="primary"
          class="submit-btn"
          :loading="loading"
          @click="handleLogin">
          登录
        </el-button>
      </el-form>

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

const REMEMBER_KEY = 'portal_remember_me'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const formRef = ref(null)
const loading = ref(false)
const rememberMe = ref(false)

const form = reactive({ username: '', password: '' })
const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

onMounted(() => {
  try {
    const saved = localStorage.getItem(REMEMBER_KEY)
    if (saved) {
      const { username, password } = JSON.parse(saved)
      form.username = username || ''
      form.password = password ? atob(password) : ''
      rememberMe.value = true
    }
  } catch {
    localStorage.removeItem(REMEMBER_KEY)
  }
})

async function handleLogin() {
  await formRef.value.validate()
  loading.value = true
  try {
    const user = await auth.login(form.username, form.password)
    if (rememberMe.value) {
      localStorage.setItem(REMEMBER_KEY, JSON.stringify({
        username: form.username,
        password: btoa(form.password)
      }))
    } else {
      localStorage.removeItem(REMEMBER_KEY)
    }
    ElMessage.success('登录成功')
    // 管理员和超级管理员直接进入后台
    if (user.role === 'SuperAdmin' || user.role === 'Admin') {
      window.location.href = '/admin/'
      return
    }
    const redirect = route.query.redirect || '/'
    router.push(redirect)
  } catch {
    // error handled by request interceptor
  } finally {
    loading.value = false
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
  padding: 48px 40px;
  width: 100%;
  max-width: 440px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.25);
  color: #fff;
}
.auth-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 28px;
  font-size: 18px;
  font-weight: 700;
  color: #fff;
}
.auth-title {
  font-size: 26px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 6px;
}
.auth-sub {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.75);
  margin-bottom: 28px;
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
