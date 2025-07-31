<template>
  <div class="min-h-screen bg-gray-900 flex items-center justify-center px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full text-center">
      <h1 class="text-6xl font-bold text-white mb-4">404</h1>
      <h2 class="text-2xl font-semibold text-gray-300 mb-4">Page Not Found</h2>
      <p class="text-gray-400 mb-8">
        The page you're looking for doesn't exist.
      </p>
      <router-link 
        to="/" 
        class="bg-blue-600 hover:bg-blue-700 text-white font-medium py-3 px-6 rounded-lg transition-colors"
      >
        Go Home
      </router-link>
    </div>
  </div>
</template>