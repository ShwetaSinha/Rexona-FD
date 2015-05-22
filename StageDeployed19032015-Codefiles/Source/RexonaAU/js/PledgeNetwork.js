var queryString = '';
var pledgeViewModel;
var currentPageIndex = 0, myCurrentPageIndex = 0, followedCurrentPageIndex = 0, sharedCurrentPageIndex = 0;
var requestPending = false, myRequestPending = false, followedRequestPending = false, sharedRequestPending = false;
var totalPages, myTotalPages, followedTotalPages, sharedTotalPages;
var existingItems = [];
var myExistingItems = [];
var followedExistingItems = [];
var sharedExistingItems = [];
var PageSize = 4, SharedPageSize = 8;
var myFollowedDiscussionIds = [], myDiscussionIds = [], sharedArticles = [];

$(document).ready(function () {

    $(document).on('click', '.reply-entry button', function (event) {
        //event.preventDefault();
        AddReply($(this).attr('id'));
    });

    if (Modernizr.touch) {
        //Fire Touch Events
    }
    //Apply lazy loading for individual divs
    $('#discussions-all .entity-list').scroll(function (e) {
        var eachChildLength = $('#discussions-all .discussion-in-list').height();
        var lengthToCallScroll = eachChildLength * (2 + (4 * currentPageIndex));
        if (!requestPending && (currentPageIndex + 1 < totalPages)) {
            if ($('#discussions-all .entity-list').scrollTop() > lengthToCallScroll) {
                currentPageIndex = currentPageIndex + 1;
                getDiscussions(currentPageIndex);
            }

        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    $('#discussions-my-posts .entity-list').scroll(function (e) {
        var eachChildLength = $('#discussions-my-posts .discussion-in-list').height();
        var lengthToCallScroll = eachChildLength * (2 + (4 * myCurrentPageIndex));
        if (!myRequestPending && (myCurrentPageIndex + 1 < myTotalPages)) {
            if ($('#discussions-my-posts .entity-list').scrollTop() > lengthToCallScroll) {
                myCurrentPageIndex = myCurrentPageIndex + 1;
                getMyDiscussions(myCurrentPageIndex);
            }

        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    $('#discussions-following .entity-list').scroll(function (e) {
        var eachChildLength = $('#discussions-following .discussion-in-list').height();
        var lengthToCallScroll = eachChildLength * (2 + (4 * followedCurrentPageIndex));
        if (!followedRequestPending && (followedCurrentPageIndex + 1 < followedTotalPages)) {
            if ($('#discussions-following .entity-list').scrollTop() > lengthToCallScroll) {
                followedCurrentPageIndex = followedCurrentPageIndex + 1;
                getFollowedDiscussions(followedCurrentPageIndex);
            }

        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });
    
    //Check if user is logged in
    if (JSON.parse($('#hdnIsLoggedIn').val())) {
        $('#DiscussionPost').parent().show();
        $('.discussions-actions, #shareGoal, #imgStartBadge, #imgSharedBadge, #imgEndBadge, a[href="#discussions-following"], a[href="#discussions-my-posts"]').show();
        

        //Check if user connected using fb
        if (JSON.parse($('#hdnIsConnectedUsingFb').val())) {
            $('#facebookTab').parent().show();
        }
    }

    if (JSON.parse($('#hdnIsMemberofGroup').val()) && JSON.parse($('#hdnIsLoggedIn').val())) {
        $('.pill-meta .primary-meta').show();
        $('#createEvent,#statusEvent,#JoinEvent,#leaveEvent,a[href="#my-events"]').show();
    }
    if (JSON.parse($('#hdnIsConnectedUsingFb').val()) || !JSON.parse($('#hdnIsLoggedIn').val())) {
        $('a[href="#near-me"]').hide();
    }



    $(document).on('click', '.entity-name a', function () {
        var discussionId = $(this).parents('.discussion-in-list').find('.discussions-actions').attr('id');
        BindReplies(discussionId);

    });

    $(document).on('click', '#discussionReply', function () {
        var discussionId = $(this).parent().attr('id');
        BindReplies(discussionId);
    });

    pledgeViewModel = new PledgeModel();
    ko.applyBindings(pledgeViewModel);


    //Get All discussions
    currentPageIndex = 0;
    getDiscussions(currentPageIndex);

    //Get My Followed discussions
    followedCurrentPageIndex = 0;
    getFollowedDiscussions(followedCurrentPageIndex);

    //Get My Discussions
    myCurrentPageIndex = 0;
    getMyDiscussions(myCurrentPageIndex);

    //Get Shared Articles
    sharedCurrentPageIndex = 0;
    getSharedArticles(sharedCurrentPageIndex);

    //Below line needs to be removed
    $('#postAvatarImage').css('height', '50px');

    // Provide badges to the end user 
    if (JSON.parse($('#hdnStartBadge').val())) {
        $('#imgStartBadge').removeClass('inactive');
    }

    if (JSON.parse($('#hdnSharedBadge').val())) {
        $('#imgSharedBadge').removeClass('inactive');
    }

    if ($('#hdnEndBadge').val() == 'hide') {
        $('#imgEndBadge').hide();
    }
    else if (JSON.parse($('#hdnEndBadge').val())) {
        $('#imgEndBadge').show().removeClass('inactive');
    }

    //Share pledge
    $('#shareGoal').click(function () {
        var pathToShare = window.location.href;
        globalShare(pathToShare, 'pledge ');

        var dataToSendToShare = new Object();
        dataToSendToShare.pledgeId = $('#hdnPledgeId').val();

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToShare,
            type: 'GET',
            url: '/umbraco/surface/Pledge/SharePledge',
            success: function (data) {
                $('#imgSharedBadge').removeClass('inactive');
            },
            error: function (data) {
                console.log('error while sharing pledge');
            }

        });

    });

    //Show Join and Hide joined link
    if (JSON.parse($('#hdnIsMemberofGroup').val())) {
        $('#joinedGoal').show();
        $('#joinGoal').hide();
    }
    else {
        $('#joinedGoal').hide();
        if($('#hdnPledgeType').val() != 'Close'){
            $('#joinGoal').show();
        }
    }

    //Join functionality
    $('#joinGoal').click(function () {

        var pledgeId = $('#hdnPledgeId').val();

        var dataToSendToJoin = new Object();
        dataToSendToJoin.NodeId = "" + pledgeId + "";

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToJoin,
            type: 'GET',
            url: '/umbraco/surface/Pledge/JoinPledge',
            success: function (data) {

                sessionStorage.setItem('IsJoined', true);
                sessionStorage.setItem('PledgeTitle', data.PledgeTitle);
                sessionStorage.setItem('PledgeId', data.PledgeId);

                if (data.IsMemberLoggedIn) {
                    //window.location.href = "/enter-your-goal/";
                    window.location.href = "/enter-your-goal";
                }
                else {
                    window.location.href = "/sign-up/";
                }
            },
            error: function (data) {
                console.log('error');
            }

        });
    })

    //Post a discussion
    $('#DiscussionPost').unbind('click').click(function () {

        var dataToSendToAjax = new Object();
        if ($.trim($('#discussion-title').val()) != '' && $('#discussion-title').parents('.discussions-listing-start-discussion').find('#titleError').css('display') != "block") {
            startLoad();
            $('#discussions-all .entity-list').scrollTop(0);
            $('#discussions-my-posts .entity-list').scrollTop(0);
            dataToSendToAjax.discussionTitle = $('#discussion-title').val();
            dataToSendToAjax.discussionBody = $('#discussion-body').val();
            dataToSendToAjax.pledgeId = $('#hdnPledgeId').val();

            $.ajax({
                contentType: "application/json; charset=utf-8",
                dataType: 'json',
                cache: false,
                data: dataToSendToAjax,
                type: 'GET',
                url: '/umbraco/surface/Discussion/CreateDiscussion',
                success: function (data) {
                    $('#discussion-title').val("");
                    $('#discussion-body').val("");
                    currentPageIndex = 0;
                    existingItems = [];
                    getDiscussions(currentPageIndex);

                    myCurrentPageIndex = 0;
                    myExistingItems = [];
                    getMyDiscussions(myCurrentPageIndex);
                    endLoad();
                },
                error: function (data) {
                    console.log('error');
                }

            });
        }
    });


    //HideShowLogicFor All/FB friends
    $('#facebookTab').html('Facebook (' + $('#members-facebook li').length + ')');
    if ($('#members-all li').length > 5) {
        $('.secondary-meta').show();
        $('#members-all li').hide().slice(0, 5).show();
        $('#members-facebook li').hide().slice(0, 5).show();
    }
    if (!$('#members-facebook ul li').length) {
        $('#members-facebook ul').html('<span style="color:grey"><i>No members present</i></span>');
    }
    $('.secondary-meta').click(function (e) {
        e.preventDefault();
        $('#members-all li, #members-facebook li').show();
        $(this).hide();
    });


    //Facebook invite
    var isFbInit = false;
    $('#openInvite').click(function (e) {
        e.preventDefault();
        if (sessionStorage.getItem('fbuser') == 'facebookMemberUser') {
            callInvite();
        }
        else {
            sessionStorage.setItem('invite', 'true');

            FB.getLoginStatus(function (response) {
                isFbInit = true;

            });

            if (isFbInit) {
                console.log('logged');
                fb_login();
            }
            else {
                console.log('not logged');
                fb_init();
            }
        }
    });

    $('#send-invite').on('valid.fndtn.abide', Foundation.utils.throttle(function (e) {

        if ($('#hdnMemberEmail').val() != $('input[type=email]').val()) {
            var current = $(this);
            var fbData = new Object();
            fbData.memberId = $('#hdnMemberId').val();
            fbData.title = $('.pledge-title h1').text();
            fbData.pledgeId = $('#hdnPledgeId').val();
            fbData.pledgeType = $('#hdnPledgeType').val();
            fbData.fbInvite = true;
            fbData.type = 'email';
            $.ajax({
                url: '/umbraco/surface/FacebookMember/getQuerystring',
                type: 'POST',//JSON.stringify(details),
                dataType: 'json',
                data: JSON.stringify(fbData),
                contentType: 'application/json; charset=utf-8',
                success: function (data) {
                    //console.log('querystring:' + data);
                    queryString = data.querystring;
                    
                    var emailData = new Object();
                    emailData.memberName = $('#hdnMemberName').val();
                    emailData.pledgeTitle = $('.pledge-title h1').text();
                    emailData.emailAddress = $('input[type=email]').val();
                    emailData.message = $('#invite-message').val();
                    emailData.URLtoShare = window.location.protocol + '//' + window.location.host + '?qinvite=' + queryString;
                    console.log(emailData.URLtoShare);
                    $.ajax({
                        url: '/umbraco/surface/FacebookMember/SendInviteEmail',
                        type: 'POST',//JSON.stringify(details),
                        dataType: 'json',
                        data: JSON.stringify(emailData),
                        contentType: 'application/json; charset=utf-8',
                        success: function (data) {
                            console.log(data);
                            var status = $(current).parents('.current-card').parent().find('.next-card');
                            $(status).find('.status strong').text($('input[type=email]').val());
                            runCards();
                            $('input[type=email]').val('');
                            $('#invite-message').val('');

                        }
                    });
                }
            });
            
        }
        else {

            new jBox('Notice', {
                content: '<strong>Sorry.</strong><br><br>You cannot invite yourself to this pledge.<br>Try using another Email.',
                color: 'red',
                theme: 'NoticeBorder'
            });
        }

    }, 300));

})

function BindReplies(discussionId) {
    var dataToSendToAjax = new Object();
    dataToSendToAjax.DiscussionId = discussionId;

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: dataToSendToAjax,
        type: 'GET',
        url: '/umbraco/surface/Discussion/DiscussionDetails',
        success: function (data) {
            if (data != null && data != undefined) {
                $('#DiscussionTitle').text(data.DiscussionTitle);
                $('#DiscussionDescription').text(data.DiscussionDescription);
                $('#DiscussionPostDateTime').text(data.CreatedDate);
                $('#DiscussionAuthor').html('<strong>' + data.CreatedBy + '</strong> said');
                $('.op-reply button').attr('id', discussionId);

                var replies = $.map(data.Replies, function (item) { return new Reply(item); });
                pledgeViewModel.Replies(replies);

                if (JSON.parse($('#hdnIsLoggedIn').val())) {
                    $('.reply-entry button, .reply-to-post').show();
                }

                setTimeout(function () {
                    $('.hide-replies').click(function () {
                        $(this).parent().next('.children').slideToggle(300);
                        if ($(this).html() == '<i class="fa fa-minus-square-o"></i>') {
                            $(this).html('<i class="fa fa-plus-square-o"></i>')
                        } else {
                            $(this).html('<i class="fa fa-minus-square-o"></i>')
                        }
                    });

                    $('.reply-to-post').click(function () {
                        $(this).parent().children('.reply-entry').slideToggle(300);
                        if ($(this).html() == 'Write a reply') {
                            $(this).html('Cancel reply')
                        } else {
                            $(this).html('Write a reply')
                        }
                    });

                    $(document).foundation();

                }, 1000);



                if (data.Replies != undefined && data.Replies.length == 0) {
                    $('#foreachReplies').html('<span id="spnForeachReplies" style="margin: 200px 47px;">No Conversation/Replies Found.</span>');
                    openLightbox('.discussion');
                    return;
                }

                $('#spnForeachReplies').hide();
                openLightbox('.discussion');
            }
        },
        error: function (data) {
            console.log('error');
        }

    });
}

function AddReply(parentId) {
    if ($.trim($('button[id="' + parentId + '"]').parents('.reply-entry').find('#discussions-reply').val()) != "" && $('button[id="' + parentId + '"]').parents('.reply-entry').find('#swearError').css('display') != "block") {
        var dataToSendToAjax = new Object();
        dataToSendToAjax.ParentId = parentId;
        dataToSendToAjax.ReplyText = $('button[id="' + parentId + '"]').parents('.reply-entry').find('#discussions-reply').val();

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Discussion/AddReply',
            success: function (data) {
                if (data != null && data != undefined) {
                    $('button[id="' + parentId + '"]').parents('.reply-entry').find('#discussions-reply').val("");
                    BindRepliesAgain();
                }
            },
            error: function (data) {
                console.log('error');
            }

        });
    }
    else {
        //TODO: Show error messages
        //$('button[id="' + parentId + '"]').parents('.reply-entry').find('.error').show().css('display', 'block');
        return false;
    }

    return false;
}

