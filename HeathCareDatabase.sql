GO
create database heathcareSystem
GO
use heathcareSystem
GO
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL, -- lưu hash mật khẩu
    FullName NVARCHAR(100),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO
CREATE TABLE ChatSessions (
    SessionID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    StartedAt DATETIME DEFAULT GETDATE(),
    EndedAt DATETIME NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
GO
CREATE TABLE ChatMessages (
    MessageID INT IDENTITY(1,1) PRIMARY KEY,
    SessionID INT NOT NULL,
    Sender NVARCHAR(10) NOT NULL 
        CHECK (Sender IN ('user', 'ai')),
    MessageText NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SessionID) REFERENCES ChatSessions(SessionID) ON DELETE CASCADE
);

