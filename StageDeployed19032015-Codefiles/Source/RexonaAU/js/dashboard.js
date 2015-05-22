var existingItems = [], existingPledges = [];
var commonModel;
var requestPending = false;

$(document).ready(function () {


    $('#getFriends').click(function () {

        openCloseStack('.view-friends');
    });
    if (JSON.parse($('#hdnIsConnectedUsingFb').val())) {
        $('a[href="#near-me"]').hide();
    }
    commonModel = new CommonModel();
    ko.applyBindings(commonModel);

    getInvitedMembers();
    getMyEvents();

    //pledgeViewModel = new PledgeModel();
    //ko.applyBindings(pledgeViewModel, $('#discussionPopup').get(0));


    $(document).on('click', '#invitedfriendlist li > a', function () {

        $('.card.next-card .card-content h5').text($('.entity-name p', this).text());
        var dataToSend = new Object();
        dataToSend.CurrentMemberId = this.id;
        $.ajax({
            url: '/umbraco/surface/FacebookMember/Getpledges',
            type: 'POST',//JSON.stringify(details),
            dataType: 'json',
            data: JSON.stringify(dataToSend),
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                console.log(data);
                var fbpledges = $.map(data.pledges, function (item) { return new fbMemberPledges(item); });
                existingPledges = existingPledges.concat(fbpledges);
                commonModel.Pledges(existingPledges);

                requestPending = false;
                existingPledges = [];
                runCards();
            },
            error: function (message) {
                console.log('.eerrror..');
                runCards();
            }
        });
    });

    $('.view-friends input[type=search]').on('keyup search', function (e) {

        var searchText = $(this).val();
        searchText = searchText.toLowerCase();
        $('#invitedfriendlist li .entity-name').each(function () {

            var currentLiText = $('span[data-id=fullName]', this).text().toLowerCase();
            var fullname = currentLiText;
            var displayName = $('span[data-id=displayName]', this).text().toLowerCase();
            displayName = displayName.substring(1, displayName.length - 1);
            currentLiText = currentLiText + ' ' + displayName;
            var names = currentLiText.split(' ');
            names.push(fullname);
            names.push(displayName);

            //var showCurrentLi = currentLiText.indexOf(searchText.toLowerCase()) !== -1;
            var showCurrentLi = false;
            if (names.length > 1) {
                $.each(names, function (index) {
                    if (!showCurrentLi) {
                        showCurrentLi = names[index].substr(0, searchText.length) == searchText;
                    }
                });
            }
            else {
                showCurrentLi = currentLiText.substr(0, searchText.length) == searchText;
            }
            $(this).parents('li').toggle(showCurrentLi);

        });
    });

    $('.view-friends a:contains(Close)').click(function () {
        setTimeout(function () {
            $('input[type=search]').val('');
            $('#invitedfriendlist li').show();
        }, 1000);
    });

    $(document).on('click', 'a.removeFriend', function () {
        $(this).fadeOut(300, function () {
            $(this).next('.confirm-remove').fadeIn();
        });
    });

    $(document).on('click', 'a.cancelRemove', function () {
        $(this).parent().parent().fadeOut(300, function () {
            $(this).prev('.removeFriend').fadeIn();
        });
    });

    $(document).on('click', 'a.removeItem', function () {
        var parentFriend = $(this).parents('li.user');
        var friendId = $(parentFriend).find('a').attr('id');

        var friendData = new Object();
        friendData.Id = friendId;
        $.ajax({
            url: '/umbraco/surface/FacebookMember/RemoveFriend',
            type: 'POST',//JSON.stringify(details),
            dataType: 'json',
            data: JSON.stringify(friendData),
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                //console.log(data);
                $(parentFriend).fadeOut(100);
                $(parentFriend).remove();
            },
            error: function (message) {

                $(parentFriend).fadeOut(100);
                $(parentFriend).remove();
            }
        });

    });

    getRecommendedArticles(pageIndex);

    /*
    //Lazy loading is removed for recommended articles
    $(window).scroll(function (e) {

        if (!requestPending && (pageIndex + 1 < totalPages)) {
            if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
                pageIndex = pageIndex + 1;
                getRecommendedArticles(pageIndex);
            }
        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });
    */

    $(document).on('click', '.rating-icon', function (e) {

        //  console.log(this.id);
        likeCount('like', this.id);
    });

    //Discussion popup methods
    $(document).on('click', '.openDiscussionpopup', function () {
        var discusionLI = $(this).parents('li');
        var discussionId = discusionLI.attr('id');
        BindReplies(discussionId);

        //Get discussion li for the link clicked to reduce the unread count.
        //Even if user clicked in replies tab, relevent discussion tab count should be reduced
        //following regex statement return number of replies for perticular discussion
        var repliesCount = $('a.openDiscussionpopup', $("li.discussion[id='" + discussionId + "']")).text().replace(/[^\d.]/g, '');

        if (repliesCount != '') {

            //change text to view reply
            $('a.openDiscussionpopup', $("li.discussion[id='" + discussionId + "']")).text('view reply');

            //change count in red notification circle near 'Discussion' tab in header navigation
            if ($('.notification-badge').length > 0 && $('.notification-badge').text() != '') {
                var unreadReplies = $('.notification-badge').text();
                var newUnreadCount = parseInt(unreadReplies) - parseInt(repliesCount);
                if (newUnreadCount > 0) {
                    $('.notification-badge').text(newUnreadCount);
                }
                else {
                    $('.notification-badge').hide();
                }
            }
        }
    });

    $(document).on('click', '#discussionReply', function () {
        var discussionId = $(this).parent().attr('id');
        BindReplies(discussionId);
    });

    //pledgeViewModel = new PledgeModel(); 
    //ko.applyBindings(pledgeViewModel, $('#discussionPopup').get(0));

    $(document).on('click', '.reply-entry button', function (event) {
        //event.preventDefault();
        AddReply($(this).attr('id'));
    });

});


