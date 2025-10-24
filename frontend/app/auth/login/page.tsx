'use client';

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card } from '@/components/ui/Card';
import Link from 'next/link';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const { login, error: authError } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    try {
      await login(data);
    } catch (error) {
      console.error('Login error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4">
      <Card className="w-full max-w-md">
        <div className="text-center mb-6">
          <h1 className="text-2xl font-bold text-gray-900">ERP System</h1>
          <p className="text-gray-600 mt-2">Sign in to your account</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Email"
            type="email"
            {...register('email')}
            error={errors.email?.message}
            disabled={isLoading}
            autoComplete="email"
          />
          
          <Input
            label="Password"
            type="password"
            {...register('password')}
            error={errors.password?.message}
            disabled={isLoading}
            autoComplete="current-password"
          />

          {authError && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              {authError}
            </div>
          )}

          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? 'Signing in...' : 'Sign in'}
          </Button>
        </form>

        <div className="mt-4 text-center text-sm">
          <span className="text-gray-600">Don&apos;t have an account? </span>
          <Link href="/auth/register" className="text-blue-600 hover:underline">
            Register
          </Link>
        </div>
      </Card>
    </div>
  );
}
