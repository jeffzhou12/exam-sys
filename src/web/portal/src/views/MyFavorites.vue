<template>
  <div class="favorites-page container">
    <div class="page-header">
      <h2>我的收藏</h2>
    </div>

    <!-- 类型切换 -->
    <el-tabs v-model="activeType" class="fav-tabs" @tab-change="onTypeChange">
      <el-tab-pane label="题目" :name="FavoriteType.QUESTION" />
      <el-tab-pane label="考试" :name="FavoriteType.EXAM" />
      <el-tab-pane label="图书" :name="FavoriteType.BOOK" />
    </el-tabs>

    <div v-loading="loading" class="fav-list">
      <template v-if="items.length > 0">
        <!-- 题目收藏 -->
        <template v-if="activeType === FavoriteType.QUESTION">
          <div v-for="item in items" :key="item.favoriteId" class="fav-card">
            <div class="fav-content">
              <div class="fav-title" v-html="item.title" />
              <div class="fav-sub text-muted" v-if="item.subtitle">知识点：{{ item.subtitle }}</div>
            </div>
            <div class="fav-actions">
              <el-button size="small" @click="goQuestion(item.targetId)">查看</el-button>
              <el-button size="small" type="danger" text @click="removeFav(item)">取消收藏</el-button>
            </div>
          </div>
        </template>

        <!-- 考试收藏 -->
        <template v-else-if="activeType === FavoriteType.EXAM">
          <div v-for="item in items" :key="item.favoriteId" class="fav-card">
            <div class="fav-content">
              <div class="fav-title">{{ item.title }}</div>
              <div class="fav-sub text-muted" v-if="item.subtitle">{{ item.subtitle }}</div>
            </div>
            <div class="fav-actions">
              <router-link :to="`/exams/${item.targetId}`">
                <el-button size="small">查看</el-button>
              </router-link>
              <el-button size="small" type="danger" text @click="removeFav(item)">取消收藏</el-button>
            </div>
          </div>
        </template>

        <!-- 图书收藏 -->
        <template v-else-if="activeType === FavoriteType.BOOK">
          <div v-for="item in items" :key="item.favoriteId" class="fav-card">
            <div class="fav-content">
              <div class="fav-title">{{ item.title }}</div>
              <div class="fav-sub text-muted" v-if="item.subtitle">作者：{{ item.subtitle }}</div>
            </div>
            <div class="fav-actions">
              <router-link :to="`/books/${item.targetId}/read`">
                <el-button size="small">阅读</el-button>
              </router-link>
              <el-button size="small" type="danger" text @click="removeFav(item)">取消收藏</el-button>
            </div>
          </div>
        </template>
      </template>

      <el-empty v-else-if="!loading" description="暂无收藏" />
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
.favorites-page { padding: 24px 0; }
.page-header h2 { margin-bottom: 8px; }
.fav-tabs { margin-bottom: 16px; }
.fav-list { min-height: 200px; }

.fav-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  margin-bottom: 10px;
  transition: box-shadow .2s;
}
.fav-card:hover { box-shadow: 0 2px 8px rgba(0,0,0,.08); }

.fav-content { flex: 1; min-width: 0; margin-right: 16px; }
.fav-title { font-size: 14px; line-height: 1.5; }
.fav-sub { font-size: 12px; margin-top: 4px; }
.fav-actions { display: flex; gap: 8px; flex-shrink: 0; }

.text-muted { color: var(--el-text-color-secondary); }
.pagination-wrap { display: flex; justify-content: center; margin-top: 24px; }
</style>
