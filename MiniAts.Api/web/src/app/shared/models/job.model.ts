export type JobStatus = 'Open' | 'OnHold' | 'Closed';

export interface Job {
  id: string;
  orgId: string;
  title: string;
  description: string | null;
  status: JobStatus;
  createdAt: string;
}

export interface CreateJobRequest {
  title: string;
  description?: string;
  status?: JobStatus;
}

export interface UpdateJobRequest {
  title: string;
  description?: string;
  status: JobStatus;
}
