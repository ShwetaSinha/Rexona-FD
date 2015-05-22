// Method of allowing calculated values for length units, i.e. width: calc(100%-3em) http://caniuse.com/#search=calc
// By @calvein
Modernizr.addTest("csscalc",function(){var e="width:",t="calc(10px);",n=document.createElement("div");n.style.cssText=e+Modernizr._prefixes.join(t+e);return!!n.style.length});