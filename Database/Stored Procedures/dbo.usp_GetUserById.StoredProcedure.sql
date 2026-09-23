CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserById] @UserId INT AS
BEGIN SET NOCOUNT ON; SELECT [UserId],[Username],[PasswordHash],[CreatedDate],[Email],[FullName],[Currency],[DateFormat],[Language],[EmailNotifications],[DarkMode],[ModifiedDate] FROM [dbo].[Users] WHERE [UserId]=@UserId; END
GO
