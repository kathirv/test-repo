using Dedup.Common;
using Dedup.Models;
using Dedup.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Dedup.Repositories;

namespace Dedup.Extensions
{
    public static class ConnectorsExtensions
    {
        public static T ToModel<T>(this Connectors connector, bool isSetConfig = true) where T : class
        {
            if (connector != null)
            {
                ConnectorConfig conConfig = null;
                conConfig = new ConnectorConfig()
                {
                    ccid = connector.ccid,
                    connectorId = connector.connector_id,
                    connectorName = connector.connector_name,
                    sourceObjectName = connector.src_object_name,
                    destObjectName = connector.dest_object_name,
                    scheduleType = connector.schedule_type,
                    srcNewRecordFilter = connector.src_new_record_filter,
                    srcUpdateRecordFilter = connector.src_update_record_filter,
                    twoWaySyncPriority = connector.two_way_sync_priority,
                    syncDestination = connector.connector_type,
                    syncCount = connector.sync_count,
                    syncStatus = connector.sync_status,
                    dedup_type = connector.dedup_type,
                    jobId = connector.job_id,
                    syncStartedAt = connector.sync_started_at,
                    syncEndedAt = connector.sync_ended_at,
                    lastSyncAt = connector.last_sync_at,
                    lastSyncStatus = connector.last_sync_status,
                    dbSchema = connector.src_schema,
                    dataSource = connector.sync_src,
                    destDBSchema = connector.dest_schema,
                    customScheduleInMinutes = connector.custom_schedule_in_minutes,
                    dedupSourceType = connector.dedup_source_type,

                    dedup_method = connector.dedup_method,
                    review_before_delete = connector.review_before_delete,
                    backup_before_delete = connector.backup_before_delete,
                    simulation_count = connector.simulation_count,

                    fuzzy_ratio = connector.fuzzy_ratio,
                    unique_records_count = connector.unique_records_count
                };
                //if (!string.IsNullOrEmpty(connector.src_object_fields_json))
                //{
                //    conConfig.sourceObjectFields = JsonConvert.DeserializeObject<List<string>>(connector.src_object_fields_json);
                //}
               

                //if (!string.IsNullOrEmpty(connector.sync_log_json))
                //{
                //    conConfig.connectorLogs = new ConnectorLogs()
                //    {
                //        sync_started_at = conConfig.syncStartedAt,
                //        sync_ended_at = conConfig.syncEndedAt,
                //        sync_count = conConfig.syncCount,
                //        sync_logs= JsonConvert.DeserializeObject<List<string>>(connector.sync_log_json)
                //    };
                //}
                
                if (conConfig != null)
                {
                    //conConfig.child_record_count = SyncRepository.GetChildRecordsCount(connector.ccid, connector.connector_id);

                    if (string.IsNullOrEmpty(conConfig.dbSchema))
                    {
                        if ((conConfig.syncDestination == ConnectorType.Heroku_Postgres || conConfig.dataSource == DataSource.Heroku_Postgres
                            || conConfig.syncDestination == ConnectorType.Azure_Postgres || conConfig.dataSource == DataSource.Azure_Postgres
                            || conConfig.syncDestination == ConnectorType.AWS_Postgres || conConfig.dataSource == DataSource.AWS_Postgres))
                        {
                            conConfig.dbSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                        }
                        else if (conConfig.syncDestination == ConnectorType.Azure_SQL)
                        {
                            conConfig.dbSchema = Constants.MSSQL_DEFAULT_SCHEMA;
                        }
                    }

                    if (isSetConfig)
                    {
                        if (conConfig.dataSource == DataSource.Heroku_Postgres
                            || conConfig.dataSource == DataSource.Azure_Postgres
                            || conConfig.dataSource == DataSource.AWS_Postgres
                            || conConfig.dataSource == DataSource.Azure_SQL)
                        {
                            DatabaseType databaseType;
                            if (conConfig.dataSource == DataSource.Azure_SQL)
                            {
                                databaseType = DatabaseType.Azure_SQL;
                            }
                            else if (conConfig.dataSource == DataSource.Azure_Postgres)
                            {
                                databaseType = DatabaseType.Azure_Postgres;
                            }
                            else if (conConfig.dataSource == DataSource.AWS_Postgres)
                            {
                                databaseType = DatabaseType.AWS_Postgres;
                            }
                            else
                            {
                                databaseType = DatabaseType.Heroku_Postgres;
                            }
                            if (!string.IsNullOrEmpty(connector.compare_config_json) && (connector.dedup_source_type == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || connector.dedup_source_type == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B))
                            {
                                if (connector.compare_config_json.StartsWith("["))
                                {
                                    conConfig.multipleDBConfigs.AddRange(JsonConvert.DeserializeObject<List<DatabaseConfig>>(connector.compare_config_json));
                                    //conConfig.dbConfig_compare = JsonConvert.DeserializeObject<DatabaseConfig>(connector.compare_config_json);
                                }
                                else
                                {
                                    conConfig.dbConfig_compare = JsonConvert.DeserializeObject<DatabaseConfig>(connector.compare_config_json);
                                    conConfig.multipleDBConfigs.Add(conConfig.dbConfig_compare);
                                }
                            }
                            if (!string.IsNullOrEmpty(connector.src_config_json))
                            {
                                conConfig.dbConfig = JsonConvert.DeserializeObject<DatabaseConfig>(connector.src_config_json);
                                if (conConfig.dbConfig.databaseType == DatabaseType.None)
                                {
                                    conConfig.dbConfig.databaseType = databaseType;
                                }
                                conConfig.dbConfig.db_schema = conConfig.dbSchema;
                                conConfig.dbConfig.object_name = conConfig.sourceObjectName;
                                conConfig.multipleDBConfigs.Add(conConfig.dbConfig);
                            }
                            else if (connector.DeDupSetting != null && !string.IsNullOrEmpty(connector.DeDupSetting.database_config_json))
                            {
                                conConfig.dbConfig = connector.DeDupSetting.ToModel<DatabaseConfig>(databaseType: databaseType);
                                conConfig.dbConfig.db_schema = conConfig.dbSchema;
                                conConfig.dbConfig.object_name = conConfig.sourceObjectName;
                                conConfig.multipleDBConfigs.Add(conConfig.dbConfig);
                            }
                           
                            if (!string.IsNullOrEmpty(connector.compare_object_fields))
                            {
                                conConfig.compareObjectFieldsMapping = JsonConvert.DeserializeObject<List<string>>(connector.compare_object_fields);
                                if (connector.dedup_source_type == SourceType.Remove_Duplicates_from_a_Single_Table || conConfig.compareObjectFieldsMapping != null)
                                {
                                    conConfig.sourceObjectFields = conConfig.compareObjectFieldsMapping;
                                    conConfig.dbConfig_compare.compareObjectFields = conConfig.compareObjectFieldsMapping;
                                }
                            }
                        }


                        if (conConfig.syncDestination == ConnectorType.Heroku_Postgres
                            || conConfig.syncDestination == ConnectorType.Azure_Postgres
                            || conConfig.syncDestination == ConnectorType.AWS_Postgres
                            || conConfig.syncDestination == ConnectorType.Azure_SQL)
                        {
                            DatabaseType databaseType;
                            if (conConfig.syncDestination == ConnectorType.Azure_SQL)
                            {
                                databaseType = DatabaseType.Azure_SQL;
                            }
                            else if (conConfig.syncDestination == ConnectorType.Azure_Postgres)
                            {
                                databaseType = DatabaseType.Azure_Postgres;
                            }
                            else if (conConfig.syncDestination == ConnectorType.AWS_Postgres)
                            {
                                databaseType = DatabaseType.AWS_Postgres;
                            }
                            else
                            {
                                databaseType = DatabaseType.Heroku_Postgres;
                            }
                            if (!string.IsNullOrEmpty(connector.dest_config_json))
                            {
                                conConfig.destDBConfig = JsonConvert.DeserializeObject<DatabaseConfig>(connector.dest_config_json);
                                if (conConfig.destDBConfig.databaseType == DatabaseType.None)
                                {
                                    conConfig.destDBConfig.databaseType = databaseType;
                                }
                            }
                            else if (connector.DeDupSetting != null && !string.IsNullOrEmpty(connector.DeDupSetting.database_config_json))
                            {
                                conConfig.destDBConfig = connector.DeDupSetting.ToModel<DatabaseConfig>(databaseType: databaseType);
                            }
                        }
                        conConfig.child_record_count = SyncRepository.GetChildRecordsCount(conConfig);

                    }

                    if (typeof(T) == typeof(ConnectorConfig))
                    {
                        return conConfig as T;
                    }
                    if (typeof(T) == typeof(List<ConnectorConfig>))
                    {
                        List<ConnectorConfig> connectorConfigs = new List<ConnectorConfig>() { conConfig };
                        return connectorConfigs as T;
                    }
                }
            }

            return null;
        }

