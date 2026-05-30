<template>
  <div class="practice-setup container">
    <div class="setup-card">
      <div class="setup-header">
        <el-icon size="36" color="#1d4ed8"><Edit /></el-icon>
        <h2>在线练习</h2>
        <p>根据条件自由抽题，即时批改，挑战自我</p>
      </div>

      <el-form :model="form" label-width="100px" size="large" class="setup-form">
        <el-form-item label="题目类型">
          <el-radio-group v-model="form.type">
            <el-radio-button :value="null">全部</el-radio-button>
            <el-radio-button :value="1">单选题</el-radio-button>
            <el-radio-button :value="2">多选题</el-radio-button>
            <el-radio-button :value="3">判断题</el-radio-button>
            <el-radio-button :value="4">简答题</el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="难度">
          <el-radio-group v-model="form.difficulty">
            <el-radio-button :value="null">全部</el-radio-button>
            <el-radio-button v-for="d in 5" :key="d" :value="d">
              {{ '★'.repeat(d) }}
            </el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="知识点">
          <el-input
            v-model="form.knowledgePoint"
            placeholder="输入知识点关键词（可选）"
            clearable
            style="max-width: 360px"
          />
        </el-form-item>

        <el-form-item label="题目数量">
          <el-slider
            v-model="form.count"
            :min="5"
            :max="50"
            :step="5"
            show-stops
            style="max-width: 360px"
          />
          <span class="count-label">{{ form.count }} 题</span>
        </el-form-item>
      </el-form>

      <div class="setup-actions">
        <el-button
          type="primary"
          size="large"
          round
          :loading="loading"
          @click="startPractice">
          <el-icon><VideoPlay /></el-icon>
          开始练习
        </el-button>
      </div>
    </div>

    <!-- 成绩详情弹框 -->
    <el-dialog
      v-model="scoreDialogVisible"
      title="练习成绩"
      width="400px"
      align-center>
      <div v-if="scoreRec" class="score-dialog-body">
        <el-progress
          type="circle"
          :percentage="Math.round((scoreRec.correctRate ?? 0) * 100)"
          :width="110"
          :stroke-width="10"
          :color="scoreColor(scoreRec.correctRate)"
        />
        <div class="score-dialog-stats">
          <div class="score-stat">
            <span class="stat-label">答题数</span>
            <span class="stat-val">{{ scoreRec.count }}</span>
          </div>
          <div class="score-stat">
            <span class="stat-label">答对</span>
            <span class="stat-val success">{{ scoreRec.correctCount ?? '-' }}</span>
          </div>
          <div class="score-stat">
            <span class="stat-label">得分</span>
            <span class="stat-val">{{ scoreRec.totalScore ?? '-' }} / {{ scoreRec.maxScore ?? '-' }}</span>
          </div>
          <div class="score-stat">
            <span class="stat-label">正确率</span>
            <span class="stat-val" :style="{ color: scoreColor(scoreRec.correctRate) }">
              {{ Math.round((scoreRec.correctRate ?? 0) * 100) }}%
            </span>
          </div>
        </div>
        <div class="score-dialog-meta">
          <span v-if="scoreRec.typeName">{{ scoreRec.typeName }}</span>
          <span v-if="scoreRec.knowledgePoint">· {{ scoreRec.knowledgePoint }}</span>
          <span v-if="scoreRec.difficulty">· 难度 {{ '★'.repeat(scoreRec.difficulty) }}</span>
          <div class="score-time">{{ formatDate(scoreRec.time) }}</div>
        </div>
      </div>
      <template #footer>
        <el-button type="primary" @click="retryPractice(scoreRec); scoreDialogVisible = false">
          <el-icon><Refresh /></el-icon> 再做一次
        </el-button>
        <el-button @click="scoreDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 最近练习记录 -->
    <div v-if="history.length" class="history-section">
      <div class="history-header">
        <h3>最近练习记录</h3>
        <el-button size="small" plain type="danger" :icon="Delete" @click="clearAllHistory">清除全部</el-button>
      </div>
      <div class="history-list">
        <div v-for="rec in history" :key="rec.time" class="history-item">
          <div class="history-meta">
            <span>{{ rec.count }} 题</span>
            <span v-if="rec.typeName">· {{ rec.typeName }}</span>
            <span v-if="rec.knowledgePoint">· {{ rec.knowledgePoint }}</span>
          </div>
          <div class="history-score">
            <el-tag :type="scoreTagType(rec.correctRate)" size="small">
              正确率 {{ Math.round(rec.correctRate * 100) }}%
            </el-tag>
            <span v-if="rec.correctCount != null" class="history-score-detail">
              {{ rec.correctCount }}/{{ rec.count }}
            </span>
          </div>
          <div class="history-time">{{ formatDate(rec.time) }}</div>
          <div class="history-actions">
            <el-button
              size="small"
              plain
              :icon="DataAnalysis"
              @click="viewScore(rec)">
              查看成绩
            </el-button>
            <el-tooltip content="用相同设置再练一次" placement="top">
              <el-button
                size="small"
                type="primary"
                plain
                :icon="Refresh"
                :loading="loading"
                @click="retryPractice(rec)">
                再做一次
              </el-button>
            </el-tooltip>
            <el-button
              size="small"
              type="danger"
              plain
              :icon="Delete"
              circle
              @click="deleteHistory(rec)"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Edit, VideoPlay, Delete, Refresh, DataAnalysis } from '@element-plus/icons-vue'
