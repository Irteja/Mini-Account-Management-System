-- Users (handled mostly by ASP.NET Identity)
-- But for custom roles and access, we’ll have our own additional tables

CREATE DATABASE MiniAccountDB;


CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY,
    RoleName NVARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY,
    UserName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX),  
    RoleId INT FOREIGN KEY REFERENCES Roles(RoleId)
);

CREATE TABLE Modules (
    ModuleId INT PRIMARY KEY IDENTITY,
    ModuleName NVARCHAR(100) NOT NULL  
);

CREATE TABLE RolePermissions (
    RoleId INT FOREIGN KEY REFERENCES Roles(RoleId),
    ModuleId INT FOREIGN KEY REFERENCES Modules(ModuleId),
    CanAccess BIT DEFAULT 0,
    PRIMARY KEY (RoleId, ModuleId)
);


CREATE TABLE Accounts (AccountId INT PRIMARY KEY IDENTITY,AccountName NVARCHAR(100) NOT NULL,ParentAccountId INT NULL,AccountType NVARCHAR(50), FOREIGN KEY (ParentAccountId) REFERENCES Accounts(AccountId));

CREATE TABLE Vouchers
(
    VoucherId INT IDENTITY(1,1) PRIMARY KEY,
    VoucherType NVARCHAR(20) NOT NULL, 
    Date DATE NOT NULL,
    ReferenceNo NVARCHAR(50) NOT NULL,
    AccountId INT NOT NULL,
    Debit DECIMAL(18,2) NOT NULL DEFAULT 0,
    Credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    Description NVARCHAR(200),
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId)
);
