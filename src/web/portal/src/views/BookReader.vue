<template>
  <div class="reader-layout">
    <!-- 顶部工具栏 -->
    <div class="reader-toolbar">
      <button class="tb-btn back-btn" @click="$router.back()">
        <el-icon><ArrowLeft /></el-icon>返回
      </button>
      <el-divider direction="vertical" />
      <div class="book-title-bar">{{ bookInfo?.title || '图书阅读' }}</div>
      <div class="toolbar-spacer" />
      <div class="toolbar-controls">
        <!-- 缩放控件 -->
        <div class="zoom-group">
          <button class="tb-btn" :class="{ active: zoomMode === 'fit' }" title="适合页面" @click="setZoom('fit')">
            <el-icon><FullScreen /></el-icon>
          </button>
          <button class="tb-btn" :class="{ active: zoomMode === 'width' }" title="适合宽度" @click="setZoom('width')">
            <el-icon><ScaleToOriginal /></el-icon>
          </button>
          <button class="tb-btn" @click="zoomOut" :disabled="zoomMode === 'fit' || zoomMode === 'width' || renderScale <= 0.5" title="缩小">
            <el-icon><Minus /></el-icon>
          </button>
          <span class="zoom-label">{{ Math.round(renderScale * 100) }}%</span>
          <button class="tb-btn" @click="zoomIn" :disabled="renderScale >= 4" title="放大">
            <el-icon><Plus /></el-icon>
          </button>
        </div>
        <el-divider direction="vertical" />
        <!-- 翻页 -->
        <div class="page-nav">
          <button class="tb-btn" :disabled="currentPage <= 1" @click="prevPage">
            <el-icon><ArrowLeft /></el-icon>
          </button>
          <span class="page-indicator-text">{{ currentPage }} / {{ totalPages || '…' }}</span>
          <button class="tb-btn" :disabled="currentPage >= totalPages" @click="nextPage">
            <el-icon><ArrowRight /></el-icon>
          </button>
        </div>
        <el-divider direction="vertical" />
        <!-- 功能按钮 -->
        <button class="tb-btn" :class="{ active: drawMode }" @click="toggleDrawMode" title="框选标注">
          <el-icon><Edit /></el-icon><span>框选</span>
        </button>
        <button class="tb-btn" @click="openNoteDialog" title="添加备注">
          <el-icon><EditPen /></el-icon><span>备注</span>
        </button>
        <button class="tb-btn" @click="openAiDrawer" title="AI问答">
          <el-icon><MagicStick /></el-icon><span>AI</span>
        </button>
        <button class="tb-btn" :class="{ active: sidebarVisible }" @click="sidebarVisible = !sidebarVisible" title="标注列表">
          <el-icon><Notebook /></el-icon><span>标注 ({{ annotations.length }})</span>
        </button>
        <template v-if="lastReadPos">
          <el-divider direction="vertical" />
          <button class="tb-btn last-read-btn" @click="jumpToLastRead"
            :title="`上次阅读：第${lastReadPos.pageNumber}页`">
            <div class="last-read-main">
              <el-icon><Clock /></el-icon>
              <span>继续 · 第{{ lastReadPos.pageNumber }}页</span>
            </div>
            <span class="last-read-time">{{ formatLastReadTime(lastReadPos.savedAt) }}</span>
          </button>
        </template>
      </div>
    </div>

    <!-- 主体区域 -->
    <div class="reader-body">
      <!-- 左侧缩略图导航 -->
      <transition name="slide-left">
        <div v-if="thumbVisible" class="thumb-sidebar">
          <div class="thumb-header">
            <span class="thumb-title">页面导航</span>
            <button class="tb-btn tb-btn-sm" @click="thumbVisible = false">
              <el-icon><Close /></el-icon>
            </button>
          </div>
          <div class="thumb-list" ref="thumbListEl">
            <div
              v-for="(url, idx) in thumbnails"
              :key="idx"
              class="thumb-item"
              :class="{ active: currentPage === idx + 1 }"
              @click="goPage(idx + 1)"
            >
              <img v-if="url" :src="url" class="thumb-img" :alt="`第${idx+1}页`" />
              <div v-else class="thumb-placeholder">
                <el-icon class="is-loading"><Loading /></el-icon>
              </div>
              <span class="thumb-num">{{ idx + 1 }}</span>
            </div>
          </div>
        </div>
      </transition>

      <!-- 左侧缩略图切换按钮（隐藏时浮动显示） -->
      <button v-if="!thumbVisible" class="thumb-toggle-btn" @click="thumbVisible = true" title="展开导航">
        <el-icon><Menu /></el-icon>
      </button>

      <!-- PDF 展示区 -->
      <div class="pdf-container" ref="pdfContainerEl">
        <div v-if="pdfLoading" class="pdf-state">
          <el-icon class="is-loading" size="44"><Loading /></el-icon>
          <p>PDF 加载中…</p>
        </div>
        <div v-else-if="pdfError" class="pdf-state pdf-state--error">
          <el-icon size="44"><CircleCloseFilled /></el-icon>
          <p>{{ pdfError }}</p>
          <el-button type="primary" plain @click="initPdf">重新加载</el-button>
        </div>
        <!-- 渲染区 -->
        <div v-else class="pdf-scroll-inner">
          <div
            class="pdf-page-wrap"
            :class="{ 'draw-cursor': drawMode }"
            @mousedown="onDrawMousedown"
          >
            <canvas ref="pdfCanvas" class="pdf-canvas" />
            <div ref="textLayerEl" class="textLayer" />
            <canvas ref="highlightCanvas" class="pdf-highlight-canvas" />
            <!-- 实时框选预览 -->
            <div
              v-if="drawState.drawing"
              class="draw-rect-preview"
              :style="drawState.previewStyle"
            />
            <!-- 备注叠加按钮（框选模式下隐藏） -->
            <template v-if="!drawMode">
              <button
                v-for="ann in currentNoteAnns"
                :key="'nb-' + ann.id"
                class="note-overlay-btn"
                :style="getNoteOverlayStyle(ann)"
                @click.stop="openNotePopup(ann)"
                title="查看备注"
              >
                <el-icon><EditPen /></el-icon>
              </button>
            </template>
          </div>
        </div>
      </div>

      <!-- 右侧标注面板 -->
      <transition name="slide-right">
        <div v-if="sidebarVisible" class="annotation-sidebar">
          <div class="sidebar-header">
            <span class="sidebar-title">我的标注</span>
            <button class="tb-btn tb-btn-sm sidebar-close-btn" @click="sidebarVisible = false">
              <el-icon><Close /></el-icon>
            </button>
          </div>

          <div class="sidebar-quick-add">
            <el-button
              size="small"
              :icon="Collection"
              type="warning"
              plain
              style="width:100%"
              @click="addBookmark"
            >为第 {{ currentPage }} 页添加书签</el-button>
          </div>

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
              :class="{ 'ann-item--active': activeAnnotationId === ann.id }"
              :style="{ borderLeft: `4px solid ${ann.highlightColor || '#409eff'}` }"
              @click="clickAnnotation(ann)"
            >
              <div class="ann-header">
                <el-tag :type="annTypeTag(ann.annotationType)" size="small">
                  {{ annTypeLabel(ann.annotationType) }}
                </el-tag>
                <span class="ann-page">第 {{ ann.pageNumber }} 页</span>
                <el-button
                  :icon="Delete"
                  text
                  circle
                  size="small"
                  @click.stop="deleteAnnotation(ann)"
                />
              </div>
              <div v-if="ann.selectedText" class="ann-quote">{{ ann.selectedText }}</div>
              <div v-if="ann.note" class="ann-note" v-html="ann.note" />
              <div v-if="ann.annotationType === 3" class="ann-qa">
                <div class="ann-q"><b>Q：</b>{{ ann.aiQuestion }}</div>
                <div class="ann-a"><b>A：</b>{{ ann.aiAnswer }}</div>
              </div>
            </div>
            <el-empty
              v-if="!annLoading && filteredAnnotations.length === 0"
              description="暂无标注"
              :image-size="60"
            />
          </div>
        </div>
      </transition>
    </div>

    <!-- 添加/编辑备注 Dialog -->
    <el-dialog v-model="noteDialog.visible" :title="noteDialog.editingId ? '编辑备注' : '添加备注'" width="520px" destroy-on-close>
      <el-form label-width="80px" label-position="left">
        <el-form-item label="页码">
          <el-input-number v-model="noteDialog.pageNumber" :min="1" controls-position="right" style="width:120px" />
        </el-form-item>
        <el-form-item v-if="noteDialog.positionRects" label="框选区域">
          <span style="color:#67c23a;font-size:13px">已框选第 {{ noteDialog.pageNumber }} 页区域</span>
        </el-form-item>
        <el-form-item label="重要程度">
          <div class="level-row">
            <button
              v-for="lv in noteLevels"
              :key="lv.color"
              type="button"
              class="level-btn"
              :class="{ active: noteDialog.color === lv.color }"
              :style="{ '--lv-color': lv.color }"
              @click="noteDialog.color = lv.color"
            >{{ lv.label }}</button>
          </div>
        </el-form-item>
        <el-form-item label="备注内容">
          <div class="rich-editor-wrap">
            <div class="rich-toolbar">
              <button type="button" class="rt-btn" @mousedown.prevent="execCmd('bold')" title="粗体"><b>B</b></button>
              <button type="button" class="rt-btn" @mousedown.prevent="execCmd('italic')" title="斜体"><i>I</i></button>
              <button type="button" class="rt-btn" @mousedown.prevent="execCmd('underline')" title="下划线"><u>U</u></button>
              <span class="rt-sep"></span>
              <button type="button" class="rt-btn" @mousedown.prevent="execCmd('insertUnorderedList')" title="无序列表">•</button>
              <button type="button" class="rt-btn" @mousedown.prevent="execCmd('insertOrderedList')" title="有序列表">1.</button>
              <button type="button" class="rt-btn" @mousedown.prevent="execCmd('removeFormat')" title="清除格式">✕</button>
            </div>
            <div
              ref="noteEditorEl"
              class="rich-editor"
              contenteditable="true"
              data-placeholder="请输入备注内容…"
            />
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="noteDialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="noteDialog.saving" @click="saveNote">保存备注</el-button>
      </template>
    </el-dialog>

    <!-- 备注查看弹窗（点击 PDF 高亮上的按钮触发） -->
    <el-dialog v-model="notePopup.visible" title="备注内容" width="440px" destroy-on-close>
      <div v-if="notePopup.ann" class="note-popup-view">
        <div class="note-popup-meta">
          <span
            class="note-level-badge"
            :style="{ background: notePopup.ann.highlightColor || '#FFEB3B' }"
          >{{ noteLevelLabel(notePopup.ann.highlightColor) }}</span>
          <span class="note-popup-page">第 {{ notePopup.ann.pageNumber }} 页</span>
        </div>
        <div class="note-popup-content" v-html="notePopup.ann.note" />
      </div>
      <template #footer>
        <el-button @click="notePopup.visible = false">关闭</el-button>
        <el-button type="primary" @click="editNoteFromPopup(notePopup.ann)">编辑</el-button>
      </template>
    </el-dialog>

    <!-- AI 问答 Drawer -->
    <el-drawer
      v-model="aiDrawer.visible"
      title="AI 智能分析"
      direction="rtl"
      size="440px"
      destroy-on-close
    >
      <div class="ai-drawer-body">
        <el-form label-width="80px" label-position="left">
          <el-form-item label="页码">
            <el-input-number v-model="aiDrawer.pageNumber" :min="1" controls-position="right" style="width:120px" />
          </el-form-item>
          <el-form-item v-if="aiDrawer.positionRects" label="框选区域">
            <span style="color:#34d399;font-size:13px">已框选第 {{ aiDrawer.pageNumber }} 页区域</span>
          </el-form-item>
          <el-form-item v-if="aiDrawer.imageBase64" label="截图预览">
            <div class="ai-image-preview">
              <img :src="'data:image/jpeg;base64,' + aiDrawer.imageBase64" alt="框选区域截图" />
            </div>
          </el-form-item>
          <el-form-item label="提取文字">
            <el-input
              v-model="aiDrawer.selectedText"
              type="textarea"
              :rows="3"
              placeholder="框选区域文字（自动提取或手动输入）"
            />
          </el-form-item>
          <el-form-item label="提问">
            <el-input
              v-model="aiDrawer.question"
              type="textarea"
              :rows="2"
              placeholder="输入你的问题（留空则让 AI 自由分析）"
            />
          </el-form-item>
        </el-form>
        <el-button
          type="primary"
          :icon="MagicStick"
          :loading="aiDrawer.loading"
          :disabled="!aiDrawer.selectedText.trim() && !aiDrawer.positionRects"
          style="width:100%;margin:8px 0 16px"
          @click="runAiAnalyze"
        >开始分析</el-button>

        <template v-if="aiDrawer.answer">
          <el-card class="ai-answer-card" shadow="never">
            <div class="ai-answer-content" v-html="renderMarkdown(aiDrawer.answer)" />
          </el-card>
          <div style="text-align:right;margin-top:12px">
            <el-button
              size="small"
              type="success"
              :icon="Check"
              :loading="aiDrawer.saving"
              @click="saveAiAnnotation"
            >保存到标注</el-button>
          </div>
        </template>
      </div>
    </el-drawer>

    <!-- 框选操作浮窗（框选完成后显示） -->
    <div
      v-if="drawPopup.visible"
      class="draw-action-popup"
      :style="drawPopup.style"
      @mousedown.prevent
    >
      <div class="draw-popup-tip">选择操作</div>
      <div class="draw-popup-actions">
        <button class="draw-popup-btn draw-popup-bm" @click="confirmDrawAs('bookmark')">
          <el-icon><Collection /></el-icon><span>书签</span>
        </button>
        <button class="draw-popup-btn draw-popup-note" @click="confirmDrawAs('note')">
          <el-icon><EditPen /></el-icon><span>备注</span>
        </button>
        <button class="draw-popup-btn draw-popup-ai" @click="confirmDrawAs('ai')">
          <el-icon><MagicStick /></el-icon><span>AI 解析</span>
        </button>
        <button class="draw-popup-cancel" @click="cancelDraw" title="取消">
          <el-icon><Close /></el-icon>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, reactive, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  ArrowLeft, ArrowRight, Notebook, Loading, CircleCloseFilled,
  Delete, Close, Collection, EditPen, MagicStick, Check,
  FullScreen, ScaleToOriginal, Minus, Plus, Edit, Menu, Clock,
} from '@element-plus/icons-vue'
import * as pdfjsLib from 'pdfjs-dist'
import 'pdfjs-dist/web/pdf_viewer.css'
import { booksApi } from '@/api/books'

