import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MeService } from '../../core/me.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  private readonly meService = inject(MeService);

  readonly me = this.meService.me;
}
