<template>
  <div class="min-h-screen bg-gray-900 text-white">
    <!-- Header -->
    <header class="border-b border-gray-800 px-6 py-4">
      <div class="max-w-7xl mx-auto flex items-center justify-between">
        <div class="flex items-center space-x-8">
          <h1 class="text-xl font-bold text-white">ChallengeHub</h1>
          <nav class="flex space-x-6">
            <router-link to="/dashboard" class="text-white hover:text-gray-300 transition-colors">Challenges</router-link>
            <router-link to="/challenges/create" class="text-gray-400 hover:text-gray-300 transition-colors">Create Challenge</router-link>
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

    <!-- Loading State -->
    <div v-if="loading" class="max-w-7xl mx-auto px-6 py-8" data-testid="loading-state">
      <div class="animate-pulse">
        <div class="h-8 bg-gray-700 rounded w-64 mb-2"></div>
        <div class="h-4 bg-gray-700 rounded w-96 mb-8"></div>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div v-for="i in 3" :key="i" class="bg-gray-800 rounded-lg p-6">
            <div class="h-6 bg-gray-700 rounded mb-2"></div>
            <div class="h-4 bg-gray-700 rounded mb-6"></div>
            <div class="h-20 bg-gray-700 rounded"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <main v-else class="max-w-7xl mx-auto px-6 py-8">
      <!-- Page Title -->
      <div class="mb-8 flex items-center justify-between">
        <div>
          <h2 class="text-3xl font-bold text-white mb-2">My Challenges</h2>
          <p class="text-gray-400">Track your progress and see how you're performing</p>
        </div>
        <router-link
          to="/challenges/create"
          class="bg-blue-600 hover:bg-blue-700 text-white px-6 py-3 rounded-lg transition-colors flex items-center space-x-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          <span>Create Challenge</span>
        </router-link>
      </div>

      <!-- Active Challenges -->
      <section class="mb-12" data-testid="active-challenges">
        <div class="flex items-center justify-between mb-6">
          <h3 class="text-xl font-semibold text-white">Active Challenges</h3>
          <span class="bg-green-600 text-white px-3 py-1 rounded-full text-sm font-medium">
            {{ activeChallenges.length }} ongoing
          </span>
        </div>

        <!-- No active challenges -->
        <div v-if="activeChallenges.length === 0" class="text-center py-12">
          <div class="text-gray-500 mb-4">
            <svg class="w-16 h-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
            </svg>
          </div>
          <h4 class="text-xl font-medium text-gray-300 mb-2">No Active Challenges</h4>
          <p class="text-gray-500 mb-6">Create or join a challenge to get started!</p>
          <router-link
            to="/challenges/create"
            class="inline-flex items-center space-x-2 bg-blue-600 hover:bg-blue-700 text-white px-6 py-3 rounded-lg transition-colors"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            <span>Create Your First Challenge</span>
          </router-link>
        </div>

        <!-- Active challenges grid -->
        <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div 
            v-for="challenge in activeChallenges" 
            :key="challenge.id"
            class="bg-gray-800 rounded-lg p-6 hover:bg-gray-750 transition-colors cursor-pointer"
            data-testid="active-challenge-card"
            @click="viewChallenge(challenge.id)"
          >
            <!-- Challenge Header -->
            <div class="flex items-start justify-between mb-4">
              <div class="flex-1">
                <h4 class="text-lg font-semibold text-white mb-1">{{ challenge.title }}</h4>
                <p class="text-gray-400 text-sm">{{ challenge.description || 'No description' }}</p>
              </div>
              <span 
                class="px-2 py-1 rounded-full text-xs font-medium"
                :class="getTypeColorClass(challenge.challengeTypeName)"
              >
                {{ challenge.challengeTypeName }}
              </span>
            </div>

            <!-- Challenge Stats -->
            <div class="mb-4">
              <div class="flex items-center justify-between text-sm mb-2">
                <span class="text-gray-400">Participants</span>
                <span class="text-white font-medium">{{ challenge.participantCount }}</span>
              </div>
              <div class="flex items-center justify-between text-sm mb-2">
                <span class="text-gray-400">Created by</span>
                <span class="text-white font-medium">{{ challenge.createdByUsername }}</span>
              </div>
              <div class="flex items-center justify-between text-sm">
                <span class="text-gray-400">Ends</span>
                <span class="text-white font-medium">{{ formatDate(challenge.endDate) }}</span>
              </div>
            </div>

            <!-- Participation Status -->
            <div class="flex items-center justify-between">
              <span 
                v-if="challenge.isUserParticipating"
                class="px-3 py-1 bg-green-600 text-white rounded-full text-xs font-medium"
              >
                Participating
              </span>
              <button
                v-else
                @click.stop="joinChallenge(challenge.id)"
                class="px-3 py-1 bg-blue-600 hover:bg-blue-700 text-white rounded-full text-xs font-medium transition-colors"
              >
                Join Challenge
              </button>
              
              <div class="text-right text-xs text-gray-400">
                {{ getDaysRemaining(challenge.endDate) }} days left
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- All Challenges -->
      <section data-testid="all-challenges">
        <div class="flex items-center justify-between mb-6">
          <h3 class="text-xl font-semibold text-white">All Challenges</h3>
          <span class="bg-gray-600 text-white px-3 py-1 rounded-full text-sm font-medium">
            {{ allChallenges.length }} total
          </span>
        </div>

        <!-- No challenges -->
        <div v-if="allChallenges.length === 0" class="text-center py-12">
          <div class="text-gray-500 mb-4">
            <svg class="w-16 h-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M9 12h6m-6 0a9 9 0 110-18 9 9 0 010 18z" />
            </svg>
          </div>
          <h4 class="text-xl font-medium text-gray-300 mb-2">No Challenges Yet</h4>
          <p class="text-gray-500">Be the first to create a challenge!</p>
        </div>

        <!-- All challenges list -->
        <div v-else class="space-y-4">
          <div 
            v-for="challenge in allChallenges" 
            :key="challenge.id"
            class="bg-gray-800 rounded-lg p-4 flex items-center justify-between hover:bg-gray-750 transition-colors cursor-pointer"
            data-testid="challenge-card"
            @click="viewChallenge(challenge.id)"
          >
            <div class="flex-1">
              <div class="flex items-center space-x-3 mb-2">
                <h4 class="text-lg font-medium text-white">{{ challenge.title }}</h4>
                <span 
                  class="px-2 py-1 rounded-full text-xs font-medium"
                  :class="getTypeColorClass(challenge.challengeTypeName)"
                >
                  {{ challenge.challengeTypeName }}
                </span>
                <span 
                  v-if="challenge.isUserParticipating"
                  class="px-2 py-1 bg-green-600 text-white rounded-full text-xs font-medium"
                >
                  Participating
                </span>
              </div>
              <p class="text-gray-400 text-sm mb-1">{{ challenge.description || 'No description' }}</p>
              <div class="flex items-center space-x-4 text-xs text-gray-500">
                <span>{{ challenge.participantCount }} participants</span>
                <span>Created by {{ challenge.createdByUsername }}</span>
                <span>{{ formatDateRange(challenge.startDate, challenge.endDate) }}</span>
              </div>
            </div>
            
            <div class="flex items-center space-x-3">
              <button
                v-if="!challenge.isUserParticipating && isDateInFuture(challenge.endDate)"
                @click.stop="joinChallenge(challenge.id)"
                class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium transition-colors"
                data-testid="join-button"
              >
                Join
              </button>
              <span 
                v-else-if="!isDateInFuture(challenge.endDate)"
                class="px-4 py-2 bg-gray-600 text-gray-300 rounded-lg text-sm font-medium"
              >
                Ended
              </span>
            </div>
          </div>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { challengeService } from '../services/challenge'
