export enum ChallengeType {
  Distance = 1,
  Elevation = 2,
  Time = 3
}

export interface Challenge {
  id: number
  title: string
  description?: string
  createdById: number
  createdByUsername: string
  challengeType: ChallengeType
  challengeTypeName: string
  startDate: string
  endDate: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  participantCount: number
  isUserParticipating: boolean
}

export interface ChallengeParticipant {
  id: number
  userId: number
  username: string
  fullName?: string
  joinedAt: string
  currentTotal: number
  lastActivityDate?: string
}

export interface ChallengeDetails extends Challenge {
  participants: ChallengeParticipant[]
}

export interface CreateChallengeRequest {
  title: string
  description?: string
  challengeType: ChallengeType
  startDate: string
  endDate: string
}

export interface UpdateChallengeRequest {
  title: string
  description?: string
  challengeType: ChallengeType
  startDate: string
  endDate: string
  isActive: boolean
}

export interface JoinChallengeRequest {
  // Empty for now, can be extended later
}