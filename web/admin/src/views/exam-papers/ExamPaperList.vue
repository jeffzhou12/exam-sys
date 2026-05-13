<template>
  <div class="page-container">
    <div class="page-header">
      <h3>试卷管理</h3>
      <el-button
        v-if="auth.isAdminOrTeacher"
        type="primary"
        :icon="Plus"
        @click="$router.push('/exam-papers/create')"
      >新建试卷</el-button>
    </div>

    <!-- 筛选 -->
    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 130px">
            <el-option label="草稿" :value="0" />
            <el-option label="已发布" :value="1" />
            <el-option label="进行中" :value="2" />
            <el-option label="已结束" :value="3" />
            <el-option label="已取消" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadData">查询</el-button>
          <el-button @click="resetQuery">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 表格 -->
    <el-card shadow="never" class="table-card">
      <el-table v-loading="loading" :data="tableData" stripe>
        <el-table-column prop="title" label="试卷标题" min-width="200" />
        <el-table-column prop="totalScore" label="总分" width="80" />
        <el-table-column prop="durationMinutes" label="时长(分)" width="90" />
        <el-table-column prop="questionCount" label="题目数" width="80" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="startTime" label="开始时间" width="180">
          <template #default="{ row }">{{ formatDate(row.startTime) }}</template>
        </el-table-column>
        <el-table-column prop="endTime" label="结束时间" width="180">
          <template #default="{ row }">{{ formatDate(row.endTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.status === 0 && auth.isAdminOrTeacher"
              size="small"
              @click="$router.push(`/exam-papers/${row.id}/edit`)"
            >编辑</el-button>
            <el-button
              v-if="row.status === 0 && auth.isAdminOrTeacher"
              size="small"
              type="success"
              @click="publish(row)"
            >发布</el-button>
            <el-button
              v-if="[1, 2].includes(row.status) && auth.isAdminOrTeacher"
              size="small"
              type="warning"
              @click="cancel(row)"
            >取消</el-button>
            <el-button
              size="small"
              type="info"
              @click="$router.push(`/exam-papers/${row.id}/results`)"
            >成绩</el-button>
            <el-button
              size="small"
              @click="openPreview(row)"
            >预览</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :page-sizes="[10, 20, 50]"
        :total="total"
        layout="total, sizes, prev, pager, next"
        class="pagination"
        @change="loadData"
      />
    </el-card>

    <!-- 试卷预览对话框 -->
    <el-dialog v-model="previewVisible" title="试卷预览" width="760px" top="5vh">
    <div v-if="previewLoading" style="text-align:center;padding:40px">
      <el-icon class="is-loading" size="32"><Loading /></el-icon>
    </div>
    <template v-else-if="previewData">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="标题">{{ previewData.title }}</el-descriptions-item>
        <el-descriptions-item label="总分">{{ previewData.totalScore }} 分</el-descriptions-item>
        <el-descriptions-item label="考试时长">{{ previewData.durationMinutes }} 分钟</el-descriptions-item>
        <el-descriptions-item label="题目数">{{ previewData.questions?.length ?? 0 }} 题</el-descriptions-item>
        <el-descriptions-item label="开始时间">{{ formatDate(previewData.startTime) }}</el-descriptions-item>
        <el-descriptions-item label="结束时间">{{ formatDate(previewData.endTime) }}</el-descriptions-item>
        <el-descriptions-item v-if="previewData.description" label="描述" :span="2">{{ previewData.description }}</el-descriptions-item>
      </el-descriptions>

      <el-divider>题目列表</el-divider>
      <div
        v-for="(q, idx) in previewData.questions"
        :key="q.questionId"
        class="preview-question"
      >
        <div class="preview-q-header">
          <span class="preview-q-index">{{ idx + 1 }}.</span>
          <el-tag size="small" style="margin: 0 8px">{{ typeLabel(q.type) }}</el-tag>
          <span class="preview-q-score">{{ q.score }} 分</span>
        </div>
        <div class="preview-q-content">{{ q.content }}</div>
        <div v-if="q.options && q.options.length" class="preview-q-options">
          <div v-for="(opt, oi) in q.options" :key="oi" class="preview-q-option">
            <span class="option-label">{{ String.fromCharCode(65 + oi) }}.</span>
            {{ opt }}
          </div>
        </div>
        <div v-if="q.answer" class="preview-q-answer">参考答案：{{ q.answer }}</div>
      </div>
    </template>
  </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { examPapersApi } from '@/api/examPapers'
import { useAuthStore } from '@/stores/auth'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Loading } from '@element-plus/icons-vue'

const auth = useAuthStore()
const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 10, status: null })

const statusLabel = (s) => ['草稿', '已发布', '进行中', '已结束', '已取消'][s] ?? s
const statusTagType = (s) => ['info', 'success', 'warning', 'default', 'danger'][s] ?? 'info'

async function loadData() {
  loading.value = true
  try {
    const res = await examPapersApi.getList({
      page: query.page,
      pageSize: query.pageSize,
      status: query.status ?? undefined
    })
    tableData.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.page = 1
  query.status = null
  loadData()
}

async function publish(row) {
  await ElMessageBox.confirm(`确定发布试卷「${row.title}」吗？发布后将无法编辑。`, '提示', { type: 'warning' })
  await examPapersApi.publish(row.id)
  ElMessage.success('发布成功')
  loadData()
}

async function cancel(row) {
  await ElMessageBox.confirm(`确定取消试卷「${row.title}」吗？`, '提示', { type: 'warning' })
  await examPapersApi.cancel(row.id)
  ElMessage.success('取消成功')
  loadData()
}

function formatDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleString('zh-CN')
}

// 预览
const previewVisible = ref(false)
const previewLoading = ref(false)
const previewData = ref(null)

const typeLabel = (t) => ({ 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }[t] ?? t)

async function openPreview(row) {
  previewVisible.value = true
  previewLoading.value = true
  previewData.value = null
  try {
    previewData.value = await examPapersApi.getById(row.id)
  } finally {
    previewLoading.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.page-container { padding: 20px; }
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.page-header h3 { margin: 0; font-size: 18px; }
.filter-card { margin-bottom: 16px; }
.filter-card :deep(.el-card__body) { padding: 16px 16px 0; }
.table-card :deep(.el-card__body) { padding: 16px; }
.pagination { margin-top: 16px; justify-content: flex-end; }

.preview-question {
  border: 1px solid #ebeef5;
  border-radius: 6px;
  padding: 12px 16px;
  margin-bottom: 12px;
}
.preview-q-header {
  display: flex;
  align-items: center;
  margin-bottom: 8px;
}
.preview-q-index { font-weight: 600; font-size: 15px; }
.preview-q-score { color: #f56c6c; font-size: 13px; margin-left: auto; }
.preview-q-content { font-size: 14px; color: #303133; line-height: 1.6; margin-bottom: 8px; }
.preview-q-options { display: flex; flex-direction: column; gap: 4px; }
.preview-q-option { font-size: 13px; color: #606266; }
.option-label { font-weight: 600; margin-right: 4px; }
.preview-q-answer { margin-top: 8px; font-size: 13px; color: #67c23a; }
</style>
