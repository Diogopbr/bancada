import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LoginComponent } from './login.component';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let mockApiClient: Partial<ApiClient>;
  let mockSessionService: Partial<SessionService>;
  let router: Router;

  beforeEach(async () => {
    mockApiClient = {
      getCsrf: vi.fn().mockResolvedValue('csrf-token'),
    };

    mockSessionService = {
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
});
