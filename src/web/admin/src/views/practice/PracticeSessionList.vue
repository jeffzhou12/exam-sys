<template>
  <div class="practice-session-list">
    <div class="page-header">
      <h2>练习记录</h2>
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
        :data="sessions"
        border
        stripe
        style="width: 100%"
      >
        <el-table-column prop="studentId" label="学生 ID" width="220" show-overflow-tooltip />
        <el-table-column prop="knowledgePoint" label="知识点" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.knowledgePoint || '综合练习' }}</template>
        </el-table-column>
        <el-table-column prop="typeName" label="题型" width="120">
          <template #default="{ row }">{{ row.typeName || '全部' }}</template>
        </el-table-column>
        <el-table-column label="题数/答对" width="110" align="center">
          <template #default="{ row }">
            {{ row.count }} / {{ row.correctCount }}
          </template>
        </el-table-column>
        <el-table-column label="正确率" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.count > 0 && row.correctCount / row.count >= 0.8 ? 'success' : 'warning'">
              {{ row.count > 0 ? Math.round(row.correctCount / row.count * 100) : 0 }}%
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="得分" width="110" align="center">
          <template #default="{ row }">
            {{ row.totalScore }} / {{ row.maxScore }}
          </template>
        </el-table-column>
        <el-table-column label="难度" width="80" align="center">
          <template #default="{ row }">
            <span v-if="row.difficulty">{{ ['', '简单', '中等', '困难'][row.difficulty] || row.difficulty }}</span>
            <span v-else class="text-muted">—</span>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="练习时间" width="170" align="center">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
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
const sessions = ref([])

const filters = reactive({ studentId: '', knowledgePoint: '' })
const pagination = reactive({ page: 1, pageSize: 20, total: 0 })

const formatDate = (val) => {
  if (!val) return '—'
  return new Date(val).toLocaleString('zh-CN', { hour12: false })
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await practiceApi.getAdminSessions({
      page: pagination.page,
      pageSize: pagination.pageSize,
      studentId: filters.studentId || undefined,
      knowledgePoint: filters.knowledgePoint || undefined,
    })
    sessions.value = res.data.items
    pagination.total = res.data.total
  } catch (e) {
    ElMessage.error('加载练习记录失败')
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
.practice-session-list {
  padding: 0;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.page-header h2 {
  margin: 0;
  font-size: 20px;
}
.filter-card {
  margin-bottom: 0;
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
