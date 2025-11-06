import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authService } from '../services/auth'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const isAuthenticated = ref(authService.isAuthenticated())

  async function login(credentials: LoginRequest) {
    isLoading.value = true
    error.value = null
    
    try {
      const response = await authService.login(credentials)
      
      // Update authentication state
      isAuthenticated.value = true
      
      // Fetch full user data
      user.value = await authService.getCurrentUser()
      
      return response
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Login failed'
      isAuthenticated.value = false
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function register(userData: RegisterRequest) {
    isLoading.value = true
    error.value = null
    
    try {
      const response = await authService.register(userData)
      
      // Update authentication state
      isAuthenticated.value = true
      
      // Fetch full user data
      user.value = await authService.getCurrentUser()
      
      return response
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Registration failed'
      isAuthenticated.value = false
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function getCurrentUser() {
    if (!isAuthenticated.value) {
      return null
    }

    isLoading.value = true
    error.value = null
    
    try {
      user.value = await authService.getCurrentUser()
      return user.value
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch user data'
      // If token is invalid, logout
      logout()
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function logout() {
    authService.logout()
    user.value = null
    error.value = null
    isAuthenticated.value = false
  }

  function clearError() {
    error.value = null
  }

  function setAuthenticated(value: boolean) {
    isAuthenticated.value = value
  }

  // Initialize user data if authenticated
  async function initialize() {
    // Sync authentication state with service
    isAuthenticated.value = authService.isAuthenticated()

    if (isAuthenticated.value && !user.value) {
      try {
        await getCurrentUser()
      } catch {
        // Silently handle initialization errors
        logout()
      }
    }
  }

  return {
    user,
    isLoading,
    error,
    isAuthenticated,
    login,
    register,
    getCurrentUser,
    logout,
    clearError,
    setAuthenticated,
    initialize,
  }
})