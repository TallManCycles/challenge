<template>
  <div 
    :class="[
      'rounded-full overflow-hidden bg-gray-700 border border-gray-600 flex-shrink-0',
      sizeClasses[size]
    ]"
  >
    <img 
      v-if="photoUrl" 
      :src="getPhotoUrl(photoUrl)" 
      :alt="`${username || 'User'}'s profile photo`" 
      class="w-full h-full object-cover"
      @error="handleImageError"
    />
    <div v-else class="w-full h-full flex items-center justify-center">
      <div v-if="initials" class="text-white font-medium" :class="textSizeClasses[size]">
        {{ initials }}
      </div>
      <svg v-else class="text-gray-500" :class="iconSizeClasses[size]" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
      </svg>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

// Props
interface Props {
  photoUrl?: string | null
  username?: string
  fullName?: string
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl'
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md'
})

// Size classes mapping
const sizeClasses = {
  xs: 'w-6 h-6',
  sm: 'w-8 h-8', 
  md: 'w-10 h-10',
  lg: 'w-12 h-12',
  xl: 'w-16 h-16'
}

const iconSizeClasses = {
  xs: 'w-3 h-3',
  sm: 'w-4 h-4',
  md: 'w-5 h-5', 
  lg: 'w-6 h-6',
  xl: 'w-8 h-8'
}

const textSizeClasses = {
  xs: 'text-xs',
  sm: 'text-xs',
  md: 'text-sm',
  lg: 'text-base', 
  xl: 'text-lg'
}

// Reactive state
const imageError = ref(false)

// Computed
const initials = computed(() => {
  if (imageError.value && (props.fullName || props.username)) {
    const name = props.fullName || props.username || ''
    return name
      .split(' ')
      .map(word => word.charAt(0).toUpperCase())
      .slice(0, 2)
      .join('')
  }
  return null
})

const getPhotoUrl = computed(() => {
  return (url: string | null) => {
    if (!url) return null
    if (url.startsWith('http')) return url
    const baseUrl = import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'
    return `${baseUrl}${url}`
  }
})

// Methods
const handleImageError = () => {
  imageError.value = true
}
</script>