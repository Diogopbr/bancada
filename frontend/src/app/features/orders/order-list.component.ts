import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';
import {
  ORDER_STATUS_LABELS,
  OrderStatus,
  OrderSummary,
  TechnicianSummary,
} from '../../core/contracts';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss',
})
export class OrderListComponent implements OnInit {
  private readonly api = inject(ApiClient);
  public readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);

  public readonly orders = signal<OrderSummary[]>([]);
  public readonly total = signal<number>(0);
  public readonly page = signal<number>(1);
  public readonly pageSize = signal<number>(20);

  public readonly searchQuery = signal<string>('');
  public readonly statusFilter = signal<string>('');
  public readonly technicianFilter = signal<string>('');
  public readonly technicians = signal<TechnicianSummary[]>([]);

  public readonly isLoading = signal<boolean>(false);
  public readonly hasError = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');

  public readonly hasActiveFilters = computed(
    () =>
      Boolean(this.searchQuery().trim()) ||
      Boolean(this.statusFilter()) ||
      Boolean(this.technicianFilter())
  );

  public readonly isEmpty = computed(
    () =>
      !this.isLoading() &&
      !this.hasError() &&
      this.orders().length === 0 &&
      !this.hasActiveFilters()
  );

  public readonly hasNoMatches = computed(
    () =>
      !this.isLoading() &&
      !this.hasError() &&
      this.orders().length === 0 &&
      this.hasActiveFilters()
  );

  public readonly statusOptions: { value: OrderStatus; label: string }[] = [
    { value: 'received', label: 'Recebida' },
    { value: 'diagnosing', label: 'Em diagnóstico' },
    { value: 'awaitingApproval', label: 'Aguardando aprovação' },
    { value: 'approved', label: 'Aprovada' },
    { value: 'repairing', label: 'Em reparo' },
    { value: 'ready', label: 'Pronta para retirada' },
    { value: 'delivered', label: 'Entregue' },
    { value: 'cancelled', label: 'Cancelada' },
  ];

  ngOnInit(): void {
    this.loadTechnicians();
    this.loadOrders();
  }

  public async loadTechnicians(): Promise<void> {
    try {
      const list = await this.api.getTechnicians();
      this.technicians.set(list || []);
    } catch {
      // Ignora falha de busca de técnicos no filtro
    }
  }

  public async loadOrders(): Promise<void> {
    this.isLoading.set(true);
    this.hasError.set(false);
    this.errorMessage.set('');

    try {
      const res = await this.api.getOrders({
        search: this.searchQuery().trim() || undefined,
        status: this.statusFilter() || undefined,
        technicianId: this.technicianFilter() || undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      });
      this.orders.set(res.items || []);
      this.total.set(res.total || 0);
    } catch (err: any) {
      this.hasError.set(true);
      const problem = ApiClient.extractProblem(err);
      this.errorMessage.set(
        problem?.detail ||
          'Não foi possível carregar as ordens de serviço. Verifique a conexão com o servidor.'
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  public onFilterChange(): void {
    this.page.set(1);
    this.loadOrders();
  }

  public clearFilters(): void {
    this.searchQuery.set('');
    this.statusFilter.set('');
    this.technicianFilter.set('');
    this.page.set(1);
    this.loadOrders();
  }

  public navigateToCreate(): void {
    this.router.navigate(['/ordens/nova']);
  }

  public getStatusLabel(status: OrderStatus): string {
    return ORDER_STATUS_LABELS[status] || status;
  }

  public formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }).format(d);
    } catch {
      return dateStr;
    }
  }
}
