<template>
  <div class="page-container">
    <div class="page-header">
      <div class="breadcrumb-back">
        <el-button :icon="ArrowLeft" text @click="$router.push('/exam-papers')">返回列表</el-button>
        <h3>{{ isEdit ? '编辑试卷' : '新建试卷' }}</h3>
      </div>
    </div>

    <el-form
      ref="formRef"
      v-loading="pageLoading"
      :model="form"
      :rules="rules"
      label-width="110px"
    >
      <el-card shadow="never" class="form-card">
        <template #header><span>基本信息</span></template>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="试卷标题" prop="title">
              <el-input v-model="form.title" placeholder="请输入试卷标题" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="总分" prop="totalScore">
              <el-input-number v-model="form.totalScore" :min="1" :max="1000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="考试时长(分)" prop="durationMinutes">
              <el-input-number v-model="form.durationMinutes" :min="1" :max="480" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="开始时间">
              <el-date-picker
                v-model="form.startTime"
                type="datetime"
                placeholder="选择开始时间"
                style="width: 100%"
                value-format="YYYY-MM-DDTHH:mm:ss"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束时间">
              <el-date-picker
                v-model="form.endTime"
                type="datetime"
                placeholder="选择结束时间"
                style="width: 100%"
                value-format="YYYY-MM-DDTHH:mm:ss"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="试卷描述">
              <el-input
                v-model="form.description"
                type="textarea"
                :rows="3"
                placeholder="请输入试卷描述（选填）"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row>
          <el-col :span="12">
            <el-form-item label="防作弊">
              <el-switch v-model="form.antiCheatingEnabled" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-card>

      <!-- 题目列表 -->
      <el-card shadow="never" class="form-card">
        <template #header>
          <div style="display: flex; justify-content: space-between; align-items: center">
            <span>题目列表（已选 {{ form.questions.length }} 题，总分 {{ computedScore }} 分）</span>
            <div style="display:flex;gap:8px">
              <el-button type="success" size="small" :icon="MagicStick" @click="openAiWizard">AI 出题</el-button>
              <el-button type="primary" size="small" :icon="Plus" @click="showQuestionPicker = true">
                手动添加
              </el-button>
            </div>
          </div>
        </template>

        <el-table :data="form.questions" stripe>
          <el-table-column label="序号" width="60">
            <template #default="{ $index }">{{ $index + 1 }}</template>
          </el-table-column>
          <el-table-column prop="content" label="题目内容" min-width="300">
            <template #default="{ row }">
              <span class="question-content">{{ row.content }}</span>
            </template>
          </el-table-column>
          <el-table-column label="类型" width="100">
            <template #default="{ row }">{{ typeLabel(row.type) }}</template>
          </el-table-column>
          <el-table-column label="分值" width="120">
            <template #default="{ row }">
              <el-input-number
                v-model="row.score"
                :min="1"
                :max="100"
                size="small"
                style="width: 100px"
              />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80">
            <template #default="{ $index }">
              <el-button size="small" type="danger" text @click="removeQuestion($index)">
                <el-icon><Delete /></el-icon>
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <div class="form-actions">
        <el-button @click="$router.push('/exam-papers')">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">
          {{ isEdit ? '保存修改' : '创建试卷' }}
        </el-button>
      </div>
    </el-form>

    <!-- 题目选择器对话框 -->
    <el-dialog v-model="showQuestionPicker" title="选择题目" width="800px">
      <div class="picker-filter">
        <el-form inline>
          <el-form-item label="类型">
            <el-select v-model="pickerQuery.type" placeholder="全部" clearable style="width: 120px">
              <el-option label="单选题" :value="1" />
              <el-option label="多选题" :value="2" />
              <el-option label="判断题" :value="3" />
              <el-option label="简答题" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="知识点">
            <el-input v-model="pickerQuery.knowledgePoint" clearable placeholder="输入知识点" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="loadPickerQuestions">查询</el-button>
          </el-form-item>
        </el-form>
      </div>
      <el-table
        v-loading="pickerLoading"
        :data="pickerQuestions"
        @selection-change="selectedQuestions = $event"
      >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="content" label="题目内容" min-width="280">
          <template #default="{ row }">
            <span class="question-content">{{ row.content }}</span>
          </template>
        </el-table-column>
        <el-table-column label="类型" width="90">
          <template #default="{ row }">{{ typeLabel(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="difficulty" label="难度" width="80" />
        <el-table-column prop="knowledgePoint" label="知识点" width="120" />
      </el-table>
      <el-pagination
        v-model:current-page="pickerQuery.page"
        v-model:page-size="pickerQuery.pageSize"
        :total="pickerTotal"
        layout="prev, pager, next"
        small
        class="pagination"
        @change="loadPickerQuestions"
      />
      <template #footer>
        <el-button @click="showQuestionPicker = false">取消</el-button>
        <el-button type="primary" @click="addSelectedQuestions">添加选中（{{ selectedQuestions.length }}）</el-button>
      </template>
    </el-dialog>

    <!-- AI 出题向导 -->
    <el-dialog
      v-model="showAiWizard"
      :title="aiStep === 1 ? 'AI 出题配置' : `AI 出题审核（共 ${aiReviewItems.length} 题）`"
      width="860px"
      :close-on-click-modal="false"
      @closed="resetAiWizard"
    >
      <!-- Step 1: 配置 -->
      <div v-if="aiStep === 1">
        <el-form :model="aiConfig" label-width="110px">
          <el-form-item label="知识点" required>
            <el-input v-model="aiConfig.knowledgePoint" placeholder="如：高中数学·二次函数" clearable />
          </el-form-item>

          <el-form-item label="AI生成配置">
            <div class="type-config-table">
              <div class="type-config-table-header">
                <span>题型</span><span>难度</span><span>数量</span>
              </div>
              <div v-for="(row, idx) in aiConfig.typeConfigs" :key="idx" class="type-config-row">
                <el-select v-model="row.type" size="small" style="width:96px">
                  <el-option label="单选题" :value="1" />
                  <el-option label="多选题" :value="2" />
                  <el-option label="判断题" :value="3" />
                  <el-option label="简答题" :value="4" />
                </el-select>
                <el-select v-model="row.difficulty" size="small" style="width:90px">
                  <el-option label="简单" :value="1" />
                  <el-option label="较易" :value="2" />
                  <el-option label="中等" :value="3" />
                  <el-option label="较难" :value="4" />
                  <el-option label="困难" :value="5" />
                </el-select>
                <el-input-number v-model="row.count" :min="1" :max="20" size="small" style="width:88px" />
                <span class="type-count-label">道</span>
                <el-button type="danger" :icon="Delete" circle size="small" text
                  @click="aiConfig.typeConfigs.splice(idx,1)"
                  :disabled="aiConfig.typeConfigs.length <= 1" />
              </div>
              <el-button size="small" :icon="Plus" plain style="margin-top:6px"
                @click="aiConfig.typeConfigs.push({type:1,difficulty:3,count:5})">添加一行</el-button>
            </div>
          </el-form-item>

          <el-divider content-position="left">从题库随机抽取（可选）</el-divider>

          <el-form-item label="启用抽题">
            <el-switch v-model="aiConfig.pickFromBank" />
          </el-form-item>

          <template v-if="aiConfig.pickFromBank">
            <el-form-item label="题库知识点">
              <el-input v-model="aiConfig.bankKnowledgePoint" placeholder="留空则不过滤" clearable style="width:250px" />
            </el-form-item>
            <el-form-item label="题型及数量">
              <div class="type-config-table">
                <div class="type-config-table-header">
                  <span>题型</span><span>难度</span><span>数量</span>
                </div>
                <div v-for="(row, idx) in aiConfig.bankTypeConfigs" :key="idx" class="type-config-row">
                  <el-select v-model="row.type" size="small" style="width:96px">
                    <el-option label="单选题" :value="1" />
                    <el-option label="多选题" :value="2" />
                    <el-option label="判断题" :value="3" />
                    <el-option label="简答题" :value="4" />
                  </el-select>
                  <el-select v-model="row.difficulty" placeholder="不限" clearable size="small" style="width:90px">
                    <el-option label="简单" :value="1" />
                    <el-option label="较易" :value="2" />
                    <el-option label="中等" :value="3" />
                    <el-option label="较难" :value="4" />
                    <el-option label="困难" :value="5" />
                  </el-select>
                  <el-input-number v-model="row.count" :min="1" :max="30" size="small" style="width:88px" />
                  <span class="type-count-label">道</span>
                  <el-button type="danger" :icon="Delete" circle size="small" text
                    @click="aiConfig.bankTypeConfigs.splice(idx,1)"
                    :disabled="aiConfig.bankTypeConfigs.length <= 1" />
                </div>
                <el-button size="small" :icon="Plus" plain style="margin-top:6px"
                  @click="aiConfig.bankTypeConfigs.push({type:1,difficulty:null,count:3})">添加一行</el-button>
              </div>
            </el-form-item>
          </template>
        </el-form>
      </div>

      <!-- Step 2: 审核 -->
      <div v-else class="ai-review-container">
        <el-alert
          :title="`共 ${aiReviewItems.length} 题（题库 ${bankCount} 题 + AI新生成 ${newAiCount} 题），已选 ${selectedCount} 题`"
          type="info"
          :closable="false"
          style="margin-bottom:12px"
        />

        <div v-if="bankCount > 0" class="review-section">
          <div class="review-section-title">
            <el-icon style="color:#409eff"><Reading /></el-icon>
            题库题目（{{ bankCount }} 题）
          </div>
          <div
            v-for="(item, idx) in bankItems"
            :key="idx"
            class="review-item"
            :class="{ deselected: !item.selected }"
          >
            <el-checkbox v-model="item.selected" class="review-checkbox" />
            <div class="review-item-body">
              <div class="review-item-tags">
                <el-tag size="small" type="info">{{ typeLabel(item.type) }}</el-tag>
                <el-tag size="small" type="warning">难度 {{ item.difficulty }}</el-tag>
                <span v-if="item.knowledgePoint" class="kp-label">{{ item.knowledgePoint }}</span>
              </div>
              <div class="review-content">{{ item.content }}</div>
            </div>
            <div class="review-score">
              <el-input-number v-model="item.score" :min="1" :max="50" size="small" style="width:80px" />
              <span class="score-suffix">分</span>
            </div>
          </div>
        </div>

        <div v-if="newAiCount > 0" class="review-section">
          <div class="review-section-title">
            <el-icon style="color:#67c23a"><MagicStick /></el-icon>
            AI 新生成题目（{{ newAiCount }} 题）— 可直接修改后保存入库
          </div>
          <div
            v-for="(item, idx) in aiNewItems"
            :key="idx"
            class="review-item ai-item"
            :class="{ deselected: !item.selected }"
          >
            <el-checkbox v-model="item.selected" class="review-checkbox" />
            <div class="review-item-body" style="flex:1">
              <div class="review-item-tags">
                <el-tag size="small" type="success">AI生成</el-tag>
                <el-select v-model="item.type" size="small" style="width:90px">
                  <el-option label="单选题" :value="1" />
                  <el-option label="多选题" :value="2" />
                  <el-option label="判断题" :value="3" />
                  <el-option label="简答题" :value="4" />
                </el-select>
                <el-select v-model="item.difficulty" size="small" style="width:90px">
                  <el-option label="难度 1" :value="1" />
                  <el-option label="难度 2" :value="2" />
                  <el-option label="难度 3" :value="3" />
                  <el-option label="难度 4" :value="4" />
                  <el-option label="难度 5" :value="5" />
                </el-select>
                <el-input
                  v-model="item.knowledgePoint"
                  size="small"
                  placeholder="知识点"
                  style="width:160px"
                />
              </div>
              <el-input
                v-model="item.content"
                type="textarea"
                :rows="2"
                placeholder="题目内容"
                class="review-field"
              />
              <div v-if="item.options && item.options.length" class="options-edit">
                <div
                  v-for="(opt, oi) in item.options"
                  :key="oi"
                  class="option-edit-row"
                >
                  <span class="option-alpha">{{ String.fromCharCode(65 + oi) }}.</span>
                  <el-input v-model="item.options[oi]" size="small" style="flex:1" />
                </div>
              </div>
              <el-input
                v-model="item.correctAnswer"
                size="small"
                placeholder="正确答案"
                class="review-field"
              >
                <template #prepend>答案</template>
              </el-input>
              <el-input
                v-model="item.explanation"
                size="small"
                placeholder="解析（可选）"
                class="review-field"
              >
                <template #prepend>解析</template>
              </el-input>
            </div>
            <div class="review-score">
              <el-input-number v-model="item.score" :min="1" :max="50" size="small" style="width:80px" />
              <span class="score-suffix">分</span>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <template v-if="aiStep === 1">
          <el-button @click="showAiWizard = false">取消</el-button>
          <el-button type="primary" :loading="aiGenerating" @click="generateAiPreview">
            生成预览 →
          </el-button>
        </template>
        <template v-else>
          <el-button @click="aiStep = 1">← 返回配置</el-button>
          <el-button type="primary" :loading="aiSubmitting" @click="confirmAiQuestions">
            确认添加（已选 {{ selectedCount }} 题）
          </el-button>
        </template>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { examPapersApi } from '@/api/examPapers'
