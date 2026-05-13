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
  } else {
    next()
  }
})

export default router
