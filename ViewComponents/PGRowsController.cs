﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Dedup.Repositories;
using Pioneer.Pagination;
using Dedup.Extensions;
using Dedup.ViewModels;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace Dedup.ViewComponents
{
    [ViewComponent(Name = "PGRows")]
    public class PGRows : ViewComponent
    {
        private readonly ILogger _logger;
        private readonly IConnectorsRepository _connectorRepository;
        private readonly IPaginatedMetaService _paginatedMetaService;
        private int PAGESIZE = 50;
        private readonly ISyncRepository _syncRepository;

        public PGRows(IConnectorsRepository connectorRepository, IPaginatedMetaService paginatedMetaService, ILogger<PGRows> logger, ISyncRepository syncRepository)
        {
            _connectorRepository = connectorRepository;
            _paginatedMetaService = paginatedMetaService;
            _logger = logger;
            _syncRepository = syncRepository;
        }

        /// <summary>
        /// Method: InvokeAsync
        /// Description: It is used to get postgres sync data by page wise.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <param name="isRowsRead"></param>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        public async Task<IViewComponentResult> InvokeAsync(int id, int? page, bool isRowsRead = true, ConnectorConfig connectorConfig = null, string ctid = null)
        {
            //Get postgres sync data
            return View(await GetPGRowsAsync(id, page, isRowsRead, connectorConfig, ctid));
        }
        /// <summary>
        /// Method: GetPGRowsAsync
        /// Description: It is used to get postgres sync data by page wise.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <param name="isRowsRead"></param>
        /// <param name="connectorConfig"></param>
        /// <returns></returns>
        private async Task<IList<IDictionary<string, object>>> GetPGRowsAsync(int id, int? page, bool isRowsRead, ConnectorConfig connectorConfig, string ctid)
        {
            IEnumerable<dynamic> etDataRows = null;
            IList<IDictionary<string, object>> pgRows = new List<IDictionary<string, object>>();
           
            try
            {
                var ccid = HttpContext.GetClaimValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(ccid) && id > 0)
                {
                    //Get ConnectorConfig
                    if (connectorConfig == null)
                    {
                        connectorConfig = _connectorRepository.Get<ConnectorConfig>(ccid, id, IsSetConfig: true);
                    }

                    //Get the current page
                    int currentPage = page.HasValue ? (int)page : 1;
                    int totalRecords = 0;
                    int marked_for_delete_count = 0;
                    if (connectorConfig != null)
                    {
                        ViewBag.connectorConfig = connectorConfig;
                        //Get total record
                        //totalRecords = SyncRepository.GetPGRecordCountByName(connectorConfig);
                        totalRecords = _syncRepository.GetCTIndexTableCount(ccid, id);
                        //marked_for_delete_count = _syncRepository.GetMarkedForDeleteCount(ccid, id);
                        ViewBag.marked_for_delete_count = marked_for_delete_count;

                        
                            ViewData["count_" + id.ToString()] = totalRecords;
                        //Read sync data if isRowsRead flag is true
                        if (String.IsNullOrEmpty(ctid))
                        {
                            if (isRowsRead && totalRecords > 0)
                            {
                                //Get page settings
                                ViewData[id.ToString()] = _paginatedMetaService.GetMetaData(totalRecords, currentPage, PAGESIZE);
                                //Get sync data by pageNo, ccid, connectorId and limit
                                //etDataRows = SyncRepository.FindPGRowsByPageNo(connectorConfig, currentPage, PAGESIZE);

                                pgRows = await _syncRepository.GetParentRecordsPageByPageForReviewAndDelete(connectorConfig.ccid, id, currentPage, PAGESIZE);
                                //pgRows = result.Cast<IDictionary<string, object>>().ToList();
                                //pgRows = SyncRepository.GetParentRecordsPageByPageForReviewAndDelete(connectorConfig, currentPage, PAGESIZE);


                            }
                        }
                        else
                        {
                            pgRows = await _syncRepository.GetChildRecordsByParentForReviewAndDelete(connectorConfig.ccid, id, ctid);
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development)
                    _logger.LogError(ex.Message, ex);
                else
                    Console.WriteLine("ERROR: {0}", ex.Message);
            }
            
            return pgRows;
        }
    }
}
