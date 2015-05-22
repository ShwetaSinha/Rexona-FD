// developer.mozilla.org/en/CSS/background-repeat
// test page: jsbin.com/uzesun/
// http://jsfiddle.net/ryanseddon/yMLTQ/6/    
(function(){function e(e){return window.getComputedStyle?getComputedStyle(e,null).getPropertyValue("background"):e.currentStyle.background}Modernizr.testStyles(" #modernizr { background-repeat: round; } ",function(t,n){Modernizr.addTest("bgrepeatround",e(t)=="round")});Modernizr.testStyles(" #modernizr { background-repeat: space; } ",function(t,n){Modernizr.addTest("bgrepeatspace",e(t)=="space")})})();