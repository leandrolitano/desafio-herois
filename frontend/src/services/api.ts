import type {
  ApiResult,
  CreateHeroRequest,
  HeroiDto,
  PagedResult,
  ProblemDetails,
  SuperpoderDto,
  UpdateHeroRequest
} from '../types/models'

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function http<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const res = await fetch(input, {
    headers: { 'Content-Type': 'application/json', ...(init?.headers || {}) },
    ...init
  })

  const text = await res.text()
  const json = text ? JSON.parse(text) : null

  if (!res.ok) {
    const problem: ProblemDetails | undefined = json && (json.title || json.detail || json.errors)
      ? (json as ProblemDetails)
      : undefined

    // Mensagem amigavel
    let msg =
      problem?.detail ||
      problem?.title ||
      json?.message ||
      `Erro HTTP ${res.status}`

    // Se vier ValidationProblemDetails, tenta usar o 1o erro do dicionario
    const firstValidationError = problem?.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined
    if (firstValidationError) msg = firstValidationError

    throw new ApiError(msg, res.status, problem)
  }

  return json as T
}

function buildQuery(params: Record<string, any>): string {
  const usp = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null) return
    const s = String(v)
    if (s.trim() === '') return
    usp.set(k, s)
  })
  const qs = usp.toString()
  return qs ? `?${qs}` : ''
}

export const api = {
  async listHerois(opts?: { page?: number; pageSize?: number; search?: string }): Promise<ApiResult<PagedResult<HeroiDto>>> {
    const qs = buildQuery({ page: opts?.page ?? 1, pageSize: opts?.pageSize ?? 20, search: opts?.search })
    return http<ApiResult<PagedResult<HeroiDto>>>(`/api/herois${qs}`)
  },

  async getHeroi(id: number): Promise<ApiResult<HeroiDto>> {
    return http<ApiResult<HeroiDto>>(`/api/herois/${id}`)
  },

  async createHeroi(payload: CreateHeroRequest): Promise<ApiResult<HeroiDto>> {
    return http<ApiResult<HeroiDto>>('/api/herois', { method: 'POST', body: JSON.stringify(payload) })
  },

  async updateHeroi(id: number, payload: UpdateHeroRequest): Promise<ApiResult<HeroiDto>> {
    return http<ApiResult<HeroiDto>>(`/api/herois/${id}`, { method: 'PUT', body: JSON.stringify(payload) })
  },

  async deleteHeroi(id: number): Promise<ApiResult<string>> {
    return http<ApiResult<string>>(`/api/herois/${id}`, { method: 'DELETE' })
  },

  async listSuperpoderes(): Promise<ApiResult<SuperpoderDto[]>> {
    return http<ApiResult<SuperpoderDto[]>>('/api/superpoderes')
  }
}