import { questionsApi } from '@/api/questions'
import { ElMessage } from 'element-plus'
import { Plus, Delete, ArrowLeft, MagicStick, Reading } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const isEdit = computed(() => !!route.params.id)
const pageLoading = ref(false)
const submitting = ref(false)
const formRef = ref(null)

const form = reactive({
  title: '',
  description: '',
  totalScore: 100,
  durationMinutes: 90,
  startTime: null,
  endTime: null,
  antiCheatingEnabled: false,
  questions: []
})

const rules = {
  title: [{ required: true, message: '请输入试卷标题', trigger: 'blur' }],
  totalScore: [{ required: true, message: '请设置总分', trigger: 'blur' }],
  durationMinutes: [{ required: true, message: '请设置考试时长', trigger: 'blur' }]
}

const computedScore = computed(() => form.questions.reduce((sum, q) => sum + (q.score || 0), 0))

const typeLabel = (t) => ({ 1: '单选题', 2: '多选题', 3: '判断题', 4: '简答题' }[t] ?? t)

// 题目选择器
const showQuestionPicker = ref(false)
const pickerLoading = ref(false)
const pickerQuestions = ref([])
const pickerTotal = ref(0)
const selectedQuestions = ref([])
const pickerQuery = reactive({ page: 1, pageSize: 10, type: null, knowledgePoint: '' })

