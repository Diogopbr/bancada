import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CustomerViewComponent } from './customer-view.component';
import { ApiClient } from '../../core/api-client';
import { CustomerOrderProjection } from '../../core/contracts';

describe('CustomerViewComponent', () => {
  let fixture: ComponentFixture<CustomerViewComponent>;
  let mockApiClient: Partial<ApiClient>;

  const sampleProjection: CustomerOrderProjection = {
    code: 'OS-2026-0001',
    equipmentLabel: 'Notebook Dell XPS 13',
    status: 'awaitingApproval',
    version: 2,
    currentQuote: {
      revision: 1,
      items: [
        { description: 'Troca da bateria', kind: 'part', quantity: 1, unitPrice: '250.00' },
        { description: 'Serviço de troca', kind: 'labor', quantity: 1, unitPrice: '100.00' },
      ],
      total: '350.00',
      publishedAt: '2026-10-10T10:00:00Z',
      decision: null,
    },
    history: [
      { id: '1', action: 'received', actorName: 'Atendente', createdAt: '2026-10-10T08:00:00Z', detail: 'Ordem recebida na oficina' },
      { id: '2', action: 'quotePublished', actorName: 'Técnico', createdAt: '2026-10-10T10:00:00Z', detail: 'Orçamento publicado' },
    ],
  };

  beforeEach(async () => {
    mockApiClient = {
      getCsrf: vi.fn().mockResolvedValue('test-csrf'),
      createCustomerSession: vi.fn().mockResolvedValue(undefined),
      getCustomerOrder: vi.fn().mockResolvedValue(sampleProjection),
      submitCustomerDecision: vi.fn().mockResolvedValue({
        ...sampleProjection,
        status: 'approved',
        currentQuote: {
          ...sampleProjection.currentQuote!,
          decision: 'accepted',
        },
      }),
    };

    await TestBed.configureTestingModule({
      imports: [CustomerViewComponent],
      providers: [
        provideRouter([]),
        { provide: ApiClient, useValue: mockApiClient },
      ],
    }).compileComponents();
  });

  it('should render customer equipment projection and quote items', async () => {
    fixture = TestBed.createComponent(CustomerViewComponent);
    fixture.detectChanges();
    await fixture.componentInstance.initializeCustomerSession();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.order-code')?.textContent).toContain('OS-2026-0001');
    expect(compiled.querySelector('.equipment-headline')?.textContent).toContain('Notebook Dell XPS 13');
    expect(compiled.querySelector('.total-value')?.textContent).toContain('350,00');
    expect(compiled.querySelector('.btn-accept')).toBeTruthy();
    expect(compiled.querySelector('.btn-reject')).toBeTruthy();
  });

  it('should display expired message when access token returns 404', async () => {
    (mockApiClient.getCustomerOrder as any).mockRejectedValue({ status: 404 });

    fixture = TestBed.createComponent(CustomerViewComponent);
    fixture.detectChanges();
    await fixture.componentInstance.initializeCustomerSession();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.expired-state')).toBeTruthy();
    expect(compiled.querySelector('.expired-state')?.textContent).toContain(
      'Acesso indisponível ou expirado'
    );
  });
});
