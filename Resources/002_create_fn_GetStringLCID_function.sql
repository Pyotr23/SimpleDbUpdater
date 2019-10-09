USE [I-EMS_S];
GO

/****** Object:  UserDefinedFunction [dbo].[fn_GetStringLCID]    Script Date: 04.09.2019 12:35:44 ******/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS
(
    SELECT name
    FROM sys.objects
    WHERE name = 'fn_GetStringLCID'
)
BEGIN
    EXEC(N'
CREATE FUNCTION [dbo].[fn_GetStringLCID] -- 20140904
(@StringIdentifier NVARCHAR(150), 
 @LCID             INT
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @Result NVARCHAR(MAX);
    -- Поиск строки
    SELECT @Result = [Value]
    FROM [dbo].[SysStringsLoc]
    WHERE([LCID] = @LCID)
        AND ([StringIdentifier] = @StringIdentifier);
    RETURN @Result;
END;	
	');
END;
GO
ALTER FUNCTION [dbo].[fn_GetStringLCID] -- 20140904
(@StringIdentifier NVARCHAR(150), 
 @LCID             INT
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @Result NVARCHAR(MAX);
    -- Поиск строки
    SELECT @Result = [Value]
    FROM [dbo].[SysStringsLoc]
    WHERE([LCID] = @LCID)
        AND ([StringIdentifier] = @StringIdentifier);
    RETURN @Result;
END;

