'use strict';
app.controller('contactsController', ['$scope', 'contactsService', function ($scope, contactsService) {

    $scope.contacts = [];

    contactsService.getContacts().then(function (results) {

        $scope.contacts = results.data;

    }, function (error) {
        alert(error.data.message);
    });

}]);