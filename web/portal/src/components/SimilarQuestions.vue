<template>
  <div v-if="items.length" class="similar-box">
    <div class="similar-header">
      <el-icon><Connection /></el-icon>
      <span>相似题目推荐</span>
      <el-tag size="small" type="info">基于知识点 {{ question.knowledgePoint }}</el-tag>
    </div>
    <div class="similar-list">
      <div v-for="q in items" :key="q.id" class="similar-item">
        <div class="similar-meta">
          <el-tag size="small" type="info">{{ typeLabel(q.type) }}</el-tag>
          <el-tag size="small" :type="diffTagType(q.difficulty)">难度 {{ q.difficulty }}</el-tag>
        </div>
        <div class="similar-content">{{ truncate(q.content, 80) }}</div>
        <el-button size="small" type="primary" plain @click="$emit('add-to-practice', q)">
          + 加入练习
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { Connection } from '@element-plus/icons-vue'
import { practiceApi } from '@/api/practice'

const props = defineProps({
  question: { type: Object, required: true },
  currentIds: { type: Array, default: () => [] },
})

defineEmits(['add-to-practice'])

const items = ref([])

const typeMap = { 1: '单选', 2: '多选', 3: '判断', 4: '简答' }
const typeLabel = (t) => typeMap[t] ?? '?'
const diffTagType = (d) => d <= 2 ? 'success' : d === 3 ? 'warning' : 'danger'
const truncate = (s, n) => s.length > n ? s.slice(0, n) + '…' : s

async function load() {
  if (!props.question?.knowledgePoint) { items.value = []; return }
  try {
    const result = await practiceApi.getSimilar(
      props.question.id,
      props.question.knowledgePoint,
      props.question.difficulty,
      6,
    )
    items.value = result.filter(q => !props.currentIds.includes(q.id)).slice(0, 4)
  } catch { items.value = [] }
}

watch(() => props.question?.id, load, { immediate: true })
</script>

<style scoped>
.similar-box {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px 20px;
}

.similar-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: #374151;
  margin-bottom: 14px;
}

.similar-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.similar-item {
  border: 1px solid #f1f5f9;
  border-radius: 8px;
  padding: 10px 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  background: #fafafa;
}

.similar-meta {
  display: flex;
  gap: 6px;
}

.similar-content {
  font-size: 13px;
  color: #475569;
  line-height: 1.5;
}
</style>
