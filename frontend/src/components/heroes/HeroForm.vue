<template>
  <div class="modal-backdrop" @click.self="emit('cancel')">
    <section class="modal modal--wide" role="dialog" aria-modal="true" @keydown.esc.prevent="emit('cancel')" tabindex="-1">
      <header class="modal__header hero-modal__header">
        <div class="hero-modal__title">
          <h2 class="modal__title">{{ title }}</h2>
          <span class="pill" :class="mode === 'edit' ? 'pill--edit' : 'pill--create'">
            {{ mode === 'edit' ? 'Modo edição' : 'Modo inclusão' }}
          </span>
        </div>

        <button type="button" class="icon-btn" aria-label="Fechar" @click="emit('cancel')">
          ×
        </button>
      </header>

      <div class="modal__body">
        <form @submit.prevent="submit">
          <div class="grid">
        <label>
          Nome
          <input v-model="nome" v-bind="nomeAttrs" autocomplete="off" />
          <small v-if="errors.nome" class="field-error">{{ errors.nome }}</small>
        </label>

        <label>
          Nome de Herói
          <input v-model="nomeHeroi" v-bind="nomeHeroiAttrs" autocomplete="off" />
          <small v-if="errors.nomeHeroi" class="field-error">{{ errors.nomeHeroi }}</small>
        </label>

        <label>
          Data de Nascimento
          <input type="date" v-model="dataNascimento" v-bind="dataNascimentoAttrs" />
          <small v-if="errors.dataNascimento" class="field-error">{{ errors.dataNascimento }}</small>
        </label>

        <label>
          Altura
          <input type="number" step="0.01" v-model="altura" v-bind="alturaAttrs" />
          <small v-if="errors.altura" class="field-error">{{ errors.altura }}</small>
        </label>

        <label>
          Peso
          <input type="number" step="0.01" v-model="peso" v-bind="pesoAttrs" />
          <small v-if="errors.peso" class="field-error">{{ errors.peso }}</small>
        </label>

        <!--
          IMPORTANTE:
          Nao envolvemos o multi-select em <label>.
          Quando o dropdown abre, inputs (checkbox) passam a existir dentro do label e o navegador pode
          "ativar" automaticamente um deles no mesmo clique, causando desmarcacao involuntaria.
        -->
        <div class="field">
          <div class="field__label">Superpoderes</div>
          <SuperpoderMultiSelect
            v-model="superpoderIds"
            :options="superpoderes"
            placeholder="Pesquise e selecione superpoderes"
          />
          <small v-if="errors.superpoderIds" class="field-error">{{ errors.superpoderIds }}</small>
        </div>
      </div>

          <div class="actions">
            <button type="submit" :disabled="isSubmitting">Salvar</button>
            <button type="button" @click="emit('cancel')" :disabled="isSubmitting">Cancelar</button>
          </div>
        </form>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import SuperpoderMultiSelect from './SuperpoderMultiSelect.vue'
import type { HeroFormState, SuperpoderDto } from '../../types/models'

const props = defineProps<{
  title: string
  mode: 'create' | 'edit'
  initial: HeroFormState
  superpoderes: SuperpoderDto[]
}>()

const emit = defineEmits<{
  (e: 'save', value: HeroFormState): void
  (e: 'cancel'): void
}>()

const schema = z.object({
  nome: z.string().trim().min(1, 'Informe o nome').max(120, 'Nome muito longo'),
  nomeHeroi: z.string().trim().min(1, 'Informe o nome de herói').max(120, 'Nome de herói muito longo'),
  dataNascimento: z
    .string()
    .min(1, 'Informe a data de nascimento')
    .refine((v) => !Number.isNaN(Date.parse(v)), 'Data inválida'),
  altura: z.coerce.number().positive('Altura deve ser maior que 0'),
  peso: z.coerce.number().positive('Peso deve ser maior que 0'),
  superpoderIds: z.array(z.coerce.number()).min(1, 'Selecione ao menos 1 superpoder')
})

const {
  errors,
  defineField,
  handleSubmit,
  setValues,
  isSubmitting
} = useForm<HeroFormState>({
  validationSchema: toTypedSchema(schema),
  initialValues: props.initial
})

const [nome, nomeAttrs] = defineField('nome')
const [nomeHeroi, nomeHeroiAttrs] = defineField('nomeHeroi')
const [dataNascimento, dataNascimentoAttrs] = defineField('dataNascimento')
const [altura, alturaAttrs] = defineField('altura')
const [peso, pesoAttrs] = defineField('peso')
const [superpoderIds] = defineField('superpoderIds')

watch(
  () => props.initial,
  (v) => {
    setValues({
      nome: v.nome,
      nomeHeroi: v.nomeHeroi,
      dataNascimento: v.dataNascimento,
      altura: v.altura,
      peso: v.peso,
      superpoderIds: [...v.superpoderIds]
    })
  },
  { deep: true }
)

const submit = handleSubmit((values) => {
  // values ja vem validado e com numeros coerced
  emit('save', {
    nome: values.nome,
    nomeHeroi: values.nomeHeroi,
    dataNascimento: values.dataNascimento,
    altura: values.altura,
    peso: values.peso,
    superpoderIds: [...values.superpoderIds]
  })
})
</script>
