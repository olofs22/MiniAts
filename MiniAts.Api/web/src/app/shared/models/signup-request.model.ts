export interface CreateSignupRequest {
  companyName: string;
  contactName: string;
  email: string;
  message: string;
  /** Honeypot; must stay empty for real visitors. */
  website: string;
}

export interface SignupRequest {
  id: string;
  companyName: string;
  contactName: string;
  email: string;
  message: string | null;
  createdAt: string;
}
