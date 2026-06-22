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

export const ALL_MEMBER_STATUSES: StatusOption[] = [
  { value: '', label: 'Tất cả' },
  ...MEMBER_STATUSES
];

export const ALL_AGENT_STATUSES: StatusOption[] = [
  { value: '', label: 'Tất cả' },
  ...AGENT_STATUSES
];

export function getStatusLabel(statusMap: StatusOption[], code: string): string {
  return statusMap.find(s => s.value === code)?.label ?? code;
}

export function getAgentStatusLabel(code: string): string {
  return getStatusLabel(AGENT_STATUSES, code);
}

export function getMemberStatusLabel(code: string): string {
  return getStatusLabel(MEMBER_STATUSES, code);
}
