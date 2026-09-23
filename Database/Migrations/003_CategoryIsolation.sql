USE [ExpenseTracker]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateCategory]
    @CategoryId INT,
    @Name NVARCHAR(100),
    @UserId INT,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RowsAffected INT = 0;
    DECLARE @ShareCount INT = 0;
    DECLARE @NewCategoryId INT = NULL;

    BEGIN TRANSACTION;

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM [dbo].[UserCategories] WHERE UserId = @UserId AND CategoryId = @CategoryId)
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM [dbo].[Categories] c
                INNER JOIN [dbo].[UserCategories] uc ON c.CategoryId = uc.CategoryId
                WHERE uc.UserId = @UserId
                  AND c.Name = @Name
                  AND c.CategoryId <> @CategoryId
            )
            BEGIN
                RAISERROR('A category with this name already exists for the user.', 16, 1);
            END
            ELSE
            BEGIN
                SELECT @ShareCount = COUNT(*)
                FROM [dbo].[UserCategories]
                WHERE CategoryId = @CategoryId;

                IF @ShareCount <= 1
                BEGIN
                    UPDATE [dbo].[Categories]
                    SET Name = @Name
                    WHERE CategoryId = @CategoryId;

                    UPDATE [dbo].[UserCategories]
                    SET IsActive = @IsActive
                    WHERE UserId = @UserId AND CategoryId = @CategoryId;

                    SET @RowsAffected = @@ROWCOUNT;
                END
                ELSE
                BEGIN
                    INSERT INTO [dbo].[Categories] (Name, CreatedDate, IsActive)
                    VALUES (@Name, SYSUTCDATETIME(), 1);

                    SET @NewCategoryId = SCOPE_IDENTITY();

                    UPDATE [dbo].[UserCategories]
                    SET CategoryId = @NewCategoryId,
                        IsActive = @IsActive
                    WHERE UserId = @UserId AND CategoryId = @CategoryId;

                    UPDATE [dbo].[Expenses]
                    SET CategoryId = @NewCategoryId
                    WHERE UserId = @UserId AND CategoryId = @CategoryId AND IsActive = 1;

                    SET @RowsAffected = 1;
                END
            END
        END

        COMMIT TRANSACTION;

        SELECT @RowsAffected AS RowsAffected;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
