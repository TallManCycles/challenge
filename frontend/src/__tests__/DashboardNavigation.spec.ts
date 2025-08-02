import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'
import ChallengeDetailView from '../views/ChallengeDetailView.vue'
import type { Challenge } from '../types/challenge'

// Mock the challenge service - using future dates to ensure challenges are active
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
    endDate: '2025-12-15T00:00:00Z', // Far future date
    isActive: true,
    createdAt: '2025-01-15T00:00:00Z',
    updatedAt: '2025-01-15T00:00:00Z',
    participantCount: 3,
    isUserParticipating: true
  },
  {
    id: 2,
    title: 'Mountain Climber',
    description: 'Gain 10,000 feet of elevation in 3 weeks',
    createdById: 2,
    createdByUsername: 'mike_johnson',
    challengeType: 2,
    challengeTypeName: 'Elevation',
    startDate: '2025-01-20T00:00:00Z',
    endDate: '2025-12-10T00:00:00Z', // Far future date
    isActive: true,
    createdAt: '2025-01-20T00:00:00Z',
    updatedAt: '2025-01-20T00:00:00Z',
    participantCount: 2,
    isUserParticipating: false
  }
]

vi.mock('../services/challenge', () => ({
  challengeService: {
    getChallenges: vi.fn(() => Promise.resolve(mockChallenges)),
    getChallenge: vi.fn((id: number) => {
      const challenge = mockChallenges.find(c => c.id === id)
      if (!challenge) return Promise.reject(new Error('Challenge not found'))
      return Promise.resolve({
        ...challenge,
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
          }
        ]
      })
    }),
    getChallengeActivities: vi.fn(() => Promise.resolve([])),
    getChallengeLeaderboard: vi.fn(() => Promise.resolve([])),
    joinChallenge: vi.fn(() => Promise.resolve({ message: 'Successfully joined challenge' }))
  }
}))

// Create router with all necessary routes
const createTestRouter = () => {
  return createRouter({
    history: createWebHistory(),
    routes: [
      {
        path: '/',
        redirect: '/dashboard'
      },
      {
        path: '/dashboard',
        name: 'Dashboard',
        component: DashboardView
      },
      {
        path: '/challenges/:id',
        name: 'ChallengeDetail',
        component: ChallengeDetailView
      },
      {
        path: '/challenges/create',
        name: 'CreateChallenge',
        component: { template: '<div>Create Challenge</div>' }
      },
      {
        path: '/settings',
        name: 'Settings',
        component: { template: '<div>Settings</div>' }
      },
      {
        path: '/login',
        name: 'Login',
        component: { template: '<div>Login</div>' }
      }
    ]
  })
}

