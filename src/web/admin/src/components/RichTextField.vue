<template>
  <div class="rich-field">
    <div class="rich-toolbar">
      <el-button size="small" @click="insertTemplate('<strong>加粗文本</strong>')">加粗</el-button>
      <el-button size="small" @click="insertTemplate('<em>斜体文本</em>')">斜体</el-button>
      <el-button size="small" @click="insertTemplate('\\(x^2 + y^2 = z^2\\)')">行内公式</el-button>
      <el-button size="small" @click="insertTemplate('$$\\n\\frac{a}{b} = c\\n$$')">块公式</el-button>
      <el-button size="small" @click="insertTemplate('<svg width=&quot;320&quot; height=&quot;180&quot; viewBox=&quot;0 0 320 180&quot; xmlns=&quot;http://www.w3.org/2000/svg&quot;>\\n  <rect x=&quot;20&quot; y=&quot;20&quot; width=&quot;280&quot; height=&quot;140&quot; fill=&quot;none&quot; stroke=&quot;#666&quot; stroke-width=&quot;2&quot; />\\n  <line x1=&quot;20&quot; y1=&quot;160&quot; x2=&quot;300&quot; y2=&quot;20&quot; stroke=&quot;#0d6efd&quot; stroke-width=&quot;3&quot; />\\n</svg>')">
        插入 SVG 绘图
      </el-button>
    </div>

    <el-input
      ref="textareaRef"
      :model-value="modelValue"
      type="textarea"
      :rows="rows"
      :placeholder="placeholder"
      @update:model-value="emit('update:modelValue', $event)"
    />

    <div class="rich-preview-wrap">
      <div class="rich-preview-title">预览</div>
      <div class="rich-preview" v-html="previewHtml"></div>
    </div>

    <p class="rich-hint">
      支持 HTML/SVG 片段、LaTeX 公式标记（如 \(x^2\) 或 $$...$$），可粘贴几何绘图工具导出的 SVG。
    </p>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
  rows: { type: Number, default: 6 },
  placeholder: { type: String, default: '' }
})

const emit = defineEmits(['update:modelValue'])
const textareaRef = ref(null)

const previewHtml = computed(() => sanitizeAndFormat(props.modelValue))

function insertTemplate(fragment) {
  const textarea = textareaRef.value?.textarea
  if (!textarea) {
    emit('update:modelValue', `${props.modelValue}${fragment}`)
    return
  }

  const start = textarea.selectionStart ?? 0
  const end = textarea.selectionEnd ?? 0
  const next = `${props.modelValue.slice(0, start)}${fragment}${props.modelValue.slice(end)}`
  emit('update:modelValue', next)

  requestAnimationFrame(() => {
    textarea.focus()
    const cursor = start + fragment.length
    textarea.setSelectionRange(cursor, cursor)
  })
}

function sanitizeAndFormat(raw) {
  if (!raw) return '<p class="empty">暂无内容</p>'

  const parser = new DOMParser()
  const doc = parser.parseFromString(`<div>${raw}</div>`, 'text/html')

  doc.querySelectorAll('script,style,iframe,object,embed').forEach((el) => el.remove())

  doc.querySelectorAll('*').forEach((el) => {
    for (const attr of [...el.attributes]) {
      const name = attr.name.toLowerCase()
      const value = (attr.value || '').toLowerCase()
      if (name.startsWith('on')) el.removeAttribute(attr.name)
      if ((name === 'href' || name === 'src') && value.startsWith('javascript:')) {
        el.removeAttribute(attr.name)
      }
    }
  })

  const html = doc.body.innerHTML
  if (/<[^>]+>/.test(html)) return html

  return escapeHtml(raw).replace(/\n/g, '<br>')
}

function escapeHtml(text) {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}
</script>

<style scoped>
.rich-field {
  width: 100%;
}

.rich-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 8px;
}

.rich-preview-wrap {
  margin-top: 8px;
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  overflow: hidden;
}

.rich-preview-title {
  padding: 6px 10px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  background: var(--el-fill-color-light);
  border-bottom: 1px solid var(--el-border-color);
}

.rich-preview {
  padding: 10px;
  min-height: 80px;
  line-height: 1.7;
  color: var(--el-text-color-regular);
  max-height: 220px;
  overflow: auto;
}

.rich-preview :deep(svg) {
  max-width: 100%;
  height: auto;
}

.rich-preview :deep(.empty) {
  color: var(--el-text-color-placeholder);
}

.rich-hint {
  margin: 8px 0 0;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
</style>
