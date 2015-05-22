// Foundation JavaScript
// Documentation can be found at: http://foundation.zurb.com/docs
$(document).foundation();

var $container = $('.packery-wall');
// initialize
$container.packery({
  itemSelector: '.item',
  gutter: 3,
  transitionDuration: '0.6s'
});

$( '.tick-1' ).ticker(
{
  incremental: 1,
  delay: 800,
  separators: true
});

$( '.tick-2' ).ticker(
{
  incremental: 1,
  delay: 1000,
  separators: true
});

$('.step1-photo').click(function(){
	$(this).fadeOut(300, function(){
		$('.step2-photo').fadeIn(300)
	})
})

$('.take-another').click(function(){
	$('.step2-photo').fadeOut(300, function(){
		$('.step1-photo').fadeIn(300);	
	})
})

$('.datepicker').pickadate()

function closeModal(){
  $('.lightbox').fadeOut(300);
}
function openModal(){
  $('.lightbox').fadeIn(300);
}

$('.packery-wall .pledge').click(function(){
  openModal()
})

$('.close-modal').click(function(){
  closeModal()
})

var originalUrl = $(location).attr('href');
var splitParts = originalUrl.split("?modal=");
var modalStatus = splitParts[1];

if(modalStatus == 'true'){
  openModal()
}