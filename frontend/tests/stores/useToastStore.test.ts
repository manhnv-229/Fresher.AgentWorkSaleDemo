import { describe, expect, it, vi } from 'vitest';

import { useToastStore } from '../../src/stores/useToastStore';

describe('useToastStore', () => {
  it('should keep only the latest three visible toasts', () => {
    const store = useToastStore();

    store.push({ title: 'Toast 1', tone: 'info' });
    store.push({ title: 'Toast 2', tone: 'success' });
    store.push({ title: 'Toast 3', tone: 'warning' });
    store.push({ title: 'Toast 4', tone: 'error' });

    expect(store.toasts).toHaveLength(3);
    expect(store.visibleToasts.map((item) => item.title)).toEqual(['Toast 4', 'Toast 3', 'Toast 2']);
  });

  it('should auto remove toast after delay', () => {
    vi.useFakeTimers();
    const store = useToastStore();

    const id = store.push({ title: 'Auto close', tone: 'info' });
    expect(store.toasts.some((item) => item.id === id)).toBe(true);

    vi.advanceTimersByTime(5000);

    expect(store.toasts.some((item) => item.id === id)).toBe(false);
  });

  it('should clear timers and toasts when clear is called', () => {
    vi.useFakeTimers();
    const clearTimeoutSpy = vi.spyOn(window, 'clearTimeout');
    const store = useToastStore();

    store.push({ title: 'Toast 1', tone: 'info' });
    store.push({ title: 'Toast 2', tone: 'success' });

    store.clear();

    expect(store.toasts).toEqual([]);
    expect(clearTimeoutSpy).toHaveBeenCalled();
  });
});
