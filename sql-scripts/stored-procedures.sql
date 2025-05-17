-- Stored procedure for inserting user
CREATE PROCEDURE InsertUser
    @UserName NVARCHAR(100),
    @Email NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @RoleId INT
AS
BEGIN
    INSERT INTO Users (UserName, Email, PasswordHash, RoleId)
    VALUES (@UserName, @Email, @PasswordHash, @RoleId)
END
GO

-- Stored procedure for getting all roles
CREATE PROCEDURE GetAllRoles
AS
BEGIN
    SELECT RoleId, RoleName FROM Roles
END
GO



-- Define TVP (Table-Valued Parameter type)
CREATE TYPE TVP_Permissions AS TABLE (
    ModuleId INT,
    CanAccess BIT
);
GO

CREATE PROCEDURE sp_AssignRolePermissions
    @RoleId INT,
    @Permissions TVP_Permissions READONLY  -- Table-Valued Parameter
AS
BEGIN
    DELETE FROM RolePermissions WHERE RoleId = @RoleId;

    INSERT INTO RolePermissions (RoleId, ModuleId, CanAccess) SELECT @RoleId, ModuleId, CanAccess FROM @Permissions;
END;
GO

CREATE PROCEDURE sp_ManageChartOfAccounts
    @Action NVARCHAR(10),@AccountId INT = NULL,@AccountName NVARCHAR(100) = NULL,@ParentAccountId INT = NULL,@AccountType NVARCHAR(50) = NULL
AS BEGIN SET NOCOUNT ON;
IF @Action = 'Create' BEGIN INSERT INTO Accounts (AccountName, ParentAccountId, AccountType) VALUES (@AccountName, @ParentAccountId, @AccountType); END
ELSE IF @Action = 'Update' BEGIN UPDATE Accounts SET AccountName = @AccountName, ParentAccountId = @ParentAccountId,AccountType = @AccountType WHERE AccountId = @AccountId;END
ELSE IF @Action = 'Delete' BEGIN DELETE FROM Accounts WHERE AccountId = @AccountId; END
ELSE IF @Action = 'Select' BEGIN SELECT AccountId, AccountName, ParentAccountId, AccountType FROM Accounts;END
END
GO