function BindRepliesAgain() {
    var parentId = $('.op-reply button').attr('id');
    var dataToSendToAjax = new Object();
    dataToSendToAjax.DiscussionId = parentId;

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: dataToSendToAjax,
        type: 'GET',
        url: '/umbraco/surface/Discussion/DiscussionDetails',
        success: function (data) {
            if (data != null && data != undefined) {
                $('#DiscussionTitle').text(data.DiscussionTitle);
                $('#DiscussionDescription').text(data.DiscussionDescription);
                $('#DiscussionPostDateTime').text(data.CreatedDate);
                $('#DiscussionAuthor').html('<strong>' + data.CreatedBy + '</strong> said');
                $('.op-reply button').attr('id', parentId);

                var replies = $.map(data.Replies, function (item) { return new Reply(item); });
                pledgeViewModel.Replies(replies);

                $('.reply-entry button, .reply-to-post').show();

                setTimeout(function () {
                    $('.hide-replies').unbind('click').click(function () {
                        $(this).parent().next('.children').slideToggle(300);
                        if ($(this).html() == '<i class="fa fa-minus-square-o"></i>') {
                            $(this).html('<i class="fa fa-plus-square-o"></i>')
                        } else {
                            $(this).html('<i class="fa fa-minus-square-o"></i>')
                        }
                    });

                    $('.reply-to-post').unbind('click').click(function () {
                        $(this).parent().children('.reply-entry').slideToggle(300);
                        if ($(this).html() == 'Write a reply') {
                            $(this).html('Cancel reply')
                        } else {
                            $(this).html('Write a reply')
                        }
                    });

                    $(document).foundation();

                }, 1000);

                $('#spnForeachReplies').hide();
            }
        },
        error: function (data) {
            console.log('error');
        }
    });
}