// ── PDF.js Worker ───────────────────────────────────────────────
pdfjsLib.GlobalWorkerOptions.workerSrc = new URL(
  'pdfjs-dist/build/pdf.worker.min.mjs',
  import.meta.url,
).href

const route  = useRoute()
const bookId = route.params.id

// ── State ─────────────────────────────────────────────────────
const bookInfo    = ref(null)
const pdfLoading  = ref(true)
const pdfError    = ref('')
const currentPage = ref(1)
const totalPages  = ref(0)

const sidebarVisible     = ref(false)
const thumbVisible       = ref(true)
const annotations        = ref([])
const annLoading         = ref(false)
const activeTab          = ref('all')
const activeAnnotationId = ref(null)

const highlightColors = ['#FFEB3B', '#A5D6A7', '#CE93D8', '#90CAF9', '#FFCCBC']

// 备注三级颜色
const noteLevels = [
  { label: '一般',   color: '#FFEB3B' },
  { label: '重要',   color: '#FF9800' },
  { label: '非常重要', color: '#ef4444' },
]

// 上次阅读位置
const lastReadPos = ref(null)

// 备注查看弹窗（点击 PDF 高亮按钮时显示）
const notePopup = reactive({ visible: false, ann: null })

// 当前页面的备注标注（用于渲染叠加按钮）
const currentNoteAnns = computed(() =>
  annotations.value.filter(a =>
    a.pageNumber === currentPage.value && a.annotationType === 2 && a.positionJson
  )
)

