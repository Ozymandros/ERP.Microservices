# Frontend Role Management Implementation Prompt

**Complete specification for implementing Role-Permission management UI**  
Last Updated: January 28, 2026

---

## 🎯 **Objective**

Create a comprehensive frontend role management system that integrates with the backend ERP microservices authentication API. The system should provide full CRUD operations, permission assignment, user-role management, search/filter capabilities, and export functionality.

---

## 📋 **Backend API Specification**

### **Base URL Configuration**

**Development (Local)**:
- Base URL: `http://localhost:5000` (API Gateway)
- Auth Service Routes: `/auth/api/*`

**Production**:
- Base URL: `https://your-domain.com`
- Auth Service Routes: `/auth/api/*`

**Direct Service Access** (if bypassing gateway):
- Auth Service: `http://localhost:5001/api/*`

### **Authentication**

All endpoints require **JWT Bearer Token** authentication:
```
Authorization: Bearer <access_token>
```

The token is obtained from `/auth/api/auth/login` endpoint.

---

## 🔐 **Permission Requirements**

The frontend must check user permissions before showing UI elements:

- **Roles Module**:
  - `Roles.Read` - View roles list/details
  - `Roles.Create` - Create new roles
  - `Roles.Update` - Edit existing roles
  - `Roles.Delete` - Delete roles

- **Permissions Module**:
  - `Permissions.Read` - View permissions
  - `Permissions.Create` - Create permissions
  - `Permissions.Update` - Edit permissions
  - `Permissions.Delete` - Delete permissions

---

## 📊 **Data Models**

### **RoleDto**
```typescript
interface RoleDto {
  id: string;                    // GUID
  name: string;                  // Role name (e.g., "Admin", "Manager")
  description?: string;          // Optional description
  createdAt: string;             // ISO 8601 datetime
  createdBy?: string;           // User ID who created
  updatedAt?: string;           // ISO 8601 datetime
  updatedBy?: string;           // User ID who last updated
}
```

### **CreateRoleDto**
```typescript
interface CreateRoleDto {
  name: string;                  // Required, 1-256 characters
  description?: string;          // Optional, max 500 characters
}
```

### **PermissionDto**
```typescript
interface PermissionDto {
  id: string;                    // GUID
  module: string;                // Module name (e.g., "Roles", "Users", "Sales")
  action: string;               // Action name (e.g., "Read", "Create", "Update", "Delete")
  description?: string;         // Optional description
  createdAt: string;            // ISO 8601 datetime
  createdBy?: string;          // User ID who created
  updatedAt?: string;          // ISO 8601 datetime
  updatedBy?: string;          // User ID who last updated
}
```

### **CreatePermissionDto**
```typescript
interface CreatePermissionDto {
  module: string;                // Required
  action: string;               // Required
  description?: string;         // Optional
}
```

### **UpdatePermissionDto**
```typescript
interface UpdatePermissionDto {
  module: string;                // Required
  action: string;               // Required
  description?: string;         // Optional
}
```

### **PaginatedResult<T>**
```typescript
interface PaginatedResult<T> {
  items: T[];                    // Array of items for current page
  page: number;                 // Current page number (1-indexed)
  pageSize: number;             // Items per page
  total: number;                // Total count across all pages
  totalPages: number;           // Calculated total pages
  hasPreviousPage: boolean;     // Whether previous page exists
  hasNextPage: boolean;        // Whether next page exists
}
```

### **QuerySpec** (for search/filter)
```typescript
interface QuerySpec {
  page?: number;                // Page number (default: 1)
  pageSize?: number;            // Items per page (default: 20, max: 100)
  sortBy?: string;             // Field to sort by (e.g., "name", "createdAt")
  sortDesc?: boolean;          // Sort direction (default: false = ascending)
  filters?: Record<string, string>;  // Key-value filters
  searchFields?: string;       // Comma-separated fields to search (e.g., "name,description")
  searchTerm?: string;         // Search term to apply
}
```

### **UserDto** (for role-user relationships)
```typescript
interface UserDto {
  id: string;                   // GUID
  email: string;
  username: string;
  firstName?: string;
  lastName?: string;
  roles: RoleDto[];             // User's assigned roles
  permissions: PermissionDto[]; // User's permissions (from roles + direct)
  isAdmin: boolean;             // Whether user is admin
  isActive: boolean;            // Whether user account is active
  // ... other fields
}
```

---

## 🔌 **API Endpoints**

### **Roles Endpoints**

#### **1. Get All Roles**
```
GET /auth/api/roles
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Body: RoleDto[]
```

