USE [ExpenseTracker]
GO
/****** Object:  Table [dbo].[UserCategories]    Script Date: 07-09-2025 10:57:52 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserCategories](
	[UserCategoryId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[CategoryId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[UserCategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[UserId] ASC,
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Add foreign key constraints
ALTER TABLE [dbo].[UserCategories] WITH CHECK ADD CONSTRAINT [FK_UserCategories_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([UserId])
GO
ALTER TABLE [dbo].[UserCategories] CHECK CONSTRAINT [FK_UserCategories_Users]
GO

ALTER TABLE [dbo].[UserCategories] WITH CHECK ADD CONSTRAINT [FK_UserCategories_Categories] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Categories] ([CategoryId])
GO
ALTER TABLE [dbo].[UserCategories] CHECK CONSTRAINT [FK_UserCategories_Categories]
GO

ALTER TABLE [dbo].[UserCategories] WITH CHECK ADD CONSTRAINT [FK_UserCategories_CreatedBy] FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Users] ([UserId])
GO
ALTER TABLE [dbo].[UserCategories] CHECK CONSTRAINT [FK_UserCategories_CreatedBy]
GO

-- Add default constraints
ALTER TABLE [dbo].[UserCategories] ADD DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[UserCategories] ADD DEFAULT ((1)) FOR [IsActive]
GO

-- Add indexes for performance
CREATE NONCLUSTERED INDEX [IX_UserCategories_UserId_IsActive] ON [dbo].[UserCategories]
(
	[UserId] ASC,
	[IsActive] ASC
)
INCLUDE ([CategoryId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_UserCategories_CategoryId] ON [dbo].[UserCategories]
(
	[CategoryId] ASC
)
INCLUDE ([UserId], [IsActive]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
