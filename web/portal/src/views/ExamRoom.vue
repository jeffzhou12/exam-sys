<template>
  <!-- 加载中 -->
  <div v-if="pageLoading" class="fullpage-loading">
    <el-icon class="spin" size="40" color="#1d4ed8"><Loading /></el-icon>
    <p>正在加载试卷…</p>
  </div>

  <!-- 考试页面 -->
  <div v-else-if="paper" class="exam-room">
    <!-- 顶部固定栏 -->
    <div class="exam-header">
      <div class="header-title">{{ paper.title }}</div>
      <div class="header-timer" :class="{ warning: timeLeft < 300, danger: timeLeft < 60 }">
        <el-icon><AlarmClock /></el-icon>
        {{ formatTime(timeLeft) }}
      </div>
      <el-button type="danger" size="small" round :loading="submitting" @click="confirmSubmit">
        交卷
      </el-button>
    </div>

    <!-- 防作弊提示 -->
    <el-alert
      v-if="paper.antiCheatingEnabled && violations > 0"
      :title="`检测到 ${violations} 次切换窗口行为，请专注作答`"
      type="warning"
      :closable="false"
      class="cheat-alert"
    />

    <!-- 主体：侧边导航 + 题目区 -->
    <div class="exam-body">
      <!-- 题目导航 -->
      <aside class="question-nav">
        <div class="nav-title">答题卡</div>
        <div class="nav-grid">
          <div
            v-for="(q, idx) in questions"
            :key="q.questionId"
            class="nav-item"
            :class="{ answered: isAnswered(q), current: idx === currentIdx }"
            @click="currentIdx = idx">
            {{ idx + 1 }}
          </div>
        </div>
        <el-divider />
        <div class="nav-legend">
          <span class="legend-dot answered"></span> 已答 {{ answeredCount }}
          <span class="legend-dot unanswered"></span> 未答 {{ questions.length - answeredCount }}
        </div>
      </aside>

      <!-- 题目内容 -->
      <main class="question-area" v-if="currentQuestion">
        <div class="question-card">
          <!-- 题头 -->
          <div class="q-head">
            <span class="q-index">第 {{ currentIdx + 1 }} 题</span>
            <el-tag size="small">{{ typeLabel(currentQuestion.type) }}</el-tag>
            <span class="q-score">{{ currentQuestion.score }} 分</span>
          </div>

          <!-- 题目内容 -->
          <div class="q-content">{{ currentQuestion.content }}</div>

          <!-- 答题区 -->
          <div class="q-answer">
            <!-- 单选题 -->
            <el-radio-group
              v-if="currentQuestion.type === 1"
              v-model="answers[currentQuestion.questionId]"
              class="option-group">
              <el-radio
                v-for="(opt, i) in (currentQuestion.options || [])"
                :key="i"
                :value="opt"
                class="option-item">
                <span class="option-label">{{ optLabel(i) }}</span>{{ opt }}
              </el-radio>
              <el-radio v-if="!currentQuestion.options?.length" value="" disabled>
                （题目无选项，请联系教师）
              </el-radio>
            </el-radio-group>

            <!-- 多选题 -->
            <el-checkbox-group
              v-else-if="currentQuestion.type === 2"
              v-model="mcAnswers[currentQuestion.questionId]"
              class="option-group">
              <el-checkbox
                v-for="(opt, i) in (currentQuestion.options || [])"
                :key="i"
                :value="opt"
                class="option-item">
                <span class="option-label">{{ optLabel(i) }}</span>{{ opt }}
              </el-checkbox>
            </el-checkbox-group>

            <!-- 判断题 -->
            <el-radio-group
              v-else-if="currentQuestion.type === 3"
              v-model="answers[currentQuestion.questionId]"
              class="tf-group">
              <el-radio-button value="正确">✓ 正确</el-radio-button>
              <el-radio-button value="错误">✗ 错误</el-radio-button>
            </el-radio-group>

            <!-- 简答题 -->
            <el-input
              v-else
              v-model="answers[currentQuestion.questionId]"
              type="textarea"
              :rows="8"
              placeholder="请输入你的答案…"
              resize="none"
            />
          </div>

          <!-- 翻页按钮 -->
          <div class="q-nav-btns">
            <el-button :disabled="currentIdx === 0" @click="currentIdx--">
              <el-icon><ArrowLeft /></el-icon> 上一题
            </el-button>
            <span class="q-progress">{{ currentIdx + 1 }} / {{ questions.length }}</span>
            <el-button
              v-if="currentIdx < questions.length - 1"
              type="primary"
              @click="currentIdx++">
              下一题 <el-icon><ArrowRight /></el-icon>
            </el-button>
            <el-button v-else type="danger" :loading="submitting" @click="confirmSubmit">
              提交答案
            </el-button>
          </div>
        </div>
      </main>
    </div>
  </div>

  <div v-else class="fullpage-loading">
    <el-empty description="考试不存在或无法加载" />
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { examsApi } from '@/api/exams'
import { Loading, AlarmClock, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const paper = ref(null)
const questions = ref([])
const pageLoading = ref(true)
const submitting = ref(false)
const currentIdx = ref(0)
const violations = ref(0)
const timeLeft = ref(0)
const timerHandle = ref(null)

// 普通题答案（单选/判断/简答）
const answers = reactive({})
// 多选题答案
const mcAnswers = reactive({})

const currentQuestion = computed(() => questions.value[currentIdx.value])

const answeredCount = computed(() =>
  questions.value.filter((q) => isAnswered(q)).length,
)

function isAnswered(q) {
  if (q.type === 2) return (mcAnswers[q.questionId] || []).length > 0
  return !!(answers[q.questionId])
}

function typeLabel(t) {
  return { 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }[t] ?? '题目'
}
function optLabel(i) {
  return String.fromCharCode(65 + i) + '. '
}
function formatTime(s) {
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = s % 60
  const pad = (n) => String(n).padStart(2, '0')
  return h > 0 ? `${h}:${pad(m)}:${pad(sec)}` : `${pad(m)}:${pad(sec)}`
}

// ——— 计时器 ———
function startTimer() {
  timerHandle.value = setInterval(() => {
    timeLeft.value--
    if (timeLeft.value <= 0) {
      clearInterval(timerHandle.value)
      ElMessage.warning('时间到！正在自动提交…')
      doSubmit()
    }
  }, 1000)
}

// ——— 防作弊 ———
function onVisibilityChange() {
  if (document.hidden && paper.value?.antiCheatingEnabled) {
    violations.value++
    ElMessage.warning(`检测到切换窗口（第 ${violations.value} 次）`)
  }
}

// ——— 加载试卷 ———
async function loadExam() {
  try {
    const data = await examsApi.getById(route.params.id)
    paper.value = data
    questions.value = (data.questions || []).slice().sort((a, b) => a.order - b.order)

    // 初始化答案
    for (const q of questions.value) {
      if (q.type === 2) mcAnswers[q.questionId] = []
      else answers[q.questionId] = ''
    }

    // 计时器：用 localStorage 记录开始时间，刷新后继续倒计时
    const key = `exam-start-${route.params.id}-${auth.user?.id}`
    let startTs = parseInt(localStorage.getItem(key) || '0')
    if (!startTs) {
      startTs = Date.now()
      localStorage.setItem(key, String(startTs))
    }
    const elapsed = Math.floor((Date.now() - startTs) / 1000)
    const total = data.durationMinutes * 60
    timeLeft.value = Math.max(0, total - elapsed)

    if (timeLeft.value > 0) {
      startTimer()
    } else {
      ElMessage.warning('考试时间已到，正在提交…')
      await doSubmit()
    }
  } finally {
    pageLoading.value = false
  }
}

// ——— 构建提交 payload ———
function buildPayload() {
  return {
    studentId: auth.user?.id,
    answers: questions.value.map((q) => ({
      questionId: q.questionId,
      content:
        q.type === 2
          ? [...(mcAnswers[q.questionId] || [])].sort().join(',')
          : (answers[q.questionId] || ''),
    })),
  }
}

// ——— 提交 ———
async function doSubmit() {
  submitting.value = true
  clearInterval(timerHandle.value)
  try {
    await examsApi.submit(route.params.id, buildPayload())
    const key = `exam-start-${route.params.id}-${auth.user?.id}`
    localStorage.removeItem(key)
    ElMessage.success('提交成功！')
    router.replace({ name: 'ResultDetail', params: { examId: route.params.id } })
  } catch {
    ElMessage.error('提交失败，请重试')
    startTimer() // 恢复计时
  } finally {
    submitting.value = false
  }
}

async function confirmSubmit() {
  const unanswered = questions.value.length - answeredCount.value
  const msg = unanswered > 0
    ? `还有 ${unanswered} 道题未作答，确认交卷吗？`
    : `已完成全部 ${questions.value.length} 题，确认交卷？`
  await ElMessageBox.confirm(msg, '确认交卷', { type: 'warning', confirmButtonText: '确认交卷' })
  await doSubmit()
}

onMounted(() => {
  document.addEventListener('visibilitychange', onVisibilityChange)
  loadExam()
})
onUnmounted(() => {
  clearInterval(timerHandle.value)
  document.removeEventListener('visibilitychange', onVisibilityChange)
})
</script>

<style scoped>
.fullpage-loading {
  min-height: 60vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  color: #64748b;
}
.spin {
  animation: spin 1s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.exam-room {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f1f5f9;
}

/* 顶部栏 */
.exam-header {
  position: sticky;
  top: 0;
  z-index: 50;
  background: #1e293b;
  color: #fff;
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 0 24px;
  height: 56px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.2);
}
.header-title {
  flex: 1;
  font-size: 15px;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.header-timer {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 20px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: #94a3b8;
  min-width: 80px;
  justify-content: flex-end;
}
.header-timer.warning { color: #fbbf24; }
.header-timer.danger  { color: #f87171; animation: pulse 1s infinite; }
@keyframes pulse { 0%,100% { opacity:1; } 50% { opacity:0.5; } }

.cheat-alert {
  border-radius: 0;
}

/* 主体 */
.exam-body {
  flex: 1;
  display: flex;
  max-width: 1100px;
  margin: 24px auto;
  width: 100%;
  padding: 0 16px;
  gap: 20px;
  align-items: flex-start;
}

/* 侧边导航 */
.question-nav {
  width: 200px;
  flex-shrink: 0;
  background: #fff;
  border-radius: 14px;
  padding: 18px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.06);
  position: sticky;
  top: 76px;
}
.nav-title {
  font-size: 13px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.nav-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 6px;
}
.nav-item {
  aspect-ratio: 1;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  background: #f1f5f9;
  color: #475569;
  border: 2px solid transparent;
  transition: all 0.15s;
}
.nav-item:hover { background: #e2e8f0; }
.nav-item.answered { background: #dcfce7; color: #15803d; border-color: #bbf7d0; }
.nav-item.current  { border-color: #1d4ed8; color: #1d4ed8; }
.nav-legend {
  font-size: 12px;
  color: #94a3b8;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.legend-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 3px;
  margin-right: 5px;
}
.legend-dot.answered  { background: #dcfce7; border: 1px solid #bbf7d0; }
.legend-dot.unanswered { background: #f1f5f9; border: 1px solid #e2e8f0; }

/* 题目卡 */
.question-area {
  flex: 1;
  min-width: 0;
}
.question-card {
  background: #fff;
  border-radius: 16px;
  padding: 30px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.06);
}
.q-head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 18px;
}
.q-index {
  font-size: 15px;
  font-weight: 700;
  color: #1e293b;
}
.q-score {
  margin-left: auto;
  font-size: 13px;
  color: #64748b;
  background: #f1f5f9;
  padding: 2px 10px;
  border-radius: 20px;
}
.q-content {
  font-size: 17px;
  color: #1e293b;
  line-height: 1.8;
  margin-bottom: 28px;
  white-space: pre-wrap;
}
.q-answer {
  margin-bottom: 32px;
}
.option-group {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.option-item {
  padding: 12px 16px;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  width: 100%;
  height: auto;
  line-height: 1.6;
  transition: background 0.15s, border-color 0.15s;
}
.option-item:hover {
  background: #f8fafc;
  border-color: #93c5fd;
}
.option-label {
  font-weight: 600;
  color: #1d4ed8;
  margin-right: 4px;
}
.tf-group {
  display: flex;
  gap: 16px;
}
.q-nav-btns {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px solid #f1f5f9;
  padding-top: 20px;
}
.q-progress {
  font-size: 13px;
  color: #94a3b8;
}
</style>
