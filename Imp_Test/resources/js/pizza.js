var test = {}

test.Pizza = function() {}
test.Pizza.prototype = {
	toppings: [],
	crust: 0,
	add: function(topping) {
		var x = 0
		if (topping == null)
			return
		else
			x = 1

		this.toppings.push(topping)
		this.crust = 2
	}
}
