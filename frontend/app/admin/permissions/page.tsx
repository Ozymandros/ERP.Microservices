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
import { PermissionDto } from '@/api/types';

const permissionSchema = z.object({
  module: z.string().min(1, 'Module is required').max(100),
  action: z.string().min(1, 'Action is required').max(100),
  description: z.string().max(500).optional(),
});

type PermissionFormData = z.infer<typeof permissionSchema>;

export default function PermissionsPage() {
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [selectedPermission, setSelectedPermission] = useState<PermissionDto | null>(null);
  
  const apiClient = getApiClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PermissionFormData>({
    resolver: zodResolver(permissionSchema),
  });

  useEffect(() => {
    loadPermissions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadPermissions = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getPermissions();
      setPermissions(data);
      setError(null);
    } catch (err) {
      setError('Failed to load permissions');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    reset({ module: '', action: '', description: '' });
    setIsCreateModalOpen(true);
  };

  const openEditModal = (permission: PermissionDto) => {
    setSelectedPermission(permission);
    reset({
      module: permission.module,
      action: permission.action,
      description: permission.description,
    });
    setIsEditModalOpen(true);
  };

  const handleCreatePermission = async (data: PermissionFormData) => {
    try {
      await apiClient.createPermission(data);
      setIsCreateModalOpen(false);
      loadPermissions();
    } catch (err) {
      setError('Failed to create permission');
      console.error(err);
    }
  };

  const handleUpdatePermission = async (data: PermissionFormData) => {
    if (!selectedPermission) return;
    
    try {
      await apiClient.updatePermission(selectedPermission.id, data);
      setIsEditModalOpen(false);
      loadPermissions();
    } catch (err) {
      setError('Failed to update permission');
      console.error(err);
    }
  };

  const handleDeletePermission = async (permissionId: string) => {
    if (!confirm('Are you sure you want to delete this permission?')) return;
    
    try {
      await apiClient.deletePermission(permissionId);
      loadPermissions();
    } catch (err) {
      setError('Failed to delete permission');
      console.error(err);
    }
  };

  const filteredPermissions = permissions.filter((permission) => {
    const search = searchTerm.toLowerCase();
    return (
      permission.module?.toLowerCase().includes(search) ||
      permission.action?.toLowerCase().includes(search) ||
      permission.description?.toLowerCase().includes(search)
    );
  });

  // Group permissions by module
  const groupedPermissions = filteredPermissions.reduce((acc, permission) => {
    const moduleName = permission.module || 'Unknown';
    if (!acc[moduleName]) {
      acc[moduleName] = [];
    }
    acc[moduleName].push(permission);
    return acc;
  }, {} as Record<string, PermissionDto[]>);

  return (
    <AdminLayout>
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <h1 className="text-3xl font-bold text-gray-900">Permissions</h1>
          <Button onClick={openCreateModal}>Create Permission</Button>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        <Card>
          <div className="mb-4">
            <Input
              placeholder="Search permissions..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          {loading ? (
            <div className="text-center py-8">Loading...</div>
          ) : (
            <div className="space-y-6">
              {Object.keys(groupedPermissions).map((module) => (
                <div key={module}>
                  <h3 className="text-lg font-semibold text-gray-900 mb-3">
                    {module}
                  </h3>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Action</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead>Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {groupedPermissions[module].map((permission) => (
                        <TableRow key={permission.id}>
                          <TableCell className="font-medium">
                            {permission.action}
                          </TableCell>
                          <TableCell>{permission.description || '-'}</TableCell>
                          <TableCell>
                            <div className="flex gap-2">
                              <Button
                                size="sm"
                                variant="secondary"
                                onClick={() => openEditModal(permission)}
                              >
                                Edit
                              </Button>
                              <Button
                                size="sm"
                                variant="danger"
                                onClick={() => handleDeletePermission(permission.id)}
                              >
                                Delete
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              ))}
            </div>
          )}
        </Card>

        {/* Create Permission Modal */}
        <Modal
          isOpen={isCreateModalOpen}
          onClose={() => setIsCreateModalOpen(false)}
          title="Create Permission"
          footer={
            <>
              <Button variant="secondary" onClick={() => setIsCreateModalOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleSubmit(handleCreatePermission)}>Create</Button>
            </>
          }
        >
          <form className="space-y-4">
            <Input
              label="Module"
              type="text"
              placeholder="e.g., Users, Roles, Products"
              {...register('module')}
              error={errors.module?.message}
            />
            <Input
              label="Action"
              type="text"
              placeholder="e.g., Create, Read, Update, Delete"
              {...register('action')}
              error={errors.action?.message}
            />
            <Input
              label="Description"
              type="text"
              {...register('description')}
              error={errors.description?.message}
            />
          </form>
        </Modal>

        {/* Edit Permission Modal */}
        <Modal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          title="Edit Permission"
          footer={
            <>
              <Button variant="secondary" onClick={() => setIsEditModalOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleSubmit(handleUpdatePermission)}>Save</Button>
            </>
          }
        >
          <form className="space-y-4">
            <Input
              label="Module"
              type="text"
              {...register('module')}
              error={errors.module?.message}
            />
            <Input
              label="Action"
              type="text"
              {...register('action')}
              error={errors.action?.message}
            />
            <Input
              label="Description"
              type="text"
              {...register('description')}
              error={errors.description?.message}
            />
          </form>
        </Modal>
      </div>
    </AdminLayout>
  );
}
