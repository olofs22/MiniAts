import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from './auth.service';
import { MeService } from '../me.service';
import { unauthorizedInterceptor } from './unauthorized.interceptor';

describe('unauthorizedInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  const auth = { signOut: vi.fn(() => Promise.resolve()) };
  const meService = { clear: vi.fn() };
  const router = { url: '/jobs', navigateByUrl: vi.fn(() => Promise.resolve(true)) };

  beforeEach(() => {
    vi.clearAllMocks();
    router.url = '/jobs';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([unauthorizedInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: MeService, useValue: meService },
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  async function requestFailingWith(status: number): Promise<unknown> {
    const request = firstValueFrom(http.get('/api/jobs')).catch((err) => err);
    httpTesting.expectOne('/api/jobs').flush(null, { status, statusText: 'Error' });
    return request;
  }

  it('signs out and redirects to /login on 401, still surfacing the error', async () => {
    const error = await requestFailingWith(401);

    expect(error).toBeTruthy();
    expect(meService.clear).toHaveBeenCalled();
    expect(auth.signOut).toHaveBeenCalled();
    await vi.waitFor(() => expect(router.navigateByUrl).toHaveBeenCalledWith('/login'));
  });

  it('leaves other errors to the calling feature', async () => {
    await requestFailingWith(500);
    await requestFailingWith(403);

    expect(auth.signOut).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not redirect again when already on /login', async () => {
    router.url = '/login';

    await requestFailingWith(401);

    expect(auth.signOut).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
