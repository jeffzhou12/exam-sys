<template>
  <div class="page-wrap container">
    <div class="page-header">
      <h1>考试中心</h1>
      <p>选择一场考试，开始你的在线答题</p>
    </div>

    <!-- 筛选栏 -->
    <div class="filter-bar">
      <el-input
        v-model="keyword"
        placeholder="搜索考试名称"
        prefix-icon="Search"
        clearable
        style="width:260px"
        @input="onSearch"
      />
      <div class="status-tabs">
        <el-button
          v-for="tab in statusTabs"
          :key="tab.value"
          :type="activeStatus === tab.value ? 'primary' : ''"
          size="small"
          round
          @click="switchStatus(tab.value)">
          {{ tab.label }}
        </el-button>
      </div>
    </div>

    <!-- 列表 -->
    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="4" animated />
    </div>
    <div v-else-if="exams.length === 0" class="empty-wrap">
      <el-empty description="暂无符合条件的考试" />
    </div>
    <div v-else class="exam-grid">
      <div v-for="exam in exams" :key="exam.id" class="exam-card">
        <div class="card-top">
          <el-tag :type="statusType(exam.status)" size="small">{{ statusLabel(exam.status) }}</el-tag>
          <span class="card-score">{{ exam.totalScore }} 分</span>
        </div>
        <h3 class="card-title">{{ exam.title }}</h3>
        <p class="card-desc">{{ exam.description || '暂无描述' }}</p>
        <div class="card-meta">
          <span><el-icon><Clock /></el-icon> {{ exam.durationMinutes }} 分钟</span>
          <span><el-icon><Document /></el-icon> {{ exam.questionCount }} 题</span>
        </div>
        <div v-if="exam.startTime || exam.endTime" class="card-time">
          <el-icon><Calendar /></el-icon>
          {{ formatDateRange(exam.startTime, exam.endTime) }}
        </div>
        <div class="card-actions">
          <FavoriteButton :target-type="2" :target-id="exam.id" />
          <router-link :to="`/exams/${exam.id}`">
            <el-button type="primary" round size="small">查看详情</el-button>
          </router-link>
          <router-link v-if="canTake(exam)" :to="`/exam/${exam.id}/room`">
            <el-button type="success" round size="small">立即作答</el-button>
          </router-link>
        </div>
      </div>
    </div>

    <!-- 分页 -->
    <div class="pagination" v-if="total > pageSize">
      <el-pagination
        v-model:current-page="page"
        :page-size="pageSize"
        :total="total"
        layout="prev, pager, next"
        @current-change="loadData"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { examsApi } from '@/api/exams'
import { Clock, Document, Calendar } from '@element-plus/icons-vue'
import FavoriteButton from '@/components/FavoriteButton.vue'

const exams = ref([])
const loading = ref(false)
const keyword = ref('')
const activeStatus = ref(null)
const page = ref(1)
const pageSize = 12
const total = ref(0)
let searchTimer = null

const statusTabs = [
  { label: '全部', value: null },
  { label: '报名中', value: 1 },
  { label: '进行中', value: 2 },
  { label: '已结束', value: 3 },
]

function statusLabel(s) {
  return { 0: '草稿', 1: '报名中', 2: '进行中', 3: '已结束', 4: '已取消' }[s] ?? ''
}
function statusType(s) {
  return { 1: 'success', 2: 'warning', 3: 'info', 4: 'danger' }[s] ?? ''
}
function formatDateRange(start, end) {
  const fmt = (d) => d ? new Date(d).toLocaleString('zh-CN', { month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' }) : ''
  if (start && end) return `${fmt(start)} — ${fmt(end)}`
  if (start) return `${fmt(start)} 开始`
  if (end) return `截止 ${fmt(end)}`
  return ''
}
function canTake(exam) {
  if (exam.status !== 1 && exam.status !== 2) return false
  const now = Date.now()
  if (exam.startTime && new Date(exam.startTime) > now) return false
  if (exam.endTime && new Date(exam.endTime) < now) return false
  return true
}

async function loadData() {
  loading.value = true
  try {
    const res = await examsApi.getList({
      page: page.value,
      pageSize,
      status: activeStatus.value ?? undefined,
    })
    exams.value = keyword.value
      ? res.items.filter(e => e.title.includes(keyword.value))
      : res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

function switchStatus(val) {
  activeStatus.value = val
  page.value = 1
  loadData()
}

function onSearch() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    page.value = 1
    loadData()
  }, 400)
}

onMounted(loadData)
</script>

<style scoped>
/* ── 页面布局 ─────────────────────────────────────────── */
.page-wrap { padding: 48px 0 72px; }
.page-header { margin-bottom: 0; }
.page-header h1 {
  font-size: 32px;
  font-weight: 800;
  color: #0f172a;
  letter-spacing: -0.5px;
}
.page-header p {
  color: #64748b;
  margin-top: 6px;
  font-size: 15px;
}

/* ── 筛选栏 ──────────────────────────────────────────── */
.filter-bar {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
  padding: 20px 24px;
  background: #fff;
  border-radius: 16px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  margin: 28px 0;
}
.status-tabs {
  display: flex;
  gap: 6px;
}
:deep(.status-tabs .el-button) {
  border-radius: 8px;
  font-weight: 500;
}

/* ── 考试网格 ─────────────────────────────────────────── */
.loading-wrap, .empty-wrap { padding: 60px 0; }

.exam-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}

.exam-card {
  background: #fff;
  border-radius: 16px;
  padding: 22px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  display: flex;
  flex-direction: column;
  gap: 12px;
  transition: all 0.25s ease;
  position: relative;
  overflow: hidden;
}
.exam-card::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, #1d4ed8, #3b82f6);
  opacity: 0;
  transition: opacity 0.25s;
}
.exam-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(29,78,216,0.1);
  border-color: #bfdbfe;
}
.exam-card:hover::after { opacity: 1; }

.card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.card-score {
  font-size: 12px;
  font-weight: 600;
  color: #fff;
  background: linear-gradient(135deg, #1d4ed8, #3b82f6);
  padding: 3px 10px;
  border-radius: 20px;
}
.card-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.card-desc {
  font-size: 13px;
  color: #94a3b8;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.6;
}
.card-meta {
  display: flex;
  gap: 20px;
}
.card-meta span {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 13px;
  color: #64748b;
}
.card-time {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: #94a3b8;
}
.card-actions {
  display: flex;
  gap: 10px;
  margin-top: 4px;
  padding-top: 12px;
  border-top: 1px solid #f8fafc;
}

/* ── 分页 ────────────────────────────────────────────── */
.pagination { display: flex; justify-content: center; margin-top: 40px; }

/* ── 响应式 ──────────────────────────────────────────── */
@media (max-width: 768px) {
  .page-wrap { padding: 32px 0 48px; }
  .filter-bar { flex-direction: column; align-items: flex-start; }
  .exam-grid { grid-template-columns: 1fr; }
}
</style>
