<template>
  <div class="wrong-book container">
    <div class="page-header">
      <h2><el-icon><Collection /></el-icon> 错题本</h2>
      <div class="header-actions">
        <el-button
          v-if="selected.length"
          type="primary"
          @click="practiceSelected">
          练习已选 ({{ selected.length }})
        </el-button>
        <el-button
          v-if="book.length"
          type="primary"
          plain
          @click="practiceAll">
          全部练习
        </el-button>
        <el-button
          v-if="selected.length"
          type="danger"
          plain
          @click="removeSelected">
          删除已选
        </el-button>
        <el-button v-if="book.length" plain @click="clearAll">清空错题本</el-button>
      </div>
    </div>

    <el-empty v-if="!book.length" description="错题本是空的，快去做题吧！">
      <el-button type="primary" @click="$router.push('/practice')">去练习</el-button>
    </el-empty>

    <div v-else class="book-list">
      <div
        v-for="item in book"
        :key="item.questionId"
        class="book-item"
        :class="{ selected: selected.includes(item.questionId) }">
        <el-checkbox
          :model-value="selected.includes(item.questionId)"
          @change="toggleSelect(item.questionId)"
          class="item-check"
        />

        <div class="item-body">
          <div class="item-header">
            <el-tag size="small" type="info">{{ typeLabel(item.type) }}</el-tag>
            <el-tag size="small" :type="diffTagType(item.difficulty)">
              难度 {{ item.difficulty }}
            </el-tag>
            <span v-if="item.knowledgePoint" class="kp-tag">{{ item.knowledgePoint }}</span>
            <span class="added-time">{{ formatDate(item.addedAt) }} 加入</span>
          </div>

          <div class="item-content">{{ item.content }}</div>

          <div class="item-answer">
            <div class="answer-row my-answer">
              <span class="answer-label">我的错误答案：</span>{{ item.studentAnswer || '（未作答）' }}
            </div>
            <div class="answer-row correct-answer">
              <span class="answer-label">正确答案：</span>{{ item.correctAnswer }}
            </div>
            <div v-if="item.explanation" class="explanation">
              <el-icon><InfoFilled /></el-icon> 解析：{{ item.explanation }}
            </div>
          </div>

          <!-- 相似题推荐 -->
          <SimilarQuestions
            v-if="item.knowledgePoint"
            :question="{ id: item.questionId, ...item }"
            :current-ids="book.map(x => x.questionId)"
            @add-to-practice="addSimilarToPractice"
          />
        </div>

        <div class="item-ops">
          <el-button size="small" plain @click="openSendMsg(item)">
            <el-icon><ChatDotRound /></el-icon> 求助
          </el-button>
          <el-button size="small" type="danger" plain @click="removeItem(item.questionId)">
            移除
          </el-button>
        </div>
      </div>
    </div>

    <!-- AI 悬浮解析（针对当前展开的题目） -->
    <AiExplain v-if="book.length" :question-id="book[0].questionId" />

    <!-- 求助对话框 -->
    <SendMessageDialog
      v-if="msgVisible"
      v-model:visible="msgVisible"
      :attached-questions="msgItem ? [msgItem] : []"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Collection, InfoFilled, ChatDotRound } from '@element-plus/icons-vue'
import SimilarQuestions from '@/components/SimilarQuestions.vue'
import AiExplain from '@/components/AiExplain.vue'
import SendMessageDialog from '@/components/SendMessageDialog.vue'

const router = useRouter()
const book = ref([])
const selected = ref([])

const typeMap = { 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }
const typeLabel = (t) => typeMap[t] ?? '?'
const diffTagType = (d) => d <= 2 ? 'success' : d === 3 ? 'warning' : 'danger'
const formatDate = (iso) => new Date(iso).toLocaleDateString('zh-CN')

function load() {
  try { book.value = JSON.parse(localStorage.getItem('wrong-book') || '[]') }
  catch { book.value = [] }
}

function save() {
  localStorage.setItem('wrong-book', JSON.stringify(book.value))
}

