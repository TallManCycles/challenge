import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import { createRouter, createWebHistory } from 'vue-router'
import ChallengeDetailView from '../views/ChallengeDetailView.vue'
import type { ChallengeDetails, ChallengeActivity, ChallengeLeaderboard } from '../types/challenge'

// Mock challenge data
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
    },
    {
      id: 2,
      userId: 2,
      username: 'testuser',
      fullName: 'Test User',
      joinedAt: '2025-01-16T00:00:00Z',
      currentTotal: 73.2,
      lastActivityDate: '2025-01-29T00:00:00Z',
      isCurrentUser: true
    },
    {
      id: 3,
      userId: 3,
      username: 'mike_johnson',
      fullName: 'Mike Johnson',
      joinedAt: '2025-01-17T00:00:00Z',
      currentTotal: 87.6,
      lastActivityDate: '2025-01-28T00:00:00Z',
      isCurrentUser: false
    }
  ]
}

const mockActivities: ChallengeActivity[] = [
  {
    id: 1,
    userId: 2,
    username: 'testuser',
    fullName: 'Test User',
    activityName: 'Morning ride',
    distance: 12.5,
    elevationGain: 250,
    movingTime: 2700, // 45 minutes
    activityDate: '2025-01-30T08:00:00Z'
  },
  {
    id: 2,
    userId: 1,
    username: 'alex_rivera',
    fullName: 'Alex Rivera',
    activityName: 'Weekend adventure',
    distance: 25.7,
    elevationGain: 1200,
    movingTime: 6300, // 1h 45m
    activityDate: '2025-01-29T10:00:00Z'
  }
]

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
    userId: 3,
    username: 'mike_johnson',
    fullName: 'Mike Johnson',
    currentTotal: 87.6,
    isCurrentUser: false,
    lastActivityDate: '2025-01-28T00:00:00Z'
  },
  {
    position: 3,
    userId: 2,
    username: 'testuser',
    fullName: 'Test User',
    currentTotal: 73.2,
    isCurrentUser: true,
    lastActivityDate: '2025-01-29T00:00:00Z'
  }
]

// Mock the challenge service
vi.mock('../services/challenge', () => ({
  challengeService: {
    getChallenge: vi.fn(() => Promise.resolve(mockChallengeDetails)),
    getChallengeActivities: vi.fn(() => Promise.resolve(mockActivities)),
    getChallengeLeaderboard: vi.fn(() => Promise.resolve(mockLeaderboard))
  }
}))

const createTestRouter = () => {
  return createRouter({
    history: createWebHistory(),
    routes: [
      {
        path: '/',
        redirect: '/dashboard'
      },
      {
        path: '/challenges/:id',
        name: 'ChallengeDetail',
        component: ChallengeDetailView
      },
      {
        path: '/dashboard',
        name: 'Dashboard',
        component: { template: '<div>Dashboard</div>' }
      }
    ]
  })
}

