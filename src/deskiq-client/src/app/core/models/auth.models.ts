export enum UserRole {
  Cliente = 1,
  ClienteSupervisor = 2,
  Operador = 3,
  OperadorSupervisor = 4,
  SupervisorGeneral = 5,
  Auditor = 6,
  Administrador = 7,
}

export interface DepartmentDto {
  id: string;
  name: string;
  description: string;
}

export interface AuthUserDto {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  departmentId: string;
  department: DepartmentDto | null;
}

export interface LoginResponse {
  token: string;
  user: AuthUserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}
