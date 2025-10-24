import { IApiClient } from './IApiClient';
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
  ApiError,
  ProblemDetails,
} from '../types';

/**
 * Dapr HTTP API Client
 * Uses Dapr sidecar to invoke backend services
 */
export class DaprApiClient implements IApiClient {
  private daprUrl: string;
  private serviceName: string;
  private tokenProvider: () => string | null;

  constructor(daprUrl: string, serviceName: string, tokenProvider: () => string | null) {
    this.daprUrl = daprUrl;
    this.serviceName = serviceName;
    this.tokenProvider = tokenProvider;
  }

  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const token = this.tokenProvider();
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const url = `${this.daprUrl}/v1.0/invoke/${this.serviceName}/method${path}`;
    
    try {
      const response = await fetch(url, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({})) as ProblemDetails;
        const apiError: ApiError = {
          message: errorData.detail || response.statusText,
          status: response.status,
          details: errorData,
        };
        throw apiError;
      }

      // Handle 204 No Content
      if (response.status === 204) {
        return undefined as T;
      }

      return await response.json() as T;
    } catch (error) {
      if ((error as ApiError).status) {
        throw error;
      }
      throw {
        message: (error as Error).message || 'Network error',
        status: 0,
      } as ApiError;
    }
  }

  // Auth endpoints
  async login(data: LoginDto): Promise<TokenResponseDto> {
    return this.request<TokenResponseDto>('POST', '/api/Auth/login', data);
  }

  async register(data: RegisterDto): Promise<TokenResponseDto> {
    return this.request<TokenResponseDto>('POST', '/api/Auth/register', data);
  }

  async refresh(data: RefreshTokenDto): Promise<TokenResponseDto> {
    return this.request<TokenResponseDto>('POST', '/api/Auth/refresh', data);
  }

  async logout(): Promise<void> {
    return this.request<void>('POST', '/api/Auth/logout');
  }

  // User endpoints
  async getUsers(): Promise<UserDto[]> {
    return this.request<UserDto[]>('GET', '/api/Users');
  }

  async getCurrentUser(): Promise<UserDto> {
    return this.request<UserDto>('GET', '/api/Users/me');
  }

  async getUser(id: string): Promise<UserDto> {
    return this.request<UserDto>('GET', `/api/Users/${id}`);
  }

  async getUserByEmail(email: string): Promise<UserDto> {
    return this.request<UserDto>('GET', `/api/Users/email/${email}`);
  }

  async updateUser(id: string, data: UpdateUserDto): Promise<void> {
    return this.request<void>('PUT', `/api/Users/${id}`, data);
  }

  async deleteUser(id: string): Promise<void> {
    return this.request<void>('DELETE', `/api/Users/${id}`);
  }

  async getUserRoles(id: string): Promise<RoleDto[]> {
    return this.request<RoleDto[]>('GET', `/api/Users/${id}/roles`);
  }

  async addUserRole(id: string, roleName: string): Promise<void> {
    return this.request<void>('POST', `/api/Users/${id}/roles/${roleName}`);
  }

  async removeUserRole(id: string, roleName: string): Promise<void> {
    return this.request<void>('DELETE', `/api/Users/${id}/roles/${roleName}`);
  }

  // Role endpoints
  async getRoles(): Promise<RoleDto[]> {
    return this.request<RoleDto[]>('GET', '/api/Roles');
  }

  async getRole(id: string): Promise<RoleDto> {
    return this.request<RoleDto>('GET', `/api/Roles/${id}`);
  }

  async getRoleByName(name: string): Promise<RoleDto> {
    return this.request<RoleDto>('GET', `/api/Roles/name/${name}`);
  }

  async createRole(data: CreateRoleDto): Promise<RoleDto> {
    return this.request<RoleDto>('POST', '/api/Roles', data);
  }

  async updateRole(id: string, data: CreateRoleDto): Promise<void> {
    return this.request<void>('PUT', `/api/Roles/${id}`, data);
  }

  async deleteRole(id: string): Promise<void> {
    return this.request<void>('DELETE', `/api/Roles/${id}`);
  }

  async getRoleUsers(name: string): Promise<UserDto[]> {
    return this.request<UserDto[]>('GET', `/api/Roles/${name}/users`);
  }

  async getRolePermissions(roleId: string): Promise<PermissionDto[]> {
    return this.request<PermissionDto[]>('GET', `/api/Roles/${roleId}/permissions`);
  }

  async addRolePermission(roleId: string, permissionId: string): Promise<void> {
    return this.request<void>('POST', `/api/Roles/${roleId}/permissions?permissionId=${permissionId}`);
  }

  async removeRolePermission(roleId: string, permissionId: string): Promise<void> {
    return this.request<void>('DELETE', `/api/Roles/${roleId}/permissions/${permissionId}`);
  }

  // Permission endpoints
  async getPermissions(): Promise<PermissionDto[]> {
    return this.request<PermissionDto[]>('GET', '/api/Permissions');
  }

  async getPermission(id: string): Promise<PermissionDto> {
    return this.request<PermissionDto>('GET', `/api/Permissions/${id}`);
  }

  async createPermission(data: CreatePermissionDto): Promise<PermissionDto> {
    return this.request<PermissionDto>('POST', '/api/Permissions', data);
  }

  async updatePermission(id: string, data: UpdatePermissionDto): Promise<void> {
    return this.request<void>('PUT', `/api/Permissions/${id}`, data);
  }

  async deletePermission(id: string): Promise<void> {
    return this.request<void>('DELETE', `/api/Permissions/${id}`);
  }

  async getPermissionByModuleAction(module: string, action: string): Promise<PermissionDto> {
    return this.request<PermissionDto>(
      'GET',
      `/api/Permissions/module-action?module=${module}&action=${action}`
    );
  }

  async checkPermission(module: string, action: string): Promise<boolean> {
    return this.request<boolean>(
      'GET',
      `/api/Permissions/check?module=${module}&action=${action}`
    );
  }
}
