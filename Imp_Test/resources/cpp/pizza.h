#pragma once
#include "stdafx.h"

namespace test {
	
	class Crust;

	class Pizza {
public:
		std::vector<std::string> toppings;
		Crust* crust;
		bool variation;
		std::string taste;
		
		virtual void add(std::string topping);
		virtual std::vector<std::string> get_eaten();
	};
}
