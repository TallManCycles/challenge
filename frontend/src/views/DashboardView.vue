<template>
  <div class="min-h-screen bg-gray-900 text-white">
    <!-- Header -->
    <header class="border-b border-gray-800 px-6 py-4">
      <div class="max-w-7xl mx-auto flex items-center justify-between">
        <div class="flex items-center space-x-8">
          <h1 class="text-xl font-bold text-white">ChallengeHub</h1>
          <nav class="flex space-x-6">
            <router-link to="/dashboard" class="text-white hover:text-gray-300 transition-colors">Challenges</router-link>
            <a href="#" class="text-gray-400 hover:text-gray-300 transition-colors">Create Challenge</a>
            <router-link to="/settings" class="text-gray-400 hover:text-gray-300 transition-colors">Settings</router-link>
          </nav>
        </div>
        <button 
          @click="logout" 
          class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg transition-colors text-sm"
        >
          Logout
        </button>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto px-6 py-8">
      <!-- Page Title -->
      <div class="mb-8">
        <h2 class="text-3xl font-bold text-white mb-2">My Challenges</h2>
        <p class="text-gray-400">Track your progress and see how you're performing</p>
      </div>

      <!-- Active Challenges -->
      <section class="mb-12">
        <div class="flex items-center justify-between mb-6">
          <h3 class="text-xl font-semibold text-white">Active Challenges</h3>
          <span class="bg-green-600 text-white px-3 py-1 rounded-full text-sm font-medium">
            {{ activeChallenges.length }} ongoing
          </span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div 
            v-for="challenge in activeChallenges" 
            :key="challenge.id"
            class="bg-gray-800 rounded-lg p-6 hover:bg-gray-750 transition-colors"
          >
            <!-- Challenge Header -->
            <div class="flex items-start justify-between mb-4">
              <div>
                <h4 class="text-lg font-semibold text-white mb-1">{{ challenge.title }}</h4>
                <p class="text-gray-400 text-sm">{{ challenge.description }}</p>
              </div>
              <span 
                :class="`bg-${challenge.color}-600 text-white px-2 py-1 rounded text-xs font-medium`"
              >
                #{{ challenge.rank }}
              </span>
            </div>

            <!-- Progress Circle -->
            <div class="flex items-center justify-center mb-4">
              <div class="relative w-24 h-24">
                <svg class="w-24 h-24 transform -rotate-90" viewBox="0 0 36 36">
                  <!-- Background circle -->
                  <path
                    class="text-gray-700"
                    stroke="currentColor"
                    stroke-width="3"
                    fill="none"
                    d="M18 2.0845
                      a 15.9155 15.9155 0 0 1 0 31.831
                      a 15.9155 15.9155 0 0 1 0 -31.831"
                  />
                  <!-- Progress circle -->
                  <path
                    :class="`text-${challenge.color}-500`"
                    stroke="currentColor"
                    stroke-width="3"
                    fill="none"
                    stroke-linecap="round"
                    :stroke-dasharray="`${challenge.progress}, 100`"
                    d="M18 2.0845
                      a 15.9155 15.9155 0 0 1 0 31.831
                      a 15.9155 15.9155 0 0 1 0 -31.831"
                  />
                </svg>
                <div class="absolute inset-0 flex items-center justify-center">
                  <span class="text-sm font-medium text-gray-400">{{ challenge.progress }}%</span>
                </div>
              </div>
            </div>

            <!-- Progress Info -->
            <div class="mb-4">
              <div class="flex justify-between text-sm text-gray-400 mb-1">
                <span>Progress</span>
                <span>{{ challenge.current }}/{{ challenge.target }} {{ challenge.unit }}</span>
              </div>
              <div class="w-full bg-gray-700 rounded-full h-2">
                <div 
                  :class="`bg-${challenge.color}-500 h-2 rounded-full transition-all duration-300`"
                  :style="`width: ${challenge.progress}%`"
                ></div>
              </div>
            </div>

            <!-- View Details Button -->
            <button 
              :class="`w-full bg-${challenge.color}-600 hover:bg-${challenge.color}-700 text-white py-2 px-4 rounded-lg font-medium transition-colors`"
            >
              View Details
            </button>
          </div>
        </div>
      </section>

      <!-- Previous Challenges -->
      <section>
        <div class="flex items-center justify-between mb-6">
          <h3 class="text-xl font-semibold text-white">Previous Challenges</h3>
          <span class="text-gray-400 text-sm">{{ previousChallenges.length }} completed</span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div 
            v-for="challenge in previousChallenges" 
            :key="challenge.id"
            class="bg-gray-800 rounded-lg p-6 hover:bg-gray-750 transition-colors"
          >
            <!-- Challenge Header -->
            <div class="flex items-start justify-between mb-4">
              <div>
                <h4 class="text-lg font-semibold text-white mb-1">{{ challenge.title }}</h4>
                <p class="text-gray-400 text-sm">{{ challenge.description }}</p>
              </div>
              <span 
                :class="`px-2 py-1 rounded text-xs font-medium ${challenge.status === 'Completed' ? 'bg-green-600 text-white' : 'bg-red-600 text-white'}`"
              >
                {{ challenge.status }}
              </span>
            </div>

            <!-- Winner Info -->
            <div class="flex items-center mb-4" v-if="challenge.winner">
              <img 
                :src="challenge.winner.avatar" 
                :alt="challenge.winner.name"
                class="w-8 h-8 rounded-full mr-3"
              />
              <div>
                <p class="text-sm font-medium text-white">Winner: {{ challenge.winner.name }}</p>
                <p class="text-xs text-gray-400">{{ challenge.winner.achievement }}</p>
              </div>
            </div>

            <!-- Progress Bar -->
            <div class="mb-4">
              <div class="w-full bg-gray-700 rounded-full h-2">
                <div 
                  :class="`h-2 rounded-full transition-all duration-300 ${challenge.status === 'Completed' ? 'bg-green-500' : 'bg-red-500'}`"
                  :style="`width: ${challenge.completion}%`"
                ></div>
              </div>
            </div>

            <!-- View Details Button -->
            <button class="w-full bg-gray-700 hover:bg-gray-600 text-white py-2 px-4 rounded-lg font-medium transition-colors">
              View Details
            </button>
          </div>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const user = authStore.user

