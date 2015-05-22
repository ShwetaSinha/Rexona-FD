// Low Battery Level
// Enable a developer to remove CPU intensive CSS/JS when battery is low
// developer.mozilla.org/en/DOM/window.navigator.mozBattery
// By: Paul Sayre
Modernizr.addTest("lowbattery",function(){var e=.2,t=Modernizr.prefixed("battery",navigator);return!!(t&&!t.charging&&t.level<=e)});