import type { Challenge } from '../types/challenge'

const router = useRouter()
const authStore = useAuthStore()

const loading = ref(true)
const allChallenges = ref<Challenge[]>([])

const logout = () => {
  authStore.logout()
  router.push('/login')
}

// Computed properties for filtering challenges
const activeChallenges = computed(() => {
  const now = new Date()
  return allChallenges.value.filter(challenge => 
    challenge.isActive && 
    new Date(challenge.endDate) > now &&
    challenge.isUserParticipating
  )
})

const loadChallenges = async () => {
  try {
    loading.value = true
    const challenges = await challengeService.getChallenges()
    allChallenges.value = challenges
  } catch (error) {
    console.error('Failed to load challenges:', error)
    // You could show a toast notification here
  } finally {
    loading.value = false
  }
}

const joinChallenge = async (challengeId: number) => {
  try {
    await challengeService.joinChallenge(challengeId)
    // Reload challenges to update participation status
    await loadChallenges()
  } catch (error) {
    console.error('Failed to join challenge:', error)
    // You could show a toast notification here
  }
}

const viewChallenge = (challengeId: number) => {
  router.push(`/challenges/${challengeId}`)
}

const getTypeColorClass = (type: string) => {
  switch (type.toLowerCase()) {
    case 'distance':
      return 'bg-blue-600 text-white'
    case 'elevation':
      return 'bg-green-600 text-white'
    case 'time':
      return 'bg-purple-600 text-white'
    default:
      return 'bg-gray-600 text-white'
  }
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  }).format(date)
}

const formatDateRange = (startDate: string, endDate: string) => {
  const start = formatDate(startDate)
  const end = formatDate(endDate)
  return `${start} - ${end}`
}

const getDaysRemaining = (endDate: string) => {
  const now = new Date()
  const end = new Date(endDate)
  const diffTime = end.getTime() - now.getTime()
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))
  return Math.max(0, diffDays)
}

const isDateInFuture = (dateString: string) => {
  return new Date(dateString) > new Date()
}

onMounted(() => {
  loadChallenges()
})
</script>