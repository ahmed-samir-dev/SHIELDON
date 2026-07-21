/** TypeScript interfaces matching the backend LeaderboardDtos.cs */

/** A single student entry in the leaderboard. */
export interface LeaderboardEntry {
  rank: number;            // Dense rank position (ties share same rank)
  studentId: string;
  studentName: string;
  studentDisplayId: string | null;   // e.g. "STU-0042"
  avatarUrl: string | null;
  score: number;
  rankDelta: number | null;          // null = new, positive = climbed, negative = fell
}

/** Full leaderboard data returned from GET /api/courses/{id}/leaderboard */
export interface LeaderboardResponse {
  courseId: string;
  courseTitle: string;
  scoringMetric: string;             // "TotalScore" | "ExamAverage" | "AssignmentAverage"
  isLeaderboardVisible: boolean;
  showStudentOwnRank: boolean;
  topEntries: LeaderboardEntry[];    // Up to 10 rank positions (may have >10 students due to ties)
  studentOwnRank: LeaderboardEntry | null;  // null if student is in Top-10 or ShowStudentOwnRank=false
  generatedAt: string;               // ISO 8601 UTC string
}

/** Leaderboard settings returned from GET /api/courses/{id}/leaderboard/settings */
export interface LeaderboardSettings {
  id: string;
  courseId: string;
  isLeaderboardVisible: boolean;
  showStudentOwnRank: boolean;
  scoringMetric: string;
  updatedAt: string;
}

/** Payload for PUT /api/courses/{id}/leaderboard/settings */
export interface UpdateLeaderboardSettingsRequest {
  isLeaderboardVisible: boolean;
  showStudentOwnRank: boolean;
  scoringMetric: string;
}
