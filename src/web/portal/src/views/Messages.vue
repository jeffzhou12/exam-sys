<template>
  <div class="messages-page container">
    <div class="page-header">
      <h2>
          <el-icon><Message /></el-icon> 站内信
          <el-badge v-if="unreadCount" :value="unreadCount" class="header-badge" />
        </h2>
      <el-button type="primary" @click="composing = true">
        <el-icon><EditPen /></el-icon> 写新消息
      </el-button>
    </div>

    <div v-loading="loading" class="msg-list">
      <el-empty v-if="!loading && !messages.length" description="暂无消息" />
      <div
        v-for="msg in messages"
        :key="msg.id"
        class="msg-item"
        :class="{ unread: msg._direction === 'received' && !msg.isRead }"
        @click="openThread(msg)">
        <div class="msg-meta">
          <span class="msg-from">
            <el-tag
              size="small"
              :type="msg._direction === 'received' ? '' : 'info'"
              effect="plain"
              class="dir-tag">
              {{ msg._direction === 'received' ? '来自' : '发给' }}
            </el-tag>
            {{ msg._direction === 'received' ? msg.senderName : msg.recipientName }}
          </span>
          <span class="msg-date">{{ formatDate(msg.latestReplyAt || msg.createdAt) }}</span>
        </div>
        <div class="msg-subject">{{ msg.subject }}</div>
        <div class="msg-preview">{{ truncate(msg.body, 80) }}</div>
        <div class="msg-badges">
          <el-tag v-if="msg._direction === 'received' && !msg.isRead" size="small" type="danger" effect="dark">
            未读
          </el-tag>
          <el-tag v-if="msg.replyCount > 0" size="small" type="info" effect="plain">
            {{ msg.replyCount }} 条回复
          </el-tag>
        </div>
      </div>
    </div>

    <!-- 对话线程弹窗 -->
    <el-dialog
      v-if="currentThread"
      v-model="threadVisible"
      :title="currentThread.subject"
      width="660px"
      :close-on-click-modal="false"
      class="thread-dialog"
      @closed="onThreadClose">
      <div class="thread-body" ref="threadBodyRef">
        <div v-loading="threadLoading" class="bubble-list">
          <template v-for="item in threadMessages" :key="item.id">
            <div class="bubble-row" :class="item.senderId === myUserId ? 'me' : 'other'">
              <div class="bubble-wrap">
                <div class="bubble-sender">{{ item.senderName }}</div>
                <div class="bubble">
                  <div class="bubble-body">{{ item.body }}</div>
                  <!-- 关联题目 -->
                  <div
                    v-if="item.attachedQuestionIds?.length"
                    class="bubble-questions">
                    <div class="bq-header">
                      <el-icon><Files /></el-icon> 关联题目 ({{ item.attachedQuestionIds.length }})
                    </div>
                    <div v-if="questionMap[item.id]">
                      <div
                        v-for="q in questionMap[item.id]"
                        :key="q.id"
                        class="bq-card">
                        <div class="bq-meta">
                          <el-tag size="small" :type="qTypeTag(q.questionType)">{{ qTypeLabel(q.questionType) }}</el-tag>
                          <el-tag size="small" type="info" effect="plain">难度 {{ q.difficulty }}</el-tag>
                          <span v-if="q.knowledgePoint" class="knowledge-point">{{ q.knowledgePoint }}</span>
                        </div>
                        <div class="bq-content">{{ q.content }}</div>
                        <div v-if="q.options?.length" class="bq-options">
                          <div
                            v-for="(opt, idx) in q.options"
                            :key="idx"
                            class="bq-option">
                            <span class="bq-opt-label">{{ String.fromCharCode(65 + idx) }}</span>
                            <span>{{ opt }}</span>
                          </div>
                        </div>
                        <div v-if="q.answer" class="bq-answer">
                          <strong>参考答案：</strong>{{ q.answer }}
                        </div>
                        <div v-if="q.explanation" class="bq-explanation">
                          <strong>解析：</strong>{{ q.explanation }}
                        </div>
                      </div>
                    </div>
                    <div v-else class="bq-loading">
                      <el-button link size="small" @click="loadQuestions(item)">点击加载题目</el-button>
                    </div>
                  </div>
                </div>
                <div class="bubble-time">{{ formatDate(item.createdAt) }}</div>
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- 回复输入区 -->
      <div class="reply-area">
        <el-input
          v-model="replyText"
          type="textarea"
          :rows="3"
          placeholder="输入回复内容…"
          :disabled="replying"
          resize="none"
          @keydown.ctrl.enter="sendReply" />
        <div class="reply-footer">
          <span class="reply-hint">Ctrl+Enter 发送</span>
          <el-button
            type="primary"
            :loading="replying"
            :disabled="!replyText.trim()"
            @click="sendReply">
            发送回复
          </el-button>
        </div>
      </div>
    </el-dialog>

    <!-- 撰写新消息 -->
    <SendMessageDialog
      v-if="composing"
      v-model:visible="composing"
      @sent="onNewMessageSent"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { Message, EditPen, Files } from '@element-plus/icons-vue'
