<template>
  <div class="favorites-page container">
    <div class="page-header">
      <h1>我的收藏</h1>
      <p>管理你收藏的题目、考试和图书</p>
    </div>

    <!-- 类型切换 -->
    <el-tabs v-model="activeType" class="fav-tabs" @tab-change="onTypeChange">
      <el-tab-pane label="题目" :name="FavoriteType.QUESTION" />
      <el-tab-pane label="考试" :name="FavoriteType.EXAM" />
      <el-tab-pane label="图书" :name="FavoriteType.BOOK" />
    </el-tabs>

    <div v-loading="loading" class="fav-content">
      <template v-if="items.length > 0">

        <!-- ── 题目收藏 ── -->
        <template v-if="activeType === FavoriteType.QUESTION">
          <div class="question-list">
            <div v-for="(item, idx) in items" :key="item.favoriteId" class="question-card">
              <div class="q-index">{{ idx + 1 }}</div>
              <div class="q-body">
                <div class="q-title" v-html="item.title" />
                <div class="q-meta" v-if="item.subtitle">
                  <el-tag size="small" type="info">{{ item.subtitle }}</el-tag>
                </div>
              </div>
              <div class="q-actions">
                <el-button size="small" type="primary" plain round @click="goQuestion(item.targetId)">去练习</el-button>
                <el-button size="small" type="danger" plain round @click="removeFav(item)">取消收藏</el-button>
              </div>
            </div>
          </div>
        </template>

        <!-- ── 考试收藏 ── -->
        <template v-else-if="activeType === FavoriteType.EXAM">
          <div class="exam-grid">
            <div v-for="item in items" :key="item.favoriteId" class="exam-card">
              <div class="card-top">
                <el-tag type="primary" size="small">考试</el-tag>
              </div>
              <h3 class="card-title">{{ item.title }}</h3>
              <p class="card-desc">{{ item.subtitle || '暂无简介' }}</p>
              <div class="card-actions">
                <router-link :to="`/exams/${item.targetId}`" style="display:inline-flex">
                  <el-button size="small" type="primary" round>查看详情</el-button>
                </router-link>
                <el-button size="small" type="danger" plain round @click="removeFav(item)">取消收藏</el-button>
              </div>
            </div>
          </div>
        </template>

        <!-- ── 图书收藏 ── -->
        <template v-else-if="activeType === FavoriteType.BOOK">
          <div class="book-grid">
            <div v-for="item in items" :key="item.favoriteId" class="book-card">
              <div class="book-cover">
                <div class="cover-placeholder">
                  <el-icon size="40" color="#409eff"><Reading /></el-icon>
                </div>
              </div>
              <div class="book-info">
                <div class="book-title">{{ item.title }}</div>
                <div class="book-author text-muted" v-if="item.subtitle">{{ item.subtitle }}</div>
                <div class="book-actions">
                  <router-link :to="`/books/${item.targetId}/read`" style="display:inline-flex">
                    <el-button size="small" type="primary" round>立即阅读</el-button>
                  </router-link>
                  <el-button size="small" type="danger" plain round @click="removeFav(item)">取消收藏</el-button>
                </div>
              </div>
            </div>
          </div>
        </template>

      </template>

      <el-empty v-else-if="!loading" description="暂无收藏" class="empty-wrap" />
    </div>

    <div class="pagination-wrap" v-if="total > pageSize">
      <el-pagination
        v-model:current-page="page"
        :page-size="pageSize"
        :total="total"
        layout="prev, pager, next"
        @current-change="fetchFavs"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { favoritesApi, FavoriteType } from '@/api/favorites'
import { useRouter } from 'vue-router'
import { Reading } from '@element-plus/icons-vue'

const router = useRouter()

const activeType = ref(FavoriteType.QUESTION)
const items      = ref([])
const total      = ref(0)
const page       = ref(1)
const pageSize   = ref(20)
const loading    = ref(false)

onMounted(() => fetchFavs())

