USE [ExpenseTracker]
GO
/****** Object:  StoredProcedure [dbo].[usp_ToggleCategoryStatus]    Script Date: 07-09-2025 10:57:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[usp_ToggleCategoryStatus]
    @CategoryId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Toggle the IsActive status in UserCategories mapping (not global Categories)
    UPDATE UserCategories
    SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END
    WHERE CategoryId = @CategoryId AND UserId = @UserId;

    -- Return the updated category with user-specific IsActive status
    SELECT 
        c.CategoryId, 
        c.Name, 
        c.CreatedDate, 
        uc.IsActive
    FROM Categories c
    INNER JOIN UserCategories uc ON c.CategoryId = uc.CategoryId
    WHERE c.CategoryId = @CategoryId AND uc.UserId = @UserId;
END
GO
