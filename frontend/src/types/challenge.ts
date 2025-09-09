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
  profilePhotoUrl?: string
  joinedAt: string
  currentTotal: number
  lastActivityDate?: string
  isCurrentUser?: boolean
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

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface JoinChallengeRequest {
  // Empty for now, can be extended later
}

export interface ChallengeActivity {
  id: number
  userId: number
  username: string
  fullName?: string
  profilePhotoUrl?: string
  activityName: string
  distance: number
  elevationGain: number
  movingTime: number
  activityDate: string
  likeCount: number
  isLikedByCurrentUser: boolean
}

export interface ChallengeLeaderboard {
  position: number
  userId: number
  username: string
  fullName?: string
  profilePhotoUrl?: string
  currentTotal: number
  isCurrentUser: boolean
  lastActivityDate?: string
}

export interface DailyProgressEntry {
  date: string
  dayValue: number
  cumulativeValue: number
}

export interface ParticipantDailyProgress {
  userId: number
  username: string
  fullName?: string
  profilePhotoUrl?: string
  isCurrentUser: boolean
  dailyProgress: DailyProgressEntry[]
}

export interface ChallengeDailyProgress {
  challengeId: number
  startDate: string
  endDate: string
  challengeType: ChallengeType
  challengeTypeName: string
  participants: ParticipantDailyProgress[]
}