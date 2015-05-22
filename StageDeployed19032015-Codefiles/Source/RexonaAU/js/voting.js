
$(document).ready(function () {

    markLikes();

    //$(document).on('click', '.rating-icon', function (e) {
    //    e.stopPropagation();
    //    e.preventDefault();
    //    console.log(this.id);
    //    $(this).parent().toggleClass('voted');
    //    //console.log($(this).parents('a'))
    //    //var entry = $(this).parents('a').attr('href');
    //    //var entry_id = entry.split('#')[1];

    //    if (!$.cookie("entrylikes")) {
    //        var likeString = ',' + entry_id + ',';
    //        setCookie(likeString);
    //        markLikes();

    //        //ajax increment
    //        voteAjax(true, entry_id);

    //    } else {
    //        var cookie = $.cookie("entrylikes");

    //        if (cookie.indexOf(entry_id + ',') > -1) {
    //            cookie = removeValue(cookie, entry_id);
    //            setCookie(cookie);
    //            markLikes();

    //            //ajax decrement
    //            voteAjax(false, entry_id);
    //        } else {
    //            cookie = cookie + entry_id + ',';
    //            $.removeCookie('entrylikes');
    //            setCookie(cookie);
    //            markLikes();

    //            //ajax increment
    //            voteAjax(true, entry_id);
    //        }
    //    }

    //});

});

function likeCount(fieldName, entry_id) {



    if (!$.cookie("entrylikes")) {
        var likeString = ',' + entry_id + ',';
        setCookie(likeString);
        markLikes();

        //ajax increment
        voteAjax(true, entry_id, fieldName);

    } else {
        var cookie = $.cookie("entrylikes");

        if (cookie.indexOf(entry_id + ',') > -1) {
            cookie = removeValue(cookie, entry_id);
            setCookie(cookie);
            markLikes();

            //ajax decrement
            voteAjax(false, entry_id, fieldName);
        } else {
            cookie = cookie + entry_id + ',';
            $.removeCookie('entrylikes');
            setCookie(cookie);
            markLikes();

            //ajax increment
            voteAjax(true, entry_id, fieldName);
        }
    }
}

function removeValue(list, value) {
    return list.replace(new RegExp(value + ',?'), '')
}


function setCookie(value) {
    $.cookie("entrylikes", value, { path: '/', expires: 10000000 });

}

function markLikes() {

    var likedList = $.cookie("entrylikes") ? $.cookie("entrylikes").split(',') : '';
    $('.rating-icon').parent().removeClass('voted');
    $.each(likedList, function (index, value) {
        if (value) {
            $('[id=' + value + ']').parent().addClass('voted');
        }

    })

}
function voteAjax(flag, entryId, fieldName) {
    var votedata = new Object();
    votedata.vote = flag;
    votedata.entryId = entryId;
    votedata.fieldName = fieldName;

    $.ajax({
        url: '/umbraco/surface/Vote/CountVote',
        type: 'POST',
        cache: false,
        data: votedata,
        success: function (data) {
            if (data) {
                //console.log(data);
                if (data.message == 'Success') {
                    $('#' + entryId).parent().find('.rating-number span').text(data.like);
                }
                else {
                    //TO DO change function name;
                    //console.log('Oops. Something went wrong.Please try again');
                   
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            //TO DO change function name;
           // console.log('Oops. Something went wrong.Please try again');
        }
    });

}

