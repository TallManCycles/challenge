<template>
  <div class="min-h-screen bg-gray-900 text-white p-8">
    <div class="max-w-7xl mx-auto">
      <div class="flex justify-between items-center mb-8">
        <h1 class="text-3xl font-bold">Dashboard</h1>
        <button 
          @click="logout" 
          class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg transition-colors"
        >
          Logout
        </button>
      </div>
      
      <div v-if="user" class="bg-gray-800 p-6 rounded-lg mb-6">
        <h2 class="text-xl font-semibold mb-4">Welcome back, {{ user.username }}!</h2>
        <p class="text-gray-300">Email: {{ user.email }}</p>
        <p class="text-gray-300">Garmin Connected: {{ user.garminConnected ? 'Yes' : 'No' }}</p>
      </div>
      
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div class="bg-gray-800 p-6 rounded-lg">
          <h3 class="text-lg font-semibold mb-2">Challenges</h3>
          <p class="text-gray-400">Manage your fitness challenges</p>
        </div>
        
        <div class="bg-gray-800 p-6 rounded-lg">
          <h3 class="text-lg font-semibold mb-2">Activities</h3>
          <p class="text-gray-400">View your recent activities</p>
        </div>
        
        <div class="bg-gray-800 p-6 rounded-lg">
          <h3 class="text-lg font-semibold mb-2">Settings</h3>
          <p class="text-gray-400">Manage your account settings</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const user = authStore.user

const logout = () => {
  authStore.logout()
  router.push('/login')
}

onMounted(async () => {
  if (!user) {
    try {
      await authStore.getCurrentUser()
    } catch (error) {
      console.error('Failed to fetch user data:', error)
    }
  }
})
</script>