#### **2. Get Paginated Roles**
```
GET /auth/api/roles/paginated?page=1&pageSize=10
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Body: PaginatedResult<RoleDto>
```

#### **3. Search Roles**
```
GET /auth/api/roles/search?page=1&pageSize=20&sortBy=name&sortDesc=false&searchTerm=admin&searchFields=name,description
Authorization: Bearer <token>
Permission Required: Roles.Read

Query Parameters:
- page: number (default: 1)
- pageSize: number (default: 20, max: 100)
- sortBy: string (supported: "id", "name", "createdAt")
- sortDesc: boolean (default: false)
- filters: object (key-value pairs, e.g., {"name": "Admin"})
- searchFields: string (comma-separated: "name,description")
- searchTerm: string

Response: 200 OK
Body: PaginatedResult<RoleDto>
```

#### **4. Get Role by ID**
```
GET /auth/api/roles/{id}
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Body: RoleDto

Response: 404 Not Found
Body: { message: "Role not found" }
```

#### **5. Get Role by Name**
```
GET /auth/api/roles/name/{name}
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Body: RoleDto

Response: 404 Not Found
Body: { message: "Role not found" }
```

#### **6. Create Role**
```
POST /auth/api/roles
Authorization: Bearer <token>
Permission Required: Roles.Create
Content-Type: application/json

Body: CreateRoleDto

Response: 201 Created
Body: RoleDto
Headers: Location: /auth/api/roles/{id}

Response: 400 Bad Request
Body: { errors: {...} } // Validation errors

Response: 409 Conflict
Body: { message: "Role already exists" }
```

#### **7. Update Role**
```
PUT /auth/api/roles/{id}
Authorization: Bearer <token>
Permission Required: Roles.Update
Content-Type: application/json

Body: CreateRoleDto

Response: 204 No Content

Response: 400 Bad Request
Body: { errors: {...} }

Response: 404 Not Found
Body: { message: "Role not found" }
```

#### **8. Delete Role**
```
DELETE /auth/api/roles/{id}
Authorization: Bearer <token>
Permission Required: Roles.Delete

Response: 204 No Content

Response: 404 Not Found
Body: { message: "Role not found" }
```

#### **9. Get Users in Role**
```
GET /auth/api/roles/{name}/users
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Body: UserDto[]
```

#### **10. Add Permission to Role**
```
POST /auth/api/roles/{roleId}/permissions?permissionId={permissionId}
Authorization: Bearer <token>
Permission Required: Roles.Update

Query Parameters:
- permissionId: GUID (required)

Response: 204 No Content

Response: 404 Not Found
Body: { message: "Role not found" } or { message: "Permission not found" }

Response: 409 Conflict
Body: { message: "Role already exists" } // Permission already assigned
```

**Note**: The backend currently expects `permissionId` as a query parameter, but the endpoint signature suggests it should be in the URL path. Check the actual implementation.

#### **11. Remove Permission from Role**
```
DELETE /auth/api/roles/{roleId}/permissions/{permissionId}
Authorization: Bearer <token>
Permission Required: Roles.Delete

Response: 204 No Content

Response: 500 Internal Server Error
Body: { message: "Failed to unassign permission due to an internal error." }
```

#### **12. Get Role Permissions**
```
GET /auth/api/roles/{roleId}/permissions
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Body: PermissionDto[]

Response: 404 Not Found
Body: { message: "Role with ID '{roleId}' not found." }
```

#### **13. Export Roles to Excel**
```
GET /auth/api/roles/export-xlsx
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Body: Binary file (Roles.xlsx)
```

#### **14. Export Roles to PDF**
```
GET /auth/api/roles/export-pdf
Authorization: Bearer <token>
Permission Required: Roles.Read

Response: 200 OK
Content-Type: application/pdf
Body: Binary file (Roles.pdf)
```

---

### **Permissions Endpoints**

#### **1. Get All Permissions**
```
GET /auth/api/permissions
Authorization: Bearer <token>
Permission Required: Permissions.Read

Response: 200 OK
Body: PermissionDto[]
```

#### **2. Get Paginated Permissions**
```
GET /auth/api/permissions/paginated?page=1&pageSize=10
Authorization: Bearer <token>
Permission Required: Permissions.Read

Response: 200 OK
Body: PaginatedResult<PermissionDto>
```

