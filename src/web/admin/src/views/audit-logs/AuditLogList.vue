<template>
  <div class="audit-log-page">
    <!-- 筛选栏 -->
    <el-card shadow="never" class="search-card">
      <el-row :gutter="12" align="middle">
        <el-col :span="5">
          <el-select
            v-model="query.tenantId"
            placeholder="租户（全部）"
            clearable
            style="width:100%"
          >
            <el-option v-for="t in tenants" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-input
            v-model="query.username"
            placeholder="用户名"
            clearable
            :prefix-icon="Search"
            @keyup.enter="doSearch"
          />
        </el-col>
        <el-col :span="3">
          <el-select v-model="query.action" placeholder="操作类型" clearable style="width:100%">
            <el-option label="GET" value="GET" />
            <el-option label="POST" value="POST" />
            <el-option label="PUT" value="PUT" />
            <el-option label="PATCH" value="PATCH" />
            <el-option label="DELETE" value="DELETE" />
          </el-select>
        </el-col>
        <el-col :span="3">
          <el-input v-model="query.entityType" placeholder="资源类型" clearable @keyup.enter="doSearch" />
        </el-col>
        <el-col :span="6">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="~"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width:100%"
          />
        </el-col>
        <el-col :span="3">
          <el-button type="primary" :icon="Search" @click="doSearch">查询</el-button>
          <el-button :icon="Refresh" @click="resetQuery">重置</el-button>
        </el-col>
      </el-row>
    </el-card>

    <!-- 表格 -->
    <el-card shadow="never" style="margin-top:16px">
      <el-table v-loading="loading" :data="logs" stripe size="small">
        <el-table-column label="时间" width="156" fixed="left">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="用户" width="110">
          <template #default="{ row }">
            <div>{{ row.username || '—' }}</div>
            <el-tag v-if="row.role" size="small" :type="roleTagType(row.role)" style="margin-top:2px">
              {{ roleLabel(row.role) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="actionTagType(row.action)" size="small">{{ row.action }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="资源类型" width="110">
          <template #default="{ row }">
            <span>{{ row.entityType || '—' }}</span>
            <div v-if="row.entityId" class="entity-id text-muted">{{ row.entityId.slice(0, 8) }}…</div>
          </template>
        </el-table-column>
        <el-table-column label="请求路径" min-width="200">
          <template #default="{ row }">
            <span class="path-text">{{ row.requestPath }}</span>
            <span v-if="row.queryString" class="text-muted">{{ row.queryString }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="68" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.statusCode)" size="small">{{ row.statusCode }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="耗时" width="72" align="right">
          <template #default="{ row }">{{ row.durationMs }}ms</template>
        </el-table-column>
        <el-table-column label="IP" width="120">
          <template #default="{ row }">{{ row.ipAddress || '—' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="72" fixed="right" align="center">
          <template #default="{ row }">
            <el-button type="info" size="small" :icon="View" @click="openDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          :page-sizes="[20, 50, 100]"
          layout="total, sizes, prev, pager, next"
          @change="fetchLogs"
        />
      </div>
    </el-card>

    <!-- 详情 Dialog -->
    <el-dialog v-model="detailVisible" title="审计日志详情" width="700px" destroy-on-close>
      <template v-if="detailRow">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="时间" :span="2">{{ formatTime(detailRow.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="用户">{{ detailRow.username || '—' }}</el-descriptions-item>
          <el-descriptions-item label="角色">{{ roleLabel(detailRow.role) }}</el-descriptions-item>
          <el-descriptions-item label="操作">
            <el-tag :type="actionTagType(detailRow.action)" size="small">{{ detailRow.action }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="状态码">
            <el-tag :type="statusTagType(detailRow.statusCode)" size="small">{{ detailRow.statusCode }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="资源类型">{{ detailRow.entityType || '—' }}</el-descriptions-item>
          <el-descriptions-item label="资源 ID">{{ detailRow.entityId || '—' }}</el-descriptions-item>
          <el-descriptions-item label="请求路径" :span="2">
            {{ detailRow.requestPath }}{{ detailRow.queryString }}
          </el-descriptions-item>
          <el-descriptions-item label="耗时">{{ detailRow.durationMs }}ms</el-descriptions-item>
          <el-descriptions-item label="IP">{{ detailRow.ipAddress || '—' }}</el-descriptions-item>
          <el-descriptions-item v-if="detailRow.errorMessage" label="错误信息" :span="2">
            <el-text type="danger">{{ detailRow.errorMessage }}</el-text>
          </el-descriptions-item>
        </el-descriptions>
      </template>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { auditLogsApi } from '@/api/auditLogs'
import { tenantsApi } from '@/api/tenants'
import { useAuthStore } from '@/stores/auth'
import { Search, Refresh, View } from '@element-plus/icons-vue'

const auth = useAuthStore()

const logs = ref([])
const total = ref(0)
const loading = ref(false)
const tenants = ref([])
const dateRange = ref(null)

const query = reactive({
  tenantId: null,
  username: '',
  action: '',
  entityType: '',
  from: null,
  to: null,
  page: 1,
  pageSize: 20,
})

watch(dateRange, (val) => {
  query.from = val?.[0] || null
  query.to = val?.[1] ? val[1] + 'T23:59:59' : null
})

// 同步顶部租户切换器
watch(() => auth.activeTenantId, (id) => {
  query.tenantId = id || null
  doSearch()
})

async function fetchLogs() {
  loading.value = true
  try {
    const params = { ...query }
    if (!params.tenantId) delete params.tenantId
    if (!params.username) delete params.username
    if (!params.action) delete params.action
    if (!params.entityType) delete params.entityType
    if (!params.from) delete params.from
    if (!params.to) delete params.to
    const res = await auditLogsApi.getList(params)
    logs.value = res.items || []
    total.value = res.totalCount || 0
  } catch {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

function doSearch() {
  query.page = 1
  fetchLogs()
}

function resetQuery() {
  Object.assign(query, {
    tenantId: null, username: '', action: '', entityType: '',
    from: null, to: null, page: 1, pageSize: 50,
  })
  dateRange.value = null
  fetchLogs()
}

// ── 详情 ──────────────────────────────────────────────
const detailVisible = ref(false)
const detailRow = ref(null)

function openDetail(row) {
  detailRow.value = row
  detailVisible.value = true
}

// ── 辅助函数 ──────────────────────────────────────────
function formatTime(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('zh-CN', { hour12: false })
}

function actionTagType(action) {
  const map = { GET: 'info', POST: 'success', PUT: 'warning', PATCH: 'warning', DELETE: 'danger' }
  return map[action] || ''
}

function statusTagType(code) {
  if (code >= 500) return 'danger'
  if (code >= 400) return 'warning'
  if (code >= 200) return 'success'
  return 'info'
}

function roleLabel(role) {
  const map = { SuperAdmin: '超级管理员', Admin: '管理员', Teacher: '教师', Student: '学生' }
  return map[role] || role || '—'
}

function roleTagType(role) {
  const map = { SuperAdmin: 'danger', Admin: 'warning', Teacher: 'primary', Student: 'info' }
  return map[role] || ''
}

onMounted(async () => {
  query.tenantId = auth.activeTenantId || null
  fetchLogs()
  try {
    const res = await tenantsApi.getList({ page: 1, pageSize: 200 })
    tenants.value = res.items || []
  } catch { /* ignore */ }
})
</script>

<style scoped>
.search-card { }
.text-muted { color: #999; font-size: 12px; }
.entity-id { font-size: 11px; color: #bbb; font-family: monospace; }
.path-text { font-family: monospace; font-size: 12px; word-break: break-all; }
.pagination-wrap {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
