import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { OrderListComponent } from './order-list.component';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';
import { OrdersListResponse, TechnicianSummary } from '../../core/contracts';

describe('OrderListComponent', () => {
  let fixture: ComponentFixture<OrderListComponent>;
  let mockApiClient: Partial<ApiClient>;
  let mockSessionService: Partial<SessionService>;

  beforeEach(async () => {
    mockApiClient = {
      getOrders: vi.fn(),
      getTechnicians: vi.fn().mockResolvedValue([] as TechnicianSummary[]),
    };

    mockSessionService = {
      isReceptionist: vi.fn().mockReturnValue(true) as any,
      isAuthenticated: vi.fn().mockReturnValue(true) as any,
    };

    await TestBed.configureTestingModule({
      imports: [OrderListComponent],
      providers: [
        provideRouter([]),
        { provide: ApiClient, useValue: mockApiClient },
        { provide: SessionService, useValue: mockSessionService },
      ],
    }).compileComponents();
  });

  it('emptyOrdersShowsCreateAction', async () => {
    (mockApiClient.getOrders as any).mockResolvedValue({
      items: [],
      total: 0,
      page: 1,
      pageSize: 20,
    } as OrdersListResponse);

    fixture = TestBed.createComponent(OrderListComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const emptyState = compiled.querySelector('[data-testid="empty-orders-state"]');
    expect(emptyState).toBeTruthy();
    expect(emptyState?.textContent).toContain('Nenhuma ordem de serviço cadastrada');
    const createBtn = compiled.querySelector('[data-testid="create-order-button"]');
    expect(createBtn).toBeTruthy();
    expect(createBtn?.textContent).toContain('Nova ordem');
  });

  it('noMatchesShowsClearFilters', async () => {
    (mockApiClient.getOrders as any).mockResolvedValue({
      items: [],
      total: 0,
      page: 1,
      pageSize: 20,
    } as OrdersListResponse);

    fixture = TestBed.createComponent(OrderListComponent);
    const component = fixture.componentInstance as any;
    if (component.searchQuery) {
      component.searchQuery.set('termo-inexistente');
    }
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const noMatchesState = compiled.querySelector('[data-testid="no-matches-state"]');
    expect(noMatchesState).toBeTruthy();
    expect(noMatchesState?.textContent).toContain('Nenhuma ordem encontrada para os filtros aplicados');
    const clearBtn = compiled.querySelector('[data-testid="clear-filters-button"]');
    expect(clearBtn).toBeTruthy();
    expect(clearBtn?.textContent).toContain('Limpar filtros');
  });

  it('apiFailureOffersRetry', async () => {
    (mockApiClient.getOrders as any).mockRejectedValue(new Error('Falha no servidor'));

    fixture = TestBed.createComponent(OrderListComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const errorState = compiled.querySelector('[data-testid="error-state"]');
    expect(errorState).toBeTruthy();
    expect(errorState?.textContent).toContain('Não foi possível carregar as ordens de serviço');
    const retryBtn = compiled.querySelector('[data-testid="retry-button"]');
    expect(retryBtn).toBeTruthy();
    expect(retryBtn?.textContent).toContain('Tentar novamente');
  });
});
