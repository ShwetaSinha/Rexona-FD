// Foundation JavaScript
// Documentation can be found at: http://foundation.zurb.com/docs
var profanityWords;
var takeAnother;

$(document).foundation({

    abide: {
        validators: {
            nameValidator: function (el, required, parent) {
                
                if (el.value.length <= 0) {
                    document.getElementById('nameError').textContent = "Please enter your name.";
                    return false;
                } else if (el.value.length >= 26) {
                    document.getElementById('nameError').textContent = "Name must be 25 characters or less";
                    return false;
                } //other rules can go here

                var stringVal = String(el.value)

                // extend strings with the method "contains"
                String.prototype.contains = function (str) { return this.indexOf(str) != -1; };

                // swear words to filter out
                var profanities = new Array("cocks", "cock", "dick", "anus", "anal", "fisting", "jizz", "poon", "vagina", "cunt", "shit", "shitting", "piss", "pissing", "bitch", "slut", "whore", "fuck", "fucking", "twat", "boob", "vajayjay", "moot", "pussy", "genitals", "genitalia", "asshole", "muff", "butthole", "scrote", "scrotum", "scrot", "stiffy", "erection", "boner", "hardon", "hard on", "hard-on", "semen", "ejaculate", "pubes", "pubic hair", "pubic", "slutbag", "shitmuffin", "coon", "nigger", "anal sex", "rape", "molest", "shitfuck", "shitfucker", "shitfucking", "fuckers", "fucked", "infinity docking", "ballsack", "nutsack", "blowjob", "blowjobs", "clit", "dildo", "homo", "sextoy", "fag", "faggot", "skank", "smegma", "fudgepacker", "nigga", "wank", "wanking", "wanker", "tit", "titties", "titty", "tittys", "titfuck", "tittyfuck");

                var containsProfanity = function (text) {
                    var returnVal = false;
                    for (var i = 0; i < profanities.length; i++) {
                        if (text.toLowerCase().contains(profanities[i].toLowerCase())) {
                            returnVal = true;
                            break;
                        }
                    }
                    return returnVal;
                }
                if (containsProfanity(stringVal)) {
                    document.getElementById('nameError').textContent = "No profanity, please.";
                    return false;
                } else {
                    return true;
                }
                return true;
            },
            dobValidation: function (el, required, parent) {
                if (el.value.length <= 0) {
                    document.getElementById('dobError').textContent = "Please enter your post code.";
                    return false;
                } else if (el.value.length > 4 || el.value.length <= 3) {
                    document.getElementById('dobError').textContent = "Post code must be 4 digits";
                    return false;
                }
                return true;
            },
            postcodeValidation: function (el, required, parent) {

                var postcode = String(el.value)
                if (postcode.match(/^[0-9]+$/) != null) 
                {
                    if (postcode.length < 4) {
                        document.getElementById('postError').textContent = "Post code must be a 4 digit number";
                        return false;
                    } else if (postcode.length > 4) {
                        document.getElementById('postError').textContent = "Post code must be a 4 digit number";
                        return false;
                    } else {
                        return true;
                    }
                } else {
                    document.getElementById('postError').textContent = "Post code must be a 4 digit number";
                    return false;
                }
            },
            passwordCheck: function (el, required, parent)
            {
                var stringVal = String(el.value)
                if (stringVal.length <= 5)
                {
                    document.getElementById('passwordError').textContent = "Password must contain minimun of 6 characters.";
                    return false;
                }
                else
                {
                    return true;
                }
            },

            passwordCheck_dashboard: function (el, required, parent) {
                var stringVal = String(el.value)
                if (stringVal != "" && stringVal.length <= 5) {
                    document.getElementById('passwordError').textContent = "Password must contain minimun of 6 characters.";
                    return false;
                }
                else {
                    return true;
                }
            },
           /*passwordCheck: function (el, required, parent) {
                var password = String(el.value)
                function checkCase(str) {
                    return str.match(/[a-z]/) && str.match(/[A-Z]/);
                }
                function checkNumber(str) {
                    return str.match(/[0-9]/)
                }
                function checkPunctuation(str) {
                    return str.match(/[^\w\s]|_/)
                }
                if (checkCase(password) && checkNumber(password) && checkPunctuation(password)) {
                    // meets pw requirements
                    return true;
                } else {
                    document.getElementById('passwordError').textContent = "Passwords must contain a number, punctuation (e.g. (),!@#$%^&*), and at least one capital letter.";
                    return false;
                }
            },*/
            profanity: function (el, required, parent) {
                var stringVal = String(el.value)

                // extend strings with the method "contains"
                String.prototype.contains = function (str) { return this.indexOf(str) != -1; };

                // swear words to filter out
                var profanities = new Array("cocks", "cock", "dick", "anus", "anal", "fisting", "jizz", "poon", "vagina", "cunt", "shit", "shitting", "piss", "pissing", "bitch", "slut", "whore", "fuck", "fucking", "twat", "boob", "vajayjay", "moot", "pussy", "genitals", "genitalia", "asshole", "muff", "butthole", "scrote", "scrotum", "scrot", "stiffy", "erection", "boner", "hardon", "hard on", "hard-on", "semen", "ejaculate", "pubes", "pubic hair", "pubic", "slutbag", "shitmuffin", "coon", "nigger", "anal sex", "rape", "molest", "shitfuck", "shitfucker", "shitfucking", "fuckers", "fucked", "infinity docking", "ballsack", "nutsack", "blowjob", "blowjobs", "clit", "dildo", "homo", "sextoy", "fag", "faggot", "skank", "smegma", "fudgepacker", "nigga", "wank", "wanking", "wanker", "tit", "titties", "titty", "tittys", "titfuck", "tittyfuck");

                var containsProfanity = function (text) {
                    var returnVal = false;
                    for (var i = 0; i < profanities.length; i++) {
                        if (text.toLowerCase().contains(profanities[i].toLowerCase())) {
                            returnVal = true;
                            break;
                        }
                    }
                    return returnVal;
                }


                if (containsProfanity(stringVal)) {
                    if (el.id == 'discussions-reply') {
                        $(el).next().text("No profanity, please.");
                    }
                    else {
                        document.getElementById('swearError').textContent = "No profanity, please.";
                    }
                    return false;
                }
                if (el.value.length == 0) {
                    if (el.id == 'discussions-reply') {
                        $(el).next().text("Please enter some text.");
                    }
                    else {
                        document.getElementById('swearError').textContent = "Please enter some text.";
                    }
                } else {
                    return true;
                }
            },
            pledgeText: function (el, required, parent) {
                var stringVal = String(el.value)
                
                // extend strings with the method "contains"
                String.prototype.contains = function (str) { return this.indexOf(str) != -1; };

                // swear words to filter out
                var profanities = new Array("cocks", "cock", "dick", "anus", "anal", "fisting", "jizz", "poon", "vagina", "cunt", "shit", "shitting", "piss", "pissing", "bitch", "slut", "whore", "fuck", "fucking", "twat", "boob", "vajayjay", "moot", "pussy", "genitals", "genitalia", "asshole", "muff", "butthole", "scrote", "scrotum", "scrot", "stiffy", "erection", "boner", "hardon", "hard on", "hard-on", "semen", "ejaculate", "pubes", "pubic hair", "pubic", "slutbag", "shitmuffin", "coon", "nigger", "anal sex", "rape", "molest", "shitfuck", "shitfucker", "shitfucking", "fuckers", "fucked", "infinity docking", "ballsack", "nutsack", "blowjob", "blowjobs", "clit", "dildo", "homo", "sextoy", "fag", "faggot", "skank", "smegma", "fudgepacker", "nigga", "wank", "wanking", "wanker", "tit", "titties", "titty", "tittys", "titfuck", "tittyfuck");

                var profanityCheck = function (text) {
                    var returnVal = false;
                    for (var i = 0; i < profanities.length; i++) {
                        if (text.toLowerCase().contains(profanities[i].toLowerCase())) {
                            returnVal = true;
                            break;
                        }
                    }
                    return returnVal;
                }
                if (profanityCheck(stringVal))
                {
                    document.getElementById('swearError').textContent = "No profanity, please.";
                    return false;
                }

                if (el.value.length <= 0)
                {
                    document.getElementById('swearError').textContent = "Please enter a goal.";
                }
                else if (el.value.length >= 41)
                {
                    document.getElementById('swearError').textContent = "Goal must be 40 characters or less";
                }
                else
                {
                    return true;
                }
            },
            discussionTitle: function (el, required, parent) {
                var stringVal = String(el.value)

                // extend strings with the method "contains"
                String.prototype.contains = function (str) { return this.indexOf(str) != -1; };

                // swear words to filter out
                var profanities = new Array("cocks", "cock", "dick", "anus", "anal", "fisting", "jizz", "poon", "vagina", "cunt", "shit", "shitting", "piss", "pissing", "bitch", "slut", "whore", "fuck", "fucking", "twat", "boob", "vajayjay", "moot", "pussy", "genitals", "genitalia", "asshole", "muff", "butthole", "scrote", "scrotum", "scrot", "stiffy", "erection", "boner", "hardon", "hard on", "hard-on", "semen", "ejaculate", "pubes", "pubic hair", "pubic", "slutbag", "shitmuffin", "coon", "nigger", "anal sex", "rape", "molest", "shitfuck", "shitfucker", "shitfucking", "fuckers", "fucked", "infinity docking", "ballsack", "nutsack", "blowjob", "blowjobs", "clit", "dildo", "homo", "sextoy", "fag", "faggot", "skank", "smegma", "fudgepacker", "nigga", "wank", "wanking", "wanker", "tit", "titties", "titty", "tittys", "titfuck", "tittyfuck");

                var profanityCheck = function (text) {
                    var returnVal = false;
                    for (var i = 0; i < profanities.length; i++) {
                        if (text.toLowerCase().contains(profanities[i].toLowerCase())) {
                            returnVal = true;
                            break;
                        }
                    }
                    return returnVal;
                }
                if (profanityCheck(stringVal)) {
                    document.getElementById('titleError').textContent = "No profanity, please.";
                    return false;
                }
                else if (el.value.length >= 141) {
                    document.getElementById('titleError').textContent = "Title must be 140 characters or less";
                }
                else if (el.value.length == 0) {
                    document.getElementById('titleError').textContent = "Please enter a title.";
                } else {
                    return true;
                }
            }
        }
    }
});


