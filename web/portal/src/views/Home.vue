<template>
  <div>
    <!-- ════ Hero 区域 ════ -->
    <section class="hero">
      <div class="hero-bg-pattern" aria-hidden="true"></div>
      <div class="container hero-content">
        <div class="hero-badge">
          <span class="badge-dot"></span>
          智能 · 高效 · 公正的在线考试平台
        </div>
        <h1 class="hero-title">
          学习，从<span class="hero-title-accent">此刻</span>开始
        </h1>
        <p class="hero-subtitle">
          支持单选、多选、判断、简答等多种题型<br/>
          AI 辅助批改，即时出分，全面成绩分析
        </p>
        <div class="hero-actions">
          <router-link to="/exams">
            <el-button type="primary" size="large" class="hero-btn-primary">
              <el-icon><Promotion /></el-icon>
              立即参加考试
            </el-button>
          </router-link>
          <router-link v-if="!auth.isLoggedIn" to="/register">
            <el-button size="large" class="hero-btn-outline">
              免费注册 →
            </el-button>
          </router-link>
          <router-link v-else to="/practice">
            <el-button size="large" class="hero-btn-outline">
              <el-icon><EditPen /></el-icon>
              在线练习
            </el-button>
          </router-link>
        </div>

        <!-- 数据统计 -->
        <div class="hero-stats">
          <div class="stat-item" v-for="s in stats" :key="s.label">
            <div class="stat-num">{{ s.num }}</div>
            <div class="stat-label">{{ s.label }}</div>
          </div>
        </div>
      </div>
      <div class="hero-wave">
        <svg viewBox="0 0 1440 100" preserveAspectRatio="none">
          <path d="M0,60 C200,100 400,20 600,60 C800,100 1000,20 1200,60 C1350,90 1400,50 1440,40 L1440,100 L0,100 Z" fill="#f8fafc"/>
        </svg>
      </div>
    </section>

    <!-- ════ 特性卡片 ════ -->
    <section class="features-section">
      <div class="container">
        <div class="section-title-group">
          <h2 class="section-title">为什么选择我们</h2>
          <p class="section-subtitle">专为在线教育场景设计的考试系统</p>
        </div>
        <div class="features-grid">
          <div class="feature-card" v-for="f in features" :key="f.title">
            <div class="feature-icon" :style="{ '--icon-color': f.color }">
              <el-icon size="26" color="#fff"><component :is="f.icon" /></el-icon>
            </div>
            <h3 class="feature-title">{{ f.title }}</h3>
            <p class="feature-desc">{{ f.desc }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ════ 当前开放考试 ════ -->
    <section class="exams-section">
      <div class="container">
        <div class="section-header">
          <div>
            <h2 class="section-title">当前开放考试</h2>
            <p class="section-subtitle">报名或直接作答，从这里开始</p>
          </div>
          <router-link to="/exams" class="view-all-link">
            查看全部
            <el-icon><ArrowRight /></el-icon>
          </router-link>
        </div>

        <div v-if="loading" class="exam-grid">
          <el-skeleton v-for="i in 3" :key="i" :rows="4" animated class="exam-skeleton" />
        </div>
        <div v-else-if="exams.length === 0" class="empty-wrap">
          <el-empty description="暂无开放中的考试" />
        </div>
        <div v-else class="exam-grid">
          <div v-for="exam in exams" :key="exam.id" class="exam-card">
            <div class="exam-card-top">
              <el-tag :type="statusType(exam.status)" size="small" effect="light">{{ statusLabel(exam.status) }}</el-tag>
              <span class="exam-score-badge">{{ exam.totalScore }} 分</span>
            </div>
            <h3 class="exam-title">{{ exam.title }}</h3>
            <p class="exam-desc">{{ exam.description || '暂无描述' }}</p>
            <div class="exam-meta-row">
              <span class="meta-item">
                <el-icon><Clock /></el-icon>
                {{ exam.durationMinutes }} 分钟
              </span>
              <span class="meta-item">
                <el-icon><Document /></el-icon>
                {{ exam.questionCount }} 题
              </span>
            </div>
            <div v-if="exam.startTime || exam.endTime" class="exam-time-row">
              <el-icon><Calendar /></el-icon>
              {{ formatDateRange(exam.startTime, exam.endTime) }}
            </div>
            <div class="exam-card-actions">
              <router-link :to="`/exams/${exam.id}`">
                <el-button plain size="small" round>查看详情</el-button>
              </router-link>
              <router-link :to="`/exam/${exam.id}/room`">
                <el-button type="primary" size="small" round>
                  <el-icon><VideoPlay /></el-icon>
                  立即作答
                </el-button>
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ════ 图书展示 ════ -->
    <section class="books-section">
      <div class="container">
        <div class="section-header">
          <div>
            <h2 class="section-title">图书馆</h2>
            <p class="section-subtitle">阅读经典，拓展知识边界</p>
          </div>
          <router-link to="/books" class="view-all-link">
            查看全部
            <el-icon><ArrowRight /></el-icon>
          </router-link>
        </div>

        <div v-if="booksLoading" class="books-grid">
          <el-skeleton v-for="i in 4" :key="i" animated class="book-skeleton">
            <template #template>
              <el-skeleton-item variant="image" style="height:180px;border-radius:12px 12px 0 0" />
              <div style="padding:14px">
                <el-skeleton-item variant="h3" style="width:80%" />
                <el-skeleton-item variant="text" style="width:50%;margin-top:8px" />
                <el-skeleton-item variant="text" style="width:100%;margin-top:8px" />
              </div>
            </template>
          </el-skeleton>
        </div>
        <el-empty v-else-if="books.length === 0" description="暂无图书" />
        <div v-else class="books-grid">
          <div v-for="book in books" :key="book.id" class="book-card">
            <div class="book-cover">
              <img v-if="book.coverImageUrl" :src="book.coverImageUrl" :alt="book.title" />
              <div v-else class="book-cover-placeholder">
                <el-icon size="36" color="rgba(255,255,255,0.6)"><Reading /></el-icon>
                <span class="cover-title-text">{{ book.title }}</span>
              </div>
              <el-tag v-if="book.category" class="book-category-tag" size="small" effect="dark">{{ book.category }}</el-tag>
            </div>
            <div class="book-info">
              <h3 class="book-title">{{ book.title }}</h3>
              <p v-if="book.author" class="book-author">
                <el-icon><User /></el-icon>{{ book.author }}
              </p>
              <p v-if="book.description" class="book-desc">{{ book.description }}</p>
              <div class="book-meta">
                <span v-if="book.pageCount" class="meta-item">
                  <el-icon><Document /></el-icon>{{ book.pageCount }} 页
                </span>
                <span v-if="book.publishYear" class="meta-item">
                  <el-icon><Calendar /></el-icon>{{ book.publishYear }}
                </span>
              </div>
              <router-link :to="`/books/${book.id}`" class="book-read-btn">
                <el-button type="primary" size="small" round style="width:100%">
                  <el-icon><Reading /></el-icon>开始阅读
                </el-button>
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ════ 号召行动 ════ -->
    <section v-if="!auth.isLoggedIn" class="cta-section">
      <div class="container cta-inner">
        <div class="cta-text">
          <h2>准备好了吗？</h2>
          <p>注册账号，即刻开始你的学习之旅</p>
        </div>
        <router-link to="/register">
          <el-button type="primary" size="large" class="cta-btn">免费注册</el-button>
        </router-link>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { examsApi } from '@/api/exams'
import {
  Promotion, Clock, Document, Calendar, ArrowRight,
  VideoPlay, EditPen, Reading, Trophy, Cpu, DataAnalysis, User
} from '@element-plus/icons-vue'
import { booksApi } from '@/api/books'

const auth = useAuthStore()
const exams = ref([])
const loading = ref(true)
const books = ref([])
const booksLoading = ref(true)

const stats = [
  { num: '10,000+', label: '注册用户' },
  { num: '500+',    label: '在线考试' },
  { num: '98%',     label: '用户满意度' },
  { num: '24/7',    label: '随时在线' },
]

const features = [
  { icon: 'Reading',      title: '多题型支持',    desc: '支持单选、多选、判断、简答等多种题型，灵活组卷', color: '#1d4ed8' },
  { icon: 'Cpu',          title: 'AI 智能评分',   desc: 'AI 辅助批改简答题，给出详细评分与评语，公正高效', color: '#7c3aed' },
  { icon: 'Trophy',       title: '即时出分',       desc: '客观题提交后即时出分，成绩报告一目了然', color: '#d97706' },
  { icon: 'DataAnalysis', title: '深度分析',       desc: '逐题分析答题情况，精准定位知识薄弱点', color: '#059669' },
]

function statusLabel(s) {
  return { 1: '报名中', 2: '进行中', 3: '已结束', 4: '已取消' }[s] ?? '未知'
}
function statusType(s) {
  return { 1: 'success', 2: 'warning', 3: 'info', 4: 'danger' }[s] ?? ''
}
function formatDateRange(start, end) {
  const fmt = (d) => d ? new Date(d).toLocaleDateString('zh-CN', { month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' }) : ''
  if (start && end) return `${fmt(start)} — ${fmt(end)}`
  if (start) return `${fmt(start)} 开始`
  if (end) return `截止 ${fmt(end)}`
  return ''
}

onMounted(async () => {
  try {
    const [pub, ing] = await Promise.all([
      examsApi.getList({ page: 1, pageSize: 6, status: 1 }),
      examsApi.getList({ page: 1, pageSize: 6, status: 2 }),
    ])
    exams.value = [...ing.items, ...pub.items].slice(0, 6)
  } catch {
    // ignore
  } finally {
    loading.value = false
  }

  try {
    const res = await booksApi.getBooks({ page: 1, pageSize: 8 })
    books.value = res.items ?? res
  } catch {
    // ignore
  } finally {
    booksLoading.value = false
  }
})
</script>

<style scoped>
/* ── Hero ──────────────────────────────────────────────── */
.hero {
  background: linear-gradient(145deg, #0f172a 0%, #1e3a8a 45%, #1d4ed8 80%, #2563eb 100%);
  padding: 90px 0 0;
  position: relative;
  overflow: hidden;
  color: #fff;
}

.hero-bg-pattern {
  position: absolute;
  inset: 0;
  background-image:
    radial-gradient(circle at 20% 20%, rgba(99,102,241,0.15) 0%, transparent 50%),
    radial-gradient(circle at 80% 80%, rgba(59,130,246,0.2) 0%, transparent 50%),
    radial-gradient(circle at 50% 50%, rgba(29,78,216,0.1) 0%, transparent 60%);
  pointer-events: none;
}

.hero-content {
  position: relative;
  z-index: 1;
  text-align: center;
  padding-bottom: 64px;
}

.hero-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: rgba(255,255,255,0.12);
  border: 1px solid rgba(255,255,255,0.2);
  border-radius: 100px;
  padding: 6px 18px;
  font-size: 13px;
  font-weight: 500;
  color: rgba(255,255,255,0.9);
  margin-bottom: 28px;
  backdrop-filter: blur(8px);
}
.badge-dot {
  width: 6px;
  height: 6px;
  background: #4ade80;
  border-radius: 50%;
  animation: pulse 2s infinite;
}
@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(1.3); }
}

.hero-title {
  font-size: 56px;
  font-weight: 800;
  line-height: 1.15;
  letter-spacing: -1px;
  color: #fff;
  margin-bottom: 20px;
}
.hero-title-accent {
  background: linear-gradient(135deg, #60a5fa, #a78bfa);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.hero-subtitle {
  font-size: 18px;
  color: rgba(255,255,255,0.75);
  line-height: 1.8;
  margin-bottom: 44px;
  max-width: 560px;
  margin-left: auto;
  margin-right: auto;
}

.hero-actions {
  display: flex;
  justify-content: center;
  gap: 14px;
  flex-wrap: wrap;
  margin-bottom: 64px;
}
.hero-btn-primary {
  height: 48px;
  padding: 0 36px;
  font-size: 16px;
  font-weight: 600;
  border-radius: 12px;
  background: #fff;
  color: #1d4ed8;
  border: none;
  box-shadow: 0 8px 24px rgba(0,0,0,0.2);
  transition: all 0.25s;
}
.hero-btn-primary:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 32px rgba(0,0,0,0.25);
}
.hero-btn-outline {
  height: 48px;
  padding: 0 32px;
  font-size: 16px;
  font-weight: 500;
  border-radius: 12px;
  background: rgba(255,255,255,0.12);
  color: #fff;
  border: 1px solid rgba(255,255,255,0.3);
  transition: all 0.25s;
}
.hero-btn-outline:hover {
  background: rgba(255,255,255,0.22);
  border-color: rgba(255,255,255,0.5);
  transform: translateY(-2px);
}

.hero-stats {
  display: flex;
  justify-content: center;
  gap: 48px;
  flex-wrap: wrap;
  padding: 28px 0;
  border-top: 1px solid rgba(255,255,255,0.12);
}
.stat-item { text-align: center; }
.stat-num {
  font-size: 28px;
  font-weight: 800;
  color: #fff;
  line-height: 1;
  margin-bottom: 4px;
}
.stat-label {
  font-size: 12px;
  color: rgba(255,255,255,0.6);
  font-weight: 500;
  letter-spacing: 0.5px;
}

.hero-wave {
  height: 100px;
  overflow: hidden;
  margin-top: -1px;
}
.hero-wave svg { width: 100%; height: 100%; display: block; }

/* ── 特性区域 ─────────────────────────────────────────── */
.features-section {
  padding: 72px 0 56px;
  background: #f8fafc;
}
.section-title-group {
  text-align: center;
  margin-bottom: 48px;
}
.section-title {
  font-size: 32px;
  font-weight: 800;
  color: #0f172a;
  margin-bottom: 10px;
  letter-spacing: -0.5px;
}
.section-subtitle {
  font-size: 16px;
  color: #64748b;
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 24px;
}
.feature-card {
  background: #fff;
  border-radius: 20px;
  padding: 32px 26px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 12px rgba(0,0,0,0.04);
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}
.feature-card:hover {
  transform: translateY(-6px);
  box-shadow: 0 12px 36px rgba(0,0,0,0.08);
  border-color: transparent;
}
.feature-icon {
  width: 56px;
  height: 56px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 20px;
  background: var(--icon-color);
}
.feature-title {
  font-size: 17px;
  font-weight: 700;
  color: #0f172a;
  margin-bottom: 10px;
}
.feature-desc {
  font-size: 14px;
  color: #64748b;
  line-height: 1.7;
}

/* ── 考试区域 ─────────────────────────────────────────── */
.exams-section {
  padding: 56px 0 72px;
  background: #fff;
}
.section-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  margin-bottom: 32px;
}
.view-all-link {
  display: flex;
  align-items: center;
  gap: 4px;
  color: #1d4ed8;
  font-size: 14px;
  font-weight: 500;
  padding: 6px 12px;
  border-radius: 8px;
  transition: background 0.2s;
}
.view-all-link:hover { background: rgba(29,78,216,0.06); }

