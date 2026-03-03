using System;
using System.Data;
using System.Globalization;
using System.IO;
using OfficeOpenXml;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        // Установка контекста лицензии EPPlus на NonCommercial
        ExcelPackage.License.SetNonCommercialPersonal("Vadim");

        string filePath = @"D:\Download\Задача_для_кандидата_v2.xlsx";
        string connectionString =
            "Server=Desktop-Vadim\\SQLEXPRESS;Database=AstraZenekaTest;Trusted_Connection=True;TrustServerCertificate=True;";

        var table = new DataTable();
        table.Columns.Add("dataTypeID", typeof(int));
        table.Columns.Add("monthDate", typeof(DateTime));
        table.Columns.Add("empID", typeof(int));
        table.Columns.Add("areaID", typeof(int));
        table.Columns.Add("regID", typeof(int));
        table.Columns.Add("vol", typeof(decimal));

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var sheet = package.Workbook.Worksheets["массив данных"];
            if (sheet == null || sheet.Dimension == null)
            {
                Console.WriteLine("Лист 'массив данных' не найден или пуст.");
                return;
            }

            // Начинаем с 3-й строки согласно исходному диапазону "B3:G2689"
            int startRow = 3;

            // Итерируемся от начальной строки до последней используемой строки на листе
            for (int row = startRow; row <= sheet.Dimension.End.Row; row++)
            {
                // Проверяем, пуста ли первая ячейка строки в предполагаемом диапазоне (т.е. столбец B)
                if (string.IsNullOrWhiteSpace(sheet.Cells[row, 2].Text))
                    continue;

                try
                {
                    // В EPPlus столбцы нумеруются с 1. B=2, C=3, D=4, E=5, F=6, G=7
                    var newRow = table.NewRow();

                    // --- Improved Parsing Logic with Detailed Error Logging ---

                    if (!int.TryParse(sheet.Cells[row, 2].Text, out int dataTypeID))
                    {
                        Console.WriteLine($"Пропускаем строку {row}: не удалось преобразовать '{sheet.Cells[row, 2].Text}' в число (столбец B).");
                        continue;
                    }
                    if (!DateTime.TryParse(sheet.Cells[row, 3].Text, out DateTime monthDate))
                    {
                        Console.WriteLine($"Пропускаем строку {row}: не удалось преобразовать '{sheet.Cells[row, 3].Text}' в дату (столбец C).");
                        continue;
                    }
                    if (!int.TryParse(sheet.Cells[row, 4].Text, out int empID))
                    {
                        Console.WriteLine($"Пропускаем строку {row}: не удалось преобразовать '{sheet.Cells[row, 4].Text}' в число (столбец D).");
                        continue;
                    }
                    if (!int.TryParse(sheet.Cells[row, 5].Text, out int areaID))
                    {
                        Console.WriteLine($"Пропускаем строку {row}: не удалось преобразовать '{sheet.Cells[row, 5].Text}' в число (столбец E).");
                        continue;
                    }
                    if (!int.TryParse(sheet.Cells[row, 6].Text, out int regID))
                    {
                        Console.WriteLine($"Пропускаем строку {row}: не удалось преобразовать '{sheet.Cells[row, 6].Text}' в число (столбец F).");
                        continue;
                    }
                    // Clean the number string: remove spaces (thousands separator) and replace comma with a period (decimal separator)
                    // Also handle non-breaking spaces (\u00A0) which are common in Excel.
                    string volText = sheet.Cells[row, 7].Text
                                          .Replace(" ", "").Replace("\u00A0", "") // Remove both standard and non-breaking spaces
                                          .Replace(",", ".");
                    if (!decimal.TryParse(volText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vol))
                    {
                        Console.WriteLine($"Пропускаем строку {row}: не удалось преобразовать '{sheet.Cells[row, 7].Text}' в десятичное число (столбец G).");
                        continue; // Пропускаем строку, если не удалось распарсить любую из ячеек
                    }

                    newRow["dataTypeID"] = dataTypeID;
                    newRow["monthDate"] = monthDate;
                    newRow["empID"] = empID;
                    newRow["areaID"] = areaID;
                    newRow["regID"] = regID;
                    newRow["vol"] = vol;

                    table.Rows.Add(newRow);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла ошибка при обработке строки {row}: {ex.Message}");
                    // Здесь можно решить, остановить выполнение или продолжить
                    // return; 
                }
            }
        }

        if (table.Rows.Count == 0)
        {
            Console.WriteLine("Данные из Excel-файла не были считаны. Вставка в базу данных отменена.");
            return;
        }

        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = "EmployeeData";

                    bulk.ColumnMappings.Add("dataTypeID", "dataTypeID");
                    bulk.ColumnMappings.Add("monthDate", "monthDate");
                    bulk.ColumnMappings.Add("empID", "empID");
                    bulk.ColumnMappings.Add("areaID", "areaID");
                    bulk.ColumnMappings.Add("regID", "regID");
                    bulk.ColumnMappings.Add("vol", "vol");

                    bulk.WriteToServer(table);
                }
            }

            Console.WriteLine($"{table.Rows.Count} строк данных успешно перенесено в базу данных.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine("\n--- ОШИБКА ПОДКЛЮЧЕНИЯ К БАЗЕ ДАННЫХ ---");
            Console.WriteLine($"Не удалось подключиться к SQL Server. Пожалуйста, проверьте строку подключения и убедитесь, что сервер доступен.");
            Console.WriteLine($"Сообщение об ошибке: {ex.Message}");
        }
    }
}