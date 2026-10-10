import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { App } from './app';
import { SessionService } from './core/session.service';
import { ApiClient } from './core/api-client';
import { DemoRole } from './core/contracts';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let mockSessionService: Partial<SessionService>;
  let mockApiClient: Partial<ApiClient>;
  let router: Router;
  let isDemoEnabledSignal: ReturnType<typeof signal<boolean>>;
  let currentDemoRoleSignal: ReturnType<typeof signal<DemoRole>>;

  beforeEach(async () => {
    isDemoEnabledSignal = signal<boolean>(false);
    currentDemoRoleSignal = signal<DemoRole>('receptionist');

    mockSessionService = {
      loadSession: vi.fn().mockResolvedValue(null),
      logout: vi.fn().mockResolvedValue(undefined),
      isAuthenticated: vi.fn().mockReturnValue(true) as any,
      isReceptionist: vi.fn().mockReturnValue(true) as any,
      isTechnician: vi.fn().mockReturnValue(false) as any,
      displayName: vi.fn().mockReturnValue('Atendente Demo') as any,
      role: vi.fn().mockReturnValue('receptionist') as any,
      isDemoEnabled: isDemoEnabledSignal as any,
      currentDemoRole: currentDemoRoleSignal as any,
      checkDemoStatus: vi.fn().mockResolvedValue(false),
      switchDemoRole: vi.fn().mockResolvedValue({
        id: 'tech-1',
        displayName: 'Técnico 1',
        role: 'technician',
      }),
      resetDemo: vi.fn().mockResolvedValue({
        id: 'rec-1',
        displayName: 'Atendente Demo',
        role: 'receptionist',
      }),
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

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
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

  it('should display restrained demo strip when demo is enabled and allow switching between the 3 roles', async () => {
    isDemoEnabledSignal.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const demoStrip = compiled.querySelector('[data-testid="demo-strip"]');
    expect(demoStrip).toBeTruthy();
    expect(demoStrip?.textContent).toContain('Demonstração');

    const roleSelect = compiled.querySelector('[data-testid="demo-role-select"]') as HTMLSelectElement;
    expect(roleSelect).toBeTruthy();

    const options = Array.from(roleSelect.options).map((o) => o.value);
    expect(options).toEqual(['receptionist', 'technician1', 'technician2']);

    await fixture.componentInstance.onSwitchRole('technician1');
    expect(mockSessionService.switchDemoRole).toHaveBeenCalledWith('technician1');
  });

  it('should require explicit product confirmation before destructive demo reset', async () => {
    isDemoEnabledSignal.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    expect(comp.showResetModal()).toBe(false);

    // Click reset trigger
    comp.openResetModal();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(comp.showResetModal()).toBe(true);
    const dialog = fixture.nativeElement.querySelector('[data-testid="demo-reset-modal"]');
    expect(dialog).toBeTruthy();
    expect(dialog?.textContent).toContain('Reiniciar dados da demonstração');

    // Confirm reset
    await comp.confirmReset();
    expect(mockSessionService.resetDemo).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/ordens']);
    expect(comp.showResetModal()).toBe(false);
  });
});
