<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Login.ascx.cs" Inherits="DotNetNuke.Authentication.Cas.Login" %>
<%@ Register TagPrefix="dnnC" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnnC:DnnCssInclude ID="CasCss" runat="server" FilePath="~/DesktopModules/AuthenticationServices/Cas/module.css" />


<li id="loginItem" runat="server" class="cas" >
    <asp:LinkButton runat="server" ID="loginButton" CausesValidation="False">
        <span><%=LocalizeString("LoginCas")%></span>
    </asp:LinkButton>
</li>
<li id="registerItem" runat="Server" class="cas">
    <asp:LinkButton ID="registerButton" runat="server" CausesValidation="False">
        <span><%=LocalizeString("RegisterCas") %></span>
    </asp:LinkButton>
</li>
