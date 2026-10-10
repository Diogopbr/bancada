export type StaffRole = 'receptionist' | 'technician';

export interface SessionInfo {
  id: string;
  displayName: string;
  role: StaffRole;
}

export interface TechnicianSummary {
  id: string;
  displayName: string;
}

export type OrderStatus =
  | 'received'
  | 'diagnosing'
  | 'awaitingApproval'
  | 'approved'
  | 'repairing'
  | 'ready'
  | 'delivered'
  | 'cancelled';

export type QuoteItemKind = 'part' | 'labor';

export interface QuoteItem {
  description: string;
  kind: QuoteItemKind;
  quantity: number;
  unitPrice: string; // decimal string with two decimals, e.g. "120.00"
}

export type QuoteDecision = 'accepted' | 'rejected' | null;

export interface Quote {
  revision: number;
  items: QuoteItem[];
  total: string;
  publishedAt: string;
  decision: QuoteDecision;
}

export interface HistoryEvent {
  id: string;
  action: string;
  actorName: string;
  createdAt: string;
  detail: string;
}

export interface OrderSummary {
  id: string;
  code: string;
  customerName: string;
  equipmentLabel: string;
  status: OrderStatus;
  technicianId: string;
  technicianName: string;
  updatedAt: string;
  version: number;
}

export interface OrderDetail extends OrderSummary {
  customerEmail: string;
  equipmentSerial: string;
  reportedIssue: string;
  diagnosis: string | null;
  currentQuote: Quote | null;
  history: HistoryEvent[];
}

export interface OrdersListResponse {
  items: OrderSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateOrderPayload {
  customerName: string;
  customerEmail: string;
  equipmentLabel: string;
  equipmentSerial: string;
  reportedIssue: string;
  technicianId: string;
}

export interface SetDiagnosisPayload {
  expectedVersion: number;
  diagnosis: string;
}

export interface TransitionPayload {
  expectedVersion: number;
  target: OrderStatus;
  reason?: string;
}

export interface PublishQuotePayload {
  expectedVersion: number;
  items: QuoteItem[];
}

export interface CustomerAccessResponse {
  url: string;
  expiresAt: string;
}

export interface CustomerOrderProjection {
  code: string;
  equipmentLabel: string;
  status: OrderStatus;
  version: number;
  currentQuote: Quote | null;
  history: HistoryEvent[];
}

export interface CustomerDecisionPayload {
  quoteRevision: number;
  decision: 'accepted' | 'rejected';
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status: number;
  code?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  received: 'Recebida',
  diagnosing: 'Em diagnóstico',
  awaitingApproval: 'Aguardando aprovação',
  approved: 'Aprovada',
  repairing: 'Em reparo',
  ready: 'Pronta para retirada',
  delivered: 'Entregue',
  cancelled: 'Cancelada',
};

export const STAFF_ROLE_LABELS: Record<StaffRole, string> = {
  receptionist: 'Atendente',
  technician: 'Técnico',
};
