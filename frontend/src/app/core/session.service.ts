import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiClient } from './api-client';
import { SessionInfo, StaffRole } from './contracts';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private readonly api = inject(ApiClient);

  public readonly session = signal<SessionInfo | null>(null);
  public readonly isLoading = signal<boolean>(false);
  public readonly isInitialized = signal<boolean>(false);

  public readonly isAuthenticated = computed(() => this.session() !== null);
  public readonly role = computed<StaffRole | null>(() => this.session()?.role ?? null);
  public readonly displayName = computed(() => this.session()?.displayName ?? '');
  public readonly isReceptionist = computed(() => this.role() === 'receptionist');
  public readonly isTechnician = computed(() => this.role() === 'technician');

  public async loadSession(): Promise<SessionInfo | null> {
    this.isLoading.set(true);
    try {
      const info = await this.api.getSession();
      this.session.set(info);
      return info;
    } catch {
      this.session.set(null);
      return null;
    } finally {
      this.isLoading.set(false);
      this.isInitialized.set(true);
    }
  }

  public async login(credentials: { email: string; password: string }): Promise<SessionInfo> {
    const info = await this.api.login(credentials);
    this.session.set(info);
    return info;
  }

  public async logout(): Promise<void> {
    try {
      await this.api.logout();
    } finally {
      this.session.set(null);
    }
  }
}
