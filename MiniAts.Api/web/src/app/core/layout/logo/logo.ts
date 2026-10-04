import { Component, input } from '@angular/core';

/**
 * The Mini-ATS logo (public/logo.svg) drawn inline, so the wordmark uses the app's
 * Inter font instead of whatever the browser substitutes inside an <img>.
 */
@Component({
  selector: 'app-logo',
  standalone: true,
  host: { class: 'inline-flex' },
  template: `
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 256 80"
      role="img"
      aria-label="Mini-ATS"
      [class]="size() === 'lg' ? 'h-14 w-auto' : 'h-9 w-auto'"
    >
      <g transform="translate(8,8)">
        <rect x="0" y="0" width="64" height="64" rx="14" fill="#4F46E5" />
        <rect x="11" y="14" width="10" height="36" rx="3" fill="#FFFFFF" opacity="0.55" />
        <rect x="27" y="14" width="10" height="26" rx="3" fill="#FFFFFF" opacity="0.8" />
        <rect x="43" y="14" width="10" height="16" rx="3" fill="#FFFFFF" />
        <path
          d="M41 42 l5 5 l9 -11"
          fill="none"
          stroke="#5EEAD4"
          stroke-width="4"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </g>
      <text
        x="88"
        y="52"
        font-family="Inter, Segoe UI, Helvetica, Arial, sans-serif"
        font-size="34"
        font-weight="700"
        fill="#0F172A"
      >
        Mini<tspan fill="#4F46E5">-ATS</tspan>
      </text>
    </svg>
  `,
})
export class Logo {
  /** sm: header bars; lg: standalone auth/welcome pages. */
  readonly size = input<'sm' | 'lg'>('sm');
}
