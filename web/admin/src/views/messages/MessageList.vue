<template>
  <div class="messages-page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-radio-group v-model="activeTab" @change="handleTabChange">
          <el-radio-button value="inbox">
            收件箱
            <el-badge v-if="unreadCount > 0" :value="unreadCount" :max="99" class="tab-badge" />
          </el-radio-button>
          <el-radio-button value="sent">已发送</el-radio-button>
        </el-radio-group>
        <el-button type="primary" :icon="Plus" @click="openCompose">发送消息</el-button>
      </div>

      <el-table
        v-loading="loading"
        :data="messages"
        stripe
        style="margin-top:16px"
        @row-click="openDetail"
        :row-class-name="rowClass"
      >
        <el-table-column v-if="activeTab === 'inbox'" label="发件人" prop="senderName" width="120" />
        <el-table-column v-else label="收件人" prop="recipientName" width="120" />
        <el-table-column label="主题" min-width="200">
          <template #default="{ row }">
            <span :class="{ 'unread-subject': activeTab === 'inbox' && !row.isRead }">
              {{ row.subject }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="内容摘要" min-width="260">
          <template #default="{ row }">
            <span class="body-preview text-muted">{{ row.body }}</span>
          </template>
        </el-table-column>
        <el-table-column v-if="activeTab === 'inbox'" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isRead ? 'info' : 'primary'" size="small">
              {{ row.isRead ? '已读' : '未读' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="时间" width="160" align="center">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="80" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="info" size="small" :icon="View" @click.stop="openDetail(row)">查看</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @change="fetchMessages"
        />
      </div>
    </el-card>

    <!-- 消息详情 Drawer -->
    <el-drawer
      v-model="detailVisible"
      :title="detailMsg?.subject || '消息详情'"
      size="520px"
      destroy-on-close
    >
      <template v-if="detailMsg">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="发件人">{{ detailMsg.senderName }}</el-descriptions-item>
          <el-descriptions-item label="收件人">{{ detailMsg.recipientName }}</el-descriptions-item>
          <el-descriptions-item label="时间" :span="2">{{ formatTime(detailMsg.createdAt) }}</el-descriptions-item>
        </el-descriptions>
        <div class="message-body">{{ detailMsg.body }}</div>
        <div v-if="detailMsg.attachedExamPaperId || detailMsg.attachedQuestionIds?.length" class="attachments">
          <el-divider content-position="left">附件</el-divider>
          <div v-if="detailMsg.attachedExamPaperId">
            <el-tag type="primary" size="small" :icon="Document">试卷 {{ detailMsg.attachedExamPaperId }}</el-tag>
          </div>
          <div v-if="detailMsg.attachedQuestionIds?.length" style="margin-top:6px">
            <el-tag
              v-for="qid in detailMsg.attachedQuestionIds"
              :key="qid"
              size="small"
              type="success"
              style="margin:2px"
            >题目 {{ qid }}</el-tag>
          </div>
        </div>
        <div v-if="activeTab === 'inbox' && !detailMsg.isRead" style="margin-top:16px">
          <el-button type="primary" :icon="Check" @click="markRead(detailMsg)">标为已读</el-button>
        </div>
      </template>
    </el-drawer>

    <!-- 发送消息 Dialog -->
    <el-dialog v-model="composeVisible" title="发送消息" width="560px" destroy-on-close>
      <el-form ref="composeRef" :model="composeForm" :rules="composeRules" label-width="80px">
        <!-- SuperAdmin 额外显示租户选择器，选择后动态加载教师列表 -->
        <el-form-item v-if="auth.isSuperAdmin" label="租户" prop="tenantId">
          <el-select
            v-model="composeForm.tenantId"
            placeholder="选择目标租户"
            style="width:100%"
            filterable
            @change="onComposeTenantChange"
          >
            <el-option v-for="t in allTenants" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="收件人" prop="recipientId">
          <el-select
            v-model="composeForm.recipientId"
            placeholder="选择收件教师"
            style="width:100%"
            filterable
            :loading="teachersLoading"
            :disabled="auth.isSuperAdmin && !composeForm.tenantId"
          >
            <el-option
              v-for="t in teachers"
              :key="t.id"
              :label="t.username"
              :value="t.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="主题" prop="subject">
          <el-input v-model="composeForm.subject" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item label="正文" prop="body">
          <el-input
            v-model="composeForm.body"
            type="textarea"
            :rows="6"
            maxlength="2000"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="composeVisible = false">取消</el-button>
        <el-button type="primary" :loading="sending" :icon="Promotion" @click="submitCompose">发送</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onBeforeUnmount } from 'vue'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { messagesApi } from '@/api/messages'
import { Plus, View, Check, Promotion, Document } from '@element-plus/icons-vue'

const auth = useAuthStore()

const activeTab = ref('inbox')
const messages = ref([])
const total = ref(0)
const loading = ref(false)
const unreadCount = ref(0)

const query = reactive({ page: 1, pageSize: 10 })

async function fetchMessages() {
  loading.value = true
  try {
    const fn = activeTab.value === 'inbox' ? messagesApi.getInbox : messagesApi.getSent
    const res = await fn({ page: query.page, pageSize: query.pageSize })
    // API 当前返回数组，兼容带 totalCount 的对象
    if (Array.isArray(res)) {
      messages.value = res
      total.value = res.length
    } else {
      messages.value = res.items || res
      total.value = res.totalCount ?? messages.value.length
    }
    if (activeTab.value === 'inbox') {
      unreadCount.value = messages.value.filter(m => !m.isRead).length
    }
  } catch {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

function handleTabChange() {
  query.page = 1
  fetchMessages()
}

function rowClass({ row }) {
  return activeTab.value === 'inbox' && !row.isRead ? 'row-unread' : ''
}

// ── 详情 ──────────────────────────────────────────────
const detailVisible = ref(false)
const detailMsg = ref(null)

function openDetail(row) {
  detailMsg.value = row
  detailVisible.value = true
  if (activeTab.value === 'inbox' && !row.isRead) {
    markRead(row, false)
  }
}

async function markRead(msg, showMsg = true) {
  try {
    await messagesApi.markRead(msg.id)
    msg.isRead = true
    unreadCount.value = Math.max(0, unreadCount.value - 1)
    if (showMsg) ElMessage.success('已标为已读')
  } catch {
    if (showMsg) ElMessage.error('操作失败')
  }
}

// ── 发送消息 ──────────────────────────────────────────
const composeVisible = ref(false)
const composeRef = ref(null)
const sending = ref(false)
const teachers = ref([])
const teachersLoading = ref(false)
const allTenants = ref([])

function syncTenantsFromCache() {
  try {
    const raw = localStorage.getItem('admin.tenants.cache')
    if (!raw) return
    const parsed = JSON.parse(raw)
    allTenants.value = Array.isArray(parsed) ? parsed : []
  } catch {
    allTenants.value = []
  }
}

function onTenantsUpdated(event) {
  const list = event?.detail
  allTenants.value = Array.isArray(list) ? list : []
}

const defaultCompose = () => ({ tenantId: null, recipientId: null, subject: '', body: '' })
const composeForm = reactive(defaultCompose())
const composeRules = {
  tenantId: auth.isSuperAdmin
    ? [{ required: true, message: '请选择租户', trigger: 'change' }]
    : [],
  recipientId: [{ required: true, message: '请选择收件人', trigger: 'change' }],
  subject: [{ required: true, message: '请输入主题', trigger: 'blur' }],
  body: [{ required: true, message: '请输入正文', trigger: 'blur' }],
}

async function loadTeachers(tenantId) {
  teachersLoading.value = true
  teachers.value = []
  try {
    const params = tenantId ? { tenantId } : undefined
    teachers.value = await messagesApi.getTeachers(params)
  } catch {
    ElMessage.warning('获取教师列表失败')
  } finally {
    teachersLoading.value = false
  }
}

async function onComposeTenantChange(id) {
  composeForm.recipientId = null
  if (id) await loadTeachers(id)
}

async function openCompose() {
  Object.assign(composeForm, defaultCompose())
  teachers.value = []
  composeVisible.value = true
  // 普通管理员：直接加载本租户教师列表
  if (!auth.isSuperAdmin) {
    await loadTeachers()
  }
}

async function submitCompose() {
  await composeRef.value?.validate()
  sending.value = true
  try {
    const payload = {
      recipientId: composeForm.recipientId,
      subject: composeForm.subject,
      body: composeForm.body,
    }
    // SuperAdmin 需将选择的租户传入请求体
    if (auth.isSuperAdmin) payload.tenantId = composeForm.tenantId
    await messagesApi.send(payload)
    ElMessage.success('发送成功')
    composeVisible.value = false
    if (activeTab.value === 'sent') fetchMessages()
  } catch (e) {
    ElMessage.error(e?.message || '发送失败')
  } finally {
    sending.value = false
  }
}

// ── 时间格式化 ──────────────────────────────────────────
function formatTime(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  return d.toLocaleString('zh-CN', { hour12: false })
}

onMounted(async () => {
  fetchMessages()
  if (auth.isSuperAdmin) syncTenantsFromCache()
  window.addEventListener('admin-tenants-updated', onTenantsUpdated)
})

onBeforeUnmount(() => {
  window.removeEventListener('admin-tenants-updated', onTenantsUpdated)
})
</script>

<style scoped>
.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.tab-badge {
  margin-left: 4px;
  vertical-align: middle;
}
.unread-subject {
  font-weight: 600;
}
.body-preview {
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
  color: #999;
  font-size: 13px;
}
.text-muted { color: #999; }
.pagination-wrap {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
.message-body {
  margin-top: 16px;
  padding: 16px;
  background: #f9f9f9;
  border-radius: 6px;
  white-space: pre-wrap;
  line-height: 1.7;
  min-height: 80px;
}
.attachments { margin-top: 16px; }
:deep(.row-unread td) {
  background-color: #f0f7ff !important;
}
:deep(.el-table__row) { cursor: pointer; }
</style>