import { messagesApi } from '@/api/messages'
import { useAuthStore } from '@/stores/auth'
import SendMessageDialog from '@/components/SendMessageDialog.vue'

const authStore = useAuthStore()
const myUserId = computed(() => authStore.user?.id)

const messages = ref([])
const loading = ref(false)
const composing = ref(false)

// 对话线程状态
const threadVisible = ref(false)
const currentThread = ref(null)   // 当前打开的根消息
const threadMessages = ref([])    // 线程中所有消息（含根消息）
const threadLoading = ref(false)
const questionMap = ref({})       // { messageId: [ ...questions ] }
const threadBodyRef = ref(null)

// 回复状态
const replyText = ref('')
const replying = ref(false)

const unreadCount = computed(() =>
  messages.value.filter(m => m._direction === 'received' && !m.isRead).length
)

const truncate = (s, n) => s?.length > n ? s.slice(0, n) + '…' : (s ?? '')

const formatDate = (iso) => {
  if (!iso) return ''
  const d = new Date(iso)
  const now = new Date()
  if (d.toDateString() === now.toDateString())
    return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  return d.toLocaleDateString('zh-CN')
}

async function fetchMessages() {
  loading.value = true
  try {
    const [inbox, sent] = await Promise.all([
      messagesApi.getInbox().catch(() => []),
      messagesApi.getSent().catch(() => []),
    ])
    // 合并并去重（按 id），inbox 优先（保留 isRead 状态）
    const map = new Map()
    inbox.forEach(m => map.set(m.id, { ...m, _direction: 'received' }))
    sent.forEach(m => { if (!map.has(m.id)) map.set(m.id, { ...m, _direction: 'sent' }) })
    // 按最新活动时间倒序排列
    messages.value = [...map.values()].sort((a, b) =>
      new Date(b.latestReplyAt || b.createdAt) - new Date(a.latestReplyAt || a.createdAt)
    )
  } catch {
    ElMessage.error('获取消息失败')
    messages.value = []
  } finally {
    loading.value = false
  }
}

async function openThread(msg) {
  currentThread.value = msg
  threadVisible.value = true
  threadMessages.value = []
  questionMap.value = {}
  replyText.value = ''
  threadLoading.value = true
  try {
    threadMessages.value = await messagesApi.getThread(msg.id)
    // 更新列表中该消息的已读状态
    msg.isRead = true
    // 自动加载每条消息的关联题目
    await Promise.all(
      threadMessages.value
        .filter(m => m.attachedQuestionIds?.length)
        .map(m => loadQuestions(m))
    )
  } catch {
    ElMessage.error('加载对话失败')
    threadVisible.value = false
  } finally {
    threadLoading.value = false
    scrollToBottom()
  }
}

async function loadQuestions(item) {
  if (questionMap.value[item.id] !== undefined) return
  try {
    const qs = await messagesApi.getMessageQuestions(item.id)
    questionMap.value = { ...questionMap.value, [item.id]: qs }
  } catch {
    questionMap.value = { ...questionMap.value, [item.id]: [] }
  }
}

async function sendReply() {
  if (!replyText.value.trim() || replying.value) return
  replying.value = true
  try {
    const root = currentThread.value
    // 回复时 recipientId 是对话的另一方
    const recipientId = root.senderId === myUserId.value
      ? root.recipientId   // 我是发起者 → 回复给接收者（教师）
      : root.senderId      // 我是接收者 → 回复给发起者
    // 从 threadMessages 中获取另一方的 userId
    const otherMsg = threadMessages.value.find(m => m.senderId !== myUserId.value)
    const realRecipientId = otherMsg?.senderId ?? recipientId

    await messagesApi.send({
      recipientId: realRecipientId,
      subject: root.subject,
      body: replyText.value.trim(),
      parentMessageId: root.id,
    })
    replyText.value = ''
    // 刷新线程
    threadMessages.value = await messagesApi.getThread(root.id)
    await nextTick()
    scrollToBottom()
    // 刷新列表（更新回复计数 / 置顶）
    fetchMessages()
  } catch (e) {
    ElMessage.error(e?.response?.data?.error || '发送失败')
  } finally {
    replying.value = false
  }
}

