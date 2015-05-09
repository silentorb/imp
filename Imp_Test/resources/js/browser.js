var bloom = {}

bloom.Garden = function() {}
bloom.Garden.vineyard_url = 'http://localhost:3000/'
bloom.Garden.prototype = {
	start: function() {
		var $injector = angular.injector(['ng'])
		window.$q = $injector.get('$q')
	}
}