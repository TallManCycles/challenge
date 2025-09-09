<template>
  <div class="profile-photo-upload">
    <!-- Current Profile Photo Display -->
    <div class="mb-6 text-center">
      <div class="inline-block relative">
        <div class="w-32 h-32 rounded-full overflow-hidden bg-gray-700 border-4 border-gray-600 mx-auto">
          <img 
            v-if="currentPhotoUrl" 
            :src="getPhotoUrl(currentPhotoUrl)" 
            alt="Profile photo" 
            class="w-full h-full object-cover"
            @error="handleImageError"
          />
          <div v-else class="w-full h-full flex items-center justify-center">
            <svg class="w-16 h-16 text-gray-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
            </svg>
          </div>
        </div>
        <!-- Delete button for existing photo -->
        <button 
          v-if="currentPhotoUrl && !isUploading"
          @click="deletePhoto"
          class="absolute -top-2 -right-2 bg-red-600 hover:bg-red-700 text-white rounded-full w-8 h-8 flex items-center justify-center transition-colors"
          :disabled="isDeleting"
        >
          <svg v-if="isDeleting" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <svg v-else class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Upload Button -->
    <div class="text-center mb-6">
      <label 
        for="photo-upload" 
        class="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg cursor-pointer transition-colors"
        :class="{ 'opacity-50 cursor-not-allowed': isUploading }"
      >
        <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clip-rule="evenodd" />
        </svg>
        {{ currentPhotoUrl ? 'Change Photo' : 'Upload Photo' }}
        <input 
          id="photo-upload"
          ref="fileInput"
          type="file" 
          accept="image/jpeg,image/png,image/webp"
          @change="handleFileSelect"
          class="hidden"
          :disabled="isUploading"
        />
      </label>
      <p class="text-sm text-gray-400 mt-2">
        JPEG, PNG, or WebP. Max 5MB.
      </p>
    </div>

    <!-- Cropper Modal -->
    <div v-if="showCropper" class="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50">
      <div class="bg-gray-800 rounded-lg p-6 max-w-2xl w-full mx-4">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xl font-semibold text-white">Crop Your Photo</h3>
          <button 
            @click="cancelCrop"
            class="text-gray-400 hover:text-white transition-colors"
          >
            <svg class="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <!-- Cropper Component -->
        <div class="mb-6">
          <Cropper
            ref="cropper"
            :src="originalImageSrc"
            :stencil-props="{
              aspectRatio: 1,
              resizeImage: true
            }"
            :auto-zoom="true"
            class="w-full h-96"
          />
        </div>

        <!-- Crop Controls -->
        <div class="flex items-center justify-between">
          <button
            @click="cancelCrop"
            class="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white rounded-lg transition-colors"
          >
            Cancel
          </button>
          <button
            @click="confirmCrop"
            :disabled="isUploading"
            class="px-6 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-lg transition-colors flex items-center"
          >
            <svg v-if="isUploading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            {{ isUploading ? 'Uploading...' : 'Save Photo' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Progress Bar -->
    <div v-if="uploadProgress > 0 && uploadProgress < 100" class="w-full bg-gray-700 rounded-full h-2 mb-4">
      <div 
        class="bg-blue-600 h-2 rounded-full transition-all duration-300" 
        :style="{ width: uploadProgress + '%' }"
      ></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Cropper } from 'vue-advanced-cropper'
import { authService } from '../services/auth'
import 'vue-advanced-cropper/dist/style.css'

// Props
interface Props {
  modelValue?: string | null
}

const props = defineProps<Props>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: string | null): void
  (e: 'uploaded', photoUrl: string): void
  (e: 'deleted'): void
  (e: 'error', message: string): void
}>()

// Reactive state
const currentPhotoUrl = ref<string | null>(props.modelValue || null)
const showCropper = ref(false)
const isUploading = ref(false)
const isDeleting = ref(false)
const uploadProgress = ref(0)
const originalImageSrc = ref<string>('')
const fileInput = ref<HTMLInputElement>()
const cropper = ref()

// Computed
const getPhotoUrl = computed(() => {
  return (url: string | null) => {
    if (!url) return undefined
    if (url.startsWith('http')) return url
    const baseUrl = import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'
    return `${baseUrl}${url}`
  }
})

// Watch for prop changes
const handleImageError = () => {
  console.warn('Failed to load profile image')
}

