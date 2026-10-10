import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { OrderDetailComponent } from './order-detail.component';
import { ApiClient } from '../../core/api-client';
import { SessionService } from '../../core/session.service';
import { OrderDetail } from '../../core/contracts';

describe('OrderDetailComponent', () => {
  let fixture: ComponentFixture<OrderDetailComponent>;
  let mockApiClient: Partial<ApiClient>;
  let mockSessionService: Partial<SessionService>;
  let isReceptionistSig: ReturnType<typeof signal<boolean>>;
  let isTechnicianSig: ReturnType<typeof signal<boolean>>;

  const sampleOrder: OrderDetail = {
    id: 'ord-abc',
    code: 'OS-2026-0042',
    customerName: 'Roberto Santos',
    customerEmail: 'roberto@exemplo.com.br',
    equipmentLabel: 'Console PlayStation 5',
    equipmentSerial: 'PS5-998877',
    reportedIssue: 'Superaquecimento durante jogos pesados',
    status: 'approved',
    technicianId: 'tech-1',
    technicianName: 'Técnico Especialista',
    updatedAt: '2026-10-10T09:30:00Z',
    version: 3,
    diagnosis: 'Pasta térmica ressecada e cooler obstruído por poeira.',
    currentQuote: {
      revision: 1,
      items: [
        { description: 'Troca de metal líquido / pasta térmica', kind: 'part', quantity: 1, unitPrice: '120.00' },
        { description: 'Limpeza química e desobstrução', kind: 'labor', quantity: 1, unitPrice: '180.00' },
      ],
      total: '300.00',
      publishedAt: '2026-10-10T10:00:00Z',
      decision: 'accepted',
    },
    history: [
      { id: 'h1', action: 'received', actorName: 'Atendente', createdAt: '2026-10-10T08:00:00Z', detail: 'Entrada na oficina' },
      { id: 'h2', action: 'quoteAccepted', actorName: 'Cliente', createdAt: '2026-10-10T11:00:00Z', detail: 'Orçamento aprovado pelo cliente' },
    ],
  };

  beforeEach(async () => {
    mockApiClient = {
      getOrder: vi.fn().mockResolvedValue(sampleOrder),
      transitionOrder: vi.fn().mockResolvedValue({
        ...sampleOrder,
        status: 'repairing',
        version: 4,
      }),
    };

    isReceptionistSig = signal<boolean>(false);
    isTechnicianSig = signal<boolean>(true);

    mockSessionService = {
      session: vi.fn().mockReturnValue({ id: 'tech-1', displayName: 'Técnico Especialista', role: 'technician' }) as any,
      isTechnician: isTechnicianSig as any,
      isReceptionist: isReceptionistSig as any,
      isAuthenticated: vi.fn().mockReturnValue(true) as any,
    };

    await TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ApiClient, useValue: mockApiClient },
        { provide: SessionService, useValue: mockSessionService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'id' ? 'ord-abc' : null),
              },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailComponent);
  });

  it('should load and render order details and current quote', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.order-code')?.textContent).toContain('OS-2026-0042');
    expect(compiled.querySelector('.equipment-headline, .info-value')?.textContent).toContain('Console PlayStation 5');
    expect(compiled.querySelector('.diagnosis-content')?.textContent).toContain('Pasta térmica ressecada');
    expect(compiled.querySelector('.quote-total-value')?.textContent).toContain('300,00');
  });

  it('should allow assigned technician to start repair when order is approved', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    expect(comp.canStartRepair()).toBe(true);

    await comp.transition('repairing');

    expect(mockApiClient.transitionOrder).toHaveBeenCalledWith('ord-abc', {
      expectedVersion: 3,
      target: 'repairing',
    });
    expect(comp.order()?.status).toBe('repairing');
  });

  it('should handle HTTP 409 conflict and display message', async () => {
    (mockApiClient.transitionOrder as any).mockRejectedValue({ status: 409 });

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    await comp.transition('repairing');
    fixture.detectChanges();

    expect(comp.conflictMessage()).toContain('Conflito de versão (409)');
  });

  it.each([
    { quantity: 1, unitPrice: '1.999' },
    { quantity: 1.5, unitPrice: '10.00' },
    { quantity: 1, unitPrice: '1e2' },
  ])('rejects imprecise input without rounding or truncating it: %j', async (input) => {
    fixture.componentInstance.order.set(sampleOrder);
    mockApiClient.publishQuote = vi.fn().mockResolvedValue(sampleOrder);
    const alert = vi.spyOn(window, 'alert').mockImplementation(() => {});
    fixture.componentInstance.quoteItems.set([{ description: 'Peça fictícia', kind: 'part', ...input }]);
    await fixture.componentInstance.publishQuote();
    expect(mockApiClient.publishQuote).not.toHaveBeenCalled();
    alert.mockRestore();
  });

  it('should support keyboard dialog behavior on cancel modal: trap focus, close on Esc and restore focus', async () => {
    isReceptionistSig.set(true);
    sampleOrder.status = 'received';
    fixture.componentInstance.order.set(sampleOrder);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const cancelTrigger = fixture.nativeElement.querySelector('.btn-danger') as HTMLElement;
    expect(cancelTrigger).toBeTruthy();
    cancelTrigger.focus();
    expect(document.activeElement).toBe(cancelTrigger);

    cancelTrigger.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('[role="dialog"]') as HTMLElement;
    expect(dialog).toBeTruthy();
    expect(dialog.getAttribute('aria-labelledby')).toBe('cancel-modal-title');

    // Focus enters dialog
    const focusable = dialog.querySelectorAll('textarea, button');
    expect(Array.from(focusable)).toContain(document.activeElement as HTMLElement);

    // Escape closes dialog and restores focus
    const escEvent = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
    dialog.dispatchEvent(escEvent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.showCancelModal()).toBe(false);
    expect(document.activeElement).toBe(cancelTrigger);
  });
});
