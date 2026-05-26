<template>
  <Teleport to="body">
    <Transition name="sc-popup-fade">
      <div v-if="visible" class="sc-overlay" @click="close">
        <div class="sc-popup" :style="popupStyle" @click.stop>
          <!-- 标题栏 -->
          <div class="sc-popup-header">
            <span>安全验证</span>
            <button class="sc-close-btn" title="取消" @click="close">✕</button>
          </div>

          <!-- 加载中 -->
          <div v-if="loading" class="sc-loading">
            <span class="sc-loading-dot" />
            <span class="sc-loading-dot" />
            <span class="sc-loading-dot" />
          </div>

          <!-- 主体 -->
          <template v-else-if="challenge">
            <div
              class="sc-image-wrap"
              :style="{ width: challenge.bgWidth + 'px', height: challenge.bgHeight + 'px' }"
            >
              <img :src="challenge.bgImage" class="sc-bg" draggable="false" />
              <img
                :src="challenge.pieceImage"
                class="sc-piece"
                draggable="false"
                :style="{
                  left: sliderVal + 'px',
                  top: challenge.pieceY + 'px',
                  width: challenge.pieceSize + 'px',
                  height: challenge.pieceSize + 'px',
                }"
              />
              <Transition name="sc-fade">
                <div v-if="status === 'success'" class="sc-mask sc-success">✓ 验证通过</div>
                <div
                  v-else-if="status === 'error'"
                  class="sc-mask sc-error"
                  :class="{ 'sc-shake': shaking }"
                >✕ 请重新拼合图片</div>
              </Transition>
              <button
                v-if="status !== 'success'"
                class="sc-refresh-btn"
                title="换一张"
                @click="loadChallenge"
              >↻</button>
            </div>

            <div class="sc-slider-wrap">
              <div v-if="status === 'success'" class="sc-success-bar">验证通过 ✓</div>
              <div v-else class="sc-track">
                <span class="sc-track-hint" :style="{ opacity: sliderVal > 8 ? 0 : 1 }">
                  向右滑动完成验证
                </span>
                <!-- 填充在前，滑块圆圈覆盖右侧端点（宽度取到圆心，避免蓝色露出圆圈右侧） -->
                <div class="sc-track-fill" :style="{ width: sliderVal + handleSize / 2 + 'px' }" />
                <div
                  class="sc-handle"
                  :class="{ 'sc-handle-verifying': verifying }"
                  :style="{ left: sliderVal + 'px' }"
                  @mousedown.prevent="startDrag"
                  @touchstart.prevent="startDrag"
                >
                  <span class="sc-handle-icon">{{ verifying ? '…' : '›' }}</span>
                </div>
              </div>
            </div>
          </template>

          <!-- 加载失败 -->
          <div v-else class="sc-error-tip">
            验证码加载失败
            <button class="sc-retry-btn" @click="loadChallenge">重试</button>
          </div>

          <!-- 向下箭头，指向触发按钮 -->
          <div class="sc-popup-arrow" />
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { onBeforeUnmount, ref } from 'vue'
import { authApi } from '@/api/auth'

// ── 状态 ────────────────────────────────────────────────────────────────────
const visible = ref(false)
const loading = ref(false)
const challenge = ref(null)
const sliderVal = ref(0)
const status = ref('')       // '' | 'success' | 'error'
const verifying = ref(false)
const shaking = ref(false)
const popupStyle = ref({})

const handleSize = 40

let pendingCallback = null
let anchorEl = null

// ── 定位（在按钮正上方） ─────────────────────────────────────────────────────
function computePosition() {
  if (!anchorEl) return
  const rect = anchorEl.getBoundingClientRect()
  const bgW = challenge.value?.bgWidth ?? 280
  const popupWidth = bgW + 28   // 14px padding × 2
  let left = rect.left + rect.width / 2 - popupWidth / 2
  left = Math.max(12, Math.min(left, window.innerWidth - popupWidth - 12))
  popupStyle.value = {
    left: left + 'px',
    bottom: (window.innerHeight - rect.top + 14) + 'px',
    width: popupWidth + 'px',
  }
}

// ── 拖拽 ────────────────────────────────────────────────────────────────────
let dragStartX = 0
let dragStartVal = 0

