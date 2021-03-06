﻿using Pathfinder.Factories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinder
{
    public class Map : IMap
    {
        public Node[,] Nodes { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public Node StartNode { get { return _startNode; } set { DefineNode(value); _startNode = value; } }
        public Node EndNode { get { return _endNode; } set { DefineNode(value); _endNode = value; } }
        public DiagonalMovement Diagonal { get; set; }
        Node _startNode;
        Node _endNode;

        readonly object _lockClosed = new object();
        readonly object _lockOpen = new object();


        protected int _maxExpandedNodes { get; set; } = 0;
        private IList<Node> _openList { get; set; } = new List<Node>();
        private IList<Node> _closedList { get; set; } = new List<Node>();
        public MapGeneratorEnum MapType { get; set; }

        public virtual int GetMaxExpandedNodes() => _maxExpandedNodes;

        public Map(DiagonalMovement diagonal, int width, int height)
        {
            Setup(diagonal, width, height, 0, 0, width - 1, height - 1);
        }
        public Map(DiagonalMovement diagonal, int width, int height, int startNodeX, int startNodeY, int endNodeX, int endNodeY)
        {
            Setup(diagonal, width, height, startNodeX, startNodeY, endNodeX, endNodeY);
        }
        private void Setup(DiagonalMovement diagonal, int width, int height, int startNodeX, int startNodeY, int endNodeX, int endNodeY)
        {
            Diagonal = diagonal;
            Width = width;
            Height = height;
            Nodes = new Node[height, width];
            DefineAllNodes();
            StartNode = Nodes[startNodeY, startNodeX];
            EndNode = Nodes[endNodeY, endNodeX];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    Nodes[j, i].X = i;
                    Nodes[j, i].Y = j;
                    Nodes[j, i].Walkable = true;
                }
        }
        public Node this[int y, int x]
        {
            get { return Nodes[y, x]; }
            set { Nodes[y, x] = value; }
        }
        public bool IsInside(int y, int x)
        {
            return (x >= 0 && x < this.Width) && (y >= 0 && y < this.Height);
        }
        public bool IsWalkableAt(Node node)
        {
            return IsInside(node.Y, node.X) && node.Walkable;
        }
        public bool IsWalkableAt(int y, int x)
        {
            return IsInside(y, x) && Nodes[y, x].Walkable;
        }
        public Node GetDirectionNode(Node node, bool ByRef = true, bool valid = true)
        {
            var rand = RandomFactory.Rand;
            var dir = (DirectionMovement)rand.Next(1, Enum.GetNames(typeof(DirectionMovement)).Length);
            return GetDirectionNode(node, dir, ByRef, valid);
        }
        public Node GetDirectionNode(Node node, DirectionMovement direction, bool ByRef = true, bool valid = true)
        {
            var x = node.X;
            var y = node.Y;
            Node newnode = null;
            switch (direction)
            {
                case DirectionMovement.Up:
                    if (IsWalkableAt(y - 1, x) || (!valid && IsInside(y - 1, x)))
                        newnode = Nodes[y - 1, x];
                    break;
                case DirectionMovement.Down:
                    if (IsWalkableAt(y + 1, x) || (!valid && IsInside(y + 1, x)))
                        newnode = Nodes[y + 1, x];
                    break;
                case DirectionMovement.Left:
                    if (IsWalkableAt(y, x - 1) || (!valid && IsInside(y, x - 1)))
                        newnode = Nodes[y, x - 1];
                    break;
                case DirectionMovement.Right:
                    if (IsWalkableAt(y, x + 1) || (!valid && IsInside(y, x + 1)))
                        newnode = Nodes[y, x + 1];
                    break;
                // Diagonais
                case DirectionMovement.UpLeft:
                    if (IsWalkableAt(y - 1, x - 1) || (!valid && IsInside(y - 1, x - 1)))
                        newnode = Nodes[y - 1, x - 1];
                    break;
                case DirectionMovement.UpRight:
                    if (IsWalkableAt(y - 1, x + 1) || (!valid && IsInside(y - 1, x + 1)))
                        newnode = Nodes[y - 1, x + 1];
                    break;
                case DirectionMovement.DownLeft:
                    if (IsWalkableAt(y + 1, x - 1) || (!valid && IsInside(y + 1, x - 1)))
                        newnode = Nodes[y + 1, x - 1];
                    break;
                case DirectionMovement.DownRight:
                    if (IsWalkableAt(y + 1, x + 1) || (!valid && IsInside(y + 1, x + 1)))
                        newnode = Nodes[y + 1, x + 1];
                    break;
            }
            if (newnode == null)
                return null;
            newnode.Direction = direction;
            return ByRef ? newnode : new Node(newnode, node, direction);
        }
        public IList<Node> GetNeighbors(Node node, bool ByRef = true, bool valid = true)
        {
            return GetNeighbors(node, Diagonal, ByRef, valid);
        }
        public IList<Node> GetNeighbors(Node node, DiagonalMovement diag, bool ByRef = true, bool valid = true)
        {
            Node newnode;
            var neighbors = new List<Node>();
            var s0 = false;
            var d0 = false;
            var s1 = false;
            var d1 = false;
            var s2 = false;
            var d2 = false;
            var s3 = false;
            var d3 = false;
            newnode = GetDirectionNode(node, DirectionMovement.Up, ByRef, valid);
            if (newnode != null)
            {
                neighbors.Add(newnode);
                s0 = true;
            }
            newnode = GetDirectionNode(node, DirectionMovement.Down, ByRef, valid);
            if (newnode != null)
            {
                neighbors.Add(newnode);
                s2 = true;
            }
            newnode = GetDirectionNode(node, DirectionMovement.Left, ByRef, valid);
            if (newnode != null)
            {
                neighbors.Add(newnode);
                s1 = true;
            }
            newnode = GetDirectionNode(node, DirectionMovement.Right, ByRef, valid);
            if (newnode != null)
            {
                neighbors.Add(newnode);
                s3 = true;
            }
            if (diag == DiagonalMovement.Never)
            {
                return neighbors;
            }
            switch (diag)
            {
                case DiagonalMovement.OnlyWhenNoObstacles:
                    d0 = s3 && s0;
                    d1 = s0 && s1;
                    d2 = s1 && s2;
                    d3 = s2 && s3;
                    break;
                case DiagonalMovement.IfAtMostOneObstacle:
                    d0 = s3 || s0;
                    d1 = s0 || s1;
                    d2 = s1 || s2;
                    d3 = s2 || s3;
                    break;
                case DiagonalMovement.Always:
                    d0 = true;
                    d1 = true;
                    d2 = true;
                    d3 = true;
                    break;
                default:
                    throw new Exception("Incorrect value of diagonalMovement");
            }
            newnode = GetDirectionNode(node, DirectionMovement.UpRight, ByRef, valid);
            if (d0 && newnode != null)
                neighbors.Add(newnode);
            newnode = GetDirectionNode(node, DirectionMovement.UpLeft, ByRef, valid);
            if (d1 && newnode != null)
                neighbors.Add(newnode);
            newnode = GetDirectionNode(node, DirectionMovement.DownLeft, ByRef, valid);
            if (d2 && newnode != null)
                neighbors.Add(newnode);
            newnode = GetDirectionNode(node, DirectionMovement.DownRight, ByRef, valid);
            if (d3 && newnode != null)
                neighbors.Add(newnode);
            //if (neighbors.Any(e => e == null || !e.Walkable ))
            //    throw new Exception("NO!!");
            return neighbors;
        }
        public void DefineNode(Node node)
        {
            this[node.Y, node.X] = node;
        }
        public bool ValidMap()
        {
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    if (this[i, j] == null)
                        return false;
                    else if (this[i, j].X != j || this[i, j].Y != i)
                        return false;
            if (StartNode == null || EndNode == null)
                return false;
            return true;
        }
        public void DefineAllNodes(IList<Node> nodes)
        {
            foreach (var item in nodes)
                DefineNode(item);
        }
        public void DefineAllNodes()
        {
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    this[i, j] = new Node(j, i);
        }
        public void Clear()
        {
            UpdateOpenList(new List<Node>());
            UpdateClosedList(new List<Node>());
            UpdateMaxNodes();

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    var node = Nodes[i, j];
                    node.Cost = 0;
                    node.G = 0;
                    node.H = 0;
                    node.ParentNode = null;
                    node.Tested = false;
                    node.Collision = false;
                    node.RetainCount = 0;
                    node.Direction = DirectionMovement.None;
                }
        }

        public virtual IEnumerable<Node> GetPath()
        {
            var path = new List<Node>();
            var node = _endNode;
            while (node != null || node == _startNode)
            {
                path.Add(node);
                node = node.ParentNode;
            }
            return path;
        }

        public virtual void UpdateMaxNodes()
        {
            var atualNodes = _openList.Count + _closedList.Count;
            if (atualNodes >= _maxExpandedNodes)
                _maxExpandedNodes = atualNodes;
        }

        public void AddInOpenList(Node node)
        {
            lock (_lockOpen)
                _openList.Add(node);
        }
        public void AddInClosedList(Node node)
        {
            lock (_lockClosed)
                _closedList.Add(node);
        }


        public IEnumerable<Node> GetNodesInOpenList()
        {
            lock (_lockOpen)
                foreach (var item in _openList)
                    yield return item;

        }

        public IEnumerable<Node> GetNodesInClosedLit()
        {
            lock (_lockClosed)
                foreach (var item in _closedList)
                    yield return item;
        }


        public int OpenListCount()
        {
            lock (_lockOpen)
                return _openList.Count();
        }


        public int ClosedListCount()
        {
            lock (_lockClosed)
                return _closedList.Count();
        }

        public Node PopOpenList()
        {
            lock (_lockOpen)
                return _openList.Pop();

        }

        public void PushInOpenList(Node node)
        {
            lock (_lockOpen)
                _openList.Push(node);

        }

        public void OrderOpenList(Func<Node, object> predicate)
        {
            _openList = _openList.OrderByDescending(predicate).ToList();
        }


        public void UpdateOpenList(IList<Node> newList)
        {
            lock (_lockOpen)
                _openList = newList;
        }

        public void UpdateClosedList(IList<Node> newList)
        {
            lock (_lockClosed)
                _closedList = newList;
        }
        public virtual bool IsOpen(Node e)
        {
            lock (_lockOpen)
                return _openList.ToList().Exists(i => i != null && i.Equals(e));
        }
        public virtual bool IsClosed(Node e)
        {
            lock (_lockClosed)
                return _closedList.ToList().Exists(i => i != null && i.Equals(e));
        }

        public void SetMaxExpandedNodes(int value)
        {
            _maxExpandedNodes = value;
        }
    }
}