// open shared pledges
$(document).ready(function () {
    // Get URL of the page
    // Split the url and get the current Pledge
    if (sessionStorage.getItem('redirectError') == "true") {
        new jBox('Notice', {
            content: '<strong>Oops.</strong><br><br>The link you are trying to use has expired.',
            color: 'red',
            theme: 'NoticeBorder'
        });
        sessionStorage.setItem('redirectError', "false");
    }


    $(".scroll").click(function (event) {
        //prevent the default action for the click event
        event.preventDefault();

        //get the full url - like mysitecom/index.htm#home
        var full_url = this.href;

        //split the url by # and get the anchor target name - home in mysitecom/index.htm#home
        var parts = full_url.split("#");
        var trgt = parts[1];

        //get the top offset of the target anchor
        var target_offset = $("#" + trgt).offset();
        var target_top = target_offset.top - 55;

        //goto that anchor by setting the body scroll top to anchor top
        $('html, body').animate({ scrollTop: target_top }, 750);
        var chosenDiv = $("#" + trgt)


    });


    var originalUrl = $(location).attr('href');
    var splitParts = originalUrl.split("?share=");
    var sharedImg = splitParts[1];


    FastClick.attach(document.body);

    //start invite
    if (window.location.href.indexOf('?qinvite=') > -1) {
        startLoad();

        var queryData = new Object();
        queryData.queryString = decodeURIComponent(getParameterByName('qinvite'));
        $.ajax({
            url: '/umbraco/surface/FacebookMember/decodeQuerystring',
            type: 'POST',//JSON.stringify(details),
            dataType: 'json',
            data: JSON.stringify(queryData),
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                //console.log("query");
                //console.log(data.details);
                if (data.details) {
                    var details = data.details;
                    sessionStorage.setItem('FbInvite', true);
                    sessionStorage.setItem('InvitePledgeId', details.PledgeId);
                    sessionStorage.setItem('InviteMemberId', details.MemberId);
                    sessionStorage.setItem('InvitePledgeTitle', details.Title);
                    sessionStorage.setItem('InvitePledgeType', details.PledgeType);
                    if (data.IsLoggedIn) {
                        window.location.href = '/enter-your-goal';
                    }
                    else {
                        window.location.href = '/sign-up';
                    }
                }
                else {
                    endLoad();
                    sessionStorage.setItem('redirectError', "true");
                    window.location.href = "/";
                }
            }
        });
    }
    //end invite
    if (originalUrl.indexOf("?share=") >= 0) {

        //openGoalImage(sharedImg);
        //console.log(sharedImg)
    }
    profanityWords = $('#hdnProfanities').val();


    if (typeof (Zenbox) != "undefined") {
        //console.log('in zendbox');
        Zenbox.init({
            dropboxID: "20134669",
            url: "https://theworkssydney.zendesk.com",
            tabTooltip: "Feedback",
            tabImageURL: "https://assets.zendesk.com/external/zenbox/images/tab_feedback_right.png",
            tabColor: "black",
            tabPosition: "Right"
        });
    }
});

