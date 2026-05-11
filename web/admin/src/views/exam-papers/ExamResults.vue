<template>
  <div class="page-container">
    <div class="page-header">
      <div class="breadcrumb-back">
        <el-button :icon="ArrowLeft" text @click="$router.push('/exam-papers')">返回列表</el-button>
        <h3>考试成绩 - {{ paperTitle }}</h3>
      </div>
    </div>

    <el-card shadow="never" class="table-card">
      <el-table v-loading="loading" :data="tableData" stripe>
        <el-table-column type="index" label="排名" width="70" />
        <el-table-column prop="studentId" label="学生 ID" min-width="220" />
        <el-table-column prop="totalScore" label="得分" width="90" />
        <el-table-column label="批改状态" width="120">
          <template #default="{ row }">
            <el-tag v-if="row.pendingCount === 0" type="success" size="small">已全部批改</el-tag>
            <el-tag v-else type="warning" size="small">待批改 {{ row.pendingCount }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="gradedCount" label="已批改题数" width="110" />
        <el-table-column prop="submittedAt" label="提交时间" width="180">
          <template #default="{ row }">{{ formatDate(row.submittedAt) }}</template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :page-sizes="[20, 50, 100]"
        :total="total"
        layout="total, sizes, prev, pager, next"
        class="pagination"
        @change="loadData"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { examPapersApi } from '@/api/examPapers'
import { ArrowLeft } from '@element-plus/icons-vue'

const route = useRoute()
const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const paperTitle = ref('')
const query = reactive({ page: 1, pageSize: 20 })

async function loadData() {
  loading.value = true
  try {
    const res = await examPapersApi.getResults(route.params.id, { page: query.page, pageSize: query.pageSize })
    tableData.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

async function loadPaperTitle() {
  try {
    const paper = await examPapersApi.getById(route.params.id)
    paperTitle.value = paper.title
  } catch { /* ignore */ }
}

function formatDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleString('zh-CN')
}

onMounted(() => {
  loadData()
  loadPaperTitle()
})
</script>

<style scoped>
.page-container { padding: 20px; }
.page-header { margin-bottom: 16px; }
.breadcrumb-back {
  display: flex;
  align-items: center;
  gap: 8px;
}
.breadcrumb-back h3 { margin: 0; font-size: 18px; }
.table-card :deep(.el-card__body) { padding: 16px; }
.pagination { margin-top: 16px; justify-content: flex-end; }
</style>
