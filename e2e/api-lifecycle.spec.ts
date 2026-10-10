import { test, expect, request, type APIRequestContext } from '@playwright/test';

async function csrf(client: APIRequestContext) {
  const response = await client.get('/api/csrf');
  expect(response.status()).toBe(200);
  return { 'X-CSRF-TOKEN': (await response.json()).token as string };
}

async function staff(email: string) {
  const password = process.env['BANCADA_DEMO_PASSWORD'];
  if (!password) throw new Error('BANCADA_DEMO_PASSWORD is required for real API journey tests.');
  const client = await request.newContext({ baseURL: 'http://localhost:5080' });
  const login = await client.post('/api/login', {
    headers: await csrf(client), data: { email, password },
  });
  expect(login.status()).toBe(200);
  return client;
}

test('real API persists the complete repair journey and rejects duplicate updates', async () => {
  const receptionist = await staff('atendente@bancada.example');
  const technician = await staff('tecnico1@bancada.example');
  const customer = await request.newContext({ baseURL: 'http://localhost:5080' });
  try {
    const technicians = await (await receptionist.get('/api/technicians')).json();
    const assigned = technicians.find((item: { displayName: string }) => /um|1/i.test(item.displayName)) ?? technicians[0];
    expect(assigned).toBeTruthy();
    const created = await receptionist.post('/api/orders', {
      headers: await csrf(receptionist), data: {
        customerName: 'Pessoa Fictícia de Verificação', customerEmail: 'verificacao@bancada.example',
        equipmentLabel: 'Notebook de demonstração', equipmentSerial: 'FICTICIO-E2E',
        reportedIssue: 'Não liga após queda de energia', technicianId: assigned.id,
      },
    });
    expect(created.status()).toBe(201);
    let order = await created.json();
    const path = `/api/orders/${order.id}`;
    const diagnostic = await technician.post(`${path}/diagnosis`, {
      headers: await csrf(technician), data: { expectedVersion: order.version, diagnosis: 'Fonte danificada; troca necessária.' },
    });
    expect(diagnostic.status()).toBe(200);
    order = await diagnostic.json();
    const quoted = await technician.post(`${path}/quotes`, {
      headers: await csrf(technician), data: { expectedVersion: order.version, items: [
        { description: 'Fonte de alimentação', kind: 'part', quantity: 1, unitPrice: '190.00' },
        { description: 'Diagnóstico e instalação', kind: 'labor', quantity: 1, unitPrice: '85.50' },
      ] },
    });
    expect(quoted.status()).toBe(200);
    order = await quoted.json();
    expect(order.currentQuote.total).toBe('275.50');
    const access = await receptionist.post(`${path}/customer-access`, {
      headers: await csrf(receptionist), data: { expectedVersion: order.version },
    });
    expect(access.ok()).toBeTruthy();
    const link = new URL((await access.json()).url, 'http://localhost:4200');
    const token = new URLSearchParams(link.hash.slice(1)).get('token');
    expect(token).toBeTruthy();
    const exchange = await customer.post('/api/customer/session', {
      headers: await csrf(customer), data: { token },
    });
    expect(exchange.status()).toBe(204);
    const publicOrder = await (await customer.get('/api/customer/order')).json();
    expect(publicOrder.customerEmail).toBeUndefined();
    expect(publicOrder.diagnosis).toBeUndefined();
    const decision = { quoteRevision: order.currentQuote.revision, decision: 'accepted' };
    const accepted = await customer.post('/api/customer/decision', { headers: await csrf(customer), data: decision });
    expect(accepted.status()).toBe(200);
    const historyCount = (await accepted.json()).history.length;
    const repeated = await customer.post('/api/customer/decision', { headers: await csrf(customer), data: decision });
    expect(repeated.status()).toBe(200);
    expect((await repeated.json()).history.length).toBe(historyCount);
    order = await (await receptionist.get(path)).json();
    expect(order.status).toBe('approved');
    const headers = await csrf(technician);
    const starts = await Promise.all([1, 2].map(() => technician.post(`${path}/transitions`, {
      headers, data: { expectedVersion: order.version, target: 'repairing' },
    })));
    expect(starts.map(response => response.status()).sort()).toEqual([200, 409]);
    order = await (await receptionist.get(path)).json();
    const completed = await technician.post(`${path}/transitions`, {
      headers: await csrf(technician), data: { expectedVersion: order.version, target: 'ready' },
    });
    expect(completed.status()).toBe(200);
    order = await completed.json();
    const delivered = await receptionist.post(`${path}/transitions`, {
      headers: await csrf(receptionist), data: { expectedVersion: order.version, target: 'delivered' },
    });
    expect(delivered.status()).toBe(200);
    const final = await delivered.json();
    expect(final.status).toBe('delivered');
    const saved = await (await receptionist.get(path)).json();
    expect(saved.status).toBe('delivered');
    expect(saved.history).toEqual(final.history);
    expect(saved.history.filter((item: { action: string }) => item.action === 'repairStarted')).toHaveLength(1);
  } finally {
    await receptionist.dispose();
    await technician.dispose();
    await customer.dispose();
  }
});
