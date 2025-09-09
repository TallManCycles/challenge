<template>
  <div class="min-h-screen bg-gray-900 text-white">
    <!-- Header -->
    <header class="border-b border-gray-800 px-6 py-4">
      <div class="max-w-7xl mx-auto flex items-center justify-between">
        <div class="flex items-center">
          <h1 class="text-xl font-bold text-white">ChallengeHub</h1>

          <!-- Desktop Navigation -->
          <nav class="hidden md:flex space-x-6 ml-8">
            <router-link to="/dashboard" class="text-gray-400 hover:text-gray-300 transition-colors">Challenges</router-link>
            <router-link to="/challenges/create" class="text-gray-400 hover:text-gray-300 transition-colors">Create Challenge</router-link>
            <router-link to="/activities" class="text-gray-400 hover:text-gray-300 transition-colors">My Activities</router-link>
            <router-link to="/settings" class="text-white hover:text-gray-300 transition-colors">Settings</router-link>
          </nav>
        </div>

        <div class="flex items-center space-x-4">
          <!-- Desktop Logout Button -->
          <button
            @click="logout"
            class="hidden md:block bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg transition-colors text-sm"
          >
            Logout
          </button>

          <!-- Mobile Menu Button -->
          <button
            @click="toggleMobileMenu"
            class="md:hidden text-gray-400 hover:text-white transition-colors p-2"
            aria-label="Toggle mobile menu"
          >
            <svg v-if="!mobileMenuOpen" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
            </svg>
            <svg v-else class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>
      </div>

      <!-- Mobile Menu -->
      <div v-if="mobileMenuOpen" class="md:hidden border-t border-gray-800 mt-4 pt-4">
        <nav class="flex flex-col space-y-3">
          <router-link
            to="/dashboard"
            @click="closeMobileMenu"
            class="text-gray-400 hover:text-gray-300 transition-colors py-2"
          >
            Challenges
          </router-link>
          <router-link
            to="/challenges/create"
            @click="closeMobileMenu"
            class="text-gray-400 hover:text-gray-300 transition-colors py-2"
          >
            Create Challenge
          </router-link>
          <router-link
            to="/activities"
            @click="closeMobileMenu"
            class="text-gray-400 hover:text-gray-300 transition-colors py-2"
          >
            My Activities
          </router-link>
          <router-link
            to="/settings"
            @click="closeMobileMenu"
            class="text-white hover:text-gray-300 transition-colors py-2"
          >
            Settings
          </router-link>
          <button
            @click="logout"
            class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg transition-colors text-sm text-left mt-4"
          >
            Logout
          </button>
        </nav>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-4xl mx-auto px-6 py-8">
      <!-- Page Title -->
      <div class="mb-8">
        <h2 class="text-3xl font-bold text-white mb-2">Settings</h2>
        <p class="text-gray-400">Manage your account preferences and integrations</p>
      </div>

      <!-- Toast Notifications -->
      <div v-if="showToast" class="fixed top-4 right-4 z-50">
        <div
          :class="`px-6 py-4 rounded-lg shadow-lg ${toastType === 'success' ? 'bg-green-600' : toastType === 'error' ? 'bg-red-600' : 'bg-blue-600'} text-white flex items-center space-x-3`"
        >
          <svg v-if="toastType === 'success'" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
          </svg>
          <svg v-else-if="toastType === 'error'" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
          </svg>
          <svg v-else class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
          </svg>
          <span>{{ toastMessage }}</span>
          <button @click="hideToast" class="ml-4 text-white/80 hover:text-white">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>

      <form @submit.prevent="handleSubmit" class="space-y-8">
        <!-- Profile Information Section -->
        <section class="bg-gray-800 rounded-lg p-6">
          <div class="flex items-center mb-6">
            <div class="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center mr-3">
              <svg class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white">Profile Information</h3>
          </div>

          <!-- Profile Photo Upload -->
          <div class="mb-8">
            <ProfilePhotoUpload 
              v-model="currentUser.profilePhotoUrl"
              @uploaded="handlePhotoUploaded"
              @deleted="handlePhotoDeleted"
              @error="handlePhotoError"
            />
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label for="fullName" class="block text-sm font-medium text-gray-300 mb-2">
                Full Name
              </label>
              <input
                id="fullName"
                v-model="profileForm.fullName"
                type="text"
                placeholder="John Doe"
                class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              />
            </div>

            <div>
              <label for="email" class="block text-sm font-medium text-gray-300 mb-2">
                Email Address
              </label>
              <input
                id="email"
                v-model="profileForm.email"
                type="email"
                placeholder="john.doe@example.com"
                required
                class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              />
            </div>
          </div>
        </section>

        <!-- Zwift Integration Section -->
        <section class="bg-gray-800 rounded-lg p-6">
          <div class="flex items-center mb-6">
            <div class="w-8 h-8 bg-orange-500 rounded-lg flex items-center justify-center mr-3">
              <svg class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2a10 10 0 100 20 10 10 0 000-20zm0 18a8 8 0 110-16 8 8 0 010 16z"/>
                <path d="M8 12l2 2 4-4"/>
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white">Zwift Integration</h3>
          </div>

          <div class="grid grid-cols-1 gap-6">
            <div>
              <label for="zwiftUserId" class="block text-sm font-medium text-gray-300 mb-2">
                Zwift User ID
              </label>
              <input
                id="zwiftUserId"
                v-model="profileForm.zwiftUserId"
                type="text"
                placeholder="e.g., 123456789"
                class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-transparent transition-all duration-200"
              />
              <p class="text-sm text-gray-400 mt-2">
                Enter your Zwift User ID to link your FIT file activities to challenges. 
                Any unprocessed FIT files will be automatically reprocessed when you save this.
              </p>
            </div>
          </div>
        </section>

        <!-- Change Password Section -->
        <section class="bg-gray-800 rounded-lg p-6">
          <div class="flex items-center mb-6">
            <div class="w-8 h-8 bg-green-600 rounded-lg flex items-center justify-center mr-3">
              <svg class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white">Change Password</h3>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label for="currentPassword" class="block text-sm font-medium text-gray-300 mb-2">
                Current Password
              </label>
              <input
                id="currentPassword"
                v-model="passwordForm.currentPassword"
                type="password"
                class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              />
            </div>

            <div>
              <label for="newPassword" class="block text-sm font-medium text-gray-300 mb-2">
                New Password
              </label>
              <input
                id="newPassword"
                v-model="passwordForm.newPassword"
                type="password"
                class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              />
            </div>
          </div>
        </section>

        <!-- Notification Preferences Section -->
        <section class="bg-gray-800 rounded-lg p-6">
          <div class="flex items-center mb-6">
            <div class="w-8 h-8 bg-yellow-600 rounded-lg flex items-center justify-center mr-3">
              <svg class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path d="M10 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 004 14h12a1 1 0 00.707-1.707L16 11.586V8a6 6 0 00-6-6zM10 18a3 3 0 01-3-3h6a3 3 0 01-3 3z"/>
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white">Notification Preferences</h3>
          </div>

          <div class="space-y-4">
            <div class="flex items-center justify-between p-4 bg-gray-700 rounded-lg">
              <div>
                <h4 class="text-white font-medium">Email Notifications</h4>
                <p class="text-gray-400 text-sm">Receive email notifications when other participants upload activities to challenges you're part of</p>
              </div>
              <label class="relative inline-flex items-center cursor-pointer">
                <input 
                  type="checkbox" 
                  v-model="profileForm.emailNotificationsEnabled"
                  class="sr-only peer"
                >
                <div class="w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              </label>
            </div>
          </div>
        </section>

        <div class="flex items-center justify-between">
          <div class="flex space-x-3">
            <button
              type="button"
              @click="cancelChanges"
              class="bg-gray-700 hover:bg-gray-600 text-white py-2 px-6 rounded-lg font-medium transition-colors"
            >
              Cancel
            </button>

            <button
              type="submit"
              :disabled="isSubmitting"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white py-2 px-6 rounded-lg font-medium transition-colors flex items-center"
            >
              <svg v-if="isSubmitting" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <svg v-else class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              {{ isSubmitting ? 'Saving...' : 'Save Settings' }}
            </button>
          </div>
        </div>

        <!-- Integrations Section -->
        <section class="bg-gray-800 rounded-lg p-6">
          <div class="flex items-center mb-6">
            <div class="w-8 h-8 bg-purple-600 rounded-lg flex items-center justify-center mr-3">
              <svg class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5zm-5 5a2 2 0 012.828 0 1 1 0 101.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5a2 2 0 11-2.828-2.828l3-3z" clip-rule="evenodd" />
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white">Integrations</h3>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <!-- Garmin Connect -->
            <div class="bg-gray-700 rounded-lg p-4 text-center">
              <div class="w-12 h-12 mx-auto mb-3 bg-red-600 rounded-lg flex items-center justify-center">
                <svg class="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
                </svg>
              </div>
              <h4 class="font-semibold text-white mb-2">Garmin Connect</h4>
              <button
                type="button"
                @click="handleIntegrationClick('Garmin Connect')"
                :disabled="isConnectingGarmin"
                :class="[
                  'w-full py-2 px-4 rounded-lg font-medium transition-colors mb-2',
                  garminStatus.isConnected
                    ? 'bg-green-600 hover:bg-green-700 text-white'
                    : 'bg-red-600 hover:bg-red-700 text-white',
                  isConnectingGarmin && 'opacity-50 cursor-not-allowed'
                ]"
              >
                <svg v-if="isConnectingGarmin" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {{ isConnectingGarmin ? 'Processing...' : (garminStatus.isConnected ? 'Disconnect' : 'Connect') }}
              </button>
              <p class="text-xs text-gray-400">
                {{ garminStatus.isConnected ? 'Connected' : 'Not Connected' }}
                <span v-if="garminStatus.isConnected && garminStatus.connectedAt" class="block">
                  {{ new Date(garminStatus.connectedAt).toLocaleDateString() }}
                </span>
              </p>
            </div>

            <!-- Strava -->
            <div class="bg-gray-700 rounded-lg p-4 text-center">
              <div class="w-12 h-12 mx-auto mb-3 bg-orange-600 rounded-lg flex items-center justify-center">
                <svg class="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M9.5 2L6 10h2.5l1.5-4 1.5 4H14L10.5 2H9.5zM15 10l-2 6 2 6h1.5l2-6-2-6H15z"/>
                </svg>
              </div>
              <h4 class="font-semibold text-white mb-2">Strava</h4>
              <button
                type="button"
                @click="handleIntegrationClick('Strava')"
                class="w-full bg-green-600 hover:bg-green-700 text-white py-2 px-4 rounded-lg font-medium transition-colors mb-2"
              >
                Disconnect
              </button>
              <p class="text-xs text-gray-400">Connected</p>
            </div>

            <!-- Zwift -->
            <div class="bg-gray-700 rounded-lg p-4 text-center">
              <div class="w-12 h-12 mx-auto mb-3 bg-orange-500 rounded-lg flex items-center justify-center">
                <svg class="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 2a10 10 0 100 20 10 10 0 000-20zm0 18a8 8 0 110-16 8 8 0 010 16z"/>
                  <path d="M8 12l2 2 4-4"/>
                </svg>
              </div>
              <h4 class="font-semibold text-white mb-2">Zwift</h4>
              <button
                type="button"
                @click="handleIntegrationClick('Zwift')"
                class="w-full bg-orange-600 hover:bg-orange-700 text-white py-2 px-4 rounded-lg font-medium transition-colors mb-2"
              >
                Connect
              </button>
              <p class="text-xs text-gray-400">Not Connected</p>
            </div>

            <!-- Wahoo -->
            <div class="bg-gray-700 rounded-lg p-4 text-center">
              <div class="w-12 h-12 mx-auto mb-3 bg-blue-600 rounded-lg flex items-center justify-center">
                <svg class="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z"/>
                </svg>
              </div>
              <h4 class="font-semibold text-white mb-2">Wahoo</h4>
              <button
                type="button"
                @click="handleIntegrationClick('Wahoo')"
                class="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 px-4 rounded-lg font-medium transition-colors mb-2"
              >
                Connect
              </button>
              <p class="text-xs text-gray-400">Not Connected</p>
            </div>
          </div>
        </section>
      </form>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { authService } from '../services/auth'
