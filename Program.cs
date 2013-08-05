using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{
    class Program
    {
        static void Main(string[] args)
        {
            var info = new Microsoft.VisualBasic.Devices.ComputerInfo();
            System.Console.WriteLine(info.TotalPhysicalMemory);
            System.Console.WriteLine(info.AvailablePhysicalMemory);
            System.Console.WriteLine(info.TotalVirtualMemory);
            System.Console.WriteLine(info.AvailableVirtualMemory);
            System.Console.WriteLine(info.OSFullName);

            ChallengeService.ChallengeClient client = new ChallengeService.ChallengeClient("ChallengePort", "http://localhost:7070/Challenge/ChallengeService");
            client.login();
        }
    }
}
