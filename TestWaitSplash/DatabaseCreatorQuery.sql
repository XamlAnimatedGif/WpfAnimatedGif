Declare @logPath nvarchar(256),
        @dataPath nvarchar(256),
        @dbName nvarchar(256);
                     
SET @dbName = 'TestDB'
                     
SET @logPath = (select 
                    LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1) 
                from sys.master_files 
                where name = 'modellog') + @dbName + '.ldf'
                     
SET @dataPath = (select 
                    LEFT(physical_name, LEN(physical_name)  - CHARINDEX('\', REVERSE(physical_name)) + 1) 
                    from sys.master_files 
                    where name = 'modeldev') + @dbName + '_log.mdf'
                     
                     
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
BEGIN
	DECLARE @createDatabase NVARCHAR(MAX)
	SET @createDatabase = 'CREATE DATABASE [' + @dbName + '] ON  PRIMARY 
	    ( NAME = N''' + @dbName + ''', FILENAME = N''' + @dataPath + ''', SIZE = 51200KB , FILEGROWTH = 10240KB )
	    LOG ON 
	    ( NAME = N''' + @dbName + '' + '_log'' , FILENAME = N''' + @logPath + ''' , SIZE = 5120KB , FILEGROWTH = 5120KB )'
	                 
	EXEC sp_executesql @createDatabase
END

DECLARE @CreateTable NVARCHAR(4000) =
	'USE TestDB
	-- Object:  Table [dbo].[TestTable]    Script Date: 03/25/2015 09:09:44
	IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''TestTable'') AND type in (N''U''))
		CREATE TABLE [dbo].[TestTable](
			[ID] [bigint] IDENTITY(1,1) NOT NULL,
			[Name] [nvarchar](50) NULL,
			[Mobile] [numeric](18, 0) NULL
		) ON [PRIMARY]'
		
EXEC (@CreateTable)