async function fetchFavs() {
  loading.value = true
  try {
    const res = await favoritesApi.getList(activeType.value, page.value, pageSize.value)
    items.value = res.items
    total.value = res.total
  } catch {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

function onTypeChange() {
  page.value = 1
  fetchFavs()
}

async function removeFav(item) {
  await favoritesApi.toggle(activeType.value, item.targetId)
  items.value = items.value.filter(i => i.favoriteId !== item.favoriteId)
  total.value--
  ElMessage.success('已取消收藏')
}

function goQuestion(id) {
  router.push(`/practice?questionId=${id}`)
}
</script>

<style scoped>
.favorites-page { padding: 48px 0 72px; }
.page-header { margin-bottom: 28px; }
.page-header h1 {
  font-size: 32px;
  font-weight: 800;
  color: #0f172a;
  letter-spacing: -0.5px;
}
.page-header p {
  color: #64748b;
  margin-top: 6px;
  font-size: 15px;
}
.fav-tabs { margin-bottom: 24px; }
.fav-content { min-height: 200px; }
.empty-wrap { padding: 60px 0; }
.pagination-wrap { display: flex; justify-content: center; margin-top: 40px; }

/* ── 题目列表 ──────────────────────────────────────────── */
.question-list { display: flex; flex-direction: column; gap: 12px; }
.question-card {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  background: #fff;
  border: 1px solid #f1f5f9;
  border-radius: 12px;
  padding: 16px 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  transition: all 0.25s ease;
}
.question-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(29,78,216,0.08);
  border-color: #bfdbfe;
}
.q-index {
  flex-shrink: 0;
  width: 28px;
  height: 28px;
  background: linear-gradient(135deg, #1d4ed8, #3b82f6);
  color: #fff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 700;
  margin-top: 2px;
}
.q-body { flex: 1; min-width: 0; }
.q-title {
  font-size: 14px;
  line-height: 1.6;
  color: #0f172a;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.q-meta { margin-top: 6px; }
.q-actions { display: flex; flex-direction: column; gap: 6px; flex-shrink: 0; align-items: flex-end; }

/* ── 考试卡片网格 ──────────────────────────────────────── */
.exam-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}
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
  position: relative;
  overflow: hidden;
}
.exam-card::after {
  content: '';
  position: absolute;
  top: 0; left: 0; right: 0;
  height: 3px;
  background: linear-gradient(90deg, #1d4ed8, #3b82f6);
  opacity: 0;
  transition: opacity 0.25s;
}
.exam-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(29,78,216,0.1);
  border-color: #bfdbfe;
}
.exam-card:hover::after { opacity: 1; }
.card-top { display: flex; align-items: center; justify-content: space-between; }
.card-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.4;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.card-desc {
  font-size: 13px;
  color: #94a3b8;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.6;
  flex: 1;
}
.card-actions {
  display: flex;
  gap: 10px;
  margin-top: 4px;
  padding-top: 12px;
  border-top: 1px solid #f8fafc;
}

/* ── 图书卡片网格 ──────────────────────────────────────── */
.book-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}
.book-card {
  background: #fff;
  border: 1px solid #f1f5f9;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  transition: all 0.25s ease;
  cursor: pointer;
  display: flex;
  flex-direction: column;
}
.book-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 28px rgba(0,0,0,0.1);
  border-color: #bfdbfe;
}
.book-cover {
  height: 140px;
  background: linear-gradient(135deg, #eff6ff, #dbeafe);
  display: flex;
  align-items: center;
  justify-content: center;
}
.book-info { padding: 14px; flex: 1; display: flex; flex-direction: column; }
.book-title {
  font-size: 14px;
  font-weight: 600;
  color: #0f172a;
  line-height: 1.4;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  margin-bottom: 4px;
}
.book-author {
  font-size: 12px;
  margin-bottom: 10px;
}
.book-actions { display: flex; flex-direction: row; flex-wrap: wrap; gap: 6px; margin-top: auto; padding-top: 10px; }
.text-muted { color: var(--el-text-color-secondary); }

/* ── 响应式 ──────────────────────────────────────────── */
@media (max-width: 768px) {
  .favorites-page { padding: 32px 0 48px; }
  .exam-grid { grid-template-columns: 1fr; }
  .book-grid { grid-template-columns: repeat(2, 1fr); gap: 14px; }
  .q-actions { flex-direction: row; }
}
</style>

