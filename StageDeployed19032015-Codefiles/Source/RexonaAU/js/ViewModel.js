
var viewModel;
var instModel;

$(document).ready(function () {
    viewModel = new TwitterViewModel();
    ko.applyBindings(viewModel, $('#divTwitterResults')[0]);

    instModel = new InstagramViewModel();
    ko.applyBindings(instModel, $('#divInstagramResults')[0]);
});

function TwitterViewModel() {
  //  alert('called from viewModel');
    var self = this;
    self.tweets = ko.observableArray([]);
}

function InstagramViewModel() {
   
    var self = this;
    self.instagram = ko.observableArray([]);
    //self.approveContent = approveContent(isTweet);
}