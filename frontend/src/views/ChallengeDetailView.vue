<template>
  <div class="min-h-screen bg-gray-900 text-white">
    <!-- Header -->
    <header class="border-b border-gray-800 px-6 py-4">
      <div class="max-w-7xl mx-auto flex items-center justify-between">
        <div class="flex items-center space-x-4">
          <button 
            @click="goBack"
            class="text-gray-400 hover:text-white transition-colors"
            data-testid="back-button"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <h1 class="text-xl font-bold text-white" data-testid="challenge-title">{{ challenge?.title || 'Loading...' }}</h1>
        </div>
        <div class="flex items-center space-x-4">
          <button class="text-gray-400 hover:text-white transition-colors">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.367 2.684 3 3 0 00-5.367-2.684z" />
            </svg>
          </button>
          <UserAvatar 
            :photo-url="currentUser?.profilePhotoUrl"
            :username="currentUser?.username"
            :full-name="currentUser?.fullName"
            size="sm"
          />
        </div>
      </div>
    </header>

    <!-- Loading State -->
    <div v-if="loading" class="max-w-7xl mx-auto px-6 py-8" data-testid="loading-state">
      <div class="animate-pulse">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2 space-y-6">
            <div class="bg-gray-800 rounded-lg p-6 h-96"></div>
            <div class="bg-gray-800 rounded-lg p-6 h-64"></div>
          </div>
          <div class="space-y-6">
            <div class="bg-gray-800 rounded-lg p-6 h-48"></div>
            <div class="bg-gray-800 rounded-lg p-6 h-96"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <main v-else class="max-w-7xl mx-auto px-6 py-8">
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Left Column -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Progress Comparison Chart -->
          <div class="bg-gray-800 rounded-lg p-6">
            <h3 class="text-xl font-semibold text-white mb-6">Progress Comparison</h3>
            <ChallengeProgressChart 
              v-if="challenge"
              :challenge-id="challenge.id"
              :challenge-type="challenge.challengeType"
            />
          </div>

          <!-- Recent Activities -->
          <div class="bg-gray-800 rounded-lg p-6">
            <h3 class="text-xl font-semibold text-white mb-6">Recent Activities</h3>
            <div v-if="recentActivities.length === 0" class="text-center py-8">
              <div class="text-gray-500 mb-2">
                <svg class="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <p class="text-gray-400">No recent activities</p>
            </div>
            <div v-else class="space-y-4">
              <div v-for="activity in recentActivities" :key="activity.id" class="flex items-center space-x-4 p-3 bg-gray-750 rounded-lg">
                <UserAvatar 
                  :photo-url="activity.profilePhotoUrl"
                  :username="activity.username"
                  :full-name="activity.fullName"
                  size="md"
                />
                <div class="flex-1">
                  <div class="flex items-center space-x-2 mb-1">
                    <span class="font-medium text-white">{{ activity.fullName || activity.username }}</span>
                    <span class="text-gray-400 text-sm">{{ activity.activityName }}</span>
                  </div>
                  <p class="text-gray-300 text-sm">
                    {{ activity.distance.toFixed(1) }} km • {{ formatDuration(activity.movingTime) }}
                  </p>
                  <p class="text-gray-500 text-xs">{{ formatTimeAgo(activity.activityDate) }}</p>
                </div>
                <div class="flex items-center space-x-1">
                  <button 
                    @click="toggleLike(activity)"
                    class="flex items-center space-x-1 transition-colors"
                    :class="activity.isLikedByCurrentUser ? 'text-red-500' : 'text-gray-400 hover:text-red-400'"
                    :disabled="likingActivity === activity.id"
                  >
                    <svg 
                      class="w-4 h-4 transition-transform"
                      :class="likingActivity === activity.id ? 'animate-pulse' : ''"
                      :fill="activity.isLikedByCurrentUser ? 'currentColor' : 'none'" 
                      stroke="currentColor" 
                      viewBox="0 0 24 24"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                    </svg>
                    <span class="text-sm">{{ activity.likeCount }}</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Column -->
        <div class="space-y-6">
          <!-- Your Position Card -->
          <div class="bg-gradient-to-br from-blue-600 to-purple-700 rounded-lg p-6 text-white">
            <h3 class="text-lg font-semibold mb-4">Your Position</h3>
            <div class="text-center mb-4">
              <div class="text-4xl font-bold mb-1">{{ userPosition.total }}</div>
              <div class="text-blue-200 text-sm">{{ challenge?.challengeTypeName || 'units' }} completed</div>
            </div>
            
            <!-- Position Status -->
            <div class="text-center mb-4">
              <div class="text-2xl font-bold mb-1" :class="userPosition.statusColor">
                {{ userPosition.positionText }}
              </div>
              <div class="text-blue-200 text-sm">{{ userPosition.statusMessage }}</div>
            </div>
            
            <div class="flex justify-between text-sm">
              <div class="text-center">
                <div class="font-semibold">#{{ userPosition.rank }}</div>
                <div class="text-blue-200">Position</div>
              </div>
              <div class="text-center" v-if="userPosition.gapToLeader !== null">
                <div class="font-semibold">{{ formatGapValue(userPosition.gapToLeader) }}</div>
                <div class="text-blue-200">{{ userPosition.gapToLeader >= 0 ? 'Ahead' : 'Behind' }}</div>
              </div>
            </div>
          </div>

          <!-- Leaderboard -->
          <div class="bg-gray-800 rounded-lg p-6">
            <div class="flex items-center justify-between mb-6">
              <h3 class="text-xl font-semibold text-white">Leaderboard</h3>
            </div>
            
            <div v-if="leaderboard.length === 0" class="text-center py-8">
              <div class="text-gray-500 mb-2">
                <svg class="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
                </svg>
              </div>
              <p class="text-gray-400">No participants yet</p>
            </div>
            <div v-else class="space-y-3">
              <div 
                v-for="participant in leaderboard" 
                :key="participant.userId"
                class="flex items-center space-x-3 p-3 rounded-lg"
                :class="participant.isCurrentUser ? 'bg-blue-600' : 'bg-gray-750'"
              >
                <div class="flex-shrink-0">
                  <div 
                    class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold"
                    :class="getPositionColorClass(participant.position)"
                  >
                    {{ participant.position }}
                  </div>
                </div>
                <UserAvatar 
                  :photo-url="participant.profilePhotoUrl"
                  :username="participant.username"
                  :full-name="participant.fullName"
                  size="sm"
                />
                <div class="flex-1">
                  <div class="font-medium text-white">
                    {{ participant.isCurrentUser ? 'You' : (participant.fullName || participant.username) }}
                  </div>
                  <div class="text-gray-400 text-sm">{{ participant.currentTotal.toFixed(1) }} {{ challenge?.challengeTypeName || 'units' }}</div>
                </div>
                <div v-if="participant.position === 1" class="text-yellow-500">
                  <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                </div>
              </div>
            </div>

            <button class="w-full mt-4 bg-blue-600 hover:bg-blue-700 text-white py-2 px-4 rounded-lg transition-colors">
              View Full Leaderboard
            </button>
          </div>
        </div>
      </div>

      <!-- Leave Challenge Section -->
      <div v-if="challenge && isUserParticipating" class="max-w-7xl mx-auto px-6 py-6 mt-8 border-t border-gray-800">
        <div class="text-center">
          <h3 class="text-lg font-medium text-gray-300 mb-2">Want to leave this challenge?</h3>
          <p class="text-gray-500 text-sm mb-4">
            You'll lose your current progress and won't receive any more activity updates for this challenge.
          </p>
          <button
            @click="showLeaveConfirmation = true"
            :disabled="leavingChallenge"
            class="px-6 py-2 bg-red-600 hover:bg-red-700 disabled:bg-red-800 disabled:cursor-not-allowed text-white rounded-lg transition-colors"
          >
            {{ leavingChallenge ? 'Leaving...' : 'Leave Challenge' }}
          </button>
        </div>
      </div>
    </main>

    <!-- Leave Confirmation Modal -->
    <div v-if="showLeaveConfirmation" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-gray-800 rounded-lg p-6 max-w-md mx-4">
        <div class="text-center">
          <div class="w-12 h-12 bg-red-600 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
          </div>
          <h3 class="text-xl font-semibold text-white mb-2">Leave Challenge?</h3>
          <p class="text-gray-300 mb-6">
            Are you sure you want to leave "{{ challenge?.title }}"? This action cannot be undone and you'll lose your current progress.
          </p>
          <div class="flex space-x-3">
            <button
              @click="showLeaveConfirmation = false"
              :disabled="leavingChallenge"
              class="flex-1 px-4 py-2 bg-gray-600 hover:bg-gray-700 disabled:bg-gray-800 text-white rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              @click="leaveChallenge"
              :disabled="leavingChallenge"
              class="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 disabled:bg-red-800 text-white rounded-lg transition-colors"
            >
              {{ leavingChallenge ? 'Leaving...' : 'Leave Challenge' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { challengeService } from '../services/challenge'
import { activityService } from '../services/activity'
import { authService } from '../services/auth'
import ChallengeProgressChart from '../components/ChallengeProgressChart.vue'
import UserAvatar from '../components/UserAvatar.vue'
import type { ChallengeDetails, ChallengeActivity, ChallengeLeaderboard } from '../types/challenge'
import type { User } from '../types/auth'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const challenge = ref<ChallengeDetails | null>(null)
const recentActivities = ref<ChallengeActivity[]>([])
const leaderboard = ref<ChallengeLeaderboard[]>([])
const currentUser = ref<User | null>(null)
const likingActivity = ref<number | null>(null)
const showLeaveConfirmation = ref(false)
const leavingChallenge = ref(false)

const goBack = () => {
  router.push('/dashboard')
}

const isUserParticipating = computed(() => {
  if (!challenge.value) return false
  return challenge.value.participants.some(p => p.isCurrentUser)
})

const leaveChallenge = async () => {
  if (!challenge.value) return
  
  try {
    leavingChallenge.value = true
    await challengeService.leaveChallenge(challenge.value.id)
    
    // Close modal and navigate back to dashboard
    showLeaveConfirmation.value = false
    router.push('/dashboard')
  } catch (error) {
    console.error('Failed to leave challenge:', error)
    // Could show a toast notification here
    leavingChallenge.value = false
  }
}

const toggleLike = async (activity: ChallengeActivity) => {
  try {
    likingActivity.value = activity.id
    
    if (activity.isLikedByCurrentUser) {
      // Unlike the activity
      const response = await activityService.unlikeActivity(activity.id)
      activity.likeCount = response.likeCount
      activity.isLikedByCurrentUser = response.isLiked
    } else {
      // Like the activity
      const response = await activityService.likeActivity(activity.id)
      activity.likeCount = response.likeCount
      activity.isLikedByCurrentUser = response.isLiked
    }
  } catch (error) {
    console.error('Failed to toggle like:', error)
    // Could show a toast notification here
  } finally {
    likingActivity.value = null
  }
}

// Calculate user's position relative to other participants
const userPosition = computed(() => {
  if (!challenge.value || !leaderboard.value.length) {
    return { 
      total: 0, 
      rank: 0, 
      positionText: 'Loading...', 
      statusMessage: '', 
      statusColor: 'text-gray-300',
      gapToLeader: null
    }
  }

  // Find current user in leaderboard
  const currentUser = leaderboard.value.find(p => p.isCurrentUser)
  if (!currentUser) {
    return { 
      total: 0, 
      rank: 0, 
      positionText: 'Not Participating', 
      statusMessage: '', 
      statusColor: 'text-gray-300',
      gapToLeader: null
    }
  }

  const total = currentUser.currentTotal
  const rank = currentUser.position
  
  // Calculate gap - if user is leader, compare to second place, otherwise compare to leader
  let gapToLeader = 0
  if (rank === 1 && leaderboard.value.length > 1) {
    // User is in first place, compare to second place
    const secondPlace = leaderboard.value[1]
    gapToLeader = total - secondPlace.currentTotal // This will be positive (ahead)
  } else if (rank > 1) {
    // User is not in first place, compare to leader
    const leader = leaderboard.value[0]
    gapToLeader = total - leader.currentTotal // This will be negative (behind)
  }

  // Format total display based on challenge type
  const formatValue = (value: number) => {
    if (challenge.value?.challengeType === 3) { // Time challenge
      return formatDuration(value * 3600) // Convert hours back to seconds for formatting
    }
    return value.toFixed(1)
  }

  // Format gap display based on challenge type
  const formatGap = (gap: number) => {
    if (challenge.value?.challengeType === 3) { // Time challenge
      return formatDuration(Math.abs(gap) * 3600) // Convert hours back to seconds for formatting
    }
    return Math.abs(gap).toFixed(1)
  }

  let positionText = ''
  let statusMessage = ''
  let statusColor = 'text-white'

  if (rank === 1) {
    positionText = '🏆 LEADING'
    if (leaderboard.value.length > 1) {
      statusMessage = `${formatGap(gapToLeader)} ahead of 2nd place`
    } else {
      statusMessage = 'You\'re in first place!'
    }
    statusColor = 'text-yellow-300'
  } else if (rank === 2) {
    positionText = '🥈 2ND PLACE'
    statusMessage = `${formatGap(gapToLeader)} behind leader`
    statusColor = 'text-gray-300'
  } else if (rank === 3) {
    positionText = '🥉 3RD PLACE'
    statusMessage = `${formatGap(gapToLeader)} behind leader`
    statusColor = 'text-orange-300'
  } else {
    positionText = `#${rank}`
    if (leaderboard.value.length > 1) {
      statusMessage = `${formatGap(gapToLeader)} behind leader`
    } else {
      statusMessage = 'Keep going!'
    }
    statusColor = 'text-blue-200'
  }

  return { 
    total: formatValue(total), 
    rank, 
    positionText, 
    statusMessage, 
    statusColor,
    gapToLeader: (leaderboard.value.length > 1 && gapToLeader !== 0) ? gapToLeader : null
  }
})

const getPositionColorClass = (position: number) => {
  switch (position) {
    case 1:
      return 'bg-yellow-500 text-yellow-900'
    case 2:
      return 'bg-gray-400 text-gray-900'
    case 3:
      return 'bg-orange-500 text-orange-900'
    default:
      return 'bg-gray-600 text-white'
  }
}

const formatTimeAgo = (dateString: string) => {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
  const diffDays = Math.floor(diffHours / 24)
  
  if (diffDays > 0) {
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
  } else if (diffHours > 0) {
    return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
  } else {
    return 'Just now'
  }
}

const formatDuration = (seconds: number) => {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  
  if (hours > 0) {
    return `${hours}h ${minutes}m`
  } else {
    return `${minutes} min`
  }
}

const formatGapValue = (gap: number) => {
  if (challenge.value?.challengeType === 3) { // Time challenge
    return formatDuration(Math.abs(gap) * 3600) // Convert hours back to seconds for formatting
  }
  return Math.abs(gap).toFixed(1)
}

const loadChallengeDetails = async () => {
  try {
    loading.value = true
    const challengeId = parseInt(route.params.id as string)
    
    // Load all the data in parallel
    const [challengeDetails, activities, leaderboardData] = await Promise.all([
      challengeService.getChallenge(challengeId),
      challengeService.getChallengeActivities(challengeId, 10),
      challengeService.getChallengeLeaderboard(challengeId)
    ])
    
    challenge.value = challengeDetails
    recentActivities.value = activities
    leaderboard.value = leaderboardData
    
  } catch (error) {
    console.error('Failed to load challenge details:', error)
    router.push('/dashboard')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadChallengeDetails()
  
  // Load current user data for avatar
  try {
    currentUser.value = await authService.getCurrentUser()
  } catch (error) {
    console.warn('Failed to load current user data:', error)
  }
})
</script>