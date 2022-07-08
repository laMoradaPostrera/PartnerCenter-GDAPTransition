using PartnerLed.Model;

namespace PartnerLed.Utility
{
    public class Helper
    {
        /// <summary>
        ///  Get the extension in formated string
        /// </summary>
        /// <param name="exportImportType"></param>
        /// <returns></returns>
        public static string GetExtenstion(ExportImport exportImportType) => exportImportType.ToString().ToLower();


        public static bool UserConfirmation(string message)
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\t");
            Console.WriteLine(message);
            Console.ResetColor();
            Console.WriteLine("press [y/Y] to continue or any other key to exit the operation.");
            var option = Console.ReadLine();

            return option != null && option.Trim().ToLower() == "y";

        }
    }
}
