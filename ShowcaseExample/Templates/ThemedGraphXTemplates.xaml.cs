using GraphX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShowcaseExample.Templates
{
    partial class ThemedGraphXTemplates : ResourceDictionary
    {
        public ThemedGraphXTemplates()
        {
            InitializeComponent();
        }

        private void NotifyMainWindow(object sender, EventArgs e)
        {
            VertexControl vc = ((FrameworkElement)sender).TemplatedParent as VertexControl;
            if (vc!=null)
            {
                if (sender is Button)
                    MainWindow.MW().RouteCommand(vc, RoutedCommands.EdgeDrag);
                else if (sender is Image)
                    MainWindow.MW().RouteCommand(vc, RoutedCommands.VertexDragDrop);
            }
        }
    }
}
