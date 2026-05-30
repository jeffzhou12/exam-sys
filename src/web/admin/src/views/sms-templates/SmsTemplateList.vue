<template>
  <div class="page-container">
    <div class="page-header">
      <h3>短信模板管理</h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">新增模板</el-button>
    </div>

    <!-- 筛选栏 -->
    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item v-if="auth.isSuperAdmin" label="租户">
          <el-select
            v-model="query.tenantId"
            placeholder="系统级（不限租户）"
            clearable
            style="width: 200px"
            @change="loadData"
          >
            <el-option v-for="t in tenantOptions" :key="t.id" :label="t.name" :value="t.id" />
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
        <el-table-column label="场景" width="130">
          <template #default="{ row }">
            <el-tag size="small">{{ sceneLabel(row.scene) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="模板名称" width="160" show-overflow-tooltip />
        <el-table-column prop="templateBody" label="短信内容" min-width="260" show-overflow-tooltip />
        <el-table-column label="优先级" width="80" align="center">
          <template #default="{ row }">{{ row.priority }}</template>
        </el-table-column>
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
              {{ row.isEnabled ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="所属租户" width="120">
          <template #default="{ row }">
            <el-tag v-if="!row.tenantId" size="small" type="warning">系统级</el-tag>
            <span v-else class="text-muted">{{ tenantName(row.tenantId) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="更新时间" width="160">
          <template #default="{ row }">{{ formatTime(row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="130" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="primary" link @click="openDialog(row)">编辑</el-button>
            <el-popconfirm title="确认删除此模板？" @confirm="handleDelete(row.id)">
              <template #reference>
                <el-button size="small" type="danger" link>删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingId ? '编辑短信模板' : '新增短信模板'"
      width="560px"
      destroy-on-close
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item v-if="auth.isSuperAdmin" label="所属租户">
          <el-select v-model="form.tenantId" placeholder="留空=系统级" clearable style="width: 100%">
            <el-option v-for="t in tenantOptions" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
          <div class="form-tip">留空表示系统级模板，可被租户专属模板覆盖</div>
        </el-form-item>

        <el-form-item label="业务场景" prop="scene">
          <el-autocomplete
            v-model="form.scene"
            :fetch-suggestions="suggestScene"
            placeholder="如 login、register、reset-password"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="模板名称" prop="name">
          <el-input v-model="form.name" placeholder="便于识别的名称" />
        </el-form-item>

        <el-form-item label="短信内容" prop="templateBody">
          <el-input
            v-model="form.templateBody"
            type="textarea"
            :rows="4"
            placeholder="支持占位符：{code}、{scene}、{target}、{appName}"
          />
          <div class="form-tip">占位符：{code}=验证码，{scene}=场景，{target}=手机号，{appName}=应用名</div>
        </el-form-item>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="优先级">
              <el-input-number v-model="form.priority" :min="0" :max="100" style="width: 100%" />
              <div class="form-tip">数值越大越优先</div>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用">
              <el-switch v-model="form.isEnabled" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="可选备注" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { Plus, Search, Refresh } from '@element-plus/icons-vue'
import { smsTemplatesApi, SMS_SCENES } from '@/api/smsTemplates'
import { tenantsApi } from '@/api/tenants'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const tableData = ref([])
const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const editingId = ref(null)
const formRef = ref(null)
const tenantOptions = ref([])

const query = reactive({
  tenantId: auth.isSuperAdmin ? (auth.activeTenantId || undefined) : undefined
})

const defaultForm = () => ({
  tenantId: null,
  scene: '',
  name: '',
  templateBody: '',
  isEnabled: true,
  priority: 0,
  description: ''
})

const form = reactive(defaultForm())

const rules = {
  scene:        [{ required: true, message: '请输入业务场景标识', trigger: 'blur' }],
  name:         [{ required: true, message: '请输入模板名称', trigger: 'blur' }],
  templateBody: [{ required: true, message: '请输入短信内容', trigger: 'blur' }]
}

watch(() => auth.activeTenantId, (id) => {
  query.tenantId = id || undefined
  loadData()
})

onMounted(async () => {
  if (auth.isSuperAdmin) {
    try {
      const res = await tenantsApi.getList({ pageSize: 200 })
      tenantOptions.value = res.items || []
    } catch { /* ignore */ }
  }
  loadData()
})

async function loadData() {
  loading.value = true
  try {
    const params = { ...(query.tenantId ? { tenantId: query.tenantId } : {}) }
    const res = await smsTemplatesApi.getList(params)
    tableData.value = res || []
  } catch {
    ElMessage.error('加载短信模板失败')
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.tenantId = auth.isSuperAdmin ? (auth.activeTenantId || undefined) : undefined
  loadData()
}

function openDialog(row = null) {
  editingId.value = row?.id || null
  Object.assign(form, defaultForm())
  if (row) {
    Object.assign(form, {
      tenantId: row.tenantId || null,
      scene: row.scene,
      name: row.name,
      templateBody: row.templateBody,
      isEnabled: row.isEnabled,
      priority: row.priority,
      description: row.description || ''
    })
  }
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  saving.value = true
  try {
    const payload = {
      tenantId: form.tenantId || null,
      scene: form.scene.trim(),
      name: form.name.trim(),
      templateBody: form.templateBody.trim(),
      isEnabled: form.isEnabled,
      priority: form.priority,
      description: form.description || null
    }
    if (editingId.value) {
      await smsTemplatesApi.update(editingId.value, payload)
      ElMessage.success('更新成功')
    } else {
      await smsTemplatesApi.create(payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e) {
    ElMessage.error(e?.response?.data?.message || '保存失败')
  } finally {
    saving.value = false
  }
}

async function handleDelete(id) {
  try {
    await smsTemplatesApi.remove(id)
    ElMessage.success('删除成功')
    loadData()
  } catch {
    ElMessage.error('删除失败')
  }
}

function suggestScene(query, cb) {
  const suggestions = SMS_SCENES.map(s => ({ value: s.value }))
  cb(query ? suggestions.filter(s => s.value.includes(query)) : suggestions)
}

function sceneLabel(scene) {
  return SMS_SCENES.find(s => s.value === scene)?.label || scene
}

function tenantName(id) {
  return tenantOptions.value.find(t => t.id === id)?.name || id?.slice(0, 8) + '…'
}

function formatTime(val) {
  if (!val) return '—'
  return new Date(val).toLocaleString('zh-CN', { hour12: false })
}
</script>

<style scoped>
.page-container { padding: 20px; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h3 { margin: 0; font-size: 18px; }
.filter-card { margin-bottom: 16px; }
.table-card {}
.form-tip { font-size: 12px; color: #909399; margin-top: 4px; }
.text-muted { color: #909399; font-size: 13px; }
</style>
