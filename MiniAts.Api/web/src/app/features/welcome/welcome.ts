import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { Footer } from '../../core/layout/footer/footer';
import { SignupService } from './signup.service';

@Component({
  selector: 'app-welcome',
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
  templateUrl: './welcome.html',
})
export class Welcome {
  private readonly fb = inject(FormBuilder);
  private readonly signupService = inject(SignupService);

  readonly isAuthenticated = inject(AuthService).isAuthenticated;

  readonly sending = signal(false);
  readonly sent = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    contactName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    message: ['', Validators.maxLength(2000)],
    website: [''],
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.sending()) {
      return;
    }

    this.sending.set(true);
    this.error.set(null);
    try {
      await this.signupService.submit(this.form.getRawValue());
      this.sent.set(true);
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 429) {
        this.error.set('Too many requests. Please try again later or email us.');
      } else if (err instanceof HttpErrorResponse && err.status === 400 && typeof err.error === 'string') {
        this.error.set(err.error);
      } else {
        this.error.set('Could not send your request. Please try again or email us.');
      }
    } finally {
      this.sending.set(false);
    }
  }
}
