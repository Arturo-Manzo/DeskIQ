import { environment } from '../../../environments/environment';

type RuntimeConfig = {
	apiBaseUrl?: string;
};

declare global {
	interface Window {
		__DESKIQ_CONFIG__?: RuntimeConfig;
	}
}

const runtimeApiBaseUrl = window.__DESKIQ_CONFIG__?.apiBaseUrl?.trim();

export const API_BASE_URL = runtimeApiBaseUrl || environment.apiBaseUrl;
