<template>
  <div>
    <!-- Hero 区域 -->
    <section class="hero">
      <div class="container hero-content">
        <h1 class="hero-title">智能在线考试平台</h1>
        <p class="hero-subtitle">高效、便捷、公正的在线考试体验<br/>客观题即时出分，简答题 AI 辅助批改</p>
        <div class="hero-actions">
          <router-link to="/exams">
            <el-button type="primary" size="large" round class="hero-btn-main">
              <el-icon><Promotion /></el-icon>&ensp;立即参加考试
            </el-button>
          </router-link>
          <router-link v-if="!auth.isLoggedIn" to="/register">
            <el-button size="large" round class="hero-btn-sub">免费注册</el-button>
          </router-link>
        </div>
      </div>
      <div class="hero-wave">
        <svg viewBox="0 0 1440 80" preserveAspectRatio="none"><path d="M0,40 C360,80 1080,0 1440,40 L1440,80 L0,80 Z" fill="#f5f7fa"/></svg>
      </div>
    </section>

    <!-- 特性卡片 -->
    <section class="features container">
      <div class="feature-card" v-for="f in features" :key="f.title">
        <div class="feature-icon" :style="{ background: f.color }">
          <el-icon size="26" color="#fff"><component :is="f.icon" /></el-icon>
        </div>
        <h3>{{ f.title }}</h3>
        <p>{{ f.desc }}</p>
      </div>
    </section>

    <!-- 当前开放考试 -->
    <section class="exams-section container">
      <div class="section-header">
        <h2>当前开放考试</h2>
        <router-link to="/exams" class="view-all">查看全部 →</router-link>
      </div>

      <div v-if="loading" class="loading-wrap">
        <el-skeleton :rows="3" animated />
      </div>

      <div v-else-if="exams.length === 0" class="empty-wrap">
        <el-empty description="暂无开放中的考试" />
      </div>

      <div v-else class="exam-grid">
        <div v-for="exam in exams" :key="exam.id" class="exam-card">
          <div class="exam-card-header">
            <el-tag :type="statusType(exam.status)" size="small">{{ statusLabel(exam.status) }}</el-tag>
            <span class="exam-score">{{ exam.totalScore }} 分</span>
          </div>
          <h3 class="exam-title">{{ exam.title }}</h3>
          <p class="exam-desc">{{ exam.description || '暂无描述' }}</p>
          <div class="exam-meta">
            <span><el-icon><Clock /></el-icon> {{ exam.durationMinutes }} 分钟</span>
            <span><el-icon><Document /></el-icon> {{ exam.questionCount }} 题</span>
          </div>
          <div v-if="exam.startTime || exam.endTime" class="exam-time">
            <el-icon><Calendar /></el-icon>
            {{ formatDateRange(exam.startTime, exam.endTime) }}
          </div>
          <div class="exam-card-footer">
            <router-link :to="`/exams/${exam.id}`">
              <el-button type="primary" size="small" round>参加考试</el-button>
            </router-link>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { examsApi } from '@/api/exams'
import {
  Promotion, Clock, Document, Calendar,
  Reading, Trophy, Cpu, DataAnalysis
} from '@element-plus/icons-vue'

const auth = useAuthStore()
const exams = ref([])
const loading = ref(true)

