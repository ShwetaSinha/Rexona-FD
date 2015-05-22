// by tauren
// https://github.com/Modernizr/Modernizr/issues/191
Modernizr.addTest("cookies",function(){if(navigator.cookieEnabled)return!0;document.cookie="cookietest=1";var e=document.cookie.indexOf("cookietest=")!=-1;document.cookie="cookietest=1; expires=Thu, 01-Jan-1970 00:00:01 GMT";return e});