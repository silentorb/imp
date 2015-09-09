#pragma once
#include <string>
#include <vector>
#include <map>
#include <memory>
namespace test {
  class Crust;
  class Pizza {
    public:
    std::vector<std::string> toppings;
    std::shared_ptr<Crust> crust;
    bool variation;
    std::string taste;
    virtual void add(std::string topping);
    virtual std::vector<std::string> get_eaten();
  }
}