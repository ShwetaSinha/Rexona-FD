var existingItems = [];
var ambassadorArticleViewModel, topAmbassadorModel;
var requestPending = false;
var pageIndex = 0;
var PageSize = 24;
var totalPages;






function getAmbassadorArticles(pageIndex, showLightBox) {

    if (!requestPending) {
        requestPending = true;
        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.SortingText = $('#ambassadorFilter option:selected').text().toUpperCase();

        dataToSendToAjax.ArticleType = "Ambassador";

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Ambassador/GetArticles',
            success: function (data) {
                //console.log(data);
                var articles = $.map(data.articles, function (item) { return new article(item); });
                existingItems = existingItems.concat(articles);
                ambassadorArticleViewModel.Articles(existingItems);

                requestPending = false;
                totalPages = data.totalPages;

                setTimeout(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                }, 200);

                $('.full-on-mobile').load(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                });

                markLikes();
            },
            error: function (data) {
                //  alert('error');
            }

        });
    }
}

var ambassadorsData = [];
function getTopAmbassadors(model) {

    //var dataToSendToAjax = new Object();
    //dataToSendToAjax.ArticleType = "Ambassador";
    //data: dataToSendToAjax,
    // console.log(model);
    $.ajax({
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        type: 'GET',
        url: '/umbraco/surface/Ambassador/GetHomePageAmbassadorData',
        success: function (data) {
            //console.log('top');
            //console.log(data);
            var ambassadors = $.map(data, function (item) { return new TopAmbassadors(item); });
            ambassadorsData = ambassadorsData.concat(ambassadors);
            model.TopAmbassador(ambassadorsData);

        },
        error: function (data) {
            //alert('error');
        }

    });
}

function AmbassadorArticleViewModel() {
    var self = this;
    self.Articles = ko.observableArray([]);
}

function TopAmbassadorViewModel() {
    var self = this;
    self.TopAmbassador = ko.observableArray([]);
}

function article(data) {
    this.Id = data.Id;
    this.UploadDate = data.UploadedDateAsString;
    this.ActualArticleURL = data.ActualArticleURL;
    this.ArticleTitle = data.ArticleTitle;
    // this.AmbassadorName = data.AmbassadorName;
    this.Hearts = data.Hearts;
    this.Excerpt = data.Excerpt;
    this.Type = data.Type;
    this.ArticleThumbnail = data.ArticleThumbnail;
    //this.AmbassadorURL = data.AmbassadorURL;
    //this.AmbassadorImage = data.AmbassadorImage;
    this.Author = ko.computed(function () {
        if (data.AmbassadorName == "") {
            return "";
        }
        else { return 'By ' + data.AmbassadorName; }
    });
}

function TopAmbassadors(data) {
    this.Id = ko.observable(data.AmbassadorId);
    this.AmbassadorURL = ko.observable(data.AmbassadorURL);
    // this.AmbassadorImage = ko.observable(data.AmbassadorImage);
    this.AmbassadorImage = ko.computed(function () {
        if (data.AmbassadorImage == "false") {
            return "http://placehold.it/500x400";
        }
        else { return data.AmbassadorImage; }
    });
    this.AmbassadorGoal = ko.observable(data.AmbassadorGoal);
    this.AmbassadorName = ko.computed(function () {
        return data.AmbassadorName.split(' ')[0];
    });
    this.Name = ko.observable(data.AmbassadorName);
    this.AmbassadorDescription = ko.observable(data.AmbassadorDescription);
}