// ── 缩放状态：'fit' | 'width' | number ───────────────────────
const zoomMode = ref('fit')

// ── 框选标注状态 ─────────────────────────────────────────────
const drawMode = ref(false)
const drawState = reactive({
  drawing: false,
  startX: 0,
  startY: 0,
  previewStyle: {},
})

// 框选操作浮窗
const drawPopup = reactive({
  visible: false,
  style: {},
  rect: null,
})

// ── 缩略图 ───────────────────────────────────────────────────
const thumbnails = ref([])

// ── DOM Refs ──────────────────────────────────────────────────
const pdfContainerEl  = ref(null)
const pdfCanvas       = ref(null)
const textLayerEl     = ref(null)
const highlightCanvas = ref(null)
const thumbListEl     = ref(null)
const noteEditorEl    = ref(null)

// ── PDF.js internals ────────────────────────────────────────────
let pdfDoc      = null
let renderTask  = null
let renderScale = 1.5

// ── 缩放控制 ──────────────────────────────────────────────────
function setZoom(mode) {
  zoomMode.value = mode
  renderPage(currentPage.value)
}

function zoomIn() {
  const newScale = Math.min(4, parseFloat((renderScale + 0.25).toFixed(2)))
  zoomMode.value = newScale
  renderPage(currentPage.value)
}

function zoomOut() {
  const newScale = Math.max(0.5, parseFloat((renderScale - 0.25).toFixed(2)))
  zoomMode.value = newScale
  renderPage(currentPage.value)
}

// ── PDF Init & Render ─────────────────────────────────────────────────────────
async function initPdf() {
  pdfLoading.value = true
  pdfError.value   = ''
  try {
    bookInfo.value = await booksApi.getBook(bookId)

    // 使用 HTTP Range 分片加载：PDF.js 会自动按需请求内容，无需预先下载整个文件
    const pdfConfig = booksApi.getPdfConfig(bookId)
    const loadingTask = pdfjsLib.getDocument({
      ...pdfConfig,
      rangeChunkSize: 65536,   // 每次 Range 请求拉取 64 KB
      disableAutoFetch: false, // 允许后台预取
      disableStream: false,    // 允许流式处理
    })

    pdfDoc = await loadingTask.promise
    totalPages.value = pdfDoc.numPages
    thumbnails.value = new Array(pdfDoc.numPages).fill(null)
    pdfLoading.value = false
    await nextTick()
    await renderPage(currentPage.value)
    generateThumbnails()
  } catch (e) {
    pdfError.value   = e?.message || 'PDF 加载失败'
    pdfLoading.value = false
  }
}

function computeScale(baseVp) {
  const containerW = (pdfContainerEl.value?.clientWidth  ?? 900) - 64
  const containerH = (pdfContainerEl.value?.clientHeight ?? 700) - 64
  if (zoomMode.value === 'fit') {
    return Math.min(containerW / baseVp.width, containerH / baseVp.height, 2.5)
  }
  if (zoomMode.value === 'width') {
    return Math.min(containerW / baseVp.width, 2.5)
  }
  // numeric
  return typeof zoomMode.value === 'number' ? zoomMode.value : 1
}

async function renderPage(num) {
  if (!pdfDoc || !pdfCanvas.value) return
  if (renderTask) {
    try { renderTask.cancel() } catch { /* ignore */ }
    renderTask = null
  }
  const page   = await pdfDoc.getPage(num)
  const baseVp = page.getViewport({ scale: 1 })
  const scale  = computeScale(baseVp)
  renderScale  = scale
  const viewport = page.getViewport({ scale })

  const canvas  = pdfCanvas.value
  canvas.width  = viewport.width
  canvas.height = viewport.height

  const hc  = highlightCanvas.value
  hc.width  = viewport.width
  hc.height = viewport.height
  clearHighlight()

  renderTask = page.render({ canvasContext: canvas.getContext('2d'), viewport })
  await renderTask.promise
  renderTask = null
  await buildTextLayer(page, viewport, scale)
  drawPageAnnotations(num)
}

async function buildTextLayer(page, viewport, scale) {
  const tlDiv = textLayerEl.value
  if (!tlDiv) return
  tlDiv.innerHTML = ''
  tlDiv.style.setProperty('--scale-factor', String(scale))
  if (typeof pdfjsLib.TextLayer === 'function') {
    const tl = new pdfjsLib.TextLayer({
      textContentSource: page.streamTextContent({ includeMarkedContent: true }),
      container: tlDiv,
      viewport,
    })
    await tl.render()
  } else {
    const textContent = await page.getTextContent()
    const task = pdfjsLib.renderTextLayer({ textContent, container: tlDiv, viewport, textDivs: [] })
    await (task.promise ?? task)
  }
}

// ── 缩略图生成 ───────────────────────────────────────────────
async function generateThumbnails() {
  if (!pdfDoc) return
  const thumbScale = 0.15
  for (let i = 1; i <= pdfDoc.numPages; i++) {
    try {
      const page     = await pdfDoc.getPage(i)
      const viewport = page.getViewport({ scale: thumbScale })
      const canvas   = document.createElement('canvas')
      canvas.width   = viewport.width
      canvas.height  = viewport.height
      await page.render({ canvasContext: canvas.getContext('2d'), viewport }).promise
      thumbnails.value[i - 1] = canvas.toDataURL('image/jpeg', 0.7)
    } catch { /* ignore */ }
  }
}

// ── 翻页 ─────────────────────────────────────────────────────
function prevPage() { if (currentPage.value > 1) currentPage.value-- }
function nextPage() { if (currentPage.value < totalPages.value) currentPage.value++ }
function goPage(n) {
  if (n >= 1 && n <= totalPages.value) {
    currentPage.value = n
    nextTick(() => {
      const el = thumbListEl.value?.querySelector(`.thumb-item:nth-child(${n})`)
      el?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
    })
  }
}

watch(currentPage, (num) => {
  activeAnnotationId.value = null
  clearHighlight()
  renderPage(num)
  // 自动保存阅读位置
  const pos = { pageNumber: num, savedAt: new Date().toISOString() }
  localStorage.setItem(`lastRead_${bookId}`, JSON.stringify(pos))
  lastReadPos.value = pos
})

let resizeTimer = null
function onResize() {
  clearTimeout(resizeTimer)
  resizeTimer = setTimeout(() => {
    if (pdfDoc && (zoomMode.value === 'fit' || zoomMode.value === 'width')) {
      renderPage(currentPage.value)
    }
  }, 300)
}

