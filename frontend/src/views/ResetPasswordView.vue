<template>
  <div class="min-h-screen bg-gray-900 flex items-center justify-center px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
      <!-- Logo -->
      <div class="text-center">
        <div class="mx-auto h-16 w-16 flex items-center justify-center">
          <!-- Shield Icon -->
          <svg 
            class="h-12 w-12 text-blue-500" 
            fill="currentColor" 
            viewBox="0 0 24 24"
          >
            <path d="M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1M11,7H13V9H11V7M11,11H13V17H11V11Z" />
          </svg>
        </div>
        <h2 class="mt-6 text-3xl font-bold text-white">
          Reset Password
        </h2>
        <p class="mt-2 text-sm text-gray-400">
          Enter your new password
        </p>
      </div>

      <!-- Success Message -->
      <div v-if="resetSuccess" class="bg-green-900 border border-green-700 rounded-lg p-4">
        <div class="flex items-center">
          <svg class="h-5 w-5 text-green-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <p class="text-green-300 text-sm">
            Password reset successfully! Redirecting to login...
          </p>
        </div>
      </div>

      <!-- Error Message -->
      <div v-if="errorMessage" class="bg-red-900 border border-red-700 rounded-lg p-4">
        <div class="flex items-center">
          <svg class="h-5 w-5 text-red-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-red-300 text-sm">{{ errorMessage }}</p>
        </div>
      </div>

      <!-- Invalid Token Message -->
      <div v-if="!isValidToken && !isLoading" class="bg-red-900 border border-red-700 rounded-lg p-4 text-center">
        <div class="mb-4">
          <svg class="h-16 w-16 text-red-400 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.464 0L4.348 16.5c-.77.833.192 2.5 1.732 2.5z" />
          </svg>
        </div>
        <h3 class="text-lg font-medium text-white mb-2">Invalid Reset Link</h3>
        <p class="text-red-300 text-sm mb-4">
          This password reset link is invalid or has expired.
        </p>
        <router-link
          to="/forgot-password"
          class="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium transition-colors"
        >
          Request New Reset Link
        </router-link>
      </div>

      <!-- Form -->
      <div v-if="isValidToken && !resetSuccess">
        <form class="mt-8 space-y-6" @submit.prevent="handleSubmit">
          <div class="space-y-4">
            <!-- New Password Field -->
            <div>
              <label for="password" class="block text-sm font-medium text-gray-300 mb-2">
                New Password
              </label>
              <div class="relative">
                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <svg class="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                  </svg>
                </div>
                <input
                  id="password"
                  v-model="form.password"
                  name="password"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="new-password"
                  required
                  class="w-full px-4 py-3 pl-10 pr-10 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
                  :class="{ 'border-red-500 focus:ring-red-500': errors.password }"
                  placeholder="Enter your new password"
                  :disabled="isLoading"
                />
                <button
                  type="button"
                  @click="showPassword = !showPassword"
                  class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-300"
                  :disabled="isLoading"
                >
                  <svg v-if="showPassword" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
                  </svg>
                  <svg v-else class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                </button>
              </div>
              <p v-if="errors.password" class="mt-1 text-sm text-red-400">{{ errors.password }}</p>
              <div class="mt-2 text-xs text-gray-400">
                Password must be at least 6 characters long
              </div>
            </div>

            <!-- Confirm Password Field -->
            <div>
              <label for="confirmPassword" class="block text-sm font-medium text-gray-300 mb-2">
                Confirm New Password
              </label>
              <div class="relative">
                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <svg class="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                  </svg>
                </div>
                <input
                  id="confirmPassword"
                  v-model="form.confirmPassword"
                  name="confirmPassword"
                  :type="showConfirmPassword ? 'text' : 'password'"
                  autocomplete="new-password"
                  required
                  class="w-full px-4 py-3 pl-10 pr-10 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
                  :class="{ 'border-red-500 focus:ring-red-500': errors.confirmPassword }"
                  placeholder="Confirm your new password"
                  :disabled="isLoading"
                />
                <button
                  type="button"
                  @click="showConfirmPassword = !showConfirmPassword"
                  class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-300"
                  :disabled="isLoading"
                >
                  <svg v-if="showConfirmPassword" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
                  </svg>
                  <svg v-else class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                </button>
              </div>
              <p v-if="errors.confirmPassword" class="mt-1 text-sm text-red-400">{{ errors.confirmPassword }}</p>
            </div>
          </div>

          <!-- Submit Button -->
          <div>
            <button
              type="submit"
              :disabled="isLoading"
              class="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-lg text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200"
            >
              <span v-if="isLoading" class="absolute left-0 inset-y-0 flex items-center pl-3">
                <svg class="h-5 w-5 text-blue-300 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              </span>
              {{ isLoading ? 'Resetting Password...' : 'Reset Password' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Back to Login -->
      <div class="text-center">
        <p class="text-sm text-gray-400">
          Remember your password?
          <router-link 
            to="/login" 
            class="font-medium text-blue-400 hover:text-blue-300 transition-colors"
          >
            Back to Sign In
          </router-link>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authService } from '../services/auth'

const route = useRoute()
const router = useRouter()

const form = reactive({
  password: '',
  confirmPassword: ''
})

const errors = reactive({
  password: '',
  confirmPassword: ''
})

const isLoading = ref(false)
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const errorMessage = ref('')
const resetSuccess = ref(false)
const isValidToken = ref(true)

const clearErrors = () => {
  errors.password = ''
  errors.confirmPassword = ''
  errorMessage.value = ''
}

const validateForm = (): boolean => {
  clearErrors()
  let isValid = true

  // Password validation
  if (!form.password) {
    errors.password = 'Password is required'
    isValid = false
  } else if (form.password.length < 6) {
    errors.password = 'Password must be at least 6 characters'
    isValid = false
  }

  // Confirm password validation
  if (!form.confirmPassword) {
    errors.confirmPassword = 'Please confirm your password'
    isValid = false
  } else if (form.password !== form.confirmPassword) {
    errors.confirmPassword = 'Passwords do not match'
    isValid = false
  }

  return isValid
}

const handleSubmit = async () => {
  if (!validateForm()) {
    return
  }

  isLoading.value = true
  errorMessage.value = ''

  try {
    const resetData = {
      resetToken: route.query.token as string,
      newPassword: form.password,
      confirmNewPassword: form.confirmPassword
    }

    await authService.resetPassword(resetData)
    
    // Show success state
    resetSuccess.value = true

    // Redirect to login after delay
    setTimeout(() => {
      router.push('/login')
    }, 2000)

  } catch (error: unknown) {
    console.error('Password reset failed:', error)
    const errorText = error instanceof Error ? error.message : 'Failed to reset password. Please try again.'
    if (errorText.toLowerCase().includes('token') || errorText.toLowerCase().includes('expired')) {
      isValidToken.value = false
    } else {
      errorMessage.value = errorText
    }
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  // Check if we have the required query parameters
  const token = route.query.token as string
  const email = route.query.email as string
  
  if (!token || !email) {
    isValidToken.value = false
  }
})
</script>