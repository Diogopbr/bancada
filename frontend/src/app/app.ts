import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { SessionService } from './core/session.service';
import { STAFF_ROLE_LABELS, StaffRole } from './core/contracts';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  public readonly sessionService = inject(SessionService);
  public readonly router = inject(Router);

  public readonly currentUrl = signal<string>('');

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
}
