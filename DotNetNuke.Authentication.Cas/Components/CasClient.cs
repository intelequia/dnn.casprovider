#region Copyright

// 
// Intelequia Software solutions - https://intelequia.com
// Copyright (c) 2010-2020
// by Intelequia Software Solutions
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using DotNetNuke.Authentication.Cas.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.OAuth;

#endregion

namespace DotNetNuke.Authentication.Cas.Components
{
    public class CasClient : OAuthClientBase
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(CasClient));

        private const string TokenEndpointPattern = "{0}/oauth2.0/accessToken";
        private const string LogoutEndpointPattern = "{0}/oauth2.0/logout?post_logout_redirect_uri={1}";
        private const string AuthorizationEndpointPattern = "{0}/oauth2.0/authorize";
        private const string ProfileEndpointPattern = "{0}/oauth2.0/profile?access_token={1}";
        #region Constructors

        private JwtSecurityToken JwtSecurityToken { get; set; }
        private string AccessToken { get; set; }
        private Uri LogoutEndpoint { get; }

        private CasConfig Settings { get;  }


        public CasClient(int portalId, AuthMode mode) 
            : base(portalId, mode, "Cas")
        {
            Settings = new CasConfig("Cas", portalId);

            TokenMethod = HttpMethod.POST;
    
            
            if (!string.IsNullOrEmpty(Settings.ServerUrl))
            {
                TokenEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.TokenEndpointPattern", TokenEndpointPattern), Settings.ServerUrl));  
                LogoutEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.LogoutEndpointPattern", LogoutEndpointPattern), Settings.ServerUrl, UrlEncode(HttpContext.Current.Request.Url.ToString())));
                AuthorizationEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.AuthorizationEndpointPattern", AuthorizationEndpointPattern), Settings.ServerUrl));
            }

            Scope = "email";

            AuthTokenName = "CasUserToken";
            APIResource = Settings.APIKey;
            OAuthVersion = "2.0";
            LoadTokenCookie(string.Empty);
            JwtSecurityToken = null;
        }

        #endregion
        protected override TimeSpan GetExpiry(string responseText)
        {
            var jsonSerializer = new JavaScriptSerializer();
            var tokenDictionary = jsonSerializer.DeserializeObject(responseText) as Dictionary<string, object>;

            return new TimeSpan(0, 0, Convert.ToInt32(tokenDictionary["expires_in"]));
        }

        public override void AuthenticateUser(UserData user, PortalSettings settings, string IPAddress, Action<NameValueCollection> addCustomProperties, Action<UserAuthenticatedEventArgs> onAuthenticated)
        {
            var userInfo = UserController.GetUserByEmail(PortalSettings.Current.PortalId, user.Email);
            // If user doesn't exist, base.AuthenticateUser() will create it. 
            if (userInfo == null)
            {
                base.AuthenticateUser(user, settings, IPAddress, addCustomProperties, onAuthenticated);
                if (IsCurrentUserAuthorized())
                {
                    userInfo = UserController.GetUserByEmail(PortalSettings.Current.PortalId, user.Email);
                    ApproveUser(userInfo);
                }
            }
            else
            {
                if (IsCurrentUserAuthorized())
                {
                    ApproveUser(userInfo);
                }
                base.AuthenticateUser(user, settings, IPAddress, addCustomProperties, onAuthenticated);
            }
        }

        private void ApproveUser(UserInfo userInfo)
        {
            if (!userInfo.Membership.Approved && IsCurrentUserAuthorized())
            {
                userInfo.Membership.Approved = true; // Delegate approval on Auth Provider
                UserController.UpdateUser(userInfo.PortalID, userInfo);
            }
        }

        protected override string GetToken(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
            {
                throw new Exception("There was an error processing the credentials. Contact your system administrator.");
            }
            var jsonSerializer = new JavaScriptSerializer();
            var tokenDictionary = jsonSerializer.DeserializeObject(responseText) as Dictionary<string, object>;
            if (!tokenDictionary.ContainsKey("access_token"))
                throw new SecurityTokenException("The token does not contain the 'access_token'");
            AccessToken = Convert.ToString(tokenDictionary["access_token"]);

            // Get the user profile
            var profileEndPoint = new Uri(string.Format(Utils.GetAppSetting("Cas.ProfileEndpointPattern", ProfileEndpointPattern), Settings.ServerUrl, AccessToken));
            var request = WebRequest.CreateDefault(profileEndPoint);
            request.Method = "GET";

            var profile = string.Empty;
            try
            {
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (var responseReader = new StreamReader(responseStream))
                            {
                                profile = responseReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                using (Stream responseStream = ex.Response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var responseReader = new StreamReader(responseStream))
                        {
                            Logger.ErrorFormat("WebResponse exception: {0}", responseReader.ReadToEnd());
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(profile))
                throw new SecurityTokenException("Could not obtain the profile. Check the log4net logs for details.");

            JwtSecurityToken = new JwtSecurityToken(profile);                        
            return AccessToken;
        }

        public override TUserData GetCurrentUser<TUserData>()
        {
            LoadTokenCookie(String.Empty);

            if (!IsCurrentUserAuthorized() || JwtSecurityToken == null)
            {
                return null;
            }
            var claims = JwtSecurityToken.Claims.ToArray();
            var user = new CasUserData()
            {
                CasFirstName = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.GivenName)?.Value,
                CasLastName = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.FamilyName)?.Value,
                CasDisplayName = claims.FirstOrDefault(x => x.Type == "name").Value,
                Email = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName).Value,
                Id = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName).Value
            };
            return user as TUserData;
        }

        public void Logout()
        {
            if (HttpContext.Current.Request.Cookies.AllKeys.Contains(AuthTokenName)
                && (!(HttpContext.Current.Request.Cookies[AuthTokenName].Expires < DateTime.UtcNow.AddDays(-1))
                    || HttpContext.Current.Request.Cookies[AuthTokenName].Expires == DateTime.MinValue))
            {
                RemoveToken();
                HttpContext.Current.Response.Redirect(LogoutEndpoint.ToString(), true);
            }
        }
    }
}