<template>
  <div class="profile-page container">
    <div class="page-header">
      <h2>个人资料</h2>
      <p class="page-sub">完善资料有助于 AI 为你推荐合适的练习题和考试</p>
    </div>

    <el-form
      v-loading="loading"
      :model="form"
      label-width="100px"
      class="profile-form"
    >
      <!-- 基本信息 -->
      <el-form-item label="用户名">
        <el-input :value="form.username" disabled />
      </el-form-item>
      <el-form-item label="昵称">
        <el-input v-model="form.nickname" placeholder="输入昵称" clearable />
      </el-form-item>
      <el-form-item label="性别">
        <el-select v-model="form.gender" placeholder="请选择" clearable style="width:200px">
          <el-option label="男" value="male" />
          <el-option label="女" value="female" />
          <el-option label="保密" value="other" />
        </el-select>
      </el-form-item>

      <el-divider content-position="left">学习信息</el-divider>

      <!-- 学历 -->
      <el-form-item label="当前学历">
        <el-select v-model="form.educationLevel" placeholder="请选择学历" clearable style="width:200px">
          <el-option v-for="lv in EDUCATION_LEVELS" :key="lv" :label="lv" :value="lv" />
        </el-select>
      </el-form-item>

      <!-- 感兴趣的学科 -->
      <el-form-item label="感兴趣学科">
        <div class="subject-tags">
          <el-check-tag
            v-for="sub in SUBJECT_OPTIONS"
            :key="sub"
            :checked="form.interestedSubjects.includes(sub)"
            @change="(checked) => toggleSubject(sub, checked)"
            style="margin:4px"
          >{{ sub }}</el-check-tag>
        </div>
        <div class="subject-hint text-muted">AI 将根据你选择的学科推荐练习题和考试</div>
      </el-form-item>

      <el-form-item>
        <el-button type="primary" :loading="saving" @click="saveProfile">保存</el-button>
        <el-button @click="resetForm">取消</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { meApi, EDUCATION_LEVELS, SUBJECT_OPTIONS } from '@/api/me'

const loading = ref(false)
const saving  = ref(false)
let original  = null

const form = reactive({
  username:          '',
  nickname:          '',
  gender:            '',
  educationLevel:    '',
  interestedSubjects: [],
})

onMounted(() => fetchProfile())

async function fetchProfile() {
  loading.value = true
  try {
    const data = await meApi.getProfile()
    Object.assign(form, {
      username:          data.username         || '',
      nickname:          data.nickname         || '',
      gender:            data.gender           || '',
      educationLevel:    data.educationLevel   || '',
      interestedSubjects: data.interestedSubjects || [],
    })
    original = { ...form, interestedSubjects: [...form.interestedSubjects] }
  } catch {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

function toggleSubject(sub, checked) {
  if (checked) {
    if (!form.interestedSubjects.includes(sub))
      form.interestedSubjects.push(sub)
  } else {
    form.interestedSubjects = form.interestedSubjects.filter(s => s !== sub)
  }
}

async function saveProfile() {
  saving.value = true
  try {
    await meApi.updateProfile({
      nickname:          form.nickname          || null,
      gender:            form.gender            || null,
      educationLevel:    form.educationLevel    || null,
      interestedSubjects: form.interestedSubjects,
    })
    ElMessage.success('保存成功')
    original = { ...form, interestedSubjects: [...form.interestedSubjects] }
  } catch {
    ElMessage.error('保存失败，请稍后重试')
  } finally {
    saving.value = false
  }
}

function resetForm() {
  if (!original) return
  Object.assign(form, { ...original, interestedSubjects: [...original.interestedSubjects] })
}
</script>

<style scoped>
.profile-page { padding: 24px 0; max-width: 640px; }
.page-header { margin-bottom: 24px; }
.page-header h2 { margin-bottom: 4px; }
.page-sub { color: var(--el-text-color-secondary); font-size: 13px; }
.profile-form { background: var(--el-bg-color); border-radius: 12px; padding: 24px; }
.subject-tags { display: flex; flex-wrap: wrap; gap: 4px; margin-bottom: 6px; }
.subject-hint { font-size: 12px; }
.text-muted { color: var(--el-text-color-secondary); }
</style>