#### **3. Search Permissions**
```
GET /auth/api/permissions/search?page=1&pageSize=20&sortBy=module&sortDesc=false&searchTerm=sales&searchFields=module,action,description
Authorization: Bearer <token>
Permission Required: Permissions.Read

Query Parameters:
- page: number (default: 1)
- pageSize: number (default: 20, max: 100)
- sortBy: string (supported: "id", "module", "action", "createdAt")
- sortDesc: boolean (default: false)
- filters: object (key-value pairs)
- searchFields: string (comma-separated: "module,action,description")
- searchTerm: string

Response: 200 OK
Body: PaginatedResult<PermissionDto>
```

#### **4. Get Permission by ID**
```
GET /auth/api/permissions/{id}
Authorization: Bearer <token>
Permission Required: Permissions.Read

Response: 200 OK
Body: PermissionDto

Response: 404 Not Found
Body: { message: "Permission not found" }
```

#### **5. Get Permission by Module and Action**
```
GET /auth/api/permissions/module-action?module=Roles&action=Read
Authorization: Bearer <token>
Permission Required: Permissions.Read

Query Parameters:
- module: string (required)
- action: string (required)

Response: 200 OK
Body: PermissionDto

Response: 404 Not Found
Body: { message: "Permission not found" }
```

#### **6. Check User Permission**
```
GET /auth/api/permissions/check?module=Roles&action=Read
Authorization: Bearer <token>
No specific permission required (uses authenticated user)

Response: 200 OK
Body: boolean

Response: 401 Unauthorized
Body: { message: "Unauthorized" }
```

#### **7. Create Permission**
```
POST /auth/api/permissions
Authorization: Bearer <token>
Permission Required: Permissions.Create
Content-Type: application/json

Body: CreatePermissionDto

Response: 201 Created
Body: PermissionDto
Headers: Location: /auth/api/permissions/{id}

Response: 400 Bad Request
Body: { errors: {...} }

Response: 409 Conflict
Body: { message: "Permission already exists" }
```

#### **8. Update Permission**
```
PUT /auth/api/permissions/{id}
Authorization: Bearer <token>
Permission Required: Permissions.Update
Content-Type: application/json

Body: UpdatePermissionDto

Response: 204 No Content

Response: 400 Bad Request
Body: { errors: {...} }

Response: 404 Not Found
Body: { message: "Permission not found" }
```

#### **9. Delete Permission**
```
DELETE /auth/api/permissions/{id}
Authorization: Bearer <token>
Permission Required: Permissions.Delete

Response: 204 No Content

Response: 404 Not Found
Body: { message: "Permission not found" }
```

#### **10. Export Permissions to Excel**
```
GET /auth/api/permissions/export-xlsx
Authorization: Bearer <token>
Permission Required: Permissions.Read

Response: 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Body: Binary file (Permissions.xlsx)
```

#### **11. Export Permissions to PDF**
```
GET /auth/api/permissions/export-pdf
Authorization: Bearer <token>
Permission Required: Permissions.Read

Response: 200 OK
Content-Type: application/pdf
Body: Binary file (Permissions.pdf)
```

---

## 🎨 **UI/UX Requirements**

### **Role Management Page**

#### **Roles List View**
- **Table Columns**:
  - Name (sortable, searchable)
  - Description (searchable)
  - Created At (sortable)
  - Updated At (sortable)
  - Actions (View, Edit, Delete, Manage Permissions)

- **Features**:
  - ✅ Pagination controls (page size selector: 10, 20, 50, 100)
  - ✅ Search bar (searches name and description)
  - ✅ Sort by column headers
  - ✅ Filter by name
  - ✅ Export buttons (Excel, PDF) - only if `Roles.Read` permission
  - ✅ Create Role button - only if `Roles.Create` permission
  - ✅ Bulk selection (optional, for future bulk operations)

#### **Role Detail/Edit View**
- **Form Fields**:
  - Name (required, 1-256 chars, disabled if editing existing)
  - Description (optional, max 500 chars, textarea)

- **Permissions Section**:
  - ✅ List of all available permissions (grouped by module)
  - ✅ Checkboxes for each permission
  - ✅ Search/filter permissions by module/action
  - ✅ "Select All" per module
  - ✅ Show currently assigned permissions (checked)
  - ✅ Save button - only if `Roles.Update` permission

- **Users Section** (optional):
  - ✅ List of users assigned to this role
  - ✅ Link to user management page
  - ✅ Remove user from role (if `Users.Update` permission)

#### **Create Role Modal/Page**
- Same form as Edit view
- Permission assignment available during creation
- Submit button - requires `Roles.Create` permission

### **Permission Management Page**

#### **Permissions List View**
- **Table Columns**:
  - Module (sortable, filterable)
  - Action (sortable, filterable)
  - Description (searchable)
  - Created At (sortable)
  - Actions (View, Edit, Delete)

