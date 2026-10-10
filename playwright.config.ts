import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  retries: process.env['CI'] ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: 'http://localhost:4200', trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --no-launch-profile --project backend/Bancada.Api',
      url: 'http://localhost:5080/health/ready',
      reuseExistingServer: !process.env['CI'],
      timeout: 90000,
      env: { ASPNETCORE_URLS: 'http://localhost:5080', ASPNETCORE_ENVIRONMENT: 'Development' },
    },
    {
      command: 'npm --prefix frontend start -- --port 4200 --proxy-config proxy.conf.json',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env['CI'],
      timeout: 90000,
    },
  ],
});
