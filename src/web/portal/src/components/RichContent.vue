<template>
  <div class="rich-content" v-html="safeHtml"></div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  content: { type: String, default: '' }
})

const safeHtml = computed(() => sanitizeAndFormat(props.content || ''))

function sanitizeAndFormat(raw) {
  if (!raw) return ''

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
.rich-content {
  line-height: 1.7;
  word-break: break-word;
}

.rich-content :deep(svg),
.rich-content :deep(img),
.rich-content :deep(canvas) {
  max-width: 100%;
  height: auto;
}

.rich-content :deep(pre) {
  overflow: auto;
}
</style>
