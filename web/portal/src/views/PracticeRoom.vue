<template>
  <div v-if="!questions.length" class="fullpage-loading">
    <el-icon class="spin" size="40" color="#1d4ed8"><Loading /></el-icon>
    <p>正在加载题目…</p>
  </div>

  <div v-else class="practice-room container">
    <!-- 顶部栏 -->
    <div class="practice-header">
      <el-button :icon="ArrowLeft" text @click="confirmExit">退出练习</el-button>
      <div class="header-progress">
        <span>第 {{ currentIdx + 1 }} / {{ questions.length }} 题</span>
        <el-progress
          :percentage="Math.round(((currentIdx + 1) / questions.length) * 100)"
          :show-text="false"
          style="width: 120px"
        />
      </div>
      <el-button type="primary" size="small" round :loading="submitting" @click="confirmSubmit">
        交卷
      </el-button>
    </div>

    <!-- 主体 -->
    <div class="practice-body">
      <!-- 题目导航侧栏 -->
      <aside class="question-nav">
        <div class="nav-title">答题卡</div>
        <div class="nav-grid">
          <div
            v-for="(q, idx) in questions"
            :key="q.id"
            class="nav-item"
            :class="{ answered: !!answers[q.id], current: idx === currentIdx }"
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

      <!-- 题目区 -->
      <main class="question-main">
        <div class="question-card" v-if="currentQ">
          <div class="question-meta">
            <el-tag size="small" type="info">{{ typeLabel(currentQ.type) }}</el-tag>
            <el-tag size="small" :type="diffTagType(currentQ.difficulty)">
              {{ '★'.repeat(currentQ.difficulty) }} 难度 {{ currentQ.difficulty }}
            </el-tag>
            <span v-if="currentQ.knowledgePoint" class="kp-tag">{{ currentQ.knowledgePoint }}</span>
          </div>

          <div class="question-content">{{ currentQ.content }}</div>

          <!-- 单选题 -->
          <el-radio-group
            v-if="currentQ.type === 1"
            v-model="answers[currentQ.id]"
            class="options-group">
            <el-radio
              v-for="opt in parsedOptions(currentQ)"
              :key="opt.key"
              :value="opt.key"
              class="option-item">
              <span class="option-key">{{ opt.key }}.</span>
              {{ opt.value }}
            </el-radio>
          </el-radio-group>

          <!-- 多选题 -->
          <el-checkbox-group
            v-else-if="currentQ.type === 2"
            v-model="multiAnswers[currentQ.id]"
            class="options-group">
            <el-checkbox
              v-for="opt in parsedOptions(currentQ)"
              :key="opt.key"
              :value="opt.key"
              class="option-item">
              <span class="option-key">{{ opt.key }}.</span>
              {{ opt.value }}
            </el-checkbox>
          </el-checkbox-group>

          <!-- 判断题 -->
          <el-radio-group
            v-else-if="currentQ.type === 3"
            v-model="answers[currentQ.id]"
            class="options-group">
            <el-radio value="True" class="option-item">✓ 正确</el-radio>
            <el-radio value="False" class="option-item">✗ 错误</el-radio>
          </el-radio-group>

          <!-- 简答题 -->
          <el-input
            v-else-if="currentQ.type === 4"
            v-model="answers[currentQ.id]"
            type="textarea"
            :rows="6"
            placeholder="请输入你的答案…"
            class="short-answer-input"
          />

          <!-- 辅助功能栏 -->
          <div class="question-helps">
            <el-button size="small" plain round @click="toggleAnswer">
              <el-icon><QuestionFilled /></el-icon>
              {{ answerShown ? '收起答案' : '查看参考答案' }}
            </el-button>
            <el-button size="small" plain round @click="msgVisible = true">
              <el-icon><ChatDotRound /></el-icon>
              求助教师
            </el-button>
            <el-button size="small" plain round @click="openAiExplain">
              <el-icon><MagicStick /></el-icon>
              AI 分析
            </el-button>
          </div>

          <!-- 参考答案面板 -->
          <div v-if="answerShown" class="answer-panel">
            <div v-if="answerLoading" class="answer-loading">加载中…</div>
            <template v-else-if="currentAnswer">
              <div class="answer-row">
                <span class="answer-label">参考答案：</span>
                <span class="answer-value">{{ currentAnswer.correctAnswer }}</span>
              </div>
              <div v-if="currentAnswer.explanation" class="answer-explanation">
                <span class="answer-label">解析：</span>{{ currentAnswer.explanation }}
              </div>
            </template>
            <div v-else class="answer-loading">暂无答案信息</div>
          </div>

          <!-- 题目操作 -->
          <div class="question-actions">
            <el-button
              v-if="currentIdx > 0"
              :icon="ArrowLeft"
              @click="currentIdx--">
              上一题
            </el-button>
            <el-button
              v-if="currentIdx < questions.length - 1"
              type="primary"
              @click="goNext">
              下一题 <el-icon><ArrowRight /></el-icon>
            </el-button>
            <el-button
              v-else
              type="success"
              :loading="submitting"
              @click="confirmSubmit">
              完成并交卷
            </el-button>
          </div>
        </div>

        <!-- 相似题目推荐 -->
        <SimilarQuestions
          v-if="currentQ && currentQ.knowledgePoint"
          :question="currentQ"
          :current-ids="questions.map(q => q.id)"
          @add-to-practice="addQuestion"
        />
      </main>
    </div>

    <!-- AI 悬浮解析按钮 -->
    <AiExplain ref="aiExplainRef" v-if="currentQ" :question-id="currentQ.id" />

    <!-- 求助教师对话框 -->
    <SendMessageDialog
      v-model:visible="msgVisible"
      :attached-questions="currentQ ? [currentQ] : []"
    />
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft, ArrowRight, Loading, QuestionFilled, ChatDotRound, MagicStick } from '@element-plus/icons-vue'
import { practiceApi } from '@/api/practice'
import SimilarQuestions from '@/components/SimilarQuestions.vue'
import AiExplain from '@/components/AiExplain.vue'
import SendMessageDialog from '@/components/SendMessageDialog.vue'

