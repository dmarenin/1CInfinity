USE [AutoInformatorInfinity]
GO
/****** Object:  StoredProcedure [dbo].[_DELETE_RECORD_AutoInformatorInfinity]    Script Date: 09.09.2016 9:03:17 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[_DELETE_RECORD_AutoInformatorInfinity](	
	
	@idreg nvarchar(36)
)
AS
BEGIN

	SET NOCOUNT ON;
	
	DELETE FROM AutoInformatorInfinity  
	WHERE idreg = @idreg; 
	 
END
