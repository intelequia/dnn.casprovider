﻿#region Copyright

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
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using DotNetNuke.Authentication.Cas.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.OAuth;

#endregion

namespace DotNetNuke.Authentication.Cas.Components
{
    public class CasClient : OAuthClientBase
    {
        private const string TokenEndpointPattern = "https://login.microsoftonline.com/{0}/oauth2/token";
        private const string LogoutEndpointPattern =
            "https://login.microsoftonline.com/{0}/oauth2/logout?post_logout_redirect_uri={1}";
        private const string AuthorizationEndpointPattern = "https://login.microsoftonline.com/{0}/oauth2/authorize";
        private const string GraphEndpointPattern = "https://graph.windows.net/{0}";
        #region Constructors

        private JwtSecurityToken JwtSecurityToken { get; set; }
        private Uri LogoutEndpoint { get; }


        public CasClient(int portalId, AuthMode mode) 
            : base(portalId, mode, "Cas")
        {
            var config = new CasConfig("Cas", portalId);

            TokenMethod = HttpMethod.POST;
    
            
            if (!string.IsNullOrEmpty(config.TenantId))
            {
                TokenEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.TokenEndpointPattern", TokenEndpointPattern), config.TenantId));  
                LogoutEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.LogoutEndpointPattern", LogoutEndpointPattern), config.TenantId, UrlEncode(HttpContext.Current.Request.Url.ToString())));
                AuthorizationEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.AuthorizationEndpointPattern", AuthorizationEndpointPattern), config.TenantId));
                MeGraphEndpoint = new Uri(string.Format(Utils.GetAppSetting("Cas.GraphEndpointPattern", GraphEndpointPattern), config.TenantId));
            }

            Scope = "email";

            AuthTokenName = "CasUserToken";
            APIResource = config.APIKey;
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
            var token = Convert.ToString(tokenDictionary["access_token"]);
            JwtSecurityToken = new JwtSecurityToken(Convert.ToString(tokenDictionary["id_token"]));                        
            return token;
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