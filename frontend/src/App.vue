<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from './stores/auth'

const authStore = useAuthStore()

onMounted(() => {
  // Initialize auth store on app start
  authStore.initialize()
})
</script>

<template>
  <div id="app">
    <RouterView />
  </div>
</template>

<style scoped></style>
