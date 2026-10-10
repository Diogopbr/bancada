import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { SessionService } from './core/session.service';
import { ApiClient } from './core/api-client';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let mockSessionService: Partial<SessionService>;
  let mockApiClient: Partial<ApiClient>;

  beforeEach(async () => {
    mockSessionService = {
      loadSession: vi.fn().mockResolvedValue(null),
      logout: vi.fn().mockResolvedValue(undefined),
      isAuthenticated: vi.fn().mockReturnValue(true) as any,
      isReceptionist: vi.fn().mockReturnValue(true) as any,
      isTechnician: vi.fn().mockReturnValue(false) as any,
      displayName: vi.fn().mockReturnValue('Atendente Demo') as any,
      role: vi.fn().mockReturnValue('receptionist') as any,
    };

    mockApiClient = {
      getCsrf: vi.fn().mockResolvedValue('test-csrf'),
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        { provide: SessionService, useValue: mockSessionService },
        { provide: ApiClient, useValue: mockApiClient },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
  });

  it('should create the app', () => {
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render Bancada wordmark in the sidebar navigation', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const brand = compiled.querySelector('.brand-name');
    expect(brand).toBeTruthy();
    expect(brand?.textContent).toContain('Bancada');
  });

  it('should render staff navigation links', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const navText = compiled.querySelector('.sidebar-nav')?.textContent;
    expect(navText).toContain('Ordens de serviço');
    expect(navText).toContain('Nova ordem');
  });
});
