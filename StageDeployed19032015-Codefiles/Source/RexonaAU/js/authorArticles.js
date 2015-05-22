var existingItems = [];

var requestPending = false;
var pageIndex = 0;
var PageSize = 24;
var totalPages;

var individualArticleModel;
$(document).ready(function () {

    //ko

    individualArticleModel = new AmbassadorArticleViewModel();
    ko.applyBindings(individualArticleModel, document.getElementById('articles'));


    //function calls
    getIndividualArticles(pageIndex, false);

    $(window).scroll(function (e) {
        if (!requestPending && (pageIndex + 1 < totalPages)) {
            if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
                pageIndex = pageIndex + 1;
                getIndividualArticles(pageIndex, false);
            }
        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    $('#ambassadorDrop').change(function () {

        existingItems = [];
        pageIndex = 0;
        var scrollTop = $(window).scrollTop();
        $('.masonry-wall').fadeOut(200);
        
        getIndividualArticles(pageIndex, false);

        setTimeout(function () {
            $('.masonry-wall').fadeIn(500);
            setTimeout(function () {
                $(window).scrollTop(scrollTop);
            }, 10);
        }, 500);
    });

    $(document).on('click', '.rating-icon', function (e) {

        //console.log(this.id);
        likeCount('like', this.id);
    });

    $(document).on('click', '.small-link:contains(Share)', function (e) {
        var currentItem = $(this).parents('.item-content').find('.item-heading a');
        var url = window.location.protocol + '//' + window.location.host + currentItem.attr('href');
        // console.log(url);
        var data = currentItem.find(':header').text();
        e.preventDefault();
        globalShare(url, 'Article');
    });

});


function getIndividualArticles(pageIndex, showLightBox) {

    if (!requestPending) {
        requestPending = true;
        var dataToSendToAjax = new Object();
        dataToSendToAjax.PageSize = PageSize;
        dataToSendToAjax.currentPageIndex = pageIndex;
        dataToSendToAjax.SortingText = $('#ambassadorDrop option:selected').text().toUpperCase();

        dataToSendToAjax.ArticleType = "Ambassador";
        dataToSendToAjax.AId = $('#hdnId').val();
        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            cache: false,
            data: dataToSendToAjax,
            type: 'GET',
            url: '/umbraco/surface/Ambassador/GetIndividualArticles',
            success: function (data) {
                //console.log(data);
                var articles = $.map(data.articles, function (item) { return new article(item); });
                existingItems = existingItems.concat(articles);
                individualArticleModel.Articles(existingItems);

                requestPending = false;
                totalPages = data.totalPages;

                setTimeout(function () {
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');

                    var top = $('.item.has-badge:last').offset().top + $('.item.has-badge:last').height() + 60;
                    $('#footer').offset({ top: top }).css('position','');
                }, 300);

                $('.full-on-mobile').load(function () {
                    masonryTiles();
                    $('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');
                    $('.masonry-wall').masonry('layout');
                });

                markLikes();
            },
            error: function (data) {
                alert('error');
            }

        });
    }
}