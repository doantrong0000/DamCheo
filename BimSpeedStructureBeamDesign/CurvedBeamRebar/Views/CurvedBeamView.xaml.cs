﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.Views
{
    /// <summary>
    /// Interaction logic for CurvedBeamView.xaml
    /// </summary>
    public partial class CurvedBeamView : Window
    {
        public CurvedBeamView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
