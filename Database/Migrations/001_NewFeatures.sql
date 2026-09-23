USE [ExpenseTracker];
GO

IF COL_LENGTH('dbo.Users', 'Email') IS NULL ALTER TABLE [dbo].[Users] ADD [Email] VARCHAR(255) NULL;
IF COL_LENGTH('dbo.Users', 'FullName') IS NULL ALTER TABLE [dbo].[Users] ADD [FullName] VARCHAR(200) NULL;
IF COL_LENGTH('dbo.Users', 'Currency') IS NULL ALTER TABLE [dbo].[Users] ADD [Currency] VARCHAR(10) NOT NULL CONSTRAINT [DF_Users_Currency] DEFAULT ('INR');
IF COL_LENGTH('dbo.Users', 'DateFormat') IS NULL ALTER TABLE [dbo].[Users] ADD [DateFormat] VARCHAR(20) NOT NULL CONSTRAINT [DF_Users_DateFormat] DEFAULT ('dd/MM/yyyy');
IF COL_LENGTH('dbo.Users', 'Language') IS NULL ALTER TABLE [dbo].[Users] ADD [Language] VARCHAR(50) NOT NULL CONSTRAINT [DF_Users_Language] DEFAULT ('English');
IF COL_LENGTH('dbo.Users', 'EmailNotifications') IS NULL ALTER TABLE [dbo].[Users] ADD [EmailNotifications] BIT NOT NULL CONSTRAINT [DF_Users_EmailNotifications] DEFAULT (1);
IF COL_LENGTH('dbo.Users', 'DarkMode') IS NULL ALTER TABLE [dbo].[Users] ADD [DarkMode] BIT NOT NULL CONSTRAINT [DF_Users_DarkMode] DEFAULT (0);
IF COL_LENGTH('dbo.Users', 'ModifiedDate') IS NULL ALTER TABLE [dbo].[Users] ADD [ModifiedDate] DATETIME2 NULL;
IF COL_LENGTH('dbo.Expenses', 'ReceiptPath') IS NULL ALTER TABLE [dbo].[Expenses] ADD [ReceiptPath] NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Expenses_UserId_IsActive_ExpenseDate' AND object_id = OBJECT_ID('dbo.Expenses'))
    CREATE INDEX [IX_Expenses_UserId_IsActive_ExpenseDate] ON [dbo].[Expenses] ([UserId], [IsActive], [ExpenseDate]) INCLUDE ([Amount], [CategoryId]);
GO