function startDrag(e) {
  if (verifying.value || status.value === 'success') return
  dragStartX = e.touches ? e.touches[0].clientX : e.clientX
  dragStartVal = sliderVal.value
  window.addEventListener('mousemove', onDrag)
  window.addEventListener('mouseup', endDrag)
  window.addEventListener('touchmove', onDrag, { passive: false })
  window.addEventListener('touchend', endDrag)
}

function onDrag(e) {
  if (verifying.value) return
  e.preventDefault?.()
  const clientX = e.touches ? e.touches[0].clientX : e.clientX
  const delta = clientX - dragStartX
  const maxX = (challenge.value?.bgWidth ?? 280) - handleSize
  sliderVal.value = Math.max(0, Math.min(maxX, dragStartVal + delta))
}

async function endDrag() {
  window.removeEventListener('mousemove', onDrag)
  window.removeEventListener('mouseup', endDrag)
  window.removeEventListener('touchmove', onDrag)
  window.removeEventListener('touchend', endDrag)
  if (sliderVal.value < 5) return
  await verify()
}

// ── 核心方法 ────────────────────────────────────────────────────────────────
async function loadChallenge() {
  loading.value = true
  status.value = ''
  sliderVal.value = 0
  challenge.value = null
  try {
    challenge.value = await authApi.getCaptcha()
    computePosition()   // 获取到实际 bgWidth 后重新计算位置
  } catch {
    // 静默，显示"加载失败"提示
  } finally {
    loading.value = false
  }
}

async function verify() {
  if (verifying.value || !challenge.value) return
  verifying.value = true
  try {
    const t = await authApi.verifyCaptcha({ id: challenge.value.id, x: sliderVal.value })
    status.value = 'success'
    // 短暂展示成功状态，然后关闭并执行回调
    setTimeout(() => {
      const cb = pendingCallback
      close()
      cb?.(t.token)
    }, 600)
  } catch {
    // 错误内部处理，不向父组件传播
    status.value = 'error'
    shaking.value = true
    setTimeout(() => {
      shaking.value = false
      setTimeout(() => {
        sliderVal.value = 0
        status.value = ''
        loadChallenge()
      }, 1200)
    }, 500)
  } finally {
    verifying.value = false
  }
}

// ── 公开 API ────────────────────────────────────────────────────────────────
/**
 * 打开验证码弹窗
 * @param {HTMLElement} anchor   触发按钮的 DOM 元素（用于定位）
 * @param {Function}    callback 验证成功后调用，参数为 token 字符串
 */
function open(anchor, callback) {
  anchorEl = anchor
  pendingCallback = callback
  visible.value = true
  computePosition()
  loadChallenge()
}

function close() {
  visible.value = false
  pendingCallback = null
  anchorEl = null
  challenge.value = null
  status.value = ''
  sliderVal.value = 0
  shaking.value = false
}

defineExpose({ open, close })

onBeforeUnmount(() => {
  window.removeEventListener('mousemove', onDrag)
  window.removeEventListener('mouseup', endDrag)
  window.removeEventListener('touchmove', onDrag)
  window.removeEventListener('touchend', endDrag)
})
</script>

<style scoped>
/* ── 遮罩层 ──────────────────────────────────────────────────────────────── */
.sc-overlay {
  position: fixed;
  inset: 0;
  z-index: 9000;
  background: rgba(0, 0, 0, 0.4);
}

/* ── 弹窗 ────────────────────────────────────────────────────────────────── */
.sc-popup {
  position: fixed;
  z-index: 9001;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.22);
  padding: 14px 14px 16px;
  user-select: none;
}

/* ── 标题栏 ──────────────────────────────────────────────────────────────── */
.sc-popup-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  font-size: 14px;
  font-weight: 600;
  color: #303133;
}
.sc-close-btn {
  width: 24px;
  height: 24px;
  border: none;
  background: transparent;
  color: #909399;
  font-size: 14px;
  cursor: pointer;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s, color 0.15s;
  line-height: 1;
  padding: 0;
}
.sc-close-btn:hover { background: #f0f2f5; color: #606266; }

/* ── 向下箭头（指向触发按钮） ────────────────────────────────────────────── */
.sc-popup-arrow {
  position: absolute;
  bottom: -9px;
  left: 50%;
  transform: translateX(-50%);
  width: 0;
  height: 0;
  border-left: 9px solid transparent;
  border-right: 9px solid transparent;
  border-top: 9px solid #fff;
  filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.1));
}

