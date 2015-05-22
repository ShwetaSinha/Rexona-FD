var reportModel, homeAmbassadorModel;
var existingItems = [];
var rowcountArticle = 0;
var successmethodExecutedCount = 0;
var videoData;
$(document).ready(function () {

    reportModel = new ReportViewModel();
    ko.applyBindings(reportModel);

    fetchReport('', '');
    fetchGoalsReport('', '');
    fetchUsersReport('', '');

    fetchVideoStat('', '', $('#VideoSort').val());

    $('#btnExport').click(function (event) {
        event.preventDefault();
        var contentTab = $('li.current a').html();
        var startDate = '', endDate = '';
        if ($('#reportrange2 span').html() != 'All Time') {
            startDate = $.trim($('#reportrange2 span').html().split('-')[0]);
            endDate = $.trim($('#reportrange2 span').html().split('-')[1]);
        }

        ExportReport(contentTab, startDate, endDate);
    });
    $('#VideoSort').change(function () {

        if (videoData == null || videoData.length == 0) {
            return;
        }
        $('#divLoader').show();

        var sortedVideo;
        if (this.value == 'popular') {
            videoData.sort(function (a, b) { return (a.Likes == b.Likes) ? 0 : ((a.Likes < b.Likes) ? 1 : -1); });
        }
        else if (this.value == 'plays') {
            videoData.sort(function (a, b) { return (a.TotalPlays == b.TotalPlays) ? 0 : ((a.TotalPlays < b.TotalPlays) ? 1 : -1); });
        }
        else if (this.value == 'recent') {
            videoData.sort(function (a, b) { return (a.CreatedDate == b.CreatedDate) ? 0 : ((a.CreatedDate < b.CreatedDate) ? 1 : -1); });
        }
        sortedVideo = $.map(videoData, function (item) { return new topVideos(item); });

        
        reportModel.VideoStat(sortedVideo);
        $('#divLoader').hide();
    });
});

function ExportReport(contentTabName, startDate, endDate) {

    var params = new Object();
    params.contentTabName = contentTabName;
    params.startDate = startDate;
    params.endDate = endDate;

    if (contentTabName == 'Users') {

        var TotalUsers = $('#totalUsers label').html();
        var ActiveUsers = $('#activeUsers  label').html();
        var InvitesSent = $('#invitesSent label').html();
        var InvitesAccepted = $('#inviteAccepted label').html();
        document.location.href = '/umbraco/surface/Export/UsersReporting?startDate=' + startDate + '&endDate=' + endDate + '&TotalUsers=' + TotalUsers + '&ActiveUsers=' + ActiveUsers + '&InvitesSent=' + InvitesSent + '&InvitesAccepted=' + InvitesAccepted;

    }
    else if (contentTabName == 'Goals') {

        var TotalGoalsCreated = $('#totalGoals').text().split(' ')[0];
        var UniqueGoalsCreated = $('#uniqueGoals').text().split(' ')[0];
        var EventsCreated = $('#totalEvents').text().split(' ')[0];
        var DiscussionsCreated = $('#totalDiscussions').text().split(' ')[0];
        var MembersJoined = $('#membersJoined').text().split(' ')[0];

        var key, value;
        var dictionary = {};
        $(".chart-details li").each(function (index, value) {
            key = $(this).find('span').eq(0).html().replace('&amp;', 'and');
            value = $(this).find('span').eq(1).html().replace('(', '').replace(')', '');
            dictionary[key] = value;
        });

        var DropOff = JSON.stringify(dictionary);

        document.location.href = '/umbraco/surface/Export/GoalsReporting?startDate=' + startDate + '&endDate=' + endDate + '&TotalGoalsCreated=' + TotalGoalsCreated + '&UniqueGoalsCreated=' + UniqueGoalsCreated + '&EventsCreated=' + EventsCreated + '&DiscussionsCreated=' + DiscussionsCreated + '&DropOff=' + DropOff + '&MembersJoined=' + MembersJoined;

    }
    else if (contentTabName == 'Videos') {

        var totalPlays = $('#totalPlays').html().split('<span>')[0];
        var uniquePlays = $('#uniquePlays').html().split('<span>')[0];
        var totalVideos = $('#totalVideos').html().split('<span>')[0];
        var searchText = $('#VideoSort').val();

        document.location.href = '/umbraco/surface/Export/VideosReporting?totalPlays=' + totalPlays + '&uniquePlays=' + uniquePlays + '&totalVideos=' + totalVideos + '&searchText=' + searchText;

    }
    else {
        document.location.href = '/umbraco/surface/Export/ContentReporting?contentTabName=' + contentTabName + '&startDate=' + startDate + '&endDate=' + endDate;
    }
}

