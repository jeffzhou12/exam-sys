<template>
  <div class="messages-page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="toolbar-title">全部消息</span>
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
        <el-table-column label="发件人" prop="senderName" width="120" />
        <el-table-column label="收件人" prop="recipientName" width="120" />
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
        <el-table-column label="状态" width="80" align="center">
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
          <div v-if="detailMsg.attachedQuestionIds?.length" style="margin-top:16px">
            <el-divider content-position="left">关联题目</el-divider>
            <div v-loading="questionsLoading" class="question-list">
              <div
                v-for="qid in detailMsg.attachedQuestionIds"
                :key="qid"
                class="question-card"
              >
                <template v-if="questionDetails[qid]">
                  <div class="q-meta">
                    <el-tag size="small" :type="qTypeTag(questionDetails[qid].questionType)">{{ qTypeLabel(questionDetails[qid].questionType) }}</el-tag>
                    <el-tag size="small" type="info" effect="plain">难度 {{ questionDetails[qid].difficulty }}</el-tag>
                    <span v-if="questionDetails[qid].knowledgePoint" class="knowledge-point">{{ questionDetails[qid].knowledgePoint }}</span>
                    <el-button
                      size="small"
                      type="primary"
                      link
                      style="margin-left:auto"
                      @click="router.push({ name: 'Questions' })"
                    >前往题库 →</el-button>
                  </div>
                  <div class="q-content">{{ questionDetails[qid].content }}</div>
                  <div v-if="questionDetails[qid].options?.length" class="q-options">
                    <div
                      v-for="(opt, idx) in questionDetails[qid].options"
                      :key="idx"
                      class="q-option">
                      <span class="q-opt-label">{{ String.fromCharCode(65 + idx) }}.</span>
                      <span>{{ opt }}</span>
                    </div>
                  </div>
                </template>
                <span v-else class="text-muted">题目加载中...</span>
              </div>
            </div>
          </div>
        </div>
        <div v-if="!detailMsg.isRead" style="margin-top:16px">
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
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { messagesApi } from '@/api/messages'
import { Plus, View, Check, Promotion, Document } from '@element-plus/icons-vue'

const auth = useAuthStore()
const router = useRouter()

const messages = ref([])
const total = ref(0)
const loading = ref(false)

const query = reactive({ page: 1, pageSize: 10 })

async function fetchMessages() {
  loading.value = true
  try {
    const res = await messagesApi.getAll({ page: query.page, pageSize: query.pageSize })
    // API 当前返回数组，兼容带 totalCount 的对象
    if (Array.isArray(res)) {
      messages.value = res
      total.value = res.length
    } else {
      messages.value = res.items || res
      total.value = res.totalCount ?? messages.value.length
    }
  } catch {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

function rowClass({ row }) {
  return !row.isRead ? 'row-unread' : ''
}

// ── 详情 ──────────────────────────────────────────────
const detailVisible = ref(false)
const detailMsg = ref(null)
const questionDetails = ref({})
const questionsLoading = ref(false)

function openDetail(row) {
  detailMsg.value = row
  detailVisible.value = true
  questionDetails.value = {}
  if (row.attachedQuestionIds?.length) {
    questionsLoading.value = true
    messagesApi.getMessageQuestions(row.id)
      .then(qs => {
        const map = {}
        qs?.forEach(q => { map[q.id] = q })
        questionDetails.value = map
      })
      .catch(() => {})
      .finally(() => { questionsLoading.value = false })
  }
}

async function markRead(msg, showMsg = true) {
  try {
    await messagesApi.markRead(msg.id)
    msg.isRead = true
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
    fetchMessages()
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

// ── 题目类型 ──────────────────────────────────────────
const Q_TYPE_LABELS = { SingleChoice: '单选题', MultipleChoice: '多选题', TrueFalse: '判断题', ShortAnswer: '简答题' }
const Q_TYPE_TAGS   = { SingleChoice: '', MultipleChoice: 'warning', TrueFalse: 'success', ShortAnswer: 'info' }
const qTypeLabel = (t) => Q_TYPE_LABELS[t] ?? t
const qTypeTag   = (t) => Q_TYPE_TAGS[t] ?? ''

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
.toolbar-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
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
.question-list { display: flex; flex-direction: column; gap: 10px; margin-top: 4px; }
.question-card {
  border: 1px solid #dbeafe;
  border-radius: 8px;
  padding: 10px 14px;
  background: #f0f7ff;
}
.q-meta { display: flex; align-items: center; gap: 6px; margin-bottom: 8px; flex-wrap: wrap; }
.knowledge-point { font-size: 12px; color: #1d4ed8; background: #dbeafe; padding: 2px 8px; border-radius: 10px; }
.q-content { font-size: 14px; color: #1e293b; line-height: 1.6; }
.q-options {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-top: 8px;
}
.q-option {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  font-size: 13px;
  color: #374151;
  line-height: 1.5;
}
.q-opt-label {
  font-weight: 600;
  color: #1d4ed8;
  flex-shrink: 0;
  min-width: 18px;
}
:deep(.row-unread td) {
  background-color: #f0f7ff !important;
}
:deep(.el-table__row) { cursor: pointer; }
</style>
