using System;
using System.Collections.Generic;
using Dedup.ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Dapper;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Dedup.Common
{
    public sealed class ConfigVars
    {
        public string ClientId = string.Empty;
        public string ClientSecret = string.Empty;
        public string HerokuApiUrl = string.Empty;
        public Uri herokuAuthServerBaseUrl = null;
        public string herokuPassword = string.Empty;
        public string herokuAddonId = string.Empty;
        public string herokuSalt = string.Empty;
        public string herokuAddonAppName = string.Empty;
        public string connectionString = string.Empty;
        public string hangfireConnectionString = string.Empty;
        public string deDupWebUrl = string.Empty;
        public bool deDupPvtEdition = false;
        public List<PlanInfos> addonPlans = null;
        public int DEDUP_PAGE_SIZE = 1000;
        public double pgMaxQuerySizeInMB = 3.5;
        public int deDup_workerCount = 2;
        public string addonClientSecret = string.Empty;
        public string PROVISION_ERR_MSG = string.Empty;
        public string DFT_PROVISION_MSG = string.Empty;
        public string deDupBuildGitRepoUrl = string.Empty;
        public string deDupBuildpackUrl = string.Empty;
        public string deDupPvtAppVersion = string.Empty;

        private ConfigVars()
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", "postgres://vvhajfndsxnfuh:526d248215b2834367b5073030b35ff64b83e547c0407707defcfe71cac325c6@ec2-52-202-146-43.compute-1.amazonaws.com:5432/df21q4pqk9q075");
            Environment.SetEnvironmentVariable("HEROKU_POSTGRESQL_NAVY_URL", "postgres://zlkxdrvxsxsmur:a4816d7a0711f79dc50d590deb9cb81a6ead1de9f99f66c5f279124b2974681f@ec2-184-72-236-3.compute-1.amazonaws.com:5432/d2db9jprauqmnq");
            //if (Utilities.HostingEnvironment.IsDevelopment())
            //{
            //    //Local
            //    Environment.SetEnvironmentVariable("DATABASE_URL", "postgres://vvhajfndsxnfuh:526d248215b2834367b5073030b35ff64b83e547c0407707defcfe71cac325c6@ec2-52-202-146-43.compute-1.amazonaws.com:5432/df21q4pqk9q075");
            //    Environment.SetEnvironmentVariable("HEROKU_POSTGRESQL_NAVY_URL", "postgres://zlkxdrvxsxsmur:a4816d7a0711f79dc50d590deb9cb81a6ead1de9f99f66c5f279124b2974681f@ec2-184-72-236-3.compute-1.amazonaws.com:5432/d2db9jprauqmnq");
            //    //merg360
            //    //Environment.SetEnvironmentVariable("DATABASE_URL", "postgres://pqgturfskwctks:8843110335049f9def1d68c6847ce61c8d60253129d02d6762aed0a278849833@ec2-54-197-249-140.compute-1.amazonaws.com:5432/dfbkaq2i99fdf0");
            //    //Environment.SetEnvironmentVariable("HEROKU_POSTGRESQL_NAVY_URL", "postgres://jwjhqxmyjwkgxl:dfd0dd9cac6386940499dfabd9c331629072580252ce0a7b81e082fc187b6110@ec2-54-204-36-249.compute-1.amazonaws.com:5432/d3g2itdb8tk7h5");

            //    connectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("DATABASE_URL"));
            //    hangfireConnectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("HEROKU_POSTGRESQL_NAVY_URL"));
            //}
            connectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("DATABASE_URL"));
            hangfireConnectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("HEROKU_POSTGRESQL_NAVY_URL"));
            //else if (Utilities.HostingEnvironment.IsStaging())
            //{
            //    connectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("DATABASE_URL"));
            //    hangfireConnectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("HEROKU_POSTGRESQL_ROSE_URL"));
            //}
            //else
            //{
            //    connectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("DATABASE_URL"));
            //    hangfireConnectionString = ConnectionFactory.GetPGConnectionStringFromUrl(Environment.GetEnvironmentVariable("HEROKU_POSTGRESQL_NAVY_URL"));
            //}

            LoadDeDupConfigsByType();
        }

        public static ConfigVars Instance { get { return ConfigVarInstance.Instance; } }

        private class ConfigVarInstance
        {
            static ConfigVarInstance()
            {
            }
            internal static readonly ConfigVars Instance = new ConfigVars();
        }

        public void LoadDeDupConfigsByType(LoadConfigType type = LoadConfigType.ALL)
        {
            try
            {
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionString))
                {
                    switch (type)
                    {
                        case LoadConfigType.PLAN:
                            addonPlans = connectionFactory.DbConnection.Query<PlanInfos>("SELECT * FROM \"dedup-settings\".\"planinfos\";").ToList();
                            break;
                        case LoadConfigType.CONFIGVARS:
                            var configvars = connectionFactory.DbConnection.Query<dynamic>("SELECT * FROM \"dedup-settings\".\"configvars\";").FirstOrDefault();
                            SetConfigvars(configvars);
                            break;
                        default:
                            var queryResult = connectionFactory.DbConnection.QueryMultiple("SELECT * FROM \"dedup-settings\".\"configvars\";SELECT * FROM \"dedup-settings\".\"planinfos\";");
                            if (queryResult != null)
                            {
                                SetConfigvars(queryResult.Read<dynamic>().FirstOrDefault());
                                addonPlans = queryResult.Read<PlanInfos>().ToList();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                throw;
            }
        }

        public Task LoadDeDupConfigsByTypeAsync(LoadConfigType type = LoadConfigType.ALL)
        {
            try
            {
                using (ConnectionFactory connectionFactory = new ConnectionFactory(connectionString))
                {
                    switch (type)
                    {
                        case LoadConfigType.PLAN:
                            addonPlans = connectionFactory.DbConnection.Query<PlanInfos>("SELECT * FROM \"dedup-settings\".\"planinfos\";").ToList();
                            break;
                        case LoadConfigType.CONFIGVARS:
                            var configvars = connectionFactory.DbConnection.Query<dynamic>("SELECT * FROM \"dedup-settings\".\"configvars\";").FirstOrDefault();
                            SetConfigvars(configvars);
                            break;
                        default:
                            var queryResult = connectionFactory.DbConnection.QueryMultiple("SELECT * FROM \"dedup-settings\".\"configvars\";SELECT * FROM \"dedup-settings\".\"planinfos\";");
                            if (queryResult != null)
                            {
                                SetConfigvars(queryResult.Read<dynamic>().FirstOrDefault());
                                addonPlans = queryResult.Read<PlanInfos>().ToList();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                throw;
            }
            return Task.CompletedTask;
        }

        private void SetConfigvars(dynamic configvars)
        {
            try
            {
                if (configvars != null)
                {
                    herokuAddonAppName = configvars.public_addon_name.Trim();
                    deDupWebUrl = configvars.public_addon_url.Trim();
                    herokuAddonId = configvars.heroku_user.Trim();
                    herokuPassword = configvars.heroku_password.Trim();
                    herokuSalt = configvars.heroku_salt.Trim();
                    HerokuApiUrl = configvars.heroku_api_url.Trim();
                    Uri.TryCreate(configvars.heroku_auth_url.Trim(), UriKind.Absolute, out herokuAuthServerBaseUrl);
                    addonClientSecret = configvars.addon_client_secret.Trim();
                    DFT_PROVISION_MSG = configvars.provision_success_message.Trim();
                    PROVISION_ERR_MSG = configvars.provision_error_message.Trim();
                    DEDUP_PAGE_SIZE = (int)configvars.dedup_page_size;
                    deDup_workerCount = (int)configvars.dedup_worker_count;
                    pgMaxQuerySizeInMB= (double)configvars.pg_max_query_size_in_mb;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
