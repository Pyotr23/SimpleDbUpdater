USE [I-EMS_S];
GO

/****** Object:  Table [dbo].[SysStringsLoc]    Script Date: 04.09.2019 13:58:41 ******/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS
(
    SELECT name
    FROM sys.tables
    WHERE name = 'SysStringsLoc'
)
BEGIN
    CREATE TABLE [dbo].[SysStringsLoc]
    ([LCID]             [INT] NOT NULL, 
     [StringIdentifier] [NVARCHAR](150) NOT NULL, 
     [Value]            [NVARCHAR](MAX) NULL, 
     CONSTRAINT [PK_SysStringsLoc] PRIMARY KEY CLUSTERED([LCID] ASC, [StringIdentifier] ASC)
     WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
    )
    ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END;