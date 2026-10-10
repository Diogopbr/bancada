import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { OrderCreateComponent } from './order-create.component';
import { ApiClient } from '../../core/api-client';
import { OrderDetail, TechnicianSummary } from '../../core/contracts';

describe('OrderCreateComponent', () => {
  let fixture: ComponentFixture<OrderCreateComponent>;
  let mockApiClient: Partial<ApiClient>;
  let router: Router;

  const mockTechnicians: TechnicianSummary[] = [
    { id: 'tech-1', displayName: 'Técnico Um' },
    { id: 'tech-2', displayName: 'Técnico Dois' },
  ];

  beforeEach(async () => {
    mockApiClient = {
      getTechnicians: vi.fn().mockResolvedValue(mockTechnicians),
      createOrder: vi.fn().mockResolvedValue({
        id: 'ord-123',
        code: 'OS-2026-0001',
      } as OrderDetail),
    };

    await TestBed.configureTestingModule({
      imports: [OrderCreateComponent],
      providers: [
        provideRouter([]),
        { provide: ApiClient, useValue: mockApiClient },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(OrderCreateComponent);
  });

  it('should load technicians on init and select first technician', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const comp = fixture.componentInstance;
    expect(comp.technicians().length).toBe(2);
    expect(comp.createForm.get('technicianId')?.value).toBe('tech-1');
  });

  it('should validate form and submit order successfully', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const comp = fixture.componentInstance;
    comp.createForm.patchValue({
      customerName: 'Maria Silva',
      customerEmail: 'maria@exemplo.com.br',
      equipmentLabel: 'Smartphone Samsung S22',
      equipmentSerial: 'SM-S901B',
      reportedIssue: 'Tela quebrada após queda',
      technicianId: 'tech-1',
    });

    expect(comp.createForm.valid).toBe(true);

    await comp.onSubmit();

    expect(mockApiClient.createOrder).toHaveBeenCalledWith({
      customerName: 'Maria Silva',
      customerEmail: 'maria@exemplo.com.br',
      equipmentLabel: 'Smartphone Samsung S22',
      equipmentSerial: 'SM-S901B',
      reportedIssue: 'Tela quebrada após queda',
      technicianId: 'tech-1',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/ordens', 'ord-123']);
  });
});
