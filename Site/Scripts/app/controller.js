'use strict';

app.controller('LoginController', ['$scope', '$sce', 'backendHubProxy',
  function ($scope, $sce, backendHubProxy) {
      console.log('trying to connect to service');
      var connProxy = backendHubProxy(backendHubProxy.defaultServer, 'ugolkiHub');
      var ugolkiHub = connProxy.Proxy;

      console.log('connected to service');

      $scope.gameState = ['Wait', 'Move 1', 'Move 2', 'Player 1 win', 'Player 2 win', 'Draw'];

      $scope.sendMessage = function (roomId, message) {
          ugolkiHub.invoke('sendMessage', roomId, message);
          $scope.message = "";
      };

      $scope.setSelected = function (roomId, x, y) {
          ugolkiHub.invoke('selectCell', roomId, x, y);
          /*if ($scope.rooms[$scope.roomIndex].Game.Cells[x][y] != 0) {
              var selected = $scope.rooms[$scope.roomIndex].Game.SelectedCell;
              if (selected.X == x && selected.Y == y) {
                  selected.X = -1;
                  selected.Y = -1;
              }
              else {
                  selected.X = x;
                  selected.Y = y;
              }
          }*/
      }

      $scope.login = function (user) {
          ugolkiHub.invoke('login', user).done(function (hash) {
              $scope.user.hash = hash;
              $scope.$apply();
              if (hash.length > 0) {
                  localStorage.setItem('login', user.name);
                  localStorage.setItem('password', user.password);
              }
          }).fail(function (error) {
              console.log('Invocation of login failed. Error: ' + error);
          });
      };

      $scope.logout = function (user) {
          ugolkiHub.invoke('logout', user).done(function (hash) {
              $scope.user.hash = undefined;
              $scope.user.password = "";
              $scope.$apply();
              localStorage.removeItem('login');
              localStorage.removeItem('password');
          }).fail(function (error) {
              console.log('Invocation of login failed. Error: ' + error);
          });
      };

      $scope.loaded = function () {
          if ($scope.user == undefined) {
              $scope.user = {};
          }
          $scope.user.name = localStorage.getItem("login");
          $scope.user.password = localStorage.getItem("password");
          if ($scope.user.name != null && $scope.user.password != null) {
              ugolkiHub.invoke('login', $scope.user).done(function (hash) {
                  $scope.user.hash = hash;
                  $scope.$apply();
              });
          }
      };

      $scope.join = function (room, index) {
          $scope.roomIndex = index;
          ugolkiHub.invoke('join', room.RoomId).done(function () {

          });
      };

      $scope.exit = function (roomId) {
          if (confirm('Are you sure you want to exit?')) {
              $scope.roomIndex = undefined;
              ugolkiHub.invoke('exit', roomId);
          }
      };

      $scope.refreshClients = function (roomId) {
          ugolkiHub.invoke('refreshClients', roomId);
      };

      $scope.setObserver = function (roomId) {
          ugolkiHub.invoke('setObserver', roomId);
      };

      $scope.setPlayer = function (roomId) {
          ugolkiHub.invoke('setPlayer', roomId);
      };

      $scope.ready = function (roomId, value) {
          ugolkiHub.invoke('ready', roomId, value);
      };

      $scope.addAi = function (roomId) {
          ugolkiHub.invoke('addAi', roomId);
      };

      $scope.removeAi = function (roomId) {
          ugolkiHub.invoke('removeAi', roomId);
      };

      $scope.createRoom = function () {
          ugolkiHub.invoke('createRoom').done(function (hash) {
              $scope.roomIndex = $scope.rooms.length - 1;
              $scope.$apply();
          }).fail(function (error) {
              console.log('Invocation of createRoom failed. Error: ' + error);
          });;
      };

      $scope.getRooms = function () {
          ugolkiHub.invoke('getRooms').done(function (rooms) {
              //$scope.rooms = rooms;
              //$scope.$apply();
          }).fail(function (error) {
              console.log('Invocation of getRooms failed. Error: ' + error);
          });
      };

      $scope.getUsers = function () {
          ugolkiHub.invoke('getUsers').done(function (users) {
              $scope.users = users;
              $scope.$apply();
          }).fail(function (error) {
              console.log('Invocation of getUsers failed. Error: ' + error);
          });
      };

      $scope.messages = [];
      $scope.rooms = [];
      $scope.users = [];

      ugolkiHub.on('hello', function (data) {
          $scope.messages.push('Hello! (' + $scope.messages.length + ')');
          $scope.$apply();
      });

      ugolkiHub.on('addUser', function (user) {
          $scope.users.push(user);
          $scope.$apply();
      });

      ugolkiHub.on('addRoom', function (room) {
          $scope.rooms.push(room);
          $scope.$apply();
      });

      ugolkiHub.on('removeUser', function (name) {
          var index = -1;
          for (var i = 0; i < $scope.users.length && index == -1; i++) {
              if ($scope.users[i].Name == name) {
                  index = i;
              }
          }
          $scope.users.splice(index, 1);
          $scope.$apply();
      });

      ugolkiHub.on('removeRoom', function (id) {
          var index = -1;
          for (var i = 0; i < $scope.rooms.length && index == -1; i++) {
              if ($scope.rooms[i].RoomId == id) {
                  index = i;
              }
          }
          $scope.rooms.splice(index, 1);
          $scope.$apply();
      });

      ugolkiHub.on('updateRoom', function (room) {
          var index = -1;
          for (var i = 0; i < $scope.rooms.length && index == -1; i++) {
              if ($scope.rooms[i].RoomId == room.RoomId) {
                  index = i;
              }
          }
          var messages = $scope.rooms[index].messages;
          var clientsCount = $scope.rooms[index].clientsCount;
          $scope.rooms[index] = room;
          $scope.rooms[index].messages = messages;
          $scope.rooms[index].clientsCount = clientsCount;
          $scope.$apply();
      });

      ugolkiHub.on('setClientsCount', function (roomId, count) {
          var index = -1;
          for (var i = 0; i < $scope.rooms.length && index == -1; i++) {
              if ($scope.rooms[i].RoomId == roomId) {
                  index = i;
              }
          }
          $scope.rooms[index].clientsCount = count;
          $scope.$apply();
      });

      ugolkiHub.on('setData', function (users, rooms) {
          $scope.users = users;
          $scope.rooms = rooms;
          $scope.$apply();
      });

      ugolkiHub.on('sendMessage', function (message, roomId) {
          if (typeof $scope.rooms[$scope.roomIndex].messages == "undefined") {
              $scope.rooms[$scope.roomIndex].messages = [];
          }
          var index = -1;
          for (var i = 0; i < $scope.rooms.length && index == -1; i++) {
              if ($scope.rooms[i].RoomId == roomId) {
                  index = i;
              }
          }
          if (message.Text.indexOf("youtu.be") > -1 || message.Text.indexOf("youtube.com") > -1) {
              var text = message.Text;
              var ind = text.indexOf("http");
              text = text.substr(ind, text.length - ind);
              text = text.split(" ")[0];
              var id = YouTubeGetID(text);
              if (id.length > 1 && message.Text.indexOf(" ") < 0) {
                  message.Text = "";
              }
              message.Video = $sce.trustAsHtml("<iframe width=\"603\" height=\"345\" src=\"https://www.youtube.com/embed/" + id + "\" frameborder=\"0\" allowfullscreen></iframe>");
              //<iframe width="560" height="315" src="https://www.youtube.com/embed/N9HmSMaY92E" frameborder="0" allowfullscreen></iframe>
          }
          if (index != -1) {
              $scope.rooms[index].messages.push(message);
              $scope.$apply();
          }
      });

      connProxy.Connection.start().done($scope.loaded);
  }
]);