async function loadPickerQuestions() {
  pickerLoading.value = true
  try {
    const res = await questionsApi.getList({
      page: pickerQuery.page,
      pageSize: pickerQuery.pageSize,
      type: pickerQuery.type ?? undefined,
      knowledgePoint: pickerQuery.knowledgePoint || undefined
    })
    pickerQuestions.value = res.items
    pickerTotal.value = res.totalCount
  } finally {
    pickerLoading.value = false
  }
}

function addSelectedQuestions() {
  const existingIds = new Set(form.questions.map(q => q.questionId))
  const toAdd = selectedQuestions.value.filter(q => !existingIds.has(q.id))
  toAdd.forEach((q, i) => {
    form.questions.push({
      questionId: q.id,
      content: q.content,
      type: q.type,
      score: 5,
      order: form.questions.length + i + 1
    })
  })
  showQuestionPicker.value = false
  if (toAdd.length) ElMessage.success(`已添加 ${toAdd.length} 道题目`)
}

function removeQuestion(index) {
  form.questions.splice(index, 1)
  form.questions.forEach((q, i) => { q.order = i + 1 })
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    const payload = {
      title: form.title,
      description: form.description,
      totalScore: form.totalScore,
      durationMinutes: form.durationMinutes,
      startTime: form.startTime,
      endTime: form.endTime,
      antiCheatingEnabled: form.antiCheatingEnabled,
      questions: form.questions.map((q, i) => ({
        questionId: q.questionId,
        score: q.score,
        order: i + 1
      }))
    }
    if (isEdit.value) {
      await examPapersApi.update(route.params.id, payload)
      ElMessage.success('保存成功')
    } else {
      await examPapersApi.create(payload)
      ElMessage.success('创建成功')
    }
    router.push('/exam-papers')
  } finally {
    submitting.value = false
  }
}