var pledgeUrl;
function openGoalImage(img) {
    $('.shared-image-lightbox').toggleClass('is-open');
    $('#shared-image').attr('src', img)

    pledgeUrl = 'http://testground.newrepublique.com/pledge-network?share=' + img

    // share to twitter
    $('.twitter-share-button').attr('href', 'https://twitter.com/intent/tweet?text=%23IWILLDO%20&url=' + pledgeUrl)
    // share to google+ 
    $('.g-plus').attr('href', 'https://plus.google.com/share?url={' + pledgeUrl + '});')
}

// share to Facebook
$('.fb-share-pledge').click(function () {
    FB.ui({ // open a share dialog and pass it the url 
        method: 'share',
        href: pledgeUrl
    }, function (response) { });
    //alert(pledgeUrl)
});

//$('.facebook-share-button').click(function () {
//    FB.ui({ // open a share dialog and pass it the url 
//        method: 'share',
//        href: theUrl,
//    }, function (response) { });
//})

// control stacked cards
function runCards() {
    $('.is-open .current-card').toggleClass('is-retreating');
    $('.is-open .next-card').toggleClass('is-entering');
}
function openCloseStack(target) {
    $(target).toggleClass('is-open')
}

function closeStacks() {
    // fade out then reset the style attr so we can re-open lightboxes
    if ($('#eventForm').length) {
        $('form#eventForm')[0].reset();
    }
    $('.is-open').css({ 'opacity': 0 });
    setTimeout(function () {
        $('.is-open').attr('style', '');
        $('.is-open').removeClass('is-open');

        $('video:not("#home-video")').each(function () {
            if (this.player) {
                this.player.pause();
                this.player.remove();
            }
        });
        //if ($('.play-video .video-lightbox object').length) {
        //    $('.play-video .video-lightbox object').remove();
        //}
        if ($('.dashboardVideo iframe').length) {
            $('.dashboardVideo iframe').remove();
        }
    }, 400)
}

