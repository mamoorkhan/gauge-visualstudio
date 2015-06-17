﻿using System;

namespace Gauge.VisualStudio.Loggers
{
    class StatusBarLogger
    {
        internal static void Log(string message)
        {
            var statusBar = GaugeDTEProvider.DTE.StatusBar;
            statusBar.Text = message;
            statusBar.Highlight(true);
            Console.WriteLine(message);
        }
    }
}
