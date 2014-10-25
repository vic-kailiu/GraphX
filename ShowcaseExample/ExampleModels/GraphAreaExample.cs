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

        // nestedvisualLayers[0] to be the root folder, placed at the very back
        private List<Layer> nestedvisualLayers = null;

        public GraphAreaExample()
        {
            nestedvisualLayers = new List<Layer>();
        }

        public void GraphAddVertex(DataVertex vertexData, VertexControl vertexControl)
        {
            int visualLayer = -1;

            if (vertexData.layerLevel == 0)
            {
                AddVertex(vertexData, vertexControl); // add to top, simply add it
            }
            else
            {
                // find corresonding layer
                int layer = nestedvisualLayers.Count - vertexData.layerLevel;

                visualLayer = FindLayer(ref layer);

                InternalAddVertex(vertexData, vertexControl, visualLayer);
                nestedvisualLayers[layer].vertexlayer++;

                if (EnableVisualPropsApply && vertexControl != null)
                    ReapplySingleVertexVisualProperties(vertexControl);
            }
        }

        public void MoveLayer(DataVertex dv, VertexControl vc, int previousLayer)
        {
            base.removeVisual(vc);
            if (nestedvisualLayers.Count > 0) // for promote leave to folder
                nestedvisualLayers[nestedvisualLayers.Count - previousLayer].vertexlayer--;

            int layer = nestedvisualLayers.Count - dv.layerLevel;
            int visualLayer = FindLayer(ref layer);
            base.addVisual(vc, visualLayer);
            nestedvisualLayers[layer].vertexlayer++;
        }

        private int FindLayer(ref int layer)
        {
            int visualLayer = -1;

            // count layer below and edge layer ==> visualLayer
            if (layer == -1)
            {
                Layer newLayer = new Layer();
                layer = 0;
                nestedvisualLayers.Insert(layer, newLayer);
            }

            visualLayer = 0;
            for (int i = 0; i < layer; i++)
            {
                visualLayer += nestedvisualLayers[i].vertexlayer
                             + nestedvisualLayers[i].edgeLayer;
            }
            visualLayer += nestedvisualLayers[layer].edgeLayer; // visual layer is above the edge layer

            return visualLayer;
        }

        public void GraphAddEdge(DataEdge edgeData, EdgeControl edgeControl)
        {
            //edge should be below the highest ranking vertex
            int layer = edgeData.Source.layerLevel > edgeData.Target.layerLevel ?
                        edgeData.Source.layerLevel : edgeData.Target.layerLevel;
            layer = nestedvisualLayers.Count - layer;

            int visualLayer = 0;
            for (int i = 0; i < layer; i++)
            {
                visualLayer += nestedvisualLayers[i].vertexlayer
                             + nestedvisualLayers[i].edgeLayer;
            }

            InternalAddEdge(edgeData, edgeControl, visualLayer);
            if (layer < nestedvisualLayers.Count)
                nestedvisualLayers[layer].edgeLayer++;
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