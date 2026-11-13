import { test, expect } from '@playwright/test';
import { isReceptionistPathAllowed } from '../receptionist-paths';

test('receptionist can open the trainers list to add a coach', () => {
  expect(isReceptionistPathAllowed('/trainers')).toBe(true);
});

test('receptionist cannot open trainer detail pages', () => {
  expect(isReceptionistPathAllowed('/trainers/abc')).toBe(false);
});

test('receptionist keeps the desk pages and stays out of manager pages', () => {
  for (const path of ['/', '/members', '/members/abc', '/check-ins', '/billing', '/pt/trials']) {
    expect(isReceptionistPathAllowed(path), path).toBe(true);
  }
  for (const path of ['/staff', '/settings', '/analytics', '/pt/dashboard', '/trainersX']) {
    expect(isReceptionistPathAllowed(path), path).toBe(false);
  }
});