.exam-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}
.exam-skeleton { border-radius: 16px; overflow: hidden; }

.exam-card {
  background: #fff;
  border-radius: 16px;
  padding: 22px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  display: flex;
  flex-direction: column;
  gap: 12px;
  transition: all 0.25s ease;
}
.exam-card:hover {
  border-color: #93c5fd;
  box-shadow: 0 8px 24px rgba(29,78,216,0.1);
  transform: translateY(-3px);
}
.exam-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.exam-score-badge {
  font-size: 12px;
  color: #64748b;
  background: #f1f5f9;
  padding: 2px 8px;
  border-radius: 6px;
  font-weight: 500;
}
.exam-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.exam-desc {
  font-size: 13px;
  color: #94a3b8;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.6;
}
.exam-meta-row {
  display: flex;
  gap: 20px;
}
.meta-item {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 13px;
  color: #64748b;
}
.exam-time-row {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: #94a3b8;
}
.exam-card-actions {
  display: flex;
  gap: 10px;
  padding-top: 12px;
  border-top: 1px solid #f1f5f9;
}

/* ── 号召行动区 ───────────────────────────────────────── */
.cta-section {
  background: linear-gradient(135deg, #1d4ed8, #3b82f6);
  padding: 56px 0;
}
.cta-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  flex-wrap: wrap;
}
.cta-text h2 {
  font-size: 28px;
  font-weight: 800;
  color: #fff;
  margin-bottom: 6px;
}
.cta-text p {
  font-size: 16px;
  color: rgba(255,255,255,0.8);
}
.cta-btn {
  height: 48px;
  padding: 0 40px;
  font-size: 16px;
  font-weight: 600;
  background: #fff;
  color: #1d4ed8;
  border: none;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.15);
  flex-shrink: 0;
  transition: all 0.25s;
}
.cta-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0,0,0,0.2);
}