import { garminService, type GarminOAuthStatus } from '../services/garmin'
import ProfilePhotoUpload from '../components/ProfilePhotoUpload.vue'
import type { User } from '../types/auth'

const router = useRouter()
const authStore = useAuthStore()

// Current user data
const currentUser = ref<User>({
  email: '',
  username: '',
  fullName: '',
  garminConnected: false,
  emailNotificationsEnabled: true,
  zwiftUserId: '',
  profilePhotoUrl: undefined
})

// Form data
const profileForm = ref({
  fullName: '',
  email: '',
  emailNotificationsEnabled: true,
  zwiftUserId: ''
})

const passwordForm = ref({
  currentPassword: '',
  newPassword: ''
})

// UI state
const isSubmitting = ref(false)
const showToast = ref(false)
const toastMessage = ref('')
const toastType = ref<'success' | 'error' | 'info'>('info')
const mobileMenuOpen = ref(false)

// Garmin integration state
const garminStatus = ref<GarminOAuthStatus>({ isConnected: false })
const isConnectingGarmin = ref(false)

// Load current user data
onMounted(async () => {
  try {
    const user = await authService.getCurrentUser()
    // Update currentUser data
    currentUser.value = { ...user }
    
    // Update form data
    profileForm.value.email = user.email
    profileForm.value.fullName = user.fullName || ''
    profileForm.value.emailNotificationsEnabled = user.emailNotificationsEnabled
    profileForm.value.zwiftUserId = user.zwiftUserId || ''
  } catch {
    showToastMessage('Failed to load user data', 'error')
  }

  // Load Garmin connection status
  await loadGarminStatus()

  // Check for OAuth callback parameters
  checkOAuthCallback()
})