// Watch for changes in modelValue prop
watch(() => props.modelValue, (newValue) => {
  currentPhotoUrl.value = newValue || null
}, { immediate: true })

onMounted(() => {
  currentPhotoUrl.value = props.modelValue || null
})

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  
  if (!file) return

  // Validate file type
  const allowedTypes = ['image/jpeg', 'image/png', 'image/webp']
  if (!allowedTypes.includes(file.type)) {
    emit('error', 'Please select a JPEG, PNG, or WebP image file')
    return
  }

  // Validate file size (5MB)
  const maxSize = 5 * 1024 * 1024
  if (file.size > maxSize) {
    emit('error', 'File size must be less than 5MB')
    return
  }

  // Create object URL for the cropper
  const reader = new FileReader()
  reader.onload = (e) => {
    originalImageSrc.value = e.target?.result as string
    showCropper.value = true
  }
  reader.readAsDataURL(file)
}

const cancelCrop = () => {
  showCropper.value = false
  originalImageSrc.value = ''
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

const confirmCrop = async () => {
  if (!cropper.value) return

  try {
    isUploading.value = true
    uploadProgress.value = 0

    // Get cropped canvas from the cropper
    const { canvas } = cropper.value.getResult()
    if (!canvas) {
      throw new Error('Failed to process image')
    }

    // Resize canvas to optimal size (max 512x512 for profile photos)
    const maxSize = 512
    let scaledCanvas = canvas
    
    if (canvas.width > maxSize || canvas.height > maxSize) {
      const scaleRatio = Math.min(maxSize / canvas.width, maxSize / canvas.height)
      
      scaledCanvas = document.createElement('canvas')
      const ctx = scaledCanvas.getContext('2d')
      
      if (!ctx) {
        throw new Error('Failed to get canvas context')
      }
      
      scaledCanvas.width = Math.round(canvas.width * scaleRatio)
      scaledCanvas.height = Math.round(canvas.height * scaleRatio)
      
      // Use high-quality image scaling
      ctx.imageSmoothingEnabled = true
      ctx.imageSmoothingQuality = 'high'
      
      ctx.drawImage(canvas, 0, 0, scaledCanvas.width, scaledCanvas.height)
    }

    // Convert canvas to blob with optimal settings
    const blob = await new Promise<Blob>((resolve, reject) => {
      scaledCanvas.toBlob((blob: Blob | null) => {
        if (blob) {
          resolve(blob)
        } else {
          reject(new Error('Failed to create image blob'))
        }
      }, 'image/jpeg', 0.8) // Use JPEG with 80% quality for good balance of quality and file size
    })

    // Create form data
    const formData = new FormData()
    formData.append('photo', blob, 'profile-photo.jpg')

    // Simulate progress for user feedback
    const progressInterval = setInterval(() => {
      if (uploadProgress.value < 90) {
        uploadProgress.value += 10
      }
    }, 100)

    // Upload the photo
    const response = await authService.uploadProfilePhoto(formData)
    
    clearInterval(progressInterval)
    uploadProgress.value = 100

    // Update state
    currentPhotoUrl.value = response.profilePhotoUrl
    emit('update:modelValue', response.profilePhotoUrl)
    emit('uploaded', response.profilePhotoUrl)
    
    // Hide cropper
    showCropper.value = false
    originalImageSrc.value = ''
    if (fileInput.value) {
      fileInput.value.value = ''
    }

    // Reset progress after a short delay
    setTimeout(() => {
      uploadProgress.value = 0
    }, 1000)

  } catch (error) {
    console.error('Upload failed:', error)
    const message = error instanceof Error ? error.message : 'Failed to upload photo'
    emit('error', message)
  } finally {
    isUploading.value = false
  }
}

const deletePhoto = async () => {
  if (!currentPhotoUrl.value) return

  try {
    isDeleting.value = true
    await authService.deleteProfilePhoto()
    
    currentPhotoUrl.value = null
    emit('update:modelValue', null)
    emit('deleted')
  } catch (error) {
    console.error('Delete failed:', error)
    const message = error instanceof Error ? error.message : 'Failed to delete photo'
    emit('error', message)
  } finally {
    isDeleting.value = false
  }
}
</script>

<style scoped>
.cropper {
  max-height: 24rem;
}
</style>