export type SurveyStatus = 0 | 1 | 2 | 3;
export type SurveyAccessType = 0 | 1;

export type SurveySummary = {
  id: number;
  title: string;
  description?: string | null;
  isActive: boolean;
  status: SurveyStatus;
  accessType: SurveyAccessType;
  isAnonymous: boolean;
  createdAt: string;
  startsAt?: string | null;
  endsAt?: string | null;
  completedAt?: string | null;
};

export type Survey = {
  id: number;
  title: string;
  description?: string | null;
  createdAt: string;
  completedAt?: string | null;
  status: SurveyStatus;
  accessType: SurveyAccessType;
  isAnonymous: boolean;
  startsAt?: string | null;
  endsAt?: string | null;
  inviteToken?: string | null;
  updatedAt?: string | null;
  isActive: boolean;
  createdBy: number;
  enablePredictions?: boolean;
  questions: Question[];
};

export type Question = {
  id: number;
  text: string;
  order: number;
  isRequired: boolean;
  surveyId: number;
  options: Option[];
};

export type Option = {
  id: number;
  text: string;
  order: number;
  questionId: number;
};

export type UserProfile = {
  userId: number;
  displayName: string;
  avatarUrl?: string | null;
  points: number;
  publishedSurveys: { id: number; title: string; createdAt: string; endsAt?: string | null; enablePredictions: boolean }[];
};

export type VoteResults = {
  surveyId: number;
  totalVotes: number;
  questions: { questionId: number; options: { optionId: number; votesCount: number }[] }[];
};

export type Prediction = {
  id: number;
  surveyId: number;
  userId: number;
  createdAt: string;
  isScored: boolean;
  score: number;
  scoredAt?: string | null;
  answers: { questionId: number; optionId: number }[];
};

const API_BASE = (import.meta as any).env?.VITE_API_BASE_URL ?? "http://localhost:5000";

async function req<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {})
    }
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(text || `Request failed: ${res.status}`);
  }

  if (res.status === 204) return undefined as unknown as T;
  return (await res.json()) as T;
}

export const api = {
  getPublicSurveys: () => req<SurveySummary[]>("/api/surveys/public"),
  getSurvey: (id: number) => req<Survey>(`/api/surveys/${id}`),
  createSurvey: (body: any) => req<SurveySummary>(`/api/surveys`, { method: "POST", body: JSON.stringify(body) }),

  submitVote: (body: { surveyId: number; voterId: number; answers: { questionId: number; optionId: number }[] }) =>
    req<any>(`/api/votes`, { method: "POST", body: JSON.stringify(body) }),
  getVoteResults: (surveyId: number) => req<VoteResults>(`/api/votes/surveys/${surveyId}/results`),
  hasVoted: (surveyId: number, voterId: number) => req<{ surveyId: number; voterId: number; hasVoted: boolean }>(`/api/votes/surveys/${surveyId}/has-voted?voterId=${voterId}`),

  createPrediction: (surveyId: number, body: { userId: number; answers: { questionId: number; optionId: number }[] }) =>
    req<Prediction>(`/api/surveys/${surveyId}/predictions`, { method: "POST", body: JSON.stringify(body) }),
  getPrediction: (surveyId: number, userId: number) => req<Prediction>(`/api/surveys/${surveyId}/predictions/${userId}`),
  scorePredictions: (surveyId: number) => req<{ surveyId: number; scoredPredictions: number }>(`/api/surveys/${surveyId}/predictions/score`, { method: "POST" }),

  getUser: (userId: number) => req<UserProfile>(`/api/users/${userId}`),
  upsertUser: (userId: number, body: { userId: number; displayName: string; avatarUrl?: string | null }) =>
    req<void>(`/api/users/${userId}`, { method: "PUT", body: JSON.stringify(body) })
};