const router = useRouter()
const questions = ref([])
const currentIdx = ref(0)
const answers = reactive({})    // 单选/判断/简答 { questionId: string }
const multiAnswers = reactive({}) // 多选 { questionId: string[] }
const submitting = ref(false)

const currentQ = computed(() => questions.value[currentIdx.value] ?? null)
const answeredCount = computed(() =>
  questions.value.filter(q =>
    q.type === 2 ? (multiAnswers[q.id]?.length > 0) : !!answers[q.id]
  ).length
)

// ── 参考答案 & 辅助功能 ─────────────────────────────────────────────────────
const aiExplainRef = ref(null)
const answerShown = ref(false)
const answerLoading = ref(false)
const currentAnswer = ref(null)
const answerCache = reactive({})
const msgVisible = ref(false)

watch(currentIdx, () => {
  answerShown.value = false
  const id = currentQ.value?.id
  currentAnswer.value = id != null ? (answerCache[id] ?? null) : null
})

async function toggleAnswer() {
  if (!answerShown.value) {
    answerShown.value = true
    const id = currentQ.value?.id
    if (id && !(id in answerCache)) {
      answerLoading.value = true
      try {
        answerCache[id] = await practiceApi.getAnswer(id)
      } catch {
        ElMessage.error('获取答案失败')
        answerCache[id] = null
      } finally {
        answerLoading.value = false
      }
    }
    currentAnswer.value = id != null ? (answerCache[id] ?? null) : null
  } else {
    answerShown.value = false
  }
}

function openAiExplain() {
  aiExplainRef.value?.open()
}

