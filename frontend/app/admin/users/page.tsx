'use client';

import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AdminLayout } from '@/components/layouts/AdminLayout';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card } from '@/components/ui/Card';
import { Modal } from '@/components/ui/Modal';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/Table';
import { getApiClient } from '@/lib/apiClientFactory';
import { UserDto, RoleDto } from '@/api/types';

const updateUserSchema = z.object({
  email: z.string().email('Invalid email address').optional(),
  firstName: z.string().max(100).optional(),
  lastName: z.string().max(100).optional(),
  phoneNumber: z.string().optional(),
});

type UpdateUserFormData = z.infer<typeof updateUserSchema>;

export default function UsersPage() {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  
  const apiClient = getApiClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UpdateUserFormData>({
    resolver: zodResolver(updateUserSchema),
  });

  useEffect(() => {
    loadUsers();
    loadRoles();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getUsers();
      setUsers(data);
      setError(null);
    } catch (err) {
      setError('Failed to load users');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadRoles = async () => {
    try {
      const data = await apiClient.getRoles();
      setRoles(data);
    } catch (err) {
      console.error('Failed to load roles:', err);
    }
  };

  const openEditModal = (user: UserDto) => {
    setSelectedUser(user);
    reset({
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      phoneNumber: '',
    });
    setIsEditModalOpen(true);
  };

  const handleUpdateUser = async (data: UpdateUserFormData) => {
    if (!selectedUser) return;
    
    try {
      await apiClient.updateUser(selectedUser.id, data);
      setIsEditModalOpen(false);
      loadUsers();
    } catch (err) {
      setError('Failed to update user');
      console.error(err);
    }
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Are you sure you want to delete this user?')) return;
    
    try {
      setIsDeleting(true);
      await apiClient.deleteUser(userId);
      loadUsers();
    } catch (err) {
      setError('Failed to delete user');
      console.error(err);
    } finally {
      setIsDeleting(false);
    }
  };

  const openRoleModal = (user: UserDto) => {
    setSelectedUser(user);
    setIsRoleModalOpen(true);
  };

  const handleAddRole = async (roleName: string) => {
    if (!selectedUser) return;
    
    try {
      await apiClient.addUserRole(selectedUser.id, roleName);
      loadUsers();
    } catch (err) {
      setError('Failed to add role');
      console.error(err);
    }
  };

  const handleRemoveRole = async (roleName: string) => {
    if (!selectedUser) return;
    
    try {
      await apiClient.removeUserRole(selectedUser.id, roleName);
      loadUsers();
    } catch (err) {
      setError('Failed to remove role');
      console.error(err);
    }
  };

  const filteredUsers = users.filter((user) => {
    const search = searchTerm.toLowerCase();
    return (
      user.email?.toLowerCase().includes(search) ||
      user.username?.toLowerCase().includes(search) ||
      user.firstName?.toLowerCase().includes(search) ||
      user.lastName?.toLowerCase().includes(search)
    );
  });

  const userHasRole = (user: UserDto, roleName: string): boolean => {
    return user.roles?.some((r) => r.name === roleName) || false;
  };

  return (
    <AdminLayout>
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <h1 className="text-3xl font-bold text-gray-900">Users</h1>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        <Card>
          <div className="mb-4">
            <Input
              placeholder="Search users..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          {loading ? (
            <div className="text-center py-8">Loading...</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Email</TableHead>
                  <TableHead>Username</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Roles</TableHead>
                  <TableHead>Admin</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredUsers.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>{user.username}</TableCell>
                    <TableCell>
                      {user.firstName && user.lastName
                        ? `${user.firstName} ${user.lastName}`
                        : '-'}
                    </TableCell>
                    <TableCell>
                      {user.roles && user.roles.length > 0 ? (
                        <div className="flex flex-wrap gap-1">
                          {user.roles.map((role) => (
                            <span
                              key={role.id}
                              className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                            >
                              {role.name}
                            </span>
                          ))}
                        </div>
                      ) : (
                        '-'
                      )}
                    </TableCell>
                    <TableCell>
                      {user.isAdmin ? (
                        <span className="text-green-600 font-medium">Yes</span>
                      ) : (
                        <span className="text-gray-500">No</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => openEditModal(user)}
                        >
                          Edit
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => openRoleModal(user)}
                        >
                          Roles
                        </Button>
                        <Button
                          size="sm"
                          variant="danger"
                          onClick={() => handleDeleteUser(user.id)}
                          disabled={isDeleting}
                        >
                          Delete
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </Card>

        {/* Edit User Modal */}
        <Modal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          title="Edit User"
          footer={
            <>
              <Button variant="secondary" onClick={() => setIsEditModalOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleSubmit(handleUpdateUser)}>Save</Button>
            </>
          }
        >
          <form className="space-y-4">
            <Input
              label="Email"
              type="email"
              {...register('email')}
              error={errors.email?.message}
            />
            <Input
              label="First Name"
              type="text"
              {...register('firstName')}
              error={errors.firstName?.message}
            />
            <Input
              label="Last Name"
              type="text"
              {...register('lastName')}
              error={errors.lastName?.message}
            />
            <Input
              label="Phone Number"
              type="text"
              {...register('phoneNumber')}
              error={errors.phoneNumber?.message}
            />
          </form>
        </Modal>

        {/* Assign Roles Modal */}
        <Modal
          isOpen={isRoleModalOpen}
          onClose={() => setIsRoleModalOpen(false)}
          title={`Manage Roles for ${selectedUser?.email}`}
        >
          <div className="space-y-2">
            {roles.map((role) => {
              const hasRole = selectedUser ? userHasRole(selectedUser, role.name || '') : false;
              return (
                <div
                  key={role.id}
                  className="flex items-center justify-between p-3 border rounded"
                >
                  <div>
                    <div className="font-medium">{role.name}</div>
                    <div className="text-sm text-gray-500">{role.description}</div>
                  </div>
                  {hasRole ? (
                    <Button
                      size="sm"
                      variant="danger"
                      onClick={() => handleRemoveRole(role.name || '')}
                    >
                      Remove
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      variant="primary"
                      onClick={() => handleAddRole(role.name || '')}
                    >
                      Add
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
        </Modal>
      </div>
    </AdminLayout>
  );
}
