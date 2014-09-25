using GraphX;
using GraphX.Controls;
using GraphX.GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphX.GraphSharp.Algorithms.OverlapRemoval;
using GraphX.Logic;
using QuickGraph;
using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Knowledge_Map
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Set minimap (overview) window to be visible by default
            ZoomControl.SetViewFinderVisibility(zoomctrl, System.Windows.Visibility.Visible);
            //Set Fill zooming strategy so whole graph will be always visible
            zoomctrl.ZoomToFill();

            GraphArea_Setup();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Call realyout. Additional parameters shows that edges must be created or updated if any exists.
            Area.RelayoutGraph(true);
            zoomctrl.ZoomToFill();
        }

        private void GraphArea_Setup()
        {
            var LogicCore = new GXLogicCoreModel() { Graph = Graph_Setup() };
            //Assign data graph
            Area.LogicCore = LogicCore;
        }

        private GraphModel Graph_Setup()
        {
            //Create data graph object
            var graph = new GraphModel();
 
            //Create and add vertices
            graph.AddVertex(new DataVertex() { ID = 1, Text = "Item 1" });
            graph.AddVertex(new DataVertex() { ID = 2, Text = "Item 2" });
            var v1 = graph.Vertices.First();
            var v2 = graph.Vertices.Last();
            var e1 = new DataEdge(v1, v2, 1) { Text = "1 -> 2" };
            graph.AddEdge(e1);

            //VC DataContext will be bound to v1 by default. You can control this by specifing additional property in the constructor
            var vc1 = new VertexControl(v1);
            Area.AddVertex(v1, vc1);
            var vc2 = new VertexControl(v2);
            Area.AddVertex(v2, vc2);
 
            var ec = new EdgeControl(vc1, vc2, e1);
            Area.InsertEdge(e1, ec); //inserts edge into the start of the children list to draw it below vertices

            return graph;
        }
    }
}
