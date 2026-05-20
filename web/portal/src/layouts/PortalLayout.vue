<template>
  <div class="portal-app">
    <!-- 顶部导航 -->
    <header class="nav-header" :class="{ 'nav-scrolled': isScrolled }">
      <div class="container nav-inner">
        <!-- Logo -->
        <router-link to="/" class="nav-logo">
          <div class="logo-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="white" aria-hidden="true">
              <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3z"/>
              <path d="M5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" opacity="0.8"/>
            </svg>
          </div>
          <span class="logo-text">学考平台</span>
        </router-link>

        <!-- 主导航 -->
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
            to="/books"
            :class="{ active: $route.name === 'BookList' }">
            图书馆
          </router-link>
          <router-link
            v-if="auth.isLoggedIn"
            to="/messages"
            :class="{ active: $route.name === 'Messages' }">
            <span>站内信</span>
          </router-link>
          <router-link
            v-if="auth.isTeacher"
            to="/teacher/exams"
            :class="{ active: $route.name === 'TeacherExams' || $route.name === 'TeacherExamResults' }">
            查阅考试
          </router-link>
        </nav>

        <!-- 右侧操作区 -->
        <div class="nav-actions">
          <!-- 超级管理员租户切换器 -->
          <div v-if="auth.isLoggedIn && auth.isSuperAdmin" class="tenant-switcher">
            <el-icon size="13" class="tenant-icon"><OfficeBuilding /></el-icon>
            <el-select
              :model-value="auth.activeTenantId"
              placeholder="切换租户"
              clearable
              size="small"
              class="tenant-select"
              @change="handleTenantChange"
            >
              <el-option
                v-for="t in tenants"
                :key="t.id"
                :label="t.name"
                :value="t.id"
              />
            </el-select>
          </div>

          <!-- 后台管理入口（管理员） -->
          <a
            v-if="auth.isLoggedIn && auth.isAnyAdmin"
            href="/admin/"
            class="admin-entry-btn"
          >
            <el-icon size="13"><Setting /></el-icon>
            管理后台
          </a>

          <template v-if="auth.isLoggedIn">
            <el-dropdown trigger="click" @command="handleCommand">
              <div class="user-trigger">
                <el-avatar :size="32" class="user-avatar">
                  {{ auth.user?.username?.[0]?.toUpperCase() }}
                </el-avatar>
                <span class="username">{{ auth.user?.username }}</span>
                <el-icon class="arrow-icon"><ArrowDown /></el-icon>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <div class="dropdown-user-info">
                    <el-avatar :size="40" class="dropdown-avatar">
                      {{ auth.user?.username?.[0]?.toUpperCase() }}
                    </el-avatar>
                    <div>
                      <div class="dropdown-username">{{ auth.user?.username }}</div>
                      <el-tag size="small" :type="roleTagType" class="dropdown-role">{{ roleLabel }}</el-tag>
                    </div>
                  </div>
                  <el-divider style="margin: 6px 0" />
                  <el-dropdown-item v-if="auth.isStudent" command="results">
                    <el-icon><DataAnalysis /></el-icon> 我的成绩
                  </el-dropdown-item>
                  <el-dropdown-item command="practice">
                    <el-icon><Memo /></el-icon> 在线练习
                  </el-dropdown-item>
                  <el-dropdown-item command="wrongBook">
                    <el-icon><Collection /></el-icon> 错题本
                  </el-dropdown-item>
                  <el-dropdown-item command="messages">
                    <el-icon><Message /></el-icon> 站内信
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isTeacher" command="teacherExams">
                    <el-icon><Document /></el-icon> 查阅考试
                  </el-dropdown-item>
                  <el-dropdown-item v-if="auth.isAnyAdmin" command="admin">
                    <el-icon><Setting /></el-icon> 管理后台
                  </el-dropdown-item>
                  <el-divider style="margin: 6px 0" />
                  <el-dropdown-item command="logout" class="logout-item">
                    <el-icon><SwitchButton /></el-icon> 退出登录
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
          <template v-else>
            <router-link to="/login">
              <el-button plain class="nav-btn-login">登录</el-button>
            </router-link>
            <router-link to="/register">
              <el-button type="primary" class="nav-btn-register">免费注册</el-button>
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
        <div class="footer-brand">
          <div class="footer-logo-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="white"><path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3z"/></svg>
          </div>
          <span>学考平台</span>
        </div>
        <span class="footer-copy">© 2026 在线考试系统 &nbsp;·&nbsp; 高效 · 智能 · 公正</span>
        <div class="footer-links">
          <router-link to="/exams">考试中心</router-link>
          <router-link to="/books">图书馆</router-link>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { tenantsApi } from '@/api/tenants'