- **Features**:
  - ✅ Pagination controls
  - ✅ Search bar (searches module, action, description)
  - ✅ Filter by module dropdown
  - ✅ Filter by action dropdown
  - ✅ Sort by column headers
  - ✅ Export buttons (Excel, PDF) - only if `Permissions.Read` permission
  - ✅ Create Permission button - only if `Permissions.Create` permission

#### **Permission Detail/Edit View**
- **Form Fields**:
  - Module (required, dropdown or autocomplete from existing modules)
  - Action (required, dropdown: Read, Create, Update, Delete, or custom)
  - Description (optional, textarea)

- **Roles Section** (optional):
  - ✅ List of roles that have this permission
  - ✅ Link to role management page

#### **Create Permission Modal/Page**
- Same form as Edit view
- Submit button - requires `Permissions.Create` permission

---

## 🔧 **Technical Implementation Guidelines**

### **API Client Structure**

```typescript
// Example structure (adapt to your framework)

class RoleService {
  private baseUrl = '/auth/api/roles';
  
  async getAll(): Promise<RoleDto[]>
  async getPaginated(page: number, pageSize: number): Promise<PaginatedResult<RoleDto>>
  async search(query: QuerySpec): Promise<PaginatedResult<RoleDto>>
  async getById(id: string): Promise<RoleDto>
  async getByName(name: string): Promise<RoleDto>
  async create(data: CreateRoleDto): Promise<RoleDto>
  async update(id: string, data: CreateRoleDto): Promise<void>
  async delete(id: string): Promise<void>
  async getUsersInRole(roleName: string): Promise<UserDto[]>
  async addPermission(roleId: string, permissionId: string): Promise<void>
  async removePermission(roleId: string, permissionId: string): Promise<void>
  async getRolePermissions(roleId: string): Promise<PermissionDto[]>
  async exportXlsx(): Promise<Blob>
  async exportPdf(): Promise<Blob>
}

class PermissionService {
  private baseUrl = '/auth/api/permissions';
  
  async getAll(): Promise<PermissionDto[]>
  async getPaginated(page: number, pageSize: number): Promise<PaginatedResult<PermissionDto>>
  async search(query: QuerySpec): Promise<PaginatedResult<PermissionDto>>
  async getById(id: string): Promise<PermissionDto>
  async getByModuleAction(module: string, action: string): Promise<PermissionDto>
  async checkPermission(module: string, action: string): Promise<boolean>
  async create(data: CreatePermissionDto): Promise<PermissionDto>
  async update(id: string, data: UpdatePermissionDto): Promise<void>
  async delete(id: string): Promise<void>
  async exportXlsx(): Promise<Blob>
  async exportPdf(): Promise<Blob>
}
```

### **Error Handling**

Handle these HTTP status codes:
- **200 OK** - Success
- **201 Created** - Resource created (check `Location` header)
- **204 No Content** - Success (update/delete operations)
- **400 Bad Request** - Validation errors (show field-level errors)
- **401 Unauthorized** - Token expired/invalid (redirect to login)
- **403 Forbidden** - Missing permission (show message, hide UI)
- **404 Not Found** - Resource doesn't exist
- **409 Conflict** - Resource already exists (duplicate name)
- **500 Internal Server Error** - Server error (show generic error message)

### **Loading States**

- Show loading spinners during API calls
- Disable form buttons while submitting
- Show skeleton loaders for list views

### **Caching Strategy**

- Cache role/permission lists (invalidate on create/update/delete)
- Cache individual role/permission details
- Consider using React Query, SWR, or similar for automatic caching

### **Permission-Based UI**

```typescript
// Example permission check utility
function hasPermission(module: string, action: string): boolean {
  // Check user's permissions from JWT token or user context
  return userPermissions.some(
    p => p.module === module && p.action === action
  );
}

// Usage in components
{hasPermission('Roles', 'Create') && (
  <Button onClick={handleCreate}>Create Role</Button>
)}
```

---

## 📱 **Component Structure Recommendations**

### **Suggested Components**

1. **RoleList** - Table view with pagination, search, filters
2. **RoleForm** - Create/Edit form modal/page
3. **RoleDetail** - Detail view with permissions and users
4. **PermissionSelector** - Multi-select component for assigning permissions
5. **PermissionList** - Table view for permissions
6. **PermissionForm** - Create/Edit form modal/page
7. **ExportButtons** - Excel/PDF export buttons
8. **SearchBar** - Reusable search component
9. **PaginationControls** - Reusable pagination component
10. **PermissionGuard** - HOC/component wrapper for permission checks

---

## 🎯 **User Flows**

