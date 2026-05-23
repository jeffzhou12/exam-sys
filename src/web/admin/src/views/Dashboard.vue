<template>
  <div class="dashboard">

    <!-- 欢迎横幅 -->
    <div class="welcome-bar">
      <div>
        <h2 class="welcome-title">你好，{{ auth.user?.username }} 👋</h2>
        <div class="welcome-sub">
          {{ todayStr }}
          <el-tag size="small" :type="roleTagType" class="welcome-role">{{ roleLabel }}</el-tag>
        </div>
      </div>
      <div v-if="auth.activeTenantName" class="welcome-tenant">
        <el-icon><OfficeBuilding /></el-icon>
        {{ auth.activeTenantName }}
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="14" class="stats-row">
      <el-col v-for="s in visibleStats" :key="s.label" :xs="12" :sm="8" :md="4">
        <div class="stat-card">
          <div class="stat-accent" :style="`background:${s.color}`" />
          <div class="stat-row">
            <div>
              <div class="stat-value">{{ s.value }}</div>
              <div class="stat-label">{{ s.label }}</div>
            </div>
            <div class="stat-icon" :style="`background:${s.color}18;color:${s.color}`">
              <el-icon :size="22"><component :is="s.icon" /></el-icon>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 快捷入口 -->
    <el-card shadow="never" class="data-card shortcut-card">
      <template #header>
        <div class="card-hd">
          <span class="card-hd-title">
            <el-icon class="hd-icon"><Grid /></el-icon>快捷入口
          </span>
        </div>
      </template>
      <div class="shortcut-grid">
        <button
          v-for="item in shortcuts"
          :key="item.label"
          class="sc-btn"
          @click="$router.push(item.to)"
        >
          <span class="sc-icon" :style="`background:${item.color}18;color:${item.color}`">
            <el-icon :size="18"><component :is="item.icon" /></el-icon>
          </span>
          <span class="sc-label">{{ item.label }}</span>
        </button>
      </div>
    </el-card>

    <!-- 数据模块网格（始终两列，新增模块自动换行） -->
    <div class="data-grid">

      <!-- 最近试卷 -->
      <el-card shadow="never" class="data-card">
        <template #header>
          <div class="card-hd">
            <span class="card-hd-title">
              <el-icon class="hd-icon"><Document /></el-icon>最近试卷
            </span>
            <el-button text type="primary" size="small" @click="$router.push('/exam-papers')">查看全部</el-button>
          </div>
        </template>
        <el-table v-loading="loading.papers" :data="recentPapers" size="small">
          <el-table-column prop="title" label="试卷标题" min-width="120" show-overflow-tooltip />
          <el-table-column label="状态" width="78">
            <template #default="{ row }">
              <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="questionCount" label="题数" width="55" align="center" />
          <el-table-column prop="totalScore" label="总分" width="55" align="center" />
          <el-table-column label="创建时间" width="88">
            <template #default="{ row }">{{ shortDate(row.createdAt) }}</template>
          </el-table-column>
        </el-table>
      </el-card>

      <!-- 最近图书 -->
      <el-card shadow="never" class="data-card">
        <template #header>
          <div class="card-hd">
            <span class="card-hd-title">
              <el-icon class="hd-icon"><Reading /></el-icon>最近图书
            </span>
            <el-button text type="primary" size="small" @click="$router.push('/books')">查看全部</el-button>
          </div>
        </template>
        <div v-loading="loading.books" class="book-list">
          <div
            v-for="book in recentBooks"
            :key="book.id"
            class="book-item"
            @click="$router.push('/books')"
          >
            <div class="book-cover">
              <img v-if="book.coverImageUrl" :src="book.coverImageUrl" class="book-thumb" />
              <el-icon v-else :size="18" color="#c0c4cc"><Reading /></el-icon>
            </div>
            <div class="book-info">
              <div class="book-title">{{ book.title }}</div>
              <div class="book-sub">
                <span>{{ book.author }}</span>
                <el-tag
                  :type="book.isActive ? 'success' : 'info'"
                  size="small"
                  class="book-status"
                >{{ book.isActive ? '上架' : '下架' }}</el-tag>
              </div>
            </div>
          </div>
          <div v-if="!loading.books && recentBooks.length === 0" class="empty-hint">暂无图书数据</div>
        </div>
      </el-card>

      <!-- 最近题目 -->
      <el-card shadow="never" class="data-card">
        <template #header>
          <div class="card-hd">
            <span class="card-hd-title">
              <el-icon class="hd-icon"><QuestionFilled /></el-icon>最近题目
            </span>
            <el-button text type="primary" size="small" @click="$router.push('/questions')">查看全部</el-button>
          </div>
        </template>
        <div v-loading="loading.questions" class="question-list">
          <div v-for="q in recentQuestions" :key="q.id" class="q-item">
            <el-tag size="small" :type="qTypeColor(q.type)" class="q-tag">{{ qTypeLabel(q.type) }}</el-tag>
            <span class="q-content">{{ q.content }}</span>
          </div>
          <div v-if="!loading.questions && recentQuestions.length === 0" class="empty-hint">暂无题目数据</div>
        </div>
      </el-card>

      <!-- 最新消息 -->
      <el-card shadow="never" class="data-card">
        <template #header>
          <div class="card-hd">
            <span class="card-hd-title">
              <el-icon class="hd-icon"><Message /></el-icon>最新消息
            </span>
            <el-button
              v-if="auth.isAnyAdmin"
              text
              type="primary"
              size="small"
              @click="$router.push('/messages')"
            >查看全部</el-button>
          </div>
        </template>
        <div v-loading="loading.messages" class="message-list">
          <div v-for="msg in recentMessages" :key="msg.id" class="msg-item">
            <div class="msg-head">
              <span class="msg-subject">{{ msg.subject || '无主题' }}</span>
              <el-tag v-if="!msg.isRead" size="small" type="primary" effect="light">未读</el-tag>
            </div>
            <div class="msg-meta">
              <span>{{ msg.senderName || '系统' }} → {{ msg.recipientName || '我' }}</span>
              <span>{{ shortDateTime(msg.createdAt) }}</span>
            </div>
            <div class="msg-body">{{ msg.body }}</div>
          </div>
          <div v-if="!loading.messages && recentMessages.length === 0" class="empty-hint">暂无消息</div>
        </div>
      </el-card>

    </div>

  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { examPapersApi } from '@/api/examPapers'