import {
  ArrowDown, DataAnalysis, SwitchButton, Setting, Document,
  Memo, Collection, Message, OfficeBuilding
} from '@element-plus/icons-vue'

const router = useRouter()
const auth = useAuthStore()

const isScrolled = ref(false)
const tenants = ref([])

// ── 角色标签 ────────────────────────────────────────────────────
const roleLabel = computed(() => {
  const map = { SuperAdmin: '超级管理员', Admin: '管理员', Teacher: '教师', Student: '学生' }
  return map[auth.role] || auth.role
})
const roleTagType = computed(() => {
  const map = { SuperAdmin: 'danger', Admin: 'warning', Teacher: 'primary', Student: '' }
  return map[auth.role] || 'info'
})

// ── 滚动检测 ────────────────────────────────────────────────────
function onScroll() { isScrolled.value = window.scrollY > 8 }

// ── 加载租户列表（仅超级管理员）───────────────────────────────────
async function loadTenants() {
  if (!auth.isSuperAdmin) return
  try {
    const res = await tenantsApi.getList({ page: 1, pageSize: 200 })
    tenants.value = res.items || []
  } catch { /* ignore */ }
}

// ── 租户切换（超级管理员专属）─────────────────────────────────────
function handleTenantChange(id) {
  const tenant = tenants.value.find(t => t.id === id)
  auth.setActiveTenant(id || null, tenant?.name || '')
  router.go(0)
}

// ── 实时登出检测（跨标签 localStorage 变化）───────────────────────
function onStorageChange(e) {
  if (e.key === 'exam-token' && !e.newValue) {
    auth.logout()
    router.push('/login')
  }
  // 租户切换同步
  if (e.key === 'exam-activeTenantId') {
    auth.activeTenantId = e.newValue || null
  }
}

// ── 命令处理 ────────────────────────────────────────────────────
function handleCommand(cmd) {
  if (cmd === 'logout') {
    auth.logout()
    router.push('/')
  } else if (cmd === 'results')     { router.push('/my-results') }
  else if (cmd === 'teacherExams')  { router.push('/teacher/exams') }
  else if (cmd === 'admin')         { window.location.href = '/admin/' }
  else if (cmd === 'practice')      { router.push('/practice') }
  else if (cmd === 'wrongBook')     { router.push('/wrong-book') }
  else if (cmd === 'messages')      { router.push('/messages') }
}

onMounted(() => {
  window.addEventListener('scroll', onScroll)
  window.addEventListener('storage', onStorageChange)
  loadTenants()
})

onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScroll)
  window.removeEventListener('storage', onStorageChange)
})
</script>

<style scoped>
/* ── 整体结构 ────────────────────────────────────────────────── */
.portal-app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg-page, #f5f7fa);
}

/* ── 导航栏 ─────────────────────────────────────────────────── */
.nav-header {
  position: sticky;
  top: 0;
  z-index: 200;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);
  transition: box-shadow 0.3s ease;
}
.nav-header.nav-scrolled {
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08);
}

.nav-inner {
  height: 64px;
  display: flex;
  align-items: center;
  gap: 0;
}

