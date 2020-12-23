using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Dedup.Repositories;
using Dedup.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Net;
using System.IO;
using System.Linq;
using Dedup.Common;
using Dedup.Extensions;
using System.Security.Claims;
using Dedup.HttpFilters;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Dedup.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConnectorsRepository _connectorsRepository;
        private readonly IResourcesRepository _resourcesRepository;
        public HomeController(IConnectorsRepository connectorsRepository, IResourcesRepository resourcesRepository)
        {
            _connectorsRepository = connectorsRepository;
            _resourcesRepository = resourcesRepository;
        }

        /// <summary>
        /// Action: Index
        /// Description: It is called to get all connectors to display on home page.
        /// </summary>
        /// <returns>List<ConnectorConfig></returns>
        [LoginAuthorizeAttribute]
        [TypeFilter(typeof(AddonPlanFilter))]
        [TypeFilter(typeof(UserRoleFilter))]
        public async Task<IActionResult> Index()
        {
            List<ConnectorConfig> ConnectorConfigs = null;
            ConnectorConfig connectorConfig = null;
            try
            {
                if (ViewBag.CurrentPlan.IsInitialized)
                {
                    if (ViewBag.CurrentPlan.isLicenseAccepted)
                    {
                        //Get all connectors from connectors table by ccid
                        ConnectorConfigs = _connectorsRepository.Get<List<ConnectorConfig>>(HttpContext.GetClaimValue(ClaimTypes.NameIdentifier), null, null);

                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
            }

            return View(await Task.FromResult(ConnectorConfigs));
        }

        [LoginAuthorizeAttribute]
        [TypeFilter(typeof(AddonPlanFilter))]
        public IActionResult ExternalObjects()
        {
            return View();
        }

        /// <summary>
        /// Action: Error
        /// Description: It is called when any exception will occur in any action
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;
            return View(new ErrorViewModel() { Code = HttpStatusCode.InternalServerError, Message = (exception.Message.IndexOf("DeDup") == -1 ? exception.Message : exception.Message.Substring(exception.Message.IndexOf("DeDup"))) });
        }

        /// <summary>
        /// Action: Forbidden
        /// Description: It is called when user is not authorised
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult Forbidden()
        {
            ErrorViewModel errorViewModel = new ErrorViewModel();
            var httpStatusCode = HttpStatusCode.Forbidden;
            if (TempData["httpStatusCode"] != null)
                httpStatusCode = (HttpStatusCode)TempData["httpStatusCode"];
            switch (httpStatusCode)
            {
                case HttpStatusCode.NotFound:
                    errorViewModel.Code = httpStatusCode;
                    errorViewModel.Message = string.Format("You did not add the {0} addon to anyone of your heroku app.", ConfigVars.Instance.herokuAddonId);
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                default:
                    errorViewModel.Code = httpStatusCode;
                    if (TempData["errorMessage"] == null)
                        errorViewModel.Message = string.Format("You are not authenticated to access the {0} addon.", ConfigVars.Instance.herokuAddonId);
                    else
                        errorViewModel.Message = (string)TempData["errorMessage"];
                    break;
            }

            //clear all session
            HttpContext.Session.Clear();
            //clear all cookie
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            return View("~/Views/Shared/Unauthorized.cshtml", errorViewModel);
        }

        [LoginAuthorizeAttribute]
        public IActionResult GetLogs()
        {
            try
            {
                string dirPath = Path.Combine(Utilities.HostingEnvironment.ContentRootPath, "wwwroot", "Logs");
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                if (dirInfo.Exists && dirInfo.GetFiles().Count() > 0)
                {
                    var myFile = (from file in dirInfo.GetFiles()
                                  orderby file.LastWriteTime descending
                                  select file).FirstOrDefault();

                    var fileContent = string.Empty;
                    using (FileStream fileStream = new FileStream(myFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader streamReader = new StreamReader(fileStream))
                        {
                            fileContent = streamReader.ReadToEnd();
                        }
                    }

                    return Content(fileContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
            }

            return Content("File not found!");
        }

        [LoginAuthorizeAttribute]
        public async Task<JsonResult> UpdateLicenseAgreement(bool isAccepted)
        {
            try
            {
                Console.WriteLine("UpdateLicenseAgreement starts");
                _resourcesRepository.UpdateLicenseAgreement(HttpContext.GetClaimValue(ClaimTypes.NameIdentifier), isAccepted);
                Console.WriteLine("UpdateLicenseAgreement ended");
                return Json(await Task.FromResult(new { status = HttpStatusCode.OK }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
                return Json(await Task.FromResult(new { status = HttpStatusCode.InternalServerError }));
            }
        }

        protected override void Dispose(bool disposing)
        {
            _resourcesRepository.Dispose();
            _connectorsRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}
