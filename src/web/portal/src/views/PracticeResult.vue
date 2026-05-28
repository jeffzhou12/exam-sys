<template>
  <div v-if="!result" class="empty-result">
    <p>暂无练习结果，请先完成一次练习。</p>
    <el-button type="primary" @click="$router.push('/practice')">去练习</el-button>
  </div>

  <div v-else class="practice-result container">
    <!-- 总览卡片 -->
    <div class="result-overview">
      <div class="overview-score">
        <el-progress
          type="circle"
          :percentage="scoreRate"
          :width="100"
          :color="scoreColor"
          :stroke-width="10"
        />
        <div class="overview-label">
          <div class="score-num">{{ result.totalScore }} / {{ result.maxScore }}</div>
          <div class="score-sub">正确 {{ result.correctCount }} 题</div>
        </div>
      </div>
      <div class="overview-actions">
        <el-button type="primary" @click="$router.push('/practice')">再次练习</el-button>
        <el-button @click="addAllWrong" :disabled="!wrongItems.length">
          一键加入错题本 ({{ wrongItems.length }})
        </el-button>
        <el-button @click="$router.push('/wrong-book')">查看错题本</el-button>
        <el-button
          type="warning"
          plain
          :loading="aiLoading"
          :disabled="!!aiResult"
          @click="doAiAnalyze"
        >
          <el-icon><Cpu /></el-icon>
          {{ aiResult ? 'AI 已分析' : 'AI 智能分析' }}
        </el-button>
      </div>
    </div>

    <!-- AI 分析结果 -->
    <div v-if="aiResult || aiLoading" class="ai-analysis-block">
      <div class="ai-block-title">
        <el-icon color="#8b5cf6" size="18"><Cpu /></el-icon>
        <span>AI 智能分析报告</span>
      </div>
      <el-skeleton v-if="aiLoading" :rows="5" animated />
      <div v-else class="ai-block-content" v-html="renderMarkdown(aiResult)" />
    </div>

    <!-- 题目详情列表 -->
    <div class="result-list">
      <div
        v-for="(item, idx) in result.items"
        :key="item.questionId"
        class="result-item"
        :class="{ wrong: !item.isCorrect, short: item.type === 4 }">

        <div class="item-header">
          <span class="item-index">第 {{ idx + 1 }} 题</span>
          <el-tag size="small" type="info">{{ typeLabel(item.type) }}</el-tag>
          <span v-if="item.knowledgePoint" class="kp-tag">{{ item.knowledgePoint }}</span>
          <el-icon v-if="item.isCorrect" color="#22c55e" size="18"><CircleCheck /></el-icon>
          <el-icon v-else-if="item.type !== 4" color="#ef4444" size="18"><CircleClose /></el-icon>
          <el-tag v-else size="small" type="warning">简答·请对照参考答案自评</el-tag>
          <div class="item-ops">
            <el-button
              v-if="!item.isCorrect"
              size="small"
              :type="inWrongBook(item.questionId) ? 'success' : 'warning'"
              plain
              @click="toggleWrongBook(item)">
              {{ inWrongBook(item.questionId) ? '✓ 已加错题本' : '加入错题本' }}
            </el-button>
            <el-button size="small" plain @click="openSendMsg(item)">
              <el-icon><ChatDotRound /></el-icon> 求助教师
            </el-button>
          </div>
        </div>

        <RichContent class="item-content" :content="item.content" />

        <div class="item-answers">
          <div class="answer-row my-answer" :class="{ correct: item.isCorrect, wrong: !item.isCorrect && item.type !== 4 }">
            <span class="answer-label">我的答案：</span>
            <span>{{ item.studentAnswer || '（未作答）' }}</span>
          </div>
          <div v-if="item.type !== 4 && !item.isCorrect" class="answer-row correct-answer">
            <span class="answer-label">正确答案：</span>
            <RichContent class="answer-rich" :content="item.correctAnswer" />
          </div>
          <div v-if="item.type === 4" class="answer-row ref-answer">
            <span class="answer-label">参考答案：</span>
            <RichContent class="answer-rich" :content="item.correctAnswer" />
          </div>
          <div v-if="item.explanation" class="explanation">
            <el-icon><InfoFilled /></el-icon>
            <span>解析：</span>
            <RichContent class="answer-rich" :content="item.explanation" />
          </div>
        </div>

        <!-- 该题相似推荐 -->
        <SimilarQuestions
          v-if="!item.isCorrect && item.knowledgePoint"
          :question="{ id: item.questionId, knowledgePoint: item.knowledgePoint, difficulty: item.difficulty }"
          :current-ids="result.items.map(x => x.questionId)"
          @add-to-practice="addWrongToPractice"
        />
      </div>
    </div>

    <!-- 求助弹框 -->
    <SendMessageDialog
      v-if="msgDialogVisible"
      v-model:visible="msgDialogVisible"
      :attached-questions="msgAttached"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { CircleCheck, CircleClose, InfoFilled, ChatDotRound, Cpu } from '@element-plus/icons-vue'
