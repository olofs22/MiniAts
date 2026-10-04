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

    // Supabase parses the invite/recovery token from the URL fragment asynchronously; give it
    // a few seconds before concluding the link is broken/expired rather than never resolving.
    setTimeout(() => {
      if (!this.isAuthenticated()) {
        this.linkInvalid.set(true);
      }
    }, 5000);
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
