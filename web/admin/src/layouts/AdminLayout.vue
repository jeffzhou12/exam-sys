<template>
  <el-container class="admin-layout">
    <!-- 侧边栏 -->
    <el-aside :width="isCollapsed ? '64px' : '220px'" class="sidebar">
      <div class="logo">
        <el-icon size="24"><School /></el-icon>
        <span v-show="!isCollapsed" class="logo-text">学考平台管理</span>
      </div>
      <el-menu
        :default-active="activeMenu"
        :collapse="isCollapsed"
        router
        background-color="#001529"
        text-color="#ffffffa6"
        active-text-color="#ffffff"
        class="sidebar-menu"
      >
        <el-menu-item index="/dashboard">
          <el-icon><DataBoard /></el-icon>
          <template #title>仪表盘</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isSuperAdmin" index="/tenants">
          <el-icon><OfficeBuilding /></el-icon>
          <template #title>租户管理</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isAnyAdmin" index="/users">
          <el-icon><User /></el-icon>
          <template #title>用户管理</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isAdminOrTeacher" index="/exam-papers">
          <el-icon><Document /></el-icon>
          <template #title>试卷管理</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isAdminOrTeacher" index="/questions">
          <el-icon><QuestionFilled /></el-icon>
          <template #title>题库管理</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isAdminOrTeacher" index="/books">
          <el-icon><Reading /></el-icon>
          <template #title>图书管理</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isAnyAdmin" index="/messages">
          <el-icon><ChatDotRound /></el-icon>
          <template #title>消息管理</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isAnyAdmin" index="/ai-configs">
          <el-icon><Setting /></el-icon>
          <template #title>AI 模型配置</template>
        </el-menu-item>

        <el-menu-item v-if="auth.isSuperAdmin" index="/audit-logs">
          <el-icon><Notebook /></el-icon>
          <template #title>审计日志</template>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <!-- 顶部导航 -->
      <el-header class="header">
        <div class="header-left">
          <el-button
            :icon="isCollapsed ? Expand : Fold"
            text
            size="large"
            @click="isCollapsed = !isCollapsed"
          />
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/dashboard' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item v-if="currentTitle">{{ currentTitle }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <!-- SuperAdmin 专属：租户切换器 -->
          <div v-if="auth.isSuperAdmin" class="tenant-switcher">
            <el-icon size="14" style="color: #999"><OfficeBuilding /></el-icon>
            <el-select
              :model-value="auth.activeTenantId"
              placeholder="选择租户"
              clearable
              size="small"
              style="width: 180px"
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

          <el-divider v-if="auth.isSuperAdmin" direction="vertical" />

          <el-dropdown @command="handleCommand">
            <div class="user-info">
              <el-avatar :size="32" :icon="UserFilled" />
              <span class="username">{{ auth.user?.username }}</span>
              <el-tag size="small" :type="roleTagType">{{ roleLabel }}</el-tag>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="logout">
                  <el-icon><SwitchButton /></el-icon>退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <!-- 主内容区 -->
      <el-main class="main-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { tenantsApi } from '@/api/tenants'
import { ElMessageBox, ElMessage } from 'element-plus'
import {
  DataBoard, OfficeBuilding, User, Document, QuestionFilled,
  Expand, Fold, UserFilled, SwitchButton, School, Reading,
  ChatDotRound, Notebook, Setting
} from '@element-plus/icons-vue'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const isCollapsed = ref(false)
const tenants = ref([])

const activeMenu = computed(() => {
  const path = route.path
  if (path.startsWith('/exam-papers')) return '/exam-papers'
  if (path.startsWith('/questions')) return '/questions'
  return path
})

const currentTitle = computed(() => route.meta?.title)

const roleLabel = computed(() => {
  const map = { SuperAdmin: '超级管理员', Admin: '管理员', Teacher: '教师', Student: '学生' }
  return map[auth.role] || auth.role
})

const roleTagType = computed(() => {
  const map = { SuperAdmin: 'danger', Admin: 'warning', Teacher: 'primary', Student: 'info' }
  return map[auth.role] || ''
})

// 加载租户列表（SuperAdmin 需要用于切换器）
async function loadTenants() {
  if (!auth.isSuperAdmin) return
  try {
    const res = await tenantsApi.getList({ page: 1, pageSize: 200 })
    tenants.value = res.items || []
    localStorage.setItem('admin.tenants.cache', JSON.stringify(tenants.value))
    window.dispatchEvent(new CustomEvent('admin-tenants-updated', { detail: tenants.value }))
  } catch { /* ignore */ }
}

function handleTenantChange(id) {
  const tenant = tenants.value.find(t => t.id === id)
  auth.setActiveTenant(id || null, tenant?.name || '')
  // 切换租户后刷新当前页面数据
  router.go(0)
}

async function handleCommand(cmd) {
  if (cmd === 'logout') {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    auth.logout()
    // 跳转到 portal 登录页（绕过 /admin/ base 前缀）
    window.location.href = '/login'
  }
}

onMounted(loadTenants)

// ── 实时登出检测（跨标签 localStorage 变化）───────────────────────
function onStorageChange(e) {
  if (e.key === 'exam-token' && !e.newValue) {
    auth.syncFromStorage()
    window.location.href = '/login'
  }
  // 租户切换同步（另一个标签页切换了租户）
  if (e.key === 'exam-activeTenantId') {
    auth.syncFromStorage()
    router.go(0)
  }
}

window.addEventListener('storage', onStorageChange)

</script>

<style scoped>
.admin-layout {
  height: 100vh;
}
.sidebar {
  background: #001529;
  transition: width 0.3s;
  overflow: hidden;
}
.logo {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: left;
  gap: 10px;
  color: #fff;
  font-size: 18px;
  font-weight: 600;
  border-bottom: 1px solid #ffffff1a;
  padding: 0 16px;
  overflow: hidden;
}
.logo-text {
  white-space: nowrap;
}
.sidebar-menu {
  border-right: none;
  height: calc(100vh - 60px);
}
.header {
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  border-bottom: 1px solid #f0f0f0;
  box-shadow: 0 1px 4px rgba(0,21,41,.08);
}
.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}
.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}
.tenant-switcher {
  display: flex;
  align-items: center;
  gap: 6px;
}
.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
  transition: background 0.2s;
}
.user-info:hover {
  background: #f5f5f5;
}
.username {
  font-size: 14px;
  color: #333;
}
.main-content {
  background: #f0f2f5;
  overflow-y: auto;
}
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
