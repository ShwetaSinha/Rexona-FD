
$(document).ready(function () {

    var enteredHashTag = $.trim($('#txtTwitter').val());
    startLoad();
    $('#divTwitterResults table').hide();
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        type: 'GET',
        url: '/umbraco/surface/SocialContent/GetTweets',
        success: function (data) {
            // console.log(data);
            var tweets = $.map(data, function (item) { return new Tweet(item); });
            viewModel.tweets(tweets);
            if (tweets.length == 0) {
                $('#divTwitterResults table tbody').html("<span style='color:red;'>No results found</span>");
            }
            $('#divTwitterResults,#divTwitterResults table').show();
            loadDone();
        },
        error: function (data) {
            console.log('Error Occured');
            loadDone();
        }
    });


});

//$(document).on('click','.approve',function () {
function approveContent() {
    startLoad();
    $('#' + this.Id()).hide();
    var tweet = new Object();
    tweet.entryId = this.Id();
    //console.log(this.Id());
    $.ajax({
        type: 'POST',
        contentType: "application/x-www-form-urlencoded",
        url: "/umbraco/surface/SocialContent/ApproveEntry/",
        async: true,
        data: tweet,
        success: function (data) {
            //console.log(data);
            if (data==true) {
                loadDone();
                alert('The selected entry has been approved');
            }
            else {
                loadDone();
                alert('Something went wrong');
            }
        }
    });
}
function rejectContent() {
    startLoad();
    $('#' + this.Id()).hide();
    var tweet = new Object();
    tweet.entryId = this.Id();
    $.ajax({
        type: 'POST',
        contentType: "application/x-www-form-urlencoded",
        url: "/umbraco/surface/SocialContent/RejectEntry/",
        async: true,
        data: tweet,
        success: function (data) {
            //console.log(data);
            if (data==true) {
                loadDone();
                alert('The selected entry has been Rejected');
            }
            else {
                loadDone();
                alert('Something went wrong');
            }
        }
    });
}


function approveInstaContent() {
    
    startLoad();
    $('#' + this.ID()).hide();

    var instPost = new Object();
    instPost.entryId = this.ID();

    $.ajax({
        type: 'POST',
        contentType: "application/x-www-form-urlencoded",
        url: "/umbraco/surface/SocialContent/ApproveInstaGramEntry/",
        async: true,
        data: instPost,
        success: function (data) {
            if (data == true) {
                loadDone();
                alert('The selected entry has been approved');
            }
            else {
                loadDone();
                alert('Something went wrong');
            }
        }
    });
}
function rejectInstaContent() {
    startLoad();
    $('#' + this.ID()).hide();
    var instPost = new Object();
    instPost.entryId = this.ID();

    $.ajax({
        type: 'POST',
        contentType: "application/x-www-form-urlencoded",
        url: "/umbraco/surface/SocialContent/RejectInstaEntry/",
        async: true,
        data: instPost,
        success: function (data) {
            if (data == true) {
                loadDone();
                alert('The selected entry has been Rejected');
            }
            else {
                loadDone();
                alert('Something went wrong');
            }
        }
    });
}


function Tweet(data) {
    this.Id = ko.observable(data.Id);
    this.Author = ko.observable(data.UserName);
    this.TweetText = ko.observable(data.Post);
  //  this.ProfilePic = ko.observable(data.ProfilePic);
    this.ScreenNameResponse = ko.observable(data.ScreeName);
    this.CreatedAt = ko.observable(data.CreatedDate);
   // this.FriendCount = ko.observable(data.FriendCount);
    this.TwitterURL = ko.observable(data.PostUrl);
    this.MediaURL = ko.observable(data.Post);
   // this.ProfileURL = ko.observable(data.ProfileURL);
    this.likes = ko.observable(data.Likes);
}

function Instagram(data) {
    //console.log(data);
    this.ID = ko.observable(data.Id);
    this.captiontext = ko.observable(data.Post);
    this.MediaURL = ko.observable(data.MediaURL);
    this.link = ko.observable(data.PostUrl);
    this.ScreenNameResponse = ko.observable(data.ScreeName);
    this.CreatedAt = ko.observable(data.CreatedDate);
    this.likes = ko.observable(data.Likes);
    this.buttonId = ko.computed(function () {
        return 'btn' + data.ID;
    });
    this.user = ko.observable(data.ScreeName);
}


