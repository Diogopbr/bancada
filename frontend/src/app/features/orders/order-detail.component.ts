import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';
import {
  CustomerAccessResponse,
  OrderDetail,
  OrderStatus,
  ORDER_STATUS_LABELS,
  QuoteItem,
  QuoteItemKind,
} from '../../core/contracts';

interface FormQuoteItem {
  description: string;
  kind: QuoteItemKind;
  quantity: number;
  unitPrice: string;
}

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss',
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClient);
  public readonly sessionService = inject(SessionService);

  public readonly order = signal<OrderDetail | null>(null);
  public readonly isLoading = signal<boolean>(true);
  public readonly hasError = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');
  public readonly conflictMessage = signal<string>('');

  // Diagnosis editing
  public readonly isEditingDiagnosis = signal<boolean>(false);
  public readonly diagnosisText = signal<string>('');
  public readonly isSavingDiagnosis = signal<boolean>(false);

  // Quote editing
  public readonly isEditingQuote = signal<boolean>(false);
  public readonly quoteItems = signal<FormQuoteItem[]>([]);
  public readonly isPublishingQuote = signal<boolean>(false);
  public readonly quoteValidation = signal('');

  // Customer access link
  public readonly customerAccess = signal<CustomerAccessResponse | null>(null);
  public readonly isGeneratingAccess = signal<boolean>(false);
  public readonly copyFeedback = signal<string>('');

  // Cancel modal
  public readonly showCancelModal = signal<boolean>(false);
  public readonly cancelReason = signal<string>('');
  public readonly isCancelling = signal<boolean>(false);

  // General transition
  public readonly isTransitioning = signal<boolean>(false);

  public readonly orderId = computed(() => this.route.snapshot.paramMap.get('id') || '');

  public readonly canEditDiagnosis = computed(() => {
    const o = this.order();
    if (!o) return false;
    const isTech = this.sessionService.isTechnician();
    const isAssigned = o.technicianId === this.sessionService.session()?.id;
    return isTech && isAssigned && (o.status === 'received' || o.status === 'diagnosing');
  });

  public readonly canPublishQuote = computed(() => {
    const o = this.order();
    if (!o) return false;
    const isTech = this.sessionService.isTechnician();
    const isAssigned = o.technicianId === this.sessionService.session()?.id;
    const validStatus = ['diagnosing', 'awaitingApproval', 'approved'].includes(o.status);
    return isTech && isAssigned && validStatus && Boolean(o.diagnosis?.trim());
  });

  public readonly canStartRepair = computed(() => {
    const o = this.order();
    if (!o) return false;
    const isTech = this.sessionService.isTechnician();
    const isAssigned = o.technicianId === this.sessionService.session()?.id;
    return isTech && isAssigned && o.status === 'approved';
  });

  public readonly canCompleteRepair = computed(() => {
    const o = this.order();
    if (!o) return false;
    const isTech = this.sessionService.isTechnician();
    const isAssigned = o.technicianId === this.sessionService.session()?.id;
    return isTech && isAssigned && o.status === 'repairing';
  });

  public readonly canDeliver = computed(() => {
    const o = this.order();
    if (!o) return false;
    return this.sessionService.isReceptionist() && o.status === 'ready';
  });

  public readonly canCancel = computed(() => {
    const o = this.order();
    if (!o) return false;
    const preRepairStatuses = ['received', 'diagnosing', 'awaitingApproval', 'approved'];
    return this.sessionService.isReceptionist() && preRepairStatuses.includes(o.status);
  });

  ngOnInit(): void {
    this.loadOrder();
  }

  public async loadOrder(): Promise<void> {
    const id = this.orderId();
    if (!id) return;

    this.isLoading.set(true);
    this.hasError.set(false);
    this.errorMessage.set('');
    this.conflictMessage.set('');

    try {
      const data = await this.api.getOrder(id);
      this.order.set(data);
      this.diagnosisText.set(data.diagnosis || '');
    } catch (err: any) {
      this.hasError.set(true);
      const problem = ApiClient.extractProblem(err);
      this.errorMessage.set(
        problem?.detail || 'Não foi possível carregar os dados desta ordem de serviço.'
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  // --- Diagnosis ---

  public startEditingDiagnosis(): void {
    this.diagnosisText.set(this.order()?.diagnosis || '');
    this.isEditingDiagnosis.set(true);
  }

  public cancelEditingDiagnosis(): void {
    this.diagnosisText.set(this.order()?.diagnosis || '');
    this.isEditingDiagnosis.set(false);
  }

  public async saveDiagnosis(): Promise<void> {
    const o = this.order();
    const text = this.diagnosisText().trim();
    if (!o || !text) return;

    this.isSavingDiagnosis.set(true);
    this.conflictMessage.set('');

    try {
      const updated = await this.api.updateDiagnosis(o.id, {
        expectedVersion: o.version,
        diagnosis: text,
      });
      this.order.set(updated);
      this.isEditingDiagnosis.set(false);
    } catch (err: any) {
      this.handleActionError(err);
    } finally {
      this.isSavingDiagnosis.set(false);
    }
  }

  // --- Quote ---

  public startEditingQuote(): void {
    const current = this.order()?.currentQuote;
    if (current && current.items.length > 0) {
      this.quoteItems.set(
        current.items.map((i) => ({
          description: i.description,
          kind: i.kind,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        }))
      );
    } else {
      this.quoteItems.set([
        { description: 'Mão de obra de reparo técnico', kind: 'labor', quantity: 1, unitPrice: '150.00' },
      ]);
    }
    this.isEditingQuote.set(true);
  }

  public cancelEditingQuote(): void {
    this.isEditingQuote.set(false);
  }

  public addQuoteItem(): void {
    this.quoteItems.update((items) => [
      ...items,
      { description: '', kind: 'part', quantity: 1, unitPrice: '0.00' },
    ]);
  }

  public removeQuoteItem(index: number): void {
    this.quoteItems.update((items) => items.filter((_, i) => i !== index));
  }

  public async publishQuote(): Promise<void> {
    const o = this.order();
    const items = this.quoteItems();
    if (!o || items.length === 0) return;
    this.quoteValidation.set('');

    // Validação local
    for (const item of items) {
      if (!item.description.trim()) {
        this.quoteValidation.set('Todos os itens do orçamento precisam ter uma descrição.');
        return;
      }
      if (!Number.isInteger(item.quantity) || item.quantity < 1 || item.quantity > 999) {
        this.quoteValidation.set('A quantidade deve ser um número inteiro entre 1 e 999.');
        return;
      }
      if (!/^\d{1,6}(\.\d{1,2})?$/.test(item.unitPrice)) {
        this.quoteValidation.set('Informe o preço com ponto decimal e até duas casas, entre 0 e 999999.99.');
        return;
      }
    }

    this.isPublishingQuote.set(true);
    this.conflictMessage.set('');

    const payloadItems: QuoteItem[] = items.map((i) => ({
      description: i.description.trim(),
      kind: i.kind,
      quantity: i.quantity,
      unitPrice: i.unitPrice,
    }));

    try {
      const updated = await this.api.publishQuote(o.id, {
        expectedVersion: o.version,
        items: payloadItems,
      });
      this.order.set(updated);
      this.isEditingQuote.set(false);
    } catch (err: any) {
      this.handleActionError(err);
    } finally {
      this.isPublishingQuote.set(false);
    }
  }

  // --- Transitions ---

  public async transition(target: OrderStatus): Promise<void> {
    const o = this.order();
    if (!o) return;

    this.isTransitioning.set(true);
    this.conflictMessage.set('');

    try {
      const updated = await this.api.transitionOrder(o.id, {
        expectedVersion: o.version,
        target,
      });
      this.order.set(updated);
    } catch (err: any) {
      this.handleActionError(err);
    } finally {
      this.isTransitioning.set(false);
    }
  }

  public openCancelModal(): void {
    this.cancelReason.set('');
    this.showCancelModal.set(true);
  }

  public closeCancelModal(): void {
    this.showCancelModal.set(false);
  }

  public async confirmCancel(): Promise<void> {
    const o = this.order();
    const reason = this.cancelReason().trim();
    if (!o || !reason) return;

    this.isCancelling.set(true);
    this.conflictMessage.set('');

    try {
      const updated = await this.api.transitionOrder(o.id, {
        expectedVersion: o.version,
        target: 'cancelled',
        reason,
      });
      this.order.set(updated);
      this.closeCancelModal();
    } catch (err: any) {
      this.handleActionError(err);
    } finally {
      this.isCancelling.set(false);
    }
  }

  // --- Customer Access ---

  public async generateCustomerAccess(): Promise<void> {
    const o = this.order();
    if (!o) return;

    this.isGeneratingAccess.set(true);
    this.conflictMessage.set('');

    try {
      const res = await this.api.createCustomerAccess(o.id, o.version);
      this.customerAccess.set(res);
      this.order.set(await this.api.getOrder(o.id));
      this.copyFeedback.set('');
    } catch (err: any) {
      this.handleActionError(err);
    } finally {
      this.isGeneratingAccess.set(false);
    }
  }

  public async revokeCustomerAccess(): Promise<void> {
    const o = this.order();
    if (!o) return;

    if (!confirm('Deseja realmente revogar o acesso do cliente a esta ordem? O link atual deixará de funcionar.')) {
      return;
    }

    try {
      await this.api.revokeCustomerAccess(o.id);
      this.customerAccess.set(null);
    } catch (err: any) {
      this.handleActionError(err);
    }
  }

  public async copyLink(url: string): Promise<void> {
    try {
      const fullUrl = url.startsWith('http') ? url : `${window.location.origin}${url}`;
      await navigator.clipboard.writeText(fullUrl);
      this.copyFeedback.set('Link copiado com sucesso!');
      setTimeout(() => this.copyFeedback.set(''), 3000);
    } catch {
      this.copyFeedback.set('Não foi possível copiar automaticamente.');
    }
  }

  // --- Error & Conflict Helper ---

  private handleActionError(err: any): void {
    if (err.status === 409) {
      this.conflictMessage.set(
        'Conflito de versão (409): Os dados desta ordem foram atualizados por outro usuário ou processo. Recarregue a página para continuar.'
      );
    } else {
      const problem = ApiClient.extractProblem(err);
      alert(problem?.detail || 'Ocorreu um erro ao processar a ação. Tente novamente.');
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
