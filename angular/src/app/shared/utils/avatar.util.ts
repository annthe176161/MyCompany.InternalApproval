// Bảng màu đại diện cố định cho Avatar nhân sự
const AVATAR_PALETTE = [
  '#0284c7',
  '#059669',
  '#7c3aed',
  '#d97706',
  '#dc2626',
  '#0891b2',
  '#4f46e5',
  '#2563eb',
];

// Lấy chữ cái viết tắt từ họ và tên nhân viên (tối đa 2 ký tự)
export function getAvatarInitials(name?: string | null): string {
  if (!name) return 'U';
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
}

// Lấy ký tự viết tắt cuối cùng của tên nhân viên (1 ký tự)
export function getSingleInitial(name?: string | null): string {
  if (!name) return 'U';
  const parts = name.trim().split(/\s+/);
  return parts[parts.length - 1].charAt(0).toUpperCase();
}

// Sinh mã màu ổn định dựa trên chuỗi tên nhân viên
export function getAvatarColor(name?: string | null): string {
  if (!name) return '#475569';
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const index = Math.abs(hash) % AVATAR_PALETTE.length;
  return AVATAR_PALETTE[index];
}

// Xác định nhãn và màu sắc hiển thị theo số tháng thâm niên công tác
export function getTenureBadgeStatus(months: number): { color: string; label: string } {
  if (months < 2) {
    return { color: 'default', label: 'Thử việc' };
  }
  if (months < 12) {
    return { color: 'blue', label: 'Dưới 1 năm' };
  }
  if (months < 36) {
    return { color: 'green', label: '1 - 3 năm' };
  }
  return { color: 'purple', label: 'Kỳ cựu (> 3 năm)' };
}

// Xác định mốc thâm niên chi tiết kèm mã màu tiến trình trong hồ sơ nhân sự
export function getTenureMilestone(months: number): { label: string; color: string } {
  if (months < 2) {
    return { label: 'Thử việc (< 2 tháng)', color: '#8c8c8c' };
  }
  if (months < 12) {
    return { label: 'Nhân viên chính thức (< 1 năm)', color: '#1890ff' };
  }
  if (months < 36) {
    return { label: 'Nhân sự gắn bó (1 - 3 năm)', color: '#52c41a' };
  }
  return { label: 'Nhân sự kỳ cựu (> 3 năm)', color: '#722ed1' };
}