/* ── 加载中 ──────────────────────────────────────────────────────────────── */
.sc-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  height: 180px;
}
.sc-loading-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--el-color-primary, #409eff);
  animation: sc-bounce 1.2s infinite ease-in-out;
}
.sc-loading-dot:nth-child(2) { animation-delay: 0.2s; }
.sc-loading-dot:nth-child(3) { animation-delay: 0.4s; }
@keyframes sc-bounce {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
  40% { transform: scale(1); opacity: 1; }
}

/* ── 图片区域 ──────────────────────────────── */
.sc-image-wrap {
  position: relative;
  border-radius: 4px;
  overflow: hidden;
  border: 1px solid var(--el-border-color, #dcdfe6);
  background: #eee;
}
.sc-bg {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.sc-piece {
  position: absolute;
  pointer-events: none;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.5));
  transition: filter 0.1s;
}

/* 状态遮罩 */
.sc-mask {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.5px;
}
.sc-success {
  background: rgba(103, 194, 58, 0.75);
  color: #fff;
}
.sc-error {
  background: rgba(245, 108, 108, 0.75);
  color: #fff;
}
.sc-shake {
  animation: sc-shake-anim 0.5s ease;
}
@keyframes sc-shake-anim {
  0%, 100% { transform: translateX(0); }
  20% { transform: translateX(-8px); }
  40% { transform: translateX(8px); }
  60% { transform: translateX(-5px); }
  80% { transform: translateX(5px); }
}

/* 刷新按钮 */
.sc-refresh-btn {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 26px;
  height: 26px;
  border: none;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.35);
  color: #fff;
  font-size: 15px;
  cursor: pointer;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
}
.sc-refresh-btn:hover { background: rgba(0, 0, 0, 0.55); }

/* ── 滑动条 ────────────────────────────────── */
.sc-slider-wrap {
  margin-top: 8px;
}
.sc-success-bar {
  height: 40px;
  border-radius: 20px;
  background: linear-gradient(135deg, #67c23a, #85ce61);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  font-weight: 600;
}
.sc-track {
  position: relative;
  height: 40px;
  border-radius: 20px;
  background: var(--el-fill-color, #f0f2f5);
  border: 1px solid var(--el-border-color, #dcdfe6);
  overflow: hidden;
}
.sc-track-hint {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  color: var(--el-text-color-placeholder, #a8abb2);
  pointer-events: none;
  transition: opacity 0.2s;
}
.sc-track-fill {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  background: linear-gradient(135deg, #409eff, #79bbff);
  border-radius: 20px 0 0 20px;
  pointer-events: none;
  transition: width 0.05s linear;
}
.sc-handle {
  position: absolute;
  top: 0;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
  cursor: grab;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2;
  transition: box-shadow 0.15s;
}
.sc-handle:active { cursor: grabbing; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.35); }
.sc-handle-verifying { cursor: wait; }
.sc-handle-icon {
  font-size: 20px;
  color: var(--el-color-primary, #409eff);
  font-weight: 700;
  line-height: 1;
  pointer-events: none;
}

/* ── 加载失败 ──────────────────────────────── */
.sc-error-tip {
  height: 180px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  color: var(--el-text-color-secondary, #909399);
  font-size: 14px;
  border: 1px dashed var(--el-border-color, #dcdfe6);
  border-radius: 4px;
}
.sc-retry-btn {
  padding: 4px 14px;
  border: 1px solid var(--el-color-primary, #409eff);
  border-radius: 4px;
  background: transparent;
  color: var(--el-color-primary, #409eff);
  cursor: pointer;
  font-size: 13px;
  transition: all 0.2s;
}
.sc-retry-btn:hover {
  background: var(--el-color-primary, #409eff);
  color: #fff;
}

/* ── 过渡 ──────────────────────────────────── */.sc-popup-fade-enter-active { transition: opacity 0.2s, transform 0.2s; }
.sc-popup-fade-leave-active { transition: opacity 0.15s; }
.sc-popup-fade-enter-from   { opacity: 0; transform: translateY(6px); }
.sc-popup-fade-leave-to     { opacity: 0; }
.sc-fade-enter-active, .sc-fade-leave-active { transition: opacity 0.25s; }
.sc-fade-enter-from, .sc-fade-leave-to { opacity: 0; }
</style>
