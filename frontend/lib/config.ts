export const config = {
  // API Client type: 'axios' or 'dapr'
  apiClientType: process.env.NEXT_PUBLIC_API_CLIENT_TYPE || 'axios',
  
  // Axios configuration
  axios: {
    baseURL: process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000',
  },
  
  // Dapr configuration
  dapr: {
    url: process.env.NEXT_PUBLIC_DAPR_URL || 'http://localhost:3500',
    serviceName: process.env.NEXT_PUBLIC_DAPR_SERVICE_NAME || 'auth-service',
  },
  
  // Auth configuration
  auth: {
    tokenKey: 'access_token',
    refreshTokenKey: 'refresh_token',
    userKey: 'user',
  },
};
