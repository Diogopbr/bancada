import { Routes } from '@angular/router';
import { staffGuard } from './core/staff.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'ordens',
  },
  {
    path: 'entrar',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'ordens',
    canActivate: [staffGuard],
    loadComponent: () =>
      import('./features/orders/order-list.component').then((m) => m.OrderListComponent),
  },
  {
    path: 'ordens/nova',
    canActivate: [staffGuard],
    loadComponent: () =>
      import('./features/orders/order-create.component').then((m) => m.OrderCreateComponent),
  },
  {
    path: 'ordens/:id',
    canActivate: [staffGuard],
    loadComponent: () =>
      import('./features/orders/order-detail.component').then((m) => m.OrderDetailComponent),
  },
  {
    path: 'acompanhar',
    loadComponent: () =>
      import('./features/customer/customer-view.component').then(
        (m) => m.CustomerViewComponent
      ),
  },
  {
    path: '**',
    redirectTo: 'ordens',
  },
];
