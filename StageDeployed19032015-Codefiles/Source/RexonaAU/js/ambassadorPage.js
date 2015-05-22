$(document).ready(function () {

    //ko
    topAmbassadorModel = new TopAmbassadorViewModel();
    ko.applyBindings(topAmbassadorModel, document.getElementById('topAmbassadors'));

    ambassadorArticleViewModel = new AmbassadorArticleViewModel();
    ko.applyBindings(ambassadorArticleViewModel, document.getElementById('articles'));



    //function calls
    getAmbassadorArticles(pageIndex, false);
    getTopAmbassadors(topAmbassadorModel);

    $(window).scroll(function (e) {
        if (!requestPending && (pageIndex + 1 < totalPages)) {
            if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
                pageIndex = pageIndex + 1;
                getAmbassadorArticles(pageIndex, false);
            }
        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    $('#ambassadorFilter').change(function () {

        existingItems = [];
        pageIndex = 0;
        var scrollTop = $(window).scrollTop();
        $('.masonry-wall').fadeOut(200);
        setTimeout(function () {

        }, 200);
        getAmbassadorArticles(pageIndex, false);

        setTimeout(function () {
            $('.masonry-wall').fadeIn(100);
            setTimeout(function () {
                $(window).scrollTop(scrollTop);
            }, 10);
        }, 400);
    });

    $(document).on('click', '.small-link:contains(Share)', function (e) {
        var currentItem = $(this).parents('.item-content').find('.item-heading a');
        var url = window.location.protocol + '//' + window.location.host + currentItem.attr('href');
       // console.log(url);
        var data = currentItem.find(':header').text();
        e.preventDefault();
        globalShare(url, 'article');
    });

    $(document).on('click', '.rating-icon', function (e) {

        //console.log(this.id);
        likeCount('like', this.id);
    });
});