import { questionsApi } from '@/api/questions'
import { booksApi } from '@/api/books'
import { tenantsApi } from '@/api/tenants'
import { usersApi } from '@/api/users'
import { messagesApi } from '@/api/messages'
import {
  Document, QuestionFilled, OfficeBuilding, User,
  Reading, Message, Cpu, DataAnalysis, Edit, Grid
} from '@element-plus/icons-vue'

const auth = useAuthStore()

// ── 日期 ─────────────────────────────────────────────────────────────────────
const todayStr = new Date().toLocaleDateString('zh-CN', {
  year: 'numeric', month: 'long', day: 'numeric', weekday: 'long'
})

// ── 角色标签 ──────────────────────────────────────────────────────────────────
const roleLabel = computed(() => (
  { SuperAdmin: '超级管理员', Admin: '管理员', Teacher: '教师', Student: '学生' }[auth.role] || auth.role
))
const roleTagType = computed(() => (
  { SuperAdmin: 'danger', Admin: 'warning', Teacher: 'primary' }[auth.role] || 'info'
))

// ── 统计数字 ──────────────────────────────────────────────────────────────────
const counts = reactive({
  papers: '-', questions: '-', books: '-',
  users: '-', tenants: '-', messages: '-'
})

const visibleStats = computed(() => [
  { label: '试卷总数', value: counts.papers,   color: '#3b82f6', icon: Document       },
  { label: '题目总数', value: counts.questions, color: '#10b981', icon: QuestionFilled },
  { label: '图书总数', value: counts.books,     color: '#8b5cf6', icon: Reading        },
  ...(auth.isAnyAdmin   ? [{ label: '用户数量', value: counts.users,   color: '#f43f5e', icon: User           }] : []),
  ...(auth.isSuperAdmin ? [{ label: '租户数量', value: counts.tenants, color: '#f59e0b', icon: OfficeBuilding }] : []),
  { label: '收件箱',   value: counts.messages,  color: '#06b6d4', icon: Message        },
])