const logout = () => {
  authStore.logout()
  router.push('/login')
}

// Stubbed active challenges data
const activeChallenges = ref([
  {
    id: 1,
    title: '30-Day Fitness Challenge',
    description: 'Daily workouts & nutrition',
    rank: 2,
    color: 'green',
    progress: 60,
    current: 18,
    target: 30,
    unit: 'days'
  },
  {
    id: 2,
    title: 'Code Daily',
    description: '100 days of coding',
    rank: 1,
    color: 'purple',
    progress: 45,
    current: 45,
    target: 100,
    unit: 'days'
  },
  {
    id: 3,
    title: 'Water Challenge',
    description: '8 glasses daily for 30 days',
    rank: 4,
    color: 'blue',
    progress: 73,
    current: 22,
    target: 30,
    unit: 'days'
  }
])

// Stubbed previous challenges data
const previousChallenges = ref([
  {
    id: 4,
    title: 'Reading Marathon',
    description: '12 books in 12 months',
    status: 'Completed',
    completion: 100,
    winner: {
      name: 'Sarah Chen',
      achievement: '12/12 books completed',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=32&h=32&fit=crop&crop=face&auto=format'
    }
  },
  {
    id: 5,
    title: 'Meditation Journey',
    description: '21 days of mindfulness',
    status: 'Completed',
    completion: 100,
    winner: {
      name: 'Mike Johnson',
      achievement: '21/21 days completed',
      avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=32&h=32&fit=crop&crop=face&auto=format'
    }
  },
  {
    id: 6,
    title: 'Language Learning',
    description: 'Spanish fluency in 6 months',
    status: 'Failed',
    completion: 20,
    winner: {
      name: 'Emma Davis',
      achievement: '6/6 months completed',
      avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=32&h=32&fit=crop&crop=face&auto=format'
    }
  },
  {
    id: 7,
    title: 'Morning Run',
    description: '5km daily for 30 days',
    status: 'Completed',
    completion: 100,
    winner: {
      name: 'Alex Rodriguez',
      achievement: '30/30 days completed',
      avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=32&h=32&fit=crop&crop=face&auto=format'
    }
  },
  {
    id: 8,
    title: 'No Social Media',
    description: 'Digital detox for 14 days',
    status: 'Completed',
    completion: 100,
    winner: {
      name: 'Lisa Park',
      achievement: '14/14 days completed',
      avatar: 'https://images.unsplash.com/photo-1517841905240-472988babdf9?w=32&h=32&fit=crop&crop=face&auto=format'
    }
  }
])

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