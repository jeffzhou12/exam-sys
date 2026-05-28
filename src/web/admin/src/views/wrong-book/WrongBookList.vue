<template>
  <div class="wrong-book-list">
    <div class="page-header">
      <h2>错题本管理</h2>
    </div>

    <!-- 搜索过滤 -->
    <el-card class="filter-card" shadow="never">
      <el-form :model="filters" inline>
        <el-form-item label="学生 ID">
          <el-input
            v-model="filters.studentId"
            placeholder="输入学生 ID"
            clearable
            style="width: 200px"
            @keyup.enter="loadData"
          />
        </el-form-item>
        <el-form-item label="知识点">
          <el-input
            v-model="filters.knowledgePoint"
            placeholder="知识点关键词"
            clearable
            style="width: 200px"
            @keyup.enter="loadData"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="loadData">查询</el-button>
          <el-button :icon="Refresh" @click="resetFilters">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <el-card shadow="never" style="margin-top: 16px">
      <el-table
        v-loading="loading"
        :data="items"
        border
        stripe
        style="width: 100%"
      >
        <el-table-column prop="studentId" label="学生 ID" width="220" show-overflow-tooltip />
        <el-table-column prop="questionContent" label="题目内容" min-width="260" show-overflow-tooltip />
        <el-table-column prop="knowledgePoint" label="知识点" width="150" show-overflow-tooltip>
          <template #default="{ row }">{{ row.knowledgePoint || '—' }}</template>
        </el-table-column>
        <el-table-column label="难度" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.difficulty" size="small" :type="['', 'success', 'warning', 'danger'][row.difficulty]">
              {{ ['', '简单', '中等', '困难'][row.difficulty] }}
            </el-tag>
            <span v-else class="text-muted">—</span>
          </template>
        </el-table-column>
        <el-table-column prop="wrongCount" label="错误次数" width="90" align="center">
          <template #default="{ row }">
            <el-tag type="danger" effect="plain">{{ row.wrongCount }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="answerGiven" label="最近作答" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">{{ row.answerGiven || '—' }}</template>
        </el-table-column>
        <el-table-column prop="createdAt" label="首次错误" width="170" align="center">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="pagination.page"
          v-model:page-size="pagination.pageSize"
          :total="pagination.total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { practiceApi } from '@/api/practice'

const loading = ref(false)
const items = ref([])

const filters = reactive({ studentId: '', knowledgePoint: '' })
const pagination = reactive({ page: 1, pageSize: 20, total: 0 })

const formatDate = (val) => {
  if (!val) return '—'
  return new Date(val).toLocaleString('zh-CN', { hour12: false })
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await practiceApi.getAdminWrongBook({
      page: pagination.page,
      pageSize: pagination.pageSize,
      studentId: filters.studentId || undefined,
      knowledgePoint: filters.knowledgePoint || undefined,
    })
    items.value = res.data.items
    pagination.total = res.data.total
  } catch (e) {
    ElMessage.error('加载错题本数据失败')
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  filters.studentId = ''
  filters.knowledgePoint = ''
  pagination.page = 1
  loadData()
}

onMounted(loadData)
</script>

<style scoped>
.wrong-book-list {
  padding: 0;
}
.page-header {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
}
.page-header h2 {
  margin: 0;
  font-size: 20px;
}
.pagination-wrap {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
.text-muted {
  color: #999;
}
</style>
