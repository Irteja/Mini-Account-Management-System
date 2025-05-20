INSERT INTO Modules (ModuleName)
VALUES 
    ('Chart of account'),
    ('VoucherEntry'),
    ('Reports'),
    ('Permission Pages'),
    ('Voucher List'),
    ('Create User'),
    ('Edit chart account'),
    ('Manage Module'),
    ('Add Account')
    ;


INSERT INTO Roles (RoleName)
VALUES 
    ('Admin'),
    ('Accountant'),
    ('Viewer');

INSERT INTO RolePermissions (RoleId, ModuleId, CanAccess)
SELECT r.RoleId, m.ModuleId, 
    CASE WHEN r.RoleName = 'Admin' THEN 1 ELSE 0 END AS CanAccess
FROM Roles r
CROSS JOIN Modules m;

INSERT INTO Users (UserName, Email, PasswordHash, RoleId)
VALUES ('AdminUser', 'admin@example.com', 'MTIzNA==', 1);