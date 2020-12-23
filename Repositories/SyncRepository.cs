using System;
using System.Collections.Generic;
using System.Linq;
using Dedup.ViewModels;
using System.Data;
using Dedup.Common;
using System.Text;
using Dapper;
using Dedup.Extensions;
using Newtonsoft.Json;
using Hangfire;
using Dedup.Models;
using System.Threading;
using System.Threading.Tasks;
using Dedup.HangfireFilters;
using Dedup.ViewComponents;
using System.Web;
using System.Runtime;
using Hangfire.States;

namespace Dedup.Repositories
{
    [Queue("critical")]
    public class SyncRepository : ISyncRepository
    {
        public SyncRepository()
        {
        }

        /// <summary>
        /// Method: SyncTableIsExist
        /// Description: It is used to check sync table exists or not
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns>1/0</returns>
        public static int SyncTableIsExist(ConnectorConfig connectorConfig)
        {
            var recordCount = 0;
            try
            {
                if (connectorConfig.syncDestination == ConnectorType.Azure_SQL)
                {
                    if (connectorConfig.dbConfig != null)
                    {
                        //Remove special chars in table name
                        connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                        //db schema
                        if (string.IsNullOrEmpty(connectorConfig.dbSchema))
                        {
                            connectorConfig.dbSchema = Constants.MSSQL_DEFAULT_SCHEMA;
                        }
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl, DatabaseType.Azure_SQL))
                        {
                            recordCount = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables t WHERE t.object_id = OBJECT_ID('" + connectorConfig.destObjectName + "') AND t.schema_id = SCHEMA_ID('" + connectorConfig.dbSchema + "')");
                        }
                    }
                }
                else if (connectorConfig.syncDestination == ConnectorType.Heroku_Postgres
                    || connectorConfig.syncDestination == ConnectorType.Azure_Postgres
                    || connectorConfig.syncDestination == ConnectorType.AWS_Postgres)
                {
                    if (connectorConfig.destDBConfig != null)
                    {
                        //Remove special chars in table name
                        connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                        //db schema
                        if (string.IsNullOrEmpty(connectorConfig.destDBSchema))
                        {
                            connectorConfig.destDBSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                        }
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                        {
                            recordCount = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT count(*) FROM pg_class c LEFT JOIN pg_namespace n ON n.oid = c.relnamespace where LOWER(n.nspname)=LOWER('" + connectorConfig.destDBSchema + "') AND LOWER(c.relname)=LOWER('" + connectorConfig.destObjectName + "') AND c.relkind='r'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return recordCount;
        }

        /// <summary>
        /// Description : Sync Golden table exist or not
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        public static int SyncGoldenTableIsExist(ConnectorConfig connectorConfig)
        {
            var recordCount = 0;
            try
            {
                if (connectorConfig?.dbConfig_compare.dataSource == DataSource.Azure_SQL)
                {
                    if (connectorConfig?.dbConfig_compare.dataSource != null)
                    {
                        //Remove special chars in table name
                        connectorConfig.dbConfig_compare.new_table_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);
                        //db schema
                        if (string.IsNullOrEmpty(connectorConfig.dbConfig_compare.db_schema))
                        {
                            connectorConfig.dbConfig_compare.db_schema = Constants.MSSQL_DEFAULT_SCHEMA;
                        }
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl, DatabaseType.Azure_SQL))
                        {
                            recordCount = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables t WHERE t.object_id = OBJECT_ID('" + connectorConfig.dbConfig_compare.new_table_name + "') AND t.schema_id = SCHEMA_ID('" + connectorConfig.dbConfig_compare.db_schema + "')");
                        }
                    }
                }
                else if (connectorConfig?.destDBConfig.dataSource == DataSource.Heroku_Postgres
                   || connectorConfig?.destDBConfig.dataSource == DataSource.Azure_Postgres
                   || connectorConfig?.destDBConfig.dataSource == DataSource.AWS_Postgres)
                {
                    if (connectorConfig?.destDBConfig.dataSource != null)
                    {
                        //Remove special chars in table name
                        connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                        //db schema
                        if (string.IsNullOrEmpty(connectorConfig.destDBSchema))
                        {
                            connectorConfig.destDBSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                        }
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                        {
                            recordCount = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT count(*) FROM pg_class c LEFT JOIN pg_namespace n ON n.oid = c.relnamespace where LOWER(n.nspname)=LOWER('" + connectorConfig.destDBSchema + "') AND LOWER(c.relname)=LOWER('" + connectorConfig.destObjectName + "') AND c.relkind='r'");
                        }
                    }
                }
                else if (connectorConfig?.dbConfig_compare.dataSource == DataSource.Heroku_Postgres
                    || connectorConfig?.dbConfig_compare.dataSource == DataSource.Azure_Postgres
                    || connectorConfig?.dbConfig_compare.dataSource == DataSource.AWS_Postgres)
                {
                    if (connectorConfig?.dbConfig_compare.dataSource != null)
                    {
                        //Remove special chars in table name
                        connectorConfig.dbConfig_compare.new_table_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);
                        //db schema
                        if (string.IsNullOrEmpty(connectorConfig.dbConfig_compare.db_schema))
                        {
                            connectorConfig.dbConfig_compare.db_schema = Constants.POSTGRES_DEFAULT_SCHEMA;
                        }
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl))
                        {
                            recordCount = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT count(*) FROM pg_class c LEFT JOIN pg_namespace n ON n.oid = c.relnamespace where LOWER(n.nspname)=LOWER('" + connectorConfig.dbConfig_compare.db_schema + "') AND LOWER(c.relname)=LOWER('" + connectorConfig.dbConfig_compare.new_table_name + "') AND c.relkind='r'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return recordCount;
        }

