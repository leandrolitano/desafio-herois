<template>
  <div v-if="visible" class="toast" :class="`toast--${type}`" role="status" aria-live="polite">
    <span>{{ message }}</span>
    <button class="toast__close" @click="$emit('close')" aria-label="Fechar">×</button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  visible: boolean
  message: string
  type?: 'success' | 'error' | 'info'
}>()

defineEmits<{
  (e: 'close'): void
}>()
</script>