import { practiceApi } from '@/api/practice'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const loading = ref(false)

const form = reactive({ type: null, difficulty: null, knowledgePoint: '', count: 10 })
const history = ref([])

const typeNames = { 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }

function scoreTagType(rate) {
  if (rate >= 0.8) return 'success'
  if (rate >= 0.6) return 'warning'
  return 'danger'
}

function formatDate(iso) {
  return new Date(iso).toLocaleString('zh-CN', { hour12: false })
}

function loadHistory() {
  try {
    history.value = JSON.parse(localStorage.getItem('practice-history') || '[]')
  } catch { history.value = [] }
}

async function syncHistoryFromServer() {
  try {
    const serverRecords = await practiceApi.getHistory()
    if (!serverRecords?.length) return
    // 将服务端记录转换为本地格式，然后与本地合并（以 time 去重，服务端优先）
    const local = JSON.parse(localStorage.getItem('practice-history') || '[]')
    const localTimes = new Set(local.map(r => r.time))
    const fromServer = serverRecords.map(r => ({
      id:             r.id,
      count:          r.count,
      correctRate:    r.correctRate,
      correctCount:   r.correctCount,
      totalScore:     r.totalScore,
      maxScore:       r.maxScore,
      typeName:       r.typeName,
      knowledgePoint: r.knowledgePoint,
      type:           r.questionType,
      difficulty:     r.difficulty,
      setupCount:     r.setupCount,
      time:           r.createdAt,
      _fromServer:    true,
    })).filter(r => !localTimes.has(r.time))
    // 合并后按时间降序，取最近 20 条
    const merged = [...local, ...fromServer]
      .sort((a, b) => new Date(b.time) - new Date(a.time))
      .slice(0, 20)
    history.value = merged
    localStorage.setItem('practice-history', JSON.stringify(merged))
  } catch { /* 网络失败时保持本地数据不变 */ }
}

async function startPractice() {
  if (!auth.tenantId) {
    ElMessage.error('无法确定租户，请重新登录')
    return
  }
  loading.value = true
  try {
    const params = { count: form.count }
    if (form.type !== null) params.type = form.type
    if (form.difficulty !== null) params.difficulty = form.difficulty
    if (form.knowledgePoint) params.knowledgePoint = form.knowledgePoint

    const questions = await practiceApi.getQuestions(params)
    if (!questions.length) {
      ElMessage.warning('没有符合条件的题目，请调整筛选条件')
      return
    }

    // 把题目存入 sessionStorage，跳转练习房间
    sessionStorage.setItem('practice-questions', JSON.stringify(questions))
    sessionStorage.setItem('practice-setup', JSON.stringify({
      typeName: typeNames[form.type] || null,
      knowledgePoint: form.knowledgePoint || null,
    }))
    // 保存完整表单参数供历史记录重做使用
    sessionStorage.setItem('practice-setup-params', JSON.stringify({
      type: form.type,
      difficulty: form.difficulty,
      knowledgePoint: form.knowledgePoint || null,
      count: form.count,
    }))
    router.push('/practice/room')
  } finally {
    loading.value = false
  }
}

