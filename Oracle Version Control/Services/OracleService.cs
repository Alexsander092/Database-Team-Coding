using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Oracle_Version_Control.Services;

public class OracleService
{
    private OracleConnection? _connection;
    private readonly Dictionary<string, OracleCommand> _commandCache = new();
    public string CurrentUser { get; } = Environment.UserName.ToUpper();

    private const int COMMAND_TIMEOUT = 30; 
    private const int POOLING_SIZE = 5;
    
    private DataTable? _lastQueryResultCache;
    private string? _lastQueryHash;

    public async Task<bool> ConnectAsync(string username, string password, string dataSource, string tnsPath)
    {
        try
        {
            await DisconnectAsync();

            string connectionString = $"Data Source={dataSource};" +
                                     $"User Id={username};" +
                                     $"Password={password};" +
                                     $"Connection Timeout=60;" +
                                     $"TNS_ADMIN={tnsPath};" +
                                     $"Pooling=true;" + 
                                     $"Min Pool Size=1;" +
                                     $"Max Pool Size={POOLING_SIZE};" +
                                     $"Statement Cache Size=20;" +
                                     $"Self Tuning=true";

            _connection = new OracleConnection(connectionString);
            await _connection.OpenAsync();
            
            _commandCache.Clear();
            _lastQueryResultCache = null;
            _lastQueryHash = null;
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        foreach (var cmd in _commandCache.Values)
        {
            cmd.Dispose();
        }
        _commandCache.Clear();
        _lastQueryResultCache = null;
        
        if (_connection != null && _connection.State != ConnectionState.Closed)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
            _connection = null;
        }
    }