function openLightbox(target) {
    $(target).toggleClass('is-open');
}

// video lightbox function. pass it the video src as an option
var player;
function playVideo(source, ytThumb, pledgeTitle) {
    // reinit media-element
    var videoId = source.split('embed/')[1];

    var ytUrl = 'https://www.youtube.com/watch?v=' + videoId;
    var width = $('.dashboardVideo').width();
    if ($('.dashboardVideo iframe').length) {
        $('.dashboardVideo iframe').remove();
    }
    //if (source.indexOf(':') > -1) {
    //    source = source.split(':')[1];
    //}
    if (source.indexOf('https:') <= -1 && source.indexOf('//') > -1) {
        source = "https://www." + source.split("//")[1];
    }
    source = source + "?enablejsapi=1&rel=0&showinfo=0";
    $('.dashboardVideo a').before('<iframe class="video" height="350" width="' + width + '" src="' + source + '" frameborder="0" allowfullscreen>');

    var facebookstring = 'https://www.facebook.com/dialog/feed?' +
'app_id=1506171272955397' +
'&link=' + encodeURIComponent(ytUrl) +
'&display=popup' +
    '&name=' + encodeURIComponent(pledgeTitle) +
    '&caption=' +
    '&description=Rexona : I Will Do.' +
    '&picture=https%3A//i.ytimg.com/vi/' + videoId + '/maxresdefault.jpg' +
    '&source=' + encodeURIComponent('https://www.youtube.com/v/' + videoId + '&version=3&autohide=1&feature=share&autoplay=1&showinfo=1') +
        '&redirect_uri=https://www.youtube.com/facebook_redirect';

    $('.fbVideoShare').attr('href', facebookstring);
    $('.twVideoShare').attr('href', 'https://twitter.com/intent/tweet?url=' + encodeURIComponent(ytUrl) + '&text=%23IWILLDO%20' + pledgeTitle + ' : &related=YouTube,YouTubeTrends,YTCreators');
    $('.gpVideoShare').attr('href', 'https://plus.google.com/u/0/share?url=' + encodeURIComponent(ytUrl) + '&source=yt&hl=en&soc-platform=1&soc-app=130');

    $('.video-player').addClass('is-open');
    trackYouTube();

}

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);

    return results == null ? "" : results[1];
}

// loading functions
function startLoad() {
    $('.loading-container').fadeIn(300);
}
function endLoad() {
    $('.loading-container').fadeOut(300);
}

// checksize of viewport to determine
//// which image processing method to undertake
//// when making a pledge
//var currentSize = 'desktop'
//function checkSize() {
//    if ($('.on-desktop').is(":visible")) {
//        currentSize = 'desktop'
//    } else if ($('.on-mobile').is(":visible")) {
//        currentSize = 'mobile'
//    }
//}
//// assemble the pledge
//function makePledge() {
//    // change the caman target depending on screen size
//    checkSize()
//    if (currentSize == 'desktop') {
//        theTarget = '.desktop-target'
//    } else if (currentSize == 'mobile') {
//        theTarget = '.mobile-target'
//    };

//    // hit the appropriate target with Caman.js
//    Caman(theTarget, function () {
//        this.greyscale();
//        this.colorize('#c3c4c7', 100);
//        // apply metal texture
//        this.newLayer(function () {
//            this.setBlendingMode('normal');
//            this.overlayImage('/images/metal-overlay.jpg');
//        });
//        // apply b+w image on top of texture
//        this.newLayer(function () {
//            this.setBlendingMode("multiply");
//            this.copyParent();
//            this.filter.greyscale();
//            this.filter.brightness(20);
//            this.filter.contrast(110);
//            this.filter.sharpen(30);
//        });
//        // make sure no color was introduced    
//        this.greyscale();
//        // render to canvas
//        this.render();
//    });
//}
//// close the loading screen when Caman finishes
//Caman.Event.listen("renderFinished", function (job) {
//    endLoad();
//});