const typeMap = { 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }
const typeLabel = (t) => typeMap[t] ?? '未知'

function diffTagType(d) {
  if (d <= 2) return 'success'
  if (d === 3) return 'warning'
  return 'danger'
}

function parsedOptions(q) {
  if (!q.options) return []
  try {
    const raw = typeof q.options === 'string' ? JSON.parse(q.options) : q.options
    if (Array.isArray(raw)) return raw.map((v, i) => ({ key: String.fromCharCode(65 + i), value: v }))
    return Object.entries(raw).map(([k, v]) => ({ key: k, value: v }))
  } catch { return [] }
}

function collectAnswer(q) {
  if (q.type === 2) return (multiAnswers[q.id] ?? []).sort().join(',')
  return answers[q.id] ?? ''
}

function goNext() {
  currentIdx.value++
}

function addQuestion(q) {
  if (questions.value.find(x => x.id === q.id)) {
    ElMessage.info('该题目已在练习列表中')
    return
  }
  questions.value.push(q)
  ElMessage.success('已加入本次练习')
}

function confirmExit() {
  ElMessageBox.confirm('确定要退出练习吗？当前作答将不会保存。', '退出确认', {
    confirmButtonText: '退出',
    cancelButtonText: '继续',
    type: 'warning',
  }).then(() => router.push('/practice')).catch(() => {})
}

async function confirmSubmit() {
  const unanswered = questions.value.length - answeredCount.value
  if (unanswered > 0) {
    await ElMessageBox.confirm(
      `还有 ${unanswered} 题未作答，确定提交吗？`,
      '提交确认',
      { confirmButtonText: '提交', cancelButtonText: '继续作答', type: 'warning' }
    ).catch(() => { throw new Error('cancel') })
  }
  await submitPractice()
}

async function submitPractice() {
  submitting.value = true
  try {
    const payload = questions.value.map(q => ({
      questionId: q.id,
      answer: collectAnswer(q),
    }))
    const result = await practiceApi.submit(payload)

    // 保存结果到 sessionStorage
    sessionStorage.setItem('practice-result', JSON.stringify(result))

    // 记录练习历史
    const setup = JSON.parse(sessionStorage.getItem('practice-setup') || '{}')
    const setupParams = JSON.parse(sessionStorage.getItem('practice-setup-params') || '{}')
    const history = JSON.parse(localStorage.getItem('practice-history') || '[]')
    history.unshift({
      count: result.items.length,
      correctRate: result.maxScore > 0 ? result.totalScore / result.maxScore : 0,
      correctCount: result.correctCount ?? null,
      totalScore: result.totalScore ?? null,
      maxScore: result.maxScore ?? null,
      typeName: setup.typeName,
      knowledgePoint: setup.knowledgePoint,
      type: setupParams.type ?? null,
      difficulty: setupParams.difficulty ?? null,
      setupCount: setupParams.count ?? 10,
      time: new Date().toISOString(),
    })
    localStorage.setItem('practice-history', JSON.stringify(history.slice(0, 20)))

    // 同步保存到服务端（失败不阻断流程）
    practiceApi.saveSession({
      count:          result.items.length,
      correctCount:   result.correctCount ?? 0,
      totalScore:     result.totalScore   ?? 0,
      maxScore:       result.maxScore     ?? 0,
      typeName:       setup.typeName      ?? null,
      knowledgePoint: setup.knowledgePoint ?? null,
      questionType:   setupParams.type    ?? null,
      difficulty:     setupParams.difficulty ?? null,
      setupCount:     setupParams.count   ?? 10,
    }).catch(() => { /* 网络失败时静默忽略，本地记录仍有效 */ })

    router.push('/practice/result')
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  try {
    const raw = sessionStorage.getItem('practice-questions')
    if (!raw) { router.push('/practice'); return }
    questions.value = JSON.parse(raw)
    // 初始化多选题答案数组
    questions.value.filter(q => q.type === 2).forEach(q => {
      multiAnswers[q.id] = []
    })
  } catch { router.push('/practice') }
})
</script>

