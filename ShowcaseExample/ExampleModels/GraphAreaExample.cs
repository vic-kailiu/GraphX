using GraphX;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ShowcaseExample
{
    public class GraphAreaExample : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>
    {
        private class Layer
        {
            public int vertexlayer = 0;
            public int edgeLayer = 0;
        }

        private List<Layer> nestedvisualLayers = null;

        public GraphAreaExample()
        {
            nestedvisualLayers = new List<Layer>();
        }

        public void AddVertex(DataVertex vertexData, VertexControl vertexControl, DataVertex childVertex)
        {
            int visualLayer = -1;

            if (childVertex == null)
            {
                AddVertex(vertexData, vertexControl); // add to top, simply add it
            }
            else
            {
                // find corresonding layer
                int layer = nestedvisualLayers.Count - childVertex.layerLever - 1;
                // count layer below and edge layer ==> visualLayer
                if (layer == -1)
                {
                    Layer newLayer = new Layer();
                    layer = 0;
                    nestedvisualLayers.Insert(layer, newLayer);
                }

                visualLayer = 0;
                for (int i = 0; i<layer; i++)
                {
                    visualLayer += nestedvisualLayers[i].vertexlayer 
                                 + nestedvisualLayers[i].edgeLayer; 
                }
                visualLayer += nestedvisualLayers[layer].edgeLayer; // visual layer is above the edge layer

                InternalAddVertex(vertexData, vertexControl, visualLayer);
                if (EnableVisualPropsApply && vertexControl != null)
                    ReapplySingleVertexVisualProperties(vertexControl);
            }            
        }

        public void AddEdge(DataEdge edgeData, EdgeControl edgeControl, int i)
        {
            InternalAddEdge(edgeData, edgeControl, i);
            if (EnableVisualPropsApply && edgeControl != null)
                ReapplySingleEdgeVisualProperties(edgeControl);
        }
    
        public VertexControl GetVertexControl(string id)
        {
            Guid guid = new Guid(id);
            if (guid == Guid.Empty)
                throw new Exception("Invalid GUID");

            List<DataVertex> dvList = base.VertexList.Keys.ToList();
            var dv = dvList.First(o => o.ID == guid);
            if (dv != null)
            {
                return base.VertexList[dv] as VertexControl;
            }
            else
            {
                return null;
            }
        }
    }
}