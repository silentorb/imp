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
        [STAThread]
        static void Main(string[] args)
        {
            var daemon = new Daemon();
            daemon.on_run += Overlord.run;
            daemon.start(args);
        }

    }
}
