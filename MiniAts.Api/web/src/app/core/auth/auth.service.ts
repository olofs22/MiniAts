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
}
