<template>
  <div class="auth-page">
    <div class="auth-card container">
      <div class="auth-logo">
        <svg width="36" height="36" viewBox="0 0 24 24" fill="white" aria-hidden="true">
          <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3z"/>
          <path d="M5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" opacity="0.8"/>
        </svg>
        <span>在线考试系统</span>
      </div>
      <h2 class="auth-title">创建账号</h2>
      <p class="auth-sub">注册后即可参加在线考试</p>

      <el-form ref="formRef" :model="form" :rules="rules" size="large">
        <el-form-item prop="role">
          <div class="role-row">
            <button
              type="button"
              class="role-btn"
              :class="{ active: form.role === 'Student' }"
              @click="form.role = 'Student'">
              <span class="role-icon">🎓</span>
              <span class="role-label">我是学生</span>
            </button>
            <button
              type="button"
              class="role-btn"
              :class="{ active: form.role === 'Teacher' }"
              @click="form.role = 'Teacher'">
              <span class="role-icon">📚</span>
              <span class="role-label">我是教师</span>
            </button>
          </div>
        </el-form-item>

        <el-form-item prop="tenantId">
          <el-select
            v-model="form.tenantId"
            :placeholder="`选择${form.role === 'Teacher' ? '任教' : '就读'}机构`"
            :loading="tenantsLoading"
            style="width:100%"
            filterable>
            <el-option
              v-for="t in tenants"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item prop="username">
          <el-input v-model="form.username" placeholder="用户名（4-20位字符）" prefix-icon="User" clearable />
        </el-form-item>
        <el-form-item prop="phoneNumber">
          <el-input v-model="form.phoneNumber" placeholder="手机号（选填）" prefix-icon="Phone" clearable />
        </el-form-item>
        <el-form-item prop="email">
          <el-input v-model="form.email" placeholder="邮箱（选填）" prefix-icon="Message" clearable />
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="form.password" type="password" placeholder="密码（至少6位）" prefix-icon="Lock" show-password />
        </el-form-item>
        <el-form-item prop="confirmPassword">
          <el-input v-model="form.confirmPassword" type="password" placeholder="确认密码" prefix-icon="Lock" show-password @keyup.enter="handleRegister" />
        </el-form-item>

        <el-button ref="registerBtnRef" type="primary" class="submit-btn" :loading="loading" @click="handleRegister">
          注 册
        </el-button>
      </el-form>

      <SlideCaptchaWidget ref="captchaRef" />

      <div class="auth-footer">
        已有账号？<router-link to="/login" class="link">立即登录</router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { authApi } from '@/api/auth'
import SlideCaptchaWidget from '@/components/SlideCaptchaWidget.vue'

const router = useRouter()
const formRef = ref(null)
const loading = ref(false)
const tenantsLoading = ref(false)
const tenants = ref([])
const registerBtnRef = ref(null)
const captchaRef = ref(null)
const captchaConfig = ref({ enabled: false })
let captchaConfigPromise = Promise.resolve()

const form = reactive({
  role: 'Student',
  tenantId: null,
  username: '',
  phoneNumber: '',
  email: '',
  password: '',
  confirmPassword: '',
})

const rules = {
  role: [{ required: true, message: '请选择身份', trigger: 'change' }],
  tenantId: [{ required: true, message: '请选择所属机构', trigger: 'change' }],
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 4, max: 20, message: '用户名长度为 4-20 位', trigger: 'blur' },
  ],
  phoneNumber: [{
    validator: (_, value, callback) => {
      if (value && !/^1[3-9]\d{9}$/.test(value)) callback(new Error('请输入有效手机号'))
      else callback()
    },
    trigger: 'blur',
  }],
  email: [{ type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    {
      validator: (_, value, callback) => {
        if (value !== form.password) callback(new Error('两次输入的密码不一致'))
        else callback()
      },
      trigger: 'blur',
    },
  ],
}

async function handleRegister() {
  await formRef.value.validate()
  await captchaConfigPromise

  const doRegister = async (captchaToken) => {
    loading.value = true
    try {
      await authApi.register({
        tenantId: form.tenantId,
        username: form.username,
        password: form.password,
        email: form.email || null,
        phoneNumber: form.phoneNumber || null,
        role: form.role,
        captchaToken,
      })
      ElMessage.success('注册成功，请完成登录。')
      router.push({ path: '/login', query: { identifier: form.username } })
    } catch {
      // handled by interceptor
    } finally {
      loading.value = false
    }
  }

  if (captchaConfig.value.enabled) {
    captchaRef.value.open(registerBtnRef.value.$el, doRegister)
  } else {
    await doRegister(null)
  }
}

onMounted(() => {
  captchaConfigPromise = (async () => {
    tenantsLoading.value = true
    try {
      const [tenantList, captcha] = await Promise.all([
        authApi.getPublicTenants(),
        authApi.getCaptchaConfig(),
      ])
      tenants.value = tenantList
      captchaConfig.value = captcha
    } catch {
      captchaConfig.value = { enabled: false }
    } finally {
      tenantsLoading.value = false
    }
  })()
})
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
  max-width: 460px;
  margin-left: auto;
  margin-right: auto;
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
  margin-bottom: 6px;
}
.auth-sub {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.75);
  margin-bottom: 24px;
}
.role-row {
  display: flex;
  gap: 12px;
  width: 100%;
}
.role-btn {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 14px 8px;
  background: rgba(255,255,255,0.12);
  border: 2px solid rgba(255,255,255,0.25);
  border-radius: 12px;
  cursor: pointer;
  transition: all .2s;
  color: rgba(255,255,255,0.7);
}
.role-btn.active {
  background: rgba(64,158,255,0.35);
  border-color: #409eff;
  color: #fff;
}
.role-icon {
  font-size: 24px;
}
.role-label {
  font-size: 14px;
  font-weight: 500;
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
</style>
