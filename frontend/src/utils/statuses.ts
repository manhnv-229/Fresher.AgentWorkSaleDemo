export interface StatusOption {
  value: string;
  label: string;
}

export const AGENT_STATUSES: StatusOption[] = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Active', label: 'Hoạt động' },
  { value: 'Published', label: 'Đã phát hành' },
  { value: 'Inactive', label: 'Dừng' }
];

export const MEMBER_STATUSES: StatusOption[] = [
  { value: 'Active', label: 'Hoạt động' },
  { value: 'Locked', label: 'Đã khóa' }
];

export function withAllOption(statuses: StatusOption[]): StatusOption[] {
  return [{ value: '', label: 'Tất cả' }, ...statuses];
}

export function getStatusLabel(statusMap: StatusOption[], code: string): string {
  return statusMap.find(s => s.value === code)?.label ?? code;
}

const AGENT_STATUS_LABELS: Record<string, string> = {
  Draft: 'Nháp',
  Active: 'Hoạt động',
  Published: 'Đã phát hành',
  Inactive: 'Dừng',
  Deleted: 'Đã xóa',
  Publish: 'Đã phát hành'
};

export function getAgentStatusLabel(code: string): string {
  return AGENT_STATUS_LABELS[code] ?? code;
}

export function getMemberStatusLabel(code: string): string {
  return getStatusLabel(MEMBER_STATUSES, code);
}
