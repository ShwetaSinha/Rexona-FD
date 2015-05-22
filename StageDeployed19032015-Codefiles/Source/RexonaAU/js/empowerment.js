var articleModel;
var currentPageIndex;
var requestPending = false;
var totalPages;
var existingArticles = [];
//To Do Change Page Size 
var ArticlePageSize = 24;


$(document).ready(function () {

    articleModel = new EmpowermentArticleViewModel('#empowerArticles');
    ko.applyBindings(articleModel);
    currentPageIndex = 0;
    getEmpowerArticles(currentPageIndex);

    $(window).scroll(function (e) {

        if (!requestPending && (currentPageIndex + 1 < totalPages)) {
            //console.log('pending' + requestPending);

            if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
                currentPageIndex = currentPageIndex + 1;
                getEmpowerArticles(currentPageIndex);
            }
        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    $('#sortArticle').change(function () {
        existingArticles = [];
        currentPageIndex = 0;
        getEmpowerArticles(currentPageIndex);
    });

    $(document).on('click', '.rating-icon', function (e) {

        //  console.log(this.id);
        likeCount('like', this.id);
    });

    $("#signUpForUpdates").click(function () {
        signupForUpdates();
    });
});

function getEmpowerArticles(pageIndex) {

    if (!requestPending) {

        requestPending = true;

        var sortText = $("#sortArticle option:selected").text();

        var params = new Object();
        params.PageSize = ArticlePageSize;
        params.currentPageIndex = pageIndex;
        params.sortingText = sortText;
        params.ArticleType = "AIS";

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: params,
            type: 'GET',
            url: '/umbraco/surface/Ambassador/GetAISArticles',
            success: function (data) {
                // console.log(data);
                var articles = $.map(data.articles, function (item) { return new article(item); });
                existingArticles = existingArticles.concat(articles);
                articleModel.Articles(existingArticles);

                requestPending = false;
                totalPages = data.totalPages;

                setTimeout(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');

                    $('.has-badge .item-share .small-link').unbind('click').click(function () {
                        var parentId = $(this).parents('.has-badge').find('.rating-icon').attr('Id');
                        var pathToShare = window.location.protocol + '//' + window.location.host + $('#anchor' + parentId).attr('href');
                        globalShare(pathToShare, 'Article');
                        return false;
                    });

                }, 200);


                $('[Id$="spnArticles"]').each(function () {
                    if ($(this).text() == 'DoMore team') {
                        $(this).text('TEAM I WILL DO');
                    }
                });

                $('.full-on-mobile').load(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                });

                markLikes();
            },
            error: function (data) {
                //alert('error');
            }

        });
    }
}

function article(data) {
    this.ID = ko.observable(data.Id);
    this.UploadDate = ko.observable(data.UploadedDateAsString);
    this.ActualArticleURL = ko.observable(data.ActualArticleURL);
    this.ArticleTitle = ko.observable(data.ArticleTitle);
    //this.AmbassadorName = ko.observable(data.AmbassadorName);
    this.Hearts = ko.observable(data.Hearts);
    this.Excerpt = ko.observable(data.Excerpt);
    this.Type = ko.observable(data.Type);
    this.ArticleThumbnail = ko.observable(data.ArticleThumbnail);
    this.AmbassadorURL = ko.observable(data.AmbassadorURL);
    this.AmbassadorImage = ko.observable(data.AmbassadorImage);
    this.Id = ko.computed(function () {
        return "#" + data.Id;
    });

    this.hrefId = ko.computed(function () {
        return 'anchor' + data.Id;
    })
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


function EmpowermentArticleViewModel() {
    var self = this;
    self.Articles = ko.observableArray([]);
}

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