        public static T ToModel<T>(this List<Connectors> connectors, bool isSetConfig = true) where T : class
        {
            if (typeof(T) == typeof(List<ConnectorConfig>))
            {
                List<ConnectorConfig> connectorConfigs = null;
                if (connectors != null)
                {
                    DeDupSettings dedupSettings = isSetConfig ? connectors.FirstOrDefault().DeDupSetting : null;
                    connectorConfigs = connectors.Select(c =>
                    {
                        ConnectorConfig conConfig;
                        conConfig = new ConnectorConfig()
                        {
                            ccid = c.ccid,
                            connectorId = c.connector_id,
                            connectorName = c.connector_name,
                            sourceObjectName = c.src_object_name,
                            destObjectName = c.dest_object_name,
                            scheduleType = c.schedule_type,
                            srcNewRecordFilter = c.src_new_record_filter,
                            srcUpdateRecordFilter = c.src_update_record_filter,
                            twoWaySyncPriority = c.two_way_sync_priority,
                            syncDestination = c.connector_type,
                            syncCount = c.sync_count,
                            syncStatus = c.sync_status,
                            dedup_type = c.dedup_type,
                            jobId = c.job_id,
                            syncStartedAt = c.sync_started_at,
                            syncEndedAt = c.sync_ended_at,
                            lastSyncAt = c.last_sync_at,
                            lastSyncStatus = c.last_sync_status,
                            dbSchema = c.src_schema,
                            dataSource = c.sync_src,
                            destDBSchema = c.dest_schema,
                            customScheduleInMinutes = c.custom_schedule_in_minutes,
                            dedupSourceType = c.dedup_source_type,

                            dedup_method = c.dedup_method,
                            review_before_delete = c.review_before_delete,
                            backup_before_delete = c.backup_before_delete,

                            simulation_count = c.simulation_count,

                            total_records_count = c.total_records_count,
                            deduped_count = c.deduped_count,
                            fuzzy_ratio = c.fuzzy_ratio * 100,
                            unique_records_count = c.unique_records_count

                        };


                        //if (!string.IsNullOrEmpty(c.src_object_fields_json))
                        //{
                        //    conConfig.sourceObjectFields = JsonConvert.DeserializeObject<List<string>>(c.compare_object_fields);
                        //}
                        
                        
                        if (!string.IsNullOrEmpty(c.compare_config_json) && (c.dedup_source_type == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || c.dedup_source_type == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B))
                        {
                            conConfig.dbConfig_compare = JsonConvert.DeserializeObject<DatabaseConfig>(c.compare_config_json);
                        }
                        if (!string.IsNullOrEmpty(c.compare_object_fields))
                        {
                            conConfig.compareObjectFieldsMapping = JsonConvert.DeserializeObject<List<string>>(c.compare_object_fields);
                            if (c.dedup_source_type == SourceType.Remove_Duplicates_from_a_Single_Table || conConfig.compareObjectFieldsMapping != null)
                            {
                                conConfig.sourceObjectFields = conConfig.compareObjectFieldsMapping;
                                conConfig.dbConfig_compare.compareObjectFields = conConfig.compareObjectFieldsMapping;
                            }
                        }
                        //if (!string.IsNullOrEmpty(c.sync_log_json))
                        //{
                        //    conConfig.connectorLogs = new ConnectorLogs()
                        //    {
                        //        sync_started_at = conConfig.syncStartedAt,
                        //        sync_ended_at = conConfig.syncEndedAt,
                        //        sync_count = conConfig.syncCount,
                        //        sync_logs= JsonConvert.DeserializeObject<List<string>>(c.sync_log_json)
                        //    };
                        //}

                        if (conConfig != null)
                        {


                            if (conConfig.syncDestination == ConnectorType.Heroku_Postgres
                            || conConfig.syncDestination == ConnectorType.Azure_Postgres
                            || conConfig.syncDestination == ConnectorType.AWS_Postgres
                            || conConfig.syncDestination == ConnectorType.Azure_SQL)
                            {
                                DatabaseType databaseType;
                                if (conConfig.syncDestination == ConnectorType.Azure_SQL)
                                {
                                    databaseType = DatabaseType.Azure_SQL;
                                }
                                else if (conConfig.syncDestination == ConnectorType.Azure_Postgres)
                                {
                                    databaseType = DatabaseType.Azure_Postgres;
                                }
                                else if (conConfig.syncDestination == ConnectorType.AWS_Postgres)
                                {
                                    databaseType = DatabaseType.AWS_Postgres;
                                }
                                else
                                {
                                    databaseType = DatabaseType.Heroku_Postgres;
                                }
                                if (!string.IsNullOrEmpty(c.dest_config_json))
                                {
                                    conConfig.destDBConfig = JsonConvert.DeserializeObject<DatabaseConfig>(c.dest_config_json);
                                    if (conConfig.destDBConfig.databaseType == DatabaseType.None)
                                    {
                                        conConfig.destDBConfig.databaseType = databaseType;
                                    }
                                }
                                else if (c.DeDupSetting != null && !string.IsNullOrEmpty(c.DeDupSetting.database_config_json))
                                {
                                    conConfig.destDBConfig = c.DeDupSetting.ToModel<DatabaseConfig>(databaseType: databaseType);
                                }
                            }

                            if (string.IsNullOrEmpty(conConfig.dbSchema))
                            {
                                if ((conConfig.dataSource == DataSource.Heroku_Postgres
                                || conConfig.dataSource == DataSource.Azure_Postgres
                                || conConfig.dataSource == DataSource.AWS_Postgres))
                                {
                                    conConfig.dbSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                                }
                                else if (conConfig.dataSource == DataSource.Azure_SQL)
                                {
                                    conConfig.dbSchema = Constants.MSSQL_DEFAULT_SCHEMA;
                                }
                            }

                            if (string.IsNullOrEmpty(conConfig.destDBSchema))
                            {
                                if ((conConfig.syncDestination == ConnectorType.Heroku_Postgres
                                || conConfig.syncDestination == ConnectorType.Azure_Postgres
                                || conConfig.syncDestination == ConnectorType.AWS_Postgres))
                                {
                                    conConfig.destDBSchema = Constants.POSTGRES_DEFAULT_SCHEMA;
                                }
                                else if (conConfig.syncDestination == ConnectorType.Azure_SQL)
                                {
                                    conConfig.destDBSchema = Constants.MSSQL_DEFAULT_SCHEMA;
                                }
                            }

                            if (isSetConfig)
                            {
                                if (conConfig.dataSource == DataSource.Heroku_Postgres
                                || conConfig.dataSource == DataSource.Azure_Postgres
                                || conConfig.dataSource == DataSource.AWS_Postgres
                                || conConfig.dataSource == DataSource.Azure_SQL)
                                {
                                    DatabaseType databaseType;
                                    if (conConfig.syncDestination == ConnectorType.Azure_SQL)
                                    {
                                        databaseType = DatabaseType.Azure_SQL;
                                    }
                                    else if (conConfig.syncDestination == ConnectorType.Azure_Postgres)
                                    {
                                        databaseType = DatabaseType.Azure_Postgres;
                                    }
                                    else if (conConfig.syncDestination == ConnectorType.AWS_Postgres)
                                    {
                                        databaseType = DatabaseType.AWS_Postgres;
                                    }
                                    else
                                    {
                                        databaseType = DatabaseType.Heroku_Postgres;
                                    }
                                    if (!string.IsNullOrEmpty(c.compare_config_json) && (c.dedup_source_type == SourceType.Copy_Source_data_to_Destination_and_Remove_Duplicates_from_Destination || c.dedup_source_type == SourceType.Merge_Table_A_Data_to_Table_B_and_Remove_Duplicates_from_Table_B))
                                    {
                                        if (c.compare_config_json.StartsWith("["))
                                        {
                                            conConfig.multipleDBConfigs.AddRange(JsonConvert.DeserializeObject<List<DatabaseConfig>>(c.compare_config_json));
                                            //when multiple source assigned then we need to use th below line
                                            //conConfig.dbConfig_compare = JsonConvert.DeserializeObject<DatabaseConfig>(connector.compare_config_json);
                                        }
                                        else
                                        {
                                            conConfig.dbConfig_compare = JsonConvert.DeserializeObject<DatabaseConfig>(c.compare_config_json);
                                            conConfig.multipleDBConfigs.Add(conConfig.dbConfig_compare);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(c.src_config_json))
                                    {
                                        conConfig.dbConfig = JsonConvert.DeserializeObject<DatabaseConfig>(c.src_config_json);
                                        if (conConfig.dbConfig.databaseType == DatabaseType.None)
                                        {
                                            conConfig.dbConfig.databaseType = databaseType;
                                        }
                                        conConfig.dbConfig.db_schema = conConfig.dbSchema;
                                        conConfig.dbConfig.object_name = conConfig.sourceObjectName;
                                        conConfig.multipleDBConfigs.Add(conConfig.dbConfig);
                                    }
                                    else if (dedupSettings != null && !string.IsNullOrEmpty(dedupSettings.database_config_json))
                                    {
                                        conConfig.dbConfig = dedupSettings.ToModel<DatabaseConfig>(databaseType: databaseType);
                                        conConfig.dbConfig.db_schema = conConfig.dbSchema;
                                        conConfig.dbConfig.object_name = conConfig.sourceObjectName;
                                        conConfig.multipleDBConfigs.Add(conConfig.dbConfig);
                                    }
                                   
                                }

                                if (conConfig.syncDestination == ConnectorType.Heroku_Postgres
                                || conConfig.syncDestination == ConnectorType.Azure_Postgres
                                || conConfig.syncDestination == ConnectorType.AWS_Postgres
                                || conConfig.syncDestination == ConnectorType.Azure_SQL)
                                {
                                    DatabaseType databaseType;
                                    if (conConfig.syncDestination == ConnectorType.Azure_SQL)
                                    {
                                        databaseType = DatabaseType.Azure_SQL;
                                    }
                                    else if (conConfig.syncDestination == ConnectorType.Azure_Postgres)
                                    {
                                        databaseType = DatabaseType.Azure_Postgres;
                                    }
                                    else if (conConfig.syncDestination == ConnectorType.AWS_Postgres)
                                    {
                                        databaseType = DatabaseType.AWS_Postgres;
                                    }
                                    else
                                    {
                                        databaseType = DatabaseType.Heroku_Postgres;
                                    }
                                    if (!string.IsNullOrEmpty(c.dest_config_json))
                                    {
                                        conConfig.destDBConfig = JsonConvert.DeserializeObject<DatabaseConfig>(c.dest_config_json);
                                        if (conConfig.destDBConfig.databaseType == DatabaseType.None)
                                        {
                                            conConfig.destDBConfig.databaseType = databaseType;
                                        }
                                    }
                                    else if (dedupSettings != null && !string.IsNullOrEmpty(dedupSettings.database_config_json))
                                    {
                                        conConfig.destDBConfig = dedupSettings.ToModel<DatabaseConfig>(databaseType: databaseType);
                                    }
                                }

                            }
                            conConfig.child_record_count = SyncRepository.GetChildRecordsCount(conConfig);

                        }
                        return conConfig;
                    }).ToList();
                }

                return connectorConfigs as T;
            }
            else if (typeof(T) == typeof(List<ConnectorLogs>))
            {
                List<ConnectorLogs> connectorLogs = null;
                if (connectors != null)
                {
                    connectorLogs = connectors.Select(c =>
                    {
                        return new ConnectorLogs()
                        {
                            sync_connector_name = c.connector_name,
                            sync_started_at = c.sync_started_at,
                            sync_ended_at = c.sync_ended_at,
                            sync_count = c.sync_count,
                            last_sync_at = c.last_sync_at,
                            last_sync_status = c.last_sync_status,
                            sync_status = c.sync_status,
                            sync_logs = Utilities.GetJsonPropertyValueByKeyPath<List<string>>(c.sync_log_json, "")
                        };
                    }).ToList();
                }

                return connectorLogs as T;
            }

            return null;
        }
    }
}