async function retryPractice(rec) {
  form.type        = rec.type        ?? null
  form.difficulty  = rec.difficulty  ?? null
  form.knowledgePoint = rec.knowledgePoint || ''
  form.count       = rec.setupCount  || 10
  await startPractice()
}

function deleteHistory(rec) {
  history.value = history.value.filter(x => x.time !== rec.time)
  localStorage.setItem('practice-history', JSON.stringify(history.value))
  // 同步删除服务端记录
  if (rec.id) {
    practiceApi.deleteSession(rec.id).catch(() => {})
  }
}

function clearAllHistory() {
  ElMessageBox.confirm('确定清除所有练习记录？', '清除确认', { type: 'warning' })
    .then(async () => {
      history.value = []
      localStorage.removeItem('practice-history')
      ElMessage.success('记录已清除')
      // 同步清除服务端记录
      await practiceApi.clearSessions().catch(() => {})
    })
    .catch(() => {})
}

// ─── 查看成绩弹框 ────────────────────────────────────────────────────────────
const scoreDialogVisible = ref(false)
const scoreRec = ref(null)

function viewScore(rec) {
  scoreRec.value = rec
  scoreDialogVisible.value = true
}

function scoreColor(rate) {
  if ((rate ?? 0) >= 0.8) return '#22c55e'
  if ((rate ?? 0) >= 0.6) return '#f59e0b'
  return '#ef4444'
}

onMounted(() => {
  loadHistory()
  syncHistoryFromServer()
})
</script>

<style scoped>
.practice-setup {
  padding: 40px 40px 60px;
}

.setup-card {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 40px 48px;
  margin-bottom: 32px;
}

.setup-header {
  text-align: center;
  margin-bottom: 40px;
}

.setup-header h2 {
  font-size: 24px;
  font-weight: 700;
  color: #1e293b;
  margin: 12px 0 6px;
}

.setup-header p {
  color: #64748b;
  font-size: 14px;
  margin: 0;
}

.setup-form {
  margin-bottom: 8px;
}

.count-label {
  margin-left: 16px;
  font-size: 15px;
  font-weight: 600;
  color: #1d4ed8;
}

.setup-actions {
  text-align: center;
  margin-top: 32px;
}

.setup-actions .el-button {
  min-width: 180px;
  font-size: 16px;
  height: 48px;
}

.history-section h3 {
  font-size: 16px;
  font-weight: 600;
  color: #374151;
  margin-bottom: 12px;
  margin: 0;
}
.history-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.history-item {
  display: flex;
  align-items: center;
  gap: 14px;
  background: #f8fafc;
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 13px;
}

.history-meta {
  flex: 1;
  color: #475569;
}

.history-score {
  display: flex;
  align-items: center;
  gap: 6px;
}

.history-score-detail {
  font-size: 12px;
  color: #64748b;
}

.history-time {
  color: #94a3b8;
  font-size: 12px;
  white-space: nowrap;
}

.history-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

/* 成绩弹框 */
.score-dialog-body {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
  padding: 8px 0 4px;
}

.score-dialog-stats {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px 24px;
  width: 100%;
}

.score-stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  background: #f8fafc;
  border-radius: 8px;
  padding: 10px 0;
}

.stat-label {
  font-size: 12px;
  color: #94a3b8;
  margin-bottom: 4px;
}

.stat-val {
  font-size: 22px;
  font-weight: 700;
  color: #1e293b;
}

.stat-val.success {
  color: #22c55e;
}

.score-dialog-meta {
  font-size: 13px;
  color: #64748b;
  text-align: center;
  line-height: 1.8;
}

.score-time {
  font-size: 12px;
  color: #94a3b8;
  margin-top: 2px;
}
</style>