function getFriends() {
    if (sessionStorage.getItem('friends') != 'undefined' && sessionStorage.getItem('friends') != undefined) {
        var members = sessionStorage.getItem('friends');
        var result;
        members = JSON.parse(members);
        if (members.length > 0) {
            $('#invite-facebook ul').html('');

            $.each(members, function (index) {

                var text = '<li class="user-fb" id="' + members[index].id + '">' +
                                '<div class="entity-image entity-block hide-for-small">' +
                                    '<img src="' + members[index].picture.data.url + '" alt="">' +
                                '</div>' +
                                '<div class="entity-details entity-block">' +
                                    '<div class="entity-name">' +
                                        '<p>' + members[index].name + '</p>' +
                                    '</div>' +
                                '</div>' +
                                '<div class="entity-action entity-block">' +
                                    '<a onclick="/*runCards()*/" class="action constructive invite-fbfriend">Invite</a>' +
                                '</div>' +
                                        '</li>';

                $('#invite-facebook ul').append(text);
            });
        }
        else {
            $('#invite-facebook ul').text('No Data found');
        }

    }
    else {
        FB.api('me/friends?fields=name,picture', function (resp) {
            sessionStorage.setItem('friends', JSON.stringify(resp.data));


            if (resp.error && !requestsent) {
                sessionStorage.setItem('invite', 'true');
                fb_login();
            }
            else {
                getFriends();
            }
        });
    }

}

function callInvite() {
    var fbData = new Object();
    fbData.memberId = $('#hdnMemberId').val();
    fbData.title = $('.pledge-title h1').text();
    fbData.pledgeId = $('#hdnPledgeId').val();
    fbData.pledgeType = $('#hdnPledgeType').val();
    fbData.fbInvite = true;
    fbData.type = 'fb';
    $.ajax({
        url: '/umbraco/surface/FacebookMember/getQuerystring',
        type: 'POST',//JSON.stringify(details),
        dataType: 'json',
        data: JSON.stringify(fbData),
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            //console.log('querystring:' + data);
            queryString = data.querystring;
            var pledgeUrl = window.location.protocol + '//' + window.location.host;//+'/make-a-pledge/step-1';
            //var url = pledgeUrl.slice(0, -1) + '?query1=777&query2=6655&fbInvite=true';
            var url = pledgeUrl + '?qinvite=' + queryString;
            console.log(url);
            //console.log(url);
            FB.ui({
                method: 'send',
                link: url,
            }, function (response) {
                console.log(response.success == true);
            });
        }
    });
    
}

//For Lazy Loading
$(window).scroll(function (e) {
    if (!sharedRequestPending && (sharedCurrentPageIndex + 1 < sharedTotalPages)) {
        if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
            sharedCurrentPageIndex = sharedCurrentPageIndex + 1;
            getSharedArticles(sharedCurrentPageIndex);
        }
    } else {
        e.preventDefault();
        e.stopPropagation();
    }
});


function getInviteLink() {
    //Invite feature query string url

    var fbData = new Object();
    fbData.memberId = $('#hdnMemberId').val();
    fbData.title = $('.pledge-title h1').text();
    fbData.pledgeId = $('#hdnPledgeId').val();
    fbData.pledgeType = $('#hdnPledgeType').val();
    fbData.fbInvite = true;

    $.ajax({
        url: '/umbraco/surface/FacebookMember/getQuerystring',
        type: 'POST',//JSON.stringify(details),
        dataType: 'json',
        data: JSON.stringify(fbData),
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            //console.log('querystring:' + data);
            queryString = data.querystring;
        }
    });
}

