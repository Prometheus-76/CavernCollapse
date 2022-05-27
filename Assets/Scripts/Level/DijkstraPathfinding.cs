using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DijkstraPathfinding : MonoBehaviour
{
    public class DijkstraNode
    {
        public Vector2Int position;
        public DijkstraNode[] neighbours;
        public int weight;

        public DijkstraNode previous;
        public int totalWeight;
        public bool isClosed;
    }

    // For sorting the open list
    public class ByTotalWeight : IComparer<DijkstraNode>
    {
        public int Compare(DijkstraNode a, DijkstraNode b)
        {
            // Used for deleting, compares instances to see if they are identical
            if (a.position == b.position) return 0;

            // If the total weight is the same, allow both in the set, arbitrarily order them
            if (a.totalWeight - b.totalWeight == 0) return -1;

            // The weights must be different, so return the lowest one
            return a.totalWeight - b.totalWeight;
        }
    }

    DijkstraNode[,] dijkstraGrid;
    Vector2Int targetPosition;
    SortedSet<DijkstraNode> openList;

    // Should be called before use
    public void InitialiseDijkstraPath(int width, int height)
    {
        openList = new SortedSet<DijkstraNode>(new ByTotalWeight());
        dijkstraGrid = new DijkstraNode[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // These should only need to be assigned/initialised once
                dijkstraGrid[x, y] = new DijkstraNode();

                dijkstraGrid[x, y].position.x = x;
                dijkstraGrid[x, y].position.y = y;

                dijkstraGrid[x, y].neighbours = new DijkstraNode[4];
            }
        }

        // Assign neighbours
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Ensure neighbours are within range before assigning
                if ((x) >= 0 && (x) < width && (y + 1) >= 0 && (y + 1) < height) dijkstraGrid[x, y].neighbours[0] = dijkstraGrid[(x), (y + 1)];
                if ((x - 1) >= 0 && (x - 1) < width && (y) >= 0 && (y) < height) dijkstraGrid[x, y].neighbours[1] = dijkstraGrid[(x - 1), (y)];
                if ((x + 1) >= 0 && (x + 1) < width && (y) >= 0 && (y) < height) dijkstraGrid[x, y].neighbours[2] = dijkstraGrid[(x + 1), (y)];
                if ((x) >= 0 && (x) < width && (y - 1) >= 0 && (y - 1) < height) dijkstraGrid[x, y].neighbours[3] = dijkstraGrid[(x), (y - 1)];
            }
        }

        ResetGraphData();
    }

    // Resets dynamic data of grid
    void ResetGraphData()
    {
        for (int y = 0; y < dijkstraGrid.GetLength(1); y++)
        {
            for (int x = 0; x < dijkstraGrid.GetLength(0); x++)
            {
                // Calculated when baking graph
                dijkstraGrid[x, y].previous = null;
                dijkstraGrid[x, y].totalWeight = int.MaxValue; // Important that this starts off the highest it can be
                dijkstraGrid[x, y].isClosed = false;
            }
        }
    }

    // The weight of a node at this position
    public void SetNodeWeight(int x, int y, int weight)
    {
        // Setting a weight of -1 means the node cannot be explored
        dijkstraGrid[x, y].weight = weight;
    }

    // The place the Dijkstra paths originate from
    public void SetTarget(int x, int y)
    {
        targetPosition.x = x;
        targetPosition.y = y;
    }

    // Calculates the Dijkstra map of all paths from the origin to every explorable point in the graph
    public void BakeFullGraph()
    {
        ResetGraphData();
        openList.Clear();

        // Set the start of the bake algorithm (target of the path)
        dijkstraGrid[targetPosition.x, targetPosition.y].totalWeight = 0;
        dijkstraGrid[targetPosition.x, targetPosition.y].weight = 0;
        openList.Add(dijkstraGrid[targetPosition.x, targetPosition.y]);

        // Set all nodes to opened
        for (int y = 0; y < dijkstraGrid.GetLength(1); y++)
        {
            for (int x = 0; x < dijkstraGrid.GetLength(0); x++)
            {
                dijkstraGrid[x, y].isClosed = false;
            }
        }

        // Continue until list is exhausted
        while (openList.Count > 0)
        {
            // Get the node with the lowest total weight
            DijkstraNode lightestNode = openList.Min;

            // For all neighbours of this node that are still in the open list
            foreach (DijkstraNode n in lightestNode.neighbours)
            {
                if (n != null && n.isClosed == false && n.weight != -1)
                {
                    // If this is a new shortest distance to this node
                    int thisTotalWeight = lightestNode.totalWeight + n.weight;
                    if (thisTotalWeight < n.totalWeight)
                    {
                        // Update the node with a new fastest neighbour and lowest weight
                        n.totalWeight = thisTotalWeight;
                        n.previous = lightestNode;
                    }

                    openList.Add(n);
                }
            }

            // This node has now been processed, remove it from the open list and set to closed
            openList.Remove(lightestNode);
            lightestNode.isClosed = true;
        }
    }

    // Returns the shortest path to a point, assuming the graph is baked
    public List<Vector2Int> ShortestPathTo(int x, int y)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPos = new Vector2Int(x, y);

        // Back trace until the origin is hit
        while (currentPos != targetPosition)
        {
            path.Add(currentPos);
            
            // Check if this step can be taken
            if (dijkstraGrid[currentPos.x, currentPos.y].previous == null)
            {
                // If the path doesn't exist, empty the list and return it
                path.Clear();
                return path;
            }

            // Move to this space
            currentPos = dijkstraGrid[currentPos.x, currentPos.y].previous.position;
        }

        // Return the completed path
        path.Add(targetPosition);
        return path;
    }
}