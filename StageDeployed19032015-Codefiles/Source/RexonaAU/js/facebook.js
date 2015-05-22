//facebook connect code
var fbAppId = $('#hdnFbAppId').val();
//pick AppId from web.config file
var mode = '';
var friends;

var isFbInit = false;
$(document).ready(function () {

    $('#fb-terms').change(function () {
        if (this.checked) {
            $('#fb-terms').parent().find('.error').hide();
            $('#fb-terms').parent().find('span').css('color', '#181818');
        }
        else {
            $('#fb-terms').parent().find('.error').css('display', 'block');
            $('#fb-terms').parent().find('span').css('color', '#cb4444');
        }
    });

    $('.fb-button').click(function (e) {
        e.preventDefault();
        if ($('#fb-terms').length) {
            if ($('#fb-terms').is(':checked')) {
                facebookConnect();
            }
            else {
                $('#fb-terms').parent().find('.error').css('display', 'block');
                $('#fb-terms').parent().find('span').css('color', '#cb4444');
                return false;
            }
        }
        else {
            facebookConnect();
            return false;
        }

    });
});

function facebookConnect() {
    sessionStorage.setItem('fbuser', 'facebookMemberUser');
    if ($(this).is($('.sign-in'))) {
        mode = 'signin';
    }
    else {
        mode = 'signup';
    }

    FB.getLoginStatus(function (response) {
        isFbInit = true;

    });

    if (isFbInit) {
        fb_login();
    }
    else {
        fb_init();
    }

}


function fb_init() {

    FB.init({
        appId: fbAppId,
        xfbml: true,
        version: 'v2.0'
    });

    fb_login();
}

function fb_login() {
    FB.login(function (response) {
       
        if (response.authResponse) {
            checkLoginState();
        } else {
        }
    }, {
        scope: 'publish_stream,email,user_friends'
    });
}


function checkLoginState() {
    FB.getLoginStatus(function (response) {
        statusChangeCallback(response);

    });
}

function statusChangeCallback(response) {

    // The response object is returned with a status field that lets the
    // app know the current login status of the person.
    // for FB.getLoginStatus().

    if (response.status === 'connected') {
        fillFromAPI();
    }
    else {
        callError('You weren’t connected with Facebook. Please try again, or try signing up manually');
    }
}
function fillFromAPI() {
    var fbresponse;
    FB.api('/me', function (response) {
        //console.log(response);
        var useremail = response.email;
        fbresponse = response;

        FB.api('me/friends?fields=name,picture', function (resp) {
            //console.log(resp);
            sessionStorage.setItem('friends', JSON.stringify(resp.data))
            FB.api('/me?fields=age_range', function (responseage) {
                if (responseage) {
                    if (responseage.age_range) {
                        if (responseage.age_range.min >= 18) {

                            if (sessionStorage.getItem('fbuser') == 'facebookMemberUser' && sessionStorage.getItem('invite') != 'true') {
                                facebookLogin(fbresponse);
                            }
                            else {
                                console.log(FB.getAuthResponse(["accessToken"]) == undefined);
                                callInvite();
                                
                                sessionStorage.setItem('invite', 'false');
                            }
                            
                        }
                        else {
                            callError('You need to be above 18 years of age to continue.');
                        }
                    }
                }
            });
        });


    });


}

/*function checkExistingUser(fbresponse) {
    var data = { 'email': fbresponse.email };
    $.ajax({
        url: '/umbraco/surface/LogOn/CheckExistingUser',
        type: 'POST',//JSON.stringify(details),
        dataType: 'json',
        data: JSON.stringify(data),
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            var dataToSend = new Object();
            if (data == 'True') {
                //user exists call signin..
                dataToSend.EmailAddress = fbresponse.email;
                dataToSend.FacebookConnect = true;
                signIn(dataToSend);
            }
            else {
                //new user call signup..
                
                console.log(fbresponse);
                dataToSend.FirstName = fbresponse.first_name;
                dataToSend.LastName = fbresponse.last_name;
                dataToSend.DisplayName = fbresponse.first_name + ' ' + fbresponse.last_name;
                dataToSend.Email = fbresponse.email;
                dataToSend.FacebookConnect = true;

                signUp(dataToSend);
            }
        }
    });
}*/

function facebookLogin(fbresponse) {
    var dataToSend = new Object();
    dataToSend.FirstName = fbresponse.first_name;
    dataToSend.LastName = fbresponse.last_name;
    dataToSend.DisplayName = fbresponse.first_name + ' ' + fbresponse.last_name;
    dataToSend.Email = fbresponse.email;
    dataToSend.FacebookId = fbresponse.id;
    dataToSend.Referrer = window.location.href.indexOf('login') > -1 ? 'login' : 'register';
    dataToSend.Subscribe = $('#fb-subscribe').is(':checked') ? 1 : 0;
    $.ajax({
        url: '/umbraco/surface/LogOn/fbLogin',
        type: 'POST',//JSON.stringify(details),
        dataType: 'json',
        data: JSON.stringify(dataToSend),
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            console.log(data);

            switch (data) {
                case "Success":
                    //var IsJoined = JSON.parse(sessionStorage.getItem('IsJoined'));
                    //var IsInvited = JSON.parse(sessionStorage.getItem('FbInvite'));
                    //var returnBackToMakeAPledge = JSON.parse(sessionStorage.getItem("returnBackToMakeAPledge"));

                    //if (IsJoined || IsInvited || returnBackToMakeAPledge) {
                    //    sessionStorage.setItem("returnBackToMakeAPledge", false);
                    //    window.location.href = "/enter-your-goal"
                    //}
                    //else {
                    //    window.location.href = "/dashboard/";
                    //}
                    window.location.href = "/dashboard/";
                    break;
                case "duplicateemailId":
                    new jBox('Notice', {
                        content: '<strong>Error.</strong><br><br>An account with the entered Email already exists.<br>Please Sign In to continue.',
                        color: 'red',
                        theme: 'NoticeBorder'
                    });
                    break;
                case "SignUpSuccess":
                    window.location.href = "/dashboard/";
                    break;
                case "error":
                    new jBox('Notice', {
                        content: '<strong>Oops.</strong><br><br>Something went wrong. Please try again.',
                        color: 'red',
                        theme: 'NoticeBorder'
                    });
                    break;
                case "PleaseSignUp":
                    new jBox('Notice', {
                        content: '<strong>Error.</strong><br><br>User does not exist. Please Sign Up to continue.',
                        color: 'red',
                        theme: 'NoticeBorder'
                    });
                    break;

            }

        },
        error: function (message) {
            console.log('.eerrr..');
        }
    });

}