// ---- AI 出题向导 ----
const showAiWizard = ref(false)
const aiStep = ref(1)
const aiGenerating = ref(false)
const aiSubmitting = ref(false)
const aiReviewItems = ref([])

const aiConfig = reactive({
  knowledgePoint: '',
  typeConfigs: [
    { type: 1, difficulty: 3, count: 5 },
  ],
  pickFromBank: false,
  bankKnowledgePoint: '',
  bankTypeConfigs: [
    { type: 1, difficulty: null, count: 3 },
  ],
})

const bankItems = computed(() => aiReviewItems.value.filter(q => q.source === 'bank'))
const aiNewItems = computed(() => aiReviewItems.value.filter(q => q.source === 'ai'))
const bankCount = computed(() => bankItems.value.length)
const newAiCount = computed(() => aiNewItems.value.length)
const selectedCount = computed(() => aiReviewItems.value.filter(q => q.selected).length)

function openAiWizard() {
  aiStep.value = 1
  aiReviewItems.value = []
  showAiWizard.value = true
}

function resetAiWizard() {
  aiStep.value = 1
  aiReviewItems.value = []
}

async function generateAiPreview() {
  if (!aiConfig.knowledgePoint.trim()) {
    ElMessage.warning('请输入知识点')
    return
  }
  const validTypes = aiConfig.typeConfigs.filter(t => t.count > 0)
  const validBankTypes = aiConfig.bankTypeConfigs.filter(t => t.count > 0)
  if (validTypes.length === 0 && !aiConfig.pickFromBank) {
    ElMessage.warning('请至少配置一种 AI 生成题型，或启用题库抽取')
    return
  }
  if (aiConfig.pickFromBank && validBankTypes.length === 0) {
    ElMessage.warning('题库抽取已启用，请至少添加一行题型配置')
    return
  }

  aiGenerating.value = true
  aiReviewItems.value = []
  try {
    // 题库随机抽取（按题型+难度分别抽取）
    if (aiConfig.pickFromBank && validBankTypes.length > 0) {
      for (const tc of validBankTypes) {
        const res = await questionsApi.getList({
          page: 1,
          pageSize: 100,
          type: tc.type,
          difficulty: tc.difficulty || undefined,
          knowledgePoint: aiConfig.bankKnowledgePoint || undefined,
        })
        const pool = [...res.items]
        const take = Math.min(tc.count, pool.length)
        for (let i = 0; i < take; i++) {
          const idx = Math.floor(Math.random() * pool.length)
          const q = pool.splice(idx, 1)[0]
          aiReviewItems.value.push({
            source: 'bank',
            selected: true,
            questionId: q.id,
            type: q.type,
            content: q.content,
            knowledgePoint: q.knowledgePoint || '',
            difficulty: q.difficulty,
            options: [],
            correctAnswer: '',
            explanation: '',
            score: 5,
          })
        }
      }
    }

    // AI 生成预览
    if (validTypes.length > 0) {
      const previews = await questionsApi.aiPreview({
        knowledgePoint: aiConfig.knowledgePoint,
        typeConfigs: validTypes.map(t => ({ type: t.type, difficulty: t.difficulty, count: t.count })),
      })
      for (const q of previews) {
        aiReviewItems.value.push({
          source: 'ai',
          selected: true,
          questionId: null,
          type: q.type,
          content: q.content,
          options: q.options || [],
          correctAnswer: q.correctAnswer || '',
          explanation: q.explanation || '',
          knowledgePoint: q.knowledgePoint || aiConfig.knowledgePoint,
          difficulty: q.difficulty || 3,
          score: 5,
        })
      }
    }

    if (aiReviewItems.value.length === 0) {
      ElMessage.warning('未获取到任何题目，请检查题库或 AI 配置')
      return
    }
    aiStep.value = 2
  } catch {
    ElMessage.error('生成失败，请检查网络或 AI 服务配置')
  } finally {
    aiGenerating.value = false
  }
}

