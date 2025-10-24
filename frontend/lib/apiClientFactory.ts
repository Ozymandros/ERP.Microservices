import { IApiClient } from '../api/clients/IApiClient';
import { AxiosApiClient } from '../api/clients/AxiosApiClient';
import { DaprApiClient } from '../api/clients/DaprApiClient';
import { config } from './config';

let apiClientInstance: IApiClient | null = null;

/**
 * Get the current access token from storage
 */
export function getToken(): string | null {
  if (typeof window === 'undefined') {
    return null;
  }
  return localStorage.getItem(config.auth.tokenKey);
}

/**
 * Create an API client instance based on configuration
 */
export function createApiClient(): IApiClient {
  if (config.apiClientType === 'dapr') {
    return new DaprApiClient(
      config.dapr.url,
      config.dapr.serviceName,
      getToken
    );
  }
  
  return new AxiosApiClient(config.axios.baseURL, getToken);
}

/**
 * Get singleton API client instance
 */
export function getApiClient(): IApiClient {
  if (!apiClientInstance) {
    apiClientInstance = createApiClient();
  }
  return apiClientInstance;
}

/**
 * Reset the API client instance (useful after configuration changes)
 */
export function resetApiClient(): void {
  apiClientInstance = null;
}
