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
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="primary" @click="openDetail(row)">查看详情</el-button>
          </template>
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

    <!-- 答题详情 & 手动阅卷 对话框 -->
    <el-dialog
      v-model="detailVisible"
      :title="`答题详情 — ${currentStudentId}`"
      width="800px"
      top="4vh"
      @closed="detailData = null"
    >
    <div v-if="detailLoading" style="text-align:center;padding:40px">
      <el-icon class="is-loading" size="32"><Loading /></el-icon>
    </div>
    <template v-else-if="detailData">
      <el-alert
        :title="`总得分：${detailData.totalScore} / ${detailData.maxScore}　状态：${gradingStatusLabel(detailData.gradingStatus)}`"
        type="info"
        :closable="false"
        style="margin-bottom: 16px"
      />
      <div
        v-for="(ans, idx) in detailData.answers"
        :key="ans.questionId"
        class="answer-item"
      >
        <div class="answer-header">
          <span class="answer-index">第 {{ idx + 1 }} 题</span>
          <el-tag size="small" :type="gradingTagType(ans.gradingStatus)">
            {{ gradingStatusLabel(ans.gradingStatus) }}
          </el-tag>
          <span class="answer-score">
            得分：{{ ans.score ?? '-' }} / {{ ans.maxScore }}
          </span>
        </div>
        <div class="answer-question">{{ ans.questionContent }}</div>
        <div class="answer-content">
          <span class="answer-label">学生作答：</span>{{ ans.answerContent || '（未作答）' }}
        </div>
        <div v-if="ans.aiFeedback" class="answer-ai-feedback">
          <el-icon><MagicStick /></el-icon> AI 反馈：{{ ans.aiFeedback }}
        </div>

        <!-- 手动评分区域：仅待批改题目且有权限时显示 -->
        <template v-if="ans.gradingStatus === 'Pending' && auth.isAdminOrTeacher">
          <el-divider style="margin: 10px 0" />
          <div class="manual-grade-area">
            <el-form :model="gradeForm[ans.questionId]" inline>
              <el-form-item label="评分">
                <el-input-number
                  v-model="gradeForm[ans.questionId].score"
                  :min="0"
                  :max="ans.maxScore"
                  size="small"
                  style="width: 100px"
                />
                <span style="margin-left:4px;color:#999">/ {{ ans.maxScore }}</span>
              </el-form-item>
              <el-form-item label="批注">
                <el-input
                  v-model="gradeForm[ans.questionId].feedback"
                  size="small"
                  placeholder="可选批注"
                  style="width: 200px"
                />
              </el-form-item>
              <el-form-item>
                <el-button
                  type="primary"
                  size="small"
                  :loading="gradeForm[ans.questionId].loading"
                  @click="submitGrade(ans)"
                >
                  提交评分
                </el-button>
              </el-form-item>
            </el-form>
          </div>
        </template>
      </div>
    </template>
  </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { examPapersApi } from '@/api/examPapers'
import { useAuthStore } from '@/stores/auth'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Loading, MagicStick } from '@element-plus/icons-vue'

const route = useRoute()
const auth = useAuthStore()
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

// ---- 答题详情 ----
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref(null)
const currentStudentId = ref('')
const gradeForm = reactive({})

const gradingStatusLabel = (s) =>
  ({ Pending: '待批改', AutoGraded: '已自动批改', ManualGraded: '已人工批改', PartiallyGraded: '部分批改' }[s] ?? s)

const gradingTagType = (s) =>
  ({ Pending: 'warning', AutoGraded: 'info', ManualGraded: 'success', PartiallyGraded: '' }[s] ?? '')

async function openDetail(row) {
  currentStudentId.value = row.studentId
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    const result = await examPapersApi.getStudentResult(route.params.id, row.studentId)
    detailData.value = result
    // 初始化手动评分表单
    for (const ans of result.answers) {
      if (ans.gradingStatus === 'Pending') {
        gradeForm[ans.questionId] = { score: 0, feedback: '', loading: false }
      }
    }
  } finally {
    detailLoading.value = false
  }
}

async function submitGrade(ans) {
  const f = gradeForm[ans.questionId]
  if (f.score == null) return
  f.loading = true
  try {
    await examPapersApi.manualGrade(route.params.id, ans.answerId, {
      score: f.score,
      feedback: f.feedback || null
    })
    ElMessage.success('评分已提交')
    ans.score = f.score
    ans.aiFeedback = f.feedback || ans.aiFeedback
    ans.gradingStatus = 'ManualGraded'
    loadData()
  } finally {
    f.loading = false
  }
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

.answer-item {
  border: 1px solid #ebeef5;
  border-radius: 6px;
  padding: 12px 16px;
  margin-bottom: 12px;
}
.answer-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}
.answer-index { font-weight: 600; font-size: 14px; }
.answer-score { margin-left: auto; color: #f56c6c; font-size: 13px; }
.answer-question { font-size: 14px; color: #303133; line-height: 1.6; margin-bottom: 6px; }
.answer-content { font-size: 13px; color: #606266; margin-bottom: 4px; }
.answer-label { font-weight: 500; }
.answer-ai-feedback { font-size: 12px; color: #909399; display: flex; align-items: center; gap: 4px; }
.manual-grade-area { background: #fafafa; padding: 8px 12px; border-radius: 4px; }
</style>
