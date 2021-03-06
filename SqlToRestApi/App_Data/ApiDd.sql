---------- FUNCTIONS
/*
Function to validate access key
*/
CREATE FUNCTION [dbo].[f_validate_key]
(	
	@key nvarchar(255)
)
RETURNS nvarchar(50)
AS
BEGIN
	declare @ukey uniqueidentifier

	declare @user nvarchar(50) = null
    
	set @ukey = CONVERT (uniqueidentifier, @key)

	select top 1 @user = [user] from [dbo].[api_keys]
	where [key] = @ukey
	and ([expires_on] >= GETDATE() or [expires_on] is null)
	
	return @user
END
GO


---------- TABLES
/*
Access keys table. AuthorizationRequired Web.config should be 'true' to make keys necessary
*/
CREATE TABLE [dbo].[api_keys](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user] [nvarchar](50) NOT NULL,
	[key] [uniqueidentifier] NOT NULL,
	[issued_on] [datetime] NOT NULL,
	[expires_on] [datetime] NULL,
 CONSTRAINT [PK_api_keys] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/*
Fields description table
*/
CREATE TABLE [dbo].[fields_descr](
	[table_name] [nvarchar](50) NULL,
	[field_name] [nvarchar](50) NULL,
	[descr] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/*
Tables description table
*/
CREATE TABLE [dbo].[tables_descr](
	[table_name] [nvarchar](50) NULL,
	[alter_name] [nvarchar](255) NULL,
	[descr] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


---------- VIEWS
/*
View to generate help: http://localhost:8080/help
*/
CREATE view [dbo].[doc] as 
select right(v.name,len(v.name)-6) as resource
,c.ORDINAL_POSITION as position
, c.COLUMN_NAME as field_name 
,case c.DATA_TYPE
when 'nvarchar' then c.DATA_TYPE + '('+cast(c.CHARACTER_MAXIMUM_LENGTH as nvarchar(10))+')'
when 'varchar' then c.DATA_TYPE + '('+cast(c.CHARACTER_MAXIMUM_LENGTH as nvarchar(10))+')'
when 'numeric' then c.DATA_TYPE + '('+cast(c.NUMERIC_PRECISION as nvarchar(10)) + ',' + cast(c.NUMERIC_SCALE as nvarchar(10))+')'
else c.DATA_TYPE
end as type
,coalesce(nullif(d.[alter_name],''), replace(Upper(substring(v.name,3,1)) + Lower(substring(v.name,4,len(v.name)-3)), '_', ' ')) as table_name
,coalesce(d.[descr], '') as [descr]
,coalesce(f.[descr], '') as [field_descr]
from sys.views v
left join INFORMATION_SCHEMA.COLUMNS c on v.name = c.TABLE_NAME
left join [dbo].[tables_descr] d on v.name = d.[table_name]
left join [dbo].[fields_descr] f on v.name = f.[table_name] and c.COLUMN_NAME = f.[field_name]
where v.name like 'v_api_%'
GO


---------- PROCEDURES
/*
Grant user access to necessary db objects
*/
--exec [dbo].[sp_grant_views_to_api_user] 
CREATE PROCEDURE [dbo].[sp_grant_views_to_api_user] 
	@user_name nvarchar(128) = 'api_user'
as begin
	declare @cmd nvarchar(4000)

	declare cr_grant cursor for
		select 'grant select on [dbo].[' + name + '] to ' + @user_name as name from sys.views where left(name, 6) = 'v_api_'

	open cr_grant

	FETCH NEXT FROM cr_grant INTO @cmd

	WHILE @@FETCH_STATUS = 0  
	BEGIN 
		exec sp_executesql @cmd;

		FETCH NEXT FROM cr_grant INTO @cmd
	END	

	close cr_grant

	DEALLOCATE cr_grant

	set @cmd = 'grant exec on [dbo].[f_validate_key] to ' + @user_name;
	exec sp_executesql @cmd;

	set @cmd = 'grant select on [dbo].[doc] to ' + @user_name;
	exec sp_executesql @cmd;
end
GO

/*
Adds new access key
*/
CREATE PROCEDURE [dbo].[sp_new_key]
	@user nvarchar(50) = null,
	@expiry bigint = null
AS
BEGIN
	DECLARE @key uniqueidentifier  
	DECLARE @issued_on datetime = getdate();
	DECLARE @expires_on datetime = null;

	if @expiry is not null
		SET @expires_on = DATEADD(second, @expiry, @issued_on);

	if @user is null begin
		select @user = 'user' + right('000000' + cast(max(cast(right(t.[user], len(t.[user]) - patindex('%[0-9]%', t.[user])) as int)) + 1 as nvarchar(6)), 6)  
		from (select [user]
				from [dbo].[api_keys]
				where [user] like 'user[0-9][0-9][0-9][0-9][0-9][0-9]'
			  union select 'user000000') t
	end

	SET @key = NEWID()  
	
	insert into [dbo].[api_keys] ([user], [key], [issued_on], [expires_on])
	values (@user, @key, @issued_on, @expires_on)

	select [key] from [dbo].[api_keys] where [id] = SCOPE_IDENTITY()
END
GO


---------- DEMO DATA
/*
This view will be accessible as resource 'brands'
For example: http://localhost:8080/api/brands?brand_id=f,v
*/
CREATE view [dbo].[v_api_brands] as
	select 'F' as brand_id, 'Ford' as brand_name
	union select 'V', 'Volvo'
	union select 'J', 'Jaguar'
	union select 'L', 'Land Rover'
	union select 'P', 'Porsche'
	union select 'B', 'Bentley'
GO
/*
Add some data description tabes (not necessary).
*/
DELETE FROM [dbo].[tables_descr]
GO
INSERT INTO [dbo].[tables_descr]
           ([table_name]
           ,[alter_name]
           ,[descr])
     VALUES
           ('v_api_brands'
           ,'Car Brands'
           ,'Demo data')
GO

DELETE FROM [dbo].[fields_descr]
GO
INSERT INTO [dbo].[fields_descr]
           ([table_name]
           ,[field_name]
           ,[descr])
     VALUES
           ('v_api_brands'
           ,'brand_id'
           ,'Brand ID')
GO
INSERT INTO [dbo].[fields_descr]
           ([table_name]
           ,[field_name]
           ,[descr])
     VALUES
           ('v_api_brands'
           ,'brand_name'
           ,'Brand Name')
GO