using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.PriceBuilder.Domain.Enums
{
    public enum EnumMathExpresstion
    {
        /// <summary>
        /// Summation (+)
        /// </summary>
        Sum = 10,
        /// <summary>
        /// Subtraction (-)
        /// </summary>
        Sub = 20,
        /// <summary>
        /// Multiplication (*)
        /// </summary>
        Mul = 30,
        /// <summary>
        /// Division (/)
        /// </summary>
        Div = 40,
        /// <summary>
        /// Division and remainder (%)
        /// </summary>
        DivR = 50

    }
}
