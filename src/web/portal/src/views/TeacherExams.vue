<template>
  <div class="teacher-exams container">
    <div class="page-header">
      <h2>查阅考试</h2>
      <p class="page-desc">查看本租户下的所有考试，进行阅卷或查看考生成绩</p>
    </div>

    <!-- 筛选栏 -->
    <div class="filter-bar">
      <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 140px">
        <el-option label="草稿" :value="0" />
        <el-option label="已发布" :value="1" />
        <el-option label="进行中" :value="2" />
        <el-option label="已结束" :value="3" />
        <el-option label="已取消" :value="4" />
      </el-select>
      <el-button type="primary" @click="loadData">查询</el-button>
      <el-button @click="resetQuery">重置</el-button>
    </div>

    <!-- 考试卡片列表 -->
    <div v-loading="loading" class="exam-grid">
      <el-empty v-if="!loading && tableData.length === 0" description="暂无考试数据" />
      <div
        v-for="exam in tableData"
        :key="exam.id"
        class="exam-card"
        @click="goResults(exam.id)"
      >
        <div class="exam-card-header">
          <span class="exam-title">{{ exam.title }}</span>
          <el-tag :type="statusTagType(exam.status)" size="small">{{ statusLabel(exam.status) }}</el-tag>
        </div>
        <div class="exam-card-meta">
          <span><el-icon><Timer /></el-icon>{{ exam.durationMinutes }} 分钟</span>
          <span><el-icon><Document /></el-icon>{{ exam.questionCount }} 题</span>
          <span><el-icon><Trophy /></el-icon>满分 {{ exam.totalScore }} 分</span>
        </div>
        <div class="exam-card-time">
          <span v-if="exam.startTime">开始：{{ formatDate(exam.startTime) }}</span>
          <span v-if="exam.endTime">结束：{{ formatDate(exam.endTime) }}</span>
        </div>
        <div class="exam-card-action">
          <el-button type="primary" size="small" @click.stop="goResults(exam.id)">
            查看成绩 / 阅卷
          </el-button>
        </div>
      </div>
    </div>

    <!-- 分页 -->
    <div class="pagination-wrap" v-if="total > query.pageSize">
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :page-sizes="[12, 24, 48]"
        :total="total"
        layout="total, sizes, prev, pager, next"
        @change="loadData"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { teacherApi } from '@/api/teacher'
import { Timer, Document, Trophy } from '@element-plus/icons-vue'

const router = useRouter()
const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 12, status: null })

const statusMap = {
  0: { label: '草稿', type: 'info' },
  1: { label: '已发布', type: 'primary' },
  2: { label: '进行中', type: 'success' },
  3: { label: '已结束', type: 'warning' },
  4: { label: '已取消', type: 'danger' },
}
const statusLabel = (s) => statusMap[s]?.label ?? s
const statusTagType = (s) => statusMap[s]?.type ?? ''

function formatDate(d) {
  if (!d) return '-'
  return new Date(d).toLocaleString('zh-CN', { hour12: false })
}

async function loadData() {
  loading.value = true
  try {
    const params = { page: query.page, pageSize: query.pageSize }
    if (query.status !== null && query.status !== undefined && query.status !== '') {
      params.status = query.status
    }
    const res = await teacherApi.getExams(params)
    tableData.value = res.items ?? res
    total.value = res.totalCount ?? (res.items?.length ?? 0)
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.status = null
  query.page = 1
  loadData()
}

function goResults(examId) {
  router.push(`/teacher/exams/${examId}/results`)
}

onMounted(loadData)
</script>

<style scoped>
.teacher-exams {
  padding: 32px 40px;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h2 {
  font-size: 22px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 6px;
}

.page-desc {
  color: #64748b;
  font-size: 14px;
  margin: 0;
}

.filter-bar {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 24px;
  flex-wrap: wrap;
}

.exam-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 18px;
}

.exam-card {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 20px;
  cursor: pointer;
  transition: box-shadow 0.2s, border-color 0.2s;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.exam-card:hover {
  box-shadow: 0 4px 16px rgba(29, 78, 216, 0.12);
  border-color: #1d4ed8;
}

.exam-card-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.exam-title {
  font-size: 16px;
  font-weight: 600;
  color: #1e293b;
  line-height: 1.4;
  flex: 1;
}

.exam-card-meta {
  display: flex;
  gap: 14px;
  color: #64748b;
  font-size: 13px;
}

.exam-card-meta span {
  display: flex;
  align-items: center;
  gap: 4px;
}

.exam-card-time {
  display: flex;
  flex-direction: column;
  gap: 2px;
  color: #94a3b8;
  font-size: 12px;
}

.exam-card-action {
  margin-top: 4px;
}

.pagination-wrap {
  margin-top: 32px;
  display: flex;
  justify-content: center;
}
</style>
