<template>
  <div class="messages-page container">
    <div class="page-header">
      <h2><el-icon><Message /></el-icon> 站内信</h2>
      <el-button type="primary" @click="composing = true">
        <el-icon><EditPen /></el-icon> 写新消息
      </el-button>
    </div>

    <el-tabs v-model="activeTab" @tab-change="fetchMessages">
      <el-tab-pane name="inbox">
        <template #label>
          <span>
            收件箱
            <el-badge v-if="unreadCount" :value="unreadCount" class="tab-badge" />
          </span>
        </template>
      </el-tab-pane>
      <el-tab-pane label="已发送" name="sent" />
    </el-tabs>

    <div v-loading="loading" class="msg-list">
      <el-empty v-if="!loading && !messages.length" description="暂无消息" />
      <div
        v-for="msg in messages"
        :key="msg.id"
        class="msg-item"
        :class="{ unread: activeTab === 'inbox' && !msg.isRead }"
        @click="openMessage(msg)">
        <div class="msg-meta">
          <span class="msg-from">
            {{ activeTab === 'inbox' ? msg.senderName : msg.recipientName }}
          </span>
          <span class="msg-date">{{ formatDate(msg.sentAt) }}</span>
        </div>
        <div class="msg-subject">{{ msg.subject }}</div>
        <div class="msg-preview">{{ truncate(msg.body, 80) }}</div>
        <el-tag v-if="activeTab === 'inbox' && !msg.isRead" size="small" type="danger" effect="dark">
          未读
        </el-tag>
      </div>
    </div>

    <!-- 消息详情 -->
    <el-dialog
      v-if="current"
      v-model="detailVisible"
      :title="current.subject"
      width="640px">
      <div class="msg-detail">
        <div class="detail-meta">
          <span>
            <strong>{{ activeTab === 'inbox' ? '发件人' : '收件人' }}：</strong>
            {{ activeTab === 'inbox' ? current.senderName : current.recipientName }}
          </span>
          <span>{{ formatDate(current.sentAt) }}</span>
        </div>
        <div class="detail-body">{{ current.body }}</div>
        <div v-if="current.attachedQuestionIds?.length" class="detail-attached">
          附带题目 ID：{{ current.attachedQuestionIds.join(', ') }}
        </div>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
        <el-button
          v-if="activeTab === 'inbox'"
          type="primary"
          @click="replyTo(current)">
          回复
        </el-button>
      </template>
    </el-dialog>

    <!-- 撰写消息 -->
    <SendMessageDialog
      v-if="composing"
      v-model:visible="composing"
      :prefill-recipient="replyRecipient"
      @sent="() => { if (activeTab === 'sent') fetchMessages() }"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Message, EditPen } from '@element-plus/icons-vue'
import { messagesApi } from '@/api/messages'
import SendMessageDialog from '@/components/SendMessageDialog.vue'

const activeTab = ref('inbox')
const messages = ref([])
const loading = ref(false)
const composing = ref(false)
const replyRecipient = ref(null)
const detailVisible = ref(false)
const current = ref(null)

const unreadCount = computed(() =>
  messages.value.filter(m => !m.isRead).length
)

const truncate = (s, n) => s?.length > n ? s.slice(0, n) + '…' : (s ?? '')
const formatDate = (iso) => {
  if (!iso) return ''
  const d = new Date(iso)
  const now = new Date()
  if (d.toDateString() === now.toDateString()) return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  return d.toLocaleDateString('zh-CN')
}

async function fetchMessages() {
  loading.value = true
  try {
    messages.value = activeTab.value === 'inbox'
      ? await messagesApi.getInbox()
      : await messagesApi.getSent()
  } catch {
    ElMessage.error('获取消息失败')
    messages.value = []
  } finally {
    loading.value = false
  }
}

async function openMessage(msg) {
  current.value = msg
  detailVisible.value = true
  if (activeTab.value === 'inbox' && !msg.isRead) {
    try {
      await messagesApi.markRead(msg.id)
      msg.isRead = true
    } catch { /* 标记已读失败静默处理 */ }
  }
}

function replyTo(msg) {
  replyRecipient.value = msg.senderId
  detailVisible.value = false
  composing.value = true
}

onMounted(fetchMessages)
</script>

<style scoped>
.messages-page {
  padding: 40px 40px 60px;
}

.page-header {
  display: flex;
  align-items: center;
  margin-bottom: 20px;
}

.page-header h2 {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 22px;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
  flex: 1;
}

.tab-badge {
  margin-left: 6px;
  vertical-align: middle;
}

.msg-list {
  display: flex;
  flex-direction: column;
  gap: 1px;
  background: #e2e8f0;
  border-radius: 10px;
  overflow: hidden;
  margin-top: 4px;
  min-height: 120px;
}

.msg-item {
  background: #fff;
  padding: 14px 20px;
  cursor: pointer;
  transition: background .15s;
  position: relative;
}

.msg-item:hover { background: #f8fafc; }

.msg-item.unread { background: #eff6ff; }
.msg-item.unread .msg-subject { font-weight: 700; }

.msg-meta {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
}

.msg-from { font-size: 13px; color: #374151; font-weight: 500; }
.msg-date { font-size: 12px; color: #94a3b8; }
.msg-subject { font-size: 14px; color: #1e293b; margin-bottom: 2px; }
.msg-preview { font-size: 13px; color: #6b7280; }

.el-tag {
  position: absolute;
  right: 16px;
  top: 50%;
  transform: translateY(-50%);
}

.msg-detail { display: flex; flex-direction: column; gap: 14px; }
.detail-meta {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  color: #6b7280;
}
.detail-body { font-size: 14px; color: #1e293b; line-height: 1.8; white-space: pre-wrap; }
.detail-attached { font-size: 13px; color: #6366f1; background: #ede9fe; padding: 6px 10px; border-radius: 6px; }
</style>
