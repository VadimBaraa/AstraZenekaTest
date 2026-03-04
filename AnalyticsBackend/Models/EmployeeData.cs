using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalyticsBackend.Models
{
    public class EmployeeData
    {
        public int DataTypeID { get; set; }    // 1=Продажи, 2=Уходимость, 3=План, 4=Остатки
        public DateTime MonthDate { get; set; }
        public int EmpID { get; set; }
        public int AreaID { get; set; }        // Макро-территория
        public int RegID { get; set; }         // Регион
        public decimal Vol { get; set; }
    }
}