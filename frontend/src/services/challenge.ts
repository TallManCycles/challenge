import type {
  Challenge,
  ChallengeDetails,
  ChallengeActivity,
  ChallengeLeaderboard,
  ChallengeDailyProgress,
  CreateChallengeRequest,
  UpdateChallengeRequest,
  JoinChallengeRequest
} from '../types/challenge'

const API_BASE_URL = `${import.meta.env.VITE_APP_API_ENDPOINT || 'http://localhost:5000'}/api`

class ChallengeService {
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

  async getChallenges(): Promise<Challenge[]> {
    return await this.makeRequest<Challenge[]>('/challenge')
  }

  async getChallenge(id: number): Promise<ChallengeDetails> {
    return await this.makeRequest<ChallengeDetails>(`/challenge/${id}`)
  }

  async createChallenge(data: CreateChallengeRequest): Promise<Challenge> {
    return await this.makeRequest<Challenge>('/challenge', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async updateChallenge(id: number, data: UpdateChallengeRequest): Promise<Challenge> {
    return await this.makeRequest<Challenge>(`/challenge/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async deleteChallenge(id: number): Promise<void> {
    await this.makeRequest<void>(`/challenge/${id}`, {
      method: 'DELETE',
    })
  }

  async joinChallenge(id: number, data: JoinChallengeRequest = {}): Promise<{ message: string }> {
    return await this.makeRequest<{ message: string }>(`/challenge/${id}/join`, {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async leaveChallenge(id: number): Promise<{ message: string }> {
    return await this.makeRequest<{ message: string }>(`/challenge/${id}/leave`, {
      method: 'DELETE',
    })
  }

  async getChallengeActivities(id: number, limit: number = 10): Promise<ChallengeActivity[]> {
    return await this.makeRequest<ChallengeActivity[]>(`/challenge/${id}/activities?limit=${limit}`)
  }

  async getChallengeLeaderboard(id: number): Promise<ChallengeLeaderboard[]> {
    return await this.makeRequest<ChallengeLeaderboard[]>(`/challenge/${id}/leaderboard`)
  }

  async getChallengeDailyProgress(id: number): Promise<ChallengeDailyProgress> {
    return await this.makeRequest<ChallengeDailyProgress>(`/challenge/${id}/progress`)
  }
}

export const challengeService = new ChallengeService()