const features = [
  { icon: 'Reading', title: '在线作答', desc: '随时随地参加考试，支持单选、多选、判断、简答题型', color: '#3b82f6' },
  { icon: 'Cpu', title: 'AI 智能评分', desc: '简答题由 AI 辅助批改，给出评分与详细评语', color: '#8b5cf6' },
  { icon: 'Trophy', title: '即时出分', desc: '客观题提交即出分，成绩报告一目了然', color: '#f59e0b' },
  { icon: 'DataAnalysis', title: '成绩分析', desc: '详细查看每题答题情况，找到知识薄弱点', color: '#10b981' },
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
    // 同时获取已发布(1)和进行中(2)的考试
    const [pub, ing] = await Promise.all([
      examsApi.getList({ page: 1, pageSize: 6, status: 1 }),
      examsApi.getList({ page: 1, pageSize: 6, status: 2 }),
    ])
    const merged = [...ing.items, ...pub.items].slice(0, 6)
    exams.value = merged
  } catch {
    // ignore
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
/* Hero */
.hero {
  background: linear-gradient(135deg, #1e3a8a 0%, #1d4ed8 60%, #3b82f6 100%);
  padding: 100px 0 0;
  position: relative;
  color: #fff;
}
.hero-content {
  text-align: center;
  padding-bottom: 60px;
}
.hero-title {
  font-size: 48px;
  font-weight: 800;
  margin-bottom: 16px;
  letter-spacing: -0.5px;
}
.hero-subtitle {
  font-size: 18px;
  opacity: 0.85;
  line-height: 1.8;
  margin-bottom: 40px;
}
.hero-actions {
  display: flex;
  justify-content: center;
  gap: 16px;
  flex-wrap: wrap;
}
.hero-btn-main {
  padding: 12px 36px;
  font-size: 16px;
}
.hero-btn-sub {
  padding: 12px 36px;
  font-size: 16px;
  background: rgba(255,255,255,0.15);
  color: #fff;
  border-color: rgba(255,255,255,0.4);
}
.hero-btn-sub:hover {
  background: rgba(255,255,255,0.25);
}
.hero-wave {
  height: 80px;
  overflow: hidden;
}
.hero-wave svg {
  width: 100%;
  height: 100%;
  display: block;
}

/* Features */
.features {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 24px;
  padding: 56px 24px;
}
.feature-card {
  background: #fff;
  border-radius: 16px;
  padding: 28px 24px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.06);
  transition: transform 0.2s, box-shadow 0.2s;
}
.feature-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0,0,0,0.1);
}
.feature-icon {
  width: 52px;
  height: 52px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
}
.feature-card h3 {
  font-size: 17px;
  font-weight: 600;
  margin-bottom: 8px;
  color: #1e293b;
}
.feature-card p {
  font-size: 14px;
  color: #64748b;
  line-height: 1.6;
}

/* Exams Section */
.exams-section {
  padding: 0 24px 60px;
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}
.section-header h2 {
  font-size: 24px;
  font-weight: 700;
  color: #1e293b;
}
.view-all {
  color: #1d4ed8;
  font-size: 14px;
  font-weight: 500;
}
.loading-wrap,
.empty-wrap {
  padding: 40px 0;
}
.exam-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
}
.exam-card {
  background: #fff;
  border-radius: 14px;
  padding: 22px 22px 18px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.06);
  display: flex;
  flex-direction: column;
  gap: 10px;
  transition: box-shadow 0.2s;
}
.exam-card:hover {
  box-shadow: 0 6px 20px rgba(0,0,0,0.1);
}
.exam-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.exam-score {
  font-size: 13px;
  color: #64748b;
  font-weight: 500;
}
.exam-title {
  font-size: 16px;
  font-weight: 600;
  color: #1e293b;
  line-height: 1.4;
}
.exam-desc {
  font-size: 13px;
  color: #94a3b8;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.5;
}
.exam-meta {
  display: flex;
  gap: 20px;
  font-size: 13px;
  color: #64748b;
}
.exam-meta span {
  display: flex;
  align-items: center;
  gap: 4px;
}
.exam-time {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #94a3b8;
}
.exam-card-footer {
  margin-top: 4px;
}

/* CTA */
.cta-section {
  background: linear-gradient(135deg, #1e3a8a 0%, #1d4ed8 100%);
  padding: 80px 0;
}
.cta-content {
  text-align: center;
  color: #fff;
}
.cta-content h2 {
  font-size: 32px;
  font-weight: 700;
  margin-bottom: 12px;
}
.cta-content p {
  font-size: 16px;
  opacity: 0.85;
  margin-bottom: 32px;
}
</style>