async function confirmAiQuestions() {
  const selected = aiReviewItems.value.filter(q => q.selected)
  if (!selected.length) {
    ElMessage.warning('请至少选择一道题目')
    return
  }

  aiSubmitting.value = true
  try {
    // 将新 AI 题目保存入题库
    const toSave = selected.filter(q => q.source === 'ai')
    if (toSave.length > 0) {
      const savedIds = await questionsApi.batchCreate({
        questions: toSave.map(q => ({
          type: q.type,
          content: q.content,
          options: q.options?.length ? q.options : null,
          correctAnswer: q.correctAnswer,
          explanation: q.explanation || null,
          knowledgePoint: q.knowledgePoint || null,
          difficulty: q.difficulty,
        })),
      })
      toSave.forEach((q, i) => { q.questionId = savedIds[i] })
    }

    // 添加到试卷题目列表
    const existingIds = new Set(form.questions.map(q => q.questionId))
    let added = 0
    for (const q of selected) {
      if (!existingIds.has(q.questionId)) {
        form.questions.push({
          questionId: q.questionId,
          content: q.content,
          type: q.type,
          score: q.score,
          order: form.questions.length + 1,
        })
        added++
      }
    }

    ElMessage.success(
      `已添加 ${added} 道题目${toSave.length ? `，其中 ${toSave.length} 道 AI 新题已保存至题库` : ''}`
    )
    showAiWizard.value = false
  } catch {
    ElMessage.error('保存失败，请重试')
  } finally {
    aiSubmitting.value = false
  }
}

