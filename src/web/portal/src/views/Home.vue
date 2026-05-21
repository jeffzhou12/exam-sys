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
          高效<span class="hero-title-accent">学习</span>，认真<span class="hero-title-accent">考试</span>，从<span class="hero-title-accent">此刻</span>开始
        </h1>
        <p class="hero-subtitle">
          支持单选、多选、判断、简答等多种题型<br/>
          AI 辅助批改，即时出分，全面成绩分析
        </p>
        <div class="hero-actions">
          <router-link to="/exams">
            <el-button type="primary" size="large"  class="hero-btn-outline">
              <el-icon><Promotion /></el-icon>
              参加考试
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
          <div class="section-eyebrow">核心功能</div>
          <h2 class="section-title">为什么选择我们</h2>
          <p class="section-subtitle">专为在线教育场景打磨，每一处细节都服务于真实教学需求</p>
        </div>

        <div class="features-list">
          <div
            v-for="(f, idx) in features"
            :key="f.title"
            class="feature-row"
            :class="idx % 2 === 1 ? 'feature-row--reverse' : ''"
          >
            <!-- 装饰视觉面板 -->
            <div class="feature-visual" :style="{ background: f.bg }">
              <div class="feature-visual-inner">
                <!-- 大图标 -->
                <div class="feature-big-icon" :style="{ background: f.color }">
                  <el-icon size="36" color="#fff"><component :is="f.icon" /></el-icon>
                </div>
                <!-- 装饰徽章序号 -->
                <div class="feature-seq" :style="{ color: f.accent }">0{{ idx + 1 }}</div>
                <!-- 动态装饰图形 -->
                <div class="feature-deco" :class="'feature-deco--' + f.decoType" :style="{ '--c': f.accent }">
                  <template v-if="f.decoType === 'qtype'">
                    <div class="deco-qtype">
                      <div class="dq-chip" v-for="t in ['单选题','多选题','判断题','简答题']" :key="t" :style="{ '--chip-c': f.accent }">{{ t }}</div>
                    </div>
                  </template>
                  <template v-else-if="f.decoType === 'ai'">
                    <div class="deco-ai">
                      <div class="deco-ai-ring deco-ai-ring--1"></div>
                      <div class="deco-ai-ring deco-ai-ring--2"></div>
                      <div class="deco-ai-core"><el-icon size="28" color="#fff"><Cpu /></el-icon></div>
                      <div class="deco-ai-node" v-for="n in 6" :key="n" :class="'node-'+n"></div>
                    </div>
                  </template>
                  <template v-else-if="f.decoType === 'score'">
                    <div class="deco-score">
                      <div class="deco-score-card">
                        <div class="dsc-label">最终得分</div>
                        <div class="dsc-num" :style="{ color: f.accent }">96</div>
                        <div class="dsc-bar-wrap">
                          <div class="dsc-bar" v-for="v in [100,70,90,60,85]" :key="v" :style="{ height: v+'%', background: f.accent }"></div>
                        </div>
                        <div class="dsc-tag">超越 92% 的同学</div>
                      </div>
                    </div>
                  </template>
                  <template v-else-if="f.decoType === 'chart'">
                    <div class="deco-chart">
                      <div class="deco-chart-bar" v-for="(v,i) in [55,80,65,90,72,95]" :key="i"
                        :style="{ height: v+'%', background: `hsl(${152+i*8},65%,${38+i*4}%)`, animationDelay: i*0.1+'s' }"
                      ></div>
                      <div class="deco-chart-line"></div>
                    </div>
                  </template>
                </div>
              </div>
            </div>

            <!-- 文字内容面板 -->
            <div class="feature-content">
              <div class="feature-eyebrow" :style="{ color: f.accent, background: f.bg }">
                <el-icon :size="14"><component :is="f.icon" /></el-icon>
                {{ f.subtitle }}
              </div>
              <h3 class="feature-title">{{ f.title }}</h3>
              <p class="feature-desc">{{ f.desc }}</p>
              <ul class="feature-points">
                <li v-for="pt in f.points" :key="pt">
                  <span class="fp-dot" :style="{ background: f.accent }"></span>
                  {{ pt }}
                </li>
              </ul>
            </div>
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
                <el-button plain round>查看详情</el-button>
              </router-link>
              <router-link :to="`/exam/${exam.id}/room`">
                <el-button type="primary" round>
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
                <el-button type="primary" round style="width:100%">
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
  { num: '7*24',    label: '随时在线' },
]