function ReportViewModel() {
    var self = this;
    self.Content = ko.observableArray([]);
    self.Goals = ko.observableArray([]);
    self.Users = ko.observableArray([]);
    self.Videos = ko.observableArray([]);
    self.Articles = ko.observableArray([]);
    self.Tweets = ko.observableArray([]);
    self.Instagram = ko.observableArray([]);
    self.VideoStat = ko.observableArray([]);
}

function ContentData(data) {

    this.AISArticleViews = ko.computed(function () {
        if (data.AISArticleViews == 1) {
            return data.AISArticleViews + ' <span>Total View</span>';
        }
        else {
            return data.AISArticleViews + ' <span>Total Views</span>';
        }
    });

    this.AISArticles = ko.computed(function () {
        if (data.AISArticles == 1) {
            return data.AISArticles + ' <span>Total article</span>'
        }
        else
            return data.AISArticles + ' <span>Total articles</span>'
    });

    this.AmbassadorArticles = ko.computed(function () {
        if (data.AmbassadorArticles == 1) {
            return data.AmbassadorArticles + ' <span>Total article</span>'
        }
        else
            return data.AmbassadorArticles + ' <span>Total articles</span>'
    });

    this.AmbassadorArticleViews = ko.computed(function () {
        if (data.AmbassadorArticleViews == 1) {
            return data.AmbassadorArticleViews + ' <span>Total View</span>'
        }
        else {
            return data.AmbassadorArticleViews + ' <span>Total Views</span>'
        }
    });

    this.DoMoreArticles = ko.computed(function () {
        if (data.DoMoreArticles == 1)
        { return data.DoMoreArticles + ' <span>Total article</span>' }
        else
            return data.DoMoreArticles + ' <span>Total articles</span>'
    });

    //this.DoMoreArticleViews = ko.observable(data.DoMoreArticleViews);
    this.DoMoreArticleViews = ko.computed(function () {
        if (data.DoMoreArticleViews == 1) {
            return data.DoMoreArticleViews + ' <span>Total View</span>'
        }
        else {
            return data.DoMoreArticleViews + ' <span>Total Views</span>'
        }
    });

    this.InstaPosts = ko.observable(data.InstaPosts);
    this.Tweets = ko.observable(data.Tweets);
    this.SocialPosts = ko.observable(data.SocialPosts);

    this.AwaitingApproval = ko.computed(function () {
        return data.AwaitingApproval + ' <span>Awaiting Moderation</span>';
    });

    this.RejectedPosts = ko.computed(function () {
        if (data.RejectedPosts.length == 1) {
            return data.RejectedPosts + '<span>Rejected Post</span>';
        }
        else
            return data.RejectedPosts + '<span>Rejected Posts</span>';
    });
    //

    this.ApprovedPosts = ko.computed(function () {
        if (data.ApprovedPosts.length == 1) {
            return data.ApprovedPosts + '<span>Approved Post</span>';
        }
        else
            return data.ApprovedPosts + '<span>Approved Posts</span>';
    });
}

function Article(data) {
    // this.ArticleName = ko.observable(data.ArticleName);
    this.TotalViews = ko.observable(data.TotalViews);

    this.ArticleName = ko.computed(function () {
        rowcountArticle = rowcountArticle + 1;
        return rowcountArticle + '. ' + data.ArticleName + '<span>( ' + data.TotalViews + ' views)</span>';

    });
}

function tweets(data) {
    //this.Author = ko.observable(data.Author);

    this.Author = ko.computed(function () { return 'posted by ' + data.Author });
    this.Date = ko.observable(data.DateToDisplay);
    //this.Likes = ko.observable(data.Likes);
    this.tweet = ko.observable(data.TweetText);
    this.Likes = ko.computed(function () { return data.Likes + ' Likes' });
}

function instagram(data) {
    this.Author = ko.computed(function () { return 'posted by ' + data.Author });
    this.Date = ko.observable(data.DateToDisplay);
    //this.Likes = ko.observable(data.Likes);
    this.MediaURL = ko.observable(data.MediaURL);
    this.post = ko.observable(data.TweetText);
    this.Likes = ko.computed(function () { return data.Likes + ' Likes' });
    this.LinkUrl = ko.observable(data.TweetURL);
}

