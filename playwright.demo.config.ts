import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  testMatch: 'demo.spec.ts',
  workers: 1,
  use: { baseURL: 'http://localhost:5092', trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet Bancada.Api.dll',
    cwd: '.local/publish',
    url: 'http://localhost:5092/health/ready',
    reuseExistingServer: !process.env['CI'],
    env: { ASPNETCORE_URLS: 'http://localhost:5092', ASPNETCORE_ENVIRONMENT: 'Development', BANCADA_DEMO_MODE: 'true' },
  },
});
