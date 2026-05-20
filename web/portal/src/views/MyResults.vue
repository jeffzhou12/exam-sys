<template>
  <div class="page-wrap container">
    <div class="page-header">
      <h1>我的成绩</h1>
      <p>查看所有参与过的考试及成绩</p>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="5" animated />
    </div>

    <el-empty v-else-if="exams.length === 0" description="暂无考试记录" style="padding:80px 0" />

    <div v-else class="results-list">
      <div v-for="item in exams" :key="item.id" class="result-card">
        <div class="result-left">
          <div class="result-title">{{ item.title }}</div>
          <div class="result-meta">
            <span v-if="item.endTime"><el-icon><Calendar /></el-icon> 截止 {{ formatDate(item.endTime) }}</span>
            <span><el-icon><Clock /></el-icon> {{ item.durationMinutes }} 分钟</span>
            <el-tag size="small" :type="statusType(item.status)">{{ statusLabel(item.status) }}</el-tag>
          </div>
        </div>
        <div class="result-score-area">
          <div class="score-display">
            <span class="my-score" :class="scoreClass(item.myScore, item.totalScore)">
              {{ item.myScore }}
            </span>
            <span class="total-score">/ {{ item.totalScore }}</span>
          </div>
          <div class="score-pct">{{ scorePct(item.myScore, item.totalScore) }}</div>
          <el-tag v-if="item.isPending" size="small" type="warning">部分待评分</el-tag>
        </div>
        <div class="result-actions">
          <router-link :to="`/results/${item.id}`">
            <el-button type="primary" round>查看详情</el-button>
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { examsApi } from '@/api/exams'
import { Calendar, Clock } from '@element-plus/icons-vue'

const exams = ref([])
const loading = ref(true)

function statusLabel(s) {
  return { 1: '报名中', 2: '进行中', 3: '已结束', 4: '已取消' }[s] ?? ''
}
function statusType(s) {
  return { 1: 'success', 2: 'warning', 3: 'info', 4: 'danger' }[s] ?? ''
}
function formatDate(d) {
  return d ? new Date(d).toLocaleString('zh-CN', { year: 'numeric', month: 'numeric', day: 'numeric' }) : ''
}
function scorePct(my, total) {
  if (!total) return ''
  return Math.round((my / total) * 100) + '%'
}
function scoreClass(my, total) {
  if (!total) return ''
  const pct = my / total
  if (pct >= 0.9) return 'score-excellent'
  if (pct >= 0.6) return 'score-pass'
  return 'score-fail'
}

onMounted(async () => {
  try {
    exams.value = await examsApi.getMyExams()
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.page-wrap {
  padding: 40px 24px 60px;
}
.page-header {
  margin-bottom: 32px;
}
.page-header h1 {
  font-size: 28px;
  font-weight: 700;
  color: #1e293b;
}
.page-header p {
  color: #64748b;
  margin-top: 6px;
}
.loading-wrap { padding: 40px 0; }
.results-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.result-card {
  background: #fff;
  border-radius: 14px;
  padding: 20px 24px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.06);
  display: flex;
  align-items: center;
  gap: 20px;
  transition: box-shadow 0.2s;
}
.result-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
.result-left {
  flex: 1;
  min-width: 0;
}
.result-title {
  font-size: 16px;
  font-weight: 600;
  color: #1e293b;
  margin-bottom: 8px;
}
.result-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 13px;
  color: #64748b;
  flex-wrap: wrap;
}
.result-meta span {
  display: flex;
  align-items: center;
  gap: 4px;
}
.result-score-area {
  text-align: center;
  min-width: 100px;
}
.score-display {
  font-size: 15px;
}
.my-score {
  font-size: 28px;
  font-weight: 800;
}
.score-excellent { color: #15803d; }
.score-pass      { color: #1d4ed8; }
.score-fail      { color: #dc2626; }
.total-score {
  color: #94a3b8;
  font-size: 14px;
}
.score-pct {
  font-size: 12px;
  color: #94a3b8;
  margin-top: 2px;
}
</style>
