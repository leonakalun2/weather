using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherService.Model.Dtos
{
    public class UvDataDto
    {
        public double UvIndex { get; set; }
        public string UvCategory { get; set; } = string.Empty;
    }
}
