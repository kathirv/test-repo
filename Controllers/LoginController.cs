using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Dedup.Services;
using Dedup.ViewModels;
using System.Net;
using Dedup.Extensions;
using Dedup.Repositories;
using System.Security.Claims;
using System.Collections.Generic;
using System.Web;
using Dedup.Common;

namespace Dedup.Controllers
{
    public class LoginController : Controller
    {
        private readonly IPartnerAuthTokenRepository _partnerAuthTokenRepository;

        public LoginController(IPartnerAuthTokenRepository partnerAuthTokenRepository)
        {
            _partnerAuthTokenRepository = partnerAuthTokenRepository;
        }

        /// <summary>
        /// Action: Index
        /// Description: It is called to get heroku auth token url. If token url is empty then redirect to forbidden page
        /// else redirect to action(GetToken) and get the heroku auth token url
        /// </summary>
        /// <returns></returns>
        [ActionName("herokuauth")]
        public async Task<ActionResult> Index(string returnUrl = "")
        {
            Console.WriteLine("Login Controller- Index");
            string resourceId = HttpContext.GetClaimValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(resourceId))
            {
                TempData["httpStatusCode"] = HttpStatusCode.Unauthorized;
                TempData["errorMessage"] = "You are not authenticated due to heroku auth token not accessed.";
                return RedirectToAction("forbidden", "home");
            }

            HerokuAuthToken authToken = default(HerokuAuthToken);
            OauthGrant oathGrant = default(OauthGrant);

            var partnerAuthToken = _partnerAuthTokenRepository.Find(resourceId);
            if (partnerAuthToken == null || (partnerAuthToken != null && (partnerAuthToken.expires_in == DateTime.MinValue
               || (partnerAuthToken.expires_in != DateTime.MinValue && partnerAuthToken.expires_in?.AddSeconds(-300) < DateTime.Now))))
            {
                oathGrant = await HerokuApi.GetOauthGrant(resourceId).ConfigureAwait(false);
                if (oathGrant.IsNull())
                {
                    TempData["httpStatusCode"] = HttpStatusCode.Unauthorized;
                    TempData["errorMessage"] = "You are not authenticated due to heroku auth token not accessed.";
                    return RedirectToAction("forbidden", "home");
                }

                //update heroku auth token
                partnerAuthToken = oathGrant.ToPartnerAuthToken(resourceId);
                partnerAuthToken = _partnerAuthTokenRepository.Add(partnerAuthToken);

                //Get heroku auth token
                authToken = await HerokuApi.GetAddonAccessToken(partnerAuthToken.oauth_code, partnerAuthToken.oauth_type).ConfigureAwait(false);
                if (authToken.IsNull())
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    TempData["httpStatusCode"] = HttpStatusCode.Unauthorized;
                    TempData["errorMessage"] = "You are not authenticated due to heroku auth token not received.";
                    return RedirectToAction("forbidden", "home");
                }

                //assign current resourceId as auth_id
                authToken.auth_id = resourceId;
                partnerAuthToken = authToken.ToPartnerAuthToken();
                if (partnerAuthToken != null)
                {
                    //update heroku auth token
                    partnerAuthToken = _partnerAuthTokenRepository.Add(partnerAuthToken);
                }
            }

