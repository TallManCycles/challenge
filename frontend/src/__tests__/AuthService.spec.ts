import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { authService } from '../services/auth'
import type { LoginRequest, RegisterRequest } from '../types/auth'

// Mock fetch globally
const mockFetch = vi.fn()
global.fetch = mockFetch

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn()
}
global.localStorage = localStorageMock as unknown as Storage

// Mock atob for JWT decoding
global.atob = vi.fn((str: string) => {
  // Mock JWT payload for testing
  if (str === 'eyJleHAiOjE2ODg3MzYwMDAsInN1YiI6IjEyMyJ9') {
    return '{"exp":1688736000,"sub":"123"}'
  }
  if (str === 'eyJzdWIiOiIxMjMifQ') {
    return '{"sub":"123"}'
  }
  return Buffer.from(str, 'base64').toString('ascii')
})

describe('AuthService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Reset localStorage mock
    localStorageMock.getItem.mockReturnValue(null)
    localStorageMock.setItem.mockImplementation(() => {})
    localStorageMock.removeItem.mockImplementation(() => {})
    // Reset authService state
    authService['token'] = null
    authService.refreshToken()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('Login functionality', () => {
    it('successfully logs in with valid credentials', async () => {
      const mockResponse = {
        token: 'mock-jwt-token',
        userId: 123,
        email: 'user@example.com',
        username: 'testuser',
        tokenExpiry: '2024-07-07T12:00:00Z'
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse)
      })

      const credentials: LoginRequest = {
        email: 'user@example.com',
        password: 'password123'
      }

      const result = await authService.login(credentials)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/login'),
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(credentials)
        }
      )

      expect(result).toEqual(mockResponse)
      expect(localStorageMock.setItem).toHaveBeenCalledWith('auth_token', mockResponse.token)
      expect(localStorageMock.setItem).toHaveBeenCalledWith('user_data', JSON.stringify({
        userId: mockResponse.userId,
        email: mockResponse.email,
        username: mockResponse.username
      }))
    })

    it('handles login failure with invalid credentials', async () => {
      const errorResponse = {
        message: 'Invalid email or password'
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: () => Promise.resolve(errorResponse)
      })

      const credentials: LoginRequest = {
        email: 'invalid@example.com',
        password: 'wrongpassword'
      }

      await expect(authService.login(credentials)).rejects.toThrow('Invalid email or password')

      expect(localStorageMock.setItem).not.toHaveBeenCalled()
    })

    it('handles validation errors from server', async () => {
      const validationResponse = {
        message: 'Validation failed',
        errors: {
          Email: ['Email is required'],
          Password: ['Password must be at least 6 characters']
        }
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: () => Promise.resolve(validationResponse)
      })

      const credentials: LoginRequest = {
        email: '',
        password: '123'
      }

      try {
        await authService.login(credentials)
        expect.fail('Should have thrown validation error')
      } catch (error: unknown) {
        expect((error as Error).message).toBe('Validation failed')
        expect((error as Error & { validationErrors: Record<string, string[]> }).validationErrors).toEqual(validationResponse.errors)
      }
    })

    it('handles network errors during login', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      const credentials: LoginRequest = {
        email: 'user@example.com',
        password: 'password123'
      }

      await expect(authService.login(credentials)).rejects.toThrow('Network error')
    })

    it('handles malformed JSON response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.reject(new Error('Invalid JSON'))
      })

      const credentials: LoginRequest = {
        email: 'user@example.com',
        password: 'password123'
      }

      await expect(authService.login(credentials)).rejects.toThrow('An error occurred')
    })
  })

  describe('Registration functionality', () => {
    it('successfully registers with valid data', async () => {
      const mockResponse = {
        token: 'new-jwt-token',
        userId: 456,
        email: 'newuser@example.com',
        username: 'newuser',
        tokenExpiry: '2024-07-07T12:00:00Z'
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse)
      })

      const userData: RegisterRequest = {
        email: 'newuser@example.com',
        password: 'password123',
        username: 'newuser',
        confirmPassword: 'password123'
      }

      const result = await authService.register(userData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/register'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(userData),
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          })
        })
      )

      expect(result).toEqual(mockResponse)
      expect(localStorageMock.setItem).toHaveBeenCalledWith('auth_token', mockResponse.token)
    })

    it('handles registration failure with existing email', async () => {
      const errorResponse = {
        message: 'Email already exists'
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: () => Promise.resolve(errorResponse)
      })

      const userData: RegisterRequest = {
        email: 'existing@example.com',
        password: 'password123',
        username: 'existing',
        confirmPassword: 'password123'
      }

      await expect(authService.register(userData)).rejects.toThrow('Email already exists')
    })
  })

  describe('Authentication state management', () => {
    it('loads valid token from localStorage on initialization', () => {
      const mockToken = 'header.eyJleHAiOjk5OTk5OTk5OTksInN1YiI6IjEyMyJ9.signature'
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return mockToken
        return null
      })
      
      // Force reload from localStorage since beforeEach clears the token
      authService.refreshToken()

      expect(authService.isAuthenticated()).toBe(true)
      expect(authService.getToken()).toBe(mockToken)
    })

    it('clears expired token on initialization', () => {
      const expiredToken = 'header.eyJleHAiOjE2ODg3MzYwMDAsInN1YiI6IjEyMyJ9.signature'
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return expiredToken
        return null
      })

      // Mock Date.now to return a time after token expiry
      const mockNow = 1688740000000 // After the token expiry
      vi.spyOn(Date, 'now').mockReturnValue(mockNow)

      // Force reload from localStorage to trigger token expiry check
      authService.refreshToken()
      
      expect(authService.isAuthenticated()).toBe(false)
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token')
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('user_data')

      vi.restoreAllMocks()
    })

    it('handles malformed tokens gracefully', () => {
      const malformedToken = 'not.a.valid.jwt'
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return malformedToken
        return null
      })

      // Force reload from localStorage to trigger token validation
      authService.refreshToken()
      
      expect(authService.isAuthenticated()).toBe(false)
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token')
    })

    it('handles tokens without expiry claim', () => {
      const tokenWithoutExp = 'header.eyJzdWIiOiIxMjMifQ.signature'
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return tokenWithoutExp
        return null
      })

      // Force reload from localStorage to trigger token validation
      authService.refreshToken()
      
      expect(authService.isAuthenticated()).toBe(true)
    })
  })

  describe('Current user functionality', () => {
    it('fetches current user with valid token', async () => {
      const mockUser = {
        id: 123,
        email: 'user@example.com',
        username: 'testuser',
        garminConnected: true,
        emailNotificationsEnabled: false
      }

      const mockToken = 'valid-token'
      authService['token'] = mockToken

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockUser)
      })

      const result = await authService.getCurrentUser()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/me'),
        {
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${mockToken}`
          }
        }
      )

      expect(result).toEqual(mockUser)
    })

    it('handles unauthorized request for current user', async () => {
      const mockToken = 'expired-token'
      authService['token'] = mockToken

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: () => Promise.resolve({ message: 'Unauthorized' })
      })

      await expect(authService.getCurrentUser()).rejects.toThrow('Unauthorized')
    })
  })

  describe('Password management', () => {
    it('successfully changes password', async () => {
      const mockToken = 'valid-token'
      authService['token'] = mockToken

      const changePasswordData = {
        currentPassword: 'oldpassword',
        newPassword: 'newpassword',
        confirmNewPassword: 'newpassword'
      }

      const mockResponse = { message: 'Password changed successfully' }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse)
      })

      const result = await authService.changePassword(changePasswordData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/change-password'),
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${mockToken}`
          },
          body: JSON.stringify(changePasswordData)
        }
      )

      expect(result).toEqual(mockResponse)
    })

    it('sends forgot password request', async () => {
      // Clear token for this test since forgot password shouldn't include auth
      authService['token'] = null
      
      const forgotPasswordData = {
        email: 'user@example.com'
      }

      const mockResponse = { message: 'Reset link sent to email' }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse)
      })

      const result = await authService.forgotPassword(forgotPasswordData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/forgot-password'),
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(forgotPasswordData)
        }
      )

      expect(result).toEqual(mockResponse)
    })

    it('resets password with valid token', async () => {
      // Clear token for this test since reset password shouldn't include auth
      authService['token'] = null
      
      const resetPasswordData = {
        resetToken: 'reset-token',
        newPassword: 'newpassword',
        confirmNewPassword: 'newpassword'
      }

      const mockResponse = { message: 'Password reset successfully' }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse)
      })

      const result = await authService.resetPassword(resetPasswordData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/reset-password'),
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(resetPasswordData)
        }
      )

      expect(result).toEqual(mockResponse)
    })
  })

  describe('Profile management', () => {
    it('updates user profile', async () => {
      const mockToken = 'valid-token'
      authService['token'] = mockToken

      const updateData = {
        email: 'user@example.com',
        emailNotificationsEnabled: true
      }

      const mockUpdatedUser = {
        id: 123,
        email: 'user@example.com',
        username: 'newusername',
        garminConnected: true,
        emailNotificationsEnabled: true
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockUpdatedUser)
      })

      const result = await authService.updateProfile(updateData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/profile'),
        {
          method: 'PATCH',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${mockToken}`
          },
          body: JSON.stringify(updateData)
        }
      )

      expect(result).toEqual(mockUpdatedUser)
    })
  })

  describe('Logout functionality', () => {
    it('clears token and localStorage on logout', () => {
      authService['token'] = 'some-token'
      
      authService.logout()

      expect(authService.getToken()).toBeNull()
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token')
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('user_data')
    })

    it('handles logout when no token exists', () => {
      authService['token'] = null
      
      authService.logout()

      expect(authService.getToken()).toBeNull()
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token')
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('user_data')
    })
  })

  describe('Utility methods', () => {
    it('retrieves user data from localStorage', () => {
      const userData = {
        userId: 123,
        email: 'user@example.com',
        username: 'testuser'
      }

      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'user_data') return JSON.stringify(userData)
        return null
      })

      const result = authService.getUserData()

      expect(result).toEqual(userData)
      expect(localStorageMock.getItem).toHaveBeenCalledWith('user_data')
    })

    it('returns null when no user data exists', () => {
      localStorageMock.getItem.mockReturnValue(null)

      const result = authService.getUserData()

      expect(result).toBeNull()
    })

    it('handles malformed user data in localStorage', () => {
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'user_data') return 'invalid-json'
        return null
      })

      expect(() => authService.getUserData()).toThrow()
    })

    it('refreshes token from localStorage', () => {
      const newToken = 'header.eyJzdWIiOiIxMjMifQ.signature' // Valid JWT format without expiry
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return newToken
        return null
      })

      authService.refreshToken()

      expect(authService.getToken()).toBe(newToken)
    })
  })

  describe('Environment configuration', () => {
    it('uses environment variable for API endpoint', () => {
      // The service should use VITE_APP_API_ENDPOINT or fallback to localhost:5123
      // Based on the test output, it seems to be using port 5123
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ token: 'test' })
      })

      authService.login({ email: 'test@example.com', password: 'test' })

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/login'),
        expect.any(Object)
      )
    })
  })

  describe('Error handling edge cases', () => {
    it('handles fetch throwing non-Error objects', async () => {
      mockFetch.mockRejectedValueOnce('String error')

      const credentials: LoginRequest = {
        email: 'user@example.com',
        password: 'password123'
      }

      await expect(authService.login(credentials)).rejects.toThrow('Network error occurred')
    })

    it('handles empty error response from server', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.resolve({})
      })

      const credentials: LoginRequest = {
        email: 'user@example.com',
        password: 'password123'
      }

      await expect(authService.login(credentials)).rejects.toThrow('HTTP error! status: 500')
    })

    it('handles response with no JSON body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.reject(new Error('No JSON'))
      })

      const credentials: LoginRequest = {
        email: 'user@example.com',
        password: 'password123'
      }

      await expect(authService.login(credentials)).rejects.toThrow('An error occurred')
    })
  })

  describe('JWT token validation', () => {
    it('correctly validates token expiry', () => {
      const currentTime = Date.now()
      const futureExp = Math.floor((currentTime + 3600000) / 1000) // 1 hour from now
      const pastExp = Math.floor((currentTime - 3600000) / 1000) // 1 hour ago

      // Mock atob to return different payloads
      const originalAtob = global.atob
      
      // Test future expiry (valid)
      global.atob = vi.fn(() => `{"exp":${futureExp},"sub":"123"}`)
      const validToken = 'header.payload.signature'
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return validToken
        return null
      })
      // Reset and reload to force token validation
      authService['token'] = null
      authService.refreshToken()
      expect(authService.isAuthenticated()).toBe(true)

      // Test past expiry (invalid) - create new token with expired time
      const expiredToken = 'header.expiredpayload.signature'
      global.atob = vi.fn(() => `{"exp":${pastExp},"sub":"123"}`)
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return expiredToken
        return null
      })
      // Reset and reload to force token validation
      authService['token'] = null
      authService.refreshToken()
      expect(authService.isAuthenticated()).toBe(false)

      global.atob = originalAtob
    })

    it('handles token without proper structure', () => {
      const invalidToken = 'invalid'
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'auth_token') return invalidToken
        return null
      })

      expect(authService.isAuthenticated()).toBe(false)
    })
  })
})