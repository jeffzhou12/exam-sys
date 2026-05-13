<template>
  <div class="page-wrap container">
    <el-button :icon="ArrowLeft" @click="router.back()" class="back-btn">返回成绩列表</el-button>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="6" animated />
    </div>

    <template v-else-if="result">
      <!-- 成绩总览 -->
      <div class="summary-card">
        <div class="summary-left">
          <h1 class="exam-name">{{ examTitle }}</h1>
          <div v-if="result.submittedAt" class="submit-time">
            提交时间：{{ formatDate(result.submittedAt) }}
          </div>
        </div>
        <div class="summary-score">
          <div class="big-score">
            <span class="my-score" :class="scoreClass">{{ totalMyScore }}</span>
            <span class="divider">/</span>
            <span class="total">{{ result.totalScore }}</span>
          </div>
          <div class="score-pct">{{ scorePct }}</div>
          <div class="score-label">{{ scoreRemark }}</div>
        </div>
      </div>

      <!-- 各题得分 -->
      <div class="answers-section">
        <h2 class="section-title">答题详情</h2>
        <div v-for="(item, idx) in result.answers" :key="item.answerId" class="answer-card">
          <div class="answer-head">
            <span class="ans-index">第 {{ idx + 1 }} 题</span>
            <el-tag size="small" :type="gradingType(item.gradingStatus)">
              {{ gradingLabel(item.gradingStatus) }}
            </el-tag>
            <span class="ans-score">
              <template v-if="item.score !== null">
                得 <strong>{{ item.score }}</strong> / {{ item.maxScore }} 分
              </template>
              <template v-else>
                待评分（满分 {{ item.maxScore }} 分）
              </template>
            </span>
          </div>

          <div class="answer-question">
            <span class="field-label">题目：</span>
            <span>{{ item.questionContent }}</span>
          </div>

          <div class="answer-content">
            <span class="field-label">我的答案：</span>
            <span class="ans-text" :class="{ empty: !item.answerContent }">
              {{ item.answerContent || '（未作答）' }}
            </span>
          </div>

          <div v-if="item.aiFeedback" class="answer-feedback">
            <el-icon color="#8b5cf6"><Cpu /></el-icon>
            <span>AI 评语：{{ item.aiFeedback }}</span>
          </div>
        </div>
      </div>
    </template>

    <el-empty v-else description="暂无成绩记录" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { examsApi } from '@/api/exams'
import { ArrowLeft, Cpu } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const result = ref(null)
const examTitle = ref('')
const loading = ref(true)

const totalMyScore = computed(() =>
  result.value?.answers?.reduce((s, a) => s + (a.score ?? 0), 0) ?? 0,
)
const scorePct = computed(() => {
  if (!result.value?.totalScore) return ''
  return Math.round((totalMyScore.value / result.value.totalScore) * 100) + '%'
})
const scoreClass = computed(() => {
  if (!result.value?.totalScore) return ''
  const pct = totalMyScore.value / result.value.totalScore
  if (pct >= 0.9) return 'excellent'
  if (pct >= 0.6) return 'pass'
  return 'fail'
})
const scoreRemark = computed(() => {
  const pct = totalMyScore.value / (result.value?.totalScore || 1)
  if (pct >= 0.9) return '优秀'
  if (pct >= 0.75) return '良好'
  if (pct >= 0.6) return '及格'
  return '不及格'
})

function formatDate(d) {
  return d ? new Date(d).toLocaleString('zh-CN') : ''
}
function gradingLabel(s) {
  return { 0: '待评分', 1: '已自动评分', 2: 'AI 已评分', 3: '人工已评分' }[s] ?? ''
}
function gradingType(s) {
  return { 0: 'info', 1: 'success', 2: 'warning', 3: 'primary' }[s] ?? ''
}

onMounted(async () => {
  try {
    const [examData, resultData] = await Promise.all([
      examsApi.getById(route.params.examId),
      examsApi.getMyResult(route.params.examId, auth.user?.id),
    ])
    examTitle.value = examData.title
    result.value = resultData
  } catch {
    result.value = null
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.page-wrap {
  padding: 32px 24px 60px;
  max-width: 860px;
}
.back-btn { margin-bottom: 24px; }
.loading-wrap { padding: 40px 0; }

.summary-card {
  background: linear-gradient(135deg, #1e3a8a 0%, #1d4ed8 100%);
  border-radius: 18px;
  padding: 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 28px;
  color: #fff;
  flex-wrap: wrap;
}
.exam-name {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 8px;
}
.submit-time {
  font-size: 13px;
  opacity: 0.75;
}
.summary-score {
  text-align: center;
}
.big-score {
  display: flex;
  align-items: baseline;
  gap: 6px;
}
.my-score {
  font-size: 56px;
  font-weight: 800;
  line-height: 1;
}
.my-score.excellent { color: #86efac; }
.my-score.pass      { color: #93c5fd; }
.my-score.fail      { color: #fca5a5; }
.divider { font-size: 28px; opacity: 0.5; }
.total  { font-size: 22px; opacity: 0.7; }
.score-pct {
  font-size: 18px;
  opacity: 0.85;
  margin-top: 4px;
}
.score-label {
  font-size: 13px;
  opacity: 0.7;
}

.section-title {
  font-size: 20px;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 16px;
}

.answer-card {
  background: #fff;
  border-radius: 14px;
  padding: 20px 22px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.05);
  margin-bottom: 14px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.answer-head {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.ans-index {
  font-weight: 700;
  color: #1e293b;
}
.ans-score {
  margin-left: auto;
  font-size: 13px;
  color: #64748b;
}
.answer-question,
.answer-content {
  font-size: 14px;
  color: #475569;
  line-height: 1.7;
}
.field-label {
  font-weight: 600;
  color: #94a3b8;
  font-size: 12px;
  margin-right: 4px;
}
.ans-text {
  white-space: pre-wrap;
  word-break: break-all;
}
.ans-text.empty {
  color: #94a3b8;
  font-style: italic;
}
.answer-feedback {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  background: #faf5ff;
  border: 1px solid #e9d5ff;
  border-radius: 8px;
  padding: 10px 14px;
  font-size: 13px;
  color: #6b21a8;
  line-height: 1.6;
}
</style>
