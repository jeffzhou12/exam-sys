<template>
  <div class="book-list-page container">
    <!-- 搜索 + 筛选 -->
    <div class="filter-bar">
      <el-input
        v-model="query.keyword"
        placeholder="搜索书名 / 作者"
        clearable
        :prefix-icon="Search"
        class="filter-input"
        @keyup.enter="fetchBooks"
      />
      <el-select v-model="query.category" placeholder="所有分类" clearable class="filter-select" @change="fetchBooks">
        <el-option v-for="c in categories" :key="c" :label="c" :value="c" />
      </el-select>
      <el-button type="primary" :icon="Search" @click="fetchBooks">搜索</el-button>
    </div>

    <!-- 标签快速筛选 -->
    <div class="tag-filter">
      <span class="tag-filter-label">标签：</span>
      <el-check-tag
        v-for="tag in popularTags"
        :key="tag"
        :checked="query.tag === tag"
        @change="toggleTag(tag)"
        style="margin:4px"
      >{{ tag }}</el-check-tag>
    </div>

    <!-- 图书卡片列表 -->
    <div v-loading="loading" class="book-grid">
      <div
        v-for="book in books"
        :key="book.id"
        class="book-card"
        @click="openBook(book)"
      >
        <div class="book-cover" style="position:relative;">
          <img v-if="book.coverImageUrl" :src="book.coverImageUrl" :alt="book.title" />
          <div v-else class="cover-placeholder" :class="{ 'cover-has-pdf': book.hasPdf }">
            <el-icon size="40" :color="book.hasPdf ? '#409eff' : '#bbb'"><Reading /></el-icon>
          </div>
          <div class="pdf-badge" v-if="book.hasPdf">
            <el-tooltip content="已上传 PDF" placement="top">
              <el-icon size="18" color="#409eff" style="vertical-align:middle;"><Document /></el-icon>
            </el-tooltip>
          </div>
          <div class="pdf-badge no-pdf" v-else>
            <el-tooltip content="暂无 PDF" placement="top">
              <el-icon size="18" color="#dcdfe6" style="vertical-align:middle;"><Document /></el-icon>
            </el-tooltip>
          </div>
        </div>
        <div class="book-info">
          <div class="book-title" :title="book.title">{{ book.title }}</div>
          <div class="book-author text-muted">{{ book.author }}</div>
          <div class="book-category">
            <el-tag type="info" size="small">{{ book.category }}</el-tag>
          </div>
          <div class="book-tags">
            <el-tag
              v-for="tag in parseTags(book.tags).slice(0, 3)"
              :key="tag"
              size="small"
              style="margin:2px"
            >{{ tag }}</el-tag>
          </div>
          <div class="book-fav" @click.stop>
            <FavoriteButton :target-type="3" :target-id="book.id" />
          </div>
        </div>
      </div>

      <el-empty v-if="!loading && books.length === 0" description="暂无图书" style="grid-column:1/-1" />
    </div>

    <div class="pagination-wrap">
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[12, 24, 48]"
        layout="total, sizes, prev, pager, next"
        @change="fetchBooks"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { booksApi } from '@/api/books'
import { Search, Reading, Document } from '@element-plus/icons-vue'
import FavoriteButton from '@/components/FavoriteButton.vue'

const router = useRouter()
const books = ref([])
const total = ref(0)
const loading = ref(false)

const categories = [
  '计算机基础', '算法与数据结构', '数据库', '软件工程',
  '操作系统', '计算机网络', '编程语言', '人工智能', '数学', '其他'
]
const popularTags = ['经典教材', 'TCP/IP', 'HTTP', 'SQL', '算法', '数据结构', '软件工程', '重构']

const query = reactive({
  keyword: '',
  category: '',
  tag: '',
  page: 1,
  pageSize: 12
})

function parseTags(tagsJson) {
  if (!tagsJson) return []
  try { return JSON.parse(tagsJson) } catch { return [] }
}

