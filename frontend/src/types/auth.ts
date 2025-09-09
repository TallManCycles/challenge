export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  username: string
  password: string
  confirmPassword: string
}

export interface AuthResponse {
  userId: number
  email: string
  username: string
  token: string
  tokenExpiry: string
}

export interface User {
  id?: number
  userId?: number
  email: string
  username: string
  fullName?: string
  createdAt?: string
  garminConnected: boolean
  emailNotificationsEnabled: boolean
  zwiftUserId?: string
  profilePhotoUrl?: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  resetToken: string
  newPassword: string
  confirmNewPassword: string
}

export interface UpdateProfileRequest {
  email: string
  fullName?: string
  emailNotificationsEnabled?: boolean
  zwiftUserId?: string
}

export interface UpdateProfileResponse {
  userId: number
  email: string
  username: string
  fullName?: string
  emailNotificationsEnabled: boolean
  zwiftUserId?: string
  profilePhotoUrl?: string
  message: string
  reprocessedFitFiles?: number
}