var galleryViewModel;
var currentPageIndex = 0;
var requestPending = false;
var totalPages;
var existingItems = [];
var PageSize = 24;

$(document).ready(function () {
    galleryViewModel = new GalleryModel();
    ko.applyBindings(galleryViewModel);

    currentPageIndex = 0;
    getPledges(currentPageIndex);


    $('.select').change(function () {
        existingItems = [];
        currentPageIndex = 0;
        requestPending = false;
        getPledges(currentPageIndex);
    });

    $(document).on('click', '.rating-icon', function (e) {
        if ($(this).attr('Id') != undefined) {
            likeCount("likeCount", $(this).attr('Id'));
        }
    });

});



$(window).scroll(function (e) {
    if (!requestPending && (currentPageIndex + 1 < totalPages)) {
        if ($(window).scrollTop() + $(window).height() > ($(document).height() - 200)) {
            currentPageIndex = currentPageIndex + 1;
            getPledges(currentPageIndex);
        }
    } else {
        e.preventDefault();
        e.stopPropagation();
    }
});


function getPledges(pageIndex) {

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
            url: '/umbraco/surface/Pledge/GetPledges',
            success: function (data) {
                var userPledges = $.map(data.pledges, function (item) { return new pledge(item); });
                existingItems = existingItems.concat(userPledges);
                galleryViewModel.Pledges(existingItems);

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

                }, 200);

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

function GalleryModel() {
    var self = this;
    self.Pledges = ko.observableArray([]);
}

function pledge(data) {
    this.ImageURL = data.ImageURL;
    this.MemberCount = data.MemberCount;
    if (data.IsPublicSelection == "1") {
        this.ShowJoinLinkAndText = true;
    }
    else {
        this.ShowJoinLinkAndText = false;
    }

    if (data.IsMember) {
        this.ShowJoinLink = false;
    }
    else {

        this.ShowJoinLink = true;
    }
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
}

