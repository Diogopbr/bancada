import { Routes } from '@angular/router';

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
    loadComponent: () =>
      import('./features/orders/order-list.component').then((m) => m.OrderListComponent),
  },
  {
    path: 'ordens/nova',
    loadComponent: () =>
      import('./features/orders/order-create.component').then((m) => m.OrderCreateComponent),
  },
  {
    path: 'ordens/:id',
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
