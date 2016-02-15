'use strict';
app.factory('contactsService', ['$http', 'localStorageService', 'ngAuthSettings', function ($http, localStorageService, ngAuthSettings) {

    var serviceBase = ngAuthSettings.apiServiceBaseUri;

    var contactsServiceFactory = {};
    
    var _getContacts = function () {

        var authData = localStorageService.get('authorizationData');

        var authDataFormat = {
            userName: authData.userName,
            provider: 'google',
            externalAccessToken: authData.google_access_token
        };

        return $http.post(serviceBase + 'api/contacts/googleload', authDataFormat).success(function (results) {
            return results;
        }).error(function (err, status) {

        });
    };

    contactsServiceFactory.getContacts = _getContacts;

    return contactsServiceFactory;

}]);