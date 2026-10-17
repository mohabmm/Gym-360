/**
 * Generic page helpers shared by the e2e suites. Each helper wraps one
 * recurring interaction or assertion so specs read as scenarios.
 */
import { expect, type Dialog, type Locator, type Page, type Request, type Response } from '@playwright/test';

/** Fixtures destructured by page-only test bodies. */
export type PageFixtures = { page: Page };

/** `isVisible` that resolves to false instead of throwing. */
export async function isShown(locator: Locator, timeout?: number): Promise<boolean> {
  const options = timeout === undefined ? undefined : { timeout };
  return locator.isVisible(options).catch(() => false);
}

/** Asserts the first element matching Playwright `text=` selector is visible. */
export async function expectTextVisible(page: Page, text: string, timeout = 8000): Promise<void> {
  await expect(page.locator(`text=${text}`).first()).toBeVisible({ timeout });
}

/** Asserts the first heading (h1 or h2) renders. */
export async function expectHeadingVisible(page: Page): Promise<void> {
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 8000 });
}

/** Asserts the page body rendered something (i.e. no blank crash). */
export async function expectBodyNotEmpty(page: Page): Promise<void> {
  await expect(page.locator('body')).not.toBeEmpty();
}

/** Returns the body text content. */
export async function bodyText(page: Page): Promise<string | null> {
  return page.locator('body').textContent();
}

/** Clicks the Cancel button. */
export async function clickCancel(page: Page): Promise<void> {
  await page.locator('button:has-text("Cancel")').click();
}

/** Clicks Cancel only when it shows up within a second. */
export async function cancelIfShown(page: Page): Promise<void> {
  const cancelBtn = page.locator('button:has-text("Cancel")');
  if (await isShown(cancelBtn, 1000)) await cancelBtn.click();
}

export type DialogWatch = { fired: boolean };

/**
 * Records whether a native dialog (alert/confirm) fires and dismisses it.
 * `logLabel` logs the dialog message; `persistent` keeps listening after the first.
 */
export function watchDialog(page: Page, logLabel?: string, persistent = false): DialogWatch {
  const watch: DialogWatch = { fired: false };
  const handler = (dialog: Dialog) => {
    watch.fired = true;
    if (logLabel) console.log(logLabel, dialog.message());
    dialog.dismiss();
  };
  if (persistent) page.on('dialog', handler);
  else page.once('dialog', handler);
  return watch;
}

export type NoDialogCheck = {
  /** Element whose click must not raise a native dialog. */
  target: Locator;
  /** Settle time after the click, in ms. */
  waitMs: number;
  /** Prefix logged with the dialog message when it fires. */
  logLabel?: string;
  /** Extra message logged when the dialog fired. */
  bugMessage?: string;
};

/** Clicks `target` and asserts no native alert/confirm was raised. */
export async function expectClickWithoutDialog(page: Page, check: NoDialogCheck): Promise<void> {
  const dialog = watchDialog(page, check.logLabel);
  await check.target.click();
  await page.waitForTimeout(check.waitMs);
  if (dialog.fired && check.bugMessage) console.log(check.bugMessage);
  expect(dialog.fired).toBe(false);
}

export type RequestWatch = { seen: boolean };

/** Flags whether any request matching `matches` is sent. */
export function watchRequest(page: Page, matches: (req: Request) => boolean): RequestWatch {
  const watch: RequestWatch = { seen: false };
  page.on('request', (req) => {
    if (matches(req)) watch.seen = true;
  });
  return watch;
}

/** Collects the URLs of requests matching `matches`. */
export function collectRequestUrls(page: Page, matches: (req: Request) => boolean): string[] {
  const urls: string[] = [];
  page.on('request', (req) => {
    if (matches(req)) urls.push(req.url());
  });
  return urls;
}

/** True when the request uses one of `methods` and its URL contains `urlPart`. */
export function isMutation(req: Request, methods: string[], urlPart: string): boolean {
  return methods.includes(req.method()) && req.url().includes(urlPart);
}

/** Collects uncaught page errors (pageerror events). */
export function collectPageErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('pageerror', (err) => errors.push(err.message));
  return errors;
}

/** Collects `"<status> <url>"` for every 5xx response. */
export function collectServerErrors(page: Page): string[] {
  const serverErrors: string[] = [];
  page.on('response', (res: Response) => {
    if (res.status() >= 500) serverErrors.push(`${res.status()} ${res.url()}`);
  });
  return serverErrors;
}

/** Asserts a collected error list is empty, logging it first when not. */
export function expectNoErrors(errors: string[], logLabel: string): void {
  if (errors.length > 0) console.log(logLabel, errors);
  expect(errors.length).toBe(0);
}

/** Fills the email/password login form and submits it. */
export async function submitLoginForm(page: Page, email: string, password: string): Promise<void> {
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
}
