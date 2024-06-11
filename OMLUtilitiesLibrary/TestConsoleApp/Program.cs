using System;
using System.Diagnostics;
using System.IO;
using OMLtoXML;

namespace TestConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string filePath = @"C:\Users\antun\Downloads\lifetime_v11.oml";

            Console.WriteLine($"File to convert: {Path.GetFileName(filePath)}");

            // Measure the size of the OML file
            var fileInfo = new FileInfo(filePath);
            long fileSizeInBytes = fileInfo.Length;
            double fileSizeInMB = Math.Round(fileSizeInBytes / (1024.0 * 1024.0), 2); // Convert bytes to megabytes and round to 2 decimal places
            Console.WriteLine($"OML file size: {fileSizeInMB} MB");

            var fileBytes = File.ReadAllBytes(filePath);

            // Measure the time it takes to run ConvertOMLtoXML
            Stopwatch stopwatch = Stopwatch.StartNew();
            var omlXML = new OMLUtilitiesOMLtoXML().ConvertOMLtoXML(fileBytes);
            stopwatch.Stop();

            // Measure CPU time
            Process process = Process.GetCurrentProcess();
            TimeSpan totalProcessorTime = process.TotalProcessorTime;

            Console.WriteLine($"Time taken to convert OML to XML: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Total CPU time used: {totalProcessorTime.TotalMilliseconds} ms");

            // Measure memory usage
            long memoryUsed = GC.GetTotalMemory(true);
            Console.WriteLine($"Memory used after conversion: {Math.Round(memoryUsed / (1024.0 * 1024.0),2)} MB");

            // Measure the size of the omlXML
            long omlXMLSizeInBytes = System.Text.Encoding.UTF8.GetBytes(omlXML).Length;
            double omlXMLSizeInMB = Math.Round(omlXMLSizeInBytes / (1024.0 * 1024.0), 2);
            Console.WriteLine($"Size of the XML: {omlXMLSizeInMB} MB");

            // Optional: Output the converted XML (for debugging purposes)
            // Console.WriteLine(omlXML);

            Console.ReadLine();
        }
    }
}