const features = [
  {
    icon: 'Reading',
    decoType: 'qtype',
    title: '多题型灵活组卷',
    subtitle: '一套系统覆盖所有考试场景',
    desc: '从客观题到主观题，全类型无缝覆盖。教师可自由选题、自定分值，学生答题体验流畅自然，系统自动完成客观题批改。',
    points: [
      '单选 · 多选 · 判断题，提交即自动批改',
      '简答题支持 AI 辅助评分与详细评语',
      '题库分类管理，按知识点快速检索',
      '支持难度系数与知识点双维度标签',
    ],
    color: '#1d4ed8',
    bg: 'linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%)',
    accent: '#3b82f6',
  },
  {
    icon: 'Cpu',
    decoType: 'ai',
    title: 'AI 智能批改',
    subtitle: '让大模型承担重复性批改工作',
    desc: '基于大语言模型深度理解答题内容，为每道简答题给出精准评分与有据可查的评语，同时支持学生一键发起 AI 解析，彻底搞懂每道题。',
    points: [
      '毫秒级响应，批改速度远超人工',
      '逐句分析答案，评语有理有据',
      '多轮对话式 AI 答题解析',
      '支持 OpenAI、DeepSeek 等主流模型',
    ],
    color: '#7c3aed',
    bg: 'linear-gradient(135deg, #f5f3ff 0%, #ede9fe 100%)',
    accent: '#8b5cf6',
  },
  {
    icon: 'Trophy',
    decoType: 'score',
    title: '实时成绩反馈',
    subtitle: '提交即知晓，不再漫长等待',
    desc: '客观题作答完成后立即出分，简答题 AI 批改完成后自动推送完整成绩报告，每一分的来源都清晰可溯。',
    points: [
      '客观题提交后毫秒级出分',
      '简答题 AI 评分完成自动通知',
      '逐题得分详情与对比参考答案',
      '历史成绩趋势图，一眼看出进步',
    ],
    color: '#d97706',
    bg: 'linear-gradient(135deg, #fffbeb 0%, #fef3c7 100%)',
    accent: '#f59e0b',
  },
  {
    icon: 'DataAnalysis',
    decoType: 'chart',
    title: '深度学习分析',
    subtitle: '数据驱动，精准找到薄弱点',
    desc: '全方位记录每道题的答题情况，自动聚合分析知识点掌握程度，结合错题本与 AI 相似题推荐，帮助学生有针对性地强化练习。',
    points: [
      '按知识点维度聚合答题数据',
      '自动收录错题，建立专属错题本',
      'AI 推荐相似题目，针对性突破',
      '学习进度可视化，进步看得见',
    ],
    color: '#059669',
    bg: 'linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%)',
    accent: '#10b981',
  },
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
  gap: 148px;
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
  padding: 96px 0 80px;
  background: #f8fafc;
}
.section-eyebrow {
  display: inline-block;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #3b82f6;
  background: #eff6ff;
  padding: 4px 14px;
  border-radius: 100px;
  margin-bottom: 14px;
}
.section-title-group {
  text-align: center;
  margin-bottom: 72px;
}
.section-title {
  font-size: 36px;
  font-weight: 800;
  color: #0f172a;
  margin-bottom: 12px;
  letter-spacing: -0.5px;
}
.section-subtitle {
  font-size: 16px;
  color: #64748b;
  max-width: 480px;
  margin: 0 auto;
  line-height: 1.7;
}

/* ── 纵向特性列表 ──────────────────────────────────────── */
.features-list {
  display: flex;
  flex-direction: column;
  gap: 32px;
}
.feature-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  border-radius: 28px;
  overflow: hidden;
  box-shadow: 0 4px 24px rgba(0,0,0,0.06);
  border: 1px solid rgba(0,0,0,0.04);
  min-height: 360px;
  transition: box-shadow 0.3s, transform 0.3s;
}
.feature-row:hover {
  box-shadow: 0 16px 48px rgba(0,0,0,0.1);
  transform: translateY(-4px);
}
.feature-row--reverse .feature-visual { order: 2; }
.feature-row--reverse .feature-content { order: 1; }

/* ── 视觉面板 ──────────────────────────────────────────── */
.feature-visual {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 48px 36px;
  overflow: hidden;
}
.feature-visual::before {
  content: '';
  position: absolute;
  inset: 0;
  background-image:
    radial-gradient(circle at 20% 20%, rgba(255,255,255,0.4) 0%, transparent 50%),
    radial-gradient(circle at 80% 80%, rgba(255,255,255,0.2) 0%, transparent 50%);
  pointer-events: none;
}
.feature-visual-inner {
  position: relative;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
}
.feature-big-icon {
  width: 72px;
  height: 72px;
  border-radius: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(0,0,0,0.15);
  flex-shrink: 0;
}
.feature-seq {
  position: absolute;
  top: -8px;
  right: -4px;
  font-size: 72px;
  font-weight: 900;
  opacity: 0.08;
  line-height: 1;
  letter-spacing: -4px;
  user-select: none;
}

/* ── 装饰：题型芯片 ────────────────────────────────────── */
.deco-qtype {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  justify-content: center;
  max-width: 220px;
}
.dq-chip {
  padding: 8px 18px;
  background: #fff;
  border-radius: 100px;
  font-size: 13px;
  font-weight: 600;
  color: var(--chip-c);
  box-shadow: 0 2px 10px rgba(0,0,0,0.08);
  border: 1.5px solid var(--chip-c);
  white-space: nowrap;
}