// convert contents of webcam to base64 and send to DOM
var usersImage;
function base64_toimage() {

    function error() {
        new jBox('Notice', {
            content: '<strong>Error.</strong><br><br>No Camera detected, Please Upload a photo.',
            color: 'red',
            theme: 'NoticeBorder'
        });
    }

    if (navigator.getUserMedia || navigator.webkitGetUserMedia || navigator.mozGetUserMedia || navigator.msGetUserMedia || navigator.userAgent.toLowerCase().match('macintosh') != undefined || navigator.userAgent.toLowerCase().match('msie') != undefined) {
        $('#image').attr("src", "data:image/png;base64," + $.scriptcam.getFrameAsBase64());
       
        $('#hdnImgSrc').val($.scriptcam.getFrameAsBase64());
        $('.webcam-overlay').fadeOut(300);
        $('#webcam').fadeOut(300, function () {
            $('#image').show()
            $('.take-a-photo').hide()
            $('.confirm-photo').show()
        })
        usersImage = $.scriptcam.getFrameAsBase64()
        
        //send ajax call to log Step2.1 Choose Photo
        logUploadPhoto();

    } else {
        error();
    }

    //console.log('user image captured as usersImage variable');
};

// handle if user decides to take another photo instead of using current one
function masonryTiles() {
    var $container = $('.masonry-wall');
    // layout Masonry again after all images have loaded
    $container.imagesLoaded(function () {
        $container.masonry({
            itemSelector: '.item'
        });
    });

}


// handle mobile file upload
//function mobileUpload() {
//    $("#mobile-picker").click();
//}

