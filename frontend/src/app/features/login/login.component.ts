import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  public readonly sessionService = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  public readonly isLoading = signal<boolean>(false);
  public readonly isStartingDemo = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');

  public readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.sessionService.checkDemoStatus();
  }

  public async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const { email, password } = this.loginForm.getRawValue();

    try {
      await this.api.getCsrf();
      await this.sessionService.login({
        email: email || '',
        password: password || '',
      });
      await this.router.navigate(['/ordens']);
    } catch (err: any) {
      if (err.status === 401) {
        this.errorMessage.set('Credenciais inválidas. Verifique o email e senha informados.');
      } else {
        const problem = ApiClient.extractProblem(err);
        this.errorMessage.set(
          problem?.detail ||
            'Não foi possível autenticar. Verifique sua conexão com o servidor.'
        );
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  public async onStartDemo(): Promise<void> {
    this.isStartingDemo.set(true);
    this.errorMessage.set('');

    try {
      await this.sessionService.startDemo();
      await this.router.navigate(['/ordens']);
    } catch (err: any) {
      if (err.status === 429) {
        this.errorMessage.set('Limite de demonstrações ou requisições atingido. Tente novamente mais tarde.');
      } else {
        const problem = ApiClient.extractProblem(err);
        this.errorMessage.set(
          problem?.detail || 'Não foi possível iniciar a demonstração privada da oficina.'
        );
      }
    } finally {
      this.isStartingDemo.set(false);
    }
  }

  public fillDemoCredentials(email: string): void {
    this.loginForm.patchValue({ email });
    this.errorMessage.set('');
  }
}
