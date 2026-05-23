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
        <el-table-column prop="content" label="题目内容" min-width="200">
          <template #default="{ row }">
            <span class="question-content">{{ buildQuestionPreview(row) }}</span>
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
        <el-table-column label="来源" width="100">
          <template #default="{ row }">
            <el-tag size="small" :type="row.isAiGenerated ? 'warning' : 'info'">
              {{ row.isAiGenerated ? 'AI生成' : '手动' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column v-if="isAllTenantsMode" label="所属租户" width="150" show-overflow-tooltip>
          <template #default="{ row }">
            {{ tenantNameMap[row.tenantId] || row.tenantId?.slice(0, 8) || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="200">
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
      width="900px"
      class="question-edit-dialog"
      top="3vh"
      @closed="resetForm"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item
          v-if="isAllTenantsMode && !editingId"
          label="所属租户"
          prop="tenantId"
          :rules="[{ required: true, message: '请选择所属租户', trigger: 'change' }]"
        >
          <el-select v-model="form.tenantId" placeholder="请选择归属租户（必选）" style="width: 100%">
            <el-option v-for="t in allTenants" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="题目类型" prop="type">
          <el-radio-group v-model="form.type">
            <el-radio :value="1">单选题</el-radio>
            <el-radio :value="2">多选题</el-radio>
            <el-radio :value="3">判断题</el-radio>
            <el-radio :value="4">简答题</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="题目内容" prop="content">
          <RichTextField
            v-model="form.content"
            :rows="8"
            placeholder="请输入题目内容（支持 HTML/SVG/LaTeX 标记）"
          />
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
          <RichTextField
            v-else
            v-model="form.correctAnswer"
            :rows="7"
            placeholder="请输入参考答案（支持富文本与公式）"
          />
        </el-form-item>

        <el-form-item label="解析">
          <RichTextField
            v-model="form.explanation"
            :rows="6"
            placeholder="请输入答案解析（支持富文本、公式、SVG 绘图，选填）"
          />
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
    <el-dialog v-model="aiDialogVisible" title="AI 生成题目" width="620px" class="question-ai-dialog" top="6vh">
      <el-form ref="aiFormRef" :model="aiForm" :rules="aiRules" label-width="100px">
        <el-form-item label="生成配置" required>
          <div class="ai-type-config-table">
            <div class="ai-type-config-header">
              <span>知识点</span><span>题型</span><span>难度</span><span>数量</span>
            </div>
            <div v-for="(row, idx) in aiForm.typeConfigs" :key="idx" class="ai-type-config-row">
              <el-input v-model="row.knowledgePoint" size="small" placeholder="如：Python 列表" style="width: 180px" />
              <el-select v-model="row.type" size="small" style="width: 120px">
                <el-option label="单选题" :value="1" />
                <el-option label="多选题" :value="2" />
                <el-option label="判断题" :value="3" />
                <el-option label="简答题" :value="4" />
              </el-select>
              <el-select v-model="row.difficulty" size="small" style="width: 100px">
                <el-option label="难度 1" :value="1" />
                <el-option label="难度 2" :value="2" />
                <el-option label="难度 3" :value="3" />
                <el-option label="难度 4" :value="4" />
                <el-option label="难度 5" :value="5" />
              </el-select>
              <el-input-number v-model="row.count" :min="1" :max="50" size="small" style="width: 110px" />
              <span class="type-count-label">道</span>
              <el-button
                type="danger"
                :icon="Delete"
                circle
                text
                size="small"
                :disabled="aiForm.typeConfigs.length <= 1"
                @click="aiForm.typeConfigs.splice(idx, 1)"
              />
            </div>
            <el-button
              plain
              size="small"
              :icon="Plus"
              class="ai-add-config-btn"
              @click="addAiTypeConfig"
            >
              添加一行
            </el-button>
          </div>
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
import { ref, reactive, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { questionsApi } from '@/api/questions'
import { useAuthStore } from '@/stores/auth'
import RichTextField from '@/components/RichTextField.vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, MagicStick, Search, Refresh, Edit, Delete, Close, Check } from '@element-plus/icons-vue'

const auth = useAuthStore()

// 全租户模式：超级管理员且未选择具体租户
const isAllTenantsMode = computed(() => auth.isSuperAdmin && !auth.activeTenantId)
const allTenants = ref([])
const tenantNameMap = computed(() =>
  Object.fromEntries(allTenants.value.map(t => [t.id, t.name]))
)

function syncTenantsFromCache() {
  try {
    const raw = localStorage.getItem('admin.tenants.cache')
    allTenants.value = raw ? JSON.parse(raw) : []
  } catch { allTenants.value = [] }
}

function onTenantsUpdated(e) {
  const list = e?.detail
  allTenants.value = Array.isArray(list) ? list : []
}

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
  difficulty: 1,
  tenantId: null
})

const aiForm = reactive({
  typeConfigs: [{ knowledgePoint: '', type: 1, difficulty: 3, count: 5 }]
})

const rules = {
  type: [{ required: true, message: '请选择题目类型' }],
  content: [{ required: true, message: '请输入题目内容', trigger: 'blur' }],
  correctAnswer: [{ required: true, message: '请设置正确答案', trigger: 'change' }]
}

const aiRules = {}

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
    form.tenantId = detail.tenantId || null
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
    // 超级管理员已选中租户时预填
    form.tenantId = auth.activeTenantId || null
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
  form.tenantId = null
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
  // 全租户模式新建时必须选租户
  if (isAllTenantsMode.value && !editingId.value && !form.tenantId) {
    ElMessage.warning('请选择所属租户')
    return
  }
  submitting.value = true
  try {
    const tenantOverride = (isAllTenantsMode.value && !editingId.value) ? form.tenantId : null
    if (editingId.value) {
      await questionsApi.update(editingId.value, buildPayload())
      ElMessage.success('更新成功')
    } else {
      await questionsApi.create(buildPayload(), tenantOverride)
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

  const validConfigs = aiForm.typeConfigs.filter(cfg => cfg.count > 0)
  if (!validConfigs.length) {
    ElMessage.warning('请至少配置一种题型')
    return
  }
  if (validConfigs.some(cfg => !String(cfg.knowledgePoint || '').trim())) {
    ElMessage.warning('请为每个配置项填写知识点')
    return
  }
  if (validConfigs.some(cfg => cfg.difficulty < 1 || cfg.difficulty > 5)) {
    ElMessage.warning('题目难度必须在 1~5 之间')
    return
  }
  const total = validConfigs.reduce((sum, cfg) => sum + cfg.count, 0)
  if (total > 100) {
    ElMessage.warning('单次生成题目总数不能超过 100')
    return
  }

  aiLoading.value = true
  try {
    const res = await questionsApi.aiGenerate({
      typeConfigs: validConfigs.map(cfg => ({
        knowledgePoint: String(cfg.knowledgePoint || '').trim(),
        type: cfg.type,
        difficulty: cfg.difficulty,
        count: cfg.count
      }))
    })
    ElMessage.success(`AI 成功生成 ${res.generated} 道题目`)
    aiDialogVisible.value = false
    resetAiForm()
    loadData()
  } finally {
    aiLoading.value = false
  }
}

function addAiTypeConfig() {
  aiForm.typeConfigs.push({ knowledgePoint: '', type: 1, difficulty: 3, count: 5 })
}

function resetAiForm() {
  aiForm.typeConfigs = [{ knowledgePoint: '', type: 1, difficulty: 3, count: 5 }]
  aiFormRef.value?.clearValidate()
}

function formatDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleString('zh-CN')
}

function toPlainText(raw) {
  if (!raw) return ''
  return String(raw)
    .replace(/<style[\s\S]*?<\/style>/gi, ' ')
    .replace(/<script[\s\S]*?<\/script>/gi, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

function normalizeOptions(optionsRaw) {
  if (!optionsRaw) return []

  let source = optionsRaw
  if (typeof source === 'string') {
    try {
      source = JSON.parse(source)
    } catch {
      return []
    }
  }

  if (Array.isArray(source)) {
    return source
      .map((text, idx) => ({
        label: String.fromCharCode(65 + idx),
        text: toPlainText(text)
      }))
      .filter(x => x.text)
  }

  if (source && typeof source === 'object') {
    return Object.entries(source)
      .map(([key, value]) => ({
        label: String(key || '').trim().slice(0, 1).toUpperCase(),
        text: toPlainText(value)
      }))
      .filter(x => x.label && x.text)
      .sort((a, b) => a.label.localeCompare(b.label, 'en'))
  }

  return []
}

function buildQuestionPreview(row) {
  const content = toPlainText(row.content)
  const options = normalizeOptions(row.options)
  if (!options.length) return content

  const optionText = options
    .map(opt => `${opt.label}. ${opt.text}`)
    .join('  ')

  return `${content} ${optionText}`.trim()
}

// 租户切换时刷新列表
watch(() => auth.activeTenantId, () => {
  query.page = 1
  loadData()
})

onMounted(() => {
  if (auth.isSuperAdmin) syncTenantsFromCache()
  window.addEventListener('admin-tenants-updated', onTenantsUpdated)
  loadData()
})

onBeforeUnmount(() => {
  window.removeEventListener('admin-tenants-updated', onTenantsUpdated)
})

watch(aiDialogVisible, (visible) => {
  if (!visible) resetAiForm()
})
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
  line-clamp: 4;
  -webkit-line-clamp: 4;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.options-container { width: 100%; }
.option-item {
  display: flex;
  align-items: center;
  margin-bottom: 8px;
}

:deep(.question-edit-dialog .el-dialog__body) {
  max-height: calc(100vh - 170px);
  overflow-y: auto !important;
  overflow-x: hidden;
}

:deep(.question-ai-dialog .el-dialog__body) {
  max-height: calc(100vh - 170px);
  overflow-y: auto !important;
}

.ai-type-config-table {
  width: 100%;
}

.ai-type-config-header {
  display: grid;
  grid-template-columns: 180px 120px 100px 120px;
  gap: 12px;
  margin-bottom: 8px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.ai-type-config-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
}

.type-count-label {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.ai-add-config-btn {
  margin-top: 2px;
}
</style>
