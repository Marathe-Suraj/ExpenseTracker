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

    -- Toggle the IsActive status
    UPDATE Categories
    SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END
    WHERE CategoryId = @CategoryId;

    -- Return the updated category
    SELECT CategoryId, Name, CreatedDate, IsActive
    FROM Categories
    WHERE CategoryId = @CategoryId;
END
GO