IF OBJECT_ID('dbo.Budgets', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Budgets](
        [BudgetId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Budgets] PRIMARY KEY,
        [UserId] INT NOT NULL, [CategoryId] INT NOT NULL, [Amount] DECIMAL(18,2) NOT NULL,
        [Year] INT NOT NULL, [Month] INT NOT NULL, [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Budgets_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [ModifiedDate] DATETIME2 NULL, [IsActive] BIT NOT NULL CONSTRAINT [DF_Budgets_IsActive] DEFAULT (1),
        CONSTRAINT [UQ_Budgets_User_Category_Period] UNIQUE ([UserId],[CategoryId],[Year],[Month]),
        CONSTRAINT [FK_Budgets_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
        CONSTRAINT [FK_Budgets_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([CategoryId]),
        CONSTRAINT [CK_Budgets_Amount] CHECK ([Amount] > 0), CONSTRAINT [CK_Budgets_Month] CHECK ([Month] BETWEEN 1 AND 12)
    );
END
GO

IF OBJECT_ID('dbo.RecurringExpenses', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RecurringExpenses](
        [RecurringExpenseId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_RecurringExpenses] PRIMARY KEY,
        [UserId] INT NOT NULL, [CategoryId] INT NOT NULL, [Amount] DECIMAL(18,2) NOT NULL,
        [Description] NVARCHAR(500) NULL, [Frequency] VARCHAR(20) NOT NULL, [StartDate] DATE NOT NULL,
        [NextRunDate] DATE NOT NULL, [EndDate] DATE NULL, [IsActive] BIT NOT NULL CONSTRAINT [DF_RecurringExpenses_IsActive] DEFAULT (1),
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_RecurringExpenses_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_RecurringExpenses_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
        CONSTRAINT [FK_RecurringExpenses_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([CategoryId]),
        CONSTRAINT [CK_RecurringExpenses_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_RecurringExpenses_Frequency] CHECK ([Frequency] IN ('Monthly','Weekly','Yearly'))
    );
END
GO

IF OBJECT_ID('dbo.Incomes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Incomes](
        [IncomeId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Incomes] PRIMARY KEY, [UserId] INT NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL, [Source] VARCHAR(200) NOT NULL, [Description] NVARCHAR(500) NULL,
        [IncomeDate] DATE NOT NULL, [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Incomes_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Incomes_IsActive] DEFAULT (1),
        CONSTRAINT [FK_Incomes_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
        CONSTRAINT [CK_Incomes_Amount] CHECK ([Amount] > 0)
    );
END
GO

IF OBJECT_ID('dbo.Households', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Households](
        [HouseholdId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Households] PRIMARY KEY,
        [Name] VARCHAR(200) NOT NULL, [OwnerUserId] INT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Households_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Households_IsActive] DEFAULT (1),
        CONSTRAINT [FK_Households_Users] FOREIGN KEY ([OwnerUserId]) REFERENCES [dbo].[Users]([UserId])
    );
END
GO

IF OBJECT_ID('dbo.HouseholdMembers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HouseholdMembers](
        [HouseholdMemberId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_HouseholdMembers] PRIMARY KEY,
        [HouseholdId] INT NOT NULL, [UserId] INT NOT NULL, [Role] VARCHAR(20) NOT NULL,
        [JoinedDate] DATETIME2 NOT NULL CONSTRAINT [DF_HouseholdMembers_JoinedDate] DEFAULT (SYSUTCDATETIME()),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_HouseholdMembers_IsActive] DEFAULT (1),
        CONSTRAINT [UQ_HouseholdMembers_Household_User] UNIQUE ([HouseholdId],[UserId]),
        CONSTRAINT [FK_HouseholdMembers_Households] FOREIGN KEY ([HouseholdId]) REFERENCES [dbo].[Households]([HouseholdId]),
        CONSTRAINT [FK_HouseholdMembers_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
        CONSTRAINT [CK_HouseholdMembers_Role] CHECK ([Role] IN ('Owner','Member'))
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserById] @UserId INT AS
BEGIN SET NOCOUNT ON; SELECT [UserId],[Username],[PasswordHash],[CreatedDate],[Email],[FullName],[Currency],[DateFormat],[Language],[EmailNotifications],[DarkMode],[ModifiedDate] FROM [dbo].[Users] WHERE [UserId]=@UserId; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateUserProfile] @UserId INT,@Email VARCHAR(255)=NULL,@FullName VARCHAR(200)=NULL AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Users] SET [Email]=NULLIF(LTRIM(RTRIM(@Email)),''),[FullName]=NULLIF(LTRIM(RTRIM(@FullName)),''),[ModifiedDate]=SYSUTCDATETIME() WHERE [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateUserSettings] @UserId INT,@Currency VARCHAR(10),@DateFormat VARCHAR(20),@Language VARCHAR(50),@EmailNotifications BIT,@DarkMode BIT AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Users] SET [Currency]=@Currency,[DateFormat]=@DateFormat,[Language]=@Language,[EmailNotifications]=@EmailNotifications,[DarkMode]=@DarkMode,[ModifiedDate]=SYSUTCDATETIME() WHERE [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_ChangePassword] @UserId INT,@PasswordHash VARCHAR(500) AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Users] SET [PasswordHash]=@PasswordHash,[ModifiedDate]=SYSUTCDATETIME() WHERE [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetBudgets] @UserId INT,@Year INT,@Month INT AS
BEGIN SET NOCOUNT ON; SELECT b.*,c.[Name] AS CategoryName,ISNULL(SUM(e.[Amount]),0) AS SpentAmount FROM [dbo].[Budgets] b INNER JOIN [dbo].[Categories] c ON c.[CategoryId]=b.[CategoryId] LEFT JOIN [dbo].[Expenses] e ON e.[UserId]=b.[UserId] AND e.[CategoryId]=b.[CategoryId] AND e.[IsActive]=1 AND YEAR(e.[ExpenseDate])=@Year AND MONTH(e.[ExpenseDate])=@Month WHERE b.[UserId]=@UserId AND b.[Year]=@Year AND b.[Month]=@Month AND b.[IsActive]=1 GROUP BY b.[BudgetId],b.[UserId],b.[CategoryId],b.[Amount],b.[Year],b.[Month],b.[CreatedDate],b.[ModifiedDate],b.[IsActive],c.[Name]; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpsertBudget] @UserId INT,@CategoryId INT,@Amount DECIMAL(18,2),@Year INT,@Month INT AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; IF NOT EXISTS(SELECT 1 FROM [dbo].[UserCategories] WHERE [UserId]=@UserId AND [CategoryId]=@CategoryId AND [IsActive]=1) THROW 50001,'Invalid category for this user.',1; UPDATE [dbo].[Budgets] SET [Amount]=@Amount,[ModifiedDate]=SYSUTCDATETIME(),[IsActive]=1 WHERE [UserId]=@UserId AND [CategoryId]=@CategoryId AND [Year]=@Year AND [Month]=@Month; IF @@ROWCOUNT=0 INSERT [dbo].[Budgets]([UserId],[CategoryId],[Amount],[Year],[Month]) VALUES(@UserId,@CategoryId,@Amount,@Year,@Month); COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteBudget] @UserId INT,@BudgetId INT AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Budgets] SET [IsActive]=0,[ModifiedDate]=SYSUTCDATETIME() WHERE [BudgetId]=@BudgetId AND [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetRecurringExpenses] @UserId INT AS BEGIN SET NOCOUNT ON; SELECT r.*,c.[Name] CategoryName FROM [dbo].[RecurringExpenses] r INNER JOIN [dbo].[Categories] c ON c.[CategoryId]=r.[CategoryId] WHERE r.[UserId]=@UserId AND r.[IsActive]=1 ORDER BY r.[NextRunDate]; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_CreateRecurringExpense] @UserId INT,@CategoryId INT,@Amount DECIMAL(18,2),@Description NVARCHAR(500)=NULL,@Frequency VARCHAR(20),@StartDate DATE,@NextRunDate DATE,@EndDate DATE=NULL AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; IF NOT EXISTS(SELECT 1 FROM [dbo].[UserCategories] WHERE [UserId]=@UserId AND [CategoryId]=@CategoryId AND [IsActive]=1) THROW 50001,'Invalid category for this user.',1; INSERT [dbo].[RecurringExpenses]([UserId],[CategoryId],[Amount],[Description],[Frequency],[StartDate],[NextRunDate],[EndDate]) VALUES(@UserId,@CategoryId,@Amount,@Description,@Frequency,@StartDate,@NextRunDate,@EndDate); SELECT CAST(SCOPE_IDENTITY() AS INT); COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateRecurringExpense] @RecurringExpenseId INT,@UserId INT,@CategoryId INT,@Amount DECIMAL(18,2),@Description NVARCHAR(500)=NULL,@Frequency VARCHAR(20),@StartDate DATE,@NextRunDate DATE,@EndDate DATE=NULL AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE r SET [CategoryId]=@CategoryId,[Amount]=@Amount,[Description]=@Description,[Frequency]=@Frequency,[StartDate]=@StartDate,[NextRunDate]=@NextRunDate,[EndDate]=@EndDate FROM [dbo].[RecurringExpenses] r INNER JOIN [dbo].[UserCategories] uc ON uc.[UserId]=@UserId AND uc.[CategoryId]=@CategoryId AND uc.[IsActive]=1 WHERE r.[RecurringExpenseId]=@RecurringExpenseId AND r.[UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteRecurringExpense] @RecurringExpenseId INT,@UserId INT AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[RecurringExpenses] SET [IsActive]=0 WHERE [RecurringExpenseId]=@RecurringExpenseId AND [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDueRecurringExpenses] @UserId INT,@AsOfDate DATE AS BEGIN SET NOCOUNT ON; SELECT * FROM [dbo].[RecurringExpenses] WHERE [UserId]=@UserId AND [IsActive]=1 AND [NextRunDate]<=@AsOfDate AND ([EndDate] IS NULL OR [NextRunDate]<=[EndDate]); END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_AdvanceRecurringNextRun] @RecurringExpenseId INT,@UserId INT,@ExpectedNextRunDate DATE,@NewNextRunDate DATE AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[RecurringExpenses] SET [NextRunDate]=@NewNextRunDate,[IsActive]=CASE WHEN [EndDate] IS NOT NULL AND @NewNextRunDate>[EndDate] THEN 0 ELSE [IsActive] END WHERE [RecurringExpenseId]=@RecurringExpenseId AND [UserId]=@UserId AND [NextRunDate]=@ExpectedNextRunDate; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_CreateIncome] @UserId INT,@Amount DECIMAL(18,2),@Source VARCHAR(200),@Description NVARCHAR(500)=NULL,@IncomeDate DATE AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; INSERT [dbo].[Incomes]([UserId],[Amount],[Source],[Description],[IncomeDate]) VALUES(@UserId,@Amount,@Source,@Description,@IncomeDate); SELECT CAST(SCOPE_IDENTITY() AS INT); COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateIncome] @IncomeId INT,@UserId INT,@Amount DECIMAL(18,2),@Source VARCHAR(200),@Description NVARCHAR(500)=NULL,@IncomeDate DATE AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Incomes] SET [Amount]=@Amount,[Source]=@Source,[Description]=@Description,[IncomeDate]=@IncomeDate WHERE [IncomeId]=@IncomeId AND [UserId]=@UserId AND [IsActive]=1; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteIncome] @IncomeId INT,@UserId INT AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Incomes] SET [IsActive]=0 WHERE [IncomeId]=@IncomeId AND [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIncomeById] @IncomeId INT,@UserId INT AS BEGIN SET NOCOUNT ON; SELECT * FROM [dbo].[Incomes] WHERE [IncomeId]=@IncomeId AND [UserId]=@UserId AND [IsActive]=1; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_SearchIncome] @UserId INT,@FromDate DATE=NULL,@ToDate DATE=NULL AS BEGIN SET NOCOUNT ON; SELECT * FROM [dbo].[Incomes] WHERE [UserId]=@UserId AND [IsActive]=1 AND (@FromDate IS NULL OR [IncomeDate]>=@FromDate) AND (@ToDate IS NULL OR [IncomeDate]<=@ToDate) ORDER BY [IncomeDate] DESC,[IncomeId] DESC; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_TotalIncome] @UserId INT,@From DATE,@To DATE AS BEGIN SET NOCOUNT ON; SELECT ISNULL(SUM([Amount]),0) FROM [dbo].[Incomes] WHERE [UserId]=@UserId AND [IsActive]=1 AND [IncomeDate] BETWEEN @From AND @To; END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_CreateHousehold] @UserId INT,@Name VARCHAR(200) AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; IF EXISTS(SELECT 1 FROM [dbo].[HouseholdMembers] WHERE [UserId]=@UserId AND [IsActive]=1) THROW 50002,'You already belong to a household.',1; INSERT [dbo].[Households]([Name],[OwnerUserId]) VALUES(@Name,@UserId); DECLARE @HouseholdId INT=CAST(SCOPE_IDENTITY() AS INT); INSERT [dbo].[HouseholdMembers]([HouseholdId],[UserId],[Role]) VALUES(@HouseholdId,@UserId,'Owner'); SELECT @HouseholdId; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserHousehold] @UserId INT AS BEGIN SET NOCOUNT ON; SELECT h.* FROM [dbo].[Households] h INNER JOIN [dbo].[HouseholdMembers] m ON m.[HouseholdId]=h.[HouseholdId] AND m.[UserId]=@UserId AND m.[IsActive]=1 WHERE h.[IsActive]=1; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_InviteHouseholdMember] @UserId INT,@Username VARCHAR(200) AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; DECLARE @HouseholdId INT=(SELECT [HouseholdId] FROM [dbo].[Households] WHERE [OwnerUserId]=@UserId AND [IsActive]=1),@InviteeId INT=(SELECT [UserId] FROM [dbo].[Users] WHERE [Username]=@Username); IF @HouseholdId IS NULL THROW 50003,'Only a household owner can invite members.',1; IF @InviteeId IS NULL THROW 50004,'User not found.',1; IF @InviteeId=@UserId THROW 50005,'You cannot invite yourself.',1; IF EXISTS(SELECT 1 FROM [dbo].[HouseholdMembers] WHERE [UserId]=@InviteeId AND [IsActive]=1) THROW 50006,'User already belongs to a household.',1; INSERT [dbo].[HouseholdMembers]([HouseholdId],[UserId],[Role]) VALUES(@HouseholdId,@InviteeId,'Member'); COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetHouseholdMembers] @UserId INT AS BEGIN SET NOCOUNT ON; DECLARE @HouseholdId INT=(SELECT TOP 1 [HouseholdId] FROM [dbo].[HouseholdMembers] WHERE [UserId]=@UserId AND [IsActive]=1); SELECT m.*,u.[Username],u.[FullName] FROM [dbo].[HouseholdMembers] m INNER JOIN [dbo].[Users] u ON u.[UserId]=m.[UserId] WHERE m.[HouseholdId]=@HouseholdId AND m.[IsActive]=1 ORDER BY CASE m.[Role] WHEN 'Owner' THEN 0 ELSE 1 END,u.[Username]; END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_LeaveHousehold] @UserId INT AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; IF EXISTS(SELECT 1 FROM [dbo].[Households] WHERE [OwnerUserId]=@UserId AND [IsActive]=1) THROW 50007,'The owner cannot leave the household.',1; UPDATE [dbo].[HouseholdMembers] SET [IsActive]=0 WHERE [UserId]=@UserId AND [IsActive]=1; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_CreateExpenses] @UserId INT,@CategoryId INT,@Amount DECIMAL(18,2),@Description NVARCHAR(500)=NULL,@ExpenseDate DATE,@CreatedDate DATETIME2,@IsActive BIT=1,@ReceiptPath NVARCHAR(500)=NULL AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; IF NOT EXISTS(SELECT 1 FROM [dbo].[UserCategories] WHERE [UserId]=@UserId AND [CategoryId]=@CategoryId AND [IsActive]=1) THROW 50001,'Invalid category for this user.',1; INSERT [dbo].[Expenses]([UserId],[CategoryId],[Amount],[Description],[ExpenseDate],[CreatedDate],[IsActive],[ReceiptPath]) VALUES(@UserId,@CategoryId,@Amount,@Description,@ExpenseDate,@CreatedDate,@IsActive,@ReceiptPath); SELECT CAST(SCOPE_IDENTITY() AS INT); COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateExpenses] @ExpenseId INT,@UserId INT,@CategoryId INT,@Amount DECIMAL(18,2),@Description NVARCHAR(500)=NULL,@ExpenseDate DATE,@IsActive BIT=1,@ReceiptPath NVARCHAR(500)=NULL AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; IF NOT EXISTS(SELECT 1 FROM [dbo].[UserCategories] WHERE [UserId]=@UserId AND [CategoryId]=@CategoryId AND [IsActive]=1) THROW 50001,'Invalid category for this user.',1; UPDATE [dbo].[Expenses] SET [CategoryId]=@CategoryId,[Amount]=@Amount,[Description]=@Description,[ExpenseDate]=@ExpenseDate,[IsActive]=@IsActive,[ReceiptPath]=COALESCE(@ReceiptPath,[ReceiptPath]) WHERE [ExpenseId]=@ExpenseId AND [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
