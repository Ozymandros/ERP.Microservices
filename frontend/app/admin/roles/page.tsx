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
import { RoleDto, PermissionDto } from '@/api/types';

const roleSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  description: z.string().max(500).optional(),
});

type RoleFormData = z.infer<typeof roleSchema>;

export default function RolesPage() {
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isPermissionModalOpen, setIsPermissionModalOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<RoleDto | null>(null);
  const [rolePermissions, setRolePermissions] = useState<PermissionDto[]>([]);
  
  const apiClient = getApiClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<RoleFormData>({
    resolver: zodResolver(roleSchema),
  });

  useEffect(() => {
    loadRoles();
    loadPermissions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadRoles = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getRoles();
      setRoles(data);
      setError(null);
    } catch (err) {
      setError('Failed to load roles');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadPermissions = async () => {
    try {
      const data = await apiClient.getPermissions();
      setPermissions(data);
    } catch (err) {
      console.error('Failed to load permissions:', err);
    }
  };

  const loadRolePermissions = async (roleId: string) => {
    try {
      const data = await apiClient.getRolePermissions(roleId);
      setRolePermissions(data);
    } catch (err) {
      console.error('Failed to load role permissions:', err);
      setRolePermissions([]);
    }
  };

  const openCreateModal = () => {
    reset({ name: '', description: '' });
    setIsCreateModalOpen(true);
  };

  const openEditModal = (role: RoleDto) => {
    setSelectedRole(role);
    reset({
      name: role.name,
      description: role.description,
    });
    setIsEditModalOpen(true);
  };

  const openPermissionModal = async (role: RoleDto) => {
    setSelectedRole(role);
    await loadRolePermissions(role.id);
    setIsPermissionModalOpen(true);
  };

  const handleCreateRole = async (data: RoleFormData) => {
    try {
      await apiClient.createRole(data);
      setIsCreateModalOpen(false);
      loadRoles();
    } catch (err) {
      setError('Failed to create role');
      console.error(err);
    }
  };

  const handleUpdateRole = async (data: RoleFormData) => {
    if (!selectedRole) return;
    
    try {
      await apiClient.updateRole(selectedRole.id, data);
      setIsEditModalOpen(false);
      loadRoles();
    } catch (err) {
      setError('Failed to update role');
      console.error(err);
    }
  };

  const handleDeleteRole = async (roleId: string) => {
    if (!confirm('Are you sure you want to delete this role?')) return;
    
    try {
      await apiClient.deleteRole(roleId);
      loadRoles();
    } catch (err) {
      setError('Failed to delete role');
      console.error(err);
    }
  };

  const handleAddPermission = async (permissionId: string) => {
    if (!selectedRole) return;
    
    try {
      await apiClient.addRolePermission(selectedRole.id, permissionId);
      await loadRolePermissions(selectedRole.id);
    } catch (err) {
      setError('Failed to add permission');
      console.error(err);
    }
  };

  const handleRemovePermission = async (permissionId: string) => {
    if (!selectedRole) return;
    
    try {
      await apiClient.removeRolePermission(selectedRole.id, permissionId);
      await loadRolePermissions(selectedRole.id);
    } catch (err) {
      setError('Failed to remove permission');
      console.error(err);
    }
  };

  const roleHasPermission = (permissionId: string): boolean => {
    return rolePermissions.some((p) => p.id === permissionId);
  };

  return (
    <AdminLayout>
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <h1 className="text-3xl font-bold text-gray-900">Roles</h1>
          <Button onClick={openCreateModal}>Create Role</Button>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        <Card>
          {loading ? (
            <div className="text-center py-8">Loading...</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {roles.map((role) => (
                  <TableRow key={role.id}>
                    <TableCell className="font-medium">{role.name}</TableCell>
                    <TableCell>{role.description || '-'}</TableCell>
                    <TableCell>
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => openEditModal(role)}
                        >
                          Edit
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => openPermissionModal(role)}
                        >
                          Permissions
                        </Button>
                        <Button
                          size="sm"
                          variant="danger"
                          onClick={() => handleDeleteRole(role.id)}
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

        {/* Create Role Modal */}
        <Modal
          isOpen={isCreateModalOpen}
          onClose={() => setIsCreateModalOpen(false)}
          title="Create Role"
          footer={
            <>
              <Button variant="secondary" onClick={() => setIsCreateModalOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleSubmit(handleCreateRole)}>Create</Button>
            </>
          }
        >
          <form className="space-y-4">
            <Input
              label="Name"
              type="text"
              {...register('name')}
              error={errors.name?.message}
            />
            <Input
              label="Description"
              type="text"
              {...register('description')}
              error={errors.description?.message}
            />
          </form>
        </Modal>

        {/* Edit Role Modal */}
        <Modal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          title="Edit Role"
          footer={
            <>
              <Button variant="secondary" onClick={() => setIsEditModalOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleSubmit(handleUpdateRole)}>Save</Button>
            </>
          }
        >
          <form className="space-y-4">
            <Input
              label="Name"
              type="text"
              {...register('name')}
              error={errors.name?.message}
            />
            <Input
              label="Description"
              type="text"
              {...register('description')}
              error={errors.description?.message}
            />
          </form>
        </Modal>

        {/* Manage Permissions Modal */}
        <Modal
          isOpen={isPermissionModalOpen}
          onClose={() => setIsPermissionModalOpen(false)}
          title={`Manage Permissions for ${selectedRole?.name}`}
        >
          <div className="space-y-2 max-h-96 overflow-y-auto">
            {permissions.map((permission) => {
              const hasPermission = roleHasPermission(permission.id);
              return (
                <div
                  key={permission.id}
                  className="flex items-center justify-between p-3 border rounded"
                >
                  <div>
                    <div className="font-medium">
                      {permission.module} - {permission.action}
                    </div>
                    <div className="text-sm text-gray-500">{permission.description}</div>
                  </div>
                  {hasPermission ? (
                    <Button
                      size="sm"
                      variant="danger"
                      onClick={() => handleRemovePermission(permission.id)}
                    >
                      Remove
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      variant="primary"
                      onClick={() => handleAddPermission(permission.id)}
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
