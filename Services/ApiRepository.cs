using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using Dynamitey;

using SqlToRestApi.Models;

namespace SqlToRestApi.Services
{
    public class ApiRepository
    {
        private static readonly SqlConnection DbCon = new SqlConnection();

        public static SqlConnection Connection
        {
            get
            {
                if (DbCon.State == ConnectionState.Open)
                    return DbCon;
                DbCon.ConnectionString = Config.ApiDbConnectionString;
                DbCon.Open();
                return DbCon;
            }
        }

        public static void CloseConnection()
        {
            if (DbCon.State == ConnectionState.Open)
                DbCon.Close();
        }

        public static IEnumerable<dynamic> GetDynData(string query)
        {
            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            using (var rs = q.ExecuteReader())
            {
                while (rs.Read())
                {
                    dynamic dynamicDto = new DynamicContext();

                    var fList = new List<string>();

                    for (var i = 0; i < rs.FieldCount; i++)
                    {
                        var columnName = rs.GetName(i).ToLower().Trim();
                        var columnValue = rs[i];

                        var orgColumnName = columnName;
                        var counter = 1;
                        while (fList.Contains(columnName))
                        {
                            columnName = orgColumnName + (counter++).ToString();
                        }

                        fList.Add(columnName);

                        Dynamic.InvokeSet(dynamicDto, columnName, columnValue);                        
                    }

                    yield return dynamicDto;
                }
                rs.Close();
                
            }
        }

        public static List<string> GetDataList(string query)
        {
            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            var viewsList = new List<string>();

            using (var rs = q.ExecuteReader())
            {
                while (rs.Read())
                {
                    if (!Equals(rs[0], DBNull.Value))
                        viewsList.Add(rs[0].ToString().ToUpper());
                }
                rs.Close();
            }
            return viewsList;
        }

        public static List<string> GetViews()
        {
            var query = @"select right(name, len(name)-6) as name from sys.views where left(name, 6) = 'v_api_'";
            return GetDataList(query);
        }

        public static List<string> GetColumns(string viewname)
        {
            var query = $@"SELECT '['+COLUMN_NAME+']'
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = N'v_api_{viewname.Trim()}'";
            return GetDataList(query);
        }

        public static void SetData(string query, object obj)
        {
            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            foreach (var prop in obj.GetType().GetProperties())
            {
                var param = new SqlParameter
                {
                    ParameterName = "@" + prop.Name,
                    Value = prop.GetValue(obj) ?? DBNull.Value
                };
                q.Parameters.Add(param);
            }
            q.ExecuteNonQuery();
        }

        public static void Exec(string query)
        {
            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            q.ExecuteNonQuery();
        }

        public static List<T> GetData<T>(string query)
        {
            var list = new List<T>();

            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            using (var rs = q.ExecuteReader())
            {
                var schema = rs.GetSchemaTable();

                while (rs.Read())
                {
                    if (typeof(T) == typeof(string))
                    {
                        list.Add((T)rs[0]);
                    }
                    else
                    {
                        var obj = Activator.CreateInstance<T>();
                        foreach (var prop in obj.GetType().GetProperties())
                        {
                            if (schema != null && !schema.Select($"ColumnName = '{prop.Name}'").Any())
                                continue;
                            if (!Equals(rs[prop.Name], DBNull.Value))
                            {
                                prop.SetValue(obj, rs[prop.Name], null);
                            }
                        }
                        list.Add(obj);
                    }
                }
                rs.Close();
            }
            return list;
        }

        public static string ValidateKey(string key)
        {
            var query = $"SELECT dbo.f_validate_key ('{key}') as [user]";

            string user = null;

            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            object res;
            try
            {
                res = q.ExecuteScalar();
            }
            catch (SqlException)
            {
                res = DBNull.Value;
            }            

            if (!Equals(res, DBNull.Value))
                user = res.ToString();

            return user;
        }

        public static DataTable GetTable(string query)
        {
            var q = new SqlCommand
            {
                Connection = Connection,
                CommandTimeout = Config.SqlCommandTimeout,
                CommandText = query
            };

            var dt = new DataTable();

            using (var rs = q.ExecuteReader())
            {
                dt.Load(rs);
            }

            return dt;
        }       

    }
}