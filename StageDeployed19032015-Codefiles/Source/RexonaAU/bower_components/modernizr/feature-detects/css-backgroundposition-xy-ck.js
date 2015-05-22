/*
	Allan Lei https://github.com/allanlei
	
	Check adapted from https://github.com/brandonaaron/jquery-cssHooks/blob/master/bgpos.js
	
	Test: http://jsfiddle.net/allanlei/R8AYS/
*/Modernizr.addTest("bgpositionxy",function(){return Modernizr.testStyles("#modernizr {background-position: 3px 5px;}",function(e){var t=window.getComputedStyle?getComputedStyle(e,null):e.currentStyle,n=t.backgroundPositionX=="3px"||t["background-position-x"]=="3px",r=t.backgroundPositionY=="5px"||t["background-position-y"]=="5px";return n&&r})});