/*
    https://developer.mozilla.org/en/CSS/background-position
    http://www.w3.org/TR/css3-background/#background-position

    Example: http://jsfiddle.net/Blink/bBXvt/
*/(function(){var e=document.createElement("a"),t=e.style,n="right 10px bottom 10px";Modernizr.addTest("bgpositionshorthand",function(){t.cssText="background-position: "+n+";";return t.backgroundPosition===n})})();