// ── 上次阅读位置 ─────────────────────────────────────────────
function loadLastReadPos() {
  const raw = localStorage.getItem(`lastRead_${bookId}`)
  if (raw) {
    try { lastReadPos.value = JSON.parse(raw) } catch { /* ignore */ }
  }
}

function jumpToLastRead() {
  if (lastReadPos.value) goPage(lastReadPos.value.pageNumber)
}

function formatLastReadTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const now = new Date()
  const diffMs = now - d
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return '刚刚'
  if (diffMin < 60) return `${diffMin}分钟前`
  const diffH = Math.floor(diffMin / 60)
  if (diffH < 24) return `${diffH}小时前`
  return `${d.getMonth() + 1}/${d.getDate()}`
}

// ── 框选标注逻辑 ──────────────────────────────────────────────
let drawWrapRect = null
let ignoreNextClick = false  // 防止 mouseup 后的 click 立刻关闭 popup

function toggleDrawMode() {
  drawMode.value = !drawMode.value
  if (!drawMode.value) {
    drawState.drawing = false
    cancelDraw()
  }
}

function onDrawMousedown(e) {
  if (!drawMode.value) return
  e.preventDefault()
  drawWrapRect = e.currentTarget.getBoundingClientRect()
  const sx = e.clientX - drawWrapRect.left
  const sy = e.clientY - drawWrapRect.top
  drawState.drawing = true
  drawState.startX  = sx
  drawState.startY  = sy
  drawState.previewStyle = { left: `${sx}px`, top: `${sy}px`, width: '0px', height: '0px' }
  document.addEventListener('mousemove', onDocDrawMove)
  document.addEventListener('mouseup',   onDocDrawUp)
}

function onDocDrawMove(e) {
  if (!drawState.drawing || !drawWrapRect) return
  const cx = Math.max(0, Math.min(e.clientX - drawWrapRect.left, drawWrapRect.width))
  const cy = Math.max(0, Math.min(e.clientY - drawWrapRect.top,  drawWrapRect.height))
  const x  = Math.min(cx, drawState.startX)
  const y  = Math.min(cy, drawState.startY)
  const w  = Math.abs(cx - drawState.startX)
  const h  = Math.abs(cy - drawState.startY)
  drawState.previewStyle = { left: `${x}px`, top: `${y}px`, width: `${w}px`, height: `${h}px` }
}

function onDocDrawUp(e) {
  document.removeEventListener('mousemove', onDocDrawMove)
  document.removeEventListener('mouseup',   onDocDrawUp)
  if (!drawState.drawing) return
  drawState.drawing = false
  if (!drawWrapRect) return
  const cx = Math.max(0, Math.min(e.clientX - drawWrapRect.left, drawWrapRect.width))
  const cy = Math.max(0, Math.min(e.clientY - drawWrapRect.top,  drawWrapRect.height))
  const x  = Math.min(cx, drawState.startX)
  const y  = Math.min(cy, drawState.startY)
  const w  = Math.abs(cx - drawState.startX)
  const h  = Math.abs(cy - drawState.startY)
  if (w < 5 || h < 5) return
  const rect = {
    x:      x / renderScale,
    y:      y / renderScale,
    width:  w / renderScale,
    height: h / renderScale,
  }
  drawHighlightRect(rect, 'rgba(96,165,250,0.3)')
  drawPopup.rect   = rect
  drawPopup.style  = {
    left: `${Math.min(e.clientX - 110, window.innerWidth - 270)}px`,
    top:  `${Math.max(e.clientY - 80, 64)}px`,
  }
  drawPopup.visible = true
  ignoreNextClick = true   // 阻止随后触发的 click 事件关闭 popup
}

async function confirmDrawAs(type) {
  drawPopup.visible = false
  const rect = drawPopup.rect
  if (!rect) return
  if (type === 'bookmark') {
    try {
      const ann = await booksApi.createAnnotation(bookId, {
        pageNumber:     currentPage.value,
        selectedText:   null,
        note:           null,
        annotationType: 1,
        highlightColor: '#FFEB3B',
        positionJson:   JSON.stringify({ rects: [rect] }),
      })
      annotations.value.unshift(ann)
      drawPageAnnotations(currentPage.value)
      ElMessage.success(`第 ${currentPage.value} 页框选书签已添加`)
      sidebarVisible.value = true
    } catch {
      ElMessage.error('添加书签失败')
    }
  } else if (type === 'note') {
    noteDialog.pageNumber    = currentPage.value
    noteDialog.selectedText  = ''
    noteDialog.positionRects = [rect]
    noteDialog.note          = ''
    noteDialog.color         = '#FFEB3B'
    noteDialog.editingId     = null
    noteDialog.visible       = true
  } else if (type === 'ai') {
    // 先打开 drawer 给用户反馈，再异步提取文字和截图
    aiDrawer.pageNumber    = currentPage.value
    aiDrawer.selectedText  = '提取中…'
    aiDrawer.positionRects = [rect]
    aiDrawer.imageBase64   = null
    aiDrawer.question      = ''
    aiDrawer.answer        = ''
    aiDrawer.visible       = true
    try {
      const [text, imgB64] = await Promise.all([
        extractTextFromRect(rect),
        captureSelectionAsBase64(rect),
      ])
      aiDrawer.selectedText = text || ''
      aiDrawer.imageBase64  = imgB64
    } catch {
      aiDrawer.selectedText = ''
    }
  }
}

function cancelDraw() {
  drawPopup.visible = false
  drawPopup.rect    = null
  drawPageAnnotations(currentPage.value)
}

function onDocClickOutside(e) {
  if (ignoreNextClick) {
    ignoreNextClick = false
    return
  }
  if (drawPopup.visible && !e.target.closest('.draw-action-popup')) {
    cancelDraw()
  }
}

function onKeyDown(e) {
  if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return
  if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
    e.preventDefault(); nextPage()
  } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
    e.preventDefault(); prevPage()
  }
}

// ── Highlight ─────────────────────────────────────────────────────────────────

// ── OCR + 截图辅助 ────────────────────────────────────────────
/** 将 PDF 画布的选中矩形区域截成 base64 JPEG（无 data: 前缀） */
async function captureSelectionAsBase64(rect) {
  const canvas = pdfCanvas.value
  if (!canvas) return null
  const sx = Math.round(rect.x * renderScale)
  const sy = Math.round(rect.y * renderScale)
  const sw = Math.round(rect.width  * renderScale)
  const sh = Math.round(rect.height * renderScale)
  if (sw < 2 || sh < 2) return null
  const off = document.createElement('canvas')
  off.width  = sw
  off.height = sh
  off.getContext('2d').drawImage(canvas, sx, sy, sw, sh, 0, 0, sw, sh)
  return off.toDataURL('image/jpeg', 0.92).split(',')[1]
}

/** 从 PDF.js 文本层提取选区内的文字
 *  优先使用 textLayer DOM（比 getTextContent 坐标转换更可靠），
 *  回退到 getTextContent + 仿射变换坐标计算。
 */