            if (partnerAuthToken == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                TempData["httpStatusCode"] = HttpStatusCode.Unauthorized;
                TempData["errorMessage"] = "You are not authenticated due to heroku auth token not received.";
                return RedirectToAction("forbidden", "home");
            }
            else if (partnerAuthToken.Resource != null)
            {
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, partnerAuthToken.Resource.uuid));
                claims.Add(new Claim(ClaimTypes.Version, partnerAuthToken.Resource.plan.ToString()));
                claims.Add(new Claim(ClaimTypes.Country, partnerAuthToken.Resource.region));
                if (!string.IsNullOrEmpty(partnerAuthToken.Resource.user_email))
                    claims.Add(new Claim(Constants.HEROKU_USER_EMAIL, partnerAuthToken.Resource.user_email));
                if (!string.IsNullOrEmpty(partnerAuthToken.Resource.app_name))
                    claims.Add(new Claim(Constants.HEROKU_MAIN_APP_NAME, partnerAuthToken.Resource.app_name));
                claims.Add(new Claim(Constants.HEROKU_ACCESS_TOKEN, partnerAuthToken.access_token));
                claims.Add(new Claim(Constants.HEROKU_REFRESH_TOKEN, partnerAuthToken.refresh_token));
                claims.Add(new Claim(Constants.HEROKU_AUTH_USERID, partnerAuthToken.user_id));
                if (partnerAuthToken.expires_in.HasValue)
                {
                    claims.Add(new Claim(Constants.HEROKU_TOKEN_EXPIREDIN, partnerAuthToken.expires_in.Value.ToString()));
                }

                HttpContext.AddUpdateClaims(claims);
            }

            //redirect to url
            if (string.IsNullOrEmpty(returnUrl) ||
            (!string.IsNullOrEmpty(returnUrl) && (returnUrl.Contains("localhost"))
            || !Uri.IsWellFormedUriString(returnUrl, UriKind.RelativeOrAbsolute)))
            {
                return RedirectToAction("index", "home");
            }
            else
            {
                //Redirect to home page
                return Redirect(HttpUtility.UrlDecode(returnUrl));
            }
        }

        [HttpGet]
        public async Task<ActionResult> refreshtoken(string returnUrl = "")
        {
            Console.WriteLine("Login Controller- refreshtoken");
            //Get heroku auth token
            HerokuAuthToken authToken = await HerokuApi.GetAddonAccessToken(HttpContext.GetClaimValue(Constants.HEROKU_REFRESH_TOKEN), AuthGrantType.refresh_token).ConfigureAwait(false);
            if (authToken.IsNull())
            {
                return RedirectToAction("herokuauth", "login", new { returnUrl = HttpUtility.UrlEncode(returnUrl) });
            }
            else
            {
                //assign current resourceId as auth_id
                authToken.auth_id = HttpContext.GetClaimValue(ClaimTypes.NameIdentifier);

                //update heroku auth token
                var partnerAuthToken = authToken.ToPartnerAuthToken();
                partnerAuthToken = _partnerAuthTokenRepository.Add(partnerAuthToken);
                if (partnerAuthToken.Resource != null)
                {
                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, partnerAuthToken.Resource.uuid));
                    claims.Add(new Claim(ClaimTypes.Version, partnerAuthToken.Resource.plan.ToString()));
                    claims.Add(new Claim(ClaimTypes.Country, partnerAuthToken.Resource.region));
                    if (!string.IsNullOrEmpty(partnerAuthToken.Resource.user_email))
                        claims.Add(new Claim(Constants.HEROKU_USER_EMAIL, partnerAuthToken.Resource.user_email));
                    if (!string.IsNullOrEmpty(partnerAuthToken.Resource.app_name))
                        claims.Add(new Claim(Constants.HEROKU_MAIN_APP_NAME, partnerAuthToken.Resource.app_name));
                    claims.Add(new Claim(Constants.HEROKU_ACCESS_TOKEN, partnerAuthToken.access_token));
                    claims.Add(new Claim(Constants.HEROKU_REFRESH_TOKEN, partnerAuthToken.refresh_token));
                    claims.Add(new Claim(Constants.HEROKU_AUTH_USERID, partnerAuthToken.user_id));
                    if (partnerAuthToken.expires_in.HasValue)
                    {
                        claims.Add(new Claim(Constants.HEROKU_TOKEN_EXPIREDIN, partnerAuthToken.expires_in.Value.ToString()));
                    }
                    HttpContext.AddUpdateClaims(claims);
                }
            }

            //redirect to url
            if (string.IsNullOrEmpty(returnUrl) ||
            (!string.IsNullOrEmpty(returnUrl) && (returnUrl.Contains("localhost"))
            || !Uri.IsWellFormedUriString(returnUrl, UriKind.RelativeOrAbsolute)))
            {
                return RedirectToAction("index", "home");
            }
            else
            {
                //Redirect to home page
                return Redirect(HttpUtility.UrlDecode(returnUrl));
            }
        }

        protected override void Dispose(bool disposing)
        {
            _partnerAuthTokenRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}
