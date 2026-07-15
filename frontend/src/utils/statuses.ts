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

// Tạo bản sao danh sách status và thêm lựa chọn bỏ lọc ở đầu danh sách.
export function withAllOption(statuses: StatusOption[]): StatusOption[] {
  return [{ value: '', label: 'Tất cả' }, ...statuses];
}

// Đổi mã status sang nhãn hiển thị; fallback về code để không làm mất dữ liệu lạ.
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

// Lấy nhãn status riêng cho agent, bao gồm cả trạng thái soft-delete/publish.
export function getAgentStatusLabel(code: string): string {
  return AGENT_STATUS_LABELS[code] ?? code;
}

// Lấy nhãn status theo bộ trạng thái thành viên quản trị.
export function getMemberStatusLabel(code: string): string {
  return getStatusLabel(MEMBER_STATUSES, code);
}