import SimilarQuestions from '@/components/SimilarQuestions.vue'
import SendMessageDialog from '@/components/SendMessageDialog.vue'
import RichContent from '@/components/RichContent.vue'
import { practiceApi } from '@/api/practice'
import { marked } from 'marked'

const renderMarkdown = (text) => marked.parse(text || '')

const router = useRouter()
const result = ref(null)

const scoreRate = computed(() =>
  result.value?.maxScore > 0
    ? Math.round((result.value.totalScore / result.value.maxScore) * 100)
    : 0
)
const scoreColor = computed(() => {
  const r = scoreRate.value
  if (r >= 80) return '#22c55e'
  if (r >= 60) return '#f59e0b'
  return '#ef4444'
})

const typeMap = { 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }
const typeLabel = (t) => typeMap[t] ?? '未知'

const wrongItems = computed(() =>
  (result.value?.items ?? []).filter(x => !x.isCorrect && x.type !== 4)
)

// ─── 错题本（localStorage）─────────────────────────────────────────────────
// 用响应式 Set 追踪已在错题本中的 questionId，确保按钮即时更新
const wrongBookIds = ref(new Set())

function loadWrongBook() {
  try { return JSON.parse(localStorage.getItem('wrong-book') || '[]') } catch { return [] }
}

function saveWrongBook(book) {
  localStorage.setItem('wrong-book', JSON.stringify(book))
  wrongBookIds.value = new Set(book.map(x => x.questionId))
}

function inWrongBook(questionId) {
  return wrongBookIds.value.has(questionId)
}

function toggleWrongBook(item) {
  const book = loadWrongBook()
  const idx = book.findIndex(x => x.questionId === item.questionId)
  if (idx >= 0) {
    book.splice(idx, 1)
    saveWrongBook(book)
    ElMessage.success('已从错题本移除')
  } else {
    book.unshift({
      questionId: item.questionId,
      type: item.type,
      content: item.content,
      options: item.options,
      correctAnswer: item.correctAnswer,
      explanation: item.explanation,
      knowledgePoint: item.knowledgePoint,
      difficulty: item.difficulty,
      studentAnswer: item.studentAnswer,
      addedAt: new Date().toISOString(),
    })
    saveWrongBook(book)
    ElMessage.success('已加入错题本')
    // 同步到服务端（火就不管）
    practiceApi.saveWrongBookItem(item.questionId, item.studentAnswer ?? '').catch(() => {})
  }
}

function addAllWrong() {
  const book = loadWrongBook()
  let added = 0
  for (const item of wrongItems.value) {
    if (!book.some(x => x.questionId === item.questionId)) {
      book.unshift({
        questionId: item.questionId,
        type: item.type,
        content: item.content,
        options: item.options,
        correctAnswer: item.correctAnswer,
        explanation: item.explanation,
        knowledgePoint: item.knowledgePoint,
        difficulty: item.difficulty,
        studentAnswer: item.studentAnswer,
        addedAt: new Date().toISOString(),
      })
      added++
      practiceApi.saveWrongBookItem(item.questionId, item.studentAnswer ?? '').catch(() => {})
    }
  }
  saveWrongBook(book)
  ElMessage.success(`已添加 ${added} 道错题到错题本`)
}

// ─── 将错题加入新练习 ───────────────────────────────────────────────────────
function addWrongToPractice(q) {
  const existing = JSON.parse(sessionStorage.getItem('practice-questions') || '[]')
  if (!existing.find(x => x.id === q.id)) existing.push(q)
  sessionStorage.setItem('practice-questions', JSON.stringify(existing))
  ElMessage.success('已加入练习，返回练习页即可作答')
}

// ─── AI 分析 ───────────────────────────────────────────────────────────────
const aiLoading = ref(false)
const aiResult = ref('')

