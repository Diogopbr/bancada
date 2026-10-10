import { Component, inject, signal } from '@angular/core';
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
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly sessionService = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  public readonly isLoading = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');

  public readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  public async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const { email, password } = this.loginForm.getRawValue();

    try {
      // Carrega CSRF antes do login
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

  public fillDemoCredentials(email: string): void {
    this.loginForm.patchValue({ email });
    this.errorMessage.set('');
  }
}
