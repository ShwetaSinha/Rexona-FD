var existingItems = [];
var recArticleModel, usersPledgeModel;
var requestPending = false;
var pageIndex = 0;
var PageSize = 12;
var totalPages;
var existingPledges = [];

$(document).ready(function () {

    //ko
    $('#recommendPopupBtn').hide();
    recArticleModel = new RecArticleModel();
    ko.applyBindings(recArticleModel, document.getElementById('recommendedArticlesSection'));

    usersPledgeModel = new UsersPledgeModel();
    ko.applyBindings(usersPledgeModel, document.getElementById('Pledgelist'));

    $('#recommendPopupBtn').click(function (e) {
        e.preventDefault();
        $('input[type=checkbox]').removeAttr('checked');
        openLightbox('.pledge-share');
    });

    $('#shareWithPledges').click(function () {
        var pledgelist = $('input[type=checkbox]').map(function () {
            if (this.checked)
                return this.id;
        }).get().join(',');
        console.log(pledgelist);
        var Pledge = new Object();
        Pledge.pledgeIds = pledgelist;
        Pledge.articleId = $('#hdnArticleId').val();
        //recommend Ajax
        $.ajax({
            url: '/umbraco/surface/FacebookMember/Recommend',
            type: 'POST',//JSON.stringify(details),
            dataType: 'json',
            data: JSON.stringify(Pledge),
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                $('input[type=checkbox]:checked').each(function () {
                    var parent = $(this).parents('li.user');
                    $(this).parent().html('<h5>Recommended</h5>');
                    setTimeout(function () {
                        parent.slideUp("slow", function () { parent.remove() });;

                    }, 1500);

                });
            },
            error: function (message) {

            }
        });



    });

    //$('#socialShare').click(function(){
    //    globalShare(window.location.href, 'Article ');
    //});

    $(document).on('click', '.rating-icon', function (e) {

        //console.log(this.id);
        likeCount('like', this.id);
    });

    $(document).on('click', '.small-link:contains(Share)', function (e) {

        if ($(this).is('#socialShare')) {
            globalShare(window.location.href, 'Article ');
        } else {
            var currentItem = $(this).parents('.item-content').find('.item-heading a');
            var url = window.location.protocol + '//' + window.location.host + currentItem.attr('href');
            // console.log(url);
            var data = currentItem.find(':header').text();
            e.preventDefault();
            globalShare(url, 'article');
        }
    });

    getRecommendedArticles(recArticleModel);

    getUserPublicPledges();

    $("#signUpForUpdates").click(function () {
        signupForUpdates();
    });
    $('.article img').each(function () {
        if ($(this).attr('src').indexOf('http') == -1) {

            $(this).attr('src', 'http://iwilldo.rexona.com.au'+$(this).attr('src'));
        }
    });
});


function signupForUpdates() {
    if (!requestPending) {

        requestPending = true;

        var emailAddress = $("#txtEmailId").val();

        var dataToSend = new Object();
        dataToSend.emailAddress = emailAddress;

        if (emailAddress != '') {

            $.ajax({
                contentType: "application/json; charset=utf-8",
                dataType: 'json',
                cache: false,
                async: true,
                data: JSON.stringify(dataToSend),
                type: 'POST',
                url: '/umbraco/surface/Ambassador/SignUpForUpdates',
                success: function (data) {
                    debugger;

                    if (data == "true") {
                        new jBox('Notice', {
                            content: 'Successfully signed-up for updates.',
                            color: 'green',
                            theme: 'NoticeBorder'
                        });
                    } else {
                        new jBox('Notice', {
                            content: 'Oops,something went wrong,please try again',
                            color: 'red',
                            theme: 'NoticeBorder'
                        });
                    }
                },
                error: function (data) {
                    debugger;

                    new jBox('Notice', {
                        content: 'Oops,something went wrong,please try again',
                        color: 'red',
                        theme: 'NoticeBorder'
                    });
                }
            });
        } else {
            new jBox('Notice', {
                content: 'Email address is required for signing up.',
                color: 'red',
                theme: 'NoticeBorder'
            });
        }
    }
}

function getUserPublicPledges() {
    var dataToSend = new Object();
    dataToSend.CurrentMemberId = $('#hdnMemberId').val();
    dataToSend.ArticleId = $('#hdnArticleId').val();
    if ($('#hdnMemberId').val() !== "0") {
        $.ajax({
            url: '/umbraco/surface/FacebookMember/GetPledgesForRecommend',
            type: 'POST',//JSON.stringify(details),
            dataType: 'json',
            data: JSON.stringify(dataToSend),
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                console.log(data);
                if (data.pledges != null && data.pledges.length) {
                    var pledges = $.map(data.pledges, function (item) {

                        return new UserPledges(item);

                    });
                    existingPledges = existingPledges.concat(pledges);
                    usersPledgeModel.Pledges(existingPledges);

                    requestPending = false;
                    existingPledges = [];
                    $('#recommendPopupBtn').show();

                }
                else {
                    $('#recommendPopupBtn').hide();
                }
                runCards();

            },
            error: function (message) {
                console.log('.eerrror..');
                runCards()
            }
        });
    }

}

function getRecommendedArticles(recArticleModel) {

    if ($('#hdnTags').val() != "") {
        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        //dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.Tag = $('#hdnTags').val();
        dataToSendToAjax.ArticleId = $('#hdnArticleId').val();
        dataToSendToAjax.ArticleType = "ALL";

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Ambassador/GetRecommendedArticles',
            success: function (data) {
                console.log(data);
                var articles = $.map(data.articles, function (item) { return new article(item); });
                existingItems = existingItems.concat(articles);
                recArticleModel.RecArticles(existingItems);

                totalPages = data.totalPages;

                setTimeout(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');

                }, 300);

                markLikes();
            },
            error: function (data) {
            }

        });
    }
}

function RecArticleModel() {
    var self = this;
    self.RecArticles = ko.observableArray([]);
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

    this.ArticleThumbnail = ko.observable(data.ArticleThumbnail);
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


function UsersPledgeModel() {
    var self = this;
    self.Pledges = ko.observableArray([]);
}

function UserPledges(data) {
    this.PledgeId = data.PledgeId;
    this.Title = data.Title;
    this.Members = data.Members + ' members';
    this.PledgeUrl = data.PledgeUrl;
    this.Type = data.Type;
    this.checkboxId = ko.computed(function () {
        return data.PledgeId;
    })
}