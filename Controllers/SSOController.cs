using System;
using Microsoft.AspNetCore.Mvc;
using Dedup.Common;
using Dedup.Repositories;
using Microsoft.AspNetCore.Http;
using Dedup.ViewModels;
using Dedup.Extensions;
using Microsoft.AspNetCore.Routing;
using System.Net;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using RestSharp.Extensions.MonoHttp;

namespace Dedup.Controllers
{
    public class SSOController : Controller
    {
        private IResourcesRepository _resourcesRepository { get; set; }

        public SSOController(IResourcesRepository resourcesRepository)
        {
            _resourcesRepository = resourcesRepository;
        }

        /// <summary>
        /// Action: Index
        /// Description: It is called to authorise user to access the addon when user is coming from heroku dashboard
        /// by clicking on addon
        /// </summary>
        /// <param name="data"></param>
        /// <returns>StatusCodeResult</returns>
        [HttpPost("sso/login")]
        public async Task<IActionResult> Index([FromQuery] SSOData data)
        {
            try
            {
                Console.WriteLine("SSO Controller- Index");
                string id = HttpContext.Request.Form["id"];
                string timestamp = HttpContext.Request.Form["timestamp"];
                string token = HttpContext.Request.Form["token"];
                string appName = HttpContext.Request.Form["app"];
                string userEmail = HttpContext.Request.Form["email"];
                foreach (var key in HttpContext.Request.Form.Keys)
                {
                    Console.WriteLine("{0}=>{1}", key, HttpContext.Request.Form[key]);
                }

                //Clear all sessions
                HttpContext.Session.Clear();
                //Generate token by using resource-id,HEROKU_SSO_SALT and timestamp value
                var preToken = string.Format("{0}:{1}:{2}", id, ConfigVars.Instance.herokuSalt, timestamp);

                //Convert to hash string
                preToken = Utilities.SHA1HashStringForUTF8String(preToken);
                Console.WriteLine(token + " : " + preToken);
                Console.WriteLine(timestamp);

                //Check token is matching with preToken. If not then throw error
                if (token != preToken)
                {
                    Console.WriteLine("token not match");
                    return new StatusCodeResult(403);
                }

                //Check timestamp value is expired or not. If it expired then throw error
                if (Convert.ToInt64(timestamp) < Utilities.ConvertToUnixTime(DateTime.Now.AddMinutes(-(2 * 60))))
                {
                    return new StatusCodeResult(403);
                }
                Console.WriteLine("Find resource " + id);

                //validate account by resource id
                var resources = _resourcesRepository.Find(id);
                if (resources == null)
                {
                    return new StatusCodeResult(404);
                }
                else
                {
                    if (!string.IsNullOrEmpty(appName))
                    {
                        if (string.IsNullOrEmpty(resources.app_name))
                        {
                            //Assign app name
                            resources.app_name = appName;
                        }
                    }
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        if (string.IsNullOrEmpty(resources.user_email))
                        {
                            //Assign app name
                            resources.user_email = userEmail;
                        }
                    }
                    //Update app name
                    _resourcesRepository.Update(resources);
                }
                //Update the main app name in resource table based on resource-id if app name is null
                

                //clear all cookie
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }

                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, resources.uuid));
                claims.Add(new Claim(ClaimTypes.Version, resources.plan));
                if (!string.IsNullOrEmpty(resources.user_name))
                    claims.Add(new Claim(ClaimTypes.Name, resources.user_name));
                if (!string.IsNullOrEmpty(resources.user_email))
                    claims.Add(new Claim(ClaimTypes.Email, resources.user_email));
                if (!string.IsNullOrEmpty(resources.private_app_url))
                    claims.Add(new Claim(ClaimTypes.Uri, resources.private_app_url));
                if (!string.IsNullOrEmpty(resources.app_name))
                    claims.Add(new Claim(Dedup.Common.Constants.HEROKU_MAIN_APP_NAME, resources.app_name));
                if (resources.partnerAuthToken != null)
                {
                    Console.WriteLine("partnerAuthToken");
                    if (!string.IsNullOrEmpty(resources.partnerAuthToken.access_token))
                    {
                        Console.WriteLine("resources.partnerAuthToken : {0}", resources.partnerAuthToken.access_token);
                        claims.Add(new Claim(Dedup.Common.Constants.HEROKU_ACCESS_TOKEN, resources.partnerAuthToken.access_token));
                    }
                    if (!string.IsNullOrEmpty(resources.partnerAuthToken.refresh_token))
                    {
                        Console.WriteLine("resources.partnerAuthToken : {0}", resources.partnerAuthToken.refresh_token);
                        claims.Add(new Claim(Dedup.Common.Constants.HEROKU_REFRESH_TOKEN, resources.partnerAuthToken.refresh_token));
                    }
                    if (!string.IsNullOrEmpty(resources.partnerAuthToken.user_id))
                    {
                        Console.WriteLine("resources.partnerAuthToken.user_id : {0}", resources.partnerAuthToken.user_id);
                        claims.Add(new Claim(Dedup.Common.Constants.HEROKU_AUTH_USERID, resources.partnerAuthToken.user_id));
                    }
                    if (resources.partnerAuthToken.expires_in.HasValue)
                    {
                        Console.WriteLine("resources.expires_in.HasValue : {0}", resources.partnerAuthToken.expires_in.ToString());
                        claims.Add(new Claim(Dedup.Common.Constants.HEROKU_TOKEN_EXPIREDIN, resources.partnerAuthToken.expires_in.Value.ToString()));
                    }
                }
                else
                {
                    Console.WriteLine("partnerAuthToken  : null");
                }
                await HttpContext.SignOutAsync(Dedup.Common.Constants.DEFAULT_AUTH_COOKIE_SCHEME);
                ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Dedup.Common.Constants.DEFAULT_AUTH_COOKIE_SCHEME));
                await HttpContext.SignInAsync(Dedup.Common.Constants.DEFAULT_AUTH_COOKIE_SCHEME, principal);

                return RedirectToAction("index", "home");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Action: Auth
        /// Description: It is called to re-authorise by app itself if the current session has expired.
        /// If user authorised then redirect to home page else redirect to forbidden page
        /// </summary>
        /// <returns></returns>
        [HttpGet("sso/auth"), Route("sso/auth/{id?}")]
        public async Task<IActionResult> auth(string id = "")
        {
            try
            {
				
				
                Console.WriteLine("SSO Controller- Auth");
                //clear all session
                HttpContext.Session.Clear();

                if (string.IsNullOrEmpty(HttpContext.GetClaimValue(ClaimTypes.NameIdentifier)) && string.IsNullOrEmpty(id))
                {
                    Console.WriteLine("The current session has expired");
                    TempData["httpStatusCode"] = HttpStatusCode.Unauthorized;
                    TempData["errorMessage"] = string.Format("The current session has expired. Please try to access the {0} addon from heroku dashboard.", ConfigVars.Instance.herokuAddonId);
                    return RedirectToAction("forbidden", new RouteValueDictionary(new { controller = "home", action = "forbidden" }));
                }
                else
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        //assign resource id
                        id = HttpContext.GetClaimValue(ClaimTypes.NameIdentifier);
                    }
                    else if (!HttpContext.Request.Host.ToUriComponent().Contains("localhost"))
                    {
                        Guid resourceId = Guid.Empty;
						id="aWy8Rimh692zFCT3KxIySqpGdZsPikOmVOePc591Xza3MOnvldSsKaH+XVFQSt9i/O4g9RcxqEpJW1SV+gWgikC4X43u5dbSZEA6RbrLsak=";
                        if (!Guid.TryParse(Utilities.DecryptText(id.Replace('-', '+').Replace('_', '/')), out resourceId))
                        {
                            Console.WriteLine("The resource-Id is not valid");
                            TempData["httpStatusCode"] = HttpStatusCode.BadRequest;
                            return RedirectToAction("forbidden", new RouteValueDictionary(new { controller = "home", action = "forbidden" }));
                        }
                        else
                        {
                            id = resourceId.ToString();
                        }
                    }
					
                    //validate account by resource id
                    var resources = _resourcesRepository.Find(id);
                    if (resources == null)
                    {
                        Console.WriteLine("The resource-Id is not matching");
                        TempData["httpStatusCode"] = HttpStatusCode.NotFound;
                        return RedirectToAction("forbidden", new RouteValueDictionary(new { controller = "home", action = "forbidden" }));
                    }

                    //clear all cookie
                    foreach (var cookie in Request.Cookies.Keys)
                    {
                        Response.Cookies.Delete(cookie);
                    }

                    //Clear all sessions
                    HttpContext.Session.Clear();

                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, resources.uuid));
                    claims.Add(new Claim(ClaimTypes.Version, resources.plan));
                    if (!string.IsNullOrEmpty(resources.user_name))
                        claims.Add(new Claim(ClaimTypes.Name, resources.user_name));
                    if (!string.IsNullOrEmpty(resources.user_email))
                        claims.Add(new Claim(ClaimTypes.Email, resources.user_email));
                    if (!string.IsNullOrEmpty(resources.private_app_url))
                        claims.Add(new Claim(ClaimTypes.Uri, resources.private_app_url));
                    if (resources.partnerAuthToken != null)
                    {
                        if (!string.IsNullOrEmpty(resources.partnerAuthToken.access_token))
                            claims.Add(new Claim(Dedup.Common.Constants.HEROKU_ACCESS_TOKEN, resources.partnerAuthToken.access_token));
                        if (!string.IsNullOrEmpty(resources.partnerAuthToken.refresh_token))
                            claims.Add(new Claim(Dedup.Common.Constants.HEROKU_REFRESH_TOKEN, resources.partnerAuthToken.refresh_token));
                        if (!string.IsNullOrEmpty(resources.partnerAuthToken.user_id))
                            claims.Add(new Claim(Dedup.Common.Constants.HEROKU_AUTH_USERID, resources.partnerAuthToken.user_id));
                        if (resources.partnerAuthToken.expires_in.HasValue)
                        {
                            claims.Add(new Claim(Dedup.Common.Constants.HEROKU_TOKEN_EXPIREDIN, resources.partnerAuthToken.expires_in.Value.ToString()));
                        }
                    }
                    await HttpContext.SignOutAsync(Dedup.Common.Constants.DEFAULT_AUTH_COOKIE_SCHEME);
                    ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Dedup.Common.Constants.DEFAULT_AUTH_COOKIE_SCHEME));
                    await HttpContext.SignInAsync(Dedup.Common.Constants.DEFAULT_AUTH_COOKIE_SCHEME, principal);
                    return RedirectToAction("index", "home");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
                return new StatusCodeResult(500);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _resourcesRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}