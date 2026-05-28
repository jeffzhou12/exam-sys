<template>
  <el-button
    :icon="isFavorited ? StarFilled : Star"
    :type="isFavorited ? 'warning' : 'default'"
    :size="size"
    :loading="loading"
    circle
    @click.stop="toggle"
    :title="isFavorited ? '取消收藏' : '收藏'"
  />
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { Star, StarFilled } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { favoritesApi } from '@/api/favorites'

const props = defineProps({
  targetType: { type: Number, required: true },
  targetId:   { type: String, required: true },
  size:       { type: String, default: 'small' },
})

const isFavorited = ref(false)
const loading     = ref(false)

onMounted(async () => {
  try {
    const res = await favoritesApi.check(props.targetType, props.targetId)
    isFavorited.value = res.isFavorited
  } catch {
    // 忽略检查失败
  }
})

async function toggle() {
  loading.value = true
  try {
    const res = await favoritesApi.toggle(props.targetType, props.targetId)
    isFavorited.value = res.isFavorited
    ElMessage.success(isFavorited.value ? '已收藏' : '已取消收藏')
  } catch {
    ElMessage.error('操作失败，请稍后重试')
  } finally {
    loading.value = false
  }
}
</script>