function scrollToBottom() {
  nextTick(() => {
    if (threadBodyRef.value) {
      threadBodyRef.value.scrollTop = threadBodyRef.value.scrollHeight
    }
  })
}

function onThreadClose() {
  currentThread.value = null
  threadMessages.value = []
  questionMap.value = {}
  replyText.value = ''
}

function onNewMessageSent() {
  fetchMessages()
}

const Q_TYPE_LABELS = { SingleChoice: '单选题', MultipleChoice: '多选题', TrueFalse: '判断题', ShortAnswer: '简答题' }
const Q_TYPE_TAGS   = { SingleChoice: '', MultipleChoice: 'warning', TrueFalse: 'success', ShortAnswer: 'info' }
const qTypeLabel = (t) => Q_TYPE_LABELS[t] ?? t
const qTypeTag   = (t) => Q_TYPE_TAGS[t] ?? ''

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

.header-badge { margin-left: 4px; }

.dir-tag { margin-right: 4px; flex-shrink: 0; }

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
.msg-badges { display: flex; gap: 6px; margin-top: 6px; }

/* ── 对话线程弹窗 ── */
.thread-body {
  max-height: 420px;
  overflow-y: auto;
  padding: 0 2px 4px;
}

.bubble-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 8px 0;
  min-height: 80px;
}

.bubble-row {
  display: flex;
}
.bubble-row.me { justify-content: flex-end; }
.bubble-row.other { justify-content: flex-start; }

.bubble-wrap {
  max-width: 75%;
  display: flex;
  flex-direction: column;
}
.bubble-row.me .bubble-wrap { align-items: flex-end; }
.bubble-row.other .bubble-wrap { align-items: flex-start; }

.bubble-sender {
  font-size: 12px;
  color: #94a3b8;
  margin-bottom: 4px;
}

.bubble {
  padding: 10px 14px;
  border-radius: 14px;
  max-width: 100%;
}
.bubble-row.me .bubble {
  background: #3b82f6;
  color: #fff;
  border-bottom-right-radius: 4px;
}
.bubble-row.other .bubble {
  background: #f1f5f9;
  color: #1e293b;
  border-bottom-left-radius: 4px;
}

.bubble-body {
  font-size: 14px;
  line-height: 1.7;
  white-space: pre-wrap;
  word-break: break-word;
}

.bubble-time {
  font-size: 11px;
  color: #94a3b8;
  margin-top: 4px;
}

/* ── 关联题目 ── */
.bubble-questions {
  margin-top: 10px;
  border-top: 1px solid rgba(255,255,255,.3);
  padding-top: 8px;
}
.bubble-row.other .bubble-questions {
  border-top-color: #e2e8f0;
}

.bq-header {
  font-size: 12px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 6px;
  opacity: .85;
}

.bq-card {
  background: rgba(255,255,255,.18);
  border-radius: 8px;
  padding: 8px 10px;
  margin-bottom: 6px;
}
.bubble-row.other .bq-card {
  background: #fff;
  border: 1px solid #e0e7ff;
}

.bq-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 4px;
  flex-wrap: wrap;
}
.knowledge-point {
  font-size: 11px;
  background: rgba(255,255,255,.3);
  padding: 1px 6px;
  border-radius: 8px;
}
.bubble-row.other .knowledge-point {
  background: #ede9fe;
  color: #7c3aed;
}

.bq-content { font-size: 13px; line-height: 1.6; }
.bq-options {
  display: flex;
  flex-direction: column;
  gap: 3px;
  margin-top: 6px;
}
.bq-option {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  font-size: 12px;
  line-height: 1.5;
}
.bq-opt-label {
  font-weight: 700;
  flex-shrink: 0;
  min-width: 14px;
}
.bq-answer, .bq-explanation {
  font-size: 12px;
  margin-top: 4px;
  opacity: .85;
}
.bq-loading { font-size: 12px; opacity: .7; }

/* ── 回复区 ── */
.reply-area {
  border-top: 1px solid #e2e8f0;
  padding: 12px 0 0;
  margin-top: 8px;
}
.reply-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 8px;
}
.reply-hint { font-size: 12px; color: #94a3b8; }
</style>