<style scoped>
.fullpage-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  gap: 16px;
  color: #64748b;
}

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.practice-room {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #f8fafc;
}

.practice-header {
  position: sticky;
  top: 0;
  z-index: 50;
  background: #fff;
  box-shadow: 0 1px 6px rgba(0,0,0,.08);
  padding: 0 24px;
  height: 56px;
  display: flex;
  align-items: center;
  gap: 20px;
}

.header-progress {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 12px;
  justify-content: center;
  font-size: 14px;
  color: #475569;
}

.practice-body {
  display: flex;
  flex: 1;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
  padding: 24px 20px;
  gap: 24px;
}

.question-nav {
  width: 200px;
  flex-shrink: 0;
  background: #fff;
  border-radius: 12px;
  padding: 16px;
  border: 1px solid #e2e8f0;
  align-self: flex-start;
  position: sticky;
  top: 72px;
}

.nav-title {
  font-weight: 600;
  font-size: 14px;
  color: #374151;
  margin-bottom: 12px;
}

.nav-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 6px;
}

.nav-item {
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  border: 1px solid #e5e7eb;
  background: #f9fafb;
  color: #374151;
  transition: all .15s;
}

.nav-item.answered { background: #dbeafe; border-color: #93c5fd; color: #1d4ed8; }
.nav-item.current { background: #1d4ed8; border-color: #1d4ed8; color: #fff; }

.nav-legend {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #6b7280;
  flex-wrap: wrap;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 2px;
  display: inline-block;
}

.legend-dot.answered { background: #dbeafe; border: 1px solid #93c5fd; }
.legend-dot.unanswered { background: #f9fafb; border: 1px solid #e5e7eb; }

.question-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.question-card {
  background: #fff;
  border-radius: 12px;
  padding: 28px 32px;
  border: 1px solid #e2e8f0;
}

.question-meta {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.kp-tag {
  font-size: 12px;
  color: #6366f1;
  background: #ede9fe;
  padding: 2px 8px;
  border-radius: 4px;
}

.question-content {
  font-size: 16px;
  color: #1e293b;
  line-height: 1.75;
  margin-bottom: 24px;
  white-space: pre-wrap;
}

.options-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 24px;
}

.option-item {
  padding: 12px 16px;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  height: auto;
  transition: border-color .15s, background .15s;
}

.option-item:hover {
  border-color: #93c5fd;
  background: #f0f9ff;
}
/* 选项居左对齐 */
.options-group :deep(.el-radio),
.options-group :deep(.el-checkbox) {
  display: flex;
  align-items: flex-start;
  width: 100%;
  margin-right: 0;
}
.options-group :deep(.el-radio__label),
.options-group :deep(.el-checkbox__label) {
  white-space: normal;
  line-height: 1.6;
  text-align: left;
}

.option-key {
  font-weight: 600;
  color: #1d4ed8;
  margin-right: 6px;
}

.short-answer-input {
  margin-bottom: 24px;
}

.question-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 8px;
  padding-top: 16px;
  border-top: 1px solid #f1f5f9;
}
.question-helps {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px dashed #e2e8f0;
}
.answer-panel {
  margin-top: 12px;
  padding: 14px 18px;
  background: #f0fdf4;
  border: 1px solid #86efac;
  border-radius: 10px;
  font-size: 14px;
  line-height: 1.7;
}
.answer-row {
  display: flex;
  gap: 6px;
  margin-bottom: 6px;
}
.answer-label {
  font-weight: 600;
  color: #15803d;
  white-space: nowrap;
}
.answer-value {
  color: #1e293b;
}
.answer-explanation {
  color: #475569;
}
.answer-loading {
  color: #94a3b8;
  font-size: 13px;
}
</style>
