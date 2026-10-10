import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { CreateOrderPayload, TechnicianSummary } from '../../core/contracts';

@Component({
  selector: 'app-order-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './order-create.component.html',
  styleUrl: './order-create.component.scss',
})
export class OrderCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  public readonly technicians = signal<TechnicianSummary[]>([]);
  public readonly isLoadingTechs = signal<boolean>(false);
  public readonly isSubmitting = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');
  public readonly fieldErrors = signal<Record<string, string[]>>({});

  public readonly createForm = this.fb.group({
    customerName: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(120)]],
    customerEmail: ['', [Validators.required, Validators.email]],
    equipmentLabel: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(120)]],
    equipmentSerial: ['', [Validators.maxLength(100)]],
    reportedIssue: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(2000)]],
    technicianId: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.loadTechnicians();
  }

  public async loadTechnicians(): Promise<void> {
    this.isLoadingTechs.set(true);
    try {
      const list = await this.api.getTechnicians();
      this.technicians.set(list || []);
      if (list && list.length > 0 && !this.createForm.get('technicianId')?.value) {
        this.createForm.patchValue({ technicianId: list[0].id });
      }
    } catch {
      this.errorMessage.set('Não foi possível carregar a lista de técnicos.');
    } finally {
      this.isLoadingTechs.set(false);
    }
  }

  public async onSubmit(): Promise<void> {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.fieldErrors.set({});

    const raw = this.createForm.getRawValue();
    const payload: CreateOrderPayload = {
      customerName: raw.customerName?.trim() || '',
      customerEmail: raw.customerEmail?.trim() || '',
      equipmentLabel: raw.equipmentLabel?.trim() || '',
      equipmentSerial: raw.equipmentSerial?.trim() || '',
      reportedIssue: raw.reportedIssue?.trim() || '',
      technicianId: raw.technicianId || '',
    };

    try {
      const created = await this.api.createOrder(payload);
      await this.router.navigate(['/ordens', created.id]);
    } catch (err: any) {
      const problem = ApiClient.extractProblem(err);
      if (problem) {
        this.errorMessage.set(problem.detail || problem.title || 'Erro de validação ao criar a ordem.');
        if (problem.errors) {
          this.fieldErrors.set(problem.errors);
        }
      } else {
        this.errorMessage.set('Falha na comunicação com o servidor ao abrir a ordem de serviço.');
      }
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
