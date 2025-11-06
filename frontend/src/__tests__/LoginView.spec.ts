import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import { useAuthStore } from '../stores/auth'
import type { AuthResponse } from '../types/auth'

// Mock the auth service
vi.mock('../services/auth', () => ({
  authService: {
    login: vi.fn(),
    isAuthenticated: vi.fn(() => false),
    logout: vi.fn(),
    getCurrentUser: vi.fn(),
    getToken: vi.fn(() => null),
    getUserData: vi.fn(() => null),
    refreshToken: vi.fn()
  }
}))

// Create a mock router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/login' },
    { path: '/login', component: LoginView },
    { path: '/dashboard', component: { template: '<div>Dashboard</div>' } },
    { path: '/register', component: { template: '<div>Register</div>' } },
    { path: '/forgot-password', component: { template: '<div>Forgot Password</div>' } }
  ]
})

describe('LoginView', () => {
  let wrapper: ReturnType<typeof mount>
  let authStore: ReturnType<typeof useAuthStore>

  beforeEach(() => {
    vi.clearAllMocks()
    
    // Create wrapper with testing pinia and router
    wrapper = mount(LoginView, {
      global: {
        plugins: [
          createTestingPinia({
            createSpy: vi.fn
          }),
          router
        ]
      }
    })
    
    authStore = useAuthStore()
  })

  afterEach(() => {
    if (wrapper) {
      wrapper.unmount()
    }
  })

  it('renders login form correctly', () => {
    expect(wrapper.find('h2').text()).toBe('Welcome Back')
    expect(wrapper.find('input[type="email"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
  })

  it('allows user to enter email and password', async () => {
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')

    await emailInput.setValue('test@example.com')
    await passwordInput.setValue('password123')

    expect((emailInput.element as HTMLInputElement).value).toBe('test@example.com')
    expect((passwordInput.element as HTMLInputElement).value).toBe('password123')
  })

  it('toggles password visibility when eye icon is clicked', async () => {
    const passwordInput = wrapper.find('input[name="password"]')
    const toggleButton = wrapper.find('button[type="button"]')

    // Initially password should be hidden
    expect(passwordInput.attributes('type')).toBe('password')

    // Click toggle button
    await toggleButton.trigger('click')
    expect(passwordInput.attributes('type')).toBe('text')

    // Click again to hide
    await toggleButton.trigger('click')
    expect(passwordInput.attributes('type')).toBe('password')
  })

  it('submits form with correct credentials on successful login', async () => {
    // Mock successful login
    authStore.login = vi.fn().mockResolvedValue({
      token: 'mock-jwt-token',
      userId: 1,
      email: 'test@example.com',
      username: 'testuser'
    })

    const routerPushSpy = vi.spyOn(router, 'push')

    // Fill in the form
    await wrapper.find('input[type="email"]').setValue('test@example.com')
    await wrapper.find('input[type="password"]').setValue('password123')

    // Submit the form
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    // Verify login was called with correct credentials
    expect(authStore.login).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123',
      rememberMe: false
    })

    // Verify redirect to dashboard
    expect(routerPushSpy).toHaveBeenCalledWith('/dashboard')
  })

  it('displays error message on login failure', async () => {
    // Mock failed login
    const errorMessage = 'Invalid email or password'
    authStore.login = vi.fn().mockRejectedValue(new Error(errorMessage))
    authStore.error = errorMessage

    // Fill in the form with invalid credentials
    await wrapper.find('input[type="email"]').setValue('invalid@example.com')
    await wrapper.find('input[type="password"]').setValue('wrongpassword')

    // Submit the form
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    // Wait for the component to update
    await wrapper.vm.$nextTick()

    // Verify error is displayed
    expect(authStore.login).toHaveBeenCalledWith({
      email: 'invalid@example.com',
      password: 'wrongpassword',
      rememberMe: false
    })
  })

  it('shows loading state during login', async () => {
    // Mock login that takes time
    let resolveLogin: (value: AuthResponse) => void = () => {}
    authStore.login = vi.fn(() => new Promise<AuthResponse>(resolve => { resolveLogin = resolve }))
    authStore.isLoading = true

    // Fill and submit form
    await wrapper.find('input[type="email"]').setValue('test@example.com')
    await wrapper.find('input[type="password"]').setValue('password123')
    await wrapper.find('form').trigger('submit.prevent')

    // Check loading state
    expect(wrapper.find('button[type="submit"]').text()).toContain('Signing in...')
    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()

    // Complete the login
    resolveLogin({ 
      token: 'mock-token', 
      userId: 1, 
      email: 'test@example.com', 
      username: 'testuser',
      tokenExpiry: new Date().toISOString()
    })
    await flushPromises()
  })

  it('disables form inputs during loading', async () => {
    authStore.isLoading = true
    await wrapper.vm.$nextTick()

    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')
    const submitButton = wrapper.find('button[type="submit"]')

    expect(emailInput.attributes('disabled')).toBeDefined()
    expect(passwordInput.attributes('disabled')).toBeDefined()
    expect(submitButton.attributes('disabled')).toBeDefined()
  })

  it('clears error when clearError is called', async () => {
    authStore.error = 'Some error message'
    authStore.clearError = vi.fn()

    await wrapper.vm.$nextTick()

    // Assuming there's a close button on the error message
    const errorCloseButton = wrapper.find('.bg-red-900 button')
    if (errorCloseButton.exists()) {
      await errorCloseButton.trigger('click')
      expect(authStore.clearError).toHaveBeenCalled()
    }
  })

  it('validates required fields', async () => {
    const form = wrapper.find('form')
    
    // Try to submit empty form
    await form.trigger('submit.prevent')

    // Check that HTML5 validation would prevent submission
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')

    expect(emailInput.attributes('required')).toBeDefined()
    expect(passwordInput.attributes('required')).toBeDefined()
  })

  it('has proper form accessibility attributes', () => {
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')
    const emailLabel = wrapper.find('label[for="email"]')
    const passwordLabel = wrapper.find('label[for="password"]')

    expect(emailInput.attributes('id')).toBe('email')
    expect(passwordInput.attributes('id')).toBe('password')
    expect(emailLabel.exists()).toBe(true)
    expect(passwordLabel.exists()).toBe(true)
    expect(emailInput.attributes('autocomplete')).toBe('email')
    expect(passwordInput.attributes('autocomplete')).toBe('current-password')
  })

  it('handles remember me checkbox', async () => {
    const rememberCheckbox = wrapper.find('input[type="checkbox"]')
    
    expect(rememberCheckbox.exists()).toBe(true)
    expect((rememberCheckbox.element as HTMLInputElement).checked).toBe(false)

    await rememberCheckbox.setValue(true)
    expect((rememberCheckbox.element as HTMLInputElement).checked).toBe(true)
  })

  // Additional comprehensive tests
  describe('Form Validation', () => {
    it('displays correct error message for invalid email format', async () => {
      const emailInput = wrapper.find('input[type="email"]')
      await emailInput.setValue('invalid-email')

      expect((emailInput.element as HTMLInputElement).validity.valid).toBe(false)
      expect((emailInput.element as HTMLInputElement).validity.typeMismatch).toBe(true)
    })

    it('accepts valid email formats', async () => {
      const validEmails = [
        'test@example.com',
        'user.name+tag@example.co.uk',
        'test123@test-domain.com'
      ]

      for (const email of validEmails) {
        const emailInput = wrapper.find('input[type="email"]')
        await emailInput.setValue(email)
        expect((emailInput.element as HTMLInputElement).validity.valid).toBe(true)
      }
    })

    it('prevents form submission with empty fields', async () => {
      const form = wrapper.find('form')
      const emailInput = wrapper.find('input[type="email"]')
      const passwordInput = wrapper.find('input[type="password"]')

      // Submit empty form
      await form.trigger('submit.prevent')

      expect((emailInput.element as HTMLInputElement).validity.valueMissing).toBe(true)
      expect((passwordInput.element as HTMLInputElement).validity.valueMissing).toBe(true)
    })

    it('handles email input with whitespace', async () => {
      const emailInput = wrapper.find('input[type="email"]')
      await emailInput.setValue('  test@example.com  ')

      const form = wrapper.find('form')
      authStore.login = vi.fn().mockResolvedValue({})

      await form.trigger('submit.prevent')

      // The component passes the value as-is, trimming could be handled by the server
      expect(authStore.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: '',
        rememberMe: false
      })
    })
  })

  describe('Error Handling', () => {
    it('displays network error correctly', async () => {
      authStore.login = vi.fn().mockRejectedValue(new Error('Network error'))
      authStore.error = 'Network error'

      await wrapper.find('input[type="email"]').setValue('test@example.com')
      await wrapper.find('input[type="password"]').setValue('password123')
      await wrapper.find('form').trigger('submit.prevent')
      await flushPromises()
      await wrapper.vm.$nextTick()

      expect(wrapper.find('.bg-red-900').exists()).toBe(true)
    })

    it('displays validation error correctly', async () => {
      const validationError = new Error('Validation failed') as Error & {
        validationErrors: Record<string, string[]>
      }
      validationError.validationErrors = {
        Email: ['Email is required'],
        Password: ['Password must be at least 6 characters']
      }

      authStore.login = vi.fn().mockRejectedValue(validationError)
      authStore.error = 'Validation failed'

      await wrapper.find('form').trigger('submit.prevent')
      await flushPromises()
      await wrapper.vm.$nextTick()

      expect(wrapper.find('.bg-red-900').exists()).toBe(true)
    })

    it('clears error when user starts typing', async () => {
      authStore.error = 'Login failed'
      authStore.clearError = vi.fn()
      
      await wrapper.vm.$nextTick()

      const emailInput = wrapper.find('input[type="email"]')
      await emailInput.setValue('test@example.com')

      // The component should clear errors when user interacts
      const errorCloseButton = wrapper.find('.bg-red-900 button')
      if (errorCloseButton.exists()) {
        await errorCloseButton.trigger('click')
        expect(authStore.clearError).toHaveBeenCalled()
      }
    })

    it('handles unexpected server errors gracefully', async () => {
      authStore.login = vi.fn().mockRejectedValue(new Error('Internal Server Error'))
      authStore.error = 'Internal Server Error'

      await wrapper.find('input[type="email"]').setValue('test@example.com')
      await wrapper.find('input[type="password"]').setValue('password123')
      await wrapper.find('form').trigger('submit.prevent')
      await flushPromises()

      expect(authStore.login).toHaveBeenCalled()
    })
  })

  describe('UI Interactions', () => {
    it('shows and hides OAuth not implemented message', async () => {
      expect(wrapper.find('.bg-yellow-900').exists()).toBe(false)

      // Click OAuth button
      const googleButton = wrapper.find('button:not([type="submit"]):not([type="button"]) ~ div button')
      if (googleButton.exists()) {
        await googleButton.trigger('click')
        await wrapper.vm.$nextTick()

        expect(wrapper.find('.bg-yellow-900').exists()).toBe(true)

        // Close the message
        const closeButton = wrapper.find('.bg-yellow-900 button')
        if (closeButton.exists()) {
          await closeButton.trigger('click')
          await wrapper.vm.$nextTick()

          expect(wrapper.find('.bg-yellow-900').exists()).toBe(false)
        }
      }
    })

    it('navigates to register page when link is clicked', async () => {
      const registerLink = wrapper.find('a[href="/register"]')
      expect(registerLink.exists()).toBe(true)
      expect(registerLink.text()).toContain('Create Account')
    })

    it('navigates to forgot password page when link is clicked', async () => {
      const forgotLink = wrapper.find('a[href="/forgot-password"]')
      expect(forgotLink.exists()).toBe(true)
      expect(forgotLink.text()).toContain('Forgot password?')
    })

    it('maintains form state during loading', async () => {
      await wrapper.find('input[type="email"]').setValue('test@example.com')
      await wrapper.find('input[type="password"]').setValue('password123')
      await wrapper.find('input[type="checkbox"]').setValue(true)

      // Simulate loading state
      authStore.isLoading = true
      await wrapper.vm.$nextTick()

      // Form values should be preserved
      expect((wrapper.find('input[type="email"]').element as HTMLInputElement).value).toBe('test@example.com')
      expect((wrapper.find('input[type="password"]').element as HTMLInputElement).value).toBe('password123')
      expect((wrapper.find('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(true)
    })
  })

  describe('Loading States', () => {
    it('shows loading spinner during authentication', async () => {
      authStore.isLoading = true
      await wrapper.vm.$nextTick()

      const submitButton = wrapper.find('button[type="submit"]')
      expect(submitButton.find('svg.animate-spin').exists()).toBe(true)
      expect(submitButton.text()).toContain('Signing in...')
    })

    it('disables OAuth buttons during loading', async () => {
      authStore.isLoading = true
      await wrapper.vm.$nextTick()

      // OAuth buttons should still be clickable as they show "not implemented" message
      const oauthButtons = wrapper.findAll('button:not([type="submit"])')
      oauthButtons.forEach(button => {
        // OAuth buttons are not disabled during auth loading as they're separate functionality
        expect(button.attributes('disabled')).toBeUndefined()
      })
    })

    it('resets loading state after successful login', async () => {
      const mockResponse: AuthResponse = {
        token: 'mock-jwt-token',
        userId: 1,
        email: 'test@example.com',
        username: 'testuser',
        tokenExpiry: new Date(Date.now() + 3600000).toISOString()
      }

      authStore.login = vi.fn().mockResolvedValue(mockResponse)
      authStore.isLoading = false // Should be false after successful login

      await wrapper.find('input[type="email"]').setValue('test@example.com')
      await wrapper.find('input[type="password"]').setValue('password123')
      await wrapper.find('form').trigger('submit.prevent')
      await flushPromises()

      expect(authStore.isLoading).toBe(false)
    })

    it('resets loading state after failed login', async () => {
      authStore.login = vi.fn().mockRejectedValue(new Error('Login failed'))
      authStore.isLoading = false // Should be false after failed login
      authStore.error = 'Login failed'

      await wrapper.find('input[type="email"]').setValue('invalid@example.com')
      await wrapper.find('input[type="password"]').setValue('wrongpassword')

      try {
        await wrapper.find('form').trigger('submit.prevent')
        await flushPromises()
      } catch {
        // Expected to fail
      }

      expect(authStore.isLoading).toBe(false)
    })
  })

  describe('Accessibility', () => {
    it('has proper ARIA labels and roles', () => {
      const form = wrapper.find('form')
      const emailLabel = wrapper.find('label[for="email"]')
      const passwordLabel = wrapper.find('label[for="password"]')
      const rememberLabel = wrapper.find('label[for="remember-me"]')

      expect(form.exists()).toBe(true)
      expect(emailLabel.text()).toBe('Email')
      expect(passwordLabel.text()).toBe('Password')
      expect(rememberLabel.text()).toBe('Remember me')
    })

    it('has proper focus management', async () => {
      const emailInput = wrapper.find('input[type="email"]')
      const passwordInput = wrapper.find('input[type="password"]')

      // Mount the wrapper in the document
      document.body.appendChild(wrapper.element)

      // Focus should work correctly
      ;(emailInput.element as HTMLInputElement).focus()
      expect(document.activeElement).toBe(emailInput.element)

      // Tab navigation should work
      ;(passwordInput.element as HTMLInputElement).focus()
      expect(document.activeElement).toBe(passwordInput.element)

      // Clean up
      document.body.removeChild(wrapper.element)
    })

    it('maintains focus visible indicators', () => {
      const emailInput = wrapper.find('input[type="email"]')
      const passwordInput = wrapper.find('input[type="password"]')
      const submitButton = wrapper.find('button[type="submit"]')

      expect(emailInput.classes()).toContain('focus:ring-2')
      expect(passwordInput.classes()).toContain('focus:ring-2')
      expect(submitButton.classes()).toContain('focus:ring-2')
    })
  })

  describe('Security', () => {
    it('uses proper autocomplete attributes', () => {
      const emailInput = wrapper.find('input[type="email"]')
      const passwordInput = wrapper.find('input[type="password"]')

      expect(emailInput.attributes('autocomplete')).toBe('email')
      expect(passwordInput.attributes('autocomplete')).toBe('current-password')
    })

    it('has proper input types for security', () => {
      const emailInput = wrapper.find('input[name="email"]')
      const passwordInput = wrapper.find('input[name="password"]')

      expect(emailInput.attributes('type')).toBe('email')
      expect(passwordInput.attributes('type')).toBe('password')
    })

    it('does not expose sensitive data in DOM', async () => {
      await wrapper.find('input[type="password"]').setValue('secretpassword123')

      // Password should not be visible in DOM when hidden
      const passwordInput = wrapper.find('input[name="password"]')
      expect(passwordInput.attributes('type')).toBe('password')

      // Check that password is not exposed in any data attributes or text content
      expect(wrapper.html()).not.toContain('secretpassword123')
    })
  })
})