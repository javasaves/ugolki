'use strict';

app.factory('backendHubProxy', ['$rootScope', 'backendServerUrl',
  function ($rootScope, backendServerUrl) {

      function backendFactory(serverUrl, hubName) {
          var connection = $.hubConnection(backendServerUrl);
          var proxy = connection.createHubProxy(hubName);

          //connection.start().done(function () { });

          connection.disconnected(function () {
              setTimeout(function () {
                  connection.start();
              }, 3000);
          });

          return { Proxy: proxy, Connection: connection };
      };

      return backendFactory;
  }]);