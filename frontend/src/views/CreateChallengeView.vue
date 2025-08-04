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
            <router-link to="/challenges/create" class="text-white hover:text-gray-300 transition-colors">Create Challenge</router-link>
            <router-link to="/activities" class="text-gray-400 hover:text-gray-300 transition-colors">My Activities</router-link>
            <router-link to="/settings" class="text-gray-400 hover:text-gray-300 transition-colors">Settings</router-link>
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
            class="text-white hover:text-gray-300 transition-colors py-2"
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
            class="text-gray-400 hover:text-gray-300 transition-colors py-2"
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
    <div class="max-w-2xl mx-auto p-6">
      <!-- Back Button and Title -->
      <div class="mb-8">
        <button
          @click="goBack"
          class="flex items-center text-gray-400 hover:text-white mb-4"
        >
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <h1 class="text-3xl font-bold mb-2">Create Challenge</h1>
        <p class="text-gray-400">Set up a new challenge for the community to join and compete</p>
      </div>

      <!-- Form -->
      <div class="bg-gray-800 rounded-lg p-6 space-y-6">
        <!-- Challenge Title -->
        <div>
          <label class="block text-sm font-medium mb-2">
            Challenge Title <span class="text-red-500">*</span>
          </label>
          <input
            v-model="form.title"
            type="text"
            placeholder="Enter challenge title..."
            class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            :class="{ 'border-red-500': errors.title }"
          />
          <p v-if="errors.title" class="text-red-500 text-sm mt-1">{{ errors.title }}</p>
        </div>

        <!-- Description -->
        <div>
          <label class="block text-sm font-medium mb-2">
            Description <span class="text-red-500">*</span>
          </label>
          <textarea
            v-model="form.description"
            placeholder="Describe your challenge, rules, and objectives..."
            rows="4"
            class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
            :class="{ 'border-red-500': errors.description }"
          ></textarea>
          <p v-if="errors.description" class="text-red-500 text-sm mt-1">{{ errors.description }}</p>
        </div>

        <!-- Challenge Type -->
        <div>
          <label class="block text-sm font-medium mb-2">
            Challenge Type <span class="text-red-500">*</span>
          </label>
          <select
            v-model="form.challengeType"
            class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            :class="{ 'border-red-500': errors.challengeType }"
          >
            <option value="">Select challenge type</option>
            <option :value="ChallengeType.Distance">Distance</option>
            <option :value="ChallengeType.Elevation">Elevation</option>
            <option :value="ChallengeType.Time">Time</option>
          </select>
          <p v-if="errors.challengeType" class="text-red-500 text-sm mt-1">{{ errors.challengeType }}</p>
        </div>

        <!-- Date Range -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium mb-2">
              Start Date <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.startDate"
              type="date"
              class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              :class="{ 'border-red-500': errors.startDate }"
            />
            <p v-if="errors.startDate" class="text-red-500 text-sm mt-1">{{ errors.startDate }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium mb-2">
              End Date <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.endDate"
              type="date"
              class="w-full px-4 py-3 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              :class="{ 'border-red-500': errors.endDate }"
            />
            <p v-if="errors.endDate" class="text-red-500 text-sm mt-1">{{ errors.endDate }}</p>
          </div>
        </div>

        <!-- Challenge Settings -->
        <div>
          <h3 class="text-lg font-medium mb-4">Challenge Settings</h3>
          
          <!-- Public Challenge -->
          <div class="flex items-center justify-between p-4 bg-gray-700 rounded-lg mb-3">
            <div>
              <h4 class="font-medium">Public Challenge</h4>
              <p class="text-sm text-gray-400">Anyone can discover and join this challenge</p>
            </div>
            <label class="relative inline-flex items-center cursor-pointer">
              <input
                v-model="form.isPublic"
                type="checkbox"
                class="sr-only peer"
              />
              <div class="w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
            </label>
          </div>

          <!-- Auto-join as Creator -->
          <div class="flex items-center justify-between p-4 bg-gray-700 rounded-lg">
            <div>
              <h4 class="font-medium">Auto-join as Creator</h4>
              <p class="text-sm text-gray-400">Automatically join your own challenge</p>
            </div>
            <label class="relative inline-flex items-center cursor-pointer">
              <input
                v-model="form.autoJoin"
                type="checkbox"
                class="sr-only peer"
              />
              <div class="w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
            </label>
          </div>
        </div>

        <!-- Action Buttons -->
        <div class="flex space-x-4 pt-4">
          <button
            @click="goBack"
            class="px-6 py-3 bg-gray-600 hover:bg-gray-500 rounded-lg font-medium transition-colors"
          >
            Cancel
          </button>
          <button
            @click="handleSubmit"
            :disabled="isSubmitting"
            class="flex-1 px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-800 disabled:cursor-not-allowed rounded-lg font-medium transition-colors flex items-center justify-center"
          >
            <span v-if="isSubmitting" class="animate-spin mr-2">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            </span>
            <svg v-if="!isSubmitting" class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            {{ isSubmitting ? 'Creating...' : 'Create Challenge' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { challengeService } from '../services/challenge'
import { ChallengeType } from '../types/challenge'

const router = useRouter()

const mobileMenuOpen = ref(false)

const form = reactive({
  title: '',
  description: '',
  challengeType: '' as ChallengeType | '',
  startDate: '',
  endDate: '',
  isPublic: true,
  autoJoin: true
})

const errors = reactive({
  title: '',
  description: '',
  challengeType: '',
  startDate: '',
  endDate: ''
})

const isSubmitting = ref(false)

const goBack = () => {
  router.push('/dashboard')
}

const toggleMobileMenu = () => {
  mobileMenuOpen.value = !mobileMenuOpen.value
}

const closeMobileMenu = () => {
  mobileMenuOpen.value = false
}

const logout = () => {
  // Add logout functionality here if needed
  router.push('/login')
}

const validateForm = (): boolean => {
  // Clear previous errors
  Object.keys(errors).forEach(key => {
    errors[key as keyof typeof errors] = ''
  })

  let isValid = true

  if (!form.title.trim()) {
    errors.title = 'Challenge title is required'
    isValid = false
  } else if (form.title.length < 3) {
    errors.title = 'Challenge title must be at least 3 characters'
    isValid = false
  } else if (form.title.length > 100) {
    errors.title = 'Challenge title must be less than 100 characters'
    isValid = false
  }

  if (!form.description?.trim()) {
    errors.description = 'Description is required'
    isValid = false
  } else if (form.description.length > 500) {
    errors.description = 'Description must be less than 500 characters'
    isValid = false
  }

  if (!form.challengeType) {
    errors.challengeType = 'Challenge type is required'
    isValid = false
  }

  if (!form.startDate) {
    errors.startDate = 'Start date is required'
    isValid = false
  }

  if (!form.endDate) {
    errors.endDate = 'End date is required'
    isValid = false
  }

  if (form.startDate && form.endDate) {
    const startDate = new Date(form.startDate)
    const endDate = new Date(form.endDate)
    
    if (endDate <= startDate) {
      errors.endDate = 'End date must be after start date'
      isValid = false
    }
  }

  return isValid
}

const handleSubmit = async () => {
  if (!validateForm()) {
    return
  }

  isSubmitting.value = true

  try {
    const challengeData = {
      title: form.title.trim(),
      description: form.description?.trim(),
      challengeType: form.challengeType as ChallengeType,
      startDate: new Date(form.startDate).toISOString(),
      endDate: new Date(form.endDate).toISOString()
    }

    const challenge = await challengeService.createChallenge(challengeData)
    
    // Auto-join if enabled
    if (form.autoJoin) {
      try {
        await challengeService.joinChallenge(challenge.id)
      } catch (error) {
        console.warn('Failed to auto-join challenge:', error)
      }
    }

    // Redirect to dashboard with success message
    router.push('/dashboard')
  } catch (error) {
    console.error('Failed to create challenge:', error)
    // You could show a toast notification here
  } finally {
    isSubmitting.value = false
  }
}
</script>