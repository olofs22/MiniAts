import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';
import { Footer } from '../../../core/layout/footer/footer';

@Component({
  selector: 'app-forgot-password',
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
  templateUrl: './forgot-password.html',
})
export class ForgotPassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly sending = signal(false);
  readonly sent = signal(false);
  readonly error = signal<string | null>(null);

  async submit(): Promise<void> {
    if (this.form.invalid || this.sending()) {
      return;
    }

    this.sending.set(true);
    this.error.set(null);

    try {
      await this.auth.sendPasswordReset(this.form.getRawValue().email);
      this.sent.set(true);
    } catch (err) {
      // Usually Supabase's email rate limit; show its own message so the cause is visible.
      const detail = err instanceof Error && err.message ? ` (${err.message})` : '';
      this.error.set(`Could not send the reset email. Wait a minute and try again.${detail}`);
    } finally {
      this.sending.set(false);
    }
  }
}
