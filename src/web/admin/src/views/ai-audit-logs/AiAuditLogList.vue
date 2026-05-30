<template>
  <div class="ai-audit-log-page">
    <!-- 筛选栏 -->
    <el-card shadow="never" class="search-card">
      <el-row :gutter="12" align="middle">
        <el-col :span="5">
          <el-select
            v-model="query.tenantId"
            placeholder="租户（全部）"
            clearable
            style="width: 100%"
          >
            <el-option v-for="t in tenants" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-input
            v-model="query.operation"
            placeholder="操作类型"
            clearable
            :prefix-icon="Search"
            @keyup.enter="doSearch"
          />
        </el-col>
        <el-col :span="3">
          <el-select v-model="query.isSuccess" placeholder="结果" clearable style="width: 100%">
            <el-option label="成功" :value="true" />
            <el-option label="失败" :value="false" />
          </el-select>
        </el-col>
        <el-col :span="6">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="~"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-col>
        <el-col :span="4">
          <el-button type="primary" :icon="Search" @click="doSearch">查询</el-button>
          <el-button :icon="Refresh" @click="resetQuery">重置</el-button>
        </el-col>
      </el-row>
    </el-card>

    <!-- 统计卡片 -->
    <el-row :gutter="12" style="margin: 16px 0">
      <el-col :span="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-label">总调用次数</div>
          <div class="stat-value">{{ total }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card">
          <div class="stat-label">当页 Token 合计</div>
          <div class="stat-value">{{ pageTokenSum.toLocaleString() }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card success">
          <div class="stat-label">当页成功次数</div>
          <div class="stat-value">{{ pageSuccessCount }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never" class="stat-card danger">
          <div class="stat-label">当页失败次数</div>
          <div class="stat-value">{{ pageFailCount }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 表格 -->
    <el-card shadow="never">
      <el-table v-loading="loading" :data="logs" stripe size="small">
        <el-table-column label="时间" width="156" fixed="left">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" show-overflow-tooltip>
          <template #default="{ row }">
            <el-tag size="small" type="primary">{{ row.operation }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="modelName" label="模型" width="180" show-overflow-tooltip />
        <el-table-column label="Token 用量" width="200">
          <template #default="{ row }">
            <span class="token-detail">
              提示:{{ row.promptTokens }} / 输出:{{ row.completionTokens }} /
              <strong>共:{{ row.totalTokens }}</strong>
            </span>
          </template>
        </el-table-column>
        <el-table-column label="结果" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isSuccess ? 'success' : 'danger'" size="small">
              {{ row.isSuccess ? '成功' : '失败' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="错误信息" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <el-text v-if="row.errorMessage" type="danger" size="small">{{ row.errorMessage }}</el-text>
            <span v-else class="text-muted">—</span>
          </template>
        </el-table-column>
        <el-table-column label="关联实体" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.relatedEntityId" class="text-muted">{{ row.relatedEntityId.slice(0, 8) }}…</span>
            <span v-else class="text-muted">—</span>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div style="display: flex; justify-content: flex-end; margin-top: 16px">
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
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { aiAuditLogsApi } from '@/api/aiAuditLogs'
import { tenantsApi } from '@/api/tenants'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const logs = ref([])
const total = ref(0)
const loading = ref(false)
const tenants = ref([])
const dateRange = ref(null)

const query = reactive({
  tenantId: auth.activeTenantId || null,
  operation: '',
  isSuccess: null,
  from: null,
  to: null,
  page: 1,
  pageSize: 20
})

const pageTokenSum = computed(() => logs.value.reduce((s, l) => s + l.totalTokens, 0))
const pageSuccessCount = computed(() => logs.value.filter(l => l.isSuccess).length)
const pageFailCount = computed(() => logs.value.filter(l => !l.isSuccess).length)

watch(dateRange, (val) => {
  query.from = val?.[0] || null
  query.to = val?.[1] ? val[1] + 'T23:59:59' : null
})

watch(() => auth.activeTenantId, (id) => {
  query.tenantId = id || null
  doSearch()
})

onMounted(async () => {
  try {
    const res = await tenantsApi.getList({ pageSize: 200 })
    tenants.value = res.items || []
  } catch { /* ignore */ }
  fetchLogs()
})

async function fetchLogs() {
  loading.value = true
  try {
    const params = { ...query }
    if (!params.tenantId) delete params.tenantId
    if (!params.operation) delete params.operation
    if (params.isSuccess === null) delete params.isSuccess
    if (!params.from) delete params.from
    if (!params.to) delete params.to
    const res = await aiAuditLogsApi.getList(params)
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
  query.tenantId = auth.activeTenantId || null
  query.operation = ''
  query.isSuccess = null
  query.from = null
  query.to = null
  dateRange.value = null
  doSearch()
}

function formatTime(val) {
  if (!val) return '—'
  return new Date(val).toLocaleString('zh-CN', { hour12: false })
}
</script>

<style scoped>
.ai-audit-log-page { padding: 20px; }
.search-card { margin-bottom: 0; }
.stat-card { text-align: center; }
.stat-card.success .stat-value { color: #67c23a; }
.stat-card.danger .stat-value { color: #f56c6c; }
.stat-label { font-size: 13px; color: #909399; }
.stat-value { font-size: 24px; font-weight: 600; margin-top: 4px; }
.token-detail { font-size: 12px; color: #606266; }
.text-muted { color: #909399; font-size: 13px; }
</style>
