/**
 * Authoritative Security & Hierarchical Authorization Oracle
 * MyCompany.InternalApproval - Hierarchical Department Authorization
 * 
 * Formalizes rules from ORIGINAL_REQUEST.md (§ R1 2026-10-22T08:33:47Z) and survey reports.
 */

class AbpAuthorizationException extends Error {
  constructor(message = 'Bạn không có quyền truy cập hồ sơ nhân sự này.') {
    super(message);
    this.name = 'AbpAuthorizationException';
    this.statusCode = 403;
  }
}

class EntityNotFoundException extends Error {
  constructor(entityType = 'IdentityUser', id = null) {
    super(`Entity of type ${entityType} with id ${id} was not found.`);
    this.name = 'EntityNotFoundException';
    this.statusCode = 404;
  }
}

const Permissions = {
  Employees: {
    Default: 'Employees.Default',
    Edit: 'Employees.Edit',
    Analytics: 'Employees.Analytics',
    ViewAll: 'Employees.ViewAll'
  }
};

/**
 * Evaluates the hierarchical access scope of the current user.
 * 
 * Scope levels:
 * 1. Global / Admin: Role 'admin' or has 'Employees.ViewAll' permission.
 * 2. Department Manager: Manages one or more departments (Department.ManagerId == user.id).
 * 3. Employee: Regular employee without management rights.
 */
function evaluateAccessScope(currentUser, departments = [], members = [], grantedPermissions = []) {
  if (!currentUser || !currentUser.id) {
    throw new AbpAuthorizationException('User is not authenticated.');
  }

  const isAdminRole = currentUser.role === 'admin' || (currentUser.roles && currentUser.roles.includes('admin'));
  const hasViewAll = grantedPermissions.includes(Permissions.Employees.ViewAll);

  if (isAdminRole || hasViewAll) {
    return {
      type: 'Admin',
      isGlobal: true,
      isManager: false,
      isEmployee: false,
      userId: currentUser.id,
      managedDepartmentIds: new Set(departments.map(d => d.id))
    };
  }

  const managedDepts = departments.filter(d => d.managerId === currentUser.id);
  const managedDeptIds = new Set(managedDepts.map(d => d.id));

  if (managedDeptIds.size > 0) {
    return {
      type: 'Manager',
      isGlobal: false,
      isManager: true,
      isEmployee: false,
      userId: currentUser.id,
      managedDepartmentIds: managedDeptIds
    };
  }

  return {
    type: 'Employee',
    isGlobal: false,
    isManager: false,
    isEmployee: true,
    userId: currentUser.id,
    managedDepartmentIds: new Set()
  };
}

/**
 * Checks whether the current user is authorized to view or access targetUserId's profile,
 * requests, leave timeline, or statistics.
 */
function canAccessEmployee(currentUser, targetUserId, departments = [], members = [], grantedPermissions = []) {
  if (!currentUser || !currentUser.id) {
    return false;
  }

  // 1. Always allowed to access self
  if (currentUser.id === targetUserId) {
    return true;
  }

  const scope = evaluateAccessScope(currentUser, departments, members, grantedPermissions);

  // 2. Admin / ViewAll can access anyone
  if (scope.isGlobal) {
    return true;
  }

  // 3. Manager can access employees in departments they manage
  if (scope.isManager) {
    const targetMember = members.find(m => m.userId === targetUserId);
    if (targetMember && targetMember.departmentId && scope.managedDepartmentIds.has(targetMember.departmentId)) {
      return true;
    }
  }

  // 4. Regular employee cannot access other employees
  return false;
}

/**
 * Enforces access check, throwing AbpAuthorizationException or EntityNotFoundException.
 */
function assertCanAccessEmployee(currentUser, targetUserId, departments = [], members = [], users = [], grantedPermissions = []) {
  const userExists = users.some(u => (u.id || u.userId) === targetUserId);
  if (!userExists) {
    throw new EntityNotFoundException('IdentityUser', targetUserId);
  }

  const allowed = canAccessEmployee(currentUser, targetUserId, departments, members, grantedPermissions);
  if (!allowed) {
    throw new AbpAuthorizationException('Bạn không có quyền truy cập hồ sơ nhân sự này.');
  }
}

/**
 * Filters the employee list based on the caller's hierarchical scope and input filters.
 */
function filterEmployeeList(currentUser, allEmployees = [], departments = [], members = [], filterInput = {}, grantedPermissions = []) {
  const scope = evaluateAccessScope(currentUser, departments, members, grantedPermissions);

  // If department filter specified, validate manager's permission to query that department
  if (filterInput.departmentId) {
    if (!scope.isGlobal && !scope.managedDepartmentIds.has(filterInput.departmentId)) {
      throw new AbpAuthorizationException('Bạn không có quyền xem dữ liệu của phòng ban này.');
    }
  }

  return allEmployees.filter(emp => {
    const empId = emp.id || emp.userId;

    // Scope check
    if (!scope.isGlobal) {
      if (scope.isManager) {
        const isSelf = empId === scope.userId;
        const inManagedDept = emp.departmentId && scope.managedDepartmentIds.has(emp.departmentId);
        if (!isSelf && !inManagedDept) {
          return false;
        }
      } else {
        // Employee: strictly self
        if (empId !== scope.userId) {
          return false;
        }
      }
    }

    // Department filter
    if (filterInput.departmentId && emp.departmentId !== filterInput.departmentId) {
      return false;
    }

    // Text query filter
    if (filterInput.filter) {
      const q = filterInput.filter.toLowerCase().trim();
      const match = (emp.fullName && emp.fullName.toLowerCase().includes(q)) ||
                    (emp.userName && emp.userName.toLowerCase().includes(q)) ||
                    (emp.email && emp.email.toLowerCase().includes(q)) ||
                    (emp.position && emp.position.toLowerCase().includes(q));
      if (!match) return false;
    }

    return true;
  });
}

/**
 * Checks whether current user can update target employee's JoinDate.
 * Requires:
 * 1. Has Employees.Edit permission.
 * 2. If Admin: allowed for any employee.
 * 3. If Manager: allowed only for employees in their managed departments (cannot edit other depts, nor self).
 * 4. If Employee: rejected with AbpAuthorizationException.
 */
function assertCanUpdateJoinDate(currentUser, targetUserId, departments = [], members = [], users = [], grantedPermissions = []) {
  const userExists = users.some(u => (u.id || u.userId) === targetUserId);
  if (!userExists) {
    throw new EntityNotFoundException('IdentityUser', targetUserId);
  }

  const hasEdit = grantedPermissions.includes(Permissions.Employees.Edit) || currentUser.role === 'admin';
  if (!hasEdit) {
    throw new AbpAuthorizationException('Bạn không có quyền cập nhật ngày vào làm việc.');
  }

  const scope = evaluateAccessScope(currentUser, departments, members, grantedPermissions);
  if (scope.isGlobal) {
    return true;
  }

  if (scope.isManager) {
    const targetMember = members.find(m => m.userId === targetUserId);
    if (targetMember && targetMember.departmentId && scope.managedDepartmentIds.has(targetMember.departmentId)) {
      return true;
    }
  }

  throw new AbpAuthorizationException('Bạn không có quyền cập nhật ngày vào làm của nhân sự thuộc phòng ban khác.');
}

module.exports = {
  Permissions,
  AbpAuthorizationException,
  EntityNotFoundException,
  evaluateAccessScope,
  canAccessEmployee,
  assertCanAccessEmployee,
  filterEmployeeList,
  assertCanUpdateJoinDate
};