/* ── Logo ───────────────────────────────────────────────────── */
.nav-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
  flex-shrink: 0;
  margin-right: 36px;
}
.logo-icon {
  width: 34px;
  height: 34px;
  background: linear-gradient(135deg, #1d4ed8, #3b82f6);
  border-radius: 9px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 3px 10px rgba(29, 78, 216, 0.3);
}
.logo-text {
  font-size: 17px;
  font-weight: 800;
  color: #1e293b;
  letter-spacing: -0.3px;
}

/* ── 导航链接 ────────────────────────────────────────────────── */
.nav-links {
  display: flex;
  align-items: center;
  gap: 4px;
  flex: 1;
}
.nav-links a {
  font-size: 14px;
  font-weight: 500;
  color: #475569;
  padding: 6px 12px;
  border-radius: 8px;
  transition: all 0.2s ease;
  position: relative;
  white-space: nowrap;
}
.nav-links a:hover {
  color: #1d4ed8;
  background: rgba(29, 78, 216, 0.06);
}
.nav-links a.active {
  color: #1d4ed8;
  background: rgba(29, 78, 216, 0.08);
  font-weight: 600;
}
.nav-links a.active::after {
  content: '';
  position: absolute;
  bottom: -18px;
  left: 50%;
  transform: translateX(-50%);
  width: 20px;
  height: 2px;
  background: #1d4ed8;
  border-radius: 2px;
}

/* ── 右侧操作区 ─────────────────────────────────────────────── */
.nav-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
  flex-shrink: 0;
}

/* ── 租户切换器 ─────────────────────────────────────────────── */
.tenant-switcher {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  background: rgba(245, 158, 11, 0.08);
  border-radius: 8px;
  border: 1px solid rgba(245, 158, 11, 0.2);
}
.tenant-icon { color: #d97706; }
.tenant-select { width: 160px; }
:deep(.tenant-select .el-input__wrapper) {
  box-shadow: none !important;
  background: transparent;
  padding: 0 4px;
}

/* ── 管理后台入口按钮 ────────────────────────────────────────── */
.admin-entry-btn {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 6px 14px;
  background: linear-gradient(135deg, #f59e0b, #d97706);
  color: white;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.2s;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
}
.admin-entry-btn:hover {
  background: linear-gradient(135deg, #d97706, #b45309);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.4);
  transform: translateY(-1px);
}

/* ── 用户触发器 ─────────────────────────────────────────────── */
.user-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px 4px 4px;
  border-radius: 24px;
  border: 1px solid #e2e8f0;
  transition: all 0.2s;
  background: #fafafa;
}
.user-trigger:hover {
  border-color: #93c5fd;
  background: #eff6ff;
}
.user-avatar {
  background: linear-gradient(135deg, #1d4ed8, #3b82f6) !important;
  color: #fff !important;
  font-weight: 700;
  font-size: 13px;
  flex-shrink: 0;
}
.username {
  font-size: 13px;
  font-weight: 500;
  color: #374151;
  max-width: 88px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.arrow-icon { color: #9ca3af; font-size: 11px; }

/* ── 下拉菜单用户信息块 ──────────────────────────────────────── */
.dropdown-user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px 10px;
}
.dropdown-avatar {
  background: linear-gradient(135deg, #1d4ed8, #3b82f6) !important;
  color: #fff !important;
  font-weight: 700;
  flex-shrink: 0;
}
.dropdown-username {
  font-size: 14px;
  font-weight: 600;
  color: #1e293b;
  margin-bottom: 4px;
}
:deep(.logout-item) { color: #ef4444 !important; }

/* ── 登录/注册按钮 ──────────────────────────────────────────── */
.nav-btn-login  { border-color: #cbd5e1; color: #475569; }
.nav-btn-register { background: linear-gradient(135deg, #1d4ed8, #3b82f6); border: none; }

/* ── 主内容 ─────────────────────────────────────────────────── */
.portal-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

/* ── 页脚 ───────────────────────────────────────────────────── */
.portal-footer {
  background: linear-gradient(135deg, #0f172a, #1e293b);
  padding: 28px 0;
  margin-top: auto;
}
.footer-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}
.footer-brand {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 700;
  color: #e2e8f0;
}
.footer-logo-icon {
  width: 26px;
  height: 26px;
  background: linear-gradient(135deg, #1d4ed8, #3b82f6);
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.footer-copy {
  font-size: 12px;
  color: #64748b;
}
.footer-links {
  display: flex;
  gap: 20px;
}
.footer-links a {
  font-size: 12px;
  color: #64748b;
  transition: color 0.2s;
}
.footer-links a:hover { color: #94a3b8; }
</style>
