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
          Forgot Password
        </h2>
        <p class="mt-2 text-sm text-gray-400">
          Enter your email to reset your password
        </p>
      </div>

      <!-- Success Message -->
      <div v-if="resetSent" class="bg-green-900 border border-green-700 rounded-lg p-4">
        <div class="flex items-center">
          <svg class="h-5 w-5 text-green-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <div>
            <p class="text-green-300 text-sm font-medium">Password reset email sent!</p>
            <p class="text-green-300 text-xs mt-1">Check your inbox for reset instructions.</p>
          </div>
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

      <!-- Form or Success State -->
      <div v-if="!resetSent">
        <!-- Form -->
        <form class="mt-8 space-y-6" @submit.prevent="handleSubmit">
          <div class="space-y-4">
            <!-- Email Field -->
            <div>
              <label for="email" class="block text-sm font-medium text-gray-300 mb-2">
                Email Address
              </label>
              <div class="relative">
                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <svg class="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207" />
                  </svg>
                </div>
                <input
                  id="email"
                  v-model="form.email"
                  name="email"
                  type="email"
                  autocomplete="email"
                  required
                  class="w-full px-4 py-3 pl-10 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
                  :class="{ 'border-red-500 focus:ring-red-500': errors.email }"
                  placeholder="Enter your email address"
                  :disabled="isLoading"
                />
              </div>
              <p v-if="errors.email" class="mt-1 text-sm text-red-400">{{ errors.email }}</p>
              <div class="mt-2 text-xs text-gray-400">
                We'll send you a link to reset your password
              </div>
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
              {{ isLoading ? 'Sending Reset Email...' : 'Send Reset Email' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Success State Actions -->
      <div v-else class="space-y-4">
        <div class="bg-gray-800 p-6 rounded-lg text-center">
          <div class="mb-4">
            <svg class="h-16 w-16 text-green-400 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <h3 class="text-lg font-medium text-white mb-2">Check Your Email</h3>
          <p class="text-gray-400 text-sm mb-4">
            We've sent a password reset link to <strong class="text-white">{{ form.email }}</strong>
          </p>
          <p class="text-gray-500 text-xs">
            Didn't receive the email? Check your spam folder or try again in a few minutes.
          </p>
        </div>
        
        <button
          @click="resetForm"
          class="w-full py-3 px-4 border border-gray-600 text-sm font-medium rounded-lg text-gray-300 bg-gray-800 hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 transition-all duration-200"
        >
          Send Another Reset Email
        </button>
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
import { reactive, ref } from 'vue'
import { authService } from '../services/auth'

const form = reactive({
  email: ''
})

const errors = reactive({
  email: ''
})

const isLoading = ref(false)
const errorMessage = ref('')
const resetSent = ref(false)

const clearErrors = () => {
  errors.email = ''
  errorMessage.value = ''
}

const validateForm = (): boolean => {
  clearErrors()
  let isValid = true

  // Email validation
  if (!form.email.trim()) {
    errors.email = 'Email is required'
    isValid = false
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = 'Please enter a valid email address'
    isValid = false
  }

  return isValid
}

const resetForm = () => {
  resetSent.value = false
  clearErrors()
  form.email = ''
}

const handleSubmit = async () => {
  if (!validateForm()) {
    return
  }

  isLoading.value = true
  errorMessage.value = ''

  try {
    const resetData = {
      email: form.email.trim()
    }

    await authService.forgotPassword(resetData)
    
    // Show success state
    resetSent.value = true

  } catch (error: any) {
    console.error('Password reset failed:', error)
    errorMessage.value = error.message || 'Failed to send reset email. Please try again.'
  } finally {
    isLoading.value = false
  }
}
</script>