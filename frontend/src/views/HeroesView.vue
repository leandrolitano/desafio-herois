<template>
  <div class="page">
    <header class="header">
      <h1>Desafio — Heróis</h1>
      <div class="header-actions">
        <button @click="openCreate">Novo herói</button>
      </div>
    </header>

    <section class="card">
      <div class="toolbar">
        <div class="toolbar__left">
          <input
            v-model="searchInput"
            placeholder="Buscar por nome ou nome de herói"
            @keyup.enter="applySearch"
          />
          <button class="btn" @click="applySearch" :disabled="loading">Buscar</button>
          <button class="btn btn--ghost" @click="clearSearch" :disabled="loading || !search">Limpar</button>
        </div>

        <div class="toolbar__right">
          <label class="page-size">
            Itens/página
            <select v-model.number="pageSize" @change="changePageSize">
              <option :value="10">10</option>
              <option :value="20">20</option>
              <option :value="50">50</option>
            </select>
          </label>
        </div>
      </div>

      <p v-if="error" class="error">{{ error }}</p>

      <HeroList
        :heroes="heroes"
        :loading="loading"
        @edit="openEdit"
        @delete="requestDelete"
      />

      <div v-if="total > 0" class="pagination">
        <div class="pagination__pages" aria-label="Paginação">
          <button
            class="btn btn--ghost pagination__nav"
            aria-label="Página anterior"
            @click="goToPage(page - 1)"
            :disabled="page <= 1 || loading"
          >
            ‹
          </button>

          <template v-for="t in pageTokens" :key="t.key">
            <button
              v-if="t.kind === 'page'"
              class="btn pagination__page"
              :class="{ 'btn--active': t.page === page, 'btn--ghost': t.page !== page }"
              :aria-current="t.page === page ? 'page' : undefined"
              :disabled="loading"
              @click="goToPage(t.page)"
            >
              {{ t.page }}
            </button>

            <span v-else class="pagination__dots" aria-hidden="true">…</span>
          </template>

          <button
            class="btn btn--ghost pagination__nav"
            aria-label="Próxima página"
            @click="goToPage(page + 1)"
            :disabled="page >= totalPages || loading"
          >
            ›
          </button>
        </div>

        <div class="pagination__meta">
          <span>Página <strong>{{ page }}</strong> de <strong>{{ totalPages }}</strong></span>
          <span>•</span>
          <span>Total: <strong>{{ total }}</strong></span>
        </div>
      </div>
    </section>

    <HeroForm
      v-if="showForm"
      :title="editingId ? 'Editar herói' : 'Novo herói'"
      :mode="editingId ? 'edit' : 'create'"
      :initial="form"
      :superpoderes="superpoderes"
      @save="save"
      @cancel="closeForm"
    />

    <ConfirmDialog
      v-if="confirmOpen"
      title="Confirmar exclusão"
      :message="confirmMessage"
      confirmText="Excluir"
      cancelText="Cancelar"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />

    <Toast
      :visible="toastVisible"
      :message="toastMessage"
      :type="toastType"
      @close="hideToast"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import HeroList from '../components/heroes/HeroList.vue'
import HeroForm from '../components/heroes/HeroForm.vue'
import ConfirmDialog from '../components/common/ConfirmDialog.vue'
import Toast from '../components/common/Toast.vue'
import { ApiError, api } from '../services/api'
import type { HeroFormState, HeroiDto, SuperpoderDto } from '../types/models'

// Dados paginados
const heroes = ref<HeroiDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const search = ref('')
const searchInput = ref('')

const totalPages = computed(() => {
  if (total.value <= 0) return 1
  return Math.max(1, Math.ceil(total.value / pageSize.value))
})

type PageToken =
  | { kind: 'page'; page: number; key: string }
  | { kind: 'dots'; key: string }

const pageTokens = computed<PageToken[]>(() =>
  buildPaginationTokens(page.value, totalPages.value, 2)
)

