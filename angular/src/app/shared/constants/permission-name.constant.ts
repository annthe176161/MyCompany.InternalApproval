// Bản đồ tên hiển thị tiếng Việt cho các nhóm quyền hệ thống
export const PERMISSION_GROUP_NAME_OVERRIDES: Record<string, string> = {
  AbpIdentity: 'Tài khoản & Phân quyền',
  IdentityManagement: 'Tài khoản & Phân quyền',
  SettingManagement: 'Cài đặt quản lý',
  InternalApproval: 'Quản lý Đề xuất Nội bộ',
};

// Bản đồ tên hiển thị tiếng Việt cho từng quyền hạn cụ thể
export const PERMISSION_NAME_OVERRIDES: Record<string, string> = {
  'AbpIdentity.Roles': 'Quản lý vai trò',
  'AbpIdentity.Roles.Create': 'Thêm mới',
  'AbpIdentity.Roles.Update': 'Chỉnh sửa',
  'AbpIdentity.Roles.Delete': 'Xóa',
  'AbpIdentity.Roles.ManagePermissions': 'Phân quyền',

  'AbpIdentity.Users': 'Quản lý tài khoản',
  'AbpIdentity.Users.Create': 'Thêm mới',
  'AbpIdentity.Users.Update': 'Chỉnh sửa thông tin',
  'AbpIdentity.Users.Update.ManageRoles': 'Gán vai trò',
  'AbpIdentity.Users.Delete': 'Xóa',
  'AbpIdentity.Users.ManagePermissions': 'Phân quyền riêng',
  'AbpIdentity.UserLookup': 'Tra cứu người dùng',

  'Requests.Default': 'Quản lý đề xuất nội bộ',
  'Requests.Create': 'Tạo đề xuất',
  'Requests.Edit': 'Sửa đề xuất',
  'Requests.Delete': 'Xóa đề xuất',
  'Requests.Approve': 'Phê duyệt đề xuất',
  'Requests.Reject': 'Từ chối đề xuất',

  'Departments.Default': 'Quản lý phòng ban',
  'Departments.Create': 'Thêm phòng ban',
  'Departments.Edit': 'Sửa phòng ban',
  'Departments.Delete': 'Xóa phòng ban',
  'Departments.ManageMembers': 'Quản lý thành viên phòng ban',

  'Employees.Default': 'Hồ sơ nhân viên',
  'Employees.ViewAll': 'Xem toàn bộ nhân viên',
  'Employees.Edit': 'Cập nhật hồ sơ',
  'Employees.Analytics': 'Xem thống kê',
};