/* ── 图书展示区 ───────────────────────────────────────── */
.books-section {
  padding: 56px 0 72px;
  background: #f8fafc;
}
.books-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 22px;
}
.book-skeleton {
  border-radius: 16px;
  overflow: hidden;
  background: #fff;
  border: 1px solid #f1f5f9;
}
.book-card {
  background: #fff;
  border-radius: 16px;
  overflow: hidden;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 10px rgba(0,0,0,0.04);
  display: flex;
  flex-direction: column;
  transition: all 0.25s ease;
}
.book-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 12px 32px rgba(29,78,216,0.1);
  border-color: #93c5fd;
}
.book-cover {
  position: relative;
  height: 180px;
  background: linear-gradient(135deg, #1e3a8a, #3b82f6);
  overflow: hidden;
  flex-shrink: 0;
}
.book-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.book-cover-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 16px;
}
.cover-title-text {
  font-size: 13px;
  font-weight: 600;
  color: rgba(255,255,255,0.85);
  text-align: center;
  line-height: 1.4;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
}
.book-category-tag {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(0,0,0,0.45) !important;
  border-color: transparent !important;
  backdrop-filter: blur(4px);
}
.book-info {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}
.book-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.book-author {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  color: #64748b;
}
.book-desc {
  font-size: 13px;
  color: #94a3b8;
  line-height: 1.6;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  flex: 1;
}
.book-meta {
  display: flex;
  gap: 16px;
}
.book-read-btn {
  margin-top: 4px;
}

/* ── 响应式 ───────────────────────────────────────────── */
@media (max-width: 768px) {
  .hero-title { font-size: 36px; }
  .hero-subtitle { font-size: 15px; }
  .hero-stats { gap: 28px; }
  .stat-num { font-size: 22px; }
  .section-title { font-size: 24px; }
  .cta-inner { flex-direction: column; text-align: center; }
  .section-header { flex-direction: column; align-items: flex-start; gap: 12px; }
}
@media (max-width: 480px) {
  .hero { padding-top: 64px; }
  .hero-title { font-size: 28px; }
  .hero-btn-primary, .hero-btn-outline { height: 44px; padding: 0 24px; font-size: 15px; }
  .features-grid { grid-template-columns: 1fr 1fr; }
  .exam-grid { grid-template-columns: 1fr; }
  .books-grid { grid-template-columns: 1fr 1fr; }
}
</style>
