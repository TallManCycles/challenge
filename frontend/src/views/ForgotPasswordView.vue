<template>
  <div class="min-h-screen bg-gray-900 flex items-center justify-center px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
      <div class="text-center">
        <h2 class="mt-6 text-3xl font-bold text-white">
          Forgot Password
        </h2>
        <p class="mt-2 text-sm text-gray-400">
          Enter your email to reset your password
        </p>
      </div>
      
      <div class="bg-gray-800 p-6 rounded-lg">
        <p class="text-center text-gray-300">
          Password reset form coming soon...
        </p>
        <div class="mt-4 text-center">
          <router-link 
            to="/login" 
            class="font-medium text-blue-400 hover:text-blue-300 transition-colors"
          >
            Back to Login
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>