<template>
  <div ref="root" class="ms" :class="{ 'ms--disabled': disabled }">
    <div
      class="ms__control"
      role="combobox"
      :aria-expanded="open ? 'true' : 'false'"
      aria-haspopup="listbox"
      tabindex="0"
      @click="toggleOpen"
      @keydown.enter.prevent="toggleOpen"
      @keydown.space.prevent="toggleOpen"
      @keydown.esc.prevent="close"
    >
      <div class="ms__value">
        <template v-if="selectedOptions.length">
          <span v-for="opt in selectedOptions" :key="opt.id" class="ms__chip">
            {{ opt.nome }}
            <button
              type="button"
              class="ms__chip-remove"
              aria-label="Remover"
              @click.stop="remove(opt.id)"
            >
              ×
            </button>
          </span>
        </template>
        <span v-else class="ms__placeholder">{{ placeholder }}</span>
      </div>

      <span class="ms__arrow" aria-hidden="true">▾</span>
    </div>

    <div v-if="open" class="ms__dropdown" role="listbox">
      <div class="ms__search">
        <input
          v-model="query"
          type="text"
          placeholder="Pesquisar superpoder..."
          autocomplete="off"
          @keydown.esc.prevent="close"
        />
      </div>

      <div class="ms__options">
        <label v-for="p in filteredOptions" :key="p.id" class="ms__option">
          <input
            type="checkbox"
            :checked="isSelected(p.id)"
            :disabled="disabled"
            @change="toggle(p.id)"
          />
          <div class="ms__option-text">
            <div class="ms__option-name">{{ p.nome }}</div>
            <div class="ms__option-desc">{{ p.descricao }}</div>
          </div>
        </label>

        <div v-if="!filteredOptions.length" class="ms__empty">Nenhum superpoder encontrado.</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { SuperpoderDto } from '../../types/models'

const props = withDefaults(
  defineProps<{
    modelValue: number[]
    options: SuperpoderDto[]
    placeholder?: string
    disabled?: boolean
  }>(),
  {
    placeholder: 'Selecione superpoderes',
    disabled: false
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', v: number[]): void
}>()

const root = ref<HTMLElement | null>(null)
const open = ref(false)
const query = ref('')

const selectedOptions = computed(() => {
  const set = new Set(props.modelValue || [])
  return props.options.filter((o) => set.has(o.id))
})

const filteredOptions = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return props.options
  return props.options.filter((o) => {
    const hay = `${o.nome} ${o.descricao}`.toLowerCase()
    return hay.includes(q)
  })
})

function isSelected(id: number) {
  return (props.modelValue || []).includes(id)
}

function toggle(id: number) {
  if (props.disabled) return
  const current = props.modelValue ? [...props.modelValue] : []
  const idx = current.indexOf(id)
  if (idx >= 0) current.splice(idx, 1)
  else current.push(id)
  emit('update:modelValue', current)
}

function remove(id: number) {
  if (props.disabled) return
  const current = props.modelValue ? props.modelValue.filter((x) => x !== id) : []
  emit('update:modelValue', current)
}

function toggleOpen() {
  if (props.disabled) return
  open.value = !open.value
  if (open.value) {
    // reset search when opening
    query.value = ''
  }
}

function close() {
  open.value = false
}

function handleClickOutside(ev: MouseEvent) {
  if (!open.value) return
  const el = root.value
  if (!el) return
  if (ev.target instanceof Node && !el.contains(ev.target)) {
    close()
  }
}

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside)
})

onBeforeUnmount(() => {
  document.removeEventListener('mousedown', handleClickOutside)
})

watch(
  () => props.disabled,
  (d) => {
    if (d) close()
  }
)
</script>
