# ERP Frontend

A modern frontend application for the ERP Microservices system built with Next.js, TypeScript, and TailwindCSS.

## Features

- **Authentication & Authorization**: JWT-based authentication with refresh tokens
- **User Management**: Create, read, update, and delete users with role assignment
- **Role Management**: Manage roles and assign permissions
- **Permission Management**: Control access with fine-grained permissions
- **Dual API Client Support**: Switch between Axios and Dapr HTTP clients
- **Responsive Design**: Modern UI with TailwindCSS
- **Form Validation**: Zod schema validation with React Hook Form
- **Route Protection**: Middleware-based authentication guards

## Tech Stack

- **Framework**: Next.js 16 (App Router)
- **Language**: TypeScript
- **Styling**: TailwindCSS 4
- **State Management**: React Context API
- **Forms**: React Hook Form + Zod
- **HTTP Clients**: 
  - Axios (direct API calls)
  - Native Fetch (Dapr sidecar integration)

## Getting Started

### Prerequisites

- Node.js 20+ and npm
- Running backend services (Auth service)

### Installation

1. Install dependencies:
```bash
npm install
```

2. Configure environment variables:
```bash
cp .env.example .env.local
```

Edit `.env.local` and configure your API settings:
- Set `NEXT_PUBLIC_API_CLIENT_TYPE` to `axios` or `dapr`
- Configure the appropriate endpoint URLs

### Development

Run the development server:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Build

Build for production:

```bash
npm run build
```

### Start Production Server

```bash
npm start
```

## Project Structure

```
frontend/
├── app/                      # Next.js app directory
│   ├── admin/               # Admin pages
│   │   ├── users/          # User management
│   │   ├── roles/          # Role management
│   │   └── permissions/    # Permission management
│   ├── auth/               # Authentication pages
│   │   ├── login/         # Login page
│   │   └── register/      # Registration page
│   ├── layout.tsx         # Root layout with AuthProvider
│   └── page.tsx           # Home page (redirects to login)
├── api/                    # API layer
│   ├── clients/           # API client implementations
│   │   ├── IApiClient.ts # API client interface
│   │   ├── AxiosApiClient.ts
│   │   └── DaprApiClient.ts
│   └── types.ts          # TypeScript types from OpenAPI
├── components/            # React components
│   ├── ui/               # Reusable UI components
│   │   ├── Button.tsx
│   │   ├── Input.tsx
│   │   ├── Card.tsx
│   │   ├── Modal.tsx
│   │   └── Table.tsx
│   └── layouts/          # Layout components
│       └── AdminLayout.tsx
├── contexts/             # React contexts
│   └── AuthContext.tsx  # Authentication context
├── lib/                  # Utilities and helpers
│   ├── config.ts        # Application configuration
│   ├── apiClientFactory.ts # API client factory
│   └── utils.ts         # Utility functions
├── middleware.ts        # Next.js middleware for route protection
└── public/              # Static assets
```

## API Client Configuration

The application supports two API client implementations that can be switched via environment variables:

### Axios Client (Direct API)

Use this when connecting directly to the backend API:

```env
NEXT_PUBLIC_API_CLIENT_TYPE=axios
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

### Dapr HTTP Client

Use this when routing through Dapr sidecar:

```env
NEXT_PUBLIC_API_CLIENT_TYPE=dapr
NEXT_PUBLIC_DAPR_URL=http://localhost:3500
NEXT_PUBLIC_DAPR_SERVICE_NAME=auth-service
```

## Authentication Flow

1. User logs in via `/auth/login`
2. Backend returns JWT access token and refresh token
3. Tokens are stored in localStorage
4. Access token is included in all API requests via Authorization header
5. When token expires, the app automatically refreshes it using the refresh token
6. If refresh fails, user is redirected to login

## Route Protection

The middleware (`middleware.ts`) protects routes:

- `/admin/*` routes require authentication
- `/auth/*` routes redirect to admin if already authenticated
- Unauthenticated users accessing protected routes are redirected to login

## Available Pages

### Authentication
- **Login** (`/auth/login`): User login with email and password
- **Register** (`/auth/register`): New user registration

### Admin Panel
- **Users** (`/admin/users`): 
  - List all users with search
  - View user details
  - Edit user information
  - Delete users
  - Assign/remove roles
  
- **Roles** (`/admin/roles`):
  - List all roles
  - Create new roles
  - Edit role details
  - Delete roles
  - Assign/remove permissions
  
- **Permissions** (`/admin/permissions`):
  - List all permissions grouped by module
  - Create new permissions
  - Edit permissions
  - Delete permissions

## Development Guidelines

### Adding New API Endpoints

1. Update the `IApiClient` interface in `api/clients/IApiClient.ts`
2. Implement the method in both `AxiosApiClient.ts` and `DaprApiClient.ts`
3. Add TypeScript types to `api/types.ts` if needed

### Creating New Pages

1. Create a new directory in `app/admin/`
2. Add a `page.tsx` file
3. Wrap content with `<AdminLayout>`
4. Use existing UI components for consistency

### Adding UI Components

Add reusable components to `components/ui/` and follow the existing patterns for styling and props.

## Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm start` - Start production server
- `npm run lint` - Run ESLint

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXT_PUBLIC_API_CLIENT_TYPE` | API client type (`axios` or `dapr`) | `axios` |
| `NEXT_PUBLIC_API_BASE_URL` | Direct API base URL | `http://localhost:5000` |
| `NEXT_PUBLIC_DAPR_URL` | Dapr sidecar URL | `http://localhost:3500` |
| `NEXT_PUBLIC_DAPR_SERVICE_NAME` | Dapr service name | `auth-service` |

## License

This project is part of the ERP Microservices system.