function buildPaginationTokens(current: number, totalPages: number, siblings = 2): PageToken[] {
  // siblings=2 => janela central de 5 páginas (current-2..current+2)
  if (totalPages <= 1) return [{ kind: 'page', page: 1, key: 'p1' }]

  const maxVisible = 2 * siblings + 5 // 1st + last + 2*dots + window
  if (totalPages <= maxVisible) {
    return Array.from({ length: totalPages }, (_, i) => ({
      kind: 'page',
      page: i + 1,
      key: `p${i + 1}`
    }))
  }

  const tokens: PageToken[] = []
  const first = 1
  const last = totalPages

  const left = Math.max(2, current - siblings)
  const right = Math.min(totalPages - 1, current + siblings)

  const showLeftDots = left > 2
  const showRightDots = right < totalPages - 1

  // Sempre mostra primeira página
  tokens.push({ kind: 'page', page: first, key: 'p1' })

  if (!showLeftDots && showRightDots) {
    // Próximo do início: mostra 2..(2*siblings+3) e reticências
    const end = 2 * siblings + 2
    for (let p = 2; p <= end; p++) tokens.push({ kind: 'page', page: p, key: `p${p}` })
    tokens.push({ kind: 'dots', key: 'd1' })
  } else if (showLeftDots && !showRightDots) {
    // Próximo do fim: reticências e mostra (last-(2*siblings+2))..(last-1)
    tokens.push({ kind: 'dots', key: 'd1' })
    const start = totalPages - (2 * siblings + 1)
    for (let p = start; p <= totalPages - 1; p++) tokens.push({ kind: 'page', page: p, key: `p${p}` })
  } else if (showLeftDots && showRightDots) {
    // Meio: reticências, janela central, reticências
    tokens.push({ kind: 'dots', key: 'd1' })
    for (let p = left; p <= right; p++) tokens.push({ kind: 'page', page: p, key: `p${p}` })
    tokens.push({ kind: 'dots', key: 'd2' })
  } else {
    // Fallback (não deveria acontecer)
    for (let p = 2; p <= totalPages - 1; p++) tokens.push({ kind: 'page', page: p, key: `p${p}` })
  }

  // Sempre mostra última página
  tokens.push({ kind: 'page', page: last, key: `p${last}` })
  return tokens
}

function goToPage(target: number) {
  const clamped = Math.min(Math.max(1, target), totalPages.value)
  if (clamped === page.value) return
  page.value = clamped
  loadHeroes()
}

function changePageSize() {
  page.value = 1
  loadHeroes()
}

function applySearch() {
  search.value = searchInput.value.trim()
  page.value = 1
  loadHeroes()
}

function clearSearch() {
  searchInput.value = ''
  search.value = ''
  page.value = 1
  loadHeroes()
}

// Dados auxiliares
const superpoderes = ref<SuperpoderDto[]>([])
const loading = ref(false)
const error = ref('')

// Form
const showForm = ref(false)
const editingId = ref<number | null>(null)
const editingRowVersion = ref<string>('')

const form = reactive<HeroFormState>({
  nome: '',
  nomeHeroi: '',
  dataNascimento: '',
  altura: 0,
  peso: 0,
  superpoderIds: []
})

function resetForm() {
  form.nome = ''
  form.nomeHeroi = ''
  form.dataNascimento = ''
  form.altura = 0
  form.peso = 0
  form.superpoderIds = []
}

// Toast
const toastVisible = ref(false)
const toastMessage = ref('')
const toastType = ref<'success' | 'error' | 'info'>('info')
let toastTimer: number | undefined

function showToast(message: string, type: 'success' | 'error' | 'info' = 'info') {
  toastMessage.value = message
  toastType.value = type
  toastVisible.value = true

  if (toastTimer) window.clearTimeout(toastTimer)
  toastTimer = window.setTimeout(() => {
    toastVisible.value = false
  }, 3500)
}

function hideToast() {
  toastVisible.value = false
  if (toastTimer) window.clearTimeout(toastTimer)
}

// Confirm dialog
const confirmOpen = ref(false)
const confirmMessage = ref('')
const pendingDeleteId = ref<number | null>(null)

function requestDelete(id: number) {
  pendingDeleteId.value = id
  confirmMessage.value = `Tem certeza que deseja excluir o herói #${id}?`
  confirmOpen.value = true
}

function cancelDelete() {
  confirmOpen.value = false
  pendingDeleteId.value = null
}

async function confirmDelete() {
  if (!pendingDeleteId.value) return
  const id = pendingDeleteId.value
  confirmOpen.value = false
  pendingDeleteId.value = null
  await removeOptimistic(id)
}

