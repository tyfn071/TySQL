using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace TySQL
{
    /// <summary>
    /// Creating SQL Connection Strings
    /// </summary>
    public sealed class TySQLStringBuilder
    {
        public TySQLStringBuilder()
        {

        }
        public TySQLStringBuilder(string server,string database, string userID,string password)
        {
            Server = server;
            Database = database;
            UserID = userID;
            Password = password;
        }
        /// <summary>
        /// SQL Server Name
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Database Name
        /// </summary>
        public string Database { get; set; }
        /// <summary>
        /// SQL Username
        /// </summary>
        public string UserID { get; set; }
        /// <summary>
        /// SQL User Password
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Get SQL Connection String
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"Data Source={Server}; Initial Catalog={Database}; User Id={UserID}; Password={Password}";

    }

    /// <summary>
    /// SQL Connection struct
    /// </summary>
    public sealed class TySQLConnect : IDisposable
    {
        private string ConnectionString;

        public TySQLConnect()
        {
        }
        public TySQLConnect(string connectionString)
        {
            ConnectionString = connectionString;
            Open();
        }

        private SqlConnection c = new SqlConnection();
        /// <summary>
        /// Get SQL Connection. Call Open() before!
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetSQLConnection()
            => c ?? new SqlConnection();
        public ConnectionState GetConnectionState()
            => c == null ? ConnectionState.Closed : c.State;
        /// <summary>
        /// Start SQL Connection
        /// </summary>
        public void Open()
        {
            if (c.State != ConnectionState.Open)
            {
                c.ConnectionString = ConnectionString;
                c.Open();
            }
        }
        /// <summary>
        /// Start SQL Connection
        /// </summary>
        public void Open(string connectionString)
        {
            ConnectionString = connectionString;
            Open();
        }

        /// <summary>
        /// Close SQL Connection
        /// </summary>
        public void Close()
        {
            c.Close();
        }

        public void Dispose()
        {
            c.Dispose();
        }
    }
    /// <summary>
    /// Create Sql parameter object easly
    /// </summary>
    public sealed class TySQLParam
    {

        public TySQLParam()
        {
        }
        
        public TySQLParam(string name, SqlDbType type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
        public TySQLParam(string name, SqlDbType type, object value, int length)
        {
            Name = name;
            Type = type;
            Value = value;
            Length = length;
        }
        public int Length { get; set; }
        public object Value { get; set; }
        public SqlDbType Type { get; set; }
        public string Name { get; set; }
    }
    public sealed class TySQLProc:IDisposable
    {
        public TySQLProc()
        {

        }
        public TySQLProc(string connectionString, string query)
        {
            ConnectionString = connectionString;
            Type = TySQLType.OnlyQuery;
            Query = query;
        }
        public TySQLProc(string connectionString, string query, List<TySQLParam> _params,CommandType comtype=CommandType.Text)
        {
            ConnectionString = connectionString;
            Type = TySQLType.QueryWithParams;
            Query = query;
            Params = _params;
            SqlCommandType = comtype;
        }        

        private string Query;
        /// <summary>
        /// Set SQL query string
        /// </summary>
        /// <param name="value"></param>
        public void SetQuery(string value)
            => Query = value;
        /// <summary>
        /// Get SQL query string
        /// </summary>
        /// <returns></returns>
        public string GetQuery()
            => Query;

        private string ConnectionString;
        public List<TySQLParam> Params { get; set; }
        public CommandType SqlCommandType { get; set; }
        public TySQLType Type { get; set; }
      

       
        private TySQLConnect Connection = new TySQLConnect();

        /// <summary>
        ///  Execute query async
        /// </summary>
        public async Task<int> ExecuteAsync()
        {
            Connection.Open(ConnectionString);
            int s=0;
            using (SqlCommand comm = new SqlCommand(Query, Connection.GetSQLConnection()))
            {
                CheckTySqlType(comm);
               s= await comm.ExecuteNonQueryAsync();
            }
            Connection.Close();
            return s;

        }

        /// <summary>
        /// Execute query
        /// </summary>
        public int Execute()
        {
            Connection.Open(ConnectionString);
            int s = 0;
            using (SqlCommand comm = new SqlCommand(Query, Connection.GetSQLConnection()))
            {
                CheckTySqlType(comm);
                s=comm.ExecuteNonQuery();
            }
            Connection.Close();
            return s;

        }


        /// <summary>
        ///  Get first column from query result. <T> must be column datatype
        /// </summary>        
        public T GetFirstData<T>()
        {
            T s;
            Connection.Open(ConnectionString);

            using (SqlCommand comm = new SqlCommand(Query, Connection.GetSQLConnection()))
            {
                CheckTySqlType(comm);
                DataTable table = new DataTable();
                SqlDataAdapter adapt = new SqlDataAdapter(comm);
                adapt.Fill(table);

                if (table.Rows.Count > 0)
                {
                    DataRow row = table.Rows[0];
                    s = (T)row[0];
                }
                else
                    s = default;
            }
            Connection.Close();

            return s;

        }
        
        /// <summary>
        ///  If have rows return true
        /// </summary>
        public bool CheckData()
        {
            bool result = false;
            using (TySQLConnect conn = new TySQLConnect(ConnectionString))
            {
                using (SqlCommand comm = new SqlCommand(Query, conn.GetSQLConnection()))
                {
                    CheckTySqlType(comm);

                    using (SqlDataReader reader = comm.ExecuteReader())
                    {
                        if (reader.HasRows)
                            result= true;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Preparing Sql Command
        /// </summary>
        /// <param name="comm"></param>
        private void CheckTySqlType(SqlCommand comm)
        {
            if (Type == TySQLType.QueryWithParamsAndCommandType)
                comm.CommandType = SqlCommandType;

            if (Type == TySQLType.QueryWithParamsAndCommandType || Type == TySQLType.QueryWithParams)
            {
                foreach (TySQLParam param in Params)
                {

                    if (param.Length > 0)
                        comm.Parameters.Add(param.Name, param.Type, param.Length);
                    else
                        comm.Parameters.Add(param.Name, param.Type);

                    comm.Parameters[param.Name].Value = param.Value;

                }
            }
        }

        /// <summary>
        /// return DataTable result
        /// </summary>
        /// <returns></returns>
        public async Task<DataTable> GetDataTableAsync()
        {
            DataTable table = new DataTable();
            Task t = Task.Factory.StartNew(() =>
                {

                    Connection.Open(ConnectionString);
                using (SqlCommand comm = new SqlCommand(Query, Connection.GetSQLConnection()))
                    {
                        CheckTySqlType(comm);
                        SqlDataAdapter adapt = new SqlDataAdapter(comm);
                        adapt.Fill(table);
                        Connection.Close();

                    }
                });
            await t;
            return table;
        }

        /// <summary>
        /// Get SQL Parameter value datatype
        /// </summary>
        Func<Object, SqlDbType> getSqlType = val => new SqlParameter("Test", val).SqlDbType;

        public List<T> CreateListFromTable<T>(DataTable table) where T: new()
        {
            List<T> list = new List<T>();

            foreach(DataRow row in table.Rows)
                list.Add(CreateItemFromRow<T>(row));         

            return list;
        }
        public T CreateItemFromRow<T>(DataRow row)where T:new()
        {
            T item = new T();

            SetItemFromRow(item, row);

            return item;
        }

        /// <summary>
        /// Create Paramaters with class public properties!
        /// </summary>
        /// <param name="item"></param>
        public void CreateParameters(object item)
        {
            Params = new List<TySQLParam>();

            item.GetType()
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .ToList()
                     .ForEach(refpro => {

                         if (refpro.GetType() != typeof(List<>))
                             Params.Add(new TySQLParam("@" + refpro.Name, getSqlType(refpro.GetValue(item, null)), refpro.GetValue(item, null)));

                     });


        }

        public void SetItemFromRow<T>(T item,DataRow row) where T:new()
        {
            foreach (DataColumn c in row.Table.Columns)
            {

                PropertyInfo p = item.GetType().GetProperty(c.ColumnName);


                if (p != null && row[c] != DBNull.Value)
                {
                    if (p.PropertyType == typeof(bool))
                        p.SetValue(item, (byte)row[c] == 1, null);
                    else
                        p.SetValue(item, row[c], null);
                }
            }
        }

        public DataTable GetDataTable()
        {
            DataTable table = new DataTable();
            Connection.Open(ConnectionString);

            using (SqlCommand comm = new SqlCommand(Query, Connection.GetSQLConnection()))
            {
                CheckTySqlType(comm);

                SqlDataAdapter adapt = new SqlDataAdapter(comm);
                adapt.Fill(table);

                Connection.Close();

            }

            return table;
        }

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
            GC.SuppressFinalize(this);
            GC.Collect();
        }
    }
    public enum TySQLType
    {
        OnlyQuery = 0,
        QueryWithParams = 1,
        QueryWithParamsAndCommandType = 2
    }
}
