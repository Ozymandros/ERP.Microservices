'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { UserDto, LoginDto, RegisterDto, ApiError } from '@/api/types';
import { getApiClient } from '@/lib/apiClientFactory';
import { config } from '@/lib/config';

interface AuthContextType {
  user: UserDto | null;
  loading: boolean;
  error: string | null;
  login: (credentials: LoginDto) => Promise<void>;
  register: (data: RegisterDto) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  hasPermission: (module: string, action: string) => boolean;
  hasRole: (roleName: string) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();
  const apiClient = getApiClient();

  useEffect(() => {
    // Load user from localStorage on mount
    const loadUser = async () => {
      const storedUser = localStorage.getItem(config.auth.userKey);
      const storedToken = localStorage.getItem(config.auth.tokenKey);
      
      if (storedUser && storedToken) {
        try {
          setUser(JSON.parse(storedUser));
          // Optionally fetch current user to validate token
          const currentUser = await apiClient.getCurrentUser();
          setUser(currentUser);
          localStorage.setItem(config.auth.userKey, JSON.stringify(currentUser));
        } catch {
          // Token invalid, clear storage
          localStorage.removeItem(config.auth.tokenKey);
          localStorage.removeItem(config.auth.refreshTokenKey);
          localStorage.removeItem(config.auth.userKey);
          setUser(null);
        }
      }
      setLoading(false);
    };

    loadUser();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const login = async (credentials: LoginDto) => {
    try {
      setError(null);
      setLoading(true);
      const response = await apiClient.login(credentials);
      
      localStorage.setItem(config.auth.tokenKey, response.accessToken);
      localStorage.setItem(config.auth.refreshTokenKey, response.refreshToken);
      localStorage.setItem(config.auth.userKey, JSON.stringify(response.user));
      
      setUser(response.user);
      router.push('/admin/users');
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || 'Login failed');
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const register = async (data: RegisterDto) => {
    try {
      setError(null);
      setLoading(true);
      const response = await apiClient.register(data);
      
      localStorage.setItem(config.auth.tokenKey, response.accessToken);
      localStorage.setItem(config.auth.refreshTokenKey, response.refreshToken);
      localStorage.setItem(config.auth.userKey, JSON.stringify(response.user));
      
      setUser(response.user);
      router.push('/admin/users');
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || 'Registration failed');
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const logout = async () => {
    try {
      await apiClient.logout();
    } catch (err) {
      console.error('Logout error:', err);
    } finally {
      localStorage.removeItem(config.auth.tokenKey);
      localStorage.removeItem(config.auth.refreshTokenKey);
      localStorage.removeItem(config.auth.userKey);
      setUser(null);
      router.push('/auth/login');
    }
  };

  const refreshToken = async () => {
    const storedAccessToken = localStorage.getItem(config.auth.tokenKey);
    const storedRefreshToken = localStorage.getItem(config.auth.refreshTokenKey);
    
    if (!storedAccessToken || !storedRefreshToken) {
      throw new Error('No refresh token available');
    }

    try {
      const response = await apiClient.refresh({
        accessToken: storedAccessToken,
        refreshToken: storedRefreshToken,
      });
      
      localStorage.setItem(config.auth.tokenKey, response.accessToken);
      localStorage.setItem(config.auth.refreshTokenKey, response.refreshToken);
      localStorage.setItem(config.auth.userKey, JSON.stringify(response.user));
      
      setUser(response.user);
    } catch (err) {
      // Refresh failed, logout user
      await logout();
      throw err;
    }
  };

  const hasPermission = (module: string, action: string): boolean => {
    if (!user) return false;
    if (user.isAdmin) return true;
    
    return user.permissions?.some(
      (p) => p.module === module && p.action === action
    ) || false;
  };

  const hasRole = (roleName: string): boolean => {
    if (!user) return false;
    return user.roles?.some((r) => r.name === roleName) || false;
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        error,
        login,
        register,
        logout,
        refreshToken,
        hasPermission,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