function toggleTag(tag) {
  query.tag = query.tag === tag ? '' : tag
  fetchBooks()
}

async function fetchBooks() {
  loading.value = true
  try {
    const params = { ...query }
    if (!params.keyword) delete params.keyword
    if (!params.category) delete params.category
    if (!params.tag) delete params.tag
    const res = await booksApi.getBooks(params)
    books.value = res.items || []
    total.value = res.totalCount || 0
  } catch {
    ElMessage.error('加载图书失败')
  } finally {
    loading.value = false
  }
}

function openBook(book) {
  if (!book.hasPdf) {
    ElMessage.warning('该图书暂无 PDF 文件')
    return
  }
  router.push(`/books/${book.id}`)
}

onMounted(fetchBooks)
</script>

<style scoped>
/* ── 页面布局 ─────────────────────────────────────────── */
.book-list-page {
  padding: 40px 24px 72px;
  max-width: 1200px;
  margin: 0 auto;
}

/* ── 筛选区域 ─────────────────────────────────────────── */
.filter-bar {
  display: flex;
  gap: 12px;
  align-items: center;
  flex-wrap: wrap;
  margin-bottom: 16px;
  padding: 20px 24px;
  background: #fff;
  border-radius: 16px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
}
.filter-input { width: 280px; }
.filter-select { width: 160px; }

.tag-filter {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  margin-bottom: 28px;
  padding: 14px 18px;
  background: #fff;
  border-radius: 12px;
  border: 1px solid #f1f5f9;
  gap: 6px;
}
.tag-filter-label {
  color: #64748b;
  font-size: 13px;
  font-weight: 500;
  margin-right: 4px;
}

/* ── 图书网格 ─────────────────────────────────────────── */
.book-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 22px;
  min-height: 300px;
}

.book-card {
  background: #fff;
  border-radius: 16px;
  border: 1px solid #f1f5f9;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
  overflow: hidden;
  cursor: pointer;
  transition: all 0.25s ease;
}
.book-card:hover {
  box-shadow: 0 10px 28px rgba(29,78,216,0.1);
  transform: translateY(-5px);
  border-color: #bfdbfe;
}

/* 封面 */
.book-cover {
  width: 100%;
  height: 230px;
  overflow: hidden;
  background: linear-gradient(135deg, #f1f5f9, #e2e8f0);
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
}
.book-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}
.book-card:hover .book-cover img { transform: scale(1.04); }

.cover-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  gap: 8px;
  color: #94a3b8;
  font-size: 12px;
}
.cover-placeholder.cover-has-pdf {
  background: linear-gradient(135deg, #eff6ff, #dbeafe);
  color: #3b82f6;
}

.pdf-badge {
  position: absolute;
  right: 8px;
  bottom: 8px;
  background: rgba(255,255,255,0.92);
  border-radius: 50%;
  padding: 4px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.1);
  z-index: 2;
  backdrop-filter: blur(4px);
}
.pdf-badge.no-pdf { opacity: 0.4; }

/* 信息区 */
.book-info {
  padding: 14px 14px 12px;
}
.book-title {
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  margin-bottom: 4px;
}
.book-author {
  font-size: 12px;
  color: #94a3b8;
  margin-bottom: 8px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.book-category { margin-bottom: 6px; }
.book-tags { display: flex; flex-wrap: wrap; gap: 3px; }
.text-muted { color: #94a3b8; }

/* 分页 */
.pagination-wrap {
  margin-top: 36px;
  display: flex;
  justify-content: center;
}

/* ── 响应式 ──────────────────────────────────────────── */
@media (max-width: 768px) {
  .book-list-page { padding: 24px 16px 48px; }
  .filter-bar { flex-direction: column; align-items: stretch; }
  .filter-input, .filter-select { width: 100%; }
  .book-grid { grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 14px; }
  .book-cover { height: 180px; }
}
</style>
