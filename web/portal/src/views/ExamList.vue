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
.page-wrap {
  padding: 40px 24px 60px;
}
.page-header {
  margin-bottom: 32px;
}
.page-header h1 {
  font-size: 30px;
  font-weight: 700;
  color: #1e293b;
}
.page-header p {
  color: #64748b;
  margin-top: 6px;
}
.filter-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
  margin-bottom: 28px;
}
.status-tabs {
  display: flex;
  gap: 8px;
}
.loading-wrap,
.empty-wrap {
  padding: 60px 0;
}
.exam-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}
.exam-card {
  background: #fff;
  border-radius: 14px;
  padding: 22px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.06);
  display: flex;
  flex-direction: column;
  gap: 10px;
  transition: box-shadow 0.2s;
}
.exam-card:hover {
  box-shadow: 0 6px 20px rgba(0,0,0,0.1);
}
.card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.card-score {
  font-size: 13px;
  color: #64748b;
}
.card-title {
  font-size: 16px;
  font-weight: 600;
  color: #1e293b;
}
.card-desc {
  font-size: 13px;
  color: #94a3b8;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.card-meta {
  display: flex;
  gap: 20px;
  font-size: 13px;
  color: #64748b;
}
.card-meta span {
  display: flex;
  align-items: center;
  gap: 4px;
}
.card-time {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #94a3b8;
}
.card-actions {
  display: flex;
  gap: 10px;
  margin-top: 4px;
}
.pagination {
  display: flex;
  justify-content: center;
  margin-top: 36px;
}
</style>
