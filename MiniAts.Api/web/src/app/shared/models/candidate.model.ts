export interface Candidate {
  id: string;
  orgId: string;
  name: string;
  email: string | null;
  phone: string | null;
  linkedInUrl: string | null;
  notes: string | null;
  createdAt: string;
}

export interface CreateCandidateRequest {
  name: string;
  email?: string;
  phone?: string;
  linkedInUrl?: string;
  notes?: string;
}

export type UpdateCandidateRequest = CreateCandidateRequest;
