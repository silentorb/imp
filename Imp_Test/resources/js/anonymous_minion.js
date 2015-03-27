var test = {}

test.Pizza = function() {}
test.Pizza.prototype = {
	toppings: [],
	add: function(topping) {
		var self = this
		if (topping == null)
			return

		var anon = function(topping) {
			self.toppings.push(topping)
		}
		
		anon(topping)
	}
}