async function loadSuperpoderes() {
  // backend tem cache, mas aqui carregamos apenas uma vez
  if (superpoderes.value.length > 0) return
  const p = await api.listSuperpoderes()
  superpoderes.value = p.data || []
}

async function loadHeroes() {
  error.value = ''
  loading.value = true
  try {
    const res = await api.listHerois({ page: page.value, pageSize: pageSize.value, search: search.value })
    heroes.value = res.data?.items || []
    total.value = res.data?.total || 0

    // Se totalPages diminuiu e a pagina atual ficou invalida, ajusta e recarrega
    if (total.value > 0 && page.value > totalPages.value) {
      page.value = totalPages.value
      const res2 = await api.listHerois({ page: page.value, pageSize: pageSize.value, search: search.value })
      heroes.value = res2.data?.items || []
      total.value = res2.data?.total || 0
    }
  } catch (e: any) {
    // Importante: o backend retorna 404 quando a lista está vazia.
    if (e instanceof ApiError && e.status === 404) {
      heroes.value = []
      total.value = 0
    } else {
      const msg = e?.message || 'Falha ao carregar'
      error.value = msg
      showToast(msg, 'error')
      heroes.value = []
      total.value = 0
    }
  } finally {
    loading.value = false
  }
}

async function loadAll() {
  await Promise.all([loadSuperpoderes(), loadHeroes()])
}

function openCreate() {
  resetForm()
  editingId.value = null
  editingRowVersion.value = ''
  showForm.value = true
}

async function openEdit(id: number) {
  error.value = ''
  try {
    await loadSuperpoderes()

    const res = await api.getHeroi(id)
    const h = res.data

    editingId.value = id
    editingRowVersion.value = h.rowVersion

    form.nome = h.nome
    form.nomeHeroi = h.nomeHeroi
    form.dataNascimento = h.dataNascimento.slice(0, 10)
    form.altura = h.altura
    form.peso = h.peso
    form.superpoderIds = h.superpoderes.map((x) => x.id)

    showForm.value = true
  } catch (e: any) {
    const msg = e?.message || 'Falha ao carregar herói'
    error.value = msg
    showToast(msg, 'error')
  }
}

function closeForm() {
  showForm.value = false
}

// Evita scroll do fundo quando o modal estiver aberto
watch(showForm, (isOpen) => {
  document.body.style.overflow = isOpen ? 'hidden' : ''
})

async function save(value: HeroFormState) {
  error.value = ''

  try {
    const basePayload = {
      nome: value.nome,
      nomeHeroi: value.nomeHeroi,
      dataNascimento: new Date(value.dataNascimento).toISOString(),
      altura: value.altura,
      peso: value.peso,
      superpoderIds: value.superpoderIds
    }

    if (editingId.value) {
      const payload = { ...basePayload, rowVersion: editingRowVersion.value }
      const res = await api.updateHeroi(editingId.value, payload)
      showToast(res.message || 'Alteração realizada com sucesso.', 'success')
    } else {
      const res = await api.createHeroi(basePayload)
      showToast(res.message || 'Inclusão realizada com sucesso.', 'success')
    }

    showForm.value = false
    await loadHeroes()
  } catch (e: any) {
    const msg = e?.message || 'Falha ao salvar'
    error.value = msg
    showToast(msg, 'error')
  }
}

async function removeOptimistic(id: number) {
  error.value = ''

  const idx = heroes.value.findIndex((h) => h.id === id)
  if (idx < 0) return

  const snapshot = [...heroes.value]
  const snapshotTotal = total.value

  // Optimistic UI
  heroes.value.splice(idx, 1)
  total.value = Math.max(0, total.value - 1)

  try {
    const res = await api.deleteHeroi(id)
    showToast(res.message || 'Remoção realizada com sucesso.', 'success')

    // Se pagina ficou vazia após exclusao, volta uma pagina
    if (heroes.value.length === 0 && page.value > 1) {
      page.value = page.value - 1
    }

    await loadHeroes()
  } catch (e: any) {
    // rollback
    heroes.value = snapshot
    total.value = snapshotTotal

    const msg = e?.message || 'Falha ao excluir'
    error.value = msg
    showToast(msg, 'error')
  }
}

onMounted(loadAll)
</script>
