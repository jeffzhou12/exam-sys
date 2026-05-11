<template>
  <div class="page-container">
    <div class="page-header">
      <div class="breadcrumb-back">
        <el-button :icon="ArrowLeft" text @click="$router.push('/exam-papers')">返回列表</el-button>
        <h3>{{ isEdit ? '编辑试卷' : '新建试卷' }}</h3>
      </div>
    </div>

    <el-form
      ref="formRef"
      v-loading="pageLoading"
      :model="form"
      :rules="rules"
      label-width="110px"
    >
      <el-card shadow="never" class="form-card">
        <template #header><span>基本信息</span></template>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="试卷标题" prop="title">
              <el-input v-model="form.title" placeholder="请输入试卷标题" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="总分" prop="totalScore">
              <el-input-number v-model="form.totalScore" :min="1" :max="1000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="考试时长(分)" prop="durationMinutes">
              <el-input-number v-model="form.durationMinutes" :min="1" :max="480" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="开始时间">
              <el-date-picker
                v-model="form.startTime"
                type="datetime"
                placeholder="选择开始时间"
                style="width: 100%"
                value-format="YYYY-MM-DDTHH:mm:ss"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束时间">
              <el-date-picker
                v-model="form.endTime"
                type="datetime"
                placeholder="选择结束时间"
                style="width: 100%"
                value-format="YYYY-MM-DDTHH:mm:ss"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="试卷描述">
              <el-input
                v-model="form.description"
                type="textarea"
                :rows="3"
                placeholder="请输入试卷描述（选填）"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row>
          <el-col :span="12">
            <el-form-item label="防作弊">
              <el-switch v-model="form.antiCheatingEnabled" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-card>

      <!-- 题目列表 -->
      <el-card shadow="never" class="form-card">
        <template #header>
          <div style="display: flex; justify-content: space-between; align-items: center">
            <span>题目列表（已选 {{ form.questions.length }} 题，总分 {{ computedScore }} 分）</span>
            <el-button type="primary" size="small" :icon="Plus" @click="showQuestionPicker = true">
              添加题目
            </el-button>
          </div>
        </template>

        <el-table :data="form.questions" stripe>
          <el-table-column label="序号" width="60">
            <template #default="{ $index }">{{ $index + 1 }}</template>
          </el-table-column>
          <el-table-column prop="content" label="题目内容" min-width="300">
            <template #default="{ row }">
              <span class="question-content">{{ row.content }}</span>
            </template>
          </el-table-column>
          <el-table-column label="类型" width="100">
            <template #default="{ row }">{{ typeLabel(row.type) }}</template>
          </el-table-column>
          <el-table-column label="分值" width="120">
            <template #default="{ row }">
              <el-input-number
                v-model="row.score"
                :min="1"
                :max="100"
                size="small"
                style="width: 100px"
              />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80">
            <template #default="{ $index }">
              <el-button size="small" type="danger" text @click="removeQuestion($index)">
                <el-icon><Delete /></el-icon>
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <div class="form-actions">
        <el-button @click="$router.push('/exam-papers')">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">
          {{ isEdit ? '保存修改' : '创建试卷' }}
        </el-button>
      </div>
    </el-form>

    <!-- 题目选择器对话框 -->
    <el-dialog v-model="showQuestionPicker" title="选择题目" width="800px">
      <div class="picker-filter">
        <el-form inline>
          <el-form-item label="类型">
            <el-select v-model="pickerQuery.type" placeholder="全部" clearable style="width: 120px">
              <el-option label="单选题" :value="1" />
              <el-option label="多选题" :value="2" />
              <el-option label="判断题" :value="3" />
              <el-option label="简答题" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="知识点">
            <el-input v-model="pickerQuery.knowledgePoint" clearable placeholder="输入知识点" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="loadPickerQuestions">查询</el-button>
          </el-form-item>
        </el-form>
      </div>
      <el-table
        v-loading="pickerLoading"
        :data="pickerQuestions"
        @selection-change="selectedQuestions = $event"
      >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="content" label="题目内容" min-width="280">
          <template #default="{ row }">
            <span class="question-content">{{ row.content }}</span>
          </template>
        </el-table-column>
        <el-table-column label="类型" width="90">
          <template #default="{ row }">{{ typeLabel(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="difficulty" label="难度" width="80" />
        <el-table-column prop="knowledgePoint" label="知识点" width="120" />
      </el-table>
      <el-pagination
        v-model:current-page="pickerQuery.page"
        v-model:page-size="pickerQuery.pageSize"
        :total="pickerTotal"
        layout="prev, pager, next"
        small
        class="pagination"
        @change="loadPickerQuestions"
      />
      <template #footer>
        <el-button @click="showQuestionPicker = false">取消</el-button>
        <el-button type="primary" @click="addSelectedQuestions">添加选中（{{ selectedQuestions.length }}）</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { examPapersApi } from '@/api/examPapers'