//Get All discussions
function getDiscussions(pageIndex) {
    if (!requestPending) {
        requestPending = true;

        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.pledgeId = $('#hdnPledgeId').val();

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Discussion/GetDiscussions',
            success: function (data) {
                if (data.Message == "error" && pageIndex == 0) {
                    $('#discussions-all .entity-list').html("No Records Found.");
                    requestPending = false;
                    totalPages = 0;
                    return;
                }

                if (data.Message == "success" && pageIndex == 0) {
                    $('#discussions-all .entity-list').html("");
                }
                var userDiscussions = $.map(data.Discussions, function (item) { return new Discussion(item); });
                existingItems = existingItems.concat(userDiscussions);
                pledgeViewModel.Discussions(existingItems);

                requestPending = false;
                totalPages = data.totalPages;

                setTimeout(function () {
                    //Follow a discussion
                    $('[Id$="discussionFollow"]').unbind('click').click(function () {
                        var dataToSendToAjax = new Object();
                        startLoad();
                        var parentId = $(this).parent().attr('id');
                        dataToSendToAjax.discussionId = $(this).parent().attr('id');
                        dataToSendToAjax.memberId = JSON.parse($('#hdnMemberId').val());

                        $.ajax({
                            contentType: "application/json; charset=utf-8",
                            dataType: 'json',
                            cache: false,
                            data: dataToSendToAjax,
                            type: 'GET',
                            url: '/umbraco/surface/Discussion/FollowDiscussion',
                            success: function (data) {
                                $('#' + parentId + ' #discussionFollow').hide();
                                $('#' + parentId + ' #discussionFollowing').show().css('cursor', 'default').unbind('click');

                                //Call followed Ajax again
                                followedCurrentPageIndex = 0;
                                followedExistingItems = [];
                                getFollowedDiscussions(followedCurrentPageIndex);
                                endLoad();
                            },
                            error: function (data) {
                                alert('error');
                            }

                        });

                    });
                }, 1000);

                ShowMyDiscussionFollowingLinks();
                HideMyDiscussionFollowLinks();

            },
            error: function (data) {
                console.log('error');
            }

        });
    }
}

//Get Discussions followed by SignedIn User
function getFollowedDiscussions(pageIndex) {

    if (!followedRequestPending) {
        followedRequestPending = true;
    }
    var dataToSendToAjax = new Object();
    dataToSendToAjax.PageSize = PageSize;
    dataToSendToAjax.currentPageIndex = followedCurrentPageIndex;
    dataToSendToAjax.pledgeId = $('#hdnPledgeId').val();
    dataToSendToAjax.memberId = JSON.parse($('#hdnMemberId').val());

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: dataToSendToAjax,
        type: 'GET',
        url: '/umbraco/surface/Discussion/MyFollowedDiscussions',
        success: function (data) {

            if (data.Message == "error" && pageIndex == 0) {
                $('#discussions-following .entity-list').html("No Records Found.");
                followedTotalPages = 0;
                return;
            }

            if (data.Message == "success" && pageIndex == 0) {
                $('#discussions-following .entity-list').html("");
            }
            var myFollowedDiscussions = $.map(data.MyFollowedDiscussions, function (item) { return new Discussion(item); });
            followedExistingItems = followedExistingItems.concat(myFollowedDiscussions);
            pledgeViewModel.FollowedDiscussions(followedExistingItems);

            myFollowedDiscussionIds = data.FollowedDiscussionIds;
            HideMyDiscussionFollowLinks();
            ShowMyDiscussionFollowingLinks();
            followedRequestPending = false;
            followedTotalPages = data.totalPages;
        },
        error: function (data) {
            console.log('error');
        }

    });
}

//Get My Discussions
function getMyDiscussions(pageIndex) {

    if (!myRequestPending) {
        myRequestPending = true;

        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = myCurrentPageIndex;
        dataToSendToAjax.pledgeId = $('#hdnPledgeId').val();

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Discussion/MyDiscussions',
            success: function (data) {
                if (data.Message == "error" && pageIndex == 0) {
                    $('#discussions-my-posts .entity-list').html("No Records Found.");
                    myRequestPending = false;
                    myTotalPages = 0;
                    return;
                }
                if (data.Message == "success" && pageIndex == 0) {
                    $('#discussions-my-posts .entity-list').html("");
                }
                var myDiscussions = $.map(data.MyDiscussions, function (item) { return new Discussion(item); });
                myExistingItems = myExistingItems.concat(myDiscussions);
                pledgeViewModel.MyDiscussions(myExistingItems);
                myDiscussionIds = data.MyDiscussionIds;
                HideMyDiscussionFollowLinks();
                ShowMyDiscussionFollowingLinks();
                myRequestPending = false;
                myTotalPages = data.totalPages;
            },
            error: function (data) {
                console.log('error');
            }

        });
    }
}

//get Shared Articles
function getSharedArticles(pageIndex) {

    if (!sharedRequestPending) {
        sharedRequestPending = true;

        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = SharedPageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.pledgeId = $('#hdnPledgeId').val();

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Discussion/GetSharedArticles',
            success: function (data) {
                if (data.SharedArticles.length == 0 && pageIndex == 0) {
                    $('.content-panel .large-6').html('<h2 class="contained-title inverted">Shared Articles</h2><h3>No Articles Shared.</h3>');
                    sharedRequestPending = false;
                    sharedTotalPages = 0;
                    return;
                }

                var sharedArticles = $.map(data.SharedArticles, function (item) { return new article(item); });
                sharedExistingItems = sharedExistingItems.concat(sharedArticles);
                pledgeViewModel.SharedArticles(sharedExistingItems);
                sharedRequestPending = false;
                sharedTotalPages = data.totalPages;

                setTimeout(function () {
                    //Reload masonry plugin
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');

                    setTimeout(function () {
                        $('.item-share').unbind('click').click(function () {
                            var urlToShare = window.location.protocol + '//' + window.location.host + $(this).parents('.item-content').find('.item-heading a').attr('href');
                            globalShare(urlToShare, 'article ');
                        });


                        $('.rating-icon').unbind('click').click(function () {
                            if ($(this).attr('id') != undefined) {
                                likeCount("like", $(this).attr('Id'));
                            }
                        });

                        markLikes();

                    }, 500);

                }, 500);

                $('.full-on-mobile').load(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                });
            },
            error: function (data) {
                console.log('error in function getSharedArticles');
            }

        });
    }
}