### **Flow 1: Create Role with Permissions**
1. User clicks "Create Role" button
2. Form opens (modal or new page)
3. User enters name and description
4. User selects permissions from list (grouped by module)
5. User clicks "Save"
6. API call: `POST /auth/api/roles` with `CreateRoleDto`
7. On success: For each selected permission, call `POST /auth/api/roles/{roleId}/permissions?permissionId={id}`
8. Show success message and redirect to role list

### **Flow 2: Edit Role Permissions**
1. User clicks "Manage Permissions" on a role
2. Load role details: `GET /auth/api/roles/{id}`
3. Load all permissions: `GET /auth/api/permissions`
4. Load role's current permissions: `GET /auth/api/roles/{roleId}/permissions`
5. Display permission list with checkboxes (checked = assigned)
6. User toggles permissions
7. On save:
   - Compare current vs new permissions
   - Call `POST /auth/api/roles/{roleId}/permissions` for additions
   - Call `DELETE /auth/api/roles/{roleId}/permissions/{permissionId}` for removals
8. Show success message

### **Flow 3: Search and Filter Roles**
1. User types in search bar
2. Debounce search (300-500ms)
3. Call `GET /auth/api/roles/search?searchTerm={term}&searchFields=name,description`
4. Update table with results
5. User selects filter (e.g., "Name contains 'Admin'")
6. Add filter to query: `filters[name]=Admin`
7. Re-fetch with filters

---

## 🚨 **Important Notes**

1. **Permission Assignment**: Currently, permissions must be assigned one-by-one. Consider implementing bulk assignment on the frontend by making multiple API calls sequentially or in parallel.

2. **Error Messages**: The backend returns `{ message: "..." }` for errors. Display these messages to users clearly.

3. **Cache Invalidation**: After create/update/delete operations, invalidate cached lists to ensure data consistency.

4. **Admin Users**: Admin users have all permissions implicitly. Check `user.isAdmin` flag to show all UI elements.

5. **Export Functionality**: Handle binary file downloads. Use `blob` response type and trigger browser download.

6. **Validation**: Implement client-side validation matching backend requirements:
   - Role name: 1-256 characters, required
   - Description: max 500 characters, optional
   - Permission module/action: required

7. **Optimistic Updates**: Consider optimistic UI updates for better UX, but handle rollback on errors.

---

## 📚 **Example API Calls**

### **TypeScript/JavaScript Examples**

```typescript
// Get all roles
const response = await fetch('/auth/api/roles', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});
const roles: RoleDto[] = await response.json();

// Create role
const createResponse = await fetch('/auth/api/roles', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: 'Manager',
    description: 'Manager role with elevated permissions'
  })
});
const newRole: RoleDto = await createResponse.json();

// Search roles
const searchParams = new URLSearchParams({
  page: '1',
  pageSize: '20',
  sortBy: 'name',
  sortDesc: 'false',
  searchTerm: 'admin',
  searchFields: 'name,description'
});
const searchResponse = await fetch(`/auth/api/roles/search?${searchParams}`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
const searchResults: PaginatedResult<RoleDto> = await searchResponse.json();

// Add permission to role
await fetch(`/auth/api/roles/${roleId}/permissions?permissionId=${permissionId}`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

// Export to Excel
const exportResponse = await fetch('/auth/api/roles/export-xlsx', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
const blob = await exportResponse.blob();
const url = window.URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = url;
a.download = 'Roles.xlsx';
a.click();
```

---

## ✅ **Acceptance Criteria**

The implementation should:

1. ✅ Display roles list with pagination, search, and filters
2. ✅ Allow creating new roles with name and description
3. ✅ Allow editing existing roles
4. ✅ Allow deleting roles (with confirmation)
5. ✅ Display and manage role permissions
6. ✅ Display users assigned to a role
7. ✅ Display permissions list with pagination and search
8. ✅ Allow creating/editing/deleting permissions
9. ✅ Export roles and permissions to Excel/PDF
10. ✅ Show/hide UI elements based on user permissions
11. ✅ Handle all error cases gracefully
12. ✅ Show loading states during API calls
13. ✅ Validate form inputs client-side
14. ✅ Provide clear user feedback (success/error messages)

---

## 🔗 **Related Endpoints**

### **User Management** (for role-user relationships)
- `GET /auth/api/users/{id}/roles` - Get user's roles
- `POST /auth/api/users/{id}/roles/{roleName}` - Assign role to user
- `DELETE /auth/api/users/{id}/roles/{roleName}` - Remove role from user

---

**Status**: ✅ Specification Complete  
**Ready for**: Frontend implementation
