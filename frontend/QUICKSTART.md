# Frontend Quick Start Guide

This guide will help you get the ERP frontend application up and running quickly.

## Prerequisites

- Node.js 20+ and npm installed
- Running Auth Service backend (or mock API)

## Setup Steps

### 1. Navigate to Frontend Directory

```bash
cd frontend
```

### 2. Install Dependencies

```bash
npm install
```

This will install all required packages including:
- Next.js 16
- React 19
- TypeScript
- TailwindCSS 4
- Axios
- Zod
- React Hook Form

### 3. Configure Environment

Create a `.env.local` file in the frontend directory:

```bash
cp .env.example .env.local
```

Edit `.env.local` with your backend configuration:

```env
# For direct API connection (recommended for development)
NEXT_PUBLIC_API_CLIENT_TYPE=axios
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000

# OR for Dapr sidecar connection
# NEXT_PUBLIC_API_CLIENT_TYPE=dapr
# NEXT_PUBLIC_DAPR_URL=http://localhost:3500
# NEXT_PUBLIC_DAPR_SERVICE_NAME=auth-service
```

### 4. Start Development Server

```bash
npm run dev
```

The application will be available at [http://localhost:3000](http://localhost:3000)

### 5. Access the Application

1. Open your browser to http://localhost:3000
2. You'll be redirected to the login page at `/auth/login`
3. Register a new account at `/auth/register` or login with existing credentials

## Default Credentials

If your backend has seeded data, you can use the default admin account (check your backend service for credentials).

## Available Pages

### Public Pages
- **Login** - `/auth/login`
- **Register** - `/auth/register`

### Protected Pages (require authentication)
- **Users Management** - `/admin/users`
- **Roles Management** - `/admin/roles`
- **Permissions Management** - `/admin/permissions`

## Building for Production

```bash
npm run build
```

This creates an optimized production build in the `.next` directory.

## Running Production Build

```bash
npm start
```

This starts the production server on port 3000.

## Troubleshooting

### Cannot connect to backend

1. Verify your backend service is running
2. Check the `NEXT_PUBLIC_API_BASE_URL` in `.env.local`
3. Ensure CORS is configured on the backend to allow http://localhost:3000

### Authentication not working

1. Clear your browser's localStorage
2. Check browser console for errors
3. Verify JWT tokens are being returned from the backend

### Build errors

1. Delete `node_modules` and `.next` directories
2. Run `npm install` again
3. Try building with `npm run build`

## Development Tips

### Hot Reload
The development server supports hot reload. Changes to files will automatically refresh the browser.

### Component Development
All reusable UI components are in `components/ui/`. You can import and use them in any page.

### API Client
The API client is configurable via environment variables. You can switch between Axios and Dapr HTTP without changing code.

### Form Validation
Forms use Zod schemas for validation. See examples in the login and register pages.

## Next Steps

1. Customize the UI theme in `app/globals.css`
2. Add more pages under `app/admin/`
3. Extend the API client with new endpoints
4. Add additional UI components as needed

## Support

For issues or questions, refer to:
- [Frontend README](./README.md) - Complete documentation
- [Main Project README](../README.md) - Project overview
- Next.js documentation at https://nextjs.org/docs
