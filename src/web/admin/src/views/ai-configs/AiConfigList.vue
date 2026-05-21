<template>
  <div class="page-container">
    <div class="page-header">
      <h3>AI 模型配置</h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">新增配置</el-button>
    </div>

    <!-- 筛选栏 -->
    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <!-- SuperAdmin 可切换查询系统级 or 指定租户 -->
        <el-form-item v-if="auth.isSuperAdmin" label="租户">
          <el-select
            v-model="query.tenantId"
            placeholder="系统级（不限租户）"
            clearable
            style="width: 200px"
            @change="loadData"
          >
            <el-option
              v-for="t in tenantOptions"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="loadData">查询</el-button>
          <el-button :icon="Refresh" @click="resetQuery">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <el-card shadow="never" class="table-card">
      <el-table v-loading="loading" :data="tableData" stripe row-key="id">
        <el-table-column label="场景" width="120">
          <template #default="{ row }">
            <el-tag size="small" :type="sceneTagType(row.scene)">{{ sceneLabel(row.scene) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="providerName" label="提供商" width="120" />
        <el-table-column prop="chatModel" label="模型" min-width="160" show-overflow-tooltip />
        <el-table-column prop="baseUrl" label="接口地址" min-width="200" show-overflow-tooltip />
        <el-table-column prop="apiKeyMasked" label="API Key" width="130" />
        <el-table-column label="月度配额" width="150">
          <template #default="{ row }">
            <span v-if="row.monthlyQuotaTokens">
              {{ formatNumber(row.usedTokensCurrentMonth) }} /
              {{ formatNumber(row.monthlyQuotaTokens) }}
            </span>
            <el-tag v-else size="small" type="info">不限额</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="优先级" width="80" align="center">
          <template #default="{ row }">{{ row.priority }}</template>
        </el-table-column>
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
              {{ row.isEnabled ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column v-if="auth.isSuperAdmin" label="归属" width="100" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="!row.tenantId">
              <el-tag type="danger" size="small">系统级</el-tag>
            </span>
            <span v-else>{{ row.tenantName || row.tenantId?.slice(0, 8) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="primary" :icon="Edit" @click="openDialog(row)">编辑</el-button>
            <el-button size="small" type="warning" :icon="Refresh" @click="handleResetQuota(row)">重置配额</el-button>
            <el-button size="small" type="danger" :icon="Delete" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新建 / 编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingId ? '编辑 AI 配置' : '新增 AI 配置'"
      width="600px"
      @closed="resetForm"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="110px">
        <!-- SuperAdmin 才可以选择租户归属 -->
        <el-form-item v-if="auth.isSuperAdmin" label="归属租户">
          <el-select
            v-model="form.tenantId"
            placeholder="留空 = 系统级配置"
            clearable
            style="width: 100%"
          >
            <el-option
              v-for="t in tenantOptions"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            />
          </el-select>
          <div class="form-tip">留空表示系统级配置，对所有租户生效（优先级低于租户专属配置）</div>
        </el-form-item>

        <el-form-item label="使用场景" prop="scene">
          <el-select v-model="form.scene" style="width: 100%">
            <el-option
              v-for="s in AI_SCENES"
              :key="s.value"
              :label="s.label"
              :value="s.value"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="提供商名称" prop="providerName">
          <el-autocomplete
            v-model="form.providerName"
            :fetch-suggestions="suggestProvider"
            placeholder="如 OpenAI、DeepSeek、SiliconFlow"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="接口地址" prop="baseUrl">
          <el-input v-model="form.baseUrl" placeholder="如 https://api.openai.com/v1" />
        </el-form-item>

        <el-form-item label="API Key" :prop="editingId ? '' : 'apiKey'">
          <el-input
            v-model="form.apiKey"
            type="password"
            show-password
            :placeholder="editingId ? '留空则保留现有 Key' : '请输入 API Key'"
          />
        </el-form-item>

        <el-form-item label="对话模型" prop="chatModel">
          <el-input v-model="form.chatModel" placeholder="如 gpt-4o、deepseek-chat" />
        </el-form-item>

        <el-form-item label="嵌入模型">
          <el-input v-model="form.embeddingModel" placeholder="如 text-embedding-3-small（可选）" />
        </el-form-item>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="最大 Token" prop="maxTokens">
              <el-input-number v-model="form.maxTokens" :min="256" :max="128000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Temperature" prop="temperature">
              <el-input-number
                v-model="form.temperature"
                :min="0"
                :max="2"
                :step="0.1"
                :precision="1"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="月度配额">
              <el-input-number
                v-model="form.monthlyQuotaTokens"
                :min="0"
                placeholder="0 = 不限额"
                style="width: 100%"
              />
              <div class="form-tip">0 或留空 = 无限制</div>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="优先级">
              <el-input-number v-model="form.priority" :min="0" :max="100" style="width: 100%" />
              <div class="form-tip">数值越大优先使用</div>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="启用">
          <el-switch v-model="form.isEnabled" />
        </el-form-item>

        <el-form-item label="备注">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="可选备注" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button :icon="Close" @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :icon="Check" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { aiConfigsApi, AI_SCENES } from '@/api/aiConfigs'
import { tenantsApi } from '@/api/tenants'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Refresh, Edit, Delete, Close, Check } from '@element-plus/icons-vue'

const auth = useAuthStore()
const loading = ref(false)
const submitting = ref(false)
const tableData = ref([])
const dialogVisible = ref(false)
const editingId = ref(null)
const formRef = ref(null)
const tenantOptions = ref([])

const query = reactive({ tenantId: auth.isSuperAdmin ? (auth.activeTenantId || undefined) : undefined })

const PROVIDER_SUGGESTIONS = ['OpenAI', 'DeepSeek', 'SiliconFlow', 'Moonshot', 'ZhipuAI', 'Qwen', 'Baidu', 'ByteDance']

const defaultForm = () => ({
  tenantId: null,
  scene: 0,
  providerName: '',
  baseUrl: '',
  apiKey: '',
  chatModel: '',
  embeddingModel: '',
  maxTokens: 4096,
  temperature: 0.7,
  monthlyQuotaTokens: null,
  priority: 0,
  isEnabled: true,
  description: ''
})

const form = reactive(defaultForm())

const rules = {
  scene:        [{ required: true, message: '请选择场景', trigger: 'change' }],
  providerName: [{ required: true, message: '请输入提供商名称', trigger: 'blur' }],
  baseUrl:      [{ required: true, message: '请输入接口地址', trigger: 'blur' }],
  apiKey:       [{ required: true, message: '请输入 API Key', trigger: 'blur' }],
  chatModel:    [{ required: true, message: '请输入对话模型名称', trigger: 'blur' }],
  maxTokens:    [{ required: true, message: '请设置最大 Token', trigger: 'change' }],
  temperature:  [{ required: true, message: '请设置 Temperature', trigger: 'change' }]
}

// ── 场景标签 ────────────────────────────────────────────────────────────────
function sceneLabel(val) {
  return AI_SCENES.find(s => s.value === val)?.label ?? `场景 ${val}`
}
const SCENE_TYPES = ['', 'primary', 'success', 'warning', 'danger', 'info']
function sceneTagType(val) {
  return SCENE_TYPES[val] ?? ''
}

// ── 数字格式化 ──────────────────────────────────────────────────────────────
function formatNumber(n) {
  if (n == null) return '0'
  return n.toLocaleString('zh-CN')
}

// ── 同步顶部租户切换器 ──────────────────────────────────────────────────────
watch(() => auth.activeTenantId, (id) => {
  query.tenantId = id || undefined
  loadData()
})

// ── 加载数据 ────────────────────────────────────────────────────────────────
async function loadData() {
  loading.value = true
  try {
    const params = {}
    if (auth.isSuperAdmin && query.tenantId !== undefined) {
      params.tenantId = query.tenantId || undefined
    }
    const res = await aiConfigsApi.getList(params)
    tableData.value = Array.isArray(res) ? res : (res.items ?? [])
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.tenantId = undefined
  loadData()
}

// ── 租户选项（SuperAdmin 专用）──────────────────────────────────────────────
async function loadTenants() {
  if (!auth.isSuperAdmin) return
  try {
    const res = await tenantsApi.getList({ pageSize: 200 })
    tenantOptions.value = Array.isArray(res) ? res : (res.items ?? [])
  } catch {}
}

// ── 弹窗 ────────────────────────────────────────────────────────────────────
function openDialog(row = null) {
  if (row) {
    editingId.value = row.id
    Object.assign(form, {
      tenantId:           row.tenantId ?? null,
      scene:              row.scene,
      providerName:       row.providerName,
      baseUrl:            row.baseUrl,
      apiKey:             '',          // 展示时不回填，留空=保持原值
      chatModel:          row.chatModel,
      embeddingModel:     row.embeddingModel ?? '',
      maxTokens:          row.maxTokens,
      temperature:        row.temperature,
      monthlyQuotaTokens: row.monthlyQuotaTokens ?? null,
      priority:           row.priority,
      isEnabled:          row.isEnabled,
      description:        row.description ?? ''
    })
  } else {
    editingId.value = null
    Object.assign(form, defaultForm())
    // 超级管理员已选中租户时，预填租户
    if (auth.isSuperAdmin && auth.activeTenantId) {
      form.tenantId = auth.activeTenantId
    }
  }
  dialogVisible.value = true
}

function resetForm() {
  editingId.value = null
  formRef.value?.clearValidate()
}

function suggestProvider(query, cb) {
  const list = PROVIDER_SUGGESTIONS
    .filter(p => p.toLowerCase().includes(query.toLowerCase()))
    .map(p => ({ value: p }))
  cb(list)
}

// ── 提交 ────────────────────────────────────────────────────────────────────
async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    const payload = {
      tenantId:           form.tenantId || null,
      scene:              form.scene,
      providerName:       form.providerName,
      baseUrl:            form.baseUrl,
      apiKey:             form.apiKey || null,   // null = 后端保留原值
      chatModel:          form.chatModel,
      embeddingModel:     form.embeddingModel || null,
      maxTokens:          form.maxTokens,
      temperature:        form.temperature,
      monthlyQuotaTokens: form.monthlyQuotaTokens || null,
      priority:           form.priority,
      isEnabled:          form.isEnabled,
      description:        form.description || null
    }
    if (editingId.value) {
      await aiConfigsApi.update(editingId.value, payload)
      ElMessage.success('更新成功')
    } else {
      await aiConfigsApi.create(payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    submitting.value = false
  }
}

// ── 重置配额 ─────────────────────────────────────────────────────────────────
async function handleResetQuota(row) {
  if (!row.monthlyQuotaTokens) {
    ElMessage.info('该配置未设置月度配额，无需重置')
    return
  }
  await ElMessageBox.confirm(
    `确定要重置「${sceneLabel(row.scene)} - ${row.providerName}」的月度用量吗？`,
    '重置配额',
    { type: 'warning' }
  )
  await aiConfigsApi.resetQuota(row.id)
  ElMessage.success('配额已重置')
  loadData()
}

// ── 删除 ────────────────────────────────────────────────────────────────────
async function handleDelete(row) {
  await ElMessageBox.confirm(
    `确定要删除「${sceneLabel(row.scene)} - ${row.providerName}」配置吗？此操作不可撤销。`,
    '删除确认',
    { type: 'error', confirmButtonText: '删除', confirmButtonClass: 'el-button--danger' }
  )
  await aiConfigsApi.remove(row.id)
  ElMessage.success('删除成功')
  loadData()
}

onMounted(() => {
  loadTenants()
  loadData()
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
.filter-card { margin-bottom: 16px; }
.table-card :deep(.el-table) { font-size: 13px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
.form-tip { font-size: 12px; color: #999; margin-top: 2px; line-height: 1.4; }
</style>
