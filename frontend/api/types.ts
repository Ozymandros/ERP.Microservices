// Generated from OpenAPI specification
export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  email: string;
  username: string;
  password: string;
  passwordConfirm: string;
  firstName?: string;
  lastName?: string;
}

export interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
  user: UserDto;
}

export interface RefreshTokenDto {
  accessToken?: string;
  refreshToken?: string;
}

export interface UserDto {
  id: string;
  email?: string;
  username?: string;
  firstName?: string;
  lastName?: string;
  emailConfirmed: boolean;
  isExternalLogin: boolean;
  externalProvider?: string;
  roles?: RoleDto[];
  permissions?: PermissionDto[];
  isAdmin: boolean;
}

export interface UpdateUserDto {
  email?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
}

export interface RoleDto {
  id: string;
  name?: string;
  description?: string;
}

export interface CreateRoleDto {
  name?: string;
  description?: string;
}

export interface PermissionDto {
  id: string;
  module?: string;
  action?: string;
  description?: string;
}

export interface CreatePermissionDto {
  module?: string;
  action?: string;
  description?: string;
}

export interface UpdatePermissionDto {
  module?: string;
  action?: string;
  description?: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}

export interface ApiError {
  message: string;
  status?: number;
  details?: ProblemDetails;
}
