<template>
  <div class="page-container">
    <div class="page-header">
      <h3>用户管理</h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">新建用户</el-button>
    </div>

    <!-- 搜索栏 -->
    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item>
          <el-input v-model="query.search" placeholder="搜索用户名/邮箱" clearable :prefix-icon="Search" />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="query.role" placeholder="全部角色" clearable style="width: 120px">
            <el-option v-if="auth.isSuperAdmin" label="超级管理员" :value="-1" />
            <el-option label="管理员" :value="0" />
            <el-option label="教师" :value="1" />
            <el-option label="学生" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="query.isActive" placeholder="全部状态" clearable style="width: 120px">
            <el-option label="启用" :value="true" />
            <el-option label="禁用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadData">查询</el-button>
          <el-button @click="resetQuery">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <el-card shadow="never" class="table-card">
      <el-table v-loading="loading" :data="tableData" stripe>
        <el-table-column prop="username" label="用户名" min-width="130" />
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column label="角色" width="90">
          <template #default="{ row }">
            <el-tag :type="roleTagType(row.role)" size="small">{{ roleLabel(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'" size="small">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="lastLoginAt" label="最后登录" width="180">
          <template #default="{ row }">{{ formatDate(row.lastLoginAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="openDialog(row)">编辑</el-button>
            <el-button size="small" :type="row.isActive ? 'warning' : 'success'" @click="toggleStatus(row)">{{
              row.isActive ? '禁用' : '启用' }}</el-button>
            <el-button size="small" type="info" @click="resetPasswordDialog(row)">重置密码</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination v-model:current-page="query.page" v-model:page-size="query.pageSize" :page-sizes="[10, 20, 50]"
        :total="total" layout="total, sizes, prev, pager, next" class="pagination" @change="loadData" />
    </el-card>

    <!-- 新建/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑用户' : '新建用户'" width="500px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item v-if="auth.isSuperAdmin" label="所属租户" prop="tenantId">
          <el-select v-model="form.tenantId" placeholder="请选择租户" clearable style="width: 100%">
            <el-option v-for="t in tenants" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="!editingId" label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item v-if="!editingId" label="密码" prop="password">
          <el-input v-model="form.password" type="password" show-password placeholder="请输入密码" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-radio-group v-model="form.role">
            <el-radio v-if="auth.isSuperAdmin" :value="-1">超级管理员</el-radio>
            <el-radio :value="0">管理员</el-radio>
            <el-radio :value="1">教师</el-radio>
            <el-radio :value="2">学生</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 重置密码对话框 -->
    <el-dialog v-model="pwdDialogVisible" title="重置密码" width="420px">
      <el-form ref="pwdFormRef" :model="pwdForm" :rules="pwdRules" label-width="100px">
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="pwdForm.newPassword" type="password" show-password placeholder="请输入新密码（至少6位）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="pwdDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleResetPassword">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { usersApi } from '@/api/users'
import { tenantsApi } from '@/api/tenants'
import { useAuthStore } from '@/stores/auth'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'

const auth = useAuthStore()

// 当超级管理员切换租户时自动重新加载
watch(() => auth.activeTenantId, () => { loadData() })

const loading = ref(false)
const submitting = ref(false)
const tableData = ref([])
const total = ref(0)
const tenants = ref([])
const dialogVisible = ref(false)
const pwdDialogVisible = ref(false)
const editingId = ref(null)
const resetPwdUserId = ref(null)
const formRef = ref(null)
const pwdFormRef = ref(null)

const query = reactive({ page: 1, pageSize: 20, search: '', role: null, isActive: null })
const form = reactive({ tenantId: null, username: '', password: '', email: '', role: 2 })
const pwdForm = reactive({ newPassword: '' })

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, min: 6, message: '密码至少6位', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }]
}
const pwdRules = {
  newPassword: [{ required: true, min: 6, message: '密码至少6位', trigger: 'blur' }]
}

const roleLabel = (r) => ({ '-1': '超级管理员', 0: '管理员', 1: '教师', 2: '学生' }[r] ?? r)
const roleTagType = (r) => ({ '-1': 'danger', 0: 'warning', 1: 'primary', 2: 'info' }[r] ?? '')

async function loadData() {
  loading.value = true
  try {
    const params = {
      page: query.page,
      pageSize: query.pageSize,
      search: query.search || undefined,
      role: query.role ?? undefined,
      isActive: query.isActive ?? undefined
    }
    // 普通管理员只能查看自己租户的用户；SuperAdmin 已选租户时按租户过滤
    if (auth.isAdmin) {
      params.tenantId = auth.tenantId
    } else if (auth.isSuperAdmin && auth.activeTenantId) {
      params.tenantId = auth.activeTenantId
    }
    const res = await usersApi.getList(params)
    tableData.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

async function loadTenants() {
  // 只有超级管理员才需要租户列表（用于新建用户时选择租户）
  if (!auth.isSuperAdmin) return
  const res = await tenantsApi.getList({ page: 1, pageSize: 200 })
  tenants.value = res.items
}

function resetQuery() {
  query.page = 1
  query.search = ''
  query.role = null
  query.isActive = null
  loadData()
}

function openDialog(row = null) {
  if (row) {
    editingId.value = row.id
    form.tenantId = row.tenantId || null
    form.email = row.email || ''
    form.role = row.role
  } else {
    editingId.value = null
    // 普通管理员新建用户时自动归属自己的租户
    form.tenantId = auth.isAdmin ? auth.tenantId : null
    form.username = ''
    form.password = ''
    form.email = ''
    form.role = 2
  }
  dialogVisible.value = true
}

function resetForm() {
  editingId.value = null
  formRef.value?.resetFields()
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (editingId.value) {
      const payload = { email: form.email, role: form.role }
      if (auth.isSuperAdmin) payload.tenantId = form.tenantId
      await usersApi.update(editingId.value, payload)
      ElMessage.success('更新成功')
    } else {
      await usersApi.create({
        tenantId: form.tenantId,
        username: form.username,
        password: form.password,
        email: form.email,
        role: form.role
      })
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    submitting.value = false
  }
}

async function toggleStatus(row) {
  const action = row.isActive ? '禁用' : '启用'
  await ElMessageBox.confirm(`确定要${action}用户「${row.username}」吗？`, '提示', { type: 'warning' })
  await usersApi.toggleStatus(row.id)
  ElMessage.success(`${action}成功`)
  loadData()
}

function resetPasswordDialog(row) {
  resetPwdUserId.value = row.id
  pwdForm.newPassword = ''
  pwdDialogVisible.value = true
}

async function handleResetPassword() {
  const valid = await pwdFormRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    await usersApi.resetPassword(resetPwdUserId.value, { newPassword: pwdForm.newPassword })
    ElMessage.success('密码重置成功')
    pwdDialogVisible.value = false
  } finally {
    submitting.value = false
  }
}

function formatDate(val) {
  if (!val) return '-'
  return new Date(val).toLocaleString('zh-CN')
}

onMounted(() => {
  loadData()
  loadTenants()
})

// SuperAdmin 切换租户后重新加载用户列表
watch(() => auth.activeTenantId, () => {
  query.page = 1
  loadData()
})
</script>

<style scoped>
.page-container {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.page-header h3 {
  margin: 0;
  font-size: 18px;
}

.filter-card {
  margin-bottom: 16px;
}

.filter-card :deep(.el-card__body) {
  padding: 16px 16px 0;
}

.table-card :deep(.el-card__body) {
  padding: 16px;
}

.pagination {
  margin-top: 16px;
  justify-content: flex-end;
}
</style>