// ── 快捷入口 ──────────────────────────────────────────────────────────────────
const shortcuts = computed(() => [
  { label: '试卷管理', to: '/exam-papers',        color: '#3b82f6', icon: Document,       show: true },
  { label: '新建试卷', to: '/exam-papers/create', color: '#10b981', icon: Edit,           show: auth.isAdminOrTeacher },
  { label: '题库管理', to: '/questions',          color: '#f59e0b', icon: QuestionFilled, show: auth.isAdminOrTeacher },
  { label: '图书管理', to: '/books',              color: '#8b5cf6', icon: Reading,        show: auth.isAdminOrTeacher },
  { label: '用户管理', to: '/users',              color: '#f43f5e', icon: User,           show: auth.isAnyAdmin },
  { label: '消息管理', to: '/messages',           color: '#06b6d4', icon: Message,        show: auth.isAnyAdmin },
  { label: 'AI 配置',  to: '/ai-configs',         color: '#7c3aed', icon: Cpu,            show: auth.isAnyAdmin },
  { label: '租户管理', to: '/tenants',            color: '#f59e0b', icon: OfficeBuilding, show: auth.isSuperAdmin },
  { label: '审计日志', to: '/audit-logs',         color: '#64748b', icon: DataAnalysis,   show: auth.isSuperAdmin },
].filter(s => s.show))

// ── 加载统计数字 ──────────────────────────────────────────────────────────────
async function loadCounts() {
  const tasks = [
    examPapersApi.getList({ page: 1, pageSize: 1 })
      .then(r => { counts.papers = r.totalCount ?? '-' }).catch(() => {}),
    questionsApi.getList({ page: 1, pageSize: 1 })
      .then(r => { counts.questions = r.totalCount ?? '-' }).catch(() => {}),
    booksApi.getList({ page: 1, pageSize: 1 })
      .then(r => { counts.books = r.totalCount ?? '-' }).catch(() => {}),
    messagesApi.getAll({ page: 1, pageSize: 1 })
      .then(r => {
        if (Array.isArray(r)) counts.messages = r.length
        else counts.messages = r.totalCount ?? (r.items?.length ?? '-')
      }).catch(() => {}),
  ]
  if (auth.isAnyAdmin) {
    tasks.push(
      usersApi.getList({ page: 1, pageSize: 1 })
        .then(r => { counts.users = r.totalCount ?? '-' }).catch(() => {})
    )
  }
  if (auth.isSuperAdmin) {
    tasks.push(
      tenantsApi.getList({ page: 1, pageSize: 1 })
        .then(r => { counts.tenants = r.totalCount ?? '-' }).catch(() => {})
    )
  }
  await Promise.all(tasks)
}

// ── 最近记录 ──────────────────────────────────────────────────────────────────
const loading = reactive({ papers: false, books: false, questions: false, messages: false })
const recentPapers    = ref([])
const recentBooks     = ref([])
const recentQuestions = ref([])
const recentMessages  = ref([])

async function loadRecentPapers() {
  loading.papers = true
  try {
    const res = await examPapersApi.getList({ page: 1, pageSize: 6 })
    recentPapers.value = res.items || []
  } catch { recentPapers.value = [] }
  finally { loading.papers = false }
}

async function loadRecentBooks() {
  loading.books = true
  try {
    const res = await booksApi.getList({ page: 1, pageSize: 6 })
    recentBooks.value = res.items || []
  } catch { recentBooks.value = [] }
  finally { loading.books = false }
}

async function loadRecentQuestions() {
  loading.questions = true
  try {
    const res = await questionsApi.getList({ page: 1, pageSize: 6 })
    recentQuestions.value = res.items || []
  } catch { recentQuestions.value = [] }
  finally { loading.questions = false }
}

async function loadRecentMessages() {
  loading.messages = true
  try {
    const res = await messagesApi.getAll({ page: 1, pageSize: 6 })
    recentMessages.value = Array.isArray(res) ? res.slice(0, 6) : (res.items || [])
  } catch {
    recentMessages.value = []
  } finally {
    loading.messages = false
  }
}

// ── 辅助格式化 ────────────────────────────────────────────────────────────────
const statusLabel   = s => ['草稿', '已发布', '进行中', '已结束', '已取消'][s] ?? s
const statusTagType = s => ['info', 'success', 'warning', 'default', 'danger'][s] ?? 'info'

const qTypeLabel = t => ({ 1: '单选', 2: '多选', 3: '判断', 4: '简答' }[t] ?? '?')
const qTypeColor = t => ({ 1: '', 2: 'warning', 3: 'info', 4: 'success' }[t] ?? '')

function shortDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleDateString('zh-CN', { month: '2-digit', day: '2-digit' })
}

