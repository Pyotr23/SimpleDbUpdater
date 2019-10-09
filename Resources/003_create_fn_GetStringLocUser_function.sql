USE [I-EMS_S];
GO

/****** Object:  UserDefinedFunction [dbo].[fn_GetStringLocUser]    Script Date: 04.09.2019 12:35:04 ******/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS
(
    SELECT name
    FROM sys.objects
    WHERE name = 'fn_GetStringLocUser'
)
	EXEC(N'
CREATE FUNCTION [dbo].[fn_GetStringLocUser]
(@StringIdentifier NVARCHAR(150)
)
RETURNS NVARCHAR(MAX)
AS
     BEGIN
         DECLARE @Result NVARCHAR(MAX);
         -- Текущая культура
         SELECT @Result = [dbo].[fn_GetStringLCID](@StringIdentifier, [dbo].[fn_GetCurrentSessionCulture]());
         -- Культура по умолчанию
         IF @RESULT IS NULL
         BEGIN
             SELECT @Result = [dbo].[fn_GetStringLCID](@StringIdentifier, [dbo].[fn_GetSettings](''DefaultLCID''))
         END;
         -- --Русская (должна быть всегда)
         IF @RESULT IS NULL
         BEGIN
             SELECT @Result = [dbo].[fn_GetStringLCID](@StringIdentifier, 1049)
         END;
         RETURN ISNULL(@Result, ''NoLocString'');
     END;	
	')
GO
ALTER FUNCTION [dbo].[fn_GetStringLocUser]
(@StringIdentifier NVARCHAR(150)
)
RETURNS NVARCHAR(MAX)
AS
     BEGIN
         DECLARE @Result NVARCHAR(MAX);
         -- Текущая культура
         SELECT @Result = [dbo].[fn_GetStringLCID](@StringIdentifier, [dbo].[fn_GetCurrentSessionCulture]());
         -- Культура по умолчанию
         IF @RESULT IS NULL
         BEGIN
             SELECT @Result = [dbo].[fn_GetStringLCID](@StringIdentifier, [dbo].[fn_GetSettings]('DefaultLCID'))
         END;
         -- --Русская (должна быть всегда)
         IF @RESULT IS NULL
         BEGIN
             SELECT @Result = [dbo].[fn_GetStringLCID](@StringIdentifier, 1049)
         END;
         RETURN ISNULL(@Result, 'NoLocString');
     END;