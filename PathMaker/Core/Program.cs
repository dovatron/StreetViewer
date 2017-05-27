﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StreetViewer.Interface;

namespace StreetViewer.Core
{
    /// <summary>
    /// MainClass приложения.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Метод Main.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