function getInvitedMembers() {
    var memdata = new Object();
    memdata.memberId = $('#hdnMemId').val();
    $.ajax({
        url: '/umbraco/surface/FacebookMember/GetInvitedMembers',
        type: 'POST',//JSON.stringify(details),
        dataType: 'json',
        data: JSON.stringify(memdata),
        contentType: 'application/json; charset=utf-8',
        success: function (data) {

            if (data.facebookMembers) {
                if (data.facebookMembers.length == 0) {
                    $('#invitedfriendlist').html('<p id="noData" style="text-align: center">No friends found.</p>');
                }
                else {
                    var fbmembers = $.map(data.facebookMembers, function (item) { return new fbMember(item); });
                    existingItems = existingItems.concat(fbmembers);
                    commonModel.FBMembers(existingItems);
                    $('#noData').remove();
                }
            }
            else {
                $('#invitedfriendlist').html('<p id="noData" style="text-align: center">No friends found.</p>');
            }
            requestPending = false;

        },
        error: function (message) {
            console.log('.eerrror..');
        }
    });


}

function CommonModel() {
    var self = this;
    self.FBMembers = ko.observableArray([]);
    self.RecArticles = ko.observableArray([]);
    self.Pledges = ko.observableArray([]);
    self.AllEvents = ko.observableArray([]);
    self.MyEvents = ko.observableArray([]);
    self.NearEvents = ko.observableArray([]);
    self.Replies = ko.observableArray([]);
}

function fbMember(data) {
    this.MemberId = data.MemberId;
    this.DisplayName = '(' + data.DisplayName + ')';
    this.FullName = data.FullName;
    this.FacebookId = data.FacebookId;
    this.ProfilePic = data.ProfilePic;

}


function fbMemberPledges(data) {
    this.PledgeId = data.PledgeId;
    this.Title = data.Title;
    this.Members = data.Members + ' members';
    this.PledgeUrl = data.PledgeUrl;
    this.Type = data.Type;
}

//Discussion tab methods starts

var existingArticles = [];
var recArticleModel;
var requestPending = false;
var pageIndex = 0;
var PageSize = 12;
var totalPages;



