-- !!!!! NEED TO UPDATE HARD CODED CACHE DATABASE ON LINES 90-92 !!!!!

declare @fetchsqlstring nvarchar(4000)
declare @dropsqlstring nvarchar(4000)
declare @parmdefinition nvarchar(500)
declare @cache_table_name varchar(500)
declare @cache_table_test varchar(500)
declare @cacheDB varchar(128)
declare @result_id int
declare @pos int
declare @cache varchar(255)

create table #DeleteList (result_id int null, cache_table_name varchar(255) null)

--get all results that have more than one occurence per dd_id and security context
insert into #DeleteList(result_id, cache_table_name)
select dr.result_id, cache_table_name
from [central_user].[dash_result] dr
  join (select DVValues, [result_id], row_number() over (partition by [fk_dd_id], DVList.DVValues order by r.[fk_dd_id] asc, e.[date_complete] desc ) occurance
        from [central_user].[dash_result] r
          join [central_user].[pt_object_execution] e on e.unique_id = r.fk_object_execution_id
					join (select distinct e2.unique_id, 
									isnull((stuff((select ','+ cast([fk_distinguishing_value_resource_id] as varchar(50))
												 from [central_user].[pt_object_sql_distinguishing_value_restriction] dv2 
												 where dv2.[fk_object_sql_id] = dv.[fk_object_sql_id] 
												 for xml path ('')),1,1,'')),'Unrestricted') DVValues
								from [central_user].[pt_object_execution] e2
									join [central_user].[pt_object_sql] os on os.fk_object_execution_id = e2.unique_id
									left join [central_user].[pt_object_sql_distinguishing_value_restriction] dv on dv.fk_object_sql_id = os.sql_id
								) DVList on DVList.unique_id = e.unique_id					
					) Deletelist on dr.result_id = Deletelist.result_id					
join [central_user].[pt_object_execution] e1 on e1.unique_id = fk_object_execution_id
where occurance >=2
order by e1.date_complete desc

--delete cache tables for those results
declare c cursor
    local static forward_only read_only
    for
	  select result_id, cache_table_name from #DeleteList
		
open c;
 
fetch next from c into @result_id, @cache ;
 
while @@fetch_status = 0
begin	
	
	--extract the cache DB and cache table
	set @cacheDB = left(@cache, (CHARINDEX('.',@cache))-1)
	set @pos = CHARINDEX('.',reverse(@cache)) - 1
	set @cache_table_name = right(@cache, @pos)
	set @cache_table_name = replace(@cache_table_name,']','')
	set @cache_table_name = replace(@cache_table_name,'[','')

	--only delete if the cache database exists
	if exists(select * from sys.databases d where d.name = replace(replace(@cacheDB,']',''),'[',''))
	begin
		
		--test to see if the table exists
		set @cache_table_test = NULL
		set @fetchsqlstring = 
		 N'select @cache_tableout = name from ' + @cacheDB + '.dbo.sysobjects	where xtype = ''u'' and name = ''' + @cache_table_name + ''''
		set @parmdefinition = N'@cache_tableout varchar(128) output'
		
		execute sp_executesql @fetchsqlstring, @parmdefinition, @cache_tableout=@cache_table_test output

		--now drop table
		if @cache_table_test is not null
		begin
			set @dropsqlstring = 'drop table ' + @cacheDB + '.[cache_autogen].[' + @cache_table_test + ']'
			print 'dropping: ' + @cacheDB + '.[cache_autogen].[' + @cache_table_test + ']'
			exec (@dropsqlstring)			
		end		
	end
	fetch next from c into @result_id, @cache;
end

close c;
deallocate c;

--now delete the results
delete [central_user].[dash_result]
from [central_user].[dash_result] r
 join #DeleteList d on d.result_id = r.result_id

--now get all the rest of the cache tables that can be deleted
delete #DeleteList
insert into #DeleteList(result_id, cache_table_name)
select null, '[QA_Cache_2016].[cache_autogen].[' + name + ']'
from [QA_Cache_2016].dbo.sysobjects o
  join [QA_Cache_2016].[INFORMATION_SCHEMA].[TABLES] s on s.TABLE_NAME = o.name   
where xtype = 'u'  
  and datediff(hh,crdate, getdate()) > 24
	and s.TABLE_SCHEMA = 'cache_autogen'
	and not exists(select * 
	               from [central_user].[dash_result] r
								   join [central_user].[pt_object_execution] e on e.unique_id = r.fk_object_execution_id
								 where charindex(o.name, e.cache_table_name) > 0)


--delete cache tables for those results
declare c cursor
    local static forward_only read_only
    for
	  select result_id, cache_table_name from #DeleteList
		
open c;
 
fetch next from c into @result_id, @cache ;
 
while @@fetch_status = 0
begin	
	
	--extract the cache DB and cache table
	set @cacheDB = left(@cache, (CHARINDEX('.',@cache))-1)
	set @pos = CHARINDEX('.',reverse(@cache)) - 1
	set @cache_table_name = right(@cache, @pos)
	set @cache_table_name = replace(@cache_table_name,']','')
	set @cache_table_name = replace(@cache_table_name,'[','')

	--only delete if the cache database exists
	if exists(select * from sys.databases d where d.name = replace(replace(@cacheDB,']',''),'[',''))
	begin
		
		--test to see if the table exists
		set @cache_table_test = NULL
		set @fetchsqlstring = 
		 N'select @cache_tableout = name from ' + @cacheDB + '.dbo.sysobjects	where xtype = ''u'' and name = ''' + @cache_table_name + ''''
		set @parmdefinition = N'@cache_tableout varchar(128) output'
		
		execute sp_executesql @fetchsqlstring, @parmdefinition, @cache_tableout=@cache_table_test output

		--now drop table
		if @cache_table_test is not null
		begin
			set @dropsqlstring = 'drop table ' + @cacheDB + '.[cache_autogen].[' + @cache_table_test + ']'			
			print 'dropping: ' + @cacheDB + '.[cache_autogen].[' + @cache_table_test + ']'
			exec (@dropsqlstring)			 			
		end		
	end
	fetch next from c into @result_id, @cache;
end

close c;
deallocate c;

return




