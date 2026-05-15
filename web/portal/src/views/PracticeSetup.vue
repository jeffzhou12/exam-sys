<template>
  <div class="practice-setup">
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

    <!-- 最近练习记录 -->
    <div v-if="history.length" class="history-section">
      <h3>最近练习记录</h3>
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
          </div>
          <div class="history-time">{{ formatDate(rec.time) }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Edit, VideoPlay } from '@element-plus/icons-vue'
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
    router.push('/practice/room')
  } finally {
    loading.value = false
  }
}

onMounted(loadHistory)
</script>

<style scoped>
.practice-setup {
  max-width: 760px;
  margin: 48px auto;
  padding: 0 20px;
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

.history-time {
  color: #94a3b8;
  font-size: 12px;
  white-space: nowrap;
}
</style>