async function extractTextFromRect(rect) {
  // ── 方案 A：从已渲染的 textLayer DOM 直接提取 ─────────────────────────
  if (textLayerEl.value) {
    const tlDiv = textLayerEl.value
    const tlRect = tlDiv.getBoundingClientRect()
    const canvasEl = pdfCanvas.value
    if (canvasEl && tlRect.width > 0) {
      // 选区在 canvas 坐标系（像素）
      const sx = rect.x * renderScale
      const sy = rect.y * renderScale
      const sw = rect.width  * renderScale
      const sh = rect.height * renderScale

      const spans = tlDiv.querySelectorAll('span[role="presentation"], span')
      const words = []
      for (const span of spans) {
        if (!span.textContent?.trim()) continue
        const sr = span.getBoundingClientRect()
        const canvasRect = canvasEl.getBoundingClientRect()
        // 将 span 转换到 canvas 坐标系
        const spx = sr.left - canvasRect.left
        const spy = sr.top  - canvasRect.top
        const spw = sr.width
        const sph = sr.height
        // 交叉判断：两矩形相交
        if (spx + spw < sx - 4 || spx > sx + sw + 4) continue
        if (spy + sph < sy - 4 || spy > sy + sh + 4) continue
        words.push(span.textContent)
      }
      if (words.length > 0) return words.join(' ').trim()
    }
  }

  // ── 方案 B：回退到 PDF.js getTextContent + viewport transform ──────────
  if (!pdfDoc) return ''
  try {
    const page     = await pdfDoc.getPage(currentPage.value)
    const viewport = page.getViewport({ scale: 1 })
    const textContent = await page.getTextContent()
    const words = []
    for (const item of textContent.items) {
      if (!item.str?.trim()) continue
      // 应用仿射变换：[a, b, c, d, tx, ty]
      const [a, b, c, d, tx, ty] = item.transform
      // 将 PDF 用户空间坐标转换到 viewport 坐标
      const vpPt = pdfjsLib.Util.applyTransform([0, 0], viewport.transform)
      const pt   = pdfjsLib.Util.applyTransform([tx, ty], viewport.transform)
      const vpX  = pt[0]
      const vpY  = pt[1]
      // item.height 是字体点数，近似为行高
      const itemH = Math.abs((item.height || 12) * (d || 1))
      if (
        vpX >= rect.x - 8 && vpX <= rect.x + rect.width  + 8 &&
        vpY >= rect.y - itemH - 4 && vpY <= rect.y + rect.height + 4
      ) {
        words.push(item.str)
      }
    }
    return words.join(' ').trim()
  } catch {
    return ''
  }
}

function drawHighlight(positionJson, color = 'rgba(255,235,59,0.45)') {
  if (!positionJson || !highlightCanvas.value) return
  let pos
  try { pos = JSON.parse(positionJson) } catch { return }
  const rects = pos?.rects ?? []
  if (rects.length === 0) return
  const ctx = highlightCanvas.value.getContext('2d')
  ctx.clearRect(0, 0, highlightCanvas.value.width, highlightCanvas.value.height)
  ctx.fillStyle = color
  for (const r of rects) {
    ctx.fillRect(r.x * renderScale, r.y * renderScale, r.width * renderScale, r.height * renderScale)
  }
}

function drawHighlightRect(rect, color) {
  if (!highlightCanvas.value) return
  const ctx = highlightCanvas.value.getContext('2d')
  ctx.clearRect(0, 0, highlightCanvas.value.width, highlightCanvas.value.height)
  ctx.fillStyle = color
  ctx.fillRect(rect.x * renderScale, rect.y * renderScale, rect.width * renderScale, rect.height * renderScale)
}

function drawPageAnnotations(pageNum) {
  if (!highlightCanvas.value) return
  const ctx = highlightCanvas.value.getContext('2d')
  ctx.clearRect(0, 0, highlightCanvas.value.width, highlightCanvas.value.height)
  const pageAnns = annotations.value.filter(a => a.pageNumber === pageNum && a.positionJson)
  for (const ann of pageAnns) {
    try {
      const pos   = JSON.parse(ann.positionJson)
      const rects = pos?.rects ?? []
      const fillColor   = ann.highlightColor ? hexToRgba(ann.highlightColor, 0.35) : 'rgba(255,235,59,0.35)'
      const strokeColor = ann.highlightColor || '#FFEB3B'
      ctx.fillStyle   = fillColor
      ctx.strokeStyle = strokeColor
      ctx.lineWidth   = 1.5
      ctx.setLineDash([5, 3])
      for (const r of rects) {
        ctx.fillRect(r.x * renderScale, r.y * renderScale, r.width * renderScale, r.height * renderScale)
        ctx.strokeRect(r.x * renderScale + 0.75, r.y * renderScale + 0.75, r.width * renderScale - 1.5, r.height * renderScale - 1.5)
      }
      ctx.setLineDash([])
    } catch { /* ignore */ }
  }
}

function clearHighlight() {
  const hc = highlightCanvas.value
  if (hc) hc.getContext('2d').clearRect(0, 0, hc.width, hc.height)
}

