import { Injectable, computed, signal } from '@angular/core';
import type { Session } from '@supabase/supabase-js';
import { supabase } from '../supabase-client';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly sessionSignal = signal<Session | null>(null);
  private readonly readyPromise: Promise<void>;

  readonly session = this.sessionSignal.asReadonly();
  readonly user = computed(() => this.sessionSignal()?.user ?? null);
  readonly accessToken = computed(() => this.sessionSignal()?.access_token ?? null);
  readonly isAuthenticated = computed(() => this.sessionSignal() !== null);

  constructor() {
    this.readyPromise = supabase.auth.getSession().then(({ data }) => {
      this.sessionSignal.set(data.session);
    });

    supabase.auth.onAuthStateChange((_event, session) => {
      this.sessionSignal.set(session);
    });
  }

  /** Resolves once the initial session has been loaded from storage. */
  ready(): Promise<void> {
    return this.readyPromise;
  }

  async signIn(email: string, password: string): Promise<void> {
    const { error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) throw error;
  }

  async signOut(): Promise<void> {
    await supabase.auth.signOut();
  }

  /**
   * Emails a password-recovery link that signs the user in on /reset-password.
   * Supabase does not reveal whether the email exists, so this succeeds either way.
   */
  async sendPasswordReset(email: string): Promise<void> {
    const { error } = await supabase.auth.resetPasswordForEmail(email, {
      redirectTo: `${window.location.origin}/reset-password`,
    });
    if (error) throw error;
  }

  /** Sets a password on the currently-authenticated session (e.g. after accepting an invite). */
  async setPassword(password: string): Promise<void> {
    const { error } = await supabase.auth.updateUser({ password });
    if (error) throw error;
  }
}
