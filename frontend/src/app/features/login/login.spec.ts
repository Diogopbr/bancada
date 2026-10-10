import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { LoginComponent } from './login.component';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let mockApiClient: Partial<ApiClient>;
  let mockSessionService: Partial<SessionService>;
  let router: Router;
  let isDemoEnabledSignal: ReturnType<typeof signal<boolean>>;

  beforeEach(async () => {
    isDemoEnabledSignal = signal<boolean>(false);

    mockApiClient = {
      getCsrf: vi.fn().mockResolvedValue('csrf-token'),
    };

    mockSessionService = {
      isDemoEnabled: isDemoEnabledSignal as any,
      checkDemoStatus: vi.fn().mockResolvedValue(false),
      startDemo: vi.fn().mockResolvedValue({
        id: 'demo-user-1',
        displayName: 'Atendente Demonstração',
        role: 'receptionist',
      }),
      login: vi.fn().mockResolvedValue({
        id: 'user-1',
        displayName: 'Atendente Teste',
        role: 'receptionist',
      }),
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: ApiClient, useValue: mockApiClient },
        { provide: SessionService, useValue: mockSessionService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginComponent);
  });

  it('should initialize form with empty values and invalid status', () => {
    const comp = fixture.componentInstance;
    expect(comp.loginForm.valid).toBe(false);
  });

  it('should submit login and navigate to /ordens on valid credentials', async () => {
    const comp = fixture.componentInstance;
    comp.loginForm.setValue({
      email: 'atendente@bancada.example',
      password: 'demo-password',
    });
    expect(comp.loginForm.valid).toBe(true);

    await comp.onSubmit();
    expect(mockApiClient.getCsrf).toHaveBeenCalled();
    expect(mockSessionService.login).toHaveBeenCalledWith({
      email: 'atendente@bancada.example',
      password: 'demo-password',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/ordens']);
  });

  it('should display error message on 401 unauthorized', async () => {
    (mockSessionService.login as any).mockRejectedValue({ status: 401 });
    const comp = fixture.componentInstance;
    comp.loginForm.setValue({
      email: 'errado@bancada.example',
      password: 'senha-incorreta',
    });

    await comp.onSubmit();
    fixture.detectChanges();

    expect(comp.errorMessage()).toContain('Credenciais inválidas');
  });

  it('should show demo start button and fictional data explanation when demo is enabled', async () => {
    isDemoEnabledSignal.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const demoCard = compiled.querySelector('[data-testid="demo-entry-card"]');
    expect(demoCard).toBeTruthy();
    expect(demoCard?.textContent).toContain('Demonstração Privada');
    expect(demoCard?.textContent).toContain('20 ordens');
    expect(demoCard?.textContent).toContain('24h');

    const startBtn = compiled.querySelector('[data-testid="start-demo-button"]');
    expect(startBtn).toBeTruthy();
  });

  it('should call startDemo and navigate to /ordens when demo button is clicked', async () => {
    isDemoEnabledSignal.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    const comp = fixture.componentInstance;
    await (comp as any).onStartDemo();

    expect(mockSessionService.startDemo).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/ordens']);
  });

  it('should not show demo card when demo is disabled', async () => {
    isDemoEnabledSignal.set(false);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const demoCard = compiled.querySelector('[data-testid="demo-entry-card"]');
    expect(demoCard).toBeFalsy();
  });
});
