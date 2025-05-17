-- Users (handled mostly by ASP.NET Identity)
-- But for custom roles and access, we’ll have our own additional tables

CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY,
    RoleName NVARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY,
    UserName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX),  -- handled by ASP.NET Identity
    RoleId INT FOREIGN KEY REFERENCES Roles(RoleId)
);

CREATE TABLE Modules (
    ModuleId INT PRIMARY KEY IDENTITY,
    ModuleName NVARCHAR(100) NOT NULL  -- e.g., ChartOfAccounts, VoucherEntry, Reports
);

CREATE TABLE RolePermissions (
    RoleId INT FOREIGN KEY REFERENCES Roles(RoleId),
    ModuleId INT FOREIGN KEY REFERENCES Modules(ModuleId),
    CanAccess BIT DEFAULT 0,
    PRIMARY KEY (RoleId, ModuleId)
);


CREATE TABLE Accounts (AccountId INT PRIMARY KEY IDENTITY,AccountName NVARCHAR(100) NOT NULL,ParentAccountId INT NULL,AccountType NVARCHAR(50), FOREIGN KEY (ParentAccountId) REFERENCES Accounts(AccountId));