//Hide follow links for My discussions
function HideMyDiscussionFollowLinks() {
    if (myDiscussionIds != undefined && myDiscussionIds.length > 0) {
        $.each(myDiscussionIds, function (index, value) {
            $('#' + value + ' #discussionFollow').hide();
            $('#' + value + ' #discussionFollowing').hide();
        });
    }
}

//Change Follow to Following for discussions followed by user
function ShowMyDiscussionFollowingLinks() {
    if (myFollowedDiscussionIds != undefined && myFollowedDiscussionIds.length > 0) {
        $.each(myFollowedDiscussionIds, function (index, value) {
            $('#' + value + ' #discussionFollow').hide();
            $('#' + value + ' #discussionFollowing').show().css('cursor', 'default').unbind('click');
        });
    }
}

//Create Pledge Model function for View Model
function PledgeModel() {
    var self = this;
    self.Discussions = ko.observableArray([]);
    self.MyDiscussions = ko.observableArray([]);
    self.FollowedDiscussions = ko.observableArray([]);
    self.Replies = ko.observableArray([]);
    self.AllEvents = ko.observableArray([]);
    self.MyEvents = ko.observableArray([]);
    self.NearEvents = ko.observableArray([]);
    self.SharedArticles = ko.observableArray([]);
}

//Create Discussion Model for Knockout binding
function Discussion(data) {

    this.Id = ko.computed(function () {
        return data.ID;
    });
    this.Title = ko.computed(function () {
        return data.Title;
    });
    this.PostedBy = ko.computed(function () {
        return "Posted by " + data.PostedBy;
    });
    this.PostedDate = ko.computed(function () {
        return data.PostedDateTimeAsString;
    });
    this.PostedByAvatar = ko.computed(function () {
        return data.PostedByAvatar;
    });
}


//Create Reply model for knockout binding
function Reply(data) {
    this.Id = ko.computed(function () {
        return data.Id;
    });
    this.ReplyText = ko.computed(function () {
        return data.ReplyText;
    });
    this.PostedBy = ko.computed(function () {
        return "<strong>" + data.PostedBy + "</strong> said:";
    });
    this.PostedDate = ko.computed(function () {
        return data.PostedDateAsString;
    });
    this.PostedByAvatar = ko.computed(function () {
        return data.PostedByAvatar;
    });

    if (data.SecondLevelReplies != null) {
        this.SecondLevelReplies = ko.observableArray($.map(data.SecondLevelReplies, function (item) { return new SecondLevelReply(item); }));
        this.ShowSecondLevel = true;
    }
    else {
        this.SecondLevelReplies = ko.observableArray([]);
        this.ShowSecondLevel = false;
    }
}

function SecondLevelReply(data) {

    this.Id = ko.computed(function () {
        return data.Id;
    });
    this.ReplyText = ko.computed(function () {
        return data.ReplyText;
    });
    this.PostedBy = ko.computed(function () {
        return "<strong>" + data.PostedBy + "</strong> said:";
    });
    this.PostedDate = ko.computed(function () {
        return data.PostedDateAsString;
    });
    this.PostedByAvatar = ko.computed(function () {
        return data.PostedByAvatar;
    });

    if (data.SecondLevelReplies != null) {
        this.ThirdLevelReplies = ko.observableArray($.map(data.SecondLevelReplies, function (item) { return new ThirdLevelReply(item); }));
        this.ShowThirdLevel = true;
    }
    else {
        this.ThirdLevelReplies = ko.observableArray([]);
        this.ShowThirdLevel = false;
    }
}

function ThirdLevelReply(data) {

    this.Id = ko.computed(function () {
        return data.Id;
    });
    this.ReplyText = ko.computed(function () {
        return data.ReplyText;
    });
    this.PostedBy = ko.computed(function () {
        return "<strong>" + data.PostedBy + "</strong> said:";
    });
    this.PostedDate = ko.computed(function () {
        return data.PostedDateAsString;
    });
    this.PostedByAvatar = ko.computed(function () {
        return data.PostedByAvatar;
    });

    if (data.SecondLevelReplies != null) {
        this.FourthLevelReplies = ko.observableArray($.map(data.SecondLevelReplies, function (item) { return new FourthLevelReply(item); }));
        this.ShowFourthLevel = true;
    }
    else {
        this.FourthLevelReplies = ko.observableArray([]);
        this.ShowFourthLevel = false;
    }
}

function FourthLevelReply(data) {

    this.Id = ko.computed(function () {
        return data.Id;
    });
    this.ReplyText = ko.computed(function () {
        return data.ReplyText;
    });
    this.PostedBy = ko.computed(function () {
        return "<strong>" + data.PostedBy + "</strong> said:";
    });
    this.PostedDate = ko.computed(function () {
        return data.PostedDateAsString;
    });
    this.PostedByAvatar = ko.computed(function () {
        return data.PostedByAvatar;
    });

    if (data.OtherLevelReplies != null) {
        this.OtherLevelReplies = ko.observableArray($.map(data.OtherLevelReplies, function (item) { return new OtherLevelReply(item); }));
        this.ShowOtherLevel = true;
    }
    else {
        this.OtherLevelReplies = ko.observableArray([]);
        this.ShowOtherLevel = false;
    }
}

function OtherLevelReply(data) {

    this.Id = ko.computed(function () {
        return data.Id;
    });
    this.ReplyText = ko.computed(function () {
        return data.ReplyText;
    });
    this.PostedBy = ko.computed(function () {
        if (JSON.parse(data.level > 0)) {
            var imageString = "<img style='height: 4px;' src='/images/greendot.png' alt='' />";
            var dotsString = imageString;
            var i = 1;
            for (var i = 1; i < JSON.parse(data.level) ; i++) {
                dotsString = dotsString + "&nbsp;" + imageString;
            }

            return dotsString + "<strong > " + data.PostedBy + "</strong> said:";
        }
        return "<strong>" + data.PostedBy + "</strong> said:";
    });
    this.PostedDate = ko.computed(function () {
        return data.PostedDateAsString;
    });
    this.PostedByAvatar = ko.computed(function () {
        return data.PostedByAvatar;
    });
    this.level = ko.computed(function () {
        return data.level;
    });
}

