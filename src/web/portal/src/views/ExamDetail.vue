<template>
  <div class="page-wrap container">
    <el-button :icon="ArrowLeft" @click="router.back()" class="back-btn">返回</el-button>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="6" animated />
    </div>

    <template v-else-if="exam">
      <!-- 考试信息卡 -->
      <div class="info-card">
        <div class="info-header">
          <el-tag :type="statusType(exam.status)" size="large">{{ statusLabel(exam.status) }}</el-tag>
          <h1 class="exam-title">{{ exam.title }}</h1>
          <p class="exam-desc">{{ exam.description || '该考试暂无说明' }}</p>
        </div>

        <el-divider />

        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">总分</span>
            <span class="info-value highlight">{{ exam.totalScore }} 分</span>
          </div>
          <div class="info-item">
            <span class="info-label">时长</span>
            <span class="info-value">{{ exam.durationMinutes }} 分钟</span>
          </div>
          <div class="info-item">
            <span class="info-label">题目数量</span>
            <span class="info-value">{{ exam.questions?.length ?? 0 }} 题</span>
          </div>
          <div class="info-item" v-if="exam.startTime">
            <span class="info-label">开始时间</span>
            <span class="info-value">{{ formatDate(exam.startTime) }}</span>
          </div>
          <div class="info-item" v-if="exam.endTime">
            <span class="info-label">截止时间</span>
            <span class="info-value">{{ formatDate(exam.endTime) }}</span>
          </div>
        </div>
      </div>

      <!-- 注意事项 -->
      <div class="notice-card">
        <h3><el-icon><Warning /></el-icon> 考试须知</h3>
        <ul>
          <li>请在规定时间内完成答题，超时将自动提交。</li>
          <li>客观题（单选、多选、判断）提交后即时评分；简答题由 AI 辅助批改。</li>
          <li v-if="exam.antiCheatingEnabled">
            <strong>本场考试已开启防作弊检测</strong>，切换至其他窗口将被记录。
          </li>
          <li>每道题目提交后不可修改，请仔细核对后再交卷。</li>
        </ul>
      </div>

      <!-- 操作按钮 -->
      <div class="action-area">
        <template v-if="!auth.isLoggedIn">
          <el-alert type="warning" :closable="false" show-icon>
            请先 <router-link to="/login" class="alert-link">登录</router-link> 后参加考试
          </el-alert>
        </template>
        <template v-else-if="exam.status === 3">
          <router-link :to="`/results/${exam.id}`">
            <el-button type="primary" size="large" round>查看我的成绩</el-button>
          </router-link>
        </template>
        <template v-else-if="canTake">
          <router-link :to="`/exam/${exam.id}/room`">
            <el-button type="success" size="large" round class="take-btn">
              <el-icon><VideoPlay /></el-icon>&ensp;开始考试
            </el-button>
          </router-link>
        </template>
        <template v-else>
          <el-alert
            :type="exam.status === 1 ? 'info' : 'warning'"
            :closable="false"
            show-icon
            :title="noticeText"
          />
        </template>
      </div>
    </template>

    <el-empty v-else description="考试不存在" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { examsApi } from '@/api/exams'
import { ArrowLeft, Warning, VideoPlay } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const exam = ref(null)
const loading = ref(true)

function statusLabel(s) {
  return { 0: '草稿', 1: '报名中', 2: '进行中', 3: '已结束', 4: '已取消' }[s] ?? ''
}
function statusType(s) {
  return { 1: 'success', 2: 'warning', 3: 'info', 4: 'danger' }[s] ?? ''
}
function formatDate(d) {
  return d ? new Date(d).toLocaleString('zh-CN') : ''
}

const canTake = computed(() => {
  if (!exam.value) return false
  if (exam.value.status !== 1 && exam.value.status !== 2) return false
  const now = Date.now()
  if (exam.value.startTime && new Date(exam.value.startTime) > now) return false
  if (exam.value.endTime && new Date(exam.value.endTime) < now) return false
  return true
})

const noticeText = computed(() => {
  if (!exam.value) return ''
  if (exam.value.status === 4) return '本场考试已取消'
  if (exam.value.startTime && new Date(exam.value.startTime) > Date.now()) {
    return `考试尚未开始，开始时间：${formatDate(exam.value.startTime)}`
  }
  if (exam.value.endTime && new Date(exam.value.endTime) < Date.now()) {
    return '考试时间已截止'
  }
  return '当前不可参加考试'
})

onMounted(async () => {
  try {
    exam.value = await examsApi.getById(route.params.id)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.page-wrap {
  padding: 32px 24px 60px;
  max-width: 800px;
}
.back-btn {
  margin-bottom: 24px;
}
.loading-wrap {
  padding: 40px 0;
}
.info-card {
  background: #fff;
  border-radius: 16px;
  padding: 32px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.06);
  margin-bottom: 20px;
}
.info-header {
  margin-bottom: 8px;
}
.info-header .el-tag {
  margin-bottom: 14px;
}
.exam-title {
  font-size: 26px;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 10px;
  line-height: 1.3;
}
.exam-desc {
  font-size: 15px;
  color: #64748b;
  line-height: 1.7;
}
.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 16px;
}
.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.info-label {
  font-size: 12px;
  color: #94a3b8;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.info-value {
  font-size: 16px;
  font-weight: 600;
  color: #1e293b;
}
.info-value.highlight {
  color: #1d4ed8;
  font-size: 20px;
}
.notice-card {
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: 14px;
  padding: 22px 24px;
  margin-bottom: 24px;
}
.notice-card h3 {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #92400e;
  font-size: 15px;
  margin-bottom: 12px;
}
.notice-card ul {
  padding-left: 20px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.notice-card li {
  font-size: 14px;
  color: #78350f;
  line-height: 1.6;
}
.action-area {
  margin-top: 8px;
}
.take-btn {
  padding: 14px 48px;
  font-size: 16px;
}
.alert-link {
  color: #1d4ed8;
  font-weight: 600;
  margin: 0 4px;
}
</style>
