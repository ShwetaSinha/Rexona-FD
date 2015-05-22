var queryString = '';
var pledgeViewModel;
var currentPageIndex = 0, myCurrentPageIndex = 0, followedCurrentPageIndex = 0, sharedCurrentPageIndex = 0;
var requestPending = false, All_requestPending = false, myRequestPending = false, followedRequestPending = false, sharedRequestPending = false;
var totalPages, myTotalPages, followedTotalPages, sharedTotalPages;
var existingItems = [];
var DiscussionexistingItems = [];
var myExistingItems = [];
var followedExistingItems = [];
var sharedExistingItems = [];
var ADE_allItems = [];
var PageSize = 4, SharedPageSize = 15;
var myFollowedDiscussionIds = [], myDiscussionIds = [], sharedArticles = [];
var galleryViewModel;

$(document).ready(function () {

    $(document).on('click', '.reply-entry button', function (event) {
        //event.preventDefault();
        AddReply($(this).attr('id'));
    });

    if (Modernizr.touch) {
        //Fire Touch Events
    }

    //
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
        var lengthToCallScroll = eachChildLength * (2 + (6 * followedCurrentPageIndex));
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
   // alert($('#hdnIsLoggedIn').val());
        if (JSON.parse($('#hdnIsLoggedIn').val())) {
            $('#DiscussionPost').parent().show();
            $('.discussions-actions, #shareGoal, #imgStartBadge, #imgSharedBadge, #imgEndBadge, a[href="#discussions-following"], a[href="#discussions-my-posts"]').show();


            //Check if user connected using fb
            if (JSON.parse($('#hdnIsConnectedUsingFb').val())) {
                $('#facebookTab').parent().show();
            }
        }
        if ($('#hdnIsMemberofGroup').val() != undefined) {
            if (JSON.parse($('#hdnIsMemberofGroup').val()) && JSON.parse($('#hdnIsLoggedIn').val())) {
                $('.pill-meta .primary-meta').show();
                $('#createEvent,#statusEvent,#JoinEvent,#leaveEvent,a[href="#my-events"]').show();
            }
        }
        if (JSON.parse($('#hdnIsConnectedUsingFb').val()) || !JSON.parse($('#hdnIsLoggedIn').val())) {
            $('a[href="#near-me"]').hide();
        }


    $(document).on('click', '.entity-name a', function () {
        var discussionId = $(this).parents('.discussion-in-list').find('.discussions-actions').attr('id');
        BindReplies(discussionId);

    });

    $(document).on('click', '#discussionReply', function (e) {
        var discussionId = $(this).parent().attr('id');
        var pledgeId = sessionStorage.getItem("PledgeId");
        var pledgediscussioncnt = 0;
        pledgediscussioncnt =parseInt($('#' + pledgeId).parent().find('span').text());
        var disreplycnt = 0;
        if ($(e.target).find('span').text() != "") {
            disreplycnt = $(e.target).find('span').text();
            //alert(disreplycnt);
        }
        if (pledgediscussioncnt > 0) {
            var differnce = parseInt(pledgediscussioncnt) - parseInt(disreplycnt);
            $('#' + pledgeId).parent().find('span').text(differnce);
            if (differnce == 0) {
                $('#' + pledgeId).parent().find('span').hide();
            }
        }
        BindReplies(discussionId);

    });
    
    pledgeViewModel = new PledgeModel();
    ko.applyBindings(pledgeViewModel);
    currentPageIndex = 0;
    if ($('#hdnpledgecnt').val() == 0) {
        if ($('#get-started-view').css('display') == 'block') {
            getPledges(myCurrentPageIndex);
            $('#editlinkId').hide();
            $('.no-setgoal').css('cssText', 'float: left !important').css('width', '50%');
            $('#editProfile').css('display', 'block').css('margin', '0 auto').css('width', '50%');
            $('#dash-right').css('cssText', 'display: block !important').css('visibility', 'visible');
        }
    }    


    //Get All discussions
    $(document).on('click', '#goallink a', function (event) {
        //alert($('#hdnIsLoggedIn').val());
        $('#discussions-all .entity-list').html("");
        $('#alltab .entity-list').html("");
        $('#all .entity-list').html("");
        $('#dashboardRecArticles .entity-list').html("");
        
        currentPageIndex = 0;
        var pledgeid = $(event.target).find('#hdnPledgeId').val();
        sessionStorage.setItem("PledgeId", pledgeid);
        $('#hdnPopupPledgeId').val(sessionStorage.getItem('PledgeId'));
        

        var artTag = $(event.target).find('#hdnPledgeTag').val();
        sessionStorage.setItem("articleTag", artTag);
        $('#hdArtTag').val(sessionStorage.getItem('articleTag'));
        
        getAllrelatedtoPledge(currentPageIndex);

    });


    //currentPageIndex = 0;
    //getDiscussions(currentPageIndex);

    //Get My Followed discussions
    //followedCurrentPageIndex = 0;
    //getFollowedDiscussions(followedCurrentPageIndex);

    //Get My Discussions
    //myCurrentPageIndex = 0;
    //getMyDiscussions(myCurrentPageIndex);


    //Below line needs to be removed
    //$('#postAvatarImage').css('height', '50px');

    // Provide badges to the end user 
    //if (JSON.parse($('#hdnStartBadge').val())) {
    //    $('#imgStartBadge').removeClass('inactive');
    //}

    //if (JSON.parse($('#hdnSharedBadge').val())) {
    //    $('#imgSharedBadge').removeClass('inactive');
    //}

    //if ($('#hdnEndBadge').val() == 'hide') {
    //    $('#imgEndBadge').hide();
    //}
    //else if (JSON.parse($('#hdnEndBadge').val())) {
    //    $('#imgEndBadge').show().removeClass('inactive');
    //}

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
    if ($('#hdnIsMemberofGroup').val() != undefined) {
        if (JSON.parse($('#hdnIsMemberofGroup').val())) {
            $('#joinedGoal').show();
            $('#joinGoal').hide();
        }
        else {
            $('#joinedGoal').hide();
            if ($('#hdnPledgeType').val() != 'Close') {
                $('#joinGoal').show();
            }
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
        $('#DiscussionPost').attr('disabled', true);
        var dataToSendToAjax = new Object();
        if ($.trim($('#discussion-title').val()) != '' && $('#discussion-title').parents('.discussions-listing-start-discussion').find('#titleError').css('display') != "block") {
            startLoad();
            $('#discussions-all .entity-list').scrollTop(0);
            $('#discussions-my-posts .entity-list').scrollTop(0);
            dataToSendToAjax.discussionTitle = $('#discussion-title').val();
            dataToSendToAjax.discussionBody = $('#discussion-body').val();
            dataToSendToAjax.pledgeId = sessionStorage.getItem("PledgeId");

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
                    DiscussionexistingItems = [];
                    //getDiscussions(currentPageIndex);
                    getDiscussionsPledge(myCurrentPageIndex, sessionStorage.getItem("PledgeId"));
                    $('#DiscussionPost').attr('disabled', false);
                    myCurrentPageIndex = 0;
                    myExistingItems = [];
                    getMyDiscussions(myCurrentPageIndex);
                    getAllrelatedtoPledge(myCurrentPageIndex);
                    closeStacks();
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
                    $(".goal-list").toggle().toggle();
                }

                if ($('#alltab').css('display') == 'block') {
                    getAllrelatedtoPledge(currentPageIndex);
                }
                else if ($('#discussions-all').css('display') == 'block') {
                    getDiscussionsPledge(myCurrentPageIndex, sessionStorage.getItem("PledgeId"));
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
                    openLightbox('.discussionreply');
                    return;
                }

                $('#spnForeachReplies').hide();
                openLightbox('.discussionreply');
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
                    currentPageIndex = 0;
                    var pid = sessionStorage.getItem("PledgeId");
                    getDiscussionsPledge(currentPageIndex, pid);
                    getAllrelatedtoPledge(currentPageIndex);
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

        dataToSendToAjax.pledgeId = $(this).find('#hdnPledgeId').val();
        //$('#hdnPledgeId', this).val();

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
                DiscussionexistingItems = DiscussionexistingItems.concat(userDiscussions);
                pledgeViewModel.Discussions(DiscussionexistingItems);

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

function getDiscussionsPledge(pageIndex, PledgeId) {
    if (!requestPending) {
        requestPending = true;

        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.pledgeId = PledgeId;
        //$('#hdnPledgeId', this).val();

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
                DiscussionexistingItems = DiscussionexistingItems.concat(userDiscussions);
                pledgeViewModel.Discussions(DiscussionexistingItems);
                btn_viewreplies();
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
        dataToSendToAjax.pledgeId = sessionStorage.getItem("PledgeId");

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
        dataToSendToAjax.pledgeId = sessionStorage.getItem("PledgeId");
        dataToSendToAjax.PledgeTag = sessionStorage.getItem("articleTag");

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Discussion/GetSharedArticles',
            success: function (data) {
                if (data.SharedArticles.length == 0 && pageIndex == 0) {
                    $('#dashboardRecArticles').hide();
                    $('.content-panel').show();
                    $('.content-panel .large-6').html('<p>No Articles Shared.</p>');
                    sharedRequestPending = false;
                    sharedTotalPages = 0;
                    return;
                }

                if (data.Message == "success" && pageIndex == 0) {
                    $('#dashboardRecArticles .entity-list').html("");
                }

                $('#dashboardRecArticles').show();
                $('.content-panel').hide();
                var sharedArticles = $.map(data.SharedArticles, function (item) { return new article(item); });
                sharedExistingItems = sharedExistingItems.concat(sharedArticles);
                sharedExistingItems = sharedArticles;
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



                        $('.rating-icon').unbind('click').click(function (e) {
                            likeCount_article("like", $(this).attr('Id'));
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


function getAllrelatedtoPledge(pageIndex) {
    if (!All_requestPending) {
        All_requestPending = true;
        var dataToSendToAjax = new Object();
        //dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.pledgeId = sessionStorage.getItem("PledgeId");
        dataToSendToAjax.PledgeTag = sessionStorage.getItem("articleTag");

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Discussion/GetAll',
            success: function (data) {
                if (data.Message == "error" && pageIndex == 0) {
                    $('#alltab .entity-list').html("<p>Articles, Discussions and Events are not found.</p>");
                    All_requestPending = false;
                    //totalPages = 0;
                    return;
                }
                if (data.Message == "success" && pageIndex == 0) {
                    $('#alltab .entity-list').html("");
                }
                var ADE_All = $.map(data.ADE_all, function (item) { return new ADE(item); });
                ADE_allItems = ADE_allItems.concat(ADE_All);
                ADE_allItems = ADE_All;
                pledgeViewModel.ADE_all(ADE_allItems);
                All_requestPending = false;
                btn_viewreplies();
                // totalPages = data.totalPages;
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
                console.log('error');
            }
        });
    }
}
//Create Pledge Model function for View Model
function PledgeModel() {
    var self = this;
    self.pledgesDashboard = ko.observableArray([]);
    self.Discussions = ko.observableArray([]);
    self.MyDiscussions = ko.observableArray([]);
    self.FollowedDiscussions = ko.observableArray([]);
    self.Replies = ko.observableArray([]);
    self.AllEvents = ko.observableArray([]);
    self.MyEvents = ko.observableArray([]);
    self.NearEvents = ko.observableArray([]);
    self.SharedArticles = ko.observableArray([]);
    self.ADE_all = ko.observableArray([]);
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
    this.Description = ko.computed(function () {
        return data.Description;
    });
    this.Repliescount = ko.computed(function () {
        if (data.Repliescount == 0) {
            data.Repliescount = 'View Replies<span class="label" id="repliescnt" ></span>';
        }
        else {
            data.Repliescount = 'View Replies <span class="label" >' + data.Repliescount + '</span>';
        }
        return data.Repliescount;
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
    this.ArticlepostedDate = data.ArticlepostedDate;
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
        else { return 'Posted by ' + data.AmbassadorName; }
    });

}

function ADE(data) {

    this.Id = data.ID;
    this.Title = ko.computed(function () {
        return data.Title;
    });
    this.Description = data.Description;
    this.PostedBy = ko.computed(function () {
        return "Posted by " + data.PostedBy;
    });
    this.PostedDate = data.PostedDate;
    this.Repliescount = ko.computed(function () {
        if (data.Repliescount == 0) {
            data.Repliescount = 'View Replies<span class="label" id="repliescnt" ></span>';
        }
        else {
            data.Repliescount = 'View Replies <span class="label" >' + data.Repliescount + '</span>';
        }
        return data.Repliescount;
    });
    this.linkEventId = ko.computed(function () {
        if (!(data.IsJoined && data.IsOwner)) {
            return '#' + data.ID;
        }
    });
    this.PostCode = ko.computed(function () {
        return data.PostCode;
    });
    this.FullDate = ko.computed(function () {
        if (data.EndDate != "") {
            return data.StartDate + '<br/>to<br/> ' + data.EndDate;
        }
        else {
            return data.StartDate;
        }
    });
    var dt = data.StartDate;
    this.Day = ko.computed(function () {
        var day="";
        if (dt != null) {
            var d2 = parseInt(dt, 10);

            var dsufix = dt.substring(dt.indexOf(" "), dt.indexOf(" ") - 2);
            day = d2 + '<i>' + dsufix + '</i>';
            return data.day;
        }
        else
            return day;
    });
    this.Daysufix = ko.computed(function () {
        if (dt != null) {
            var dsufix = dt.substring(dt.indexOf(" "), dt.indexOf(" ") - 2);
            return dsufix;
        }
        else {
            return "";
        }
    });
    this.Month = ko.computed(function () {
        if (dt != null) {
            var d3 = dt.substring(dt.indexOf(" ") + 1, (dt.length - 5));
            return d3;
        }
        else {
            return "";
        }
    });
    this.EndTime = ko.computed(function () {
        return data.EndTime;
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
        else {
            return "Attend this event";
        }
    });
    this.IsJoined = ko.computed(function () {
        return data.IsJoined;
    });
    this.IsOwner = ko.computed(function () {
        return data.IsOwner;
    });
    this.ActualArticleURL = ko.computed(function () {
        return data.ActualArticleURL;
    });
    this.ArticleThumbnail = ko.computed(function () {
        if (data.ArticleThumbnail == "false") {
            return "";
        }
        return data.ArticleThumbnail;
    });
    this.Excerpt = ko.computed(function () {
        return data.Excerpt;
    });
    this.Author = ko.computed(function () {
        if (data.AmbassadorName == "") {
            return "";
        }
        else { return 'Posted by ' + data.AmbassadorName; }
    });
    this.Hearts = ko.computed(function () {
        return data.Hearts;
    });
    this.Type = ko.computed(function () {
        if (data.Type == "DoMore team") {
            return 'TEAM I WILL DO';
        }
        else {
            return data.Type;
        }
    });
    this.Typeof = data.Typeof;
    this.badgeVisible = ko.computed(function () {
        if (data.Type == "AIS") {
            return true;
        }
    });
    var typeOf = data.Typeof;
    this.loadDiv = ko.computed(function () {
        var typeoftxt = data.Typeof;
        
        
        if (typeoftxt == "Discussion") {
            data.Typeof = '<div class="content-block discussion-in-list"><b>' + data.Title + '</b><p>' + data.Description + '</p><div class="footer-block"><div class="left"><h6 class="author">Posted by ' + data.PostedBy + '</h6><h6 class="date">' + data.PostedDate + '</h6></div><div class="right"><span class="replies button thin black loadreplies" id=' + data.ID + ' ><a id="discussionReply" class="small-link" >' + data.Repliescount + '</a></span></div><div class="clearfix"></div></div></div>';
        }
        else if (typeoftxt == "Event") {
            var d2 = parseInt(data.StartDate, 10);
            var dsufix = data.StartDate.substring(data.StartDate.indexOf(" "), data.StartDate.indexOf(" ") - 2);
            var d3 = data.StartDate.substring(data.StartDate.indexOf(" ") + 1, (data.StartDate.length - 5));
            var clickevnt = "";
            var greentag = "";
                if (data.IsJoined && data.IsOwner) {
                    clickevnt= "getEvent('.edit-events',this,'1')";
                }
                else {
                    clickevnt= "getEvent('.view-event',this,'0')";
                }
                if (data.IsJoined && data.IsOwner) {
                    greentag= "Edit";
                }
                else if (data.IsJoined) {
                    greentag= "Attending";
                }
                else {
                    greentag= "Attend this event";
                }

                data.Typeof = '<div class="content-block event" data-postcode=' + data.PostCode + '"><div class="article-img event-dates" ><h1>' + d2 +
                '<i>' + dsufix + '</i>' + '</h1><p>' + d3 + '</p><div class="entity-meta"><a class="button thin black" id='
                + data.ID + ' onclick=' + clickevnt + '><span class="attendance" visible=' + data.IsJoined + '>' + greentag +
                '</span></a></div></div><section class="large-9 columns" ><b>' + data.Title + '</b> <p>' +
                data.Description + '<p>event Location : ' + data.EventLocation + '</p></p><div class="footer-block"><div class="right"></div><div class="clearfix"></div></div></section><div class="clearfix"></div></div>';
        }
        else if (typeoftxt == "article") {
            var badgeVisible = "none";
                if (data.Type == "AIS") {
                    badgeVisible= "block";
                }
                data.Typeof = '<div class="content-block article"><div class="article-header article-all" style="display:' + badgeVisible + '"><span>' + data.Type + '</span><img alt="" src="/images/ais-badge.jpg"></div><div class="article-img"><a class="articleLink" href=' + data.ActualArticleURL + '><img src=' + data.ArticleThumbnail + '></a></div><section class="large-9 columns article-all"><a class="articleLink" href=' + data.ActualArticleURL + '><b>' + data.Title + '</b></a><p>' + data.Excerpt + '</p><div class="footer-block"><div class="left"><h6 class="author">Posted by ' + data.AmbassadorName + '</h6><h6 class="date">' + data.PostedDate + '</h6></div><div class="right"><span class="heart button thin black"><div class="item-rate"><div class="rating-icon" id=' + data.ID + '><div class="rating-number"><span>' + data.Hearts + '</span><i id=' + data.ID + ' class="fa fa-heart"></i></div></div></div></span><span class="share button thin black item-share"><a class="small-link">Share</a></span></div><div class="clearfix"></div></div></section><div class="clearfix"></div></div>';
        }
        return data.Typeof;
    });
    this.articleDiv = ko.computed(function () {
        if (typeOf == "article") {
            var badgeVisible = false;
            if (data.Type == "AIS") {
                badgeVisible = true;
            }
            typeOf = '<div class="content-block article"><div class="article-header article-all" visible=' + badgeVisible + '><span>' + data.Type + '</span><img alt="" src="/images/ais-badge.jpg"></div><div class="article-img"><img src=' + data.ArticleThumbnail + '></div><section class="large-9 columns article-all"><a class="articleLink" href=' + data.ActualArticleURL + '><b>' + data.Title + '</b></a><p>' + data.Excerpt + '</p><div class="footer-block"><div class="left"><h6 class="author">Posted by ' + data.AmbassadorName + '</h6><h6 class="date">' + data.PostedDate + '</h6></div><div class="right"><span class="heart button thin black"><div class="item-rate"><div class="rating-icon" id=' + data.ID + '><div class="rating-number"><span>' + data.Hearts + '</span><i id=' + data.ID + ' class="fa fa-heart"></i></div></div></div></span><span class="share button thin black item-share"><a class="small-link">Share</a></span></div><div class="clearfix"></div></div></section><div class="clearfix"></div></div>';
            return typeOf;
        }
    });
}

$(document).ready(function () {

    //getMyEvents();

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
    this.EventLocation = ko.observable("Event Location: " + data.EventLocation);
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
        else {
            return "Attend this event";
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

    var dt = data.StartDate;
    this.Day = ko.computed(function () {
        var d2 = parseInt(dt, 10);
        var dsufix = dt.substring(dt.indexOf(" "), dt.indexOf(" ") - 2);
        var day = d2 + '<i>' + dsufix + '</i>';
        return day;
    });

    this.Daysufix = ko.computed(function () {
        var dsufix = dt.substring(dt.indexOf(" "), dt.indexOf(" ") - 2);
        return dsufix;
    });

    this.Month = ko.computed(function () {
        var d3 = dt.substring(dt.indexOf(" ") + 1, (dt.length - 5));
        return d3;
    });

    this.pledgeURL = data.EventURL;
}
//List All Events 
function getMyEvents() {

    var members = $('#hdnmemberIds').val();
    //alert(members);

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        type: 'GET',
        url: '/umbraco/surface/Event/GetEvents',
        success: function (data) {
            console.log(data);
            $('#all .entity-list').html("");
            if (data.status == true) {
                var topData = $.map(data.events, function (item) {
                    
                    //if (members.indexOf(item.OwnerId) > -1) {
                        return new Event(item);
                    //}

                });
                //alert(topData.length);
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
                currentPageIndex = 0;
                getAllrelatedtoPledge(currentPageIndex);
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
                currentPageIndex = 0;
                getAllrelatedtoPledge(currentPageIndex);
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
                currentPageIndex = 0;
                getAllrelatedtoPledge(currentPageIndex);

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
                currentPageIndex = 0;
                getAllrelatedtoPledge(currentPageIndex);

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

function btn_viewreplies() {
    $('.loadreplies span').each(function () {
        var txt = $(this).text();
        if (txt == '') {
            $(this).parent(this).parent().addClass('pdr0');
        }
    });
}


/* for likes in article tab*/
function likeCount_article(fieldName, entry_id) {
   if (!$.cookie("entrylikes")) {
        var likeString = ',' + entry_id + ',';
        setCookie(likeString);
        markLikes();

        //ajax increment
        voteAjax_article(true, entry_id, fieldName);

    } else {
        var cookie = $.cookie("entrylikes");

        if (cookie.indexOf(entry_id + ',') > -1) {
            cookie = removeValue(cookie, entry_id);
            setCookie(cookie);
            markLikes();

            //ajax decrement
            voteAjax_article(false, entry_id, fieldName);
        } else {
            cookie = cookie + entry_id + ',';
            $.removeCookie('entrylikes');
            setCookie(cookie);
            markLikes();

            //ajax increment
            voteAjax_article(true, entry_id, fieldName);
        }
    }

}

function voteAjax_article(flag, entryId, fieldName) {
    var votedata = new Object();
    votedata.vote = flag;
    votedata.entryId = entryId;
    votedata.fieldName = fieldName;

    $.ajax({
        url: '/umbraco/surface/Vote/CountVote',
        type: 'POST',
        cache: false,
        data: votedata,
        success: function (data) {
            if (data) {
                //console.log(data);
                if (data.message == 'Success') {
                    $('#alltab .entity-list').html("");
                    $('#' + entryId).parent().find('.rating-number span').text(data.like);
                }
                else {
                    //TO DO change function name;
                    //console.log('Oops. Something went wrong.Please try again');

                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            //TO DO change function name;
            // console.log('Oops. Something went wrong.Please try again');
        }
    });

}


/* Goal gallery js*/

//$(document).ready(function () {
//    galleryViewModel = new GalleryModel();
//    ko.applyBindings(galleryViewModel);

//    currentPageIndex = 0;
//    getPledges(currentPageIndex);


//    $('.select').change(function () {
//        existingItems = [];
//        currentPageIndex = 0;
//        requestPending = false;
//        getPledges(currentPageIndex);
//    });

//    $(document).on('click', '.rating-icon', function (e) {
//        if ($(this).attr('Id') != undefined) {
//            likeCount("likeCount", $(this).attr('Id'));
//        }
//    });

//});


//$(window).scroll(function (e) {
//    if ($('#hdnpledgecnt').val() == 0) {
//        if ($('#get-started-view').css('display') == 'block') {
//            if (!requestPending && (currentPageIndex + 1 < totalPages)) {
//                if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
//                    currentPageIndex = currentPageIndex + 1;
//                    getPledges(currentPageIndex);
//                }
//            } else {
//                e.preventDefault();
//                e.stopPropagation();
//            }
//        }
//    }
//});
function getPledges(pageIndex) {
    PageSize = 12;
    if (!requestPending) {
        requestPending = true;
        var sortingText = $('#filter option:selected').val();
        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.SortingText = sortingText;

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Pledge/GetPledgesDashboard',
            success: function (data) {
                var userPledges = $.map(data.pledgesDashboard, function (item) { return new pledgeDashboard(item); });
                existingItems = existingItems.concat(userPledges);
                pledgeViewModel.pledgesDashboard(existingItems);

                requestPending = false;
                totalPages = data.totalPages;

                setTimeout(function () {

                    //Reload masonry plugin
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');

                    //For sharing to social sites
                    $('.item-share .small-link').unbind('click').click(function () {
                        var parentId = $(this).parents('.is-pledge').find('.rating-icon').attr('Id');
                        var pathToShare = window.location.protocol + '//' + window.location.host + $('#anchor' + parentId).attr('href');
                        globalShare(pathToShare, 'goal ');

                        var dataToSendToShare = new Object();
                        dataToSendToShare.pledgeId = parentId;

                        $.ajax({
                            contentType: "application/json; charset=utf-8",
                            dataType: 'json',
                            cache: false,
                            data: dataToSendToShare,
                            type: 'GET',
                            url: '/umbraco/surface/Pledge/SharePledge',
                            success: function (data) {
                            },
                            error: function (data) {
                                console.log('error while sharing pledge');
                            }

                        });

                        return false;
                    });

                    //For joining pledge
                    $('.item-join .small-link').unbind('click').click(function () {

                        var parentId = $(this).parents('.is-pledge').find('.rating-icon').attr('Id');

                        var dataToSendToJoin = new Object();
                        dataToSendToJoin.NodeId = "" + parentId + "";

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
                    });

                }, 100);

                $('.full-on-mobile').load(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                });

                markLikes();
            },
            error: function (data) {
                console.log('error');
            }

        });
    }
}
function pledgeDashboard(data) {
    this.ImageURL = data.ImageURL;
    this.MemberCount = data.MemberCount;
    if (data.IsPublicSelection == "1") {
        this.ShowJoinLinkAndText = true;
    }
    else {
        this.ShowJoinLinkAndText = false;
    }
    this.ShowJoinLink = true;
    //if (data.IsMember) {
    //    this.ShowJoinLink = true;
    //}
    //else {

    //    this.ShowJoinLink = true;
    //}
    this.LikeCount = data.LikeCount;
    this.Id = ko.computed(function () {
        return data.Id;
    });
    this.PledgeURL = ko.computed(function () {
        return data.PledgeURL + "/#" + data.Id;
    });

    this.hrefId = ko.computed(function () {
        return 'anchor' + data.Id;
    });

    this.PledgeMembers = ko.computed(function () {
        if (data.MemberCount == "1") {
            return data.MemberCount + ' other person has made this goal';
        }
        else { return data.MemberCount + ' other people have made this goal'; }
    });
    this.PledgeTitle = data.PledgeTitle;

}
