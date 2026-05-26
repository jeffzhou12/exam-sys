import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    component: () => import('@/layouts/PortalLayout.vue'),
    children: [
      {
        path: '',
        name: 'Home',
        component: () => import('@/views/Home.vue'),
      },
      {
        path: 'exams',
        name: 'ExamList',
        component: () => import('@/views/ExamList.vue'),
      },
      {
        path: 'exams/:id',
        name: 'ExamDetail',
        component: () => import('@/views/ExamDetail.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'my-results',
        name: 'MyResults',
        component: () => import('@/views/MyResults.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'results/:examId',
        name: 'ResultDetail',
        component: () => import('@/views/ResultDetail.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'teacher/exams',
        name: 'TeacherExams',
        component: () => import('@/views/TeacherExams.vue'),
        meta: { requiresAuth: true, roles: ['Teacher'] },
      },
      {
        path: 'teacher/exams/:id/results',
        name: 'TeacherExamResults',
        component: () => import('@/views/TeacherExamResults.vue'),
        meta: { requiresAuth: true, roles: ['Teacher'] },
      },
      {
        path: 'practice',
        name: 'PracticeSetup',
        component: () => import('@/views/PracticeSetup.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'wrong-book',
        name: 'WrongBook',
        component: () => import('@/views/WrongBook.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'messages',
        name: 'Messages',
        component: () => import('@/views/Messages.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'books',
        name: 'BookList',
        component: () => import('@/views/BookList.vue'),
      },
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/Login.vue'),
      },
      {
        path: 'register',
        name: 'Register',
        component: () => import('@/views/Register.vue'),
      },
    ],
  },
  {
    path: '/exam/:id/room',
    name: 'ExamRoom',
    component: () => import('@/views/ExamRoom.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/practice/room',
    name: 'PracticeRoom',
    component: () => import('@/views/PracticeRoom.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/practice/result',
    name: 'PracticeResult',
    component: () => import('@/views/PracticeResult.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/books/:id',
    name: 'BookReader',
    component: () => import('@/views/BookReader.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isLoggedIn) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.meta.roles && !to.meta.roles.includes(auth.user?.role)) {
    next({ name: 'Home' })
  } else {
    next()
  }
})

export default router
