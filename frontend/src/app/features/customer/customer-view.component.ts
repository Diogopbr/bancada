import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import {
  CustomerOrderProjection,
  ORDER_STATUS_LABELS,
  OrderStatus,
} from '../../core/contracts';

@Component({
  selector: 'app-customer-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './customer-view.component.html',
  styleUrl: './customer-view.component.scss',
})
export class CustomerViewComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClient);

  public readonly order = signal<CustomerOrderProjection | null>(null);
  public readonly isLoading = signal<boolean>(true);
  public readonly isExpired = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');
  public readonly isDeciding = signal<boolean>(false);
  public readonly conflictMessage = signal<string>('');
  public readonly successMessage = signal<string>('');

  // Confirmação de decisão
  public readonly pendingDecision = signal<'accepted' | 'rejected' | null>(null);

  private initPromise: Promise<void> | null = null;

  public readonly canDecide = computed(() => {
    const o = this.order();
    if (!o || !o.currentQuote) return false;
    return o.status === 'awaitingApproval' && o.currentQuote.decision === null;
  });

  ngOnInit(): void {
    this.initializeCustomerSession();
  }

  public async initializeCustomerSession(): Promise<void> {
    if (this.initPromise) {
      return this.initPromise;
    }

    this.initPromise = (async () => {
      this.isLoading.set(true);
      this.isExpired.set(false);
      this.errorMessage.set('');
      this.conflictMessage.set('');

      try {
        // 1. Verificar se há token no fragmento de URL (#token=...)
        let token: string | null = null;
        const hash = typeof window !== 'undefined' ? window.location.hash : '';
        if (hash) {
          const match = hash.match(/token=([^&]+)/);
          if (match && match[1]) {
            token = decodeURIComponent(match[1]);
          }
        }

        // Fallback para query param (?token=...)
        if (!token) {
          token = this.route.snapshot.queryParamMap.get('token');
        }

        // Se encontrou token, troca por sessão HttpOnly e remove fragmento da URL
        if (token) {
          try {
            window.history.replaceState(null, '', window.location.pathname);
          } catch {
            // Ignora erro de histórico se ambiente restrito
          }

          await this.api.getCsrf();
          await this.api.createCustomerSession(token);
        }

        // 2. Carrega a projeção pública da ordem
        await this.loadOrderProjection();
      } catch (err: any) {
        if (err.status === 404) {
          this.isExpired.set(true);
        } else {
          const problem = ApiClient.extractProblem(err);
          this.errorMessage.set(
            problem?.detail ||
              'Não foi possível carregar as informações do seu equipamento no momento.'
          );
        }
      } finally {
        this.isLoading.set(false);
        this.initPromise = null;
      }
    })();

    return this.initPromise;
  }

  public async loadOrderProjection(): Promise<void> {
    try {
      const projection = await this.api.getCustomerOrder();
      this.order.set(projection);
    } catch (err: any) {
      if (err.status === 404) {
        this.isExpired.set(true);
      } else {
        throw err;
      }
    }
  }

  public openDecisionConfirm(decision: 'accepted' | 'rejected'): void {
    this.pendingDecision.set(decision);
    this.conflictMessage.set('');
  }

  public cancelDecision(): void {
    this.pendingDecision.set(null);
  }

  public async confirmDecision(): Promise<void> {
    const o = this.order();
    const decision = this.pendingDecision();
    if (!o || !o.currentQuote || !decision) return;

    this.isDeciding.set(true);
    this.conflictMessage.set('');

    try {
      const updated = await this.api.submitCustomerDecision({
        quoteRevision: o.currentQuote.revision,
        decision,
      });
      this.order.set(updated);
      this.pendingDecision.set(null);
      this.successMessage.set(
        decision === 'accepted'
          ? 'Orçamento aprovado com sucesso! A oficina iniciará o reparo em breve.'
          : 'Orçamento rejeitado. A equipe técnica foi notificada.'
      );
    } catch (err: any) {
      if (err.status === 409) {
        this.conflictMessage.set(
          'Esta versão do orçamento foi alterada ou superada pela oficina. Recarregue a página para visualizar a revisão mais recente.'
        );
      } else {
        const problem = ApiClient.extractProblem(err);
        alert(problem?.detail || 'Erro ao processar sua resposta. Tente novamente.');
      }
    } finally {
      this.isDeciding.set(false);
    }
  }

  // --- Formatters ---

  public getStatusLabel(status: OrderStatus): string {
    return ORDER_STATUS_LABELS[status] || status;
  }

  public formatBrl(value: string | number): string {
    const num = typeof value === 'number' ? value : parseFloat(value);
    if (isNaN(num)) return 'R$ 0,00';
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(num);
  }

  public formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    try {
      return new Intl.DateTimeFormat('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }).format(new Date(dateStr));
    } catch {
      return dateStr;
    }
  }
}