function shortDateTime(val) {
  if (!val) return '-'
  return new Date(val).toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

onMounted(() => {
  loadCounts()
  loadRecentPapers()
  loadRecentBooks()
  loadRecentQuestions()
  loadRecentMessages()
})
</script>

<style scoped>
/* ── 整体 ── */
.dashboard { padding: 0; }

/* ── 欢迎横幅 ── */
.welcome-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  background: linear-gradient(135deg, #1d4ed8 0%, #2563eb 55%, #3b82f6 100%);
  border-radius: 12px;
  color: #fff;
  margin-bottom: 16px;
}
.welcome-title {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  line-height: 1.3;
}
.welcome-sub {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.75);
  margin-top: 5px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.welcome-role {
  background: rgba(255, 255, 255, 0.2) !important;
  border-color: transparent !important;
  color: #fff !important;
}
.welcome-tenant {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.15);
  padding: 7px 14px;
  border-radius: 20px;
  white-space: nowrap;
}

/* ── 统计卡片 ── */
.stats-row { margin-bottom: 14px; }

.stat-card {
  position: relative;
  background: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 10px;
  padding: 16px 16px 14px;
  margin-bottom: 14px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  transition: box-shadow 0.2s, transform 0.2s;
  cursor: default;
}
.stat-card:hover {
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.09);
  transform: translateY(-2px);
}
.stat-accent {
  position: absolute;
  top: 0; left: 0; right: 0;
  height: 3px;
}
.stat-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 6px;
}
.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #1e293b;
  line-height: 1;
}
.stat-label {
  font-size: 12px;
  color: #94a3b8;
  margin-top: 5px;
}
.stat-icon {
  width: 46px;
  height: 46px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

/* ── 快捷入口卡片 ── */
.shortcut-card { margin-bottom: 14px !important; }

/* ── 数据模块网格（两列，自动换行）── */
.data-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

/* ── 数据卡片通用 ── */
.data-card { margin-bottom: 0; }
.data-card :deep(.el-card__header) {
  padding: 12px 16px;
  border-bottom: 1px solid #f0f2f5;
}
.data-card :deep(.el-card__body) { padding: 12px 16px; }

.card-hd {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.card-hd-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: #1e293b;
}
.hd-icon { color: #64748b; }

/* ── 快捷入口 ── */
.shortcut-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(92px, 1fr));
  gap: 8px;
  padding: 2px 0;
}
.sc-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px 6px 10px;
  border: 1px solid #e8edf2;
  border-radius: 9px;
  cursor: pointer;
  background: #fafafa;
  transition: all 0.18s;
  gap: 6px;
  outline: none;
}
.sc-btn:hover {
  border-color: var(--el-color-primary-light-5);
  background: var(--el-color-primary-light-9);
  transform: translateY(-2px);
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.08);
}
.sc-icon {
  width: 38px;
  height: 38px;
  border-radius: 9px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.sc-label {
  font-size: 12px;
  color: #475569;
  white-space: nowrap;
}

/* ── 图书列表 ── */
.book-list { display: flex; flex-direction: column; }
.book-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 4px;
  border-bottom: 1px solid #f0f2f5;
  cursor: pointer;
  transition: background 0.15s;
  border-radius: 4px;
}
.book-item:last-child { border-bottom: none; }
.book-item:hover { background: #f5f7fa; }
.book-cover {
  width: 34px;
  height: 46px;
  border-radius: 4px;
  background: #f0f2f5;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}
.book-thumb { width: 100%; height: 100%; object-fit: cover; }
.book-info { min-width: 0; flex: 1; }
.book-title {
  font-size: 13px;
  font-weight: 500;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.book-sub {
  font-size: 12px;
  color: #909399;
  margin-top: 3px;
  display: flex;
  align-items: center;
  gap: 6px;
}
.book-status { flex-shrink: 0; }

/* ── 题目列表 ── */
.question-list { display: flex; flex-direction: column; }
.q-item {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 8px 4px;
  border-bottom: 1px solid #f0f2f5;
}
.q-item:last-child { border-bottom: none; }
.q-tag { flex-shrink: 0; margin-top: 1px; }
.q-content {
  font-size: 13px;
  color: #303133;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
}

/* ── 消息列表 ── */
.message-list { display: flex; flex-direction: column; }
.msg-item {
  padding: 8px 4px;
  border-bottom: 1px solid #f0f2f5;
}
.msg-item:last-child { border-bottom: none; }
.msg-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.msg-subject {
  font-size: 13px;
  font-weight: 600;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.msg-meta {
  margin-top: 4px;
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  color: #909399;
}
.msg-body {
  margin-top: 4px;
  color: #606266;
  font-size: 12px;
  line-height: 1.55;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* ── 通用空状态 ── */
.empty-hint {
  text-align: center;
  padding: 20px 0;
  color: #c0c4cc;
  font-size: 13px;
}
</style>
