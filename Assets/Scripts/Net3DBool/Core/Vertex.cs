/*
The MIT License (MIT)

Copyright (c) 2014 Sebastian Loncar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

See:
D. H. Laidlaw, W. B. Trumbore, and J. F. Hughes.
"Constructive Solid Geometry for Polyhedral Objects"
SIGGRAPH Proceedings, 1986, p.161.

original author: Danilo Balby Silva Castanheira (danbalby@yahoo.com)

Ported from Java to C# by Sebastian Loncar, Web: http://www.loncar.de
Project: https://github.com/Arakis/Net3dBool

Optimized and refactored by: Lars Brubaker (larsbrubaker@matterhackers.com)
Project: https://github.com/MatterHackers/agg-sharp (an included library)
*/

using System.Collections.Generic;

namespace Net3dBool
{
    /// <summary>
    /// 表示 3D 面的顶点
    /// </summary>
    public class Vertex
    {
        public Vector3Double Position { get; private set; }

        /// <summary>
        ///通过边与自身相连的其他顶点
        /// </summary>
        public Vertex[] AdjacentVertices
        {
            get
            {
                Vertex[] vertices = new Vertex[adjacentVertices.Count];
                for (int i = 0; i < adjacentVertices.Count; i++) { vertices[i] = adjacentVertices[i]; }
                return vertices;
            }
        }
        private List<Vertex> adjacentVertices;

        /// <summary>
        /// 顶点状态，相对于其他对象
        /// </summary>
        public Status Status
        {
            get { return status; }
            set { if (value >= Status.UNKNOWN && value <= Status.BOUNDARY) { status = value; } }
        }
        private Status status;

        /// <summary>
        /// 公差，值的差小于此值认为相等
        /// </summary>
        private readonly static double EqualityTolerance = 1e-5f;

        //----------------------------------CONSTRUCTORS--------------------------------//

        /// <summary>
        /// 构造状态未知的顶点
        /// </summary>
        /// <param name="position">顶点位置</param>
        public Vertex(Vector3Double position)
        {
            Position = position;

            adjacentVertices = new List<Vertex>();
            status = Status.UNKNOWN;
        }

        /// <summary>
        /// 构造状态未知的顶点
        /// </summary>
        /// <param name="x">顶点 x 坐标</param>
        /// <param name="y">顶点 y 坐标</param>
        /// <param name="z">顶点 z 坐标</param>
        public Vertex(double x, double y, double z)
        {
            Position = new Vector3Double(x, y, z);

            adjacentVertices = new List<Vertex>();
            status = Status.UNKNOWN;
        }

        /// <summary>
        /// 构造指定状态的顶点
        /// </summary>
        /// <param name="position">顶点坐标</param>
        /// <param name="status">顶点状态：未知，边界，内部 或 外部</param>
        public Vertex(Vector3Double position, Status status)
        {
            Position = position;

            adjacentVertices = new List<Vertex>();
            this.status = status;
        }

        /// <summary>
        /// 构造指定状态的顶点
        /// </summary>
        /// <param name="x">顶点 x 坐标</param>
        /// <param name="y">顶点 y 坐标</param>
        /// <param name="z">顶点 z 坐标</param>
        /// <param name="status">顶点状态：未知，边界，内部 或 外部</param>
        public Vertex(double x, double y, double z, Status status)
        {
            Position = new Vector3Double(x, y, z);

            adjacentVertices = new List<Vertex>();
            this.status = status;
        }

        private Vertex() { }

        /// <summary>
        /// 复制顶点对象
        /// </summary>
        /// <returns>复制体实例</returns>
        public Vertex Clone()
        {
            Vertex clone = new Vertex
            {
                Position = Position,
                status = status,
                adjacentVertices = new List<Vertex>()
            };
            for (int i = 0; i < adjacentVertices.Count; i++)
            {
                clone.adjacentVertices.Add(adjacentVertices[i].Clone());
            }

            return clone;
        }

        public override string ToString() { return Position.ToString(); }

        /// <summary>
        /// 拥有相同位置的顶点被认为相等
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public bool Equals(Vertex vertex) { return Position.Equals(vertex.Position, EqualityTolerance); }

        //----------------------------------OTHERS--------------------------------------//

        /// <summary>
        /// 指定一个顶点加入自己的邻接点
        /// </summary>
        /// <param name="adjacentVertex"></param>
        public void AddAdjacentVertex(Vertex adjacentVertex)
        {
            if (!adjacentVertices.Contains(adjacentVertex))
            {
                adjacentVertices.Add(adjacentVertex);
            }
        }

        /// <summary>
        /// 为顶点自身及邻接点指定状态
        /// </summary>
        /// <param name="status"></param>
        public void Mark(Status status)
        {
            this.status = status;  // 指定自身状态
            Vertex[] adjacentVerts = AdjacentVertices;
            for (int i = 0; i < adjacentVerts.Length; i++)  // 指定邻接点状态
            { if (adjacentVerts[i].Status == Status.UNKNOWN) { adjacentVerts[i].Mark(status); } }
        }
    }
}

