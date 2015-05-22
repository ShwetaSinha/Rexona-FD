

$(document).ready(function () {

    instagramContent();
});

function instagramContent() {

    $.ajax({
        contentType: "application/json; charset=utf-8",
        type: 'GET',
        dataType: 'json',
        cache: false,
        url: '/umbraco/surface/SocialContent/GetInstagram',
        success: function (objInsta) {
            //console.log(objInsta);
            var insta = $.map(objInsta, function (item) { return new Instagram(item); });
            instModel.instagram(insta);
            if (insta.length == 0) {
                $('#divInstagramResults table tbody').html("<span style='color:red;'>No results found</span>");
            }
            $('#divInstagramResults,#divInstagramResults table').show();
        }
    });

}
