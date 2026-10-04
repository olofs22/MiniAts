import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';
import { Footer } from '../../../core/layout/footer/footer';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    Footer,
  ],
  templateUrl: './accept-invite.html',
  styleUrl: './accept-invite.css',
})
export class AcceptInvite {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  // Same flow serves invite links (/accept-invite) and password-recovery links (/reset-password).
  readonly isReset = inject(ActivatedRoute).snapshot.data['mode'] === 'reset';

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly linkInvalid = signal(false);
  /** Supabase's own reason when it rejected the link, e.g. "otp_expired: Email link is ...". */
  readonly linkError = signal<string | null>(null);
  /** Set for scanner-proof links (?token_hash=...); redeemed only when the user clicks Continue. */
  readonly pendingToken = signal<EmailToken | null>(null);
  readonly verifying = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required],
  });

  constructor() {
    // A rejected link comes back with error params in the fragment (or query). supabase-js
    // swallows that error, so read it ourselves and fail fast with the real reason.
    const linkError = readAuthErrorFromUrl();
    if (linkError) {
      console.warn('Supabase rejected the auth link:', linkError);
      this.linkError.set(linkError);
      this.linkInvalid.set(true);
      return;
    }

    const token = readEmailTokenFromUrl(this.isReset ? 'recovery' : 'invite');
    if (token) {
      this.pendingToken.set(token);
      return;
    }

    // Old-style link: Supabase parses the session from the URL fragment asynchronously; give it
    // a few seconds before concluding the link is broken/expired rather than never resolving.
    setTimeout(() => {
      if (!this.isAuthenticated()) {
        this.linkInvalid.set(true);
      }
    }, 5000);
  }

  async continueWithLink(): Promise<void> {
    const token = this.pendingToken();
    if (!token || this.verifying()) {
      return;
    }

    this.verifying.set(true);
    try {
      await this.auth.verifyEmailToken(token.tokenHash, token.type);
      this.pendingToken.set(null);
      // Drop the spent token from the address bar so a refresh doesn't retry it.
      await this.router.navigate([], { queryParams: {}, replaceUrl: true });
    } catch (err) {
      const detail = err instanceof Error ? err.message : String(err);
      console.warn('Supabase rejected the email token:', detail);
      this.pendingToken.set(null);
      this.linkError.set(detail);
      this.linkInvalid.set(true);
    } finally {
      this.verifying.set(false);
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const { password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    try {
      await this.auth.setPassword(password);
      await this.router.navigateByUrl('/');
    } catch {
      this.error.set(
        this.isReset
          ? 'Could not set your password. Try again, or request a new reset link.'
          : 'Could not set your password. Try again, or ask an admin to resend the invite.',
      );
    } finally {
      this.saving.set(false);
    }
  }
}

/** Returns "code: description" if Supabase redirected here with an auth error, else null. */
function readAuthErrorFromUrl(): string | null {
  const fromHash = new URLSearchParams(window.location.hash.replace(/^#/, ''));
  const fromQuery = new URLSearchParams(window.location.search);

  for (const params of [fromHash, fromQuery]) {
    const code = params.get('error_code') ?? params.get('error');
    const description = params.get('error_description');
    if (code || description) {
      return [code, description].filter(Boolean).join(': ');
    }
  }
  return null;
}

interface EmailToken {
  tokenHash: string;
  type: 'recovery' | 'invite';
}

/** Reads a scanner-proof `?token_hash=...&type=...` link; `type` falls back to the page's mode. */
function readEmailTokenFromUrl(fallbackType: EmailToken['type']): EmailToken | null {
  const params = new URLSearchParams(window.location.search);
  const tokenHash = params.get('token_hash');
  if (!tokenHash) {
    return null;
  }
  const type = params.get('type');
  return {
    tokenHash,
    type: type === 'recovery' || type === 'invite' ? type : fallbackType,
  };
}
