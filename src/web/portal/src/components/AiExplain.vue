<template>
  <!-- 悬浮触发按钮 -->
  <el-button
    class="ai-trigger-btn"
    type="primary"
    circle
    :loading="loading"
    @click="open"
    title="AI 解题分析">
    <el-icon v-if="!loading"><MagicStick /></el-icon>
  </el-button>

  <!-- 解析抽屉 -->
  <el-drawer
    v-model="visible"
    title="AI 解题分析"
    direction="rtl"
    size="460px"
    :modal="false"
  >
    <template #header>
      <div class="drawer-header">
        <el-icon color="#6366f1" size="20"><MagicStick /></el-icon>
        <span>AI 解题分析</span>
      </div>
    </template>

    <div v-if="loading" class="ai-loading">
      <el-icon class="spin" size="32" color="#6366f1"><Loading /></el-icon>
      <p>AI 正在分析中，请稍候…</p>
    </div>

    <div v-else-if="content" class="ai-content" v-html="renderedContent" />

    <div v-else class="ai-empty">
      <el-button type="primary" :loading="loading" @click="fetchExplanation">
        获取 AI 解析
      </el-button>
    </div>
  </el-drawer>
</template>

<script setup>
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { MagicStick, Loading } from '@element-plus/icons-vue'
import { practiceApi } from '@/api/practice'

const props = defineProps({
  questionId: { type: String, required: true },
})

const visible = ref(false)
const loading = ref(false)
const content = ref('')

// 简单 Markdown → HTML 渲染（粗体、标题、换行）
const renderedContent = computed(() => {
  if (!content.value) return ''
  return content.value
    .replace(/^#### (.+)$/gm, '<h4>$1</h4>')
    .replace(/^### (.+)$/gm, '<h3>$1</h3>')
    .replace(/^## (.+)$/gm, '<h2>$1</h2>')
    .replace(/^# (.+)$/gm, '<h1>$1</h1>')
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>')
    .replace(/\n/g, '<br>')
})

async function fetchExplanation() {
  loading.value = true
  content.value = ''
  try {
    const res = await practiceApi.explain(props.questionId)
    content.value = res.explanation ?? ''
    if (!content.value) ElMessage.warning('AI 未返回解析内容')
  } catch {
    ElMessage.error('AI 解析请求失败，请稍后重试')
  } finally {
    loading.value = false
  }
}

function open() {
  visible.value = true
  if (!content.value && !loading.value) fetchExplanation()
}

defineExpose({ open })
</script>

<style scoped>
.ai-trigger-btn {
  position: fixed;
  right: 32px;
  bottom: 80px;
  width: 52px;
  height: 52px;
  font-size: 22px;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  border: none;
  box-shadow: 0 4px 14px rgba(99,102,241,.45);
  z-index: 200;
}

.drawer-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
}

.ai-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  margin-top: 80px;
  color: #6b7280;
  font-size: 14px;
}

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.ai-content {
  font-size: 14px;
  line-height: 1.8;
  color: #374151;
  padding: 20px;
}

.ai-content :deep(h1),
.ai-content :deep(h2),
.ai-content :deep(h3),
.ai-content :deep(h4) {
  color: #1e293b;
  margin: 16px 0 6px;
  font-weight: 700;
}

.ai-content :deep(strong) { color: #1d4ed8; }

.ai-empty {
  display: flex;
  justify-content: center;
  margin-top: 80px;
}
</style>