$(document).ready(function () {

    $(document).on('click', '.local-nav li a', function () {
        if ($(this).attr('href').indexOf('sign') < 0 && $(this).attr('href').indexOf('login') < 0 && $(this).attr('href') != '#') {
            if ($(this).attr('href').indexOf('enter-your-goal') > 0) {
                sessionStorage.setItem("returnBackToMakeAPledge", true);
            }
        }
    });

    masonryTiles();
    $('[name=subscribe]').val($('#subscribe').is(':checked'));
    $('#subscribe').change(function () {
        $('[name=subscribe]').val(this.checked);
    });


    $("a[onclick], button[onclick]").on("click", function (e) {
        return e.preventDefault();
    });

    // init datepicker
    var currentYear = (new Date).getFullYear();
    var currentMonth = (new Date).getMonth();
    var currentDay = (new Date).getDate();
    var minimumBirthYear = currentYear - 18;
    $('.datepicker-birthdate').pickadate({
        max: new Date(minimumBirthYear, currentMonth, currentDay),
        selectYears: 80,
        selectMonths: true,
        today: '',
        format: 'dd mmm yy',
        // makes the limit 'today'
    });
    //  $('.datepicker-regular').pickadate();

    $('.datepicker-event').datepicker({ dateFormat: "dd-mm-yy" });

    var $input = $('.datepicker-regular').pickadate({
        onSet: function () {
            length = $('.datepicker-finish').val().length;
            var picker = $input.pickadate('picker');

            if (length == 0) {
                getUserDate(picker);
            }
            else {
                getUserDate(picker);
                $('.datepicker-finish').val('')
            }
        }
    });

    $(document).click(function () {

        if (!$(this).is('.video-lightbox') && !$(this).parents('.video-lightbox').length && $(this).parents('.modal-container').length) {
            $('.play-video.is-open').toggleClass('is-open')
        }
    });

    $(".video-lightbox, .plays-video").click(function (e) {
        e.stopPropagation();
    });

    // remove items from a list
    $('.removeItem').click(function () {
        var parent = $(this).parent().parent().parent().parent();
        $(parent).fadeOut(300);
    });

    $('.removeFriend').click(function () {
        $(this).fadeOut(300, function () {
            $(this).next('.confirm-remove').fadeIn();
        });
    });

    $('.cancelRemove').click(function () {
        $(this).parent().parent().fadeOut(300, function () {
            $('.removeFriend').fadeIn();
        });
    });


    // use foundation throttle so that callback isn't called twice
    //$('#send-invite').on('valid.fndtn.abide', Foundation.utils.throttle(function (e) {
    //    runCards()
    //    $('#invite-email').val('');
    //    $('#invite-message').val('');
    //}, 300));


    //stage fix 
    //$('iframe').attr('width', $('iframe').parent().width());

    // auto stay mediaelement.js on homepage only 
    $('#home-video').mediaelementplayer({
        alwaysShowControls: false,
        pauseOtherPlayers: true,
        features: []
    });



    //step-1 form
    $('#pledgeFormSubmit').click(function () {
        $('form').submit();
    });

    $('form').on('valid', function () {
        $('#step-one').hide();
        $('#step-two').show();
    });

    var webcamError = false;
    // initialise webcam in make a pledge section
    $("#webcam").scriptcam({
        path: '/js/plugins/',
        width: 600,
        height: 480,
        disableHardwareAcceleration: 1,
        maskImage: '/images/webcam-mask.png',
        noFlashFound: '<p><a href="http://www.adobe.com/go/getflashplayer"> 4 Adobe Flash Player</a> 11.7 or greater is needed to use your webcam.</p>',
        //showDebug: true,
        zoom: 1.07,
        onError: oopsError
    });
    function oopsError(errorId, errorMsg) {

        if (!webcamError) {
            webcamError = true;
            new jBox('Notice', {
                content: '<strong>Error.</strong><br><br>No Camera detected, Please Upload a photo.',
                color: 'red',
                theme: 'NoticeBorder'
            });
        }
    }


    $('.take-another').click(function () {
        var control = $("#mobile-picker");


        control.replaceWith(control = control.clone(true));
        fileManage()
        $('.webcam-overlay').fadeIn(300);
        $('#image').fadeOut(300, function () {
            $('#webcam').show()
            $('.take-a-photo').show()
            $('.confirm-photo').hide()
        })
        //send ajax call to log Step2 drop-offs
        //will reset field value to 1
        logAnotherPhoto();

    });


    // handle if user confirms the taken photo
    //$('.use-photo').click(function () {
    //    startLoad();
    //    ImgProcessing();
    //    $('.step-2').hide();
    //        $('.step-3').show();
    //    //setTimeout(function () {
    //    //    $('.step-2').hide();
    //    //    $('.step-3').show();
    //    //    // makePledge();

    //    //}, 500)
    //})

    //$('.use-photo').click(function () {
    //    startLoad();
    //    // note to cybage, this is where you should look for a response from the server
    //    // once the image has been processed, you can advance the page to step 3
    //})

    function ImgProcessing() {


        var orientation = sessionStorage.getItem("orientation") ? sessionStorage.getItem("orientation") : 0;

        var s1 = $('#hdnImgSrc').val();
        var dataFile = new Object();
        dataFile.base64 = s1;
        dataFile.orientation = orientation;
        console.log('session storage' + sessionStorage.getItem("orientation"));

        $.ajax({
            url: $('#hdnmode').val(),
            data: dataFile,
            type: 'POST',

            success: function (data) {
                if (data.Message.indexOf('error') > -1) {
                    new jBox('Notice', {
                        content: '<strong>Oops.</strong><br><br>Something went wrong. Please try again.',
                        color: 'red',
                        theme: 'NoticeBorder'
                    });
                    sessionStorage.setItem('backPressed', true);
                    window.location.href = "/enter-your-goal";
                }
                else {
                    sessionStorage.setItem('imageUrl', data.ImagePath);
                    sessionStorage.setItem('textToBeSentToYolk', data.TextToBeSentToYolk);
                    window.location.href = "/confirm-goal/";
                }
            }
        })
    }
    $('.use-photo').click(function () {
        startLoad();
        setTimeout(function () {
            // $('.step-2').hide();
            //  $('.step-3').show();
            // makePledge();
            ImgProcessing();
        }, 500)
    })

    // when the "Take a Photo or Choose from Library" is used on mobile
    $('#mobile-picker').change(function () {

        //console.log('user image captured as usersImage variable')
    })

    // toggle menu on mobile
    $('.menu-open').click(function () {
        $('.double-navigation').toggleClass('open')
    });



    // autocomplete for pledges      

    $('#pledges').autocomplete({
        source: function (request, response) {
            $.ajax({
                url: $('#mode').val(),
                data: { 'term': this.term },
                dataType: "json",
                type: "POST",
                success: function (data) {
                    response($.map(data, function (name, val) {
                        return {
                            label: name.Value
                            , value: name.Id
                        }
                    }));
                }
            });
        },
        minLength: 3,
        select: function (event, ui) {
            //console.log(ui.item);
            if (ui != null && ui.item != null) {
                $('#pledges').val(ui.item.label);
                $('#hdnId').val(ui.item.value);
            }
            return false;
        },

        focus: function (event, ui) {
            if (ui != null && ui.item != null) {
                $('#pledges').val(ui.item.label);
                $('#hdnId').val(ui.item.value);
            }
            return false;
        },
        change: function (event, ui) {
            if (ui.item) {
                $('#pledges').val(ui.item.label);
                $('#hdnId').val(ui.item.value);
            }
            else {
                $('#hdnId').val('0');
            }
        }

    });
    // autocomplete for pledges
    // backend developers will need to rejig this section and provide
    // the .autocomplete() plugin with a source from the server

    //var availableTags = [
    //  "Run more often",
    //  "Run my own gym",
    //  "Run the city to surf",
    //  "Run every day",
    //  "Wear awesome runners"
    //  "Run to work everyday",
    //  "Go for a run every week",
    //  "Swim more often",
    //  "Swim at the beach every weekend",
    //  "Learn to swim",
    //  "Swim a million laps",
    //  "Swim everyday",
    //  "Lose weight",
    //  "Lose my baby fat",
    //  "Learn to ride a bike",
    //  "Meditate every day",
    //  "Go to the gym more often",
    //  "Stop drinking caffeine",
    //  "Give up sugar",
    //  "Improve my diet",
    //  "Go on a diet",
    //  "Get a personal trainer",
    //  "Lift a bunch of weights",
    //  "Run 10 laps a week",
    //  "Sign up for the gym",
    //  "Beat my best time in the pool",
    //  "Work out every week",
    //  "Ride my bike to work",
    //  "Take my bike to work",
    //  "Buy a bike"
    //];

    //$("#pledges").autocomplete({
    //    source: availableTags,
    //    minLength: 3
    //});

    // control helper messages on sign up form for privacy settings
    $('#privacy-selection').change(function () {

        if ($(this).val() == 'public') {
            $('.private').fadeOut(300, function () {
                $('.public').fadeIn(300);
            });
        } else if ($(this).val() == 'private') {

            $('.public').fadeOut(300, function () {
                $('.private').fadeIn(300);
            });
        }
    })


    // pill boxes
    $('.pills a').click(function (e) {
        e.preventDefault();
        // get context so that snippet places nicely with multiple pill boxes on one page
        var context = $(this).parent().parent().parent().parent().attr('id');
        context = '#' + context
        //alert(context)

        // target div
        var target = $(this).attr('href');

        // remove anything tagged as active in current context
        $(context).find('li.active').removeClass('active');
        $(this).parent().addClass('active');
        $(context).find('.pill-box .active').removeClass('active');
        $(target).addClass('active');
    });


    //$('.hide-replies').next('.children').slideUp(300);

    $('.hide-replies').click(function () {
        $(this).parent().next('.children').slideToggle(300);
        if ($(this).html() == '<i class="fa fa-minus-square-o"></i>') {
            $(this).html('<i class="fa fa-plus-square-o"></i>')
        } else {
            $(this).html('<i class="fa fa-minus-square-o"></i>')
        }
    })

    $('.reply-to-post').click(function () {
        $(this).parent().children('.reply-entry').slideToggle(300);
        if ($(this).html() == 'Write a reply') {
            $(this).html('Cancel reply')
        } else {
            $(this).html('Write a reply')
        }
    })

    //$('.small-link:contains(Share)').click(function (e) {
    //    e.preventDefault();
    //    globalShare('www.facebook.com', 'facebook');
    //});

    // share to Facebook
    $('.facebook-share-button').click(function () {
        FB.ui({ // open a share dialog and pass it the url 
            method: 'share',
            href: theUrl + '?v=' + Math.random(),
        }, function (response) { });
    });

    if (Modernizr.touch) {
        //$('.touch-hide').hide();
        //commented 
        //enables photo selection in IPAD 
    }

    //Event Attend Leave

    var attending = true
    //$('.attend-event').click(function (e) {
    //    e.preventDefault()
    //    attending = !attending
    //    if (attending == false) {
    //        $(this).html('Leave this event')
    //        $('.event-status').html('Attending')
    //    }
    //    if (attending == true) {
    //        $(this).fadeOut(300, function () {
    //            $('.confirm-leave').fadeIn(300);
    //        });
    //    }
    //})


    //$('.leave').click(function () {
    //    $('.attend-event').html('Attend this event')
    //    $('.event-status').html('Not attending')
    //    $('.confirm-leave').fadeOut(300, function () {
    //        $('.attend-event').fadeIn(300);
    //    })
    //})

    $('.cancel-leave').click(function () {
        $('.confirm-leave').fadeOut(300, function () {
            $('#leaveEvent').fadeIn(300);
        })
    })


});


