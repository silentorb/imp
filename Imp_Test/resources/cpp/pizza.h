#pragma once
#include "stdafx.h"

namespace test {

	class Pizza {
public:
		std::vector<std::string> toppings;

		virtual void add(std::string topping);
	};
}
