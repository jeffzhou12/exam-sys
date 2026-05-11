import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },
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
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth === false) {
    if (auth.isLoggedIn && to.name === 'Login') {
      return next('/dashboard')
    }
    return next()
  }

  if (!auth.isLoggedIn) {
    return next('/login')
  }

  if (to.meta.roles && !to.meta.roles.includes(auth.role)) {
    return next('/dashboard')
  }

  next()
})

export default router
