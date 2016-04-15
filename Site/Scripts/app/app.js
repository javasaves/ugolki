'use strict';

var app = angular.module('ugolkiApp', []);
app.value('backendServerUrl', document.URL.substr(0, document.URL.length - 1));//'http://192.168.100.8/Ugolki');

app.directive('schrollBottom', function () {
    return {
        scope: {
            schrollBottom: "="
        },
        link: function (scope, element) {
            scope.$watchCollection('schrollBottom', function (newValue) {
                if (newValue) {
                    $(element).scrollTop($(element)[0].scrollHeight);
                }
            });
        }
    }
});

app.constant('Constants', {
    State: {
        Wait: 0, Move1: 1, Move2: 2, Win1: 3, Win2: 4, Draw: 5
    }
});


function YouTubeGetID(url) {
    var ID = '';
    url = url.replace(/(>|<)/gi, '').split(/(vi\/|v=|\/v\/|youtu\.be\/|\/embed\/)/);
    if (url[2] !== undefined) {
        ID = url[2].split(/[^0-9a-z_\-]/i);
        ID = ID[0];
    }
    else {
        ID = url;
    }
    return ID;
}