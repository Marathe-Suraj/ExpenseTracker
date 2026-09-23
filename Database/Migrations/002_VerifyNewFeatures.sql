SET NOCOUNT ON;

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @Year INT = YEAR(@Today);
DECLARE @Month INT = MONTH(@Today);
DECLARE @Start DATE = DATEFROMPARTS(@Year, @Month, 1);
DECLARE @End DATE = EOMONTH(@Today);
DECLARE @Fail INT = 0;

DECLARE @UserId INT = (SELECT TOP 1 uc.UserId FROM dbo.UserCategories uc WHERE uc.IsActive = 1 ORDER BY uc.UserId);
DECLARE @CategoryId INT = (
    SELECT TOP 1 uc.CategoryId
    FROM dbo.UserCategories uc
    WHERE uc.UserId = @UserId AND uc.IsActive = 1
);

IF @UserId IS NULL OR @CategoryId IS NULL
BEGIN
    RAISERROR('Seed user/category missing', 16, 1);
    RETURN;
END

PRINT 'Using UserId=' + CAST(@UserId AS VARCHAR(10)) + ' CategoryId=' + CAST(@CategoryId AS VARCHAR(10));

EXEC dbo.usp_UpdateUserProfile @UserId=@UserId, @Email='test@example.com', @FullName='Test User';
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND Email='test@example.com') BEGIN SET @Fail=@Fail+1; PRINT 'FAIL profile'; END ELSE PRINT 'OK profile';

EXEC dbo.usp_UpdateUserSettings @UserId=@UserId, @Currency='INR', @DateFormat='dd/MM/yyyy', @Language='English', @EmailNotifications=1, @DarkMode=0;
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND Currency='INR') BEGIN SET @Fail=@Fail+1; PRINT 'FAIL settings'; END ELSE PRINT 'OK settings';

EXEC dbo.usp_UpsertBudget @UserId=@UserId, @CategoryId=@CategoryId, @Amount=5000, @Year=@Year, @Month=@Month;
IF NOT EXISTS (SELECT 1 FROM dbo.Budgets WHERE UserId=@UserId AND CategoryId=@CategoryId AND IsActive=1) BEGIN SET @Fail=@Fail+1; PRINT 'FAIL budget'; END ELSE PRINT 'OK budget';

DECLARE @Inc TABLE (Id INT);
INSERT INTO @Inc EXEC dbo.usp_CreateIncome @UserId=@UserId, @Amount=2500.50, @Source='Bonus', @Description='Test income', @IncomeDate=@Today;
DECLARE @IncomeId INT = (SELECT TOP 1 Id FROM @Inc);
IF @IncomeId IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL income create'; END ELSE PRINT 'OK income create';

DECLARE @Tot TABLE (Total DECIMAL(18,2));
INSERT INTO @Tot EXEC dbo.usp_TotalIncome @UserId=@UserId, @From=@Start, @To=@End;
DECLARE @IncomeTotal DECIMAL(18,2) = (SELECT TOP 1 Total FROM @Tot);
IF @IncomeTotal < 2500 BEGIN SET @Fail=@Fail+1; PRINT 'FAIL income total'; END ELSE PRINT 'OK income total';

DECLARE @Rec TABLE (Id INT);
INSERT INTO @Rec EXEC dbo.usp_CreateRecurringExpense @UserId=@UserId, @CategoryId=@CategoryId, @Amount=100, @Description='Recurring test', @Frequency='Monthly', @StartDate=@Today, @NextRunDate=@Today, @EndDate=NULL;
DECLARE @RecId INT = (SELECT TOP 1 Id FROM @Rec);
IF @RecId IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL recurring create'; END ELSE PRINT 'OK recurring create';

DECLARE @OtherUser INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE UserId<>@UserId ORDER BY UserId);
IF @OtherUser IS NOT NULL
BEGIN
    UPDATE dbo.HouseholdMembers SET IsActive=0 WHERE UserId IN (@UserId, @OtherUser) AND IsActive=1;
    UPDATE dbo.Households SET IsActive=0 WHERE OwnerUserId=@UserId AND IsActive=1;

    DECLARE @Hh TABLE (Id INT);
    INSERT INTO @Hh EXEC dbo.usp_CreateHousehold @UserId=@UserId, @Name='Test Household';
    DECLARE @HhId INT = (SELECT TOP 1 Id FROM @Hh);
    IF @HhId IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL household'; END ELSE PRINT 'OK household';

    DECLARE @Invitee VARCHAR(200) = (SELECT Username FROM dbo.Users WHERE UserId=@OtherUser);
    BEGIN TRY
        EXEC dbo.usp_InviteHouseholdMember @UserId=@UserId, @Username=@Invitee;
        PRINT 'OK invite';
    END TRY
    BEGIN CATCH
        SET @Fail=@Fail+1; PRINT 'FAIL invite: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE PRINT 'SKIP household invite (single user)';

IF COL_LENGTH('dbo.Expenses','ReceiptPath') IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL ReceiptPath'; END ELSE PRINT 'OK ReceiptPath';
IF OBJECT_ID('dbo.Budgets') IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL Budgets table'; END ELSE PRINT 'OK Budgets table';
IF OBJECT_ID('dbo.Incomes') IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL Incomes table'; END ELSE PRINT 'OK Incomes table';
IF OBJECT_ID('dbo.RecurringExpenses') IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL Recurring table'; END ELSE PRINT 'OK Recurring table';
IF OBJECT_ID('dbo.Households') IS NULL BEGIN SET @Fail=@Fail+1; PRINT 'FAIL Households table'; END ELSE PRINT 'OK Households table';

IF @Fail > 0
BEGIN
    RAISERROR('Verification failed with %d error(s)', 16, 1, @Fail);
END
ELSE
    PRINT 'ALL DB CHECKS PASSED';
