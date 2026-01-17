<template>
  <div class="modal-backdrop" role="dialog" aria-modal="true" @click.self="$emit('cancel')">
    <div ref="modalEl" class="modal" tabindex="-1" @keydown.esc="$emit('cancel')">
      <header class="modal__header">
        <h3 class="modal__title">{{ title }}</h3>
      </header>

      <div class="modal__body">
        <p>{{ message }}</p>
      </div>

      <footer class="modal__footer">
        <button class="btn btn--ghost" @click="$emit('cancel')">{{ cancelText || 'Cancelar' }}</button>
        <button class="btn btn--danger" @click="$emit('confirm')">{{ confirmText || 'Confirmar' }}</button>
      </footer>
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue'

defineProps<{
  title: string
  message: string
  confirmText?: string
  cancelText?: string
}>()

defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()

const modalEl = ref<HTMLElement | null>(null)

onMounted(async () => {
  await nextTick()
  modalEl.value?.focus()
})
</script>
