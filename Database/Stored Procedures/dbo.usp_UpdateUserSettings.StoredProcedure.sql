CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateUserSettings] @UserId INT,@Currency VARCHAR(10),@DateFormat VARCHAR(20),@Language VARCHAR(50),@EmailNotifications BIT,@DarkMode BIT AS
BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE [dbo].[Users] SET [Currency]=@Currency,[DateFormat]=@DateFormat,[Language]=@Language,[EmailNotifications]=@EmailNotifications,[DarkMode]=@DarkMode,[ModifiedDate]=SYSUTCDATETIME() WHERE [UserId]=@UserId; SELECT @@ROWCOUNT; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END
GO
