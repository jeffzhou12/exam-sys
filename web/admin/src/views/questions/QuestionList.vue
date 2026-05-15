<template>
  <div class="page-container">
    <div class="page-header">
      <h3>题库管理</h3>
      <div class="header-actions">
        <el-button :icon="MagicStick" @click="aiDialogVisible = true">AI 生成题目</el-button>
        <el-button type="primary" :icon="Plus" @click="openDialog()">手动新建</el-button>
      </div>
    </div>

    <!-- 筛选 -->
    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="题目类型">
          <el-select v-model="query.type" placeholder="全部" clearable style="width: 120px">
            <el-option label="单选题" :value="1" />
            <el-option label="多选题" :value="2" />
            <el-option label="判断题" :value="3" />
            <el-option label="简答题" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item label="难度">
          <el-select v-model="query.difficulty" placeholder="全部" clearable style="width: 100px">
            <el-option label="1" :value="1" />
            <el-option label="2" :value="2" />
            <el-option label="3" :value="3" />
            <el-option label="4" :value="4" />
            <el-option label="5" :value="5" />
          </el-select>
        </el-form-item>
        <el-form-item label="知识点">
          <el-input v-model="query.knowledgePoint" clearable placeholder="输入知识点" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="loadData">查询</el-button>
          <el-button :icon="Refresh" @click="resetQuery">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 表格 -->
    <el-card shadow="never" class="table-card">
      <el-table v-loading="loading" :data="tableData" stripe>
        <el-table-column prop="content" label="题目内容" min-width="300">
          <template #default="{ row }">
            <span class="question-content">{{ row.content }}</span>
          </template>
        </el-table-column>
        <el-table-column label="类型" width="90">
          <template #default="{ row }">
            <el-tag size="small" :type="typeTagType(row.type)">{{ typeLabel(row.type) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="difficulty" label="难度" width="70" />
        <el-table-column prop="knowledgePoint" label="知识点" width="130">
          <template #default="{ row }">{{ row.knowledgePoint || '-' }}</template>
        </el-table-column>
        <el-table-column label="来源" width="90">
          <template #default="{ row }">
            <el-tag size="small" :type="row.isAiGenerated ? 'warning' : 'info'">
              {{ row.isAiGenerated ? 'AI生成' : '手动' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="180">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="primary" :icon="Edit" @click="openDialog(row)">编辑</el-button>
            <el-button size="small" type="danger" :icon="Delete" @click="deleteQuestion(row)">删除</el-button>
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

    <!-- 新建/编辑题目对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingId ? '编辑题目' : '新建题目'"
      width="640px"
      @closed="resetForm"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="题目类型" prop="type">
          <el-radio-group v-model="form.type">
            <el-radio :value="1">单选题</el-radio>
            <el-radio :value="2">多选题</el-radio>
            <el-radio :value="3">判断题</el-radio>
            <el-radio :value="4">简答题</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="题目内容" prop="content">
          <el-input v-model="form.content" type="textarea" :rows="3" placeholder="请输入题目内容" />
        </el-form-item>

        <!-- 单选/多选选项 -->
        <template v-if="[1, 2].includes(form.type)">
          <el-form-item label="选项" prop="options">
            <div class="options-container">
              <div v-for="(opt, key) in form.options" :key="key" class="option-item">
                <el-tag>{{ key }}</el-tag>
                <el-input v-model="form.options[key]" placeholder="选项内容" style="flex: 1; margin-left: 8px" />
              </div>
            </div>
          </el-form-item>
        </template>

        <el-form-item label="正确答案" prop="correctAnswer">
          <!-- 判断题 -->
          <el-radio-group v-if="form.type === 3" v-model="form.correctAnswer">
            <el-radio value="True">正确</el-radio>
            <el-radio value="False">错误</el-radio>
          </el-radio-group>
          <!-- 单选题 -->
          <el-radio-group v-else-if="form.type === 1" v-model="form.correctAnswer">
            <el-radio v-for="(_, key) in form.options" :key="key" :value="key">{{ key }}</el-radio>
          </el-radio-group>
          <!-- 多选题 -->
          <el-checkbox-group
            v-else-if="form.type === 2"
            :model-value="form.correctAnswer.split('')"
            @update:model-value="form.correctAnswer = $event.sort().join('')"
          >
            <el-checkbox v-for="(_, key) in form.options" :key="key" :value="key">{{ key }}</el-checkbox>
          </el-checkbox-group>
          <!-- 简答题 -->
          <el-input
            v-else
            v-model="form.correctAnswer"
            type="textarea"
            :rows="3"
            placeholder="请输入参考答案"
          />
        </el-form-item>

        <el-form-item label="解析">
          <el-input v-model="form.explanation" type="textarea" :rows="2" placeholder="请输入答案解析（选填）" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="知识点">
              <el-input v-model="form.knowledgePoint" placeholder="选填" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="难度">
              <el-rate v-model="form.difficulty" :max="5" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button :icon="Close" @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :icon="Check" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- AI 生成题目对话框 -->
    <el-dialog v-model="aiDialogVisible" title="AI 生成题目" width="460px">
      <el-form ref="aiFormRef" :model="aiForm" :rules="aiRules" label-width="100px">
        <el-form-item label="知识点" prop="knowledgePoint">
          <el-input v-model="aiForm.knowledgePoint" placeholder="请输入知识点，例如：Python 列表" />
        </el-form-item>
        <el-form-item label="题目类型" prop="questionType">
          <el-select v-model="aiForm.questionType" style="width: 100%">
            <el-option label="单选题" :value="1" />
            <el-option label="多选题" :value="2" />
            <el-option label="判断题" :value="3" />
            <el-option label="简答题" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item label="生成数量" prop="count">
          <el-input-number v-model="aiForm.count" :min="1" :max="20" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :icon="Close" @click="aiDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="aiLoading" :icon="MagicStick" @click="handleAiGenerate">
          开始生成
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { questionsApi } from '@/api/questions'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, MagicStick, Search, Refresh, Edit, Delete, Close, Check } from '@element-plus/icons-vue'

const loading = ref(false)
const submitting = ref(false)
const aiLoading = ref(false)
const tableData = ref([])
const total = ref(0)
const dialogVisible = ref(false)
const aiDialogVisible = ref(false)
const editingId = ref(null)
const formRef = ref(null)
const aiFormRef = ref(null)

const defaultOptions = { A: '', B: '', C: '', D: '' }

const query = reactive({ page: 1, pageSize: 20, type: null, difficulty: null, knowledgePoint: '' })

const form = reactive({
  type: 1,
  content: '',
  options: { A: '', B: '', C: '', D: '' },
  correctAnswer: '',
  explanation: '',
  knowledgePoint: '',
  difficulty: 1
})

const aiForm = reactive({ knowledgePoint: '', questionType: 1, count: 5 })

const rules = {
  type: [{ required: true, message: '请选择题目类型' }],
  content: [{ required: true, message: '请输入题目内容', trigger: 'blur' }],
  correctAnswer: [{ required: true, message: '请设置正确答案', trigger: 'change' }]
}

const aiRules = {
  knowledgePoint: [{ required: true, message: '请输入知识点', trigger: 'blur' }],
  questionType: [{ required: true }],
  count: [{ required: true }]
}

const typeLabel = (t) => ({ 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }[t] ?? t)
const typeTagType = (t) => ({ 1: '', 2: 'warning', 3: 'info', 4: 'success' }[t] ?? '')

async function loadData() {
  loading.value = true
  try {
    const res = await questionsApi.getList({
      page: query.page,
      pageSize: query.pageSize,
      type: query.type ?? undefined,
      difficulty: query.difficulty ?? undefined,
      knowledgePoint: query.knowledgePoint || undefined
    })
    tableData.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.page = 1
  query.type = null
  query.difficulty = null
  query.knowledgePoint = ''
  loadData()
}

async function openDialog(row = null) {
  if (row) {
    editingId.value = row.id
    // 加载详情
    const detail = await questionsApi.getById(row.id)
    form.type = detail.type
    form.content = detail.content
    form.correctAnswer = detail.correctAnswer
    form.explanation = detail.explanation || ''
    form.knowledgePoint = detail.knowledgePoint || ''
    form.difficulty = detail.difficulty
    // 解析选项
    if (detail.options) {
      try {
        const opts = typeof detail.options === 'string' ? JSON.parse(detail.options) : detail.options
        Object.assign(form.options, opts)
      } catch {
        Object.assign(form.options, { ...defaultOptions })
      }
    } else {
      Object.assign(form.options, { ...defaultOptions })
    }
  } else {
    editingId.value = null
  }
  dialogVisible.value = true
}

function resetForm() {
  editingId.value = null
  form.type = 1
  form.content = ''
  form.options = { A: '', B: '', C: '', D: '' }
  form.correctAnswer = ''
  form.explanation = ''
  form.knowledgePoint = ''
  form.difficulty = 1
  formRef.value?.resetFields()
}

function buildPayload() {
  const payload = {
    type: form.type,
    content: form.content,
    correctAnswer: form.correctAnswer,
    explanation: form.explanation || null,
    knowledgePoint: form.knowledgePoint || null,
    difficulty: form.difficulty,
    options: null
  }
  if ([1, 2].includes(form.type)) {
    // 过滤掉空选项
    const opts = {}
    for (const [k, v] of Object.entries(form.options)) {
      if (v) opts[k] = v
    }
    payload.options = opts
  }
  return payload
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (editingId.value) {
      await questionsApi.update(editingId.value, buildPayload())
      ElMessage.success('更新成功')
    } else {
      await questionsApi.create(buildPayload())
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    submitting.value = false
  }
}

async function deleteQuestion(row) {
  await ElMessageBox.confirm('确定要删除该题目吗？', '提示', { type: 'warning' })
  await questionsApi.delete(row.id)
  ElMessage.success('删除成功')
  loadData()
}

async function handleAiGenerate() {
  const valid = await aiFormRef.value?.validate().catch(() => false)
  if (!valid) return
  aiLoading.value = true
  try {
    const res = await questionsApi.aiGenerate({
      knowledgePoint: aiForm.knowledgePoint,
      questionType: aiForm.questionType,
      count: aiForm.count
    })
    ElMessage.success(`AI 成功生成 ${res.generated} 道题目`)
    aiDialogVisible.value = false
    loadData()
  } finally {
    aiLoading.value = false
  }
}

function formatDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleString('zh-CN')
}

onMounted(loadData)
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.page-header h3 { margin: 0; font-size: 18px; }
.header-actions { display: flex; gap: 8px; }
.filter-card { margin-bottom: 16px; }
.filter-card :deep(.el-card__body) { padding: 16px 16px 0; }
.table-card :deep(.el-card__body) { padding: 16px; }
.pagination { margin-top: 16px; justify-content: flex-end; }
.question-content {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.options-container { width: 100%; }
.option-item {
  display: flex;
  align-items: center;
  margin-bottom: 8px;
}
</style>
