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

        private void NotifyMainWindow(object sender, RoutedEventArgs e)
        {
            VertexControl vc = ((FrameworkElement)sender).TemplatedParent as VertexControl;
            if (vc!=null)
            {
                MainWindow.MW().RouteEdgeDragging(vc);
            }
        }

        private string GetParents(Object element, int parentLevel)
        {
            string returnValue = String.Format("[{0}] {1}", parentLevel, element.GetType());
            if (element is FrameworkElement)
            {
                if (((FrameworkElement)element).Parent != null)
                    returnValue += String.Format("{0}{1}",
                        Environment.NewLine, GetParents(((FrameworkElement)element).Parent, parentLevel + 1));
            }
            return returnValue;
        }
    }
}
