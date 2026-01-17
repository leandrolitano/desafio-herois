export interface ApiResult<T> {
  success: boolean;
  statusCode: number;
  message: string;
  data?: T;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
  [key: string]: any;
}

// Estado usado pelo formulario (valores editaveis)
export interface HeroFormState {
  nome: string;
  nomeHeroi: string;
  dataNascimento: string; // yyyy-MM-dd
  altura: number;
  peso: number;
  superpoderIds: number[];
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface SuperpoderDto {
  id: number;
  nome: string;
  descricao: string;
}

export interface HeroiDto {
  id: number;
  nome: string;
  nomeHeroi: string;
  dataNascimento: string;
  altura: number;
  peso: number;
  rowVersion: string;
  superpoderes: SuperpoderDto[];
}

export interface CreateHeroRequest {
  nome: string;
  nomeHeroi: string;
  dataNascimento: string;
  altura: number;
  peso: number;
  superpoderIds: number[];
}

export interface UpdateHeroRequest extends CreateHeroRequest {
  rowVersion: string;
}
