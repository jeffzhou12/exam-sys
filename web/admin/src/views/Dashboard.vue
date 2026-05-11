<template>
  <div class="page-container">
    <h3 class="page-title">仪表盘</h3>

    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col v-for="stat in stats" :key="stat.label" :xs="12" :sm="6">
        <el-card shadow="hover" class="stat-card" :style="{ borderTop: `4px solid ${stat.color}` }">
          <div class="stat-content">
            <div class="stat-value">{{ stat.value }}</div>
            <div class="stat-label">{{ stat.label }}</div>
          </div>
          <el-icon class="stat-icon" :style="{ color: stat.color }" :size="40">
            <component :is="stat.icon" />
          </el-icon>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <!-- 最近试卷 -->
      <el-col :span="14">
        <el-card shadow="never">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center">
              <span>最近试卷</span>
              <el-button text size="small" @click="$router.push('/exam-papers')">查看全部</el-button>
            </div>
          </template>
          <el-table v-loading="loadingPapers" :data="recentPapers" :show-header="true" size="small">
            <el-table-column prop="title" label="试卷标题" min-width="180" />
            <el-table-column label="状态" width="90">
              <template #default="{ row }">
                <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="questionCount" label="题数" width="60" />
            <el-table-column prop="createdAt" label="创建时间" width="100">
              <template #default="{ row }">{{ shortDate(row.createdAt) }}</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <!-- 用户信息 -->
      <el-col :span="10">
        <el-card shadow="never">
          <template #header><span>当前登录信息</span></template>
          <el-descriptions :column="1" border>
            <el-descriptions-item label="用户名">{{ auth.user?.username }}</el-descriptions-item>
            <el-descriptions-item label="角色">
              <el-tag :type="roleTagType" size="small">{{ roleLabel }}</el-tag>
            </el-descriptions-item>
            <el-descriptions-item v-if="auth.tenantId" label="租户 ID">
              <el-text truncated style="max-width: 200px">{{ auth.tenantId }}</el-text>
            </el-descriptions-item>
          </el-descriptions>

          <el-divider />
          <h4 style="margin: 0 0 12px">快捷入口</h4>
          <el-space wrap>
            <el-button v-if="auth.isSuperAdmin" @click="$router.push('/tenants')">租户管理</el-button>
            <el-button v-if="auth.isAnyAdmin" @click="$router.push('/users')">用户管理</el-button>
            <el-button v-if="auth.isAdminOrTeacher" @click="$router.push('/exam-papers/create')">新建试卷</el-button>
            <el-button v-if="auth.isAdminOrTeacher" @click="$router.push('/questions')">题库管理</el-button>
          </el-space>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { examPapersApi } from '@/api/examPapers'
import { questionsApi } from '@/api/questions'
import { tenantsApi } from '@/api/tenants'
import { usersApi } from '@/api/users'
import {
  Document, QuestionFilled, OfficeBuilding, User
} from '@element-plus/icons-vue'

const auth = useAuthStore()

const loadingPapers = ref(false)
const recentPapers = ref([])

const stats = ref([
  { label: '试卷总数', value: '-', color: '#409eff', icon: Document },
  { label: '题目总数', value: '-', color: '#67c23a', icon: QuestionFilled },
  { label: '租户数量', value: '-', color: '#e6a23c', icon: OfficeBuilding },
  { label: '用户数量', value: '-', color: '#f56c6c', icon: User }
])

const roleLabel = computed(() => {
  const map = { Admin: '管理员', Teacher: '教师', Student: '学生' }
  return map[auth.role] || auth.role
})
const roleTagType = computed(() => ({ Admin: 'danger', Teacher: 'warning', Student: 'info' }[auth.role] || ''))

const statusLabel = (s) => ['草稿', '已发布', '进行中', '已结束', '已取消'][s] ?? s
const statusTagType = (s) => ['info', 'success', 'warning', 'default', 'danger'][s] ?? 'info'

function shortDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleDateString('zh-CN')
}

async function loadStats() {
  try {
    const [papers, questions] = await Promise.all([
      examPapersApi.getList({ page: 1, pageSize: 1 }),
      questionsApi.getList({ page: 1, pageSize: 1 })
    ])
    stats.value[0].value = papers.totalCount ?? '-'
    stats.value[1].value = questions.totalCount ?? '-'
  } catch { /* ignore */ }

  if (auth.isSuperAdmin) {
    try {
      const [tenants, users] = await Promise.all([
        tenantsApi.getList({ page: 1, pageSize: 1 }),
        usersApi.getList({ page: 1, pageSize: 1 })
      ])
      stats.value[2].value = tenants.totalCount ?? '-'
      stats.value[3].value = users.totalCount ?? '-'
    } catch { /* ignore */ }
  }
}

async function loadRecentPapers() {
  loadingPapers.value = true
  try {
    const res = await examPapersApi.getList({ page: 1, pageSize: 5 })
    recentPapers.value = res.items
  } finally {
    loadingPapers.value = false
  }
}

onMounted(() => {
  loadStats()
  loadRecentPapers()
})
</script>

<style scoped>
.page-container { padding: 20px; }
.page-title { margin: 0 0 20px; font-size: 20px; }
.stats-row { margin-bottom: 16px; }
.stat-card {
  cursor: default;
}
.stat-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px;
}
.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #333;
}
.stat-label {
  font-size: 13px;
  color: #999;
  margin-top: 4px;
}
.stat-icon {
  opacity: 0.15;
}
</style>