function article(data) {
    this.Id = data.Id;
    this.UploadDate = data.UploadedDateAsString;
    this.ActualArticleURL = data.ActualArticleURL;
    this.ArticleTitle = data.ArticleTitle;
    //this.AmbassadorName = data.AmbassadorName;
    this.Hearts = data.Hearts;
    this.Excerpt = data.Excerpt;
    this.Type = ko.computed(function () {
        if (data.Type == "DoMore team") {
            return 'TEAM I WILL DO';
        }
        else {
            return data.Type;
        }
    });

    this.ArticleThumbnail = ko.computed(function () {
        if (data.ArticleThumbnail == "false") {
            return "";
        }
        return data.ArticleThumbnail;
    });
    //this.AmbassadorURL = data.AmbassadorURL;
    //this.AmbassadorImage = data.AmbassadorImage;
    this.badgeVisible = ko.computed(function () {
        if (data.Type == "AIS") {
            return true;
        }
    });
    this.showImage = ko.computed(function () {
        if (data.ArticleThumbnail == "false") {
            return false;
        }
        else { return true; }
    });
    this.Author = ko.computed(function () {
        if (data.AmbassadorName == "") {
            return "";
        }
        else { return 'By ' + data.AmbassadorName; }
    });

}

$(document).ready(function () {

    getMyEvents();

    $('#share-event').click(function () {

        var url = $('.eventLinkToShare').attr('href');
        var title = $('#titleEvent').text();
        var description = $('#eventDescription').text();
        $('#twEvent').attr('href', 'https://twitter.com/share?url=' + encodeURIComponent(url) + '&text=Rexona : I Will Do - Event : ' + title + '%20&title=' + title);

        // share to google+
        $('#ggEvent').attr('href', 'https://plus.google.com/share?url=' + url);

        $('#fbEvent').attr('href', 'https://www.facebook.com/dialog/feed?app_id=' + $('#hdnFbAppId').val() + '&' +
        'link=' + encodeURIComponent(url) + '&' +
        'picture=http://' + window.location.host + '/images/Rexona_Invite.jpg&' +
        'name=' + encodeURIComponent(title) + '&caption=Rexona : I Will Do.&' +
        'description=' + $('#eventDescription').text() + ' on ' + $('#eventDateTime').text() + '&' +
        'redirect_uri=https://www.facebook.com');

        $('#twEvent,#ggEvent,#fbEvent').click(function () {
            window.open(this.href, '', 'menubar=no,toolbar=no,resizable=yes,scrollbars=yes,height=400,width=700'); return false;
        });

        runCards();
    });



    $('#eventForm,#eventEditForm').on('valid', function () {
        var currentForm = this.id;
        function submitForm() {
            // CYBAGE: PUT YOUR FORM SUBMISSION
            if (currentForm == "eventForm") {
                createEvent();
            }
            if (currentForm == "eventEditForm") {
                editEvent();
            }
        }
        // get the dates inputted by the user
        if (this.id == "eventForm") {
            var startDate = $('#eventStartDate').val()
            var endDate = $('#eventEndDate').val()
        }
        if (this.id == "eventEditForm") {
            var startDate = $('#editStartDate').val()
            var endDate = $('#editEndDate').val()
        }
        // split the value at the "–" character, return all the split pieces
        startDatePieces = startDate.split("-");
        endDatePieces = endDate.split("-");

        // start day is the first piece, month is second, etc
        startDay = startDatePieces[0]
        startMonth = startDatePieces[1]
        startYear = startDatePieces[2]

        endDay = endDatePieces[0]
        endMonth = endDatePieces[1]
        endYear = endDatePieces[2]

        function convertTo24Hour(time) {
            var hours = parseInt(time.substr(0, 2));
            if (time.indexOf('AM') != -1 && hours == 12) {
                time = time.replace('12', '0');
            }
            if (time.indexOf('PM') != -1 && hours < 12) {
                time = time.replace(hours, (hours + 12));
            }
            return time.replace(/(AM|PM)/, '');
        }

        if (this.id == "eventForm") {
            var startTime = $('#eventStarttimeselection').val() + $('#eventStart').val()
            var endTime = $('#eventEndtimeselection').val() + $('#eventEnd').val()
        }
        if (this.id == "eventEditForm") {
            var startTime = $('#time-selection-start').val() + $('#start-am-pm').val()
            var endTime = $('#time-selection-end').val() + $('#end-am-pm').val()

        }
        startTime = convertTo24Hour(startTime)
        endTime = convertTo24Hour(endTime)

        // create our date objects
        var startObject = startMonth + '/' + startDay + '/' + startYear + ' ' + startTime
        var endObject = endMonth + '/' + endDay + '/' + endYear + ' ' + endTime


        //console.log('End date is ' + endObject)
        //console.log('Start date is ' + startObject)

        var endNumber = Date.parse(endObject)
        var startNumber = Date.parse(startObject)
        if (this.id == "eventForm") {
            if ($('#eventEndDate').val().length == 0) {
                submitForm()
            }
            else {
                if (endNumber > startNumber) {
                    submitForm()
                } else {
                    runCards()
                }
            }
        }
        if (this.id == "eventEditForm") {
            if ($('#editEndDate').val().length == 0) {
                submitForm()
            }
            else {
                if (endNumber > startNumber) {
                    submitForm()
                } else {
                    runCards()
                }
            }
        }

    });
    $('#AllSearch input[type=search],#MySearch input[type=search],#NearSearch input[type=search]').on('keyup search', function (e) {
        var parent = $(this).parent();
        searchText = $(this).val();
        searchText = searchText.toLowerCase().trim();
        var divToModify = parent.siblings('.entity-list');
        $('#noallevents', divToModify).remove();
        $('li.event', divToModify).each(function () {

            var currentLiText = $(this).attr('data-postcode');

            //var showCurrentLi = currentLiText.indexOf(searchText.toLowerCase()) !== -1;
            var showCurrentLi = false;

            showCurrentLi = currentLiText.substr(0, searchText.length) == searchText;

            $(this).toggle(showCurrentLi);

        });
        if ($('li.event:visible').length == 0) {
            $(divToModify).append('<p id="noallevents" style="text-align:center">No events found</p>');
        }

    });

});


function Event(data) {
    this.EventId = ko.observable(data.EventId);
    this.EventTitle = ko.observable(data.EventTitle);
    this.StartDate = ko.observable(data.StartDate);
    this.EndDate = ko.observable(data.EndDate);
    this.EventLocation = ko.observable(data.EventLocation);
    this.PostCode = ko.observable(data.PostCode);
    this.StartTime = ko.observable(data.StartTime);
    this.EndTime = ko.observable(data.EndTime);
    this.IsJoined = ko.observable(data.IsJoined);
    this.renderOwner = data.IsOwner;
    this.IsOwner = ko.observable(data.IsOwner);
    this.EventDescription = ko.observable(data.EventDescription);
    this.linkEventId = ko.computed(function () {
        if (!(data.IsJoined && data.IsOwner)) {
            return '#' + data.EventId;
        }
    });
    this.clickEvent = ko.computed(function () {
        if (data.IsJoined && data.IsOwner) {
            return "getEvent('.edit-events',this,'1')";
        }
        else {
            return "getEvent('.view-event',this,'0')";
        }
    });
    this.greenTag = ko.computed(function () {
        if (data.IsJoined && data.IsOwner) {
            return "Edit";
        }
        else if (data.IsJoined) {
            return "Attending";
        }
    });
    this.FullDate = ko.computed(function () {
        if (data.EndDate != "") {
            return data.StartDate + '<br/>to<br/> ' + data.EndDate;
        }
        else {
            return data.StartDate;
        }
    });
    this.pledgeURL = data.EventURL;
}