function topVideos(data) {
    this.elementID = data.VideoId;
    this.VideoId = ko.computed(function () {
        return 'https://img.youtube.com/vi/' + data.VideoId + '/mqdefault.jpg';
    });
    this.VideoName = ko.computed(function () {
        if (data.VideoName == '/') {
            return '/home';
        }
        else {
            return data.VideoName;
        }
    });
    this.VideoURL = ko.computed(function () {
        return 'https://www.youtube.com/watch?v=' + data.VideoId;
    });
    this.TotalPlays = ko.observable(data.TotalPlays);
    this.TotalStops = ko.observable(data.TotalStops);
    this.AvgViews = ko.observable(data.AvgViews);
    this.Likes = ko.observable(data.Likes);
    this.Views = ko.observable(data.Views);
    this.CreatedDate = ko.observable(data.CreatedDate);
}

function goalsData(data) {

    //this.TotalGoals = ko.observable(data.TotalGoals);

    this.TotalGoals = ko.computed(function () { return data.TotalGoals + ' <span>Total Goals Created</span>' });

    this.UniqueGoals = ko.computed(function () { return data.UniqueGoals + ' <span>Unique Goals Created</span>' });

    this.Events = ko.computed(function () { return data.Events + ' <span>Events Created</span>' });

    this.MembersJoined = ko.computed(function () { return data.MembersJoined + ' <span>Total Goals Joined</span>' });
    //this.UniqueGoals = ko.observable(data.UniqueGoals);
    //this.Events = ko.observable(data.Events);
    //this.Discussions = ko.observable(data.Discussions);
    this.Discussions = ko.computed(function () { return data.Discussions + ' <span>Discussions Created</span>' });
    this.OpenGoals = ko.observable(data.OpenGoals);
    this.ClosedGoals = ko.observable(data.ClosedGoals);
}

function fetchReport(startDate, endDate) {

    $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '0.3');
    $('#divLoader').show();
    var contentTab;
    var topArticles, toptweets, topInstaPosts;

    var params = new Object();
    params.startDate = startDate;
    params.endDate = endDate;
    $('.top-instragram-list div').eq(0).html('');
    $('.top-tweets div').eq(0).html('');
    $('.top-article-list ul').html('');
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        type: 'GET',
        data: params,
        url: '/umbraco/surface/Report/ContentReporting',
        success: function (data) {

            if (data.message == 'Success') {

                
                contentTab = new ContentData(data.report);
                existingItems = existingItems.concat(contentTab);
                reportModel.Content(existingItems);
                existingItems = [];

                rowcountArticle = 0;
                
                topArticles = $.map(data.topArticles, function (item) { return new Article(item); });
                reportModel.Articles(topArticles);
                if (data.topArticles.length == 0) {
                    $('.top-article-list ul').html('No records found');
                }

                topInstaPosts = $.map(data.topInsta, function (item) { return new instagram(item); });
                
                reportModel.Instagram(topInstaPosts);
                if (data.topInsta.length == 0) {
                    $('.top-instragram-list div').eq(0).html('No records found');
                }
                toptweets = $.map(data.topTweet, function (item) { return new tweets(item); });
                
                reportModel.Tweets(toptweets);
                if (data.topTweet.length == 0) {
                    $('.top-tweets div').eq(0).html('No records found');
                }

                //$('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
                successmethodExecutedCount = successmethodExecutedCount + 1;
                HideLoadingDiv();
            }
        },
        error: function (data) {
            console.log('Error Occured');
            $('.top-article-list ul').html('No records found');
            $('.top-instragram-list div').eq(0).html('No records found');
            $('.top-tweets div').eq(0).html('No records found');
            $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
            $('#divLoader').hide();
        }
    });
}

function fetchGoalsReport(startDate, endDate) {

    var goals;

    var params = new Object();
    params.startDate = startDate;
    params.endDate = endDate;

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        type: 'GET',
        data: params,
        url: '/umbraco/surface/Report/GoalsReporting',
        success: function (data) {
            if (data.message == 'Success') {

                //goals details 
                $('#totalGoals').html(data.goal.TotalGoals + ' <span>Total Goals<br/> Created</span>');
                $('#uniqueGoals').html(data.goal.UniqueGoals + ' <span>Unique Goals<br/> Created</span>');
                $('#totalEvents').html(data.goal.Events + ' <span>Events<br/> Created</span>');
                $('#totalDiscussions').html(data.goal.Discussions + ' <span>Discussions<br/> Created</span>');
                $('#membersJoined').html(data.goal.MembersJoined + ' <span>Total Goals<br/> Joined</span>');

                //drop-offs details
                $('#signup').html('(' + data.dropoffs.SignUp + '% dropoff)');
                $('#step1').html('(' + data.dropoffs.EnterGoal + '% dropoff)');
                $('#steptakePhoto').text('(' + data.dropoffs.TakePhoto + '% dropoff)');
                $('#stepHappy').html('(' + data.dropoffs.HappyWithPhoto + '% dropoff)');
                $('#step3').html('(' + data.dropoffs.ConfirmGoal + '% dropoff)');

                //$('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
                successmethodExecutedCount = successmethodExecutedCount + 1;
                HideLoadingDiv();
            }

        },
        error: function (data) {
            console.log('Error Occured');
            $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
            $('#divLoader').hide();
        }
    });
}

