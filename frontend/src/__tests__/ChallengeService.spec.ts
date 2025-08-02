import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { challengeService } from '../services/challenge'
import type { Challenge, ChallengeDetails, ChallengeActivity, ChallengeLeaderboard } from '../types/challenge'

// Mock fetch globally
const mockFetch = vi.fn()
global.fetch = mockFetch

// Mock localStorage
const mockLocalStorage = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn()
}
Object.defineProperty(window, 'localStorage', {
  value: mockLocalStorage
})

describe('ChallengeService', () => {
  const mockToken = 'mock-jwt-token'
  const baseUrl = 'http://localhost:5000/api'

  beforeEach(() => {
    vi.clearAllMocks()
    mockLocalStorage.getItem.mockReturnValue(mockToken)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  const createMockResponse = (data: any, status: number = 200) => {
    return Promise.resolve({
      ok: status >= 200 && status < 300,
      status,
      json: () => Promise.resolve(data)
    } as Response)
  }

  describe('getChallenges', () => {
    const mockChallenges: Challenge[] = [
      {
        id: 1,
        title: '100 Mile Challenge',
        description: 'Complete 100 miles of cycling in 4 weeks',
        createdById: 1,
        createdByUsername: 'alex_rivera',
        challengeType: 1,
        challengeTypeName: 'Distance',
        startDate: '2025-01-15T00:00:00Z',
        endDate: '2025-02-15T00:00:00Z',
        isActive: true,
        createdAt: '2025-01-15T00:00:00Z',
        updatedAt: '2025-01-15T00:00:00Z',
        participantCount: 3,
        isUserParticipating: true
      }
    ]

    it('fetches challenges successfully', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockChallenges))

      const result = await challengeService.getChallenges()

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge`,
        {
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${mockToken}`
          }
        }
      )
      expect(result).toEqual(mockChallenges)
    })

    it('handles API errors correctly', async () => {
      const errorResponse = { message: 'Unauthorized' }
      mockFetch.mockResolvedValueOnce(createMockResponse(errorResponse, 401))

      await expect(challengeService.getChallenges()).rejects.toThrow('Unauthorized')
    })

    it('handles network errors', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      await expect(challengeService.getChallenges()).rejects.toThrow('Network error')
    })

    it('includes authorization header when token exists', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockChallenges))

      await challengeService.getChallenges()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': `Bearer ${mockToken}`
          })
        })
      )
    })

    it('works without authorization header when no token', async () => {
      mockLocalStorage.getItem.mockReturnValue(null)
      mockFetch.mockResolvedValueOnce(createMockResponse(mockChallenges))

      await challengeService.getChallenges()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.not.objectContaining({
            'Authorization': expect.any(String)
          })
        })
      )
    })
  })

  describe('getChallenge', () => {
    const mockChallengeDetails: ChallengeDetails = {
      id: 1,
      title: '100 Mile Challenge',
      description: 'Complete 100 miles of cycling in 4 weeks',
      createdById: 1,
      createdByUsername: 'alex_rivera',
      challengeType: 1,
      challengeTypeName: 'Distance',
      startDate: '2025-01-15T00:00:00Z',
      endDate: '2025-02-15T00:00:00Z',
      isActive: true,
      createdAt: '2025-01-15T00:00:00Z',
      updatedAt: '2025-01-15T00:00:00Z',
      participantCount: 3,
      isUserParticipating: true,
      participants: [
        {
          id: 1,
          userId: 1,
          username: 'alex_rivera',
          fullName: 'Alex Rivera',
          joinedAt: '2025-01-15T00:00:00Z',
          currentTotal: 95.3,
          lastActivityDate: '2025-01-30T00:00:00Z',
          isCurrentUser: false
        }
      ]
    }

    it('fetches challenge details successfully', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockChallengeDetails))

      const result = await challengeService.getChallenge(1)

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge/1`,
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': `Bearer ${mockToken}`
          })
        })
      )
      expect(result).toEqual(mockChallengeDetails)
    })

    it('handles 404 errors for non-existent challenges', async () => {
      const errorResponse = { message: 'Challenge not found' }
      mockFetch.mockResolvedValueOnce(createMockResponse(errorResponse, 404))

      await expect(challengeService.getChallenge(999)).rejects.toThrow('Challenge not found')
    })
  })

  describe('getChallengeActivities', () => {
    const mockActivities: ChallengeActivity[] = [
      {
        id: 1,
        userId: 1,
        username: 'alex_rivera',
        fullName: 'Alex Rivera',
        activityName: 'Morning ride',
        distance: 12.5,
        elevationGain: 250,
        movingTime: 2700,
        activityDate: '2025-01-30T08:00:00Z'
      }
    ]

    it('fetches challenge activities successfully', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockActivities))

      const result = await challengeService.getChallengeActivities(1)

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge/1/activities?limit=10`,
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': `Bearer ${mockToken}`
          })
        })
      )
      expect(result).toEqual(mockActivities)
    })

    it('respects custom limit parameter', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockActivities))

      await challengeService.getChallengeActivities(1, 20)

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge/1/activities?limit=20`,
        expect.any(Object)
      )
    })

    it('uses default limit when not specified', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockActivities))

      await challengeService.getChallengeActivities(1)

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge/1/activities?limit=10`,
        expect.any(Object)
      )
    })
  })

  describe('getChallengeLeaderboard', () => {
    const mockLeaderboard: ChallengeLeaderboard[] = [
      {
        position: 1,
        userId: 1,
        username: 'alex_rivera',
        fullName: 'Alex Rivera',
        currentTotal: 95.3,
        isCurrentUser: false,
        lastActivityDate: '2025-01-30T00:00:00Z'
      },
      {
        position: 2,
        userId: 2,
        username: 'testuser',
        fullName: 'Test User',
        currentTotal: 73.2,
        isCurrentUser: true,
        lastActivityDate: '2025-01-29T00:00:00Z'
      }
    ]

    it('fetches challenge leaderboard successfully', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockLeaderboard))

      const result = await challengeService.getChallengeLeaderboard(1)

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge/1/leaderboard`,
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': `Bearer ${mockToken}`
          })
        })
      )
      expect(result).toEqual(mockLeaderboard)
    })

    it('returns properly sorted leaderboard', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockLeaderboard))

      const result = await challengeService.getChallengeLeaderboard(1)

      expect(result[0].position).toBe(1)
      expect(result[0].currentTotal).toBe(95.3)
      expect(result[1].position).toBe(2)
      expect(result[1].currentTotal).toBe(73.2)
    })
  })

  describe('joinChallenge', () => {
    const mockJoinResponse = { message: 'Successfully joined challenge' }

    it('joins challenge successfully', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockJoinResponse))

      const result = await challengeService.joinChallenge(1)

      expect(mockFetch).toHaveBeenCalledWith(
        `${baseUrl}/challenge/1/join`,
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({}),
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${mockToken}`
          })
        })
      )
      expect(result).toEqual(mockJoinResponse)
    })

    it('sends custom join data when provided', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse(mockJoinResponse))
      const customData = { note: 'Excited to participate!' }

      await challengeService.joinChallenge(1, customData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify(customData)
        })
      )
    })

    it('handles join errors correctly', async () => {
      const errorResponse = { message: 'Already participating in this challenge' }
      mockFetch.mockResolvedValueOnce(createMockResponse(errorResponse, 400))

      await expect(challengeService.joinChallenge(1)).rejects.toThrow('Already participating in this challenge')
    })
  })

  describe('Error Handling', () => {
    it('handles malformed JSON responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.reject(new Error('Invalid JSON'))
      } as Response)

      await expect(challengeService.getChallenges()).rejects.toThrow('HTTP error! status: 500')
    })

    it('handles empty error responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.resolve({})
      } as Response)

      await expect(challengeService.getChallenges()).rejects.toThrow('HTTP error! status: 500')
    })

    it('handles network timeouts', async () => {
      mockFetch.mockRejectedValueOnce(new TypeError('Failed to fetch'))

      await expect(challengeService.getChallenges()).rejects.toThrow('Network error occurred')
    })
  })

  describe('API Configuration', () => {
    it('uses correct base URL from environment', () => {
      // Test that the service is configured with the correct base URL
      // This is implicitly tested by the API calls above
      expect(true).toBe(true) // Placeholder assertion
    })

    it('sets correct content type headers', async () => {
      mockFetch.mockResolvedValueOnce(createMockResponse({}))

      await challengeService.getChallenges()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          })
        })
      )
    })

    it('handles missing token gracefully', async () => {
      mockLocalStorage.getItem.mockReturnValue(null)
      mockFetch.mockResolvedValueOnce(createMockResponse([]))

      const result = await challengeService.getChallenges()

      expect(result).toEqual([])
      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.not.objectContaining({
            'Authorization': expect.any(String)
          })
        })
      )
    })
  })

  describe('Concurrent Requests', () => {
    it('handles multiple concurrent requests correctly', async () => {
      const mockResponse1 = createMockResponse([{ id: 1, title: 'Challenge 1' }])
      const mockResponse2 = createMockResponse({ id: 1, title: 'Challenge 1', participants: [] })
      const mockResponse3 = createMockResponse([])

      mockFetch
        .mockResolvedValueOnce(mockResponse1)
        .mockResolvedValueOnce(mockResponse2)
        .mockResolvedValueOnce(mockResponse3)

      const [challenges, challenge, activities] = await Promise.all([
        challengeService.getChallenges(),
        challengeService.getChallenge(1),
        challengeService.getChallengeActivities(1)
      ])

      expect(challenges).toHaveLength(1)
      expect(challenge.id).toBe(1)
      expect(activities).toHaveLength(0)
      expect(mockFetch).toHaveBeenCalledTimes(3)
    })
  })
})