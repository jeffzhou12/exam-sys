<template>
  <div class="books-page">
    <!-- 搜索栏 -->
    <el-card shadow="never" class="search-card">
      <el-row :gutter="16" align="middle">
        <el-col :span="6">
          <el-input
            v-model="query.keyword"
            placeholder="书名 / 作者 / ISBN"
            clearable
            :prefix-icon="Search"
            @keyup.enter="fetchBooks"
          />
        </el-col>
        <el-col :span="4">
          <el-select v-model="query.category" placeholder="分类" clearable style="width:100%">
            <el-option v-for="c in categories" :key="c" :label="c" :value="c" />
          </el-select>
        </el-col>
        <el-col :span="3">
          <el-select v-model="query.isActive" placeholder="状态" clearable style="width:100%">
            <el-option label="已上架" :value="true" />
            <el-option label="已下架" :value="false" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-button type="primary" :icon="Search" @click="fetchBooks">搜索</el-button>
          <el-button :icon="Refresh" @click="resetQuery">重置</el-button>
        </el-col>
        <el-col :span="7" style="text-align:right">
          <el-button
            v-if="auth.isAdminOrTeacher"
            type="primary"
            :icon="Plus"
            @click="openCreateDialog"
          >新增图书</el-button>
        </el-col>
      </el-row>
    </el-card>

    <!-- 表格 -->
    <el-card shadow="never" style="margin-top:16px">
      <el-table v-loading="loading" :data="books" stripe>
        <el-table-column label="书名" min-width="220">
          <template #default="{ row }">
            <div class="book-title-cell">
              <el-icon v-if="!row.coverImageUrl" size="32" color="#ddd"><Reading /></el-icon>
              <img v-else :src="row.coverImageUrl" class="cover-thumb" />
              <div>
                <div class="title">{{ row.title }}</div>
                <div class="author text-muted">{{ row.author }}</div>
              </div>
            </div>
          </template>
        </el-table-column>

        <el-table-column prop="category" label="分类" min-width="85" />
        <el-table-column label="状态" min-width="70" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
              {{ row.isActive ? '已上架' : '已下架' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="上传者" prop="uploadedByName" min-width="80" />
        <el-table-column v-if="isAllTenantsMode" label="所属租户" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ tenantNameMap[row.tenantId] || row.tenantId?.slice(0, 8) || '—' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="320" fixed="right">
          <template #default="{ row }">
            <el-button type="success" size="small" :icon="View" @click="openPreview(row)">预览</el-button>
            <el-button
              v-if="auth.isAdminOrTeacher"
              type="primary"
              size="small"
              :icon="Edit"
              @click="openEditDialog(row)"
            >编辑</el-button>
            <el-button
              v-if="auth.isAdminOrTeacher"
              type="warning"
              size="small"
              :icon="Upload"
              @click="openUploadDialog(row)"
            >上传</el-button>
            <el-button
              v-if="auth.isAnyAdmin"
              type="danger"
              size="small"
              :icon="Delete"
              @click="handleDelete(row)"
            >删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @change="fetchBooks"
        />
      </div>
    </el-card>

    <!-- 新增 / 编辑 Dialog -->
    <el-dialog
      v-model="formDialog.visible"
      :title="formDialog.isEdit ? '编辑图书' : '新增图书'"
      width="700px"
      destroy-on-close
    >
      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="80px">
        <!-- SuperAdmin 在全租户模式下新增时，选择目标租户 -->
        <el-form-item
          v-if="auth.isSuperAdmin && isAllTenantsMode && !formDialog.isEdit"
          label="所属租户"
          required
        >
          <el-select
            v-model="formDialog.tenantId"
            placeholder="请选择图书所属租户"
            style="width:100%"
          >
            <el-option v-for="t in allTenants" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="15">
            <el-form-item label="书名" prop="title">
              <el-input v-model="formData.title" maxlength="200" show-word-limit />
            </el-form-item>
          </el-col>
          <el-col :span="9">
            <el-form-item label="作者" prop="author">
              <el-input v-model="formData.author" maxlength="100" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出版社">
              <el-input v-model="formData.publisher" maxlength="100" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="分类" prop="category">
              <el-select
                v-model="formData.category"
                filterable
                allow-create
                default-first-option
                placeholder="选择或输入分类"
                style="width:100%"
              >
                <el-option v-for="c in categories" :key="c" :label="c" :value="c" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="简介">
              <el-input v-model="formData.description" type="textarea" :rows="3" maxlength="1000" show-word-limit />
            </el-form-item>
          </el-col>
          <el-col :span="9">
            <el-form-item label="ISBN">
              <el-input v-model="formData.isbn" maxlength="20" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="出版年份">
              <el-input-number
                v-model="formData.publishYear"
                :min="1900"
                :max="2099"
                controls-position="right"
                style="width:100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="7">
            <el-form-item label="页数">
              <el-input-number
                v-model="formData.pageCount"
                :min="1"
                controls-position="right"
                style="width:100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="标签">
              <div class="tag-editor">
                <el-tag
                  v-for="(tag, i) in formData.tagList"
                  :key="i"
                  closable
                  @close="removeTag(i)"
                  style="margin:3px"
                >{{ tag }}</el-tag>
                <el-input
                  v-if="tagInputVisible"
                  ref="tagInputRef"
                  v-model="tagInputValue"
                  size="small"
                  style="width:100px;margin:3px"
                  @keyup.enter="addTag"
                  @blur="addTag"
                />
                <el-button v-else size="small" plain style="margin:3px" @click="showTagInput">+ 添加</el-button>
              </div>
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="封面图">
              <el-radio-group v-model="coverMode" size="small" style="margin-bottom:8px">
                <el-radio-button value="url">URL 链接</el-radio-button>
                <el-radio-button value="upload">上传图片</el-radio-button>
              </el-radio-group>
              <div v-if="coverMode === 'url'" class="cover-input-row">
                <el-input v-model="formData.coverImageUrl" placeholder="https://..." clearable />
                <img v-if="formData.coverImageUrl" :src="formData.coverImageUrl" class="cover-mini-thumb" />
              </div>
              <div v-else class="cover-input-row">
                <el-upload
                  :auto-upload="false"
                  accept="image/jpeg,image/png,image/webp,image/gif"
                  :limit="1"
                  :show-file-list="false"
                  :on-change="handleCoverChange"
                >
                  <el-button :icon="UploadFilled" size="small">选择图片</el-button>
                </el-upload>
                <template v-if="coverPreviewUrl">
                  <img :src="coverPreviewUrl" class="cover-mini-thumb" />
                  <el-text type="success" size="small" style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{{ coverFileName }}</el-text>
                  <el-button link type="danger" size="small" @click="clearCoverUpload">清除</el-button>
                </template>
                <el-text v-else type="info" size="small">未选择图片</el-text>
              </div>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="状态">
              <el-radio-group v-model="formData.isActive">
                <el-radio-button :value="true">上架</el-radio-button>
                <el-radio-button :value="false">下架</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="formDialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submitForm">保存</el-button>
      </template>
    </el-dialog>

    <!-- 上传 PDF Dialog -->
    <el-dialog v-model="uploadDialog.visible" title="上传 PDF 文件" width="480px" destroy-on-close>
      <div v-if="uploadDialog.book" class="upload-book-info">
        <el-text>《{{ uploadDialog.book.title }}》</el-text>
      </div>
      <el-upload
        ref="uploadRef"
        drag
        :auto-upload="false"
        accept=".pdf,application/pdf"
        :limit="1"
        :on-change="handleFileChange"
      >
        <el-icon size="48"><UploadFilled /></el-icon>
        <div class="el-upload__text">将 PDF 拖到此处，或 <em>点击上传</em></div>
        <template #tip>
          <div class="el-upload__tip">仅支持 PDF 文件，最大 200 MB</div>
        </template>
      </el-upload>
      <el-progress
        v-if="uploadProgress > 0"
        :percentage="uploadProgress"
        :status="uploadProgress >= 100 ? '' : undefined"
        style="margin-top:16px"
      />
      <div v-if="uploadStatusText" style="margin-top:8px;text-align:center;color:#909399;font-size:13px">
        {{ uploadStatusText }}
      </div>
      <template #footer>
        <el-button @click="uploadDialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="uploading" :disabled="!selectedFile" @click="submitUpload">
          开始上传
        </el-button>
      </template>
    </el-dialog>

    <!-- 预览 Drawer -->
    <el-drawer
      v-model="previewDialog.visible"
      :title="previewDialog.book?.title || '图书详情'"
      size="750px"
      destroy-on-close
      @open="onPreviewOpen"
      @close="onPreviewClose"
    >
      <template v-if="previewDialog.book">
        <!-- 封面区域 -->
        <div class="preview-cover-area">
          <img
            v-if="previewDialog.book.coverImageUrl"
            :src="previewDialog.book.coverImageUrl"
            class="preview-cover-img"
          />
          <!-- 无封面但有PDF：渲染PDF第一页作为封面 -->
          <img
            v-else-if="previewCoverDataUrl && previewCoverDataUrl !== false"
            :src="previewCoverDataUrl"
            class="preview-cover-img"
          />
          <div v-else-if="previewDialog.book.hasPdf && previewCoverLoading" class="preview-cover-placeholder">
            <el-icon size="32" class="is-loading" color="#409eff"><Loading /></el-icon>
            <div style="color:#999;font-size:12px;margin-top:8px">生成封面中…</div>
          </div>
          <div v-else class="preview-cover-placeholder">
            <el-icon size="56" color="#c0c4cc"><Reading /></el-icon>
            <div style="color:#c0c4cc;font-size:12px;margin-top:8px">暂无封面</div>
          </div>
        </div>

        <!-- 基本信息 -->
        <el-descriptions :column="2" border style="margin-top:16px">
          <el-descriptions-item label="作者">{{ previewDialog.book.author || '—' }}</el-descriptions-item>
          <el-descriptions-item label="出版社">{{ previewDialog.book.publisher || '—' }}</el-descriptions-item>
          <el-descriptions-item label="分类">{{ previewDialog.book.category || '—' }}</el-descriptions-item>
          <el-descriptions-item label="出版年份">{{ previewDialog.book.publishYear || '—' }}</el-descriptions-item>
          <el-descriptions-item label="ISBN">{{ previewDialog.book.isbn || '—' }}</el-descriptions-item>
          <el-descriptions-item label="页数">{{ previewDialog.book.pageCount || '—' }}</el-descriptions-item>
          <el-descriptions-item label="PDF" :span="1">
            <el-tag :type="previewDialog.book.hasPdf ? 'success' : 'info'" size="small">
              {{ previewDialog.book.hasPdf ? '已上传' : '未上传' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="previewDialog.book.isActive ? 'success' : 'info'" size="small">
              {{ previewDialog.book.isActive ? '已上架' : '已下架' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item v-if="previewDialog.book.tags?.length" label="标签" :span="2">
            <el-tag v-for="tag in previewDialog.book.tags" :key="tag" size="small" style="margin:2px">{{ tag }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item v-if="previewDialog.book.description" label="简介" :span="2">
            {{ previewDialog.book.description }}
          </el-descriptions-item>
        </el-descriptions>

        <!-- 操作按钮 -->
        <div v-if="auth.isAdminOrTeacher" class="preview-actions">
          <el-button type="primary" :icon="Edit" @click="openEditFromPreview">编辑图书</el-button>
          <el-button type="warning" :icon="Upload" @click="openUploadFromPreview">上传PDF</el-button>
        </div>

        <!-- PDF 预览 -->
        <div v-if="previewDialog.book.hasPdf" class="preview-pdf-section">
          <div class="preview-pdf-header">
            <span class="preview-pdf-title">
              <el-icon><Document /></el-icon> PDF 预览
            </span>
            <el-button
              size="small"
              :type="previewShowPdf ? 'info' : 'primary'"
              plain
              @click="togglePdfPreview"
            >{{ previewShowPdf ? '收起' : '展开预览' }}</el-button>
          </div>
          <div v-if="previewShowPdf" class="preview-pdf-viewer">
            <div v-if="previewPdfLoading" class="preview-pdf-loading">
              <el-icon size="24" class="is-loading" color="#409eff"><Loading /></el-icon>
              <span>加载 PDF…</span>
            </div>
            <iframe
              v-else-if="previewPdfBlobUrl"
              :src="previewPdfBlobUrl"
              class="preview-pdf-iframe"
            />
            <el-empty v-else description="PDF 加载失败" />
          </div>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, nextTick, onMounted, onBeforeUnmount } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { booksApi } from '@/api/books'
import { mediaApi } from '@/api/media'
import {
  Search, Refresh, Plus, Edit, Delete, Upload, Reading,
  Document, DocumentChecked, UploadFilled, OfficeBuilding, View, Loading
} from '@element-plus/icons-vue'

const auth = useAuthStore()

// 全租户模式：超级管理员且未选择具体租户
const isAllTenantsMode = computed(() => auth.isSuperAdmin && !auth.activeTenantId)

// 租户列表（SuperAdmin 用于展示租户名称）
const allTenants = ref([])
const tenantNameMap = computed(() =>
  Object.fromEntries(allTenants.value.map(t => [t.id, t.name]))
)

function syncTenantsFromCache() {
  try {
    const raw = localStorage.getItem('admin.tenants.cache')
    if (!raw) return
    const parsed = JSON.parse(raw)
    allTenants.value = Array.isArray(parsed) ? parsed : []
  } catch {
    allTenants.value = []
  }
}

function onTenantsUpdated(event) {
  const list = event?.detail
  allTenants.value = Array.isArray(list) ? list : []
}

const books = ref([])
const total = ref(0)
const loading = ref(false)

const query = reactive({
  keyword: '',
  category: '',
  isActive: null,
  page: 1,
  pageSize: 10
})

const categories = ref([
  '计算机基础', '算法与数据结构', '数据库', '软件工程',
  '操作系统', '计算机网络', '编程语言', '人工智能', '数学', '其他'
])

async function fetchBooks() {
  loading.value = true
  try {
    const params = { ...query }
    if (params.isActive === null || params.isActive === '') delete params.isActive
    if (!params.keyword) delete params.keyword
    if (!params.category) delete params.category
    const res = await booksApi.getList(params)
    books.value = res.items || []
    total.value = res.totalCount || 0
  } catch (e) {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  Object.assign(query, { keyword: '', category: '', isActive: null, page: 1, pageSize: 10 })
  fetchBooks()
}

onMounted(async () => {
  fetchBooks()
  if (auth.isSuperAdmin) syncTenantsFromCache()
  window.addEventListener('admin-tenants-updated', onTenantsUpdated)
})

onBeforeUnmount(() => {
  window.removeEventListener('admin-tenants-updated', onTenantsUpdated)
})

// 租户切换时刷新列表
watch(() => auth.activeTenantId, fetchBooks)

// ============ 表单 Dialog ============
const formDialog = reactive({ visible: false, isEdit: false, id: null, tenantId: null })
const formRef = ref(null)
const submitting = ref(false)

const defaultFormData = () => ({
  title: '', author: '', publisher: '', description: '',
  category: '', isbn: '', publishYear: new Date().getFullYear(),
  pageCount: null, coverImageUrl: '', isActive: true, tagList: []
})

const formData = reactive(defaultFormData())

const formRules = {
  title: [{ required: true, message: '请输入书名', trigger: 'blur' }],
  author: [{ required: true, message: '请输入作者', trigger: 'blur' }],
  category: [{ required: true, message: '请选择或输入分类', trigger: 'change' }]
}

// tag input
const tagInputVisible = ref(false)
const tagInputValue = ref('')
const tagInputRef = ref(null)

// 封面图模式（url / upload）
const coverMode = ref('url')
const coverFile = ref(null)        // 待上传的 File 对象
const coverPreviewUrl = ref(null)  // 本地 blob 预览 URL
const coverFileName = ref('')

/** 将 File/Blob 压缩为不超过 maxWidth 的 JPEG，返回新 File */
function compressCoverImage(file, maxWidth = 600, quality = 0.85) {
  return new Promise((resolve) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      const img = new Image()
      img.onload = () => {
        let w = img.width
        let h = img.height
        if (w > maxWidth) {
          h = Math.round((h * maxWidth) / w)
          w = maxWidth
        }
        const canvas = document.createElement('canvas')
        canvas.width  = w
        canvas.height = h
        canvas.getContext('2d').drawImage(img, 0, 0, w, h)
        canvas.toBlob(
          (blob) => resolve(new File([blob], file.name.replace(/\.[^.]+$/, '.jpg'), { type: 'image/jpeg' })),
          'image/jpeg',
          quality,
        )
      }
      img.src = e.target.result
    }
    reader.readAsDataURL(file)
  })
}

async function handleCoverChange(file) {
  const compressed = await compressCoverImage(file.raw)
  coverFile.value = compressed
  coverFileName.value = compressed.name
  if (coverPreviewUrl.value) URL.revokeObjectURL(coverPreviewUrl.value)
  coverPreviewUrl.value = URL.createObjectURL(compressed)
}

function clearCoverUpload() {
  if (coverPreviewUrl.value) URL.revokeObjectURL(coverPreviewUrl.value)
  coverFile.value = null
  coverPreviewUrl.value = null
  coverFileName.value = ''
}

function showTagInput() {
  tagInputVisible.value = true
  nextTick(() => tagInputRef.value?.focus())
}
function addTag() {
  const val = tagInputValue.value.trim()
  if (val && !formData.tagList.includes(val)) formData.tagList.push(val)
  tagInputVisible.value = false
  tagInputValue.value = ''
}
function removeTag(i) { formData.tagList.splice(i, 1) }

function openCreateDialog() {
  formDialog.isEdit = false
  formDialog.id = null
  formDialog.tenantId = auth.activeTenantId || null
  Object.assign(formData, defaultFormData())
  coverMode.value = 'url'
  clearCoverUpload()
  formDialog.visible = true
}

function openEditDialog(row) {
  formDialog.isEdit = true
  formDialog.id = row.id
  formDialog.tenantId = row.tenantId || null
  Object.assign(formData, {
    title: row.title, author: row.author, publisher: row.publisher || '',
    description: row.description || '', category: row.category || '',
    isbn: row.isbn || '', publishYear: row.publishYear || new Date().getFullYear(),
    pageCount: row.pageCount || null, coverImageUrl: row.coverImageUrl || '',
    isActive: row.isActive, tagList: Array.isArray(row.tags) ? [...row.tags] : []
  })
  coverMode.value = 'url'
  clearCoverUpload()
  formDialog.visible = true
}

async function submitForm() {
  await formRef.value?.validate()
  // 全租户模式下新增时，必须先选择租户
  if (isAllTenantsMode.value && !formDialog.isEdit && !formDialog.tenantId) {
    ElMessage.warning('请选择图书所属租户')
    return
  }
  submitting.value = true
  try {
    // 如果是上传模式且有待上传的封面文件，先上传图片
    if (coverMode.value === 'upload' && coverFile.value) {
      const res = await mediaApi.uploadImage(coverFile.value)
      formData.coverImageUrl = res.key  // 存储 key，由后端按请求动态拼 URL
    }

    const payload = { ...formData, tags: formData.tagList }
    delete payload.tagList
    const tenantOverride = isAllTenantsMode.value ? formDialog.tenantId : null

    if (formDialog.isEdit) {
      await booksApi.update(formDialog.id, payload, tenantOverride)
      ElMessage.success('更新成功')
    } else {
      await booksApi.create(payload, tenantOverride)
      ElMessage.success('创建成功')
    }
    formDialog.visible = false
    fetchBooks()
  } catch (e) {
    ElMessage.error(e?.message || '保存失败')
  } finally {
    submitting.value = false
  }
}

// ============ 上传 PDF ============
const uploadDialog = reactive({ visible: false, book: null })
const uploadRef = ref(null)
const selectedFile = ref(null)
const uploadProgress = ref(0)
const uploading = ref(false)
const uploadStatusText = ref('')

function openUploadDialog(row) {
  uploadDialog.book = row
  selectedFile.value = null
  uploadProgress.value = 0
  uploadStatusText.value = ''
  uploadDialog.visible = true
}

function handleFileChange(file) {
  if (file.raw.type !== 'application/pdf') {
    ElMessage.error('只能上传 PDF 文件')
    uploadRef.value?.clearFiles()
    return
  }
  if (file.size > 200 * 1024 * 1024) {
    ElMessage.error('文件大小不能超过 200 MB')
    uploadRef.value?.clearFiles()
    return
  }
  selectedFile.value = file.raw
}

async function submitUpload() {
  if (!selectedFile.value) return
  uploading.value = true
  uploadProgress.value = 0
  uploadStatusText.value = '上传中…'
  try {
    const fd = new FormData()
    fd.append('file', selectedFile.value)
    const tenantOverride = isAllTenantsMode.value ? uploadDialog.book.tenantId : null
    await booksApi.uploadPdf(uploadDialog.book.id, fd, (e) => {
      uploadProgress.value = Math.round((e.loaded / e.total) * 100)
    }, tenantOverride)
    ElMessage.success('PDF 上传成功')
    uploadDialog.visible = false
    fetchBooks()
  } catch (e) {
    ElMessage.error(e?.message || '上传失败')
  } finally {
    uploading.value = false
    uploadStatusText.value = ''
  }
}

// ============ 删除 ============
async function handleDelete(row) {
  await ElMessageBox.confirm(`确定删除《${row.title}》？此操作不可撤销。`, '删除确认', {
    type: 'warning'
  })
  try {
    const tenantOverride = isAllTenantsMode.value ? row.tenantId : null
    await booksApi.delete(row.id, tenantOverride)
    ElMessage.success('删除成功')
    fetchBooks()
  } catch (e) {
    ElMessage.error(e?.message || '删除失败')
  }
}

// ============ 预览 ============
const previewDialog = reactive({ visible: false, book: null })
const previewCoverDataUrl = ref(null)    // PDF 首页渲染后的 dataURL
const previewCoverLoading = ref(false)
const previewPdfBlobUrl = ref(null)      // PDF blob URL（封面渲染 + iframe 复用）
const previewShowPdf = ref(false)
const previewPdfLoading = ref(false)

function openPreview(row) {
  previewDialog.book = row
  previewCoverDataUrl.value = null
  previewPdfBlobUrl.value = null
  previewShowPdf.value = false
  previewDialog.visible = true
}

async function onPreviewOpen() {
  const book = previewDialog.book
  if (!book) return
  // 无封面但有 PDF：加载 PDF 并将第一页渲染为封面
  if (!book.coverImageUrl && book.hasPdf) {
    await loadPdfCover(book)
  }
}

function onPreviewClose() {
  if (previewPdfBlobUrl.value) {
    URL.revokeObjectURL(previewPdfBlobUrl.value)
    previewPdfBlobUrl.value = null
  }
}

async function loadPdfCover(book) {
  previewCoverLoading.value = true
  try {
    const tenantOverride = isAllTenantsMode.value ? book.tenantId : null
    const blob = await booksApi.getPdfBlob(book.id, tenantOverride)
    const blobUrl = URL.createObjectURL(blob)
    previewPdfBlobUrl.value = blobUrl
    previewCoverDataUrl.value = false   // 预览面板封面仅靠 coverImageUrl，不再用 pdfjs 渲染
  } catch (e) {
    previewCoverDataUrl.value = false
  } finally {
    previewCoverLoading.value = false
  }
}

async function togglePdfPreview() {
  if (previewShowPdf.value) {
    previewShowPdf.value = false
    return
  }
  previewShowPdf.value = true
  if (previewPdfBlobUrl.value) return  // 已加载（封面渲染时复用）

  previewPdfLoading.value = true
  try {
    const book = previewDialog.book
    const tenantOverride = isAllTenantsMode.value ? book.tenantId : null
    const blob = await booksApi.getPdfBlob(book.id, tenantOverride)
    previewPdfBlobUrl.value = URL.createObjectURL(blob)
  } catch (e) {
    ElMessage.error('PDF 加载失败')
    previewShowPdf.value = false
  } finally {
    previewPdfLoading.value = false
  }
}

function openEditFromPreview() {
  const row = previewDialog.book
  previewDialog.visible = false
  openEditDialog(row)
}

function openUploadFromPreview() {
  const row = previewDialog.book
  previewDialog.visible = false
  openUploadDialog(row)
}
</script>

<style scoped>
.search-card { margin-bottom: 0; }

.book-title-cell {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  max-width: 100%;
  overflow: hidden;
}
.book-title-cell > div {
  min-width: 0;
  overflow: hidden;
}
.cover-thumb {
  width: 36px;
  height: 48px;
  object-fit: cover;
  border-radius: 2px;
  flex-shrink: 0;
}
.title { font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.text-muted { color: #999; font-size: 12px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.pagination-wrap {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.upload-book-info {
  margin-bottom: 16px;
  padding: 8px 12px;
  background: #f5f7fa;
  border-radius: 4px;
}

.tag-editor {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  min-height: 38px;
  padding: 2px 0;
}

/* 预览 Drawer 样式 */
.preview-cover-area {
  display: flex;
  justify-content: center;
  margin-bottom: 12px;
}
.preview-cover-img {
  max-width: 200px;
  max-height: 280px;
  object-fit: cover;
  border-radius: 4px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.15);
}
.preview-cover-placeholder {
  width: 140px;
  height: 190px;
  background: #f5f7fa;
  border-radius: 4px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
.preview-actions {
  display: flex;
  gap: 12px;
  margin-top: 16px;
  justify-content: center;
}
.preview-pdf-section {
  margin-top: 20px;
  border-top: 1px solid #e8e8e8;
  padding-top: 16px;
}
.preview-pdf-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}
.preview-pdf-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 500;
  color: #303133;
}
.preview-pdf-viewer {
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  overflow: hidden;
  background: #525659;
}
.preview-pdf-iframe {
  width: 100%;
  height: 600px;
  border: none;
  display: block;
}
.preview-pdf-loading {
  height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: #909399;
}

/* 封面上传区域 */
.cover-input-row {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  flex-wrap: wrap;
}
.cover-mini-thumb {
  width: 48px;
  height: 64px;
  object-fit: cover;
  border-radius: 2px;
  border: 1px solid #e8e8e8;
  flex-shrink: 0;
}
</style>