function fetchUsersReport(startDate, endDate) {
    var params = new Object();
    params.startDate = startDate;
    params.endDate = endDate;

    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        type: 'GET',
        data: params,
        url: '/umbraco/surface/Report/UsersReporting',
        success: function (data) {
            // var topData = $.map(data.topContent, function (item) { return new TopContent(item); });
            if (data.message == 'Success') {
                $('#totalUsers').html('<label>' + data.Users.TotalUsers + '</label> <span>Total Users</span>');
                $('#activeUsers').html('<label>' + data.Users.ActiveUsers + '</label>  <span>Active Users</span>');
                $('#invitesSent').html('<label>' + data.Users.InvitedUsers + '</label>  <span>Invites Sent</span>');
                $('#inviteAccepted').html('<label>' + data.Users.InviteAcceptedUsers + '</label>  <span>Invites Accepted</span>');

                //$('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
                successmethodExecutedCount = successmethodExecutedCount + 1;
                HideLoadingDiv();
            }
        },
        error: function (data) {
            console.log('Error Occured');
            $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
            $('#divLoader').hide();

        }
    });
}

function fetchVideoStat(startDate, endDate, filterType) {
    $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '0.3');
    $('#divLoader').show();
    var params = new Object();
    params.startDate = startDate;
    params.endDate = endDate;
    params.filterType = filterType;
    var videos;
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        type: 'GET',
        data: params,
        url: '/umbraco/surface/Report/VideoStat',
        success: function (data) {
            $('#noData').remove();
            if (data.message == 'Success') {
                if (data.Video != null || data.Video != undefined) {
                    $('#totalPlays').html(data.Video.TotalPlays + '<span> Total Plays</span>');
                    $('#uniquePlays').html(data.Video.UniquePlays + '<span> Unique Plays</span>');
                    $('#totalVideos').html(data.Video.TotalVideos + '<span> Total Videos</span>');
                }
                else {
                    $('#totalPlays').html(0 + '<span> Total Plays</span>');
                    $('#uniquePlays').html(0 + '<span> Unique Plays</span>');
                    $('#totalVideos').html(0 + '<span> Total Videos</span>');
                }


                if (!data.VideoList || data.VideoList.length == 0) {
                    $('#tabs-4 .social h2').after('<h3 id="noData">No Data Found</h3>');
                }
                else {

                    videos = $.map(data.VideoList, function (item) { return new topVideos(item); });
                    videoData = data.VideoList;
                    

                    reportModel.VideoStat(videos);
                    $successmethodExecutedCount = successmethodExecutedCount + 1;
                    HideLoadingDiv();
                    successmethodExecutedCount = 0;
                }

                $('#divLoader').hide();
                $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
                videos = null;

            }
            else {
                $('#totalPlays').html(0 + '<span> Total Plays</span>');
                $('#uniquePlays').html(0 + '<span> Unique Plays</span>');
                $('#totalVideos').html(0 + '<span> Total Videos</span>');
                $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
                videoData = null;
                videos = null;
                reportModel.VideoStat(videos);
                $('#tabs-4 .social h2').after('<h3 id="noData">No Data Found</h3>');
                $('#divLoader').hide();
            }
        },
        error: function (data) {
            console.log('Error Occured');
            $('#noData').remove();
            $('#totalPlays').html(0 + '<span> Total Plays</span>');
            $('#uniquePlays').html(0 + '<span> Unique Plays</span>');
            $('#totalVideos').html(0 + '<span> Total Videos</span>');
            $('#tabs-4 .social h2').after('<h3 id="noData">No Data Found</h3>');

            $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
            $('#divLoader').hide();
        }
    });
}

function HideLoadingDiv() {
    if (successmethodExecutedCount > 3) {
        $('#divLoader').hide();
        $('#tabs-1, #tabs-2, #tabs-3, #tabs-4').css('opacity', '1');
        successmethodExecutedCount = 0;
    }
}