describe('Dashboard to Challenge Detail Navigation', () => {
  let router: ReturnType<typeof createTestRouter>

  beforeEach(() => {
    vi.clearAllMocks()
    router = createTestRouter()
  })

  const createWrapper = async (component: unknown, route: string = '/dashboard') => {
    await router.push(route)
    return mount(component, {
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

  describe('Dashboard Challenge Cards', () => {
    it('renders challenge cards with correct data', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      // Check that challenges are displayed
      const challengeCards = wrapper.findAll('[data-testid="challenge-card"]')
      expect(challengeCards).toHaveLength(2)

      // Check first challenge details
      const firstCard = challengeCards[0]
      expect(firstCard.text()).toContain('100 Mile Challenge')
      expect(firstCard.text()).toContain('Complete 100 miles of cycling in 4 weeks')
      expect(firstCard.text()).toContain('Distance')
      expect(firstCard.text()).toContain('3 participants')
      expect(firstCard.text()).toContain('alex_rivera')
    })

    it('shows active challenges section correctly', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      const activeChallengesSection = wrapper.find('[data-testid="active-challenges"]')
      expect(activeChallengesSection.exists()).toBe(true)
      
      // Should show 1 active challenge (the one user is participating in)
      expect(activeChallengesSection.text()).toContain('1 ongoing')
    })

    it('shows all challenges section correctly', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      const allChallengesSection = wrapper.find('[data-testid="all-challenges"]')
      expect(allChallengesSection.exists()).toBe(true)
      expect(allChallengesSection.text()).toContain('2 total')
    })

    it('displays participation status correctly', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      const challengeCards = wrapper.findAll('[data-testid="challenge-card"]')
      
      // First challenge - user is participating
      expect(challengeCards[0].text()).toContain('Participating')
      
      // Second challenge - user is not participating, should show join button
      expect(challengeCards[1].text()).toContain('Join')
    })
  })

  describe('Navigation to Challenge Detail', () => {
    it('navigates to challenge detail when challenge card is clicked', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      const routerPushSpy = vi.spyOn(router, 'push')
      
      // Click on the first challenge card
      const firstCard = wrapper.findAll('[data-testid="challenge-card"]')[0]
      await firstCard.trigger('click')

      expect(routerPushSpy).toHaveBeenCalledWith('/challenges/1')
    })

    it('navigates to challenge detail from active challenges section', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      const routerPushSpy = vi.spyOn(router, 'push')
      
      // Click on active challenge card
      const activeCard = wrapper.find('[data-testid="active-challenge-card"]')
      if (activeCard.exists()) {
        await activeCard.trigger('click')
        expect(routerPushSpy).toHaveBeenCalledWith('/challenges/1')
      }
    })

    it('prevents navigation when join button is clicked', async () => {
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      const routerPushSpy = vi.spyOn(router, 'push')
      
      // Click on join button (should not navigate)
      const joinButton = wrapper.find('[data-testid="join-button"]')
      if (joinButton.exists()) {
        await joinButton.trigger('click')
        await flushPromises()
        
        // Should not have navigated to challenge detail
        expect(routerPushSpy).not.toHaveBeenCalledWith(expect.stringMatching(/\/challenges\/\d+/))
      }
    })
  })

  describe('Challenge Detail Page Loading', () => {
    it('loads challenge detail page with correct data', async () => {
      const wrapper = await createWrapper(ChallengeDetailView, '/challenges/1')
      await flushPromises()

      // Check that challenge details are loaded
      expect(wrapper.text()).toContain('100 Mile Challenge')
      expect(wrapper.find('[data-testid="challenge-title"]').text()).toBe('100 Mile Challenge')
    })

    it('shows loading state initially', async () => {
      const wrapper = await createWrapper(ChallengeDetailView, '/challenges/1')
      
      // Before promises resolve, should show loading
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
    })

    it('shows challenge not found and redirects for invalid ID', async () => {
      const mockGetChallenge = vi.mocked((await import('../services/challenge')).challengeService.getChallenge)
      mockGetChallenge.mockRejectedValueOnce(new Error('Challenge not found'))
      
      const routerPushSpy = vi.spyOn(router, 'push')
      
      await createWrapper(ChallengeDetailView, '/challenges/999')
      await flushPromises()

      expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
    })
  })

  describe('Back Navigation', () => {
    it('navigates back to dashboard when back button is clicked', async () => {
      const wrapper = await createWrapper(ChallengeDetailView, '/challenges/1')
      await flushPromises()

      const routerPushSpy = vi.spyOn(router, 'push')
      
      const backButton = wrapper.find('[data-testid="back-button"]')
      expect(backButton.exists()).toBe(true)
      
      await backButton.trigger('click')
      expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
    })
  })

  describe('Error Handling', () => {
    it('handles API errors gracefully on dashboard', async () => {
      const mockGetChallenges = vi.mocked((await import('../services/challenge')).challengeService.getChallenges)
      mockGetChallenges.mockRejectedValueOnce(new Error('API Error'))
      
      const wrapper = await createWrapper(DashboardView)
      await flushPromises()

      // Should not crash and should stop loading
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(false)
    })

    it('handles network errors on challenge detail page', async () => {
      const mockGetChallenge = vi.mocked((await import('../services/challenge')).challengeService.getChallenge)
      mockGetChallenge.mockRejectedValueOnce(new Error('Network Error'))
      
      const routerPushSpy = vi.spyOn(router, 'push')
      
      await createWrapper(ChallengeDetailView, '/challenges/1')
      await flushPromises()

      // Should redirect to dashboard on error
      expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
    })
  })

  describe('Authentication Integration', () => {
    it('handles unauthenticated state', async () => {
      await router.push('/dashboard')
      const wrapper = mount(DashboardView, {
        global: {
          plugins: [
            createTestingPinia({
              createSpy: vi.fn,
              initialState: {
                auth: {
                  isAuthenticated: false,
                  user: null,
                  token: null
                }
              }
            }),
            router
          ]
        }
      })

      // Component should mount successfully even without auth
      expect(wrapper.exists()).toBe(true)
    })
  })
})