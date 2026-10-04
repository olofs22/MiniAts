import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../auth/auth.service';
import { MeService } from '../../me.service';
import { Footer } from '../footer/footer';
import { Logo } from '../logo/logo';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, Footer, Logo],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly meService = inject(MeService);

  readonly me = this.meService.me;

  constructor() {
    if (!this.me()) {
      this.meService.load().catch(() => {});
    }
  }

  async signOut(): Promise<void> {
    await this.auth.signOut();
    this.meService.clear();
    await this.router.navigateByUrl('/welcome');
  }
}
