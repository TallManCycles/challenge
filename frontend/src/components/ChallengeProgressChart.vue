<template>
  <div class="relative h-80">
    <div v-if="loading" class="absolute inset-0 flex items-center justify-center">
      <div class="text-center">
        <div class="w-16 h-16 bg-gray-700 rounded-lg mx-auto mb-4 flex items-center justify-center animate-pulse">
          <svg class="w-8 h-8 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
        </div>
        <p class="text-gray-400">Loading progress data...</p>
      </div>
    </div>
    
    <div v-else-if="error" class="absolute inset-0 flex items-center justify-center">
      <div class="text-center">
        <div class="w-16 h-16 bg-red-700 rounded-lg mx-auto mb-4 flex items-center justify-center">
          <svg class="w-8 h-8 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
          </svg>
        </div>
        <p class="text-red-400">Failed to load progress data</p>
      </div>
    </div>
    
    <div v-else-if="!progressData || progressData.participants.length === 0" class="absolute inset-0 flex items-center justify-center">
      <div class="text-center">
        <div class="w-16 h-16 bg-gray-700 rounded-lg mx-auto mb-4 flex items-center justify-center">
          <svg class="w-8 h-8 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
        </div>
        <p class="text-gray-400">No progress data available</p>
      </div>
    </div>
    
    <Line v-else :data="chartData" :options="chartOptions" />
  </div>
  
  <!-- Legend -->
  <div v-if="progressData && progressData.participants.length > 0" class="flex items-center justify-center space-x-6 mt-4 text-sm flex-wrap">
    <div v-for="(participant, index) in progressData.participants" :key="participant.userId" class="flex items-center space-x-2 mb-2">
      <div 
        class="w-3 h-3 rounded-full" 
        :style="{ backgroundColor: getParticipantColor(index, participant.isCurrentUser) }"
      ></div>
      <span class="text-gray-300">
        {{ participant.isCurrentUser ? 'You' : (participant.fullName || participant.username) }}
      </span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  LineElement,
  LinearScale,
  CategoryScale,
  PointElement,
  type TooltipItem
} from 'chart.js'
import { challengeService } from '../services/challenge'
import type { ChallengeDailyProgress, ChallengeType } from '../types/challenge'

// Register Chart.js components
ChartJS.register(Title, Tooltip, Legend, LineElement, LinearScale, CategoryScale, PointElement)

interface Props {
  challengeId: number
  challengeType: ChallengeType
}

const props = defineProps<Props>()

const loading = ref(true)
const error = ref(false)
const progressData = ref<ChallengeDailyProgress | null>(null)

const participantColors = [
  '#3B82F6', // blue-500 for current user
  '#10B981', // emerald-500
  '#F59E0B', // amber-500
  '#EF4444', // red-500
  '#8B5CF6', // violet-500
  '#06B6D4', // cyan-500
  '#84CC16', // lime-500
  '#F97316', // orange-500
]

const getParticipantColor = (index: number, isCurrentUser: boolean) => {
  if (isCurrentUser) return participantColors[0] // Always blue for current user
  return participantColors[(index + 1) % participantColors.length]
}

const getUnitLabel = computed(() => {
  switch (props.challengeType) {
    case 1: // Distance
      return 'km'
    case 2: // Elevation
      return 'm'
    case 3: // Time
      return 'hours'
    default:
      return 'units'
  }
})

const chartData = computed(() => {
  if (!progressData.value || progressData.value.participants.length === 0) {
    return { labels: [], datasets: [] }
  }

  const labels = progressData.value.participants[0]?.dailyProgress.map(entry => {
    const date = new Date(entry.date)
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  }) || []

  const datasets = progressData.value.participants.map((participant, index) => ({
    label: participant.isCurrentUser ? 'You' : (participant.fullName || participant.username),
    data: participant.dailyProgress.map(entry => entry.cumulativeValue),
    borderColor: getParticipantColor(index, participant.isCurrentUser),
    backgroundColor: getParticipantColor(index, participant.isCurrentUser),
    tension: 0.3,
    borderWidth: participant.isCurrentUser ? 3 : 2,
    pointRadius: participant.isCurrentUser ? 4 : 3,
    pointHoverRadius: participant.isCurrentUser ? 6 : 5,
  }))

  return { labels, datasets }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    intersect: false,
    mode: 'index' as const,
  },
  plugins: {
    legend: {
      display: false, // We'll use our custom legend
    },
    tooltip: {
      backgroundColor: 'rgba(17, 24, 39, 0.95)', // gray-900 with opacity
      titleColor: 'rgb(243, 244, 246)', // gray-100
      bodyColor: 'rgb(229, 231, 235)', // gray-200
      borderColor: 'rgb(75, 85, 99)', // gray-600
      borderWidth: 1,
      cornerRadius: 8,
      displayColors: true,
      callbacks: {
        label: (context: TooltipItem<'line'>) => {
          const value = context.parsed.y
          let formattedValue = ''
          
          if (props.challengeType === 3) { // Time challenge
            const hours = Math.floor(value)
            const minutes = Math.floor((value % 1) * 60)
            if (hours > 0) {
              formattedValue = minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`
            } else {
              formattedValue = `${minutes}m`
            }
          } else {
            formattedValue = `${value.toFixed(1)} ${getUnitLabel.value}`
          }
          
          return `${context.dataset.label}: ${formattedValue}`
        }
      }
    },
  },
  scales: {
    x: {
      grid: {
        color: 'rgba(75, 85, 99, 0.3)', // gray-600 with opacity
      },
      ticks: {
        color: 'rgb(156, 163, 175)', // gray-400
      },
    },
    y: {
      beginAtZero: true,
      grid: {
        color: 'rgba(75, 85, 99, 0.3)', // gray-600 with opacity
      },
      ticks: {
        color: 'rgb(156, 163, 175)', // gray-400
        callback: function(value: string | number) {
          // Format time values as hours and minutes
          if (props.challengeType === 3) { // Time challenge
            const hours = Math.floor(Number(value))
            const minutes = Math.floor((Number(value) % 1) * 60)
            if (hours > 0) {
              return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`
            } else {
              return `${minutes}m`
            }
          }
          return `${value} ${getUnitLabel.value}`
        }
      },
    },
  },
}))

const loadProgressData = async () => {
  try {
    loading.value = true
    error.value = false
    progressData.value = await challengeService.getChallengeDailyProgress(props.challengeId)
  } catch (err) {
    console.error('Failed to load progress data:', err)
    error.value = true
  } finally {
    loading.value = false
  }
}

// Watch for changes in challengeId
watch(() => props.challengeId, () => {
  if (props.challengeId) {
    loadProgressData()
  }
})

onMounted(() => {
  if (props.challengeId) {
    loadProgressData()
  }
})
</script>