        public static int GetChildRecordsCount(ConnectorConfig connectorConfig)
        {
            int count = 0;
            try
            {


                if (connectorConfig == null)
                {
                    throw new ArgumentNullException("Dedup config is null");
                }
                if (connectorConfig.unique_records_count != null && !String.IsNullOrEmpty(connectorConfig.destObjectName))
                {
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        //count = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT count(*) FROM pg_class c LEFT JOIN pg_namespace n ON n.oid = c.relnamespace where LOWER(n.nspname)=LOWER('" + connectorConfig.destDBSchema + "') AND LOWER(c.relname)=LOWER('" + connectorConfig.destObjectName + "') AND c.relkind='r'");

                        count = connectionFactory.DbConnection.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}_ctindex\" where parentctid is Not Null;", connectorConfig.destDBSchema, connectorConfig.destObjectName));

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
            }
            return count;
        }
        /// <summary>
        /// Description :Sync ctIndex Table  Is Exist or Not
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        public static int SyncCtIndexTableIsExist(ConnectorConfig connectorConfig, string tableName)
        {
            var recordCount = 0;
            try
            {
                if (connectorConfig.destDBConfig != null)
                {
                    //db schema
                    if (string.IsNullOrEmpty(connectorConfig.destDBSchema))
                    {
                        connectorConfig.destDBSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                    }
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        recordCount = connectionFactory.DbConnection.ExecuteScalar<int>("SELECT count(*) FROM pg_class c LEFT JOIN pg_namespace n ON n.oid = c.relnamespace where LOWER(n.nspname)=LOWER('" + connectorConfig.destDBSchema + "') AND LOWER(c.relname)=LOWER('" + tableName + "') AND c.relkind='r'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return recordCount;
        }
        #region PG Support Functions

        /// <summary>
        /// Method: RemoveDataRowsFromConnector
        /// Description: It is used to clear rows from sync table by table name. It will also reset sync info in connectors table
        /// by ccid and connectorId
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="dbConfig"></param>
        /// <param name="ccid"></param>
        public static void RemoveDataRowsFromConnector(ConnectorConfig connectorConfig)
        {
            try
            {
                if (connectorConfig == null && (connectorConfig != null && (string.IsNullOrEmpty(connectorConfig.ccid) || !connectorConfig.connectorId.HasValue)))
                    return;

                if ((connectorConfig.syncDestination == ConnectorType.Heroku_Postgres
                   || connectorConfig.syncDestination == ConnectorType.Azure_Postgres
                   || connectorConfig.syncDestination == ConnectorType.AWS_Postgres
                   || connectorConfig.syncDestination == ConnectorType.Azure_SQL)
                   && (connectorConfig.dbConfig == null ||
                   (connectorConfig.dbConfig != null && string.IsNullOrEmpty(connectorConfig.dbConfig.syncDefaultDatabaseUrl))))
                {
                    return;
                }


                if (!string.IsNullOrEmpty(connectorConfig.destObjectName))
                {
                    if (connectorConfig.syncDestination == ConnectorType.Azure_SQL)
                    {
                        //  RemoveSqlSyncTableRows(connectorConfig);
                    }
                    else
                    {
                        RemovePGSyncTableRows(connectorConfig);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
        }

        /// <summary>
        /// Method: FindPGRowsByPageNo
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns>IEnumerable<dynamic></returns>
        public static IEnumerable<dynamic> FindPGRowsByPageNo(ConnectorConfig connectorConfig, int cPage, int pageSize)
        {
            IEnumerable<dynamic> resultData = null;
            string tableName = string.Empty;
            string schemaName = string.Empty;
            string connectionURL = string.Empty;
            List<string> fetchDestColumns = (from c in connectorConfig.sourceObjectFields
                                             select $"\"{c}\"").ToList();

            try
            {

                if (connectorConfig.dedup_type == DedupType.Full_Dedup)
                {
                    if ((connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B))
                    {
                        if (connectorConfig.dbConfig_compare.table_type == SelectedTableType.Create_New_Table)
                        {
                            tableName = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);
                            schemaName = connectorConfig.dbConfig_compare.db_schema;
                        }
                        else
                        {
                            tableName = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.object_name);
                            schemaName = connectorConfig.dbConfig_compare.db_schema;
                        }
                        connectionURL = connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl;
                    }
                    else
                    {
                        tableName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
                        schemaName = connectorConfig.dbSchema;
                        connectionURL = connectorConfig.dbConfig.syncDefaultDatabaseUrl;
                    }
                }
                else
                {
                    //Remove special chars in table name
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                    tableName = connectorConfig.destObjectName;
                    schemaName = connectorConfig.destDBSchema;
                    connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;
                }

                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                {
                    StringBuilder sb = new StringBuilder();
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\"", schemaName, tableName));
                    sb.Append(string.Format(" order by " + string.Join(",", fetchDestColumns.Select(c => c).ToArray()) + ""));
                    sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0}", offSet));

                    //excuete query
                    resultData = connectionFactory.DbConnection.Query<dynamic>(sb.ToString());
                    sb = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }

            return resultData;
        }

        /// <summary>
        /// Method: DeDupFromSingleTable
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        public async Task RemoveDuplicatesFromSingleTable(ConnectorConfig connectorConfig)
        {
            try
            {
                if (connectorConfig.dbConfig != null)
                {
                    if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table && connectorConfig.dedup_type == DedupType.Full_Dedup)
                    {
                        var fetchColumns1 = (from c in connectorConfig.syncObjectColumns
                                             join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                             select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "" : "")}").ToArray();

                        StringBuilder sb = new StringBuilder();
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                        {
                            sb.Append(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Total record query=>{sb.ToString()}");
                            int TotalCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Total records:{TotalCount}");
                            UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, total_records_count: TotalCount);
                        }
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                        {
                            using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
                            {
                                try
                                {
                                    sb = new StringBuilder();
                                    sb.Append($"DELETE FROM \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\" c WHERE c.ctid = ANY(ARRAY(SELECT a.ctid FROM \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\" a");
                                    sb.Append($" LEFT OUTER JOIN(SELECT MAX(ctid) as ctid FROM \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\"");
                                    sb.Append(" GROUP BY " + string.Join(",", fetchColumns1.Select(c => c).ToArray()) + ") b on a.ctid=b.ctid WHERE b.ctid is null))");
                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Delete records {sb.ToString()}");
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete started");
                                    var starttime = DateTime.Now;
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete starttime :" + starttime);
                                    int deleted_count = await connectionFactory.DbConnection.ExecuteAsync(sb.ToString());
                                    var endtime = DateTime.Now;
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete endtime :" + endtime);
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete Time Difference :" + (endtime - starttime));
                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Deleted records:{deleted_count}");
                                    sb.Clear();
                                    transaction.Commit();
                                    int syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, deduped_count: deleted_count);
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete ended");
                                }
                                catch (Exception ex)
                                {
                                    WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                                    transaction.Rollback();
                                }
                            }
                        }

                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                        {
                            sb = new StringBuilder();
                            sb.Append(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique record query=>{sb.ToString()}");
                            int uniqueCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique records:{uniqueCount}");
                            UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, totaluniquecount: uniqueCount);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                WriteProcessLogToConsole(connectorConfig.connectorName, exc);
            }
        }

        /// <summary>
        /// Method: GetPGTableRecordCountByFilter
        /// Description: It is used to get total rows count and data size from pg table by table name and filter.
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<PGTableDataSize> GetPGTableRecordCountByFilter(ConnectorConfig connectorConfig, string status = "")
        {
            PGTableDataSize tableDataSize = default(PGTableDataSize);
            try
            {
                if (connectorConfig.dbConfig != null)
                {
                    var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                        join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                        select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();
                    //Remove special chars in table name
                    connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);

                    StringBuilder sb = new StringBuilder();
                    if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table &&
                        (!connectorConfig.isTableExist))
                    {

                        sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\" AS t");
                        // sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\") AS t");

                        //if (connectorConfig.simulation_count == -1)
                        //    sb.Append($"DROP TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\";CREATE TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\" AS TABLE \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\"");
                        //else
                        //    sb.Append($"DROP TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\";CREATE TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\" AS SELECT * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\"");
                    }
                    else
                    {
                        if (status == "compare")
                        {
                            //if (!connectorConfig.isTableExist && connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                            //{
                            //    if (connectorConfig.simulation_count == -1)
                            //        sb.Append($"DROP TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\";CREATE TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\" AS TABLE \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\"");
                            //    else
                            //        sb.Append($"DROP TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\";CREATE TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\" AS SELECT * FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\"");

                            //}
                            //else
                            // {
                            //sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\") AS t");
                            // }
                            sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\" AS t");

                        }
                        else
                        {
                            //if (!connectorConfig.isTableExist && connectorConfig.dedup_type == DedupType.Full_Dedup && connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination)
                            //{
                            //    if (connectorConfig.simulation_count == -1)
                            //        sb.Append($"DROP TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\";CREATE TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\" AS TABLE \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\"");
                            //    else
                            //        sb.Append($"DROP TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\";CREATE TABLE \"{connectorConfig.destDBSchema}\".\"{ connectorConfig.destObjectName}\" AS SELECT * FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\"");

                            //}
                            //else
                            sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\" AS t");

                            // sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\") AS t");
                        }


                        if (connectorConfig.lastSyncAt.HasValue)
                        {
                            //Add -1 min
                            connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                            DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                            if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                                    && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                                sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                            }
                            else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            }
                            else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            }
                        }
                    }
                    if (connectorConfig.simulation_count != -1)
                    {
                        sb.Append(" LIMIT " + connectorConfig.simulation_count + ";");
                    }
                    else
                        sb.Append(";");
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch pg records size & count query=>{sb.ToString()}");
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                    {
                        var qryResult = await connectionFactory.DbConnection.QueryAsync<PGTableDataSize>(sb.ToString()).ConfigureAwait(false);
                        //if (!connectorConfig.isTableExist)
                        //{
                        //    tableDataSize = null;
                        //}
                        //else
                        //{
                        if (qryResult != null && qryResult.Count() > 0)
                        {
                            tableDataSize = qryResult.FirstOrDefault();
                        }
                        //}
                        sb.Clear();
                        sb = null;
                    }
                }
            }
            catch
            {
                throw;
            }
            return tableDataSize;
        }

        /// <summary>
        /// Method: DeDupPGTableRecordsByFilter
        /// Description: It is used to delete the duplicate records from pg table with filter.
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns>rows count as int</returns>
        public async Task<int> DeDupPGTableRecordsByFilter(ConnectorConfig connectorConfig)
        {
            int recordCount = 0;
            StringBuilder sb = new StringBuilder();
            string tableName = string.Empty;
            string schemaName = string.Empty;
            string connectionURL = string.Empty;
            try
            {
                tableName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                schemaName = connectorConfig.destDBSchema;
                connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;

                if (connectorConfig.review_before_delete == ReviewBeforeDeleteDups.No)
                {


                    if (connectorConfig.backup_before_delete == ArchiveRecords.Yes)
                    {
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                        {
                            int count = SyncCtIndexTableIsExist(connectorConfig, tableName + "_deleted");
                            if (count == 1)
                            {
                                sb.Append($"INSERT INTO \"{schemaName}\".\"{tableName}_deleted\" SELECT * FROM \"{schemaName}\".\"{tableName}\"");
                                sb.Append($" WHERE ctid in (select myctid from \"{schemaName}\".\"{tableName}_ctindex\" where parentctid is not null);");
                            }
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Insert query for deleted table=>{sb.ToString()}");
                            int uniqueCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                        }
                    }

                    if (connectorConfig.dbConfig != null)
                    {
                        var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                            join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                            select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "" : "")}").ToList();


                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                        {
                            using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
                            {
                                try
                                {
                                    sb.Clear();


                                    sb.Append($"DELETE FROM \"{schemaName}\".\"{tableName}\" a USING \"{schemaName}\".\"{tableName}_ctindex\" b");
                                    sb.Append($" WHERE a.ctid = b.myctid and b.marked_for_delete=true;");

                                    sb.Append($"DELETE FROM \"{schemaName}\".\"{tableName}_ctindex\"");
                                    sb.Append($" WHERE marked_for_delete=true;");

                                    //sb.Append($" WHERE a.ctid = b.myctid and b.parentctid is not null");
                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Delete query=>{sb.ToString()}");
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete started");
                                    var starttime = DateTime.Now;
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete starttime :" + starttime);
                                    int deleted_count = connectionFactory.DbConnection.Execute(sb.ToString());
                                    var endtime = DateTime.Now;
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete endtime :" + endtime);
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete Time Difference :" + (endtime - starttime));
                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Deleted records:{deleted_count}");
                                    sb.Clear();
                                    transaction.Commit();
                                    int syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, deduped_count: deleted_count);
                                    recordCount = deleted_count;
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Delete ended");
                                }
                                catch (Exception ex)
                                {
                                    WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                                    transaction.Rollback();
                                }

                                fetchColumns.Clear();
                                fetchColumns = null;
                            }
                        }

                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                        {
                            sb.Clear();
                            sb.Append(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", schemaName, tableName));
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique record query=>{sb.ToString()}");
                            int uniqueCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique records:{uniqueCount}");
                            UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, totaluniquecount: uniqueCount);
                        }
                    }
                }

                else
                {
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                    {
                        sb.Clear();
                        sb.Append(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", schemaName, tableName));
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique record query=>{sb.ToString()}");
                        int uniqueCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique records:{uniqueCount}");
                        UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, totaluniquecount: uniqueCount);
                    }
                }
            }
            catch (Exception ex)
            {
                if (connectorConfig != null)
                    WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                else
                    Console.WriteLine("Error:{0}", ex);
            }
            sb.Clear();
            sb = null;

            return await Task.FromResult(recordCount);
        }
        /// <summary>
        /// This method is used to assign the new parent records in ctindex table.
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="connectorId"></param>
        /// <param name="ctid"></param>
        /// <returns></returns>
        public async Task<string> ConfigureNewParentByCtid(string ccid, int connectorId, string Newctid, string Oldctid)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }
            StringBuilder sb = new StringBuilder();
            string resultMessage = string.Empty;
            if (connectorConfig.destDBConfig != null)
            {
                string tableName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                string schemaName = connectorConfig.destDBSchema;
                string connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                {
                    try
                    {
                        //Assign new parent record using New ctid
                        sb.Append($"UPDATE \"{schemaName}\".\"{tableName}_ctindex\" set parentctid = null, marked_for_delete = null WHERE myctid ='" + Newctid + "';");

                        //Assign new ctid to all child records contains Old ctid
                        sb.Append($"UPDATE \"{schemaName}\".\"{tableName}_ctindex\" set parentctid = '" + Newctid + "' WHERE parentctid ='" + Oldctid + "';");

                        //Update Old parent record  to new child record using New ctid
                        sb.Append($"UPDATE \"{schemaName}\".\"{tableName}_ctindex\" set parentctid = '" + Newctid + "', marked_for_delete = true WHERE myctid ='" + Oldctid + "';");
                        connectionFactory.DbConnection.Execute(sb.ToString());
                        resultMessage = "success";
                    }
                    catch (Exception ex)
                    {
                        resultMessage = ex.Message;
                        if (connectorConfig != null)
                            WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                        else
                            Console.WriteLine("Error:{0}", ex);
                    }
                }
            }
            sb.Clear();
            sb = null;
            return await Task.FromResult(resultMessage);
        }

        public async Task<string> DeleteDuplicateChildRecordsByCTids(ConnectorConfig connectorConfig, List<string> ctids)
        {
            //ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }
            string resultMessage = string.Empty;
            int recordCount = 0;
            StringBuilder sb = new StringBuilder();
            string tableName = string.Empty;
            string schemaName = string.Empty;
            string connectionURL = string.Empty;
            try
            {
                tableName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                schemaName = connectorConfig.destDBSchema;
                connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;

                if (connectorConfig.dbConfig != null)
                {
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                    {
                        sb.Append(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", schemaName, tableName));
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Total record count query=>{sb.ToString()}");
                        int TotalCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Total records count:{TotalCount}");
                        UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, total_records_count: TotalCount);
                        if (connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination && !connectorConfig.lastSyncAt.HasValue && connectorConfig.dbConfig_compare.table_type == SelectedTableType.Create_New_Table)
                        {
                            UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, count: TotalCount);
                        }
                    }

                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                    {
                        using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
                        {
                            try
                            {
                                sb.Clear();
                                //sb.Append($"UPDATE \"{schemaName}\".\"{tableName}_ctindex\" set parentctid = null, marked_for_delete = null");
                                //sb.Append($" WHERE myctid =(Select myctid FROM \"{schemaName}\".\"{tableName}_ctindex\" ");
                                //sb.Append(string.Format(" where ctid not in (" + string.Join(",", ctids.Select(c => $"\'{c}\'").ToList()) + ")"));
                                //sb.Append(" and parentctid = '" + myCtid + "' limit 1);");

                                sb.Append($"DELETE FROM \"{schemaName}\".\"{tableName}\"");
                                sb.Append(string.Format(" where ctid in (" + string.Join(",", ctids.Select(c => $"\'{c}\'").ToList()) + ");"));
                                int deleted_count = connectionFactory.DbConnection.Execute(sb.ToString());
                                WriteProcessLogToConsole(connectorConfig.connectorName, $"Deleted records:{deleted_count}");
                                transaction.Commit();
                                int syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, deduped_count: deleted_count);
                                recordCount = deleted_count;
                            }
                            catch (Exception ex)
                            {
                                sb.Clear();
                                WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                                transaction.Rollback();
                            }
                        }
                    }

                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                    {
                        sb.Clear();
                        sb.Append($"DELETE FROM \"{schemaName}\".\"{tableName}_ctindex\"");
                        sb.Append(string.Format(" where myctid in (" + string.Join(",", ctids.Select(c => $"\'{c}\'").ToList()) + ");"));

                        sb.Append(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", schemaName, tableName));
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique record query=>{sb.ToString()}");
                        int uniqueCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Unique records:{uniqueCount}");
                        UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, totaluniquecount: uniqueCount);
                    }
                }
            }
            catch (Exception ex)
            {
                resultMessage = ex.Message;
                if (connectorConfig != null)
                    WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                else
                    Console.WriteLine("Error:{0}", ex);
            }
            sb.Clear();
            sb = null;
            resultMessage = "success";
            return await Task.FromResult(resultMessage);
        }
        /// <summary>
        /// Method: DeDup Find Duplicate Before Delete
        /// Description: It is used to identify all the duplicate records from destination table usng ctindex table.
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns>rows count as int</returns>
        public async Task<int> DeDupFindDuplicateBeforeDelete(ConnectorConfig connectorConfig, IJobCancellationToken jobCancelToken)
        {
            int recordCount = 0;
            StringBuilder sb = new StringBuilder();
            Exception taskException = null;
            try
            {
                string tableName = string.Empty;
                string schemaName = string.Empty;
                string connectionURL = string.Empty;
                if (connectorConfig.dbConfig != null)
                {
                    SimilarityType operators = connectorConfig.dedup_method;
                    tableName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                    schemaName = connectorConfig.destDBSchema;
                    connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;
                    List<string> fetchColumns = (from c in connectorConfig.sourceObjectFields
                                                 select $"\"{c}\"").ToList();
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                    {
                        sb.Append("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

                        sb.Append($" DROP TABLE IF EXISTS \"{schemaName}\".\"{tableName}_ctindex\";");
                        sb.Append($" CREATE TABLE \"{schemaName}\".\"{tableName}_ctindex\"");
                        sb.Append($" (scan_time timestamp without time zone,myctid tid,parentctid tid,marked_for_delete boolean,is_master_ctid boolean);");
                        sb.Append($" Insert Into \"{schemaName}\".\"{tableName}_ctindex\"(myctid) Select ctid from \"{schemaName}\".\"{tableName}\";");
                        sb.Append($" Update \"{schemaName}\".\"{tableName}_ctindex\" set is_master_ctid = true where myctid in(");
                        sb.Append($" SELECT t.ctid FROM(SELECT DISTINCT ON({string.Join(",", fetchColumns.Select(c => c).ToArray()) }) ctid");
                        sb.Append($" FROM \"{schemaName}\".\"{tableName}\") t);");
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Create/Insert query for ctindex table=>{sb.ToString()}");
                        int uniqueCount = await connectionFactory.DbConnection.ExecuteScalarAsync<int>(sb.ToString()).ConfigureAwait(false);
                    }

                    sb.Clear();
                    if (operators == SimilarityType.Logical_AND || operators == SimilarityType.Logical_OR)
                    {
                        string queryCondition = string.Empty;
                        if (operators == SimilarityType.Logical_AND)
                        {
                            queryCondition = "AND";
                        }
                        else if (operators == SimilarityType.Logical_OR)
                        {
                            queryCondition = "OR";
                        }
                        //Fetch Total count
                        sb.Clear();
                        //sb.Append($"SELECT set_limit({connectorConfig.fuzzy_ratio});");
                        sb.Append($"SELECT count(t.*) AS total_rows,row_number() OVER() as dp_seqno FROM(SELECT DISTINCT ON ({string.Join(",", fetchColumns.Select(c => c).ToArray()) }) {string.Join(", ", fetchColumns.Select(c => c).ToArray()) } FROM \"{schemaName}\".\"{ tableName}\") AS t");
                        //sb.Append($"SELECT MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) {string.Join(", ", fetchColumns.Select(c => c).ToArray()) } FROM \"{schemaName}\".\"{ tableName}\") AS t");
                        //sb.Append(string.Format("select MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows from (SELECT distinct on(" + string.Join(",", fetchColumns.Select(c => c).ToArray()) + ") *,count(*) over(partition by " + string.Join(",", fetchColumns.Select(c => c).ToArray()) + ") as count FROM \"{0}\".\"{1}\" as tbl) temp", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
                        //sb.Append(" WHERE temp.count > 1 ");
                        //sb.Append($" Select MAX(pg_column_size(t.*)) AS row_data_size, COUNT(*) AS total_rows FROM \"{schemaName}\".\"{ tableName}\" AS t;");
                        PGTableDataSize tableDataSize = default(PGTableDataSize);
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                        {
                            var qryResult = await connectionFactory.DbConnection.QueryAsync<PGTableDataSize>(sb.ToString()).ConfigureAwait(false);

                            if (qryResult != null && qryResult.Count() > 0)
                            {
                                tableDataSize = qryResult.FirstOrDefault();
                            }
                            sb.Clear();
                        }
                        string symbool = "=";
                        if (tableDataSize != null && tableDataSize.total_rows > 0)
                        {
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch update records count=>{tableDataSize.total_rows}");
                            if (connectorConfig.fuzzy_ratio < 1)
                            {
                                symbool = ">";
                            }
                            int iCurrentPage = 1;
                            int iPageSize = ComputeAvgPageSize(tableDataSize);
                            int iNoOfPages = (int)Math.Ceiling(tableDataSize.total_rows / (decimal)iPageSize);
                            if (iNoOfPages > 0)
                            {
                                do
                                {
                                    //Cancel current task if cancel requested (eg: when system getting shutdown)
                                    if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                    {
                                        jobCancelToken.ThrowIfCancellationRequested();
                                        throw taskException;
                                    }

                                    int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage - 1) * iPageSize);

                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Total loop count =>{tableDataSize.total_rows}");
                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Current loop count =>{iCurrentPage}");
                                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Current offset =>{offSet}");

                                    //sb.Append($"select {string.Join(",", fetchColumns.Select(c => c).ToArray()) },ctid from \"{schemaName}\".\"{tableName}\" where ctid in(select myctid from \"{schemaName}\".\"{tableName}_ctindex\" where is_master_ctid = true");
                                    sb.Clear();
                                    sb.Append($"select myctid from \"{schemaName}\".\"{tableName}_ctindex\" where is_master_ctid = true");
                                    sb.Append(string.Format(" LIMIT {0}", iPageSize));
                                    sb.Append(string.Format(" OFFSET {0};", offSet));

                                    List<IDictionary<string, object>> pgRows = null;
                                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                                    {
                                        var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                                        sb.Clear();
                                        if (dbResult != null && dbResult.Count() > 0)
                                        {
                                            pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                                        }
                                    }
                                    if (pgRows != null && pgRows.Count() > 0)
                                    {
                                        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                                        {
                                            for (int i = 0; pgRows.Count() > i; i++)
                                            {
                                                string ctid = pgRows[i]["myctid"].ToString();

                                                sb.Append($"Update \"{schemaName}\".\"{tableName}_ctindex\" a set parentctid='" + ctid + "',marked_for_delete=true where myctid");
                                                sb.Append($" in(SELECT ctid FROM \"{schemaName}\".\"{tableName}\" WHERE ");
                                                sb.Append(string.Join(" " + queryCondition + " ", fetchColumns.Select(c => $"SIMILARITY({c}, (select {c} from \"{schemaName}\".\"{tableName}\" where ctid = '" + ctid + "')) " + symbool + connectorConfig.fuzzy_ratio).ToArray()));
                                                sb.Append($" and ctid<>'" + ctid + "');");

                                                if (i % 50 == 0 || i == pgRows.Count() - 1)
                                                {
                                                    var count = connectionFactory.DbConnection.ExecuteScalar<int>(sb.ToString());
                                                    sb.Clear();
                                                }
                                            }
                                        }
                                    }
                                    sb.Clear();
                                    iCurrentPage++;
                                } while (iCurrentPage <= iNoOfPages);
                            }
                        }
                    }
                    string errorMessage = string.Empty;
                    if (connectorConfig.backup_before_delete == ArchiveRecords.Yes && connectorConfig.review_before_delete == ReviewBeforeDeleteDups.No)
                    {
                        int count = SyncCtIndexTableIsExist(connectorConfig, tableName + "_deleted");
                        if (count == 0)
                        {
                            var dbStatus = CreateBackUpTable(connectorConfig, out errorMessage);
                            if (dbStatus <= 0 && !string.IsNullOrEmpty(errorMessage))
                            {
                                Console.WriteLine(errorMessage);
                                throw new Exception(errorMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                taskException = ex;
                if (connectorConfig != null)
                    WriteProcessLogToConsole(connectorConfig.connectorName, ex);
                else
                    Console.WriteLine("Error:{0}", ex);
            }
            sb.Clear();
            sb = null;

            return await Task.FromResult(recordCount);
        }

        #region [After Full DeDup ,If Records exist in Source table those Records must be Move to Compare Table]
        public int GetPGTableRecordCountAfterFullDeDup(ConnectorConfig connectorConfig, QueryFetchType queryFetchType)
        {
            StringBuilder sb = new StringBuilder();
            var recordCount = 0;
            try
            {
                if (connectorConfig.dbConfig != null)
                {
                    //Create new connection

                    sb.Append("SELECT count(*) FROM \"" + connectorConfig.dbSchema + "\".\"" + connectorConfig.sourceObjectName + "\"");
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                    {
                        recordCount = connectionFactory.DbConnection.ExecuteScalar<int>(sb.ToString());

                        Console.WriteLine("Select query to fetch the non deduped records count : " + recordCount);
                        // int syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, totaluniquecount: uniquerecordCount);
                    }
                    sb.Clear();
                    sb = null;
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb = null;
                Console.WriteLine("Error:{0}", ex.Message);
            }

            return recordCount;
        }

        public int GetCTIndexTableCount(string ccid, int connectorId)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }
            StringBuilder sb = new StringBuilder();
            int recordCount = 0;
            try
            {
                if (connectorConfig.destDBConfig != null)
                {
                    //Remove special chars in table name
                    string tableName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                    string schemaName = connectorConfig.destDBSchema;
                    string connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;

                    //sb.Append($"select count(myctid) from \"{schemaName}\".\"{tableName}_ctindex\" a");
                    //sb.Append($" where (select count(*) from \"{schemaName}\".\"{tableName}_ctindex\" b");
                    //sb.Append($" where b.parentctid = a.myctid) > 0 and a.marked_for_delete is null");
                    sb.Append($"select count(distinct parentctid) from \"{schemaName}\".\"{tableName}_ctindex\" where parentctid is not null;");
                    //sb.Append($" where (select count(*) from \"{schemaName}\".\"{tableName}_ctindex\" b");
                    //sb.Append($" where b.parentctid = a.myctid) > 0 and a.marked_for_delete is null");
                    //Create new connection
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        recordCount = connectionFactory.DbConnection.ExecuteScalar<int>(sb.ToString());
                    }
                    sb.Clear();
                    sb = null;
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb = null;
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return recordCount;
        }

        public int GetMarkedForDeleteCount(string ccid, int connectorId)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }

            int recordCount = 0;
            if (connectorConfig.destDBConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                {
                    recordCount = connectionFactory.DbConnection.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}_ctindex\" where marked_for_delete is true;", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                }
            }
            return recordCount;
        }
        public static int CreateBackUpTable(ConnectorConfig connectorConfig, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                //Check columns are there or not
                if (connectorConfig.sourceObjectFields != null && connectorConfig.destDBConfig != null)
                {
                    var deCols = GetPGDatabaseTableColumns(connectorConfig.destDBConfig, connectorConfig.destObjectName, connectorConfig.destDBSchema);

                    if (deCols != null && deCols.Count > 0)
                    {
                        //connectorConfig.syncDestObjectColumns = (from c in deCols
                        //                                         select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), isPrimaryKey = c.isPrimaryKey, isRequired = c.isRequired, maxLength = c.length }).ToList();
                        connectorConfig.syncDestObjectColumns = (from c in deCols
                                                                 select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), maxLength = c.length }).ToList();

                    }

                    //Remove special chars in table name
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                    //Create new connection to sync database
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(" CREATE TABLE ");
                        sb.Append(string.Format("\"{0}\".\"{1}_deleted\"", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                        sb.Append("(");

                        List<string> primaryKeys = new List<string>();
                        //Assign column data type
                        for (int i = 0; i < connectorConfig.syncDestObjectColumns.Count(); i++)
                        {
                            var dataType = connectorConfig.syncDestObjectColumns[i].fieldType.ToLower();
                            sb.Append(string.Format("{0} {1}", "\"" + connectorConfig.syncDestObjectColumns[i].name + "\"", dataType));

                            if ((i + 1) < connectorConfig.syncDestObjectColumns.Count())
                            {
                                sb.Append(", ");
                            }
                        }
                        sb.Append(");");

                        //Excute create table script
                        connectionFactory.DbConnection.Execute(sb.ToString());
                        sb = null;

                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                errorMessage = ex.Message;
                return -1;
            }

            return -1;
        }
        public async Task<int> ArchieveRecordsForDelete(ConnectorConfig connectorConfig, List<string> ctid)
        {
            //ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }
            int result_count = 0;
            string errorMessage = string.Empty;
            if (connectorConfig.destDBConfig != null)
            {
                string tableName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                string schemaName = connectorConfig.destDBSchema;
                string connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;
                //Remove special chars in table name
                connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                StringBuilder sb = new StringBuilder();
                int count = SyncCtIndexTableIsExist(connectorConfig, tableName + "_deleted");
                if (count == 1)
                {
                    sb.Append($"INSERT INTO \"{schemaName}\".\"{tableName}_deleted\" SELECT * FROM \"{schemaName}\".\"{tableName}\"");
                    sb.Append($" WHERE ctid in (" + string.Join(",", ctid.Select(c => $"\'{c}\'").ToList()) + ");");
                }
                if (count == 0)
                {
                    var dbStatus = CreateBackUpTable(connectorConfig, out errorMessage);
                    if (dbStatus <= 0 && !string.IsNullOrEmpty(errorMessage))
                    {
                        Console.WriteLine(errorMessage);
                        throw new Exception(errorMessage);
                    }
                    sb.Append($"INSERT INTO \"{schemaName}\".\"{tableName}_deleted\" SELECT * FROM \"{schemaName}\".\"{tableName}\"");
                    sb.Append($" WHERE ctid in (" + string.Join(",", ctid.Select(c => $"\'{c}\'").ToList()) + ");");
                }
                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                {
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Insert query for deleted table=>{sb.ToString()}");
                    result_count = connectionFactory.DbConnection.Execute(sb.ToString());
                    sb.Clear();
                    sb = null;
                }
            }
            return await Task.FromResult(result_count);
        }

        public async Task<IList<IDictionary<string, object>>> GetParentRecordsPageByPageForReviewAndDelete(string ccid, int connectorId, int cPage, int pageSize)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }

            IList<IDictionary<string, object>> pgRows = null;

            if (connectorConfig.destDBConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                var fecthColumn = (from c in connectorConfig.sourceObjectFields
                                   select $"\"{c}\"").ToList();
                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                {
                    Console.WriteLine("Calculate Offset value cPage:{0} &  pageSize:{1}", cPage, pageSize);
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    StringBuilder sb = new StringBuilder();
                    Console.WriteLine("Calculate Offset value:{0}", offSet);
                    //Only numeric type values 00.0000 to 00.00
                    //sb.Append(string.Format($"SELECT " + string.Join(",", fecthColumn.Select(c => "a." + c).ToList()) + ", b.myctid,b.marked_for_delete FROM \"{0}\".\"{1}\" a ", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                    sb.Append(string.Format("SELECT a.*, b.myctid,b.marked_for_delete FROM \"{0}\".\"{1}\" a ", connectorConfig.destDBSchema, connectorConfig.destObjectName));

					
					sb.Append(string.Format("left join \"{0}\".\"{1}_ctindex\" b on a.ctid=b.myctid ", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                    sb.Append($" where a.ctid in(select myctid from \"{connectorConfig.destDBSchema}\".\"{connectorConfig.destObjectName}_ctindex\" a1");
                    sb.Append($" where(select count(*) from \"{connectorConfig.destDBSchema}\".\"{connectorConfig.destObjectName}_ctindex\" b1");
                    sb.Append($" where b1.parentctid = a1.myctid) > 0 and a1.marked_for_delete is null");
                    // sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0})", offSet));
                    sb.Append(" Order By " + string.Join(",", fecthColumn.Select(c => "a." + c).FirstOrDefault()) + "");
                    Console.WriteLine("Fetch pg records from {0} to {1}", offSet, pageSize + offSet);
                    Console.WriteLine("Fetch parent records query {0} ", sb.ToString());
                    var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    sb.Clear();
                    sb = null;
                    if (dbResult != null && dbResult.Count() > 0)
                    {
                        pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                    }
                    dbResult = null;
                }
            }
            return await Task.FromResult(pgRows);
        }

        public async Task<IList<IDictionary<string, object>>> GetChildRecordsByParentForReviewAndDelete(string ccid, int connectorId, string ctid)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }

            IList<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.destDBConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                {
                    StringBuilder sb = new StringBuilder();
                    //Only numeric type values 00.0000 to 00.00
                    sb.Append(string.Format("SELECT a.*,b.myctid,b.marked_for_delete FROM \"{0}\".\"{1}\" a ", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                    sb.Append(string.Format(" join \"{0}\".\"{1}_ctindex\" b on a.ctid=b.myctid ", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                    sb.Append(string.Format(" where b.myctid=\'{0}\' or b.parentctid=\'{1}\'", ctid, ctid));
                    //sb.Append(string.Format(" where b.parentctid=\'{1}\'", ctid, ctid));
                    sb.Append(string.Format("order by b.parentctid desc"));

                    Console.WriteLine("Fetch child records query {0} ", sb.ToString());
                    var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    sb.Clear();
                    sb = null;
                    if (dbResult != null && dbResult.Count() > 0)
                    {
                        pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                    }
                    dbResult = null;
                }
            }
            return await Task.FromResult(pgRows);
        }


        public async Task<int> UpdateReviewAndSelectedRecordsForDelete(string ccid, int connectorId, List<string> ctid)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }

            var count = 0;
            if (connectorConfig.destDBConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                {
                    StringBuilder sb = new StringBuilder();
                    //Only numeric type values 00.0000 to 00.00
                    sb.Append(string.Format("Update \"{0}\".\"{1}_ctindex\" set marked_for_delete=true ", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                    sb.Append(string.Format(" where myctid in (" + string.Join(",", ctid.Select(c => $"\'{c}\'").ToList()) + ");"));
                    Console.WriteLine("Update query {0} ", sb.ToString());
                    //var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    count = connectionFactory.DbConnection.Execute(sb.ToString());

                    sb.Clear();
                    sb = null;
                    count = 1;
                }
            }
            return await Task.FromResult(count);
        }
        public async Task<IList<IDictionary<string, object>>> UpdateSelectedRecordsToDeselectForDelete(string ccid, int connectorId)
        {
            ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
            if (connectorConfig == null)
            {
                throw new ArgumentNullException("Dedup config is null");
            }

            IList<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.destDBConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                {
                    StringBuilder sb = new StringBuilder();
                    //Only numeric type values 00.0000 to 00.00
                    sb.Append(string.Format("Update \"{0}\".\"{1}_ctindex\" set marked_for_delete=null ", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                    Console.WriteLine("Update query {0} ", sb.ToString());
                    var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    sb.Clear();
                    sb = null;
                    if (dbResult != null && dbResult.Count() > 0)
                    {
                        pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                    }
                    dbResult = null;
                }
            }
            return await Task.FromResult(pgRows);
        }

        public async Task<IList<IDictionary<string, object>>> GetPGRecordsAfterFullDeDup(ConnectorConfig connectorConfig, int cPage, int pageSize, List<string> MatchingColumn)
        {
            IList<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dbConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);

                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                {
                    Console.WriteLine("Calculate Offset value cPage:{0} &  pageSize:{1}", cPage, pageSize);
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    StringBuilder sb = new StringBuilder();
                    Console.WriteLine("Calculate Offset value:{0}", offSet);
                    //Only numeric type values 00.0000 to 00.00
                    sb = new StringBuilder();
                    sb.Append(string.Format("select " + string.Join(",", MatchingColumn.Select(c => $"\"{c}\"").ToList()) + " from \"{0}\".\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
                    sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0};", offSet));
                    Console.WriteLine("Fetch pg records from {0} to {1}", offSet, pageSize + offSet);
                    Console.WriteLine("Fetch pg records query {0} ", sb.ToString());
                    var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    sb.Clear();
                    sb = null;
                    if (dbResult != null && dbResult.Count() > 0)
                    {
                        pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                    }
                    dbResult = null;
                }
            }
            return await Task.FromResult(pgRows);
        }

        public async Task<int> PGBulkInsertAfterFullDeDup(IList<IDictionary<string, string>> items, ConnectorConfig connectorConfig, List<SyncObjectColumn> dataMatchingColumn)
        {
            if (connectorConfig == null || (connectorConfig != null && connectorConfig.sourceObjectFields != null
                && connectorConfig.sourceObjectFields.Count() == 0))
            {
                return 0;
            }

            //Remove special chars in table name
            connectorConfig.dbConfig_compare.object_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.object_name);
            var primaryKeys = dataMatchingColumn.Where(c => c.isPrimaryKey == true).Select(c => c.name).ToList();
            var nonPrimaryKeys = dataMatchingColumn.Where(c => c.isPrimaryKey != true).Select(c => c.name).ToList();
            int rowCount = 0;

            //Create new connection
            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WITH temp AS  (");
                sb.Append("INSERT INTO ");
                sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.object_name));
                sb.Append(string.Format("({0}) VALUES ", string.Join(",", items[0].Keys.Select(x => "\"" + x + "\"").ToArray())));
                sb.Append(string.Join(",", items.Select(c => string.Format(" ({0}) ", string.Join(", ", c.Values.Select(v => (string.IsNullOrEmpty(v) ? "NULL" : "'" + v + "'")).ToArray()))).ToArray()));

                //if (primaryKeys.Count() > 0 && nonPrimaryKeys.Count() > 0)
                //{
                //    //do update if conflict with primary key
                //    sb.Append(" ON CONFLICT (" + string.Join(",", primaryKeys.Select(c => "\"" + c + "\"").ToArray()) + ")");
                //    sb.Append(string.Format(" DO UPDATE SET {0}", string.Join(",", nonPrimaryKeys.Select(x => "\"" + x + "\"=EXCLUDED.\"" + x + "\"").ToArray())));
                //}
                sb.Append(" RETURNING xmax) SELECT COUNT(*) AS total_rows,SUM(CASE WHEN xmax = 0 THEN 1 ELSE 0 END) AS inserted_rows,SUM(CASE WHEN xmax::text::int > 0 THEN 1 ELSE 0 END) AS updated_rows FROM temp;");

                //Excute insert script
                //Console.WriteLine(string.Join(", ", items.SelectMany(k => k.Keys).ToList()));
                //Console.WriteLine(string.Join(", ", items.SelectMany(k => k.Values).ToList()));
                //Console.WriteLine("PGBulkInsert Query :{0}", sb.ToString());
                items.Clear();
                items = null;
                primaryKeys.Clear();
                primaryKeys = null;
                nonPrimaryKeys.Clear();
                nonPrimaryKeys = null;

                using (var pgQueryStatus = await connectionFactory.DbConnection.QueryFirstOrDefaultAsync<PGQueryStatus>(sb.ToString()))
                {
                    sb.Clear();
                    sb = null;
                    if (pgQueryStatus != null)
                    {
                        rowCount = pgQueryStatus.inserted_rows + pgQueryStatus.updated_rows;
                    }
                }
            }

            return rowCount;
        }
        #endregion

        /// <summary>
        /// Method: GetPGRecordsByFilterAsync
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<IDictionary<string, object>>> GetPGRecordsByFilterAsync(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        {
            List<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dedup_type == DedupType.Safe_Mode || connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
            {
                if (connectorConfig.dbConfig != null)
                {
                    var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                        join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                        select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToList();

                    //Remove special chars in table name
                    connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);

                    //Create new connection
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                    {
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Calculate Offset value cPage:{cPage} &  pageSize:{pageSize}");
                        int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Calculate Offset value:{offSet}");

                        StringBuilder sb = new StringBuilder();
                        //if (!connectorConfig.hasOrderColumns)
                        //{
                        //    sb.Append("SELECT t1.*, row_number() OVER () as dd_rownum FROM ( ");
                        //}

                        // sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\") AS t ");
                        fetchColumns.Clear();
                        fetchColumns = null;

                        sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
                        if (connectorConfig.lastSyncAt.HasValue)
                        {
                            //Add -1 min
                            connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                            DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                            if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                                && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                                sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                            }
                            else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            }
                            else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            }
                        }
                        if (connectorConfig.hasOrderColumns)
                        {
                            sb.Append(string.Format(" ORDER BY {0}", string.Join(",", connectorConfig.orderByColumns.ToArray())));
                        }
                        //else
                        //{
                        //    sb.Append(") t1 ORDER BY dd_rownum");
                        //}
                        sb.Append(string.Format(" LIMIT {0}", pageSize));
                        sb.Append(string.Format(" OFFSET {0};", offSet));
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch pg records from {offSet} to {pageSize + offSet}");
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch pg records query=>{sb.ToString()}");
                        var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                        sb.Clear();
                        sb = null;
                        if (dbResult != null && dbResult.Count() > 0)
                        {
                            pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                        }
                        dbResult = null;
                    }
                }
            }

            return await Task.FromResult(pgRows);
        }

        /// <summary>
        /// Method: GetPGRecordsByFilter
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public List<IDictionary<string, object>> GetPGRecordsByFilter(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        {
            List<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
            {
                if (connectorConfig.dbConfig != null)
                {
                    var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                        join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                        select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToList();

                    //Remove special chars in table name
                    connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);

                    //Create new connection
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                    {
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Calculate Offset value cPage:{cPage} &  pageSize:{pageSize}");
                        int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Calculate Offset value:{offSet}");

                        StringBuilder sb = new StringBuilder();
                        //if (!connectorConfig.hasOrderColumns)
                        //{
                        //    sb.Append("SELECT t1.*, row_number() OVER () as dd_rownum FROM ( ");
                        //}

                        // sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\") AS t ");
                        fetchColumns.Clear();
                        fetchColumns = null;

                        sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
                        if (connectorConfig.lastSyncAt.HasValue)
                        {
                            //Add -1 min
                            connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                            DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                            if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                                && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                                sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                            }
                            else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            }
                            else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                            {
                                sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            }
                        }
                        if (connectorConfig.hasOrderColumns)
                        {
                            sb.Append(string.Format(" ORDER BY {0}", string.Join(",", connectorConfig.orderByColumns.ToArray())));
                        }
                        //else
                        //{
                        //    sb.Append(") t1 ORDER BY dd_rownum");
                        //}
                        sb.Append(string.Format(" LIMIT {0}", pageSize));
                        sb.Append(string.Format(" OFFSET {0};", offSet));
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch pg records from {offSet} to {pageSize + offSet}");
                        WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch pg records query=>{sb.ToString()}");
                        pgRows = connectionFactory.DbConnection.Query<dynamic>(sb.ToString()).Cast<IDictionary<string, object>>().ToList();
                        sb.Clear();
                        sb = null;

                    }
                }
            }

            return pgRows;
        }

        /// <summary>
        /// Method: GetMultiplrSourcePGRecordsByFilter
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        //public static async Task<CompareMultipleTables> GetMultiplrSourcePGRecordsByFilter(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        //{
        //    CompareMultipleTables cmt = new CompareMultipleTables();
        //    IList<IDictionary<string, object>> pgRows = null;
        //    if (connectorConfig.dbConfig != null && connectorConfig.dbConfig_compare != null)
        //    {

        //        //Remove special chars in table name
        //        connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
        //        StringBuilder sb = new StringBuilder();
        //        //var fetchColumns = (from c in connectorConfig.syncObjectColumns
        //        //                    join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //        //                    select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();

        //        //var fetchColumns1 = (from c in connectorConfig.syncObjectColumns
        //        //                     join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //        //                     select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();

        //        //var filterColumn = (from c in connectorConfig.syncObjectColumns
        //        //                    join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //        //                    select $"{c.name}{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();


        //        using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl))
        //        {
        //            int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
        //            sb = new StringBuilder();

        //            sb = new StringBuilder();
        //            if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table)
        //            {
        //                sb.Append(string.Format("select * from (SELECT distinct on(" + string.Join(",", connectorConfig.compareObjectFieldsMapping.Select(c => "\"" + c.Value + "\"").ToArray()) + ") *,count(*) over(partition by " + string.Join(",", connectorConfig.compareObjectFieldsMapping.Select(c => "\"" + c.Value + "\"").ToArray()) + ") as count FROM \"{0}\".\"{1}\" as tbl) temp", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.object_name));
        //            }
        //            else
        //            {
        //                sb.Append(string.Format("select * from (SELECT distinct on(" + string.Join(",", connectorConfig.compareObjectFieldsMapping.Select(c => "\"" + c.Value + "\"").ToArray()) + ") *,count(*) over(partition by " + string.Join(",", connectorConfig.compareObjectFieldsMapping.Select(c => "\"" + c.Value + "\"").ToArray()) + ") as count FROM \"{0}\".\"{1}\" as tbl) temp", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.new_table_name));
        //            }
        //            sb.Append(" WHERE temp.count > 0 ");

        //            sb.Append(string.Format(" LIMIT {0}", pageSize));
        //            sb.Append(string.Format(" OFFSET {0};", offSet));

        //            Console.WriteLine("Fetch source records from {0} to {1}", offSet, pageSize + offSet);
        //            DateTime starttime = DateTime.Now;
        //            Console.WriteLine("Fetch source count :{0} and Starting Time :{1} and CurrentPage{2}  ", pageSize, starttime, cPage);

        //            var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
        //            if (dbResult != null && dbResult.Count() > 0)
        //            {
        //                pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
        //            }
        //            dbResult = null;
        //            sb = null;
        //            Console.WriteLine("Fetch source count :{0} and Ending Time :{1} and CurrentPage{2} ", pageSize, DateTime.Now, cPage);
        //            Console.WriteLine("Fetch source count :{0} and Total Time :{1} and CurrentPage{2}", pageSize, DateTime.Now - starttime, cPage);
        //            // fetchColumns = null;
        //        }
        //        if (pgRows != null)
        //        {
        //            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
        //            {
        //                DateTime starttime = DateTime.Now;
        //                Console.WriteLine("Comapre source data with other table count :{0} and Starting Time :{1}and CurrentPage{2}  ", pgRows.Count, starttime, cPage);
        //                if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify || connectorConfig.dedup_type == DedupType.Safe_Mode)
        //                {
        //                    try
        //                    {
        //                        sb = new StringBuilder();
        //                        // sb.Append(string.Format("select * from {0}.\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
        //                        sb.Append(" CREATE TEMP TABLE compare_temp (");
        //                        int count1 = 0;
        //                        foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                        {
        //                            var datatype = connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Value.ToLower().Trim()).fieldType;
        //                            if (count1 == 0)
        //                                sb.Append(key.Key + " " + datatype);
        //                            else
        //                                sb.Append(", " + key.Key + " " + datatype);
        //                            count1++;
        //                        }

        //                        sb.Append(" );");
        //                        sb.Append(" INSERT INTO compare_temp VALUES");
        //                        if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                        {
        //                            int rowCount = 0;
        //                            for (int i = 0; i < pgRows.Count(); i++)//foreach (var rows in pgRows)
        //                            {
        //                                var rows = pgRows[i];
        //                                int count = 0;
        //                                if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append("(");
        //                                    else
        //                                        sb.Append(",(");
        //                                    foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                                    {
        //                                        if (count == 0)
        //                                            sb.Append("'" + rows[key.Value] + "'");
        //                                        else
        //                                            sb.Append(",'" + rows[key.Value] + "'");
        //                                        count++;
        //                                    }
        //                                    sb.Append(")");
        //                                }
        //                                rowCount++;
        //                            }
        //                            rowCount = 0;

        //                            sb.Append(" ;");
        //                            sb.Append(string.Format("SELECT a.* FROM {0}.\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
        //                            sb.Append(" a INNER JOIN compare_temp b ON ");
        //                            rowCount = 0;
        //                            foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                            {

        //                                if (connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Value.ToLower().Trim() && Constants.NonEncodeDataTypes.Contains((c.fieldType.IndexOf("(") != -1 ? c.fieldType.ToLower().Substring(0, c.fieldType.IndexOf("(")) : c.fieldType.ToLower()))) == null)
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append(" COALESCE(NULLIF(a.\"" + key.Key + "\",''),'dedup')=COALESCE(NULLIF(b.\"" + key.Value.ToLower() + "\",''),'dedup')");
        //                                    else
        //                                        sb.Append(" and COALESCE(NULLIF(a.\"" + key.Key + "\",''),'dedup')=COALESCE(NULLIF(b.\"" + key.Value.ToLower() + "\",''),'dedup')");
        //                                }
        //                                else
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append(" a.\"" + key.Key + "\"=b.\"" + key.Value.ToLower() + "\"");
        //                                    else
        //                                        sb.Append(" and a.\"" + key.Key + "\"=b.\"" + key.Value.ToLower() + "\"");
        //                                }
        //                                rowCount++;

        //                            }
        //                            sb.Append(string.Format(" LIMIT {0}", pageSize));
        //                            sb.Append(" OFFSET 0");
        //                            sb.Append(";");

        //                            sb.Append(string.Format("SELECT count(a.*) FROM {0}.\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
        //                            sb.Append(" a INNER JOIN compare_temp b ON ");
        //                            rowCount = 0;
        //                            foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                            {

        //                                if (connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Value.ToLower().Trim() && Constants.NonEncodeDataTypes.Contains((c.fieldType.IndexOf("(") != -1 ? c.fieldType.ToLower().Substring(0, c.fieldType.IndexOf("(")) : c.fieldType.ToLower()))) == null)
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append(" COALESCE(NULLIF(a.\"" + key.Key + "\",''),'dedup')=COALESCE(NULLIF(b.\"" + key.Value.ToLower() + "\",''),'dedup')");
        //                                    else
        //                                        sb.Append(" and COALESCE(NULLIF(a.\"" + key.Key + "\",''),'dedup')=COALESCE(NULLIF(b.\"" + key.Value.ToLower() + "\",''),'dedup')");
        //                                }
        //                                else
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append(" a.\"" + key.Key + "\"=b.\"" + key.Value.ToLower() + "\"");
        //                                    else
        //                                        sb.Append(" and a.\"" + key.Key + "\"=b.\"" + key.Value.ToLower() + "\"");
        //                                }
        //                                rowCount++;
        //                            }
        //                            sb.Append(";");
        //                            var queryResult = connectionFactory.DbConnection.QueryMultiple(sb.ToString());
        //                            sb = null;
        //                            if (queryResult != null)
        //                            {
        //                                var dbResult = queryResult.Read<dynamic>().ToList();
        //                                if (dbResult != null && dbResult.Count() > 0)
        //                                {
        //                                    cmt.FinalResultSet = dbResult.Cast<IDictionary<string, object>>().ToList();
        //                                }
        //                                dbResult = null;
        //                                cmt.TotalRecordCount = queryResult.Read<int>().FirstOrDefault();
        //                                cmt.CompareResultSet = pgRows;
        //                                queryResult = null;
        //                                pgRows = null;
        //                                Console.WriteLine("Compare record set from first source " + ((cmt != null && cmt.FinalResultSet != null) ? cmt.FinalResultSet.Count() : 0));
        //                            }
        //                        }
        //                    }
        //                    catch (Exception exc)
        //                    {
        //                        sb = null;
        //                        Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                    }
        //                }
        //                else
        //                {
        //                    if (connectorConfig.dedup_type == DedupType.Full_Dedup)
        //                    {
        //                        int rowCount = 0;
        //                        sb = new StringBuilder();
        //                        Console.WriteLine("Delete the record set in first source");

        //                        sb.Append($"DELETE FROM {connectorConfig.dbSchema}.\"{connectorConfig.sourceObjectName}\" a USING ");
        //                        sb.Append("(SELECT  * FROM (VALUES ");
        //                        for (int i = 0; i < pgRows.Count(); i++)//foreach (var rows in pgRows)
        //                        {
        //                            var rows = pgRows[i];
        //                            int count = 0;
        //                            if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                            {
        //                                if (rowCount == 0)
        //                                    sb.Append("(");
        //                                else
        //                                    sb.Append(",(");
        //                                foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                                {
        //                                    var datatype1 = connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Key.ToLower().Trim()).fieldType;
        //                                    switch (datatype1.ToLower())
        //                                    {
        //                                        case "integer":
        //                                        case "decimal":
        //                                        case "float":
        //                                        case "int":
        //                                        case "serial":
        //                                        case "smallserial":
        //                                        case "smallint":
        //                                        case "bigint":
        //                                        case "bigserial":
        //                                        case "number":
        //                                        case "real":
        //                                        case "double":
        //                                        case "double precision":
        //                                        case "numeric":
        //                                            if (count == 0)
        //                                                sb.Append("COALESCE(NULLIF('" + rows[key.Key] + "',''),'0')");
        //                                            else
        //                                                sb.Append(",COALESCE(NULLIF('" + rows[key.Key] + "',''),'0')");
        //                                            break;
        //                                        case "timestamp without time zone":
        //                                        case "timestamp with time zone":
        //                                        case "time without time zone":
        //                                        case "time with time zone":
        //                                        case "date":
        //                                        case "timestamp":
        //                                        case "timestamp without time zone[]":
        //                                        case "timestamp with time zone[]":
        //                                        case "time without time zone[]":
        //                                        case "time with time zone[]":
        //                                        case "datetime":
        //                                        case "boolean":
        //                                        case "bit":
        //                                            if (count == 0)
        //                                                sb.Append("'" + rows[key.Key] + "'");
        //                                            else
        //                                                sb.Append(",'" + rows[key.Key] + "'");
        //                                            break;
        //                                        case "text":
        //                                        case "character varying(50)":
        //                                            if (count == 0)
        //                                                sb.Append("COALESCE(NULLIF('" + rows[key.Key] + "',''),'dedup')");
        //                                            else
        //                                                sb.Append(",COALESCE(NULLIF('" + rows[key.Key] + "',''),'dedup')");
        //                                            break;
        //                                        default:
        //                                            if (count == 0)
        //                                                sb.Append("'" + rows[key.Key] + "'");
        //                                            else
        //                                                sb.Append(",'" + rows[key.Key] + "'");
        //                                            break;
        //                                    }

        //                                    count++;
        //                                }
        //                                sb.Append(")");
        //                            }
        //                            rowCount++;
        //                        }
        //                        pgRows = null;
        //                        sb.Append(") AS a1 (" + string.Join(",", connectorConfig.compareObjectFieldsMapping.Select(c => $"\"{c.Key}\"").ToArray()) + ")) b WHERE ");
        //                        if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                        {
        //                            rowCount = 0;
        //                            foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                            {
        //                                var datatype = connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Key.ToLower().Trim()).fieldType;
        //                                if (!String.IsNullOrEmpty(datatype) && datatype.Contains("character varying"))
        //                                {
        //                                    datatype = "character varying";
        //                                }
        //                                switch (datatype.ToLower())
        //                                {
        //                                    case "integer":
        //                                    case "decimal":
        //                                    case "float":
        //                                    case "int":
        //                                    case "serial":
        //                                    case "smallserial":
        //                                    case "smallint":
        //                                    case "bigint":
        //                                    case "bigserial":
        //                                    case "number":
        //                                    case "real":
        //                                    case "double":
        //                                    case "double precision":
        //                                    case "numeric":
        //                                        if (rowCount == 0)
        //                                        {
        //                                            sb.Append("COALESCE(a.\"" + key.Key + "\",0)=COALESCE(b.\"" + key.Key + "\",0)");
        //                                        }
        //                                        else
        //                                        {
        //                                            sb.Append(" and COALESCE(a.\"" + key.Key + "\",0)=COALESCE(b.\"" + key.Key + "\",0)");
        //                                        }
        //                                        break;
        //                                    case "timestamp without time zone":
        //                                    case "timestamp with time zone":
        //                                    case "time without time zone":
        //                                    case "time with time zone":
        //                                    case "date":
        //                                    case "timestamp":
        //                                    case "timestamp without time zone[]":
        //                                    case "timestamp with time zone[]":
        //                                    case "time without time zone[]":
        //                                    case "time with time zone[]":
        //                                    case "datetime":
        //                                    case "boolean":
        //                                    case "bit":
        //                                        if (rowCount == 0)
        //                                            sb.Append(" a.\"" + key.Key + "\"=b.\"" + key.Key + "\"::" + datatype);
        //                                        else
        //                                            sb.Append(" and a.\"" + key.Key + "\"=b.\"" + key.Key + "\"::" + datatype);
        //                                        break;
        //                                    case "text":
        //                                    case "character varying":
        //                                        if (rowCount == 0)
        //                                        {
        //                                            sb.Append("COALESCE(a.\"" + key.Key + "\",'dedup')=COALESCE(b.\"" + key.Key + "\",'dedup')");
        //                                        }
        //                                        else
        //                                        {
        //                                            sb.Append(" and COALESCE(a.\"" + key.Key + "\",'dedup')=COALESCE(b.\"" + key.Key + "\",'dedup')");
        //                                        }
        //                                        break;
        //                                    default:
        //                                        if (rowCount == 0)
        //                                            sb.Append(" a.\"" + key.Key + "\"=b.\"" + key.Key + "\"::" + datatype);
        //                                        else
        //                                            sb.Append(" and a.\"" + key.Key + "\"=b.\"" + key.Key + "\"::" + datatype);
        //                                        break;
        //                                }
        //                                rowCount++;
        //                            }
        //                        }
        //                        sb.Append(";");

        //                        try
        //                        {
        //                            //excute query
        //                            int delete_count = await connectionFactory.DbConnection.ExecuteAsync(sb.ToString());
        //                            Console.WriteLine("Deleted record count " + delete_count);
        //                            if (delete_count > 0)
        //                                UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, deduped_count: delete_count);
        //                            sb = null;
        //                        }
        //                        catch (Exception exc)
        //                        {
        //                            sb = null;
        //                            Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                            throw;
        //                        }
        //                    }
        //                }
        //                Console.WriteLine("Comapre source data with other table count :{0} and Total Time :{1} and CurrentPage{2}", pageSize, DateTime.Now - starttime, cPage);

        //                //fetchColumns = null;
        //                //fetchColumns1 = null;
        //                //filterColumn = null;
        //            }
        //        }
        //    }

        //    return cmt;
        //}


        /// <summary>
        /// Method: GetRecordsFromSourceTableByFilterAsync
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<IDictionary<string, object>>> GetRecordsFromSourceTableByFilterAsync(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        {
            List<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dbConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
                StringBuilder sb = new StringBuilder();
                var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                    join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                    select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToList();

                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                {
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    //if (!connectorConfig.hasOrderColumns)
                    //{
                    //    sb.Append("SELECT t1.*, row_number() OVER () as dd_rownum FROM (");
                    //}
                    sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\") AS t ");
                    //sb.Append($"SELECT {(connectorConfig.hasPrimaryKey ? "" : "DISTINCT ")}* FROM \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\"");
                    if (connectorConfig.lastSyncAt.HasValue)
                    {
                        //Add -1 min
                        connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                        DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                        if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                            && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                    }
                    if (connectorConfig.hasOrderColumns)
                    {
                        sb.Append(string.Format(" ORDER BY {0}", string.Join(",", connectorConfig.orderByColumns.Select(c => "\"" + c + "\"").ToArray())));
                    }
                    //else
                    //{
                    //    sb.Append(") t1 ORDER BY dd_rownum");
                    //}
                    sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0};", offSet));
                    fetchColumns.Clear();
                    fetchColumns = null;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source records from {offSet} to {pageSize + offSet}");
                    DateTime starttime = DateTime.Now;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Starting Time :{starttime} and CurrentPage{cPage}");
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Query=>{sb.ToString()}");
                    var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    sb.Clear();
                    sb = null;
                    if (dbResult != null && dbResult.Count() > 0)
                    {
                        pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                    }
                    dbResult = null;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Total Time :{DateTime.Now - starttime} and CurrentPage{cPage}");
                }
            }
            return pgRows;
        }

        /// <summary>
        /// Method: GetRecordsFromSourceTableByFilter
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public List<IDictionary<string, object>> GetRecordsFromSourceTableByFilter(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        {
            List<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dbConfig != null)
            {
                //Remove special chars in table name
                connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
                StringBuilder sb = new StringBuilder();
                var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                    join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                    select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToList();

                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                {
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    //if (!connectorConfig.hasOrderColumns)
                    //{
                    //    sb.Append("SELECT t1.*, row_number() OVER () as dd_rownum FROM (");
                    //}
                    sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbSchema}\".\"{ connectorConfig.sourceObjectName}\") AS t ");
                    //sb.Append($"SELECT {(connectorConfig.hasPrimaryKey ? "" : "DISTINCT ")}* FROM \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\"");
                    if (connectorConfig.lastSyncAt.HasValue)
                    {
                        //Add -1 min
                        connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                        DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                        if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                            && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                    }
                    if (connectorConfig.hasOrderColumns)
                    {
                        sb.Append(string.Format(" ORDER BY {0}", string.Join(",", connectorConfig.orderByColumns.Select(c => "\"" + c + "\"").ToArray())));
                    }
                    //else
                    //{
                    //    sb.Append(") t1 ORDER BY dd_rownum");
                    //}
                    sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0};", offSet));
                    fetchColumns.Clear();
                    fetchColumns = null;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source records from {offSet} to {pageSize + offSet}");
                    DateTime starttime = DateTime.Now;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Starting Time :{starttime} and CurrentPage{cPage}");
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Query=>{sb.ToString()}");
                    pgRows = connectionFactory.DbConnection.Query<dynamic>(sb.ToString()).Cast<IDictionary<string, object>>().ToList();
                    sb.Clear();
                    sb = null;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Total Time :{DateTime.Now - starttime} and CurrentPage{cPage}");
                }
            }
            return pgRows;
        }

        /// <summary>
        /// Method: GetRecordsFromCompareTableByFilterAsync
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<IDictionary<string, object>>> GetRecordsFromCompareTableByFilterAsync(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        {
            List<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dbConfig_compare != null)
            {
                //Remove special chars in table name
                //if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table)
                //    connectorConfig.dbConfig_compare.object_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.object_name);
                //else
                connectorConfig.dbConfig.object_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig.object_name);

                StringBuilder sb = new StringBuilder();
                var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                    join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                    select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToList();

                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                {
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    //if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table
                    //    && !connectorConfig.hasOrderColumns)
                    //{
                    //    sb.Append("SELECT t1.*, row_number() OVER () as dd_rownum FROM (");
                    //}
                    //if (connectorConfig.dbConfig_compare.table_type == SelectedTableType.Select_Existing_Table)
                    //{

                    // sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\") AS t ");

                    sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.object_name));
                    //}
                    //else
                    //{

                    //    sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbConfig_compare.db_schema}\".\"{ connectorConfig.dbConfig_compare.new_table_name}\") AS t ");

                    //    // sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.new_table_name));
                    //}
                    if (connectorConfig.lastSyncAt.HasValue)
                    {
                        //Add -1 min
                        connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                        DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                        if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                            && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                    }
                    if (connectorConfig.hasOrderColumns)
                    {
                        sb.Append(string.Format(" ORDER BY {0}", string.Join(",", connectorConfig.orderByColumns.Select(c => "\"" + c + "\"").ToArray())));
                    }
                    //else
                    //{
                    //    sb.Append(") t1 order by mc_rownum");
                    //}
                    sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0};", offSet));
                    fetchColumns.Clear();
                    fetchColumns = null;

                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source records from {offSet} to {offSet + pageSize}");
                    DateTime starttime = DateTime.Now;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Starting Time :{starttime} and CurrentPage{cPage}");
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Query=>{sb.ToString()}");
                    var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
                    sb.Clear();
                    sb = null;
                    if (dbResult != null && dbResult.Count() > 0)
                    {
                        pgRows = dbResult.Cast<IDictionary<string, object>>().ToList();
                    }
                    dbResult = null;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Total Time :{DateTime.Now - starttime} and CurrentPage{cPage}");
                }
            }
            return pgRows;
        }

        /// <summary>
        /// Method: GetRecordsFromCompareTableByFilter
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public List<IDictionary<string, object>> GetRecordsFromCompareTableByFilter(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType)
        {
            List<IDictionary<string, object>> pgRows = null;
            if (connectorConfig.dbConfig != null)
            {
                //Remove special chars in table name
                //if (connectorConfig.dbConfig.table_type != SelectedTableType.Create_New_Table)
                connectorConfig.dbConfig.object_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig.object_name);
                //else
                //    connectorConfig.dbConfig.new_table_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig.new_table_name);

                StringBuilder sb = new StringBuilder();
                var fetchColumns = (from c in connectorConfig.syncObjectColumns
                                    join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
                                    select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToList();

                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
                {
                    int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
                    //if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table
                    //    && !connectorConfig.hasOrderColumns)
                    //{
                    //    sb.Append("SELECT t1.*, row_number() OVER () as dd_rownum FROM (");
                    //}
                    //if (connectorConfig.dbConfig.table_type == SelectedTableType.Select_Existing_Table)
                    //{

                    // sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.object_name}\") AS t ");

                    sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.object_name));
                    //}
                    //else
                    //{

                    //    sb.Append($"SELECT * FROM (select distinct on({string.Join(", ", fetchColumns.Select(c => c).ToArray()) }) * FROM \"{connectorConfig.dbConfig.db_schema}\".\"{ connectorConfig.dbConfig.new_table_name}\") AS t ");

                    //    // sb.Append(string.Format("SELECT * FROM \"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.new_table_name));
                    //}
                    if (connectorConfig.lastSyncAt.HasValue)
                    {
                        //Add -1 min
                        connectorConfig.lastSyncAt = connectorConfig.lastSyncAt.Value.AddMinutes(-1);
                        DateTime createdDate = Utilities.ConvertTimeBySystemTimeZoneId(connectorConfig.lastSyncAt.Value, TimeZoneInfo.Utc.Id, "UTC");

                        if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none"
                            && !string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                            sb.Append(" OR (\"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "')");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcNewRecordFilter) && connectorConfig.srcNewRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcNewRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcNewRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                        else if (!string.IsNullOrEmpty(connectorConfig.srcUpdateRecordFilter) && connectorConfig.srcUpdateRecordFilter.ToLower().Trim() != "none")
                        {
                            sb.Append(" WHERE \"" + connectorConfig.srcUpdateRecordFilter + "\" IS NOT NULL AND \"" + connectorConfig.srcUpdateRecordFilter + "\">='" + createdDate.ToString("yyyy-MM-dd hh:mm:00 tt") + "'");
                        }
                    }
                    if (connectorConfig.hasOrderColumns)
                    {
                        sb.Append(string.Format(" ORDER BY {0}", string.Join(",", connectorConfig.orderByColumns.Select(c => "\"" + c + "\"").ToArray())));
                    }
                    //else
                    //{
                    //    sb.Append(") t1 order by mc_rownum");
                    //}
                    sb.Append(string.Format(" LIMIT {0}", pageSize));
                    sb.Append(string.Format(" OFFSET {0};", offSet));
                    fetchColumns.Clear();
                    fetchColumns = null;

                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source records from {offSet} to {offSet + pageSize}");
                    DateTime starttime = DateTime.Now;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Starting Time :{starttime} and CurrentPage{cPage}");
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Query=>{sb.ToString()}");
                    pgRows = connectionFactory.DbConnection.Query<dynamic>(sb.ToString()).Cast<IDictionary<string, object>>().ToList();
                    sb.Clear();
                    sb = null;
                    WriteProcessLogToConsole(connectorConfig.connectorName, $"Fetch source count :{pageSize} and Total Time :{DateTime.Now - starttime} and CurrentPage{cPage}");
                }
            }
            return pgRows;
        }

        /// <summary>
        /// Method: GetMultiplrSourcePGRecordsByFilter
        /// Description: It is used to read rows by page wise from sync table by table name
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="cPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        //public static async Task<IList<IDictionary<string, object>>> GetMultiplrSourceCompareByMainSource(ConnectorConfig connectorConfig, int cPage, int pageSize, QueryFetchType queryFetchType, IList<IDictionary<string, object>> pgRows)
        //{
        //    IList<IDictionary<string, object>> finalResultRows = null;
        //    if (connectorConfig.dbConfig != null && connectorConfig.dbConfig_compare != null)
        //    {
        //        //Remove special chars in table name
        //        connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
        //        StringBuilder sb = new StringBuilder();

        //        int offSet = cPage == 1 ? 0 : ((cPage - 1) * pageSize);
        //        if (pgRows != null)
        //        {
        //            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
        //            {
        //                DateTime starttime = DateTime.Now;
        //                Console.WriteLine("Comapre source data with other table count :{0} and Starting Time :{1}and CurrentPage{2}  ", pgRows.Count, starttime, cPage);
        //                if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify || connectorConfig.dedup_type == DedupType.Safe_Mode)
        //                {
        //                    try
        //                    {
        //                        sb = new StringBuilder();
        //                        // sb.Append(string.Format("select * from {0}.\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
        //                        sb.Append(" CREATE TEMP TABLE compare_temp (");
        //                        int count1 = 0;
        //                        foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                        {
        //                            var datatype = connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Value.ToLower().Trim()).fieldType;
        //                            if (count1 == 0)
        //                                sb.Append(key.Key + " " + datatype);
        //                            else
        //                                sb.Append(", " + key.Key + " " + datatype);
        //                            count1++;
        //                        }

        //                        sb.Append(" );");
        //                        sb.Append(" INSERT INTO compare_temp VALUES");
        //                        if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                        {
        //                            int rowCount = 0;
        //                            for (int i = 0; i < pgRows.Count(); i++)//foreach (var rows in pgRows)
        //                            {
        //                                var rows = pgRows[i];
        //                                int count = 0;
        //                                if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append("(");
        //                                    else
        //                                        sb.Append(",(");
        //                                    foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                                    {
        //                                        if (count == 0)
        //                                            sb.Append("'" + rows[key.Value] + "'");
        //                                        else
        //                                            sb.Append(",'" + rows[key.Value] + "'");
        //                                        count++;
        //                                    }
        //                                    sb.Append(")");
        //                                }
        //                                rowCount++;
        //                            }
        //                            pgRows = null;
        //                            sb.Append(" ;");
        //                            sb.Append(string.Format("SELECT a.* FROM {0}.\"{1}\" ", connectorConfig.dbSchema, connectorConfig.sourceObjectName));
        //                            sb.Append(" a INNER JOIN compare_temp b ON ");
        //                            rowCount = 0;
        //                            foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                            {
        //                                if (connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Value.ToLower().Trim() && Constants.NonEncodeDataTypes.Contains((c.fieldType.IndexOf("(") != -1 ? c.fieldType.ToLower().Substring(0, c.fieldType.IndexOf("(")) : c.fieldType.ToLower()))) == null)
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append(" COALESCE(NULLIF(a.\"" + key.Key + "\",''),'dedup')=COALESCE(NULLIF(b.\"" + key.Value.ToLower() + "\",''),'dedup')");
        //                                    else
        //                                        sb.Append(" and COALESCE(NULLIF(a.\"" + key.Key + "\",''),'dedup')=COALESCE(NULLIF(b.\"" + key.Value.ToLower() + "\",''),'dedup')");
        //                                }
        //                                else
        //                                {
        //                                    if (rowCount == 0)
        //                                        sb.Append(" a.\"" + key.Key + "\"=b.\"" + key.Value.ToLower() + "\"");
        //                                    else
        //                                        sb.Append(" and a.\"" + key.Key + "\"=b.\"" + key.Value.ToLower() + "\"");
        //                                }
        //                                rowCount++;
        //                            }

        //                            sb.Append(string.Format(" LIMIT {0}", pageSize));
        //                            sb.Append(string.Format(" OFFSET {0};", offSet));
        //                            sb.Append(";");

        //                            var dbResult = await connectionFactory.DbConnection.QueryAsync<dynamic>(sb.ToString());
        //                            if (dbResult != null && dbResult.Count() > 0)
        //                            {
        //                                finalResultRows = dbResult.Cast<IDictionary<string, object>>().ToList();
        //                                dbResult = null;
        //                            }
        //                            sb.Clear();
        //                            sb = null;
        //                        }
        //                    }
        //                    catch (Exception exc)
        //                    {
        //                        sb = null;
        //                        Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                    }
        //                }
        //                pgRows = null;
        //            }
        //        }
        //    }

        //    return finalResultRows;
        //}

        /// <summary>
        /// Method: RemovePGSyncTableRows
        /// Description: It is used to clear rows from sync table by table name. It will also reset sync info in connectors table
        /// by ccid and connectorId
        /// </summary>
        /// <param name="connectorConfig"></param>
        public static void RemovePGSyncTableRows(ConnectorConfig connectorConfig)
        {
            try
            {
                if (connectorConfig.dbConfig != null && connectorConfig.connectorId > 0 && !string.IsNullOrEmpty(connectorConfig.ccid))
                {
                    //Remove special chars in table name
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                    //Create new connection
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        //Create new transaction 
                        using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
                        {
                            try
                            {
                                //excute query
                                connectionFactory.DbConnection.Execute(string.Format("TRUNCATE TABLE \"{0}\".\"{1}\";", connectorConfig.destDBSchema, connectorConfig.destObjectName));

                                //reset sync status
                                //UpdateSyncInfo((int)connectorConfig.connectorId, connectorConfig.ccid, 0, 0);

                                transaction.Commit();
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(string.Format("Error:{0}", exc.Message));
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Method: RemovePGSyncTable
        /// Description: It is used to drop sync table by table name. It will also reset sync info in connectors table
        /// by ccid and connectorId
        /// </summary>
        /// <param name="connectorConfig"></param>
        public static void RemovePGSyncTable(ConnectorConfig connectorConfig)
        {
            try
            {
                if (connectorConfig.dbConfig != null && connectorConfig.connectorId > 0 && !string.IsNullOrEmpty(connectorConfig.ccid))
                {
                    //Remove special chars in table name
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                    //Create new connection
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        //Create new transaction 
                        using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
                        {
                            try
                            {
                                //excute query
                                connectionFactory.DbConnection.Execute(string.Format("DROP TABLE \"{0}\".\"{1}\";", connectorConfig.destDBSchema, connectorConfig.destObjectName));

                                transaction.Commit();
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(string.Format("Error:{0}", exc.Message));
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Method: DeletePGDedupProcessInSourceTable
        /// Description: It is used to delete all the duplicate records from source table
        /// by ccid and connectorId
        /// </summary>
        /// <param name="connectorConfig"></param>
        //public static void DeletePGDedupProcessInSingleTableWithSafeMode(ConnectorConfig connectorConfig, IList<IDictionary<string, string>> currentRow, ref bool isDeleted)
        //{
        //    try
        //    {
        //        if (connectorConfig.dbConfig != null && connectorConfig.connectorId > 0 && !string.IsNullOrEmpty(connectorConfig.ccid))
        //        {
        //            //Only numeric type values 00.0000 to 00.00
        //            var fetchColumns = (from c in connectorConfig.syncObjectColumns
        //                                join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //                                select $"{c.name}{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();
        //            var fetchColumns1 = (from c in connectorConfig.syncObjectColumns
        //                                 join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //                                 select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();

        //            //Remove special chars in table name
        //            connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
        //            if (currentRow != null && currentRow.Count > 0)
        //            {
        //                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
        //                {
        //                    //Create new transaction 
        //                    int count = 0;
        //                    Console.WriteLine("Reading the records to delete one by one");
        //                    StringBuilder sb = new StringBuilder();
        //                    List<Task> taskLst = new List<Task>();
        //                    int rowCount = 0;
        //                    for (int i = 0; i < currentRow.Count(); i++)//  foreach (var row in currentRow)
        //                    {
        //                        var row = currentRow[i];
        //                        sb.Append($"DELETE FROM \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\" a USING \"{connectorConfig.dbSchema}\".\"{connectorConfig.sourceObjectName}\" b");
        //                        sb.Append(" WHERE  a.ctid<b.ctid ");

        //                        if (fetchColumns1 != null && fetchColumns1.Length > 0)
        //                        {
        //                            foreach (string keys in fetchColumns1)
        //                            {
        //                                sb.Append(" and a." + keys + "=b." + keys + "");
        //                            }
        //                        }
        //                        if (fetchColumns != null && fetchColumns.Length > 0)
        //                        {
        //                            foreach (string keys in fetchColumns)
        //                            {
        //                                //sb.Append(" and a.\"" + keys + "\"=b.\"" + keys + "\"");
        //                                sb.Append(" and a.\"" + keys + "\"='" + row[keys] + "'");
        //                                // sb.Append(" and a.\"" + keys + "\" in (" + string.Join(",", currentRow.Where(p => p.ContainsKey(keys)).Select(p => "'" + p[keys] + "'").Distinct().ToArray()) + ")");
        //                            }
        //                            sb.Append(";");
        //                        }
        //                        rowCount++;
        //                        if (rowCount == 10)
        //                        {
        //                            rowCount = 0;

        //                            taskLst.Add(Task.Run(() =>
        //                            {
        //                                using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
        //                                {
        //                                    try
        //                                    {
        //                                        //excute query
        //                                        connectionFactory.DbConnection.Execute(sb.ToString());
        //                                        transaction.Commit();
        //                                        Console.WriteLine("Deleted record count " + count++);

        //                                    }
        //                                    catch (Exception exc)
        //                                    {
        //                                        Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                                        transaction.Rollback();
        //                                        throw;
        //                                    }
        //                                }
        //                                sb = new StringBuilder();
        //                            }));
        //                        }
        //                    }
        //                    if (sb.ToString().Length > 50)
        //                    {
        //                        taskLst.Add(Task.Run(() =>
        //                        {
        //                            using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
        //                            {
        //                                try
        //                                {
        //                                    //excute query
        //                                    connectionFactory.DbConnection.Execute(sb.ToString());
        //                                    transaction.Commit();
        //                                    Console.WriteLine("Deleted record count " + count++);

        //                                }
        //                                catch (Exception exc)
        //                                {
        //                                    Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                                    transaction.Rollback();
        //                                    throw;
        //                                }
        //                            }
        //                            sb = new StringBuilder();
        //                        }));
        //                    }
        //                    if (taskLst.Count() > 0)
        //                    {
        //                        Task.WaitAll(taskLst.ToArray());
        //                    }
        //                    isDeleted = true;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error:{0}", ex.Message);
        //        throw;
        //    }
        //}

        /// <summary>
        /// Method: DeletePGDedupProcessInSourceTable
        /// Description: It is used to delete all the duplicate records from source table
        /// by ccid and connectorId
        /// </summary>
        /// <param name="connectorConfig"></param>
        //public static async Task DeletePGDedupProcessInSourceTable(ConnectorConfig connectorConfig, IList<IDictionary<string, string>> currentRow)
        //{
        //    try
        //    {
        //        if (connectorConfig.dbConfig != null && connectorConfig.connectorId > 0 && !string.IsNullOrEmpty(connectorConfig.ccid))
        //        {
        //            //Only numeric type values 00.0000 to 00.00
        //            var fetchColumns = (from c in connectorConfig.syncObjectColumns
        //                                join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //                                select $"{c.name}{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();
        //            var fetchColumns1 = (from c in connectorConfig.syncObjectColumns
        //                                 join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //                                 select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();
        //            StringBuilder sb = new StringBuilder();
        //            //Remove special chars in table name
        //            connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
        //            if (currentRow != null && currentRow.Count > 0)
        //            {
        //                int rowCount = 0, deleteCount = 0, count = 0;
        //                Console.WriteLine("Reading the records to delete one by one");
        //                sb = new StringBuilder();
        //                //DELETE FROM "public"."Company" a USING(SELECT* FROM (VALUES (25,'HR25','Delhi24',11111135)) AS
        //                //a1("DeptID", "Department", "Location", "ContactNumber")) b WHERE  a."ContactNumber" = b."ContactNumber"
        //                //and a."Department" = b."Department" and a."Location" = b."Location" 
        //                //and ctid not in(select min(ctid)  from  "public"."Company"
        //                //group by("Department","Location","ContactNumber") having count(ctid)> 1)

        //                //sb.Append($"DELETE FROM {connectorConfig.dbSchema}.\"{connectorConfig.sourceObjectName}\" a USING ");
        //                //sb.Append("(SELECT  * FROM (VALUES ");
        //                //foreach (var rows in currentRow)
        //                //{
        //                //    count = 0;
        //                //    if (fetchColumns != null && fetchColumns.Count() > 0)
        //                //    {
        //                //        if (rowCount == 0)
        //                //            sb.Append("(");
        //                //        else
        //                //            sb.Append(",(");
        //                //        foreach (var key in fetchColumns)
        //                //        {
        //                //            if (count == 0)
        //                //                sb.Append("'" + rows[key] + "'");
        //                //            else
        //                //                sb.Append(",'" + rows[key] + "'");
        //                //            count++;
        //                //        }
        //                //        sb.Append(")");
        //                //    }
        //                //    rowCount++;
        //                //}
        //                //sb.Append(") AS a1 (" + string.Join(",", fetchColumns.Select(c => $"\"{c}\"").ToArray()) + ")) b WHERE ");
        //                //if (fetchColumns != null && fetchColumns.Count() > 0)
        //                //{
        //                //    count = 0;
        //                //    foreach (var key in fetchColumns)
        //                //    {
        //                //        if (count == 0)
        //                //            sb.Append(" a.\"" + key + "\"= b.\"" + key + "\"");
        //                //        else
        //                //            sb.Append(" and a.\"" + key + "\"= b.\"" + key + "\"");
        //                //        count++;
        //                //    }
        //                //}
        //                //sb.Append(" and ctid not in(select min(ctid)  from" + connectorConfig.dbSchema + ".\"" + connectorConfig.sourceObjectName + "\" GROUP BY");
        //                //if (fetchColumns != null && fetchColumns.Count() > 0)
        //                //{
        //                //    count = 0;
        //                //    foreach (var key in fetchColumns)
        //                //    {
        //                //        if (count == 0)
        //                //            sb.Append(" \"" + key + "\"");
        //                //        else
        //                //            sb.Append(",\"" + key + "\"");
        //                //        count++;
        //                //    }
        //                //}
        //                //sb.Append(") HAVING COUNT(ctid)>1)");

        //                sb.Append($"DELETE FROM {connectorConfig.dbSchema}.\"{connectorConfig.sourceObjectName}\" WHERE(" + string.Join(",", fetchColumns1.Select(c => c).ToArray()) + ",ctid)");
        //                sb.Append(" NOT IN(SELECT " + string.Join(", ", fetchColumns1.Select(c => $"a." + c).ToArray()) + ",min(a.ctid) FROM " + connectorConfig.dbSchema + ".\"" + connectorConfig.sourceObjectName + "\" a");
        //                sb.Append(" LEFT JOIN(VALUES");
        //                count = 0;
        //                foreach (var rows in currentRow)
        //                {
        //                    count = 0;
        //                    if (fetchColumns != null && fetchColumns.Length > 0)
        //                    {
        //                        if (rowCount == 0)
        //                            sb.Append("(");
        //                        else
        //                            sb.Append(",(");
        //                        foreach (string keys in fetchColumns)
        //                        {
        //                            if (count == 0)
        //                                sb.Append("'" + rows[keys] + "'");
        //                            else
        //                                sb.Append(",'" + rows[keys] + "'");
        //                            count++;
        //                        }
        //                        sb.Append(")");
        //                    }
        //                    rowCount++;
        //                }
        //                sb.Append(") AS b(" + string.Join(",", fetchColumns.Select(c => c).ToArray()) + ") ON ");
        //                count = 0;
        //                foreach (string keys in fetchColumns)
        //                {
        //                    if (count == 0)
        //                        sb.Append(" " + keys + "=a.\"" + keys + "\"::varchar");
        //                    else
        //                        sb.Append(" and " + keys + "=a.\"" + keys + "\"::varchar");
        //                    count++;
        //                }
        //                sb.Append(" GROUP BY ");
        //                count = 0;
        //                foreach (string keys in fetchColumns)
        //                {
        //                    if (count == 0)
        //                        sb.Append("a.\"" + keys + "\"");
        //                    else
        //                        sb.Append(",a.\"" + keys + "\"");
        //                    count++;
        //                }
        //                sb.Append(" ); ");
        //                //DELETE FROM "public"."Company" WHERE("DeptID", "Department", "Location", "ContactNumber", ctid) not in 
        //                //(SELECT a."DeptID", a."Department", a."Location",a."ContactNumber",min(a.ctid) FROM "public"."Company" a
        //                //LEFT JOIN(VALUES('25', 'HR25', 'Delhi24', '11111135'), ('24', 'HR24', 'Delhi23', '11111134')) 
        //                //AS b(DeptID, Department, Location, ContactNumber)
        //                //on DeptID = a."DeptID"::varchar and ContactNumber = a."ContactNumber"::varchar
        //                //and Department = a."Department"::varchar  and Location = a."Location"::varchar
        //                //GROUP BY a."DeptID", a."Department", a."Location",a."ContactNumber")

        //                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
        //                {
        //                    //Create new transaction 
        //                    using (IDbTransaction transaction = connectionFactory.DbConnection.BeginTransaction())
        //                    {
        //                        try
        //                        {
        //                            //excute query
        //                            await connectionFactory.DbConnection.ExecuteAsync(sb.ToString());
        //                            transaction.Commit();
        //                            sb = null;
        //                            Console.WriteLine("Deleted record count " + deleteCount++);
        //                        }
        //                        catch (Exception exc)
        //                        {
        //                            sb = null;
        //                            Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                            transaction.Rollback();
        //                            throw;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error:{0}", ex.Message);
        //        throw;
        //    }
        //}
        ///// <summary>
        /// Method: DeleteRecordsFromFirstSourceTable
        /// Description: It is used to delete  the duplicate records from first source table which is available in secound source table
        /// by ccid and connectorId
        /// </summary>
        /// <param name="connectorConfig"></param>
        //public static async Task DeleteRecordsFromFirstSourceTable(ConnectorConfig connectorConfig, IList<IDictionary<string, string>> currentRow)
        //{
        //    try
        //    {
        //        if (connectorConfig.dbConfig_compare != null && connectorConfig.connectorId > 0 && !string.IsNullOrEmpty(connectorConfig.ccid))
        //        {
        //            //Only numeric type values 00.0000 to 00.00
        //            var fetchColumns = (from c in connectorConfig.syncObjectColumns
        //                                join f in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals f.ToLower().Trim()
        //                                select $"\"{c.name}\"{(c.fieldType.ToLower().Contains("numeric(") ? "::text" : "")}").ToArray();

        //            //Remove special chars in table name
        //            connectorConfig.sourceObjectName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
        //            if (currentRow != null && currentRow.Count > 0)
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                int rowCount = 0;
        //                sb = new StringBuilder();
        //                Console.WriteLine("Reading the records to delete one by one");

        //                sb.Append($"DELETE FROM {connectorConfig.dbSchema}.\"{connectorConfig.sourceObjectName}\" a USING ");
        //                sb.Append("(SELECT  * FROM (VALUES ");
        //                for (int i = 0; i < currentRow.Count; i++)// foreach (var rows in currentRow)
        //                {
        //                    var rows = currentRow[i];
        //                    int count = 0;
        //                    if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                    {
        //                        if (rowCount == 0)
        //                            sb.Append("(");
        //                        else
        //                            sb.Append(",(");
        //                        foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                        {
        //                            var datatype1 = connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Key.ToLower().Trim()).fieldType;
        //                            switch (datatype1.ToLower())
        //                            {
        //                                case "integer":
        //                                case "decimal":
        //                                case "float":
        //                                case "int":
        //                                case "serial":
        //                                case "smallserial":
        //                                case "smallint":
        //                                case "bigint":
        //                                case "bigserial":
        //                                case "number":
        //                                case "real":
        //                                case "double":
        //                                case "double precision":
        //                                case "numeric":
        //                                    if (count == 0)
        //                                        sb.Append("COALESCE(NULLIF('" + rows[key.Key] + "',''),'0')");
        //                                    else
        //                                        sb.Append(",COALESCE(NULLIF('" + rows[key.Key] + "',''),'0')");
        //                                    break;
        //                                case "timestamp without time zone":
        //                                case "timestamp with time zone":
        //                                case "time without time zone":
        //                                case "time with time zone":
        //                                case "date":
        //                                case "timestamp":
        //                                case "timestamp without time zone[]":
        //                                case "timestamp with time zone[]":
        //                                case "time without time zone[]":
        //                                case "time with time zone[]":
        //                                case "datetime":
        //                                case "boolean":
        //                                case "bit":
        //                                    if (count == 0)
        //                                        sb.Append("'" + rows[key.Key] + "'");
        //                                    else
        //                                        sb.Append(",'" + rows[key.Key] + "'");
        //                                    break;
        //                                case "text":
        //                                case "character varying(50)":
        //                                    if (count == 0)
        //                                        sb.Append("COALESCE(NULLIF('" + rows[key.Key] + "',''),'dedup')");
        //                                    else
        //                                        sb.Append(",COALESCE(NULLIF('" + rows[key.Key] + "',''),'dedup')");
        //                                    break;
        //                            }
        //                            count++;
        //                        }
        //                        sb.Append(")");
        //                    }
        //                    rowCount++;
        //                }
        //                currentRow = null;
        //                sb.Append(") AS a1 (" + string.Join(",", connectorConfig.compareObjectFieldsMapping.Select(c => $"\"{c.Key}\"").ToArray()) + ")) b WHERE ");
        //                if (connectorConfig.compareObjectFieldsMapping != null && connectorConfig.compareObjectFieldsMapping.Count > 0)
        //                {
        //                    int count = 0;
        //                    foreach (var key in connectorConfig.compareObjectFieldsMapping)
        //                    {
        //                        var datatype = connectorConfig.dbConfig_compare.syncCompareObjectColumns.FirstOrDefault(c => c.name.ToLower().Trim() == key.Key.ToLower().Trim()).fieldType;
        //                        if (!String.IsNullOrEmpty(datatype) && datatype.Contains("character varying"))
        //                        {
        //                            datatype = "character varying";
        //                        }
        //                        switch (datatype.ToLower())
        //                        {
        //                            case "integer":
        //                            case "decimal":
        //                            case "float":
        //                            case "int":
        //                            case "serial":
        //                            case "smallserial":
        //                            case "smallint":
        //                            case "bigint":
        //                            case "bigserial":
        //                            case "number":
        //                            case "real":
        //                            case "double":
        //                            case "double precision":
        //                            case "numeric":
        //                                if (count == 0)
        //                                {
        //                                    sb.Append("COALESCE(a.\"" + key.Key + "\",0)=COALESCE(b.\"" + key.Key + "\",0)");
        //                                }
        //                                else
        //                                {
        //                                    sb.Append(" and COALESCE(a.\"" + key.Key + "\",0)=COALESCE(b.\"" + key.Key + "\",0)");
        //                                }
        //                                break;
        //                            case "timestamp without time zone":
        //                            case "timestamp with time zone":
        //                            case "time without time zone":
        //                            case "time with time zone":
        //                            case "date":
        //                            case "timestamp":
        //                            case "timestamp without time zone[]":
        //                            case "timestamp with time zone[]":
        //                            case "time without time zone[]":
        //                            case "time with time zone[]":
        //                            case "datetime":
        //                            case "boolean":
        //                            case "bit":
        //                                if (count == 0)
        //                                    sb.Append(" a.\"" + key.Key + "\"=b.\"" + key.Key + "\"::" + datatype);
        //                                else
        //                                    sb.Append(" and a.\"" + key.Key + "\"=b.\"" + key.Key + "\"::" + datatype);
        //                                break;
        //                            case "text":
        //                            case "character varying":
        //                                if (count == 0)
        //                                {
        //                                    sb.Append("COALESCE(a.\"" + key.Key + "\",'dedup')=COALESCE(b.\"" + key.Key + "\",'dedup')");
        //                                }
        //                                else
        //                                {
        //                                    sb.Append(" and COALESCE(a.\"" + key.Key + "\",'dedup')=COALESCE(b.\"" + key.Key + "\",'dedup')");
        //                                }
        //                                break;
        //                        }
        //                        count++;

        //                    }
        //                }
        //                sb.Append(";");
        //                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig.syncDefaultDatabaseUrl))
        //                {
        //                    try
        //                    {
        //                        //excute query
        //                        int count = await connectionFactory.DbConnection.ExecuteAsync(sb.ToString());
        //                        Console.WriteLine("Deleted record count " + count);
        //                        sb = null;
        //                    }
        //                    catch (Exception exc)
        //                    {
        //                        sb = null;
        //                        Console.WriteLine(string.Format("Error:{0}", exc.Message));
        //                        throw;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error:{0}", ex.Message);
        //        throw;
        //    }
        //}
        ///// <summary>
        /// Method: GetPGRecordCountByName
        /// Description: It is used to get total rows count from sync table by table name.
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <returns>rows count as int</returns>
        public static int GetPGRecordCountByName(ConnectorConfig connectorConfig)
        {
            var recordCount = 0;
            string tableName = string.Empty;
            string schemaName = string.Empty;
            string connectionURL = string.Empty;

            try
            {
                //Remove special chars in table name

                if (connectorConfig.dedup_type == DedupType.Full_Dedup)
                {
                    if (connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B)
                    {
                        if (connectorConfig.dbConfig_compare.table_type == SelectedTableType.Create_New_Table)
                        {
                            tableName = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);
                            schemaName = connectorConfig.dbConfig_compare.db_schema;
                        }
                        else
                        {
                            tableName = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.object_name);
                            schemaName = connectorConfig.dbConfig_compare.db_schema;
                        }
                        connectionURL = connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl;
                    }
                    else
                    {
                        tableName = Utilities.RemoveSpecialChars(connectorConfig.sourceObjectName);
                        schemaName = connectorConfig.dbSchema;
                        connectionURL = connectorConfig.dbConfig.syncDefaultDatabaseUrl;
                    }
                }
                else
                {
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
                    tableName = connectorConfig.destObjectName;
                    schemaName = connectorConfig.destDBSchema;
                    connectionURL = connectorConfig.destDBConfig.syncDefaultDatabaseUrl;
                }
                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionURL))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(string.Format("SELECT count(*) FROM \"{0}\".\"{1}\"", schemaName, tableName));

                    //Excute script
                    recordCount = connectionFactory.DbConnection.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM \"{0}\".\"{1}\"", schemaName, tableName));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return recordCount;
        }

        /// Method: CreatePostgresTable
        /// Description: It is used to create/re-create sync table on destination.
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="errorMessage"></param>
        /// <returns>status as int</returns>
        public static int CreatePostgresTable(ConnectorConfig connectorConfig, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                //Check columns are there or not
                if (connectorConfig.sourceObjectFields != null && connectorConfig.destDBConfig != null)
                {
                    var deCols = GetPGDatabaseTableColumns(connectorConfig.dbConfig, connectorConfig.sourceObjectName, connectorConfig.dbSchema);

                    if (deCols != null && deCols.Count > 0)
                    {
                        //connectorConfig.syncDestObjectColumns = (from c in deCols
                        //                                         select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), isPrimaryKey = c.isPrimaryKey, isRequired = c.isRequired, maxLength = c.length }).ToList();
                        connectorConfig.syncDestObjectColumns = (from c in deCols
                                                                 select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), maxLength = c.length }).ToList();

                    }

                    //Remove special chars in table name
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                    //check if table exists or not
                    if (SyncTableIsExist(connectorConfig) == 1)
                    {
                        return 2;
                    }

                    //Create new connection to sync database
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(" CREATE TABLE ");
                        sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                        sb.Append("(");

                        List<string> primaryKeys = new List<string>();
                        //Assign column data type
                        for (int i = 0; i < connectorConfig.syncDestObjectColumns.Count(); i++)
                        {
                            var dataType = connectorConfig.syncDestObjectColumns[i].fieldType.ToLower();
                            sb.Append(string.Format("{0} {1}", "\"" + connectorConfig.syncDestObjectColumns[i].name + "\"", dataType));

                            //if (connectorConfig.syncDestObjectColumns[i].isPrimaryKey == true)
                            //{
                            //    sb.Append(" NOT NULL");
                            //    primaryKeys.Add(connectorConfig.syncDestObjectColumns[i].name);
                            //}
                            //if (connectorConfig.syncDestObjectColumns[i].isRequired == true)
                            //{
                            //    sb.Append(" NOT NULL");
                            //}

                            if ((i + 1) < connectorConfig.syncDestObjectColumns.Count())
                            {
                                sb.Append(", ");
                            }
                        }
                        //if (primaryKeys.Count() > 0)
                        //{
                        //    sb.Append(", constraint " + string.Format("pk_{0}", Utilities.RemoveSpecialCharsIncludeDash(connectorConfig.destObjectName)) + " primary key (" + string.Join(",", primaryKeys.Select(x => "\"" + x + "\"").ToArray()) + ")");
                        //}
                        sb.Append(");");

                        //Excute create table script
                        connectionFactory.DbConnection.Execute(sb.ToString());
                        sb = null;

                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                errorMessage = ex.Message;
                return -1;
            }

            return -1;
        }

        /// Method: CreatePostgresGoldenTable
        /// Description: It is used to create/re-create sync table on  Golden Table to sync data to postgres table.
        /// </summary>
        /// <param name="connectorConfig"></param>
        /// <param name="errorMessage"></param>
        /// <returns>status as int</returns>
        public static int CreatePostgresGoldenTable(ConnectorConfig connectorConfig, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                //Check columns are there or not
                if (connectorConfig.sourceObjectFields != null && connectorConfig.dbConfig_compare != null)
                {
                    var deCols = GetPGDatabaseTableColumns(connectorConfig.dbConfig, connectorConfig.sourceObjectName, connectorConfig.dbSchema);
                    if (deCols != null && deCols.Count > 0)
                    {
                        connectorConfig.syncDestObjectColumns = (from c in deCols
                                                                 select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), maxLength = c.length }).ToList();
                    }

                    //Remove special chars in table name
                    connectorConfig.dbConfig_compare.new_table_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);

                    //check if table exists or not
                    if (SyncGoldenTableIsExist(connectorConfig) == 1)
                    {
                        return 2;
                    }

                    //Create new connection to sync database
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(" CREATE TABLE ");
                        sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.new_table_name));
                        sb.Append("(");

                        List<string> primaryKeys = new List<string>();
                        //Assign column data type
                        for (int i = 0; i < connectorConfig.syncDestObjectColumns.Count(); i++)
                        {
                            var dataType = connectorConfig.syncDestObjectColumns[i].fieldType.ToLower();
                            sb.Append(string.Format("{0} {1}", "\"" + connectorConfig.syncDestObjectColumns[i].name + "\"", dataType));

                            if (connectorConfig.syncDestObjectColumns[i].isPrimaryKey == true)
                            {
                                sb.Append(" NOT NULL");
                                primaryKeys.Add(connectorConfig.syncDestObjectColumns[i].name);
                            }
                            if (connectorConfig.syncDestObjectColumns[i].isRequired == true)
                            {
                                sb.Append(" NOT NULL");
                            }

                            if ((i + 1) < connectorConfig.syncDestObjectColumns.Count())
                            {
                                sb.Append(", ");
                            }
                        }
                        //if (primaryKeys.Count() > 0)
                        //{
                        //    sb.Append(", constraint " + string.Format("pk_{0}", Utilities.RemoveSpecialCharsIncludeDash(connectorConfig.dbConfig_compare.new_table_name)) + " primary key (" + string.Join(",", primaryKeys.Select(x => "\"" + x + "\"").ToArray()) + ")");
                        //}
                        sb.Append(");");

                        //Excute create table script
                        connectionFactory.DbConnection.Execute(sb.ToString());
                        sb = null;

                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                errorMessage = ex.Message;
                return -1;
            }

            return -1;
        }
        /// <summary>
        /// Method: DeDupRowsFromDatabaseTable
        /// Description: It is used to remove duplicate rows from database table
        /// </summary>
        /// <param name="jobCancelToken"></param>
        /// <param name="connectorId"></param>
        /// <param name="ccid"></param>
        [Queue("critical")]
        [JobsFilter()]
        [AutomaticRetry(Attempts = 0, LogEvents = false, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task DeDupRowsFromDatabaseTable(IJobCancellationToken jobCancelToken, int connectorId, string ccid)
        {
            try
            {
                if (connectorId <= 0 || string.IsNullOrEmpty(ccid))
                {
                    throw new ArgumentNullException("Dedup config is null");
                }

                //Get connector config
                ConnectorConfig connectorConfig = GetConnectorById<ConnectorConfig>(ccid, connectorId);
                //await DeDupFindDuplicateBeforeDelete(connectorConfig, jobCancelToken);
                //return;
                if (connectorConfig == null)
                {
                    throw new ArgumentNullException("Dedup config is null");
                }

                if (connectorConfig.syncDestination == ConnectorType.Heroku_Postgres
                    || connectorConfig.syncDestination == ConnectorType.Azure_Postgres
                    || connectorConfig.syncDestination == ConnectorType.AWS_Postgres)
                {
                    if (connectorConfig.dbConfig == null || (connectorConfig.dbConfig != null && string.IsNullOrEmpty(connectorConfig.dbConfig.syncDefaultDatabaseUrl)))
                        throw new ArgumentNullException("Dedup postgres config is null");
                }

                #region Insert Records

                //Remove special chars in table name
                if (connectorConfig.dedup_type != DedupType.Full_Dedup)
                    connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);

                for (int src = 0; src < connectorConfig.multipleDBConfigs.Count; src++)
                {
                    var destTblMtachingCols = default(List<SyncObjectColumn>);
                    var destTblCols = default(List<SyncObjectColumn>);
                    var selectedTblCols = default(List<SyncObjectColumn>);
                    var srcTblCols = default(List<DatabaseTableColumns>);
                    var compareTblCols = default(List<DatabaseTableColumns>);

                    connectorConfig.dbConfig = connectorConfig.multipleDBConfigs.ElementAt(src);

                    if (connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination &&
                         src < connectorConfig.multipleDBConfigs.Count - 1)
                    {
                        continue;
                    }
                    if (!String.IsNullOrEmpty(connectorConfig.dbConfig.object_name))
                    {
                        connectorConfig.sourceObjectName = connectorConfig.dbConfig.object_name;
                    }
                    if (!String.IsNullOrEmpty(connectorConfig.dbConfig.new_table_name) && connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination)
                    {
                        connectorConfig.sourceObjectName = connectorConfig.dbConfig.new_table_name;
                        connectorConfig.dbConfig.object_name = connectorConfig.dbConfig.new_table_name;
                    }
                    if (!String.IsNullOrEmpty(connectorConfig.dbConfig.db_schema))
                    {
                        connectorConfig.dbSchema = connectorConfig.dbConfig.db_schema;
                    }

                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching database table columns starts");
                    srcTblCols = GetPGDatabaseTableColumns(connectorConfig.dbConfig, connectorConfig.sourceObjectName, connectorConfig.dbSchema);
                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching database table columns ended");
                    if (srcTblCols != null && srcTblCols.Count > 0)
                    {
                        //set primary key columns for order by
                        if (srcTblCols.Where(p => p.isPrimaryKey == true).Count() > 0)
                        {
                            connectorConfig.orderByColumns = srcTblCols.Where(p => p.isPrimaryKey == true).Select(p => p.name).ToList();
                            connectorConfig.hasPrimaryKey = true;
                            connectorConfig.hasOrderColumns = true;
                        }
                        if (!connectorConfig.hasOrderColumns)
                        {
                            WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching database table index columns starts");
                            List<DatabaseTableIndexedColumns> indexedColumns = GetPGDatabaseTableIndexedColumns(connectorConfig.dbConfig, connectorConfig.sourceObjectName, connectorConfig.dbSchema);
                            WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching database table index columns ended");
                            if (indexedColumns != null && indexedColumns.Count() > 0)
                            {
                                if (indexedColumns.Where(p => p.is_index_on_multiple_columns == false).Count() > 0)
                                {
                                    connectorConfig.orderByColumns = indexedColumns.Where(p => p.is_index_on_multiple_columns == false).Take(1).Select(p => p.column_names).ToList();
                                    connectorConfig.hasOrderColumns = true;
                                }
                                else
                                {
                                    connectorConfig.orderByColumns = indexedColumns.FirstOrDefault().column_names.Split(",").ToList();
                                    connectorConfig.hasOrderColumns = true;
                                }
                            }
                        }
                        selectedTblCols = (from c in srcTblCols
                                           join p in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals p.ToLower().Trim()
                                           select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), isPrimaryKey = c.isPrimaryKey, isRequired = c.isRequired, maxLength = c.length }).ToList();

                        destTblCols = (from c in srcTblCols
                                       select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), maxLength = c.length }).ToList();
                    }
                    connectorConfig.syncObjectColumns = selectedTblCols;
                    selectedTblCols = null;
                    connectorConfig.syncDestObjectColumns = destTblCols;
                    destTblCols = null;
                    if (connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B)
                    {
                        if (connectorConfig.dbConfig.table_type != SelectedTableType.Create_New_Table)
                        {
                            compareTblCols = GetPGDatabaseTableColumns(connectorConfig.dbConfig, connectorConfig.dbConfig.object_name, connectorConfig.dbConfig.db_schema);
                        }
                        else
                        {
                            compareTblCols = GetPGDatabaseTableColumns(connectorConfig.dbConfig, connectorConfig.dbConfig.new_table_name, connectorConfig.dbConfig.db_schema);
                        }

                        if (compareTblCols != null && compareTblCols.Count > 0)
                        {
                            //    connectorConfig.dbConfig.syncCompareObjectColumns = (from c in compareTblCols
                            //                                                         join p in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals p.ToLower().Trim()
                            //                                                         select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), isPrimaryKey = c.isPrimaryKey, isRequired = c.isRequired }).ToList();
                            connectorConfig.dbConfig.syncCompareObjectColumns = (from c in compareTblCols
                                                                                 join p in connectorConfig.sourceObjectFields on c.name.ToLower().Trim() equals p.ToLower().Trim()
                                                                                 select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString() }).ToList();

                            if (connectorConfig.dbConfig.table_type == SelectedTableType.Select_Existing_Table)
                            {
                                destTblMtachingCols = (from c in compareTblCols
                                                       join p in srcTblCols on new { columnName = c.name.ToLower().Trim(), dataType = c.fieldType.Trim() } equals new { columnName = p.name.ToLower().Trim(), dataType = p.fieldType.Trim() }
                                                       select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), maxLength = c.length }).ToList();
                                connectorConfig.syncDestObjectColumns = destTblMtachingCols;
                            }
                            else
                            {
                                if (srcTblCols != null && srcTblCols.Count > 0)
                                {
                                    destTblCols = (from c in srcTblCols
                                                   select new SyncObjectColumn() { name = c.name, fieldType = c.fieldType.ToString(), isPrimaryKey = c.isPrimaryKey, isRequired = c.isRequired, maxLength = c.length }).ToList();
                                    connectorConfig.syncDestObjectColumns = destTblCols;
                                }
                            }
                        }
                    }

                    if (connectorConfig.syncObjectColumns == null)
                    {
                        throw new ArgumentNullException("Database table columns are null");
                    }

                    //Cancel current task if cancel requested (eg: when system getting shutdown)
                    if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                    {
                        jobCancelToken.ThrowIfCancellationRequested();
                        return;
                    }

                    if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table && connectorConfig.dedup_type == DedupType.Full_Dedup)
                    {
                        WriteProcessLogToConsole(connectorConfig.connectorName, "No process for single full dedup");
                        // await RemoveDuplicatesFromSingleTable(connectorConfig);
                        await DeDupFindDuplicateBeforeDelete(connectorConfig, jobCancelToken);
                        await DeDupPGTableRecordsByFilter(connectorConfig);
                    }
                    else
                    {
                        //check table exist or not. if not then create table and sync
                        // if (connectorConfig.dedup_type != DedupType.Full_Dedup && connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table)
                        {
                            connectorConfig.isTableExist = true;
                            int isExist = SyncTableIsExist(connectorConfig);
                            if (isExist == 0)
                            {
                                connectorConfig.isTableExist = false;
                                string errorMessage = string.Empty;
                                if (connectorConfig.dedup_type != DedupType.Full_Dedup || connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table)
                                {
                                    var dbStatus = CreatePostgresTable(connectorConfig, out errorMessage);
                                    if (dbStatus <= 0 && !string.IsNullOrEmpty(errorMessage))
                                    {
                                        Console.WriteLine(errorMessage);
                                        throw new Exception(errorMessage);
                                    }
                                }
                            }
                            //else
                            //{
                            //    if ((connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination
                            //           && !connectorConfig.lastSyncAt.HasValue && connectorConfig.dbConfig_compare.table_type == SelectedTableType.Create_New_Table) ||
                            //            (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table
                            //            && connectorConfig.dedup_type == DedupType.Simulate_and_Verify))
                            //    {
                            //        RemovePGSyncTableRows(connectorConfig);
                            //    }
                            //}
                        }

                        //if (connectorConfig.dedup_type == DedupType.Full_Dedup && connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination && connectorConfig.dbConfig_compare.table_type == SelectedTableType.Create_New_Table)
                        //{
                        //    connectorConfig.isTableExist = true;
                        //    int isExist = SyncGoldenTableIsExist(connectorConfig);
                        //    if (isExist == 0)
                        //    {
                        //        connectorConfig.isTableExist = false;
                        //        string errorMessage = string.Empty;
                        //        if (connectorConfig.dedup_type == DedupType.Full_Dedup)
                        //        {
                        //            var dbStatus = CreatePostgresGoldenTable(connectorConfig, out errorMessage);
                        //            if (dbStatus <= 0 && !string.IsNullOrEmpty(errorMessage))
                        //            {
                        //                Console.WriteLine(errorMessage);
                        //                throw new Exception(errorMessage);
                        //            }
                        //        }
                        //    }
                        //}

                        PGTableDataSize tableDataSize = default(PGTableDataSize);
                        Exception taskException = null;
                        int syncStatus = GetSyncStatus(ccid: connectorConfig.ccid, connectorId: (int)connectorConfig.connectorId);
                        var cancelToken = CancellationTokenSource.CreateLinkedTokenSource(jobCancelToken.ShutdownToken);
                        List<Task> taskLst = new List<Task>();
                        if (Environment.ProcessorCount == 2)
                        {
                            WriteProcessLogToConsole(connectorConfig.connectorName, "ProcessorCount:2");
                            DateTime starttime = DateTime.Now;
                            if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table
                                || ((connectorConfig.dedupSourceType != SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination ||
                                connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B) &&
                                connectorConfig.dedup_type == DedupType.Simulate_and_Verify) && src < connectorConfig.multipleDBConfigs.Count - 1)
                            {
                                WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + starttime);
                                if ((connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B) && connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                                {
                                    tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig, status: "compare").ConfigureAwait(false);
                                    //if (tableDataSize == null || (tableDataSize != null && tableDataSize.total_rows == 0))
                                    //{
                                    //    tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig).ConfigureAwait(false);
                                    //}
                                }
                                else
                                {
                                    tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig).ConfigureAwait(false);
                                }
                                WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + DateTime.Now);
                                if (tableDataSize != null && tableDataSize.total_rows > 0)
                                {
                                    int iCurrentPage = 1;
                                    int iPageSize = ComputeAvgPageSize(tableDataSize);
                                    int iNoOfPages = (int)Math.Ceiling(tableDataSize.total_rows / (decimal)iPageSize);
                                    if (iNoOfPages > 0)
                                    {
                                        tableDataSize = null;
                                        List<string> fetchDestColumns = null;
                                        if ((connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B)
                                            && connectorConfig.dedup_type == DedupType.Simulate_and_Verify
                                            && connectorConfig.dbConfig_compare.table_type == SelectedTableType.Select_Existing_Table)
                                        {
                                            fetchDestColumns = (from c in destTblMtachingCols
                                                                select $"{c.name}").ToList();
                                        }
                                        else
                                        {
                                            fetchDestColumns = (from c in connectorConfig.syncDestObjectColumns
                                                                select $"{c.name}").ToList();
                                        }
                                        List<IDictionary<string, object>> pgRows = new List<IDictionary<string, object>>();

                                        do
                                        {
                                            if (taskException != null)
                                            {
                                                throw taskException;
                                            }

                                            //Cancel current task if cancel requested (eg: when system getting shutdown)
                                            if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                            {
                                                jobCancelToken.ThrowIfCancellationRequested();
                                                return;
                                            }

                                            //check sync status
                                            if (syncStatus != 1)
                                            {
                                                return;
                                            }

                                            starttime = DateTime.Now;
                                            WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching inserted records from pg table starts, Statring Time :" + starttime);
                                            if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table && connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                                            {
                                                try
                                                {
                                                    pgRows.AddMultiple(GetPGRecordsByFilter(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));
                                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching inserted records from pg table ended End time for one cycle :" + DateTime.Now);
                                                    WriteProcessLogToConsole(connectorConfig.connectorName, string.Format("Fetching inserted records from pg table ended Total time for one cycle : {0}", DateTime.Now - starttime));
                                                    if (pgRows != null && pgRows.Count() > 0)
                                                    {
                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching inserted 1");
                                                        //remove columns which don't exist in dest. table
                                                        if (pgRows.ElementAt(0).Keys.Except(fetchDestColumns).Count() > 0)
                                                        {
                                                            var properties = pgRows.ElementAt(0).Keys.Except(fetchDestColumns).ToList();
                                                            pgRows = pgRows.Select(dic => dic.Where(kv => properties.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList<IDictionary<string, object>>();
                                                        }

                                                        //if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify || connectorConfig.dedup_type == DedupType.Safe_Mode)
                                                        {
                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert started");
                                                            using (PGQueryStatus queryStatus = PGBulkInsert(pgRows, connectorConfig))
                                                            {
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert ended");
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert count : " + (queryStatus == null ? 0 : queryStatus.inserted_rows));
                                                                if (queryStatus != null && (queryStatus.inserted_rows > 0 || queryStatus.updated_rows > 0))
                                                                {
                                                                    //update sync info
                                                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info starts");
                                                                    syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, status: -1, count: (queryStatus.inserted_rows > 0 ? queryStatus.inserted_rows : -1), sync_updated_count: (queryStatus.updated_rows > 0 ? queryStatus.updated_rows : -1));
                                                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info ended");
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception outerExp)
                                                {
                                                    if (outerExp.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")
                                                        || (outerExp.InnerException != null && outerExp.InnerException.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")))
                                                    {
                                                        int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage + 1) * iPageSize);
                                                        string syncLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz} [{Microsoft.Extensions.Logging.LogLevel.Error}]: Failed to copy records from {offSet} to {offSet + iPageSize} due to {outerExp.Message} {Environment.NewLine}, continuing with remaining records {Environment.NewLine}";
                                                        syncStatus = UpdateSyncErrorLog(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, syncLog: syncLog);
                                                    }
                                                    else
                                                    {
                                                        taskException = outerExp;
                                                    }
                                                }
                                                finally
                                                {
                                                    //release allocated memory
                                                    // pgRows.ClearMemory();
                                                }
                                            }
                                            else if ((connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B)
                                                && connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                                            {
                                                try
                                                {
                                                    pgRows.AddMultiple(GetRecordsFromCompareTableByFilter(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));
                                                    if (pgRows != null && pgRows.Count() > 0)
                                                    {
                                                        //remove columns which don't exist in dest. table
                                                        if (pgRows.ElementAt(0).Keys.Except(fetchDestColumns).Count() > 0)
                                                        {
                                                            var properties = pgRows.ElementAt(0).Keys.Except(fetchDestColumns).ToList();
                                                            pgRows = pgRows.Select(dic => dic.Where(kv => properties.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList<IDictionary<string, object>>();
                                                        }

                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert started");
                                                        using (PGQueryStatus queryStatus = PGBulkInsert(pgRows, connectorConfig))
                                                        {
                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert ended");
                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert count : " + (queryStatus == null ? 0 : queryStatus.inserted_rows));
                                                            if (queryStatus != null && (queryStatus.inserted_rows > 0 || queryStatus.updated_rows > 0))
                                                            {
                                                                //update sync info
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info starts");
                                                                syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, status: -1, count: (queryStatus.inserted_rows > 0 ? queryStatus.inserted_rows : -1), sync_updated_count: (queryStatus.updated_rows > 0 ? queryStatus.updated_rows : -1));
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info ended");
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception outerExp)
                                                {
                                                    if (outerExp.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")
                                                        || (outerExp.InnerException != null && outerExp.InnerException.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")))
                                                    {
                                                        int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage + 1) * iPageSize);
                                                        string syncLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz} [{Microsoft.Extensions.Logging.LogLevel.Error}]: Failed to copy records from {offSet} to {offSet + iPageSize} due to {outerExp.Message} {Environment.NewLine}, continuing with remaining records {Environment.NewLine}";
                                                        syncStatus = UpdateSyncErrorLog(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, syncLog: syncLog);
                                                    }
                                                    else
                                                    {
                                                        taskException = outerExp;
                                                    }
                                                }
                                                finally
                                                {
                                                    //release allocated memory
                                                    pgRows.ClearMemory(iCurrentPage);
                                                }
                                            }

                                            iCurrentPage++;
                                        } while (iCurrentPage <= iNoOfPages);

                                        pgRows = null;
                                        fetchDestColumns.Clear();
                                        fetchDestColumns = null;
                                    }
                                }
                            }
                            if (src < connectorConfig.multipleDBConfigs.Count - 1)
                                continue;
                            if (connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table)
                            {
                                starttime = DateTime.Now;
                                tableDataSize = default(PGTableDataSize);
                                WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + starttime);
                                tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig).ConfigureAwait(false);
                                WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + DateTime.Now);
                                if (tableDataSize != null && tableDataSize.total_rows > 0)
                                {
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Move remaining records to destination table");
                                    int iCurrentPage = 1;
                                    int iPageSize = ComputeAvgPageSize(tableDataSize);
                                    int iNoOfPages = (int)Math.Ceiling(tableDataSize.total_rows / (decimal)iPageSize);
                                    if (iNoOfPages > 0)
                                    {
                                        tableDataSize = null;
                                        List<string> fetchDestColumns = (from c in connectorConfig.syncDestObjectColumns
                                                                         select $"{c.name}").ToList();
                                        List<IDictionary<string, object>> pgRows = new List<IDictionary<string, object>>();

                                        do
                                        {
                                            if (taskException != null)
                                            {
                                                throw taskException;
                                            }

                                            //Cancel current task if cancel requested (eg: when system getting shutdown)
                                            if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                            {
                                                jobCancelToken.ThrowIfCancellationRequested();
                                                return;
                                            }

                                            //check sync status
                                            if (syncStatus != 1)
                                            {
                                                return;
                                            }

                                            try
                                            {
                                                pgRows.AddMultiple(GetRecordsFromSourceTableByFilter(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));
                                                if (pgRows != null && pgRows.Count() > 0)
                                                {
                                                    //remove columns which don't exist in dest. table
                                                    if (pgRows.ElementAt(0).Keys.Except(fetchDestColumns).Count() > 0)
                                                    {
                                                        var properties = pgRows.ElementAt(0).Keys.Except(fetchDestColumns).ToList();
                                                        pgRows = pgRows.Select(dic => dic.Where(kv => properties.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList<IDictionary<string, object>>();
                                                    }

                                                    if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify || connectorConfig.dedup_type == DedupType.Safe_Mode)
                                                    {
                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert started");
                                                        using (PGQueryStatus queryStatus = PGBulkInsert(pgRows, connectorConfig))
                                                        {
                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert ended");
                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert count : " + (queryStatus == null ? 0 : queryStatus.inserted_rows));
                                                            if (queryStatus != null && (queryStatus.inserted_rows > 0 || queryStatus.updated_rows > 0))
                                                            {
                                                                //update sync info
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info starts");
                                                                syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, status: -1, count: (queryStatus.inserted_rows > 0 ? queryStatus.inserted_rows : -1), sync_updated_count: (queryStatus.updated_rows > 0 ? queryStatus.updated_rows : -1));
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info ended");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception outerExp)
                                            {
                                                if (outerExp.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")
                                                         || (outerExp.InnerException != null && outerExp.InnerException.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")))
                                                {
                                                    int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage + 1) * iPageSize);
                                                    string syncLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz} [{Microsoft.Extensions.Logging.LogLevel.Error}]: Failed to copy records from {offSet} to {offSet + iPageSize} due to {outerExp.Message} {Environment.NewLine}, continuing with remaining records {Environment.NewLine}";
                                                    syncStatus = UpdateSyncErrorLog(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, syncLog: syncLog);
                                                }
                                                else
                                                {
                                                    taskException = outerExp;
                                                }
                                            }
                                            finally
                                            {
                                                //release allocated memory by force
                                                pgRows.ClearMemory(iCurrentPage);
                                            }

                                            iCurrentPage++;
                                        } while (iCurrentPage <= iNoOfPages);

                                        pgRows = null;
                                        fetchDestColumns.Clear();
                                        fetchDestColumns = null;
                                    }
                                }
                            }

                            if (connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table || connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                            {
                                //Cancel current task if cancel requested (eg: when system getting shutdown)
                                if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                {
                                    jobCancelToken.ThrowIfCancellationRequested();
                                    return;
                                }

                                if (taskException != null)
                                {
                                    throw taskException;
                                }

                                //check sync status
                                if (syncStatus != 1)
                                {
                                    return;
                                }
                                await DeDupFindDuplicateBeforeDelete(connectorConfig, jobCancelToken);
                                int count = await DeDupPGTableRecordsByFilter(connectorConfig);
                            }
                            cancelToken = null;
                        }
                        else
                        {
                            int maxCount = Environment.ProcessorCount == 2 ? 8 : Environment.ProcessorCount;
                            WriteProcessLogToConsole(connectorConfig.connectorName, $"ProcessorCount:{Environment.ProcessorCount}");
                            WriteProcessLogToConsole(connectorConfig.connectorName, "Semaphore task allowed count: " + maxCount);
                            using (SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, maxCount))
                            {
                                DateTime starttime = DateTime.Now;
                                if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table
                                    || ((connectorConfig.dedupSourceType != SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B) &&
                                    connectorConfig.dedup_type == DedupType.Simulate_and_Verify) && src < connectorConfig.multipleDBConfigs.Count - 1)
                                {
                                    tableDataSize = default(PGTableDataSize);
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + starttime);
                                    if (connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table && connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                                    {
                                        tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig, status: "compare").ConfigureAwait(false);
                                        //if (tableDataSize == null || (tableDataSize != null && tableDataSize.total_rows == 0))
                                        //{
                                        //    tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig).ConfigureAwait(false);
                                        //}
                                    }
                                    else
                                    {
                                        tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig).ConfigureAwait(false);
                                    }
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + DateTime.Now);
                                    if (tableDataSize != null && tableDataSize.total_rows > 0)
                                    {
                                        int iCurrentPage = 1;
                                        int iPageSize = ComputeAvgPageSize(tableDataSize);
                                        int iNoOfPages = (int)Math.Ceiling(tableDataSize.total_rows / (decimal)iPageSize);
                                        if (iNoOfPages > 0)
                                        {
                                            tableDataSize = null;
                                            List<string> fetchDestColumns = null;
                                            if ((connectorConfig.dedupSourceType == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connectorConfig.dedupSourceType == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B)
                                                && connectorConfig.dedup_type == DedupType.Simulate_and_Verify
                                                && connectorConfig.dbConfig_compare.table_type == SelectedTableType.Select_Existing_Table)
                                            {
                                                fetchDestColumns = (from c in destTblMtachingCols
                                                                    select $"{c.name}").ToList();
                                            }
                                            else
                                            {
                                                fetchDestColumns = (from c in connectorConfig.syncDestObjectColumns
                                                                    select $"{c.name}").ToList();
                                            }
                                            List<IDictionary<string, object>> pgRows = new List<IDictionary<string, object>>();

                                            do
                                            {
                                                if (taskException != null)
                                                {
                                                    throw taskException;
                                                }

                                                //Cancel current task if cancel requested (eg: when system getting shutdown)
                                                if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                                {
                                                    jobCancelToken.ThrowIfCancellationRequested();
                                                    return;
                                                }

                                                //check sync status
                                                if (syncStatus != 1)
                                                {
                                                    return;
                                                }

                                                starttime = DateTime.Now;
                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching inserted records from pg table starts, Statring Time :" + starttime);
                                                if (connectorConfig.dedupSourceType == SourceType.Remove_Duplicates_from_a_Single_Table)
                                                {
                                                    try
                                                    {
                                                        pgRows.AddMultiple(await GetPGRecordsByFilterAsync(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));
                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching inserted records from pg table ended End time for one cycle :" + DateTime.Now);
                                                        WriteProcessLogToConsole(connectorConfig.connectorName, string.Format("Fetching inserted records from pg table ended Total time for one cycle : {0}", DateTime.Now - starttime));
                                                        if (pgRows != null && pgRows.Count() > 0)
                                                        {
                                                            var dedupRows = new List<IDictionary<string, object>>(pgRows);
                                                            //var dedupRows = pgRows;
                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "task count before =>" + taskLst == null ? "0" : taskLst.Count().ToString());
                                                            taskLst.Add(Task.Run(async () =>
                                                            {
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "task count after =>" + taskLst == null ? "0" : taskLst.Count().ToString());

                                                                try
                                                                {
                                                                    if (cancelToken.IsCancellationRequested)
                                                                    {
                                                                        cancelToken.Token.ThrowIfCancellationRequested();
                                                                        return;
                                                                    }

                                                                    //wait for thread
                                                                    await semaphoreSlim.WaitAsync(cancelToken.Token);

                                                                    //remove columns which don't exist in dest. table

                                                                    if (dedupRows.ElementAt(0).Keys.Except(fetchDestColumns).Count() > 0)
                                                                    {
                                                                        var properties = dedupRows.ElementAt(0).Keys.Except(fetchDestColumns).ToList();
                                                                        dedupRows = dedupRows.Select(dic => dic.Where(kv => properties.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList<IDictionary<string, object>>();
                                                                    }

                                                                    if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify || connectorConfig.dedup_type == DedupType.Safe_Mode)
                                                                    {
                                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert started");
                                                                        using (PGQueryStatus queryStatus = await PGBulkInsertAsync(dedupRows, connectorConfig))
                                                                        {
                                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert ended");
                                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert count : " + (queryStatus == null ? 0 : queryStatus.inserted_rows));
                                                                            if (queryStatus != null && (queryStatus.inserted_rows > 0 || queryStatus.updated_rows > 0))
                                                                            {
                                                                                //update sync info
                                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info starts");
                                                                                syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, status: -1, count: (queryStatus.inserted_rows > 0 ? queryStatus.inserted_rows : -1), sync_updated_count: (queryStatus.updated_rows > 0 ? queryStatus.updated_rows : -1));
                                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info ended");
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception taskExp)
                                                                {
                                                                    taskException = taskExp;
                                                                }
                                                                finally
                                                                {
                                                                    semaphoreSlim.Release();
                                                                }

                                                            }, cancelToken.Token));
                                                        }
                                                    }
                                                    catch (Exception outerExp)
                                                    {
                                                        if (outerExp.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")
                                                            || (outerExp.InnerException != null && outerExp.InnerException.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")))
                                                        {
                                                            int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage + 1) * iPageSize);
                                                            string syncLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz} [{Microsoft.Extensions.Logging.LogLevel.Error}]: Failed to copy records from {offSet} to {offSet + iPageSize} due to {outerExp.Message} {Environment.NewLine}, continuing with remaining records {Environment.NewLine}";
                                                            syncStatus = UpdateSyncErrorLog(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, syncLog: syncLog);
                                                        }
                                                        else
                                                        {
                                                            taskException = outerExp;
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        // pgRows.ClearMemory(iCurrentPage);
                                                    }
                                                }
                                                else if (connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table && connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                                                {
                                                    try
                                                    {
                                                        pgRows.AddMultiple(await GetPGRecordsByFilterAsync(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));

                                                        //pgRows.AddMultiple(await GetRecordsFromCompareTableByFilterAsync(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));
                                                        if (pgRows != null && pgRows.Count() > 0)
                                                        {
                                                            //int iLastPageNo = iCurrentPage;
                                                            var dedupRows = new List<IDictionary<string, object>>(pgRows);
                                                            taskLst.Add(Task.Run(async () =>
                                                            {
                                                                try
                                                                {
                                                                    if (cancelToken.IsCancellationRequested)
                                                                    {
                                                                        cancelToken.Token.ThrowIfCancellationRequested();
                                                                        return;
                                                                    }

                                                                    //wait for thread
                                                                    await semaphoreSlim.WaitAsync(cancelToken.Token);

                                                                    //remove columns which don't exist in dest. table
                                                                    if (dedupRows.ElementAt(0).Keys.Except(fetchDestColumns).Count() > 0)
                                                                    {
                                                                        var properties = dedupRows.ElementAt(0).Keys.Except(fetchDestColumns).ToList();
                                                                        dedupRows = dedupRows.Select(dic => dic.Where(kv => properties.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList<IDictionary<string, object>>();
                                                                    }

                                                                    WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert started");
                                                                    using (PGQueryStatus queryStatus = await PGBulkInsertAsync(dedupRows, connectorConfig))
                                                                    {
                                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert ended");
                                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert count : " + (queryStatus == null ? 0 : queryStatus.inserted_rows));
                                                                        if (queryStatus != null && (queryStatus.inserted_rows > 0 || queryStatus.updated_rows > 0))
                                                                        {
                                                                            //update sync info
                                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info starts");
                                                                            syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, status: -1, count: (queryStatus.inserted_rows > 0 ? queryStatus.inserted_rows : -1), sync_updated_count: (queryStatus.updated_rows > 0 ? queryStatus.updated_rows : -1));
                                                                            WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info ended");
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception taskExp)
                                                                {
                                                                    taskException = taskExp;
                                                                }
                                                                finally
                                                                {
                                                                    semaphoreSlim.Release();
                                                                }
                                                            }, cancelToken.Token));
                                                        }
                                                    }
                                                    catch (Exception outerExp)
                                                    {
                                                        if (outerExp.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")
                                                            || (outerExp.InnerException != null && outerExp.InnerException.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")))
                                                        {
                                                            int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage + 1) * iPageSize);
                                                            string syncLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz} [{Microsoft.Extensions.Logging.LogLevel.Error}]: Failed to copy records from {offSet} to {offSet + iPageSize} due to {outerExp.Message} {Environment.NewLine}, continuing with remaining records {Environment.NewLine}";
                                                            syncStatus = UpdateSyncErrorLog(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, syncLog: syncLog);
                                                        }
                                                        else
                                                        {
                                                            taskException = outerExp;
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        //  pgRows.ClearMemory(iCurrentPage);
                                                    }
                                                }

                                                iCurrentPage++;
                                            } while (iCurrentPage <= iNoOfPages);

                                            pgRows = null;
                                        }
                                    }

                                    if (taskLst.Count() > 0)
                                    {
                                        await Task.WhenAll(taskLst.ToArray());
                                        taskLst.Clear();
                                    }
                                }

                                taskLst = new List<Task>();

                                if (src < connectorConfig.multipleDBConfigs.Count - 1)
                                    continue;
                                if (connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table)
                                {
                                    tableDataSize = default(PGTableDataSize);
                                    starttime = DateTime.Now;
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + starttime);
                                    tableDataSize = await GetPGTableRecordCountByFilter(connectorConfig).ConfigureAwait(false);
                                    WriteProcessLogToConsole(connectorConfig.connectorName, "Fetching unique total duplicate records :" + DateTime.Now);
                                    if (tableDataSize != null && tableDataSize.total_rows > 0)
                                    {
                                        WriteProcessLogToConsole(connectorConfig.connectorName, "Move remaining records to compare table");
                                        int iCurrentPage = 1;
                                        int iPageSize = ComputeAvgPageSize(tableDataSize);
                                        int iNoOfPages = (int)Math.Ceiling(tableDataSize.total_rows / (decimal)iPageSize);
                                        if (iNoOfPages > 0)
                                        {
                                            tableDataSize = null;
                                            List<string> fetchDestColumns = (from c in connectorConfig.syncDestObjectColumns
                                                                             select $"{c.name}").ToList();
                                            List<IDictionary<string, object>> pgRows = new List<IDictionary<string, object>>();

                                            do
                                            {
                                                if (taskException != null)
                                                {
                                                    throw taskException;
                                                }

                                                //Cancel current task if cancel requested (eg: when system getting shutdown)
                                                if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                                {
                                                    jobCancelToken.ThrowIfCancellationRequested();
                                                    return;
                                                }

                                                //check sync status
                                                if (syncStatus != 1)
                                                {
                                                    return;
                                                }

                                                try
                                                {
                                                    pgRows.AddMultiple(await GetPGRecordsByFilterAsync(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));

                                                    //  pgRows.AddMultiple(await GetRecordsFromSourceTableByFilterAsync(connectorConfig, iCurrentPage, iPageSize, QueryFetchType.Insert_Only));
                                                    if (pgRows != null && pgRows.Count() > 0)
                                                    {
                                                        //int iLastPageNo = iCurrentPage;
                                                        var dedupRows = new List<IDictionary<string, object>>(pgRows);
                                                        taskLst.Add(Task.Run(async () =>
                                                        {
                                                            try
                                                            {
                                                                if (cancelToken.IsCancellationRequested)
                                                                {
                                                                    cancelToken.Token.ThrowIfCancellationRequested();
                                                                    return;
                                                                }

                                                                //wait for thread
                                                                await semaphoreSlim.WaitAsync(cancelToken.Token);

                                                                //remove columns which don't exist in dest. table
                                                                if (dedupRows.ElementAt(0).Keys.Except(fetchDestColumns).Count() > 0)
                                                                {
                                                                    var properties = dedupRows.ElementAt(0).Keys.Except(fetchDestColumns).ToList();
                                                                    dedupRows = dedupRows.Select(dic => dic.Where(kv => properties.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList<IDictionary<string, object>>();
                                                                }

                                                                // if (connectorConfig.dedup_type == DedupType.Simulate_and_Verify || connectorConfig.dedup_type == DedupType.Safe_Mode)
                                                                //{
                                                                WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert started");
                                                                using (PGQueryStatus queryStatus = await PGBulkInsertAsync(dedupRows, connectorConfig))
                                                                {
                                                                    WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert ended");
                                                                    WriteProcessLogToConsole(connectorConfig.connectorName, "PGBulkInsert count : " + (queryStatus == null ? 0 : queryStatus.inserted_rows));
                                                                    if (queryStatus != null && (queryStatus.inserted_rows > 0 || queryStatus.updated_rows > 0))
                                                                    {
                                                                        //update sync info
                                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info starts");
                                                                        syncStatus = UpdateSyncInfo(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, status: -1, count: (queryStatus.inserted_rows > 0 ? queryStatus.inserted_rows : -1), sync_updated_count: (queryStatus.updated_rows > 0 ? queryStatus.updated_rows : -1));
                                                                        WriteProcessLogToConsole(connectorConfig.connectorName, "Update sync info ended");
                                                                    }
                                                                }
                                                                //}
                                                            }
                                                            catch (Exception taskExp)
                                                            {
                                                                taskException = taskExp;
                                                            }
                                                            finally
                                                            {
                                                                semaphoreSlim.Release();
                                                            }
                                                        }, cancelToken.Token));
                                                    }
                                                }
                                                catch (Exception outerExp)
                                                {
                                                    if (outerExp.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")
                                                        || (outerExp.InnerException != null && outerExp.InnerException.Message.Contains("Out of the range of DateTime (year must be between 1 and 9999)")))
                                                    {
                                                        int offSet = iCurrentPage == 1 ? 0 : ((iCurrentPage + 1) * iPageSize);
                                                        string syncLog = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz} [{Microsoft.Extensions.Logging.LogLevel.Error}]: Failed to copy records from {offSet} to {offSet + iPageSize} due to {outerExp.Message} {Environment.NewLine}, continuing with remaining records {Environment.NewLine}";
                                                        syncStatus = UpdateSyncErrorLog(id: (int)connectorConfig.connectorId, ccid: connectorConfig.ccid, syncLog: syncLog);
                                                    }
                                                    else
                                                    {
                                                        taskException = outerExp;
                                                    }
                                                }
                                                finally
                                                {
                                                    //  pgRows.ClearMemory(iCurrentPage);
                                                }

                                                iCurrentPage++;
                                            } while (iCurrentPage <= iNoOfPages);

                                            pgRows = null;
                                        }
                                    }

                                    if (taskLst.Count() > 0)
                                    {
                                        await Task.WhenAll(taskLst.ToArray());
                                        taskLst.Clear();
                                    }
                                }
                                if (connectorConfig.dedupSourceType != SourceType.Remove_Duplicates_from_a_Single_Table || connectorConfig.dedup_type == DedupType.Simulate_and_Verify)
                                {
                                    //Cancel current task if cancel requested (eg: when system getting shutdown)
                                    if (jobCancelToken != null && jobCancelToken.ShutdownToken.IsCancellationRequested)
                                    {
                                        jobCancelToken.ThrowIfCancellationRequested();
                                        return;
                                    }
                                    if (taskException != null)
                                    {
                                        throw taskException;
                                    }
                                    //check sync status
                                    if (syncStatus != 1)
                                    {
                                        return;
                                    }
                                    await DeDupFindDuplicateBeforeDelete(connectorConfig, jobCancelToken);
                                    int count = await DeDupPGTableRecordsByFilter(connectorConfig);
                                }

                                taskLst = null;
                                cancelToken = null;
                            }
                        }
                    }
                }

                #endregion

                WriteProcessLogToConsole(connectorConfig.connectorName, "The dedup sync completed");
            }
            catch (Exception ex)
            {
                WriteProcessLogToConsole($"{ccid}:{connectorId}", ex);
                throw;
            }
        }
        public void WriteProcessLogToConsole(string processName, string message)
        {
            Console.WriteLine($"{processName}:{message}");
        }

        public void WriteProcessLogToConsole(string processName, Exception exception)
        {
            Console.WriteLine($"{processName}:{exception}");
        }

        public int ComputeAvgPageSize<T>(T data) where T : class
        {
            int pageSize = ConfigVars.Instance.DEDUP_PAGE_SIZE;
            try
            {
                if (data is PGTableDataSize)
                {
                    //Check whether sending data size is greater than configured size(MB) or not
                    //If not then post data else post data with less size
                    var tableDataSize = (PGTableDataSize)(object)data;
                    if (tableDataSize.row_data_size > 0)
                    {
                        pageSize = Convert.ToInt32(Math.Floor(ConfigVars.Instance.pgMaxQuerySizeInMB * 1024f * 1024f / tableDataSize.row_data_size));
                        if (tableDataSize.total_rows > 0 && pageSize > tableDataSize.total_rows)
                        {
                            pageSize = (int)tableDataSize.total_rows;
                        }
                    }
                }
                else if (data is IList<IDictionary<string, object>>)
                {
                    //Check whether sending data size is greater than configured size(MB) or not
                    //If not then post data else post data with less size
                    var averageRecordSize = ASCIIEncoding.ASCII.GetByteCount(JsonConvert.SerializeObject((IList<IDictionary<string, object>>)data)) / pageSize;
                    if (averageRecordSize > 0 && averageRecordSize < Convert.ToInt32(Math.Floor(ConfigVars.Instance.pgMaxQuerySizeInMB * 1024f * 1024f)))
                    {
                        pageSize = Convert.ToInt32(Math.Floor((ConfigVars.Instance.pgMaxQuerySizeInMB * 1024f * 1024f) / averageRecordSize));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }

            return pageSize;
        }

        /// <summary>
        /// Method: PGBulkInsertToGoldenTableAsync
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        public async Task<PGQueryStatus> PGBulkInsertToGoldenTableAsync(List<IDictionary<string, object>> items, ConnectorConfig connectorConfig)
        {
            PGQueryStatus queryStatus = new PGQueryStatus();
            if (connectorConfig == null || (connectorConfig != null && connectorConfig.sourceObjectFields != null
                && connectorConfig.sourceObjectFields.Count() == 0))
            {
                return queryStatus;
            }

            //Remove special chars in table name
            if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table)
                connectorConfig.dbConfig_compare.object_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.object_name);
            else
                connectorConfig.dbConfig_compare.new_table_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);
            var primaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey == true).Select(c => c.name).ToList();
            var nonPrimaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey != true).Select(c => c.name).ToList();

            //Create new connection
            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WITH temp AS  (");
                sb.Append("INSERT INTO ");
                if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table)
                    sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.object_name));
                else
                    sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.new_table_name));
                sb.Append(string.Format("({0}) VALUES ", string.Join(",", items.ElementAt(0).Keys.Select(x => "\"" + x + "\"").ToArray())));
                sb.Append(string.Join(",", items.Select(c => string.Format(" ({0}) ", string.Join(", ", c.Values.Select(v => (v == null ? "NULL" : "'" + v.ToString().Replace("'", "''") + "'")).ToArray()))).ToArray()));

                //if (primaryKeys.Count() > 0 && nonPrimaryKeys.Count() > 0)
                //{
                //    //do update if conflict with primary key
                //    sb.Append(" ON CONFLICT (" + string.Join(",", primaryKeys.Select(c => "\"" + c + "\"").ToArray()) + ")");
                //    sb.Append(string.Format(" DO UPDATE SET {0}", string.Join(",", nonPrimaryKeys.Select(x => "\"" + x + "\"=EXCLUDED.\"" + x + "\"").ToArray())));
                //}
                sb.Append(" RETURNING xmax) SELECT COUNT(*) AS total_rows,SUM(CASE WHEN xmax = 0 THEN 1 ELSE 0 END) AS inserted_rows,SUM(CASE WHEN xmax::text::int > 0 THEN 1 ELSE 0 END) AS updated_rows FROM temp;");

                //Excute insert script
                //Console.WriteLine(string.Join(", ", items.SelectMany(k => k.Keys).ToList()));
                //Console.WriteLine(string.Join(", ", items.SelectMany(k => k.Values).ToList()));
                //Console.WriteLine("PGBulkInsert Query :{0}", sb.ToString());
                items.ClearMemory();
                items = null;
                primaryKeys.Clear();
                primaryKeys = null;
                nonPrimaryKeys.Clear();
                nonPrimaryKeys = null;

                queryStatus = await connectionFactory.DbConnection.QueryFirstOrDefaultAsync<PGQueryStatus>(sb.ToString());
                sb.Clear();
                sb = null;
            }

            return queryStatus;
        }

        /// <summary>
        /// Method: PGBulkInsertToGoldenTable
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        public PGQueryStatus PGBulkInsertToGoldenTable(List<IDictionary<string, object>> items, ConnectorConfig connectorConfig)
        {
            PGQueryStatus queryStatus = new PGQueryStatus();
            if (connectorConfig == null || (connectorConfig != null && connectorConfig.sourceObjectFields != null
                && connectorConfig.sourceObjectFields.Count() == 0))
            {
                return queryStatus;
            }

            //Remove special chars in table name
            if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table)
                connectorConfig.dbConfig_compare.object_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.object_name);
            else
                connectorConfig.dbConfig_compare.new_table_name = Utilities.RemoveSpecialChars(connectorConfig.dbConfig_compare.new_table_name);
            var primaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey == true).Select(c => c.name).ToList();
            var nonPrimaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey != true).Select(c => c.name).ToList();

            //Create new connection
            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.dbConfig_compare.syncDefaultDatabaseUrl))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WITH temp AS  (");
                sb.Append("INSERT INTO ");
                if (connectorConfig.dbConfig_compare.table_type != SelectedTableType.Create_New_Table)
                    sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.object_name));
                else
                    sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.dbConfig_compare.db_schema, connectorConfig.dbConfig_compare.new_table_name));
                sb.Append(string.Format("({0}) VALUES ", string.Join(",", items.ElementAt(0).Keys.Select(x => "\"" + x + "\"").ToArray())));
                sb.Append(string.Join(",", items.Select(c => string.Format(" ({0}) ", string.Join(", ", c.Values.Select(v => (v == null ? "NULL" : "'" + v.ToString().Replace("'", "''") + "'")).ToArray()))).ToArray()));

                //if (primaryKeys.Count() > 0 && nonPrimaryKeys.Count() > 0)
                //{
                //    //do update if conflict with primary key
                //    sb.Append(" ON CONFLICT (" + string.Join(",", primaryKeys.Select(c => "\"" + c + "\"").ToArray()) + ")");
                //    sb.Append(string.Format(" DO UPDATE SET {0}", string.Join(",", nonPrimaryKeys.Select(x => "\"" + x + "\"=EXCLUDED.\"" + x + "\"").ToArray())));
                //}
                sb.Append(" RETURNING xmax) SELECT COUNT(*) AS total_rows,SUM(CASE WHEN xmax = 0 THEN 1 ELSE 0 END) AS inserted_rows,SUM(CASE WHEN xmax::text::int > 0 THEN 1 ELSE 0 END) AS updated_rows FROM temp;");

                //Excute insert script
                //Console.WriteLine(string.Join(", ", items.SelectMany(k => k.Keys).ToList()));
                //Console.WriteLine(string.Join(", ", items.SelectMany(k => k.Values).ToList()));
                //Console.WriteLine("PGBulkInsert Query :{0}", sb.ToString());
                items.ClearMemory();
                items = null;
                primaryKeys.Clear();
                primaryKeys = null;
                nonPrimaryKeys.Clear();
                nonPrimaryKeys = null;

                queryStatus = connectionFactory.DbConnection.QueryFirstOrDefault<PGQueryStatus>(sb.ToString());
                sb.Clear();
                sb = null;
            }

            return queryStatus;
        }

        /// <summary>
        /// Method: PGBulkInsertAsync
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connectorConfig"></param>
        /// <returns>true/false</returns>
        public async Task<PGQueryStatus> PGBulkInsertAsync(List<IDictionary<string, object>> items, ConnectorConfig connectorConfig)
        {
            PGQueryStatus queryStatus = new PGQueryStatus();
            if (connectorConfig == null || (connectorConfig != null && connectorConfig.sourceObjectFields != null
                && connectorConfig.sourceObjectFields.Count() == 0))
            {
                return queryStatus;
            }

            //Remove special chars in table name
            connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
            var primaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey == true).Select(c => c.name).ToList();
            var nonPrimaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey != true).Select(c => c.name).ToList();

            //Create new connection
            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WITH temp AS  (");
                sb.Append("INSERT INTO ");
                sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                sb.Append(string.Format("({0}) VALUES ", string.Join(",", items.ElementAt(0).Keys.Select(x => "\"" + x + "\"").ToArray())));
                sb.Append(string.Join(",", items.Select(c => string.Format(" ({0}) ", string.Join(", ", c.Values.Select(v => (v == null ? "NULL" : "'" + v.ToString().Replace("'", "''") + "'")).ToArray()))).ToArray()));

                //if (primaryKeys.Count() > 0 && nonPrimaryKeys.Count() > 0)
                //{
                //    //do update if conflict with primary key
                //    sb.Append(" ON CONFLICT (" + string.Join(",", primaryKeys.Select(c => "\"" + c + "\"").ToArray()) + ")");
                //    sb.Append(string.Format(" DO UPDATE SET {0}", string.Join(",", nonPrimaryKeys.Select(x => "\"" + x + "\"=EXCLUDED.\"" + x + "\"").ToArray())));
                //}
                sb.Append(" RETURNING xmax) SELECT COUNT(*) AS total_rows,SUM(CASE WHEN xmax = 0 THEN 1 ELSE 0 END) AS inserted_rows,SUM(CASE WHEN xmax::text::int > 0 THEN 1 ELSE 0 END) AS updated_rows FROM temp;");
                items.ClearMemory();
                items = null;
                primaryKeys.Clear();
                primaryKeys = null;
                nonPrimaryKeys.Clear();
                nonPrimaryKeys = null;

                //Excute insert script
                //Console.WriteLine("PGBulkInsert Query :{0}", sb.ToString());
                queryStatus = await connectionFactory.DbConnection.QueryFirstOrDefaultAsync<PGQueryStatus>(sb.ToString());
                sb.Clear();
                sb = null;
            }
            return queryStatus;
        }

        /// <summary>
        /// Method: PGBulkInsert
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connectorConfig"></param>
        /// <returns>true/false</returns>
        public PGQueryStatus PGBulkInsert(List<IDictionary<string, object>> items, ConnectorConfig connectorConfig)
        {
            PGQueryStatus queryStatus = new PGQueryStatus();
            if (connectorConfig == null || (connectorConfig != null && connectorConfig.sourceObjectFields != null
                && connectorConfig.sourceObjectFields.Count() == 0))
            {
                return queryStatus;
            }

            //Remove special chars in table name
            connectorConfig.destObjectName = Utilities.RemoveSpecialChars(connectorConfig.destObjectName);
            var primaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey == true).Select(c => c.name).ToList();
            var nonPrimaryKeys = connectorConfig.syncDestObjectColumns.Where(c => c.isPrimaryKey != true).Select(c => c.name).ToList();

            //Create new connection
            using (ConnectionFactory connectionFactory = new ConnectionFactory(connectorConfig.destDBConfig.syncDefaultDatabaseUrl))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WITH temp AS  (");
                sb.Append("INSERT INTO ");
                sb.Append(string.Format("\"{0}\".\"{1}\"", connectorConfig.destDBSchema, connectorConfig.destObjectName));
                sb.Append(string.Format("({0}) VALUES ", string.Join(",", items.ElementAt(0).Keys.Select(x => "\"" + x + "\"").ToArray())));
                sb.Append(string.Join(",", items.Select(c => string.Format(" ({0}) ", string.Join(", ", c.Values.Select(v => (v == null ? "NULL" : "'" + v.ToString().Replace("'", "''") + "'")).ToArray()))).ToArray()));

                //if (primaryKeys.Count() > 0 && nonPrimaryKeys.Count() > 0)
                //{
                //    //do update if conflict with primary key
                //    sb.Append(" ON CONFLICT (" + string.Join(",", primaryKeys.Select(c => "\"" + c + "\"").ToArray()) + ")");
                //    sb.Append(string.Format(" DO UPDATE SET {0}", string.Join(",", nonPrimaryKeys.Select(x => "\"" + x + "\"=EXCLUDED.\"" + x + "\"").ToArray())));
                //}
                sb.Append(" RETURNING xmax) SELECT COUNT(*) AS total_rows,SUM(CASE WHEN xmax = 0 THEN 1 ELSE 0 END) AS inserted_rows,SUM(CASE WHEN xmax::text::int > 0 THEN 1 ELSE 0 END) AS updated_rows FROM temp;");
                items.ClearMemory();
                items = null;
                primaryKeys.Clear();
                primaryKeys = null;
                nonPrimaryKeys.Clear();
                nonPrimaryKeys = null;

                //Excute insert script
                //Console.WriteLine("PGBulkInsert Query :{0}", sb.ToString());
                queryStatus = connectionFactory.DbConnection.QueryFirstOrDefault<PGQueryStatus>(sb.ToString());
                sb.Clear();
                sb = null;
            }

            return queryStatus;
        }


        /// <summary>
        /// Method: GetPGDatabaseTablesListByChar
        /// Description: It is used to get all database tables baseed on user entered text 
        /// </summary>
        /// <param name="databaseConfig"></param>
        /// <param name="tableName"></param>
        /// <param name="dbSchema"></param>
        /// <returns></returns>
        public static List<string> GetPGDatabaseTablesListByChar(DatabaseConfig databaseConfig)
        {
            List<string> databaseTables = null;
            try
            {
                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(databaseConfig.syncDefaultDatabaseUrl))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT table_name as tableName FROM information_schema.tables");
                    sb.Append(" WHERE table_schema = '" + databaseConfig.db_schema + "' and table_name like '" + databaseConfig.object_name + "%' order by table_name;");
                    //Excute create table script
                    databaseTables = connectionFactory.DbConnection.Query<string>(sb.ToString()).ToList();

                    sb.Clear();
                    sb.Length = 0;
                    sb = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }

            if (databaseTables == null || (databaseTables != null && databaseTables.Count() == 0))
            {
                databaseTables = new List<string>();
            }

            return databaseTables;
        }

        /// <summary>
        /// Method: GetPGDatabaseTables
        /// Description: It is used to get all database tables from configured dbsetting db url.
        /// </summary>
        /// <param name="databaseConfig"></param>
        /// <param name="tableName"></param>
        /// <param name="dbSchema"></param>
        /// <returns></returns>
        public static List<DatabaseTables> GetPGDatabaseTables(DatabaseConfig databaseConfig)
        {
            List<DatabaseTables> databaseTables = null;
            try
            {
                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(databaseConfig.syncDefaultDatabaseUrl))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT n.nspname as dbSchema,c.relname As tableName,uuid_in(md5(n.nspname::text||c.relname::text)::cstring)::text as customerKey FROM pg_class c");
                    sb.Append(" LEFT JOIN pg_namespace n ON n.oid = c.relnamespace");
                    sb.Append(" WHERE c.relkind = 'r'::char AND n.nspname<>'information_schema' AND n.nspname<>'pg_catalog' AND");
                    sb.Append(" c.relname<>'__EFMigrationsHistory'");
                    sb.Append(" GROUP BY c.relname,n.nspname");
                    sb.Append(" ORDER BY n.nspname,c.relname");

                    //Excute create table script
                    databaseTables = connectionFactory.DbConnection.Query<DatabaseTables>(sb.ToString()).ToList();

                    sb.Clear();
                    sb.Length = 0;
                    sb = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }

            if (databaseTables == null || (databaseTables != null && databaseTables.Count() == 0))
            {
                databaseTables = new List<DatabaseTables>();
                databaseTables.Add(new DatabaseTables() { dbSchema = Constants.POSTGRES_DEFAULT_SCHEMA, tableName = string.Empty, customerKey = string.Empty });
            }

            return databaseTables;
        }

        /// <summary>
        /// Method: GetPGDatabaseTables
        /// Description: It is used to get all database tables from configured dbsetting db url.
        /// </summary>
        /// <param name="databaseConfig"></param>
        /// <param name="tableName"></param>
        /// <param name="dbSchema"></param>
        /// <returns></returns>
        public static List<DatabaseTables> GetALLPGDatabaseTables(DatabaseConfig databaseConfig)
        {
            List<DatabaseTables> databaseTables = null;
            try
            {
                //Create new connection
                using (ConnectionFactory connectionFactory = new ConnectionFactory(databaseConfig.syncDefaultDatabaseUrl))
                {
                    StringBuilder sb = new StringBuilder();
                    //sb.Append("SELECT n.nspname as dbSchema,c.relname As tableName,uuid_in(md5(n.nspname::text||c.relname::text)::cstring)::text as customerKey FROM pg_namespace n");
                    //sb.Append(" LEFT JOIN pg_class c  ON n.oid =c.relnamespace");
                    //sb.Append(" WHERE (c.relkind = 'r'::char OR c.relkind is null) AND n.nspname<>'information_schema' AND n.nspname<>'pg_catalog' AND");
                    //sb.Append(" pg_catalog.pg_get_userbyid(n.nspowner) = 'postgres'");
                    //sb.Append(" GROUP BY c.relname,n.nspname");
                    //sb.Append(" ORDER BY n.nspname,c.relname");

                    sb.Append("select s.schema_name as dbSchema, t.table_name as tableName,");
                    sb.Append("uuid_in(md5(s.schema_name ::text || t.table_name::text)::cstring)::text as customerKey");
                    sb.Append(" from information_schema.schemata s  left");
                    sb.Append(" join information_schema.tables t");
                    sb.Append(" on t.table_schema = s.schema_name");
                    sb.Append(" where s.schema_name not in ('pg_catalog', 'information_schema')");
                    sb.Append(" and s.schema_name not like 'pg_t%'");
                    sb.Append(" GROUP BY s.schema_name,t.table_name");
                    sb.Append(" ORDER BY s.schema_name");

                    //Excute create table script
                    databaseTables = connectionFactory.DbConnection.Query<DatabaseTables>(sb.ToString()).ToList();

                    sb.Clear();
                    sb.Length = 0;
                    sb = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }

            if (databaseTables == null || (databaseTables != null && databaseTables.Count() == 0))
            {
                databaseTables = new List<DatabaseTables>();
                databaseTables.Add(new DatabaseTables() { dbSchema = Constants.POSTGRES_DEFAULT_SCHEMA, tableName = string.Empty, customerKey = string.Empty });
            }

            return databaseTables;
        }

        /// <summary>
        /// Method: GetPGDatabaseTableColumns
        /// Description: It is used to get database table's columns from configured dbsetting db url by table name and schema.
        /// </summary>
        /// <param name="databaseConfig"></param>
        /// <param name="tableName"></param>
        /// <param name="dbSchema"></param>
        /// <returns></returns>
        public static List<DatabaseTableColumns> GetPGDatabaseTableColumns(DatabaseConfig databaseConfig, string tableName, string dbSchema)
        {
            List<DatabaseTableColumns> databaseTableColumns = null;
            try
            {
                if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(dbSchema))
                {
                    //Create new connection to sync database
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(databaseConfig.syncDefaultDatabaseUrl))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("SELECT n.nspname as dbSchema,c.relname As tableName,f.attname AS name,f.attnotnull AS isRequired, pg_catalog.format_type(f.atttypid, f.atttypmod) AS fieldType,CASE WHEN p.contype = 'p' THEN 1 ELSE 0 END AS isPrimaryKey,CASE WHEN p.contype = 'u' THEN 1 ELSE 0 END AS isUniqueKey,CASE WHEN f.atthasdef = 't' THEN pg_get_expr(d.adbin, d.adrelid) END AS defaultValue FROM pg_attribute f");
                        sb.Append(" JOIN pg_class c ON c.oid = f.attrelid JOIN pg_type t ON t.oid = f.atttypid");
                        sb.Append(" LEFT JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = f.attnum");
                        sb.Append(" LEFT JOIN pg_namespace n ON n.oid = c.relnamespace");
                        //sb.Append(" LEFT JOIN pg_constraint p ON p.conrelid = c.oid AND f.attnum = ANY(p.conkey)");
                        sb.Append(" LEFT JOIN (select * from pg_constraint pc where pc.contype in ('p','u')) p ON p.conrelid = c.oid AND f.attnum = ANY(p.conkey)");
                        //sb.Append(" LEFT JOIN pg_class AS g ON p.confrelid = g.oid");
                        sb.Append(" WHERE c.relkind = 'r'::char AND f.attnum > 0 AND n.nspname <> 'information_schema' AND n.nspname <> 'pg_catalog' AND c.relname <> '__EFMigrationsHistory'");
                        sb.Append(" AND n.nspname = '" + dbSchema + "' AND c.relname='" + tableName + "'");
                        //sb.Append(" AND g.relname is null"); commented on 3rd septemper foregin key columns missing
                        //sb.Append(" AND uuid_in(md5(n.nspname::text||c.relname::text)::cstring)::text = '" + sourceName + "'");
                        //sb.Append(" GROUP By f.attname,f.attnotnull,fieldType,isPrimaryKey,isUniqueKey,defaultValue,c.relname,n.nspname Order By f.attname asc");// n.nspname,c.relname,f.attname");

                        //Excute create script
                        databaseTableColumns = connectionFactory.DbConnection.Query<DatabaseTableColumns>(sb.ToString()).ToList();

                        sb.Clear();
                        sb.Length = 0;
                        sb = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return databaseTableColumns;
        }
        /// <summary>
        /// Method: GetPGDatabaseTableIndexedColumns
        /// Description: It is used to get database table's indexed columns from configured dbsetting db url by table name and schema.
        /// </summary>
        /// <param name="databaseConfig"></param>
        /// <param name="tableName"></param>
        /// <param name="dbSchema"></param>
        /// <returns></returns>
        public List<DatabaseTableIndexedColumns> GetPGDatabaseTableIndexedColumns(DatabaseConfig databaseConfig, string tableName, string dbSchema)
        {
            List<DatabaseTableIndexedColumns> databaseTableIndexColumns = null;
            try
            {
                if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(dbSchema))
                {
                    //Create new connection to sync database
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(databaseConfig.syncDefaultDatabaseUrl))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("SELECT t.relname as table_name, i.relname as index_name, array_to_string(ARRAY_AGG(a.attname), ', ') as column_names FROM pg_class t, pg_class i, pg_index ix, pg_attribute a, pg_namespace n");
                        sb.Append(" WHERE t.oid = ix.indrelid AND i.oid = ix.indexrelid AND a.attrelid = t.oid AND a.attnum = ANY(ix.indkey) AND n.oid = t.relnamespace AND t.relkind = 'r'");
                        sb.Append($" AND n.nspname = '{dbSchema.Trim()}' AND t.relname = '{tableName.Trim()}' GROUP BY t.relname,i.relname");

                        //Excute create script
                        databaseTableIndexColumns = connectionFactory.DbConnection.Query<DatabaseTableIndexedColumns>(sb.ToString()).ToList();

                        sb.Clear();
                        sb = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }
            return databaseTableIndexColumns;
        }

        public static bool CheckPGUserWritePermission(DatabaseConfig databaseConfig, string tableName, string dbSchema, string previlegeType)
        {
            try
            {
                if (!string.IsNullOrEmpty(databaseConfig.syncDefaultDatabaseUrl))
                {
                    //get postgres userId
                    var pgUserId = ConnectionFactory.GetUserIdFromPGConnectionUrl(databaseConfig.syncDefaultDatabaseUrl);
                    if (!string.IsNullOrEmpty(pgUserId))
                    {
                        //db schema
                        if (string.IsNullOrEmpty(dbSchema))
                        {
                            dbSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                        }

                        //Remove special chars in table name
                        tableName = Utilities.RemoveSpecialChars(tableName);

                        //Create new connection to sync database
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(databaseConfig.syncDefaultDatabaseUrl))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("SELECT table_catalog, table_schema, table_name, privilege_type");
                            sb.Append(" FROM information_schema.table_privileges");
                            sb.Append(string.Format(" WHERE  grantee='{0}' AND table_schema='{1}' AND table_name='{2}'", pgUserId, dbSchema, tableName));

                            //Excute create script
                            var databaseUserPrevileges = connectionFactory.DbConnection.Query<DatabaseUserPrevileges>(sb.ToString()).ToList();
                            sb = null;

                            if (databaseUserPrevileges != null && databaseUserPrevileges.FirstOrDefault(p => p.privilege_type == previlegeType).IsNull())
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Method: GetSyncStatus
        /// Description: It is used to get connector sync status by connector from connectors table
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="connectorId"></param>
        /// <returns>status as int</returns>
        public static int GetSyncStatus(string ccid, int connectorId)
        {
            int syncStatus = 0;
            try
            {
                if (!string.IsNullOrEmpty(ccid) && connectorId > 0)
                {
                    //Create new connection
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(ConfigVars.Instance.connectionString))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(string.Format("SELECT c.\"sync_status\" FROM \"{0}\".\"Connectors\" as c", Constants.ADDON_DB_DEFAULT_SCHEMA));
                        sb.Append(string.Format(" WHERE c.\"ccid\"='{0}' AND c.\"connector_id\"={1};", ccid, connectorId));
                        Console.WriteLine(sb.ToString());

                        //Excute select scripts and sync status
                        var syncStaus = connectionFactory.DbConnection.ExecuteScalar<int?>(sb.ToString());

                        //get sync status
                        if (syncStaus != null && syncStaus.HasValue)
                        {
                            syncStatus = (int)syncStaus;
                        }
                        sb = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                throw;
            }

            return syncStatus;
        }

        /// <summary>
        /// Method: UpdateSyncInfo
        /// Description: It is used to update connector sync info by connector on connectors table
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ccid"></param>
        /// <param name="status"></param>
        /// <param name="count"></param>
        /// <param name="sync_updated_count"></param>
        /// <param name="jobid"></param>
        /// <param name="connectorLogs"></param>
        /// <param name="totaluniquecount"></param>
        /// <param name="deduped_count"></param>
        /// <param name="total_records_count"></param>
        /// <returns></returns>
        public static int UpdateSyncInfo(int id, string ccid, int status = -1, int count = -1, int sync_updated_count = -1, string jobid = "", ConnectorLogs connectorLogs = default(ConnectorLogs), int totaluniquecount = -1, int deduped_count = -1, int total_records_count = -1)
        {
            int syncStatus = 0;

            try
            {
                if (id > 0 && !string.IsNullOrEmpty(ccid))
                {
                    bool columnToBeUpdated = false;
                    StringBuilder sb = new StringBuilder();
                    sb.Append(string.Format("UPDATE \"{0}\".\"Connectors\" as c SET ", Constants.ADDON_DB_DEFAULT_SCHEMA));
                    //Update sync_count and sync_status
                    if (status >= 0 && count >= 0 && sync_updated_count >= 0 && deduped_count >= 0 && !string.IsNullOrEmpty(jobid))
                    {
                        sb.Append(" \"last_sync_at\"= case when coalesce(c.\"sync_status\", 0)=2 then \"sync_started_at\" else \"last_sync_at\" end,");
                        sb.Append(string.Format("  \"sync_status\"={0},\"job_id\"='{1}', \"last_sync_status\"=\"sync_status\", \"sync_ended_at\"=\"sync_started_at\",", status, jobid));
                        sb.Append(string.Format(" \"sync_count\"=" + (count > 0 ? "c.\"sync_count\"+{0}" : "{0}") + ",\"sync_updated_count\"=" + (sync_updated_count > 0 ? "c.\"sync_updated_count\"+{1}" : "{1}") + ",\"deduped_count\"=" + (deduped_count > 0 ? "c.\"deduped_count\"+{2}" : "{2}") + ", \"total_records_count\"= 0", count, sync_updated_count, deduped_count));
                        columnToBeUpdated = true;
                    }
                    else if (status >= 0 && count >= 0 && sync_updated_count >= 0)
                    {
                        sb.Append(string.Format(" \"sync_status\"={0}, \"sync_count\"=" + (count > 0 ? "c.\"sync_count\"+{1}" : "{1}") + ", \"sync_updated_count\"=" + (sync_updated_count > 0 ? "c.\"sync_updated_count\"+{2}" : "{2}") + ", \"sync_ended_at\"='{3}'", status, count, sync_updated_count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (status >= 0 && count >= 0)
                    {
                        sb.Append(string.Format(" \"sync_status\"={0}, \"sync_count\"=" + (count > 0 ? "c.\"sync_count\"+{1}" : "{1}") + ", \"sync_ended_at\"='{2}'", status, count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (status >= 0 && sync_updated_count >= 0)
                    {
                        sb.Append(string.Format(" \"sync_status\"={0}, \"sync_updated_count\"=" + (sync_updated_count > 0 ? "c.\"sync_updated_count\"+{1}" : "{1}") + ", \"sync_ended_at\"='{2}'", status, sync_updated_count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (totaluniquecount >= 0)
                    {
                        sb.Append(string.Format(" \"unique_records_count\"={0}, \"sync_ended_at\"='{1}'", totaluniquecount, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (count >= 0)
                    {
                        sb.Append(string.Format(" \"sync_count\"=" + (count > 0 ? "c.\"sync_count\"+{0}" : "{0}") + ", \"sync_ended_at\"='{1}'", count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (sync_updated_count >= 0)
                    {
                        sb.Append(string.Format(" \"sync_updated_count\"=" + (sync_updated_count > 0 ? "c.\"sync_updated_count\"+{0}" : "{0}") + ", \"sync_ended_at\"='{1}'", sync_updated_count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (deduped_count >= 0)
                    {
                        sb.Append(string.Format(" \"deduped_count\"=" + (deduped_count > 0 ? "c.\"deduped_count\"+{0}" : "{0}") + ", \"sync_ended_at\"='{1}'", deduped_count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (total_records_count >= 0)
                    {
                        sb.Append(string.Format(" \"total_records_count\"=" + (total_records_count > 0 ? "c.\"total_records_count\"+{0}" : "{0}") + ", \"sync_ended_at\"='{1}'", total_records_count, DateTime.UtcNow));
                        columnToBeUpdated = true;
                    }
                    else if (!string.IsNullOrEmpty(jobid))
                    {
                        sb.Append(string.Format(" \"job_id\"='{0}'", jobid));
                        columnToBeUpdated = true;
                    }
                    else if (status >= 0)
                    {
                        sb.Append(string.Format(" \"sync_status\"={0}", status));
                        columnToBeUpdated = true;
                    }

                    if (connectorLogs != null)
                    {
                        if (status == 1)
                        {
                            sb.Append($", \"sync_log_json\"=case when (coalesce(c.\"sync_status\", 0) <> 10 and coalesce(c.\"sync_status\", 0) <> 9) then '{JsonConvert.SerializeObject(connectorLogs.sync_logs)}'  else \"sync_log_json\" end, \"sync_started_at\"=case when (coalesce(c.\"sync_status\", 0) <> 10 and coalesce(c.\"sync_status\", 0) <> 9) then '{connectorLogs.sync_started_at}' else \"sync_started_at\" end ");
                            columnToBeUpdated = true;
                        }
                        else if (status == 2 || status == 3)
                        {
                            sb.Append(string.Format(", \"sync_log_json\"='{0}', \"sync_ended_at\"='{1}'", JsonConvert.SerializeObject(connectorLogs.sync_logs), connectorLogs.sync_ended_at));
                            columnToBeUpdated = true;
                        }
                        else if (status == 0 && count == 0)
                        {
                            sb.Append(",\"sync_log_json\"=NULL, \"sync_started_at\"=NULL, \"sync_ended_at\"=NULL, \"last_sync_at\"=NULL, \"last_sync_status\"=NULL");
                            columnToBeUpdated = true;
                        }
                    }
                    else if (status == 0 && count == 0)
                    {
                        sb.Append(",\"sync_log_json\"=NULL, \"sync_started_at\"=NULL, \"sync_ended_at\"=NULL, \"last_sync_at\"=NULL, \"last_sync_status\"=NULL");
                        columnToBeUpdated = true;
                    }

                    if (columnToBeUpdated)
                    {
                        //set where conditions
                        sb.Append(string.Format(" WHERE \"ccid\"='{0}' AND \"connector_id\"={1} RETURNING \"sync_status\";", ccid, id));

                        //Create new connection to addon database
                        using (ConnectionFactory connectionFactory = new ConnectionFactory(ConfigVars.Instance.connectionString))
                        {
                            //Excute update script
                            syncStatus = connectionFactory.DbConnection.ExecuteScalar<int>(sb.ToString());
                            sb.Clear();
                            sb = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                throw;
            }

            return syncStatus;
        }

        /// <summary>
        /// Method: UpdateSyncErrorLog
        /// Description: It is used to update connector log info by connectorId on connectors table
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ccid"></param>
        /// <param name="syncLog"></param>
        public static int UpdateSyncErrorLog(int id, string ccid, string syncLog, bool increaseErrorCount = false, string checkDuplicateInExistingLog = "")
        {
            int syncStatus = 0;

            try
            {
                if (id > 0 && !string.IsNullOrEmpty(ccid))
                {
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(ConfigVars.Instance.connectionString))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(string.Format("SELECT \"sync_log_json\" FROM \"{0}\".\"Connectors\"", Constants.ADDON_DB_DEFAULT_SCHEMA));
                        sb.Append(string.Format(" WHERE \"ccid\"='{0}' AND \"connector_id\"={1};", ccid, id));
                        var sync_log_json = connectionFactory.DbConnection.ExecuteScalar<string>(sb.ToString());

                        List<string> sync_logs = null;
                        if (!string.IsNullOrEmpty(sync_log_json))
                        {
                            sync_logs = JsonConvert.DeserializeObject<List<string>>(sync_log_json);
                            //if (sync_logs.Count > 0 && !string.IsNullOrEmpty(checkDuplicateInExistingLog))
                            //{
                            //    if (sync_logs.Where(x => x.Contains(HttpUtility.UrlEncode(checkDuplicateInExistingLog))).Count() > 0)
                            //    {
                            //        Console.WriteLine("Check duplicate error log: {0}", checkDuplicateInExistingLog);
                            //        increaseErrorCount = false;
                            //        Console.WriteLine("Increasing the Error count -{0}-{1}:{2}", ccid, id, increaseErrorCount);
                            //    }
                            //}
                        }
                        if (sync_logs == null)
                        {
                            sync_logs = new List<string>();
                        }
                        sync_logs.Insert(sync_logs.Count(), HttpUtility.UrlEncode(syncLog));
                        if (sync_logs.Count() > 0)
                        {
                            sb.Clear();
                            connectionFactory.OpenConnection();
                            sb.Append(string.Format("UPDATE \"{0}\".\"Connectors\" as c SET ", Constants.ADDON_DB_DEFAULT_SCHEMA));
                            sb.Append(string.Format(" \"sync_log_json\"='{0}'", JsonConvert.SerializeObject(sync_logs)));
                            //if (increaseErrorCount)
                            //{
                            //    sb.Append(string.Format(", \"sync_error_count\"=coalesce(c.\"sync_error_count\", 0)+{0}", 1));
                            //}
                            //set where conditions
                            sb.Append(string.Format(" WHERE \"ccid\"='{0}' AND \"connector_id\"={1} RETURNING \"sync_status\";", ccid, id));

                            //Excute update script
                            syncStatus = connectionFactory.DbConnection.ExecuteScalar<int>(sb.ToString());
                            sb.Clear();
                            sb = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
            }

            return syncStatus;
        }

        /// <summary>
        /// Method: GetConnectorById
        /// Description: It is used to get connector settings by ccid and connectorId.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetConnectorById<T>(string ccid, int id) where T : class
        {
            try
            {
                if (!string.IsNullOrEmpty(ccid) && id > 0)
                {
                    //Create new connection to addon database
                    using (ConnectionFactory connectionFactory = new ConnectionFactory(ConfigVars.Instance.connectionString))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(string.Format("SELECT * FROM \"{0}\".\"Connectors\"", Constants.ADDON_DB_DEFAULT_SCHEMA));
                        sb.Append(string.Format(" WHERE \"ccid\"='{0}' AND \"connector_id\"={1};", ccid, id));
                        sb.Append(string.Format("SELECT * FROM \"{0}\".\"DeDupSettings\"", Constants.ADDON_DB_DEFAULT_SCHEMA));
                        sb.Append(string.Format(" WHERE \"ccid\"='{0}';", ccid));
                        sb.Append(string.Format("SELECT \"private_app_url\" FROM \"{0}\".\"Resources\"", Constants.ADDON_DB_DEFAULT_SCHEMA));
                        sb.Append(string.Format(" WHERE \"uuid\"='{0}';", ccid));
                        var queryResult = connectionFactory.DbConnection.QueryMultiple(sb.ToString());
                        if (queryResult != null)
                        {
                            var connector = queryResult.Read<Connectors>().FirstOrDefault();
                            if (connector != null)
                            {
                                connector.DeDupSetting = queryResult.Read<DeDupSettings>().FirstOrDefault();

                                if (typeof(T) == typeof(Connectors))
                                {
                                    return connector as T;
                                }
                                else if (typeof(T) == typeof(ConnectorConfig))
                                    return connector.ToModel<ConnectorConfig>() as T;
                            }
                        }
                        queryResult = null;
                        sb = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}", ex.Message);
                throw;
            }

            return null;
        }

        #endregion

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                //if (disposing)
                //{
                //}
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SyncRepository()
        {
            Dispose(false);
        }
    }
}
