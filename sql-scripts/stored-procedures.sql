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

CREATE TYPE VoucherEntryType AS TABLE
(
    AccountId INT,
    Debit DECIMAL(18,2),
    Credit DECIMAL(18,2),
    Description NVARCHAR(200)
);
GO

CREATE PROCEDURE sp_SaveVoucher
    @VoucherType NVARCHAR(20),
    @Date DATE,
    @ReferenceNo NVARCHAR(50),
    @Entries VoucherEntryType READONLY,
    @Result NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate inputs
        IF @VoucherType NOT IN ('Journal', 'Payment', 'Receipt')
            THROW 50001, 'Invalid voucher type.', 1;
        IF @Date IS NULL OR @ReferenceNo IS NULL
            THROW 50002, 'Date and ReferenceNo are required.', 1;

        -- Check if ReferenceNo is unique
        IF EXISTS (SELECT 1 FROM Vouchers WHERE ReferenceNo = @ReferenceNo)
            THROW 50003, 'Reference number already exists.', 1;

        -- Validate entries: at least two entries, debits = credits
        DECLARE @TotalDebit DECIMAL(18,2) = (SELECT SUM(Debit) FROM @Entries);
        DECLARE @TotalCredit DECIMAL(18,2) = (SELECT SUM(Credit) FROM @Entries);
        IF @TotalDebit IS NULL OR @TotalCredit IS NULL OR @TotalDebit = 0
            THROW 50004, 'At least two entries with non-zero debit and credit required.', 1;
        IF @TotalDebit != @TotalCredit
            THROW 50005, 'Debits must equal credits.', 1;

        -- Insert entries
        INSERT INTO Vouchers (VoucherType, Date, ReferenceNo, AccountId, Debit, Credit, Description)
        SELECT @VoucherType, @Date, @ReferenceNo, AccountId, Debit, Credit, Description
        FROM @Entries;

        SET @Result = 'Voucher saved successfully.';
    END TRY
    BEGIN CATCH
        SET @Result = ERROR_MESSAGE();
        THROW;
    END CATCH
END
GO


CREATE PROCEDURE CheckUserModulePermission
    @Email NVARCHAR(100),
    @ModuleName NVARCHAR(100),
    @HasPermission BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @HasPermission = 0;

    SELECT @HasPermission = CASE 
        WHEN rp.CanAccess = 1 THEN 1 
        ELSE 0 
    END
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    INNER JOIN RolePermissions rp ON r.RoleId = rp.RoleId
    INNER JOIN Modules m ON rp.ModuleId = m.ModuleId
    WHERE u.Email = @Email 
    AND m.ModuleName = @ModuleName;

    IF @HasPermission IS NULL
        SET @HasPermission = 0;
END;
GO



