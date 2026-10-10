import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiClient } from './api-client';
import { SessionInfo } from './contracts';

describe('Demo API and State Management', () => {
  let api: ApiClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ApiClient,
      ],
    });
    api = TestBed.inject(ApiClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getDemo should query GET /api/demo and return enabled status', async () => {
    const demoPromise = api.getDemo();

    const req = httpMock.expectOne('/api/demo');
    expect(req.request.method).toBe('GET');
    req.flush({ enabled: true });

    const result = await demoPromise;
    expect(result).toEqual({ enabled: true });
  });

  it('startDemo should send CSRF header, call POST /api/demo/start, and invalidate cached CSRF', async () => {
    api.setCsrfToken('cached-csrf-1');

    const startPromise = api.startDemo();
    await Promise.resolve();

    const startReq = httpMock.expectOne('/api/demo/start');
    expect(startReq.request.method).toBe('POST');
    expect(startReq.request.headers.get('X-CSRF-TOKEN')).toBe('cached-csrf-1');

    const demoSession: SessionInfo = {
      id: 'demo-user-1',
      displayName: 'Atendente Demonstração',
      role: 'receptionist',
    };
    startReq.flush(demoSession);

    const session = await startPromise;
    expect(session).toEqual(demoSession);
    expect(api.getCachedCsrfToken()).toBeNull();
  });

  it('switchDemoRole should call POST /api/demo/role with role and invalidate cached CSRF', async () => {
    api.setCsrfToken('cached-csrf-2');

    const switchPromise = api.switchDemoRole('technician1');
    await Promise.resolve();

    const switchReq = httpMock.expectOne('/api/demo/role');
    expect(switchReq.request.method).toBe('POST');
    expect(switchReq.request.body).toEqual({ role: 'technician1' });
    expect(switchReq.request.headers.get('X-CSRF-TOKEN')).toBe('cached-csrf-2');

    const techSession: SessionInfo = {
      id: 'tech-demo-1',
      displayName: 'Técnico 1 Demonstração',
      role: 'technician',
    };
    switchReq.flush(techSession);

    const session = await switchPromise;
    expect(session).toEqual(techSession);
    expect(api.getCachedCsrfToken()).toBeNull();
  });

  it('resetDemo should call POST /api/demo/reset and invalidate cached CSRF', async () => {
    api.setCsrfToken('cached-csrf-3');

    const resetPromise = api.resetDemo();
    await Promise.resolve();

    const resetReq = httpMock.expectOne('/api/demo/reset');
    expect(resetReq.request.method).toBe('POST');
    expect(resetReq.request.headers.get('X-CSRF-TOKEN')).toBe('cached-csrf-3');

    const resetSession: SessionInfo = {
      id: 'receptionist-reset',
      displayName: 'Atendente Demonstração',
      role: 'receptionist',
    };
    resetReq.flush(resetSession);

    const session = await resetPromise;
    expect(session).toEqual(resetSession);
    expect(api.getCachedCsrfToken()).toBeNull();
  });
});
