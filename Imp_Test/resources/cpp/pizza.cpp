#include "test/Crust.h"
#include "test/Pizza.h"

namespace test {

  void Pizza::add(std::string topping) {
    auto x = 0;
    if (topping == null)
      return;
    else
      x = 1.5f;

    toppings.push_back(topping);
    crust = Crust::burnt;
  }

  std::vector<std::string> Pizza::get_eaten() {
    return toppings;
  }

}