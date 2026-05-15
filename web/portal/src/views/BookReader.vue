<template>
  <div class="reader-layout" @mouseup="handleTextSelection">
    <!-- 顶部工具栏 -->
    <div class="reader-toolbar">
      <el-button :icon="ArrowLeft" text @click="$router.back()">返回</el-button>
      <div class="book-title-bar">{{ bookInfo?.title }}</div>
      <div class="toolbar-controls">
        <el-button :icon="Minus" circle text @click="changeScale(-0.1)" />
        <span class="scale-label">{{ Math.round(scale * 100) }}%</span>
        <el-button :icon="Plus" circle text @click="changeScale(0.1)" />
        <el-divider direction="vertical" />
        <el-button :icon="Notebook" text @click="sidebarVisible = !sidebarVisible">
          标注 ({{ annotations.length }})
        </el-button>
      </div>
    </div>

    <div class="reader-body">
      <!-- PDF 主内容区 -->
      <div ref="viewerRef" class="pdf-viewer" @scroll="handleScroll">
        <div v-if="pdfLoading" class="pdf-loading">
          <el-icon class="is-loading" size="48"><Loading /></el-icon>
          <div>PDF 加载中...</div>
        </div>
        <div v-if="pdfError" class="pdf-error">
          <el-icon size="48" color="#f56c6c"><CircleCloseFilled /></el-icon>
          <div>{{ pdfError }}</div>
        </div>
        <div v-for="p in renderedPages" :key="p.pageNum" :id="`page-${p.pageNum}`" class="pdf-page-wrap">
          <canvas :ref="el => pageCanvases[p.pageNum] = el" class="pdf-canvas" />
          <div class="text-layer" :ref="el => textLayers[p.pageNum] = el" />
        </div>
      </div>

      <!-- 右侧标注面板 -->
      <transition name="slide-right">
        <div v-if="sidebarVisible" class="annotation-sidebar">
          <div class="sidebar-header">
            <span>我的标注</span>
            <el-button :icon="Close" text circle @click="sidebarVisible = false" />
          </div>

          <!-- 标注类型 Tab -->
          <el-tabs v-model="activeTab" class="ann-tabs">
            <el-tab-pane label="全部" name="all" />
            <el-tab-pane label="书签" name="1" />
            <el-tab-pane label="备注" name="2" />
            <el-tab-pane label="AI问答" name="3" />
          </el-tabs>

          <div class="ann-list" v-loading="annLoading">
            <div
              v-for="ann in filteredAnnotations"
              :key="ann.id"
              class="ann-item"
              :style="{ borderLeft: `4px solid ${ann.highlightColor || '#409eff'}` }"
              @click="jumpToPage(ann.pageNumber)"
            >
              <div class="ann-header">
                <el-tag
                  :type="annTypeTag(ann.annotationType)"
                  size="small"
                >{{ annTypeLabel(ann.annotationType) }}</el-tag>
                <span class="ann-page">P{{ ann.pageNumber }}</span>
                <el-button :icon="Delete" text circle size="small" @click.stop="deleteAnnotation(ann)" />
              </div>
              <div v-if="ann.selectedText" class="ann-selected-text">{{ ann.selectedText }}</div>
              <div v-if="ann.note" class="ann-note">{{ ann.note }}</div>
              <div v-if="ann.annotationType === 3" class="ann-qa">
                <div class="ann-question"><b>Q：</b>{{ ann.aiQuestion }}</div>
                <div class="ann-answer"><b>A：</b>{{ ann.aiAnswer }}</div>
              </div>
            </div>
            <el-empty v-if="!annLoading && filteredAnnotations.length === 0" description="暂无标注" />
          </div>
        </div>
      </transition>
    </div>

    <!-- 文字选择浮动工具栏 -->
    <div
      v-if="selectionToolbar.visible"
      class="selection-toolbar"
      :style="{ top: selectionToolbar.y + 'px', left: selectionToolbar.x + 'px' }"
    >
      <el-button size="small" :icon="Bookmark" @click="addBookmark">书签</el-button>
      <el-button size="small" :icon="EditPen" @click="openNoteDialog">备注</el-button>
      <el-button size="small" type="primary" :icon="MagicStick" @click="openAiDialog">AI问答</el-button>
    </div>

    <!-- 备注 Dialog -->
    <el-dialog v-model="noteDialog.visible" title="添加备注" width="440px" destroy-on-close>
      <el-form>
        <el-form-item label="选中文字">
          <el-text type="info" size="small">{{ noteDialog.selectedText }}</el-text>
        </el-form-item>
        <el-form-item label="备注内容">
          <el-input
            v-model="noteDialog.note"
            type="textarea"
            :rows="4"
            placeholder="请输入备注..."
            autofocus
          />
        </el-form-item>
        <el-form-item label="高亮颜色">
          <div class="color-picker-row">
            <span
              v-for="c in highlightColors"
              :key="c"
              class="color-dot"
              :class="{ active: noteDialog.color === c }"
              :style="{ background: c }"
              @click="noteDialog.color = c"
            />
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="noteDialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="noteDialog.saving" @click="saveNote">保存</el-button>
      </template>
    </el-dialog>

    <!-- AI 问答 Drawer -->
    <el-drawer
      v-model="aiDrawer.visible"
      title="AI 智能分析"
      direction="rtl"
      size="420px"
      destroy-on-close
    >
      <div class="ai-drawer-body">
        <div class="ai-selected-text">
          <el-text type="info" size="small">选中内容：</el-text>
          <blockquote>{{ aiDrawer.selectedText }}</blockquote>
        </div>
        <el-input
          v-model="aiDrawer.question"
          type="textarea"
          :rows="3"
          placeholder="请输入你的问题（留空则让AI自由分析）..."
          style="margin: 16px 0"
        />
        <el-button
          type="primary"
          :icon="MagicStick"
          :loading="aiDrawer.loading"
          style="width:100%;margin-bottom:16px"
          @click="runAiAnalyze"
        >开始分析</el-button>

        <div v-if="aiDrawer.answer" class="ai-answer-box">
          <div class="ai-answer-content" v-html="renderMarkdown(aiDrawer.answer)" />
          <div class="ai-save-row">
            <el-button size="small" type="success" :icon="Check" :loading="aiDrawer.saving" @click="saveAiAnnotation">
              保存到标注
            </el-button>
          </div>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  ArrowLeft, Minus, Plus, Notebook, Loading, CircleCloseFilled,
  Delete, Close, Bookmark, EditPen, MagicStick, Check
} from '@element-plus/icons-vue'
import { booksApi } from '@/api/books'

