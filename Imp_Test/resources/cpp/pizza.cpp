#include "stdafx.h"
#include "test/Pizza.h"

namespace test {

	void Pizza::add(std::string topping) {
		var x = 0;
		if (topping == null)
			return;
		else
			x = 1;

		toppings.push_back(topping);
	}
}
