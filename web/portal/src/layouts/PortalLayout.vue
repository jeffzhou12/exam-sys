<template>
  <div class="portal-app">
    <!-- 顶部导航 -->
    <header class="nav-header">
      <div class="container nav-inner">
        <router-link to="/" class="nav-logo">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3z"/>
            <path d="M5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" opacity="0.75"/>
          </svg>
          <span>在线考试</span>
        </router-link>

        <nav class="nav-links">
          <router-link to="/" :class="{ active: $route.name === 'Home' }">首页</router-link>
          <router-link to="/exams" :class="{ active: $route.name === 'ExamList' }">考试中心</router-link>
          <router-link
            v-if="auth.isLoggedIn && auth.isStudent"
            to="/my-results"
            :class="{ active: $route.name === 'MyResults' }">
            我的成绩
          </router-link>
          <router-link
            v-if="auth.isLoggedIn"
            to="/practice"
            :class="{ active: ['PracticeSetup','PracticeRoom','PracticeResult'].includes($route.name) }">
            在线练习
          </router-link>
          <router-link
            v-if="auth.isLoggedIn"
            to="/wrong-book"
            :class="{ active: $route.name === 'WrongBook' }">
            错题本
          </router-link>
          <router-link
            v-if="auth.isLoggedIn"
            to="/messages"
            :class="{ active: $route.name === 'Messages' }">
            站内信
          </router-link>
          <router-link
            v-if="auth.isLoggedIn"
            to="/books"
            :class="{ active: $route.name === 'BookList' }">
            图书馆
          </router-link>
          <router-link
            v-if="auth.isTeacher"
            to="/teacher/exams"
            :class="{ active: $route.name === 'TeacherExams' || $route.name === 'TeacherExamResults' }">
            查阅考试
          </router-link>
          <a
            v-if="auth.isAdmin"
            :href="buildAdminUrl()"
            target="_blank"
            class="admin-link">
            管理后台
          </a>
        </nav>

        <div class="nav-actions">
          <template v-if="auth.isLoggedIn">
            <el-dropdown @command="handleCommand">
              <span class="user-trigger">
                <el-avatar :size="30" class="user-avatar">
                  {{ auth.user?.username?.[0]?.toUpperCase() }}
                </el-avatar>
                <span class="username">{{ auth.user?.username }}</span>
                <el-icon><ArrowDown /></el-icon>
              </span>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item v-if="auth.isStudent" command="results">
                    <el-icon><DataAnalysis /></el-icon> 我的成绩
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isLoggedIn" command="practice">
                    <el-icon><Memo /></el-icon> 在线练习
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isLoggedIn" command="wrongBook">
                    <el-icon><Collection /></el-icon> 错题本
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isLoggedIn" command="messages">
                    <el-icon><Message /></el-icon> 站内信
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isTeacher" command="teacherExams">
                    <el-icon><Document /></el-icon> 查阅考试
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isAdmin" command="admin">
                    <el-icon><Setting /></el-icon> 管理后台
                  </el-dropdown-item>
                  <el-dropdown-item divided command="logout">
                    <el-icon><SwitchButton /></el-icon> 退出登录
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
          <template v-else>
            <router-link to="/login">
              <el-button>登录</el-button>
            </router-link>
            <router-link to="/register">
              <el-button type="primary">注册</el-button>
            </router-link>
          </template>
        </div>
      </div>
    </header>

    <!-- 主内容 -->
    <main class="portal-main">
      <router-view />
    </main>

    <!-- 页脚 -->
    <footer class="portal-footer">
      <div class="container footer-inner">
        <span>© 2026 在线考试系统 &nbsp;|&nbsp; 高效 · 智能 · 公正</span>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ArrowDown, DataAnalysis, SwitchButton, Setting, Document, Memo, Collection, Message } from '@element-plus/icons-vue'

const router = useRouter()
const auth = useAuthStore()

const adminUrl = import.meta.env.VITE_ADMIN_URL || 'http://localhost:3000'

function buildAdminUrl() {
  if (!auth.token) return adminUrl
  const params = new URLSearchParams({
    sso_token: auth.token,
    sso_user: btoa(JSON.stringify(auth.user)),
  })
  return `${adminUrl}?${params}`
}

function handleCommand(cmd) {
  if (cmd === 'logout') {
    auth.logout()
    router.push('/')
  } else if (cmd === 'results') {
    router.push('/my-results')
  } else if (cmd === 'teacherExams') {
    router.push('/teacher/exams')
  } else if (cmd === 'admin') {
    window.open(buildAdminUrl(), '_blank')
  } else if (cmd === 'practice') {
    router.push('/practice')
  } else if (cmd === 'wrongBook') {
    router.push('/wrong-book')
  } else if (cmd === 'messages') {
    router.push('/messages')
  }
}
</script>

<style scoped>
.portal-app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.nav-header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: #fff;
  box-shadow: 0 1px 8px rgba(0, 0, 0, 0.08);
}

.nav-inner {
  height: 60px;
  display: flex;
  align-items: center;
  gap: 32px;
}

.nav-logo {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 18px;
  font-weight: 700;
  color: #1d4ed8;
  white-space: nowrap;
}

.nav-links {
  display: flex;
  gap: 28px;
  flex: 1;
}

.nav-links a {
  font-size: 15px;
  color: #606266;
  transition: color 0.2s;
  position: relative;
  padding-bottom: 2px;
}

.nav-links a:hover,
.nav-links a.active {
  color: #1d4ed8;
}

.nav-links a.active::after {
  content: '';
  position: absolute;
  bottom: -4px;
  left: 0;
  right: 0;
  height: 2px;
  background: #1d4ed8;
  border-radius: 2px;
}

.nav-links .admin-link {
  color: #d97706;
  font-weight: 600;
}

.nav-links .admin-link:hover {
  color: #b45309;
}

.nav-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
}

.user-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  color: #303133;
  outline: none;
}

.user-avatar {
  background: #1d4ed8;
  color: #fff;
  font-weight: 600;
  font-size: 13px;
}

.username {
  font-size: 14px;
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.portal-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.portal-footer {
  background: #1e293b;
  padding: 20px 0;
}

.footer-inner {
  text-align: center;
  color: #94a3b8;
  font-size: 13px;
}
</style>
