var test = {}

test.Pizza = function() {}
test.Pizza.prototype = {
	toppings: [],
	crust: 0,
	variation: false,
	add: function(topping) {
		var x = 0
		if (topping == null)
			return
		else
			x = 1

		this.toppings.push(topping)
		this.crust = 2
	},
	get_eaten: function() {
		return this.toppings
	}
}
