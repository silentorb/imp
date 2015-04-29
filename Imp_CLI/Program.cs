using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using imperative;
using metahub.render;
using metahub.render.targets;
using runic.parser;

namespace imp
{
    class Program
    {
        static Mutex mutex = new Mutex(true, "{EFFA67AE-0917-49C8-BCE7-87BB876CC741}");
        private const string pipe_name = "Imp-CLI";

        [STAThread]
        static void Main(string[] args)
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                run_client(args);
                return;
            }

            run(args);

            mutex.ReleaseMutex();
        }

        static void run(string[] args)
        {
            try
            {
                if (args.Length == 0)
                    throw new Exception("Missing configuration arguments.");

                var config = new Overlord_Configuration();

                for (var i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];
                    switch (arg)
                    {
                        case "-i":
                            config.input = args[i + 1];
                            break;

                        case "-o":
                            config.output = args[i + 1];
                            break;

                        case "-t":
                            config.target = args[i + 1];
                            break;

                        case "-d":
                            run_daemon();
                            return;
                    }
                }

                if (config.input == null)
                    throw new Exception("Missing input file/folder argument (-i)");

                if (config.output == null)
                    throw new Exception("Missing output file/folder argument (-o)");

                if (config.target == null)
                    throw new Exception("Missing target language argument (-t)");

                Overlord.run(config);
                Console.WriteLine("Imp sucessfully stormed the Keep.");
            }

            catch (Parser_Exception ex)
            {
                //                Console.WriteLine(ex.filename); Path.GetFullPath(ex.filename)
                                Console.WriteLine(ex.filename + "(" + ex.position.y + "," + ex.position.x + 
                                    "): error Imp Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Imp.error(0) : error Imp Error: Imp Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

        }

        static void resolve_paths(string[] args)
        {

            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "-i":
                    case "-o":
                        args[i + 1] = Path.GetFullPath(args[i + 1]);
                        break;
                }
            }
        }

        static void run_daemon()
        {
            Console.WriteLine("Daemon awakened and awaiting.");
            var server = new NamedPipeServerStream(pipe_name);
            server.WaitForConnection();
            var reader = new StreamReader(server);
            var writer = new StreamWriter(server);

            while (true)
            {
                var line = reader.ReadLine();
                var output = Console.Out;
                Console.SetOut(writer);
                var args = line.Split(' ');
                run(args);

                Console.SetOut(output);

                writer.WriteLine("end");
                writer.Flush();
            }
        }

        static void run_client(string[] args)
        {
            var client = new NamedPipeClientStream(pipe_name);
            client.Connect();
            var reader = new StreamReader(client);
            var writer = new StreamWriter(client);

            resolve_paths(args);
            writer.WriteLine(args.join(" "));
            writer.Flush();

            while (true)
            {
                var message = reader.ReadLine();
                if (message == "end")
                    break;

                Console.Write(message);
            }

            client.Close();
        }
    }
}