    private async Task<bool> EnsureConnected()
    {
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Database not connected.");
        }
        return true;
    }

    private OracleCommand GetOrCreateCommand(string commandText, CommandType commandType = CommandType.Text)
    {
        string commandHash = $"{commandType}:{commandText}";
        
        if (_commandCache.TryGetValue(commandHash, out OracleCommand? existingCmd))
        {
            return existingCmd;
        }
        
        var cmd = new OracleCommand(commandText, _connection)
        {
            CommandType = commandType,
            CommandTimeout = COMMAND_TIMEOUT
        };
        
        if (commandType == CommandType.Text)
        {
            _commandCache[commandHash] = cmd;
        }
        
        return cmd;
    }
    
    private string CreateQueryHash(string query, Dictionary<string, object> parameters)
    {
        var hash = query;
        if (parameters != null)
        {
            foreach (var param in parameters.OrderBy(p => p.Key))
            {
                hash += $"|{param.Key}={param.Value}";
            }
        }
        return hash;
    }

    public async Task CheckoutObjectAsync(string objectName, string objectType, string comment = "")
    {
        await EnsureConnected();
        
        using var cmd = new OracleCommand("VSC_E2.CHECKOUT", _connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = COMMAND_TIMEOUT
        };

        cmd.Parameters.Add("nomeObjeto", OracleDbType.Varchar2).Value = objectName;
        cmd.Parameters.Add("tipoObjeto", OracleDbType.Varchar2).Value = objectType;
        cmd.Parameters.Add("comentario", OracleDbType.Varchar2).Value = comment ?? string.Empty;

        await cmd.ExecuteNonQueryAsync();
        _lastQueryResultCache = null;
    }

    public async Task CheckinObjectAsync(string objectName, string objectType, string comment = "")
    {
        await EnsureConnected();
        
        using var cmd = new OracleCommand("VSC_E2.CHECKIN", _connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = COMMAND_TIMEOUT
        };

        cmd.Parameters.Add("nomeObjeto", OracleDbType.Varchar2).Value = objectName;
        cmd.Parameters.Add("tipoObjeto", OracleDbType.Varchar2).Value = objectType;
        cmd.Parameters.Add("comentario", OracleDbType.Varchar2).Value = comment ?? string.Empty;

        await cmd.ExecuteNonQueryAsync();
        _lastQueryResultCache = null;
    }

    public async Task<DataTable> GetObjectStatusAsync(string objectName)
    {
        await EnsureConnected();
        
        string sql = 
            @"SELECT 
                PSL_OBJECT_NAME AS Objeto,
                PSL_OBJECT_TYPE AS ObjectType,
                PSL_CHECKED_OUT AS Status,
                PSL_CHECKED_OUT_BY AS Usuario,
                TO_CHAR(PSL_CHECK_OUT_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkout,
                TO_CHAR(PSL_CHECK_IN_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkin,
                PSL_COMMENTS AS Comments
            FROM toad.tc_objstatus
            WHERE UPPER(PSL_OBJECT_NAME) = UPPER(:objName)";
        
        var parameters = new Dictionary<string, object>
        {
            { "objName", objectName }
        };
        
        string queryHash = CreateQueryHash(sql, parameters);
        if (_lastQueryHash == queryHash && _lastQueryResultCache != null)
        {
            return _lastQueryResultCache;
        }

        var cmd = GetOrCreateCommand(sql);
        cmd.Parameters.Clear();
        cmd.Parameters.Add("objName", OracleDbType.Varchar2).Value = objectName;

        using var da = new OracleDataAdapter(cmd);
        var dt = new DataTable();
        await Task.Run(() => da.Fill(dt));
        
        _lastQueryHash = queryHash;
        _lastQueryResultCache = dt;
        
        return dt;
    }

    public async Task<DataTable> GetUserCheckoutObjectsAsync(string userName)
    {
        await EnsureConnected();
        
        string sql = 
            @"SELECT 
                PSL_OBJECT_NAME AS Objeto,
                PSL_OBJECT_TYPE AS ObjectType,
                PSL_CHECKED_OUT AS Status,
                PSL_CHECKED_OUT_BY AS Usuario,
                TO_CHAR(PSL_CHECK_OUT_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkout,
                TO_CHAR(PSL_CHECK_IN_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkin,
                PSL_COMMENTS AS Comments
            FROM toad.tc_objstatus
            WHERE UPPER(PSL_CHECKED_OUT_BY) = UPPER(:userName)
            AND PSL_CHECKED_OUT = 'Y'
            ORDER BY PSL_CHECK_OUT_TIMESTAMP DESC";
        
        var parameters = new Dictionary<string, object>
        {
            { "userName", userName }
        };
        
        string queryHash = CreateQueryHash(sql, parameters);
        if (_lastQueryHash == queryHash && _lastQueryResultCache != null)
        {
            return _lastQueryResultCache;
        }

        var cmd = new OracleCommand(sql, _connection)
        {
            CommandTimeout = COMMAND_TIMEOUT
        };
        cmd.Parameters.Add("userName", OracleDbType.Varchar2).Value = userName;

        using var da = new OracleDataAdapter(cmd);
        var dt = new DataTable();
        
        try
        {
            await Task.Run(() => da.Fill(dt));
            _lastQueryHash = queryHash;
            _lastQueryResultCache = dt;
            return dt;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DataTable> SearchObjectsAsync(string searchTerm, string sortField, bool ascending, int maxRows = 50, int startRow = 0)
    {
        await EnsureConnected();

        string dbField = MapSortFieldToDatabaseField(sortField);
        string sortDirection = ascending ? "ASC" : "DESC";
        
        string sql = $@"SELECT * FROM (
            SELECT a.*, ROWNUM rnum FROM (
                SELECT 
                    PSL_OBJECT_NAME AS Objeto,
                    PSL_OBJECT_TYPE AS ObjectType,
                    PSL_CHECKED_OUT AS Status,
                    PSL_CHECKED_OUT_BY AS Usuario,
                    TO_CHAR(PSL_CHECK_OUT_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkout,
                    TO_CHAR(PSL_CHECK_IN_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkin,
                    PSL_COMMENTS AS Comments
                FROM toad.tc_objstatus
                WHERE UPPER(PSL_OBJECT_NAME) LIKE '%' || UPPER(:search) || '%'
                ORDER BY {dbField} {sortDirection}
            ) a WHERE ROWNUM <= :maxRow
        ) WHERE rnum > :startRow";
        
        var parameters = new Dictionary<string, object>
        {
            { "search", searchTerm },
            { "sortField", dbField },
            { "sortDirection", sortDirection },
            { "maxRow", maxRows + startRow },
            { "startRow", startRow }
        };
        
        string queryHash = CreateQueryHash(sql, parameters);
        if (_lastQueryHash == queryHash && _lastQueryResultCache != null)
        {
            return _lastQueryResultCache;
        }
        
        var cmd = new OracleCommand(sql, _connection)
        {
            CommandTimeout = COMMAND_TIMEOUT
        };
        cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = searchTerm;
        cmd.Parameters.Add("maxRow", OracleDbType.Int32).Value = maxRows + startRow;
        cmd.Parameters.Add("startRow", OracleDbType.Int32).Value = startRow;

        using var da = new OracleDataAdapter(cmd);
        var dt = new DataTable();
        
        try
        {
            await Task.Run(() => da.Fill(dt));
            _lastQueryHash = queryHash;
            _lastQueryResultCache = dt;
            return dt;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (!_commandCache.Values.Contains(cmd))
            {
                cmd.Dispose();
            }
        }
    }

    public async Task<DataTable> SearchObjectsByColumnAsync(string column, string searchTerm, string sortField, bool ascending, int maxRows = 50, int startRow = 0)
    {
        await EnsureConnected();

        string dbField = MapSortFieldToDatabaseField(sortField);
        string sortDirection = ascending ? "ASC" : "DESC";
        string dbSearchField = column switch
        {
            "Objeto" => "PSL_OBJECT_NAME",
            "ObjectType" => "PSL_OBJECT_TYPE",
            "Status" => "PSL_CHECKED_OUT",
            "Usuario" => "PSL_CHECKED_OUT_BY",
            "Comments" => "PSL_COMMENTS",
            _ => "PSL_OBJECT_NAME"
        };

        string whereClause = "1=1";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (dbSearchField == "PSL_CHECKED_OUT")
            {
                whereClause = $"{dbSearchField} = :search";
            }
            else
            {
                whereClause = $"UPPER({dbSearchField}) LIKE '%' || UPPER(:search) || '%'";
            }
        }

        string sql = $@"SELECT * FROM (
            SELECT a.*, ROWNUM rnum FROM (
                SELECT 
                    PSL_OBJECT_NAME AS Objeto,
                    PSL_OBJECT_TYPE AS ObjectType,
                    PSL_CHECKED_OUT AS Status,
                    PSL_CHECKED_OUT_BY AS Usuario,
                    TO_CHAR(PSL_CHECK_OUT_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkout,
                    TO_CHAR(PSL_CHECK_IN_TIMESTAMP, 'DD/MM/YYYY HH24:MI') AS Checkin,
                    PSL_COMMENTS AS Comments
                FROM toad.tc_objstatus
                WHERE {whereClause}
                ORDER BY {dbField} {sortDirection}
            ) a WHERE ROWNUM <= :maxRow
        ) WHERE rnum > :startRow";

        var parameters = new Dictionary<string, object>
        {
            { "search", searchTerm },
            { "maxRow", maxRows + startRow },
            { "startRow", startRow }
        };

        string queryHash = CreateQueryHash(sql, parameters);
        if (_lastQueryHash == queryHash && _lastQueryResultCache != null)
        {
            return _lastQueryResultCache;
        }

        var cmd = new OracleCommand(sql, _connection)
        {
            CommandTimeout = COMMAND_TIMEOUT
        };
        if (!string.IsNullOrWhiteSpace(searchTerm))
            cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = searchTerm;
        cmd.Parameters.Add("maxRow", OracleDbType.Int32).Value = maxRows + startRow;
        cmd.Parameters.Add("startRow", OracleDbType.Int32).Value = startRow;

        using var da = new OracleDataAdapter(cmd);
        var dt = new DataTable();
        try
        {
            await Task.Run(() => da.Fill(dt));
            _lastQueryHash = queryHash;
            _lastQueryResultCache = dt;
            return dt;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (!_commandCache.Values.Contains(cmd))
            {
                cmd.Dispose();
            }
        }
    }

    private string MapSortFieldToDatabaseField(string sortField)
    {
        string dbField = sortField switch
        {
            "Objeto" => "PSL_OBJECT_NAME",
            "ObjectType" => "PSL_OBJECT_TYPE",
            "Status" => "PSL_CHECKED_OUT",
            "Usuario" => "PSL_CHECKED_OUT_BY",
            "Checkout" => "PSL_CHECK_OUT_TIMESTAMP",
            "Checkin" => "PSL_CHECK_IN_TIMESTAMP",
            _ => "PSL_CHECK_IN_TIMESTAMP" 
        };
        
        return dbField;
    }

    private string MapAppObjectTypeToDbmsMetadata(string appType)
    {
        return appType.Trim().ToUpperInvariant() switch
        {
            "PACKAGE" => "PACKAGE",
            "PACKAGE BODY" => "PACKAGE_BODY",
            "PROCEDURE" => "PROCEDURE",
            "FUNCTION" => "FUNCTION",
            "TRIGGER" => "TRIGGER",
            "TYPE" => "TYPE",
            "TYPE BODY" => "TYPE_BODY",
            "VIEW" => "VIEW",
            "PACKAGEBODY" => "PACKAGE_BODY",
            "PACKAGE_BODY" => "PACKAGE_BODY",
            "TYPEBODY" => "TYPE_BODY",
            "TYPE_BODY" => "TYPE_BODY",
            var t when t.Contains("PACKAGE") && t.Contains("BODY") => "PACKAGE_BODY",
            var t when t.Contains("TYPE") && t.Contains("BODY") => "TYPE_BODY",
            _ => appType.Trim().ToUpperInvariant()
        };
    }

    public async Task<string?> GetObjectDdlAsync(string objectType, string objectName)
    {
        await EnsureConnected();
        string dbmsType = MapAppObjectTypeToDbmsMetadata(objectType);
        string sql = "SELECT DBMS_METADATA.GET_DDL(:type, :name) AS full_text FROM DUAL";
        using var cmd = new OracleCommand(sql, _connection)
        {
            CommandTimeout = COMMAND_TIMEOUT
        };
        cmd.Parameters.Add(":type", OracleDbType.Varchar2).Value = dbmsType;
        cmd.Parameters.Add(":name", OracleDbType.Varchar2).Value = objectName;
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader["full_text"]?.ToString();
        }
        return null;
    }
}