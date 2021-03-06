USE [AutoInformatorInfinity]
GO
/****** Object:  StoredProcedure [dbo].[_INSERT_RECORD_AutoInformatorInfinity]    Script Date: 09.09.2016 9:02:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[_INSERT_RECORD_AutoInformatorInfinity](

	@tel nvarchar(50),
	@text nvarchar(MAX),
	@idstr nvarchar(36),
	@idreg nvarchar(36),
	@idcomp nvarchar(150)
					
)

AS
BEGIN

SET NOCOUNT ON;

INSERT INTO AutoInformatorInfinity(tel, text, idstr, idreg, idcomp)

VALUES (@tel, @text, @idstr, @idreg, @idcomp)

END