describe('ChallengeDetailView', () => {
  let router: ReturnType<typeof createTestRouter>

  beforeEach(() => {
    vi.clearAllMocks()
    router = createTestRouter()
  })

  const createWrapper = async (challengeId: string = '1') => {
    await router.push(`/challenges/${challengeId}`)
    return mount(ChallengeDetailView, {
      global: {
        plugins: [
          createTestingPinia({
            createSpy: vi.fn,
            initialState: {
              auth: {
                isAuthenticated: true,
                user: { id: 2, email: 'test@example.com', username: 'testuser' },
                token: 'mock-token'
              }
            }
          }),
          router
        ]
      }
    })
  }

  describe('Component Rendering', () => {
    it('renders loading state initially', async () => {
      const wrapper = await createWrapper()
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
    })

    it('renders challenge details after loading', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="challenge-title"]').text()).toBe('100 Mile Challenge')
    })

    it('displays back button correctly', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const backButton = wrapper.find('[data-testid="back-button"]')
      expect(backButton.exists()).toBe(true)
    })
  })

  describe('User Progress Display', () => {
    it('calculates and displays user progress correctly', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      // Check progress card content
      const progressCard = wrapper.find('.bg-gradient-to-br')
      expect(progressCard.exists()).toBe(true)
      expect(progressCard.text()).toContain('73.2') // User's current total
      expect(progressCard.text()).toContain('73%') // Calculated percentage
      expect(progressCard.text()).toContain('26') // Remaining (rounded)
    })

    it('shows correct progress bar width', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const progressBar = wrapper.find('.bg-white.h-2.rounded-full')
      expect(progressBar.exists()).toBe(true)
      expect(progressBar.attributes('style')).toContain('width: 73%')
    })

    it('displays challenge type correctly', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('Distance')
      expect(wrapper.text()).toContain('completed')
    })
  })

  describe('Recent Activities', () => {
    it('renders recent activities list', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const activitiesH3 = wrapper.findAll('h3').find((h3) => h3.text() === 'Recent Activities')
      expect(activitiesH3).toBeDefined()
      
      // Check that activities are displayed
      expect(wrapper.text()).toContain('Morning ride')
      expect(wrapper.text()).toContain('Weekend adventure')
      expect(wrapper.text()).toContain('12.5 miles')
      expect(wrapper.text()).toContain('25.7 miles')
    })

    it('formats activity duration correctly', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('45 min') // 2700 seconds = 45 minutes
      expect(wrapper.text()).toContain('1h 45m') // 6300 seconds = 1h 45m
    })

    it('displays relative time correctly', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      // Activities from the past should show some time ago
      expect(wrapper.text()).toContain('ago')
    })

    it('shows empty state when no activities', async () => {
      const mockService = vi.mocked((await import('../services/challenge')).challengeService)
      mockService.getChallengeActivities.mockResolvedValueOnce([])

      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('No recent activities')
    })
  })

  describe('Leaderboard', () => {
    it('renders leaderboard with correct positions', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      // Check leaderboard section exists
      const leaderboardH3 = wrapper.findAll('h3').find((h3) => h3.text() === 'Leaderboard')
      expect(leaderboardH3).toBeDefined()

      // Check positions are displayed
      expect(wrapper.text()).toContain('Alex Rivera')
      expect(wrapper.text()).toContain('Mike Johnson')
      expect(wrapper.text()).toContain('95.3')
      expect(wrapper.text()).toContain('87.6')
      expect(wrapper.text()).toContain('73.2')
    })

    it('highlights current user correctly', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      // Current user should be highlighted with blue background
      const currentUserRow = wrapper.find('.bg-blue-600')
      expect(currentUserRow.exists()).toBe(true)
      expect(currentUserRow.text()).toContain('You')
    })

    it('shows crown icon for first place', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const crownIcon = wrapper.find('.text-yellow-500 svg')
      expect(crownIcon.exists()).toBe(true)
    })

    it('shows correct position colors', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      // Gold for 1st place
      const firstPlace = wrapper.find('.bg-yellow-500')
      expect(firstPlace.exists()).toBe(true)

      // Silver for 2nd place
      const secondPlace = wrapper.find('.bg-gray-400')
      expect(secondPlace.exists()).toBe(true)

      // Bronze for 3rd place
      const thirdPlace = wrapper.find('.bg-orange-500')
      expect(thirdPlace.exists()).toBe(true)
    })

    it('shows empty state when no participants', async () => {
      const mockService = vi.mocked((await import('../services/challenge')).challengeService)
      mockService.getChallengeLeaderboard.mockResolvedValueOnce([])

      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('No participants yet')
    })
  })

  describe('Progress Comparison Chart', () => {
    it('renders progress comparison section', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const chartH3 = wrapper.findAll('h3').find((h3) => h3.text() === 'Progress Comparison')
      expect(chartH3).toBeDefined()
    })

    it('shows chart legend with correct users', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('You')
      expect(wrapper.text()).toContain('Alex Rivera')
      expect(wrapper.text()).toContain('Mike Johnson')
      expect(wrapper.text()).toContain('Challenge Goal')
    })

    it('displays chart placeholder', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('Progress chart will be displayed here')
    })
  })

  describe('Navigation', () => {
    it('navigates back to dashboard when back button is clicked', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const routerPushSpy = vi.spyOn(router, 'push')
      const backButton = wrapper.find('[data-testid="back-button"]')
      
      await backButton.trigger('click')
      expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
    })
  })

  describe('Error Handling', () => {
    it('redirects to dashboard when challenge is not found', async () => {
      const mockService = vi.mocked((await import('../services/challenge')).challengeService)
      mockService.getChallenge.mockRejectedValueOnce(new Error('Challenge not found'))

      const routerPushSpy = vi.spyOn(router, 'push')
      
      await createWrapper('999')
      await flushPromises()

      expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
    })

    it('handles API errors gracefully', async () => {
      // This test verifies that the component doesn't crash when API calls fail
      // Since the current implementation redirects on any error, we'll just test that it doesn't crash
      const mockService = vi.mocked((await import('../services/challenge')).challengeService)
      
      mockService.getChallenge.mockRejectedValueOnce(new Error('API Error'))
      mockService.getChallengeActivities.mockRejectedValueOnce(new Error('API Error'))
      mockService.getChallengeLeaderboard.mockRejectedValueOnce(new Error('API Error'))

      const routerPushSpy = vi.spyOn(router, 'push')
      
      await createWrapper()
      await flushPromises()

      // Should redirect to dashboard on error without crashing
      expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
    })
  })

  describe('Data Loading', () => {
    it('makes all required API calls in parallel', async () => {
      const mockService = vi.mocked((await import('../services/challenge')).challengeService)
      
      // Reset mocks to ensure clean state
      vi.resetAllMocks()
      mockService.getChallenge.mockResolvedValueOnce(mockChallengeDetails)
      mockService.getChallengeActivities.mockResolvedValueOnce(mockActivities)
      mockService.getChallengeLeaderboard.mockResolvedValueOnce(mockLeaderboard)
      
      await createWrapper('1')
      await flushPromises()

      expect(mockService.getChallenge).toHaveBeenCalledWith(1)
      expect(mockService.getChallengeActivities).toHaveBeenCalledWith(1, 10)
      expect(mockService.getChallengeLeaderboard).toHaveBeenCalledWith(1)
    })

    it('handles different challenge types correctly', async () => {
      const elevationChallenge = {
        ...mockChallengeDetails,
        challengeType: 2,
        challengeTypeName: 'Elevation'
      }
      
      const mockService = vi.mocked((await import('../services/challenge')).challengeService)
      mockService.getChallenge.mockResolvedValueOnce(elevationChallenge)

      const wrapper = await createWrapper()
      await flushPromises()

      expect(wrapper.text()).toContain('Elevation')
      // Should calculate progress with elevation goal (10,000) - 73.2/10000 = 0.7%
      expect(wrapper.find('.bg-white.h-2.rounded-full').attributes('style')).toContain('width: 1%')
    })
  })

  describe('User Interaction', () => {
    it('shows View Full Leaderboard button', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const fullLeaderboardButton = wrapper.findAll('button').find((btn) => 
        btn.text() === 'View Full Leaderboard'
      )
      expect(fullLeaderboardButton).toBeDefined()
    })
  })

  describe('Accessibility', () => {
    it('has proper heading structure', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const h1 = wrapper.find('h1')
      expect(h1.exists()).toBe(true)
      expect(h1.text()).toBe('100 Mile Challenge')

      const h3Elements = wrapper.findAll('h3')
      expect(h3Elements.length).toBeGreaterThan(0)
    })

    it('has proper button accessibility', async () => {
      const wrapper = await createWrapper()
      await flushPromises()

      const backButton = wrapper.find('[data-testid="back-button"]')
      expect(backButton.exists()).toBe(true)
      expect(backButton.element.tagName).toBe('BUTTON')
    })
  })
})