import { questionsApi } from '@/api/questions'
import { ElMessage } from 'element-plus'
import { Plus, Delete, ArrowLeft } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const isEdit = computed(() => !!route.params.id)
const pageLoading = ref(false)
const submitting = ref(false)
const formRef = ref(null)

const form = reactive({
  title: '',
  description: '',
  totalScore: 100,
  durationMinutes: 90,
  startTime: null,
  endTime: null,
  antiCheatingEnabled: false,
  questions: []
})

const rules = {
  title: [{ required: true, message: '请输入试卷标题', trigger: 'blur' }],
  totalScore: [{ required: true, message: '请设置总分', trigger: 'blur' }],
  durationMinutes: [{ required: true, message: '请设置考试时长', trigger: 'blur' }]
}

const computedScore = computed(() => form.questions.reduce((sum, q) => sum + (q.score || 0), 0))

const typeLabel = (t) => ({ 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }[t] ?? t)

// 题目选择器
const showQuestionPicker = ref(false)
const pickerLoading = ref(false)
const pickerQuestions = ref([])
const pickerTotal = ref(0)
const selectedQuestions = ref([])
const pickerQuery = reactive({ page: 1, pageSize: 10, type: null, knowledgePoint: '' })

async function loadPickerQuestions() {
  pickerLoading.value = true
  try {
    const res = await questionsApi.getList({
      page: pickerQuery.page,
      pageSize: pickerQuery.pageSize,
      type: pickerQuery.type ?? undefined,
      knowledgePoint: pickerQuery.knowledgePoint || undefined
    })
    pickerQuestions.value = res.items
    pickerTotal.value = res.totalCount
  } finally {
    pickerLoading.value = false
  }
}

function addSelectedQuestions() {
  const existingIds = new Set(form.questions.map(q => q.questionId))
  const toAdd = selectedQuestions.value.filter(q => !existingIds.has(q.id))
  toAdd.forEach((q, i) => {
    form.questions.push({
      questionId: q.id,
      content: q.content,
      type: q.type,
      score: 5,
      order: form.questions.length + i + 1
    })
  })
  showQuestionPicker.value = false
  if (toAdd.length) ElMessage.success(`已添加 ${toAdd.length} 道题目`)
}

function removeQuestion(index) {
  form.questions.splice(index, 1)
  form.questions.forEach((q, i) => { q.order = i + 1 })
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    const payload = {
      title: form.title,
      description: form.description,
      totalScore: form.totalScore,
      durationMinutes: form.durationMinutes,
      startTime: form.startTime,
      endTime: form.endTime,
      antiCheatingEnabled: form.antiCheatingEnabled,
      questions: form.questions.map((q, i) => ({
        questionId: q.questionId,
        score: q.score,
        order: i + 1
      }))
    }
    if (isEdit.value) {
      await examPapersApi.update(route.params.id, payload)
      ElMessage.success('保存成功')
    } else {
      await examPapersApi.create(payload)
      ElMessage.success('创建成功')
    }
    router.push('/exam-papers')
  } finally {
    submitting.value = false
  }
}

async function loadDetail() {
  if (!isEdit.value) return
  pageLoading.value = true
  try {
    const data = await examPapersApi.getById(route.params.id)
    form.title = data.title
    form.description = data.description || ''
    form.totalScore = data.totalScore
    form.durationMinutes = data.durationMinutes
    form.startTime = data.startTime
    form.endTime = data.endTime
    form.antiCheatingEnabled = data.antiCheatingEnabled
    form.questions = data.questions.map(q => ({
      questionId: q.questionId,
      content: q.content,
      type: q.type,
      score: q.score,
      order: q.order
    }))
  } finally {
    pageLoading.value = false
  }
}

onMounted(() => {
  loadDetail()
  loadPickerQuestions()
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
.form-card { margin-bottom: 16px; }
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 16px;
}
.question-content {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.picker-filter { margin-bottom: 12px; }
.pagination { margin-top: 12px; justify-content: center; }
</style>
