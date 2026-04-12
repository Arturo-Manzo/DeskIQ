export enum UserRole {
  Agent = 1,
  Supervisor = 2,
  Admin = 3,
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
