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
using System.Diagnostics;
using DotNetNuke.Authentication.Cas.Components;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.OAuth;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins.Controls;
using log4net;

#endregion

namespace DotNetNuke.Authentication.Cas
{
    public partial class Login : OAuthLoginBase
    {
        private ILog _logger = LogManager.GetLogger(typeof(Login));
        protected override string AuthSystemApplicationName => "Cas";

        public override bool SupportsRegistration => true;

        protected override UserData GetCurrentUser()
        {
            return OAuthClient.GetCurrentUser<CasUserData>();
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            loginButton.Click += loginButton_Click;
            registerButton.Click += loginButton_Click;

            OAuthClient = new CasClient(PortalId, Mode);

            loginItem.Visible = (Mode == AuthMode.Login);
            registerItem.Visible = (Mode == AuthMode.Register);

            var config = new CasConfig("Cas", PortalId);
            if (config.AutoRedirect && Request["legacy"] != "1")
                loginButton_Click(null, null);
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Request["error"]))
            {
                var errorMessage = Localization.GetString("LoginError", LocalResourceFile);
                errorMessage = string.Format(errorMessage, Request["error"], Request["error_description"]);
                _logger.Error(errorMessage);
                UI.Skins.Skin.AddModuleMessage(this, errorMessage, ModuleMessage.ModuleMessageType.RedError);
            }           
            else
            {
                var hasVerificationCode = OAuthClient.IsCurrentService() && OAuthClient.HaveVerificationCode();
                if (!hasVerificationCode)
                {
                    AuthorisationResult result = OAuthClient.Authorize();
                    if (result == AuthorisationResult.Denied)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("PrivateConfirmationMessage", Localization.SharedResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
                    }
                }
            }
        }
    }
}