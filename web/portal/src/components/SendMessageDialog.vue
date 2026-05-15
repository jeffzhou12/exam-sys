<template>
  <el-dialog
    :model-value="visible"
    title="发送站内信 · 求助教师"
    width="560px"
    @update:model-value="$emit('update:visible', $event)"
    @closed="resetForm"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="80px" size="default">
      <el-form-item label="收件教师" prop="recipientId">
        <el-select
          v-model="form.recipientId"
          placeholder="请选择教师"
          filterable
          style="width: 100%"
          :loading="teachersLoading">
          <el-option
            v-for="t in teachers"
            :key="t.id"
            :label="t.username"
            :value="t.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="主题" prop="subject">
        <el-input v-model="form.subject" placeholder="请输入主题" maxlength="100" show-word-limit />
      </el-form-item>

      <el-form-item label="内容" prop="body">
        <el-input
          v-model="form.body"
          type="textarea"
          :rows="5"
          placeholder="描述你的疑问…"
          maxlength="2000"
          show-word-limit
        />
      </el-form-item>

      <!-- 附带题目预览 -->
      <el-form-item v-if="attachedQuestions.length" label="附带题目">
        <div class="attached-list">
          <div
            v-for="q in attachedQuestions"
            :key="q.questionId || q.id"
            class="attached-item">
            <el-tag size="small" type="info">{{ typeLabel(q.type) }}</el-tag>
            <span class="attached-content">{{ truncate(q.content, 60) }}</span>
          </div>
        </div>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="$emit('update:visible', false)">取消</el-button>
      <el-button type="primary" :loading="sending" @click="send">发送</el-button>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { messagesApi } from '@/api/messages'

const props = defineProps({
  visible: { type: Boolean, default: false },
  attachedQuestions: { type: Array, default: () => [] },
  attachedExamPaperId: { type: String, default: null },
})
const emit = defineEmits(['update:visible'])

const formRef = ref(null)
const teachers = ref([])
const teachersLoading = ref(false)
const sending = ref(false)

const form = reactive({ recipientId: '', subject: '', body: '' })
const rules = {
  recipientId: [{ required: true, message: '请选择收件教师', trigger: 'change' }],
  subject: [{ required: true, message: '请输入主题', trigger: 'blur' }],
  body: [{ required: true, message: '请输入消息内容', trigger: 'blur' }],
}

const typeMap = { 1: '单选', 2: '多选', 3: '判断', 4: '简答' }
const typeLabel = (t) => typeMap[t] ?? '?'
const truncate = (s, n) => s?.length > n ? s.slice(0, n) + '…' : (s ?? '')

async function loadTeachers() {
  teachersLoading.value = true
  try {
    teachers.value = await messagesApi.getTeachers()
  } catch { teachers.value = [] } finally {
    teachersLoading.value = false
  }
}

async function send() {
  await formRef.value.validate()
  sending.value = true
  try {
    const questionIds = props.attachedQuestions.map(q => q.questionId || q.id).filter(Boolean)
    await messagesApi.send({
      recipientId: form.recipientId,
      subject: form.subject,
      body: form.body,
      attachedQuestionIds: questionIds.length ? questionIds : null,
      attachedExamPaperId: props.attachedExamPaperId || null,
    })
    ElMessage.success('消息已发送')
    emit('update:visible', false)
  } finally {
    sending.value = false
  }
}

function resetForm() {
  form.recipientId = ''
  form.subject = ''
  form.body = ''
}

onMounted(loadTeachers)
</script>

<style scoped>
.attached-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}

.attached-item {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #f8fafc;
  padding: 6px 10px;
  border-radius: 6px;
  font-size: 13px;
}

.attached-content {
  color: #475569;
  flex: 1;
}
</style>