//List All Events 
function getMyEvents() {

    var members = $('#hdnmemberIds').val();


    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        type: 'GET',
        url: '/umbraco/surface/Event/GetEvents',
        success: function (data) {
            console.log(data);
            if (data.status == true) {
                var topData = $.map(data.events, function (item) {
                    if (members.indexOf(item.OwnerId) > -1) {
                        return new Event(item);
                    }

                });

                pledgeViewModel.AllEvents(topData);
                var MyData = $.map(data.events, function (item) {
                    if ((item.IsOwner || item.IsJoined) && members.indexOf(item.OwnerId) > -1) {
                        return new Event(item);
                    }
                });

                pledgeViewModel.MyEvents(MyData);

                var memberPostCode = $('#hdnMemPostcode').val() ? $('#hdnMemPostcode').val() : "";
                var memberCapitalCity = $('#hdnMemCaptialCity').val() ? $('#hdnMemCaptialCity').val() : "";


                var NearData = $.map(data.events, function (item) {
                    var capitalMatch = memberCapitalCity == "" || item.CapitalCity == "" ? false : item.CapitalCity.toLowerCase() == memberCapitalCity.toLowerCase();
                    if ((item.PostCode == memberPostCode || capitalMatch) && members.indexOf(item.OwnerId) > -1) {
                        return new Event(item);
                    }
                });

                pledgeViewModel.NearEvents(NearData);

                nearEventsCount = NearData.length;
                myEventsCount = MyData.length;
                allEventsCount = topData.length;

                $('#MyCount').text('(' + myEventsCount + ')');
                $('#AllCount').text('(' + allEventsCount + ')');
                $('#nearCount').text('(' + nearEventsCount + ')');
                if (allEventsCount == 0) {
                    $('#all ul').html('<p id="noallevents" style="text-align:center">No events found</p>');
                    $('#AllSearch').hide();
                }
                else {
                    $('#noallevents').remove();
                    $('#AllSearch').show();
                }
                if (myEventsCount == 0) {
                    $('#my-events ul').html('<p id="nomyevents" style="text-align:center">No events found</p>');
                    $('#MySearch').hide();
                }
                else {
                    $('#nomyevents').remove();
                    $('#MySearch').show();
                }
                if (nearEventsCount == 0) {
                    $('#near-me ul').html('<p id="nonearevents" style="text-align:center">No events found</p>');
                    $('#NearSearch').hide();
                }
                else {
                    $('#nonearevents').remove();
                    $('#NearSearch').show();
                }

            }
            else {

            }
            //if (window.location.hash != "") {
            //    var eventId = window.location.hash.slice(1);
            //    $('[id="' + eventId + '"]').click();
            //}
        },
        error: function (data) {
            //endLoad();
            //alert('error');
        }

    });
}


//Create New Event 
function createEvent() {

    var paramObj = new Object();

    //send 0 to create event
    paramObj.EventId = 0;
    paramObj.EventTitle = $('#eventTitle').val();
    paramObj.StartDate = $('#eventStartDate').val();
    paramObj.EndDate = $('#eventEndDate').val();
    paramObj.EventLocation = $('#eventLocation').val();
    paramObj.PostCode = $('#eventPostCode').val();
    paramObj.StartTime = $("#eventStarttimeselection option:selected").text() + $('#eventStart').val();
    paramObj.EndTime = $("#eventEndtimeselection option:selected").text() + $('#eventEnd').val();
    paramObj.EventDescription = $('#eventDesc').val();
    paramObj.EventURL = window.location.href;
    paramObj.State = $("#state-selection option:selected").val();

    if (paramObj.EndDate == "") {
        paramObj.EndTime = "";
    }
    startLoad();
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: JSON.stringify(paramObj),
        type: 'POST',
        url: '/umbraco/surface/Event/CreateEvent',
        success: function (data) {
            endLoad();
            //console.log('top');

            if (data == 'Success') {
                getMyEvents();
                closeStacks();
                new jBox('Notice', {
                    content: 'Event successfully created',
                    color: 'green',
                    theme: 'NoticeBorder'
                });
            }
            else {
                new jBox('Notice', {
                    content: 'Oops,something went wrong,please try again',
                    color: 'red',
                    theme: 'NoticeBorder'
                });

            }
        },
        error: function (data) {
            endLoad();
            //alert('error');
        }

    });
}

