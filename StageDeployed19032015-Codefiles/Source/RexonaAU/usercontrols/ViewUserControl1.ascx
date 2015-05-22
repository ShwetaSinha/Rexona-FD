<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>




     <%@ Import Namespace="umbraco.MacroEngines.Library" %>

     <%@ Import Namespace="System.Web.Mvc.Html" %>
     <%@ Import Namespace="Umbraco.Web" %>
     <%@ Import Namespace="umbraco.NodeFactory" %>
     <%@ Import Namespace="umbraco.MacroEngines.Library" %>
<%@ Import Namespace="Umbraco.Web.PublishedContentExtensions"> %>

<%
   // Node homePage = new Node(1108);

   // var homePage = new Node(1108);
    var homePage = Umbraco.Web.PublishedContentExtensions.AncestorOrSelf(1);
    
    var pages = homePage.Children.hWhere(showInHeader => showInHeader.GetPropertyValue("showInHeader") != null).OrderBy(navigationOrder => navigationOrder.GetPropertyValue("navigationOrder")).ToList();
  
    
     %>