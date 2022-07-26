using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPMaahomServer2
{
    class Program
    {
        static void Main(string[] args)
        {
            AsynchronousSocketListener.StartListening();
            Console.ReadLine();
        }
    }
}
