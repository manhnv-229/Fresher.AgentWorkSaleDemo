export interface StatusOption {
  value: string;
  label: string;
}

export const AGENT_STATUSES: StatusOption[] = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Active', label: 'Hoạt động' },
  { value: 'Inactive', label: 'Ngừng hoạt động' }
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
  Inactive: 'Ngừng hoạt động',
  Deleted: 'Đã xóa',
  Publish: 'Đã xuất bản'
};

export function getAgentStatusLabel(code: string): string {
  return AGENT_STATUS_LABELS[code] ?? code;
}

export function getMemberStatusLabel(code: string): string {
  return getStatusLabel(MEMBER_STATUSES, code);
}
