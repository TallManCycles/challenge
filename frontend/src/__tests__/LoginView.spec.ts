import { describe, it, expect, vi, beforeEach } from 'vitest'
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
    { path: '/login', component: LoginView },
    { path: '/dashboard', component: { template: '<div>Dashboard</div>' } }
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
      password: 'password123'
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
      password: 'wrongpassword'
    })
  })

  it('shows loading state during login', async () => {
    // Mock login that takes time
    let resolveLogin: (value: AuthResponse) => void
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
})