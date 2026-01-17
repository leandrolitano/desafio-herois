<template>
  <section class="card">
    <h2>Lista</h2>

    <div v-if="loading">Carregando...</div>

    <div v-else>
      <table class="table" v-if="heroes.length">
        <thead>
          <tr>
            <th>Id</th>
            <th>Nome</th>
            <th>Nome de Herói</th>
            <th>Superpoderes</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="h in heroes" :key="h.id">
            <td>{{ h.id }}</td>
            <td>{{ h.nome }}</td>
            <td>{{ h.nomeHeroi }}</td>
            <td>
              <div class="badges">
                <span v-for="p in h.superpoderes" :key="p.id" class="badge">
                  {{ p.nome }}
                </span>
              </div>
            </td>
            <td class="actions-cell">
              <button @click="$emit('edit', h.id)">Editar</button>
              <button @click="$emit('delete', h.id)">Excluir</button>
            </td>
          </tr>
        </tbody>
      </table>

      <div v-else>Nenhum herói ainda.</div>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { HeroiDto } from '../../types/models'

defineProps<{
  heroes: HeroiDto[]
  loading: boolean
}>()

defineEmits<{
  (e: 'edit', id: number): void
  (e: 'delete', id: number): void
}>()
</script>

<style scoped>
.actions-cell {
  display: flex;
  gap: 8px;
}
</style>