async function doAiAnalyze() {
  if (!result.value) return
  aiLoading.value = true
  try {
    const wrongItemsPayload = wrongItems.value.map(x => ({
      content: x.content,
      knowledgePoint: x.knowledgePoint ?? null,
      difficulty: x.difficulty ?? null,
    }))
    const res = await practiceApi.analyzeSession({
      totalCount: result.value.items?.length ?? 0,
      correctCount: result.value.correctCount ?? 0,
      totalScore: result.value.totalScore ?? 0,
      maxScore: result.value.maxScore ?? 0,
      knowledgePoint: result.value.knowledgePoint ?? null,
      typeName: result.value.typeName ?? null,
      wrongItems: wrongItemsPayload,
    })
    aiResult.value = res.analysis
  } catch {
    ElMessage.error('AI 分析失败，请稍后再试')
  } finally {
    aiLoading.value = false
  }
}

// ─── 求助教师 ────────────────────────────────────────────────────────────────
const msgDialogVisible = ref(false)
const msgAttached = ref([])

function openSendMsg(item) {
  msgAttached.value = [item]
  msgDialogVisible.value = true
}

onMounted(() => {
  try {
    const raw = sessionStorage.getItem('practice-result')
    if (raw) result.value = JSON.parse(raw)
  } catch { result.value = null }
  // 初始化错题本响应式 ID 集合
  wrongBookIds.value = new Set(loadWrongBook().map(x => x.questionId))
})
</script>

<style scoped>
.empty-result {
  text-align: center;
  padding: 80px 20px;
  color: #64748b;
}

.practice-result {
  max-width: 900px;
  margin: 32px auto;
  padding: 0 20px 60px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.result-overview {
  background: #fff;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  padding: 32px 40px;
  display: flex;
  align-items: center;
  gap: 40px;
  flex-wrap: wrap;
}

.overview-score {
  display: flex;
  align-items: center;
  gap: 20px;
}

.overview-label .score-num {
  font-size: 26px;
  font-weight: 700;
  color: #1e293b;
}

.overview-label .score-sub {
  font-size: 13px;
  color: #64748b;
  margin-top: 2px;
}

.overview-actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  flex: 1;
  justify-content: flex-end;
}

.result-item {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 20px;
}

.result-item.wrong {
  border-left: 4px solid #ef4444;
}

.item-header {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.item-index {
  font-weight: 600;
  color: #374151;
}

.kp-tag {
  font-size: 12px;
  color: #6366f1;
  background: #ede9fe;
  padding: 2px 8px;
  border-radius: 4px;
}

.item-ops {
  margin-left: auto;
  display: flex;
  gap: 8px;
}

.item-content {
  font-size: 15px;
  color: #1e293b;
  line-height: 1.7;
  white-space: pre-wrap;
}

.item-answers {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.answer-row {
  font-size: 13px;
  padding: 6px 10px;
  border-radius: 6px;
}

.answer-row.my-answer.correct {
  background: #dcfce7;
  color: #166534;
}

.answer-row.my-answer.wrong {
  background: #fee2e2;
  color: #991b1b;
}

.answer-row.correct-answer {
  background: #dbeafe;
  color: #1e40af;
}

.answer-row.ref-answer {
  background: #fef9c3;
  color: #713f12;
}

.answer-label {
  font-weight: 600;
  margin-right: 6px;
}

.explanation {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  font-size: 13px;
  color: #6b7280;
  background: #f8fafc;
  padding: 8px 10px;
  border-radius: 6px;
}

/* AI 分析 */
.ai-analysis-block {
  background: #faf5ff;
  border: 1px solid #e9d5ff;
  border-radius: 14px;
  padding: 24px;
}
.ai-block-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
  color: #6b21a8;
  margin-bottom: 16px;
}
.ai-block-content {
  font-size: 14px;
  line-height: 1.8;
  color: #1e293b;
}
.ai-block-content :deep(h1),
.ai-block-content :deep(h2),
.ai-block-content :deep(h3) {
  font-size: 15px;
  font-weight: 700;
  margin: 12px 0 6px;
  color: #4c1d95;
}
.ai-block-content :deep(ul),
.ai-block-content :deep(ol) {
  padding-left: 20px;
}
.ai-block-content :deep(li) { margin-bottom: 4px; }
.ai-block-content :deep(p) { margin: 6px 0; }
</style>