// handle mobile file upload
function mobileUpload() {
    $("#mobile-picker").click();
}


function zen() {
    $('#zenbox_tab').click();
}



function getUserDate(picker) {
    if ($('#startDate').val().length > 0) {
        var theDate = picker.get('select')

        var $end = $('.datepicker-finish').pickadate();
        $('.datepicker-finish').prop('disabled', false)
        var endPicker = $end.pickadate('picker');
        //console.log(theDate.year,theDate.month,theDate.day)
        endPicker.set('min', new Date(theDate.year, theDate.month, theDate.date + 1))
    }
}
var imageOrientation, orient;
// take mobile upload and display on page as DOM element
// authored by http://jsfiddle.net/0GiS0/Yvgc2/
window.onload = function () {
    //Check File API support
    fileManage();


}
//$('.hide-replies').next('.children').slideUp(300);
function fileManage() {
    
    if (window.File && window.FileList && window.FileReader) {
        var filesInput = document.getElementById("mobile-picker");        
        if (filesInput) {
            filesInput.addEventListener("change", function (event) {

                var files = event.target.files; //FileList object
                var output = document.getElementById("the-pledge");
                for (var i = 0; i < files.length; i++) {
                    var file = files[i];
                    //Only pics
                    if (!file.type.match('image') || file.type.match('tif') || file.type.match('gif')) {
                        console.log(file.type);
                        new jBox('Notice', {
                            content: '<strong>Invalid Extension.</strong><br><br>Please enter a valid file extension.<br/>Accepted file types: PNG, JPEG OR BMP',
                            color: 'red',
                            theme: 'NoticeBorder'
                        });
                        $('.webcam-overlay').fadeIn(300);
                        $('#image').fadeOut(300, function () {
                            $('#webcam').show()
                            $('.take-a-photo').show()
                            $('.confirm-photo').hide()
                        })
                        continue;
                    }
                    else {
                        $('.webcam-overlay').fadeOut(300);
                        $('#webcam').fadeOut(300, function () {
                            $('#image').show();
                            var str = $('#image').attr("src");
                            var n = str ? str.lastIndexOf(',') : 0;
                            var result = str.substring(n + 1);

                            //console.log(result);
                            $('#hdnImgSrc').val(result);

                            $('.take-a-photo').hide()
                            $('.confirm-photo').show()
                        })

                        EXIF.getData(file, function () {
                            imageOrientation = (EXIF.getTag(this, "Orientation"));
                            sessionStorage.setItem("orientation", imageOrientation);
                            console.log('image rota' + imageOrientation);
                        });
                        console.log(orient);

                        var picReader = new FileReader();
                        picReader.addEventListener("load", function (event) {

                            var picFile = event.target;

                            var div = document.createElement("div");

                            $('#image').attr("src", picFile.result);
                            usersImage = picFile.result;

                            output.insertBefore(div, null);

                        });

                        //Read the image
                        picReader.readAsDataURL(file);

                        //
                        //send ajax call to log Step2.1 Happy With Photo
                        //step2 field value set to 2
                        logUploadPhoto();
                    }
                }

            });
        }
    }
    else {
        console.log("Your browser does not support File API");
    }
}