var shareUrl;
function getEvent(target, source, isEdit) {

    var EventId = $(source).attr('id');

    var EventURL = $(source).find('.pledgeURLEvent').attr('href');
    shareUrl = EventURL;
    // console.log(EventId);
    var eventDateTime, eventStartDateTime, eventEndDateTime;
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: JSON.stringify({ "EventId": EventId, "IsEdit": isEdit }),
        type: 'POST',
        url: '/umbraco/surface/Event/GetEventDetails',
        success: function (data) {
            console.log(data);
            if (data.status == true) {
                if (data.result) {

                    if (target == '.view-event') {

                        eventStartDateTime = data.result.StartDate + ' at ' + data.result.StartTime;
                        if (data.result.EndDate != "") {
                            eventEndDateTime = data.result.EndDate + ' at ' + data.result.EndTime;
                            eventDateTime = eventStartDateTime + ' to ' + eventEndDateTime;
                        }
                        else {
                            eventDateTime = eventStartDateTime;
                        }

                        $('.eventLinkToShare').attr('href', EventURL);
                        $(target).toggleClass('is-open');
                        $('#titleEvent').html(data.result.EventTitle);
                        $('#statusEvent').html(data.result.MyStatus);
                        if (JSON.parse($('#hdnIsMemberofGroup').val()) && JSON.parse($('#hdnIsLoggedIn').val())) {
                            if (data.result.IsJoined) {
                                $('#leaveEvent').show();
                                $('#JoinEvent').hide();
                            }
                            else {
                                $('#JoinEvent').show();
                                $('#leaveEvent').hide();
                            }
                        }
                        $('#eventDateTime').html(eventDateTime);

                        $('#locationEvent').html(data.result.EventLocation);

                        $('#eventDescription').html(data.result.EventDescription);

                        $('#hdnEventId').val(data.result.EventId);
                    }
                    else if (target == '.edit-events') {

                        $('#editTitle').val(data.result.EventTitle);
                        //if (data.result.IsJoined) {
                        //    $('#statusEvent').html(data.result.MyStatus);
                        //}
                        //else {
                        //    $('#statusEvent').html(data.result.MyStatus);
                        //}

                        $('#hdnEventId').val(data.result.EventId);

                        $('#editStartDate').datepicker("setDate", data.result.StartDate);
                        $('#editEndDate').datepicker("setDate", data.result.EndDate);

                        var result = data.result;
                        var startampm, startTime;

                        if (result.StartTime.indexOf('AM') > -1) {
                            startTime = result.StartTime.split('AM')[0]; startampm = 'AM';
                        }
                        else {
                            startTime = result.StartTime.split('PM')[0]; startampm = 'PM';
                        }

                        var endampm, endTime;

                        if (result.EndTime.indexOf('AM') > -1) {
                            endTime = result.EndTime.split('AM')[0]; endampm = 'AM';
                        }
                        else {
                            endTime = result.EndTime.split('PM')[0]; endampm = 'PM';
                        }

                        $('#time-selection-start').val(startTime);
                        $('#start-am-pm').val(startampm);
                        $('#time-selection-end').val(endTime);
                        $('#end-am-pm').val(endampm);


                        $('#editLocation').val(data.result.EventLocation);

                        $('#editDescription').val(data.result.EventDescription);

                        $('#editPostCode').val(data.result.PostCode);

                        $('#editSelect').val(data.result.State)
                        $(target).toggleClass('is-open');

                    }
                }
            }
            else {
                new jBox('Notice', {
                    content: 'Oops,something went wrong,please try again',
                    color: 'red',
                    theme: 'NoticeBorder'
                });

            }
        },
        error: function (data) {
            //endLoad();
            //alert('error');
        }

    });
}


function editEvent() {
    var paramObj = new Object();
    var EventId = $('#hdnEventId').val();
    paramObj.EventId = EventId;
    paramObj.EventTitle = $('#editTitle').val();
    paramObj.StartDate = $('#editStartDate').val();
    paramObj.EndDate = $('#editEndDate').val();
    paramObj.EventLocation = $('#editLocation').val();
    paramObj.PostCode = $('#editPostCode').val();
    paramObj.StartTime = $("#time-selection-start option:selected").val() + $('#eventStart').val();
    paramObj.EndTime = $("#time-selection-end option:selected").val() + $('#eventEnd').val();
    paramObj.EventDescription = $('#editDescription').val();
    paramObj.State = $('#editSelect option:selected').val();
    paramObj.EventURL = shareUrl;
    if (paramObj.EndDate == "") {
        paramObj.EndTime = "";
    }

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: JSON.stringify(paramObj),
        type: 'POST',
        url: '/umbraco/surface/Event/CreateEvent',
        success: function (data) {

            //console.log('top');
            console.log(data);
            if (data == 'Success') {
                getMyEvents();
                closeStacks();
                new jBox('Notice', {
                    content: 'Event successfully updated.',
                    color: 'green',
                    theme: 'NoticeBorder'
                });
            }
            else {
                new jBox('Notice', {
                    content: 'Oops,something went wrong,please try again',
                    color: 'red',
                    theme: 'NoticeBorder'
                });

            }
        },
        error: function (data) {
            endLoad();
            //alert('error');
        }

    });

}


function deleteEvent() {
    var EventId = $('#hdnEventId').val();

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: JSON.stringify({ "EventId": EventId }),
        type: 'POST',
        url: '/umbraco/surface/Event/DeleteEvent',
        success: function (data) {
            endLoad();
            //console.log('top');

            if (data.message == 'Success') {
                getMyEvents();
                closeStacks();
                new jBox('Notice', {
                    content: 'Event successfully Deleted',
                    color: 'green',
                    theme: 'NoticeBorder'
                });
            }
            else {
                new jBox('Notice', {
                    content: 'Oops,something went wrong,please try again',
                    color: 'red',
                    theme: 'NoticeBorder'
                });

            }
        },
        error: function (data) {
            endLoad();
            //alert('error');
        }

    });
}


//leave or Join Event 

function leaveEvent() {

    var EventId = $('#hdnEventId').val();

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: JSON.stringify({ "EventId": EventId }),
        type: 'POST',
        url: '/umbraco/surface/Event/LeaveEvent',
        success: function (data) {
            endLoad();
            //console.log('top');

            if (data.message == 'Success') {
                $('.event-status').html('Not attending')
                $('.confirm-leave').fadeOut(300, function () {
                    //$('.attend-event').fadeIn(300);
                    $('#JoinEvent').show();
                    $('#leaveEvent').hide();
                })

                getMyEvents();

            }
            else {
                new jBox('Notice', {
                    content: 'Oops,something went wrong,please try again',
                    color: 'red',
                    theme: 'NoticeBorder'
                });

            }
        },
        error: function (data) {
            endLoad();
            //alert('error');
        }

    });
}

function attendEvent() {

    var EventId = $('#hdnEventId').val();
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        data: JSON.stringify({ "EventId": EventId }),
        type: 'POST',
        url: '/umbraco/surface/Event/AttendEvent',
        success: function (data) {
            endLoad();
            //console.log('top');

            if (data.message == 'Success') {
                $('#JoinEvent').hide();
                $('#leaveEvent').show();
                $('#statusEvent').html('Attending');
                getMyEvents();

            }
            else {
                new jBox('Notice', {
                    content: 'Oops,something went wrong,please try again',
                    color: 'red',
                    theme: 'NoticeBorder'
                });

            }
        },
        error: function (data) {
            endLoad();
            //alert('error');
        }

    });
}

var attending = true;

$('#JoinEvent').click(function (e) {
    //e.preventDefault()
    //attending = !attending
    //if (attending == false) {
    //    $(this).html('Leave Event')
    //} else {
    //    $(this).html('Attend')
    //}
    //$(this).toggleClass('alert')
    attendEvent();

})

$('#leaveEvent').click(function (e) {

    $(this).fadeOut(300, function () {
        $('.confirm-leave').fadeIn(300);
    });
});


$('.leave').click(function () {

    //call to ajax to update status of member 

    leaveEvent();
    //
    //$('.attend-event').html('Attend this event')
    //$('.event-status').html('Not attending')
    //$('.confirm-leave').fadeOut(300, function () {
    //    $('.attend-event').fadeIn(300);
    //})
})