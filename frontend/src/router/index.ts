import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { authService } from '../services/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/login'
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/LoginView.vue'),
      meta: { requiresGuest: true }
    },
    {
      path: '/register',
      name: 'Register',
      component: () => import('../views/RegisterView.vue'),
      meta: { requiresGuest: true }
    },
    {
      path: '/dashboard',
      name: 'Dashboard',
      component: () => import('../views/DashboardView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/activities',
      name: 'Activities',
      component: () => import('../views/ActivitiesView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/settings',
      name: 'Settings',
      component: () => import('../views/SettingsView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/challenges/create',
      name: 'CreateChallenge',
      component: () => import('../views/CreateChallengeView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/challenges/:id',
      name: 'ChallengeDetail',
      component: () => import('../views/ChallengeDetailView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/forgot-password',
      name: 'ForgotPassword',
      component: () => import('../views/ForgotPasswordView.vue'),
      meta: { requiresGuest: true }
    },
    {
      path: '/reset-password',
      name: 'ResetPassword',
      component: () => import('../views/ResetPasswordView.vue'),
      meta: { requiresGuest: true }
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'NotFound',
      component: () => import('../views/NotFoundView.vue')
    }
  ],
})

// Navigation guards
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()

  // For protected routes, try to refresh token if needed before checking auth
  if (to.meta.requiresAuth) {
    const isAuthenticated = await authService.ensureAuthenticated()

    // Update store state after potential refresh using proper action
    authStore.setAuthenticated(isAuthenticated)

    if (!isAuthenticated) {
      next('/login')
      return
    }
  }

  // For guest-only routes, check if already authenticated
  if (to.meta.requiresGuest && authStore.isAuthenticated) {
    next('/dashboard')
    return
  }

  next()
})

export default router
