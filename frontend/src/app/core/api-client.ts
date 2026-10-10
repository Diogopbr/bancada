import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  CreateOrderPayload,
  CustomerAccessResponse,
  CustomerDecisionPayload,
  CustomerOrderProjection,
  OrderDetail,
  OrdersListResponse,
  PublishQuotePayload,
  SessionInfo,
  SetDiagnosisPayload,
  TechnicianSummary,
  TransitionPayload,
  ProblemDetails,
  DemoRole,
  DemoStatus,
} from './contracts';

@Injectable({
  providedIn: 'root',
})
export class ApiClient {
  private readonly http = inject(HttpClient);
  private csrfToken: string | null = null;
  private readonly baseUrl = '/api';

  public async getCsrf(): Promise<string> {
    const res = await firstValueFrom(
      this.http.get<{ token: string }>(`${this.baseUrl}/csrf`, {
        withCredentials: true,
      })
    );
    this.csrfToken = res.token;
    return res.token;
  }

  public setCsrfToken(token: string): void {
    this.csrfToken = token;
  }

  public getCachedCsrfToken(): string | null {
    return this.csrfToken;
  }

  private async getWriteHeaders(): Promise<HttpHeaders> {
    // Another tab may have changed the staff identity since the previous write.
    const token = await this.getCsrf();
    return new HttpHeaders().set('X-CSRF-TOKEN', token);
  }

  // --- Session Endpoints ---

  public async getSession(): Promise<SessionInfo> {
    return firstValueFrom(
      this.http.get<SessionInfo>(`${this.baseUrl}/session`, {
        withCredentials: true,
      })
    );
  }

  public async login(credentials: { email: string; password: string }): Promise<SessionInfo> {
    const headers = await this.getWriteHeaders();
    const session = await firstValueFrom(
      this.http.post<SessionInfo>(`${this.baseUrl}/login`, credentials, {
        headers,
        withCredentials: true,
      })
    );
    this.csrfToken = null;
    return session;
  }

  public async logout(): Promise<void> {
    const headers = await this.getWriteHeaders();
    await firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/logout`, {}, {
        headers,
        withCredentials: true,
      })
    );
    this.csrfToken = null;
  }

  // --- Demo Endpoints ---

  public async getDemo(): Promise<DemoStatus> {
    return firstValueFrom(
      this.http.get<DemoStatus>(`${this.baseUrl}/demo`, {
        withCredentials: true,
      })
    );
  }

  public async startDemo(): Promise<SessionInfo> {
    const headers = await this.getWriteHeaders();
    const session = await firstValueFrom(
      this.http.post<SessionInfo>(`${this.baseUrl}/demo/start`, {}, {
        headers,
        withCredentials: true,
      })
    );
    this.csrfToken = null;
    return session;
  }

  public async switchDemoRole(role: DemoRole): Promise<SessionInfo> {
    const headers = await this.getWriteHeaders();
    const session = await firstValueFrom(
      this.http.post<SessionInfo>(`${this.baseUrl}/demo/role`, { role }, {
        headers,
        withCredentials: true,
      })
    );
    this.csrfToken = null;
    return session;
  }

  public async resetDemo(): Promise<SessionInfo> {
    const headers = await this.getWriteHeaders();
    const session = await firstValueFrom(
      this.http.post<SessionInfo>(`${this.baseUrl}/demo/reset`, {}, {
        headers,
        withCredentials: true,
      })
    );
    this.csrfToken = null;
    return session;
  }

  public async getTechnicians(): Promise<TechnicianSummary[]> {
    return firstValueFrom(
      this.http.get<TechnicianSummary[]>(`${this.baseUrl}/technicians`, {
        withCredentials: true,
      })
    );
  }

  // --- Orders Endpoints ---

  public async getOrders(params: {
    search?: string;
    status?: string;
    technicianId?: string;
    page?: number;
    pageSize?: number;
  }): Promise<OrdersListResponse> {
    let httpParams = new HttpParams();
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.technicianId) httpParams = httpParams.set('technicianId', params.technicianId);
    if (params.page !== undefined) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize !== undefined) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return firstValueFrom(
      this.http.get<OrdersListResponse>(`${this.baseUrl}/orders`, {
        params: httpParams,
        withCredentials: true,
      })
    );
  }

  public async createOrder(payload: CreateOrderPayload): Promise<OrderDetail> {
    const headers = await this.getWriteHeaders();
    return firstValueFrom(
      this.http.post<OrderDetail>(`${this.baseUrl}/orders`, payload, {
        headers,
        withCredentials: true,
      })
    );
  }

  public async getOrder(id: string): Promise<OrderDetail> {
    return firstValueFrom(
      this.http.get<OrderDetail>(`${this.baseUrl}/orders/${encodeURIComponent(id)}`, {
        withCredentials: true,
      })
    );
  }

  public async updateDiagnosis(id: string, payload: SetDiagnosisPayload): Promise<OrderDetail> {
    const headers = await this.getWriteHeaders();
    return firstValueFrom(
      this.http.post<OrderDetail>(`${this.baseUrl}/orders/${encodeURIComponent(id)}/diagnosis`, payload, {
        headers,
        withCredentials: true,
      })
    );
  }

  public async transitionOrder(id: string, payload: TransitionPayload): Promise<OrderDetail> {
    const headers = await this.getWriteHeaders();
    return firstValueFrom(
      this.http.post<OrderDetail>(`${this.baseUrl}/orders/${encodeURIComponent(id)}/transitions`, payload, {
        headers,
        withCredentials: true,
      })
    );
  }

  public async publishQuote(id: string, payload: PublishQuotePayload): Promise<OrderDetail> {
    const headers = await this.getWriteHeaders();
    return firstValueFrom(
      this.http.post<OrderDetail>(`${this.baseUrl}/orders/${encodeURIComponent(id)}/quotes`, payload, {
        headers,
        withCredentials: true,
      })
    );
  }

  public async createCustomerAccess(id: string, expectedVersion: number): Promise<CustomerAccessResponse> {
    const headers = await this.getWriteHeaders();
    return firstValueFrom(
      this.http.post<CustomerAccessResponse>(
        `${this.baseUrl}/orders/${encodeURIComponent(id)}/customer-access`,
        { expectedVersion },
        {
          headers,
          withCredentials: true,
        }
      )
    );
  }

  public async revokeCustomerAccess(id: string): Promise<void> {
    const headers = await this.getWriteHeaders();
    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/orders/${encodeURIComponent(id)}/customer-access`, {
        headers,
        withCredentials: true,
      })
    );
  }

  // --- Customer Endpoints ---

  public async createCustomerSession(token: string): Promise<void> {
    const headers = await this.getWriteHeaders();
    await firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/customer/session`, { token }, {
        headers,
        withCredentials: true,
      })
    );
  }

  public async getCustomerOrder(): Promise<CustomerOrderProjection> {
    return firstValueFrom(
      this.http.get<CustomerOrderProjection>(`${this.baseUrl}/customer/order`, {
        withCredentials: true,
      })
    );
  }

  public async submitCustomerDecision(payload: CustomerDecisionPayload): Promise<CustomerOrderProjection> {
    const headers = await this.getWriteHeaders();
    return firstValueFrom(
      this.http.post<CustomerOrderProjection>(`${this.baseUrl}/customer/decision`, payload, {
        headers,
        withCredentials: true,
      })
    );
  }

  // Helper to extract problem details
  public static extractProblem(error: unknown): ProblemDetails | null {
    if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
      return error.error as ProblemDetails;
    }
    return null;
  }
}
