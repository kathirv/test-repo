using Dedup.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Dedup.ViewModels
{
    public class DatabaseConfig
    {
        [JsonIgnore]
        public string ccid { get; set; }

        public string serialNo { get; set; }

        [Required(ErrorMessage = "Database Url Is Required")]
        [RegularExpression(@"^postgres(?:ql)?:\/\/([^:]*):([^@]*)@(.*?):(.*?)\/(.*?)$", ErrorMessage = "Database Url Is Required")]
        public string syncDefaultDatabaseUrl { get; set; }

        public DataSource dataSource { get; set; }

        public string object_name { get; set; }
        public SelectedTableType table_type { get; set; }
        [Required(ErrorMessage = "Table is required")]
        public string new_table_name { get; set; }

        public string db_schema { get; set; }
        [JsonIgnore]
        public List<string> compareObjectFields { get; set; }
        [Required]
        public DatabaseType databaseType { get; set; } = DatabaseType.None;

        [JsonIgnore]
        public List<SyncObjectColumn> syncCompareObjectColumns { get; set; }

        public string new_record_filter { get; set; }
        public string update_record_filter { get; set; }

    }
}
