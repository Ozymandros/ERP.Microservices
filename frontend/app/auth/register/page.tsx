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

const registerSchema = z.object({
  email: z.string().email('Invalid email address'),
  username: z.string().min(3, 'Username must be at least 3 characters').max(100),
  password: z.string().min(6, 'Password must be at least 6 characters').max(100),
  passwordConfirm: z.string(),
  firstName: z.string().max(100).optional(),
  lastName: z.string().max(100).optional(),
}).refine((data) => data.password === data.passwordConfirm, {
  message: "Passwords don't match",
  path: ["passwordConfirm"],
});

type RegisterFormData = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const { register: registerUser, error: authError } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    setIsLoading(true);
    try {
      await registerUser(data);
    } catch (error) {
      console.error('Registration error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4 py-8">
      <Card className="w-full max-w-md">
        <div className="text-center mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Create Account</h1>
          <p className="text-gray-600 mt-2">Register for ERP System</p>
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
            label="Username"
            type="text"
            {...register('username')}
            error={errors.username?.message}
            disabled={isLoading}
            autoComplete="username"
          />
          
          <Input
            label="First Name (Optional)"
            type="text"
            {...register('firstName')}
            error={errors.firstName?.message}
            disabled={isLoading}
            autoComplete="given-name"
          />
          
          <Input
            label="Last Name (Optional)"
            type="text"
            {...register('lastName')}
            error={errors.lastName?.message}
            disabled={isLoading}
            autoComplete="family-name"
          />
          
          <Input
            label="Password"
            type="password"
            {...register('password')}
            error={errors.password?.message}
            disabled={isLoading}
            autoComplete="new-password"
          />
          
          <Input
            label="Confirm Password"
            type="password"
            {...register('passwordConfirm')}
            error={errors.passwordConfirm?.message}
            disabled={isLoading}
            autoComplete="new-password"
          />

          {authError && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              {authError}
            </div>
          )}

          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? 'Creating account...' : 'Register'}
          </Button>
        </form>

        <div className="mt-4 text-center text-sm">
          <span className="text-gray-600">Already have an account? </span>
          <Link href="/auth/login" className="text-blue-600 hover:underline">
            Sign in
          </Link>
        </div>
      </Card>
    </div>
  );
}