const loadGarminStatus = async () => {
  try {
    garminStatus.value = await garminService.getOAuthStatus()
  } catch {
    console.warn('Failed to load Garmin status')
  }
}

const checkOAuthCallback = () => {
  const urlParams = new URLSearchParams(window.location.search)
  const garminResult = urlParams.get('garmin')

  if (garminResult === 'success') {
    showToastMessage('Garmin Connect connected successfully!', 'success')
    loadGarminStatus() // Refresh status
    // Clean up URL
    window.history.replaceState({}, document.title, '/settings')
  } else if (garminResult === 'error') {
    showToastMessage('Failed to connect to Garmin Connect. Please try again.', 'error')
    // Clean up URL
    window.history.replaceState({}, document.title, '/settings')
  }
}

const handlePhotoUploaded = (photoUrl: string) => {
  currentUser.value.profilePhotoUrl = photoUrl
  showToastMessage('Profile photo updated successfully!', 'success')
}

const handlePhotoDeleted = () => {
  currentUser.value.profilePhotoUrl = undefined
  showToastMessage('Profile photo deleted successfully!', 'success')
}

const handlePhotoError = (message: string) => {
  showToastMessage(message, 'error')
}

const logout = () => {
  authStore.logout()
  router.push('/login')
}

const toggleMobileMenu = () => {
  mobileMenuOpen.value = !mobileMenuOpen.value
}

