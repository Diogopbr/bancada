import { defineConfig, devices } from '@playwright/test';
const liveUrl = process.env['BANCADA_LIVE_URL'];

export default defineConfig({
  testDir: './e2e',
  testMatch: 'demo.spec.ts',
  workers: 1,
  timeout: liveUrl ? 120_000 : 30_000,
  expect: { timeout: liveUrl ? 30_000 : 5_000 },
  use: { baseURL: liveUrl || 'http://localhost:5092', trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: liveUrl ? undefined : {
    command: 'dotnet Bancada.Api.dll',
    cwd: '.local/publish',
    url: 'http://localhost:5092/health/ready',
    reuseExistingServer: !process.env['CI'],
    env: { ASPNETCORE_URLS: 'http://localhost:5092', ASPNETCORE_ENVIRONMENT: 'Development', BANCADA_DEMO_MODE: 'true' },
  },
});