function toggleSelect(id) {
  const idx = selected.value.indexOf(id)
  if (idx >= 0) selected.value.splice(idx, 1)
  else selected.value.push(id)
}

function removeItem(id) {
  book.value = book.value.filter(x => x.questionId !== id)
  selected.value = selected.value.filter(x => x !== id)
  save()
}

function removeSelected() {
  book.value = book.value.filter(x => !selected.value.includes(x.questionId))
  selected.value = []
  save()
  ElMessage.success('已删除选中的错题')
}

function clearAll() {
  ElMessageBox.confirm('确定清空整个错题本？', '提示', { type: 'warning' })
    .then(() => {
      book.value = []
      selected.value = []
      localStorage.removeItem('wrong-book')
      ElMessage.success('错题本已清空')
    }).catch(() => {})
}

function toPracticeQuestions(items) {
  return items.map(item => ({
    id: item.questionId,
    type: item.type,
    content: item.content,
    options: item.options,
    knowledgePoint: item.knowledgePoint,
    difficulty: item.difficulty,
  }))
}

function practiceAll() {
  const qs = toPracticeQuestions(book.value)
  sessionStorage.setItem('practice-questions', JSON.stringify(qs))
  sessionStorage.setItem('practice-setup', JSON.stringify({ typeName: null, knowledgePoint: '错题本' }))
  router.push('/practice/room')
}

function practiceSelected() {
  const items = book.value.filter(x => selected.value.includes(x.questionId))
  const qs = toPracticeQuestions(items)
  sessionStorage.setItem('practice-questions', JSON.stringify(qs))
  sessionStorage.setItem('practice-setup', JSON.stringify({ typeName: null, knowledgePoint: '错题本(选中)' }))
  router.push('/practice/room')
}

function addSimilarToPractice(q) {
  const existing = JSON.parse(sessionStorage.getItem('practice-questions') || '[]')
  if (!existing.find(x => x.id === q.id)) existing.push(q)
  sessionStorage.setItem('practice-questions', JSON.stringify(existing))
  ElMessage.success('已加入练习队列，点击"全部练习"即可开始')
}

// ─── 求助 ────────────────────────────────────────────────────────────────────
const msgVisible = ref(false)
const msgItem = ref(null)

function openSendMsg(item) {
  msgItem.value = item
  msgVisible.value = true
}

onMounted(load)
</script>

<style scoped>
.wrong-book {
  padding: 40px 40px 60px;
}

.page-header {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
  flex-wrap: wrap;
}

.page-header h2 {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 22px;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
  flex: 1;
}

.header-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.book-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.book-item {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px 20px;
  display: flex;
  gap: 12px;
  transition: border-color .2s;
}

.book-item.selected {
  border-color: #1d4ed8;
  background: #eff6ff;
}

.item-check { flex-shrink: 0; padding-top: 2px; }

.item-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-width: 0;
}

.item-header {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.kp-tag {
  font-size: 12px;
  color: #6366f1;
  background: #ede9fe;
  padding: 2px 8px;
  border-radius: 4px;
}

.added-time {
  margin-left: auto;
  font-size: 12px;
  color: #94a3b8;
}

.item-content {
  font-size: 15px;
  color: #1e293b;
  line-height: 1.7;
  white-space: pre-wrap;
}

.item-answer {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.answer-row {
  font-size: 13px;
  padding: 5px 10px;
  border-radius: 5px;
}

.answer-row.my-answer { background: #fee2e2; color: #991b1b; }
.answer-row.correct-answer { background: #dbeafe; color: #1e40af; }
.answer-label { font-weight: 600; margin-right: 6px; }

.explanation {
  display: flex;
  align-items: flex-start;
  gap: 5px;
  font-size: 13px;
  color: #6b7280;
  background: #f8fafc;
  padding: 6px 10px;
  border-radius: 5px;
}

.item-ops {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex-shrink: 0;
  justify-content: flex-start;
}
</style>
