var topContentModel, homeAmbassadorModel;
var currentPageIndex;
var requestPending = false;
var totalPages;
var existingItems = [];
//To Do Change Page Size 
var TopPageSize = 24;


$(document).ready(function () {


    topContentModel = new TopContentViewModel();
    ko.applyBindings(topContentModel, document.getElementById('topContent'));

    currentPageIndex = 0;

    FetchTopContent(currentPageIndex, true);


    homeAmbassadorModel = new TopAmbassadorViewModel();
    ko.applyBindings(homeAmbassadorModel, document.getElementById('homeambassadors'));
    getTopAmbassadors(homeAmbassadorModel);

    $(window).scroll(function (e) {

        if (!requestPending && (currentPageIndex + 1 < totalPages)) {
            //console.log('pending' + requestPending);

            if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
                currentPageIndex = currentPageIndex + 1;
                FetchTopContent(currentPageIndex, false);
            }
        } else {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    $('#sortBy').change(function () {
        existingItems = [];
        currentPageIndex = 0;
        FetchTopContent(currentPageIndex, false);
    });

    $('#filterBy').change(function () {
        existingItems = [];
        currentPageIndex = 0;
        FetchTopContent(currentPageIndex, false);
    });

    $(document).on('click', '.rating-icon', function (e) {

        var source, fieldName;
        source = $('#span' + this.id).attr('text');
        if (typeof source != 'undefined') {
            if (source.trim() == 'Ambassador' || source.trim() == 'AIS' || source.trim() == 'DoMore team' || source.trim() == 'TEAM I WILL DO') {
                fieldName = 'like';
            }
            else if (source.trim() == 'Pledge') {
                fieldName = 'likeCount';

            }
            likeCount(fieldName, this.id);
        }
    });

});


//function manageLike() {

//    console.log($(this).Source());
//    console.log(this.id);
//    likeCount('like', this.id);

//}

function TopContentViewModel() {
    var self = this;

    self.topContent = ko.observableArray([]);
}


function FetchTopContent(pageIndex, ShowLoading) {

    if (!requestPending) {

        requestPending = true;

        //console.log('current page' + pageIndex);

        var filterText = $("#filterBy option:selected").text();
        var sortText = $("#sortBy option:selected").text();
        if ($("#filterBy option:selected").val() == 'empower') {
            filterText = 'Empower Articles';
        }

        var params = new Object();
        params.PageSize = TopPageSize;
        params.currentPageIndex = pageIndex;
        params.sortingText = sortText;
        params.filterText = filterText;
        
        if (pageIndex == 0 && ShowLoading) {
            $('#topContent #divLoading').show();
        }

        $.ajax({
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            type: 'GET',
            data: params,
            url: '/umbraco/surface/TopContent/FetchTopContent',
            success: function (data) {
                //console.log(data.topContent);
                //console.log('pages' + data.totalPages);
                $('#topContent #divLoading').hide();
                var topData = $.map(data.topContent, function (item) { return new TopContent(item); });
                existingItems = existingItems.concat(topData);
                topContentModel.topContent(existingItems);

                requestPending = false;
                totalPages = data.totalPages;

                //setTimeout(function () {
                //    $('.masonry-wall').masonry('reloadItems');
                //    $('.masonry-wall').masonry('layout');
                //}, 300);
                //  $('#sociaContent .item-rate').removeAttr('hover');



                setTimeout(function () {

                    var $container = $('.masonry-wall');
                    // layout Masonry again after all images have loaded
                    $container.imagesLoaded(function () {
                        $container.masonry({
                            itemSelector: '.item'
                        });
                    });
                    //Reload masonry plugin
                    //$('.masonry-wall').masonry();
                    $('.masonry-wall').masonry('reloadItems');


                    //$('[id$="templateDiv"]').removeAttr('style');

                    //For sharing to social sites

                    $('.is-pledge .item-share .small-link').unbind('click').click(function () {
                        var parentId = $(this).parents('.is-pledge').find('.rating-icon').attr('Id');
                        var pathToShare = window.location.protocol + '//' + window.location.host + $('#anchor' + parentId).attr('href');

                        if (pathToShare.indexOf('localhost') > 0) {

                        }
                        globalShare(pathToShare, 'goal');
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

                    $('.has-badge .item-share .small-link').unbind('click').click(function () {
                        var parentId = $(this).parents('.has-badge').find('.rating-icon').attr('Id');
                        var pathToShare = window.location.protocol + '//' + window.location.host + $('#anchor' + parentId).attr('href');

                        globalShare(pathToShare, 'Article');

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




                }, 500);

                $('[Id$="spnArticles"]').each(function () {
                    if ($(this).text() == 'DoMore team') {
                        $(this).text('TEAM I WILL DO');
                    }
                });

                $('.full-on-mobile').load(function () {


                    //masonryTiles();
                    //$('.masonry-wall').masonry();
                    // $('.masonry-wall').masonry('reloadItems');
                    //$('.masonry-wall').masonry('layout');
                });

                markLikes();
            },
            error: function (data) {
                console.log('Error Occured');
                //loadDone();
                endLoad();
            }
        });
    }
}

function TopContent(data) {

    this.Id = ko.observable(data.Id);
    this.ArticleTitle = ko.observable(data.ArticleTitle);
    //  this.Author = ko.observable(data.Author);
    this.Content = ko.observable(data.Content);
    this.Likes = ko.observable(data.Likes);
    this.link = ko.observable(data.LinkUrl);
    //this.ScreenNameResponse = ko.observable(data.username);
    this.CreatedAt = ko.observable(data.CreatedAt);
    this.Source = ko.observable(data.Source);
    this.MediaURL = data.MediaURL;
    this.Style = ko.observable(data.Style);
    this.IsPublicPledge = ko.observable(data.PublicPledge);
    //this.ArticleTitle = ko.observable(data.ArticleTitle);
    this.Excerpt = ko.observable(data.Excerpt);
    this.MemberCount = ko.observable(data.JoinedPeoples);

    this.newAuth = ko.computed(function () { return '- ' + data.Author });

    this.badgeVisible = ko.computed(function () {
        if (data.Source == "AIS") {
            return true;
        }
    });
    this.instaGram = ko.computed(function () {
        //console.log(this.Source);
        if (data.Source == "Instagram") {
            return true;
        }
    });
    this.spanId = ko.computed(function () {
        return "span" + data.Id;
    });

    this.hrefId = ko.computed(function () {
        return 'anchor' + data.Id;
    })

    this.PledgeURL = ko.computed(function () {
        return data.LinkUrl + "/#" + data.Id;
    });

    if (data.IsOwner) {
        this.ShowJoinLink = false;
    }
    else {

        this.ShowJoinLink = true;
    }

    if (data.PublicPledge) {
        this.ShowJoinLinkAndText = true;
    }
    else {
        this.ShowJoinLinkAndText = false;
    }


    this.showImage = ko.computed(function () {
        if (data.MediaURL == "false") {
            return false;
        }
        else { return true; }
    });

    this.PledgeMembers = ko.computed(function () {
        if (data.JoinedPeoples == "1") {
            return data.JoinedPeoples + ' other person has made this goal';
        }
        else { return data.JoinedPeoples + ' other people have made this goal'; }
    });

    //this.username = ko.observable(data.username);
    //this.ProfilePic = ko.observable(data.ProfilePic);

    //this.created_date = ko.observable(data.CreatedAt);
    //this.FriendCount = ko.observable(data.FriendCount);
    //this.image = ko.observable(data.MediaURL);
    //this.ProfileURL = ko.observable(data.link);

    this.Author = ko.computed(function () {
        if (data.Author == "") {
            return "";
        }
        else { return 'By ' + data.Author; }
    });
}