const route = useRoute()
const router = useRouter()
const bookId = route.params.id

// ==================== State ====================
const bookInfo = ref(null)
const pdfLoading = ref(true)
const pdfError = ref('')
const scale = ref(1.2)
const viewerRef = ref(null)
const pageCanvases = ref({})
const textLayers = ref({})
const renderedPages = ref([])
const currentPage = ref(1)
let pdfDoc = null
let pdfObjectUrl = ''
const renderingQueue = new Set()

const sidebarVisible = ref(false)
const annotations = ref([])
const annLoading = ref(false)
const activeTab = ref('all')

const highlightColors = ['#FFEB3B', '#A5D6A7', '#CE93D8', '#90CAF9', '#FFCCBC']

// ==================== PDF Rendering ====================
async function initPdf() {
  pdfLoading.value = true
  pdfError.value = ''
  try {
    // Load book info
    bookInfo.value = await booksApi.getBook(bookId)

    // Dynamic import pdfjs-dist
    const pdfjsLib = await import('pdfjs-dist')
    // Set worker - use CDN for simplicity
    pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjsLib.version}/pdf.worker.min.js`

    pdfObjectUrl = await booksApi.getPdfObjectUrl(bookId)
    pdfDoc = await pdfjsLib.getDocument(pdfObjectUrl).promise

    const numPages = pdfDoc.numPages
    renderedPages.value = Array.from({ length: numPages }, (_, i) => ({ pageNum: i + 1 }))

    await nextTick()
    for (let i = 1; i <= Math.min(3, numPages); i++) {
      await renderPage(i)
    }
  } catch (e) {
    pdfError.value = e?.message || 'PDF 加载失败'
  } finally {
    pdfLoading.value = false
  }
}

async function renderPage(pageNum) {
  if (renderingQueue.has(pageNum)) return
  renderingQueue.add(pageNum)
  try {
    const page = await pdfDoc.getPage(pageNum)
    const viewport = page.getViewport({ scale: scale.value })
    const canvas = pageCanvases.value[pageNum]
    if (!canvas) return
    canvas.width = viewport.width
    canvas.height = viewport.height
    const ctx = canvas.getContext('2d')
    await page.render({ canvasContext: ctx, viewport }).promise

    // Text layer
    const textLayerDiv = textLayers.value[pageNum]
    if (textLayerDiv) {
      textLayerDiv.innerHTML = ''
      textLayerDiv.style.width = viewport.width + 'px'
      textLayerDiv.style.height = viewport.height + 'px'
      const textContent = await page.getTextContent()
      const { renderTextLayer } = await import('pdfjs-dist')
      renderTextLayer({
        textContentSource: textContent,
        container: textLayerDiv,
        viewport
      })
    }
  } finally {
    renderingQueue.delete(pageNum)
  }
}

async function rerenderAll() {
  for (let i = 1; i <= (pdfDoc?.numPages || 0); i++) {
    renderingQueue.delete(i)
  }
  const visibleNums = getVisiblePageNums()
  for (const p of visibleNums) await renderPage(p)
}

function changeScale(delta) {
  scale.value = Math.min(3, Math.max(0.5, +(scale.value + delta).toFixed(1)))
  nextTick(rerenderAll)
}

function getVisiblePageNums() {
  if (!viewerRef.value) return [1]
  const rect = viewerRef.value.getBoundingClientRect()
  const visible = []
  for (const p of renderedPages.value) {
    const el = document.getElementById(`page-${p.pageNum}`)
    if (!el) continue
    const r = el.getBoundingClientRect()
    if (r.bottom > rect.top && r.top < rect.bottom) visible.push(p.pageNum)
  }
  return visible.length ? visible : [1]
}

function handleScroll() {
  const visible = getVisiblePageNums()
  currentPage.value = visible[0] || 1
  // Lazy render visible pages
  visible.forEach(p => renderPage(p))
  // Pre-render next page
  const next = visible[visible.length - 1] + 1
  if (next <= (pdfDoc?.numPages || 0)) renderPage(next)
}

function jumpToPage(pageNum) {
  const el = document.getElementById(`page-${pageNum}`)
  if (el) el.scrollIntoView({ behavior: 'smooth' })
}

// ==================== Annotations ====================
async function loadAnnotations() {
  annLoading.value = true
  try {
    annotations.value = await booksApi.getAnnotations(bookId)
  } catch {
    ElMessage.error('加载标注失败')
  } finally {
    annLoading.value = false
  }
}

const filteredAnnotations = computed(() => {
  if (activeTab.value === 'all') return annotations.value
  return annotations.value.filter(a => String(a.annotationType) === activeTab.value)
})

function annTypeLabel(t) {
  return { 1: '书签', 2: '备注', 3: 'AI问答' }[t] || '未知'
}
function annTypeTag(t) {
  return { 1: 'warning', 2: 'success', 3: 'primary' }[t] || 'info'
}

async function deleteAnnotation(ann) {
  await ElMessageBox.confirm('确定删除该标注？', '确认', { type: 'warning' })
  await booksApi.deleteAnnotation(bookId, ann.id)
  annotations.value = annotations.value.filter(a => a.id !== ann.id)
  ElMessage.success('已删除')
}

// ==================== Text Selection Toolbar ====================
const selectionToolbar = reactive({ visible: false, x: 0, y: 0 })
let selectedText = ''
let selectionPageNum = 1

function handleTextSelection() {
  const sel = window.getSelection()
  const text = sel?.toString().trim()
  if (!text) {
    selectionToolbar.visible = false
    return
  }
  selectedText = text

  // Determine page number from nearest page-wrap
  const anchor = sel.anchorNode?.parentElement
  const pageWrap = anchor?.closest('[id^="page-"]')
  if (pageWrap) {
    selectionPageNum = parseInt(pageWrap.id.replace('page-', ''))
  }

  const range = sel.getRangeAt(0)
  const rect = range.getBoundingClientRect()
  selectionToolbar.x = rect.left + rect.width / 2 - 100
  selectionToolbar.y = rect.top - 48 + window.scrollY
  selectionToolbar.visible = true
}

document.addEventListener('mousedown', (e) => {
  if (!e.target.closest('.selection-toolbar')) {
    selectionToolbar.visible = false
  }
})

// ==================== Bookmark ====================
async function addBookmark() {
  selectionToolbar.visible = false
  try {
    const ann = await booksApi.createAnnotation(bookId, {
      pageNumber: selectionPageNum,
      selectedText,
      note: null,
      annotationType: 1,
      highlightColor: '#FFEB3B'
    })
    annotations.value.unshift(ann)
    ElMessage.success('书签已添加')
    sidebarVisible.value = true
  } catch {
    ElMessage.error('添加书签失败')
  }
}

// ==================== Note Dialog ====================
const noteDialog = reactive({
  visible: false, note: '', color: '#A5D6A7', selectedText: '', saving: false
})

function openNoteDialog() {
  selectionToolbar.visible = false
  noteDialog.selectedText = selectedText
  noteDialog.note = ''
  noteDialog.color = '#A5D6A7'
  noteDialog.visible = true
}

async function saveNote() {
  if (!noteDialog.note.trim()) {
    ElMessage.warning('请输入备注内容')
    return
  }
  noteDialog.saving = true
  try {
    const ann = await booksApi.createAnnotation(bookId, {
      pageNumber: selectionPageNum,
      selectedText: noteDialog.selectedText,
      note: noteDialog.note,
      annotationType: 2,
      highlightColor: noteDialog.color
    })
    annotations.value.unshift(ann)
    noteDialog.visible = false
    ElMessage.success('备注已保存')
    sidebarVisible.value = true
  } catch {
    ElMessage.error('保存失败')
  } finally {
    noteDialog.saving = false
  }
}

// ==================== AI Drawer ====================
const aiDrawer = reactive({
  visible: false, selectedText: '', question: '',
  answer: '', loading: false, saving: false
})

function openAiDialog() {
  selectionToolbar.visible = false
  aiDrawer.selectedText = selectedText
  aiDrawer.question = ''
  aiDrawer.answer = ''
  aiDrawer.visible = true
}

async function runAiAnalyze() {
  if (!aiDrawer.selectedText) return
  aiDrawer.loading = true
  aiDrawer.answer = ''
  try {
    const res = await booksApi.aiAnalyze(bookId, {
      selectedText: aiDrawer.selectedText,
      question: aiDrawer.question || null
    })
    aiDrawer.answer = res.answer || res
  } catch {
    ElMessage.error('AI 分析失败')
  } finally {
    aiDrawer.loading = false
  }
}

async function saveAiAnnotation() {
  aiDrawer.saving = true
  try {
    const ann = await booksApi.createAnnotation(bookId, {
      pageNumber: selectionPageNum,
      selectedText: aiDrawer.selectedText,
      note: null,
      annotationType: 3,
      aiQuestion: aiDrawer.question || '请分析这段文字',
      highlightColor: '#CE93D8'
    })
    annotations.value.unshift(ann)
    ElMessage.success('已保存到标注')
    sidebarVisible.value = true
  } catch {
    ElMessage.error('保存失败')
  } finally {
    aiDrawer.saving = false
  }
}

// Simple markdown renderer (newlines → <br>, **bold**)
function renderMarkdown(text) {
  if (!text) return ''
  return text
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\n/g, '<br>')
}

// ==================== Lifecycle ====================
onMounted(() => {
  initPdf()
  loadAnnotations()
})

onBeforeUnmount(() => {
  if (pdfObjectUrl) URL.revokeObjectURL(pdfObjectUrl)
})
</script>

<style scoped>
.reader-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: #404040;
  overflow: hidden;
}

.reader-toolbar {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #2c2c2c;
  color: #fff;
  padding: 0 16px;
  height: 52px;
  flex-shrink: 0;
  z-index: 10;
}
.book-title-bar {
  flex: 1;
  text-align: center;
  font-size: 15px;
  font-weight: 500;
  color: #e0e0e0;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.toolbar-controls {
  display: flex;
  align-items: center;
  gap: 4px;
}
.scale-label {
  min-width: 48px;
  text-align: center;
  color: #ccc;
  font-size: 13px;
}

.reader-body {
  display: flex;
  flex: 1;
  overflow: hidden;
  position: relative;
}

.pdf-viewer {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.pdf-loading, .pdf-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  color: #ccc;
  margin-top: 80px;
  font-size: 15px;
}

.pdf-page-wrap {
  position: relative;
  box-shadow: 0 4px 20px rgba(0,0,0,.4);
}
.pdf-canvas { display: block; }
.text-layer {
  position: absolute;
  top: 0;
  left: 0;
  overflow: hidden;
  opacity: 0.2;
  line-height: 1;
  pointer-events: auto;
}
.text-layer :deep(span) {
  color: transparent;
  position: absolute;
  white-space: pre;
  cursor: text;
  transform-origin: 0% 0%;
}

/* Annotation Sidebar */
.annotation-sidebar {
  width: 360px;
  background: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: -2px 0 12px rgba(0,0,0,.15);
}
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  font-weight: 600;
  border-bottom: 1px solid #eee;
}

.ann-tabs { padding: 0 8px; }

.ann-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}
.ann-item {
  padding: 10px 12px;
  margin-bottom: 8px;
  background: #fafafa;
  border-radius: 4px;
  cursor: pointer;
  transition: background .15s;
}
.ann-item:hover { background: #f0f5ff; }
.ann-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
}
.ann-page { font-size: 12px; color: #999; flex: 1; }
.ann-selected-text {
  font-size: 13px;
  color: #555;
  padding: 6px 8px;
  background: #fffde7;
  border-radius: 3px;
  margin-bottom: 4px;
  font-style: italic;
}
.ann-note { font-size: 13px; color: #333; }
.ann-qa { font-size: 12px; line-height: 1.6; }
.ann-question { color: #666; margin-bottom: 4px; }
.ann-answer { color: #333; }

/* Floating selection toolbar */
.selection-toolbar {
  position: fixed;
  z-index: 1000;
  background: #2c2c2c;
  border-radius: 6px;
  padding: 6px 8px;
  display: flex;
  gap: 6px;
  box-shadow: 0 4px 16px rgba(0,0,0,.3);
}
.selection-toolbar .el-button { color: #fff; }
.selection-toolbar .el-button:hover { background: rgba(255,255,255,.1); }

/* Note Dialog */
.color-picker-row { display: flex; gap: 8px; align-items: center; }
.color-dot {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  cursor: pointer;
  transition: transform .15s;
  border: 2px solid transparent;
}
.color-dot.active, .color-dot:hover {
  transform: scale(1.2);
  border-color: #666;
}

/* AI Drawer */
.ai-drawer-body { padding: 8px; }
blockquote {
  margin: 8px 0;
  padding: 8px 12px;
  background: #f5f7fa;
  border-left: 4px solid #409eff;
  border-radius: 4px;
  font-size: 13px;
  color: #555;
  font-style: italic;
}
.ai-answer-box {
  background: #f5f7fa;
  border-radius: 8px;
  padding: 16px;
}
.ai-answer-content {
  font-size: 14px;
  line-height: 1.8;
  color: #303133;
}
.ai-save-row {
  margin-top: 12px;
  text-align: right;
}

/* Slide transition */
.slide-right-enter-active, .slide-right-leave-active {
  transition: transform .25s ease;
}
.slide-right-enter-from, .slide-right-leave-to {
  transform: translateX(100%);
}
</style>
