import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    component: () => import('@/layouts/AdminLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: '/dashboard'
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue'),
        meta: { title: '仪表盘' }
      },
      {
        path: 'tenants',
        name: 'Tenants',
        component: () => import('@/views/tenants/TenantList.vue'),
        meta: { title: '租户管理', roles: ['SuperAdmin'] }
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('@/views/users/UserList.vue'),
        meta: { title: '用户管理', roles: ['SuperAdmin', 'Admin'] }
      },
      {
        path: 'exam-papers',
        name: 'ExamPapers',
        component: () => import('@/views/exam-papers/ExamPaperList.vue'),
        meta: { title: '试卷管理' }
      },
      {
        path: 'exam-papers/create',
        name: 'ExamPaperCreate',
        component: () => import('@/views/exam-papers/ExamPaperForm.vue'),
        meta: { title: '创建试卷', roles: ['SuperAdmin', 'Admin', 'Teacher'] }
      },
      {
        path: 'exam-papers/:id/edit',
        name: 'ExamPaperEdit',
        component: () => import('@/views/exam-papers/ExamPaperForm.vue'),
        meta: { title: '编辑试卷', roles: ['SuperAdmin', 'Admin', 'Teacher'] }
      },
      {
        path: 'exam-papers/:id/results',
        name: 'ExamResults',
        component: () => import('@/views/exam-papers/ExamResults.vue'),
        meta: { title: '考试成绩', roles: ['SuperAdmin', 'Admin', 'Teacher'] }
      },
      {
        path: 'questions',
        name: 'Questions',
        component: () => import('@/views/questions/QuestionList.vue'),
        meta: { title: '题库管理', roles: ['SuperAdmin', 'Admin', 'Teacher'] }
      },
      {
        path: 'books',
        name: 'Books',
        component: () => import('@/views/books/Books.vue'),
        meta: { title: '图书管理', roles: ['SuperAdmin', 'Admin', 'Teacher'] }
      },
      {
        path: 'messages',
        name: 'Messages',
        component: () => import('@/views/messages/MessageList.vue'),
        meta: { title: '消息管理', roles: ['SuperAdmin', 'Admin'] }
      },
      {
        path: 'ai-configs',
        name: 'AiConfigs',
        component: () => import('@/views/ai-configs/AiConfigList.vue'),
        meta: { title: 'AI 模型配置', roles: ['SuperAdmin', 'Admin'] }
      },
      {
        path: 'audit-logs',
        name: 'AuditLogs',
        component: () => import('@/views/audit-logs/AuditLogList.vue'),
        meta: { title: '审计日志', roles: ['SuperAdmin'] }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
]

const router = createRouter({
  // 与 vite.config base: '/admin/' 一致，实现同源代理访问
  history: createWebHistory('/admin/'),
  routes
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()

  if (!auth.isLoggedIn) {
    // 跳转到 portal 登录页（绕过 /admin/ base 前缀）
    window.location.href = '/login'
    return
  }

  // 只允许管理员和教师进入后台
  if (!auth.isAdminOrTeacher) {
    window.location.href = '/login'
    return
  }

  if (to.meta.roles && !to.meta.roles.includes(auth.role)) {
    return next('/dashboard')
  }

  next()
})

export default router
