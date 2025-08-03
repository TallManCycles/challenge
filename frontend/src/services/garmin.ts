import axios from 'axios'

const API_BASE_URL = import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'

export interface GarminOAuthStatus {
  isConnected: boolean
  connectedAt?: string
}

export interface GarminOAuthInitiate {
  url: string
  state: string
}

class GarminService {
  private getAuthHeaders() {
    const token = localStorage.getItem('auth_token')
    return {
      headers: {
        Authorization: `Bearer ${token}`
      }
    }
  }

  async getOAuthStatus(): Promise<GarminOAuthStatus> {
    try {
      const response = await axios.get(`${API_BASE_URL}/api/garminoauth/status`, this.getAuthHeaders())
      return response.data
    } catch (error) {
      console.error('Failed to get Garmin OAuth status:', error)
      throw new Error('Failed to get Garmin connection status')
    }
  }

  async initiateOAuth(): Promise<GarminOAuthInitiate> {
    try {
      const response = await axios.get(`${API_BASE_URL}/api/garminoauth/initiate`, this.getAuthHeaders())
      return response.data
    } catch (error) {
      console.error('Failed to initiate Garmin OAuth:', error)
      throw new Error('Failed to initiate Garmin connection')
    }
  }

  async disconnectGarmin(): Promise<void> {
    try {
      await axios.post(`${API_BASE_URL}/api/garminoauth/disconnect`, {}, this.getAuthHeaders())
    } catch (error) {
      console.error('Failed to disconnect Garmin:', error)
      throw new Error('Failed to disconnect Garmin')
    }
  }

  redirectToGarmin(authUrl: string): void {
    // Store the current page URL for redirect after OAuth
    sessionStorage.setItem('garmin_oauth_return_url', window.location.pathname)

    // Redirect to Garmin OAuth
    window.location.href = authUrl
  }
}

export const garminService = new GarminService()
