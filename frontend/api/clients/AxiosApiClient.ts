import axios, { AxiosInstance, AxiosError } from 'axios';
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

export class AxiosApiClient implements IApiClient {
  private client: AxiosInstance;
  private tokenProvider: () => string | null;

  constructor(baseURL: string, tokenProvider: () => string | null) {
    this.tokenProvider = tokenProvider;
    this.client = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add request interceptor to include auth token
    this.client.interceptors.request.use((config) => {
      const token = this.tokenProvider();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
  }

  private handleError(error: unknown): never {
    if (axios.isAxiosError(error)) {
      const axiosError = error as AxiosError<ProblemDetails>;
      const apiError: ApiError = {
        message: axiosError.response?.data?.detail || axiosError.message,
        status: axiosError.response?.status,
        details: axiosError.response?.data,
      };
      throw apiError;
    }
    throw error;
  }

  // Auth endpoints
  async login(data: LoginDto): Promise<TokenResponseDto> {
    try {
      const response = await this.client.post<TokenResponseDto>('/api/Auth/login', data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async register(data: RegisterDto): Promise<TokenResponseDto> {
    try {
      const response = await this.client.post<TokenResponseDto>('/api/Auth/register', data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async refresh(data: RefreshTokenDto): Promise<TokenResponseDto> {
    try {
      const response = await this.client.post<TokenResponseDto>('/api/Auth/refresh', data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async logout(): Promise<void> {
    try {
      await this.client.post('/api/Auth/logout');
    } catch (error) {
      this.handleError(error);
    }
  }

  // User endpoints
  async getUsers(): Promise<UserDto[]> {
    try {
      const response = await this.client.get<UserDto[]>('/api/Users');
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getCurrentUser(): Promise<UserDto> {
    try {
      const response = await this.client.get<UserDto>('/api/Users/me');
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getUser(id: string): Promise<UserDto> {
    try {
      const response = await this.client.get<UserDto>(`/api/Users/${id}`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getUserByEmail(email: string): Promise<UserDto> {
    try {
      const response = await this.client.get<UserDto>(`/api/Users/email/${email}`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async updateUser(id: string, data: UpdateUserDto): Promise<void> {
    try {
      await this.client.put(`/api/Users/${id}`, data);
    } catch (error) {
      this.handleError(error);
    }
  }

  async deleteUser(id: string): Promise<void> {
    try {
      await this.client.delete(`/api/Users/${id}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  async getUserRoles(id: string): Promise<RoleDto[]> {
    try {
      const response = await this.client.get<RoleDto[]>(`/api/Users/${id}/roles`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async addUserRole(id: string, roleName: string): Promise<void> {
    try {
      await this.client.post(`/api/Users/${id}/roles/${roleName}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  async removeUserRole(id: string, roleName: string): Promise<void> {
    try {
      await this.client.delete(`/api/Users/${id}/roles/${roleName}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  // Role endpoints
  async getRoles(): Promise<RoleDto[]> {
    try {
      const response = await this.client.get<RoleDto[]>('/api/Roles');
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getRole(id: string): Promise<RoleDto> {
    try {
      const response = await this.client.get<RoleDto>(`/api/Roles/${id}`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getRoleByName(name: string): Promise<RoleDto> {
    try {
      const response = await this.client.get<RoleDto>(`/api/Roles/name/${name}`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async createRole(data: CreateRoleDto): Promise<RoleDto> {
    try {
      const response = await this.client.post<RoleDto>('/api/Roles', data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async updateRole(id: string, data: CreateRoleDto): Promise<void> {
    try {
      await this.client.put(`/api/Roles/${id}`, data);
    } catch (error) {
      this.handleError(error);
    }
  }

  async deleteRole(id: string): Promise<void> {
    try {
      await this.client.delete(`/api/Roles/${id}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  async getRoleUsers(name: string): Promise<UserDto[]> {
    try {
      const response = await this.client.get<UserDto[]>(`/api/Roles/${name}/users`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getRolePermissions(roleId: string): Promise<PermissionDto[]> {
    try {
      const response = await this.client.get<PermissionDto[]>(`/api/Roles/${roleId}/permissions`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async addRolePermission(roleId: string, permissionId: string): Promise<void> {
    try {
      await this.client.post(`/api/Roles/${roleId}/permissions?permissionId=${permissionId}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  async removeRolePermission(roleId: string, permissionId: string): Promise<void> {
    try {
      await this.client.delete(`/api/Roles/${roleId}/permissions/${permissionId}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  // Permission endpoints
  async getPermissions(): Promise<PermissionDto[]> {
    try {
      const response = await this.client.get<PermissionDto[]>('/api/Permissions');
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async getPermission(id: string): Promise<PermissionDto> {
    try {
      const response = await this.client.get<PermissionDto>(`/api/Permissions/${id}`);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async createPermission(data: CreatePermissionDto): Promise<PermissionDto> {
    try {
      const response = await this.client.post<PermissionDto>('/api/Permissions', data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async updatePermission(id: string, data: UpdatePermissionDto): Promise<void> {
    try {
      await this.client.put(`/api/Permissions/${id}`, data);
    } catch (error) {
      this.handleError(error);
    }
  }

  async deletePermission(id: string): Promise<void> {
    try {
      await this.client.delete(`/api/Permissions/${id}`);
    } catch (error) {
      this.handleError(error);
    }
  }

  async getPermissionByModuleAction(module: string, action: string): Promise<PermissionDto> {
    try {
      const response = await this.client.get<PermissionDto>(
        `/api/Permissions/module-action?module=${module}&action=${action}`
      );
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async checkPermission(module: string, action: string): Promise<boolean> {
    try {
      const response = await this.client.get<boolean>(
        `/api/Permissions/check?module=${module}&action=${action}`
      );
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }
}