/* ── 装饰：AI 神经网络 ─────────────────────────────────── */
.deco-ai {
  position: relative;
  width: 140px;
  height: 140px;
}
.deco-ai-ring {
  position: absolute;
  border-radius: 50%;
  border: 2px dashed rgba(139,92,246,0.3);
  animation: spin-slow linear infinite;
}
.deco-ai-ring--1 { inset: 0; animation-duration: 12s; }
.deco-ai-ring--2 { inset: 16px; border-style: solid; opacity: 0.2; animation-duration: 8s; animation-direction: reverse; }
@keyframes spin-slow { to { transform: rotate(360deg); } }
.deco-ai-core {
  position: absolute;
  inset: 36px;
  background: linear-gradient(135deg, #7c3aed, #a78bfa);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 20px rgba(124,58,237,0.4);
}
.deco-ai-node {
  position: absolute;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #8b5cf6;
  box-shadow: 0 2px 6px rgba(139,92,246,0.5);
}
.node-1 { top: 4px; left: 50%; transform: translateX(-50%); }
.node-2 { bottom: 4px; left: 50%; transform: translateX(-50%); }
.node-3 { left: 4px; top: 50%; transform: translateY(-50%); }
.node-4 { right: 4px; top: 50%; transform: translateY(-50%); }
.node-5 { top: 18px; right: 18px; width: 8px; height: 8px; opacity: 0.6; }
.node-6 { bottom: 18px; left: 18px; width: 8px; height: 8px; opacity: 0.6; }

/* ── 装饰：成绩卡 ──────────────────────────────────────── */
.deco-score-card {
  background: #fff;
  border-radius: 20px;
  padding: 20px 24px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.12);
  min-width: 160px;
  text-align: center;
}
.dsc-label { font-size: 11px; color: #94a3b8; font-weight: 600; letter-spacing: 1px; margin-bottom: 4px; }
.dsc-num { font-size: 48px; font-weight: 900; line-height: 1; margin-bottom: 12px; }
.dsc-bar-wrap {
  display: flex;
  align-items: flex-end;
  justify-content: center;
  gap: 5px;
  height: 40px;
  margin-bottom: 10px;
}
.dsc-bar {
  width: 14px;
  border-radius: 4px 4px 0 0;
  opacity: 0.75;
}
.dsc-tag { font-size: 11px; color: #fff; background: #f59e0b; border-radius: 100px; padding: 3px 10px; display: inline-block; }

/* ── 装饰：柱状图 ──────────────────────────────────────── */
.deco-chart {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  height: 100px;
  padding-bottom: 2px;
  position: relative;
}
.deco-chart-bar {
  flex: 1;
  border-radius: 6px 6px 0 0;
  min-width: 18px;
  animation: bar-grow 0.8s ease backwards;
}
@keyframes bar-grow {
  from { transform: scaleY(0); transform-origin: bottom; }
  to   { transform: scaleY(1); transform-origin: bottom; }
}
.deco-chart-line {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: rgba(16,185,129,0.2);
  border-radius: 1px;
}

/* ── 文字内容面板 ─────────────────────────────────────── */
.feature-content {
  background: #fff;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 52px 52px 52px 48px;
}
.feature-eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.5px;
  padding: 5px 14px;
  border-radius: 100px;
  width: fit-content;
  margin-bottom: 16px;
}
.feature-title {
  font-size: 26px;
  font-weight: 800;
  color: #0f172a;
  margin-bottom: 14px;
  line-height: 1.3;
  letter-spacing: -0.3px;
}
.feature-desc {
  font-size: 15px;
  color: #64748b;
  line-height: 1.8;
  margin-bottom: 28px;
}
.feature-points {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.feature-points li {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  color: #334155;
  font-weight: 500;
}
.fp-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}
.feature-deco {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 8px;
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
@media (max-width: 900px) {
  .feature-row {
    grid-template-columns: 1fr;
    min-height: unset;
  }
  .feature-row--reverse .feature-visual { order: 0; }
  .feature-row--reverse .feature-content { order: 1; }
  .feature-visual { min-height: 220px; padding: 40px 24px 36px; }
  .feature-content { padding: 36px 32px; }
  .feature-seq { font-size: 52px; }
}
@media (max-width: 768px) {
  .hero-title { font-size: 36px; }
  .hero-subtitle { font-size: 15px; }
  .hero-stats { gap: 28px; }
  .stat-num { font-size: 22px; }
  .section-title { font-size: 28px; }
  .cta-inner { flex-direction: column; text-align: center; }
  .section-header { flex-direction: column; align-items: flex-start; gap: 12px; }
  .features-list { gap: 20px; }
  .feature-title { font-size: 22px; }
}
@media (max-width: 480px) {
  .hero { padding-top: 64px; }
  .hero-title { font-size: 28px; }
  .hero-btn-primary, .hero-btn-outline { height: 44px; padding: 0 24px; font-size: 15px; }
  .exam-grid { grid-template-columns: 1fr; }
  .books-grid { grid-template-columns: 1fr 1fr; }
  .feature-content { padding: 28px 24px; }
  .feature-title { font-size: 20px; }
  .feature-desc { font-size: 14px; }
}
</style>
