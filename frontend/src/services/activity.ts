const API_BASE_URL = `${import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'}/api`

interface LikeResponse {
  message: string
  likeCount: number
  isLiked: boolean
}

interface ActivityLikesResponse {
  likeCount: number
  isLiked: boolean
}

class ActivityService {
  private async makeRequest<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`
    const token = localStorage.getItem('auth_token')

    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
        ...options.headers,
      },
      ...options,
    }

    try {
      const response = await fetch(url, config)

      if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'An error occurred' }))
        throw new Error(error.message || `HTTP error! status: ${response.status}`)
      }

      return await response.json()
    } catch (error) {
      if (error instanceof Error) {
        throw error
      }
      throw new Error('Network error occurred')
    }
  }

  async likeActivity(activityId: number): Promise<LikeResponse> {
    return await this.makeRequest<LikeResponse>(`/activities/${activityId}/like`, {
      method: 'POST',
    })
  }

  async unlikeActivity(activityId: number): Promise<LikeResponse> {
    return await this.makeRequest<LikeResponse>(`/activities/${activityId}/like`, {
      method: 'DELETE',
    })
  }

  async getActivityLikes(activityId: number): Promise<ActivityLikesResponse> {
    return await this.makeRequest<ActivityLikesResponse>(`/activities/${activityId}/likes`)
  }
}

export const activityService = new ActivityService()