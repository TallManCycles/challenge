<template>
  <div class="min-h-screen bg-gray-900 text-white">
    <!-- Header -->
    <header class="border-b border-gray-800 px-6 py-4">
      <div class="max-w-7xl mx-auto flex items-center justify-between">
        <div class="flex items-center space-x-8">
          <h1 class="text-xl font-bold text-white">ChallengeHub</h1>
          <nav class="flex space-x-6">
            <router-link to="/dashboard" class="text-gray-400 hover:text-gray-300 transition-colors">Challenges</router-link>
            <a href="#" class="text-gray-400 hover:text-gray-300 transition-colors">Create Challenge</a>
            <router-link to="/activities" class="text-white hover:text-gray-300 transition-colors">My Activities</router-link>
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
        <h2 class="text-3xl font-bold text-white mb-2">My Activities</h2>
        <p class="text-gray-400">Track your Garmin activities and cycling progress</p>
      </div>

      <!-- Filter Controls -->
      <div class="mb-6 flex flex-wrap gap-4 items-center">
        <div class="flex items-center space-x-2">
          <label class="text-sm font-medium text-gray-300">Activity Type:</label>
          <select
            v-model="selectedFilter"
            @change="handleFilterChange"
            class="bg-gray-700 border border-gray-600 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">All Activities</option>
            <option value="cycling">Cycling Only</option>
          </select>
        </div>

        <div class="flex items-center space-x-2">
          <label class="text-sm font-medium text-gray-300">From:</label>
          <input
            v-model="dateFrom"
            @change="handleDateChange"
            type="date"
            class="bg-gray-700 border border-gray-600 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
        </div>

        <div class="flex items-center space-x-2">
          <label class="text-sm font-medium text-gray-300">To:</label>
          <input
            v-model="dateTo"
            @change="handleDateChange"
            type="date"
            class="bg-gray-700 border border-gray-600 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
        </div>

        <button
          @click="refreshActivities"
          :disabled="loading"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white px-4 py-2 rounded-lg text-sm transition-colors flex items-center"
        >
          <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          {{ loading ? 'Loading...' : 'Refresh' }}
        </button>
      </div>

      <!-- Loading State -->
      <div v-if="loading && activities.length === 0" class="text-center py-12">
        <div class="inline-flex items-center justify-center w-16 h-16 bg-gray-800 rounded-full mb-4">
          <svg class="animate-spin h-8 w-8 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
        <p class="text-gray-400">Loading your activities...</p>
      </div>

      <!-- No Activities State -->
      <div v-else-if="!loading && activities.length === 0" class="text-center py-12">
        <div class="inline-flex items-center justify-center w-16 h-16 bg-gray-800 rounded-full mb-4">
          <svg class="h-8 w-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"></path>
          </svg>
        </div>
        <h3 class="text-lg font-medium text-white mb-2">No Activities Found</h3>
        <p class="text-gray-400 mb-4">Connect your Garmin device and start recording activities to see them here.</p>
      </div>

      <!-- Activities List -->
      <div v-else class="space-y-4">
        <div
          v-for="activity in activities"
          :key="activity.id"
          class="bg-gray-800 rounded-lg p-6 hover:bg-gray-750 transition-colors"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center space-x-3 mb-3">
                <!-- Activity Type Icon -->
                <div
                  :class="[
                    'w-10 h-10 rounded-full flex items-center justify-center',
                    isCyclingActivity(activity.activityType) ? 'bg-blue-600' : 'bg-green-600'
                  ]"
                >
                  <svg v-if="isCyclingActivity(activity.activityType)" class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
                  </svg>
                  <svg v-else class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                  </svg>
                </div>

                <div>
                  <h3 class="text-lg font-semibold text-white">{{ formatActivityType(activity.activityType) }}</h3>
                  <p class="text-sm text-gray-400">{{ formatDate(activity.startTime) }}</p>
                </div>
              </div>

              <!-- Activity Stats -->
              <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                <div v-if="activity.distanceInMeters" class="text-center">
                  <div class="text-2xl font-bold text-white">{{ formatDistance(activity.distanceInMeters) }}</div>
                  <div class="text-xs text-gray-400 uppercase tracking-wide">Distance</div>
                </div>

                <div class="text-center">
                  <div class="text-2xl font-bold text-white">{{ formatDuration(activity.durationInSeconds) }}</div>
                  <div class="text-xs text-gray-400 uppercase tracking-wide">Duration</div>
                </div>

                <div v-if="activity.totalElevationGainInMeters" class="text-center">
                  <div class="text-2xl font-bold text-white">{{ Math.round(activity.totalElevationGainInMeters) }}m</div>
                  <div class="text-xs text-gray-400 uppercase tracking-wide">Elevation</div>
                </div>

                <div v-if="activity.activeKilocalories" class="text-center">
                  <div class="text-2xl font-bold text-white">{{ activity.activeKilocalories }}</div>
                  <div class="text-xs text-gray-400 uppercase tracking-wide">Calories</div>
                </div>
              </div>

              <!-- Activity Meta Info -->
              <div class="flex flex-wrap items-center gap-4 text-sm text-gray-400">
                <span v-if="activity.deviceName" class="flex items-center">
                  <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M17 2H7c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 18H7V4h10v16z"/>
                  </svg>
                  {{ activity.deviceName }}
                </span>

                <span v-if="activity.isManual" class="flex items-center text-yellow-400">
                  <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
                  </svg>
                  Manual Entry
                </span>

                <span v-if="activity.isWebUpload" class="flex items-center text-blue-400">
                  <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM14 13v4h-4v-4H7l5-5 5 5h-3z"/>
                  </svg>
                  Web Upload
                </span>

                <span class="flex items-center">
                  <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
                  </svg>
                  {{ formatDate(activity.receivedAt, true) }}
                </span>
              </div>
            </div>

            <!-- Cycling Badge -->
            <div v-if="isCyclingActivity(activity.activityType)" class="ml-4">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-600 text-white">
                <svg class="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
                </svg>
                Cycling
              </span>
            </div>
          </div>
        </div>

        <!-- Load More Button -->
        <div v-if="hasMoreActivities" class="text-center pt-6">
          <button
            @click="loadMoreActivities"
            :disabled="loadingMore"
            class="bg-gray-700 hover:bg-gray-600 disabled:opacity-50 text-white px-6 py-3 rounded-lg transition-colors inline-flex items-center"
          >
            <svg v-if="loadingMore" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            {{ loadingMore ? 'Loading...' : 'Load More Activities' }}
          </button>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import axios from 'axios'