function getRecommendedArticles(pageIndex) {

    if (!requestPending) {
        requestPending = true;
        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Ambassador/GetRecommendedArticlesForDashboard',
            success: function (data) {
                if (data == null || data.articles == null || data.articles.length <= 0) {
                    $('.content-panel .large-6').html('<h2 class="contained-title inverted">Recommended Articles</h2><h3>No Matching Articles Found.</h3>');
                    return;
                }
                var articles = $.map(data.articles, function (item) { return new article(item); });
                existingArticles = existingArticles.concat(articles);
                commonModel.RecArticles(existingArticles);

                requestPending = false;
                totalPages = data.totalPages;

                setTimeout(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');

                }, 300);
                markLikes();

                $('.has-badge .item-share .small-link').unbind('click').click(function () {
                    var currentItem = $(this).parents('.item-content').find('.item-heading a');
                    var url = window.location.protocol + '//' + window.location.host + currentItem.attr('href');
                    globalShare(url, 'Article');
                    return false;
                });
                $('.full-on-mobile').load(function () {
                    masonryTiles();
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                });

            },
            error: function (data) {
                alert('error');
            }

        });
    }
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
    this.ArticleThumbnail = data.ArticleThumbnail;
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

//Discussion tab methods finish

//Create Pledge Model function for View Model
function PledgeModel() {
    var self = this;

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
                commonModel.Replies(replies);


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
                    $('#discussionPopup').html('<span id="spnForeachReplies" style="margin: 200px 47px;">No Conversation/Replies Found.</span>');
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
    if ($.trim($('button[id="' + parentId + '"]').parents('.reply-entry').find('#discussions-reply').val()) != "") {
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
                commonModel.Replies(replies);

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

var MyEventsModel, AllEventsModel;
var allEventsCount, myEventsCount;

$(document).ready(function () {

    $('#share-event').click(function () {

        var url = $('.eventLinkToShare').attr('href');
        var title = $('#titleEvent').text();
        var description = $('#eventDescription').text();
        $('#twEvent').attr('href', 'https://twitter.com/share?url=' + encodeURIComponent(url) + '&text=Rexona : I Will Do - Event : ' + title + '%20&title=' + title);

        // share to google+
        $('#ggEvent').attr('href', 'https://plus.google.com/share?url=' + url);

        $('#fbEvent').attr('href', 'https://www.facebook.com/dialog/feed?app_id=' + $('#hdnFbAppId').val() + '&' +
        'link=' + encodeURIComponent(url) + '&' +
        'picture=http://' + window.location.host + '/images/Rexona_Invite.png&' +
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


    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        type: 'GET',
        url: '/umbraco/surface/Event/GetEvents',
        success: function (data) {

            if (data.status == true) {
                console.log(data);
                var topData = $.map(data.events, function (item) { return new Event(item); });

                commonModel.AllEvents(topData);
                var MyData = $.map(data.events, function (item) {
                    if (item.IsOwner || item.IsJoined) {
                        return new Event(item);
                    }
                });

                commonModel.MyEvents(MyData);

                var memberPostCode = $('#hdnMemPostcode').val() ? $('#hdnMemPostcode').val() : "";
                var memberCapitalCity = $('#hdnMemCaptialCity').val() ? $('#hdnMemCaptialCity').val() : "";


                var NearData = $.map(data.events, function (item) {
                    var capitalMatch = memberCapitalCity == "" || item.CapitalCity == "" ? false : item.CapitalCity.toLowerCase() == memberCapitalCity.toLowerCase();
                    if (item.PostCode == memberPostCode || capitalMatch) {
                        return new Event(item);
                    }
                });

                commonModel.NearEvents(NearData);

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
            console.log(data);
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
	shareUrl = EventURL ;
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
                        if (data.result.IsJoined) {
                            $('#leaveEvent').show();
                            $('#JoinEvent').hide();
                        }
                        else {
                            $('#JoinEvent').show();
                            $('#leaveEvent').hide();
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

var attending = true

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