const closeMobileMenu = () => {
  mobileMenuOpen.value = false
}

const showToastMessage = (message: string, type: 'success' | 'error' | 'info' = 'info') => {
  toastMessage.value = message
  toastType.value = type
  showToast.value = true

  setTimeout(() => {
    hideToast()
  }, 5000)
}

const hideToast = () => {
  showToast.value = false
}

const handleIntegrationClick = async (service: string) => {
  if (service === 'Garmin Connect') {
    await handleGarminIntegration()
  } else {
    showToastMessage(`${service} integration is not implemented yet`, 'info')
  }
}

const handleGarminIntegration = async () => {
  if (garminStatus.value.isConnected) {
    // Disconnect Garmin
    try {
      isConnectingGarmin.value = true
      await garminService.disconnectGarmin()
      garminStatus.value = { isConnected: false }
      showToastMessage('Garmin Connect disconnected successfully', 'success')
    } catch (error) {
      console.error('Failed to disconnect Garmin:', error)
      showToastMessage('Failed to disconnect Garmin Connect', 'error')
    } finally {
      isConnectingGarmin.value = false
    }
  } else {
    // Connect to Garmin
    try {
      isConnectingGarmin.value = true
      showToastMessage('Redirecting to Garmin Connect...', 'info')

      const oauthData = await garminService.initiateOAuth()
      garminService.redirectToGarmin(oauthData.url)
    } catch (error) {
      console.error('Failed to initiate Garmin OAuth:', error)
      showToastMessage('Failed to connect to Garmin Connect', 'error')
      isConnectingGarmin.value = false
    }
  }
}

const cancelChanges = async () => {
  try {
    const user = await authService.getCurrentUser()
    profileForm.value.email = user.email
    profileForm.value.fullName = user.fullName || ''
    profileForm.value.emailNotificationsEnabled = user.emailNotificationsEnabled
    profileForm.value.zwiftUserId = user.zwiftUserId || ''
    passwordForm.value.currentPassword = ''
    passwordForm.value.newPassword = ''
    showToastMessage('Changes cancelled', 'info')
  } catch {
    showToastMessage('Failed to reset form', 'error')
  }
}

const handleSubmit = async () => {
  if (isSubmitting.value) return

  isSubmitting.value = true

  try {
    // Update profile information
    const response = await authService.updateProfile({
      email: profileForm.value.email,
      fullName: profileForm.value.fullName || undefined,
      emailNotificationsEnabled: profileForm.value.emailNotificationsEnabled,
      zwiftUserId: profileForm.value.zwiftUserId || undefined
    })

    // Handle password change if provided
    if (passwordForm.value.currentPassword && passwordForm.value.newPassword) {
      await authService.changePassword({
        currentPassword: passwordForm.value.currentPassword,
        newPassword: passwordForm.value.newPassword,
        confirmNewPassword: passwordForm.value.newPassword
      })

      // Clear password fields on success
      passwordForm.value.currentPassword = ''
      passwordForm.value.newPassword = ''
    }

    // Show success message with FIT file reprocessing info if applicable
    let successMessage = 'Settings saved successfully!'
    if (response.reprocessedFitFiles && response.reprocessedFitFiles > 0) {
      successMessage += ` ${response.reprocessedFitFiles} FIT files were reprocessed for challenges.`
    }
    showToastMessage(successMessage, 'success')

    // Refresh user data in auth store
    await authStore.getCurrentUser()

  } catch (error) {
    console.error('Settings update failed:', error)
    showToastMessage(
      error instanceof Error ? error.message : 'Failed to save settings',
      'error'
    )
  } finally {
    isSubmitting.value = false
  }
}
</script>
