using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace test
{
    public class Pizza
    {
        public List<string> toppings = new List<string>();
        public Crust crust = Crust.crispy;

        public void add(string topping)
        {
            var x = 0;
            if (topping == null)
                return;
            else
                x = 1;

            toppings.Add(topping);
            crust = Crust.burnt;
        }
    }
}
