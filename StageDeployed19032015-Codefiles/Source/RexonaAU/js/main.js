$(document).ready(function() {
	$('.bigBubble img').hide();
	$('.smallBubbles img').hide();
	$('.bubbleLines img').hide();
	var waypoints = $('#js-waypoint-goal').waypoint({
	  handler: function(direction) {
	    jsGoalAnim();
	  },offset: "85%",
	});
	var waypoint2 = $('#js-waypoint-connect').waypoint({
	  handler: function(direction) {
	    jsConnectAnim();
		this.disable();
	  }, offset: "75%"
	})

$('.ipad-images .img-wrapper').slick({arrows:false, infinite:true, speed:500,autoplay:true, autoplaySpeed: 5000});
});

function jsGoalAnim () {
	var strings = [
		"Run 10km in 1 hour",
		"Swim once a week",
		"Lose 5kg",
		"Do a Tough Mudder",
		"Ride my bike to work",
	];
	for (var i = 0; i < strings.length; i++) {
		var str = strings[i];
		$('.js-goal input').typetype(str, { e: 0, t: 200 }).delay(1000).backspace(30, { t: 100 });
		
	};
};
function jsConnectAnim () {
	$('.bigBubble img').hide();
	$('.smallBubbles img').hide();
	$('.bubbleLines img').hide();
	
	setTimeout(function(){
	$(".bigBubble > img").addClass('animated zoomIn').show();
	},500);
	setTimeout(function(){
	$(".smallBubbles > img").addClass('animated zoomIn').show();
	},1500);
	setTimeout(function(){
	$(".bubbleLines > img").fadeIn();
	},3000);	
}


	

