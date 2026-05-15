<template>
  <div class="book-list-page">
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
        <div class="book-cover">
          <img v-if="book.coverImageUrl" :src="book.coverImageUrl" :alt="book.title" />
          <div v-else class="cover-placeholder">
            <el-icon size="40" color="#bbb"><Reading /></el-icon>
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
import { Search, Reading } from '@element-plus/icons-vue'

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
  if (!book.pdfFilePath) {
    ElMessage.warning('该图书暂无 PDF 文件')
    return
  }
  router.push(`/books/${book.id}`)
}

onMounted(fetchBooks)
</script>

<style scoped>
.book-list-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px 16px;
}

.filter-bar {
  display: flex;
  gap: 12px;
  align-items: center;
  margin-bottom: 16px;
}
.filter-input { width: 280px; }
.filter-select { width: 160px; }

.tag-filter {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  margin-bottom: 24px;
  padding: 12px 16px;
  background: #f5f7fa;
  border-radius: 8px;
}
.tag-filter-label {
  color: #666;
  font-size: 14px;
  margin-right: 8px;
}

.book-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 20px;
  min-height: 300px;
}

.book-card {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,.06);
  overflow: hidden;
  cursor: pointer;
  transition: box-shadow .2s, transform .2s;
}
.book-card:hover {
  box-shadow: 0 4px 16px rgba(0,0,0,.12);
  transform: translateY(-2px);
}

.book-cover {
  width: 100%;
  height: 200px;
  overflow: hidden;
  background: #f0f2f5;
  display: flex;
  align-items: center;
  justify-content: center;
}
.book-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.cover-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
}

.book-info {
  padding: 12px;
}
.book-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  margin-bottom: 4px;
}
.book-author {
  font-size: 12px;
  margin-bottom: 6px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.book-category { margin-bottom: 4px; }
.book-tags { display: flex; flex-wrap: wrap; }
.text-muted { color: #999; }

.pagination-wrap {
  margin-top: 24px;
  display: flex;
  justify-content: center;
}
</style>
