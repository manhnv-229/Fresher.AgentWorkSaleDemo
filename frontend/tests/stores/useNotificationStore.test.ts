import { describe, expect, it } from 'vitest';

import { useNotificationStore } from '../../src/stores/useNotificationStore';

describe('useNotificationStore', () => {
  it('should prepend notifications and increment ids', () => {
    const store = useNotificationStore();

    const firstId = store.push({
      title: 'Lưu thành công',
      message: 'Agent đã được cập nhật.',
      tone: 'success'
    });
    const secondId = store.push({
      title: 'Có cảnh báo',
      tone: 'warning'
    });

    expect(firstId).toBe(1);
    expect(secondId).toBe(2);
    expect(store.visibleNotifications).toEqual([
      { id: 2, title: 'Có cảnh báo', tone: 'warning' },
      { id: 1, title: 'Lưu thành công', message: 'Agent đã được cập nhật.', tone: 'success' }
    ]);
  });

  it('should remove one notification or clear all notifications', () => {
    const store = useNotificationStore();

    const firstId = store.push({ title: 'A', tone: 'info' });
    store.push({ title: 'B', tone: 'success' });
    store.remove(firstId);

    expect(store.notifications).toHaveLength(1);
    expect(store.notifications[0].title).toBe('B');

    store.clear();
    expect(store.notifications).toEqual([]);
  });
});