function hexToRgba(hex, alpha) {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r},${g},${b},${alpha})`
}

// ── 标注列表加载 ──────────────────────────────────────────────
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

function annTypeLabel(t) { return { 1: '书签', 2: '备注', 3: 'AI问答' }[t] || '未知' }
function annTypeTag(t)   { return { 1: 'warning', 2: 'success', 3: 'primary' }[t] || 'info' }

async function clickAnnotation(ann) {
  activeAnnotationId.value = ann.id
  if (ann.pageNumber !== currentPage.value) {
    currentPage.value = ann.pageNumber
    await nextTick()
    await new Promise(r => setTimeout(r, 450))
  }
  if (!highlightCanvas.value) return
  const ctx = highlightCanvas.value.getContext('2d')
  ctx.clearRect(0, 0, highlightCanvas.value.width, highlightCanvas.value.height)
  // 先把当前页所有标注用低透明度绘制
  const pageAnns = annotations.value.filter(a => a.pageNumber === ann.pageNumber && a.positionJson)
  for (const a of pageAnns) {
    try {
      const pos = JSON.parse(a.positionJson)
      const rects = pos?.rects ?? []
      const isActive = a.id === ann.id
      const alpha = isActive ? 0.6 : 0.2
      ctx.fillStyle   = a.highlightColor ? hexToRgba(a.highlightColor, alpha) : `rgba(255,235,59,${alpha})`
      ctx.strokeStyle = a.highlightColor || '#FFEB3B'
      for (const r of rects) {
        ctx.fillRect(r.x * renderScale, r.y * renderScale, r.width * renderScale, r.height * renderScale)
      }
      if (isActive) {
        // 选中标注：实线描边，更突出
        ctx.lineWidth = 2
        ctx.setLineDash([])
        for (const r of rects) {
          ctx.strokeRect(
            r.x * renderScale + 1, r.y * renderScale + 1,
            r.width * renderScale - 2, r.height * renderScale - 2
          )
        }
      } else {
        // 非选中标注：虚线描边
        ctx.lineWidth = 1.5
        ctx.setLineDash([5, 3])
        for (const r of rects) {
          ctx.strokeRect(
            r.x * renderScale + 0.75, r.y * renderScale + 0.75,
            r.width * renderScale - 1.5, r.height * renderScale - 1.5
          )
        }
        ctx.setLineDash([])
      }
    } catch { /* ignore */ }
  }
  // 没有位置信息的标注，清空高亮
  if (!ann.positionJson) clearHighlight()
}

async function deleteAnnotation(ann) {
  await ElMessageBox.confirm('确定删除该标注？', '确认', { type: 'warning' })
  await booksApi.deleteAnnotation(bookId, ann.id)
  annotations.value = annotations.value.filter(a => a.id !== ann.id)
  if (activeAnnotationId.value === ann.id) { activeAnnotationId.value = null; clearHighlight() }
  ElMessage.success('已删除')
}

// ── 书签 ──────────────────────────────────────────────────────
async function addBookmark() {
  try {
    const ann = await booksApi.createAnnotation(bookId, {
      pageNumber:     currentPage.value,
      selectedText:   null,
      note:           null,
      annotationType: 1,
      highlightColor: '#FFEB3B',
      positionJson:   null,
    })
    annotations.value.unshift(ann)
    ElMessage.success(`第 ${currentPage.value} 页书签已添加`)
    sidebarVisible.value = true
  } catch {
    ElMessage.error('添加书签失败')
  }
}

// ── Note Dialog ───────────────────────────────────────────────
const noteDialog = reactive({
  visible: false, pageNumber: 1, selectedText: '', note: '',
  color: '#FFEB3B', saving: false, positionRects: null, editingId: null,
})

function openNoteDialog() {
  noteDialog.pageNumber    = currentPage.value
  noteDialog.selectedText  = ''
  noteDialog.positionRects = null
  noteDialog.note          = ''
  noteDialog.color         = '#FFEB3B'
  noteDialog.editingId     = null
  noteDialog.visible       = true
}

function execCmd(cmd) {
  noteEditorEl.value?.focus()
  document.execCommand(cmd, false)
}

async function saveNote() {
  const html = noteEditorEl.value?.innerHTML?.trim() ?? ''
  if (!html || html === '<br>') { ElMessage.warning('请输入备注内容'); return }
  noteDialog.saving = true
  try {
    const positionJson = noteDialog.positionRects
      ? JSON.stringify({ rects: noteDialog.positionRects })
      : null
    const payload = {
      pageNumber:     noteDialog.pageNumber,
      selectedText:   noteDialog.selectedText || null,
      note:           html,
      annotationType: 2,
      highlightColor: noteDialog.color,
      positionJson,
    }
    if (noteDialog.editingId) {
      const ann = await booksApi.updateAnnotation(bookId, noteDialog.editingId, payload)
      const idx = annotations.value.findIndex(a => a.id === noteDialog.editingId)
      if (idx >= 0) annotations.value[idx] = ann
    } else {
      const ann = await booksApi.createAnnotation(bookId, payload)
      annotations.value.unshift(ann)
    }
    noteDialog.visible = false
    drawPageAnnotations(currentPage.value)
    ElMessage.success(noteDialog.editingId ? '备注已更新' : '备注已保存')
    sidebarVisible.value = true
  } catch {
    ElMessage.error('保存失败')
  } finally {
    noteDialog.saving = false
  }
}

// 打开/关闭 noteDialog 时初始化富文本编辑器内容
watch(() => noteDialog.visible, async (v) => {
  if (v) {
    await nextTick()
    if (noteEditorEl.value) {
      noteEditorEl.value.innerHTML = noteDialog.note || ''
      nextTick(() => noteEditorEl.value?.focus())
    }
  } else {
    drawPageAnnotations(currentPage.value)
  }
})

// ── 备注查看 / 编辑弹窗 ─────────────────────────────────────
function openNotePopup(ann) {
  notePopup.ann     = ann
  notePopup.visible = true
}

function editNoteFromPopup(ann) {
  if (!ann) return
  notePopup.visible    = false
  noteDialog.pageNumber    = ann.pageNumber
  noteDialog.selectedText  = ann.selectedText || ''
  noteDialog.note          = ann.note || ''
  noteDialog.color         = ann.highlightColor || '#FFEB3B'
  noteDialog.positionRects = ann.positionJson ? JSON.parse(ann.positionJson).rects : null
  noteDialog.editingId     = ann.id
  noteDialog.visible       = true
}

function getNoteOverlayStyle(ann) {
  try {
    const pos  = JSON.parse(ann.positionJson)
    const rect = pos?.rects?.[0]
    if (!rect) return { display: 'none' }
    return {
      position: 'absolute',
      left: `${rect.x * renderScale}px`,
      top:  `${rect.y * renderScale}px`,
    }
  } catch {
    return { display: 'none' }
  }
}

function noteLevelLabel(color) {
  return noteLevels.find(lv => lv.color === color)?.label ?? '备注'
}

// ── AI Drawer ──────────────────────────────────────────────────
const aiDrawer = reactive({
  visible: false, pageNumber: 1, selectedText: '', question: '',
  answer: '', loading: false, saving: false, positionRects: null, imageBase64: null,
})

// 取消AI问答时清空预览
watch(() => aiDrawer.visible, (v) => {
  if (!v) drawPageAnnotations(currentPage.value)
})

function openAiDrawer() {
  aiDrawer.pageNumber    = currentPage.value
  aiDrawer.selectedText  = ''
  aiDrawer.positionRects = null
  aiDrawer.imageBase64   = null
  aiDrawer.question      = ''
  aiDrawer.answer        = ''
  aiDrawer.visible       = true
}

async function runAiAnalyze() {
  if (!aiDrawer.selectedText.trim() && !aiDrawer.positionRects && !aiDrawer.imageBase64) return
  aiDrawer.loading = true
  aiDrawer.answer  = ''
  try {
    const res = await booksApi.aiAnalyze(bookId, {
      selectedText: aiDrawer.selectedText || null,
      question:     aiDrawer.question || null,
      imageBase64:  aiDrawer.imageBase64 || null,
    })
    aiDrawer.answer = res.answer || res
  } catch (err) {
    const msg = err?.response?.data?.error
      || err?.response?.data?.message
      || err?.message
      || 'AI 分析失败，请检查 AI 配置或稍后重试'
    ElMessage.error(msg)
  } finally {
    aiDrawer.loading = false
  }
}

async function saveAiAnnotation() {
  aiDrawer.saving = true
  try {
    const positionJson = aiDrawer.positionRects
      ? JSON.stringify({ rects: aiDrawer.positionRects })
      : null
    const ann = await booksApi.createAnnotation(bookId, {
      pageNumber:     aiDrawer.pageNumber,
      selectedText:   aiDrawer.selectedText,
      note:           null,
      annotationType: 3,
      aiQuestion:     aiDrawer.question || '请分析这段文字',
      aiAnswer:       aiDrawer.answer,
      highlightColor: '#CE93D8',
      positionJson,
    })
    annotations.value.unshift(ann)
    ElMessage.success('已保存到标注')
    sidebarVisible.value = true
    aiDrawer.visible     = false
  } catch {
    ElMessage.error('保存失败')
  } finally {
    aiDrawer.saving = false
  }
}

function renderMarkdown(text) {
  if (!text) return ''
  return text
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\n/g, '<br>')
}

// ── Lifecycle ──────────────────────────────────────────────────
onMounted(() => {
  initPdf()
  loadAnnotations()
  loadLastReadPos()
  window.addEventListener('resize', onResize)
  document.addEventListener('click', onDocClickOutside)
  document.addEventListener('keydown', onKeyDown)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  document.removeEventListener('click', onDocClickOutside)
  document.removeEventListener('keydown', onKeyDown)
  document.removeEventListener('mousemove', onDocDrawMove)
  document.removeEventListener('mouseup', onDocDrawUp)
  clearTimeout(resizeTimer)
  if (renderTask) { try { renderTask.cancel() } catch { /* noop */ } }
  if (pdfDoc) { pdfDoc.destroy(); pdfDoc = null }
})
</script>

<style scoped>
/* ── 整体布局 ───────────────────────────────────────── */
.reader-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: #374151;
  overflow: hidden;
}

/* ── 工具栏 ─────────────────────────────────────────── */
.reader-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #1e293b;
  padding: 0 16px;
  height: 52px;
  flex-shrink: 0;
  border-bottom: 1px solid rgba(255,255,255,0.08);
  box-shadow: 0 2px 12px rgba(0,0,0,.3);
  z-index: 10;
}

/* 工具栏通用按钮：默认深色，hover浅色 */
.tb-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 6px 12px;
  border: 1px solid rgba(255,255,255,0.18);
  border-radius: 6px;
  background: rgba(255,255,255,0.1);
  color: #e2e8f0;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.15s, color 0.15s, border-color 0.15s, box-shadow 0.15s;
  white-space: nowrap;
  user-select: none;
  line-height: 1.4;
  letter-spacing: 0.01em;
}
.tb-btn:hover:not(:disabled) {
  background: rgba(255,255,255,0.24);
  color: #ffffff;
  border-color: rgba(255,255,255,0.4);
  box-shadow: 0 1px 6px rgba(0,0,0,0.35);
}
.tb-btn.active {
  background: #2563eb;
  color: #fff;
  border-color: #3b82f6;
  box-shadow: 0 0 0 2px rgba(59,130,246,0.35);
}
.tb-btn:disabled {
  opacity: 0.28;
  cursor: not-allowed;
}
.tb-btn-sm {
  padding: 3px 7px;
  font-size: 12px;
}
.back-btn { font-weight: 500; }

/* 右侧标注面板中按钮颜色调整 */
.sidebar-close-btn {
  background: transparent;
  border-color: #e4e7ed;
  color: #909399;
}
.sidebar-close-btn:hover {
  background: #f5f7fa;
  color: #303133;
  border-color: #c0c4cc;
}

.book-title-bar {
  font-size: 14px;
  font-weight: 600;
  color: #e2e8f0;
  max-width: 280px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.toolbar-spacer { flex: 1; }
.toolbar-controls { display: flex; align-items: center; gap: 6px; }

/* 缩放组 */
.zoom-group { display: flex; align-items: center; gap: 4px; }
.zoom-label {
  font-size: 12px;
  font-weight: 700;
  color: #f1f5f9;
  min-width: 46px;
  text-align: center;
  user-select: none;
  letter-spacing: 0.03em;
  background: rgba(255,255,255,0.06);
  border-radius: 4px;
  padding: 3px 4px;
}

/* 翻页 */
.page-nav { display: flex; align-items: center; gap: 4px; }
.page-indicator-text {
  font-size: 13px;
  font-weight: 600;
  color: #f1f5f9;
  white-space: nowrap;
  min-width: 58px;
  text-align: center;
  background: rgba(255,255,255,0.06);
  border-radius: 4px;
  padding: 3px 6px;
}

/* Element Plus 分隔线 */
.reader-toolbar :deep(.el-divider--vertical) {
  border-color: rgba(255,255,255,0.15);
  height: 20px;
  margin: 0 2px;
}

/* ── 主体 ───────────────────────────────────────────── */
.reader-body {
  display: flex;
  flex: 1;
  overflow: hidden;
  position: relative;
}

/* ── 左侧缩略图面板 ──────────────────────────────────── */
.thumb-sidebar {
  width: 160px;
  flex-shrink: 0;
  background: #1a2436;
  display: flex;
  flex-direction: column;
  border-right: 1px solid rgba(255,255,255,0.08);
  overflow: hidden;
  z-index: 5;
}
.thumb-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 10px 8px;
  border-bottom: 1px solid rgba(255,255,255,0.08);
  flex-shrink: 0;
}
.thumb-title { font-size: 12px; font-weight: 600; color: #94a3b8; }
.thumb-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px 6px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.thumb-list::-webkit-scrollbar { width: 3px; }
.thumb-list::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.12); border-radius: 2px; }
.thumb-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  padding: 4px;
  border-radius: 5px;
  cursor: pointer;
  border: 2px solid transparent;
  transition: border-color 0.15s, background 0.15s;
}
.thumb-item:hover { background: rgba(255,255,255,0.07); }
.thumb-item.active { border-color: #3b82f6; background: rgba(59,130,246,0.1); }
.thumb-img {
  width: 100%;
  border-radius: 2px;
  display: block;
  background: #fff;
}
.thumb-placeholder {
  width: 100%;
  aspect-ratio: 0.7;
  background: #2d3f55;
  border-radius: 2px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #475569;
}
.thumb-num {
  font-size: 11px;
  color: #475569;
  line-height: 1;
}
.thumb-item.active .thumb-num { color: #93c5fd; }

/* 缩略图收起后的浮动展开按钮 */
.thumb-toggle-btn {
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  z-index: 6;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 48px;
  background: #1a2436;
  border: 1px solid rgba(255,255,255,0.12);
  border-left: none;
  border-radius: 0 6px 6px 0;
  color: #475569;
  cursor: pointer;
  transition: color 0.15s, background 0.15s;
  font-size: 13px;
}
.thumb-toggle-btn:hover { color: #cbd5e1; background: #283348; }

/* ── PDF 容器 ───────────────────────────────────────── */
.pdf-container {
  flex: 1;
  overflow: auto;
  position: relative;
  background: #374151;
}

/* ── PDF 内层滚动区（实现居中对齐，内容超出时允许滚动）── */
.pdf-scroll-inner {
  display: flex;
  min-height: 100%;
  align-items: center;
  justify-content: center;
  padding: 32px;
  box-sizing: border-box;
}

/* ── PDF 页面包装 ─────────────────────────────────────── */
.pdf-page-wrap {
  position: relative;
  display: inline-block;
  box-shadow: 0 4px 24px rgba(0,0,0,.4);
  line-height: 0;
  user-select: none;
  -webkit-user-select: none;
}
.pdf-page-wrap.draw-cursor { cursor: crosshair; }
.pdf-canvas { display: block; }

/* ── PDF.js 文本层（框选模式下不需要文字选中） ─────────── */
:deep(.textLayer) {
  position: absolute;
  inset: 0;
  overflow: clip;
  z-index: 1;
  user-select: none;
  -webkit-user-select: none;
  pointer-events: none;
}
:deep(.textLayer span),
:deep(.textLayer br) {
  user-select: none;
  -webkit-user-select: none;
  pointer-events: none;
}

/* ── 高亮叠加层 ─────────────────────────────────────── */
.pdf-highlight-canvas {
  position: absolute;
  inset: 0;
  pointer-events: none;
  mix-blend-mode: multiply;
  z-index: 2;
}

/* ── 框选预览矩形（类截图效果）─────────────────────────── */
.draw-rect-preview {
  position: absolute;
  border: 2px solid rgba(255,255,255,0.9);
  outline: 1px solid rgba(0,100,255,0.7);
  background: rgba(59,130,246,0.1);
  pointer-events: none;
  z-index: 10;
  border-radius: 1px;
  box-shadow: 0 0 0 9999px rgba(0,0,0,0.3);
}

/* ── 框选操作浮窗 ─────────────────────────────────────── */
.draw-action-popup {
  position: fixed;
  z-index: 9999;
  background: #0f172a;
  border: 1px solid rgba(255,255,255,0.16);
  border-radius: 10px;
  padding: 10px 12px 12px;
  box-shadow: 0 12px 40px rgba(0,0,0,0.6), 0 2px 8px rgba(0,0,0,0.4);
  min-width: 240px;
}
.draw-popup-tip {
  font-size: 11px;
  color: #475569;
  margin-bottom: 8px;
  text-align: center;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  font-weight: 600;
}
.draw-popup-actions {
  display: flex;
  gap: 6px;
  align-items: center;
}
.draw-popup-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 8px 10px;
  border-radius: 7px;
  border: 1px solid rgba(255,255,255,0.1);
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.15s, color 0.15s, transform 0.1s;
  white-space: nowrap;
  flex: 1;
  justify-content: center;
}
.draw-popup-btn:hover { transform: translateY(-1px); }
.draw-popup-bm   { background: rgba(251,191,36,0.12); color: #fbbf24; border-color: rgba(251,191,36,0.25); }
.draw-popup-bm:hover   { background: rgba(251,191,36,0.25); color: #fde68a; }
.draw-popup-note { background: rgba(52,211,153,0.12); color: #34d399; border-color: rgba(52,211,153,0.25); }
.draw-popup-note:hover { background: rgba(52,211,153,0.25); color: #6ee7b7; }
.draw-popup-ai   { background: rgba(167,139,250,0.12); color: #a78bfa; border-color: rgba(167,139,250,0.25); }
.draw-popup-ai:hover   { background: rgba(167,139,250,0.25); color: #c4b5fd; }
.draw-popup-cancel {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  border-radius: 7px;
  border: 1px solid rgba(255,255,255,0.1);
  background: rgba(255,255,255,0.05);
  color: #475569;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}
.draw-popup-cancel:hover { background: rgba(239,68,68,0.2); color: #f87171; border-color: rgba(239,68,68,0.3); }

/* ── 状态占位 ───────────────────────────────────────── */
.pdf-state {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
  color: #ccc;
  font-size: 15px;
}
.pdf-state--error { color: #f89898; }

/* ── 右侧标注面板 ───────────────────────────────────── */
.annotation-sidebar {
  width: 360px;
  background: #fff;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-left: 1px solid #e4e7ed;
  box-shadow: -2px 0 10px rgba(0,0,0,.06);
}
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 14px 8px;
  border-bottom: 1px solid #f0f0f0;
}
.sidebar-title { font-size: 15px; font-weight: 600; color: #303133; }
.sidebar-quick-add { padding: 10px 12px 2px; }
.ann-tabs { padding: 0 8px; flex-shrink: 0; }
.ann-list { flex: 1; overflow-y: auto; padding: 8px; }
.ann-item {
  padding: 10px 12px;
  margin-bottom: 8px;
  background: #fafafa;
  border-radius: 6px;
  cursor: pointer;
  transition: background .15s, box-shadow .15s;
}
.ann-item:hover { background: #f0f5ff; box-shadow: 0 1px 6px rgba(64,158,255,.12); }
.ann-item--active { background: #ecf5ff; box-shadow: 0 0 0 2px #409eff; }
.ann-header { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; }
.ann-page { font-size: 12px; color: #909399; flex: 1; }
.ann-quote {
  font-size: 13px;
  color: #555;
  padding: 5px 8px;
  background: #fffde7;
  border-radius: 4px;
  margin-bottom: 4px;
  font-style: italic;
  line-height: 1.5;
  border-left: 3px solid #ffe082;
}
.ann-note { font-size: 13px; color: #333; line-height: 1.6; }
.ann-qa  { font-size: 12px; line-height: 1.7; }
.ann-q   { color: #666; margin-bottom: 4px; }
.ann-a   { color: #303133; }

/* ── 颜色选择 ───────────────────────────────────────── */
.color-row { display: flex; gap: 10px; align-items: center; }
.color-dot {
  width: 24px; height: 24px; border-radius: 50%;
  cursor: pointer; border: 2px solid transparent;
  transition: transform .15s, border-color .15s;
}
.color-dot:hover, .color-dot.active { transform: scale(1.2); border-color: #606266; }

/* ── AI Drawer ──────────────────────────────────────── */
.ai-drawer-body { padding: 4px 4px 0; }
.ai-answer-card { border-radius: 8px; background: #f7f9fc; }
.ai-answer-content { font-size: 14px; line-height: 1.85; color: #303133; }

/* ── 上次阅读按钮 ─────────────────────────────────────────── */
.last-read-btn {
  flex-direction: column;
  align-items: flex-start;
  padding: 4px 10px;
  gap: 1px;
  line-height: 1.25;
}
.last-read-main {
  display: flex;
  align-items: center;
  gap: 5px;
}
.last-read-time {
  font-size: 10px;
  color: #94a3b8;
  font-weight: 400;
}

/* ── 备注叠加按钮 ─────────────────────────────────────────── */
.note-overlay-btn {
  position: absolute;
  z-index: 3;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 1px solid rgba(255,255,255,0.3);
  background: rgba(30, 41, 59, 0.82);
  color: #e2e8f0;
  font-size: 11px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  box-shadow: 0 1px 4px rgba(0,0,0,.4);
  transition: transform 0.15s, background 0.15s;
  transform: translate(-50%, -50%);
}
.note-overlay-btn:hover {
  background: #2563eb;
  transform: translate(-50%, -50%) scale(1.2);
}

/* ── 重要程度选择 ─────────────────────────────────────────── */
.level-row { display: flex; gap: 8px; }
.level-btn {
  padding: 5px 14px;
  border-radius: 20px;
  border: 2px solid var(--lv-color, #FFEB3B);
  background: transparent;
  color: #606266;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
  font-weight: 500;
}
.level-btn:hover { background: color-mix(in srgb, var(--lv-color) 20%, transparent); }
.level-btn.active {
  background: var(--lv-color);
  color: #1e293b;
  font-weight: 700;
}

/* ── 富文本编辑器 ─────────────────────────────────────────── */
.rich-editor-wrap {
  border: 1px solid #dcdfe6;
  border-radius: 6px;
  overflow: hidden;
  width: 100%;
}
.rich-toolbar {
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 5px 8px;
  background: #f5f7fa;
  border-bottom: 1px solid #e4e7ed;
}
.rt-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border: none;
  background: transparent;
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
  color: #606266;
  transition: background 0.12s;
}
.rt-btn:hover { background: #e0e5ee; }
.rt-sep { width: 1px; height: 18px; background: #dcdfe6; margin: 0 4px; }
.rich-editor {
  min-height: 100px;
  max-height: 220px;
  padding: 10px 12px;
  font-size: 14px;
  line-height: 1.7;
  outline: none;
  overflow-y: auto;
  color: #303133;
}
.rich-editor:empty::before {
  content: attr(data-placeholder);
  color: #c0c4cc;
  pointer-events: none;
}
.rich-editor ul, .rich-editor ol { padding-left: 20px; margin: 4px 0; }

/* ── 备注查看弹窗 ─────────────────────────────────────────── */
.note-popup-view { padding: 4px 0; }
.note-popup-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}
.note-level-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
  color: #1e293b;
}
.note-popup-page { font-size: 12px; color: #909399; }
.note-popup-content {
  font-size: 14px;
  line-height: 1.8;
  color: #303133;
  min-height: 60px;
  border-radius: 6px;
  padding: 12px;
  background: #fafafa;
  border: 1px solid #f0f0f0;
}
.note-popup-content ul, .note-popup-content ol { padding-left: 20px; }

/* ── AI 截图预览 ──────────────────────────────────────────── */
.ai-image-preview {
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  overflow: hidden;
  max-height: 160px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
}
.ai-image-preview img {
  max-width: 100%;
  max-height: 160px;
  display: block;
  object-fit: contain;
}

/* ── 过渡动画 ───────────────────────────────────────── */
.slide-left-enter-active,
.slide-left-leave-active {
  transition: width 0.22s ease, opacity 0.22s ease;
  overflow: hidden;
}
.slide-left-enter-from,
.slide-left-leave-to { width: 0 !important; opacity: 0; }

.slide-right-enter-active,
.slide-right-leave-active { transition: transform .25s ease; }
.slide-right-enter-from,
.slide-right-leave-to    { transform: translateX(100%); }
.ai-drawer-body {padding: 20px !important;}
</style>