async function loadDetail() {
  if (!isEdit.value) return
  pageLoading.value = true
  try {
    const data = await examPapersApi.getById(route.params.id)
    form.title = data.title
    form.description = data.description || ''
    form.totalScore = data.totalScore
    form.durationMinutes = data.durationMinutes
    form.startTime = data.startTime
    form.endTime = data.endTime
    form.antiCheatingEnabled = data.antiCheatingEnabled
    form.questions = data.questions.map(q => ({
      questionId: q.questionId,
      content: q.content,
      type: q.type,
      score: q.score,
      order: q.order
    }))
  } finally {
    pageLoading.value = false
  }
}

onMounted(() => {
  loadDetail()
  loadPickerQuestions()
})
</script>

<style scoped>
.page-container { padding: 20px; }
.page-header { margin-bottom: 16px; }
.breadcrumb-back {
  display: flex;
  align-items: center;
  gap: 8px;
}
.breadcrumb-back h3 { margin: 0; font-size: 18px; }
.form-card { margin-bottom: 16px; }
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 16px;
}
.question-content {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.picker-filter { margin-bottom: 12px; }
.pagination { margin-top: 12px; justify-content: center; }

/* AI 出题向导 */
.type-config-table { display: flex; flex-direction: column; gap: 6px; }
.type-config-table-header {
  display: flex;
  gap: 10px;
  padding: 0 2px 4px;
  border-bottom: 1px solid #ebeef5;
}
.type-config-table-header span {
  font-size: 12px;
  color: #909399;
  font-weight: 500;
}
.type-config-table-header span:nth-child(1) { width: 96px; }
.type-config-table-header span:nth-child(2) { width: 90px; }
.type-config-table-header span:nth-child(3) { width: 88px; }
.type-config-row { display: flex; align-items: center; gap: 10px; }
.type-count-label { font-size: 13px; color: #606266; }

.ai-review-container { max-height: 60vh; overflow-y: auto; padding-right: 4px; }
.review-section { margin-bottom: 16px; }
.review-section-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 600;
  font-size: 14px;
  padding: 8px 0;
  border-bottom: 1px solid #ebeef5;
  margin-bottom: 10px;
}
.review-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 12px;
  border: 1px solid #ebeef5;
  border-radius: 6px;
  margin-bottom: 8px;
  transition: opacity 0.2s;
}
.review-item.deselected { opacity: 0.45; }
.review-checkbox { margin-top: 2px; flex-shrink: 0; }
.review-item-body { flex: 1; min-width: 0; }
.review-item-tags { display: flex; align-items: center; flex-wrap: wrap; gap: 6px; margin-bottom: 8px; }
.kp-label { font-size: 12px; color: #909399; background: #f5f7fa; padding: 2px 6px; border-radius: 4px; }
.review-content { font-size: 13px; color: #303133; line-height: 1.6; }
.review-score {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}
.score-suffix { font-size: 12px; color: #606266; }
.review-field { margin-top: 8px; }
.options-edit { margin-top: 8px; display: flex; flex-direction: column; gap: 6px; }
.option-edit-row { display: flex; align-items: center; gap: 6px; }
.option-alpha { font-weight: 600; color: #409eff; width: 20px; flex-shrink: 0; }
</style>
