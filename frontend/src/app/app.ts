import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { SessionService } from './core/session.service';
import { ApiClient } from './core/api-client';
import { DemoRole, STAFF_ROLE_LABELS, StaffRole } from './core/contracts';
import { FocusTrapDirective } from './core/focus-trap.directive';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, FocusTrapDirective],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  public readonly sessionService = inject(SessionService);
  public readonly router = inject(Router);

  public readonly currentUrl = signal<string>('');
  public readonly showResetModal = signal<boolean>(false);
  public readonly isResetting = signal<boolean>(false);
  public readonly isSwitchingRole = signal<boolean>(false);
  public readonly demoErrorMessage = signal<string>('');

  public readonly demoRoleOptions: { value: DemoRole; label: string }[] = [
    { value: 'receptionist', label: 'Atendente' },
    { value: 'technician1', label: 'Técnico 1' },
    { value: 'technician2', label: 'Técnico 2' },
  ];

  public readonly isCustomerView = computed(() =>
    this.currentUrl().startsWith('/acompanhar')
  );

  public readonly isLoginView = computed(() =>
    this.currentUrl().startsWith('/entrar')
  );

  public readonly isStandalone = computed(
    () => this.isCustomerView() || this.isLoginView()
  );

  ngOnInit(): void {
    this.currentUrl.set(this.router.url);
    this.sessionService.loadSession();
    this.sessionService.checkDemoStatus();

    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e: any) => {
        this.currentUrl.set(e.urlAfterRedirects || e.url);
      });
  }

  public getRoleLabel(role: StaffRole | null): string {
    return role ? STAFF_ROLE_LABELS[role] || role : '';
  }

  public async onLogout(): Promise<void> {
    await this.sessionService.logout();
    await this.router.navigate(['/entrar']);
  }

  // --- Demo Controls ---

  public onRoleSelectChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (select && select.value) {
      this.onSwitchRole(select.value as DemoRole);
    }
  }

  public async onSwitchRole(role: DemoRole): Promise<void> {
    this.isSwitchingRole.set(true);
    this.demoErrorMessage.set('');

    try {
      await this.sessionService.switchDemoRole(role);
      // Reload route without window reload
      const url = this.router.url;
      await this.router.navigateByUrl('/', { skipLocationChange: true });
      await this.router.navigateByUrl(url);
    } catch (err: any) {
      if (err.status === 401) {
        this.demoErrorMessage.set('Sessão de demonstração expirada (24h). Redirecionando...');
        await this.router.navigate(['/entrar']);
      } else if (err.status === 429) {
        this.demoErrorMessage.set('Limite de requisições da demonstração atingido.');
      } else {
        const problem = ApiClient.extractProblem(err);
        this.demoErrorMessage.set(problem?.detail || 'Erro ao alternar papel na demonstração.');
      }
    } finally {
      this.isSwitchingRole.set(false);
    }
  }

  public openResetModal(): void {
    this.demoErrorMessage.set('');
    this.showResetModal.set(true);
  }

  public closeResetModal(): void {
    this.showResetModal.set(false);
  }

  public async confirmReset(): Promise<void> {
    this.isResetting.set(true);
    this.demoErrorMessage.set('');

    try {
      await this.sessionService.resetDemo();
      this.showResetModal.set(false);
      // Navigate to queue after new session
      await this.router.navigate(['/ordens']);
    } catch (err: any) {
      if (err.status === 401) {
        this.demoErrorMessage.set('Sessão expirada. Inicie uma nova demonstração.');
        await this.router.navigate(['/entrar']);
      } else if (err.status === 429) {
        this.demoErrorMessage.set('Limite de reinicializações da demonstração atingido.');
      } else {
        const problem = ApiClient.extractProblem(err);
        this.demoErrorMessage.set(problem?.detail || 'Falha ao reiniciar demonstração.');
      }
    } finally {
      this.isResetting.set(false);
    }
  }
}
