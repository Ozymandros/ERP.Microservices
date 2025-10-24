import {
  LoginDto,
  RegisterDto,
  TokenResponseDto,
  RefreshTokenDto,
  UserDto,
  UpdateUserDto,
  RoleDto,
  CreateRoleDto,
  PermissionDto,
  CreatePermissionDto,
  UpdatePermissionDto,
} from '../types';

/**
 * Generic API Client interface that can be implemented using Axios or Dapr HTTP
 */
export interface IApiClient {
  // Auth endpoints
  login(data: LoginDto): Promise<TokenResponseDto>;
  register(data: RegisterDto): Promise<TokenResponseDto>;
  refresh(data: RefreshTokenDto): Promise<TokenResponseDto>;
  logout(): Promise<void>;

  // User endpoints
  getUsers(): Promise<UserDto[]>;
  getCurrentUser(): Promise<UserDto>;
  getUser(id: string): Promise<UserDto>;
  getUserByEmail(email: string): Promise<UserDto>;
  updateUser(id: string, data: UpdateUserDto): Promise<void>;
  deleteUser(id: string): Promise<void>;
  getUserRoles(id: string): Promise<RoleDto[]>;
  addUserRole(id: string, roleName: string): Promise<void>;
  removeUserRole(id: string, roleName: string): Promise<void>;

  // Role endpoints
  getRoles(): Promise<RoleDto[]>;
  getRole(id: string): Promise<RoleDto>;
  getRoleByName(name: string): Promise<RoleDto>;
  createRole(data: CreateRoleDto): Promise<RoleDto>;
  updateRole(id: string, data: CreateRoleDto): Promise<void>;
  deleteRole(id: string): Promise<void>;
  getRoleUsers(name: string): Promise<UserDto[]>;
  getRolePermissions(roleId: string): Promise<PermissionDto[]>;
  addRolePermission(roleId: string, permissionId: string): Promise<void>;
  removeRolePermission(roleId: string, permissionId: string): Promise<void>;

  // Permission endpoints
  getPermissions(): Promise<PermissionDto[]>;
  getPermission(id: string): Promise<PermissionDto>;
  createPermission(data: CreatePermissionDto): Promise<PermissionDto>;
  updatePermission(id: string, data: UpdatePermissionDto): Promise<void>;
  deletePermission(id: string): Promise<void>;
  getPermissionByModuleAction(module: string, action: string): Promise<PermissionDto>;
  checkPermission(module: string, action: string): Promise<boolean>;
}
