export type ProfileRole = 'Admin' | 'Customer';

export interface Organization {
  id: string;
  name: string;
  createdAt: string;
}

export interface CreateOrganizationRequest {
  name: string;
}

export interface UpdateOrganizationRequest {
  name: string;
}

export interface CreateUserRequest {
  email: string;
  orgId: string | null;
  role: ProfileRole;
}

export interface UserResponse {
  userId: string;
  email: string;
  orgId: string | null;
  role: ProfileRole;
  createdAt: string;
}

export interface CreateUserPartialFailureResponse {
  supabaseUserId: string;
  email: string;
  orgId: string | null;
  role: string;
  message: string;
}