const router = useRouter()
const authStore = useAuthStore()

interface Activity {
  id: number
  summaryId: string
  activityId?: string
  activityType: string
  startTime: string
  durationInSeconds: number
  distanceInMeters?: number
  totalElevationGainInMeters?: number
  totalElevationLossInMeters?: number
  activeKilocalories?: number
  deviceName?: string
  isManual: boolean
  isWebUpload: boolean
  receivedAt: string
}

// Data
const activities = ref<Activity[]>([])
const loading = ref(false)
const loadingMore = ref(false)
const hasMoreActivities = ref(true)
const currentPage = ref(1)
const selectedFilter = ref('all')
const dateFrom = ref('')
const dateTo = ref('')

const API_BASE_URL = import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'

// Methods
const getAuthHeaders = () => {
  const token = localStorage.getItem('auth_token')
  return {
    headers: {
      Authorization: `Bearer ${token}`
    }
  }
}

const loadActivities = async (reset = true) => {
  loading.value = true

  try {
    if (reset) {
      activities.value = []
      currentPage.value = 1
    }

    let endpoint = ''
    let params: any = {
      page: currentPage.value,
      pageSize: 20
    }

    if (selectedFilter.value === 'cycling') {
      endpoint = '/api/garminactivities/cycling'
      if (dateFrom.value) params.fromDate = dateFrom.value
      if (dateTo.value) params.toDate = dateTo.value
    } else {
      endpoint = '/api/garminactivities'
    }

    const response = await axios.get(`${API_BASE_URL}${endpoint}`, {
      ...getAuthHeaders(),
      params
    })

    const newActivities = response.data.activities || []

    if (reset) {
      activities.value = newActivities
    } else {
      activities.value.push(...newActivities)
    }

    hasMoreActivities.value = response.data.hasMore || false

  } catch (error) {
    console.error('Failed to load activities:', error)
  } finally {
    loading.value = false
  }
}

const loadMoreActivities = async () => {
  if (loadingMore.value || !hasMoreActivities.value) return

  loadingMore.value = true
  currentPage.value++

  try {
    await loadActivities(false)
  } finally {
    loadingMore.value = false
  }
}

const refreshActivities = () => {
  loadActivities(true)
}

const handleFilterChange = () => {
  loadActivities(true)
}

const handleDateChange = () => {
  if (selectedFilter.value === 'cycling') {
    loadActivities(true)
  }
}

const logout = () => {
  authStore.logout()
  router.push('/login')
}

const isCyclingActivity = (activityType: string) => {
  const cyclingTypes = [
    'CYCLING', 'BMX', 'CYCLOCROSS', 'DOWNHILL_BIKING',
    'E_BIKE_FITNESS', 'E_BIKE_MOUNTAIN', 'E_ENDURO_MTB',
    'ENDURO_MTB', 'GRAVEL_CYCLING', 'INDOOR_CYCLING',
    'MOUNTAIN_BIKING', 'RECUMBENT_CYCLING', 'ROAD_BIKING',
    'TRACK_CYCLING', 'VIRTUAL_RIDE', 'HANDCYCLING', 'INDOOR_HANDCYCLING'
  ]
  return cyclingTypes.includes(activityType)
}

const formatActivityType = (activityType: string) => {
  return activityType
    .replace(/_/g, ' ')
    .toLowerCase()
    .replace(/\b\w/g, l => l.toUpperCase())
}

const formatDate = (dateString: string, includeTime = false) => {
  const date = new Date(dateString)
  const options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }

  if (includeTime) {
    options.hour = '2-digit'
    options.minute = '2-digit'
  }

  return date.toLocaleDateString('en-US', options)
}

const formatDistance = (meters: number) => {
  const km = meters / 1000
  return km >= 1 ? `${km.toFixed(2)} km` : `${Math.round(meters)} m`
}

const formatDuration = (seconds: number) => {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const remainingSeconds = seconds % 60

  if (hours > 0) {
    return `${hours}h ${minutes}m`
  } else if (minutes > 0) {
    return `${minutes}m ${remainingSeconds}s`
  } else {
    return `${remainingSeconds}s`
  }
}

// Set default date range (last 3 months)
onMounted(() => {
  const today = new Date()
  const threeMonthsAgo = new Date(today.getFullYear(), today.getMonth() - 3, today.getDate())

  dateTo.value = today.toISOString().split('T')[0]
  dateFrom.value = threeMonthsAgo.toISOString().split('T')[0]

  loadActivities()
})
</script>