$('.hide-replies').click(function () {
    $(this).parent().next('.children').slideToggle(300);
    if ($(this).html() == '<i class="fa fa-minus-square-o"></i>') {
        $(this).html('<i class="fa fa-plus-square-o"></i>')
    } else {
        $(this).html('<i class="fa fa-minus-square-o"></i>')
    }
})

$('.reply-to-post').click(function () {
    $(this).parent().children('.reply-entry').slideToggle(300);
    if ($(this).html() == 'Write a reply') {
        $(this).html('Cancel reply')
    } else {
        $(this).html('Write a reply')
    }
})

// global sharing 
// pass a url as an option e.g. globalShare('http://google.com')
var theUrl;
function globalShare(url, nameOfItem) {

    //debugger;
    //TO DO change url after release 1
    //Now all share links will show home page URL 

    theUrl = url;
    // share to twitter
    $('.twitter-share-button').attr('href', 'https://twitter.com/intent/tweet?text=%23IWILLDO%20&url=' + url);

    // share to google+
    $('.g-plus').attr('href', 'https://plus.google.com/share?url=' + url + '&v=' + Math.random());

    // set title 
    $('#sharedItem').html(nameOfItem)

    // open lightbox
    $('.share').addClass('is-open');
}


// note to cybage: use these for errors and messaging

// FACEBOOK CONNECT FAILED
//  new jBox('Notice', {
//      content: '<strong>Oops.</strong><br><br>Facebook connect failed. Please try again.',
//      color: 'red',
//      theme: 'NoticeBorder'
//  });

// NON-MATCHING EMAIL ENTERED FOR FORGOTTEN PASSWORD
//  new jBox('Notice', {
//      content: '<strong>Oops.</strong><br><br>An account using that email address does not exist.',
//      color: 'red',
//      theme: 'NoticeBorder'
//  });

// PASSWORD RESET: EMAIL SENT WITH RESET LINK
//  new jBox('Notice', {
//      content: '<strong>Check your email.</strong><br><br>An email has been sent with a link to reset your password.',
//      color: 'green',
//      theme: 'NoticeBorder'
//  });

// PASSWORD SUCCESFULLY RESET (DASHBOARD PAGE IS NOW VISIBLE)
//  new jBox('Notice', {
//      content: '<strong>Success.</strong><br><br>Your password was reset and you have been logged in.',
//      color: 'green',
//      theme: 'NoticeBorder'
//  });
function logUploadPhoto() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        type: 'POST',
        url: '/umbraco/surface/Pledge/LogHappyWithPhoto',
        success: function (data) {
            console.log(data);
        },
        error: function (data) {
            //  alert('error');
        }
    });
}

function logAnotherPhoto() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        type: 'POST',
        url: '/umbraco/surface/Pledge/LogAnotherPhoto',
        success: function (data) {
            console.log(data);
        },
        error: function (data) {
            //  alert('error');
        }
    });
}



function DIPPre() 
{
    startLoad();
    setTimeout(function () {        
        DefaultImgProcessing();
    }, 500)
}




function DefaultImgProcessing()
{
   
    var orientation = sessionStorage.getItem("orientation") ? sessionStorage.getItem("orientation") : 0;

    
    $.ajax({
        url: $('#hdnDefaultmode').val(),
        data: orientation,
        type: 'POST',
        
        success: function (data) {
            if (data.Message.indexOf('error') > -1) {
                new jBox('Notice', {
                    content: '<strong>Oops.</strong><br><br>Something went wrong. Please try again.',
                    color: 'red',
                    theme: 'NoticeBorder'
                });
                sessionStorage.setItem('backPressed', true);
                window.location.href = "/enter-your-goal";                
            }
            else {
                sessionStorage.setItem('imageUrl', data.ImagePath);
                sessionStorage.setItem('textToBeSentToYolk', data.TextToBeSentToYolk);
                window.location.href = "/confirm-goal/";               
            }
        }
    })
}
